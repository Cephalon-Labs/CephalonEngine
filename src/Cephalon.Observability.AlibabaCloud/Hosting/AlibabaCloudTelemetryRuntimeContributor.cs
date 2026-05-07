using Cephalon.Abstractions.Technologies;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.Runtime;

namespace Cephalon.Observability.AlibabaCloud.Hosting;

internal sealed class AlibabaCloudTelemetryRuntimeContributor(TelemetryExportOptions telemetry) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return TelemetryExportRuntimeSurfaceFactory.CreateSurface(
            surfaceId: "telemetry-export-alibaba-cloud",
            displayName: "Alibaba Cloud Telemetry Export",
            description: "Projects sanitized Alibaba Cloud observability exporter wiring for the active host.",
            entryId: "alibaba-cloud",
            entryDisplayName: "Alibaba Cloud Telemetry Export",
            entryDescription: "Cephalon.Observability.AlibabaCloud is active for host telemetry export.",
            telemetry: telemetry,
            metadata: CreateMetadata("Cephalon.Observability.AlibabaCloud", "cloud-observability"));
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
