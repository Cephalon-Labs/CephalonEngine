using System.Globalization;

namespace Cephalon.Data.MySql.Services;

internal readonly record struct MySqlBinlogCheckpointToken(
    string BinlogFile,
    long Position,
    string? SourceServerUuid = null,
    long? SourceServerId = null,
    string? GtidExecutedSet = null,
    string? BinlogFormat = null,
    string? BinlogRowImage = null)
{
    public string Serialize()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{BinlogFile}|{Position}");
    }

    public static MySqlBinlogCheckpointToken Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var parts = value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new FormatException($"Checkpoint token '{value}' is not in the expected binlog-file|position format.");
        }

        return new MySqlBinlogCheckpointToken(
            parts[0],
            long.Parse(parts[1], CultureInfo.InvariantCulture));
    }
}
