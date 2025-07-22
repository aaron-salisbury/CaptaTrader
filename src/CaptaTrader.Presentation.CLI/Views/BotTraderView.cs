using CaptaTrader.Business.Modules.Trading.DTOs;
using CaptaTrader.Presentation.CLI.Base.MVP;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaptaTrader.Presentation.CLI.Views;

internal sealed class BotTraderView : View
{
    internal event EventHandler? ShutdownRequested;

    private static readonly Text[] _tradeOrderColumnHeaders = BuildTradeOrderColumnHeaders();

    internal override void Show()
    {
        ClearConsole();

        AnsiConsole.MarkupLine("Bot is [lime]active[/], press any key to shutdown..." + Environment.NewLine);
        Console.ReadKey();

        ShutdownRequested?.Invoke(this, EventArgs.Empty);
    }

    internal void PopupTradeOrder(TradeOrderDto trade)
    {
        Grid transactionGrid = new();
        transactionGrid.AddColumns(_tradeOrderColumnHeaders.Length);
        transactionGrid.AddRow(_tradeOrderColumnHeaders);
        transactionGrid.AddRow(GetTradeOrderAsDataRow(trade));

        string headerColor = trade.TransactionType == TradeOrderDto.TransactionTypes.Sell ? "green" : "purple";

        Panel transactionPanel = new(transactionGrid)
        {
            Header = new PanelHeader($"[{headerColor}]{trade.TransactionType} Order[/]"),
            Border = BoxBorder.Ascii,
            Expand = false
        };

        AnsiConsole.Write(transactionPanel);
    }

    private static string[] GetTradeOrderAsDataRow(TradeOrderDto tradeOrder)
    {
        return
            [
                tradeOrder.TimeStamp.ToString(),
                tradeOrder.TransactionType.ToString(),
                tradeOrder.Symbol,
                tradeOrder.PriceUSDT.ToString(),
                tradeOrder.Amount.ToString()
            ];
    }

    private static Text[] BuildTradeOrderColumnHeaders()
    {
        IEnumerable<string> columnNames = 
            [
                nameof(TradeOrderDto.TimeStamp),
                nameof(TradeOrderDto.TransactionType),
                nameof(TradeOrderDto.Symbol),
                nameof(TradeOrderDto.PriceUSDT),
                nameof(TradeOrderDto.Amount)
            ];

        Style columnsHeaderStyle = new(decoration: Decoration.Bold);

        return columnNames
            .Select(ttc => new Text(ttc, columnsHeaderStyle))
            .ToArray();
    }
}
