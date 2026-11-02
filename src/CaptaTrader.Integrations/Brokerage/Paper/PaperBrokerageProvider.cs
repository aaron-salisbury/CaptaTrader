using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Providers;
using CaptaTrader.Business.Modules.Strategies;

namespace CaptaTrader.Integrations.Brokerage.Paper;

/// <summary>
/// A simulated brokerage that fills market orders immediately at the reference price, adjusted for
/// slippage, and charges a typical taker fee. Balances persist so paper trading survives restarts.
/// </summary>
internal sealed class PaperBrokerageProvider : IBrokerageProvider
{
    internal const string PROVIDER_ID = "paper";
    internal const decimal STARTING_CASH = 1_000m;

    private readonly IPaperAccountStore _accountStore;
    private readonly TimeSpan _simulatedLatency;

    public PaperBrokerageProvider(IPaperAccountStore accountStore)
        : this(accountStore, TimeSpan.FromMilliseconds(150))
    {
    }

    internal PaperBrokerageProvider(IPaperAccountStore accountStore, TimeSpan simulatedLatency)
    {
        _accountStore = accountStore ?? throw new ArgumentNullException(nameof(accountStore));
        _simulatedLatency = simulatedLatency;
    }

    public ProviderDescriptor Descriptor { get; } = new(
        PROVIDER_ID,
        ProviderKind.Brokerage,
        "Paper trading (simulated)",
        $"Practice account that starts with {STARTING_CASH:N0} USD. No real money; excluded from tax reports.",
        IsSimulated: true,
        CredentialFields: []);

    /// <summary>
    /// Fees mirror a common entry-level crypto taker tier so paper results are not flattering. Market
    /// orders cross the spread; 5 basis points of slippage approximates that cost, and fills use exactly it.
    /// </summary>
    public BrokerageRules Rules { get; } = new(
        new FeeSchedule(MakerRate: 0.0015m, TakerRate: 0.0025m),
        MinimumOrderNotional: 1m,
        QuantityDecimals: 8,
        ExpectedSlippageRate: 0.0005m);

    private decimal SlippageRate => Rules.ExpectedSlippageRate;

    public IBrokerage Connect(ProviderConnection connection)
    {
        return new PaperBrokerage(this);
    }

    private sealed class PaperBrokerage : IBrokerage
    {
        private static readonly SemaphoreSlim GATE = new(1, 1);

        private readonly PaperBrokerageProvider _provider;

        internal PaperBrokerage(PaperBrokerageProvider provider)
        {
            _provider = provider;
        }

        public async Task<AccountSnapshot> GetAccountAsync(CancellationToken cancellationToken = default)
        {
            PaperAccount account = await _provider._accountStore.GetOrCreateAsync(STARTING_CASH, cancellationToken);

            return new AccountSnapshot(account.CashUsd, account.Bitcoin, DateTimeOffset.UtcNow);
        }

        public async Task<OrderResult> SubmitMarketOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (_provider._simulatedLatency > TimeSpan.Zero)
            {
                await Task.Delay(_provider._simulatedLatency, cancellationToken);
            }

            await GATE.WaitAsync(cancellationToken);

            try
            {
                PaperAccount account = await _provider._accountStore.GetOrCreateAsync(STARTING_CASH, cancellationToken);
                (OrderResult result, PaperAccount updated) = request.Side == OrderSide.Buy
                    ? Buy(request, account)
                    : Sell(request, account);

                if (result.Status == OrderResultStatus.Filled)
                {
                    await _provider._accountStore.SaveAsync(updated, cancellationToken);
                }

                return result;
            }
            finally
            {
                GATE.Release();
            }
        }

        private (OrderResult Result, PaperAccount Account) Buy(OrderRequest request, PaperAccount account)
        {
            decimal notional = request.Notional ?? 0m;

            if (notional < _provider.Rules.MinimumOrderNotional)
            {
                return (OrderResult.Rejected("Order is below the minimum notional."), account);
            }

            if (notional > account.CashUsd)
            {
                return (OrderResult.Rejected("Insufficient paper cash."), account);
            }

            decimal price = request.ReferencePrice * (1m + _provider.SlippageRate);
            decimal fee = notional * _provider.Rules.Fees.TakerRate;
            decimal quantity = TradeEconomics.FloorQuantity((notional - fee) / price, _provider.Rules.QuantityDecimals);
            decimal spent = (quantity * price) + fee;

            OrderResult result = new(OrderResultStatus.Filled, PaperOrderId(), quantity, price, fee, false, request.RequestedAt, null);
            return (result, account with { CashUsd = account.CashUsd - spent, Bitcoin = account.Bitcoin + quantity });
        }

        private (OrderResult Result, PaperAccount Account) Sell(OrderRequest request, PaperAccount account)
        {
            decimal quantity = request.Quantity ?? 0m;

            if (quantity <= 0m || quantity > account.Bitcoin)
            {
                return (OrderResult.Rejected("Insufficient paper BTC."), account);
            }

            decimal price = request.ReferencePrice * (1m - _provider.SlippageRate);
            decimal gross = quantity * price;

            if (gross < _provider.Rules.MinimumOrderNotional)
            {
                return (OrderResult.Rejected("Order is below the minimum notional."), account);
            }

            decimal fee = gross * _provider.Rules.Fees.TakerRate;

            OrderResult result = new(OrderResultStatus.Filled, PaperOrderId(), quantity, price, fee, false, request.RequestedAt, null);
            return (result, account with { CashUsd = account.CashUsd + gross - fee, Bitcoin = account.Bitcoin - quantity });
        }

        private static string PaperOrderId()
        {
            return "paper-" + Guid.NewGuid().ToString("N")[..12];
        }
    }
}
