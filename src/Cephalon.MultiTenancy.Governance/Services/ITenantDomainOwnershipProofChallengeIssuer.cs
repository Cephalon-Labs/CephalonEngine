namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Issues tenant-domain ownership proof challenges and records the expected proof value for later evaluation.
/// </summary>
/// <remarks>
/// The issuer owns challenge generation and runtime metadata mutation. It does not publish DNS records, host HTTP
/// proof files, or poll external endpoints; applications or provider packs publish and observe the issued challenge.
/// </remarks>
public interface ITenantDomainOwnershipProofChallengeIssuer
{
    /// <summary>
    /// Issues or refreshes a proof challenge for a tenant-domain ownership declaration.
    /// </summary>
    /// <param name="request">The proof challenge request.</param>
    /// <param name="cancellationToken">A token that cancels challenge issuance before runtime state is stored.</param>
    /// <returns>The issued challenge details and runtime state outcome.</returns>
    ValueTask<TenantDomainOwnershipProofChallengeResult> IssueAsync(
        TenantDomainOwnershipProofChallengeRequest request,
        CancellationToken cancellationToken = default);
}
