using CaptaTrader.Presentation.CLI.Base.Commands;
using CaptaTrader.Presentation.CLI.Base.Infrastructure;
using CaptaTrader.Presentation.CLI.Base.MVP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        CommandApp<MainCommand> app = RegisterApp();

        try
        {
            return await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            Serilog.Log.Fatal(ex, "Application terminated unexpectedly.");
            return 1;
        }
        finally
        {
            Serilog.Log.CloseAndFlush();
        }
    }

    private static CommandApp<MainCommand> RegisterApp()
    {
        return AnsiConsole.Status()
            .Spinner(Spinner.Known.SimpleDotsScrolling)
            .SpinnerStyle(new Style(foreground: Color.Olive))
            .Start<CommandApp<MainCommand>>("[olive]Loading[/]", _ =>
            {
                TypeRegistrar registrar = new(ConfigureServices());
                CommandApp<MainCommand> app = new(registrar);

                app.Configure(config =>
                {
                    config.SetInterceptor(new CommandInterceptor());
                });

                return app;
            });
    }

    private static IServiceCollection ConfigureServices()
    {
        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(CommandInterceptor.LogLevel) // ref: https://github.com/spectreconsole/examples/blob/main/examples/Cli/Logging/Program.cs
            .WriteTo.Console()
            .CreateLogger();

        IServiceCollection services = new ServiceCollection()
            .AddLogging(configure => configure.AddSerilog(Serilog.Log.Logger))
            .AddSingleton(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Application)))
            .AddSingleton<Navigator, Navigator>();

        foreach (Type presenterType in GetPresenterTypes())
        {
            services.AddScoped(presenterType);
        }

        services.AddSingleton<Application, Application>();

        return services;
    }

    private static IEnumerable<Type> GetPresenterTypes()
    {
        foreach (Type assemblyType in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (assemblyType.Name.EndsWith(nameof(Presenter)) && !assemblyType.Name.Equals(nameof(Presenter)))
            {
                yield return assemblyType;
            }
        }
    }
}
