namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one engine-owned operator action for the current database-topology posture.
/// </summary>
public sealed class DatabaseTopologyOperationalAction
{
    /// <summary>
    /// Creates a new database-topology operator action.
    /// </summary>
    /// <param name="id">The stable action identifier.</param>
    /// <param name="category">The stable machine-readable remediation category.</param>
    /// <param name="tone">The operator-facing tone such as <c>Success</c>, <c>Warning</c>, or <c>Error</c>.</param>
    /// <param name="title">The human-readable action title.</param>
    /// <param name="detail">The operator-facing action detail.</param>
    /// <param name="completionSignal">The operator-facing signal that the action is complete.</param>
    /// <param name="actionLabel">The suggested operator action label.</param>
    /// <param name="actionPath">The suggested operator action path.</param>
    /// <param name="sourceRoleIds">Optional logical database-role identifiers that contributed to the action.</param>
    /// <param name="sourceMigrationIds">Optional logical migration-target identifiers that contributed to the action.</param>
    public DatabaseTopologyOperationalAction(
        string id,
        string category,
        string tone,
        string title,
        string detail,
        string completionSignal,
        string actionLabel,
        string actionPath,
        IReadOnlyList<string>? sourceRoleIds = null,
        IReadOnlyList<string>? sourceMigrationIds = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Database-topology action id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Database-topology action category is required.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(tone))
        {
            throw new ArgumentException("Database-topology action tone is required.", nameof(tone));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Database-topology action title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(detail))
        {
            throw new ArgumentException("Database-topology action detail is required.", nameof(detail));
        }

        if (string.IsNullOrWhiteSpace(completionSignal))
        {
            throw new ArgumentException("Database-topology action completion signal is required.", nameof(completionSignal));
        }

        if (string.IsNullOrWhiteSpace(actionLabel))
        {
            throw new ArgumentException("Database-topology action label is required.", nameof(actionLabel));
        }

        if (string.IsNullOrWhiteSpace(actionPath))
        {
            throw new ArgumentException("Database-topology action path is required.", nameof(actionPath));
        }

        Id = id.Trim();
        Category = category.Trim();
        Tone = tone.Trim();
        Title = title.Trim();
        Detail = detail.Trim();
        CompletionSignal = completionSignal.Trim();
        ActionLabel = actionLabel.Trim();
        ActionPath = actionPath.Trim();
        SourceRoleIds = Normalize(sourceRoleIds);
        SourceMigrationIds = Normalize(sourceMigrationIds);
    }

    /// <summary>
    /// Gets the stable action identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the stable machine-readable remediation category.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the operator-facing action tone.
    /// </summary>
    public string Tone { get; }

    /// <summary>
    /// Gets the human-readable action title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the operator-facing action detail.
    /// </summary>
    public string Detail { get; }

    /// <summary>
    /// Gets the operator-facing signal that the action is complete.
    /// </summary>
    public string CompletionSignal { get; }

    /// <summary>
    /// Gets the suggested operator action label.
    /// </summary>
    public string ActionLabel { get; }

    /// <summary>
    /// Gets the suggested operator action path.
    /// </summary>
    public string ActionPath { get; }

    /// <summary>
    /// Gets the logical database-role identifiers that contributed to the action.
    /// </summary>
    public IReadOnlyList<string> SourceRoleIds { get; }

    /// <summary>
    /// Gets the logical migration-target identifiers that contributed to the action.
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
