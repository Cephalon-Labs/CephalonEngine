namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector governance states used by CDC execution-runtime descriptors.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates
{
    /// <summary>
    /// The execution runtime cannot currently determine its managed-connector governance posture.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// The execution runtime does not currently represent a managed connector.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The execution runtime is currently declared as an observe-only managed connector.
    /// </summary>
    public const string ObserveOnly = "observe-only";

    /// <summary>
    /// The execution runtime declares a future write-path or control-plane management mode that Cephalon does not yet own.
    /// </summary>
    public const string FutureControlPlane = "future-control-plane";

    /// <summary>
    /// The execution runtime is missing required managed-connector governance metadata.
    /// </summary>
    public const string OutOfPolicy = "out-of-policy";
}
