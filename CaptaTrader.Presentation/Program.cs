using CaptaTrader.Business;
using CaptaTrader.Presentation.Base.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation
{
    public class Program
    {
        internal static int ReturnCode = 0; // Return Codes: https://www.tutorialspoint.com/batch_script/batch_script_return_code.htm

        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine($"Capta Trader v{ConsoleExtensions.GetAppVersion()}");
            Console.Write(Environment.NewLine);

            await RunAsync();

            return Program.ReturnCode;
        }

        private static async Task RunAsync()
        {
            string passPhrase = ConsoleExtensions.RequestPassword("Enter your pass phrase for API encryption: ");
            Console.Write(Environment.NewLine);

            //TODO: Verify pass phrase is correct.

            Manager tradingManager = new(CreateAppLogger());

            Interactions interactions = new(tradingManager.Save, tradingManager.LaunchBot);

            await interactions.PresentMainMenuAsync(tradingManager.AppData, passPhrase);
        }

        private static Microsoft.Extensions.Logging.ILogger CreateAppLogger()
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
