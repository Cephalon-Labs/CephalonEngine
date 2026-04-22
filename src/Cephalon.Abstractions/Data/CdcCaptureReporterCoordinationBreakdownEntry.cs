namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one grouped reporter-coordination bucket visible on an execution-runtime rollup.
/// </summary>
public sealed record CdcCaptureReporterCoordinationBreakdownEntry
{
    /// <summary>
    /// Creates a new grouped reporter-coordination bucket.
    /// </summary>
    /// <param name="id">
    /// The stable reporter-coordination identifier carried by the bucket, such as a coordination state or degraded reason.
    /// </param>
    /// <param name="count">The number of CDC captures currently reporting the bucket.</param>
    public CdcCaptureReporterCoordinationBreakdownEntry(
        string id,
        int count)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Reporter-coordination breakdown id is required.", nameof(id));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Reporter-coordination breakdown count must be greater than or equal to 0.");
        }

        Id = id.Trim();
        Count = count;
    }

    /// <summary>
    /// Gets the stable reporter-coordination identifier carried by the bucket.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the number of CDC captures currently reporting the bucket.
    /// </summary>
    public int Count { get; }
}
