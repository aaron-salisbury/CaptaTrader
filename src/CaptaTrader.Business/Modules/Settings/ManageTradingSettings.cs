using CaptaTrader.Business.Modules.Capabilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Settings;

/// <summary>
/// Reads, validates, and saves <see cref="TradingSettings"/>.
/// </summary>
public sealed class ManageTradingSettings
{
    private const string STRATEGY_KEY = "trading.strategy";
    private const string MAX_BUY_NOTIONAL_KEY = "trading.maxBuyNotional";
    private const string MAX_ORDERS_PER_HOUR_KEY = "trading.maxOrdersPerHour";
    private const string MINIMUM_NET_PROFIT_KEY = "trading.minimumNetProfit";
    private const string SHORT_TERM_TAX_RATE_KEY = "tax.shortTermRate";
    private const string LONG_TERM_TAX_RATE_KEY = "tax.longTermRate";

    private readonly IApplicationSettingStore _settingStore;
    private readonly TimeProvider _timeProvider;

    public ManageTradingSettings(IApplicationSettingStore settingStore, TimeProvider timeProvider)
    {
        _settingStore = settingStore ?? throw new ArgumentNullException(nameof(settingStore));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<TradingSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        TradingSettings defaults = TradingSettings.Default;

        return new TradingSettings(
            await _settingStore.GetAsync(STRATEGY_KEY, cancellationToken) ?? defaults.StrategyId,
            await GetDecimalAsync(MAX_BUY_NOTIONAL_KEY, defaults.MaxBuyNotional, cancellationToken),
            (int)await GetDecimalAsync(MAX_ORDERS_PER_HOUR_KEY, defaults.MaxOrdersPerHour, cancellationToken),
            await GetDecimalAsync(MINIMUM_NET_PROFIT_KEY, defaults.MinimumNetProfit, cancellationToken),
            await GetDecimalAsync(SHORT_TERM_TAX_RATE_KEY, defaults.ShortTermTaxRate, cancellationToken),
            await GetDecimalAsync(LONG_TERM_TAX_RATE_KEY, defaults.LongTermTaxRate, cancellationToken));
    }

    public async Task<CapabilityResult<TradingSettings>> SaveAsync(TradingSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        IReadOnlyList<string> errors = Validate(settings);

        if (errors.Count > 0)
        {
            return CapabilityResult<TradingSettings>.Failure(CapabilityStatus.InvalidRequest, string.Join(" ", errors), _timeProvider.GetUtcNow());
        }

        await _settingStore.SetAsync(STRATEGY_KEY, settings.StrategyId, cancellationToken);
        await SetDecimalAsync(MAX_BUY_NOTIONAL_KEY, settings.MaxBuyNotional, cancellationToken);
        await SetDecimalAsync(MAX_ORDERS_PER_HOUR_KEY, settings.MaxOrdersPerHour, cancellationToken);
        await SetDecimalAsync(MINIMUM_NET_PROFIT_KEY, settings.MinimumNetProfit, cancellationToken);
        await SetDecimalAsync(SHORT_TERM_TAX_RATE_KEY, settings.ShortTermTaxRate, cancellationToken);
        await SetDecimalAsync(LONG_TERM_TAX_RATE_KEY, settings.LongTermTaxRate, cancellationToken);

        return CapabilityResult<TradingSettings>.Success(settings, _timeProvider.GetUtcNow());
    }

    internal static IReadOnlyList<string> Validate(TradingSettings settings)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(settings.StrategyId))
        {
            errors.Add("A strategy is required.");
        }

        if (settings.MaxBuyNotional <= 0m)
        {
            errors.Add("Maximum buy amount must be greater than zero.");
        }

        if (settings.MaxOrdersPerHour < 1)
        {
            errors.Add("Maximum orders per hour must be at least one.");
        }

        if (settings.MinimumNetProfit < 0.01m)
        {
            errors.Add("Minimum net profit must be at least one cent.");
        }

        if (settings.ShortTermTaxRate is < 0m or >= 1m || settings.LongTermTaxRate is < 0m or >= 1m)
        {
            errors.Add("Tax rates must be from 0 up to (but not including) 1, for example 0.30 for 30%.");
        }

        return errors;
    }

    private async Task<decimal> GetDecimalAsync(string key, decimal fallback, CancellationToken cancellationToken)
    {
        string? value = await _settingStore.GetAsync(key, cancellationToken);

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private Task SetDecimalAsync(string key, decimal value, CancellationToken cancellationToken)
    {
        return _settingStore.SetAsync(key, value.ToString(CultureInfo.InvariantCulture), cancellationToken);
    }
}
