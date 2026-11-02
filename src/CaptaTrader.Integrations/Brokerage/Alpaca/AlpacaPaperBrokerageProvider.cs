using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Capabilities;
using CaptaTrader.Business.Modules.Providers;
using CaptaTrader.Business.Modules.Strategies;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CaptaTrader.Integrations.Brokerage.Alpaca;

/// <summary>
/// Alpaca's crypto trading API against its paper-trading environment: real API, real order
/// lifecycle, simulated money. A live-money Provider is deliberately a separate, later step.
/// </summary>
/// <remarks>
/// API reference: <see href="https://docs.alpaca.markets/reference"/>. Alpaca charges crypto fees in the
/// asset received, so a buy delivers slightly less BTC than <c>filled_qty</c>; the app records the net
/// quantity and an estimated USD fee so cost basis still equals the USD spent.
/// </remarks>
internal sealed class AlpacaPaperBrokerageProvider : IBrokerageProvider
{
    internal const string PROVIDER_ID = "alpaca-paper";
    internal const string KEY_ID_FIELD = "keyId";
    internal const string SECRET_KEY_FIELD = "secretKey";

    private const string PROVIDER_NAME = "Alpaca (paper)";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _timeProvider;

    public AlpacaPaperBrokerageProvider(IHttpClientFactory httpClientFactory, TimeProvider timeProvider)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public ProviderDescriptor Descriptor { get; } = new(
        PROVIDER_ID,
        ProviderKind.Brokerage,
        PROVIDER_NAME,
        "Alpaca crypto trading API in its paper environment. Create paper API keys at app.alpaca.markets.",
        IsSimulated: true,
        CredentialFields:
        [
            new CredentialField(KEY_ID_FIELD, "Alpaca API key ID", IsSecret: false),
            new CredentialField(SECRET_KEY_FIELD, "Alpaca API secret key", IsSecret: true)
        ]);

    /// <summary>Alpaca's entry crypto tier at the time of writing; verify against the account's fee page.</summary>
    public BrokerageRules Rules { get; } = new(
        new FeeSchedule(MakerRate: 0.0015m, TakerRate: 0.0025m),
        MinimumOrderNotional: 1m,
        QuantityDecimals: 8,
        ExpectedSlippageRate: 0.0005m);

    internal Uri BaseAddress { get; init; } = new("https://paper-api.alpaca.markets/");

    internal TimeSpan FillPollInterval { get; init; } = TimeSpan.FromMilliseconds(500);

    internal TimeSpan FillTimeout { get; init; } = TimeSpan.FromSeconds(20);

    public IBrokerage Connect(ProviderConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return new AlpacaBrokerage(this, connection.GetRequired(KEY_ID_FIELD), connection.GetRequired(SECRET_KEY_FIELD));
    }

    private sealed class AlpacaBrokerage : IBrokerage
    {
        private const string SYMBOL = "BTC/USD";
        private const string POSITION_SYMBOL = "BTCUSD";

        private readonly AlpacaPaperBrokerageProvider _provider;
        private readonly string _keyId;
        private readonly string _secretKey;

        internal AlpacaBrokerage(AlpacaPaperBrokerageProvider provider, string keyId, string secretKey)
        {
            _provider = provider;
            _keyId = keyId;
            _secretKey = secretKey;
        }

        public async Task<AccountSnapshot> GetAccountAsync(CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage accountResponse = await SendAsync(HttpMethod.Get, "v2/account", null, cancellationToken);
            await EnsureAuthorizedAsync(accountResponse, cancellationToken);
            AlpacaAccount account = await ReadAsync<AlpacaAccount>(accountResponse, cancellationToken);

            using HttpResponseMessage positionResponse = await SendAsync(HttpMethod.Get, $"v2/positions/{POSITION_SYMBOL}", null, cancellationToken);
            decimal bitcoin = 0m;

            if (positionResponse.StatusCode != HttpStatusCode.NotFound)
            {
                await EnsureAuthorizedAsync(positionResponse, cancellationToken);
                AlpacaPosition position = await ReadAsync<AlpacaPosition>(positionResponse, cancellationToken);
                bitcoin = ParseDecimal(position.Quantity);
            }

            return new AccountSnapshot(ParseDecimal(account.Cash), bitcoin, _provider._timeProvider.GetUtcNow());
        }

        public async Task<OrderResult> SubmitMarketOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            AlpacaOrderRequest body = new(
                SYMBOL,
                request.Side == OrderSide.Buy ? "buy" : "sell",
                "market",
                // Immediate-or-cancel: whatever cannot fill right away is canceled, so every order reaches a
                // final state quickly instead of resting on the book after the app stops watching it.
                "ioc",
                request.Side == OrderSide.Sell ? FormatDecimal(request.Quantity) : null,
                request.Side == OrderSide.Buy ? FormatDecimal(Math.Round(request.Notional ?? 0m, 2, MidpointRounding.ToZero)) : null,
                request.ClientOrderId.ToString("D"));

            using HttpResponseMessage response = await SendAsync(HttpMethod.Post, "v2/orders", body, cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized)
            {
                throw new CapabilityException(CapabilityStatus.Unauthorized, "Alpaca rejected the API keys.");
            }

            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.UnprocessableEntity)
            {
                // Alpaca uses 403 for insufficient buying power and 422 for invalid orders: refusals, not failures.
                return OrderResult.Rejected("Alpaca refused the order: " + await ReadErrorAsync(response, cancellationToken));
            }

            if (response.StatusCode is HttpStatusCode.TooManyRequests)
            {
                return OrderResult.Rejected("Alpaca rate limit reached; the order was not accepted.");
            }

            if (!response.IsSuccessStatusCode)
            {
                // A server error after sending is ambiguous: the order may exist. The client order ID allows reconciliation.
                return OrderResult.Unknown($"Alpaca returned {(int)response.StatusCode}; check order {request.ClientOrderId} in Alpaca.");
            }

            AlpacaOrder order;

            try
            {
                order = await ReadAsync<AlpacaOrder>(response, cancellationToken);
            }
            catch (Exception exception)
            {
                // Alpaca accepted the order, so an unreadable response says nothing about whether it exists or filled.
                return OrderResult.Unknown($"Alpaca accepted the order but its response could not be read ({exception.Message}); check order {request.ClientOrderId} in Alpaca.");
            }

            return await AwaitFillAsync(order, request, cancellationToken);
        }

        private async Task<OrderResult> AwaitFillAsync(AlpacaOrder order, OrderRequest request, CancellationToken cancellationToken)
        {
            DateTimeOffset deadline = _provider._timeProvider.GetUtcNow() + _provider.FillTimeout;

            while (true)
            {
                switch (order.Status)
                {
                    case "filled":
                        return ToFill(order, request);
                    case "canceled" or "expired" or "rejected":
                        return ParseDecimal(order.FilledQuantity) > 0m
                            ? ToFill(order, request)
                            : OrderResult.Rejected($"Alpaca order {order.Status}.", order.Id);
                }

                if (_provider._timeProvider.GetUtcNow() >= deadline)
                {
                    return OrderResult.Unknown($"Alpaca order {order.Id} was still '{order.Status}' after {_provider.FillTimeout.TotalSeconds:0}s.", order.Id);
                }

                try
                {
                    await Task.Delay(_provider.FillPollInterval, _provider._timeProvider, cancellationToken);

                    using HttpResponseMessage response = await SendAsync(HttpMethod.Get, $"v2/orders/{order.Id}", null, cancellationToken);
                    await EnsureAuthorizedAsync(response, cancellationToken);
                    order = await ReadAsync<AlpacaOrder>(response, cancellationToken);
                }
                catch (Exception exception)
                {
                    // Alpaca already accepted the order, so a failed status check says nothing about whether it filled.
                    return OrderResult.Unknown($"Alpaca accepted order {order.Id}, but checking its status failed ({exception.Message}); check it in Alpaca.", order.Id);
                }
            }
        }

        private OrderResult ToFill(AlpacaOrder order, OrderRequest request)
        {
            decimal grossQuantity = ParseDecimal(order.FilledQuantity);
            decimal price = ParseDecimal(order.FilledAveragePrice);

            if (grossQuantity <= 0m || price <= 0m)
            {
                // Missing or malformed fill data must not be recorded as a fill: the BTC would be untracked.
                return OrderResult.Unknown($"Alpaca reported order {order.Id} as '{order.Status}' without a valid filled quantity and price; check it in Alpaca.", order.Id);
            }

            decimal feeRate = _provider.Rules.Fees.TakerRate;
            decimal quantity;
            decimal fee;

            if (request.Side == OrderSide.Buy)
            {
                // The fee is taken in BTC; express it in USD so quantity * price + fee equals the USD spent.
                quantity = TradeEconomics.FloorQuantity(grossQuantity * (1m - feeRate), _provider.Rules.QuantityDecimals);
                fee = (grossQuantity - quantity) * price;
            }
            else
            {
                quantity = grossQuantity;
                fee = grossQuantity * price * feeRate;
            }

            DateTimeOffset filledAt = order.FilledAt ?? _provider._timeProvider.GetUtcNow();

            return new OrderResult(OrderResultStatus.Filled, order.Id, quantity, price, fee, FeeIsEstimated: true, filledAt, null);
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
        {
            HttpClient httpClient = _provider._httpClientFactory.CreateClient();
            using HttpRequestMessage message = new(method, new Uri(_provider.BaseAddress, path));
            message.Headers.Add("APCA-API-KEY-ID", _keyId);
            message.Headers.Add("APCA-API-SECRET-KEY", _secretKey);

            if (body is not null)
            {
                message.Content = JsonContent.Create(body, options: JSON_OPTIONS);
            }

            return await httpClient.SendAsync(message, cancellationToken);
        }

        private static async Task EnsureAuthorizedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new CapabilityException(CapabilityStatus.Unauthorized, "Alpaca rejected the API keys.");
            }

            if (response.StatusCode is HttpStatusCode.TooManyRequests)
            {
                throw new CapabilityException(CapabilityStatus.Unavailable, "Alpaca rate limit reached.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new CapabilityException(CapabilityStatus.Unavailable, $"Alpaca returned {(int)response.StatusCode}: {await ReadErrorAsync(response, cancellationToken)}");
            }
        }

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                T? value = await response.Content.ReadFromJsonAsync<T>(JSON_OPTIONS, cancellationToken);
                return value ?? throw new CapabilityException(CapabilityStatus.InvalidResponse, "Alpaca returned an empty response.");
            }
            catch (JsonException exception)
            {
                throw new CapabilityException(CapabilityStatus.InvalidResponse, "Alpaca returned an unexpected response.", exception);
            }
        }

        /// <summary>Alpaca error bodies are <c>{"code":…,"message":"…"}</c>; only the message is surfaced.</summary>
        private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                AlpacaError? error = await response.Content.ReadFromJsonAsync<AlpacaError>(JSON_OPTIONS, cancellationToken);
                return string.IsNullOrWhiteSpace(error?.Message) ? response.ReasonPhrase ?? "no details" : error.Message;
            }
            catch (JsonException)
            {
                return response.ReasonPhrase ?? "no details";
            }
        }
    }

    private static readonly JsonSerializerOptions JSON_OPTIONS = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static decimal ParseDecimal(string? value)
    {
        return decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out decimal parsed) ? parsed : 0m;
    }

    private static string? FormatDecimal(decimal? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
    }

    private sealed record AlpacaOrderRequest(
        [property: JsonPropertyName("symbol")] string Symbol,
        [property: JsonPropertyName("side")] string Side,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("time_in_force")] string TimeInForce,
        [property: JsonPropertyName("qty")] string? Quantity,
        [property: JsonPropertyName("notional")] string? Notional,
        [property: JsonPropertyName("client_order_id")] string ClientOrderId);

    private sealed record AlpacaOrder(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("filled_qty")] string? FilledQuantity,
        [property: JsonPropertyName("filled_avg_price")] string? FilledAveragePrice,
        [property: JsonPropertyName("filled_at")] DateTimeOffset? FilledAt);

    private sealed record AlpacaAccount([property: JsonPropertyName("cash")] string? Cash);

    private sealed record AlpacaPosition([property: JsonPropertyName("qty")] string? Quantity);

    private sealed record AlpacaError([property: JsonPropertyName("message")] string? Message);
}
