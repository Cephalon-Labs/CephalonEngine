using Cephalon.Abstractions.Data;
using Cephalon.Data.Postgres.Configuration;

namespace Cephalon.Data.Postgres.Services;

internal interface IPostgresLogicalReplicationTransport
{
    Task<PostgresLogicalReplicationReadBatch> ReadBatchAsync(
        PostgresLogicalReplicationCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        CancellationToken cancellationToken);

    Task CommitCheckpointAsync(
        PostgresLogicalReplicationCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        PostgresLogicalReplicationCheckpointToken checkpointToken,
        CancellationToken cancellationToken);

    Task AbandonPendingBatchAsync(
        PostgresLogicalReplicationCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        CancellationToken cancellationToken);
}
