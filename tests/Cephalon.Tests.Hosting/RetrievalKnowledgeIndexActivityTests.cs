using System.Diagnostics;
using Cephalon.Abstractions.Retrieval;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Diagnostics.Redaction;
using Cephalon.Diagnostics.Redaction.Extensions;
using Cephalon.Engine.Configuration;
using Cephalon.Retrieval.Registration;
using Cephalon.Retrieval.Services;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

/// <summary>
/// Hosting-level proof that <see cref="KnowledgeIndexer"/> and <see cref="KnowledgeQueryEngine"/>
/// emit <c>retrieval.knowledge.index</c> and <c>retrieval.knowledge.query</c> activities under
/// <see cref="RetrievalDiagnostics.ActivitySourceName"/> and route the indexer / query-engine /
/// collection / run / actor / correlation / outcome tag values through the consumer-registered
/// <see cref="RedactionPipeline"/>. Mirrors the Cephalon.Agentics tool-dispatch redaction proof.
/// </summary>
public sealed class RetrievalKnowledgeIndexActivityTests
{
    [Fact]
    public async Task KnowledgeIndexer_RoutesEmittedActivityTagValues_ThroughRedactionPipeline()
    {
        const string runId = "retrieval-index-redaction-tracking-001";
        var observed = new List<(string AttributeKey, object? Value)>();
        // The tracking filter is registered into this test's DI container only; xUnit's parallel
        // sibling tests build their own containers and resolve their own pipeline, so the filter
        // only sees attribute values emitted by this test's in-process indexer.
        var trackingFilter = new TrackingRedactionFilter(observed);

        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == RetrievalDiagnostics.ActivitySourceName,
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
                technologies: ["KnowledgeRetrieval"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddRetrieval();
        });
        builder.Services.AddSingleton<IRedactionFilter>(trackingFilter);
        builder.Services.AddRedactionPipeline();

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var indexer = scope.ServiceProvider.GetRequiredService<IKnowledgeIndexer>();
            await indexer.IndexAsync(new KnowledgeIndexingRequest(
                collectionId: "runbooks",
                runId: runId,
                actorId: "operator-redaction-001",
                correlationId: "corr-retrieval-index-redaction-001"));
        }

        var keys = observed.Select(o => o.AttributeKey).ToHashSet();

        Assert.Contains(RetrievalDiagnostics.IndexerIdTag, keys);
        Assert.Contains(RetrievalDiagnostics.CollectionIdTag, keys);
        Assert.Contains(RetrievalDiagnostics.RunIdTag, keys);
        Assert.Contains(RetrievalDiagnostics.ActorIdTag, keys);
        Assert.Contains(RetrievalDiagnostics.CorrelationIdTag, keys);
        Assert.Contains(RetrievalDiagnostics.IndexingOutcomeTag, keys);
        Assert.Contains(RetrievalDiagnostics.DocumentCountTag, keys);
        Assert.Contains(RetrievalDiagnostics.ProviderCountTag, keys);

        var runIdEntry = observed.First(o => o.AttributeKey == RetrievalDiagnostics.RunIdTag);
        Assert.Equal(runId, runIdEntry.Value);

        var collectionIdEntry = observed.First(o => o.AttributeKey == RetrievalDiagnostics.CollectionIdTag);
        Assert.Equal("runbooks", collectionIdEntry.Value);

        var actorEntry = observed.First(o => o.AttributeKey == RetrievalDiagnostics.ActorIdTag);
        Assert.Equal("operator-redaction-001", actorEntry.Value);
    }

    [Fact]
    public async Task KnowledgeIndexer_AppliesRedactionReplacement_BeforeTaggingActivity()
    {
        const string runId = "retrieval-index-redaction-replacement-001";
        const string redactedRunId = "[REDACTED-INDEX-RUN]";
        Activity? capturedIndexActivity = null;
        // Match by both operation name AND the redacted run-id tag value because xUnit runs tests
        // in parallel and the global ActivitySource listener would otherwise see indexing
        // activities from concurrent sibling tests publishing under the same canonical source name.
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == RetrievalDiagnostics.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName != RetrievalDiagnostics.KnowledgeIndexActivityName)
                {
                    return;
                }

                var runIdTag = activity.Tags.FirstOrDefault(t => t.Key == RetrievalDiagnostics.RunIdTag);
                if (string.Equals(runIdTag.Value, redactedRunId, StringComparison.Ordinal))
                {
                    capturedIndexActivity = activity;
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
                technologies: ["KnowledgeRetrieval"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddRetrieval();
        });
        builder.Services.AddSingleton<IRedactionFilter>(new ReplaceRunIdFilter(redactedRunId));
        builder.Services.AddRedactionPipeline();

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var indexer = scope.ServiceProvider.GetRequiredService<IKnowledgeIndexer>();
            await indexer.IndexAsync(new KnowledgeIndexingRequest(
                collectionId: "runbooks",
                runId: runId,
                actorId: "operator-replace-001",
                correlationId: "corr-retrieval-index-replace-001"));
        }

        Assert.NotNull(capturedIndexActivity);
        var runIdTag = capturedIndexActivity!.Tags.FirstOrDefault(t => t.Key == RetrievalDiagnostics.RunIdTag);
        Assert.Equal(redactedRunId, runIdTag.Value);
    }

    [Fact]
    public async Task KnowledgeQueryEngine_RoutesEmittedActivityTagValues_ThroughRedactionPipeline()
    {
        const string runId = "retrieval-query-redaction-tracking-001";
        var observed = new List<(string AttributeKey, object? Value)>();
        var trackingFilter = new TrackingRedactionFilter(observed);

        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == RetrievalDiagnostics.ActivitySourceName,
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
                technologies: ["KnowledgeRetrieval"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddRetrieval();
        });
        builder.Services.AddSingleton<IRedactionFilter>(trackingFilter);
        builder.Services.AddRedactionPipeline();

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            // Index first so the query lane has documents to score and the query path returns
            // a populated runtime catalog rather than an empty match set.
            var indexer = scope.ServiceProvider.GetRequiredService<IKnowledgeIndexer>();
            await indexer.IndexAsync(new KnowledgeIndexingRequest(
                collectionId: "runbooks",
                runId: "retrieval-query-precondition-index-001"));

            // Reset observation list to capture only query-activity tags.
            observed.Clear();

            var queryEngine = scope.ServiceProvider.GetRequiredService<IKnowledgeQueryEngine>();
            await queryEngine.QueryAsync(new KnowledgeQueryRequest(
                collectionId: "runbooks",
                queryText: "retrieval freshness",
                actorId: "operator-query-redaction-001",
                correlationId: "corr-retrieval-query-redaction-001",
                metadata: new Dictionary<string, string>
                {
                    ["runId"] = runId
                }));
        }

        var keys = observed.Select(o => o.AttributeKey).ToHashSet();

        Assert.Contains(RetrievalDiagnostics.QueryEngineIdTag, keys);
        Assert.Contains(RetrievalDiagnostics.CollectionIdTag, keys);
        Assert.Contains(RetrievalDiagnostics.QueryLengthTag, keys);
        Assert.Contains(RetrievalDiagnostics.QueryLimitTag, keys);
        Assert.Contains(RetrievalDiagnostics.MatchCountTag, keys);
        Assert.Contains(RetrievalDiagnostics.QueryOutcomeTag, keys);
        Assert.Contains(RetrievalDiagnostics.ActorIdTag, keys);
        Assert.Contains(RetrievalDiagnostics.CorrelationIdTag, keys);

        var collectionIdEntry = observed.First(o => o.AttributeKey == RetrievalDiagnostics.CollectionIdTag);
        Assert.Equal("runbooks", collectionIdEntry.Value);

        var queryLengthEntry = observed.First(o => o.AttributeKey == RetrievalDiagnostics.QueryLengthTag);
        Assert.Equal("retrieval freshness".Length, queryLengthEntry.Value);

        var actorEntry = observed.First(o => o.AttributeKey == RetrievalDiagnostics.ActorIdTag);
        Assert.Equal("operator-query-redaction-001", actorEntry.Value);
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
            => context.AttributeKey == RetrievalDiagnostics.RunIdTag ? replacement : value;
    }
}
