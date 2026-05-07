using Cephalon.Abstractions.Technologies;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.Runtime;

namespace Cephalon.Observability.OpenTelemetry.Hosting;

internal sealed class OpenTelemetryTelemetryRuntimeContributor(TelemetryExportOptions telemetry) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return TelemetryExportRuntimeSurfaceFactory.CreateSurface(
            surfaceId: "telemetry-export-opentelemetry",
            displayName: "OpenTelemetry Telemetry Export",
            description: "Projects sanitized OpenTelemetry exporter wiring for the active host.",
            entryId: "opentelemetry",
            entryDisplayName: "OpenTelemetry Telemetry Export",
            entryDescription: "Cephalon.Observability.OpenTelemetry is active for host telemetry export.",
            telemetry: telemetry,
            metadata: CreateMetadata("Cephalon.Observability.OpenTelemetry", "otlp-exporter"));
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
