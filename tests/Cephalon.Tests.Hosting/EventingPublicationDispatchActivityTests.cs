using System.Diagnostics;
using Cephalon.Abstractions.Data;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Diagnostics.Redaction;
using Cephalon.Diagnostics.Redaction.Extensions;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

/// <summary>
/// Hosting-level proof that <see cref="InProcessEventPublisher"/> emits an
/// <c>eventing.publication.dispatch</c> activity under
/// <see cref="CephalonActivitySources.Eventing"/> and routes the publisher / publication / channel
/// / event-type / outcome / matched-subscription-count tag values through the consumer-registered
/// <see cref="RedactionPipeline"/>. Mirrors the existing AspNetCore middleware redaction proof.
/// </summary>
public sealed class EventingPublicationDispatchActivityTests
{
    [Fact]
    public async Task InProcessPublisher_RoutesEmittedActivityTagValues_ThroughRedactionPipeline()
    {
        const string publicationId = "audit-redaction-tracking-001";
        var observed = new List<(string AttributeKey, object? Value)>();
        // The tracking filter is registered into this test's DI container only; xUnit's parallel
        // sibling tests build their own containers and resolve their own pipeline, so the filter
        // only sees attribute values emitted by this test's in-process publisher.
        var trackingFilter = new TrackingRedactionFilter(observed);

        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == EventingDiagnostics.ActivitySourceName,
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
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
            });
        });
        builder.Services.AddSingleton<IRedactionFilter>(trackingFilter);
        builder.Services.AddRedactionPipeline();

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new EventPublication(
                id: publicationId,
                channelId: "audit",
                eventType: "audit.created",
                payload: $$"""{"id":"{{publicationId}}"}""",
                occurredAtUtc: new DateTimeOffset(2026, 04, 29, 10, 0, 0, TimeSpan.Zero),
                contentType: "application/json",
                correlationId: "corr-audit-redaction-001",
                tenantId: "tenant-redaction-001"));
        }

        var keys = observed.Select(o => o.AttributeKey).ToHashSet();

        Assert.Contains(EventingDiagnostics.PublisherIdTag, keys);
        Assert.Contains(EventingDiagnostics.PublicationIdTag, keys);
        Assert.Contains(EventingDiagnostics.ChannelIdTag, keys);
        Assert.Contains(EventingDiagnostics.EventTypeTag, keys);
        Assert.Contains(EventingDiagnostics.PublicationOutcomeTag, keys);
        Assert.Contains(EventingDiagnostics.MatchedSubscriptionCountTag, keys);

        var publicationIdEntry = observed.First(o => o.AttributeKey == EventingDiagnostics.PublicationIdTag);
        Assert.Equal(publicationId, publicationIdEntry.Value);

        var channelIdEntry = observed.First(o => o.AttributeKey == EventingDiagnostics.ChannelIdTag);
        Assert.Equal("audit", channelIdEntry.Value);

        var eventTypeEntry = observed.First(o => o.AttributeKey == EventingDiagnostics.EventTypeTag);
        Assert.Equal("audit.created", eventTypeEntry.Value);
    }

    [Fact]
    public async Task InProcessPublisher_AppliesRedactionReplacement_BeforeTaggingActivity()
    {
        const string publicationId = "audit-redaction-replacement-001";
        const string redactedPublicationId = "[REDACTED-PUBLICATION]";
        Activity? capturedDispatchActivity = null;
        // Match by both operation name AND the redacted publication-id tag value because xUnit
        // runs tests in parallel and the global ActivitySource listener would otherwise see
        // dispatch activities from concurrent sibling tests publishing under the same canonical
        // source name.
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == EventingDiagnostics.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName != EventingDiagnostics.PublicationDispatchActivityName)
                {
                    return;
                }

                var publicationIdTag = activity.Tags.FirstOrDefault(t => t.Key == EventingDiagnostics.PublicationIdTag);
                if (string.Equals(publicationIdTag.Value, redactedPublicationId, StringComparison.Ordinal))
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
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
            });
        });
        builder.Services.AddSingleton<IRedactionFilter>(new ReplacePublicationIdFilter(redactedPublicationId));
        builder.Services.AddRedactionPipeline();

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new EventPublication(
                id: publicationId,
                channelId: "audit",
                eventType: "audit.created",
                payload: $$"""{"id":"{{publicationId}}"}""",
                occurredAtUtc: new DateTimeOffset(2026, 04, 29, 11, 0, 0, TimeSpan.Zero),
                contentType: "application/json",
                correlationId: "corr-audit-replace-001",
                tenantId: "tenant-replace-001"));
        }

        Assert.NotNull(capturedDispatchActivity);
        var publicationIdTag = capturedDispatchActivity!.Tags.FirstOrDefault(t => t.Key == EventingDiagnostics.PublicationIdTag);
        Assert.Equal(redactedPublicationId, publicationIdTag.Value);
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

    private sealed class ReplacePublicationIdFilter : IRedactionFilter
    {
        private readonly string replacement;

        public ReplacePublicationIdFilter(string replacement) => this.replacement = replacement;

        public object? Filter(RedactionContext context, object? value)
            => context.AttributeKey == EventingDiagnostics.PublicationIdTag ? replacement : value;
    }
}
