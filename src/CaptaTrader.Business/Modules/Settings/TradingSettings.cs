using CaptaTrader.Business.Modules.Strategies;

namespace CaptaTrader.Business.Modules.Settings;

/// <summary>
/// User-adjustable trading preferences and safety limits.
/// </summary>
/// <param name="StrategyId">Selected trading strategy.</param>
/// <param name="MaxBuyNotional">Largest USD amount a single buy may spend.</param>
/// <param name="MaxOrdersPerHour">Upper bound on order attempts in any rolling hour, regardless of strategy.</param>
/// <param name="MinimumNetProfit">Smallest after-fee, after-estimated-tax profit (USD) a profit-taking sell must clear.</param>
/// <param name="ShortTermTaxRate">Estimated combined marginal rate applied to gains held one year or less.</param>
/// <param name="LongTermTaxRate">Estimated combined marginal rate applied to gains held more than one year.</param>
public sealed record TradingSettings(
    string StrategyId,
    decimal MaxBuyNotional,
    int MaxOrdersPerHour,
    decimal MinimumNetProfit,
    decimal ShortTermTaxRate,
    decimal LongTermTaxRate)
{
    public static TradingSettings Default { get; } = new(
        StrategyId: RandomVolatilityStrategy.STRATEGY_ID,
        MaxBuyNotional: 25m,
        MaxOrdersPerHour: 60,
        MinimumNetProfit: 0.01m,
        ShortTermTaxRate: 0.30m,
        LongTermTaxRate: 0.15m);

    public TaxRates TaxRates => new(ShortTermTaxRate, LongTermTaxRate);
}

/// <summary>
/// Estimated marginal tax rates used only for pre-trade profitability estimates, never for filing.
/// </summary>
public sealed record TaxRates(decimal ShortTerm, decimal LongTerm);
