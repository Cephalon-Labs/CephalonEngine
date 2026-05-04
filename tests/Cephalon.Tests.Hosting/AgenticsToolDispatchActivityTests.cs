using System.Diagnostics;
using Cephalon.Abstractions.Agentics;
using Cephalon.Agentics.Registration;
using Cephalon.Agentics.Services;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Diagnostics.Redaction;
using Cephalon.Diagnostics.Redaction.Extensions;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

/// <summary>
/// Hosting-level proof that <see cref="AgentToolDispatcher"/> emits an
/// <c>agentics.tool.dispatch</c> activity under <see cref="CephalonActivitySources.Agentics"/>
/// and routes the dispatcher / tool / run / actor / correlation / attempt / outcome tag values
/// through the consumer-registered <see cref="RedactionPipeline"/>. Mirrors the existing
/// Cephalon.Eventing publication-dispatch redaction proof.
/// </summary>
public sealed class AgenticsToolDispatchActivityTests
{
    [Fact]
    public async Task AgentToolDispatcher_RoutesEmittedActivityTagValues_ThroughRedactionPipeline()
    {
        const string runId = "agentics-redaction-tracking-001";
        var observed = new List<(string AttributeKey, object? Value)>();
        // The tracking filter is registered into this test's DI container only; xUnit's parallel
        // sibling tests build their own containers and resolve their own pipeline, so the filter
        // only sees attribute values emitted by this test's in-process dispatcher.
        var trackingFilter = new TrackingRedactionFilter(observed);

        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == AgenticsDiagnostics.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["AgenticWorkloads"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddAgentics();
        });
        builder.Services.AddSingleton<IRedactionFilter>(trackingFilter);
        builder.Services.AddRedactionPipeline();

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var dispatcher = scope.ServiceProvider.GetRequiredService<IAgentToolDispatcher>();
            await dispatcher.ExecuteAsync(new AgentToolExecutionRequest(
                toolId: "analyst",
                runId: runId,
                actorId: "operator-redaction-001",
                correlationId: "corr-agentics-redaction-001"));
        }

        var keys = observed.Select(o => o.AttributeKey).ToHashSet();

        Assert.Contains(AgenticsDiagnostics.DispatcherIdTag, keys);
        Assert.Contains(AgenticsDiagnostics.ToolIdTag, keys);
        Assert.Contains(AgenticsDiagnostics.RunIdTag, keys);
        Assert.Contains(AgenticsDiagnostics.ActorIdTag, keys);
        Assert.Contains(AgenticsDiagnostics.CorrelationIdTag, keys);
        Assert.Contains(AgenticsDiagnostics.AttemptTag, keys);
        Assert.Contains(AgenticsDiagnostics.ExecutionOutcomeTag, keys);

        var runIdEntry = observed.First(o => o.AttributeKey == AgenticsDiagnostics.RunIdTag);
        Assert.Equal(runId, runIdEntry.Value);

        var toolIdEntry = observed.First(o => o.AttributeKey == AgenticsDiagnostics.ToolIdTag);
        Assert.Equal("analyst", toolIdEntry.Value);

        var actorEntry = observed.First(o => o.AttributeKey == AgenticsDiagnostics.ActorIdTag);
        Assert.Equal("operator-redaction-001", actorEntry.Value);
    }

    [Fact]
    public async Task AgentToolDispatcher_AppliesRedactionReplacement_BeforeTaggingActivity()
    {
        const string runId = "agentics-redaction-replacement-001";
        const string redactedRunId = "[REDACTED-RUN]";
        Activity? capturedDispatchActivity = null;
        // Match by both operation name AND the redacted run-id tag value because xUnit runs tests
        // in parallel and the global ActivitySource listener would otherwise see dispatch
        // activities from concurrent sibling tests publishing under the same canonical source name.
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == AgenticsDiagnostics.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName != AgenticsDiagnostics.ToolDispatchActivityName)
                {
                    return;
                }

                var runIdTag = activity.Tags.FirstOrDefault(t => t.Key == AgenticsDiagnostics.RunIdTag);
                if (string.Equals(runIdTag.Value, redactedRunId, StringComparison.Ordinal))
                {
                    capturedDispatchActivity = activity;
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["AgenticWorkloads"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddAgentics();
        });
        builder.Services.AddSingleton<IRedactionFilter>(new ReplaceRunIdFilter(redactedRunId));
        builder.Services.AddRedactionPipeline();

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var dispatcher = scope.ServiceProvider.GetRequiredService<IAgentToolDispatcher>();
            await dispatcher.ExecuteAsync(new AgentToolExecutionRequest(
                toolId: "analyst",
                runId: runId,
                actorId: "operator-replace-001",
                correlationId: "corr-agentics-replace-001"));
        }

        Assert.NotNull(capturedDispatchActivity);
        var runIdTag = capturedDispatchActivity!.Tags.FirstOrDefault(t => t.Key == AgenticsDiagnostics.RunIdTag);
        Assert.Equal(redactedRunId, runIdTag.Value);
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

    private sealed class ReplaceRunIdFilter : IRedactionFilter
    {
        private readonly string replacement;

        public ReplaceRunIdFilter(string replacement) => this.replacement = replacement;

        public object? Filter(RedactionContext context, object? value)
            => context.AttributeKey == AgenticsDiagnostics.RunIdTag ? replacement : value;
    }
}
