using Cephalon.Abstractions.AppModel;
using Cephalon.Engine.AppModel;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Scaffolding.Generation;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.Support;

internal static class BenchmarkScenarioFactory
{
    public static EngineBuilder CreateEngineBuilder()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(CreateSettings());
        builder.AddModule(new BenchmarkExperienceModule());
        builder.AddModule(new BenchmarkOperationsModule());
        builder.AddModule(new BenchmarkDiscoveryModule());
        builder.AddModule(new BenchmarkClockModule());
        return builder;
    }

    public static AppProfile CreateAppProfile()
    {
        return AppProfileFactory.Create(CreateSettings());
    }

    public static ScaffoldRequest CreateScaffoldRequest()
    {
        return new ScaffoldRequest(
            appName: "Cephalon.Benchmark.Sample",
            modules: ["Platform", "Discovery", "Operations", "Experience"],
            features: ["Inbox", "Reporting", "Realtime"],
            cephalonPackageVersion: "0.1.0-preview");
    }

    private static EngineSettings CreateSettings()
    {
        return new EngineSettings(
            blueprint: "ModularVerticalSlice",
            patterns: ["StrategyPattern", "PipelinePattern", "MediatorPattern"],
            transports: ["RestApi", "JsonRpc", "Grpc"]);
    }
}
