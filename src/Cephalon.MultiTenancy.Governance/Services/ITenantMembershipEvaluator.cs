namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Evaluates whether a principal has active membership in a tenant.
/// </summary>
public interface ITenantMembershipEvaluator
{
    /// <summary>
    /// Evaluates tenant membership for the supplied request.
    /// </summary>
    /// <param name="request">The membership evaluation request.</param>
    /// <param name="cancellationToken">The token that cancels evaluation.</param>
    /// <returns>The membership evaluation result.</returns>
    ValueTask<TenantMembershipEvaluationResult> EvaluateAsync(
        TenantMembershipEvaluationRequest request,
        CancellationToken cancellationToken = default);
}
