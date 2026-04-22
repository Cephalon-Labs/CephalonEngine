using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Oracle.Services;

internal sealed class OracleLogMinerCapturedChange(
    string changeId,
    string operationName,
    int operationCode,
    OracleLogMinerCheckpointToken checkpointToken,
    OutboxMessage message)
{
    public string ChangeId { get; } = changeId;

    public string OperationName { get; } = operationName;

    public int OperationCode { get; } = operationCode;

    public OracleLogMinerCheckpointToken CheckpointToken { get; } = checkpointToken;

    public OutboxMessage Message { get; } = message;
}
