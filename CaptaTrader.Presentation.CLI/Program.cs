using CaptaTrader.Presentation.CLI.Commands;
using CaptaTrader.Presentation.CLI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            CommandApp<MainCommand> app = LoadResources();

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

        private static CommandApp<MainCommand> LoadResources()
        {
            return AnsiConsole.Status()
                .Spinner(Spinner.Known.SimpleDotsScrolling)
                .SpinnerStyle(new Style(foreground: Color.Olive))
                .Start<CommandApp<MainCommand>>("[olive]Loading[/]", _ =>
                {
                    IServiceCollection services = ConfigureServices();
                    var registrar = new TypeRegistrar(services);
                    CommandApp<MainCommand> app = new(registrar);

                    app.Configure(config =>
                    {
                        config.SetInterceptor(new LogInterceptor());
                    });

                    return app;
                });
        }

        private static IServiceCollection ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();

            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LogInterceptor.LogLevel) // https://github.com/spectreconsole/examples/tree/main/examples/Cli/Logging
                .WriteTo.Console()
                .CreateLogger();

            services.AddSingleton(
                typeof(Microsoft.Extensions.Logging.ILogger), 
                new LoggerFactory()
                    .AddSerilog(Serilog.Log.Logger)
                    .CreateLogger("Logger"));

            services.AddLogging(configure => configure.AddSerilog(Serilog.Log.Logger));

            return services;
        }
    }
}
