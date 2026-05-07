using Cephalon.Abstractions.Technologies;
using Cephalon.Observability.Configuration;

namespace Cephalon.Observability.Runtime;

internal sealed class TelemetryExportRuntimeContributor(ObservabilityOptions options) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return TelemetryExportRuntimeSurfaceFactory.CreateSurface(
            surfaceId: "telemetry-export-guidance",
            displayName: "Telemetry Export Guidance",
            description: "Projects the shared Engine:Observability:Telemetry contract without exposing exporter endpoints or secrets.",
            entryId: "telemetry-export-guidance",
            entryDisplayName: "Shared Telemetry Export Guidance",
            entryDescription: "Cephalon.Observability is active and publishes the host telemetry guidance used by exporter companion packages.",
            telemetry: options.Telemetry,
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pack"] = "Cephalon.Observability",
                ["integrationKind"] = "shared-telemetry-guidance"
            });
    }
}
