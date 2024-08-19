using CaptaTrader.Business;
using CaptaTrader.Presentation.Base.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Security;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation
{
    public class Program
    {
        internal static int ReturnCode = 0; // Return Codes: https://www.tutorialspoint.com/batch_script/batch_script_return_code.htm
        internal static SecureString EncryptPhrase = new();

        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine($"Capta Trader v{ConsoleExtensions.GetAppVersion()}");
            Console.Write(Environment.NewLine);

            EncryptPhrase = ConsoleExtensions.RequestPassword("Enter your pass phrase for API encryption: ");

            //TODO: Verify pass phrase is correct.

            Manager tradingManager = new(CreateAppLogger());

            await LaunchMainMenuAsync(tradingManager);

            return Program.ReturnCode;
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

        private static async Task LaunchMainMenuAsync(Manager tradingManager)
        {
            Console.Write(Environment.NewLine);

            Interactions interactions = new(tradingManager);

            bool beginTrading = await interactions.PresentMainMenuAsync();

            if (beginTrading)
            {
                tradingManager.InitializeBot(EncryptPhrase);

                await RunApplicationLoopAsync(tradingManager);

                await LaunchMainMenuAsync(tradingManager);
            }
        }

        private static async Task RunApplicationLoopAsync(Manager tradingManager)
        {
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        Console.WriteLine("Shutting down bot as 'Escape' key was pressed.");
                        break;
                    }
                }

                await tradingManager.SeekTradeAsync();
            }
        }
    }
}
