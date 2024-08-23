using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
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
                .SpinnerStyle(new Style(foreground: Color.DodgerBlue2))
                .Start<(ServiceProvider, Application)>("Loading", _ =>
                {
                    ServiceProvider serviceProvider = ConfigureServices();
                    return (serviceProvider, serviceProvider.GetRequiredService<Application>());
                });
        }

        private static ServiceProvider ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();

            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger), new LoggerFactory()
                .AddSerilog(Serilog.Log.Logger)
                .CreateLogger("Logger"));

            services.AddTransient<Application>();

            return services.BuildServiceProvider();
        }
    }
}
