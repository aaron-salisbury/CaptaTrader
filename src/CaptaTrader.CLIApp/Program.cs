using CaptaTrader.Business.Modules.AutoTrading;
using CaptaTrader.Business.Modules.Capabilities;
using CaptaTrader.Business.Modules.Providers;
using CaptaTrader.Business.Modules.UserAccess;
using CaptaTrader.CLIApp.Base;
using CaptaTrader.CLIApp.Screens;
using CaptaTrader.Data.Database;
using Microsoft.Extensions.DependencyInjection;
using PrettyConsole;
using Serilog;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CaptaTrader.CLIApp;

internal static class Program
{
    private const int EXIT_OK = 0;
    private const int EXIT_USAGE = 2;
    private const int EXIT_NOT_SIGNED_IN = 3;
    private const int EXIT_ALREADY_RUNNING = 4;
    private const int EXIT_INTERRUPTED = 130;

    public static async Task<int> Main(string[] args)
    {
        AppOptions options = AppOptions.Parse(args);

        if (options.ShowVersion)
        {
            Console.WriteLineInterpolated($"CaptaTrader {DependencyInjection.Version}");
            return EXIT_OK;
        }

        if (options.ShowHelp)
        {
            if (options.Error is not null)
            {
                Console.WriteLineInterpolated(OutputPipe.Error, $"{Color.Red}{options.Error}");
            }

            Console.WriteLineInterpolated($"{AppOptions.HELP_TEXT}");
            return options.Error is null ? EXIT_OK : EXIT_USAGE;
        }

        // Braille spinners and box drawing need UTF-8 on Windows consoles.
        System.Console.OutputEncoding = Encoding.UTF8;

        using FileStream? instanceLock = SingleInstance.TryAcquire(DependencyInjection.GetApplicationRootDirectory());

        if (instanceLock is null)
        {
            Console.WriteLineInterpolated(OutputPipe.Error, $"{Color.Red}CaptaTrader is already running for this user. Close it first.");
            return EXIT_ALREADY_RUNNING;
        }

        string applicationDataDirectory = DependencyInjection.GetApplicationDataDirectory(options.IsDemo);
        await using ServiceProvider services = DependencyInjection.BuildServiceCollection(applicationDataDirectory)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        AutoTrader autoTrader = services.GetRequiredService<AutoTrader>();
        RegisterInterruptHandler(autoTrader);

        try
        {
            string? legacyBackupPath = await services.InitializeApplicationDataAsync();

            if (legacyBackupPath is not null)
            {
                Ui.Warning("Found a database from an earlier version of CaptaTrader that this version cannot use.");
                Ui.Hint($"It was moved, unchanged, to {legacyBackupPath}. A new database was created in its place.");
                Ui.Pause();
            }

            UserSession session = services.GetRequiredService<UserSession>();

            if (options.IsDemo)
            {
                CapabilityResult<bool> demoSetup = await services.GetRequiredService<ConfigureProviders>().SelectSimulatedProvidersAsync();

                if (!demoSetup.IsSuccess)
                {
                    Ui.Failure(demoSetup);
                    return EXIT_USAGE;
                }

                session.SignInDemo();
            }
            else if (!await services.GetRequiredService<LoginScreen>().RunAsync())
            {
                return EXIT_NOT_SIGNED_IN;
            }

            await using WindowTitle windowTitle = new(autoTrader, options.IsDemo);
            windowTitle.Start();

            await services.GetRequiredService<MainMenuScreen>().RunAsync();
            await autoTrader.StopAsync();

            return EXIT_OK;
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Unhandled exception.");
            Ui.Error("CaptaTrader hit an unexpected error and must close. Details are in the log folder:");
            Ui.Hint("  " + System.IO.Path.Combine(applicationDataDirectory, "logs"));
            await autoTrader.StopAsync();
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Ctrl+C stops trading gracefully, letting any in-flight order finish and reach the ledger, then exits.
    /// </summary>
    private static void RegisterInterruptHandler(AutoTrader autoTrader)
    {
        System.Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;

            _ = Task.Run(async () =>
            {
                Console.NewLine();
                Ui.Warning("Interrupted. Stopping after any in-flight order completes…");
                await autoTrader.StopAsync();
                await Log.CloseAndFlushAsync();
                Environment.Exit(EXIT_INTERRUPTED);
            });
        };
    }
}
