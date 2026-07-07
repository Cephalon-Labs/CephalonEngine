using System.Text.Json.Serialization;

namespace Cephalon.Engine.Runtime;

/// <summary>
/// Describes one operator-readable condition for a runtime introspection entry.
/// </summary>
public sealed class RuntimeOperatorCondition
{
    /// <summary>Creates an operator-readable runtime condition.</summary>
    /// <param name="type">The stable condition type.</param>
    /// <param name="status">The normalized condition status, such as <c>true</c>, <c>false</c>, or <c>unknown</c>.</param>
    /// <param name="severity">The operator severity, such as <c>info</c>, <c>warning</c>, or <c>error</c>.</param>
    /// <param name="reason">The stable machine-readable reason.</param>
    /// <param name="message">The operator-facing condition message.</param>
    /// <param name="observedAtUtc">The UTC timestamp at which the condition was observed, when known.</param>
    [JsonConstructor]
    public RuntimeOperatorCondition(
        string type,
        string status,
        string severity,
        string reason,
        string message,
        DateTimeOffset? observedAtUtc = null)
    {
        Type = RequireValue(type, nameof(type), "Condition type is required.");
        Status = RequireValue(status, nameof(status), "Condition status is required.");
        Severity = RequireValue(severity, nameof(severity), "Condition severity is required.");
        Reason = RequireValue(reason, nameof(reason), "Condition reason is required.");
        Message = RequireValue(message, nameof(message), "Condition message is required.");
        ObservedAtUtc = observedAtUtc;
    }

    /// <summary>Gets the stable condition type.</summary>
    public string Type { get; }

    /// <summary>Gets the normalized condition status.</summary>
    public string Status { get; }

    /// <summary>Gets the operator severity.</summary>
    public string Severity { get; }

    /// <summary>Gets the stable machine-readable reason.</summary>
    public string Reason { get; }

    /// <summary>Gets the operator-facing condition message.</summary>
    public string Message { get; }

    /// <summary>Gets the UTC timestamp at which the condition was observed, when known.</summary>
    public DateTimeOffset? ObservedAtUtc { get; }

    private static string RequireValue(string value, string parameterName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, parameterName);
        }

        return value.Trim();
    }
}
