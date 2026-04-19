namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the recommended stable publication-state identifiers for CDC runtime reporting.
/// </summary>
public static class CdcCapturePublicationStates
{
    /// <summary>
    /// Indicates that the active runtime does not yet have a publication answer.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// Indicates that the capture is current through the linked publication path.
    /// </summary>
    public const string Current = "current";

    /// <summary>
    /// Indicates that the capture still has pending publications to push into or through the outbox.
    /// </summary>
    public const string PendingPublication = "pending-publication";

    /// <summary>
    /// Indicates that the linked outbox dispatch runtime is actively dispatching publications.
    /// </summary>
    public const string Dispatching = "dispatching";

    /// <summary>
    /// Indicates that the linked outbox dispatch runtime has a retry pending.
    /// </summary>
    public const string DispatchRetryPending = "dispatch-retry-pending";

    /// <summary>
    /// Indicates that the linked outbox dispatch runtime last reported a failure.
    /// </summary>
    public const string DispatchFailed = "dispatch-failed";

    /// <summary>
    /// Indicates that the capture itself last reported a failure before publication completed.
    /// </summary>
    public const string CaptureFailed = "capture-failed";
}
