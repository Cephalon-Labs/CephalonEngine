namespace Cephalon.Data.Services;

/// <summary>
/// Defines the stable outcome identifiers used when reporting CDC capture activity.
/// </summary>
public static class CdcCaptureRuntimeOutcomes
{
    /// <summary>
    /// Gets the outcome identifier used when a capture runtime starts or resumes work.
    /// </summary>
    public const string Started = "started";

    /// <summary>
    /// Gets the outcome identifier used when a capture observes one or more source changes.
    /// </summary>
    public const string Captured = "captured";

    /// <summary>
    /// Gets the outcome identifier used when a capture polls successfully but finds no new changes.
    /// </summary>
    public const string Idle = "idle";

    /// <summary>
    /// Gets the outcome identifier used when a capture fails.
    /// </summary>
    public const string Failed = "failed";
}
