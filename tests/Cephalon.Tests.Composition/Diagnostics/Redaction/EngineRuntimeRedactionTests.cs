using System.Diagnostics;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Diagnostics.Redaction;
using Cephalon.Diagnostics.Redaction.Extensions;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Diagnostics.Redaction;

public sealed class EngineRuntimeRedactionTests
{
    [Fact]
    public async Task EngineRuntime_RoutesModulePhaseTagValues_ThroughRedactionPipeline()
    {
        var observed = new List<(string AttributeKey, object? Value)>();
        var trackingFilter = new TrackingRedactionFilter(observed);

        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                patterns: ["CQRS"],
                technologies: [],
                transports: ["RestApi"]));
            engine.AddModule(new RedactionPilotModule());
        });
        services.AddSingleton<IRedactionFilter>(trackingFilter);
        services.AddRedactionPipeline();

        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == "Cephalon.Engine",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        await using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        await runtime.InitializeAsync(provider);

        var keys = observed.Select(o => o.AttributeKey).ToHashSet();

        Assert.Contains("cephalon.phase", keys);
        Assert.Contains("cephalon.blueprint", keys);
        Assert.Contains("cephalon.module.count", keys);
        Assert.Contains("cephalon.module.id", keys);
        Assert.Contains("cephalon.module.version", keys);
    }

    [Fact]
    public async Task EngineRuntime_AppliesRedactionReplacement_BeforeTaggingActivity()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                patterns: ["CQRS"],
                technologies: [],
                transports: ["RestApi"]));
            engine.AddModule(new RedactionPilotModule());
        });
        services.AddSingleton<IRedactionFilter>(new ReplaceModuleIdFilter("[REDACTED-MODULE]"));
        services.AddRedactionPipeline();

        var moduleActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == "Cephalon.Engine",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == "module.initialize")
                {
                    moduleActivities.Add(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        await using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        await runtime.InitializeAsync(provider);

        var moduleActivity = Assert.Single(moduleActivities);
        var moduleIdTag = moduleActivity.Tags.FirstOrDefault(t => t.Key == "cephalon.module.id");
        Assert.Equal("[REDACTED-MODULE]", moduleIdTag.Value);
    }

    private sealed class RedactionPilotModule : ModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "redaction-pilot-tests",
            displayName: "Redaction Pilot Tests",
            description: "Module that exercises engine module-phase activity tag emission for redaction pilot tests.",
            tags: ["redaction", "tests"],
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void RegisterCapabilities(ICapabilityRegistry capabilities)
        {
        }
    }

    private sealed class TrackingRedactionFilter : IRedactionFilter
    {
        private readonly List<(string, object?)> sink;

        public TrackingRedactionFilter(List<(string, object?)> sink) => this.sink = sink;

        public object? Filter(RedactionContext context, object? value)
        {
            sink.Add((context.AttributeKey, value));
            return value;
        }
    }

    private sealed class ReplaceModuleIdFilter : IRedactionFilter
    {
        private readonly string replacement;

        public ReplaceModuleIdFilter(string replacement) => this.replacement = replacement;

        public object? Filter(RedactionContext context, object? value)
            => context.AttributeKey == "cephalon.module.id" ? replacement : value;
    }
}
