using Cephalon.Abstractions.Data;
using Cephalon.Data.Services;
using Cephalon.Data.SqlServer.Configuration;

namespace Cephalon.Data.SqlServer.Services;

internal sealed class SqlServerCdcExecutionRuntimeContributor(SqlServerDataOptions options)
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
            id: SqlServerDataRuntimeIds.CdcExecutionRuntimeId,
            displayName: "SQL Server CDC Capture Pump",
            description: "Runs the provider-native SQL Server CDC background pump that polls configured capture instances, stages outbox publications, and durably persists SQL LSN checkpoints.",
            executionOwnership: "host-managed",
            executionTopology: "provider-native",
            acknowledgementMode: "provider-native",
            hostedExecutionId: SqlServerDataRuntimeIds.CdcHostedExecutionId,
            executionGraphId: SqlServerDataRuntimeIds.CdcExecutionGraphId,
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pack"] = "Cephalon.Data.SqlServer",
                ["provider"] = SqlServerDataOptions.ProviderId,
                ["surface"] = "sqlserver-cdc",
                ["databaseName"] = options.DatabaseName.Trim(),
                ["checkpointStore"] = $"{options.CheckpointTableSchema.Trim()}.{options.CheckpointTableName.Trim()}"
            },
            cdcCaptureIds: options.CdcCaptures
                .Where(static capture => !string.IsNullOrWhiteSpace(capture.Id))
                .Select(static capture => capture.Id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray()));
    }
}
