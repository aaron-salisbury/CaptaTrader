using System;
using System.Linq;

namespace CaptaTrader.Business.Modules.Strategies;

/// <summary>
/// Placeholder strategy that exercises the whole trading pipeline. It buys at random on short dips,
/// but not while the market is falling hard (short average below the long average), and sells
/// everything it holds as soon as the sale clears the minimum after-fee, after-tax profit.
/// It never sells at a loss. It has no edge and is not meant to make money.
/// </summary>
/// <remarks>
/// Because lots are matched first-in, first-out, one lot bought high blocks every later sale until the
/// price recovers past it, even when newer lots are profitable. The falling-market filter exists only
/// so the demo does not buy the top of a crash and sit idle all day.
/// </remarks>
public sealed class RandomVolatilityStrategy : ITradingStrategy
{
    public const string STRATEGY_ID = "random-volatility";

    private const int SHORT_AVERAGE_TICKS = 20;
    private const int LONG_AVERAGE_TICKS = 240;
    private const double BUY_PROBABILITY = 0.35;
    private const int MAXIMUM_OPEN_BUYS = 10;

    private readonly int? _seed;
    private Random _random;

    public RandomVolatilityStrategy()
        : this(null)
    {
    }

    internal RandomVolatilityStrategy(int? seed)
    {
        _seed = seed;
        _random = CreateRandom(seed);
    }

    public StrategyDescriptor Descriptor { get; } = new(
        STRATEGY_ID,
        "Random volatility (placeholder)",
        "Randomly buys short dips unless the market is falling hard, and sells once a sale clears the minimum net profit. Never sells at a loss. For testing the pipeline only.");

    public void Reset()
    {
        _random = CreateRandom(_seed);
    }

    public TradeDecision Evaluate(StrategyContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.RecentTicks.Count < LONG_AVERAGE_TICKS)
        {
            return TradeDecision.Hold("Collecting price history.");
        }

        decimal held = TradeEconomics.FloorQuantity(context.HeldQuantity, context.Rules.QuantityDecimals);

        if (held > 0m)
        {
            SellEstimate estimate = TradeEconomics.EstimateSell(context.OpenLots, held, context.SellPrice, context.Rules, context.Settings.TaxRates, context.Tick.Time);

            // Take any profit that clears the minimum, however small, as soon as it appears.
            if (estimate.NetProfitAfterTax >= context.Settings.MinimumNetProfit)
            {
                return TradeDecision.Sell(held, FormattableString.Invariant($"Take profit: about ${estimate.NetProfitAfterTax:0.00} after fees and estimated tax."));
            }
        }

        decimal shortAverage = context.RecentTicks.TakeLast(SHORT_AVERAGE_TICKS).Average(tick => tick.Price);
        decimal longAverage = context.RecentTicks.TakeLast(LONG_AVERAGE_TICKS).Average(tick => tick.Price);

        if (shortAverage < longAverage)
        {
            return TradeDecision.Hold("Falling market; not buying.");
        }

        if (context.Tick.Price >= shortAverage)
        {
            return TradeDecision.Hold("No dip.");
        }

        decimal notional = Math.Min(context.Settings.MaxBuyNotional, context.Account.CashUsd);

        if (notional >= context.Rules.MinimumOrderNotional
            && context.OpenLots.Count < MAXIMUM_OPEN_BUYS
            && _random.NextDouble() < BUY_PROBABILITY)
        {
            decimal discount = (shortAverage - context.Tick.Price) / shortAverage;
            return TradeDecision.Buy(notional, FormattableString.Invariant($"Dip {discount * 100m:0.00}% below the {SHORT_AVERAGE_TICKS}-tick average."));
        }

        return TradeDecision.Hold("Dip seen; random draw declined to buy.");
    }

    private static Random CreateRandom(int? seed)
    {
        return seed is null ? new Random() : new Random(seed.Value);
    }
}
