using CaptaTrader.Business;
using CaptaTrader.CLIApp.Screens;
using CaptaTrader.Data;
using CaptaTrader.Integrations;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Reflection;

namespace CaptaTrader.CLIApp;

internal static class DependencyInjection
{
    private const string APP_FOLDER_NAME = "CaptaTrader";
    private const string DEMO_FOLDER_NAME = "demo";

    internal static IServiceCollection BuildServiceCollection(string applicationDataDirectory)
    {
        Serilog.Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(applicationDataDirectory, "logs", "captatrader-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31)
            .CreateLogger();

        IServiceCollection services = new ServiceCollection();

        // The console is the user interface, so diagnostics go to the log file only.
        services.AddLogging(configure => configure.AddSerilog(Serilog.Log.Logger, dispose: true));

        // The composition root owns HTTP infrastructure; integrations only create clients from the factory.
        services.AddHttpClient();
        services.ConfigureHttpClientDefaults(builder => builder.ConfigureHttpClient(httpClient =>
        {
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"CaptaTrader/{Version}");
        }));

        services.RegisterInternalDataServices(applicationDataDirectory)
            .RegisterInternalIntegrationsServices(applicationDataDirectory)
            .RegisterInternalBusinessServices();

        services.AddTransient<LoginScreen>();
        services.AddTransient<MainMenuScreen>();
        services.AddTransient<TradingMonitorScreen>();
        services.AddTransient<LedgerScreen>();
        services.AddTransient<TaxReportScreen>();
        services.AddTransient<SettingsScreen>();

        return services;
    }

    /// <summary>
    /// Per-user local app data, for example %LOCALAPPDATA%\CaptaTrader on Windows. Demo mode uses a
    /// subfolder that is recreated on every run so it can never mix with real records.
    /// </summary>
    /// <summary>The per-user app folder, created if needed. Holds the real data and the demo subfolder.</summary>
    internal static string GetApplicationRootDirectory()
    {
        string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APP_FOLDER_NAME);
        Directory.CreateDirectory(root);
        return root;
    }

    internal static string GetApplicationDataDirectory(bool isDemo)
    {
        string root = GetApplicationRootDirectory();

        if (!isDemo)
        {
            return root;
        }

        string demo = Path.Combine(root, DEMO_FOLDER_NAME);

        if (Directory.Exists(demo))
        {
            Directory.Delete(demo, recursive: true);
        }

        Directory.CreateDirectory(demo);
        return demo;
    }

    internal static string Version
    {
        get
        {
            string? informational = typeof(DependencyInjection).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            // Strip SourceLink's "+commit" suffix for display.
            return informational?.Split('+')[0] ?? "0.0.0";
        }
    }
}
