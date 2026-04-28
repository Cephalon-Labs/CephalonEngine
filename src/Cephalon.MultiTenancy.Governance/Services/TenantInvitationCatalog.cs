using Cephalon.MultiTenancy.Governance.Configuration;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantInvitationCatalog : ITenantInvitationCatalog
{
    private readonly TenantInvitationDescriptor[] configuredInvitations;
    private readonly ITenantInvitationStore invitationStore;

    public TenantInvitationCatalog(
        MultiTenancyGovernanceOptions options,
        IEnumerable<ITenantInvitationContributor> contributors,
        ITenantInvitationStore invitationStore)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contributors);
        ArgumentNullException.ThrowIfNull(invitationStore);

        var registry = new TenantInvitationRegistry();
        foreach (var invitation in options.Invitations)
        {
            registry.Add(invitation);
        }

        foreach (var contributor in contributors)
        {
            contributor.RegisterInvitations(registry);
        }

        configuredInvitations = [.. registry.Build()];
        this.invitationStore = invitationStore;
    }

    public IReadOnlyList<TenantInvitationDescriptor> Invitations => BuildInvitations();

    public IReadOnlyList<TenantInvitationDescriptor> GetByTenantId(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return Invitations
            .Where(invitation => string.Equals(invitation.TenantId, tenantId.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public IReadOnlyList<TenantInvitationDescriptor> GetByInviteeId(string inviteeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inviteeId);

        return Invitations
            .Where(invitation => string.Equals(invitation.InviteeId, inviteeId.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public IReadOnlyList<TenantInvitationDescriptor> GetByInvitationId(string invitationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invitationId);

        return Invitations
            .Where(invitation => string.Equals(invitation.InvitationId, invitationId.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public IReadOnlyList<TenantInvitationDescriptor> GetByTenantAndInvitation(string tenantId, string invitationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(invitationId);

        return Invitations
            .Where(invitation => string.Equals(
                CreateTenantInvitationKey(invitation.TenantId, invitation.InvitationId),
                CreateTenantInvitationKey(tenantId, invitationId),
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private IReadOnlyList<TenantInvitationDescriptor> BuildInvitations()
    {
        var registry = new TenantInvitationRegistry();
        foreach (var invitation in invitationStore.Invitations)
        {
            registry.Add(invitation);
        }

        foreach (var invitation in configuredInvitations)
        {
            registry.Add(invitation);
        }

        return registry.Build();
    }

    private static string CreateTenantInvitationKey(string tenantId, string invitationId)
    {
        return $"{tenantId.Trim()}|{invitationId.Trim()}";
    }
}
