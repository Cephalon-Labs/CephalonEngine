using System.Globalization;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Data.Postgres.Configuration;
using Cephalon.Data.Postgres.Services;
using Cephalon.Data.Services;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Data.Postgres.Modules;

internal sealed class PostgresDataModule(PostgresDataOptions options)
    : ModuleBase,
        ICdcCaptureContributor,
        IExecutionGraphContributor,
        IHostedExecutionContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "postgres-data",
        displayName: "PostgreSQL Data",
        description: "PostgreSQL provider-native logical-replication CDC registration for Cephalon data workloads.",
        tags: ["data", "postgresql", "relational"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "postgres-data"
        });

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(options);

        if (options.CdcCaptures.Count > 0)
        {
            services.TryAddSingleton<IPostgresLogicalReplicationTransport>(serviceProvider =>
            {
                var configuration = serviceProvider.GetService<IConfiguration>();
                var connectionString = ResolveConnectionString(configuration);
                var logger = serviceProvider.GetRequiredService<ILogger<PostgresLogicalReplicationTransport>>();
                return new PostgresLogicalReplicationTransport(connectionString, logger);
            });
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ICdcCaptureExecutionRuntimeContributor, PostgresLogicalReplicationExecutionRuntimeContributor>());
            services.AddHostedService<PostgresLogicalReplicationCaptureHostedService>();
        }
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: "data.postgresql",
            displayName: "PostgreSQL Data Provider",
            description: "Registers PostgreSQL provider-native logical-replication CDC wiring for Cephalon data workloads.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Postgres",
                ["provider"] = PostgresDataOptions.ProviderId,
                ["database"] = options.DatabaseName.Trim()
            }));

        capabilities.Add(new Capability(
            key: "data.relational-store",
            displayName: "Relational Store",
            description: "The active data provider integrates with a relational database.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Postgres",
                ["provider"] = PostgresDataOptions.ProviderId
            }));

        if (options.CdcCaptures.Count > 0)
        {
            capabilities.Add(new Capability(
                key: "data.cdc.postgresql",
                displayName: "PostgreSQL Logical Replication CDC",
                description: "Runs provider-native PostgreSQL logical-replication captures and stages durable publications through the linked outbox path.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.Postgres",
                    ["executionRuntimeId"] = PostgresDataRuntimeIds.CdcExecutionRuntimeId,
                    ["hostedExecutionId"] = PostgresDataRuntimeIds.CdcHostedExecutionId,
                    ["executionGraphId"] = PostgresDataRuntimeIds.CdcExecutionGraphId
                }));
        }
    }

    public void RegisterCdcCaptures(ICdcCaptureRegistry cdcCaptures)
    {
        ArgumentNullException.ThrowIfNull(cdcCaptures);

        foreach (var capture in options.CdcCaptures)
        {
            var id = capture.Id.Trim();
            var sourceId = string.IsNullOrWhiteSpace(capture.SourceId)
                ? $"{PostgresDataOptions.ProviderId}:{options.DatabaseName.Trim()}/{capture.TableSchema.Trim()}.{capture.TableName.Trim()}"
                : capture.SourceId.Trim();
            var metadata = new Dictionary<string, string>(capture.Metadata, StringComparer.OrdinalIgnoreCase)
            {
                ["provider"] = PostgresDataOptions.ProviderId,
                ["databaseName"] = options.DatabaseName.Trim(),
                ["publicationName"] = capture.PublicationName.Trim(),
                ["slotName"] = capture.SlotName.Trim(),
                ["tableSchema"] = capture.TableSchema.Trim(),
                ["tableName"] = capture.TableName.Trim(),
                ["channelId"] = capture.ChannelId.Trim(),
                ["messageType"] = capture.MessageType.Trim(),
                ["initialPosition"] = capture.InitialPosition.Trim(),
                ["createSlotIfMissing"] = capture.CreateSlotIfMissing ? "true" : "false",
                ["recreateSlotIfInvalidated"] = capture.RecreateSlotIfInvalidated ? "true" : "false",
                ["slotLifecyclePolicy"] = capture.RecreateSlotIfInvalidated
                    ? "recreate-invalidated-slot"
                    : "fail-on-invalidated-slot",
                ["slotResumeMode"] = "slot-confirmed-flush-lsn",
                ["maxChangesPerRead"] = capture.MaxChangesPerRead.ToString(CultureInfo.InvariantCulture),
                ["maxAwaitTimeSeconds"] = capture.MaxAwaitTimeSeconds.ToString(CultureInfo.InvariantCulture),
                ["pollingIntervalSeconds"] = capture.PollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                ["replicationCheckpointSource"] = "slot-confirmed-flush-lsn",
                ["executionRuntimeId"] = PostgresDataRuntimeIds.CdcExecutionRuntimeId,
                ["contributorModuleId"] = Descriptor.Id
            };

            var resourceIds = capture.ResourceIds.Count == 0
                ? [$"{options.DatabaseName.Trim()}.{capture.TableSchema.Trim()}.{capture.TableName.Trim()}"]
                : capture.ResourceIds.ToArray();

            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: id,
                displayName: string.IsNullOrWhiteSpace(capture.DisplayName) ? id : capture.DisplayName.Trim(),
                description: string.IsNullOrWhiteSpace(capture.Description)
                    ? $"Reads PostgreSQL logical replication publication '{capture.PublicationName.Trim()}' for table '{options.DatabaseName.Trim()}.{capture.TableSchema.Trim()}.{capture.TableName.Trim()}' through slot '{capture.SlotName.Trim()}' and stages publications through outbox '{capture.OutboxId.Trim()}'."
                    : capture.Description.Trim(),
                sourceModuleId: capture.SourceModuleId.Trim(),
                provider: PostgresDataOptions.ProviderId,
                sourceId: sourceId,
                outboxId: capture.OutboxId.Trim(),
                executionBinding: new CdcCaptureExecutionBindingDescriptor(
                    cdcCaptureId: id,
                    authoredExecutionRuntimeId: PostgresDataRuntimeIds.CdcExecutionRuntimeId,
                    requestedExecutionRuntimeId: PostgresDataRuntimeIds.CdcExecutionRuntimeId),
                mode: "logical-replication",
                eventFormat: string.IsNullOrWhiteSpace(capture.EventFormat)
                    ? "postgresql-logical-replication-event"
                    : capture.EventFormat.Trim(),
                resourceIds: resourceIds,
                tags: capture.Tags.Count == 0
                    ? ["cdc", "postgresql", "provider-native"]
                    : capture.Tags.ToArray(),
                metadata: metadata));
        }
    }

    public void RegisterExecutionGraphs(IExecutionGraphRegistry graphs)
    {
        ArgumentNullException.ThrowIfNull(graphs);

        if (options.CdcCaptures.Count == 0)
        {
            return;
        }

        graphs.Add(new ExecutionGraphDescriptor(
            id: PostgresDataRuntimeIds.CdcExecutionGraphId,
            displayName: "PostgreSQL Logical Replication Capture Flow",
            description: "Resolves provider-native PostgreSQL logical-replication capture declarations, reads one bounded WAL batch, stages linked outbox publications, confirms replication-slot progress after stage success, and reports runtime observations.",
            sourceModuleId: Descriptor.Id,
            entryNodeId: "resolve-postgresql-cdc-captures",
            nodes:
            [
                new ExecutionGraphNodeDescriptor(
                    id: "resolve-postgresql-cdc-captures",
                    displayName: "Resolve PostgreSQL CDC Captures",
                    description: "Resolves active PostgreSQL logical-replication captures and their linked outbox bindings.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.postgresql",
                    tags: ["data", "cdc", "postgresql"]),
                new ExecutionGraphNodeDescriptor(
                    id: "read-postgresql-logical-replication-batch",
                    displayName: "Read PostgreSQL Logical Replication Batch",
                    description: "Reads one bounded provider-native PostgreSQL logical-replication batch from the configured publication and replication slot.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.postgresql",
                    tags: ["data", "cdc", "postgresql", "logical-replication"]),
                new ExecutionGraphNodeDescriptor(
                    id: "stage-postgresql-cdc-publications",
                    displayName: "Stage PostgreSQL CDC Publications",
                    description: "Stages the captured PostgreSQL logical-replication events through the linked outbox implementation.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.postgresql",
                    tags: ["data", "cdc", "postgresql", "outbox"]),
                new ExecutionGraphNodeDescriptor(
                    id: "confirm-postgresql-replication-progress",
                    displayName: "Confirm PostgreSQL Replication Progress",
                    description: "Confirms logical-replication slot progress only after the linked outbox publications were staged successfully.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.postgresql",
                    tags: ["data", "cdc", "postgresql", "checkpoint"]),
                new ExecutionGraphNodeDescriptor(
                    id: "report-postgresql-cdc-runtime-observation",
                    displayName: "Report PostgreSQL CDC Runtime Observation",
                    description: "Projects the latest provider-native PostgreSQL logical-replication posture into the shared CDC runtime-state catalog.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.postgresql",
                    tags: ["data", "cdc", "postgresql", "runtime-state"])
            ],
            edges:
            [
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "resolve-postgresql-cdc-captures",
                    toNodeId: "read-postgresql-logical-replication-batch",
                    displayName: "resolved"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "read-postgresql-logical-replication-batch",
                    toNodeId: "stage-postgresql-cdc-publications",
                    displayName: "captured"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "stage-postgresql-cdc-publications",
                    toNodeId: "confirm-postgresql-replication-progress",
                    displayName: "staged"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "confirm-postgresql-replication-progress",
                    toNodeId: "report-postgresql-cdc-runtime-observation",
                    displayName: "reported")
            ],
            tags: ["data", "cdc", "postgresql"],
            metadata: new Dictionary<string, string>
            {
                ["surface"] = "postgresql-cdc"
            }));
    }

    public void RegisterHostedExecutions(IHostedExecutionRegistry hostedExecutions)
    {
        ArgumentNullException.ThrowIfNull(hostedExecutions);

        if (options.CdcCaptures.Count == 0)
        {
            return;
        }

        hostedExecutions.Add(new HostedExecutionDescriptor(
            id: PostgresDataRuntimeIds.CdcHostedExecutionId,
            displayName: "PostgreSQL Logical Replication Capture Pump",
            description: "Runs the provider-native PostgreSQL logical-replication background pump for configured captures.",
            sourceModuleId: Descriptor.Id,
            kind: "background-service",
            executionGraphId: PostgresDataRuntimeIds.CdcExecutionGraphId,
            startsWithHost: true,
            tags: ["data", "cdc", "postgresql"],
            metadata: new Dictionary<string, string>
            {
                ["surface"] = "postgresql-cdc"
            }));
    }

    private string ResolveConnectionString(IConfiguration? configuration)
    {
        var connectionString = ConnectionStringResolution.Resolve(
            configuration,
            options.ConnectionString,
            options.ConnectionStringName,
            string.Empty,
            PostgresDataOptions.SectionPath,
            "PostgreSQL");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{PostgresDataOptions.SectionPath} must configure either ConnectionStringName or ConnectionString before PostgreSQL logical replication can start.");
        }

        return connectionString.Trim();
    }
}
