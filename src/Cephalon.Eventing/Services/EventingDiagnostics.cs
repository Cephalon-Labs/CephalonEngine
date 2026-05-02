using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Cephalon.Diagnostics;

namespace Cephalon.Eventing.Services;

/// <summary>
/// Defines the stable activity source, meter, activity, counter, and tag names emitted by the
/// eventing companion runtime. Names are sourced from <see cref="CephalonActivitySources.Eventing"/>
/// and <see cref="CephalonMeters.Eventing"/> so the eventing pack and observability companion
/// packs share one canonical name set with the rest of the engine.
/// </summary>
public static class EventingDiagnostics
{
    /// <summary>
    /// Gets the stable activity-source name emitted by the eventing runtime.
    /// </summary>
    public const string ActivitySourceName = CephalonActivitySources.Eventing;

    /// <summary>
    /// Gets the stable meter name emitted by the eventing runtime.
    /// </summary>
    public const string MeterName = CephalonMeters.Eventing;

    /// <summary>
    /// Gets the stable activity name emitted around one in-process event publication dispatch.
    /// </summary>
    public const string PublicationDispatchActivityName = "eventing.publication.dispatch";

    /// <summary>
    /// Gets the stable counter name for completed in-process publication dispatches.
    /// </summary>
    public const string PublicationDispatchCounterName = "cephalon.eventing.publications";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the publisher identifier responsible for the dispatch.
    /// </summary>
    public const string PublisherIdTag = "cephalon.eventing.publisher.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the publication identifier emitted on the activity.
    /// </summary>
    public const string PublicationIdTag = "cephalon.eventing.publication.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the channel identifier emitted on the activity.
    /// </summary>
    public const string ChannelIdTag = "cephalon.eventing.channel.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the event type emitted on the activity.
    /// </summary>
    public const string EventTypeTag = "cephalon.eventing.event.type";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the publication outcome emitted on the activity
    /// (succeeded, skipped, or failed).
    /// </summary>
    public const string PublicationOutcomeTag = "cephalon.eventing.publication.outcome";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the count of subscriptions matched for the publication.
    /// </summary>
    public const string MatchedSubscriptionCountTag = "cephalon.eventing.matched_subscription.count";

    private static readonly string Version = typeof(EventingDiagnostics).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion
        ?? typeof(EventingDiagnostics).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);
    internal static readonly Meter Meter = new(MeterName, Version);
    internal static readonly Counter<long> PublicationDispatchCounter = Meter.CreateCounter<long>(
        PublicationDispatchCounterName,
        unit: "publications",
        description: "Counts in-process event publication dispatches by outcome.");
}
