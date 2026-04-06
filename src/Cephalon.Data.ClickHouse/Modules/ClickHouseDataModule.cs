using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.ClickHouse.Configuration;
using Cephalon.Data.ClickHouse.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Data.ClickHouse.Modules;

internal sealed class ClickHouseDataModule(ClickHouseDataOptions options) : ModuleBase, IInboxContributor, IOutboxContributor, ITechnologyServiceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "clickhouse-data",
        displayName: "ClickHouse Data",
        description: "ClickHouse analytics store registration for Cephalon data workloads.",
        tags: ["data", "clickhouse", "analytics"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "clickhouse-data"
        });

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(options);

        if (options.RegisterOutbox)
        {
            services.TryAddScoped<IOutbox>(_ => new ClickHouseOutbox(options));
        }

        if (options.RegisterInbox)
        {
            services.TryAddScoped<IInbox>(_ => new ClickHouseInbox(options));
        }
    }

    /// <inheritdoc />
    public void ConfigureTechnologyServices(IServiceCollection services, TechnologySelection technologies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(technologies);

        if (!technologies.IsSelected("event-driven-integration"))
        {
            return;
        }

        if (options.RegisterOutbox)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, ClickHouseOutboxRuntimeSurfaceContributor>());
        }

        if (options.RegisterInbox)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, ClickHouseInboxRuntimeSurfaceContributor>());
        }
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: "data.clickhouse",
            displayName: "ClickHouse Data Provider",
            description: "Registers ClickHouse analytics store as the backing data provider for Cephalon data workloads.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.ClickHouse",
                ["provider"] = ClickHouseDataOptions.ProviderId,
                ["host"] = options.Host,
                ["database"] = options.Database
            }));

        capabilities.Add(new Capability(
            key: "data.analytics-store",
            displayName: "Analytics Store",
            description: "The active data provider is an analytics-optimized columnar database.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.ClickHouse",
                ["provider"] = ClickHouseDataOptions.ProviderId
            }));

        if (options.RegisterOutbox)
        {
            capabilities.Add(new Capability(
                key: "data.outbox.clickhouse",
                displayName: "ClickHouse Outbox",
                description: "Stages outbox messages through the active ClickHouse ReplacingMergeTree table.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.ClickHouse",
                    ["provider"] = ClickHouseDataOptions.ProviderId
                }));
        }

        if (options.RegisterInbox)
        {
            capabilities.Add(new Capability(
                key: "data.inbox.clickhouse",
                displayName: "ClickHouse Inbox",
                description: "Tracks processed inbound messages through the active ClickHouse ReplacingMergeTree table.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.ClickHouse",
                    ["provider"] = ClickHouseDataOptions.ProviderId
                }));
        }
    }

    /// <inheritdoc />
    public void RegisterOutboxes(IOutboxRegistry outboxes)
    {
        ArgumentNullException.ThrowIfNull(outboxes);

        if (!options.RegisterOutbox)
        {
            return;
        }

        outboxes.Add(new OutboxDescriptor(
            id: "clickhouse-outbox",
            displayName: "ClickHouse Outbox",
            description: "Stages durable outbound messages through the active ClickHouse ReplacingMergeTree table with eventual deduplication via ORDER BY (message_id).",
            sourceModuleId: Descriptor.Id,
            provider: ClickHouseDataOptions.ProviderId,
            mode: "replacing-merge-tree",
            tags: ["data", "clickhouse", "outbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.ClickHouse",
                ["database"] = options.Database,
                ["table"] = $"{options.TablePrefix}outbox_messages",
                ["idempotency"] = "replacing-merge-tree-eventual",
                ["engine"] = "ReplacingMergeTree",
                ["dispatchRuntime"] = "not-configured",
                ["channelMode"] = "dynamic"
            }));
    }

    /// <inheritdoc />
    public void RegisterInboxes(IInboxRegistry inboxes)
    {
        ArgumentNullException.ThrowIfNull(inboxes);

        if (!options.RegisterInbox)
        {
            return;
        }

        inboxes.Add(new InboxDescriptor(
            id: "clickhouse-inbox",
            displayName: "ClickHouse Inbox",
            description: "Tracks processed inbound messages through the active ClickHouse ReplacingMergeTree table with FINAL-read deduplication.",
            sourceModuleId: Descriptor.Id,
            provider: ClickHouseDataOptions.ProviderId,
            mode: "replacing-merge-tree",
            tags: ["data", "clickhouse", "inbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.ClickHouse",
                ["database"] = options.Database,
                ["table"] = $"{options.TablePrefix}inbox_receipts",
                ["idempotency"] = "replacing-merge-tree-eventual",
                ["engine"] = "ReplacingMergeTree",
                ["dispatchRuntime"] = "not-configured",
                ["channelMode"] = "dynamic",
                ["subscriptionRuntime"] = "not-configured"
            }));
    }
}
