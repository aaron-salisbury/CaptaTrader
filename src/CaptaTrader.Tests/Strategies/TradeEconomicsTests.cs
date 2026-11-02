using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.MarketData;
using CaptaTrader.Business.Modules.Settings;
using CaptaTrader.Business.Modules.Strategies;
using CaptaTrader.Tests.Ledger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaptaTrader.Tests.Strategies;

[TestClass]
public sealed class TradeEconomicsTests
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly BrokerageRules RULES = new(new FeeSchedule(0.0015m, 0.0025m), 1m, 8, 0m);
    private static readonly TaxRates RATES = new(0.30m, 0.15m);

    [TestMethod]
    public void EstimateSell_DeductsFeeThenTaxesTheGain()
    {
        TaxLot lot = LotAccountingTests.Lot(T0, 0.001m, 50m);

        SellEstimate estimate = TradeEconomics.EstimateSell([lot], 0.001m, 60_000m, RULES, RATES, T0.AddHours(1));

        Assert.AreEqual(60m, estimate.GrossProceeds);
        Assert.AreEqual(0.15m, estimate.Fee);
        Assert.AreEqual(9.85m, estimate.RealizedGain);
        Assert.AreEqual(2.955m, estimate.EstimatedTax);
        Assert.AreEqual(6.895m, estimate.NetProfitAfterTax);
    }

    [TestMethod]
    public void EstimateSell_UsesLongTermRate_AfterMoreThanAYear()
    {
        TaxLot lot = LotAccountingTests.Lot(T0, 0.001m, 50m);

        SellEstimate estimate = TradeEconomics.EstimateSell([lot], 0.001m, 60_000m, RULES, RATES, T0.AddYears(1).AddDays(1));

        Assert.AreEqual(9.85m * 0.15m, estimate.EstimatedTax);
    }

    [TestMethod]
    public void EstimateSell_ALoss_IsNotCreditedWithTaxSavings()
    {
        TaxLot lot = LotAccountingTests.Lot(T0, 0.001m, 50m);

        SellEstimate estimate = TradeEconomics.EstimateSell([lot], 0.001m, 40_000m, RULES, RATES, T0.AddHours(1));

        Assert.AreEqual(0m, estimate.EstimatedTax);
        Assert.AreEqual(estimate.RealizedGain, estimate.NetProfitAfterTax);
        Assert.IsLessThan(0m, estimate.NetProfitAfterTax);
    }

    [TestMethod]
    public void EstimateSell_IncludesExpectedSlippage()
    {
        TaxLot lot = LotAccountingTests.Lot(T0, 0.001m, 50m);
        BrokerageRules slippery = RULES with { ExpectedSlippageRate = 0.001m };

        SellEstimate estimate = TradeEconomics.EstimateSell([lot], 0.001m, 60_000m, slippery, RATES, T0.AddHours(1));

        Assert.AreEqual(59.94m, estimate.GrossProceeds);
    }

    [TestMethod]
    public void Tax_NeverTurnsAGainIntoALoss()
    {
        TaxLot lot = LotAccountingTests.Lot(T0, 0.001m, 50m);

        // Find the smallest price whose after-fee gain is positive; the after-tax result must stay positive.
        for (decimal price = 50_100m; price < 50_200m; price += 1m)
        {
            SellEstimate estimate = TradeEconomics.EstimateSell([lot], 0.001m, price, RULES, RATES, T0.AddHours(1));

            if (estimate.RealizedGain > 0m)
            {
                Assert.IsGreaterThan(0m, estimate.NetProfitAfterTax);
            }
        }
    }

    [TestMethod]
    public void FloorQuantity_NeverRoundsUp()
    {
        Assert.AreEqual(0.12345678m, TradeEconomics.FloorQuantity(0.123456789m, 8));
    }

    [TestMethod]
    public void RandomStrategy_SellsAsSoonAsMinimumProfitClears_AndNeverAtALoss()
    {
        RandomVolatilityStrategy strategy = new(seed: 7);
        TaxLot lot = LotAccountingTests.Lot(T0, 0.001m, 50m);
        List<MarketTick> history = [.. Enumerable.Range(0, 240).Select(index => new MarketTick(T0.AddSeconds(index), 50_000m))];

        // Below break-even after fees: hold.
        TradeDecision hold = strategy.Evaluate(Context(history, new MarketTick(T0.AddMinutes(5), 50_100m), [lot]));
        Assert.AreNotEqual(TradeAction.Sell, hold.Action);

        // Comfortably above: sell everything the app holds.
        TradeDecision sell = strategy.Evaluate(Context(history, new MarketTick(T0.AddMinutes(5), 51_000m), [lot]));
        Assert.AreEqual(TradeAction.Sell, sell.Action);
        Assert.AreEqual(0.001m, sell.Quantity);
    }

    private static StrategyContext Context(List<MarketTick> history, MarketTick tick, IReadOnlyList<TaxLot> lots)
    {
        List<MarketTick> ticks = [.. history, tick];

        return new StrategyContext(tick, ticks, new AccountSnapshot(1_000m, lots.Sum(lot => lot.RemainingQuantity), tick.Time), lots, RULES, TradingSettings.Default, 10);
    }
}
