using Cephalon.Abstractions.AppModel;

namespace Cephalon.Data.EntityFramework.Configuration;

/// <summary>
/// Describes one resolved <c>Engine:Databases</c> role as consumed by the Entity Framework pack.
/// </summary>
public sealed class EntityFrameworkDatabaseRoleContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkDatabaseRoleContext" /> class.
    /// </summary>
    /// <param name="requestedRoleId">The logical role the caller requested, such as <c>write</c> or <c>read</c>.</param>
    /// <param name="resolvedRoleId">The logical role that ultimately supplied the effective database target.</param>
    /// <param name="target">The selected database target metadata.</param>
    /// <param name="runtime">The merged runtime settings for the role.</param>
    /// <param name="connectionString">The resolved connection string for the role.</param>
    public EntityFrameworkDatabaseRoleContext(
        string requestedRoleId,
        string resolvedRoleId,
        DatabaseTargetSelection target,
        DatabaseRuntimeSelection runtime,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedRoleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedRoleId);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        RequestedRoleId = requestedRoleId.Trim();
        ResolvedRoleId = resolvedRoleId.Trim();
        Target = target;
        Runtime = runtime;
        ConnectionString = connectionString.Trim();
    }

    /// <summary>
    /// Gets the logical database role requested by the caller.
    /// </summary>
    public string RequestedRoleId { get; }

    /// <summary>
    /// Gets the convenience role identifier used by most host callbacks.
    /// </summary>
    public string Role => RequestedRoleId;

    /// <summary>
    /// Gets the logical database role that supplied the effective target.
    /// </summary>
    public string ResolvedRoleId { get; }

    /// <summary>
    /// Gets the selected database target metadata.
    /// </summary>
    public DatabaseTargetSelection Target { get; }

    /// <summary>
    /// Gets the merged runtime settings for the selected role.
    /// </summary>
    public DatabaseRuntimeSelection Runtime { get; }

    /// <summary>
    /// Gets the resolved connection string for the selected role.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Gets the selected provider identifier, if one was declared.
    /// </summary>
    public string? Provider => Target.Provider;

    /// <summary>
    /// Gets the selected named connection-string reference, if one was declared.
    /// </summary>
    public string? ConnectionStringName => Target.ConnectionStringName;

    /// <summary>
    /// Gets a value indicating whether the role resolved through a named connection string.
    /// </summary>
    public bool UsesNamedConnectionString => !string.IsNullOrWhiteSpace(ConnectionStringName);

    /// <summary>
    /// Gets the selected schema override, if one was declared.
    /// </summary>
    public string? Schema => Target.Schema;

    /// <summary>
    /// Gets a value indicating whether the requested role fell back to another configured role.
    /// </summary>
    public bool IsFallback =>
        !string.Equals(RequestedRoleId, ResolvedRoleId, StringComparison.OrdinalIgnoreCase);
}
