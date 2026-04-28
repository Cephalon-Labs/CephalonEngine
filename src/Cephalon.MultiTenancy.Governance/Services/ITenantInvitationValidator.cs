namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Validates tenant invitations against the active governance runtime.
/// </summary>
public interface ITenantInvitationValidator
{
    /// <summary>
    /// Validates a tenant invitation request.
    /// </summary>
    /// <param name="request">The validation request.</param>
    /// <param name="cancellationToken">A token that cancels validation.</param>
    /// <returns>The validation result.</returns>
    ValueTask<TenantInvitationValidationResult> ValidateAsync(
        TenantInvitationValidationRequest request,
        CancellationToken cancellationToken = default);
}
