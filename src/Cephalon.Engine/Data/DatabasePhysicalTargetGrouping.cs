using System.Security.Cryptography;
using System.Text;
using Cephalon.Abstractions.AppModel;
using Cephalon.Engine.AppModel;

namespace Cephalon.Engine.Data;

internal static class DatabasePhysicalTargetGrouping
{
    private static readonly string[] KnownRoleIds = ["write", "read", "outbox", "history"];

    public static IReadOnlyDictionary<string, PhysicalTargetInfo> BuildRoleMap(AppProfile appProfile)
    {
        ArgumentNullException.ThrowIfNull(appProfile);

        var resolvedRoles = KnownRoleIds
            .Where(roleId => GetTarget(appProfile.Databases, roleId).HasValues)
            .Select(roleId => (RoleId: roleId, Resolution: DatabaseTopologyRoleResolver.Resolve(appProfile.Databases, roleId)))
            .Select(entry => (entry.RoleId, Info: CreateInfo(entry.RoleId, entry.Resolution)))
            .ToArray();
        var rolesByPhysicalTarget = resolvedRoles
            .GroupBy(static entry => entry.Info.PhysicalTargetId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .Select(static entry => entry.RoleId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static roleId => roleId, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return resolvedRoles.ToDictionary(
            static entry => entry.RoleId,
            entry => entry.Info with
            {
                PhysicalCoLocatedRoles = rolesByPhysicalTarget[entry.Info.PhysicalTargetId]
                    .Where(roleId => !string.Equals(roleId, entry.RoleId, StringComparison.OrdinalIgnoreCase))
                    .ToArray()
            },
            StringComparer.OrdinalIgnoreCase);
    }

    private static PhysicalTargetInfo CreateInfo(
        string roleId,
        DatabaseTopologyRoleResolution resolution)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);
        ArgumentNullException.ThrowIfNull(resolution);

        var provider = resolution.EffectiveTarget.Provider ?? "unknown";

        if (!string.IsNullOrWhiteSpace(resolution.EffectiveTarget.ConnectionStringName))
        {
            var connectionStringName = resolution.EffectiveTarget.ConnectionStringName.Trim();
            var normalizedName = NormalizeKey(connectionStringName);

            return new PhysicalTargetInfo(
                PhysicalTargetId: $"named:{NormalizeKey(provider)}:{normalizedName}",
                PhysicalTargetDisplayName: $"{provider} named connection '{connectionStringName}'");
        }

        if (!string.IsNullOrWhiteSpace(resolution.EffectiveTarget.ConnectionString))
        {
            var hash = ComputeShortHash(resolution.EffectiveTarget.ConnectionString);

            return new PhysicalTargetInfo(
                PhysicalTargetId: $"inline:{NormalizeKey(provider)}:{hash}",
                PhysicalTargetDisplayName: $"{provider} inline connection ({hash})");
        }

        if (resolution.UsesRoleReference)
        {
            return new PhysicalTargetInfo(
                PhysicalTargetId: $"role-reference:{NormalizeKey(provider)}:{NormalizeKey(resolution.ResolvedRoleId)}",
                PhysicalTargetDisplayName: $"{provider} resolved role '{resolution.ResolvedRoleId}'");
        }

        return new PhysicalTargetInfo(
            PhysicalTargetId: $"direct-role:{NormalizeKey(provider)}:{NormalizeKey(roleId)}",
            PhysicalTargetDisplayName: $"{provider} direct role '{roleId}'");
    }

    private static DatabaseTargetSelection GetTarget(DatabaseTopologySelection databases, string roleId)
    {
        return roleId switch
        {
            "write" => databases.Write,
            "read" => databases.Read,
            "outbox" => databases.Outbox,
            "history" => databases.History,
            _ => DatabaseTargetSelection.Empty
        };
    }

    private static string NormalizeKey(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static string ComputeShortHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return Convert.ToHexString(bytes)[..12].ToLowerInvariant();
    }

    internal sealed record PhysicalTargetInfo(
        string PhysicalTargetId,
        string PhysicalTargetDisplayName,
        IReadOnlyList<string>? PhysicalCoLocatedRoles = null);
}
