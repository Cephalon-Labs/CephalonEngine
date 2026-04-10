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
            enabled: settings.Audit.Enabled,
            history: new Abstractions.AppModel.AuditHistorySelection(
                enabled: settings.Audit.History.Enabled,
                provider: settings.Audit.History.Provider,
                databaseRole: settings.Audit.History.Enabled == true &&
                    string.IsNullOrWhiteSpace(settings.Audit.History.DatabaseRole)
                    ? AuditHistorySettings.DefaultDatabaseRole
                    : settings.Audit.History.DatabaseRole,
                export: new Abstractions.AppModel.AuditHistoryExportSelection(
                    enabled: settings.Audit.History.Export.Enabled,
                    maxEntries: settings.Audit.History.Export.Enabled == true &&
                        !settings.Audit.History.Export.MaxEntries.HasValue
                        ? AuditHistoryExportSettings.DefaultMaxEntries
                        : settings.Audit.History.Export.MaxEntries),
                retention: new Abstractions.AppModel.AuditHistoryRetentionSelection(
                    enabled: settings.Audit.History.Retention.Enabled,
                    maxAgeDays: settings.Audit.History.Retention.MaxAgeDays,
                    deleteBatchSize: settings.Audit.History.Retention.Enabled == true &&
                        !settings.Audit.History.Retention.DeleteBatchSize.HasValue
                        ? AuditHistoryRetentionSettings.DefaultDeleteBatchSize
                        : settings.Audit.History.Retention.DeleteBatchSize,
                    applyOnStartup: settings.Audit.History.Retention.ApplyOnStartup,
                    runIntervalMinutes: settings.Audit.History.Retention.RunIntervalMinutes))));
        builder.UseMessagingSelection(new Abstractions.AppModel.MessagingSelection(
            provider: settings.Messaging.Provider));
        builder.UseResilienceSelection(ToResilienceSelection(settings.Resilience));
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
            useRole: settings.UseRole,
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

    private static Abstractions.AppModel.ResilienceSelection ToResilienceSelection(ResilienceSettings settings)
    {
        return new Abstractions.AppModel.ResilienceSelection(
            retry: new Abstractions.AppModel.RetrySelection(
                enabled: settings.Retry.Enabled,
                maxAttempts: settings.Retry.MaxAttempts,
                backoff: settings.Retry.Backoff,
                baseDelayMilliseconds: settings.Retry.BaseDelayMilliseconds,
                maxDelayMilliseconds: settings.Retry.MaxDelayMilliseconds,
                useJitter: settings.Retry.UseJitter),
            timeout: new Abstractions.AppModel.TimeoutSelection(
                enabled: settings.Timeout.Enabled,
                totalTimeoutSeconds: settings.Timeout.TotalTimeoutSeconds,
                attemptTimeoutSeconds: settings.Timeout.AttemptTimeoutSeconds),
            circuitBreaker: new Abstractions.AppModel.CircuitBreakerSelection(
                enabled: settings.CircuitBreaker.Enabled,
                failureRatio: settings.CircuitBreaker.FailureRatio,
                minimumThroughput: settings.CircuitBreaker.MinimumThroughput,
                samplingDurationSeconds: settings.CircuitBreaker.SamplingDurationSeconds,
                breakDurationSeconds: settings.CircuitBreaker.BreakDurationSeconds),
            bulkhead: new Abstractions.AppModel.BulkheadSelection(
                enabled: settings.Bulkhead.Enabled,
                maxConcurrentExecutions: settings.Bulkhead.MaxConcurrentExecutions,
                maxQueuedActions: settings.Bulkhead.MaxQueuedActions),
            rateLimiting: new Abstractions.AppModel.RateLimitingSelection(
                enabled: settings.RateLimiting.Enabled,
                algorithm: settings.RateLimiting.Algorithm,
                permitLimit: settings.RateLimiting.PermitLimit,
                queueLimit: settings.RateLimiting.QueueLimit,
                windowSeconds: settings.RateLimiting.WindowSeconds,
                segmentsPerWindow: settings.RateLimiting.SegmentsPerWindow));
    }
}
