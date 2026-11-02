using CaptaTrader.Business.Modules.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Brokerage;

public enum OrderSide
{
    Buy,
    Sell
}

/// <summary>
/// Fee rates as fractions of notional, for example 0.0025 for 0.25%.
/// </summary>
public sealed record FeeSchedule(decimal MakerRate, decimal TakerRate);

/// <summary>
/// Account-level constraints the bot must respect for a brokerage.
/// </summary>
/// <param name="Fees">Fee schedule used for pre-trade estimates. Market orders pay the taker rate.</param>
/// <param name="MinimumOrderNotional">Smallest USD value the brokerage accepts for one order.</param>
/// <param name="QuantityDecimals">BTC quantity precision accepted by the brokerage.</param>
/// <param name="ExpectedSlippageRate">How much worse than the quoted price a market order typically fills, as a
/// fraction. At one-cent margins slippage alone can turn an estimated profit into a realized loss.</param>
public sealed record BrokerageRules(FeeSchedule Fees, decimal MinimumOrderNotional, int QuantityDecimals, decimal ExpectedSlippageRate);

/// <summary>
/// Balances as reported by the brokerage, which is the canonical owner of account state.
/// </summary>
public sealed record AccountSnapshot(decimal CashUsd, decimal Bitcoin, DateTimeOffset ObservedAt);

/// <summary>
/// A market order. Buys specify <see cref="Notional"/> (USD to spend, fees included); sells specify
/// <see cref="Quantity"/> (BTC). <see cref="ClientOrderId"/> lets a brokerage de-duplicate retries.
/// </summary>
/// <param name="ReferencePrice">Price the decision was based on, used to measure slippage and by simulators.</param>
public sealed record OrderRequest(
    Guid ClientOrderId,
    OrderSide Side,
    decimal? Quantity,
    decimal? Notional,
    decimal ReferencePrice,
    DateTimeOffset RequestedAt);

public enum OrderResultStatus
{
    Filled,

    /// <summary>The brokerage refused the order; nothing was traded.</summary>
    Rejected,

    /// <summary>The outcome could not be confirmed and must be reconciled with the brokerage.</summary>
    Unknown
}

/// <summary>
/// Outcome of an order. For fills, <see cref="FilledQuantity"/> is the BTC actually received or
/// delivered and <see cref="Fee"/> is in USD, so buy cost basis is <c>quantity * price + fee</c> and
/// sell proceeds are <c>quantity * price - fee</c>.
/// </summary>
public sealed record OrderResult(
    OrderResultStatus Status,
    string? BrokerOrderId,
    decimal FilledQuantity,
    decimal AveragePrice,
    decimal Fee,
    bool FeeIsEstimated,
    DateTimeOffset? FilledAt,
    string? Message)
{
    public static OrderResult Rejected(string message, string? brokerOrderId = null)
    {
        return new OrderResult(OrderResultStatus.Rejected, brokerOrderId, 0m, 0m, 0m, false, null, message);
    }

    public static OrderResult Unknown(string message, string? brokerOrderId = null)
    {
        return new OrderResult(OrderResultStatus.Unknown, brokerOrderId, 0m, 0m, 0m, false, null, message);
    }
}

/// <summary>
/// A Provider of order execution for BTC-USD.
/// </summary>
public interface IBrokerageProvider
{
    ProviderDescriptor Descriptor { get; }

    BrokerageRules Rules { get; }

    IBrokerage Connect(ProviderConnection connection);
}

/// <summary>
/// A connected brokerage account. Implementations throw <see cref="Capabilities.CapabilityException"/>
/// for expected failures such as bad credentials; order refusals are returned as
/// <see cref="OrderResultStatus.Rejected"/> rather than thrown.
/// </summary>
public interface IBrokerage
{
    Task<AccountSnapshot> GetAccountAsync(CancellationToken cancellationToken = default);

    Task<OrderResult> SubmitMarketOrderAsync(OrderRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Balances of the simulated paper account, persisted so paper trading survives restarts.
/// </summary>
public sealed record PaperAccount(decimal CashUsd, decimal Bitcoin);

public interface IPaperAccountStore
{
    /// <summary>Returns the paper account, creating it with <paramref name="startingCash"/> if absent.</summary>
    Task<PaperAccount> GetOrCreateAsync(decimal startingCash, CancellationToken cancellationToken = default);

    Task SaveAsync(PaperAccount account, CancellationToken cancellationToken = default);
}
