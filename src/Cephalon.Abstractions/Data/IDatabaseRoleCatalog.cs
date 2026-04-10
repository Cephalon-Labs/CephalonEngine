namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes the active engine-owned database-role catalog for the current runtime.
/// </summary>
public interface IDatabaseRoleCatalog
{
    /// <summary>
    /// Gets every database role visible to the current runtime.
    /// </summary>
    IReadOnlyList<DatabaseRoleDescriptor> DatabaseRoles { get; }

    /// <summary>
    /// Gets one database role by its logical identifier.
    /// </summary>
    /// <param name="databaseRoleId">The logical database-role identifier.</param>
    /// <returns>The matching database-role descriptor, or <see langword="null" /> when none exists.</returns>
    DatabaseRoleDescriptor? GetById(string databaseRoleId);

    /// <summary>
    /// Gets every database role that resolves to the supplied concrete role identifier.
    /// </summary>
    /// <param name="resolvedRoleId">The resolved concrete database-role identifier.</param>
    /// <returns>The matching database-role descriptors.</returns>
    IReadOnlyList<DatabaseRoleDescriptor> GetByResolvedRole(string resolvedRoleId);

    /// <summary>
    /// Gets every database role backed by the supplied provider identifier.
    /// </summary>
    /// <param name="provider">The provider identifier to match.</param>
    /// <returns>The matching database-role descriptors.</returns>
    IReadOnlyList<DatabaseRoleDescriptor> GetByProvider(string provider);
}
