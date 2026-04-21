using System.Globalization;

namespace Cephalon.Data.SqlServer.Services;

internal readonly record struct SqlServerCdcCheckpointToken(
    byte[] StartLsn,
    byte[] SequenceValue,
    int Operation)
{
    public string Serialize()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"0x{Convert.ToHexString(StartLsn)}|0x{Convert.ToHexString(SequenceValue)}|{Operation}");
    }

    public static SqlServerCdcCheckpointToken Zero(byte[] startLsn)
    {
        ArgumentNullException.ThrowIfNull(startLsn);

        return new SqlServerCdcCheckpointToken((byte[])startLsn.Clone(), new byte[10], 0);
    }

    public static SqlServerCdcCheckpointToken Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var parts = value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            throw new FormatException($"Checkpoint token '{value}' is not in the expected start-lsn|seqval|operation format.");
        }

        return FromHexStrings(
            parts[0],
            parts[1],
            int.Parse(parts[2], CultureInfo.InvariantCulture));
    }

    public static SqlServerCdcCheckpointToken FromHexStrings(
        string startLsn,
        string sequenceValue,
        int operation)
    {
        return new SqlServerCdcCheckpointToken(
            ParseHex(startLsn),
            ParseHex(sequenceValue),
            operation);
    }

    private static byte[] ParseHex(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[2..];
        }

        if (normalized.Length % 2 != 0)
        {
            throw new FormatException($"Hex value '{value}' must have an even number of characters.");
        }

        return Convert.FromHexString(normalized);
    }
}
