using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.Settings;
using System;
using System.Collections.Generic;

namespace CaptaTrader.Business.Modules.Strategies;

/// <summary>
/// Estimated outcome of selling a quantity now.
/// </summary>
/// <param name="NetProceeds">Gross proceeds minus the sell fee.</param>
/// <param name="RealizedGain">Net proceeds minus the FIFO cost basis (which already includes buy fees).</param>
/// <param name="EstimatedTax">Tax on positive gains at the configured marginal rates. Losses are not credited.</param>
/// <param name="NetProfitAfterTax">What the sale would leave in hand after fees and estimated tax.</param>
public sealed record SellEstimate(
    decimal GrossProceeds,
    decimal Fee,
    decimal NetProceeds,
    decimal CostBasis,
    decimal RealizedGain,
    decimal EstimatedTax,
    decimal NetProfitAfterTax,
    decimal UnmatchedQuantity);

/// <summary>
/// Fee and tax arithmetic shared by strategies and pre-trade checks.
/// </summary>
public static class TradeEconomics
{
    public static decimal EstimateFee(decimal grossValue, FeeSchedule fees)
    {
        ArgumentNullException.ThrowIfNull(fees);

        return grossValue * fees.TakerRate;
    }

    /// <summary>
    /// Estimates selling <paramref name="quantity"/> at the quoted <paramref name="price"/>, less expected
    /// slippage, against FIFO lots.
    /// </summary>
    /// <remarks>
    /// Because tax is a fraction of a positive gain, it never turns a profitable sale unprofitable; it only
    /// shrinks the profit. Fees are what decide whether a sale clears the minimum. The estimate is
    /// conservative: a loss is not credited with the tax it may save against other gains.
    /// </remarks>
    public static SellEstimate EstimateSell(IReadOnlyList<TaxLot> openLots, decimal quantity, decimal price, BrokerageRules rules, TaxRates taxRates, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(openLots);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(taxRates);

        LotMatch match = LotAccounting.MatchFifo(openLots, quantity);
        decimal gross = quantity * price * (1m - rules.ExpectedSlippageRate);
        decimal fee = EstimateFee(gross, rules.Fees);
        decimal net = gross - fee;
        decimal shortTermGain = 0m;
        decimal longTermGain = 0m;

        foreach (LotAllocation allocation in match.Allocations)
        {
            decimal gain = (net * allocation.Quantity / quantity) - allocation.CostBasis;

            if (LotAccounting.IsLongTerm(DateOnly.FromDateTime(allocation.Lot.AcquiredAtUtc.UtcDateTime), DateOnly.FromDateTime(at.UtcDateTime)))
            {
                longTermGain += gain;
            }
            else
            {
                shortTermGain += gain;
            }
        }

        decimal basis = match.CostBasis;
        decimal realizedGain = net - basis;
        decimal tax = (Math.Max(0m, shortTermGain) * taxRates.ShortTerm) + (Math.Max(0m, longTermGain) * taxRates.LongTerm);

        return new SellEstimate(gross, fee, net, basis, realizedGain, tax, realizedGain - tax, match.UnmatchedQuantity);
    }

    /// <summary>Rounds a BTC quantity down to the brokerage's precision so the app never sells more than it holds.</summary>
    public static decimal FloorQuantity(decimal quantity, int decimals)
    {
        return Math.Round(quantity, decimals, MidpointRounding.ToZero);
    }
}
