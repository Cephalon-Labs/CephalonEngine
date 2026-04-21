using Cephalon.Abstractions.Data;

namespace Cephalon.Data.SqlServer.Services;

internal sealed class SqlServerCdcCapturedChange(
    string changeId,
    string operationName,
    SqlServerCdcCheckpointToken checkpointToken,
    OutboxMessage message)
{
    public string ChangeId { get; } = changeId;

    public string OperationName { get; } = operationName;

    public SqlServerCdcCheckpointToken CheckpointToken { get; } = checkpointToken;

    public OutboxMessage Message { get; } = message;
}
