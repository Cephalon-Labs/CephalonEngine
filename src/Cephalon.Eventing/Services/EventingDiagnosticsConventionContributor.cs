using Cephalon.Engine.Diagnostics;

namespace Cephalon.Eventing.Services;

internal sealed class EventingDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => EventingDiagnosticsConventions.Convention;
}

internal static class EventingDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition PublicationStaged = new(
        Id: 4200,
        Name: "EventPublicationStaged",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Event publication '{PublicationId}' was staged on channel '{ChannelId}' through the active outbox path.",
        Description: "Emitted when the eventing runtime stages one publication through the active outbox path.");

    public static readonly DiagnosticEventDefinition SubscriptionStarted = new(
        Id: 4201,
        Name: "EventSubscriptionStarted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Declared subscription '{SubscriptionId}' started handling message '{MessageId}' at attempt {Attempt}.",
        Description: "Emitted when application-managed subscription handling begins for one message.");

    public static readonly DiagnosticEventDefinition SubscriptionSucceeded = new(
        Id: 4202,
        Name: "EventSubscriptionSucceeded",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Declared subscription '{SubscriptionId}' succeeded for message '{MessageId}' at attempt {Attempt}.",
        Description: "Emitted when application-managed subscription handling succeeds for one message.");

    public static readonly DiagnosticEventDefinition SubscriptionFailed = new(
        Id: 4203,
        Name: "EventSubscriptionFailed",
        Severity: DiagnosticSeverity.Error,
        MessageTemplate: "Declared subscription '{SubscriptionId}' failed for message '{MessageId}' at attempt {Attempt}.",
        Description: "Emitted when application-managed subscription handling fails for one message.");

    public static readonly DiagnosticEventDefinition SubscriptionRetryScheduled = new(
        Id: 4204,
        Name: "EventSubscriptionRetryScheduled",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Declared subscription '{SubscriptionId}' scheduled another retry for message '{MessageId}' at attempt {Attempt}.",
        Description: "Emitted when application-managed subscription handling reports that another retry is expected.");

    public static readonly DiagnosticEventDefinition SubscriptionSkipped = new(
        Id: 4205,
        Name: "EventSubscriptionSkipped",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Declared subscription '{SubscriptionId}' skipped message '{MessageId}' at attempt {Attempt}.",
        Description: "Emitted when application-managed subscription handling intentionally skips one message.");

    public static readonly DiagnosticEventDefinition PublicationDispatchStarted = new(
        Id: 4206,
        Name: "EventPublicationDispatchStarted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Event publisher '{PublisherId}' started dispatch for publication '{PublicationId}' at attempt {Attempt}.",
        Description: "Emitted when application-managed publication dispatch begins for one staged publication.");

    public static readonly DiagnosticEventDefinition PublicationDispatchSucceeded = new(
        Id: 4207,
        Name: "EventPublicationDispatchSucceeded",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Event publisher '{PublisherId}' succeeded for publication '{PublicationId}' at attempt {Attempt}.",
        Description: "Emitted when application-managed publication dispatch succeeds for one staged publication.");

    public static readonly DiagnosticEventDefinition PublicationDispatchFailed = new(
        Id: 4208,
        Name: "EventPublicationDispatchFailed",
        Severity: DiagnosticSeverity.Error,
        MessageTemplate: "Event publisher '{PublisherId}' failed for publication '{PublicationId}' at attempt {Attempt}.",
        Description: "Emitted when application-managed publication dispatch fails for one staged publication.");

    public static readonly DiagnosticEventDefinition PublicationDispatchRetryScheduled = new(
        Id: 4209,
        Name: "EventPublicationDispatchRetryScheduled",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Event publisher '{PublisherId}' scheduled another retry for publication '{PublicationId}' at attempt {Attempt}.",
        Description: "Emitted when application-managed publication dispatch reports that another retry is expected.");

    public static readonly DiagnosticEventDefinition PublicationDispatchSkipped = new(
        Id: 4210,
        Name: "EventPublicationDispatchSkipped",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Event publisher '{PublisherId}' skipped publication '{PublicationId}' at attempt {Attempt}.",
        Description: "Emitted when application-managed publication dispatch intentionally skips one staged publication.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Eventing",
        LoggerCategoryPrefix: "Cephalon.Eventing",
        Description: "Structured publication staging, publication dispatch, and declared-subscription execution diagnostics for the eventing runtime pack.",
        Events:
        [
            PublicationStaged,
            SubscriptionStarted,
            SubscriptionSucceeded,
            SubscriptionFailed,
            SubscriptionRetryScheduled,
            SubscriptionSkipped,
            PublicationDispatchStarted,
            PublicationDispatchSucceeded,
            PublicationDispatchFailed,
            PublicationDispatchRetryScheduled,
            PublicationDispatchSkipped
        ]);
}
