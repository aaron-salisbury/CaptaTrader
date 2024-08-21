using Microsoft.Extensions.Logging;
using Serilog;
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

            return await new MainApp().RunAsync();
        }

        //TODO: Maybe everything should have a ILoggerFactory instead? I need to learn about that.
        internal static Microsoft.Extensions.Logging.ILogger CreateAppLogger()
        {
            Serilog.ILogger serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
            .CreateLogger();

            return new LoggerFactory()
                .AddSerilog(serilogLogger)
                .CreateLogger("Logger");
        }
    }
}
