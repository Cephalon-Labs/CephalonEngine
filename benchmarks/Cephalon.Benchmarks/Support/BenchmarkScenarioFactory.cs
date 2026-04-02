using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Engine.AppModel;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Scaffolding.Generation;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.Support;

internal static class BenchmarkScenarioFactory
{
    public static void ConfigureEngine(EngineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSettings(CreateSettings());
        builder.AddModule(new BenchmarkExperienceModule());
        builder.AddModule(new BenchmarkOperationsModule());
        builder.AddModule(new BenchmarkDiscoveryModule());
        builder.AddModule(new BenchmarkClockModule());
    }

    public static void ConfigureAspNetCoreEngine(EngineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSettings(CreateAspNetCoreSettings());
        builder.AddModule(new BenchmarkExperienceModule());
        builder.AddModule(new BenchmarkOperationsModule());
        builder.AddModule(new BenchmarkDiscoveryModule());
        builder.AddModule(new BenchmarkClockModule());
    }

    public static EngineBuilder CreateEngineBuilder()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        ConfigureEngine(builder);
        return builder;
    }

    public static EngineBuilder CreateStrictTrustEngineBuilder()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        ConfigureStrictTrustEngine(builder);
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

    private static EngineSettings CreateAspNetCoreSettings()
    {
        return new EngineSettings(
            blueprint: "ModularVerticalSlice",
            patterns: ["StrategyPattern", "PipelinePattern", "MediatorPattern"],
            transports: ["RestApi"]);
    }

    private static void ConfigureStrictTrustEngine(EngineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSettings(new EngineSettings(
            blueprint: "ModularVerticalSlice",
            patterns: ["StrategyPattern", "PipelinePattern", "MediatorPattern"],
            transports: ["RestApi", "JsonRpc", "Grpc"],
            trustPolicy: new TrustPolicy(
                defaultCapabilityAccess: CapabilityAccess.TrustedOnly,
                trustedAssemblies: ["Cephalon.Engine"],
                capabilities: new Dictionary<string, CapabilityAccess>(StringComparer.OrdinalIgnoreCase)
                {
                    ["platform.clock"] = CapabilityAccess.Allowed,
                    ["discovery.greetings"] = CapabilityAccess.Allowed,
                    ["experience.portal"] = CapabilityAccess.Denied
                })));
        builder.AddModule(new BenchmarkExperienceModule());
        builder.AddModule(new BenchmarkOperationsModule());
        builder.AddModule(new BenchmarkDiscoveryModule());
        builder.AddModule(new BenchmarkClockModule());
    }
}
