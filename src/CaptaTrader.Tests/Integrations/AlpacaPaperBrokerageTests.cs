using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Capabilities;
using CaptaTrader.Business.Modules.Credentials;
using CaptaTrader.Business.Modules.Providers;
using CaptaTrader.Integrations.Brokerage.Alpaca;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CaptaTrader.Tests.Integrations;

[TestClass]
public sealed class AlpacaPaperBrokerageTests
{
    private static readonly ProviderConnection CONNECTION = new(new Dictionary<string, CredentialMaterial>
    {
        [AlpacaPaperBrokerageProvider.KEY_ID_FIELD] = new("key-id"),
        [AlpacaPaperBrokerageProvider.SECRET_KEY_FIELD] = new("secret")
    });

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task GetAccount_SendsKeyHeaders_AndTreatsMissingPositionAsZero()
    {
        FakeHttp http = new((request, _) => request.RequestUri!.AbsolutePath switch
        {
            "/v2/account" => FakeHttp.Json(HttpStatusCode.OK, """{"cash":"1234.56"}"""),
            _ => FakeHttp.Json(HttpStatusCode.NotFound, """{"message":"position does not exist"}""")
        });

        AccountSnapshot account = await Connect(http).GetAccountAsync(TestContext.CancellationToken);

        Assert.AreEqual(1234.56m, account.CashUsd);
        Assert.AreEqual(0m, account.Bitcoin);
        HttpRequestMessage first = http.Requests[0].Request;
        Assert.AreEqual("key-id", first.Headers.GetValues("APCA-API-KEY-ID").Single());
        Assert.AreEqual("secret", first.Headers.GetValues("APCA-API-SECRET-KEY").Single());
        Assert.StartsWith("https://paper-api.alpaca.markets/", first.RequestUri!.ToString());
    }

    [TestMethod]
    public async Task GetAccount_WithBadKeys_ThrowsUnauthorized()
    {
        FakeHttp http = new((_, _) => FakeHttp.Json(HttpStatusCode.Unauthorized, """{"message":"unauthorized"}"""));

        CapabilityException exception = await Assert.ThrowsExactlyAsync<CapabilityException>(() => Connect(http).GetAccountAsync(TestContext.CancellationToken));

        Assert.AreEqual(CapabilityStatus.Unauthorized, exception.Status);
    }

    [TestMethod]
    public async Task Buy_PollsUntilFilled_AndRecordsNetQuantityWithUsdFee()
    {
        int polls = 0;
        FakeHttp http = new((request, _) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                return FakeHttp.Json(HttpStatusCode.OK, """{"id":"order-1","status":"accepted","filled_qty":"0","filled_avg_price":null,"filled_at":null}""");
            }

            polls++;
            return FakeHttp.Json(HttpStatusCode.OK, """{"id":"order-1","status":"filled","filled_qty":"0.0005","filled_avg_price":"50000","filled_at":"2026-06-01T12:00:01Z"}""");
        });

        Guid clientOrderId = Guid.NewGuid();
        OrderResult result = await Connect(http).SubmitMarketOrderAsync(
            new OrderRequest(clientOrderId, OrderSide.Buy, null, 25.009m, 50_000m, DateTimeOffset.UtcNow),
            TestContext.CancellationToken);

        Assert.AreEqual(OrderResultStatus.Filled, result.Status);
        Assert.AreEqual(1, polls);
        Assert.AreEqual(0.00049875m, result.FilledQuantity);
        Assert.AreEqual(25m, (result.FilledQuantity * result.AveragePrice) + result.Fee, "Basis must equal the USD spent.");
        Assert.IsTrue(result.FeeIsEstimated);

        string body = http.Requests[0].Body!;
        StringAssert.Contains(body, "\"symbol\":\"BTC/USD\"");
        StringAssert.Contains(body, "\"notional\":\"25.00\"");
        StringAssert.Contains(body, $"\"client_order_id\":\"{clientOrderId:D}\"");
        Assert.DoesNotContain("\"qty\"", body);
    }

    [TestMethod]
    [DataRow("""{"id":"order-1","status":"filled","filled_qty":null,"filled_avg_price":"50000"}""")]
    [DataRow("""{"id":"order-1","status":"filled","filled_qty":"0.0005","filled_avg_price":"n/a"}""")]
    public async Task FilledWithoutValidQuantityOrPrice_IsUnknown(string orderJson)
    {
        FakeHttp http = new((_, _) => FakeHttp.Json(HttpStatusCode.OK, orderJson));

        OrderResult result = await Connect(http).SubmitMarketOrderAsync(
            new OrderRequest(Guid.NewGuid(), OrderSide.Buy, null, 25m, 50_000m, DateTimeOffset.UtcNow),
            TestContext.CancellationToken);

        Assert.AreEqual(OrderResultStatus.Unknown, result.Status);
        Assert.AreEqual("order-1", result.BrokerOrderId);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    public async Task AcceptedOrder_WhoseStatusCheckFails_IsUnknown(HttpStatusCode pollStatus)
    {
        FakeHttp http = new((request, _) => request.Method == HttpMethod.Post
            ? FakeHttp.Json(HttpStatusCode.OK, """{"id":"order-1","status":"accepted","filled_qty":"0"}""")
            : FakeHttp.Json(pollStatus, """{"message":"nope"}"""));

        OrderResult result = await Connect(http).SubmitMarketOrderAsync(
            new OrderRequest(Guid.NewGuid(), OrderSide.Buy, null, 25m, 50_000m, DateTimeOffset.UtcNow),
            TestContext.CancellationToken);

        Assert.AreEqual(OrderResultStatus.Unknown, result.Status);
        Assert.AreEqual("order-1", result.BrokerOrderId);
    }

    [TestMethod]
    public async Task AcceptedOrder_WithUnreadableResponse_IsUnknown()
    {
        FakeHttp http = new((_, _) => FakeHttp.Json(HttpStatusCode.OK, "not json"));

        OrderResult result = await Connect(http).SubmitMarketOrderAsync(
            new OrderRequest(Guid.NewGuid(), OrderSide.Buy, null, 25m, 50_000m, DateTimeOffset.UtcNow),
            TestContext.CancellationToken);

        Assert.AreEqual(OrderResultStatus.Unknown, result.Status);
    }

    [TestMethod]
    public async Task Order_RefusedForBuyingPower_IsRejectedNotThrown()
    {
        FakeHttp http = new((_, _) => FakeHttp.Json(HttpStatusCode.Forbidden, """{"code":40310000,"message":"insufficient balance for USD"}"""));

        OrderResult result = await Connect(http).SubmitMarketOrderAsync(
            new OrderRequest(Guid.NewGuid(), OrderSide.Sell, 0.001m, null, 50_000m, DateTimeOffset.UtcNow),
            TestContext.CancellationToken);

        Assert.AreEqual(OrderResultStatus.Rejected, result.Status);
        StringAssert.Contains(result.Message, "insufficient balance");
    }

    [TestMethod]
    public async Task Order_ServerErrorAfterSending_IsUnknown()
    {
        FakeHttp http = new((_, _) => FakeHttp.Json(HttpStatusCode.BadGateway, "{}"));

        OrderResult result = await Connect(http).SubmitMarketOrderAsync(
            new OrderRequest(Guid.NewGuid(), OrderSide.Sell, 0.001m, null, 50_000m, DateTimeOffset.UtcNow),
            TestContext.CancellationToken);

        Assert.AreEqual(OrderResultStatus.Unknown, result.Status);
    }

    private static IBrokerage Connect(FakeHttp http)
    {
        AlpacaPaperBrokerageProvider provider = new(http, TimeProvider.System) { FillPollInterval = TimeSpan.FromMilliseconds(1) };

        return provider.Connect(CONNECTION);
    }
}
