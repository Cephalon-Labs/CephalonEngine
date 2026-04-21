using System.Globalization;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Data.Services;
using Cephalon.Data.SqlServer.Configuration;
using Cephalon.Data.SqlServer.Services;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Data.SqlServer.Modules;

internal sealed class SqlServerDataModule(SqlServerDataOptions options)
    : ModuleBase,
        ICdcCaptureContributor,
        IExecutionGraphContributor,
        IHostedExecutionContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "sqlserver-data",
        displayName: "SQL Server Data",
        description: "SQL Server provider-native CDC registration for Cephalon data workloads.",
        tags: ["data", "sqlserver", "relational"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "sqlserver-data"
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
            services.TryAddSingleton<ISqlServerCdcTransport>(serviceProvider =>
            {
                var configuration = serviceProvider.GetService<IConfiguration>();
                var connectionString = ResolveConnectionString(configuration);
                var logger = serviceProvider.GetRequiredService<ILogger<SqlServerCdcTransport>>();
                return new SqlServerCdcTransport(connectionString, options, logger);
            });
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ICdcCaptureExecutionRuntimeContributor, SqlServerCdcExecutionRuntimeContributor>());
            services.AddHostedService<SqlServerCdcCaptureHostedService>();
        }
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: "data.sqlserver",
            displayName: "SQL Server Data Provider",
            description: "Registers SQL Server provider-native CDC wiring for Cephalon data workloads.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.SqlServer",
                ["provider"] = SqlServerDataOptions.ProviderId,
                ["database"] = options.DatabaseName.Trim()
            }));

        capabilities.Add(new Capability(
            key: "data.relational-store",
            displayName: "Relational Store",
            description: "The active data provider integrates with a relational database.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.SqlServer",
                ["provider"] = SqlServerDataOptions.ProviderId
            }));

        if (options.CdcCaptures.Count > 0)
        {
            capabilities.Add(new Capability(
                key: "data.cdc.sqlserver",
                displayName: "SQL Server CDC",
                description: "Runs provider-native SQL Server CDC captures and stages durable publications through the linked outbox path.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.SqlServer",
                    ["executionRuntimeId"] = SqlServerDataRuntimeIds.CdcExecutionRuntimeId,
                    ["hostedExecutionId"] = SqlServerDataRuntimeIds.CdcHostedExecutionId,
                    ["executionGraphId"] = SqlServerDataRuntimeIds.CdcExecutionGraphId
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
                ? $"{SqlServerDataOptions.ProviderId}:{options.DatabaseName.Trim()}/{capture.TableSchema.Trim()}.{capture.TableName.Trim()}"
                : capture.SourceId.Trim();
            var metadata = new Dictionary<string, string>(capture.Metadata, StringComparer.OrdinalIgnoreCase)
            {
                ["provider"] = SqlServerDataOptions.ProviderId,
                ["databaseName"] = options.DatabaseName.Trim(),
                ["tableSchema"] = capture.TableSchema.Trim(),
                ["tableName"] = capture.TableName.Trim(),
                ["captureInstance"] = capture.CaptureInstance.Trim(),
                ["channelId"] = capture.ChannelId.Trim(),
                ["messageType"] = capture.MessageType.Trim(),
                ["initialPosition"] = capture.InitialPosition.Trim(),
                ["maxChangesPerPoll"] = capture.MaxChangesPerPoll.ToString(CultureInfo.InvariantCulture),
                ["pollingIntervalSeconds"] = capture.PollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                ["checkpointStore"] = $"{options.CheckpointTableSchema.Trim()}.{options.CheckpointTableName.Trim()}",
                ["executionRuntimeId"] = SqlServerDataRuntimeIds.CdcExecutionRuntimeId,
                ["contributorModuleId"] = Descriptor.Id
            };

            var resourceIds = capture.ResourceIds.Count == 0
                ? [$"{options.DatabaseName.Trim()}.{capture.TableSchema.Trim()}.{capture.TableName.Trim()}"]
                : capture.ResourceIds.ToArray();

            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: id,
                displayName: string.IsNullOrWhiteSpace(capture.DisplayName) ? id : capture.DisplayName.Trim(),
                description: string.IsNullOrWhiteSpace(capture.Description)
                    ? $"Reads SQL Server CDC capture instance '{capture.CaptureInstance.Trim()}' for table '{options.DatabaseName.Trim()}.{capture.TableSchema.Trim()}.{capture.TableName.Trim()}' and stages publications through outbox '{capture.OutboxId.Trim()}'."
                    : capture.Description.Trim(),
                sourceModuleId: capture.SourceModuleId.Trim(),
                provider: SqlServerDataOptions.ProviderId,
                sourceId: sourceId,
                outboxId: capture.OutboxId.Trim(),
                executionBinding: new CdcCaptureExecutionBindingDescriptor(
                    cdcCaptureId: id,
                    authoredExecutionRuntimeId: SqlServerDataRuntimeIds.CdcExecutionRuntimeId,
                    requestedExecutionRuntimeId: SqlServerDataRuntimeIds.CdcExecutionRuntimeId),
                mode: "change-table",
                eventFormat: string.IsNullOrWhiteSpace(capture.EventFormat)
                    ? "sqlserver-cdc-change-table-event"
                    : capture.EventFormat.Trim(),
                resourceIds: resourceIds,
                tags: capture.Tags.Count == 0
                    ? ["cdc", "sqlserver", "provider-native"]
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
            id: SqlServerDataRuntimeIds.CdcExecutionGraphId,
            displayName: "SQL Server CDC Capture Flow",
            description: "Resolves provider-native SQL Server CDC capture declarations, reads one bounded CDC batch, stages linked outbox publications, persists SQL LSN checkpoints, and reports runtime observations.",
            sourceModuleId: Descriptor.Id,
            entryNodeId: "resolve-sqlserver-cdc-captures",
            nodes:
            [
                new ExecutionGraphNodeDescriptor(
                    id: "resolve-sqlserver-cdc-captures",
                    displayName: "Resolve SQL Server CDC Captures",
                    description: "Resolves active SQL Server CDC captures and their linked outbox bindings.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.sqlserver",
                    tags: ["data", "cdc", "sqlserver"]),
                new ExecutionGraphNodeDescriptor(
                    id: "read-sqlserver-cdc-batch",
                    displayName: "Read SQL Server CDC Batch",
                    description: "Reads one bounded provider-native batch from the configured SQL Server CDC capture instance.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.sqlserver",
                    tags: ["data", "cdc", "sqlserver", "change-table"]),
                new ExecutionGraphNodeDescriptor(
                    id: "stage-sqlserver-cdc-publications",
                    displayName: "Stage SQL Server CDC Publications",
                    description: "Stages the captured SQL Server CDC rows through the linked outbox implementation.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.sqlserver",
                    tags: ["data", "cdc", "sqlserver", "outbox"]),
                new ExecutionGraphNodeDescriptor(
                    id: "persist-sqlserver-cdc-checkpoint",
                    displayName: "Persist SQL Server CDC Checkpoint",
                    description: "Persists the latest durable SQL Server CDC checkpoint only after the linked outbox publications were staged successfully.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.sqlserver",
                    tags: ["data", "cdc", "sqlserver", "checkpoint"]),
                new ExecutionGraphNodeDescriptor(
                    id: "report-sqlserver-cdc-runtime-observation",
                    displayName: "Report SQL Server CDC Runtime Observation",
                    description: "Projects the latest provider-native SQL Server CDC posture into the shared CDC runtime-state catalog.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.sqlserver",
                    tags: ["data", "cdc", "sqlserver", "runtime-state"])
            ],
            edges:
            [
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "resolve-sqlserver-cdc-captures",
                    toNodeId: "read-sqlserver-cdc-batch",
                    displayName: "resolved"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "read-sqlserver-cdc-batch",
                    toNodeId: "stage-sqlserver-cdc-publications",
                    displayName: "captured"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "stage-sqlserver-cdc-publications",
                    toNodeId: "persist-sqlserver-cdc-checkpoint",
                    displayName: "staged"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "persist-sqlserver-cdc-checkpoint",
                    toNodeId: "report-sqlserver-cdc-runtime-observation",
                    displayName: "reported")
            ],
            tags: ["data", "cdc", "sqlserver"],
            metadata: new Dictionary<string, string>
            {
                ["surface"] = "sqlserver-cdc"
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
            id: SqlServerDataRuntimeIds.CdcHostedExecutionId,
            displayName: "SQL Server CDC Capture Pump",
            description: "Runs the provider-native SQL Server CDC background pump for configured capture instances.",
            sourceModuleId: Descriptor.Id,
            kind: "background-service",
            executionGraphId: SqlServerDataRuntimeIds.CdcExecutionGraphId,
            startsWithHost: true,
            tags: ["data", "cdc", "sqlserver"],
            metadata: new Dictionary<string, string>
            {
                ["surface"] = "sqlserver-cdc"
            }));
    }

    private string ResolveConnectionString(IConfiguration? configuration)
    {
        var connectionString = ConnectionStringResolution.Resolve(
            configuration,
            options.ConnectionString,
            options.ConnectionStringName,
            string.Empty,
            SqlServerDataOptions.SectionPath,
            "SQL Server");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{SqlServerDataOptions.SectionPath} must configure either ConnectionStringName or ConnectionString before SQL Server CDC can start.");
        }

        return connectionString.Trim();
    }
}
