using Cephalon.Abstractions.Technologies;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.Runtime;

namespace Cephalon.Observability.Tanzu.Hosting;

internal sealed class TanzuTelemetryRuntimeContributor(TelemetryExportOptions telemetry) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return TelemetryExportRuntimeSurfaceFactory.CreateSurface(
            surfaceId: "telemetry-export-tanzu",
            displayName: "Tanzu Telemetry Export",
            description: "Projects sanitized Tanzu observability exporter wiring for the active host.",
            entryId: "tanzu",
            entryDisplayName: "Tanzu Telemetry Export",
            entryDescription: "Cephalon.Observability.Tanzu is active for host telemetry export.",
            telemetry: telemetry,
            metadata: CreateMetadata("Cephalon.Observability.Tanzu", "platform-observability"));
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
