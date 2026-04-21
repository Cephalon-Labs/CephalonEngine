namespace Cephalon.Data.Postgres.Services;

internal readonly record struct PostgresLogicalReplicationCheckpointToken(
    string SlotName,
    string CommitLsn,
    string TransactionEndLsn)
{
    public string Serialize()
    {
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{SlotName}|{CommitLsn}|{TransactionEndLsn}");
    }

    public static PostgresLogicalReplicationCheckpointToken Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var parts = value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            throw new FormatException($"Checkpoint token '{value}' is not in the expected slot|commit-lsn|transaction-end-lsn format.");
        }

        return new PostgresLogicalReplicationCheckpointToken(parts[0], parts[1], parts[2]);
    }
}
