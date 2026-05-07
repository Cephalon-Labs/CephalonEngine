using Cephalon.Abstractions.Technologies;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.Runtime;

namespace Cephalon.Observability.OracleCloud.Hosting;

internal sealed class OracleCloudTelemetryRuntimeContributor(TelemetryExportOptions telemetry) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return TelemetryExportRuntimeSurfaceFactory.CreateSurface(
            surfaceId: "telemetry-export-oracle-cloud",
            displayName: "Oracle Cloud Telemetry Export",
            description: "Projects sanitized Oracle Cloud observability exporter wiring for the active host.",
            entryId: "oracle-cloud",
            entryDisplayName: "Oracle Cloud Telemetry Export",
            entryDescription: "Cephalon.Observability.OracleCloud is active for host telemetry export.",
            telemetry: telemetry,
            metadata: CreateMetadata("Cephalon.Observability.OracleCloud", "cloud-observability"));
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
