using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.NewRelic.Hosting;

internal sealed class NewRelicDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => NewRelicDiagnosticsConventions.Convention;
}

internal static class NewRelicDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ExportSummary = new(
        Id: 3154,
        Name: "NewRelicTelemetrySummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "New Relic observability export uses endpoint mode {EndpointMode}, endpoint target {EndpointTarget}, authentication mode {AuthenticationMode}, region selection {RegionSelection}, resource context {ResourceContext}, signals {Signals}.",
        Description: "Emitted once on host startup to summarize the active New Relic observability defaults without logging secrets.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.NewRelic",
        LoggerCategoryPrefix: "Cephalon.Observability.NewRelic",
        Description: "New Relic OTLP endpoint and api-key authentication diagnostics.",
        Events:
        [
            ExportSummary
        ]);
}
