namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantInvitationRegistry : ITenantInvitationRegistry
{
    private readonly List<TenantInvitationDescriptor> invitations = [];

    public void Add(TenantInvitationDescriptor invitation)
    {
        ArgumentNullException.ThrowIfNull(invitation);

        invitations.Add(invitation);
    }

    public IReadOnlyList<TenantInvitationDescriptor> Build()
    {
        return invitations
            .GroupBy(
                static invitation => string.Join(
                    "|",
                    invitation.TenantId,
                    invitation.InvitationId),
                StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static invitation => invitation.TenantId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static invitation => invitation.InvitationId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
