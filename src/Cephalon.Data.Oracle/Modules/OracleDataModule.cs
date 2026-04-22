using System.Globalization;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Data.Oracle.Configuration;
using Cephalon.Data.Oracle.Services;
using Cephalon.Data.Services;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Data.Oracle.Modules;

internal sealed class OracleDataModule(OracleDataOptions options)
    : ModuleBase,
        ICdcCaptureContributor,
        IExecutionGraphContributor,
        IHostedExecutionContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "oracle-data",
        displayName: "Oracle Data",
        description: "Oracle provider-native LogMiner CDC registration for Cephalon data workloads.",
        tags: ["data", "oracle", "relational"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "oracle-data"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(options);

        if (options.CdcCaptures.Count > 0)
        {
            services.TryAddSingleton<IOracleLogMinerTransport>(serviceProvider =>
            {
                var configuration = serviceProvider.GetService<IConfiguration>();
                var connectionString = ResolveConnectionString(configuration);
                var logger = serviceProvider.GetRequiredService<ILogger<OracleLogMinerTransport>>();
                return new OracleLogMinerTransport(connectionString, options, logger);
            });
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ICdcCaptureExecutionRuntimeContributor, OracleLogMinerExecutionRuntimeContributor>());
            services.AddHostedService<OracleLogMinerCaptureHostedService>();
        }
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: "data.oracle",
            displayName: "Oracle Data Provider",
            description: "Registers Oracle provider-native LogMiner CDC wiring for Cephalon data workloads.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Oracle",
                ["provider"] = OracleDataOptions.ProviderId,
                ["database"] = options.DatabaseName.Trim()
            }));

        capabilities.Add(new Capability(
            key: "data.relational-store",
            displayName: "Relational Store",
            description: "The active data provider integrates with a relational database.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Oracle",
                ["provider"] = OracleDataOptions.ProviderId
            }));

        if (options.CdcCaptures.Count > 0)
        {
            capabilities.Add(new Capability(
                key: "data.cdc.oracle",
                displayName: "Oracle LogMiner CDC",
                description: "Runs provider-native Oracle LogMiner captures and stages durable publications through the linked outbox path.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.Oracle",
                    ["executionRuntimeId"] = OracleDataRuntimeIds.CdcExecutionRuntimeId,
                    ["hostedExecutionId"] = OracleDataRuntimeIds.CdcHostedExecutionId,
                    ["executionGraphId"] = OracleDataRuntimeIds.CdcExecutionGraphId
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
            var normalizedTableName = capture.TableName.Trim();
            var sourcePath = $"{options.DatabaseName.Trim()}/{normalizedSchema}.{normalizedTableName}";
            var sourceId = string.IsNullOrWhiteSpace(capture.SourceId)
                ? $"{OracleDataOptions.ProviderId}:{sourcePath}"
                : capture.SourceId.Trim();
            var metadata = new Dictionary<string, string>(capture.Metadata, StringComparer.OrdinalIgnoreCase)
            {
                ["provider"] = OracleDataOptions.ProviderId,
                ["databaseName"] = options.DatabaseName.Trim(),
                ["tableSchema"] = normalizedSchema,
                ["tableName"] = normalizedTableName,
                ["channelId"] = capture.ChannelId.Trim(),
                ["messageType"] = capture.MessageType.Trim(),
                ["initialPosition"] = capture.InitialPosition.Trim(),
                ["maxChangesPerRead"] = capture.MaxChangesPerRead.ToString(CultureInfo.InvariantCulture),
                ["maxAwaitTimeSeconds"] = capture.MaxAwaitTimeSeconds.ToString(CultureInfo.InvariantCulture),
                ["pollingIntervalSeconds"] = capture.PollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
                ["checkpointStore"] = options.CheckpointTableName.Trim(),
                ["logMinerDictionary"] = "online-catalog",
                ["logMinerMode"] = "committed-only",
                ["redoCursor"] = "commit-scn|change-scn|rs-id|ssn",
                ["resumeFromEarliestAvailableScnIfCheckpointUnavailable"] =
                    capture.ResumeFromEarliestAvailableScnIfCheckpointUnavailable ? "true" : "false",
                ["archiveLogLifecyclePolicy"] = capture.ResumeFromEarliestAvailableScnIfCheckpointUnavailable
                    ? "resume-from-earliest-available-when-checkpoint-unavailable"
                    : "fail-when-checkpoint-unavailable",
                ["executionRuntimeId"] = OracleDataRuntimeIds.CdcExecutionRuntimeId,
                ["contributorModuleId"] = Descriptor.Id
            };

            if (capture.ExpectedDatabaseId.HasValue)
            {
                metadata["expectedDatabaseId"] = capture.ExpectedDatabaseId.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(capture.ExpectedDatabaseUniqueName))
            {
                metadata["expectedDatabaseUniqueName"] = capture.ExpectedDatabaseUniqueName.Trim();
            }

            var resourceIds = capture.ResourceIds.Count == 0
                ? [CreateDefaultResourceId(normalizedSchema, normalizedTableName)]
                : capture.ResourceIds.ToArray();

            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: id,
                displayName: string.IsNullOrWhiteSpace(capture.DisplayName) ? id : capture.DisplayName.Trim(),
                description: string.IsNullOrWhiteSpace(capture.Description)
                    ? $"Reads Oracle LogMiner change rows for table '{normalizedSchema}.{normalizedTableName}' and stages publications through outbox '{capture.OutboxId.Trim()}'."
                    : capture.Description.Trim(),
                sourceModuleId: capture.SourceModuleId.Trim(),
                provider: OracleDataOptions.ProviderId,
                sourceId: sourceId,
                outboxId: capture.OutboxId.Trim(),
                executionBinding: new CdcCaptureExecutionBindingDescriptor(
                    cdcCaptureId: id,
                    authoredExecutionRuntimeId: OracleDataRuntimeIds.CdcExecutionRuntimeId,
                    requestedExecutionRuntimeId: OracleDataRuntimeIds.CdcExecutionRuntimeId),
                mode: "logminer",
                eventFormat: string.IsNullOrWhiteSpace(capture.EventFormat)
                    ? "oracle-logminer-redo-event"
                    : capture.EventFormat.Trim(),
                resourceIds: resourceIds,
                tags: capture.Tags.Count == 0
                    ? ["cdc", "oracle", "provider-native"]
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
            id: OracleDataRuntimeIds.CdcExecutionGraphId,
            displayName: "Oracle LogMiner Capture Flow",
            description: "Resolves provider-native Oracle LogMiner capture declarations, mines one bounded committed redo batch, stages linked outbox publications, persists durable LogMiner checkpoints after stage success, and reports runtime observations.",
            sourceModuleId: Descriptor.Id,
            entryNodeId: "resolve-oracle-cdc-captures",
            nodes:
            [
                new ExecutionGraphNodeDescriptor(
                    id: "resolve-oracle-cdc-captures",
                    displayName: "Resolve Oracle CDC Captures",
                    description: "Resolves active Oracle LogMiner captures and their linked outbox bindings.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.oracle",
                    tags: ["data", "cdc", "oracle"]),
                new ExecutionGraphNodeDescriptor(
                    id: "read-oracle-logminer-batch",
                    displayName: "Read Oracle LogMiner Batch",
                    description: "Reads one bounded provider-native Oracle LogMiner batch for the configured table.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.oracle",
                    tags: ["data", "cdc", "oracle", "logminer"]),
                new ExecutionGraphNodeDescriptor(
                    id: "stage-oracle-cdc-publications",
                    displayName: "Stage Oracle CDC Publications",
                    description: "Stages the captured Oracle LogMiner changes through the linked outbox implementation.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.oracle",
                    tags: ["data", "cdc", "oracle", "outbox"]),
                new ExecutionGraphNodeDescriptor(
                    id: "persist-oracle-logminer-checkpoint",
                    displayName: "Persist Oracle LogMiner Checkpoint",
                    description: "Persists the latest durable Oracle LogMiner checkpoint only after the linked outbox publications were staged successfully.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.oracle",
                    tags: ["data", "cdc", "oracle", "checkpoint"]),
                new ExecutionGraphNodeDescriptor(
                    id: "report-oracle-cdc-runtime-observation",
                    displayName: "Report Oracle CDC Runtime Observation",
                    description: "Projects the latest provider-native Oracle LogMiner posture into the shared CDC runtime-state catalog.",
                    kind: "activity",
                    moduleId: Descriptor.Id,
                    capabilityKey: "data.cdc.oracle",
                    tags: ["data", "cdc", "oracle", "runtime-state"])
            ],
            edges:
            [
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "resolve-oracle-cdc-captures",
                    toNodeId: "read-oracle-logminer-batch",
                    displayName: "resolved"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "read-oracle-logminer-batch",
                    toNodeId: "stage-oracle-cdc-publications",
                    displayName: "captured"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "stage-oracle-cdc-publications",
                    toNodeId: "persist-oracle-logminer-checkpoint",
                    displayName: "staged"),
                new ExecutionGraphEdgeDescriptor(
                    fromNodeId: "persist-oracle-logminer-checkpoint",
                    toNodeId: "report-oracle-cdc-runtime-observation",
                    displayName: "reported")
            ],
            tags: ["data", "cdc", "oracle"],
            metadata: new Dictionary<string, string>
            {
                ["surface"] = "oracle-cdc"
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
            id: OracleDataRuntimeIds.CdcHostedExecutionId,
            displayName: "Oracle LogMiner Capture Pump",
            description: "Runs the provider-native Oracle LogMiner background pump for configured captures.",
            sourceModuleId: Descriptor.Id,
            kind: "background-service",
            executionGraphId: OracleDataRuntimeIds.CdcExecutionGraphId,
            startsWithHost: true,
            tags: ["data", "cdc", "oracle"],
            metadata: new Dictionary<string, string>
            {
                ["surface"] = "oracle-cdc"
            }));
    }

    private string ResolveConnectionString(IConfiguration? configuration)
    {
        var connectionString = ConnectionStringResolution.Resolve(
            configuration,
            options.ConnectionString,
            options.ConnectionStringName,
            string.Empty,
            OracleDataOptions.SectionPath,
            "Oracle");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{OracleDataOptions.SectionPath} must configure either ConnectionStringName or ConnectionString before Oracle LogMiner CDC can start.");
        }

        return connectionString.Trim();
    }

    private static string ResolveTableSchema(OracleLogMinerCaptureOptions capture)
    {
        if (string.IsNullOrWhiteSpace(capture.TableSchema))
        {
            throw new InvalidOperationException(
                $"Oracle LogMiner capture '{capture.Id}' must set TableSchema because Oracle CDC ownership is schema-specific.");
        }

        return capture.TableSchema.Trim();
    }

    private static string CreateDefaultResourceId(string normalizedSchema, string tableName)
    {
        return $"{normalizedSchema}.{tableName}";
    }
}
