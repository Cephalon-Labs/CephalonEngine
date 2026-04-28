namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Evaluates reported tenant-domain ownership proof evidence and applies the resulting verification workflow transition.
/// </summary>
/// <remarks>
/// The evaluator owns proof comparison and workflow mutation. It does not collect DNS records, HTTP files, or external
/// polling evidence itself; applications or provider packs report the observed proof value into this contract.
/// </remarks>
public interface ITenantDomainOwnershipProofEvaluator
{
    /// <summary>
    /// Evaluates reported proof evidence for a tenant-domain ownership declaration.
    /// </summary>
    /// <param name="request">The proof evaluation request.</param>
    /// <param name="cancellationToken">A token that cancels proof evaluation before workflow mutation starts.</param>
    /// <returns>The proof evaluation result and workflow transition outcome.</returns>
    ValueTask<TenantDomainOwnershipProofEvaluationResult> EvaluateAsync(
        TenantDomainOwnershipProofEvaluationRequest request,
        CancellationToken cancellationToken = default);
}
