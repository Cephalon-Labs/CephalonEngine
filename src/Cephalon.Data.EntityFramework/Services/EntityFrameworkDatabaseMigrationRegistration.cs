namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkDatabaseMigrationRegistration
{
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

    public Type DbContextType { get; }

    public IReadOnlyList<string> TargetRoleIds { get; }
}
