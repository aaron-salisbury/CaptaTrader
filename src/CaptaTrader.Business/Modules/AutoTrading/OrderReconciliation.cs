using System;

namespace CaptaTrader.Business.Modules.AutoTrading;

/// <summary>
/// What the brokerage's own records say happened to an order whose outcome the app could not confirm.
/// For fills, <see cref="FilledQuantity"/> is the BTC actually received or delivered (after any fee taken
/// in BTC), <see cref="Fee"/> is in USD, matching how live fills are recorded, and
/// <see cref="FilledAtUtc"/> is the brokerage's fill time, which decides the tax year and lot order.
/// </summary>
public sealed record OrderReconciliation(bool Filled, decimal FilledQuantity, decimal AveragePrice, decimal Fee, DateTimeOffset? FilledAtUtc)
{
    public static OrderReconciliation NotFilled { get; } = new(false, 0m, 0m, 0m, null);

    public static OrderReconciliation Fill(decimal filledQuantity, decimal averagePrice, decimal fee, DateTimeOffset filledAtUtc)
    {
        return new OrderReconciliation(true, filledQuantity, averagePrice, fee, filledAtUtc);
    }
}
