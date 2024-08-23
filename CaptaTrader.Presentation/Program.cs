using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Spectre.Console;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            (ServiceProvider pserviceProvider, Application application) = LoadResources();

            try
            {
                return await application.RunAsync();
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

        private static (ServiceProvider, Application) LoadResources()
        {
            return AnsiConsole.Status()
                .Spinner(Spinner.Known.Point)
                .SpinnerStyle(new Style(foreground: Color.Gold3))
                .Start<(ServiceProvider, Application)>("[gold3]Loading[/]", _ =>
                {
                    ServiceProvider serviceProvider = ConfigureServices();
                    Application application = serviceProvider.GetRequiredService<Application>();

                    return (serviceProvider, application);
                });
        }

        private static ServiceProvider ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();

            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Warning)
                .CreateLogger();

            services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger), new LoggerFactory()
                .AddSerilog(Serilog.Log.Logger)
                .CreateLogger("Logger"));

            services.AddTransient<Application, Application>(serviceProvider => new Application(
                logger: serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger>(),
                useMockData: true));

            return services.BuildServiceProvider();
        }
    }
}
