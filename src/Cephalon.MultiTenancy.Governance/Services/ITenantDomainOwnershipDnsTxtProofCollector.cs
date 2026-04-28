namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Collects tenant-domain ownership DNS TXT proof evidence and evaluates the collected proof through the governance workflow.
/// </summary>
/// <remarks>
/// The collector owns the on-demand DNS TXT proof lookup path for declarations that use
/// <see cref="TenantDomainVerificationMethods.DnsTxt" />. It does not mutate DNS provider records,
/// publish proof values, or run background polling.
/// </remarks>
public interface ITenantDomainOwnershipDnsTxtProofCollector
{
    /// <summary>
    /// Collects and evaluates one tenant-domain ownership DNS TXT proof.
    /// </summary>
    /// <param name="request">The DNS TXT proof collection request.</param>
    /// <param name="cancellationToken">A token that cancels collection before the proof evaluator is invoked.</param>
    /// <returns>The collection result and nested proof-evaluation outcome.</returns>
    ValueTask<TenantDomainOwnershipDnsTxtProofCollectionResult> CollectAsync(
        TenantDomainOwnershipDnsTxtProofCollectionRequest request,
        CancellationToken cancellationToken = default);
}
