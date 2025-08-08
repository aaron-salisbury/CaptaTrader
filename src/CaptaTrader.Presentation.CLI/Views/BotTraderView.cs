using CaptaTrader.Business.Modules.Trading.DTOs;
using RunnethOverStudio.AppToolkit.Presentation.MVP;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaptaTrader.Presentation.CLI.Views;

internal sealed class BotTraderView : View
{
    internal event EventHandler? ShutdownRequested;

    private static readonly Text[] _tradeOrderColumnHeaders = BuildTradeOrderColumnHeaders();

    public override void Show()
    {
        Clear();

        AnsiConsole.MarkupLine("Bot is [lime]active[/], press any key to shutdown..." + Environment.NewLine);
        Console.ReadKey();

        ShutdownRequested?.Invoke(this, EventArgs.Empty);
    }

    internal void PopupTradeOrder(TradeOrder trade)
    {
        Grid transactionGrid = new();
        transactionGrid.AddColumns(_tradeOrderColumnHeaders.Length);
        transactionGrid.AddRow(_tradeOrderColumnHeaders);
        transactionGrid.AddRow(GetTradeOrderAsDataRow(trade));

        string headerColor = trade.TransactionType == TradeOrder.TransactionTypes.Sell ? "green" : "darkorange";

        Panel transactionPanel = new(transactionGrid)
        {
            Header = new PanelHeader($"[{headerColor}]{trade.TransactionType} Order[/]"),
            Border = BoxBorder.Ascii,
            Expand = false
        };

        AnsiConsole.Write(transactionPanel);
    }

    private static string[] GetTradeOrderAsDataRow(TradeOrder tradeOrder)
    {
        string cryptoSymbol = tradeOrder.TransactionType == TradeOrder.TransactionTypes.Buy
            ? tradeOrder.AssetSymbolBuying
            : tradeOrder.AssetSymbolSpending;

        return
            [
                tradeOrder.TimeStamp.ToString(),
                tradeOrder.TransactionType.ToString(),
                cryptoSymbol,
                tradeOrder.Price.ToString(),
                tradeOrder.Amount.ToString("F2")
            ];
    }

    private static Text[] BuildTradeOrderColumnHeaders()
    {
        IEnumerable<string> columnNames = 
            [
                nameof(TradeOrder.TimeStamp),
                nameof(TradeOrder.TransactionType),
                "Symbol",
                nameof(TradeOrder.Price),
                nameof(TradeOrder.Amount)
            ];

        Style columnsHeaderStyle = new(decoration: Decoration.Bold);

        return [.. columnNames.Select(ttc => new Text(ttc, columnsHeaderStyle))];
    }
}
