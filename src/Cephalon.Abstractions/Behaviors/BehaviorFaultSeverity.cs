using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Severity levels for structured behavior faults.
/// </summary>
public enum BehaviorFaultSeverity
{
    /// <summary>
    /// Informational fault details.
    /// </summary>
    [JsonStringEnumMemberName("info")]
    Info = 0,

    /// <summary>
    /// Warning-level fault details.
    /// </summary>
    [JsonStringEnumMemberName("warning")]
    Warning = 1,

    /// <summary>
    /// Error-level fault details.
    /// </summary>
    [JsonStringEnumMemberName("error")]
    Error = 2,

    /// <summary>
    /// Critical fault details.
    /// </summary>
    [JsonStringEnumMemberName("critical")]
    Critical = 3
}
