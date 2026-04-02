using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.Hosting;

internal sealed class ObservabilityDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => ObservabilityDiagnosticsConventions.Convention;
}

internal static class ObservabilityDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ManifestSummary = new(
        Id: 3000,
        Name: "ManifestSummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Runtime manifest {ManifestVersion} for engine {EngineVersion} is active on blueprint {BlueprintId}. Modules {ModuleCount}. Capabilities {CapabilityCount}.",
        Description: "Emitted once on host startup to summarize the active runtime manifest.");

    public static readonly DiagnosticEventDefinition ModuleSummary = new(
        Id: 3001,
        Name: "ModuleSummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Module loaded '{ModuleId}' version {Version}. Dependencies {DependencyCount}. Tags {TagCount}.",
        Description: "Emitted on host startup for each module when module-summary logging is enabled.");

    public static readonly DiagnosticEventDefinition CapabilitySummary = new(
        Id: 3002,
        Name: "CapabilitySummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Capability exposed '{CapabilityKey}' from module '{SourceModuleId}'.",
        Description: "Emitted on host startup for each published capability when capability-summary logging is enabled.");

    public static readonly DiagnosticEventDefinition DiagnosticsConvention = new(
        Id: 3003,
        Name: "DiagnosticsConvention",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Diagnostics use meter {MeterName} and activity source {ActivitySourceName}.",
        Description: "Emitted on host startup to describe the stable engine metrics and tracing names.");

    public static readonly DiagnosticEventDefinition OperationalHealth = new(
        Id: 3004,
        Name: "OperationalHealth",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Operational health reports liveness {LivenessState}, readiness {ReadinessState}, runtime status {RuntimeStatus}.",
        Description: "Emitted on host startup to summarize the current runtime health state.");

    public static readonly DiagnosticEventDefinition TelemetryExport = new(
        Id: 3005,
        Name: "TelemetryExport",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Telemetry export guidance uses provider {Provider}, protocol {Protocol}, endpoint {Endpoint}, logs {ExportLogs}, metrics {ExportMetrics}, traces {ExportTraces}.",
        Description: "Emitted on host startup to summarize the configured telemetry export guidance.");

    public static readonly DiagnosticEventDefinition DiagnosticsCatalogEntry = new(
        Id: 3006,
        Name: "DiagnosticsCatalogEntry",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Diagnostics convention '{Source}' uses logger category prefix {LoggerCategoryPrefix}, event ids {EventIdRange}, and {EventCount} published events.",
        Description: "Emitted on host startup for each published diagnostics convention visible to the runtime.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability",
        LoggerCategoryPrefix: "Cephalon.Observability",
        Description: "Structured startup-summary diagnostics for manifest, module, capability, telemetry, and diagnostics-catalog guidance.",
        Events:
        [
            ManifestSummary,
            ModuleSummary,
            CapabilitySummary,
            DiagnosticsConvention,
            OperationalHealth,
            TelemetryExport,
            DiagnosticsCatalogEntry
        ]);
}
