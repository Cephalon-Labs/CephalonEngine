using Cephalon.Abstractions.AppModel;

namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one logical database role resolved for the active Cephalon runtime.
/// </summary>
public sealed class DatabaseRoleDescriptor
{
    /// <summary>
    /// Creates a new database-role descriptor.
    /// </summary>
    /// <param name="id">The stable logical database-role identifier.</param>
    /// <param name="displayName">The operator-facing database-role name.</param>
    /// <param name="description">The human-readable database-role description.</param>
    /// <param name="provider">The logical provider identifier that backs the effective target.</param>
    /// <param name="requestedRoleId">The logical role that was requested by configuration or runtime selection.</param>
    /// <param name="resolvedRoleId">The concrete role that ultimately backs the physical target.</param>
    /// <param name="resolutionMode">The runtime resolution mode such as <c>direct</c> or <c>role-reference</c>.</param>
    /// <param name="runtime">The effective runtime tuning resolved for this database role.</param>
    /// <param name="usesRoleReference">Whether the logical role resolves through <c>UseRole</c>.</param>
    /// <param name="useRole">The referenced role supplied through <c>UseRole</c>, when present.</param>
    /// <param name="connectionMode">The effective connection mode such as <c>named</c> or <c>inline</c>.</param>
    /// <param name="connectionStringName">The effective named connection-string reference, when used.</param>
    /// <param name="schema">The effective schema override, when configured.</param>
    /// <param name="consumers">The logical engine features that explicitly target this role.</param>
    /// <param name="referencedByRoles">Other logical roles that explicitly reference this role through <c>UseRole</c>.</param>
    /// <param name="coLocatedRoles">Other logical roles that resolve to the same concrete role target.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the database role.</param>
    public DatabaseRoleDescriptor(
        string id,
        string displayName,
        string description,
        string provider,
        string requestedRoleId,
        string resolvedRoleId,
        string resolutionMode,
        DatabaseRuntimeSelection? runtime = null,
        bool usesRoleReference = false,
        string? useRole = null,
        string? connectionMode = null,
        string? connectionStringName = null,
        string? schema = null,
        IReadOnlyList<string>? consumers = null,
        IReadOnlyList<string>? referencedByRoles = null,
        IReadOnlyList<string>? coLocatedRoles = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Database role id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Database role display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Database role description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Database role provider is required.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(requestedRoleId))
        {
            throw new ArgumentException("Requested database role id is required.", nameof(requestedRoleId));
        }

        if (string.IsNullOrWhiteSpace(resolvedRoleId))
        {
            throw new ArgumentException("Resolved database role id is required.", nameof(resolvedRoleId));
        }

        if (string.IsNullOrWhiteSpace(resolutionMode))
        {
            throw new ArgumentException("Database role resolution mode is required.", nameof(resolutionMode));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Provider = provider.Trim();
        RequestedRoleId = requestedRoleId.Trim();
        ResolvedRoleId = resolvedRoleId.Trim();
        ResolutionMode = resolutionMode.Trim();
        Runtime = runtime ?? DatabaseRuntimeSelection.Empty;
        UsesRoleReference = usesRoleReference;
        UseRole = string.IsNullOrWhiteSpace(useRole) ? null : useRole.Trim();
        ConnectionMode = string.IsNullOrWhiteSpace(connectionMode) ? null : connectionMode.Trim();
        ConnectionStringName = string.IsNullOrWhiteSpace(connectionStringName) ? null : connectionStringName.Trim();
        Schema = string.IsNullOrWhiteSpace(schema) ? null : schema.Trim();
        Consumers = Normalize(consumers);
        ReferencedByRoles = Normalize(referencedByRoles);
        CoLocatedRoles = Normalize(coLocatedRoles);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable logical database-role identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing database-role name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable database-role description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the logical provider identifier that backs the effective target.
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// Gets the logical role requested by configuration or runtime selection.
    /// </summary>
    public string RequestedRoleId { get; }

    /// <summary>
    /// Gets the concrete role that ultimately backs the physical target.
    /// </summary>
    public string ResolvedRoleId { get; }

    /// <summary>
    /// Gets the runtime resolution mode.
    /// </summary>
    public string ResolutionMode { get; }

    /// <summary>
    /// Gets the effective runtime tuning resolved for this database role.
    /// </summary>
    public DatabaseRuntimeSelection Runtime { get; }

    /// <summary>
    /// Gets a value indicating whether this role resolves through <c>UseRole</c>.
    /// </summary>
    public bool UsesRoleReference { get; }

    /// <summary>
    /// Gets the referenced role supplied through <c>UseRole</c>, when present.
    /// </summary>
    public string? UseRole { get; }

    /// <summary>
    /// Gets the effective connection mode.
    /// </summary>
    public string? ConnectionMode { get; }

    /// <summary>
    /// Gets the effective named connection-string reference, when used.
    /// </summary>
    public string? ConnectionStringName { get; }

    /// <summary>
    /// Gets the effective schema override, when configured.
    /// </summary>
    public string? Schema { get; }

    /// <summary>
    /// Gets the logical engine features that explicitly target this role.
    /// </summary>
    public IReadOnlyList<string> Consumers { get; }

    /// <summary>
    /// Gets the logical roles that explicitly reference this role through <c>UseRole</c>.
    /// </summary>
    public IReadOnlyList<string> ReferencedByRoles { get; }

    /// <summary>
    /// Gets the logical roles that resolve to the same concrete role target.
    /// </summary>
    public IReadOnlyList<string> CoLocatedRoles { get; }

    /// <summary>
    /// Gets optional operator-facing metadata associated with the database role.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

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
