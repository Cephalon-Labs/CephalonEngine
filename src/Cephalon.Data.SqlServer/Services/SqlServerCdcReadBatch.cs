namespace Cephalon.Data.SqlServer.Services;

internal sealed class SqlServerCdcReadBatch(
    IReadOnlyList<SqlServerCdcCapturedChange> changes,
    bool hasMoreChanges,
    IReadOnlyDictionary<string, string> metadata)
{
    public IReadOnlyList<SqlServerCdcCapturedChange> Changes { get; } = changes;

    public bool HasMoreChanges { get; } = hasMoreChanges;

    public IReadOnlyDictionary<string, string> Metadata { get; } = metadata;

    public static SqlServerCdcReadBatch Idle(IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new SqlServerCdcReadBatch(
            [],
            hasMoreChanges: false,
            metadata is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
    }
}
