namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines stable readiness-state identifiers for declared event-subscription execution paths.
/// </summary>
/// <remarks>
/// These values describe how Cephalon can currently observe or bind a declared subscription
/// without claiming that the core eventing package owns a generic broker runtime.
/// </remarks>
public static class EventSubscriptionExecutionReadinessStates
{
    /// <summary>
    /// The subscription is bound to a managed execution runtime contributed by a companion pack.
    /// </summary>
    public const string RuntimeBound = "runtime-bound";

    /// <summary>
    /// The subscription is linked to a host-managed execution service.
    /// </summary>
    public const string HostedExecutionLinked = "hosted-execution-linked";

    /// <summary>
    /// The subscription has reported application-managed runtime observations.
    /// </summary>
    public const string ApplicationManagedState = "application-managed-state";

    /// <summary>
    /// The subscription is declared but no execution path has been bound, linked, or observed.
    /// </summary>
    public const string DeclaredOnly = "declared-only";
}
