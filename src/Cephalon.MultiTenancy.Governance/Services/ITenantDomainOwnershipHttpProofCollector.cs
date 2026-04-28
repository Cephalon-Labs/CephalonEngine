namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Collects tenant-domain ownership HTTP file proof evidence and evaluates the collected proof through the governance workflow.
/// </summary>
/// <remarks>
/// The collector owns the on-demand HTTP file proof collection path for declarations that use
/// <see cref="TenantDomainVerificationMethods.HttpFile" />. It does not publish the proof file, mutate DNS records,
/// collect DNS TXT values, or run background polling.
/// </remarks>
public interface ITenantDomainOwnershipHttpProofCollector
{
    /// <summary>
    /// Collects and evaluates one tenant-domain ownership HTTP file proof.
    /// </summary>
    /// <param name="request">The HTTP proof collection request.</param>
    /// <param name="cancellationToken">A token that cancels collection before the proof evaluator is invoked.</param>
    /// <returns>The collection result and nested proof-evaluation outcome.</returns>
    ValueTask<TenantDomainOwnershipHttpProofCollectionResult> CollectAsync(
        TenantDomainOwnershipHttpProofCollectionRequest request,
        CancellationToken cancellationToken = default);
}
