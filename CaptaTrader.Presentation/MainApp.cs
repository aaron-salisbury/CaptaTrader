using CaptaTrader.Business;
using CaptaTrader.Business.Models;
using CaptaTrader.Presentation.Base;
using CaptaTrader.Presentation.Base.Extensions;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation
{
    public class MainApp
    {
        internal int ReturnCode = 0;
        internal SecureString EncryptPhrase = new();
        internal Manager TradingManager { get; private set; } = new(Program.CreateAppLogger(), useMockData: true);

        private string _appVersion = ConsoleExtensions.GetAppVersion();
        private Dictionary<string, string>? latestTransaction = null;

        public async Task<int> RunAsync()
        {
            SetupConsole();

            EncryptPhrase = ConsoleExtensions.RequestPassword("Enter your pass phrase for API encryption: ");

            //TODO: Verify pass phrase is correct.

            TradingManager.PropertyChanged += TradingManager_OnPropertyChanged;

            await LaunchMainMenuAsync();

            TradingManager.PropertyChanged -= TradingManager_OnPropertyChanged;
            return ReturnCode;
        }

        internal void SetupConsole()
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Panel($"Capta Trader v{_appVersion}") { Border = BoxBorder.Rounded });
            AnsiConsole.Write(Environment.NewLine);
        }

        private async Task LaunchMainMenuAsync()
        {
            Interactions interactions = new(this);

            await interactions.PresentMainMenuAsync();
            
            if (interactions.SelectedLaunchApp)
            {
                ActivateMainProcess();

                await LaunchMainMenuAsync();
            }
        }

        private void ActivateMainProcess()
        {
            if (TradingManager.InitializeBot(EncryptPhrase))
            {
                AnsiConsole.MarkupLine("Bot is [lime]active[/], press any key to shutdown...");
                ListenForTransactions();

                TradingManager.ShutdownBot();
            }
        }

        private void ListenForTransactions()
        {
            //TODO: Right now after rows extend past the border of the console, things break after you scroll back up.

            Table transactionTable = new()
            { 
                Title = new TableTitle("Transactions"),
                Border = TableBorder.Square,
                ShowRowSeparators = true
            };

            //TODO: This isn't working, plus it should only be called once
            //  and it should live on TradeTransaction and be shared with where Notifications get made using the columns.
            foreach (System.Reflection.PropertyInfo property in typeof(TradeTransaction).GetProperties())
            {
                transactionTable.AddColumn(property.Attributes.GetAttribute<DisplayNameAttribute>()?.DisplayName ?? property.Name);
            }

            AnsiConsole.Live(transactionTable)
                .Start(ldc =>
                {
                    while (!Console.KeyAvailable)
                    {
                        if (latestTransaction != null)
                        {
                            transactionTable.InsertRow(0, latestTransaction.Values.ToArray());
                            ldc.Refresh();
                            latestTransaction = null;
                        }
                    }

                    Console.ReadKey(false);
                });
        }

        private void TradingManager_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Manager.LastNotification) && sender is Manager manager && manager.LastNotification != null)
            {
                if (manager.LastNotification.NotificationType == Notification.NotificationTypes.Message && manager.LastNotification.Message != null)
                {
                    AnsiConsole.WriteLine(manager.LastNotification.Message);
                }
                else if (manager.LastNotification.NotificationType == Notification.NotificationTypes.Transaction && manager.LastNotification.TransactionValuesByColumns != null)
                {
                    latestTransaction = manager.LastNotification.TransactionValuesByColumns;
                }
            }
        }
    }
}
