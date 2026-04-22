using Cephalon.Abstractions.Data;
using Cephalon.Data.Oracle.Configuration;

namespace Cephalon.Data.Oracle.Services;

internal interface IOracleLogMinerTransport
{
    Task<OracleLogMinerReadBatch> ReadBatchAsync(
        OracleLogMinerCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        CancellationToken cancellationToken);

    Task CommitCheckpointAsync(
        OracleLogMinerCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        OracleLogMinerCheckpointToken checkpointToken,
        CancellationToken cancellationToken);
}
