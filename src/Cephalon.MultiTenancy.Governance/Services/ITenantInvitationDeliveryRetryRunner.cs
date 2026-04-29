namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Runs bounded retries for queued tenant invitation delivery failures.
/// </summary>
public interface ITenantInvitationDeliveryRetryRunner
{
    /// <summary>
    /// Retries pending tenant invitation delivery queue entries.
    /// </summary>
    /// <param name="request">The retry runner request.</param>
    /// <param name="cancellationToken">A token that cancels the retry pass.</param>
    /// <returns>The aggregate retry pass result.</returns>
    ValueTask<TenantInvitationDeliveryRetryResult> RetryPendingAsync(
        TenantInvitationDeliveryRetryRequest? request = null,
        CancellationToken cancellationToken = default);
}
