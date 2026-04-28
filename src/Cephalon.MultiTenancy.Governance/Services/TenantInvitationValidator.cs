using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantInvitationValidator(
    MultiTenancyGovernanceOptions options,
    ITenantInvitationCatalog catalog,
    TimeProvider timeProvider,
    ILogger<TenantInvitationValidator> logger) : ITenantInvitationValidator
{
    public ValueTask<TenantInvitationValidationResult> ValidateAsync(
        TenantInvitationValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var validatedAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        var result = options.EnableInvitationValidation
            ? Validate(request, validatedAtUtc)
            : CreateResult(
                request,
                TenantInvitationValidationOutcomes.Disabled,
                valid: false,
                validatedAtUtc,
                matchedRoles: [],
                missingRoles: request.RequiredRoles,
                matchedInvitation: null,
                reason: "Tenant-invitation validation is disabled.");

        if (result.Valid)
        {
            MultiTenancyGovernanceLoggerMessages.InvitationValidationAllowed(
                logger,
                request.TenantId,
                request.InvitationId,
                string.Join(",", result.MatchedRoles),
                null);
        }
        else
        {
            MultiTenancyGovernanceLoggerMessages.InvitationValidationDenied(
                logger,
                request.TenantId,
                request.InvitationId,
                result.Outcome,
                result.Reason ?? "Tenant-invitation validation did not grant access.",
                null);
        }

        return ValueTask.FromResult(result);
    }

    private TenantInvitationValidationResult Validate(
        TenantInvitationValidationRequest request,
        DateTimeOffset validatedAtUtc)
    {
        var invitations = catalog.GetByTenantAndInvitation(request.TenantId, request.InvitationId);
        if (invitations.Count == 0)
        {
            return CreateResult(
                request,
                TenantInvitationValidationOutcomes.NotFound,
                valid: false,
                validatedAtUtc,
                matchedRoles: [],
                missingRoles: request.RequiredRoles,
                matchedInvitation: null,
                reason: "No tenant invitation matched the supplied tenant and invitation identifiers.");
        }

        var invitation = invitations[0];
        if (!MatchesInviteeBoundary(request, invitation))
        {
            return CreateResult(
                request,
                TenantInvitationValidationOutcomes.InviteeMismatch,
                valid: false,
                validatedAtUtc,
                matchedRoles: invitation.Roles,
                missingRoles: request.RequiredRoles,
                matchedInvitation: invitation,
                reason: "Matching tenant invitation did not match the requested invitee boundary.");
        }

        if (string.Equals(invitation.Status, TenantInvitationStatuses.Accepted, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantInvitationValidationOutcomes.Accepted,
                valid: false,
                validatedAtUtc,
                matchedRoles: invitation.Roles,
                missingRoles: request.RequiredRoles,
                matchedInvitation: invitation,
                reason: "Matching tenant invitation has already been accepted.");
        }

        if (string.Equals(invitation.Status, TenantInvitationStatuses.Revoked, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantInvitationValidationOutcomes.Revoked,
                valid: false,
                validatedAtUtc,
                matchedRoles: invitation.Roles,
                missingRoles: request.RequiredRoles,
                matchedInvitation: invitation,
                reason: "Matching tenant invitation has been revoked.");
        }

        if (IsExpired(invitation, validatedAtUtc))
        {
            return CreateResult(
                request,
                TenantInvitationValidationOutcomes.Expired,
                valid: false,
                validatedAtUtc,
                matchedRoles: invitation.Roles,
                missingRoles: request.RequiredRoles,
                matchedInvitation: invitation,
                reason: "Matching tenant invitation is expired.");
        }

        var missingRoles = request.RequiredRoles
            .Where(requiredRole => !invitation.Roles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        if (missingRoles.Length > 0)
        {
            return CreateResult(
                request,
                TenantInvitationValidationOutcomes.MissingRole,
                valid: false,
                validatedAtUtc,
                matchedRoles: invitation.Roles,
                missingRoles,
                matchedInvitation: invitation,
                reason: "Matching tenant invitation did not contain every required role.");
        }

        return CreateResult(
            request,
            TenantInvitationValidationOutcomes.Valid,
            valid: true,
            validatedAtUtc,
            matchedRoles: invitation.Roles,
            missingRoles: [],
            matchedInvitation: invitation,
            reason: "Pending tenant invitation satisfied the request.");
    }

    private static TenantInvitationValidationResult CreateResult(
        TenantInvitationValidationRequest request,
        string outcome,
        bool valid,
        DateTimeOffset validatedAtUtc,
        IReadOnlyList<string> matchedRoles,
        IReadOnlyList<string> missingRoles,
        TenantInvitationDescriptor? matchedInvitation,
        string reason)
    {
        return new TenantInvitationValidationResult(
            request.TenantId,
            request.InvitationId,
            outcome,
            valid,
            validatedAtUtc,
            request.RequiredRoles,
            matchedRoles,
            missingRoles,
            matchedInvitation,
            reason,
            request.Metadata,
            request.InviteeId,
            request.InviteeKind);
    }

    private static bool MatchesInviteeBoundary(TenantInvitationValidationRequest request, TenantInvitationDescriptor invitation)
    {
        if (string.IsNullOrWhiteSpace(request.InviteeId))
        {
            return true;
        }

        return string.Equals(invitation.InviteeId, request.InviteeId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(invitation.InviteeKind, request.InviteeKind, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExpired(TenantInvitationDescriptor invitation, DateTimeOffset validatedAtUtc)
    {
        return string.Equals(invitation.Status, TenantInvitationStatuses.Expired, StringComparison.OrdinalIgnoreCase) ||
            (invitation.ExpiresAtUtc is not null && invitation.ExpiresAtUtc <= validatedAtUtc);
    }
}
