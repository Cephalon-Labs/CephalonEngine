using Cephalon.Abstractions.AppModel;
using Cephalon.Engine.Configuration;

namespace Cephalon.Audit.EntityFramework.Configuration;

internal static class EntityFrameworkAuditHistorySelection
{
    public static bool MatchesProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return false;
        }

        var normalized = new string(provider
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());

        return normalized == "entityframework";
    }

    public static string ResolveDatabaseRole(AppProfile appProfile)
    {
        ArgumentNullException.ThrowIfNull(appProfile);

        return string.IsNullOrWhiteSpace(appProfile.Audit.History.DatabaseRole)
            ? AuditHistorySettings.DefaultDatabaseRole
            : appProfile.Audit.History.DatabaseRole.Trim();
    }
}
