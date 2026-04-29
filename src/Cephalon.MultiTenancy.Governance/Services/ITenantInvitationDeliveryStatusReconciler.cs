namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Reconciles provider or receiver delivery status observations into tenant invitation runtime metadata.
/// </summary>
public interface ITenantInvitationDeliveryStatusReconciler
{
    /// <summary>
    /// Reconciles one delivery status observation for a tenant invitation.
    /// </summary>
    /// <param name="request">The delivery status reconciliation request.</param>
    /// <param name="cancellationToken">The token used to cancel reconciliation.</param>
    /// <returns>The reconciliation result.</returns>
    ValueTask<TenantInvitationDeliveryStatusReconciliationResult> ReconcileAsync(
        TenantInvitationDeliveryStatusReconciliationRequest request,
        CancellationToken cancellationToken = default);
}
