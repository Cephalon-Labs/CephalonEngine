using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Configuration;

namespace Cephalon.Engine.AppModel;

internal static class AppProfileSelectionValidator
{
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
        IReadOnlyDictionary<string, PatternDescriptor> selectedPatterns,
        IReadOnlyDictionary<string, TechnologyDescriptor> selectedTechnologies)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(databases);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(tenancy);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(messaging);
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

        ValidateAuditHistorySelection(audit, databases);

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

    private static string NormalizeKey(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
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
}
