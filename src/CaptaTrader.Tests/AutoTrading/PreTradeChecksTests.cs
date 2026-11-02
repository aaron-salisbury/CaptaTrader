using CaptaTrader.Business.Modules.AutoTrading;
using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.MarketData;
using CaptaTrader.Business.Modules.Settings;
using CaptaTrader.Business.Modules.Strategies;
using CaptaTrader.Tests.Ledger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CaptaTrader.Tests.AutoTrading;

[TestClass]
public sealed class PreTradeChecksTests
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly BrokerageRules RULES = new(new FeeSchedule(0.0015m, 0.0025m), 1m, 8, 0m);

    [TestMethod]
    public void Buy_WithinLimits_Proceeds()
    {
        Assert.IsNull(PreTradeChecks.FindBlockingReason(TradeDecision.Buy(25m, "dip"), Context(cash: 100m, held: [], brokerBtc: 0m)));
    }

    [TestMethod]
    public void Buy_AboveConfiguredMaximum_IsBlocked()
    {
        Assert.IsNotNull(PreTradeChecks.FindBlockingReason(TradeDecision.Buy(26m, "dip"), Context(cash: 100m, held: [], brokerBtc: 0m)));
    }

    [TestMethod]
    public void Buy_WithoutEnoughCash_IsBlocked()
    {
        Assert.IsNotNull(PreTradeChecks.FindBlockingReason(TradeDecision.Buy(25m, "dip"), Context(cash: 10m, held: [], brokerBtc: 0m)));
    }

    [TestMethod]
    public void AnyOrder_WithNoBudgetLeftThisHour_IsBlocked()
    {
        StrategyContext context = Context(cash: 100m, held: [], brokerBtc: 0m) with { RemainingOrderBudget = 0 };

        Assert.IsNotNull(PreTradeChecks.FindBlockingReason(TradeDecision.Buy(25m, "dip"), context));
    }

    [TestMethod]
    public void Sell_OfBitcoinTheAppDidNotBuy_IsBlocked()
    {
        // The user holds 1 BTC at the brokerage, but the app bought only 0.001 of it.
        StrategyContext context = Context(cash: 0m, held: [LotAccountingTests.Lot(T0, 0.001m, 50m)], brokerBtc: 1m);

        Assert.IsNotNull(PreTradeChecks.FindBlockingReason(TradeDecision.Sell(0.5m, "profit"), context));
        Assert.IsNull(PreTradeChecks.FindBlockingReason(TradeDecision.Sell(0.001m, "profit"), context));
    }

    [TestMethod]
    public void Sell_BeyondBrokerageBalance_IsBlocked()
    {
        StrategyContext context = Context(cash: 0m, held: [LotAccountingTests.Lot(T0, 0.001m, 50m)], brokerBtc: 0.0005m);

        Assert.IsNotNull(PreTradeChecks.FindBlockingReason(TradeDecision.Sell(0.001m, "profit"), context));
    }

    private static StrategyContext Context(decimal cash, IReadOnlyList<TaxLot> held, decimal brokerBtc)
    {
        MarketTick tick = new(T0, 50_000m);

        return new StrategyContext(tick, [tick], new AccountSnapshot(cash, brokerBtc, T0), held, RULES, TradingSettings.Default, 10);
    }
}
