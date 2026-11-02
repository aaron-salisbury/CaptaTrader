using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Capabilities;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.MarketData;
using CaptaTrader.Business.Modules.Providers;
using CaptaTrader.Business.Modules.Settings;
using CaptaTrader.Business.Modules.Strategies;
using CaptaTrader.Business.Modules.TaxReporting;
using CaptaTrader.Business.Modules.UserAccess;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.AutoTrading;

/// <summary>
/// Runs one auto-trading session at a time: streams market data, asks the selected strategy for a
/// decision on each tick, applies pre-trade checks, and records every attempt on the ledger before
/// sending it to the brokerage.
/// </summary>
/// <remarks>
/// Stopping cancels the market data stream but never an order already sent; the in-flight order is
/// allowed to finish so its outcome is recorded. Stopping does not sell open positions.
/// </remarks>
public sealed class AutoTrader : IAsyncDisposable
{
    private const int HISTORY_CAPACITY = 240;
    private const int EVENT_CAPACITY = 1_000;
    private static readonly TimeSpan ORDER_WINDOW = TimeSpan.FromHours(1);
    private static readonly TimeSpan BLOCKED_NOTICE_INTERVAL = TimeSpan.FromMinutes(1);

    /// <summary>Allows for clock differences between this machine and the brokerage.</summary>
    private static readonly TimeSpan RECONCILED_FILL_TIME_TOLERANCE = TimeSpan.FromMinutes(1);

    /// <summary>Allows for rounding in what the brokerage displays; a notional buy never spends more than requested.</summary>
    private const decimal RECONCILED_BUY_COST_TOLERANCE = 0.005m;

    private readonly ProviderCatalog _providerCatalog;
    private readonly ConfigureProviders _configureProviders;
    private readonly StrategyCatalog _strategyCatalog;
    private readonly ManageTradingSettings _tradingSettings;
    private readonly ILedgerStore _ledger;
    private readonly BuildTaxReports _taxReports;
    private readonly UserSession _session;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AutoTrader> _logger;
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private readonly Channel<TradingEvent> _events = Channel.CreateBounded<TradingEvent>(
        new BoundedChannelOptions(EVENT_CAPACITY) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    private CancellationTokenSource? _sessionCancellation;
    private Task? _sessionTask;
    private volatile TradingStatus _status = TradingStatus.Idle;

    public AutoTrader(
        ProviderCatalog providerCatalog,
        ConfigureProviders configureProviders,
        StrategyCatalog strategyCatalog,
        ManageTradingSettings tradingSettings,
        ILedgerStore ledger,
        BuildTaxReports taxReports,
        UserSession session,
        TimeProvider timeProvider,
        ILogger<AutoTrader> logger)
    {
        _providerCatalog = providerCatalog ?? throw new ArgumentNullException(nameof(providerCatalog));
        _configureProviders = configureProviders ?? throw new ArgumentNullException(nameof(configureProviders));
        _strategyCatalog = strategyCatalog ?? throw new ArgumentNullException(nameof(strategyCatalog));
        _tradingSettings = tradingSettings ?? throw new ArgumentNullException(nameof(tradingSettings));
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        _taxReports = taxReports ?? throw new ArgumentNullException(nameof(taxReports));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Session notifications for a single host reader. Oldest events drop if nobody reads.</summary>
    public ChannelReader<TradingEvent> Events => _events.Reader;

    public TradingStatus Status => _status;

    public bool IsRunning => _status.IsRunning;

    public async Task<CapabilityResult<TradingStatus>> StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_session.IsAuthenticated)
        {
            return Failure(CapabilityStatus.Unauthorized, "Log in before starting auto-trading.");
        }

        await _lifecycle.WaitAsync(cancellationToken);

        try
        {
            if (_status.IsRunning)
            {
                return Failure(CapabilityStatus.Busy, "Auto-trading is already running.");
            }

            CapabilityResult<TradingSetup> setupResult = await PrepareAsync(cancellationToken);

            if (!setupResult.IsSuccess)
            {
                return setupResult.PropagateFailure<TradingStatus>();
            }

            TradingSetup setup = setupResult.Value!;
            AccountSnapshot account;

            try
            {
                account = await setup.Brokerage.GetAccountAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "Brokerage account check failed at session start.");
                return CapabilityFailure.ToResult<TradingStatus>(exception, setup.BrokerageDescriptor.DisplayName, _timeProvider.GetUtcNow());
            }

            int abandoned = await _ledger.MarkAbandonedPendingAsUnknownAsync(
                "The app stopped before this order's outcome was recorded; reconcile with the brokerage.",
                cancellationToken);

            if (abandoned > 0)
            {
                Publish(new TradingNotice(_timeProvider.GetUtcNow(), NoticeLevel.Warning,
                    $"{abandoned} earlier order(s) never recorded an outcome and are marked Unknown. Check them against the brokerage."));
            }

            IReadOnlyList<OrderAttempt> unknown = await _ledger.GetUnknownAttemptsAsync(setup.BrokerageDescriptor.Id, cancellationToken);

            if (unknown.Count > 0)
            {
                return Failure(
                    CapabilityStatus.Conflict,
                    $"{unknown.Count} order(s) on {setup.BrokerageDescriptor.DisplayName} have unknown outcomes. Look each one up at the brokerage and reconcile it before trading again.");
            }

            IReadOnlyList<TaxLot> lots = await _ledger.GetOpenLotsAsync(setup.BrokerageDescriptor.Id, cancellationToken);
            setup.Strategy.Reset();

            _status = new TradingStatus(
                true,
                setup.BrokerageDescriptor.DisplayName,
                setup.MarketDataDescriptor.DisplayName,
                setup.Strategy.Descriptor.DisplayName,
                setup.BrokerageDescriptor.IsSimulated,
                null,
                null,
                account,
                lots.Sum(lot => lot.RemainingQuantity),
                lots.Sum(lot => lot.RemainingCostBasis),
                0,
                setup.Settings.MaxOrdersPerHour,
                null,
                0,
                0,
                0m,
                "Waiting for market data.");

            _sessionCancellation = new CancellationTokenSource();
            CancellationToken sessionToken = _sessionCancellation.Token;
            _sessionTask = Task.Run(() => RunSessionAsync(setup, account, sessionToken), CancellationToken.None);

            return CapabilityResult<TradingStatus>.Success(_status, _timeProvider.GetUtcNow());
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    /// <summary>Orders on the selected brokerage whose outcome is unknown, oldest first. They block trading there.</summary>
    public async Task<CapabilityResult<IReadOnlyList<OrderAttempt>>> GetUnknownOrdersAsync(CancellationToken cancellationToken = default)
    {
        if (!_session.IsAuthenticated)
        {
            return CapabilityResult<IReadOnlyList<OrderAttempt>>.Failure(CapabilityStatus.Unauthorized, "Log in before reviewing orders.", _timeProvider.GetUtcNow());
        }

        ProviderDescriptor brokerage = await _configureProviders.GetSelectedAsync(ProviderKind.Brokerage, cancellationToken);
        IReadOnlyList<OrderAttempt> unknown = await _ledger.GetUnknownAttemptsAsync(brokerage.Id, cancellationToken);

        return CapabilityResult<IReadOnlyList<OrderAttempt>>.Success(unknown, _timeProvider.GetUtcNow());
    }

    /// <summary>
    /// Records what the brokerage says happened to an unknown order. A fill is applied to the tax lots in the
    /// same transaction as the attempt, exactly as a live fill would be. Orders must be reconciled oldest
    /// first so that FIFO lot matching sees them in the order they happened.
    /// </summary>
    public async Task<CapabilityResult<OrderAttempt>> ReconcileUnknownOrderAsync(Guid attemptId, OrderReconciliation outcome, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        if (!_session.IsAuthenticated)
        {
            return ReconcileFailure(CapabilityStatus.Unauthorized, "Log in before reconciling orders.");
        }

        await _lifecycle.WaitAsync(cancellationToken);

        try
        {
            if (_status.IsRunning)
            {
                return ReconcileFailure(CapabilityStatus.Busy, "Stop auto-trading before reconciling orders.");
            }

            ProviderDescriptor brokerage = await _configureProviders.GetSelectedAsync(ProviderKind.Brokerage, cancellationToken);
            IReadOnlyList<OrderAttempt> unknown = await _ledger.GetUnknownAttemptsAsync(brokerage.Id, cancellationToken);

            if (unknown.Count == 0 || unknown[0].Id != attemptId)
            {
                return ReconcileFailure(CapabilityStatus.InvalidRequest, "Only the oldest unknown order on the selected brokerage can be reconciled next.");
            }

            OrderAttempt attempt = unknown[0];
            OrderAttempt completed;
            LedgerChanges changes = LedgerChanges.None;

            if (!outcome.Filled)
            {
                completed = attempt with { Status = OrderAttemptStatus.Reconciled, Message = "Confirmed by the user, against the brokerage, not to have filled." };
            }
            else
            {
                if (outcome.FilledQuantity <= 0m || outcome.AveragePrice <= 0m || outcome.Fee < 0m)
                {
                    return ReconcileFailure(CapabilityStatus.InvalidRequest, "A fill needs a positive quantity and price, and a fee of zero or more.");
                }

                // A fill can never exceed what was asked for. Sells were only ever requested for quantity the
                // app's lots held, and trading has been blocked since, so a sell within its request always
                // matches the lots.
                if (attempt.Side == OrderSide.Sell && outcome.FilledQuantity > attempt.RequestedQuantity)
                {
                    return ReconcileFailure(CapabilityStatus.InvalidRequest, "A sell cannot fill more BTC than was requested; check the quantity.");
                }

                if (attempt.Side == OrderSide.Buy
                    && (outcome.FilledQuantity * outcome.AveragePrice) + outcome.Fee > attempt.RequestedNotional * (1m + RECONCILED_BUY_COST_TOLERANCE))
                {
                    return ReconcileFailure(CapabilityStatus.InvalidRequest, "A buy cannot cost more than was requested; check the quantity, price, and fee.");
                }

                if (outcome.FilledAtUtc is not DateTimeOffset filledAtUtc || filledAtUtc < attempt.AttemptedAtUtc - RECONCILED_FILL_TIME_TOLERANCE)
                {
                    return ReconcileFailure(CapabilityStatus.InvalidRequest, "A fill needs the brokerage's fill time, which cannot be before the order was sent.");
                }

                completed = attempt with
                {
                    Status = OrderAttemptStatus.Filled,
                    FilledQuantity = outcome.FilledQuantity,
                    AveragePrice = outcome.AveragePrice,
                    Fee = outcome.Fee,
                    FeeIsEstimated = false,
                    CompletedAtUtc = filledAtUtc.ToUniversalTime(),
                    Message = "Fill entered by the user from the brokerage's records."
                };

                IReadOnlyList<TaxLot> lots = await _ledger.GetOpenLotsAsync(brokerage.Id, cancellationToken);

                try
                {
                    changes = LotAccounting.ApplyFill(completed, lots);
                }
                catch (InvalidOperationException exception)
                {
                    return ReconcileFailure(CapabilityStatus.InvalidRequest, exception.Message);
                }
            }

            try
            {
                await _ledger.CompleteAttemptAsync(completed, changes, CancellationToken.None);
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(exception, "Reconciling attempt {AttemptId} lost a race with another completion.", attempt.Id);
                return ReconcileFailure(CapabilityStatus.Conflict, "This order was already reconciled elsewhere; nothing was changed.");
            }

            // The transactions file lists every fill, so any fill (not just a sale) changes the year's files.
            if (completed.Status == OrderAttemptStatus.Filled)
            {
                await RefreshTaxReportAsync(completed.CompletedAtUtc ?? attempt.AttemptedAtUtc, brokerage);
            }

            return CapabilityResult<OrderAttempt>.Success(completed, _timeProvider.GetUtcNow());
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    public async Task StopAsync()
    {
        await _lifecycle.WaitAsync();

        try
        {
            if (_sessionCancellation is null || _sessionTask is null)
            {
                return;
            }

            await _sessionCancellation.CancelAsync();

            try
            {
                await _sessionTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when the session observes cancellation.
            }

            _sessionCancellation.Dispose();
            _sessionCancellation = null;
            _sessionTask = null;
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _lifecycle.Dispose();
    }

    private async Task<CapabilityResult<TradingSetup>> PrepareAsync(CancellationToken cancellationToken)
    {
        TradingSettings settings = await _tradingSettings.GetAsync(cancellationToken);
        ITradingStrategy? strategy = _strategyCatalog.Find(settings.StrategyId);

        if (strategy is null)
        {
            return CapabilityResult<TradingSetup>.Failure(CapabilityStatus.NotConfigured, "The selected strategy is not available; choose one under Settings.", _timeProvider.GetUtcNow());
        }

        ProviderDescriptor brokerageDescriptor = await _configureProviders.GetSelectedAsync(ProviderKind.Brokerage, cancellationToken);
        ProviderDescriptor marketDataDescriptor = await _configureProviders.GetSelectedAsync(ProviderKind.MarketData, cancellationToken);
        string? combinationError = ValidateCombination(brokerageDescriptor, marketDataDescriptor, _session.IsDemo);

        if (combinationError is not null)
        {
            return CapabilityResult<TradingSetup>.Failure(CapabilityStatus.InvalidRequest, combinationError, _timeProvider.GetUtcNow());
        }

        CapabilityResult<ProviderConnection> brokerageConnection = await _configureProviders.ResolveConnectionAsync(brokerageDescriptor, cancellationToken);

        if (!brokerageConnection.IsSuccess)
        {
            return brokerageConnection.PropagateFailure<TradingSetup>();
        }

        CapabilityResult<ProviderConnection> marketDataConnection = await _configureProviders.ResolveConnectionAsync(marketDataDescriptor, cancellationToken);

        if (!marketDataConnection.IsSuccess)
        {
            return marketDataConnection.PropagateFailure<TradingSetup>();
        }

        IBrokerageProvider brokerageProvider = _providerCatalog.FindBrokerage(brokerageDescriptor.Id)!;
        IMarketDataProvider marketDataProvider = _providerCatalog.FindMarketData(marketDataDescriptor.Id)!;

        TradingSetup setup = new(
            brokerageDescriptor,
            brokerageProvider.Rules,
            brokerageProvider.Connect(brokerageConnection.Value!),
            marketDataDescriptor,
            marketDataProvider.Connect(marketDataConnection.Value!),
            strategy,
            settings);

        return CapabilityResult<TradingSetup>.Success(setup, _timeProvider.GetUtcNow());
    }

    /// <summary>
    /// Historical or simulated prices must never drive a real account, and demos never touch real accounts.
    /// </summary>
    internal static string? ValidateCombination(ProviderDescriptor brokerage, ProviderDescriptor marketData, bool isDemo)
    {
        if (marketData.IsSimulated && !brokerage.IsSimulated)
        {
            return $"{marketData.DisplayName} is simulated and can only drive a simulated brokerage.";
        }

        if (isDemo && !brokerage.IsSimulated)
        {
            return "Demo mode only trades with a simulated brokerage.";
        }

        return null;
    }

    private async Task RunSessionAsync(TradingSetup setup, AccountSnapshot account, CancellationToken cancellationToken)
    {
        Queue<MarketTick> history = new(HISTORY_CAPACITY);
        Queue<DateTimeOffset> orderTimes = new();
        bool orderTimesSeeded = false;
        string? lastBlockedReason = null;
        DateTimeOffset lastBlockedAt = DateTimeOffset.MinValue;
        string stopReason = "Market data ended.";

        Publish(new SessionStarted(
            _timeProvider.GetUtcNow(),
            setup.BrokerageDescriptor.DisplayName,
            setup.MarketDataDescriptor.DisplayName,
            setup.Strategy.Descriptor.DisplayName,
            setup.BrokerageDescriptor.IsSimulated));

        try
        {
            await foreach (MarketTick tick in setup.Feed.StreamAsync(cancellationToken))
            {
                if (history.Count == HISTORY_CAPACITY)
                {
                    history.Dequeue();
                }

                history.Enqueue(tick);

                if (!orderTimesSeeded)
                {
                    // The hourly limit spans sessions: count attempts already on the ledger in the window
                    // ending now. Attempts are stamped with market time, so the window uses it too.
                    IReadOnlyList<DateTimeOffset> recent = await _ledger.GetAttemptTimesSinceAsync(
                        setup.BrokerageDescriptor.Id, tick.Time - ORDER_WINDOW, cancellationToken);

                    foreach (DateTimeOffset attemptedAt in recent.Where(attemptedAt => attemptedAt <= tick.Time))
                    {
                        orderTimes.Enqueue(attemptedAt);
                    }

                    orderTimesSeeded = true;
                }

                while (orderTimes.Count > 0 && orderTimes.Peek() <= tick.Time - ORDER_WINDOW)
                {
                    orderTimes.Dequeue();
                }

                IReadOnlyList<TaxLot> lots = await _ledger.GetOpenLotsAsync(setup.BrokerageDescriptor.Id, cancellationToken);
                StrategyContext context = new(
                    tick,
                    [.. history],
                    account,
                    lots,
                    setup.Rules,
                    setup.Settings,
                    setup.Settings.MaxOrdersPerHour - orderTimes.Count);

                TradeDecision decision = Decide(setup.Strategy, context);
                UpdateStatus(status => status with
                {
                    LastTick = tick,
                    SessionOpenPrice = status.SessionOpenPrice ?? tick.Price,
                    Account = account,
                    HeldQuantity = context.HeldQuantity,
                    HeldCostBasis = lots.Sum(lot => lot.RemainingCostBasis),
                    OrdersInLastHour = orderTimes.Count,
                    LastDecision = decision.Reason
                });

                if (decision.Action == TradeAction.Hold)
                {
                    continue;
                }

                string? blockedReason = PreTradeChecks.FindBlockingReason(decision, context);

                if (blockedReason is not null)
                {
                    if (blockedReason != lastBlockedReason || tick.Time - lastBlockedAt >= BLOCKED_NOTICE_INTERVAL)
                    {
                        Publish(new OrderBlocked(tick.Time, decision, blockedReason));
                        lastBlockedReason = blockedReason;
                        lastBlockedAt = tick.Time;
                    }

                    continue;
                }

                orderTimes.Enqueue(tick.Time);
                (account, string? haltReason) = await ExecuteAsync(setup, decision, context, account);
                UpdateStatus(status => status with { OrdersInLastHour = orderTimes.Count });

                if (haltReason is not null)
                {
                    stopReason = haltReason;
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopReason = "Stopped by user.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Trading session stopped unexpectedly.");
            (CapabilityStatus _, string message) = CapabilityFailure.Classify(exception, "Trading");
            stopReason = "Stopped after an unexpected error: " + message;
        }
        finally
        {
            UpdateStatus(status => status with { IsRunning = false, InFlight = null });
            Publish(new SessionStopped(_timeProvider.GetUtcNow(), stopReason));
        }
    }

    private TradeDecision Decide(ITradingStrategy strategy, StrategyContext context)
    {
        try
        {
            return strategy.Evaluate(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Strategy {StrategyId} failed to evaluate.", strategy.Descriptor.Id);
            Publish(new TradingNotice(context.Tick.Time, NoticeLevel.Error, "The strategy failed on this tick; holding."));
            return TradeDecision.Hold("Strategy error.");
        }
    }

    /// <returns>
    /// The account after the attempt, and a reason to halt the session when the books can no longer be
    /// trusted (an unknown outcome, or a fill that could not be matched to lots).
    /// </returns>
    private async Task<(AccountSnapshot Account, string? HaltReason)> ExecuteAsync(TradingSetup setup, TradeDecision decision, StrategyContext context, AccountSnapshot account)
    {
        bool isBuy = decision.Action == TradeAction.Buy;
        OrderAttempt attempt = new(
            Guid.NewGuid(),
            setup.BrokerageDescriptor.Id,
            setup.Strategy.Descriptor.Id,
            isBuy ? OrderSide.Buy : OrderSide.Sell,
            isBuy ? null : decision.Quantity,
            isBuy ? decision.Notional : null,
            isBuy ? context.BuyPrice : context.SellPrice,
            decision.Reason,
            context.Tick.Time,
            OrderAttemptStatus.Pending);

        // Recording and submitting deliberately ignore the session token: once an attempt starts it must
        // reach the ledger and finish, or its outcome becomes unknown.
        await _ledger.InsertAttemptAsync(attempt, CancellationToken.None);
        UpdateStatus(status => status with { InFlight = attempt, Attempts = status.Attempts + 1 });
        Publish(new OrderAttempted(context.Tick.Time, attempt));

        OrderAttempt completed = await SubmitAsync(setup, attempt);
        LedgerChanges changes = LedgerChanges.None;
        string? haltReason = null;

        if (completed.Status == OrderAttemptStatus.Filled)
        {
            try
            {
                changes = LotAccounting.ApplyFill(completed, context.OpenLots);
            }
            catch (InvalidOperationException exception)
            {
                // The fill happened but the books cannot reflect it; treat it like any other unknown outcome.
                _logger.LogError(exception, "Fill for attempt {AttemptId} could not be matched to lots.", completed.Id);
                completed = completed with
                {
                    Status = OrderAttemptStatus.Unknown,
                    Message = "Filled, but the fill could not be matched to tax lots: " + exception.Message + " Reconcile with the brokerage."
                };
            }
        }

        if (completed.Status == OrderAttemptStatus.Unknown)
        {
            haltReason = "Stopped because an order's outcome is unknown. Check it against the brokerage and mark it reconciled before trading again.";
        }

        await _ledger.CompleteAttemptAsync(completed, changes, CancellationToken.None);

        decimal? realizedGain = changes.Disposals.Count > 0 ? changes.Disposals.Sum(disposal => disposal.GainOrLoss) : null;
        UpdateStatus(status => status with
        {
            InFlight = null,
            Fills = status.Fills + (completed.Status == OrderAttemptStatus.Filled ? 1 : 0),
            RealizedGain = status.RealizedGain + (realizedGain ?? 0m)
        });
        Publish(new OrderCompleted(completed.CompletedAtUtc ?? context.Tick.Time, completed, realizedGain));

        if (completed.Status != OrderAttemptStatus.Filled)
        {
            return (account, haltReason);
        }

        // The transactions file lists every fill, so any fill (not just a sale) changes the year's files.
        await RefreshTaxReportAsync(completed.CompletedAtUtc ?? context.Tick.Time, setup.BrokerageDescriptor);

        try
        {
            return (await setup.Brokerage.GetAccountAsync(CancellationToken.None), null);
        }
        catch (Exception exception)
        {
            // Deciding on balances known to be stale would only produce rejected orders, so stop instead.
            _logger.LogWarning(exception, "Balance refresh after fill failed.");
            return (account, "Stopped because balances could not be refreshed after a fill. Start again once the brokerage is reachable.");
        }
    }

    private async Task<OrderAttempt> SubmitAsync(TradingSetup setup, OrderAttempt attempt)
    {
        OrderRequest request = new(attempt.Id, attempt.Side, attempt.RequestedQuantity, attempt.RequestedNotional, attempt.ReferencePrice, attempt.AttemptedAtUtc);

        try
        {
            OrderResult result = await setup.Brokerage.SubmitMarketOrderAsync(request, CancellationToken.None);

            if (result.Status == OrderResultStatus.Filled && !(result.FilledQuantity > 0m && result.AveragePrice > 0m && result.Fee >= 0m))
            {
                return attempt with
                {
                    Status = OrderAttemptStatus.Unknown,
                    BrokerOrderId = result.BrokerOrderId,
                    CompletedAtUtc = result.FilledAt ?? attempt.AttemptedAtUtc,
                    Message = "The brokerage reported a fill without a valid quantity, price, and fee; reconcile with the brokerage."
                };
            }

            return attempt with
            {
                Status = result.Status switch
                {
                    OrderResultStatus.Filled => OrderAttemptStatus.Filled,
                    OrderResultStatus.Rejected => OrderAttemptStatus.Rejected,
                    _ => OrderAttemptStatus.Unknown
                },
                BrokerOrderId = result.BrokerOrderId,
                FilledQuantity = result.Status == OrderResultStatus.Filled ? result.FilledQuantity : null,
                AveragePrice = result.Status == OrderResultStatus.Filled ? result.AveragePrice : null,
                Fee = result.Status == OrderResultStatus.Filled ? result.Fee : null,
                FeeIsEstimated = result.FeeIsEstimated,
                CompletedAtUtc = result.FilledAt ?? attempt.AttemptedAtUtc,
                Message = result.Message
            };
        }
        catch (CapabilityException exception)
        {
            // Providers raise these for states known not to have placed an order, such as bad credentials.
            return attempt with { Status = OrderAttemptStatus.Failed, CompletedAtUtc = attempt.AttemptedAtUtc, Message = exception.Message };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Order {AttemptId} submission failed.", attempt.Id);
            (CapabilityStatus _, string message) = CapabilityFailure.Classify(exception, setup.BrokerageDescriptor.DisplayName);

            return attempt with
            {
                Status = OrderAttemptStatus.Unknown,
                CompletedAtUtc = attempt.AttemptedAtUtc,
                Message = message + " The order may or may not have been placed; reconcile with the brokerage."
            };
        }
    }

    private async Task RefreshTaxReportAsync(DateTimeOffset filledAt, ProviderDescriptor brokerage)
    {
        if (brokerage.IsSimulated && !_session.IsDemo)
        {
            return;
        }

        CapabilityResult<TaxReportFiles> export = await _taxReports.ExportAsync(_taxReports.TaxYearOf(filledAt), CancellationToken.None);

        if (!export.IsSuccess)
        {
            Publish(new TradingNotice(filledAt, NoticeLevel.Warning, "Tax report was not updated: " + export.Message));
        }
    }

    private void UpdateStatus(Func<TradingStatus, TradingStatus> update)
    {
        _status = update(_status);
    }

    private void Publish(TradingEvent tradingEvent)
    {
        _events.Writer.TryWrite(tradingEvent);
    }

    private CapabilityResult<TradingStatus> Failure(CapabilityStatus status, string message)
    {
        return CapabilityResult<TradingStatus>.Failure(status, message, _timeProvider.GetUtcNow());
    }

    private CapabilityResult<OrderAttempt> ReconcileFailure(CapabilityStatus status, string message)
    {
        return CapabilityResult<OrderAttempt>.Failure(status, message, _timeProvider.GetUtcNow());
    }

    private sealed record TradingSetup(
        ProviderDescriptor BrokerageDescriptor,
        BrokerageRules Rules,
        IBrokerage Brokerage,
        ProviderDescriptor MarketDataDescriptor,
        IMarketDataFeed Feed,
        ITradingStrategy Strategy,
        TradingSettings Settings);
}
