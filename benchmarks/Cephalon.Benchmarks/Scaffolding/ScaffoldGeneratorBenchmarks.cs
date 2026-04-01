using BenchmarkDotNet.Attributes;
using Cephalon.Benchmarks.Support;
using Cephalon.Abstractions.AppModel;
using Cephalon.Scaffolding.Generation;

namespace Cephalon.Benchmarks.Scaffolding;

[MemoryDiagnoser]
[ShortRunJob]
public class ScaffoldGeneratorBenchmarks
{
    private readonly AppProfile appProfile = BenchmarkScenarioFactory.CreateAppProfile();
    private readonly ScaffoldRequest request = BenchmarkScenarioFactory.CreateScaffoldRequest();

    [Benchmark]
    public int GenerateBlueprintScaffold()
    {
        var scaffold = ScaffoldGenerator.Generate(appProfile, request);

        return scaffold.Files.Count;
    }
}
