using Cephalon.Abstractions.Data;
using Cephalon.Data.Oracle.Configuration;
using Cephalon.Data.Services;

namespace Cephalon.Data.Oracle.Services;

internal sealed class OracleLogMinerExecutionRuntimeContributor(OracleDataOptions options)
    : ICdcCaptureExecutionRuntimeContributor
{
    public void RegisterExecutionRuntimes(ICdcCaptureExecutionRuntimeRegistry executionRuntimes)
    {
        ArgumentNullException.ThrowIfNull(executionRuntimes);

        if (options.CdcCaptures.Count == 0)
        {
            return;
        }

        executionRuntimes.Add(new CdcCaptureExecutionRuntimeDescriptor(
            id: OracleDataRuntimeIds.CdcExecutionRuntimeId,
            displayName: "Oracle LogMiner Capture Pump",
            description: "Runs the provider-native Oracle LogMiner background pump that mines bounded committed redo batches, stages outbox publications, and persists durable LogMiner checkpoints after stage success.",
            executionOwnership: "host-managed",
            executionTopology: "provider-native",
            acknowledgementMode: "provider-native",
            hostedExecutionId: OracleDataRuntimeIds.CdcHostedExecutionId,
            executionGraphId: OracleDataRuntimeIds.CdcExecutionGraphId,
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pack"] = "Cephalon.Data.Oracle",
                ["provider"] = OracleDataOptions.ProviderId,
                ["surface"] = "oracle-cdc",
                ["databaseName"] = options.DatabaseName.Trim(),
                ["checkpointStore"] = options.CheckpointTableName.Trim(),
                ["logMinerDictionary"] = "online-catalog",
                ["logMinerMode"] = "committed-only",
                ["redoCursor"] = "commit-scn|change-scn|rs-id|ssn"
            },
            cdcCaptureIds: options.CdcCaptures
                .Where(static capture => !string.IsNullOrWhiteSpace(capture.Id))
                .Select(static capture => capture.Id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray()));
    }
}
