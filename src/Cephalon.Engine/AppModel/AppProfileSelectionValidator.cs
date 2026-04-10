using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Technologies;

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
        MessagingSelection messaging,
        IReadOnlyDictionary<string, PatternDescriptor> selectedPatterns,
        IReadOnlyDictionary<string, TechnologyDescriptor> selectedTechnologies)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(databases);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(tenancy);
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
}
