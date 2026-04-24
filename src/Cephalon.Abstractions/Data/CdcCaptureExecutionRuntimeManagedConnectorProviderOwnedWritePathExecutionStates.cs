namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector provider-owned write-path execution answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates
{
    /// <summary>
    /// Provider-owned write-path execution does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Provider-owned write-path execution still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Provider-owned write-path execution is currently ready on the shared runtime surface.
    /// </summary>
    public const string ProviderExecutable = "provider-executable";

    /// <summary>
    /// Provider-owned write-path execution remains blocked by shared runtime policy, approval, or adapter posture.
    /// </summary>
    public const string ProviderBlocked = "provider-blocked";

    /// <summary>
    /// Provider-owned write-path execution already translated one provider-facing command shape.
    /// </summary>
    public const string ProviderOwnedExecuting = "provider-owned-executing";

    /// <summary>
    /// Provider-owned write-path execution no longer needs an additional provider command on the shared lane.
    /// </summary>
    public const string ProviderOwnedCompleted = "provider-owned-completed";

    /// <summary>
    /// Provider-owned write-path execution currently remains risky because broader shared runtime truth is not safe enough yet.
    /// </summary>
    public const string ProviderOwnedRisk = "provider-owned-risk";
}
