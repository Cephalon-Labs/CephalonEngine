using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantGovernanceActionDecider(
    MultiTenancyGovernanceOptions options,
    ITenantGovernanceActionCatalog catalog,
    TimeProvider timeProvider,
    ILogger<TenantGovernanceActionDecider> logger) : ITenantGovernanceActionDecider
{
    public ValueTask<TenantGovernanceActionDecisionResult> DecideAsync(
        TenantGovernanceActionDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var decidedAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        var result = options.EnableGovernanceActionDecision
            ? Decide(request, decidedAtUtc)
            : CreateResult(
                request,
                TenantGovernanceActionDecisionOutcomes.Disabled,
                allowed: false,
                decidedAtUtc,
                matchedAction: null,
                reason: "Tenant-governance action decisions are disabled.");

        if (result.Allowed)
        {
            MultiTenancyGovernanceLoggerMessages.GovernanceActionDecisionAllowed(
                logger,
                request.TenantId,
                request.ActionId,
                result.MatchedAction?.ActionKind ?? "unknown",
                null);
        }
        else
        {
            MultiTenancyGovernanceLoggerMessages.GovernanceActionDecisionDenied(
                logger,
                request.TenantId,
                request.ActionId,
                result.Outcome,
                result.Reason ?? "Tenant-governance action decision did not allow execution.",
                null);
        }

        return ValueTask.FromResult(result);
    }

    private TenantGovernanceActionDecisionResult Decide(
        TenantGovernanceActionDecisionRequest request,
        DateTimeOffset decidedAtUtc)
    {
        var actions = catalog.GetByTenantAndAction(request.TenantId, request.ActionId);
        if (actions.Count == 0)
        {
            var sameAction = catalog.GetByActionId(request.ActionId);
            return CreateResult(
                request,
                sameAction.Count > 0
                    ? TenantGovernanceActionDecisionOutcomes.TenantMismatch
                    : TenantGovernanceActionDecisionOutcomes.NotFound,
                allowed: false,
                decidedAtUtc,
                matchedAction: sameAction.Count > 0 ? sameAction[0] : null,
                reason: sameAction.Count > 0
                    ? "Matching tenant-governance action belongs to a different tenant."
                    : "No tenant-governance action descriptor matched the supplied action.");
        }

        var action = actions[0];
        if (request.ActionKind is not null &&
            !string.Equals(action.ActionKind, request.ActionKind, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantGovernanceActionDecisionOutcomes.ActionKindMismatch,
                allowed: false,
                decidedAtUtc,
                action,
                "Matching tenant-governance action has a different action kind.");
        }

        if (HasSubjectMismatch(request, action))
        {
            return CreateResult(
                request,
                TenantGovernanceActionDecisionOutcomes.SubjectMismatch,
                allowed: false,
                decidedAtUtc,
                action,
                "Matching tenant-governance action targets a different subject.");
        }

        if (string.Equals(action.Status, TenantGovernanceActionStatuses.PendingApproval, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantGovernanceActionDecisionOutcomes.PendingApproval,
                allowed: false,
                decidedAtUtc,
                action,
                "Matching tenant-governance action is still pending approval.");
        }

        if (string.Equals(action.Status, TenantGovernanceActionStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantGovernanceActionDecisionOutcomes.Rejected,
                allowed: false,
                decidedAtUtc,
                action,
                "Matching tenant-governance action was rejected.");
        }

        if (string.Equals(action.Status, TenantGovernanceActionStatuses.RemediationRequired, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantGovernanceActionDecisionOutcomes.RemediationRequired,
                allowed: false,
                decidedAtUtc,
                action,
                "Matching tenant-governance action requires remediation.");
        }

        if (IsExpired(action, decidedAtUtc))
        {
            return CreateResult(
                request,
                TenantGovernanceActionDecisionOutcomes.Expired,
                allowed: false,
                decidedAtUtc,
                action,
                "Matching tenant-governance action is expired.");
        }

        return CreateResult(
            request,
            TenantGovernanceActionDecisionOutcomes.Allowed,
            allowed: true,
            decidedAtUtc,
            action,
            "Approved or remediated tenant-governance action satisfied the request.");
    }

    private static TenantGovernanceActionDecisionResult CreateResult(
        TenantGovernanceActionDecisionRequest request,
        string outcome,
        bool allowed,
        DateTimeOffset decidedAtUtc,
        TenantGovernanceActionDescriptor? matchedAction,
        string reason)
    {
        return new TenantGovernanceActionDecisionResult(
            request.TenantId,
            request.ActionId,
            outcome,
            allowed,
            decidedAtUtc,
            matchedAction,
            reason,
            request.Metadata);
    }

    private static bool HasSubjectMismatch(
        TenantGovernanceActionDecisionRequest request,
        TenantGovernanceActionDescriptor action)
    {
        return (request.SubjectKind is not null &&
                !string.Equals(action.SubjectKind, request.SubjectKind, StringComparison.OrdinalIgnoreCase)) ||
            (request.SubjectId is not null &&
                !string.Equals(action.SubjectId, request.SubjectId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsExpired(TenantGovernanceActionDescriptor action, DateTimeOffset decidedAtUtc)
    {
        return string.Equals(action.Status, TenantGovernanceActionStatuses.Expired, StringComparison.OrdinalIgnoreCase) ||
            (action.ExpiresAtUtc is not null && action.ExpiresAtUtc <= decidedAtUtc);
    }
}
