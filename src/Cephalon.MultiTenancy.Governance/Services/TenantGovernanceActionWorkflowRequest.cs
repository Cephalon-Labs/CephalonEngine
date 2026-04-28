namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one tenant-governance action workflow transition request.
/// </summary>
public sealed class TenantGovernanceActionWorkflowRequest
{
    /// <summary>
    /// Creates a tenant-governance action workflow transition request.
    /// </summary>
    /// <param name="command">The workflow command to apply.</param>
    /// <param name="tenantId">The tenant identifier to transition.</param>
    /// <param name="actionId">The governance action identifier to transition.</param>
    /// <param name="actionKind">The optional governance action kind.</param>
    /// <param name="subjectKind">The optional subject kind affected by the action.</param>
    /// <param name="subjectId">The optional subject identifier affected by the action.</param>
    /// <param name="displayName">The optional operator-facing action name.</param>
    /// <param name="actor">The actor that requested the workflow transition when known.</param>
    /// <param name="reason">The optional operator-facing transition reason.</param>
    /// <param name="atUtc">The UTC timestamp used for the transition. The runtime clock is used when omitted.</param>
    /// <param name="expiresAtUtc">The optional UTC timestamp when the action expires.</param>
    /// <param name="correlationId">The optional correlation identifier for the workflow transition.</param>
    /// <param name="metadata">Optional transition metadata.</param>
    public TenantGovernanceActionWorkflowRequest(
        string command,
        string tenantId,
        string actionId,
        string? actionKind = null,
        string? subjectKind = null,
        string? subjectId = null,
        string? displayName = null,
        string? actor = null,
        string? reason = null,
        DateTimeOffset? atUtc = null,
        DateTimeOffset? expiresAtUtc = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Workflow command is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(actionId))
        {
            throw new ArgumentException("Action id is required.", nameof(actionId));
        }

        Command = NormalizeCommand(command);
        TenantId = tenantId.Trim();
        ActionId = actionId.Trim();
        ActionKind = string.IsNullOrWhiteSpace(actionKind) ? null : TenantGovernanceActionDescriptor.NormalizeActionKind(actionKind);
        SubjectKind = string.IsNullOrWhiteSpace(subjectKind) ? null : TenantGovernanceActionDescriptor.NormalizeSubjectKind(subjectKind);
        SubjectId = string.IsNullOrWhiteSpace(subjectId) ? null : subjectId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        AtUtc = atUtc;
        ExpiresAtUtc = expiresAtUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets the workflow command to apply.
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// Gets the tenant identifier to transition.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the governance action identifier to transition.
    /// </summary>
    public string ActionId { get; }

    /// <summary>
    /// Gets the optional governance action kind.
    /// </summary>
    public string? ActionKind { get; }

    /// <summary>
    /// Gets the optional subject kind affected by the action.
    /// </summary>
    public string? SubjectKind { get; }

    /// <summary>
    /// Gets the optional subject identifier affected by the action.
    /// </summary>
    public string? SubjectId { get; }

    /// <summary>
    /// Gets the optional operator-facing action name.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the actor that requested the workflow transition when known.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the optional operator-facing transition reason.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets the UTC timestamp used for the transition.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional UTC timestamp when the action expires.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>
    /// Gets the optional correlation identifier for the workflow transition.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets optional transition metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    internal static string NormalizeCommand(string command)
    {
        var normalized = command.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantGovernanceActionWorkflowCommands.Request => TenantGovernanceActionWorkflowCommands.Request,
            TenantGovernanceActionWorkflowCommands.Approve => TenantGovernanceActionWorkflowCommands.Approve,
            TenantGovernanceActionWorkflowCommands.Reject => TenantGovernanceActionWorkflowCommands.Reject,
            TenantGovernanceActionWorkflowCommands.RequireRemediation => TenantGovernanceActionWorkflowCommands.RequireRemediation,
            TenantGovernanceActionWorkflowCommands.MarkRemediated => TenantGovernanceActionWorkflowCommands.MarkRemediated,
            TenantGovernanceActionWorkflowCommands.Expire => TenantGovernanceActionWorkflowCommands.Expire,
            _ => throw new ArgumentException($"Tenant-governance action workflow command '{command}' is not supported.", nameof(command))
        };
    }

    private static Dictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return metadata
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
