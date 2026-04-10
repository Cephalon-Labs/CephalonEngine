using Cephalon.Abstractions.AppModel;

namespace Cephalon.Data.EntityFramework.Services;

internal static class EntityFrameworkDatabaseMigrationTargetResolver
{
    public static HashSet<string> ResolveRequestedTargets(
        DatabaseMigrationsSelection migrationSelection,
        IEnumerable<string> registeredRoleIds)
    {
        ArgumentNullException.ThrowIfNull(migrationSelection);
        ArgumentNullException.ThrowIfNull(registeredRoleIds);

        if (migrationSelection.Targets.Count > 0)
        {
            return migrationSelection.Targets
                .Select(static target => target.Trim().ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return registeredRoleIds
            .Where(static roleId => !string.IsNullOrWhiteSpace(roleId))
            .Select(static roleId => roleId.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
