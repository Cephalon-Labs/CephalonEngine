using Cephalon.Engine.Configuration;
using Cephalon.Engine.Patterns;
using Cephalon.Engine.Technologies;
using Cephalon.Engine.Transports;

namespace Cephalon.Engine.AppModel;

/// <summary>
/// Builds app profiles from <see cref="EngineSettings" /> values.
/// </summary>
public static class AppProfileFactory
{
    /// <summary>
    /// Creates an app profile from the configured engine settings.
    /// </summary>
    /// <param name="settings">The engine settings that describe the blueprint, patterns, transports, and technologies.</param>
    /// <returns>The built app profile.</returns>
    public static Abstractions.AppModel.AppProfile Create(EngineSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var builder = new AppProfileBuilder();
        ApplySettings(builder, settings);
        return builder.Build();
    }

    internal static void ApplySettings(AppProfileBuilder builder, EngineSettings settings)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settings);

        if (!string.IsNullOrWhiteSpace(settings.Blueprint))
        {
            builder.UseBlueprint(BuiltInBlueprints.Resolve(settings.Blueprint));
        }

        foreach (var patternName in settings.Patterns)
        {
            builder.AddPattern(BuiltInPatterns.Resolve(patternName));
        }

        foreach (var transportName in settings.Transports)
        {
            builder.AddTransport(BuiltInTransports.Resolve(transportName));
        }

        foreach (var technologyName in settings.Technologies)
        {
            builder.SelectTechnology(technologyName);
        }

        builder.UseDataSelection(new Abstractions.AppModel.DataSelection(
            provider: settings.Data.Provider,
            readWriteSplit: settings.Data.ReadWriteSplit,
            outboxEnabled: settings.Data.OutboxEnabled,
            idGenerator: settings.Data.IdGenerator));
        builder.UseDatabaseSelection(ToDatabaseSelection(settings.Databases));
        builder.UseIdentitySelection(new Abstractions.AppModel.IdentitySelection(
            enabled: settings.Identity.Enabled,
            authorizationModes: settings.Identity.AuthorizationModes));
        builder.UseTenancySelection(new Abstractions.AppModel.TenancySelection(
            enabled: settings.Tenancy.Enabled,
            mode: settings.Tenancy.Mode));
        builder.UseAuditSelection(new Abstractions.AppModel.AuditSelection(
            enabled: settings.Audit.Enabled));
        builder.UseMessagingSelection(new Abstractions.AppModel.MessagingSelection(
            provider: settings.Messaging.Provider));
    }

    private static Abstractions.AppModel.DatabaseTopologySelection ToDatabaseSelection(DatabaseTopologySettings settings)
    {
        return new Abstractions.AppModel.DatabaseTopologySelection(
            runtime: ToDatabaseRuntimeSelection(settings.Runtime),
            write: ToDatabaseTargetSelection(settings.Write),
            read: ToDatabaseTargetSelection(settings.Read),
            outbox: ToDatabaseTargetSelection(settings.Outbox),
            history: ToDatabaseTargetSelection(settings.History),
            migrations: new Abstractions.AppModel.DatabaseMigrationsSelection(
                applyOnStartup: settings.Migrations.ApplyOnStartup,
                exitAfterApply: settings.Migrations.ExitAfterApply,
                targets: settings.Migrations.Targets));
    }

    private static Abstractions.AppModel.DatabaseTargetSelection ToDatabaseTargetSelection(DatabaseTargetSettings settings)
    {
        return new Abstractions.AppModel.DatabaseTargetSelection(
            provider: settings.Provider,
            connectionStringName: settings.ConnectionStringName,
            connectionString: settings.ConnectionString,
            schema: settings.Schema,
            runtime: ToDatabaseRuntimeSelection(settings.Runtime));
    }

    private static Abstractions.AppModel.DatabaseRuntimeSelection ToDatabaseRuntimeSelection(DatabaseRuntimeSettings settings)
    {
        return new Abstractions.AppModel.DatabaseRuntimeSelection(
            enableDetailedErrors: settings.EnableDetailedErrors,
            enableSensitiveDataLogging: settings.EnableSensitiveDataLogging,
            enableRetryOnFailure: settings.EnableRetryOnFailure,
            maxRetryCount: settings.MaxRetryCount,
            maxRetryDelaySeconds: settings.MaxRetryDelaySeconds,
            commandTimeoutSeconds: settings.CommandTimeoutSeconds,
            maxBatchSize: settings.MaxBatchSize);
    }
}
