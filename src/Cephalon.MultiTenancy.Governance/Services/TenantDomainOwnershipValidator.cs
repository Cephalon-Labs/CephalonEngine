using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipValidator(
    MultiTenancyGovernanceOptions options,
    ITenantDomainOwnershipCatalog catalog,
    TimeProvider timeProvider,
    ILogger<TenantDomainOwnershipValidator> logger) : ITenantDomainOwnershipValidator
{
    public ValueTask<TenantDomainOwnershipValidationResult> ValidateAsync(
        TenantDomainOwnershipValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var validatedAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        var result = options.EnableDomainOwnershipValidation
            ? Validate(request, validatedAtUtc)
            : CreateResult(
                request,
                TenantDomainOwnershipValidationOutcomes.Disabled,
                valid: false,
                validatedAtUtc,
                matchedDomainOwnership: null,
                reason: "Tenant-domain ownership validation is disabled.");

        if (result.Valid)
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipValidationAllowed(
                logger,
                request.TenantId,
                request.DomainName,
                result.MatchedDomainOwnership?.VerificationMethod ?? TenantDomainVerificationMethods.Manual,
                null);
        }
        else
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipValidationDenied(
                logger,
                request.TenantId,
                request.DomainName,
                result.Outcome,
                result.Reason ?? "Tenant-domain ownership validation did not grant ownership use.",
                null);
        }

        return ValueTask.FromResult(result);
    }

    private TenantDomainOwnershipValidationResult Validate(
        TenantDomainOwnershipValidationRequest request,
        DateTimeOffset validatedAtUtc)
    {
        var domainOwnerships = catalog.GetByTenantAndDomain(request.TenantId, request.DomainName);
        if (domainOwnerships.Count == 0)
        {
            var sameDomainOwnerships = catalog.GetByDomainName(request.DomainName);
            return CreateResult(
                request,
                sameDomainOwnerships.Count > 0
                    ? TenantDomainOwnershipValidationOutcomes.TenantMismatch
                    : TenantDomainOwnershipValidationOutcomes.NotFound,
                valid: false,
                validatedAtUtc,
                matchedDomainOwnership: sameDomainOwnerships.Count > 0 ? sameDomainOwnerships[0] : null,
                reason: sameDomainOwnerships.Count > 0
                    ? "Matching tenant-domain ownership belongs to a different tenant."
                    : "No tenant-domain ownership descriptor matched the supplied domain.");
        }

        var domainOwnership = domainOwnerships[0];
        if (string.Equals(domainOwnership.Status, TenantDomainOwnershipStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipValidationOutcomes.Pending,
                valid: false,
                validatedAtUtc,
                domainOwnership,
                "Matching tenant-domain ownership is still pending verification.");
        }

        if (string.Equals(domainOwnership.Status, TenantDomainOwnershipStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipValidationOutcomes.Rejected,
                valid: false,
                validatedAtUtc,
                domainOwnership,
                "Matching tenant-domain ownership was rejected.");
        }

        if (string.Equals(domainOwnership.Status, TenantDomainOwnershipStatuses.Suspended, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipValidationOutcomes.Suspended,
                valid: false,
                validatedAtUtc,
                domainOwnership,
                "Matching tenant-domain ownership is suspended.");
        }

        if (IsExpired(domainOwnership, validatedAtUtc))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipValidationOutcomes.Expired,
                valid: false,
                validatedAtUtc,
                domainOwnership,
                "Matching tenant-domain ownership is expired.");
        }

        return CreateResult(
            request,
            TenantDomainOwnershipValidationOutcomes.Valid,
            valid: true,
            validatedAtUtc,
            domainOwnership,
            "Verified tenant-domain ownership satisfied the request.");
    }

    private static TenantDomainOwnershipValidationResult CreateResult(
        TenantDomainOwnershipValidationRequest request,
        string outcome,
        bool valid,
        DateTimeOffset validatedAtUtc,
        TenantDomainOwnershipDescriptor? matchedDomainOwnership,
        string reason)
    {
        return new TenantDomainOwnershipValidationResult(
            request.TenantId,
            request.DomainName,
            outcome,
            valid,
            validatedAtUtc,
            matchedDomainOwnership,
            reason,
            request.Metadata);
    }

    private static bool IsExpired(TenantDomainOwnershipDescriptor domainOwnership, DateTimeOffset validatedAtUtc)
    {
        return string.Equals(domainOwnership.Status, TenantDomainOwnershipStatuses.Expired, StringComparison.OrdinalIgnoreCase) ||
            (domainOwnership.ExpiresAtUtc is not null && domainOwnership.ExpiresAtUtc <= validatedAtUtc);
    }
}
