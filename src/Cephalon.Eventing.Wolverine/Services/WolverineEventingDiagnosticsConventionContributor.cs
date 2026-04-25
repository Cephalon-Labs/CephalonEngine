using Cephalon.Engine.Diagnostics;

namespace Cephalon.Eventing.Wolverine.Services;

internal sealed class WolverineEventingDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => WolverineEventingDiagnosticsConventions.Convention;
}

internal static class WolverineEventingDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition DispatchLoopStarted = new(
        Id: 4300,
        Name: "WolverineDispatchLoopStarted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Wolverine-managed event dispatch loop started with batch size {BatchSize} and polling interval {PollingIntervalSeconds} seconds.",
        Description: "Emitted when the Wolverine-managed staged-event dispatch loop starts running with its effective polling settings.");

    public static readonly DiagnosticEventDefinition DispatchLoopStopped = new(
        Id: 4301,
        Name: "WolverineDispatchLoopStopped",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Wolverine-managed event dispatch loop stopped.",
        Description: "Emitted when the Wolverine-managed staged-event dispatch loop stops.");

    public static readonly DiagnosticEventDefinition DispatchReadFailed = new(
        Id: 4302,
        Name: "WolverineDispatchReadFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Wolverine-managed event dispatch loop could not read pending staged events.",
        Description: "Emitted when the Wolverine-managed staged-event dispatch loop cannot read the next batch of pending dispatch items.");

    public static readonly DiagnosticEventDefinition DispatchObservationProjectionFailed = new(
        Id: 4303,
        Name: "WolverineDispatchObservationProjectionFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Wolverine-managed event dispatch loop could not project runtime observation '{Outcome}' for message '{MessageId}'.",
        Description: "Emitted when the Wolverine-managed staged-event dispatch loop cannot write its runtime observation back into the Cephalon dispatch-runtime reporting surface.");

    public static readonly DiagnosticEventDefinition DispatchActivityStarted = new(
        Id: 4304,
        Name: "WolverineDispatchActivityStarted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Wolverine-managed event dispatch activity started for message '{MessageId}'.",
        Description: "Emitted when a distributed tracing activity is created for an individual event dispatch operation.");

    public static readonly DiagnosticEventDefinition DispatchMetricsRecorded = new(
        Id: 4305,
        Name: "WolverineDispatchMetricsRecorded",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Wolverine-managed event dispatch metrics recorded.",
        Description: "Emitted when dispatch metrics (attempts, successes, failures, retries, duration) are recorded through the OpenTelemetry-compatible meter.");

    public static readonly DiagnosticEventDefinition SubscriptionObservationProjectionFailed = new(
        Id: 4306,
        Name: "WolverineSubscriptionObservationProjectionFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Wolverine-managed subscription execution could not project runtime observation '{Outcome}' for subscription '{SubscriptionId}'.",
        Description: "Emitted when the Wolverine-managed subscription execution path cannot write its runtime observation back into the shared Cephalon subscription reporting surface.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Eventing.Wolverine",
        LoggerCategoryPrefix: "Cephalon.Eventing.Wolverine",
        Description: "Structured diagnostics for the Wolverine-managed staged-event dispatch loop.",
        Events:
        [
            DispatchLoopStarted,
            DispatchLoopStopped,
            DispatchReadFailed,
            DispatchObservationProjectionFailed,
            DispatchActivityStarted,
            DispatchMetricsRecorded,
            SubscriptionObservationProjectionFailed
        ]);
}
