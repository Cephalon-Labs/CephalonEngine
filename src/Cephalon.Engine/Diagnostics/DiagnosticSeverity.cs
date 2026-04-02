namespace Cephalon.Engine.Diagnostics;

/// <summary>
/// Describes the severity attached to a published diagnostics event definition.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// The event is useful only for highly detailed trace output.
    /// </summary>
    Trace = 0,

    /// <summary>
    /// The event is intended for debug-oriented diagnostics.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// The event describes expected informational runtime behavior.
    /// </summary>
    Information = 2,

    /// <summary>
    /// The event highlights a warning condition or degraded behavior.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// The event indicates an error condition.
    /// </summary>
    Error = 4,

    /// <summary>
    /// The event indicates a critical condition that usually requires immediate attention.
    /// </summary>
    Critical = 5
}
