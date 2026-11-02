using CaptaTrader.Business.Modules.AutoTrading;
using CaptaTrader.Business.Modules.Providers;
using CaptaTrader.Business.Modules.Strategies;
using CaptaTrader.CLIApp;
using CaptaTrader.CLIApp.Screens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CaptaTrader.Tests;

/// <summary>
/// Resolves the executable's real composition without running it, so a missing registration fails in
/// tests rather than at startup.
/// </summary>
[TestClass]
public sealed class CompositionTests
{
    [TestMethod]
    public async Task CompositionRoot_ResolvesEveryScreenAndProvider()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = CLIApp.DependencyInjection.BuildServiceCollection(directory.Path)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsNotNull(services.GetRequiredService<LoginScreen>());
        Assert.IsNotNull(services.GetRequiredService<MainMenuScreen>());
        Assert.IsNotNull(services.GetRequiredService<AutoTrader>());

        ProviderCatalog catalog = services.GetRequiredService<ProviderCatalog>();
        Assert.IsTrue(catalog.GetDescriptors(ProviderKind.Brokerage)[0].IsSimulated, "A fresh install must default to a simulated brokerage.");
        Assert.IsGreaterThanOrEqualTo(2, catalog.GetDescriptors(ProviderKind.MarketData).Count);
        Assert.Contains(strategy => strategy.Id == TradingSettingsDefaultStrategy, services.GetRequiredService<StrategyCatalog>().Descriptors);

        await Serilog.Log.CloseAndFlushAsync();
    }

    private static string TradingSettingsDefaultStrategy => Business.Modules.Settings.TradingSettings.Default.StrategyId;
}

[TestClass]
public sealed class AppOptionsTests
{
    [TestMethod]
    public void NoArguments_StartsNormally()
    {
        Assert.AreEqual(new AppOptions(false, false, false, null), AppOptions.Parse([]));
    }

    [TestMethod]
    public void Demo_IsRecognized_CaseInsensitively()
    {
        Assert.IsTrue(AppOptions.Parse(["--DEMO"]).IsDemo);
    }

    [TestMethod]
    public void UnknownArgument_ShowsHelpWithAnError()
    {
        AppOptions options = AppOptions.Parse(["--live"]);

        Assert.IsTrue(options.ShowHelp);
        Assert.IsNotNull(options.Error);
        Assert.IsFalse(options.IsDemo);
    }
}

[TestClass]
public sealed class SingleInstanceTests
{
    [TestMethod]
    public void SecondCopy_CannotAcquireTheLock_UntilTheFirstReleasesIt()
    {
        using TestDirectory directory = new();

        using (System.IO.FileStream? first = CaptaTrader.CLIApp.Base.SingleInstance.TryAcquire(directory.Path))
        {
            Assert.IsNotNull(first);
            Assert.IsNull(CaptaTrader.CLIApp.Base.SingleInstance.TryAcquire(directory.Path));
        }

        using System.IO.FileStream? again = CaptaTrader.CLIApp.Base.SingleInstance.TryAcquire(directory.Path);
        Assert.IsNotNull(again);
    }
}
