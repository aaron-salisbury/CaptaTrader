using CaptaTrader.Business.Models;
using CaptaTrader.Presentation.CLI.Base.MVP;
using Spectre.Console;
using System;
using System.Linq;

namespace CaptaTrader.Presentation.CLI.Views
{
    internal class BotView : View
    {
        internal event EventHandler? ShutdownRequested;

        private static readonly Text[] _tradeOrderColumnHeaders = BuildTradeOrderColumnHeaders();

        internal override void Show()
        {
            Application.ClearConsole();

            AnsiConsole.MarkupLine("Bot is [lime]active[/], press any key to shutdown..." + Environment.NewLine);
            Console.ReadKey();

            ShutdownRequested?.Invoke(this, EventArgs.Empty);
        }

        internal void PopupTradeOrder(TradeOrder trade)
        {
            Grid transactionGrid = new();
            transactionGrid.AddColumns(_tradeOrderColumnHeaders.Length);
            transactionGrid.AddRow(_tradeOrderColumnHeaders);
            transactionGrid.AddRow(trade.AsDataRow());

            string headerColor = trade.TransactionType == TradeOrder.TransactionTypes.Sell ? "green" : "purple";

            Panel transactionPanel = new(transactionGrid)
            {
                Header = new PanelHeader($"[{headerColor}]{trade.TransactionType} Order[/]"),
                Border = BoxBorder.Ascii,
                Expand = false
            };

            AnsiConsole.Write(transactionPanel);
        }

        private static Text[] BuildTradeOrderColumnHeaders()
        {
            Style columnsHeaderStyle = new(decoration: Decoration.Bold);

            return TradeOrder.GetColumnNames()
                .Select(ttc => new Text(ttc, columnsHeaderStyle))
                .ToArray();
        }
    }
}
