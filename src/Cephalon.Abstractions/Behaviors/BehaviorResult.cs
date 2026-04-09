namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Provides factory helpers for creating transport-neutral behavior results.
/// </summary>
public static class BehaviorResult
{
    /// <summary>
    /// Creates a successful result with a payload value.
    /// </summary>
    public static BehaviorResult<T> Ok<T>(T value, string? message = null, string? code = null)
        => BehaviorResult<T>.Create(BehaviorResultStatus.Ok, value, hasValue: true, message, code, fault: null);

    /// <summary>
    /// Creates a created result with a payload value.
    /// </summary>
    public static BehaviorResult<T> Created<T>(T value, string? message = null, string? code = null)
        => BehaviorResult<T>.Create(BehaviorResultStatus.Created, value, hasValue: true, message, code, fault: null);

    /// <summary>
    /// Creates an accepted result with an optional payload value.
    /// </summary>
    public static BehaviorResult<T> Accepted<T>(T value, string? message = null, string? code = null)
        => BehaviorResult<T>.Create(BehaviorResultStatus.Accepted, value, hasValue: true, message, code, fault: null);

    /// <summary>
    /// Creates a no-content result.
    /// </summary>
    public static BehaviorResult<T> NoContent<T>(string? message = null, string? code = null)
        => BehaviorResult<T>.Create(BehaviorResultStatus.NoContent, default, hasValue: false, message, code, fault: null);

    /// <summary>
    /// Creates an invalid-request result.
    /// </summary>
    public static BehaviorResult<T> Invalid<T>(
        string code,
        string message,
        BehaviorFault? fault = null)
        => BehaviorResult<T>.Create(BehaviorResultStatus.Invalid, default, hasValue: false, message, code, fault);

    /// <summary>
    /// Creates an unauthorized result.
    /// </summary>
    public static BehaviorResult<T> Unauthorized<T>(
        string code,
        string message,
        BehaviorFault? fault = null)
        => BehaviorResult<T>.Create(BehaviorResultStatus.Unauthorized, default, hasValue: false, message, code, fault);

    /// <summary>
    /// Creates a forbidden result.
    /// </summary>
    public static BehaviorResult<T> Forbidden<T>(
        string code,
        string message,
        BehaviorFault? fault = null)
        => BehaviorResult<T>.Create(BehaviorResultStatus.Forbidden, default, hasValue: false, message, code, fault);

    /// <summary>
    /// Creates a not-found result.
    /// </summary>
    public static BehaviorResult<T> NotFound<T>(
        string code,
        string message,
        BehaviorFault? fault = null)
        => BehaviorResult<T>.Create(BehaviorResultStatus.NotFound, default, hasValue: false, message, code, fault);

    /// <summary>
    /// Creates a conflict result.
    /// </summary>
    public static BehaviorResult<T> Conflict<T>(
        string code,
        string message,
        BehaviorFault? fault = null)
        => BehaviorResult<T>.Create(BehaviorResultStatus.Conflict, default, hasValue: false, message, code, fault);
}

/// <summary>
/// Represents a transport-neutral behavior outcome with an optional payload value.
/// </summary>
/// <typeparam name="T">The payload type carried by the result.</typeparam>
public sealed class BehaviorResult<T> : IBehaviorResult
{
    private BehaviorResult(
        BehaviorResultStatus status,
        T? value,
        bool hasValue,
        string? message,
        string? code,
        BehaviorFault? fault)
    {
        Status = status;
        Value = value;
        HasValue = hasValue;
        Message = string.IsNullOrWhiteSpace(message)
            ? GetDefaultMessage(status)
            : message.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        Fault = fault;
    }

    /// <summary>
    /// Gets the transport-neutral outcome status.
    /// </summary>
    public BehaviorResultStatus Status { get; }

    /// <summary>
    /// Gets the stable outcome code when one was supplied.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Gets the human-readable outcome message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets a value indicating whether the result represents a successful outcome.
    /// </summary>
    public bool IsSuccess => Status is BehaviorResultStatus.Ok
        or BehaviorResultStatus.Created
        or BehaviorResultStatus.Accepted
        or BehaviorResultStatus.NoContent;

    /// <summary>
    /// Gets a value indicating whether the result carries a payload value.
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// Gets the typed payload value when one was supplied.
    /// </summary>
    public T? Value { get; }

    object? IBehaviorResult.Value => Value;

    /// <summary>
    /// Gets the structured fault details when the outcome is not successful.
    /// </summary>
    public BehaviorFault? Fault { get; }

    /// <summary>
    /// Converts a raw payload value into a successful result.
    /// </summary>
    public static implicit operator BehaviorResult<T>(T value)
        => BehaviorResult.Ok(value);

    internal static BehaviorResult<T> Create(
        BehaviorResultStatus status,
        T? value,
        bool hasValue,
        string? message,
        string? code,
        BehaviorFault? fault)
    {
        return new BehaviorResult<T>(status, value, hasValue, message, code, fault);
    }

    private static string GetDefaultMessage(BehaviorResultStatus status)
    {
        return status switch
        {
            BehaviorResultStatus.Ok => "Ok",
            BehaviorResultStatus.Created => "Created",
            BehaviorResultStatus.Accepted => "Accepted",
            BehaviorResultStatus.NoContent => "No content",
            BehaviorResultStatus.Invalid => "The request is invalid.",
            BehaviorResultStatus.Unauthorized => "The request is unauthorized.",
            BehaviorResultStatus.Forbidden => "The request is forbidden.",
            BehaviorResultStatus.NotFound => "The requested resource was not found.",
            BehaviorResultStatus.Conflict => "The request conflicts with the current state.",
            _ => "Completed"
        };
    }
}
