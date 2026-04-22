using Cephalon.Abstractions.Data;
using Cephalon.Data.MySql.Configuration;
using Cephalon.Data.Services;

namespace Cephalon.Data.MySql.Services;

internal sealed class MySqlBinlogExecutionRuntimeContributor(MySqlDataOptions options)
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
            id: MySqlDataRuntimeIds.CdcExecutionRuntimeId,
            displayName: "MySQL Binlog Capture Pump",
            description: "Runs the provider-native MySQL binlog background pump that validates source-server identity plus binlog lifecycle posture, tails configured row events, stages outbox publications, and persists durable binlog checkpoints after stage success.",
            executionOwnership: "host-managed",
            executionTopology: "provider-native",
            acknowledgementMode: "provider-native",
            hostedExecutionId: MySqlDataRuntimeIds.CdcHostedExecutionId,
            executionGraphId: MySqlDataRuntimeIds.CdcExecutionGraphId,
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pack"] = "Cephalon.Data.MySql",
                ["provider"] = MySqlDataOptions.ProviderId,
                ["surface"] = "mysql-cdc",
                ["databaseName"] = options.DatabaseName.Trim(),
                ["binlogCheckpointSource"] = "cephalon-checkpoint-table",
                ["binlogLifecyclePolicy"] = "checkpoint-validation",
                ["sourceServerIdentityMode"] = options.CdcCaptures.Any(static capture => !string.IsNullOrWhiteSpace(capture.ExpectedSourceServerUuid))
                    ? "configured-or-observe"
                    : "observe-only",
                ["gtidMetadataMode"] = "observe-only"
            },
            cdcCaptureIds: options.CdcCaptures
                .Where(static capture => !string.IsNullOrWhiteSpace(capture.Id))
                .Select(static capture => capture.Id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray()));
    }
}
