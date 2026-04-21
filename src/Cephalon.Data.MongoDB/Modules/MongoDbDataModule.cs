using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Data.MongoDB.Configuration;
using Cephalon.Data.MongoDB.Services;
using Cephalon.Data.Services;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Cephalon.Data.MongoDB.Modules;

internal sealed class MongoDbDataModule(MongoDbDataOptions options)
    : ModuleBase,
        ICdcCaptureContributor,
        IExecutionGraphContributor,
        IHostedExecutionContributor,
        IInboxContributor,
        IOutboxContributor,
        ITechnologyServiceContributor
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

        services.TryAddSingleton(options);
        services.TryAddSingleton<IMongoClient>(serviceProvider =>
            new MongoClient(ConnectionStringResolution.Resolve(
                serviceProvider.GetService<IConfiguration>(),
                options.ConnectionString,
                options.ConnectionStringName,
                MongoDbDataOptions.DefaultConnectionString,
                MongoDbDataOptions.SectionPath,
                "MongoDB")));
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
            services.TryAddScoped<IEventDispatchStore>(serviceProvider =>
            {
                var database = serviceProvider.GetRequiredService<IMongoDatabase>();
                var collection = database.GetCollection<MongoDbOutboxEntry>(collectionName);
                return new MongoDbEventDispatchStore(collection);
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

        if (options.ChangeStreamCaptures.Count > 0)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ICdcCaptureExecutionRuntimeContributor, MongoDbChangeStreamExecutionRuntimeContributor>());
            services.AddHostedService<MongoDbChangeStreamCaptureHostedService>();
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

        if (options.ChangeStreamCaptures.Count > 0)
        {
            capabilities.Add(new Capability(
                key: "data.cdc.mongodb",
                displayName: "MongoDB Change Stream CDC",
                description: "Runs provider-native MongoDB change-stream captures and stages durable publications through the linked outbox path.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.MongoDB",
                    ["executionRuntimeId"] = MongoDbDataRuntimeIds.ChangeStreamExecutionRuntimeId,
                    ["hostedExecutionId"] = MongoDbDataRuntimeIds.ChangeStreamHostedExecutionId,
                    ["executionGraphId"] = MongoDbDataRuntimeIds.ChangeStreamExecutionGraphId
                }));
        }
    }

    /// <inheritdoc />
    public void RegisterCdcCaptures(ICdcCaptureRegistry cdcCaptures)
    {
        ArgumentNullException.ThrowIfNull(cdcCaptures);

        foreach (var capture in options.ChangeStreamCaptures)
        {
            var id = capture.Id.Trim();
            var databaseName = ResolveDatabaseName(capture);
            var collectionName = capture.CollectionName.Trim();
            var sourceId = string.IsNullOrWhiteSpace(capture.SourceId)
                ? $"{MongoDbDataOptions.ProviderId}:{databaseName}/{collectionName}"
                : capture.SourceId.Trim();
            var metadata = new Dictionary<string, string>(capture.Metadata, StringComparer.OrdinalIgnoreCase)
            {
                ["provider"] = MongoDbDataOptions.ProviderId,
                ["databaseName"] = databaseName,
                ["collectionName"] = collectionName,
                ["channelId"] = capture.ChannelId.Trim(),
                ["messageType"] = capture.MessageType.Trim(),
                ["fullDocumentMode"] = capture.FullDocumentMode.Trim(),
                ["checkpointCollection"] = MongoDbChangeStreamCaptureHostedService.GetCheckpointCollectionName(options.CollectionPrefix),
                ["executionRuntimeId"] = MongoDbDataRuntimeIds.ChangeStreamExecutionRuntimeId
            };

            var resourceIds = capture.ResourceIds.Count == 0
                ? [$"{databaseName}.{collectionName}"]
                : capture.ResourceIds.ToArray();

            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: id,
                displayName: string.IsNullOrWhiteSpace(capture.DisplayName) ? id : capture.DisplayName.Trim(),
                description: string.IsNullOrWhiteSpace(capture.Description)
                    ? $"Watches MongoDB change-stream events on collection '{databaseName}.{collectionName}' and stages them through outbox '{capture.OutboxId.Trim()}'."
                    : capture.Description.Trim(),
                sourceModuleId: capture.SourceModuleId.Trim(),
                provider: MongoDbDataOptions.ProviderId,
                sourceId: sourceId,
                outboxId: capture.OutboxId.Trim(),
                executionBinding: new CdcCaptureExecutionBindingDescriptor(
                    cdcCaptureId: id,
                    authoredExecutionRuntimeId: MongoDbDataRuntimeIds.ChangeStreamExecutionRuntimeId,
                    requestedExecutionRuntimeId: MongoDbDataRuntimeIds.ChangeStreamExecutionRuntimeId),
                mode: "change-stream",
                eventFormat: string.IsNullOrWhiteSpace(capture.EventFormat)
                    ? "mongodb-change-stream-event"
                    : capture.EventFormat.Trim(),
                resourceIds: resourceIds,
                tags: capture.Tags.Count == 0
                    ? ["cdc", "mongodb", "provider-native"]
                    : capture.Tags.ToArray(),
                metadata: metadata));
        }
    }

    /// <inheritdoc />
    public void RegisterExecutionGraphs(IExecutionGraphRegistry graphs)
    {
        ArgumentNullException.ThrowIfNull(graphs);

        if (options.ChangeStreamCaptures.Count == 0)
        {
            return;
        }

        graphs.Add(new ExecutionGraphDescriptor(
            id: MongoDbDataRuntimeIds.ChangeStreamExecutionGraphId,
            displayName: "MongoDB Change Stream Capture Flow",
            description: "Resolves provider-native MongoDB change-stream declarations, reads one bounded batch from the watched collection, stages linked outbox publications, persists resume-token checkpoints, and reports runtime observations.",
            sourceModuleId: Descriptor.Id,
            entryNodeId: "resolve-mongodb-change-stream-captures",
            nodes:
            [
                new ExecutionGraphNodeDescriptor(
                    id: "resolve-mongodb-change-stream-captures",
                    displayName: "Resolve MongoDB Change Stream Captures",
                    description: "Resolves active MongoDB provider-native change-stream captures and their linked outbox bindings.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.mongodb",
                    tags: ["data", "cdc", "mongodb"]),
                new ExecutionGraphNodeDescriptor(
                    id: "read-mongodb-change-stream-batch",
                    displayName: "Read MongoDB Change Stream Batch",
                    description: "Reads one bounded provider-native batch from the configured MongoDB change stream.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.mongodb",
                    tags: ["data", "cdc", "mongodb", "change-stream"]),
                new ExecutionGraphNodeDescriptor(
                    id: "stage-mongodb-cdc-publications",
                    displayName: "Stage MongoDB CDC Publications",
                    description: "Stages the captured change events through the linked outbox implementation.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.mongodb",
                    tags: ["data", "cdc", "mongodb", "outbox"]),
                new ExecutionGraphNodeDescriptor(
                    id: "persist-mongodb-resume-token",
                    displayName: "Persist MongoDB Resume Token",
                    description: "Persists the latest durable MongoDB resume token only after the linked outbox publications were staged successfully.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.mongodb",
                    tags: ["data", "cdc", "mongodb", "checkpoint"]),
                new ExecutionGraphNodeDescriptor(
                    id: "report-mongodb-cdc-runtime-observation",
                    displayName: "Report MongoDB CDC Runtime Observation",
                    description: "Projects the latest provider-native MongoDB change-stream posture into the shared CDC runtime-state catalog.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.mongodb",
                    tags: ["data", "cdc", "mongodb", "runtime-state"])
            ],
            edges:
            [
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "resolve-mongodb-change-stream-captures",
                    toNodeId: "read-mongodb-change-stream-batch",
                    displayName: "resolved"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "read-mongodb-change-stream-batch",
                    toNodeId: "stage-mongodb-cdc-publications",
                    displayName: "captured"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "stage-mongodb-cdc-publications",
                    toNodeId: "persist-mongodb-resume-token",
                    displayName: "staged"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "persist-mongodb-resume-token",
                    toNodeId: "report-mongodb-cdc-runtime-observation",
                    displayName: "reported")
            ],
            tags: ["data", "cdc", "mongodb"],
            metadata: new Dictionary<string, string>
            {
                ["surface"] = "change-stream-cdc"
            }));
    }

    /// <inheritdoc />
    public void RegisterHostedExecutions(IHostedExecutionRegistry hostedExecutions)
    {
        ArgumentNullException.ThrowIfNull(hostedExecutions);

        if (options.ChangeStreamCaptures.Count == 0)
        {
            return;
        }

        hostedExecutions.Add(new HostedExecutionDescriptor(
            id: MongoDbDataRuntimeIds.ChangeStreamHostedExecutionId,
            displayName: "MongoDB Change Stream Capture Pump",
            description: "Runs the provider-native MongoDB change-stream background pump for configured CDC captures.",
            sourceModuleId: Descriptor.Id,
            kind: "background-service",
            executionGraphId: MongoDbDataRuntimeIds.ChangeStreamExecutionGraphId,
            startsWithHost: true,
            tags: ["data", "cdc", "mongodb"],
            metadata: new Dictionary<string, string>
            {
                ["surface"] = "change-stream-cdc"
            }));
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

    private string ResolveDatabaseName(MongoDbChangeStreamCaptureOptions capture)
    {
        return string.IsNullOrWhiteSpace(capture.DatabaseName)
            ? options.DatabaseName
            : capture.DatabaseName.Trim();
    }
}
