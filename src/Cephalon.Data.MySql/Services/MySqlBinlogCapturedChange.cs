using Cephalon.Abstractions.Data;

namespace Cephalon.Data.MySql.Services;

internal sealed class MySqlBinlogCapturedChange(
    string changeId,
    string operationName,
    MySqlBinlogCheckpointToken checkpointToken,
    OutboxMessage message)
{
    public string ChangeId { get; } = changeId;

    public string OperationName { get; } = operationName;

    public MySqlBinlogCheckpointToken CheckpointToken { get; } = checkpointToken;

    public OutboxMessage Message { get; } = message;
}
