namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Validates declared tenant-domain ownership against the active governance runtime.
/// </summary>
public interface ITenantDomainOwnershipValidator
{
    /// <summary>
    /// Validates a tenant-domain ownership request.
    /// </summary>
    /// <param name="request">The validation request.</param>
    /// <param name="cancellationToken">A token that cancels validation.</param>
    /// <returns>The validation result.</returns>
    ValueTask<TenantDomainOwnershipValidationResult> ValidateAsync(
        TenantDomainOwnershipValidationRequest request,
        CancellationToken cancellationToken = default);
}
