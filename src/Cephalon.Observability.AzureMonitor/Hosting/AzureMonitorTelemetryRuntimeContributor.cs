using Cephalon.Abstractions.Technologies;
using Cephalon.Observability.AzureMonitor.Configuration;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.Runtime;

namespace Cephalon.Observability.AzureMonitor.Hosting;

internal sealed class AzureMonitorTelemetryRuntimeContributor(
    TelemetryExportOptions telemetry,
    AzureMonitorExportOptions options) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return TelemetryExportRuntimeSurfaceFactory.CreateSurface(
            surfaceId: "telemetry-export-azure-monitor",
            displayName: "Azure Monitor Telemetry Export",
            description: "Projects sanitized Azure Monitor observability exporter wiring for the active host.",
            entryId: "azure-monitor",
            entryDisplayName: "Azure Monitor Telemetry Export",
            entryDescription: "Cephalon.Observability.AzureMonitor is active for host telemetry export.",
            telemetry: telemetry,
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pack"] = "Cephalon.Observability.AzureMonitor",
                ["integrationKind"] = "cloud-observability",
                ["providerConnectionConfigured"] = string.IsNullOrWhiteSpace(options.ConnectionString) ? "false" : "true",
                ["providerConnectionProjection"] = "redacted"
            });
    }
}
