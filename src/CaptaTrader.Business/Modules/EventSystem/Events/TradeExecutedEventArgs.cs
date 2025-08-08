using CaptaTrader.Business.Modules.Trading.DTOs;
using System;

namespace CaptaTrader.Business.Modules.EventSystemEvents;

public class TradeExecutedEventArgs : EventArgs
{
    public required TradeOrder TradeOrder { get; init; }
}
