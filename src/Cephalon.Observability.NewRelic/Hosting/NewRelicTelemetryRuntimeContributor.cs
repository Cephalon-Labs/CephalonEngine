using Cephalon.Abstractions.Technologies;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.Runtime;

namespace Cephalon.Observability.NewRelic.Hosting;

internal sealed class NewRelicTelemetryRuntimeContributor(TelemetryExportOptions telemetry) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return TelemetryExportRuntimeSurfaceFactory.CreateSurface(
            surfaceId: "telemetry-export-new-relic",
            displayName: "New Relic Telemetry Export",
            description: "Projects sanitized New Relic observability exporter wiring for the active host.",
            entryId: "new-relic",
            entryDisplayName: "New Relic Telemetry Export",
            entryDescription: "Cephalon.Observability.NewRelic is active for host telemetry export.",
            telemetry: telemetry,
            metadata: CreateMetadata("Cephalon.Observability.NewRelic", "observability-saas"));
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
