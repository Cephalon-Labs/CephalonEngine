using Cephalon.Abstractions.AppModel;
using Cephalon.Engine.AppModel;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Data.EntityFramework.Configuration;

/// <summary>
/// Resolves <c>Engine:Databases</c> role selections into Entity Framework-specific role contexts.
/// </summary>
public static class EntityFrameworkDatabaseRoleResolver
{
    /// <summary>
    /// Resolves the shared write role used when one <see cref="Microsoft.EntityFrameworkCore.DbContext" />
    /// type serves both reads and writes.
    /// </summary>
    /// <param name="serviceProvider">The current service provider.</param>
    /// <returns>The resolved Entity Framework database-role context.</returns>
    public static EntityFrameworkDatabaseRoleContext ResolveSharedWrite(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var appProfile = serviceProvider.GetRequiredService<AppProfile>();
        if (appProfile.Data.ReadWriteSplit == true || appProfile.Databases.Read.HasValues)
        {
            throw new InvalidOperationException(
                "Shared DbContext registration cannot consume a dedicated read database role from Engine:Databases. Use the split read/write DbContext overload instead.");
        }

        return ResolveRole(serviceProvider, "write");
    }

    /// <summary>
    /// Resolves the write database role.
    /// </summary>
    /// <param name="serviceProvider">The current service provider.</param>
    /// <returns>The resolved Entity Framework database-role context.</returns>
    public static EntityFrameworkDatabaseRoleContext ResolveWrite(IServiceProvider serviceProvider)
    {
        return ResolveRole(serviceProvider, "write");
    }

    /// <summary>
    /// Resolves the read database role.
    /// </summary>
    /// <param name="serviceProvider">The current service provider.</param>
    /// <returns>The resolved Entity Framework database-role context.</returns>
    public static EntityFrameworkDatabaseRoleContext ResolveRead(IServiceProvider serviceProvider)
    {
        return ResolveRole(serviceProvider, "read");
    }

    /// <summary>
    /// Resolves the outbox database role, falling back to the write role when a dedicated outbox role is not configured.
    /// </summary>
    /// <param name="serviceProvider">The current service provider.</param>
    /// <returns>The resolved Entity Framework database-role context.</returns>
    public static EntityFrameworkDatabaseRoleContext ResolveOutbox(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var appProfile = serviceProvider.GetRequiredService<AppProfile>();
        if (appProfile.Databases.Outbox.HasValues)
        {
            return ResolveRole(serviceProvider, "outbox");
        }

        return ResolveRole(serviceProvider, "write");
    }

    /// <summary>
    /// Resolves the audit-history database role.
    /// </summary>
    /// <param name="serviceProvider">The current service provider.</param>
    /// <returns>The resolved Entity Framework database-role context.</returns>
    public static EntityFrameworkDatabaseRoleContext ResolveHistory(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var appProfile = serviceProvider.GetRequiredService<AppProfile>();
        var requestedRoleId = string.IsNullOrWhiteSpace(appProfile.Audit.History.DatabaseRole)
            ? AuditHistorySettings.DefaultDatabaseRole
            : appProfile.Audit.History.DatabaseRole.Trim();

        return ResolveRole(serviceProvider, requestedRoleId);
    }

    /// <summary>
    /// Resolves an arbitrary supported database role from <c>Engine:Databases</c>.
    /// </summary>
    /// <param name="serviceProvider">The current service provider.</param>
    /// <param name="requestedRoleId">The logical database role identifier to resolve.</param>
    /// <returns>The resolved Entity Framework database-role context.</returns>
    public static EntityFrameworkDatabaseRoleContext ResolveRole(
        IServiceProvider serviceProvider,
        string requestedRoleId)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedRoleId);

        var normalizedRoleId = requestedRoleId.Trim();
        var appProfile = serviceProvider.GetRequiredService<AppProfile>();
        var resolution = DatabaseTopologyRoleResolver.Resolve(appProfile.Databases, normalizedRoleId);

        return ResolveRole(
            serviceProvider,
            requestedRoleId: normalizedRoleId,
            resolvedRoleId: resolution.ResolvedRoleId,
            target: resolution.EffectiveTarget,
            sharedRuntime: appProfile.Databases.Runtime);
    }

    private static EntityFrameworkDatabaseRoleContext ResolveRole(
        IServiceProvider serviceProvider,
        string requestedRoleId,
        string resolvedRoleId,
        DatabaseTargetSelection target,
        DatabaseRuntimeSelection sharedRuntime)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(sharedRuntime);

        if (!target.HasValues)
        {
            throw new InvalidOperationException(
                $"Engine:Databases:{ToSectionName(resolvedRoleId)} must be configured when Cephalon.Data.EntityFramework uses topology-driven registration for the '{requestedRoleId}' role.");
        }

        if (string.IsNullOrWhiteSpace(target.Provider))
        {
            throw new InvalidOperationException(
                $"Engine:Databases:{ToSectionName(resolvedRoleId)}:Provider is required when Cephalon.Data.EntityFramework uses topology-driven registration for the '{requestedRoleId}' role.");
        }

        if (string.IsNullOrWhiteSpace(target.ConnectionStringName) &&
            string.IsNullOrWhiteSpace(target.ConnectionString))
        {
            throw new InvalidOperationException(
                $"Engine:Databases:{ToSectionName(resolvedRoleId)} must choose either ConnectionStringName or ConnectionString when Cephalon.Data.EntityFramework uses topology-driven registration for the '{requestedRoleId}' role.");
        }

        var configuration = serviceProvider.GetService<IConfiguration>();
        var sectionPath = $"{EngineSettings.SectionName}:Databases:{ToSectionName(resolvedRoleId)}";
        var connectionString = ConnectionStringResolution.Resolve(
            configuration,
            target.ConnectionString,
            target.ConnectionStringName,
            defaultConnectionString: string.Empty,
            sectionPath: sectionPath,
            providerDisplayName: $"{target.Provider} database role '{resolvedRoleId}'");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{sectionPath} resolved an empty connection string for Cephalon.Data.EntityFramework.");
        }

        return new EntityFrameworkDatabaseRoleContext(
            requestedRoleId: requestedRoleId,
            resolvedRoleId: resolvedRoleId,
            target: target,
            runtime: MergeRuntime(sharedRuntime, target.Runtime),
            connectionString: connectionString);
    }

    private static DatabaseRuntimeSelection MergeRuntime(
        DatabaseRuntimeSelection sharedRuntime,
        DatabaseRuntimeSelection roleRuntime)
    {
        ArgumentNullException.ThrowIfNull(sharedRuntime);
        ArgumentNullException.ThrowIfNull(roleRuntime);

        return new DatabaseRuntimeSelection(
            enableDetailedErrors: roleRuntime.EnableDetailedErrors ?? sharedRuntime.EnableDetailedErrors,
            enableSensitiveDataLogging: roleRuntime.EnableSensitiveDataLogging ?? sharedRuntime.EnableSensitiveDataLogging,
            enableRetryOnFailure: roleRuntime.EnableRetryOnFailure ?? sharedRuntime.EnableRetryOnFailure,
            maxRetryCount: roleRuntime.MaxRetryCount ?? sharedRuntime.MaxRetryCount,
            maxRetryDelaySeconds: roleRuntime.MaxRetryDelaySeconds ?? sharedRuntime.MaxRetryDelaySeconds,
            commandTimeoutSeconds: roleRuntime.CommandTimeoutSeconds ?? sharedRuntime.CommandTimeoutSeconds,
            maxBatchSize: roleRuntime.MaxBatchSize ?? sharedRuntime.MaxBatchSize,
            roleProbeFreshnessSeconds: roleRuntime.RoleProbeFreshnessSeconds ?? sharedRuntime.RoleProbeFreshnessSeconds);
    }

    private static string ToSectionName(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return "Unknown";
        }

        return char.ToUpperInvariant(role[0]) + role[1..];
    }
}
