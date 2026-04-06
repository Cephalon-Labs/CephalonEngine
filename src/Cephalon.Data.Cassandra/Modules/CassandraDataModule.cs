using Cassandra;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.Cassandra.Configuration;
using Cephalon.Data.Cassandra.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Data.Cassandra.Modules;

internal sealed class CassandraDataModule(CassandraDataOptions options) : ModuleBase, IInboxContributor, IOutboxContributor, ITechnologyServiceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "cassandra-data",
        displayName: "Cassandra Data",
        description: "Apache Cassandra wide-column store registration for Cephalon data workloads.",
        tags: ["data", "cassandra", "wide-column"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "cassandra-data"
        });

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ICluster>(_ =>
            Cluster.Builder()
                .AddContactPoints(options.ContactPoints.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .WithPort(options.Port)
                .Build());

        if (options.RegisterOutbox)
        {
            services.TryAddScoped<IOutbox>(serviceProvider =>
            {
                var cluster = serviceProvider.GetRequiredService<ICluster>();
                return new CassandraOutbox(cluster, options);
            });
        }

        if (options.RegisterInbox)
        {
            services.TryAddScoped<IInbox>(serviceProvider =>
            {
                var cluster = serviceProvider.GetRequiredService<ICluster>();
                return new CassandraInbox(cluster, options);
            });
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
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, CassandraOutboxRuntimeSurfaceContributor>());
        }

        if (options.RegisterInbox)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, CassandraInboxRuntimeSurfaceContributor>());
        }
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: "data.cassandra",
            displayName: "Cassandra Data Provider",
            description: "Registers Apache Cassandra wide-column store as the backing data provider for Cephalon data workloads.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Cassandra",
                ["provider"] = CassandraDataOptions.ProviderId,
                ["keyspace"] = options.Keyspace
            }));

        capabilities.Add(new Capability(
            key: "data.wide-column-store",
            displayName: "Wide-Column Store",
            description: "The active data provider is a wide-column database.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Cassandra",
                ["provider"] = CassandraDataOptions.ProviderId
            }));

        if (options.RegisterOutbox)
        {
            capabilities.Add(new Capability(
                key: "data.outbox.cassandra",
                displayName: "Cassandra Outbox",
                description: "Stages outbox messages through the active Cassandra wide-column table.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.Cassandra",
                    ["provider"] = CassandraDataOptions.ProviderId
                }));
        }

        if (options.RegisterInbox)
        {
            capabilities.Add(new Capability(
                key: "data.inbox.cassandra",
                displayName: "Cassandra Inbox",
                description: "Tracks processed inbound messages through the active Cassandra wide-column table.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.Cassandra",
                    ["provider"] = CassandraDataOptions.ProviderId
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
            id: "cassandra-outbox",
            displayName: "Cassandra Outbox",
            description: "Stages durable outbound messages through the active Cassandra wide-column table using LWT INSERT IF NOT EXISTS.",
            sourceModuleId: Descriptor.Id,
            provider: CassandraDataOptions.ProviderId,
            mode: "wide-column-lwt",
            tags: ["data", "cassandra", "outbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Cassandra",
                ["keyspace"] = options.Keyspace,
                ["table"] = $"{options.TablePrefix}outbox_messages",
                ["idempotency"] = "lwt-if-not-exists",
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
            id: "cassandra-inbox",
            displayName: "Cassandra Inbox",
            description: "Tracks processed inbound messages through the active Cassandra wide-column table using LWT INSERT IF NOT EXISTS.",
            sourceModuleId: Descriptor.Id,
            provider: CassandraDataOptions.ProviderId,
            mode: "wide-column-lwt",
            tags: ["data", "cassandra", "inbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Cassandra",
                ["keyspace"] = options.Keyspace,
                ["table"] = $"{options.TablePrefix}inbox_receipts",
                ["idempotency"] = "lwt-if-not-exists",
                ["dispatchRuntime"] = "not-configured",
                ["channelMode"] = "dynamic",
                ["subscriptionRuntime"] = "not-configured"
            }));
    }
}
