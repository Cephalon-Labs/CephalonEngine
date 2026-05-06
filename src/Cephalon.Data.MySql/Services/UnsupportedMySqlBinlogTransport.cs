using Cephalon.Abstractions.Data;
using Cephalon.Data.MySql.Configuration;

namespace Cephalon.Data.MySql.Services;

internal sealed class UnsupportedMySqlBinlogTransport : IMySqlBinlogTransport
{
    private const string AdapterPackageId = "Cephalon.Data.MySql.SciSharpReplication";

    public Task<MySqlBinlogReadBatch> ReadBatchAsync(
        MySqlBinlogCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captureOptions);
        ArgumentNullException.ThrowIfNull(descriptor);

        throw CreateMissingAdapterException(captureOptions, descriptor, "read");
    }

    public Task CommitCheckpointAsync(
        MySqlBinlogCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        MySqlBinlogCheckpointToken checkpointToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captureOptions);
        ArgumentNullException.ThrowIfNull(descriptor);

        throw CreateMissingAdapterException(captureOptions, descriptor, "commit-checkpoint");
    }

    private static MySqlBinlogCaptureException CreateMissingAdapterException(
        MySqlBinlogCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        string operation)
    {
        return new MySqlBinlogCaptureException(
            $"MySQL binlog capture '{captureOptions.Id}' requires an installed binlog transport adapter. Reference '{AdapterPackageId}' and call AddSciSharpMySqlBinlogReplication(...) after AddMySqlData(...).",
            "binlog-transport-adapter-missing",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["adapterPackage"] = AdapterPackageId,
                ["captureId"] = captureOptions.Id.Trim(),
                ["cdcCaptureId"] = descriptor.Id,
                ["operation"] = operation,
                ["provider"] = MySqlDataOptions.ProviderId,
                ["transportAdapter"] = "missing"
            });
    }
}
