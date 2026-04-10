using Cephalon.Abstractions.AppModel;

namespace Cephalon.Engine.AppModel;

/// <summary>
/// Resolves <c>Engine:Databases</c> role selections into direct or role-referenced effective targets.
/// </summary>
public static class DatabaseTopologyRoleResolver
{
    /// <summary>
    /// Resolves the effective target for the supplied logical database role.
    /// </summary>
    /// <param name="databases">The active database-topology selection.</param>
    /// <param name="requestedRoleId">The logical role to resolve.</param>
    /// <returns>The resolved role selection.</returns>
    public static DatabaseTopologyRoleResolution Resolve(
        DatabaseTopologySelection databases,
        string requestedRoleId)
    {
        ArgumentNullException.ThrowIfNull(databases);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedRoleId);

        var (canonicalRequestedRoleId, requestedTarget) = ResolveDirectTarget(databases, requestedRoleId);
        var useRole = NormalizeRole(requestedTarget.UseRole);
        if (useRole is null)
        {
            return new DatabaseTopologyRoleResolution(
                requestedRoleId: canonicalRequestedRoleId,
                resolvedRoleId: canonicalRequestedRoleId,
                requestedTarget: requestedTarget,
                effectiveTarget: requestedTarget);
        }

        var (resolvedRoleId, resolvedTarget) = ResolveDirectTarget(databases, useRole);
        if (!resolvedTarget.HasValues)
        {
            throw new InvalidOperationException(
                $"Engine:Databases:{ToSectionName(canonicalRequestedRoleId)}:UseRole references '{resolvedRoleId}', but Engine:Databases:{ToSectionName(resolvedRoleId)} is not configured.");
        }

        if (!string.IsNullOrWhiteSpace(resolvedTarget.UseRole))
        {
            throw new InvalidOperationException(
                $"Engine:Databases:{ToSectionName(canonicalRequestedRoleId)}:UseRole references '{resolvedRoleId}', but chained database role references are not supported.");
        }

        return new DatabaseTopologyRoleResolution(
            requestedRoleId: canonicalRequestedRoleId,
            resolvedRoleId: resolvedRoleId,
            requestedTarget: requestedTarget,
            effectiveTarget: MergeTargets(resolvedTarget, requestedTarget),
            useRole: resolvedRoleId);
    }

    private static (string CanonicalRoleId, DatabaseTargetSelection Target) ResolveDirectTarget(
        DatabaseTopologySelection databases,
        string roleId)
    {
        var normalizedRole = NormalizeRoleKey(roleId);

        return normalizedRole switch
        {
            "WRITE" => ("write", databases.Write),
            "READ" => ("read", databases.Read),
            "OUTBOX" => ("outbox", databases.Outbox),
            "HISTORY" => ("history", databases.History),
            _ => throw new InvalidOperationException(
                $"Engine:Databases role '{roleId}' is not supported. Supported roles: Write, Read, Outbox, History.")
        };
    }

    private static DatabaseTargetSelection MergeTargets(
        DatabaseTargetSelection resolvedTarget,
        DatabaseTargetSelection requestedTarget)
    {
        return new DatabaseTargetSelection(
            provider: resolvedTarget.Provider,
            connectionStringName: resolvedTarget.ConnectionStringName,
            connectionString: resolvedTarget.ConnectionString,
            schema: requestedTarget.Schema ?? resolvedTarget.Schema,
            runtime: MergeRuntime(resolvedTarget.Runtime, requestedTarget.Runtime));
    }

    private static DatabaseRuntimeSelection MergeRuntime(
        DatabaseRuntimeSelection resolvedRuntime,
        DatabaseRuntimeSelection requestedRuntime)
    {
        return new DatabaseRuntimeSelection(
            enableDetailedErrors: requestedRuntime.EnableDetailedErrors ?? resolvedRuntime.EnableDetailedErrors,
            enableSensitiveDataLogging: requestedRuntime.EnableSensitiveDataLogging ?? resolvedRuntime.EnableSensitiveDataLogging,
            enableRetryOnFailure: requestedRuntime.EnableRetryOnFailure ?? resolvedRuntime.EnableRetryOnFailure,
            maxRetryCount: requestedRuntime.MaxRetryCount ?? resolvedRuntime.MaxRetryCount,
            maxRetryDelaySeconds: requestedRuntime.MaxRetryDelaySeconds ?? resolvedRuntime.MaxRetryDelaySeconds,
            commandTimeoutSeconds: requestedRuntime.CommandTimeoutSeconds ?? resolvedRuntime.CommandTimeoutSeconds,
            maxBatchSize: requestedRuntime.MaxBatchSize ?? resolvedRuntime.MaxBatchSize);
    }

    private static string NormalizeRoleKey(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    private static string? NormalizeRole(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeRoleKey(value) switch
        {
            "WRITE" => "write",
            "READ" => "read",
            "OUTBOX" => "outbox",
            "HISTORY" => "history",
            _ => value.Trim()
        };
    }

    private static string ToSectionName(string roleId)
    {
        return char.ToUpperInvariant(roleId[0]) + roleId[1..];
    }
}
