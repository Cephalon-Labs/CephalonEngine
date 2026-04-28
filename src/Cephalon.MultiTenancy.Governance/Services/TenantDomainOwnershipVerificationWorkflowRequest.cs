namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes one tenant-domain ownership verification workflow transition request.
/// </summary>
public sealed class TenantDomainOwnershipVerificationWorkflowRequest
{
    /// <summary>
    /// Creates a tenant-domain ownership verification workflow transition request.
    /// </summary>
    /// <param name="command">The workflow command to apply.</param>
    /// <param name="tenantId">The tenant identifier to transition.</param>
    /// <param name="domainName">The domain name to transition.</param>
    /// <param name="displayName">The optional operator-facing domain name.</param>
    /// <param name="verificationMethod">The optional verification method boundary.</param>
    /// <param name="actor">The actor that requested the workflow transition when known.</param>
    /// <param name="reason">The optional operator-facing transition reason.</param>
    /// <param name="evidence">The optional evidence summary observed by the application or provider.</param>
    /// <param name="atUtc">The UTC timestamp used for the transition. The runtime clock is used when omitted.</param>
    /// <param name="expiresAtUtc">The optional UTC timestamp when the ownership declaration expires.</param>
    /// <param name="correlationId">The optional correlation identifier for the workflow transition.</param>
    /// <param name="metadata">Optional transition metadata.</param>
    public TenantDomainOwnershipVerificationWorkflowRequest(
        string command,
        string tenantId,
        string domainName,
        string? displayName = null,
        string? verificationMethod = null,
        string? actor = null,
        string? reason = null,
        string? evidence = null,
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

        if (string.IsNullOrWhiteSpace(domainName))
        {
            throw new ArgumentException("Domain name is required.", nameof(domainName));
        }

        Command = NormalizeCommand(command);
        TenantId = tenantId.Trim();
        DomainName = TenantDomainOwnershipDescriptor.NormalizeDomainName(domainName);
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        VerificationMethod = NormalizeVerificationMethod(verificationMethod);
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Evidence = string.IsNullOrWhiteSpace(evidence) ? null : evidence.Trim();
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
    /// Gets the canonical domain name to transition.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the optional operator-facing domain name.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the optional verification method boundary.
    /// </summary>
    public string? VerificationMethod { get; }

    /// <summary>
    /// Gets the actor that requested the workflow transition when known.
    /// </summary>
    public string? Actor { get; }

    /// <summary>
    /// Gets the optional operator-facing transition reason.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets the optional evidence summary observed by the application or provider.
    /// </summary>
    public string? Evidence { get; }

    /// <summary>
    /// Gets the UTC timestamp used for the transition.
    /// </summary>
    public DateTimeOffset? AtUtc { get; }

    /// <summary>
    /// Gets the optional UTC timestamp when the ownership declaration expires.
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
            TenantDomainOwnershipVerificationWorkflowCommands.Request => TenantDomainOwnershipVerificationWorkflowCommands.Request,
            TenantDomainOwnershipVerificationWorkflowCommands.Verify => TenantDomainOwnershipVerificationWorkflowCommands.Verify,
            TenantDomainOwnershipVerificationWorkflowCommands.Reject => TenantDomainOwnershipVerificationWorkflowCommands.Reject,
            TenantDomainOwnershipVerificationWorkflowCommands.Suspend => TenantDomainOwnershipVerificationWorkflowCommands.Suspend,
            TenantDomainOwnershipVerificationWorkflowCommands.Expire => TenantDomainOwnershipVerificationWorkflowCommands.Expire,
            _ => throw new ArgumentException($"Tenant-domain ownership verification workflow command '{command}' is not supported.", nameof(command))
        };
    }

    private static string? NormalizeVerificationMethod(string? verificationMethod)
    {
        if (string.IsNullOrWhiteSpace(verificationMethod))
        {
            return null;
        }

        var normalized = verificationMethod.Trim().ToLowerInvariant();
        return normalized switch
        {
            TenantDomainVerificationMethods.Manual => TenantDomainVerificationMethods.Manual,
            TenantDomainVerificationMethods.DnsTxt => TenantDomainVerificationMethods.DnsTxt,
            TenantDomainVerificationMethods.HttpFile => TenantDomainVerificationMethods.HttpFile,
            _ => normalized
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
