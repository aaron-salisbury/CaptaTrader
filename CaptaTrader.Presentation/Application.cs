using CaptaTrader.Business;
using CaptaTrader.Business.Models;
using CaptaTrader.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation
{
    public class Application
    {
        internal int ExitCode { get; set; } = 0;
        internal Manager TradingManager { get; private set; }

        private readonly Menus _menus;
        private readonly string[] _tradeTransactionColumns = TradeTransaction.GetColumnNames();
        private string[]? _latestTradeTransaction = null;

        public Application(ILogger logger, bool useMockData)
        {
            TradingManager = new(logger, useMockData);
            TradingManager.PropertyChanged += TradingManager_PropertyChanged;

            _menus = new Menus(this);
        }

        public async Task<int> RunAsync()
        {
            LaunchLoginMenu();

            await LaunchMainMenuAsync();

            return ExitCode;
        }

        internal void ClearConsole()
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
                ClearConsole();

                verified = _menus.PresentLogin();

                if (!verified)
                {
                    AnsiConsole.MarkupLine("[red]Bad user.[/] Press any key to return...");
                    Console.ReadKey(false);
                }
            }
        }

        private async Task LaunchMainMenuAsync()
        {
            await _menus.PresentMainMenuAsync();
            
            if (_menus.SelectedLaunchApp)
            {
                ActivateMainProcess();

                await LaunchMainMenuAsync();
            }
        }

        private void ActivateMainProcess()
        {
            if (TradingManager.InitializeBot())
            {
                ClearConsole();
                AnsiConsole.MarkupLine("Bot is [lime]active[/], press any key to shutdown...");
                ListenForTransactions();

                TradingManager.ShutdownBot();
            }
        }

        private void ListenForTransactions()
        {
            Table transactionTable = new()
            { 
                Title = new TableTitle("Transactions"),
                Border = TableBorder.AsciiDoubleHead,
                ShowRowSeparators = true
            };

            foreach (string columnName in _tradeTransactionColumns)
            {
                transactionTable.AddColumn(columnName);
            }

            AnsiConsole.Live(transactionTable)
                .Start(ldc =>
                {
                    while (!Console.KeyAvailable)
                    {
                        if (_latestTradeTransaction != null)
                        {
                            transactionTable.AddRow(_latestTradeTransaction);
                            ldc.Refresh();
                            _latestTradeTransaction = null;
                        }
                    }

                    Console.ReadKey(false);
                });
        }

        private void TradingManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Manager manager)
            {
                if (string.Equals(e.PropertyName, nameof(Manager.LastNotification)) && manager.LastNotification != null)
                {
                    if (manager.LastNotification.NotificationType == Notification.NotificationTypes.Message && manager.LastNotification.Message != null)
                    {
                        AnsiConsole.WriteLine(manager.LastNotification.Message);
                    }
                    else if (manager.LastNotification.NotificationType == Notification.NotificationTypes.Transaction && manager.LastNotification.TradeTransactionRowData != null)
                    {
                        _latestTradeTransaction = manager.LastNotification.TradeTransactionRowData;
                    }
                }
            }
        }
    }
}
