using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Cephalon.Diagnostics;

namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines the stable activity source, meter, activity, counter, and tag names emitted by the
/// multi-tenancy governance companion runtime. Names are sourced from
/// <see cref="CephalonActivitySources.MultiTenancyGovernance"/> and
/// <see cref="CephalonMeters.MultiTenancyGovernance"/> so the governance pack and observability
/// companion packs share one canonical name set with the rest of the engine.
/// </summary>
public static class GovernanceDiagnostics
{
    /// <summary>
    /// Gets the stable activity-source name emitted by the governance runtime.
    /// </summary>
    public const string ActivitySourceName = CephalonActivitySources.MultiTenancyGovernance;

    /// <summary>
    /// Gets the stable meter name emitted by the governance runtime.
    /// </summary>
    public const string MeterName = CephalonMeters.MultiTenancyGovernance;

    /// <summary>
    /// Gets the stable activity name emitted around one in-process invitation delivery dispatch.
    /// </summary>
    public const string InvitationDispatchActivityName = "multitenancy.governance.invitation.delivery.dispatch";

    /// <summary>
    /// Gets the stable counter name for completed invitation delivery dispatches.
    /// </summary>
    public const string InvitationDispatchCounterName = "cephalon.multitenancy_governance.invitation_dispatches";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the invitation identifier emitted on the activity.
    /// </summary>
    public const string InvitationIdTag = "cephalon.multitenancy_governance.invitation.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the delivery channel emitted on the activity
    /// (for example <c>email</c>, <c>sms</c>, or a host-defined channel name).
    /// </summary>
    public const string DeliveryChannelTag = "cephalon.multitenancy_governance.delivery.channel";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the delivery sender identifier emitted on the activity
    /// once a sender has been resolved for the dispatch.
    /// </summary>
    public const string DeliverySenderIdTag = "cephalon.multitenancy_governance.delivery.sender.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the delivery dispatch outcome emitted on the activity
    /// (one of the <c>TenantInvitationDeliveryOutcomes</c> values).
    /// </summary>
    public const string DeliveryOutcomeTag = "cephalon.multitenancy_governance.delivery.outcome";

    private static readonly string Version = typeof(GovernanceDiagnostics).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion
        ?? typeof(GovernanceDiagnostics).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);
    internal static readonly Meter Meter = new(MeterName, Version);
    internal static readonly Counter<long> InvitationDispatchCounter = Meter.CreateCounter<long>(
        InvitationDispatchCounterName,
        unit: "dispatches",
        description: "Counts invitation delivery dispatches by channel, sender, and outcome.");
}
