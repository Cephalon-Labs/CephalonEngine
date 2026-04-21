using Cephalon.Abstractions.Data;
using Cephalon.Data.SqlServer.Configuration;

namespace Cephalon.Data.SqlServer.Services;

internal interface ISqlServerCdcTransport
{
    Task<SqlServerCdcReadBatch> ReadBatchAsync(
        SqlServerCdcCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        CancellationToken cancellationToken);

    Task CommitCheckpointAsync(
        SqlServerCdcCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        SqlServerCdcCheckpointToken checkpointToken,
        CancellationToken cancellationToken);
}
