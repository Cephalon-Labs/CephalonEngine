namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one host-driven tenant-administration workflow command.
/// </summary>
public sealed class TenantAdministrationWorkflowRequest
{
    /// <summary>
    /// Creates a tenant-administration workflow command request.
    /// </summary>
    /// <param name="command">The tenant-administration command to apply.</param>
    /// <param name="tenantId">The stable tenant identifier.</param>
    /// <param name="principalId">The principal identifier for membership commands.</param>
    /// <param name="principalKind">The principal kind for membership commands, such as user, group, service, or organization.</param>
    /// <param name="invitationId">The invitation identifier for invitation commands.</param>
    /// <param name="inviteeId">The invitee identifier for invitation commands.</param>
    /// <param name="inviteeKind">The invitee kind for invitation commands, such as user, group, service, or organization.</param>
    /// <param name="displayName">The optional operator-facing membership or invitation name.</param>
    /// <param name="roles">The tenant-local roles associated with the membership or invitation.</param>
    /// <param name="actor">The actor that requested the command when known.</param>
    /// <param name="reason">The optional operator-facing command reason.</param>
    /// <param name="atUtc">The UTC timestamp used for the command. The runtime clock is used when omitted.</param>
    /// <param name="effectiveFromUtc">The optional UTC timestamp when a granted membership becomes active.</param>
    /// <param name="expiresAtUtc">The optional UTC timestamp when the membership or invitation expires.</param>
    /// <param name="correlationId">The optional correlation identifier for the command.</param>
    /// <param name="metadata">Optional command metadata.</param>
    public TenantAdministrationWorkflowRequest(
        string command,
        string tenantId,
        string? principalId = null,
        string? principalKind = null,
        string? invitationId = null,
        string? inviteeId = null,
        string? inviteeKind = null,
        string? displayName = null,
        IReadOnlyList<string>? roles = null,
        string? actor = null,
        string? reason = null,
        DateTimeOffset? atUtc = null,
        DateTimeOffset? effectiveFromUtc = null,
        DateTimeOffset? expiresAtUtc = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Tenant-administration command is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        Command = NormalizeCommand(command);
        TenantId = tenantId.Trim();
        PrincipalId = string.IsNullOrWhiteSpace(principalId) ? null : principalId.Trim();
        PrincipalKind = string.IsNullOrWhiteSpace(principalKind) ? null : principalKind.Trim();
        InvitationId = string.IsNullOrWhiteSpace(invitationId) ? null : invitationId.Trim();
        InviteeId = string.IsNullOrWhiteSpace(inviteeId) ? null : inviteeId.Trim();
        InviteeKind = string.IsNullOrWhiteSpace(inviteeKind) ? null : inviteeKind.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        Roles = NormalizeValues(roles);
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        AtUtc = atUtc;
        EffectiveFromUtc = effectiveFromUtc;
        ExpiresAtUtc = expiresAtUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the tenant-administration command to apply.
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// Gets the stable tenant identifier.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the principal identifier for membership commands.
    /// </summary>
    public string? PrincipalId { get; }

    /// <summary>
    /// Gets the principal kind for membership commands.
    /// </summary>
    public string? PrincipalKind { get; }

    /// <summary>
    /// Gets the invitation identifier for invitation commands.
    /// </summary>
    public string? InvitationId { get; }

    /// <summary>
    /// Gets the invitee identifier for invitation commands.
    /// </summary>
    public string? InviteeId { get; }

    /// <summary>
    /// Gets the invitee kind for invitation commands.
    /// </summary>
    public string? InviteeKind { get; }

    /// <summary>
    /// Gets the optional operator-facing membership or invitation name.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the tenant-local roles associated with the membership or invitation.
    /// </summary>
    public IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Gets the actor that requested the command when known.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the optional operator-facing command reason.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets the UTC timestamp used for the command.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional UTC timestamp when a granted membership becomes active.
    /// </summary>
    public DateTimeOffset? EffectiveFromUtc { get; }

    /// <summary>
    /// Gets the optional UTC timestamp when the membership or invitation expires.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the command.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets optional command metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    internal static string NormalizeCommand(string command)
    {
        var normalized = command.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantAdministrationWorkflowCommands.GrantMembership => TenantAdministrationWorkflowCommands.GrantMembership,
            TenantAdministrationWorkflowCommands.SuspendMembership => TenantAdministrationWorkflowCommands.SuspendMembership,
            TenantAdministrationWorkflowCommands.ExpireMembership => TenantAdministrationWorkflowCommands.ExpireMembership,
            TenantAdministrationWorkflowCommands.IssueInvitation => TenantAdministrationWorkflowCommands.IssueInvitation,
            TenantAdministrationWorkflowCommands.AcceptInvitation => TenantAdministrationWorkflowCommands.AcceptInvitation,
            TenantAdministrationWorkflowCommands.RevokeInvitation => TenantAdministrationWorkflowCommands.RevokeInvitation,
            TenantAdministrationWorkflowCommands.ExpireInvitation => TenantAdministrationWorkflowCommands.ExpireInvitation,
            _ => throw new ArgumentException($"Tenant-administration workflow command '{command}' is not supported.", nameof(command))
        };
    }

    private static string[] NormalizeValues(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static Dictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in metadata)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                copy[pair.Key.Trim()] = pair.Value;
            }
        }

        return copy;
    }
}
