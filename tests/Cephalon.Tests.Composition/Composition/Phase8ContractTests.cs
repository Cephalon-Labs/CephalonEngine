using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Ids;
using Cephalon.Abstractions.Tenancy;

namespace Cephalon.Tests.Composition;

public sealed class Phase8ContractTests
{
    [Fact]
    public void ProjectionDescriptorNormalizesContractsTagsAndMetadata()
    {
        var descriptor = new ProjectionDescriptor(
            id: "orders-read-model",
            displayName: "Orders Read Model",
            description: "Projects order events into the read store.",
            sourceModuleId: "orders",
            targetStoreId: "orders-read",
            sourceContracts: [" order-created ", "ORDER-CREATED", "order-shipped"],
            tags: [" read-model ", "READ-MODEL", "orders"],
            metadata: new Dictionary<string, string?>
            {
                ["Store"] = "postgres",
                ["LagBudget"] = "30s"
            }!.ToDictionary(pair => pair.Key, pair => pair.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase));

        Assert.Equal(["order-created", "order-shipped"], descriptor.SourceContracts);
        Assert.Equal(["orders", "read-model"], descriptor.Tags);
        Assert.Equal("postgres", descriptor.Metadata["store"]);
        Assert.Equal("30s", descriptor.Metadata["lagbudget"]);
    }

    [Fact]
    public void OutboxDescriptorNormalizesChannelsTagsAndMetadata()
    {
        var descriptor = new OutboxDescriptor(
            id: "tenant-event-outbox",
            displayName: "Tenant Event Outbox",
            description: "Stages tenant lifecycle events for durable delivery.",
            sourceModuleId: "tenant-lifecycle",
            provider: "relational",
            mode: "transactional-store",
            channelIds: [" tenant-events ", "TENANT-EVENTS", "audit"],
            tags: [" outbox ", "OUTBOX", "tenant"],
            metadata: new Dictionary<string, string?>
            {
                ["DispatchRuntime"] = "not-configured",
                ["Consistency"] = "durable"
            }!.ToDictionary(pair => pair.Key, pair => pair.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase));

        Assert.Equal(["audit", "tenant-events"], descriptor.ChannelIds);
        Assert.Equal(["outbox", "tenant"], descriptor.Tags);
        Assert.Equal("not-configured", descriptor.Metadata["dispatchruntime"]);
        Assert.Equal("durable", descriptor.Metadata["consistency"]);
        Assert.Equal("disabled", descriptor.DispatchPolicy.PolicyId);
        Assert.Equal("disabled", descriptor.DispatchPolicy.ExecutionMode);
        Assert.Equal("tenant-event-outbox", descriptor.DispatchPolicy.OutboxId);
    }

    [Fact]
    public void EventDispatchRuntimeDescriptorNormalizesOwnedOutboxIdsAndMetadata()
    {
        var summary = new EventDispatchRuntimeSummary(
            reportedOutboxIds: [" entity-framework-outbox ", "ENTITY-FRAMEWORK-OUTBOX", "catalog-outbox"],
            lastOutboxId: " entity-framework-outbox ",
            lastChannelId: " catalog-events ",
            lastOutcome: " retry-scheduled ",
            lastObservedAtUtc: new DateTimeOffset(2026, 04, 11, 11, 15, 00, TimeSpan.Zero),
            lastMessageId: " evt-700 ",
            lastAttempt: 3,
            startedCount: 2,
            succeededCount: 1,
            failedCount: 1,
            retryScheduledCount: 1,
            skippedCount: 0,
            retryPendingCount: 1,
            lastError: " Retrying staged dispatch. ");
        var descriptor = new EventDispatchRuntimeDescriptor(
            id: "wolverine-dispatch-loop",
            displayName: "Wolverine Dispatch Loop",
            description: "Manages staged event handoff through Wolverine.",
            metadata: new Dictionary<string, string?>
            {
                ["Adapter"] = "wolverine",
                ["DispatchBridge"] = "wolverine-managed"
            }!.ToDictionary(pair => pair.Key, pair => pair.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase),
            outboxIds: [" entity-framework-outbox ", "ENTITY-FRAMEWORK-OUTBOX", "catalog-outbox"],
            summary: summary);

        Assert.Equal(["catalog-outbox", "entity-framework-outbox"], descriptor.OutboxIds);
        Assert.Equal("wolverine", descriptor.Metadata["adapter"]);
        Assert.Equal("wolverine-managed", descriptor.Metadata["dispatchbridge"]);
        Assert.Equal(["catalog-outbox", "entity-framework-outbox"], descriptor.Summary.ReportedOutboxIds);
        Assert.Equal("entity-framework-outbox", descriptor.Summary.LastOutboxId);
        Assert.Equal("catalog-events", descriptor.Summary.LastChannelId);
        Assert.Equal("retry-scheduled", descriptor.Summary.LastOutcome);
        Assert.Equal("evt-700", descriptor.Summary.LastMessageId);
        Assert.Equal(3, descriptor.Summary.LastAttempt);
        Assert.Equal(5, descriptor.Summary.TotalReports);
        Assert.Equal(1, descriptor.Summary.RetryPendingCount);
    }

    [Fact]
    public void InboxDescriptorNormalizesChannelsTagsAndMetadata()
    {
        var descriptor = new InboxDescriptor(
            id: "tenant-event-inbox",
            displayName: "Tenant Event Inbox",
            description: "Tracks processed tenant lifecycle events.",
            sourceModuleId: "tenant-lifecycle",
            provider: "relational",
            mode: "processed-message-store",
            channelIds: [" tenant-events ", "TENANT-EVENTS", "audit"],
            tags: [" inbox ", "INBOX", "tenant"],
            metadata: new Dictionary<string, string?>
            {
                ["Idempotency"] = "message-id",
                ["DispatchRuntime"] = "not-configured"
            }!.ToDictionary(pair => pair.Key, pair => pair.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase));

        Assert.Equal(["audit", "tenant-events"], descriptor.ChannelIds);
        Assert.Equal(["inbox", "tenant"], descriptor.Tags);
        Assert.Equal("message-id", descriptor.Metadata["idempotency"]);
        Assert.Equal("not-configured", descriptor.Metadata["dispatchruntime"]);
    }

    [Fact]
    public void AuditStoreDescriptorNormalizesTagsAndMetadata()
    {
        var descriptor = new AuditStoreDescriptor(
            id: "tenant-audit-store",
            displayName: "Tenant Audit Store",
            description: "Captures tenant audit entries.",
            sourceModuleId: "tenant-lifecycle",
            provider: "memory",
            mode: "volatile-buffer",
            tags: [" audit ", "AUDIT", "tenant"],
            metadata: new Dictionary<string, string?>
            {
                ["WriteMode"] = "application-managed",
                ["TenantAware"] = "true"
            }!.ToDictionary(pair => pair.Key, pair => pair.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase));

        Assert.Equal(["audit", "tenant"], descriptor.Tags);
        Assert.Equal("application-managed", descriptor.Metadata["writemode"]);
        Assert.Equal("true", descriptor.Metadata["tenantaware"]);
    }

    [Fact]
    public void AuthorizationDecisionHelpersPreserveModesAndMetadata()
    {
        var decision = AuthorizationDecision.Allow(
            policyId: "tenant-admin",
            reason: "Tenant administrators can manage this resource.",
            modes: [AuthorizationMode.Policy, AuthorizationMode.Rbac, AuthorizationMode.Policy],
            metadata: new Dictionary<string, string>
            {
                ["Evaluator"] = "default"
            });

        Assert.True(decision.IsAllowed);
        Assert.Equal("tenant-admin", decision.PolicyId);
        Assert.Equal([AuthorizationMode.Rbac, AuthorizationMode.Policy], decision.Modes);
        Assert.Equal("default", decision.Metadata["evaluator"]);
    }

    [Fact]
    public void TenantResolutionResultReportsResolvedState()
    {
        var unresolved = new TenantResolutionResult(
            tenant: null,
            source: "host",
            reason: "No tenant mapping matched the current host name.");
        var resolved = new TenantResolutionResult(
            tenant: new TenantContext(
                tenantId: "tenant-001",
                tenantKey: "acme",
                displayName: "Acme"),
            source: "subdomain");

        Assert.False(unresolved.IsResolved);
        Assert.Equal("host", unresolved.Source);
        Assert.True(resolved.IsResolved);
        Assert.Equal("tenant-001", resolved.Tenant?.TenantId);
    }

    [Fact]
    public void AuditEntryNormalizesChangesTagsAndMetadata()
    {
        var entry = new AuditEntry(
            id: "audit-001",
            category: "identity",
            action: "invite-approved",
            summary: "Approved a tenant invitation.",
            subjectType: "tenant-membership",
            subjectId: "membership-001",
            occurredAtUtc: new DateTimeOffset(2026, 4, 4, 12, 0, 0, TimeSpan.Zero),
            actor: new AuditActor(
                actorId: "user-001",
                displayName: "alice",
                actorType: "user",
                attributes: new Dictionary<string, string>
                {
                    ["Region"] = "apac"
                }),
            outcome: AuditOutcome.Succeeded,
            tenantId: "tenant-001",
            correlationId: "corr-001",
            changes:
            [
                new AuditChange("status", "pending", "approved"),
                new AuditChange("approved_by", null, "user-001")
            ],
            tags: [" membership ", "MEMBERSHIP", "identity"],
            metadata: new Dictionary<string, string>
            {
                ["Origin"] = "backoffice"
            });

        Assert.Equal(["approved_by", "status"], entry.Changes.Select(change => change.FieldName).ToArray());
        Assert.Equal(["identity", "membership"], entry.Tags);
        Assert.Equal("backoffice", entry.Metadata["origin"]);
        Assert.Equal("apac", entry.Actor.Attributes["region"]);
    }

    [Fact]
    public void IdGenerationRequestReportsWhenHintsArePresent()
    {
        var empty = new IdGenerationRequest();
        var request = new IdGenerationRequest(
            kind: "tenant",
            scope: "backoffice",
            tenantId: "tenant-001",
            attributes: new Dictionary<string, string>
            {
                ["Source"] = "ui"
            });

        Assert.False(empty.HasValues);
        Assert.True(request.HasValues);
        Assert.Equal("ui", request.Attributes["source"]);
    }
}
