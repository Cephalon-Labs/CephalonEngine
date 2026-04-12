namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one operator-facing command template for executing a database-migration target.
/// </summary>
public sealed class DatabaseMigrationCommandDescriptor
{
    /// <summary>
    /// Creates a new database-migration command descriptor.
    /// </summary>
    /// <param name="id">The stable command identifier such as <c>bundle</c>, <c>script</c>, or <c>update</c>.</param>
    /// <param name="displayName">The operator-facing command name.</param>
    /// <param name="description">The human-readable command description.</param>
    /// <param name="commandTemplate">The command template that operators can adapt for their environment.</param>
    /// <param name="recommendedForProduction">Whether this command is recommended for production use.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the command.</param>
    /// <param name="toolId">An optional stable tool identifier such as <c>dotnet-ef</c>.</param>
    /// <param name="executionCategory">An optional execution category such as <c>deploy-time</c> or <c>manual</c>.</param>
    /// <param name="workingDirectoryHint">An optional working-directory hint for where the command is typically run.</param>
    public DatabaseMigrationCommandDescriptor(
        string id,
        string displayName,
        string description,
        string commandTemplate,
        bool recommendedForProduction = false,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? toolId = null,
        string? executionCategory = null,
        string? workingDirectoryHint = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Database migration command id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Database migration command display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Database migration command description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(commandTemplate))
        {
            throw new ArgumentException("Database migration command template is required.", nameof(commandTemplate));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        CommandTemplate = commandTemplate.Trim();
        RecommendedForProduction = recommendedForProduction;
        ToolId = string.IsNullOrWhiteSpace(toolId) ? null : toolId.Trim();
        ExecutionCategory = string.IsNullOrWhiteSpace(executionCategory) ? null : executionCategory.Trim();
        WorkingDirectoryHint = string.IsNullOrWhiteSpace(workingDirectoryHint) ? null : workingDirectoryHint.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable command identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing command name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable command description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the command template that operators can adapt for their environment.
    /// </summary>
    public string CommandTemplate { get; }

    /// <summary>
    /// Gets a value indicating whether this command is recommended for production use.
    /// </summary>
    public bool RecommendedForProduction { get; }

    /// <summary>
    /// Gets the stable operator tool identifier when the provider can name one.
    /// </summary>
    public string? ToolId { get; }

    /// <summary>
    /// Gets the execution category when the provider can distinguish deploy-time, manual, or other command paths.
    /// </summary>
    public string? ExecutionCategory { get; }

    /// <summary>
    /// Gets the provider-published working-directory hint when one is known.
    /// </summary>
    public string? WorkingDirectoryHint { get; }

    /// <summary>
    /// Gets optional operator-facing metadata associated with the command.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
