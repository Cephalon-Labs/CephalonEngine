namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Runs the built-in tenant-domain ownership proof verification flow.
/// </summary>
/// <remarks>
/// The runner reduces application glue code by composing challenge issuance,
/// publication planning, reported-proof evaluation, and optional HTTP file
/// proof collection and configured DNS TXT proof collection without claiming
/// DNS mutation, HTTP file hosting, provider control-plane mutation, or
/// automatic background polling ownership.
/// </remarks>
public interface ITenantDomainOwnershipProofVerificationRunner
{
    /// <summary>
    /// Runs one tenant-domain ownership proof verification attempt.
    /// </summary>
    /// <param name="request">The proof verification request.</param>
    /// <param name="cancellationToken">A token that cancels the attempt.</param>
    /// <returns>The proof verification outcome and nested runtime results.</returns>
    ValueTask<TenantDomainOwnershipProofVerificationResult> VerifyAsync(
        TenantDomainOwnershipProofVerificationRequest request,
        CancellationToken cancellationToken = default);
}
