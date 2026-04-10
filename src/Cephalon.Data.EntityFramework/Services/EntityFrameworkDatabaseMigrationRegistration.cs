namespace Cephalon.Data.EntityFramework.Services;

/// <summary>
/// Describes one Entity Framework Core <see cref="Microsoft.EntityFrameworkCore.DbContext" /> type that can satisfy
/// one or more logical <c>Engine:Databases</c> migration targets.
/// </summary>
public sealed class EntityFrameworkDatabaseMigrationRegistration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkDatabaseMigrationRegistration" /> class.
    /// </summary>
    /// <param name="dbContextType">The <see cref="Microsoft.EntityFrameworkCore.DbContext" /> type that can apply schema changes.</param>
    /// <param name="targetRoleIds">The logical migration targets satisfied by the context.</param>
    public EntityFrameworkDatabaseMigrationRegistration(
        Type dbContextType,
        IReadOnlyList<string> targetRoleIds)
    {
        ArgumentNullException.ThrowIfNull(dbContextType);
        ArgumentNullException.ThrowIfNull(targetRoleIds);

        DbContextType = dbContextType;
        TargetRoleIds = targetRoleIds
            .Where(static targetRoleId => !string.IsNullOrWhiteSpace(targetRoleId))
            .Select(static targetRoleId => targetRoleId.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static targetRoleId => targetRoleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Gets the <see cref="Microsoft.EntityFrameworkCore.DbContext" /> type that can apply schema changes.
    /// </summary>
    public Type DbContextType { get; }

    /// <summary>
    /// Gets the logical migration targets satisfied by the context.
    /// </summary>
    public IReadOnlyList<string> TargetRoleIds { get; }
}
