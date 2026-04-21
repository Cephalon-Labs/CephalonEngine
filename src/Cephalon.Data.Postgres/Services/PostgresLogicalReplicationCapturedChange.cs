using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Postgres.Services;

internal sealed class PostgresLogicalReplicationCapturedChange(
    string changeId,
    string operationName,
    PostgresLogicalReplicationCheckpointToken checkpointToken,
    OutboxMessage message)
{
    public string ChangeId { get; } = changeId;

    public string OperationName { get; } = operationName;

    public PostgresLogicalReplicationCheckpointToken CheckpointToken { get; } = checkpointToken;

    public OutboxMessage Message { get; } = message;
}
