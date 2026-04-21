using Cephalon.Abstractions.Data;
using Cephalon.Data.Postgres.Configuration;
using Cephalon.Data.Services;

namespace Cephalon.Data.Postgres.Services;

internal sealed class PostgresLogicalReplicationExecutionRuntimeContributor(PostgresDataOptions options)
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
            id: PostgresDataRuntimeIds.CdcExecutionRuntimeId,
            displayName: "PostgreSQL Logical Replication Capture Pump",
            description: "Runs the provider-native PostgreSQL logical-replication background pump that streams configured publications, stages outbox publications, and confirms replication-slot progress after stage success.",
            executionOwnership: "host-managed",
            executionTopology: "provider-native",
            acknowledgementMode: "provider-native",
            hostedExecutionId: PostgresDataRuntimeIds.CdcHostedExecutionId,
            executionGraphId: PostgresDataRuntimeIds.CdcExecutionGraphId,
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pack"] = "Cephalon.Data.Postgres",
                ["provider"] = PostgresDataOptions.ProviderId,
                ["surface"] = "postgresql-cdc",
                ["databaseName"] = options.DatabaseName.Trim(),
                ["replicationCheckpointSource"] = "slot-confirmed-flush-lsn"
            },
            cdcCaptureIds: options.CdcCaptures
                .Where(static capture => !string.IsNullOrWhiteSpace(capture.Id))
                .Select(static capture => capture.Id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray()));
    }
}
