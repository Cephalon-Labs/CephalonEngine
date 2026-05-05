using System.Diagnostics;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Diagnostics;
using Cephalon.Diagnostics.Redaction;
using Cephalon.Diagnostics.Redaction.Extensions;
using Cephalon.Engine.Configuration;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

/// <summary>
/// Hosting-level proof that <see cref="TenantInvitationDeliveryDispatcher"/> emits a
/// <c>multitenancy.governance.invitation.delivery.dispatch</c> activity under
/// <see cref="CephalonActivitySources.MultiTenancyGovernance"/> and routes the tenant /
/// invitation / channel / sender / outcome tag values through the consumer-registered
/// <see cref="RedactionPipeline"/>. Mirrors the existing eventing in-process publisher and
/// AspNetCore middleware redaction proofs.
/// </summary>
public sealed class MultiTenancyGovernanceInvitationDispatchActivityTests
{
    [Fact]
    public async Task Dispatcher_RoutesEmittedActivityTagValues_ThroughRedactionPipeline()
    {
        const string tenantId = "tenant-redaction-tracking-001";
        const string invitationId = "invite-redaction-tracking-001";
        const string senderId = "tracking-sender";
        var observed = new List<(string AttributeKey, object? Value)>();
        // The tracking filter is registered into this test's DI container only; xUnit's parallel
        // sibling tests build their own containers and resolve their own pipeline, so the filter
        // only sees attribute values emitted by this test's dispatcher.
        var trackingFilter = new TrackingRedactionFilter(observed);

        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == GovernanceDiagnostics.ActivitySourceName,
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
                technologies: ["MultiTenancy"]));
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: invitationId,
                    tenantId: tenantId,
                    inviteeId: "user-001",
                    roles: ["member"],
                    sourceModuleId: "hosting-test"));
            });
        });
        builder.Services.AddSingleton<ITenantInvitationDeliverySender>(
            new RecordingDeliverySender(senderId));
        builder.Services.AddSingleton<IRedactionFilter>(trackingFilter);
        builder.Services.AddRedactionPipeline();

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var dispatcher = scope.ServiceProvider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
            await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
                tenantId: tenantId,
                invitationId: invitationId,
                channel: "email",
                senderId: senderId,
                source: "hosting-test",
                actor: "operator-001",
                atUtc: new DateTimeOffset(2026, 05, 03, 9, 0, 0, TimeSpan.Zero),
                correlationId: "corr-redaction-tracking-001"));
        }

        var keys = observed.Select(o => o.AttributeKey).ToHashSet();

        Assert.Contains(CephalonDiagnosticsAttributeKeys.TenantId, keys);
        Assert.Contains(GovernanceDiagnostics.InvitationIdTag, keys);
        Assert.Contains(GovernanceDiagnostics.DeliveryChannelTag, keys);
        Assert.Contains(GovernanceDiagnostics.DeliverySenderIdTag, keys);
        Assert.Contains(GovernanceDiagnostics.DeliveryOutcomeTag, keys);

        var tenantEntry = observed.First(o => o.AttributeKey == CephalonDiagnosticsAttributeKeys.TenantId);
        Assert.Equal(tenantId, tenantEntry.Value);

        var invitationEntry = observed.First(o => o.AttributeKey == GovernanceDiagnostics.InvitationIdTag);
        Assert.Equal(invitationId, invitationEntry.Value);

        var channelEntry = observed.First(o => o.AttributeKey == GovernanceDiagnostics.DeliveryChannelTag);
        Assert.Equal("email", channelEntry.Value);

        var senderEntry = observed.First(o => o.AttributeKey == GovernanceDiagnostics.DeliverySenderIdTag);
        Assert.Equal(senderId, senderEntry.Value);

        var outcomeEntry = observed.First(o => o.AttributeKey == GovernanceDiagnostics.DeliveryOutcomeTag);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, outcomeEntry.Value);
    }

    [Fact]
    public async Task Dispatcher_AppliesRedactionReplacement_BeforeTaggingActivity()
    {
        const string tenantId = "tenant-redaction-replacement-001";
        const string invitationId = "invite-redaction-replacement-001";
        const string redactedInvitationId = "[REDACTED-INVITATION]";
        const string senderId = "replacement-sender";
        Activity? capturedDispatchActivity = null;
        // Match by both operation name AND the redacted invitation-id tag value because xUnit
        // runs tests in parallel and the global ActivitySource listener would otherwise see
        // dispatch activities from concurrent sibling tests publishing under the same canonical
        // source name.
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == GovernanceDiagnostics.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName != GovernanceDiagnostics.InvitationDispatchActivityName)
                {
                    return;
                }

                var invitationIdTag = activity.Tags.FirstOrDefault(t => t.Key == GovernanceDiagnostics.InvitationIdTag);
                if (string.Equals(invitationIdTag.Value, redactedInvitationId, StringComparison.Ordinal))
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
                technologies: ["MultiTenancy"]));
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: invitationId,
                    tenantId: tenantId,
                    inviteeId: "user-002",
                    roles: ["member"],
                    sourceModuleId: "hosting-test"));
            });
        });
        builder.Services.AddSingleton<ITenantInvitationDeliverySender>(
            new RecordingDeliverySender(senderId));
        builder.Services.AddSingleton<IRedactionFilter>(
            new ReplaceInvitationIdFilter(redactedInvitationId));
        builder.Services.AddRedactionPipeline();

        await using var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var dispatcher = scope.ServiceProvider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
            await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
                tenantId: tenantId,
                invitationId: invitationId,
                channel: "email",
                senderId: senderId,
                source: "hosting-test",
                actor: "operator-002",
                atUtc: new DateTimeOffset(2026, 05, 03, 10, 0, 0, TimeSpan.Zero),
                correlationId: "corr-redaction-replace-001"));
        }

        Assert.NotNull(capturedDispatchActivity);
        var invitationIdTag = capturedDispatchActivity!.Tags.FirstOrDefault(
            t => t.Key == GovernanceDiagnostics.InvitationIdTag);
        Assert.Equal(redactedInvitationId, invitationIdTag.Value);
    }

    private sealed class TrackingRedactionFilter(List<(string, object?)> sink) : IRedactionFilter
    {
        public object? Filter(RedactionContext context, object? value)
        {
            sink.Add((context.AttributeKey, value));
            return value;
        }
    }

    private sealed class ReplaceInvitationIdFilter(string replacement) : IRedactionFilter
    {
        public object? Filter(RedactionContext context, object? value)
            => context.AttributeKey == GovernanceDiagnostics.InvitationIdTag ? replacement : value;
    }

    private sealed class RecordingDeliverySender(string senderId) : ITenantInvitationDeliverySender
    {
        public string SenderId { get; } = senderId;

        public ValueTask<TenantInvitationDeliverySenderResult> SendAsync(
            TenantInvitationDeliveryContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(new TenantInvitationDeliverySenderResult(
                TenantInvitationDeliveryOutcomes.Dispatched,
                dispatched: true,
                providerMessageId: "provider-message-001",
                reason: "Test sender accepted the invitation delivery dispatch.",
                dispatchedAtUtc: context.DispatchedAtUtc.AddSeconds(1)));
        }
    }
}
