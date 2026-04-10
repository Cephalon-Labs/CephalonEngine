using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.Qdrant.Configuration;
using Cephalon.Data.Qdrant.Services;
using Cephalon.Eventing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qdrant.Client;

namespace Cephalon.Data.Qdrant.Modules;

internal sealed class QdrantDataModule(QdrantDataOptions options) : ModuleBase, IInboxContributor, IOutboxContributor, ITechnologyServiceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "qdrant-data",
        displayName: "Qdrant Data",
        description: "Qdrant vector-store registration for Cephalon data workloads.",
        tags: ["data", "qdrant", "vector-store"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "qdrant-data"
        });

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<QdrantClient>(_ => new QdrantClient(options.Host, options.Port, apiKey: options.ApiKey));

        var outboxCollection = $"{options.CollectionPrefix}outbox_messages";
        var inboxCollection = $"{options.CollectionPrefix}inbox_receipts";

        if (options.RegisterOutbox)
        {
            services.TryAddScoped<IOutbox>(serviceProvider =>
            {
                var client = serviceProvider.GetRequiredService<QdrantClient>();
                return new QdrantOutbox(client, outboxCollection);
            });
            services.TryAddScoped<IEventDispatchStore>(serviceProvider =>
            {
                var client = serviceProvider.GetRequiredService<QdrantClient>();
                return new QdrantEventDispatchStore(client, outboxCollection);
            });
        }

        if (options.RegisterInbox)
        {
            services.TryAddScoped<IInbox>(serviceProvider =>
            {
                var client = serviceProvider.GetRequiredService<QdrantClient>();
                return new QdrantInbox(client, inboxCollection);
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
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, QdrantOutboxRuntimeSurfaceContributor>());
        }

        if (options.RegisterInbox)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, QdrantInboxRuntimeSurfaceContributor>());
        }
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: "data.qdrant",
            displayName: "Qdrant Data Provider",
            description: "Registers Qdrant vector store as the backing data provider for Cephalon data workloads.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Qdrant",
                ["provider"] = QdrantDataOptions.ProviderId,
                ["host"] = options.Host,
                ["port"] = options.Port.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }));

        capabilities.Add(new Capability(
            key: "data.vector-store",
            displayName: "Vector Store",
            description: "The active data provider is a vector database.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Qdrant",
                ["provider"] = QdrantDataOptions.ProviderId
            }));

        if (options.RegisterOutbox)
        {
            capabilities.Add(new Capability(
                key: "data.outbox.qdrant",
                displayName: "Qdrant Outbox",
                description: "Stages outbox messages through the active Qdrant vector collection.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.Qdrant",
                    ["provider"] = QdrantDataOptions.ProviderId
                }));
        }

        if (options.RegisterInbox)
        {
            capabilities.Add(new Capability(
                key: "data.inbox.qdrant",
                displayName: "Qdrant Inbox",
                description: "Tracks processed inbound messages through the active Qdrant vector collection.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.Qdrant",
                    ["provider"] = QdrantDataOptions.ProviderId
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
            id: "qdrant-outbox",
            displayName: "Qdrant Outbox",
            description: "Stages durable outbound messages through the active Qdrant vector collection using point-ID existence checks for idempotency.",
            sourceModuleId: Descriptor.Id,
            provider: QdrantDataOptions.ProviderId,
            mode: "vector-collection",
            tags: ["data", "qdrant", "outbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Qdrant",
                ["collection"] = $"{options.CollectionPrefix}outbox_messages",
                ["idempotency"] = "point-id",
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
            id: "qdrant-inbox",
            displayName: "Qdrant Inbox",
            description: "Tracks processed inbound messages through the active Qdrant vector collection using point-ID existence checks for idempotency.",
            sourceModuleId: Descriptor.Id,
            provider: QdrantDataOptions.ProviderId,
            mode: "vector-collection",
            tags: ["data", "qdrant", "inbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Qdrant",
                ["collection"] = $"{options.CollectionPrefix}inbox_receipts",
                ["idempotency"] = "point-id",
                ["dispatchRuntime"] = "not-configured",
                ["channelMode"] = "dynamic",
                ["subscriptionRuntime"] = "not-configured"
            }));
    }
}
