using CaptaTrader.Business;
using CaptaTrader.Business.Entities;
using CaptaTrader.Presentation.CLI.Base.Extensions;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Presenters;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI
{
    public class Application
    {
        internal static IServiceProvider? DomainServices { get; set; }
        internal static User? SessionUser { get; set; }
        internal static int ExitCode { get; set; } = 0;

        //private readonly string[] _tradeTransactionColumns = TradeOrder.GetColumnNames();
        private static readonly FigletFont _titleFont = SetTitleFont();

        public Application(ILoggerProvider _loggerProvider, bool useMockData)
        {
            ILogger logger = _loggerProvider.CreateLogger(nameof(Application));

            DomainServices = Startup.InitializeDomainServices(logger, useMockData);
        }

        public async Task<int> RunAsync()
        {
            new Navigator().NavigateTo(typeof(LoginPresenter));

            return ExitCode;
        }

        private static FigletFont SetTitleFont()
        {
            // https://github.com/xero/figlet-fonts
            string? font = Assembly.GetExecutingAssembly().ReadResource("Ogre.flf");
            return font != null ? FigletFont.Parse(font) : FigletFont.Default;
        }

        internal static void ClearConsole()
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText(_titleFont, "Capta Trader").Color(Color.Green));
            AnsiConsole.Write(Environment.NewLine);
        }

        //private void WriteTradeOrder(TradeOrder? tradeOrder)
        //{
        //    if (tradeOrder != null)
        //    {
        //        Style columnsHeaderStyle = new(decoration: Decoration.Bold);
        //        Text[] columnHeaders = _tradeTransactionColumns.Select(ttc => new Text(ttc, columnsHeaderStyle)).ToArray();

        //        Grid transactionGrid = new();
        //        transactionGrid.AddColumns(_tradeTransactionColumns.Length);
        //        transactionGrid.AddRow(columnHeaders);
        //        transactionGrid.AddRow(tradeOrder.AsDataRow().ToArray());

        //        string headerColor = tradeOrder.TransactionType == TradeOrder.TransactionTypes.Sell ? "green" : "purple";
        //        Panel transactionPanel = new(transactionGrid)
        //        {
        //            Header = new PanelHeader($"[{headerColor}]{tradeOrder.TransactionType} Order[/]"),
        //            Border = BoxBorder.Ascii,
        //            Expand = false
        //        };

        //        AnsiConsole.Write(transactionPanel);
        //    }
        //}

        //private void TradingManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        //{
        //    if (sender is AppManager manager)
        //    {
        //        if (string.Equals(e.PropertyName, nameof(Business.AppManager.LastNotification)) && manager.LastNotification != null)
        //        {
        //            if (manager.LastNotification.NotificationType == Notification.NotificationTypes.Message && manager.LastNotification.Message != null)
        //            {
        //                AnsiConsole.WriteLine(manager.LastNotification.Message);
        //            }
        //            else if (manager.LastNotification.NotificationType == Notification.NotificationTypes.TradeTransaction)
        //            {
        //                WriteTradeOrder(manager.LastNotification.TradeOrder);
        //            }
        //        }
        //    }
        //}
    }
}
