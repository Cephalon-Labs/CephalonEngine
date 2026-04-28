using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantMembershipEvaluator(
    MultiTenancyGovernanceOptions options,
    ITenantMembershipCatalog catalog,
    TimeProvider timeProvider,
    ILogger<TenantMembershipEvaluator> logger) : ITenantMembershipEvaluator
{
    public ValueTask<TenantMembershipEvaluationResult> EvaluateAsync(
        TenantMembershipEvaluationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var evaluatedAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        var result = options.EnableMembershipEvaluation
            ? Evaluate(request, evaluatedAtUtc)
            : CreateResult(
                request,
                TenantMembershipEvaluationOutcomes.Disabled,
                allowed: false,
                evaluatedAtUtc,
                matchedMemberships: [],
                matchedRoles: [],
                missingRoles: request.RequiredRoles,
                reason: "Tenant-membership evaluation is disabled.");

        if (result.Allowed)
        {
            MultiTenancyGovernanceLoggerMessages.MembershipEvaluationAllowed(
                logger,
                request.TenantId,
                request.PrincipalId,
                string.Join(",", result.MatchedRoles),
                null);
        }
        else
        {
            MultiTenancyGovernanceLoggerMessages.MembershipEvaluationDenied(
                logger,
                request.TenantId,
                request.PrincipalId,
                result.Outcome,
                result.Reason ?? "Tenant-membership evaluation did not grant access.",
                null);
        }

        return ValueTask.FromResult(result);
    }

    private TenantMembershipEvaluationResult Evaluate(
        TenantMembershipEvaluationRequest request,
        DateTimeOffset evaluatedAtUtc)
    {
        var memberships = catalog.GetByTenantPrincipalAndKind(request.TenantId, request.PrincipalKind, request.PrincipalId);
        if (memberships.Count == 0)
        {
            return CreateResult(
                request,
                TenantMembershipEvaluationOutcomes.NoMembership,
                allowed: false,
                evaluatedAtUtc,
                memberships,
                matchedRoles: [],
                missingRoles: request.RequiredRoles,
                reason: "No tenant membership matched the supplied tenant and principal.");
        }

        var activeMemberships = memberships
            .Where(membership => IsActive(membership, evaluatedAtUtc))
            .ToArray();
        if (activeMemberships.Length == 0)
        {
            var outcome = memberships.Any(static membership =>
                string.Equals(membership.Status, TenantMembershipStatuses.Suspended, StringComparison.OrdinalIgnoreCase))
                ? TenantMembershipEvaluationOutcomes.Suspended
                : TenantMembershipEvaluationOutcomes.Expired;

            return CreateResult(
                request,
                outcome,
                allowed: false,
                evaluatedAtUtc,
                memberships,
                matchedRoles: [],
                missingRoles: request.RequiredRoles,
                reason: outcome == TenantMembershipEvaluationOutcomes.Suspended
                    ? "Matching tenant memberships are suspended."
                    : "Matching tenant memberships are expired or outside their active time window.");
        }

        var matchedRoles = activeMemberships
            .SelectMany(static membership => membership.Roles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static role => role, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var missingRoles = request.RequiredRoles
            .Where(requiredRole => !matchedRoles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        if (missingRoles.Length > 0)
        {
            return CreateResult(
                request,
                TenantMembershipEvaluationOutcomes.MissingRole,
                allowed: false,
                evaluatedAtUtc,
                activeMemberships,
                matchedRoles,
                missingRoles,
                reason: "Matching tenant memberships did not contain every required role.");
        }

        return CreateResult(
            request,
            TenantMembershipEvaluationOutcomes.Allowed,
            allowed: true,
            evaluatedAtUtc,
            activeMemberships,
            matchedRoles,
            missingRoles: [],
            reason: "Active tenant membership satisfied the request.");
    }

    private static TenantMembershipEvaluationResult CreateResult(
        TenantMembershipEvaluationRequest request,
        string outcome,
        bool allowed,
        DateTimeOffset evaluatedAtUtc,
        IReadOnlyList<TenantMembershipDescriptor> matchedMemberships,
        IReadOnlyList<string> matchedRoles,
        IReadOnlyList<string> missingRoles,
        string reason)
    {
        return new TenantMembershipEvaluationResult(
            request.TenantId,
            request.PrincipalId,
            outcome,
            allowed,
            evaluatedAtUtc,
            request.RequiredRoles,
            matchedRoles,
            missingRoles,
            matchedMemberships,
            reason,
            request.Metadata,
            request.PrincipalKind);
    }

    private static bool IsActive(TenantMembershipDescriptor membership, DateTimeOffset evaluatedAtUtc)
    {
        return string.Equals(membership.Status, TenantMembershipStatuses.Active, StringComparison.OrdinalIgnoreCase) &&
            (membership.EffectiveFromUtc is null || membership.EffectiveFromUtc <= evaluatedAtUtc) &&
            (membership.ExpiresAtUtc is null || membership.ExpiresAtUtc > evaluatedAtUtc);
    }
}
