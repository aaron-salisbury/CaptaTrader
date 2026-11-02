using System;

namespace CaptaTrader.Business.Modules.Capabilities;

/// <summary>
/// Expected, presentation-neutral outcomes of a capability call. Hosts map these states
/// instead of parsing exception text, so any host renders the same outcome consistently.
/// </summary>
public enum CapabilityStatus
{
    Success,

    /// <summary>The user has not supplied required configuration or credentials.</summary>
    NotConfigured,

    /// <summary>The integration cannot run on this platform, such as a missing OS credential store.</summary>
    UnsupportedEnvironment,

    NotFound,

    /// <summary>The caller supplied an invalid argument or the request failed validation.</summary>
    InvalidRequest,

    /// <summary>The request exceeds a configured bound, such as a trading limit.</summary>
    ExceedsLimit,

    /// <summary>Authentication failed or is missing.</summary>
    Unauthorized,

    /// <summary>Authenticated, but the request is outside the permitted scope.</summary>
    Forbidden,

    /// <summary>The source could not be reached or did not respond in time.</summary>
    Unavailable,

    InvalidResponse,

    /// <summary>The same operation is already running; the caller may retry after it completes.</summary>
    Busy,

    /// <summary>The request conflicts with current state, such as creating a login when one exists.</summary>
    Conflict
}

/// <summary>
/// Value or sanitized failure from a capability call. <see cref="Message"/> is safe to show
/// or record: it never contains raw provider exception text, secrets, or connection strings.
/// </summary>
public sealed record CapabilityResult<T>
{
    private CapabilityResult(CapabilityStatus status, T? value, string? message, DateTimeOffset observedAt)
    {
        Status = status;
        Value = value;
        Message = message;
        ObservedAt = observedAt;
    }

    public CapabilityStatus Status { get; }

    public T? Value { get; }

    public string? Message { get; }

    public DateTimeOffset ObservedAt { get; }

    public bool IsSuccess => Status == CapabilityStatus.Success;

    public static CapabilityResult<T> Success(T value, DateTimeOffset observedAt)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new CapabilityResult<T>(CapabilityStatus.Success, value, null, observedAt);
    }

    public static CapabilityResult<T> Failure(CapabilityStatus status, string message, DateTimeOffset observedAt)
    {
        if (status == CapabilityStatus.Success)
        {
            throw new ArgumentException("A failure requires a non-success status.", nameof(status));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new CapabilityResult<T>(status, default, message, observedAt);
    }

    /// <summary>
    /// Re-types this failure so a caller can propagate it without repeating its status or message.
    /// </summary>
    public CapabilityResult<TOther> PropagateFailure<TOther>()
    {
        if (IsSuccess)
        {
            throw new InvalidOperationException("Only a failed result can be propagated as a failure.");
        }

        return CapabilityResult<TOther>.Failure(Status, Message!, ObservedAt);
    }
}

/// <summary>
/// Thrown by providers for an expected state with an already-sanitized message. It derives
/// from <see cref="InvalidOperationException"/> so existing callers keep working while query
/// services convert it to a <see cref="CapabilityResult{T}"/>.
/// </summary>
public sealed class CapabilityException : InvalidOperationException
{
    public CapabilityException(CapabilityStatus status, string message)
        : this(status, message, null)
    {
    }

    /// <param name="innerException">Kept for diagnostics only; its message is never shown.</param>
    public CapabilityException(CapabilityStatus status, string message, Exception? innerException)
        : base(message, innerException)
    {
        if (status == CapabilityStatus.Success)
        {
            throw new ArgumentException("A capability exception requires a non-success status.", nameof(status));
        }

        Status = status;
    }

    public CapabilityStatus Status { get; }
}

/// <summary>
/// Converts a provider exception into a sanitized status and message.
/// </summary>
public static class CapabilityFailure
{
    public static (CapabilityStatus Status, string Message) Classify(Exception exception, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        // Never pass through raw provider messages; only CapabilityException text is authored as safe.
        return exception switch
        {
            CapabilityException capability => (capability.Status, capability.Message),
            PlatformNotSupportedException => (CapabilityStatus.UnsupportedEnvironment, sourceName + " is not available on this platform."),
            UnauthorizedAccessException => (CapabilityStatus.Forbidden, sourceName + " access was denied."),
            System.Security.Authentication.AuthenticationException => (CapabilityStatus.Unauthorized, sourceName + " authentication failed."),
            TimeoutException or OperationCanceledException => (CapabilityStatus.Unavailable, sourceName + " did not respond within the configured time limit."),
            _ => (CapabilityStatus.Unavailable, sourceName + " is unavailable; verify connectivity and configured credentials.")
        };
    }

    public static CapabilityResult<T> ToResult<T>(Exception exception, string sourceName, DateTimeOffset observedAt)
    {
        (CapabilityStatus status, string message) = Classify(exception, sourceName);

        return CapabilityResult<T>.Failure(status, message, observedAt);
    }
}
