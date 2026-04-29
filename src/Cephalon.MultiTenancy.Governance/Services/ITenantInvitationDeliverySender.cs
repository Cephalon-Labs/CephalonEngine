namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Sends tenant invitation delivery payloads for a host or provider-specific channel.
/// </summary>
/// <remarks>
/// Sender implementations own external delivery behavior and provider semantics. Cephalon calls the sender only after
/// resolving a pending invitation and records the returned outcome without assuming that every provider can guarantee
/// final recipient delivery.
/// </remarks>
public interface ITenantInvitationDeliverySender
{
    /// <summary>
    /// Gets the stable sender identifier used by configuration, runtime metadata, and diagnostics.
    /// </summary>
    string SenderId { get; }

    /// <summary>
    /// Sends or queues one tenant invitation delivery payload.
    /// </summary>
    /// <param name="context">The delivery context resolved by the governance companion pack.</param>
    /// <param name="cancellationToken">A token that cancels sender execution.</param>
    /// <returns>The provider-specific sender outcome normalized for Cephalon runtime reporting.</returns>
    ValueTask<TenantInvitationDeliverySenderResult> SendAsync(
        TenantInvitationDeliveryContext context,
        CancellationToken cancellationToken = default);
}
