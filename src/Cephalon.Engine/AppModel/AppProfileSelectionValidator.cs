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
        IdentitySelection identity,
        TenancySelection tenancy,
        MessagingSelection messaging,
        IReadOnlyDictionary<string, PatternDescriptor> selectedPatterns,
        IReadOnlyDictionary<string, TechnologyDescriptor> selectedTechnologies)
    {
        ArgumentNullException.ThrowIfNull(data);
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
}
