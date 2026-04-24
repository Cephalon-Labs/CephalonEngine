namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector provider execution-orchestration answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates
{
    /// <summary>
    /// Provider execution orchestration does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Provider execution orchestration still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Provider execution orchestration is currently ready on the shared runtime surface.
    /// </summary>
    public const string OrchestrationReady = "orchestration-ready";

    /// <summary>
    /// Provider execution orchestration remains blocked by shared runtime policy, scheduler, or orchestration truth.
    /// </summary>
    public const string OrchestrationBlocked = "orchestration-blocked";

    /// <summary>
    /// Provider execution orchestration is currently executing one provider-facing orchestration step.
    /// </summary>
    public const string OrchestrationExecuting = "orchestration-executing";

    /// <summary>
    /// Provider execution orchestration no longer needs an additional provider-facing orchestration step on the shared lane.
    /// </summary>
    public const string OrchestrationCompleted = "orchestration-completed";

    /// <summary>
    /// Provider execution orchestration currently remains risky because broader shared runtime truth is not safe enough yet.
    /// </summary>
    public const string OrchestrationRisk = "orchestration-risk";
}
