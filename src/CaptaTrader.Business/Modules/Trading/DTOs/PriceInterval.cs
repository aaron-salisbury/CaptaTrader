using System;

namespace CaptaTrader.Business.Modules.Trading.DTOs;

public record PriceInterval
{
    public required string Symbol { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required decimal StartPrice { get; set; }
    public required decimal EndPrice { get; set; }

    public decimal Change => StartPrice == 0 ? 0 : (EndPrice - StartPrice) / StartPrice * 100;
    public string Direction => Change > 0 ? "Up" : "Down";
}
