using Cephalon.Abstractions.Technologies;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.Runtime;

namespace Cephalon.Observability.DigitalOcean.Hosting;

internal sealed class DigitalOceanTelemetryRuntimeContributor(TelemetryExportOptions telemetry) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return TelemetryExportRuntimeSurfaceFactory.CreateSurface(
            surfaceId: "telemetry-export-digitalocean",
            displayName: "DigitalOcean Telemetry Export",
            description: "Projects sanitized DigitalOcean observability exporter wiring for the active host.",
            entryId: "digitalocean",
            entryDisplayName: "DigitalOcean Telemetry Export",
            entryDescription: "Cephalon.Observability.DigitalOcean is active for host telemetry export.",
            telemetry: telemetry,
            metadata: CreateMetadata("Cephalon.Observability.DigitalOcean", "cloud-observability"));
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
