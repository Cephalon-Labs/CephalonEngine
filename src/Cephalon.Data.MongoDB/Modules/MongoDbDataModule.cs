using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.MongoDB.Configuration;
using Cephalon.Data.MongoDB.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Cephalon.Data.MongoDB.Modules;

internal sealed class MongoDbDataModule(MongoDbDataOptions options) : ModuleBase, IInboxContributor, IOutboxContributor, ITechnologyServiceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "mongodb-data",
        displayName: "MongoDB Data",
        description: "MongoDB document store registration for Cephalon data workloads.",
        tags: ["data", "mongodb", "document-store"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "mongodb-data"
        });

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(options.ConnectionString));
        services.TryAddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IMongoClient>().GetDatabase(options.DatabaseName));

        if (options.RegisterOutbox)
        {
            var collectionName = $"{options.CollectionPrefix}outbox_messages";
            services.TryAddScoped<IOutbox>(serviceProvider =>
            {
                var database = serviceProvider.GetRequiredService<IMongoDatabase>();
                var collection = database.GetCollection<MongoDbOutboxEntry>(collectionName);
                return new MongoDbOutbox(collection);
            });
        }

        if (options.RegisterInbox)
        {
            var collectionName = $"{options.CollectionPrefix}inbox_receipts";
            services.TryAddScoped<IInbox>(serviceProvider =>
            {
                var database = serviceProvider.GetRequiredService<IMongoDatabase>();
                var collection = database.GetCollection<MongoDbInboxEntry>(collectionName);
                return new MongoDbInbox(collection);
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
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MongoDbOutboxRuntimeSurfaceContributor>());
        }

        if (options.RegisterInbox)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, MongoDbInboxRuntimeSurfaceContributor>());
        }
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: "data.mongodb",
            displayName: "MongoDB Data Provider",
            description: "Registers MongoDB document store as the backing data provider for Cephalon data workloads.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.MongoDB",
                ["provider"] = MongoDbDataOptions.ProviderId,
                ["database"] = options.DatabaseName
            }));

        capabilities.Add(new Capability(
            key: "data.document-store",
            displayName: "Document Store",
            description: "The active data provider is a document-oriented database.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.MongoDB",
                ["provider"] = MongoDbDataOptions.ProviderId
            }));

        if (options.RegisterOutbox)
        {
            capabilities.Add(new Capability(
                key: "data.outbox.mongodb",
                displayName: "MongoDB Outbox",
                description: "Stages outbox messages through the active MongoDB collection.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.MongoDB",
                    ["provider"] = MongoDbDataOptions.ProviderId
                }));
        }

        if (options.RegisterInbox)
        {
            capabilities.Add(new Capability(
                key: "data.inbox.mongodb",
                displayName: "MongoDB Inbox",
                description: "Tracks processed inbound messages through the active MongoDB collection.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.MongoDB",
                    ["provider"] = MongoDbDataOptions.ProviderId
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
            id: "mongodb-outbox",
            displayName: "MongoDB Outbox",
            description: "Stages durable outbound messages through the active MongoDB collection.",
            sourceModuleId: Descriptor.Id,
            provider: MongoDbDataOptions.ProviderId,
            mode: "document-collection",
            tags: ["data", "mongodb", "outbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.MongoDB",
                ["database"] = options.DatabaseName,
                ["collection"] = $"{options.CollectionPrefix}outbox_messages",
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
            id: "mongodb-inbox",
            displayName: "MongoDB Inbox",
            description: "Tracks processed inbound messages through the active MongoDB collection.",
            sourceModuleId: Descriptor.Id,
            provider: MongoDbDataOptions.ProviderId,
            mode: "document-collection",
            tags: ["data", "mongodb", "inbox"],
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.MongoDB",
                ["database"] = options.DatabaseName,
                ["collection"] = $"{options.CollectionPrefix}inbox_receipts",
                ["dispatchRuntime"] = "not-configured",
                ["channelMode"] = "dynamic",
                ["idempotency"] = "message-id",
                ["subscriptionRuntime"] = "not-configured"
            }));
    }
}
