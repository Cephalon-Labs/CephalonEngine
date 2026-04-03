using Cephalon.Engine.Diagnostics;

namespace Cephalon.Observability.DigitalOcean.Hosting;

internal sealed class DigitalOceanDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => DigitalOceanDiagnosticsConventions.Convention;
}

internal static class DigitalOceanDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition ExportSummary = new(
        Id: 3115,
        Name: "DigitalOceanTelemetrySummary",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "DigitalOcean observability export uses endpoint mode {EndpointMode}, hosted platform {HostedPlatform}, collector target {CollectorTarget}, trust mode {TrustMode}, context mode {ContextMode}, signals {Signals}.",
        Description: "Emitted once on host startup to summarize the active DigitalOcean observability defaults without logging secrets.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Observability.DigitalOcean",
        LoggerCategoryPrefix: "Cephalon.Observability.DigitalOcean",
        Description: "DigitalOcean collector defaults, best-effort Droplet metadata, and hosted resource-default diagnostics.",
        Events:
        [
            ExportSummary
        ]);
}
