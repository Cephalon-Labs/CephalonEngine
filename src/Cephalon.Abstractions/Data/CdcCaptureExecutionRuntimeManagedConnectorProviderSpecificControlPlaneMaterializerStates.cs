namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector provider-specific control-plane materializer answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates
{
    /// <summary>
    /// Provider-specific control-plane materializer follow-through does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Provider-specific control-plane materializer follow-through still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Provider-specific control-plane materializer follow-through does not currently expose enough provider identity or adapter truth to select one concrete materializer.
    /// </summary>
    public const string MaterializerUnavailable = "materializer-unavailable";

    /// <summary>
    /// Provider-specific control-plane materializer follow-through is currently ready to select one concrete materializer on the shared runtime surface.
    /// </summary>
    public const string MaterializerReady = "materializer-ready";

    /// <summary>
    /// Provider-specific control-plane materializer follow-through has selected one concrete materializer on the shared runtime surface.
    /// </summary>
    public const string MaterializerSelected = "materializer-selected";

    /// <summary>
    /// The selected provider-specific control-plane materializer is currently executing or actively driving one provider-owned lane.
    /// </summary>
    public const string MaterializerExecuting = "materializer-executing";

    /// <summary>
    /// Provider-specific control-plane materializer follow-through currently remains risky because broader provider or runtime truth is not safe enough yet.
    /// </summary>
    public const string MaterializerRisk = "materializer-risk";
}
