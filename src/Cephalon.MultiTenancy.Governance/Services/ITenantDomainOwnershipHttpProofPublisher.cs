namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Publishes tenant-domain ownership HTTP proof-file state inside the governance companion pack.
/// </summary>
/// <remarks>
/// The publisher records the proof file path, content type, and fingerprint so host adapters can serve the proof
/// through their own transport-specific endpoints. It does not mutate DNS records or external provider control planes.
/// </remarks>
public interface ITenantDomainOwnershipHttpProofPublisher
{
    /// <summary>
    /// Materializes an HTTP proof-file publication from an issued tenant-domain ownership proof challenge.
    /// </summary>
    /// <param name="request">The HTTP proof publication request.</param>
    /// <param name="cancellationToken">A token that cancels publication before runtime state is stored.</param>
    /// <returns>The HTTP proof publication outcome.</returns>
    ValueTask<TenantDomainOwnershipHttpProofPublicationResult> PublishAsync(
        TenantDomainOwnershipHttpProofPublicationRequest request,
        CancellationToken cancellationToken = default);
}
