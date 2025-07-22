using System;

namespace CaptaTrader.Business;

public record TradingBotConfig
{
    public bool UseMockTransactions { get; set; } = true;
}
