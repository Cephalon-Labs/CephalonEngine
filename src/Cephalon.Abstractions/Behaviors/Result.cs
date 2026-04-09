namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Provides concise factory helpers for creating transport-neutral behavior results.
/// </summary>
/// <remarks>
/// Prefer this type for new behavior authoring code when the longer
/// <see cref="BehaviorResult" /> naming does not add clarity.
/// </remarks>
public static class Result
{
    /// <summary>
    /// Creates a successful result with a payload value.
    /// </summary>
    public static Result<T> Ok<T>(T value, string? message = null, string? code = null)
        => BehaviorResult<T>.Create(BehaviorResultStatus.Ok, value, hasValue: true, message, code, fault: null);

    /// <summary>
    /// Creates a created result with a payload value.
    /// </summary>
    public static Result<T> Created<T>(T value, string? message = null, string? code = null)
        => BehaviorResult<T>.Create(BehaviorResultStatus.Created, value, hasValue: true, message, code, fault: null);

    /// <summary>
    /// Creates an accepted result with a payload value.
    /// </summary>
    public static Result<T> Accepted<T>(T value, string? message = null, string? code = null)
        => BehaviorResult<T>.Create(BehaviorResultStatus.Accepted, value, hasValue: true, message, code, fault: null);

    /// <summary>
    /// Creates a no-content result.
    /// </summary>
    public static BehaviorResultDescriptor NoContent(string? message = null, string? code = null)
        => new(BehaviorResultStatus.NoContent, message, code, fault: null);

    /// <summary>
    /// Creates a no-content result for the specified payload type.
    /// </summary>
    public static Result<T> NoContent<T>(string? message = null, string? code = null)
        => NoContent(message, code);

    /// <summary>
    /// Creates an invalid-request result.
    /// </summary>
    public static BehaviorResultDescriptor Invalid(
        string code,
        string message,
        BehaviorFault? fault = null)
        => new(BehaviorResultStatus.Invalid, message, code, fault);

    /// <summary>
    /// Creates an invalid-request result for the specified payload type.
    /// </summary>
    public static Result<T> Invalid<T>(
        string code,
        string message,
        BehaviorFault? fault = null)
        => Invalid(code, message, fault);

    /// <summary>
    /// Creates an unauthorized result.
    /// </summary>
    public static BehaviorResultDescriptor Unauthorized(
        string code,
        string message,
        BehaviorFault? fault = null)
        => new(BehaviorResultStatus.Unauthorized, message, code, fault);

    /// <summary>
    /// Creates an unauthorized result for the specified payload type.
    /// </summary>
    public static Result<T> Unauthorized<T>(
        string code,
        string message,
        BehaviorFault? fault = null)
        => Unauthorized(code, message, fault);

    /// <summary>
    /// Creates a forbidden result.
    /// </summary>
    public static BehaviorResultDescriptor Forbidden(
        string code,
        string message,
        BehaviorFault? fault = null)
        => new(BehaviorResultStatus.Forbidden, message, code, fault);

    /// <summary>
    /// Creates a forbidden result for the specified payload type.
    /// </summary>
    public static Result<T> Forbidden<T>(
        string code,
        string message,
        BehaviorFault? fault = null)
        => Forbidden(code, message, fault);

    /// <summary>
    /// Creates a not-found result.
    /// </summary>
    public static BehaviorResultDescriptor NotFound(
        string code,
        string message,
        BehaviorFault? fault = null)
        => new(BehaviorResultStatus.NotFound, message, code, fault);

    /// <summary>
    /// Creates a not-found result for the specified payload type.
    /// </summary>
    public static Result<T> NotFound<T>(
        string code,
        string message,
        BehaviorFault? fault = null)
        => NotFound(code, message, fault);

    /// <summary>
    /// Creates a conflict result.
    /// </summary>
    public static BehaviorResultDescriptor Conflict(
        string code,
        string message,
        BehaviorFault? fault = null)
        => new(BehaviorResultStatus.Conflict, message, code, fault);

    /// <summary>
    /// Creates a conflict result for the specified payload type.
    /// </summary>
    public static Result<T> Conflict<T>(
        string code,
        string message,
        BehaviorFault? fault = null)
        => Conflict(code, message, fault);
}

/// <summary>
/// Represents a concise transport-neutral behavior outcome with an optional payload value.
/// </summary>
/// <typeparam name="T">The payload type carried by the result.</typeparam>
public class Result<T> : IBehaviorResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}" /> class.
    /// </summary>
    /// <param name="status">The transport-neutral outcome status.</param>
    /// <param name="value">The optional payload value.</param>
    /// <param name="hasValue">Whether the result carries a payload value.</param>
    /// <param name="message">The human-readable outcome message.</param>
    /// <param name="code">The stable outcome code.</param>
    /// <param name="fault">The structured fault details.</param>
    protected Result(
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
    public static implicit operator Result<T>(T value)
        => Result.Ok(value);

    /// <summary>
    /// Converts a no-payload descriptor into a typed result.
    /// </summary>
    public static implicit operator Result<T>(BehaviorResultDescriptor descriptor)
    {
        return BehaviorResult<T>.Create(
            descriptor.Status,
            default,
            hasValue: false,
            descriptor.Message,
            descriptor.Code,
            descriptor.Fault);
    }

    internal static Result<T> Create(
        BehaviorResultStatus status,
        T? value,
        bool hasValue,
        string? message,
        string? code,
        BehaviorFault? fault)
    {
        return new Result<T>(status, value, hasValue, message, code, fault);
    }

    /// <summary>
    /// Resolves the built-in default message for a behavior-result status.
    /// </summary>
    /// <param name="status">The status whose default message should be returned.</param>
    /// <returns>The default message used when callers do not supply one explicitly.</returns>
    protected static string GetDefaultMessage(BehaviorResultStatus status)
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
