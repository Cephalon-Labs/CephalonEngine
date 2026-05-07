using Cephalon.Abstractions.Technologies;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.Runtime;

namespace Cephalon.Observability.Kubernetes.Hosting;

internal sealed class KubernetesTelemetryRuntimeContributor(TelemetryExportOptions telemetry) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return TelemetryExportRuntimeSurfaceFactory.CreateSurface(
            surfaceId: "telemetry-export-kubernetes",
            displayName: "Kubernetes Telemetry Export",
            description: "Projects sanitized Kubernetes observability exporter wiring for the active host.",
            entryId: "kubernetes",
            entryDisplayName: "Kubernetes Telemetry Export",
            entryDescription: "Cephalon.Observability.Kubernetes is active for host telemetry export.",
            telemetry: telemetry,
            metadata: CreateMetadata("Cephalon.Observability.Kubernetes", "platform-observability"));
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
