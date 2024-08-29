using CaptaTrader.Business.DTOs;
using CaptaTrader.Presentation.CLI.Base.MVP;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaptaTrader.Presentation.CLI.Views
{
    internal class BotView : View
    {
        internal event EventHandler? ShutdownRequested;

        internal override void Show()
        {
            Application.ClearConsole();

            AnsiConsole.MarkupLine("Bot is [lime]active[/], press any key to shutdown..." + Environment.NewLine);
            Console.ReadKey();

            ShutdownRequested?.Invoke(this, EventArgs.Empty);
        }

        internal void WriteTradeOrder(Trade trade)
        {
            //TODO: Streamline these.
            List<string> tradeTransactionColumns = ["Type", "PriceUSDT", "Amount"];
            string[] tradeAsDataRow = [trade.Type.ToString(), trade.PriceUSDT.ToString(), trade.Amount.ToString()];

            Style columnsHeaderStyle = new(decoration: Decoration.Bold);
            Text[] columnHeaders = tradeTransactionColumns.Select(ttc => new Text(ttc, columnsHeaderStyle)).ToArray();

            Grid transactionGrid = new();
            transactionGrid.AddColumns(tradeTransactionColumns.Count);
            transactionGrid.AddRow(columnHeaders);
            transactionGrid.AddRow(tradeAsDataRow);

            string headerColor = trade.Type == Trade.Types.Sell ? "green" : "purple";
            Panel transactionPanel = new(transactionGrid)
            {
                Header = new PanelHeader($"[{headerColor}]{trade.Type} Order[/]"),
                Border = BoxBorder.Ascii,
                Expand = false
            };

            AnsiConsole.Write(transactionPanel);
        }
    }
}
