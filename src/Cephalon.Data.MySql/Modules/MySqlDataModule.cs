using System.Globalization;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Data.MySql.Configuration;
using Cephalon.Data.MySql.Services;
using Cephalon.Data.Services;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Data.MySql.Modules;

internal sealed class MySqlDataModule(MySqlDataOptions options)
    : ModuleBase,
        ICdcCaptureContributor,
        IExecutionGraphContributor,
        IHostedExecutionContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "mysql-data",
        displayName: "MySQL Data",
        description: "MySQL provider-native binlog CDC registration for Cephalon data workloads.",
        tags: ["data", "mysql", "relational"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "mysql-data"
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
            services.TryAddSingleton<IMySqlBinlogTransport>(serviceProvider =>
            {
                var configuration = serviceProvider.GetService<IConfiguration>();
                var connectionString = ResolveConnectionString(configuration);
                var logger = serviceProvider.GetRequiredService<ILogger<MySqlBinlogTransport>>();
                return new MySqlBinlogTransport(connectionString, options, logger);
            });
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ICdcCaptureExecutionRuntimeContributor, MySqlBinlogExecutionRuntimeContributor>());
            services.AddHostedService<MySqlBinlogCaptureHostedService>();
        }
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: "data.mysql",
            displayName: "MySQL Data Provider",
            description: "Registers MySQL provider-native binlog CDC wiring for Cephalon data workloads.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.MySql",
                ["provider"] = MySqlDataOptions.ProviderId,
                ["database"] = options.DatabaseName.Trim()
            }));

        capabilities.Add(new Capability(
            key: "data.relational-store",
            displayName: "Relational Store",
            description: "The active data provider integrates with a relational database.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.MySql",
                ["provider"] = MySqlDataOptions.ProviderId
            }));

        if (options.CdcCaptures.Count > 0)
        {
            capabilities.Add(new Capability(
                key: "data.cdc.mysql",
                displayName: "MySQL Binlog CDC",
                description: "Runs provider-native MySQL binlog captures and stages durable publications through the linked outbox path.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.MySql",
                    ["executionRuntimeId"] = MySqlDataRuntimeIds.CdcExecutionRuntimeId,
                    ["hostedExecutionId"] = MySqlDataRuntimeIds.CdcHostedExecutionId,
                    ["executionGraphId"] = MySqlDataRuntimeIds.CdcExecutionGraphId
                }));
        }
    }

    public void RegisterCdcCaptures(ICdcCaptureRegistry cdcCaptures)
    {
        ArgumentNullException.ThrowIfNull(cdcCaptures);

        foreach (var capture in options.CdcCaptures)
        {
            var id = capture.Id.Trim();
            var normalizedSchema = ResolveTableSchema(capture);
            var sourcePath = CreateSourcePath(capture, normalizedSchema);
            var sourceId = string.IsNullOrWhiteSpace(capture.SourceId)
                ? $"{MySqlDataOptions.ProviderId}:{sourcePath}"
                : capture.SourceId.Trim();
            var metadata = new Dictionary<string, string>(capture.Metadata, StringComparer.OrdinalIgnoreCase)
            {
                ["provider"] = MySqlDataOptions.ProviderId,
                ["databaseName"] = options.DatabaseName.Trim(),
                ["tableSchema"] = normalizedSchema,
                ["tableName"] = capture.TableName.Trim(),
                ["serverId"] = capture.ServerId.ToString(CultureInfo.InvariantCulture),
                ["channelId"] = capture.ChannelId.Trim(),
                ["messageType"] = capture.MessageType.Trim(),
                ["initialPosition"] = capture.InitialPosition.Trim(),
                ["maxChangesPerRead"] = capture.MaxChangesPerRead.ToString(CultureInfo.InvariantCulture),
                ["maxAwaitTimeSeconds"] = capture.MaxAwaitTimeSeconds.ToString(CultureInfo.InvariantCulture),
                ["pollingIntervalSeconds"] = capture.PollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                ["checkpointStore"] = $"{options.DatabaseName.Trim()}.{options.CheckpointTableName.Trim()}",
                ["binlogCheckpointSource"] = "cephalon-checkpoint-table",
                ["binlogResumeMode"] = "checkpoint-or-initial-position",
                ["binlogLifecyclePolicy"] = "checkpoint-validation",
                ["sourceServerIdentityMode"] = string.IsNullOrWhiteSpace(capture.ExpectedSourceServerUuid)
                    ? "observe-only"
                    : "configured-uuid-match",
                ["gtidMetadataMode"] = "observe-only",
                ["executionRuntimeId"] = MySqlDataRuntimeIds.CdcExecutionRuntimeId,
                ["contributorModuleId"] = Descriptor.Id
            };
            if (!string.IsNullOrWhiteSpace(capture.ExpectedSourceServerUuid))
            {
                metadata["expectedSourceServerUuid"] = capture.ExpectedSourceServerUuid.Trim();
            }

            var resourceIds = capture.ResourceIds.Count == 0
                ? [CreateDefaultResourceId(normalizedSchema, capture.TableName.Trim())]
                : capture.ResourceIds.ToArray();

            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: id,
                displayName: string.IsNullOrWhiteSpace(capture.DisplayName) ? id : capture.DisplayName.Trim(),
                description: string.IsNullOrWhiteSpace(capture.Description)
                    ? $"Reads MySQL binlog row events for table '{normalizedSchema}.{capture.TableName.Trim()}' and stages publications through outbox '{capture.OutboxId.Trim()}'."
                    : capture.Description.Trim(),
                sourceModuleId: capture.SourceModuleId.Trim(),
                provider: MySqlDataOptions.ProviderId,
                sourceId: sourceId,
                outboxId: capture.OutboxId.Trim(),
                executionBinding: new CdcCaptureExecutionBindingDescriptor(
                    cdcCaptureId: id,
                    authoredExecutionRuntimeId: MySqlDataRuntimeIds.CdcExecutionRuntimeId,
                    requestedExecutionRuntimeId: MySqlDataRuntimeIds.CdcExecutionRuntimeId),
                mode: "binlog",
                eventFormat: string.IsNullOrWhiteSpace(capture.EventFormat)
                    ? "mysql-binlog-row-event"
                    : capture.EventFormat.Trim(),
                resourceIds: resourceIds,
                tags: capture.Tags.Count == 0
                    ? ["cdc", "mysql", "provider-native"]
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
            id: MySqlDataRuntimeIds.CdcExecutionGraphId,
            displayName: "MySQL Binlog Capture Flow",
            description: "Resolves provider-native MySQL binlog capture declarations, reads one bounded binlog batch, stages linked outbox publications, persists durable binlog checkpoints after stage success, and reports runtime observations.",
            sourceModuleId: Descriptor.Id,
            entryNodeId: "resolve-mysql-cdc-captures",
            nodes:
            [
                new ExecutionGraphNodeDescriptor(
                    id: "resolve-mysql-cdc-captures",
                    displayName: "Resolve MySQL CDC Captures",
                    description: "Resolves active MySQL binlog captures and their linked outbox bindings.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.mysql",
                    tags: ["data", "cdc", "mysql"]),
                new ExecutionGraphNodeDescriptor(
                    id: "read-mysql-binlog-batch",
                    displayName: "Read MySQL Binlog Batch",
                    description: "Reads one bounded provider-native MySQL binlog row-event batch for the configured table.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.mysql",
                    tags: ["data", "cdc", "mysql", "binlog"]),
                new ExecutionGraphNodeDescriptor(
                    id: "stage-mysql-cdc-publications",
                    displayName: "Stage MySQL CDC Publications",
                    description: "Stages the captured MySQL binlog changes through the linked outbox implementation.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.mysql",
                    tags: ["data", "cdc", "mysql", "outbox"]),
                new ExecutionGraphNodeDescriptor(
                    id: "persist-mysql-binlog-checkpoint",
                    displayName: "Persist MySQL Binlog Checkpoint",
                    description: "Persists the latest durable MySQL binlog checkpoint only after the linked outbox publications were staged successfully.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.mysql",
                    tags: ["data", "cdc", "mysql", "checkpoint"]),
                new ExecutionGraphNodeDescriptor(
                    id: "report-mysql-cdc-runtime-observation",
                    displayName: "Report MySQL CDC Runtime Observation",
                    description: "Projects the latest provider-native MySQL binlog posture into the shared CDC runtime-state catalog.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.mysql",
                    tags: ["data", "cdc", "mysql", "runtime-state"])
            ],
            edges:
            [
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "resolve-mysql-cdc-captures",
                    toNodeId: "read-mysql-binlog-batch",
                    displayName: "resolved"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "read-mysql-binlog-batch",
                    toNodeId: "stage-mysql-cdc-publications",
                    displayName: "captured"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "stage-mysql-cdc-publications",
                    toNodeId: "persist-mysql-binlog-checkpoint",
                    displayName: "staged"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "persist-mysql-binlog-checkpoint",
                    toNodeId: "report-mysql-cdc-runtime-observation",
                    displayName: "reported")
            ],
            tags: ["data", "cdc", "mysql"],
            metadata: new Dictionary<string, string>
            {
                ["surface"] = "mysql-cdc"
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
            id: MySqlDataRuntimeIds.CdcHostedExecutionId,
            displayName: "MySQL Binlog Capture Pump",
            description: "Runs the provider-native MySQL binlog background pump for configured captures.",
            sourceModuleId: Descriptor.Id,
            kind: "background-service",
            executionGraphId: MySqlDataRuntimeIds.CdcExecutionGraphId,
            startsWithHost: true,
            tags: ["data", "cdc", "mysql"],
            metadata: new Dictionary<string, string>
            {
                ["surface"] = "mysql-cdc"
            }));
    }

    private string ResolveConnectionString(IConfiguration? configuration)
    {
        var connectionString = ConnectionStringResolution.Resolve(
            configuration,
            options.ConnectionString,
            options.ConnectionStringName,
            string.Empty,
            MySqlDataOptions.SectionPath,
            "MySQL");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{MySqlDataOptions.SectionPath} must configure either ConnectionStringName or ConnectionString before MySQL binlog CDC can start.");
        }

        return connectionString.Trim();
    }

    private string ResolveTableSchema(MySqlBinlogCaptureOptions capture)
    {
        return string.IsNullOrWhiteSpace(capture.TableSchema)
            ? options.DatabaseName.Trim()
            : capture.TableSchema.Trim();
    }

    private string CreateSourcePath(MySqlBinlogCaptureOptions capture, string normalizedSchema)
    {
        return string.Equals(normalizedSchema, options.DatabaseName.Trim(), StringComparison.OrdinalIgnoreCase)
            ? $"{normalizedSchema}/{capture.TableName.Trim()}"
            : $"{options.DatabaseName.Trim()}/{normalizedSchema}.{capture.TableName.Trim()}";
    }

    private static string CreateDefaultResourceId(string normalizedSchema, string tableName)
    {
        return $"{normalizedSchema}.{tableName}";
    }
}
