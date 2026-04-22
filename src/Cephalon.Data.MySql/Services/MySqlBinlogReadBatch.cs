namespace Cephalon.Data.MySql.Services;

internal sealed class MySqlBinlogReadBatch(
    IReadOnlyList<MySqlBinlogCapturedChange> changes,
    bool hasMoreChanges,
    IReadOnlyDictionary<string, string> metadata)
{
    public IReadOnlyList<MySqlBinlogCapturedChange> Changes { get; } = changes;

    public bool HasMoreChanges { get; } = hasMoreChanges;

    public IReadOnlyDictionary<string, string> Metadata { get; } = metadata;

    public static MySqlBinlogReadBatch Idle(IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new MySqlBinlogReadBatch(
            [],
            hasMoreChanges: false,
            metadata is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
    }
}
