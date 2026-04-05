namespace Cephalon.Abstractions.Authorization;

/// <summary>
/// Evaluates access decisions for the current authorization runtime.
/// </summary>
public interface IAuthorizationEvaluator
{
    /// <summary>
    /// Evaluates one authorization request.
    /// </summary>
    /// <param name="subject">The subject requesting access.</param>
    /// <param name="resource">The protected resource being accessed.</param>
    /// <param name="context">The operation-specific authorization context.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes with the resulting authorization decision.</returns>
    ValueTask<AuthorizationDecision> EvaluateAsync(
        AuthorizationSubject subject,
        AuthorizationResource resource,
        AuthorizationContext context,
        CancellationToken cancellationToken = default);
}
