namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one combined command-batch template derived from the selected command path of an engine-owned execution group.
/// </summary>
public sealed class DatabaseMigrationOperationalExecutionGroupCommandBatch
{
    /// <summary>
    /// Creates a new execution-group command-batch template.
    /// </summary>
    /// <param name="id">The stable batch identifier such as <c>production</c> or <c>manual</c>.</param>
    /// <param name="displayName">The operator-facing batch name.</param>
    /// <param name="description">The human-readable batch description.</param>
    /// <param name="commandTemplate">The ordered combined command template for this execution-group path.</param>
    /// <param name="commandCount">The number of command entries represented in this batch.</param>
    /// <param name="databaseMigrationIds">The logical migration targets represented in this batch, in execution order.</param>
    /// <param name="commandIds">The stable command identifiers represented in this batch, in encounter order.</param>
    /// <param name="toolIds">The stable operator tool identifiers represented in this batch, in encounter order.</param>
    /// <param name="workingDirectoryHints">The working-directory hints represented in this batch, in encounter order.</param>
    public DatabaseMigrationOperationalExecutionGroupCommandBatch(
        string id,
        string displayName,
        string description,
        string commandTemplate,
        int commandCount,
        IReadOnlyList<string>? databaseMigrationIds = null,
        IReadOnlyList<string>? commandIds = null,
        IReadOnlyList<string>? toolIds = null,
        IReadOnlyList<string>? workingDirectoryHints = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Execution-group command-batch id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Execution-group command-batch display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Execution-group command-batch description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(commandTemplate))
        {
            throw new ArgumentException("Execution-group command-batch template is required.", nameof(commandTemplate));
        }

        if (commandCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(commandCount),
                commandCount,
                "Execution-group command-batch command count must be greater than zero.");
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        CommandTemplate = commandTemplate.Trim();
        CommandCount = commandCount;
        DatabaseMigrationIds = Normalize(databaseMigrationIds);
        CommandIds = Normalize(commandIds);
        ToolIds = Normalize(toolIds);
        WorkingDirectoryHints = Normalize(workingDirectoryHints);

        if (DatabaseMigrationIds.Count > 0 && DatabaseMigrationIds.Count != CommandCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(databaseMigrationIds),
                DatabaseMigrationIds.Count,
                "Execution-group command-batch target count must match the command count when target ids are supplied.");
        }
    }

    /// <summary>
    /// Gets the stable batch identifier such as <c>production</c> or <c>manual</c>.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing batch name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable batch description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the ordered combined command template for this execution-group path.
    /// </summary>
    public string CommandTemplate { get; }

    /// <summary>
    /// Gets the number of command entries represented in this batch.
    /// </summary>
    public int CommandCount { get; }

    /// <summary>
    /// Gets the logical migration targets represented in this batch, in execution order.
    /// </summary>
    public IReadOnlyList<string> DatabaseMigrationIds { get; }

    /// <summary>
    /// Gets the stable command identifiers represented in this batch, in encounter order.
    /// </summary>
    public IReadOnlyList<string> CommandIds { get; }

    /// <summary>
    /// Gets the stable operator tool identifiers represented in this batch, in encounter order.
    /// </summary>
    public IReadOnlyList<string> ToolIds { get; }

    /// <summary>
    /// Gets the working-directory hints represented in this batch, in encounter order.
    /// </summary>
    public IReadOnlyList<string> WorkingDirectoryHints { get; }

    /// <summary>
    /// Gets the single stable operator tool identifier when the batch uses only one tool.
    /// </summary>
    public string? PrimaryToolId => ToolIds.Count == 1 ? ToolIds[0] : null;

    /// <summary>
    /// Gets the single working-directory hint when every command in the batch uses the same working directory.
    /// </summary>
    public string? PrimaryWorkingDirectoryHint => WorkingDirectoryHints.Count == 1 ? WorkingDirectoryHints[0] : null;

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        var normalized = new List<string>(values.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var trimmed = value.Trim();
            if (seen.Add(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized.ToArray();
    }
}
