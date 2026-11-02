using CaptaTrader.Business.Modules.Strategies;
using System;

namespace CaptaTrader.Business.Modules.AutoTrading;

/// <summary>
/// Risk checks applied to every strategy proposal before an order is sent, independent of strategy.
/// </summary>
internal static class PreTradeChecks
{
    /// <returns>Why the decision is blocked, or <see langword="null"/> when it may proceed.</returns>
    internal static string? FindBlockingReason(TradeDecision decision, StrategyContext context)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(context);

        if (decision.Action == TradeAction.Hold)
        {
            return null;
        }

        if (context.RemainingOrderBudget <= 0)
        {
            return $"Order limit reached ({context.Settings.MaxOrdersPerHour} per hour).";
        }

        if (decision.Action == TradeAction.Buy)
        {
            if (decision.Notional < context.Rules.MinimumOrderNotional)
            {
                return $"Buy is below the brokerage minimum of {context.Rules.MinimumOrderNotional:0.00} USD.";
            }

            if (decision.Notional > context.Settings.MaxBuyNotional)
            {
                return $"Buy exceeds the configured maximum of {context.Settings.MaxBuyNotional:0.00} USD.";
            }

            if (decision.Notional > context.Account.CashUsd)
            {
                return "Not enough cash for this buy.";
            }

            return null;
        }

        // Only BTC the app bought itself may be sold, so the bot can never dispose of the user's
        // pre-existing holdings (and their long-term basis) by accident.
        if (decision.Quantity > context.HeldQuantity)
        {
            return "Sell exceeds the BTC this app bought and still holds.";
        }

        if (decision.Quantity > context.Account.Bitcoin)
        {
            return "Sell exceeds the BTC balance the brokerage reports.";
        }

        if (decision.Quantity * context.SellPrice < context.Rules.MinimumOrderNotional)
        {
            return $"Sell is below the brokerage minimum of {context.Rules.MinimumOrderNotional:0.00} USD.";
        }

        return null;
    }
}
