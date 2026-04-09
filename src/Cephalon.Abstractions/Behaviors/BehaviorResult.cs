namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Provides legacy factory helpers for creating transport-neutral behavior results.
/// </summary>
/// <remarks>
/// Prefer <see cref="Result" /> for new authoring code when the shorter name is a better fit.
/// This type remains available as a compatibility alias.
/// </remarks>
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
    public static BehaviorResultDescriptor NoContent(string? message = null, string? code = null)
        => new(BehaviorResultStatus.NoContent, message, code, fault: null);

    /// <summary>
    /// Creates a no-content result for the specified payload type.
    /// </summary>
    public static BehaviorResult<T> NoContent<T>(string? message = null, string? code = null)
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
    public static BehaviorResult<T> Invalid<T>(
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
    public static BehaviorResult<T> Unauthorized<T>(
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
    public static BehaviorResult<T> Forbidden<T>(
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
    public static BehaviorResult<T> NotFound<T>(
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
    public static BehaviorResult<T> Conflict<T>(
        string code,
        string message,
        BehaviorFault? fault = null)
        => Conflict(code, message, fault);
}

/// <summary>
/// Represents a legacy transport-neutral behavior outcome with an optional payload value.
/// </summary>
/// <typeparam name="T">The payload type carried by the result.</typeparam>
/// <remarks>
/// Prefer <see cref="Result{T}" /> for new authoring code when the shorter name is a better fit.
/// This type remains available as a compatibility alias.
/// </remarks>
public sealed class BehaviorResult<T> : Result<T>
{
    private BehaviorResult(
        BehaviorResultStatus status,
        T? value,
        bool hasValue,
        string? message,
        string? code,
        BehaviorFault? fault)
        : base(status, value, hasValue, message, code, fault)
    {
    }

    /// <summary>
    /// Converts a raw payload value into a successful result.
    /// </summary>
    public static implicit operator BehaviorResult<T>(T value)
        => BehaviorResult.Ok(value);

    /// <summary>
    /// Converts a no-payload descriptor into a typed behavior result.
    /// </summary>
    public static implicit operator BehaviorResult<T>(BehaviorResultDescriptor descriptor)
    {
        return Create(
            descriptor.Status,
            default,
            hasValue: false,
            descriptor.Message,
            descriptor.Code,
            descriptor.Fault);
    }

    internal static new BehaviorResult<T> Create(
        BehaviorResultStatus status,
        T? value,
        bool hasValue,
        string? message,
        string? code,
        BehaviorFault? fault)
    {
        return new BehaviorResult<T>(status, value, hasValue, message, code, fault);
    }
}
