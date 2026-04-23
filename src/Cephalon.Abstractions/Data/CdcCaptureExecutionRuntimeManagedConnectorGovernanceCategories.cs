namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector governance categories used by CDC execution-runtime descriptors.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories
{
    /// <summary>
    /// The execution runtime does not currently declare a managed-connector management mode.
    /// </summary>
    public const string MissingManagementMode = "missing-management-mode";

    /// <summary>
    /// The execution runtime does not currently declare the upstream connector-cluster identifier.
    /// </summary>
    public const string MissingConnectClusterId = "missing-connect-cluster-id";

    /// <summary>
    /// The execution runtime does not currently declare the upstream connector class.
    /// </summary>
    public const string MissingConnectorClass = "missing-connector-class";

    /// <summary>
    /// The execution runtime does not currently declare the upstream source-provider identifier.
    /// </summary>
    public const string MissingSourceProviderId = "missing-source-provider-id";

    /// <summary>
    /// The execution runtime declares a future control-plane management mode that Cephalon has not adopted yet.
    /// </summary>
    public const string FutureControlPlaneMode = "future-control-plane-mode";
}
