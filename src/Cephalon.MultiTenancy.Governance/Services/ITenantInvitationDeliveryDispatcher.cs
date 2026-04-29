namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Dispatches tenant invitation delivery through a registered sender and records delivery outcome metadata.
/// </summary>
/// <remarks>
/// The dispatcher owns host-agnostic lookup, validation, runtime reporting, and outcome persistence. Actual transport
/// delivery, such as email, SMS, chat, or an identity-provider invite, is supplied by <see cref="ITenantInvitationDeliverySender" />
/// implementations registered by the host or an optional provider companion package.
/// </remarks>
public interface ITenantInvitationDeliveryDispatcher
{
    /// <summary>
    /// Dispatches one tenant invitation delivery request.
    /// </summary>
    /// <param name="request">The tenant invitation delivery request.</param>
    /// <param name="cancellationToken">A token that cancels dispatch before a sender is invoked or state is stored.</param>
    /// <returns>The dispatch outcome.</returns>
    ValueTask<TenantInvitationDeliveryResult> DispatchAsync(
        TenantInvitationDeliveryRequest request,
        CancellationToken cancellationToken = default);
}
