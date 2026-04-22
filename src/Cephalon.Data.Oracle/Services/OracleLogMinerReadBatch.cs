namespace Cephalon.Data.Oracle.Services;

internal sealed class OracleLogMinerReadBatch(
    IReadOnlyList<OracleLogMinerCapturedChange> changes,
    bool hasMoreChanges,
    IReadOnlyDictionary<string, string> metadata)
{
    public IReadOnlyList<OracleLogMinerCapturedChange> Changes { get; } = changes;

    public bool HasMoreChanges { get; } = hasMoreChanges;

    public IReadOnlyDictionary<string, string> Metadata { get; } = metadata;

    public static OracleLogMinerReadBatch Idle(IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new OracleLogMinerReadBatch(
            [],
            hasMoreChanges: false,
            metadata is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
    }
}
