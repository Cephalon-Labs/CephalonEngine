using Cephalon.MultiTenancy.Governance.Configuration;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantInvitationCatalog : ITenantInvitationCatalog
{
    private readonly Dictionary<string, TenantInvitationDescriptor[]> byTenantId;
    private readonly Dictionary<string, TenantInvitationDescriptor[]> byInviteeId;
    private readonly Dictionary<string, TenantInvitationDescriptor[]> byInvitationId;
    private readonly Dictionary<string, TenantInvitationDescriptor[]> byTenantAndInvitation;

    public TenantInvitationCatalog(
        MultiTenancyGovernanceOptions options,
        IEnumerable<ITenantInvitationContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new TenantInvitationRegistry();
        foreach (var invitation in options.Invitations)
        {
            registry.Add(invitation);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterInvitations(registry);
        }

        Invitations = registry.Build();
        byTenantId = Invitations
            .GroupBy(static invitation => invitation.TenantId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        byInviteeId = Invitations
            .GroupBy(static invitation => invitation.InviteeId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        byInvitationId = Invitations
            .GroupBy(static invitation => invitation.InvitationId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        byTenantAndInvitation = Invitations
            .GroupBy(static invitation => CreateTenantInvitationKey(invitation.TenantId, invitation.InvitationId), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<TenantInvitationDescriptor> Invitations { get; }

    public IReadOnlyList<TenantInvitationDescriptor> GetByTenantId(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return byTenantId.TryGetValue(tenantId.Trim(), out var invitations)
            ? invitations
            : [];
    }

    public IReadOnlyList<TenantInvitationDescriptor> GetByInviteeId(string inviteeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inviteeId);

        return byInviteeId.TryGetValue(inviteeId.Trim(), out var invitations)
            ? invitations
            : [];
    }

    public IReadOnlyList<TenantInvitationDescriptor> GetByInvitationId(string invitationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invitationId);

        return byInvitationId.TryGetValue(invitationId.Trim(), out var invitations)
            ? invitations
            : [];
    }

    public IReadOnlyList<TenantInvitationDescriptor> GetByTenantAndInvitation(string tenantId, string invitationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(invitationId);

        return byTenantAndInvitation.TryGetValue(CreateTenantInvitationKey(tenantId, invitationId), out var invitations)
            ? invitations
            : [];
    }

    private static string CreateTenantInvitationKey(string tenantId, string invitationId)
    {
        return $"{tenantId.Trim()}|{invitationId.Trim()}";
    }
}
