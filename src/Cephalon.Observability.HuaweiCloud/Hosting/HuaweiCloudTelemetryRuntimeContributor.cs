using Cephalon.Abstractions.Technologies;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.Runtime;

namespace Cephalon.Observability.HuaweiCloud.Hosting;

internal sealed class HuaweiCloudTelemetryRuntimeContributor(TelemetryExportOptions telemetry) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return TelemetryExportRuntimeSurfaceFactory.CreateSurface(
            surfaceId: "telemetry-export-huawei-cloud",
            displayName: "Huawei Cloud Telemetry Export",
            description: "Projects sanitized Huawei Cloud observability exporter wiring for the active host.",
            entryId: "huawei-cloud",
            entryDisplayName: "Huawei Cloud Telemetry Export",
            entryDescription: "Cephalon.Observability.HuaweiCloud is active for host telemetry export.",
            telemetry: telemetry,
            metadata: CreateMetadata("Cephalon.Observability.HuaweiCloud", "cloud-observability"));
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
