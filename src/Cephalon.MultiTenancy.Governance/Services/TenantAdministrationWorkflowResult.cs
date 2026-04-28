namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the result of one tenant-administration workflow command.
/// </summary>
public sealed class TenantAdministrationWorkflowResult
{
    /// <summary>
    /// Creates a tenant-administration workflow command result.
    /// </summary>
    /// <param name="tenantId">The stable tenant identifier targeted by the command.</param>
    /// <param name="command">The tenant-administration command that was requested.</param>
    /// <param name="targetKind">The kind of target affected by the command.</param>
    /// <param name="targetId">The stable target identifier affected by the command.</param>
    /// <param name="outcome">The tenant-administration command outcome.</param>
    /// <param name="applied">A value indicating whether the command was fully applied.</param>
    /// <param name="occurredAtUtc">The UTC timestamp when the command was evaluated.</param>
    /// <param name="previousStatus">The target status before the command when a target existed.</param>
    /// <param name="currentStatus">The target status after the command when a target exists.</param>
    /// <param name="membership">The resulting membership descriptor when a membership command produced one.</param>
    /// <param name="invitation">The resulting invitation descriptor when an invitation command produced one.</param>
    /// <param name="reason">The operator-facing command result reason.</param>
    /// <param name="metadata">Optional result metadata.</param>
    public TenantAdministrationWorkflowResult(
        string tenantId,
        string command,
        string targetKind,
        string? targetId,
        string outcome,
        bool applied,
        DateTimeOffset occurredAtUtc,
        string? previousStatus,
        string? currentStatus,
        TenantMembershipDescriptor? membership,
        TenantInvitationDescriptor? invitation,
        string reason,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        TenantId = tenantId;
        Command = command;
        TargetKind = targetKind;
        TargetId = targetId;
        Outcome = outcome;
        Applied = applied;
        OccurredAtUtc = occurredAtUtc;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        Membership = membership;
        Invitation = invitation;
        Reason = reason;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable tenant identifier targeted by the command.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the tenant-administration command that was requested.
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// Gets the kind of target affected by the command.
    /// </summary>
    public string TargetKind { get; }

    /// <summary>
    /// Gets the stable target identifier affected by the command.
    /// </summary>
    public string? TargetId { get; }

    /// <summary>
    /// Gets the tenant-administration command outcome.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether the command was fully applied.
    /// </summary>
    public bool Applied { get; }

    /// <summary>
    /// Gets the UTC timestamp when the command was evaluated.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary>
    /// Gets the target status before the command when a target existed.
    /// </summary>
    public string? PreviousStatus { get; }

    /// <summary>
    /// Gets the target status after the command when a target exists.
    /// </summary>
    public string? CurrentStatus { get; }

    /// <summary>
    /// Gets the resulting membership descriptor when a membership command produced one.
    /// </summary>
    public TenantMembershipDescriptor? Membership { get; }

    /// <summary>
    /// Gets the resulting invitation descriptor when an invitation command produced one.
    /// </summary>
    public TenantInvitationDescriptor? Invitation { get; }

    /// <summary>
    /// Gets the operator-facing command result reason.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets optional result metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
