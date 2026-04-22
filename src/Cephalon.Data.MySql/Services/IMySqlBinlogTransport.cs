using Cephalon.Abstractions.Data;
using Cephalon.Data.MySql.Configuration;

namespace Cephalon.Data.MySql.Services;

internal interface IMySqlBinlogTransport
{
    Task<MySqlBinlogReadBatch> ReadBatchAsync(
        MySqlBinlogCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        CancellationToken cancellationToken);

    Task CommitCheckpointAsync(
        MySqlBinlogCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        MySqlBinlogCheckpointToken checkpointToken,
        CancellationToken cancellationToken);
}
