using CaptaTrader.Presentation.CLI.Commands;
using CaptaTrader.Presentation.CLI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console.Cli;

namespace CaptaTrader.Presentation
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var serviceCollection = new ServiceCollection()
                .AddLogging(configure => // https://github.com/spectreconsole/examples/tree/main/examples/Cli/Logging
                    configure.AddSerilog(new LoggerConfiguration()
                        .MinimumLevel.ControlledBy(LogInterceptor.LogLevel)
                        .WriteTo.Console()
                        .CreateLogger()));

            var registrar = new TypeRegistrar(serviceCollection);

            var app = new CommandApp<MainCommand>(registrar);

            app.Configure(config =>
            {
                config.SetInterceptor(new LogInterceptor());
            });

            return await app.RunAsync(args);
        }
    }
}
