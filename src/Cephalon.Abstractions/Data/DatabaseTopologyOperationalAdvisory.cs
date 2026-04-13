namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one reusable operator-facing advisory for the current database-topology posture.
/// </summary>
public sealed class DatabaseTopologyOperationalAdvisory
{
    /// <summary>
    /// Creates a new database-topology advisory.
    /// </summary>
    /// <param name="id">The stable advisory identifier.</param>
    /// <param name="tone">The operator-facing tone such as <c>Success</c>, <c>Warning</c>, or <c>Error</c>.</param>
    /// <param name="title">The human-readable advisory title.</param>
    /// <param name="detail">The operator-facing advisory detail.</param>
    /// <param name="actionLabel">The suggested operator action label.</param>
    /// <param name="actionPath">The suggested operator action path.</param>
    /// <param name="sourceRoleIds">Optional logical database-role identifiers that contributed to the advisory.</param>
    /// <param name="sourceMigrationIds">Optional logical migration-target identifiers that contributed to the advisory.</param>
    public DatabaseTopologyOperationalAdvisory(
        string id,
        string tone,
        string title,
        string detail,
        string actionLabel,
        string actionPath,
        IReadOnlyList<string>? sourceRoleIds = null,
        IReadOnlyList<string>? sourceMigrationIds = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Database-topology advisory id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(tone))
        {
            throw new ArgumentException("Database-topology advisory tone is required.", nameof(tone));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Database-topology advisory title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(detail))
        {
            throw new ArgumentException("Database-topology advisory detail is required.", nameof(detail));
        }

        if (string.IsNullOrWhiteSpace(actionLabel))
        {
            throw new ArgumentException("Database-topology advisory action label is required.", nameof(actionLabel));
        }

        if (string.IsNullOrWhiteSpace(actionPath))
        {
            throw new ArgumentException("Database-topology advisory action path is required.", nameof(actionPath));
        }

        Id = id.Trim();
        Tone = tone.Trim();
        Title = title.Trim();
        Detail = detail.Trim();
        ActionLabel = actionLabel.Trim();
        ActionPath = actionPath.Trim();
        SourceRoleIds = Normalize(sourceRoleIds);
        SourceMigrationIds = Normalize(sourceMigrationIds);
    }

    /// <summary>
    /// Gets the stable advisory identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing advisory tone.
    /// </summary>
    public string Tone { get; }

    /// <summary>
    /// Gets the human-readable advisory title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the operator-facing advisory detail.
    /// </summary>
    public string Detail { get; }

    /// <summary>
    /// Gets the suggested operator action label.
    /// </summary>
    public string ActionLabel { get; }

    /// <summary>
    /// Gets the suggested operator action path.
    /// </summary>
    public string ActionPath { get; }

    /// <summary>
    /// Gets the logical database-role identifiers that contributed to the advisory.
    /// </summary>
    public IReadOnlyList<string> SourceRoleIds { get; }

    /// <summary>
    /// Gets the logical migration-target identifiers that contributed to the advisory.
    /// </summary>
    public IReadOnlyList<string> SourceMigrationIds { get; }

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
