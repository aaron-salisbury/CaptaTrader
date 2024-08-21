using CaptaTrader.Business;
using CaptaTrader.Business.Models;
using CaptaTrader.Presentation.Base.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console;
using System;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation
{
    public class Program
    {
        internal static int ReturnCode = 0; // Return Codes: https://www.tutorialspoint.com/batch_script/batch_script_return_code.htm
        internal static SecureString EncryptPhrase = new();

        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            SetupConsole();

            EncryptPhrase = ConsoleExtensions.RequestPassword("Enter your pass phrase for API encryption: ");

            //TODO: Verify pass phrase is correct.

            Manager tradingManager = new(CreateAppLogger());
            tradingManager.PropertyChanged += TradingManager_OnPropertyChanged;

            await LaunchMainMenuAsync(tradingManager);

            tradingManager.PropertyChanged -= TradingManager_OnPropertyChanged;
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
            if (tradingManager.InitializeBot(EncryptPhrase))
            {
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Point)
                    .Start($"Bot is active, press any key to shutdown...{Environment.NewLine}", _ => Console.ReadKey());

                tradingManager.ShutdownBot();
            }
        }

        private static void TradingManager_OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Manager.LastNotification) && sender is Manager manager && manager.LastNotification != null)
            {
                if (manager.LastNotification.NotificationType == Notification.NotificationTypes.Message && manager.LastNotification.Message != null)
                {
                    AnsiConsole.WriteLine(manager.LastNotification.Message);
                }
                else if (manager.LastNotification.NotificationType == Notification.NotificationTypes.Table && manager.LastNotification.CellContentsByColumnNames != null)
                {
                    Table table = new()
                    {
                        Title = new TableTitle(manager.LastNotification.Title ?? e.PropertyName)
                    };
                    table.AddColumns(manager.LastNotification.CellContentsByColumnNames.Keys.ToArray());
                    table.AddRow(manager.LastNotification.CellContentsByColumnNames.Values.ToArray());
                    AnsiConsole.Write(table);
                }
            }
        }
    }
}
