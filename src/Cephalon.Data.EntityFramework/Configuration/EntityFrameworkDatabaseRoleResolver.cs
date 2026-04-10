using Cephalon.Abstractions.AppModel;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Data.EntityFramework.Configuration;

internal static class EntityFrameworkDatabaseRoleResolver
{
    public static EntityFrameworkDatabaseRoleContext ResolveSharedWrite(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var appProfile = serviceProvider.GetRequiredService<AppProfile>();
        if (appProfile.Data.ReadWriteSplit == true || appProfile.Databases.Read.HasValues)
        {
            throw new InvalidOperationException(
                "Shared DbContext registration cannot consume a dedicated read database role from Engine:Databases. Use the split read/write DbContext overload instead.");
        }

        return ResolveRole(serviceProvider, "write", appProfile.Databases.Write, appProfile.Databases.Runtime);
    }

    public static EntityFrameworkDatabaseRoleContext ResolveWrite(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var appProfile = serviceProvider.GetRequiredService<AppProfile>();
        return ResolveRole(serviceProvider, "write", appProfile.Databases.Write, appProfile.Databases.Runtime);
    }

    public static EntityFrameworkDatabaseRoleContext ResolveRead(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var appProfile = serviceProvider.GetRequiredService<AppProfile>();
        return ResolveRole(serviceProvider, "read", appProfile.Databases.Read, appProfile.Databases.Runtime);
    }

    private static EntityFrameworkDatabaseRoleContext ResolveRole(
        IServiceProvider serviceProvider,
        string requestedRoleId,
        DatabaseTargetSelection target,
        DatabaseRuntimeSelection sharedRuntime)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(sharedRuntime);

        if (!target.HasValues)
        {
            throw new InvalidOperationException(
                $"Engine:Databases:{ToSectionName(requestedRoleId)} must be configured when Cephalon.Data.EntityFramework uses topology-driven registration for the '{requestedRoleId}' role.");
        }

        if (string.IsNullOrWhiteSpace(target.Provider))
        {
            throw new InvalidOperationException(
                $"Engine:Databases:{ToSectionName(requestedRoleId)}:Provider is required when Cephalon.Data.EntityFramework uses topology-driven registration for the '{requestedRoleId}' role.");
        }

        if (string.IsNullOrWhiteSpace(target.ConnectionStringName) &&
            string.IsNullOrWhiteSpace(target.ConnectionString))
        {
            throw new InvalidOperationException(
                $"Engine:Databases:{ToSectionName(requestedRoleId)} must choose either ConnectionStringName or ConnectionString when Cephalon.Data.EntityFramework uses topology-driven registration for the '{requestedRoleId}' role.");
        }

        var configuration = serviceProvider.GetService<IConfiguration>();
        var sectionPath = $"{EngineSettings.SectionName}:Databases:{ToSectionName(requestedRoleId)}";
        var connectionString = ConnectionStringResolution.Resolve(
            configuration,
            target.ConnectionString,
            target.ConnectionStringName,
            defaultConnectionString: string.Empty,
            sectionPath: sectionPath,
            providerDisplayName: $"{target.Provider} database role '{requestedRoleId}'");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{sectionPath} resolved an empty connection string for Cephalon.Data.EntityFramework.");
        }

        return new EntityFrameworkDatabaseRoleContext(
            requestedRoleId: requestedRoleId,
            resolvedRoleId: requestedRoleId,
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
            maxBatchSize: roleRuntime.MaxBatchSize ?? sharedRuntime.MaxBatchSize);
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
