namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Runs bounded tenant-domain ownership proof polling over the governance domain-ownership catalog.
/// </summary>
/// <remarks>
/// The polling runner reduces application glue code by selecting pending or rejected
/// domain-ownership declarations and delegating each proof attempt to
/// <see cref="ITenantDomainOwnershipProofVerificationRunner" />. It owns the
/// on-demand polling loop, not DNS mutation, HTTP file hosting, provider control-plane
/// mutation, or automatic background scheduling.
/// </remarks>
public interface ITenantDomainOwnershipProofPollingRunner
{
    /// <summary>
    /// Runs one bounded polling pass over matching tenant-domain ownership declarations.
    /// </summary>
    /// <param name="request">The proof polling request.</param>
    /// <param name="cancellationToken">A token that cancels the polling pass.</param>
    /// <returns>The aggregate polling result plus the nested verification attempts.</returns>
    ValueTask<TenantDomainOwnershipProofPollingResult> PollAsync(
        TenantDomainOwnershipProofPollingRequest request,
        CancellationToken cancellationToken = default);
}
