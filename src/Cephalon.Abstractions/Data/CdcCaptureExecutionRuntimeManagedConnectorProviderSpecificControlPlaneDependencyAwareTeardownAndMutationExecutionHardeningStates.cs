namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector provider-specific control-plane dependency-aware teardown and mutation-execution hardening answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates
{
    /// <summary>
    /// Provider-specific dependency-aware teardown and mutation-execution hardening does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Provider-specific dependency-aware teardown and mutation-execution hardening still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// Provider-specific dependency-aware teardown and mutation-execution hardening is currently ready on the shared runtime surface.
    /// </summary>
    public const string DependencyReady = "dependency-ready";

    /// <summary>
    /// Provider-specific dependency-aware teardown currently remains blocked.
    /// </summary>
    public const string TeardownBlocked = "teardown-blocked";

    /// <summary>
    /// Provider-specific dependency-aware mutation execution currently remains blocked.
    /// </summary>
    public const string MutationExecutionBlocked = "mutation-execution-blocked";

    /// <summary>
    /// Provider-specific dependency-aware teardown and mutation execution currently remains degraded.
    /// </summary>
    public const string DependencyDegraded = "dependency-degraded";

    /// <summary>
    /// Provider-specific dependency-aware teardown hardening is fully hardened.
    /// </summary>
    public const string TeardownHardened = "teardown-hardened";

    /// <summary>
    /// Provider-specific dependency-aware mutation execution hardening is fully hardened.
    /// </summary>
    public const string MutationExecutionHardened = "mutation-execution-hardened";

    /// <summary>
    /// Provider-specific dependency-aware teardown and mutation execution currently remains risky.
    /// </summary>
    public const string DependencyRisk = "dependency-risk";
}
