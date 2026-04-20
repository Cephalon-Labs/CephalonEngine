namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one operator-facing CDC capture execution runtime visible to the current Cephalon runtime.
/// </summary>
public sealed class CdcCaptureExecutionRuntimeDescriptor
{
    /// <summary>
    /// Creates a new CDC capture execution runtime descriptor.
    /// </summary>
    /// <param name="id">The stable execution-runtime identifier.</param>
    /// <param name="displayName">The operator-facing execution-runtime name.</param>
    /// <param name="description">The human-readable execution-runtime description.</param>
    /// <param name="metadata">Optional operator-facing metadata for the execution runtime.</param>
    /// <param name="cdcCaptureIds">
    /// Optional CDC capture identifiers explicitly owned by the execution runtime when ownership is bounded to a known capture set.
    /// </param>
    /// <param name="summary">Optional aggregate runtime summary describing the latest reported operator-facing state for the execution runtime.</param>
    public CdcCaptureExecutionRuntimeDescriptor(
        string id,
        string displayName,
        string description,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyList<string>? cdcCaptureIds = null,
        CdcCaptureExecutionRuntimeSummary? summary = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("CDC capture execution runtime id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("CDC capture execution runtime display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("CDC capture execution runtime description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        CdcCaptureIds = Normalize(cdcCaptureIds);
        Summary = summary ?? CdcCaptureExecutionRuntimeSummary.Empty;
    }

    /// <summary>
    /// Gets the stable execution-runtime identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing execution-runtime name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable execution-runtime description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets operator-facing metadata for the execution runtime.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets the CDC capture identifiers explicitly owned by the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; }

    /// <summary>
    /// Gets the latest aggregate runtime summary reported for the execution runtime.
    /// </summary>
    public CdcCaptureExecutionRuntimeSummary Summary { get; }

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
