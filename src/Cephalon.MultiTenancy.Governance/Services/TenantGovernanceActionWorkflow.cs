using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantGovernanceActionWorkflow(
    MultiTenancyGovernanceOptions options,
    ITenantGovernanceActionCatalog catalog,
    TenantGovernanceActionRuntimeStore runtimeStore,
    TimeProvider timeProvider,
    ILogger<TenantGovernanceActionWorkflow> logger) : ITenantGovernanceActionWorkflow
{
    public ValueTask<TenantGovernanceActionWorkflowResult> ApplyAsync(
        TenantGovernanceActionWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var occurredAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        var result = options.EnableGovernanceActionWorkflow
            ? Apply(request, occurredAtUtc)
            : CreateResult(
                request,
                TenantGovernanceActionWorkflowOutcomes.Disabled,
                applied: false,
                occurredAtUtc,
                action: null,
                previousStatus: null,
                currentStatus: null,
                reason: "Tenant-governance action workflow execution is disabled.");

        if (result.Applied)
        {
            MultiTenancyGovernanceLoggerMessages.GovernanceActionWorkflowApplied(
                logger,
                request.TenantId,
                request.ActionId,
                request.Command,
                result.CurrentStatus ?? "unknown",
                null);
        }
        else
        {
            MultiTenancyGovernanceLoggerMessages.GovernanceActionWorkflowDenied(
                logger,
                request.TenantId,
                request.ActionId,
                request.Command,
                result.Outcome,
                result.Reason,
                null);
        }

        return ValueTask.FromResult(result);
    }

    private TenantGovernanceActionWorkflowResult Apply(
        TenantGovernanceActionWorkflowRequest request,
        DateTimeOffset occurredAtUtc)
    {
        var actions = catalog.GetByTenantAndAction(request.TenantId, request.ActionId);
        if (actions.Count == 0)
        {
            var sameAction = catalog.GetByActionId(request.ActionId);
            if (sameAction.Count > 0)
            {
                return CreateResult(
                    request,
                    TenantGovernanceActionWorkflowOutcomes.TenantMismatch,
                    applied: false,
                    occurredAtUtc,
                    sameAction[0],
                    previousStatus: sameAction[0].Status,
                    currentStatus: sameAction[0].Status,
                    "Matching tenant-governance action belongs to a different tenant.");
            }

            return request.Command == TenantGovernanceActionWorkflowCommands.Request
                ? CreateAction(request, occurredAtUtc)
                : CreateResult(
                    request,
                    TenantGovernanceActionWorkflowOutcomes.NotFound,
                    applied: false,
                    occurredAtUtc,
                    action: null,
                    previousStatus: null,
                    currentStatus: null,
                    "No tenant-governance action descriptor matched the supplied action.");
        }

        var action = actions[0];
        if (request.ActionKind is not null &&
            !string.Equals(action.ActionKind, request.ActionKind, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantGovernanceActionWorkflowOutcomes.ActionKindMismatch,
                applied: false,
                occurredAtUtc,
                action,
                action.Status,
                action.Status,
                "Matching tenant-governance action has a different action kind.");
        }

        if (HasSubjectMismatch(request, action))
        {
            return CreateResult(
                request,
                TenantGovernanceActionWorkflowOutcomes.SubjectMismatch,
                applied: false,
                occurredAtUtc,
                action,
                action.Status,
                action.Status,
                "Matching tenant-governance action targets a different subject.");
        }

        return ApplyTransition(request, action, occurredAtUtc);
    }

    private TenantGovernanceActionWorkflowResult CreateAction(
        TenantGovernanceActionWorkflowRequest request,
        DateTimeOffset occurredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(request.ActionKind))
        {
            return CreateResult(
                request,
                TenantGovernanceActionWorkflowOutcomes.InvalidTransition,
                applied: false,
                occurredAtUtc,
                action: null,
                previousStatus: null,
                currentStatus: null,
                "Creating a tenant-governance action requires an action kind.");
        }

        var action = new TenantGovernanceActionDescriptor(
            actionId: request.ActionId,
            tenantId: request.TenantId,
            actionKind: request.ActionKind,
            subjectKind: request.SubjectKind,
            subjectId: request.SubjectId,
            displayName: request.DisplayName,
            status: TenantGovernanceActionStatuses.PendingApproval,
            requestedBy: request.Actor,
            createdAtUtc: occurredAtUtc,
            expiresAtUtc: request.ExpiresAtUtc,
            metadata: BuildMetadata(request, previous: null, currentStatus: TenantGovernanceActionStatuses.PendingApproval));
        runtimeStore.Upsert(action);

        return CreateResult(
            request,
            TenantGovernanceActionWorkflowOutcomes.Created,
            applied: true,
            occurredAtUtc,
            action,
            previousStatus: null,
            currentStatus: action.Status,
            "Tenant-governance action was created and is pending approval.");
    }

    private TenantGovernanceActionWorkflowResult ApplyTransition(
        TenantGovernanceActionWorkflowRequest request,
        TenantGovernanceActionDescriptor action,
        DateTimeOffset occurredAtUtc)
    {
        var targetStatus = ResolveTargetStatus(request.Command, action.Status);
        if (targetStatus is null)
        {
            return CreateResult(
                request,
                TenantGovernanceActionWorkflowOutcomes.InvalidTransition,
                applied: false,
                occurredAtUtc,
                action,
                action.Status,
                action.Status,
                $"Tenant-governance action workflow command '{request.Command}' cannot transition from '{action.Status}'.");
        }

        var transitioned = new TenantGovernanceActionDescriptor(
            actionId: action.ActionId,
            tenantId: action.TenantId,
            actionKind: action.ActionKind,
            subjectKind: action.SubjectKind,
            subjectId: action.SubjectId,
            displayName: request.DisplayName ?? action.DisplayName,
            status: targetStatus,
            requestedBy: action.RequestedBy ?? request.Actor,
            approvedBy: ResolveApprovedBy(request, action, targetStatus),
            createdAtUtc: action.CreatedAtUtc,
            decidedAtUtc: occurredAtUtc,
            expiresAtUtc: request.ExpiresAtUtc ?? action.ExpiresAtUtc,
            sourceModuleId: action.SourceModuleId,
            metadata: BuildMetadata(request, action, targetStatus));
        runtimeStore.Upsert(transitioned);

        return CreateResult(
            request,
            TenantGovernanceActionWorkflowOutcomes.Applied,
            applied: true,
            occurredAtUtc,
            transitioned,
            action.Status,
            transitioned.Status,
            $"Tenant-governance action transitioned from '{action.Status}' to '{transitioned.Status}'.");
    }

    private static string? ResolveTargetStatus(string command, string currentStatus)
    {
        return command switch
        {
            TenantGovernanceActionWorkflowCommands.Request => null,
            TenantGovernanceActionWorkflowCommands.Approve
                when IsStatus(currentStatus, TenantGovernanceActionStatuses.PendingApproval) => TenantGovernanceActionStatuses.Approved,
            TenantGovernanceActionWorkflowCommands.Reject
                when IsStatus(currentStatus, TenantGovernanceActionStatuses.PendingApproval) ||
                    IsStatus(currentStatus, TenantGovernanceActionStatuses.RemediationRequired) => TenantGovernanceActionStatuses.Rejected,
            TenantGovernanceActionWorkflowCommands.RequireRemediation
                when IsStatus(currentStatus, TenantGovernanceActionStatuses.PendingApproval) ||
                    IsStatus(currentStatus, TenantGovernanceActionStatuses.Approved) => TenantGovernanceActionStatuses.RemediationRequired,
            TenantGovernanceActionWorkflowCommands.MarkRemediated
                when IsStatus(currentStatus, TenantGovernanceActionStatuses.RemediationRequired) => TenantGovernanceActionStatuses.Remediated,
            TenantGovernanceActionWorkflowCommands.Expire
                when IsStatus(currentStatus, TenantGovernanceActionStatuses.PendingApproval) ||
                    IsStatus(currentStatus, TenantGovernanceActionStatuses.Approved) ||
                    IsStatus(currentStatus, TenantGovernanceActionStatuses.RemediationRequired) => TenantGovernanceActionStatuses.Expired,
            _ => null
        };
    }

    private static string? ResolveApprovedBy(
        TenantGovernanceActionWorkflowRequest request,
        TenantGovernanceActionDescriptor action,
        string targetStatus)
    {
        return IsStatus(targetStatus, TenantGovernanceActionStatuses.Approved) ||
            IsStatus(targetStatus, TenantGovernanceActionStatuses.Remediated)
            ? request.Actor ?? action.ApprovedBy
            : action.ApprovedBy;
    }

    private static Dictionary<string, string> BuildMetadata(
        TenantGovernanceActionWorkflowRequest request,
        TenantGovernanceActionDescriptor? previous,
        string currentStatus)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (previous is not null)
        {
            foreach (var pair in previous.Metadata)
            {
                metadata[pair.Key] = pair.Value;
            }
        }

        foreach (var pair in request.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        metadata["lastWorkflowCommand"] = request.Command;
        metadata["lastWorkflowStatus"] = currentStatus;
        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            metadata["lastWorkflowActor"] = request.Actor;
        }

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            metadata["lastWorkflowReason"] = request.Reason;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata["lastWorkflowCorrelationId"] = request.CorrelationId;
        }

        return metadata;
    }

    private static TenantGovernanceActionWorkflowResult CreateResult(
        TenantGovernanceActionWorkflowRequest request,
        string outcome,
        bool applied,
        DateTimeOffset occurredAtUtc,
        TenantGovernanceActionDescriptor? action,
        string? previousStatus,
        string? currentStatus,
        string reason)
    {
        return new TenantGovernanceActionWorkflowResult(
            request.TenantId,
            request.ActionId,
            request.Command,
            outcome,
            applied,
            occurredAtUtc,
            previousStatus,
            currentStatus,
            action,
            reason,
            action?.Metadata ?? request.Metadata);
    }

    private static bool HasSubjectMismatch(
        TenantGovernanceActionWorkflowRequest request,
        TenantGovernanceActionDescriptor action)
    {
        return (request.SubjectKind is not null &&
                !string.Equals(action.SubjectKind, request.SubjectKind, StringComparison.OrdinalIgnoreCase)) ||
            (request.SubjectId is not null &&
                !string.Equals(action.SubjectId, request.SubjectId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsStatus(string status, string expected)
    {
        return string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);
    }
}
