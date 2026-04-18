using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Audit.Registration;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Data.Registration;
using Cephalon.Engine.AppModel;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Wolverine.Registration;
using Cephalon.Identity.Registration;
using Cephalon.Ids.Sfid.Registration;
using Cephalon.MultiTenancy.Registration;
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

    public static void ConfigureRestGovernanceEngine(EngineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSettings(CreateAspNetCoreSettings());
        builder.AddModule(new BenchmarkRestGovernanceModule());
        builder.AddModule(new BenchmarkRestGeneratedGroupsModule());
        builder.AddBehaviors(options => options.AutoRegister = false, behaviors =>
        {
            behaviors.AddHttpBehaviorBindings();
        });
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

    public static EngineBuilder CreatePhase8EngineBuilder()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        ConfigurePhase8Engine(builder);
        return builder;
    }

    public static AppProfile CreateAppProfile()
    {
        return AppProfileFactory.Create(CreateSettings());
    }

    public static AppProfile CreatePhase8AppProfile()
    {
        return AppProfileFactory.Create(CreatePhase8Settings());
    }

    public static ScaffoldRequest CreateScaffoldRequest()
    {
        return new ScaffoldRequest(
            appName: "Cephalon.Benchmark.Sample",
            modules: ["Platform", "Discovery", "Operations", "Experience"],
            features: ["Inbox", "Reporting", "Realtime"],
            cephalonPackageVersion: "0.1.0-preview");
    }

    public static ScaffoldRequest CreatePhase8ScaffoldRequest()
    {
        return new ScaffoldRequest(
            appName: "Cephalon.Benchmark.Phase8.Sample",
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

    private static EngineSettings CreatePhase8Settings()
    {
        return new EngineSettings(
            blueprint: "modular-vertical-slice",
            patterns: ["clean-architecture", "ddd", "cqrs", "outbox"],
            transports: ["rest-api", "json-rpc", "grpc"],
            technologies: ["event-driven-integration", "identity-access", "multi-tenancy"],
            data: new DataSettings(
                readWriteSplit: true,
                outboxEnabled: true,
                idGenerator: "Sfid"),
            identity: new IdentitySettings(
                enabled: true,
                authorizationModes: ["RBAC", "Policy"]),
            tenancy: new TenancySettings(
                enabled: true,
                mode: "SharedDatabase"),
            audit: new AuditSettings(enabled: true),
            messaging: new MessagingSettings(provider: "Wolverine"));
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

    private static void ConfigurePhase8Engine(EngineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSettings(CreatePhase8Settings());
        builder.AddData();
        builder.AddSfidIds();
        builder.AddEventing();
        builder.AddWolverineEventing();
        builder.AddIdentityAccess();
        builder.AddMultiTenancy();
        builder.AddAudit();
        builder.AddModule(new BenchmarkExperienceModule());
        builder.AddModule(new BenchmarkOperationsModule());
        builder.AddModule(new BenchmarkDiscoveryModule());
        builder.AddModule(new BenchmarkClockModule());
    }
}
