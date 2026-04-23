namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable desired-versus-observed drift states used by managed-connector CDC execution runtimes.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDriftStates
{
    /// <summary>
    /// The drift posture could not be determined from the current managed-connector declaration and report data.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// The execution runtime does not currently represent a managed connector.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The managed connector currently reports no declared-versus-observed drift.
    /// </summary>
    public const string InSync = "in-sync";

    /// <summary>
    /// The managed connector currently reports declared-versus-observed drift.
    /// </summary>
    public const string Drifted = "drifted";
}
