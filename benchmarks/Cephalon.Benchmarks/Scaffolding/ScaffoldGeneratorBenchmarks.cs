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
    private readonly AppProfile phase8AppProfile = BenchmarkScenarioFactory.CreatePhase8AppProfile();
    private readonly ScaffoldRequest request = BenchmarkScenarioFactory.CreateScaffoldRequest();
    private readonly ScaffoldRequest phase8Request = BenchmarkScenarioFactory.CreatePhase8ScaffoldRequest();

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

    /// <summary>
    /// Generates the scaffold plan for the shipped phase-8 starter profile.
    /// </summary>
    /// <returns>
    /// The number of files included in the generated phase-8 scaffold plan.
    /// </returns>
    [Benchmark]
    public int GeneratePhase8BlueprintScaffold()
    {
        var scaffold = ScaffoldGenerator.Generate(phase8AppProfile, phase8Request);

        return scaffold.Files.Count;
    }
}
