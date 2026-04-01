using BenchmarkDotNet.Attributes;
using Cephalon.Benchmarks.Support;
using Cephalon.Abstractions.AppModel;
using Cephalon.Scaffolding.Generation;

namespace Cephalon.Benchmarks.Scaffolding;

/// <summary>
/// Measures scaffold generation for the shipped blueprint sample profile.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ScaffoldGeneratorBenchmarks
{
    private readonly AppProfile appProfile = BenchmarkScenarioFactory.CreateAppProfile();
    private readonly ScaffoldRequest request = BenchmarkScenarioFactory.CreateScaffoldRequest();

    /// <summary>
    /// Generates the scaffold plan for the baseline application profile.
    /// </summary>
    /// <returns>
    /// The number of files included in the generated scaffold plan.
    /// </returns>
    [Benchmark]
    public int GenerateBlueprintScaffold()
    {
        var scaffold = ScaffoldGenerator.Generate(appProfile, request);

        return scaffold.Files.Count;
    }
}
