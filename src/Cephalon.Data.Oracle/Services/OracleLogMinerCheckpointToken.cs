using System.Globalization;

namespace Cephalon.Data.Oracle.Services;

internal readonly record struct OracleLogMinerCheckpointToken(
    decimal CommitScn,
    decimal ChangeScn,
    string RecordSetId,
    long SqlSequenceNumber)
{
    public string Serialize()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{CommitScn.ToString(CultureInfo.InvariantCulture)}|{ChangeScn.ToString(CultureInfo.InvariantCulture)}|{RecordSetId}|{SqlSequenceNumber.ToString(CultureInfo.InvariantCulture)}");
    }

    public static OracleLogMinerCheckpointToken Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var parts = value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
        {
            throw new FormatException($"Checkpoint token '{value}' is not in the expected commit-scn|change-scn|rs-id|ssn format.");
        }

        return new OracleLogMinerCheckpointToken(
            decimal.Parse(parts[0], CultureInfo.InvariantCulture),
            decimal.Parse(parts[1], CultureInfo.InvariantCulture),
            parts[2],
            long.Parse(parts[3], CultureInfo.InvariantCulture));
    }
}
