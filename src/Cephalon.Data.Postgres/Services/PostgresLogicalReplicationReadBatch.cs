namespace Cephalon.Data.Postgres.Services;

internal sealed class PostgresLogicalReplicationReadBatch(
    IReadOnlyList<PostgresLogicalReplicationCapturedChange> changes,
    bool hasMoreChanges,
    IReadOnlyDictionary<string, string> metadata)
{
    public IReadOnlyList<PostgresLogicalReplicationCapturedChange> Changes { get; } = changes;

    public bool HasMoreChanges { get; } = hasMoreChanges;

    public IReadOnlyDictionary<string, string> Metadata { get; } = metadata;

    public static PostgresLogicalReplicationReadBatch Idle(IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new PostgresLogicalReplicationReadBatch(
            [],
            hasMoreChanges: false,
            metadata is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
    }
}
