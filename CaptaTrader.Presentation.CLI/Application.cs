using CaptaTrader.Business;
using CaptaTrader.Business.Models;
using CaptaTrader.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.ComponentModel;
using System.Data;

namespace CaptaTrader.Presentation.CLI
{
    public class Application
    {
        internal int ExitCode { get; set; } = 0;

        private AppManager _appManager;
        private readonly string[] _tradeTransactionColumns = TradeTransaction.GetColumnNames();

        public Application(ILogger logger, bool useMockData)
        {
            _appManager = new(logger, useMockData);
            _appManager.PropertyChanged += TradingManager_PropertyChanged;
        }

        public async Task<int> RunAsync()
        {
            ClearConsole();

            LaunchLoginMenu();

            LaunchMainMenuAsync();

            return ExitCode;
        }

        internal static void ClearConsole()
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Capta Trader").Color(Color.Green));
            AnsiConsole.Write(Environment.NewLine);
        }

        private void LaunchLoginMenu()
        {
            bool verified = false;

            while (!verified)
            {
                verified = Menus.PresentLogin(_appManager);

                if (!verified)
                {
                    AnsiConsole.MarkupLine("[red]Bad user.[/] Press any key to return...");
                    Console.ReadKey(false);
                }
            }
        }

        private void LaunchMainMenuAsync()
        {
            bool userSelectedLaunchApp = Menus.PresentMainMenuAsync(_appManager);
            
            if (userSelectedLaunchApp)
            {
                ActivateMainProcess();

                LaunchMainMenuAsync();
            }
        }

        private void ActivateMainProcess()
        {
            if (_appManager.InitializeBot())
            {
                ClearConsole();
                AnsiConsole.MarkupLine("Bot is [lime]active[/], press any key to shutdown..." + Environment.NewLine);

                while (!Console.KeyAvailable) { }
                Console.ReadKey(false);

                _appManager.ShutdownBot();
            }
        }

        private void WriteTradeTransaction(TradeTransaction? tradeTransaction)
        {
            if (tradeTransaction != null)
            {
                Style columnsHeaderStyle = new(decoration: Decoration.Bold);
                Text[] columnHeaders = _tradeTransactionColumns.Select(ttc => new Text(ttc, columnsHeaderStyle)).ToArray();

                Grid transactionGrid = new();
                transactionGrid.AddColumns(_tradeTransactionColumns.Length);
                transactionGrid.AddRow(columnHeaders);
                transactionGrid.AddRow(tradeTransaction.AsDataRow().ToArray());

                Panel transactionPanel = new(transactionGrid)
                {
                    Header = new PanelHeader($"{tradeTransaction.TransactionType} Transaction"),
                    Border = BoxBorder.Ascii,
                    Expand = false
                };

                AnsiConsole.Write(transactionPanel);
            }
        }

        private void TradingManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is AppManager manager)
            {
                if (string.Equals(e.PropertyName, nameof(Business.AppManager.LastNotification)) && manager.LastNotification != null)
                {
                    if (manager.LastNotification.NotificationType == Notification.NotificationTypes.Message && manager.LastNotification.Message != null)
                    {
                        AnsiConsole.WriteLine(manager.LastNotification.Message);
                    }
                    else if (manager.LastNotification.NotificationType == Notification.NotificationTypes.Transaction)
                    {
                        WriteTradeTransaction(manager.LastNotification.TradeTransaction);
                    }
                }
            }
        }
    }
}
