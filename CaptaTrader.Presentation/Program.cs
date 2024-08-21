using CaptaTrader.Business;
using CaptaTrader.Business.Models;
using CaptaTrader.Presentation.Base.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console;
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
            SetupConsole();

            EncryptPhrase = ConsoleExtensions.RequestPassword("Enter your pass phrase for API encryption: ");

            //TODO: Verify pass phrase is correct.

            Manager tradingManager = new(CreateAppLogger());

            await LaunchMainMenuAsync(tradingManager);

            return ReturnCode;
        }

        internal static void SetupConsole()
        {
            AnsiConsole.Clear();

            //TODO: If I change this from being static, have ConsoleExtensions.GetAppVersion() be a private field and only call once.
            AnsiConsole.Write(new Panel($"Capta Trader v{ConsoleExtensions.GetAppVersion()}") { Border = BoxBorder.Square });

            AnsiConsole.Write(Environment.NewLine);
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
            Interactions interactions = new(tradingManager);

            if (await interactions.PresentMainMenuAsync())
            {
                ActivateMainProcess(tradingManager);

                await LaunchMainMenuAsync(tradingManager);
            }
        }

        private static void ActivateMainProcess(Manager tradingManager)
        {
            //TODO: Would rather have this method return a pass or fail state
            //  and have its status message write another way.
            foreach (string? updateMessage in tradingManager.InitializeBot(EncryptPhrase))
            {
                if (updateMessage != null)
                {
                    AnsiConsole.WriteLine(updateMessage);
                }
            }

            //TODO: LastNotification should maybe be on the manager itself, Exceptions too
            if (tradingManager.BitcoinTrader != null)
            {
                tradingManager.BitcoinTrader.PropertyChanged += TradingBot_OnPropertyChanged;
                //TODO: remove upon application close?
            }

            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Bot is active, press any key to shutdown...", _ => Console.ReadKey());
        }

        private static void TradingBot_OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //TODO: This isn't getting hit.
            if (e.PropertyName == nameof(BitcoinTrader.LastNotification) && sender is BitcoinTrader bot && bot.LastNotification != null)
            {
                // https://spectreconsole.net/live/live-display

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
    }
}
