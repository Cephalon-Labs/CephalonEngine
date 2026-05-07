using Cephalon.Abstractions.Technologies;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.Runtime;

namespace Cephalon.Observability.GrafanaCloud.Hosting;

internal sealed class GrafanaCloudTelemetryRuntimeContributor(TelemetryExportOptions telemetry) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return TelemetryExportRuntimeSurfaceFactory.CreateSurface(
            surfaceId: "telemetry-export-grafana-cloud",
            displayName: "Grafana Cloud Telemetry Export",
            description: "Projects sanitized Grafana Cloud observability exporter wiring for the active host.",
            entryId: "grafana-cloud",
            entryDisplayName: "Grafana Cloud Telemetry Export",
            entryDescription: "Cephalon.Observability.GrafanaCloud is active for host telemetry export.",
            telemetry: telemetry,
            metadata: CreateMetadata("Cephalon.Observability.GrafanaCloud", "observability-saas"));
    }

    private static Dictionary<string, string> CreateMetadata(string pack, string integrationKind)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["pack"] = pack,
            ["integrationKind"] = integrationKind
        };
    }
}
