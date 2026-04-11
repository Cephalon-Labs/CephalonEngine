using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Configuration;

namespace Cephalon.Engine.AppModel;

internal static class AppProfileSelectionValidator
{
    private static readonly string[] SupportedRetryBackoffModes =
    [
        "Constant",
        "Linear",
        "Exponential"
    ];

    private static readonly HashSet<string> SupportedRetryBackoffModeIndex = new(
        SupportedRetryBackoffModes.Select(NormalizeKey),
        StringComparer.Ordinal);

    private static readonly string[] SupportedRateLimitingAlgorithms =
    [
        "FixedWindow",
        "SlidingWindow",
        "TokenBucket",
        "ConcurrencyLimiter"
    ];

    private static readonly HashSet<string> SupportedRateLimitingAlgorithmIndex = new(
        SupportedRateLimitingAlgorithms.Select(NormalizeKey),
        StringComparer.Ordinal);

    private static readonly string[] SupportedAuthorizationModes =
    [
        "RBAC",
        "ABAC",
        "Policy"
    ];

    private static readonly HashSet<string> SupportedAuthorizationModeIndex = new(
        SupportedAuthorizationModes.Select(NormalizeKey),
        StringComparer.Ordinal);

    public static void Validate(
        DataSelection data,
        DatabaseTopologySelection databases,
        IdentitySelection identity,
        TenancySelection tenancy,
        AuditSelection audit,
        MessagingSelection messaging,
        ResilienceSelection resilience,
        IReadOnlyDictionary<string, PatternDescriptor> selectedPatterns,
        IReadOnlyDictionary<string, TechnologyDescriptor> selectedTechnologies)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(databases);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(tenancy);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(messaging);
        ArgumentNullException.ThrowIfNull(resilience);
        ArgumentNullException.ThrowIfNull(selectedPatterns);
        ArgumentNullException.ThrowIfNull(selectedTechnologies);

        if (data.ReadWriteSplit == true && !selectedPatterns.ContainsKey("cqrs"))
        {
            throw new InvalidOperationException(
                "Data read/write split requires the 'cqrs' pattern.");
        }

        if (data.OutboxEnabled == true && !selectedPatterns.ContainsKey("outbox"))
        {
            throw new InvalidOperationException(
                "Data outbox support requires the 'outbox' pattern.");
        }

        if (databases.Read.HasValues && !selectedPatterns.ContainsKey("cqrs"))
        {
            throw new InvalidOperationException(
                "A dedicated read database target requires the 'cqrs' pattern.");
        }

        if (databases.Outbox.HasValues && !selectedPatterns.ContainsKey("outbox"))
        {
            throw new InvalidOperationException(
                "A dedicated outbox database target requires the 'outbox' pattern.");
        }

        if (data.ReadWriteSplit == false && databases.Read.HasValues)
        {
            throw new InvalidOperationException(
                "A dedicated read database target cannot be configured when data read/write split is explicitly disabled.");
        }

        if (data.OutboxEnabled == false && databases.Outbox.HasValues)
        {
            throw new InvalidOperationException(
                "An outbox database target cannot be configured when outbox support is explicitly disabled.");
        }

        ValidateDatabaseRoleReferences(databases);
        ValidateAuditHistorySelection(audit, databases);
        ValidateResilienceSelection(resilience);

        if (databases.Migrations.ExitAfterApply == true && databases.Migrations.ApplyOnStartup != true)
        {
            throw new InvalidOperationException(
                "Database migrations cannot exit after apply unless ApplyOnStartup is explicitly enabled.");
        }

        foreach (var target in databases.Migrations.Targets)
        {
            ValidateMigrationTarget(target, databases);
        }

        if (messaging.Provider is not null &&
            !selectedTechnologies.ContainsKey("event-driven-integration"))
        {
            throw new InvalidOperationException(
                "Messaging provider selection requires the 'event-driven-integration' technology.");
        }

        if (identity.Enabled == false && identity.AuthorizationModes.Count > 0)
        {
            throw new InvalidOperationException(
                "Authorization modes cannot be selected when identity is explicitly disabled.");
        }

        if (identity.HasValues &&
            identity.Enabled != false &&
            !selectedTechnologies.ContainsKey("identity-access"))
        {
            throw new InvalidOperationException(
                "Identity settings require the 'identity-access' technology.");
        }

        foreach (var mode in identity.AuthorizationModes)
        {
            if (!SupportedAuthorizationModeIndex.Contains(NormalizeKey(mode)))
            {
                throw new InvalidOperationException(
                    $"Authorization mode '{mode}' is not supported. Supported modes: {string.Join(", ", SupportedAuthorizationModes)}.");
            }
        }

        if (tenancy.Mode is not null && tenancy.Enabled == false)
        {
            throw new InvalidOperationException(
                "Tenancy mode cannot be selected when multi-tenancy is explicitly disabled.");
        }

        if (tenancy.HasValues &&
            tenancy.Enabled != false &&
            !selectedTechnologies.ContainsKey("multi-tenancy"))
        {
            throw new InvalidOperationException(
                "Tenancy settings require the 'multi-tenancy' technology.");
        }

    }

    private static void ValidateResilienceSelection(ResilienceSelection resilience)
    {
        ArgumentNullException.ThrowIfNull(resilience);

        ValidateRetrySelection(resilience.Retry);
        ValidateTimeoutSelection(resilience.Timeout);
        ValidateCircuitBreakerSelection(resilience.CircuitBreaker);
        ValidateBulkheadSelection(resilience.Bulkhead);
        ValidateRateLimitingSelection(resilience.RateLimiting);
        foreach (var entry in resilience.BehaviorExecutionOverrides)
        {
            ValidateBehaviorExecutionResilienceOverrideSelection(entry);
        }
    }

    private static void ValidateRetrySelection(RetrySelection retry)
    {
        ArgumentNullException.ThrowIfNull(retry);

        if (retry.MaxAttempts is not null && retry.MaxAttempts <= 0)
        {
            throw new InvalidOperationException(
                "Retry MaxAttempts must be greater than zero when supplied.");
        }

        if (retry.BaseDelayMilliseconds is not null && retry.BaseDelayMilliseconds <= 0)
        {
            throw new InvalidOperationException(
                "Retry BaseDelayMilliseconds must be greater than zero when supplied.");
        }

        if (retry.MaxDelayMilliseconds is not null && retry.MaxDelayMilliseconds <= 0)
        {
            throw new InvalidOperationException(
                "Retry MaxDelayMilliseconds must be greater than zero when supplied.");
        }

        if (retry.BaseDelayMilliseconds is not null &&
            retry.MaxDelayMilliseconds is not null &&
            retry.MaxDelayMilliseconds < retry.BaseDelayMilliseconds)
        {
            throw new InvalidOperationException(
                "Retry MaxDelayMilliseconds cannot be less than BaseDelayMilliseconds.");
        }

        if (retry.Backoff is not null &&
            !SupportedRetryBackoffModeIndex.Contains(NormalizeKey(retry.Backoff)))
        {
            throw new InvalidOperationException(
                $"Retry backoff mode '{retry.Backoff}' is not supported. Supported modes: {string.Join(", ", SupportedRetryBackoffModes)}.");
        }
    }

    private static void ValidateTimeoutSelection(TimeoutSelection timeout)
    {
        ArgumentNullException.ThrowIfNull(timeout);

        if (timeout.TotalTimeoutSeconds is not null && timeout.TotalTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Timeout TotalTimeoutSeconds must be greater than zero when supplied.");
        }

        if (timeout.AttemptTimeoutSeconds is not null && timeout.AttemptTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Timeout AttemptTimeoutSeconds must be greater than zero when supplied.");
        }

        if (timeout.TotalTimeoutSeconds is not null &&
            timeout.AttemptTimeoutSeconds is not null &&
            timeout.AttemptTimeoutSeconds > timeout.TotalTimeoutSeconds)
        {
            throw new InvalidOperationException(
                "Timeout AttemptTimeoutSeconds cannot exceed TotalTimeoutSeconds.");
        }
    }

    private static void ValidateCircuitBreakerSelection(CircuitBreakerSelection circuitBreaker)
    {
        ArgumentNullException.ThrowIfNull(circuitBreaker);

        if (circuitBreaker.FailureRatio is not null &&
            (circuitBreaker.FailureRatio <= 0m || circuitBreaker.FailureRatio > 1m))
        {
            throw new InvalidOperationException(
                "Circuit breaker FailureRatio must be greater than zero and less than or equal to one when supplied.");
        }

        if (circuitBreaker.MinimumThroughput is not null && circuitBreaker.MinimumThroughput <= 0)
        {
            throw new InvalidOperationException(
                "Circuit breaker MinimumThroughput must be greater than zero when supplied.");
        }

        if (circuitBreaker.SamplingDurationSeconds is not null && circuitBreaker.SamplingDurationSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Circuit breaker SamplingDurationSeconds must be greater than zero when supplied.");
        }

        if (circuitBreaker.BreakDurationSeconds is not null && circuitBreaker.BreakDurationSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Circuit breaker BreakDurationSeconds must be greater than zero when supplied.");
        }
    }

    private static void ValidateBulkheadSelection(BulkheadSelection bulkhead)
    {
        ArgumentNullException.ThrowIfNull(bulkhead);

        if (bulkhead.MaxConcurrentExecutions is not null && bulkhead.MaxConcurrentExecutions <= 0)
        {
            throw new InvalidOperationException(
                "Bulkhead MaxConcurrentExecutions must be greater than zero when supplied.");
        }

        if (bulkhead.MaxQueuedActions is not null && bulkhead.MaxQueuedActions < 0)
        {
            throw new InvalidOperationException(
                "Bulkhead MaxQueuedActions cannot be negative when supplied.");
        }
    }

    private static void ValidateRateLimitingSelection(RateLimitingSelection rateLimiting)
    {
        ArgumentNullException.ThrowIfNull(rateLimiting);

        if (rateLimiting.Algorithm is not null &&
            !SupportedRateLimitingAlgorithmIndex.Contains(NormalizeKey(rateLimiting.Algorithm)))
        {
            throw new InvalidOperationException(
                $"Rate limiting algorithm '{rateLimiting.Algorithm}' is not supported. Supported algorithms: {string.Join(", ", SupportedRateLimitingAlgorithms)}.");
        }

        if (rateLimiting.PermitLimit is not null && rateLimiting.PermitLimit <= 0)
        {
            throw new InvalidOperationException(
                "Rate limiting PermitLimit must be greater than zero when supplied.");
        }

        if (rateLimiting.QueueLimit is not null && rateLimiting.QueueLimit < 0)
        {
            throw new InvalidOperationException(
                "Rate limiting QueueLimit cannot be negative when supplied.");
        }

        if (rateLimiting.WindowSeconds is not null && rateLimiting.WindowSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Rate limiting WindowSeconds must be greater than zero when supplied.");
        }

        if (rateLimiting.SegmentsPerWindow is not null && rateLimiting.SegmentsPerWindow <= 0)
        {
            throw new InvalidOperationException(
                "Rate limiting SegmentsPerWindow must be greater than zero when supplied.");
        }

        foreach (var entry in rateLimiting.Overrides)
        {
            ValidateRateLimitingOverrideSelection(entry);
        }
    }

    private static void ValidateRateLimitingOverrideSelection(RateLimitingOverrideSelection overrideSelection)
    {
        ArgumentNullException.ThrowIfNull(overrideSelection);

        if (overrideSelection.BehaviorIds.Count == 0 && overrideSelection.TransportIds.Count == 0)
        {
            throw new InvalidOperationException(
                $"Rate limiting override '{overrideSelection.Id}' must target at least one behavior or transport.");
        }

        if (!overrideSelection.Enabled.HasValue &&
            overrideSelection.Algorithm is null &&
            !overrideSelection.PermitLimit.HasValue &&
            !overrideSelection.QueueLimit.HasValue &&
            !overrideSelection.WindowSeconds.HasValue &&
            !overrideSelection.SegmentsPerWindow.HasValue)
        {
            throw new InvalidOperationException(
                $"Rate limiting override '{overrideSelection.Id}' must specify Enabled or at least one limiter value such as PermitLimit or Algorithm.");
        }

        if (overrideSelection.Algorithm is not null &&
            !SupportedRateLimitingAlgorithmIndex.Contains(NormalizeKey(overrideSelection.Algorithm)))
        {
            throw new InvalidOperationException(
                $"Rate limiting override '{overrideSelection.Id}' selected unsupported algorithm '{overrideSelection.Algorithm}'. Supported algorithms: {string.Join(", ", SupportedRateLimitingAlgorithms)}.");
        }

        if (overrideSelection.PermitLimit is not null && overrideSelection.PermitLimit <= 0)
        {
            throw new InvalidOperationException(
                $"Rate limiting override '{overrideSelection.Id}' PermitLimit must be greater than zero when supplied.");
        }

        if (overrideSelection.QueueLimit is not null && overrideSelection.QueueLimit < 0)
        {
            throw new InvalidOperationException(
                $"Rate limiting override '{overrideSelection.Id}' QueueLimit cannot be negative when supplied.");
        }

        if (overrideSelection.WindowSeconds is not null && overrideSelection.WindowSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"Rate limiting override '{overrideSelection.Id}' WindowSeconds must be greater than zero when supplied.");
        }

        if (overrideSelection.SegmentsPerWindow is not null && overrideSelection.SegmentsPerWindow <= 0)
        {
            throw new InvalidOperationException(
                $"Rate limiting override '{overrideSelection.Id}' SegmentsPerWindow must be greater than zero when supplied.");
        }
    }

    private static void ValidateBehaviorExecutionResilienceOverrideSelection(
        BehaviorExecutionResilienceOverrideSelection overrideSelection)
    {
        ArgumentNullException.ThrowIfNull(overrideSelection);

        if (overrideSelection.BehaviorIds.Count == 0 && overrideSelection.TransportIds.Count == 0)
        {
            throw new InvalidOperationException(
                $"Behavior execution resilience override '{overrideSelection.Id}' must target at least one behavior or transport.");
        }

        if (!overrideSelection.HasStrategyValues)
        {
            throw new InvalidOperationException(
                $"Behavior execution resilience override '{overrideSelection.Id}' must specify at least one retry, timeout, circuit-breaker, or bulkhead value.");
        }

        ValidateRetrySelection(overrideSelection.Retry);
        ValidateTimeoutSelection(overrideSelection.Timeout);
        ValidateCircuitBreakerSelection(overrideSelection.CircuitBreaker);
        ValidateBulkheadSelection(overrideSelection.Bulkhead);
    }

    private static void ValidateDatabaseRoleReferences(
        DatabaseTopologySelection databases)
    {
        ArgumentNullException.ThrowIfNull(databases);

        ValidateDatabaseRoleReference("write", databases.Write, databases, allowUseRole: false);
        ValidateDatabaseRoleReference("read", databases.Read, databases, allowUseRole: false);
        ValidateDatabaseRoleReference("outbox", databases.Outbox, databases, allowUseRole: true);
        ValidateDatabaseRoleReference("history", databases.History, databases, allowUseRole: true);
    }

    private static void ValidateDatabaseRoleReference(
        string roleId,
        DatabaseTargetSelection target,
        DatabaseTopologySelection databases,
        bool allowUseRole)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(databases);

        if (string.IsNullOrWhiteSpace(target.UseRole))
        {
            return;
        }

        var sectionPath = $"{EngineSettings.SectionName}:Databases:{ToSectionName(roleId)}";
        if (!allowUseRole)
        {
            throw new InvalidOperationException(
                $"{sectionPath}:UseRole is not supported. Only the Outbox and History database targets can reference another role in the current contract.");
        }

        var normalizedReference = NormalizeKey(target.UseRole);
        if (normalizedReference != "WRITE")
        {
            throw new InvalidOperationException(
                $"{sectionPath}:UseRole selected unsupported database role '{target.UseRole}'. Only 'write' is allowed in the current contract.");
        }

        if (!databases.Write.HasValues)
        {
            throw new InvalidOperationException(
                $"{sectionPath}:UseRole selected the 'write' database role, but no write database target is configured.");
        }

        if (!string.IsNullOrWhiteSpace(databases.Write.UseRole))
        {
            throw new InvalidOperationException(
                $"{sectionPath}:UseRole cannot reference Engine:Databases:Write because the write database target must remain a concrete root role.");
        }
    }

    private static string NormalizeKey(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    private static string ToSectionName(string role)
    {
        return char.ToUpperInvariant(role[0]) + role[1..];
    }

    private static void ValidateMigrationTarget(
        string target,
        DatabaseTopologySelection databases)
    {
        var normalizedTarget = NormalizeKey(target);

        if (normalizedTarget == "WRITE")
        {
            if (!databases.Write.HasValues)
            {
                throw new InvalidOperationException(
                    "The 'write' migration target requires a configured write database role.");
            }

            return;
        }

        if (normalizedTarget == "READ")
        {
            if (!databases.Read.HasValues)
            {
                throw new InvalidOperationException(
                    "The 'read' migration target requires a configured read database role.");
            }

            return;
        }

        if (normalizedTarget == "OUTBOX")
        {
            if (!databases.Outbox.HasValues)
            {
                throw new InvalidOperationException(
                    "The 'outbox' migration target requires a configured outbox database role.");
            }

            return;
        }

        if (normalizedTarget == "HISTORY")
        {
            if (!databases.History.HasValues)
            {
                throw new InvalidOperationException(
                    "The 'history' migration target requires a configured history database role.");
            }

            return;
        }

        throw new InvalidOperationException(
            $"Database migration target '{target}' is not supported. Supported targets: Write, Read, Outbox, History.");
    }

    private static void ValidateAuditHistorySelection(
        AuditSelection audit,
        DatabaseTopologySelection databases)
    {
        ValidateAuditHistoryExportSelection(audit);

        if (audit.History.Enabled != true)
        {
            return;
        }

        if (audit.Enabled == false)
        {
            throw new InvalidOperationException(
                "Durable audit history cannot be enabled when audit support is explicitly disabled.");
        }

        if (string.IsNullOrWhiteSpace(audit.History.Provider))
        {
            throw new InvalidOperationException(
                "Durable audit history requires a provider selection under Engine:Audit:History:Provider.");
        }

        var databaseRole = ResolveAuditHistoryDatabaseRole(audit.History);
        ValidateAuditHistoryDatabaseRole(databaseRole, databases);
        ValidateAuditHistoryRetentionSelection(audit.History.Retention);
    }

    private static string ResolveAuditHistoryDatabaseRole(
        AuditHistorySelection history)
    {
        ArgumentNullException.ThrowIfNull(history);

        return string.IsNullOrWhiteSpace(history.DatabaseRole)
            ? AuditHistorySettings.DefaultDatabaseRole
            : history.DatabaseRole.Trim();
    }

    private static void ValidateAuditHistoryDatabaseRole(
        string databaseRole,
        DatabaseTopologySelection databases)
    {
        var normalizedRole = NormalizeKey(databaseRole);

        if (normalizedRole == "WRITE")
        {
            if (!databases.Write.HasValues)
            {
                throw new InvalidOperationException(
                    "Durable audit history selected the 'write' database role, but no write database target is configured.");
            }

            return;
        }

        if (normalizedRole == "READ")
        {
            if (!databases.Read.HasValues)
            {
                throw new InvalidOperationException(
                    "Durable audit history selected the 'read' database role, but no read database target is configured.");
            }

            return;
        }

        if (normalizedRole == "OUTBOX")
        {
            if (!databases.Outbox.HasValues)
            {
                throw new InvalidOperationException(
                    "Durable audit history selected the 'outbox' database role, but no outbox database target is configured.");
            }

            return;
        }

        if (normalizedRole == "HISTORY")
        {
            if (!databases.History.HasValues)
            {
                throw new InvalidOperationException(
                    "Durable audit history selected the 'history' database role, but no history database target is configured.");
            }

            return;
        }

        throw new InvalidOperationException(
            $"Durable audit history selected unsupported database role '{databaseRole}'. Supported roles: Write, Read, Outbox, History.");
    }

    private static void ValidateAuditHistoryRetentionSelection(
        AuditHistoryRetentionSelection retention)
    {
        ArgumentNullException.ThrowIfNull(retention);

        if (retention.Enabled != true)
        {
            return;
        }

        if (retention.MaxAgeDays is not > 0)
        {
            throw new InvalidOperationException(
                "Durable audit-history retention requires a positive MaxAgeDays value under Engine:Audit:History:Retention:MaxAgeDays.");
        }

        if (retention.DeleteBatchSize is not null && retention.DeleteBatchSize <= 0)
        {
            throw new InvalidOperationException(
                "Durable audit-history retention DeleteBatchSize must be greater than zero when supplied.");
        }

        if (retention.RunIntervalMinutes is not null && retention.RunIntervalMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Durable audit-history retention RunIntervalMinutes must be greater than zero when supplied.");
        }

        if (retention.ApplyOnStartup != true && retention.RunIntervalMinutes is null)
        {
            throw new InvalidOperationException(
                "Durable audit-history retention must either apply on startup or define a recurring RunIntervalMinutes value.");
        }
    }

    private static void ValidateAuditHistoryExportSelection(
        AuditSelection audit)
    {
        ArgumentNullException.ThrowIfNull(audit);

        var export = audit.History.Export;
        if (export.Enabled != true)
        {
            return;
        }

        if (audit.History.Enabled != true)
        {
            throw new InvalidOperationException(
                "Durable audit-history export cannot be enabled unless durable audit history is explicitly enabled.");
        }

        if (export.MaxEntries is not null && export.MaxEntries <= 0)
        {
            throw new InvalidOperationException(
                "Durable audit-history export MaxEntries must be greater than zero when supplied.");
        }
    }
}
