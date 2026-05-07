using Cephalon.Abstractions.Technologies;

namespace Cephalon.Observability.Serilog.Hosting;

internal sealed class SerilogLoggingRuntimeContributor : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "observability",
            surfaceId: "logging-provider-serilog",
            displayName: "Serilog Logging Provider",
            description: "Projects that Cephalon.Observability.Serilog is active for the host logging pipeline.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "serilog",
                    displayName: "Serilog Logging Provider",
                    description: "Serilog is registered as an ILogger provider through the Cephalon companion pack.",
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["pack"] = "Cephalon.Observability.Serilog",
                        ["integrationKind"] = "logging-provider",
                        ["configurationSource"] = "serilog-section-or-code",
                        ["secretProjection"] = "redacted"
                    })
            ]);
    }
}
