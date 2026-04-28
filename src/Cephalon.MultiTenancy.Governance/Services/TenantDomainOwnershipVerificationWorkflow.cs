using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipVerificationWorkflow(
    MultiTenancyGovernanceOptions options,
    ITenantDomainOwnershipCatalog catalog,
    ITenantDomainOwnershipStore domainOwnershipStore,
    TimeProvider timeProvider,
    ILogger<TenantDomainOwnershipVerificationWorkflow> logger) : ITenantDomainOwnershipVerificationWorkflow
{
    public ValueTask<TenantDomainOwnershipVerificationWorkflowResult> ApplyAsync(
        TenantDomainOwnershipVerificationWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var occurredAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        var result = options.EnableDomainOwnershipVerificationWorkflow
            ? Apply(request, occurredAtUtc)
            : CreateResult(
                request,
                TenantDomainOwnershipVerificationWorkflowOutcomes.Disabled,
                applied: false,
                occurredAtUtc,
                domainOwnership: null,
                previousStatus: null,
                currentStatus: null,
                reason: "Tenant-domain ownership verification workflow execution is disabled.");

        if (result.Applied)
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipVerificationWorkflowApplied(
                logger,
                request.TenantId,
                request.DomainName,
                request.Command,
                result.CurrentStatus ?? "unknown",
                null);
        }
        else
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipVerificationWorkflowDenied(
                logger,
                request.TenantId,
                request.DomainName,
                request.Command,
                result.Outcome,
                result.Reason,
                null);
        }

        return ValueTask.FromResult(result);
    }

    private TenantDomainOwnershipVerificationWorkflowResult Apply(
        TenantDomainOwnershipVerificationWorkflowRequest request,
        DateTimeOffset occurredAtUtc)
    {
        var domainOwnerships = catalog.GetByTenantAndDomain(request.TenantId, request.DomainName);
        if (domainOwnerships.Count == 0)
        {
            var sameDomainOwnerships = catalog.GetByDomainName(request.DomainName);
            if (sameDomainOwnerships.Count > 0)
            {
                return CreateResult(
                    request,
                    TenantDomainOwnershipVerificationWorkflowOutcomes.TenantMismatch,
                    applied: false,
                    occurredAtUtc,
                    sameDomainOwnerships[0],
                    previousStatus: sameDomainOwnerships[0].Status,
                    currentStatus: sameDomainOwnerships[0].Status,
                    "Matching tenant-domain ownership belongs to a different tenant.");
            }

            return request.Command == TenantDomainOwnershipVerificationWorkflowCommands.Request
                ? CreateDomainOwnership(request, occurredAtUtc)
                : CreateResult(
                    request,
                    TenantDomainOwnershipVerificationWorkflowOutcomes.NotFound,
                    applied: false,
                    occurredAtUtc,
                    domainOwnership: null,
                    previousStatus: null,
                    currentStatus: null,
                    "No tenant-domain ownership descriptor matched the supplied domain.");
        }

        var domainOwnership = domainOwnerships[0];
        if (request.VerificationMethod is not null &&
            !string.Equals(domainOwnership.VerificationMethod, request.VerificationMethod, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipVerificationWorkflowOutcomes.VerificationMethodMismatch,
                applied: false,
                occurredAtUtc,
                domainOwnership,
                domainOwnership.Status,
                domainOwnership.Status,
                "Matching tenant-domain ownership uses a different verification method.");
        }

        return ApplyTransition(request, domainOwnership, occurredAtUtc);
    }

    private TenantDomainOwnershipVerificationWorkflowResult CreateDomainOwnership(
        TenantDomainOwnershipVerificationWorkflowRequest request,
        DateTimeOffset occurredAtUtc)
    {
        var domainOwnership = new TenantDomainOwnershipDescriptor(
            tenantId: request.TenantId,
            domainName: request.DomainName,
            displayName: request.DisplayName,
            status: TenantDomainOwnershipStatuses.Pending,
            verificationMethod: request.VerificationMethod ?? TenantDomainVerificationMethods.Manual,
            expiresAtUtc: request.ExpiresAtUtc,
            metadata: BuildMetadata(request, previous: null, currentStatus: TenantDomainOwnershipStatuses.Pending));
        var persisted = Persist(request, domainOwnership, previousStatus: null, currentStatus: domainOwnership.Status, occurredAtUtc);
        if (persisted is not null)
        {
            return persisted;
        }

        return CreateResult(
            request,
            TenantDomainOwnershipVerificationWorkflowOutcomes.Created,
            applied: true,
            occurredAtUtc,
            domainOwnership,
            previousStatus: null,
            currentStatus: domainOwnership.Status,
            "Tenant-domain ownership was created and is pending verification.");
    }

    private TenantDomainOwnershipVerificationWorkflowResult ApplyTransition(
        TenantDomainOwnershipVerificationWorkflowRequest request,
        TenantDomainOwnershipDescriptor domainOwnership,
        DateTimeOffset occurredAtUtc)
    {
        var targetStatus = ResolveTargetStatus(request.Command, domainOwnership.Status);
        if (targetStatus is null)
        {
            return CreateResult(
                request,
                TenantDomainOwnershipVerificationWorkflowOutcomes.InvalidTransition,
                applied: false,
                occurredAtUtc,
                domainOwnership,
                domainOwnership.Status,
                domainOwnership.Status,
                $"Tenant-domain ownership verification workflow command '{request.Command}' cannot transition from '{domainOwnership.Status}'.");
        }

        var transitioned = new TenantDomainOwnershipDescriptor(
            tenantId: domainOwnership.TenantId,
            domainName: domainOwnership.DomainName,
            displayName: request.DisplayName ?? domainOwnership.DisplayName,
            status: targetStatus,
            verificationMethod: request.VerificationMethod ?? domainOwnership.VerificationMethod,
            verifiedAtUtc: ResolveVerifiedAtUtc(domainOwnership, targetStatus, occurredAtUtc),
            expiresAtUtc: request.ExpiresAtUtc ?? domainOwnership.ExpiresAtUtc,
            sourceModuleId: domainOwnership.SourceModuleId,
            metadata: BuildMetadata(request, domainOwnership, targetStatus));
        var persisted = Persist(request, transitioned, domainOwnership.Status, transitioned.Status, occurredAtUtc);
        if (persisted is not null)
        {
            return persisted;
        }

        return CreateResult(
            request,
            TenantDomainOwnershipVerificationWorkflowOutcomes.Applied,
            applied: true,
            occurredAtUtc,
            transitioned,
            domainOwnership.Status,
            transitioned.Status,
            $"Tenant-domain ownership transitioned from '{domainOwnership.Status}' to '{transitioned.Status}'.");
    }

    private TenantDomainOwnershipVerificationWorkflowResult? Persist(
        TenantDomainOwnershipVerificationWorkflowRequest request,
        TenantDomainOwnershipDescriptor domainOwnership,
        string? previousStatus,
        string currentStatus,
        DateTimeOffset occurredAtUtc)
    {
        try
        {
            domainOwnershipStore.Upsert(domainOwnership);
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipStorePersisted(
                logger,
                domainOwnership.TenantId,
                domainOwnership.DomainName,
                domainOwnershipStore.StoreKind,
                domainOwnershipStore.IsDurable.ToString().ToLowerInvariant(),
                null);
            return null;
        }
        catch (Exception exception)
        {
            var reason = $"Tenant-domain ownership verification workflow command '{request.Command}' could not persist domain ownership state.";
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipStorePersistenceFailed(
                logger,
                request.TenantId,
                request.DomainName,
                domainOwnershipStore.StoreKind,
                reason,
                exception);

            return CreateResult(
                request,
                TenantDomainOwnershipVerificationWorkflowOutcomes.StoreFailed,
                applied: false,
                occurredAtUtc,
                domainOwnership,
                previousStatus,
                currentStatus,
                reason);
        }
    }

    private static string? ResolveTargetStatus(string command, string currentStatus)
    {
        return command switch
        {
            TenantDomainOwnershipVerificationWorkflowCommands.Request => null,
            TenantDomainOwnershipVerificationWorkflowCommands.Verify
                when IsStatus(currentStatus, TenantDomainOwnershipStatuses.Pending) ||
                    IsStatus(currentStatus, TenantDomainOwnershipStatuses.Rejected) => TenantDomainOwnershipStatuses.Verified,
            TenantDomainOwnershipVerificationWorkflowCommands.Reject
                when IsStatus(currentStatus, TenantDomainOwnershipStatuses.Pending) => TenantDomainOwnershipStatuses.Rejected,
            TenantDomainOwnershipVerificationWorkflowCommands.Suspend
                when IsStatus(currentStatus, TenantDomainOwnershipStatuses.Verified) => TenantDomainOwnershipStatuses.Suspended,
            TenantDomainOwnershipVerificationWorkflowCommands.Expire
                when IsStatus(currentStatus, TenantDomainOwnershipStatuses.Pending) ||
                    IsStatus(currentStatus, TenantDomainOwnershipStatuses.Verified) ||
                    IsStatus(currentStatus, TenantDomainOwnershipStatuses.Suspended) => TenantDomainOwnershipStatuses.Expired,
            _ => null
        };
    }

    private static DateTimeOffset? ResolveVerifiedAtUtc(
        TenantDomainOwnershipDescriptor domainOwnership,
        string targetStatus,
        DateTimeOffset occurredAtUtc)
    {
        return IsStatus(targetStatus, TenantDomainOwnershipStatuses.Verified)
            ? occurredAtUtc
            : domainOwnership.VerifiedAtUtc;
    }

    private static Dictionary<string, string> BuildMetadata(
        TenantDomainOwnershipVerificationWorkflowRequest request,
        TenantDomainOwnershipDescriptor? previous,
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

        metadata["lastVerificationWorkflowCommand"] = request.Command;
        metadata["lastVerificationWorkflowStatus"] = currentStatus;
        metadata["verificationWorkflowOwnership"] = "cephalon-managed";
        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            metadata["lastVerificationWorkflowActor"] = request.Actor;
        }

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            metadata["lastVerificationWorkflowReason"] = request.Reason;
        }

        if (!string.IsNullOrWhiteSpace(request.Evidence))
        {
            metadata["lastVerificationWorkflowEvidence"] = request.Evidence;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata["lastVerificationWorkflowCorrelationId"] = request.CorrelationId;
        }

        return metadata;
    }

    private static TenantDomainOwnershipVerificationWorkflowResult CreateResult(
        TenantDomainOwnershipVerificationWorkflowRequest request,
        string outcome,
        bool applied,
        DateTimeOffset occurredAtUtc,
        TenantDomainOwnershipDescriptor? domainOwnership,
        string? previousStatus,
        string? currentStatus,
        string reason)
    {
        return new TenantDomainOwnershipVerificationWorkflowResult(
            request.TenantId,
            request.DomainName,
            request.Command,
            outcome,
            applied,
            occurredAtUtc,
            previousStatus,
            currentStatus,
            domainOwnership,
            reason,
            domainOwnership?.Metadata ?? request.Metadata);
    }

    private static bool IsStatus(string status, string expected)
    {
        return string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);
    }
}
