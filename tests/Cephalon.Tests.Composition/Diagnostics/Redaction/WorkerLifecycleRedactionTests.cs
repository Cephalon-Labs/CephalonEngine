using System.Diagnostics;
using Cephalon.Diagnostics;
using Cephalon.Diagnostics.Redaction;
using Cephalon.Diagnostics.Redaction.Extensions;
using Cephalon.Engine.Configuration;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Diagnostics.Redaction;

/// <summary>
/// Composition-level proof that <c>Cephalon.Worker</c>'s <c>RuntimeHostedService</c> emits
/// <c>worker.lifecycle.start</c> and <c>worker.lifecycle.stop</c> activities under
/// <see cref="CephalonActivitySources.Worker"/> and routes the
/// <c>cephalon.lifecycle.phase</c> / <c>cephalon.blueprint</c> / <c>cephalon.module.count</c>
/// tag values through the consumer-registered <see cref="RedactionPipeline"/>. Mirrors
/// the existing engine-runtime / Agentics / Retrieval emission redaction proofs as the
/// sixth M1 redaction emission site shipped through ENG-412.
/// </summary>
public sealed class WorkerLifecycleRedactionTests
{
    private const string WorkerRuntimeHostedServiceTypeName = "Cephalon.Worker.Hosting.RuntimeHostedService";
    private const string LifecycleStartActivityName = "worker.lifecycle.start";
    private const string LifecycleStopActivityName = "worker.lifecycle.stop";
    private const string LifecyclePhaseTag = "cephalon.lifecycle.phase";
    private const string BlueprintTag = "cephalon.blueprint";
    private const string ModuleCountTag = "cephalon.module.count";

    [Fact]
    public async Task WorkerLifecycle_RoutesEmittedStartAndStopTagValues_ThroughRedactionPipeline()
    {
        // The tracking filter is registered into this test's DI container only; xUnit's parallel
        // sibling tests build their own containers and resolve their own pipeline, so the filter
        // only sees attribute values emitted by this test's in-process worker host.
        var observed = new List<(string AttributeKey, object? Value)>();
        var trackingFilter = new TrackingRedactionFilter(observed);

        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == CephalonActivitySources.Worker,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddCephalonWorker(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                patterns: ["CQRS"],
                technologies: [],
                transports: ["RestApi"]));
        });
        services.AddSingleton<IRedactionFilter>(trackingFilter);
        services.AddRedactionPipeline();

        await using var provider = services.BuildServiceProvider();
        var hosted = ResolveWorkerHostedService(provider);

        await hosted.StartAsync(CancellationToken.None);
        await hosted.StopAsync(CancellationToken.None);

        var keys = observed.Select(o => o.AttributeKey).ToHashSet();

        Assert.Contains(LifecyclePhaseTag, keys);
        Assert.Contains(BlueprintTag, keys);
        Assert.Contains(ModuleCountTag, keys);

        // Both lifecycle phases reach the pipeline: start and stop.
        var phaseValues = observed
            .Where(o => o.AttributeKey == LifecyclePhaseTag)
            .Select(o => o.Value as string)
            .ToList();
        Assert.Contains("start", phaseValues);
        Assert.Contains("stop", phaseValues);

        // Blueprint flows through the pipeline with the canonical kebab-case id before activity
        // tagging — the engine normalizes "ModularMonolith" → "modular-monolith" during settings
        // resolution, and the worker hosted service tags the activity with the normalized id.
        var blueprintValues = observed
            .Where(o => o.AttributeKey == BlueprintTag)
            .Select(o => o.Value as string)
            .ToList();
        Assert.Contains("modular-monolith", blueprintValues);
    }

    [Fact]
    public async Task WorkerLifecycle_AppliesRedactionReplacement_BeforeTaggingActivity()
    {
        const string redactedBlueprint = "[REDACTED-BLUEPRINT]";
        var capturedActivities = new List<Activity>();

        // Match by both operation name AND the redacted blueprint tag value because xUnit runs
        // tests in parallel and the global ActivitySource listener would otherwise see lifecycle
        // activities from concurrent sibling tests publishing under the same canonical source name.
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == CephalonActivitySources.Worker,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName != LifecycleStartActivityName
                    && activity.OperationName != LifecycleStopActivityName)
                {
                    return;
                }

                var blueprintTag = activity.Tags.FirstOrDefault(t => t.Key == BlueprintTag);
                if (string.Equals(blueprintTag.Value, redactedBlueprint, StringComparison.Ordinal))
                {
                    capturedActivities.Add(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddCephalonWorker(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                patterns: ["CQRS"],
                technologies: [],
                transports: ["RestApi"]));
        });
        services.AddSingleton<IRedactionFilter>(new ReplaceBlueprintFilter(redactedBlueprint));
        services.AddRedactionPipeline();

        await using var provider = services.BuildServiceProvider();
        var hosted = ResolveWorkerHostedService(provider);

        await hosted.StartAsync(CancellationToken.None);
        await hosted.StopAsync(CancellationToken.None);

        // Both start and stop activities should carry the redacted blueprint tag value.
        Assert.Contains(capturedActivities, a => a.OperationName == LifecycleStartActivityName);
        Assert.Contains(capturedActivities, a => a.OperationName == LifecycleStopActivityName);
    }

    private static IHostedService ResolveWorkerHostedService(IServiceProvider provider)
    {
        // RuntimeHostedService is internal to Cephalon.Worker, so filter by full type name rather
        // than referencing the type symbol directly.
        return provider.GetServices<IHostedService>()
            .Single(h => string.Equals(
                h.GetType().FullName,
                WorkerRuntimeHostedServiceTypeName,
                StringComparison.Ordinal));
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

    private sealed class ReplaceBlueprintFilter : IRedactionFilter
    {
        private readonly string replacement;

        public ReplaceBlueprintFilter(string replacement) => this.replacement = replacement;

        public object? Filter(RedactionContext context, object? value)
            => context.AttributeKey == BlueprintTag ? replacement : value;
    }
}
