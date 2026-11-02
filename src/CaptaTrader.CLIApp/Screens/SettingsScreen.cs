using CaptaTrader.Business.Modules.AutoTrading;
using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Capabilities;
using CaptaTrader.Business.Modules.Providers;
using CaptaTrader.Business.Modules.Settings;
using CaptaTrader.Business.Modules.Strategies;
using CaptaTrader.Business.Modules.UserAccess;
using CaptaTrader.CLIApp.Base;
using PrettyConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CaptaTrader.CLIApp.Screens;

/// <summary>
/// Provider choices are presented by category from descriptors only; the host never sees an
/// Integration type. Demo mode hides real-account settings.
/// </summary>
internal sealed class SettingsScreen
{
    private const string BACK = "Back";

    private readonly ConfigureProviders _configureProviders;
    private readonly ManageTradingSettings _tradingSettings;
    private readonly StrategyCatalog _strategyCatalog;
    private readonly AutoTrader _autoTrader;
    private readonly UserSession _session;

    public SettingsScreen(ConfigureProviders configureProviders, ManageTradingSettings tradingSettings, StrategyCatalog strategyCatalog, AutoTrader autoTrader, UserSession session)
    {
        _configureProviders = configureProviders ?? throw new ArgumentNullException(nameof(configureProviders));
        _tradingSettings = tradingSettings ?? throw new ArgumentNullException(nameof(tradingSettings));
        _strategyCatalog = strategyCatalog ?? throw new ArgumentNullException(nameof(strategyCatalog));
        _autoTrader = autoTrader ?? throw new ArgumentNullException(nameof(autoTrader));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    internal async Task RunAsync()
    {
        while (true)
        {
            TradingSettings settings = await _tradingSettings.GetAsync();
            ProviderDescriptor brokerage = await _configureProviders.GetSelectedAsync(ProviderKind.Brokerage);
            ProviderDescriptor marketData = await _configureProviders.GetSelectedAsync(ProviderKind.MarketData);
            string strategyName = _strategyCatalog.FindDescriptor(settings.StrategyId)?.DisplayName ?? settings.StrategyId;

            Ui.Screen("Settings", _autoTrader.Status, _session.IsDemo);

            if (_autoTrader.IsRunning)
            {
                Ui.Warning("Auto-trading is running. Changes apply the next time it starts.");
                Console.NewLine();
            }

            string brokerageItem = $"Brokerage ........ {brokerage.DisplayName}";
            string marketDataItem = $"Market data ...... {marketData.DisplayName}";
            string apiKeysItem = "API keys";
            string strategyItem = $"Strategy ......... {strategyName}";
            string limitsItem = $"Trading limits ... max buy {Ui.Usd(settings.MaxBuyNotional)}, {settings.MaxOrdersPerHour} orders/hour, min profit {Ui.Usd(settings.MinimumNetProfit)}";
            string taxItem = $"Tax estimates .... short-term {Ui.Percent(settings.ShortTermTaxRate)}, long-term {Ui.Percent(settings.LongTermTaxRate)}";
            string testItem = "Test brokerage connection";

            List<string> items = _session.IsDemo
                ? [strategyItem, limitsItem, taxItem, testItem, BACK]
                : [brokerageItem, marketDataItem, apiKeysItem, strategyItem, limitsItem, taxItem, testItem, BACK];

            string? choice = Ui.Choose("Choose a setting", items);

            if (choice is null)
            {
                continue;
            }

            if (choice == BACK)
            {
                return;
            }

            if (choice == brokerageItem)
            {
                await ChooseProviderAsync(ProviderKind.Brokerage);
            }
            else if (choice == marketDataItem)
            {
                await ChooseProviderAsync(ProviderKind.MarketData);
            }
            else if (choice == apiKeysItem)
            {
                await ManageApiKeysAsync();
            }
            else if (choice == strategyItem)
            {
                await ChooseStrategyAsync(settings);
            }
            else if (choice == limitsItem)
            {
                await EditLimitsAsync(settings);
            }
            else if (choice == taxItem)
            {
                await EditTaxRatesAsync(settings);
            }
            else if (choice == testItem)
            {
                await TestBrokerageAsync();
            }
        }
    }

    private async Task ChooseProviderAsync(ProviderKind kind)
    {
        IReadOnlyList<ProviderStatus> statuses = await _configureProviders.GetStatusesAsync(kind);
        Console.NewLine();

        foreach (ProviderStatus status in statuses)
        {
            Console.WriteLineInterpolated($"  {Markup.Bold}{status.Descriptor.DisplayName}{Markup.Reset}{(status.Descriptor.IsSimulated ? "  (simulated)" : string.Empty)}");
            Ui.Hint("    " + status.Descriptor.Description);
        }

        string? choice = Ui.Choose(kind == ProviderKind.Brokerage ? "Which brokerage?" : "Which market data source?", [.. statuses.Select(Label), BACK]);
        ProviderStatus? selected = statuses.FirstOrDefault(status => Label(status) == choice);

        if (selected is null)
        {
            return;
        }

        CapabilityResult<ProviderDescriptor> result = await _configureProviders.SelectAsync(kind, selected.Descriptor.Id);

        if (!result.IsSuccess)
        {
            Ui.Failure(result);
            Ui.Pause();
            return;
        }

        Ui.Success($"{selected.Descriptor.DisplayName} selected.");

        if (!selected.IsReady && Console.Confirm($"It needs API keys. Enter them now? ({Color.Green}y{Color.Default}/{Color.Red}n{Color.Default}) "))
        {
            await EnterCredentialsAsync(selected);
        }
        else
        {
            Ui.Pause();
        }
    }

    private async Task ManageApiKeysAsync()
    {
        List<ProviderStatus> statuses = [];
        statuses.AddRange(await _configureProviders.GetStatusesAsync(ProviderKind.Brokerage));
        statuses.AddRange(await _configureProviders.GetStatusesAsync(ProviderKind.MarketData));
        statuses = [.. statuses.Where(status => status.Fields.Count > 0)];

        Console.NewLine();
        Ui.Hint("Keys are kept in your operating system's credential store, never in the app database.");

        if (statuses.Count == 0)
        {
            Ui.Hint("No installed provider needs API keys.");
            Ui.Pause();
            return;
        }

        string? choice = Ui.Choose("Which provider's keys?", [.. statuses.Select(Label), BACK]);
        ProviderStatus? selected = statuses.FirstOrDefault(status => Label(status) == choice);

        if (selected is null)
        {
            return;
        }

        const string ENTER = "Enter or replace keys";
        const string CLEAR = "Remove stored keys";
        string? action = Ui.Choose(selected.Descriptor.DisplayName, [ENTER, CLEAR, BACK]);

        if (action == ENTER)
        {
            await EnterCredentialsAsync(selected);
        }
        else if (action == CLEAR)
        {
            CapabilityResult<bool> result = await _configureProviders.ClearCredentialsAsync(selected.Descriptor.Id);

            if (result.IsSuccess)
            {
                Ui.Success("Keys removed.");
            }
            else
            {
                Ui.Failure(result);
            }

            Ui.Pause();
        }
    }

    private async Task EnterCredentialsAsync(ProviderStatus status)
    {
        Console.NewLine();
        Ui.Hint("Leave a value blank to keep the stored one.");

        foreach (CredentialFieldStatus field in status.Fields)
        {
            string label = field.IsSet ? field.Field.Label + " (set)" : field.Field.Label;
            string value = field.Field.IsSecret ? Ui.AskSecret(label) : Ui.Ask(label);

            if (value.Length == 0)
            {
                continue;
            }

            CapabilityResult<bool> result = await _configureProviders.SetCredentialAsync(status.Descriptor.Id, field.Field.Key, value);

            if (result.IsSuccess)
            {
                Ui.Success($"{field.Field.Label} saved.");
            }
            else
            {
                Ui.Failure(result);
            }
        }

        Ui.Pause();
    }

    private async Task ChooseStrategyAsync(TradingSettings settings)
    {
        IReadOnlyList<StrategyDescriptor> strategies = _strategyCatalog.Descriptors;
        Console.NewLine();

        foreach (StrategyDescriptor strategy in strategies)
        {
            Console.WriteLineInterpolated($"  {Markup.Bold}{strategy.DisplayName}{Markup.Reset}");
            Ui.Hint("    " + strategy.Description);
        }

        string? choice = Ui.Choose("Which strategy?", [.. strategies.Select(strategy => strategy.DisplayName), BACK]);
        StrategyDescriptor? selected = strategies.FirstOrDefault(strategy => strategy.DisplayName == choice);

        if (selected is not null)
        {
            await SaveAsync(settings with { StrategyId = selected.Id });
        }
    }

    private async Task EditLimitsAsync(TradingSettings settings)
    {
        Console.NewLine();
        Ui.Hint("Press Enter to keep a value. These limits apply to every strategy.");

        decimal? maxBuy = Ui.AskDecimal("Maximum USD per buy", settings.MaxBuyNotional);
        decimal? maxOrders = Ui.AskDecimal("Maximum orders per rolling hour", settings.MaxOrdersPerHour);
        decimal? minimumProfit = Ui.AskDecimal("Minimum net profit per sale, after fees and estimated tax (USD)", settings.MinimumNetProfit);

        if (maxBuy is null || maxOrders is null || minimumProfit is null)
        {
            Ui.Warning("Please enter numbers only.");
            Ui.Pause();
            return;
        }

        await SaveAsync(settings with { MaxBuyNotional = maxBuy.Value, MaxOrdersPerHour = (int)maxOrders.Value, MinimumNetProfit = minimumProfit.Value });
    }

    private async Task EditTaxRatesAsync(TradingSettings settings)
    {
        Console.NewLine();
        Ui.Hint("Estimates for deciding whether a sale is worth it, not for filing. Use your combined federal and");
        Ui.Hint("state marginal rate as a decimal, for example 0.32. Short-term gains are taxed as ordinary income.");

        decimal? shortTerm = Ui.AskDecimal("Short-term rate (held one year or less)", settings.ShortTermTaxRate);
        decimal? longTerm = Ui.AskDecimal("Long-term rate (held more than one year)", settings.LongTermTaxRate);

        if (shortTerm is null || longTerm is null)
        {
            Ui.Warning("Please enter numbers only.");
            Ui.Pause();
            return;
        }

        await SaveAsync(settings with { ShortTermTaxRate = shortTerm.Value, LongTermTaxRate = longTerm.Value });
    }

    private async Task TestBrokerageAsync()
    {
        Console.NewLine();
        CapabilityResult<AccountSnapshot> result = await Ui.SpinAsync(_configureProviders.TestBrokerageAsync(), "Connecting to the brokerage…");

        if (result.IsSuccess)
        {
            Ui.Success($"Connected. Cash {Ui.Usd(result.Value!.CashUsd)}, {Ui.Btc(result.Value.Bitcoin)}.");
        }
        else
        {
            Ui.Failure(result);
        }

        Ui.Pause();
    }

    private async Task SaveAsync(TradingSettings settings)
    {
        CapabilityResult<TradingSettings> result = await _tradingSettings.SaveAsync(settings);

        if (result.IsSuccess)
        {
            Ui.Success("Saved.");
        }
        else
        {
            Ui.Failure(result);
        }

        Ui.Pause();
    }

    private static string Label(ProviderStatus status)
    {
        string selected = status.IsSelected ? "● " : "  ";
        string ready = status.Fields.Count == 0 ? string.Empty : status.IsReady ? "  [keys set]" : "  [needs keys]";

        return selected + status.Descriptor.DisplayName + ready;
    }
}
