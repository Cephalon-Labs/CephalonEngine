using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;

namespace Cephalon.Tests.Support;

internal sealed class Phase8CatalogModule : ModuleBase, IDataProductContributor, IProjectionContributor, IOutboxContributor, IInboxContributor, IAuthorizationPolicyContributor, IAuditStoreContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "phase8-runtime-catalogs",
        displayName: "Phase 8 Runtime Catalogs",
        description: "Contributes projection and authorization-policy descriptors for phase-8 runtime tests.",
        tags: ["phase8", "catalogs"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "application",
            ["surface"] = "phase8-runtime-catalogs"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterDataProducts(IDataProductRegistry dataProducts)
    {
        dataProducts.Add(new DataProductDescriptor(
            id: "tenant-profile",
            displayName: "Tenant Profile",
            description: "Publishes the tenant profile and membership summary as one queryable data product.",
            sourceModuleId: Descriptor.Id,
            domainId: "tenant-management",
            contractId: "tenant-profile-v1",
            mode: "query",
            tags: ["tenant", "profile"],
            metadata: new Dictionary<string, string>
            {
                ["classification"] = "internal",
                ["freshness"] = "near-real-time"
            }));
    }

    public void RegisterProjections(IProjectionRegistry projections)
    {
        projections.Add(new ProjectionDescriptor(
            id: "tenant-summary",
            displayName: "Tenant Summary",
            description: "Builds a tenant-scoped summary read model for dashboards.",
            sourceModuleId: Descriptor.Id,
            targetStoreId: "tenant-summary-read-model",
            mode: "asynchronous",
            sourceContracts: ["tenant.created", "tenant.member-approved"],
            tags: ["tenant", "summary"],
            metadata: new Dictionary<string, string>
            {
                ["consistency"] = "eventual"
            }));
    }

    public void RegisterPolicies(IAuthorizationPolicyRegistry policies)
    {
        policies.Add(new AuthorizationPolicyDescriptor(
            id: "tenant-admin",
            displayName: "Tenant Administrator",
            description: "Allows tenant administrators to manage tenant-owned resources.",
            modes: [AuthorizationMode.Rbac, AuthorizationMode.Policy],
            tags: ["tenant", "admin"],
            metadata: new Dictionary<string, string>
            {
                ["scope"] = "tenant"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: "tenant-boundary",
            displayName: "Tenant Boundary",
            description: "Prevents cross-tenant access unless the current resource and subject context align.",
            modes: [AuthorizationMode.Abac, AuthorizationMode.Policy],
            tags: ["tenant", "boundary"],
            metadata: new Dictionary<string, string>
            {
                ["scope"] = "tenant"
            }));
    }

    public void RegisterOutboxes(IOutboxRegistry outboxes)
    {
        outboxes.Add(new OutboxDescriptor(
            id: "tenant-event-outbox",
            displayName: "Tenant Event Outbox",
            description: "Stages tenant lifecycle and audit events for durable outbound delivery.",
            sourceModuleId: Descriptor.Id,
            provider: "relational",
            mode: "transactional-store",
            channelIds: ["tenant-events", "audit"],
            tags: ["tenant", "outbox"],
            metadata: new Dictionary<string, string>
            {
                ["dispatchRuntime"] = "not-configured",
                ["consistency"] = "durable"
            }));
    }

    public void RegisterInboxes(IInboxRegistry inboxes)
    {
        inboxes.Add(new InboxDescriptor(
            id: "tenant-event-inbox",
            displayName: "Tenant Event Inbox",
            description: "Tracks processed tenant lifecycle events for idempotent handling.",
            sourceModuleId: Descriptor.Id,
            provider: "relational",
            mode: "processed-message-store",
            channelIds: ["tenant-events"],
            tags: ["tenant", "inbox"],
            metadata: new Dictionary<string, string>
            {
                ["idempotency"] = "message-id",
                ["dispatchRuntime"] = "not-configured"
            }));
    }

    public void RegisterAuditStores(IAuditStoreRegistry auditStores)
    {
        auditStores.Add(new AuditStoreDescriptor(
            id: "tenant-audit-store",
            displayName: "Tenant Audit Store",
            description: "Captures tenant-facing audit entries for phase-8 runtime tests.",
            sourceModuleId: Descriptor.Id,
            provider: "memory",
            mode: "volatile-buffer",
            tags: ["audit", "tenant"],
            metadata: new Dictionary<string, string>
            {
                ["writeMode"] = "application-managed",
                ["queryMode"] = "not-configured",
                ["tenantAware"] = "true"
            }));
    }
}

internal sealed class InvalidProjectionSourceModule : ModuleBase, IProjectionContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "invalid-phase8-projection",
        displayName: "Invalid Phase 8 Projection",
        description: "Contributes an invalid projection descriptor for test coverage.",
        tags: ["phase8", "invalid"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterProjections(IProjectionRegistry projections)
    {
        projections.Add(new ProjectionDescriptor(
            id: "broken-projection",
            displayName: "Broken Projection",
            description: "Declares the wrong source module id.",
            sourceModuleId: "another-module",
            targetStoreId: "broken-read-model"));
    }
}

internal sealed class InvalidDataProductSourceModule : ModuleBase, IDataProductContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "invalid-phase8-data-product",
        displayName: "Invalid Phase 8 Data Product",
        description: "Contributes an invalid data product descriptor for test coverage.",
        tags: ["phase8", "invalid"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterDataProducts(IDataProductRegistry dataProducts)
    {
        dataProducts.Add(new DataProductDescriptor(
            id: "broken-data-product",
            displayName: "Broken Data Product",
            description: "Declares the wrong source module id.",
            sourceModuleId: "another-module",
            domainId: "tenant-management",
            contractId: "broken-contract"));
    }
}

internal sealed class InvalidOutboxSourceModule : ModuleBase, IOutboxContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "invalid-phase8-outbox",
        displayName: "Invalid Phase 8 Outbox",
        description: "Contributes an invalid outbox descriptor for test coverage.",
        tags: ["phase8", "invalid"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterOutboxes(IOutboxRegistry outboxes)
    {
        outboxes.Add(new OutboxDescriptor(
            id: "broken-outbox",
            displayName: "Broken Outbox",
            description: "Declares the wrong source module id.",
            sourceModuleId: "another-module",
            provider: "relational"));
    }
}

internal sealed class InvalidInboxSourceModule : ModuleBase, IInboxContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "invalid-phase8-inbox",
        displayName: "Invalid Phase 8 Inbox",
        description: "Contributes an invalid inbox descriptor for test coverage.",
        tags: ["phase8", "invalid"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterInboxes(IInboxRegistry inboxes)
    {
        inboxes.Add(new InboxDescriptor(
            id: "broken-inbox",
            displayName: "Broken Inbox",
            description: "Declares the wrong source module id.",
            sourceModuleId: "another-module",
            provider: "relational"));
    }
}

internal sealed class InvalidAuditStoreSourceModule : ModuleBase, IAuditStoreContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "invalid-phase8-audit-store",
        displayName: "Invalid Phase 8 Audit Store",
        description: "Contributes an invalid audit-store descriptor for test coverage.",
        tags: ["phase8", "invalid"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }

    public void RegisterAuditStores(IAuditStoreRegistry auditStores)
    {
        auditStores.Add(new AuditStoreDescriptor(
            id: "broken-audit-store",
            displayName: "Broken Audit Store",
            description: "Declares the wrong source module id.",
            sourceModuleId: "another-module",
            provider: "memory"));
    }
}
