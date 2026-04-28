namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Exposes tenant-domain ownership HTTP proof files materialized by the governance companion pack.
/// </summary>
public interface ITenantDomainOwnershipHttpProofPublicationCatalog
{
    /// <summary>
    /// Gets the HTTP proof files currently published by the governance companion pack.
    /// </summary>
    IReadOnlyList<TenantDomainOwnershipHttpProofPublicationDescriptor> PublishedProofs { get; }

    /// <summary>
    /// Finds a published HTTP proof file by request host and path.
    /// </summary>
    /// <param name="hostName">The request host name without a URI scheme.</param>
    /// <param name="httpFilePath">The HTTP path requested by the client.</param>
    /// <returns>The matching published HTTP proof file, or <see langword="null" /> when no published proof matches.</returns>
    TenantDomainOwnershipHttpProofPublicationDescriptor? GetByHostAndPath(string hostName, string httpFilePath);
}
