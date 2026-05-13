using Cephalon.Abstractions.Technologies;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingBenchmarkProofRuntimeSurfaceContributor(
    IEventingBenchmarkProofCatalog benchmarkProofs) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "eventing-benchmark-proofs",
            displayName: "Eventing Benchmark Proofs",
            description: "Runtime-readable benchmark guardrail contracts for native Cephalon eventing claims.",
            entries: benchmarkProofs.Guardrails
                .Select(CreateEntry)
                .ToArray());
    }

    private static TechnologyRuntimeEntry CreateEntry(EventingBenchmarkProofDescriptor descriptor)
    {
        return new TechnologyRuntimeEntry(
            id: descriptor.Id,
            displayName: descriptor.Benchmark,
            description: "Benchmark guardrail proof for a native Eventing hot-path claim.",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["guardrailCatalog"] = EventingBenchmarkProofCatalog.GuardrailCatalogReference,
                ["guardrailFamily"] = descriptor.Family,
                ["benchmarkReport"] = descriptor.ReportFileName,
                ["benchmark"] = descriptor.Benchmark,
                ["hotPath"] = descriptor.HotPath,
                ["maxMeanNanoseconds"] = descriptor.MaxMeanNanoseconds.ToString(CultureInfo.InvariantCulture),
                ["maxAllocatedBytes"] = descriptor.MaxAllocatedBytes.ToString(CultureInfo.InvariantCulture),
                ["benchmarkProofMaturity"] = "guardrail-catalog-mapped",
                ["providerNeutral"] = "true",
                ["wolverineRequired"] = "false"
            });
    }
}
