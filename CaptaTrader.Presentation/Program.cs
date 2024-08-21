using CaptaTrader.Business;
using CaptaTrader.Business.Models;
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

        //TODO: Maybe everything should have a ILoggerFactory instead? I need to learn about that.
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
                foreach (string? updateMessage in tradingManager.InitializeBot(EncryptPhrase))
                {
                    if (updateMessage != null)
                    {
                        Console.WriteLine(updateMessage);
                    }
                }

                if (tradingManager.BitcoinTrader !=  null)
                {
                    tradingManager.BitcoinTrader.PropertyChanged += TradingBot_OnPropertyChanged;
                    //TODO: remove upon application close?
                }

                await RunApplicationLoopAsync(tradingManager);

                await LaunchMainMenuAsync(tradingManager);
            }
        }

        private static void TradingBot_OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //TODO: This isn't getting hit.
            if (e.PropertyName == nameof(BitcoinTrader.LastNotification) && sender is BitcoinTrader bot && bot.LastNotification != null)
            {
                if (bot.LastNotification.NotificationType == Notification.NotificationTypes.Message && bot.LastNotification.Message != null)
                {
                    //TODO: title and message
                }
                else if (bot.LastNotification.NotificationType == Notification.NotificationTypes.Table && bot.LastNotification.CellContentsByColumnNames != null)
                {
                    //TODO: title and table
                }
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

                if(!await tradingManager.SeekTradeAsync())
                {
                    Console.WriteLine("Shutting down bot as process ended in error.");
                    break;
                }
            }
        }
    }
}
