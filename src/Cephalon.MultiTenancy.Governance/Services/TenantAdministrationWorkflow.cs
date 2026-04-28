using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantAdministrationWorkflow(
    MultiTenancyGovernanceOptions options,
    ITenantMembershipCatalog membershipCatalog,
    ITenantMembershipStore membershipStore,
    ITenantInvitationCatalog invitationCatalog,
    ITenantInvitationStore invitationStore,
    TimeProvider timeProvider,
    ILogger<TenantAdministrationWorkflow> logger) : ITenantAdministrationWorkflow
{
    public ValueTask<TenantAdministrationWorkflowResult> ApplyAsync(
        TenantAdministrationWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var occurredAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        if (!options.EnableTenantAdministrationWorkflow)
        {
            return ValueTask.FromResult(Denied(
                request,
                targetKind: ResolveTargetKind(request.Command),
                targetId: ResolveTargetId(request),
                outcome: TenantAdministrationWorkflowOutcomes.Disabled,
                occurredAtUtc,
                previousStatus: null,
                currentStatus: null,
                membership: null,
                invitation: null,
                reason: "Tenant-administration workflow execution is disabled."));
        }

        return ValueTask.FromResult(request.Command switch
        {
            TenantAdministrationWorkflowCommands.GrantMembership => ApplyGrantMembership(request, occurredAtUtc),
            TenantAdministrationWorkflowCommands.SuspendMembership => ApplyMembershipStatus(request, TenantMembershipStatuses.Suspended, occurredAtUtc),
            TenantAdministrationWorkflowCommands.ExpireMembership => ApplyMembershipStatus(request, TenantMembershipStatuses.Expired, occurredAtUtc),
            TenantAdministrationWorkflowCommands.IssueInvitation => ApplyIssueInvitation(request, occurredAtUtc),
            TenantAdministrationWorkflowCommands.AcceptInvitation => ApplyInvitationStatus(request, TenantInvitationStatuses.Accepted, occurredAtUtc),
            TenantAdministrationWorkflowCommands.RevokeInvitation => ApplyInvitationStatus(request, TenantInvitationStatuses.Revoked, occurredAtUtc),
            TenantAdministrationWorkflowCommands.ExpireInvitation => ApplyInvitationStatus(request, TenantInvitationStatuses.Expired, occurredAtUtc),
            _ => throw new InvalidOperationException($"Tenant-administration command '{request.Command}' was normalized but not handled.")
        });
    }

    private TenantAdministrationWorkflowResult ApplyGrantMembership(
        TenantAdministrationWorkflowRequest request,
        DateTimeOffset occurredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(request.PrincipalId))
        {
            return Denied(
                request,
                targetKind: "membership",
                targetId: null,
                outcome: TenantAdministrationWorkflowOutcomes.MembershipTargetRequired,
                occurredAtUtc,
                previousStatus: null,
                currentStatus: null,
                membership: null,
                invitation: null,
                reason: "Membership administration requires a principal id.");
        }

        var existing = FindMembership(request);
        var metadata = BuildMetadata(request, existing?.Metadata, TenantAdministrationWorkflowOutcomes.Applied, occurredAtUtc);
        var membership = new TenantMembershipDescriptor(
            tenantId: request.TenantId,
            principalId: request.PrincipalId,
            principalKind: ResolvePrincipalKind(request.PrincipalKind),
            displayName: request.DisplayName ?? existing?.DisplayName,
            roles: request.Roles.Count > 0 ? request.Roles : existing?.Roles,
            status: TenantMembershipStatuses.Active,
            effectiveFromUtc: request.EffectiveFromUtc ?? existing?.EffectiveFromUtc ?? occurredAtUtc,
            expiresAtUtc: request.ExpiresAtUtc,
            sourceModuleId: existing?.SourceModuleId,
            metadata: metadata);

        try
        {
            membershipStore.Upsert(membership);
        }
        catch (Exception exception)
        {
            return StoreFailed(
                request,
                targetKind: "membership",
                targetId: request.PrincipalId,
                occurredAtUtc,
                previousStatus: existing?.Status,
                currentStatus: existing?.Status,
                membership: existing,
                invitation: null,
                storeKind: membershipStore.StoreKind,
                exception);
        }

        MultiTenancyGovernanceLoggerMessages.TenantAdministrationWorkflowApplied(
            logger,
            request.TenantId,
            "membership",
            request.PrincipalId,
            request.Command,
            membership.Status,
            null);

        return Applied(
            request,
            targetKind: "membership",
            targetId: request.PrincipalId,
            occurredAtUtc,
            previousStatus: existing?.Status,
            currentStatus: membership.Status,
            membership,
            invitation: null,
            reason: "Tenant membership was granted or replaced through the administration workflow.",
            metadata);
    }

    private TenantAdministrationWorkflowResult ApplyMembershipStatus(
        TenantAdministrationWorkflowRequest request,
        string status,
        DateTimeOffset occurredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(request.PrincipalId))
        {
            return Denied(
                request,
                targetKind: "membership",
                targetId: null,
                outcome: TenantAdministrationWorkflowOutcomes.MembershipTargetRequired,
                occurredAtUtc,
                previousStatus: null,
                currentStatus: null,
                membership: null,
                invitation: null,
                reason: "Membership administration requires a principal id.");
        }

        var existing = FindMembership(request);
        if (existing is null)
        {
            return Denied(
                request,
                targetKind: "membership",
                targetId: request.PrincipalId,
                outcome: TenantAdministrationWorkflowOutcomes.MembershipNotFound,
                occurredAtUtc,
                previousStatus: null,
                currentStatus: null,
                membership: null,
                invitation: null,
                reason: "The targeted tenant membership was not found.");
        }

        var metadata = BuildMetadata(request, existing.Metadata, TenantAdministrationWorkflowOutcomes.Applied, occurredAtUtc);
        var membership = new TenantMembershipDescriptor(
            tenantId: existing.TenantId,
            principalId: existing.PrincipalId,
            principalKind: existing.PrincipalKind,
            displayName: request.DisplayName ?? existing.DisplayName,
            roles: request.Roles.Count > 0 ? request.Roles : existing.Roles,
            status: status,
            effectiveFromUtc: existing.EffectiveFromUtc,
            expiresAtUtc: status == TenantMembershipStatuses.Expired ? request.ExpiresAtUtc ?? occurredAtUtc : existing.ExpiresAtUtc,
            sourceModuleId: existing.SourceModuleId,
            metadata: metadata);

        try
        {
            membershipStore.Upsert(membership);
        }
        catch (Exception exception)
        {
            return StoreFailed(
                request,
                targetKind: "membership",
                targetId: existing.PrincipalId,
                occurredAtUtc,
                previousStatus: existing.Status,
                currentStatus: existing.Status,
                membership: existing,
                invitation: null,
                storeKind: membershipStore.StoreKind,
                exception);
        }

        MultiTenancyGovernanceLoggerMessages.TenantAdministrationWorkflowApplied(
            logger,
            request.TenantId,
            "membership",
            existing.PrincipalId,
            request.Command,
            membership.Status,
            null);

        return Applied(
            request,
            targetKind: "membership",
            targetId: existing.PrincipalId,
            occurredAtUtc,
            previousStatus: existing.Status,
            currentStatus: membership.Status,
            membership,
            invitation: null,
            reason: $"Tenant membership was moved to '{status}' through the administration workflow.",
            metadata);
    }

    private TenantAdministrationWorkflowResult ApplyIssueInvitation(
        TenantAdministrationWorkflowRequest request,
        DateTimeOffset occurredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(request.InvitationId) ||
            string.IsNullOrWhiteSpace(request.InviteeId))
        {
            return Denied(
                request,
                targetKind: "invitation",
                targetId: request.InvitationId,
                outcome: TenantAdministrationWorkflowOutcomes.InvitationTargetRequired,
                occurredAtUtc,
                previousStatus: null,
                currentStatus: null,
                membership: null,
                invitation: null,
                reason: "Invitation administration requires an invitation id and invitee id.");
        }

        var existing = FindInvitation(request);
        var metadata = BuildMetadata(request, existing?.Metadata, TenantAdministrationWorkflowOutcomes.Applied, occurredAtUtc);
        var invitation = new TenantInvitationDescriptor(
            invitationId: request.InvitationId,
            tenantId: request.TenantId,
            inviteeId: request.InviteeId,
            inviteeKind: ResolvePrincipalKind(request.InviteeKind),
            displayName: request.DisplayName ?? existing?.DisplayName,
            roles: request.Roles.Count > 0 ? request.Roles : existing?.Roles,
            status: TenantInvitationStatuses.Pending,
            createdAtUtc: occurredAtUtc,
            expiresAtUtc: request.ExpiresAtUtc ?? existing?.ExpiresAtUtc,
            sourceModuleId: existing?.SourceModuleId,
            metadata: metadata);

        try
        {
            invitationStore.Upsert(invitation);
        }
        catch (Exception exception)
        {
            return StoreFailed(
                request,
                targetKind: "invitation",
                targetId: request.InvitationId,
                occurredAtUtc,
                previousStatus: existing?.Status,
                currentStatus: existing?.Status,
                membership: null,
                invitation: existing,
                storeKind: invitationStore.StoreKind,
                exception);
        }

        MultiTenancyGovernanceLoggerMessages.TenantAdministrationWorkflowApplied(
            logger,
            request.TenantId,
            "invitation",
            request.InvitationId,
            request.Command,
            invitation.Status,
            null);

        return Applied(
            request,
            targetKind: "invitation",
            targetId: request.InvitationId,
            occurredAtUtc,
            previousStatus: existing?.Status,
            currentStatus: invitation.Status,
            membership: null,
            invitation,
            reason: "Tenant invitation was issued through the administration workflow.",
            metadata);
    }

    private TenantAdministrationWorkflowResult ApplyInvitationStatus(
        TenantAdministrationWorkflowRequest request,
        string status,
        DateTimeOffset occurredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(request.InvitationId))
        {
            return Denied(
                request,
                targetKind: "invitation",
                targetId: null,
                outcome: TenantAdministrationWorkflowOutcomes.InvitationTargetRequired,
                occurredAtUtc,
                previousStatus: null,
                currentStatus: null,
                membership: null,
                invitation: null,
                reason: "Invitation administration requires an invitation id.");
        }

        var existing = FindInvitation(request);
        if (existing is null)
        {
            return Denied(
                request,
                targetKind: "invitation",
                targetId: request.InvitationId,
                outcome: TenantAdministrationWorkflowOutcomes.InvitationNotFound,
                occurredAtUtc,
                previousStatus: null,
                currentStatus: null,
                membership: null,
                invitation: null,
                reason: "The targeted tenant invitation was not found.");
        }

        if (!CanMoveInvitation(existing, status, occurredAtUtc))
        {
            return Denied(
                request,
                targetKind: "invitation",
                targetId: existing.InvitationId,
                outcome: TenantAdministrationWorkflowOutcomes.InvalidInvitationState,
                occurredAtUtc,
                previousStatus: existing.Status,
                currentStatus: existing.Status,
                membership: null,
                invitation: existing,
                reason: "The targeted tenant invitation cannot transition through the requested command.");
        }

        var metadata = BuildMetadata(request, existing.Metadata, TenantAdministrationWorkflowOutcomes.Applied, occurredAtUtc);
        var invitation = new TenantInvitationDescriptor(
            invitationId: existing.InvitationId,
            tenantId: existing.TenantId,
            inviteeId: existing.InviteeId,
            inviteeKind: existing.InviteeKind,
            displayName: request.DisplayName ?? existing.DisplayName,
            roles: request.Roles.Count > 0 ? request.Roles : existing.Roles,
            status: status,
            createdAtUtc: existing.CreatedAtUtc,
            expiresAtUtc: status == TenantInvitationStatuses.Expired ? occurredAtUtc : existing.ExpiresAtUtc,
            sourceModuleId: existing.SourceModuleId,
            metadata: metadata);

        try
        {
            invitationStore.Upsert(invitation);
        }
        catch (Exception exception)
        {
            return StoreFailed(
                request,
                targetKind: "invitation",
                targetId: existing.InvitationId,
                occurredAtUtc,
                previousStatus: existing.Status,
                currentStatus: existing.Status,
                membership: null,
                invitation: existing,
                storeKind: invitationStore.StoreKind,
                exception);
        }

        MultiTenancyGovernanceLoggerMessages.TenantAdministrationWorkflowApplied(
            logger,
            request.TenantId,
            "invitation",
            existing.InvitationId,
            request.Command,
            invitation.Status,
            null);

        return Applied(
            request,
            targetKind: "invitation",
            targetId: existing.InvitationId,
            occurredAtUtc,
            previousStatus: existing.Status,
            currentStatus: invitation.Status,
            membership: null,
            invitation,
            reason: $"Tenant invitation was moved to '{status}' through the administration workflow.",
            metadata);
    }

    private TenantMembershipDescriptor? FindMembership(TenantAdministrationWorkflowRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PrincipalId))
        {
            return null;
        }

        var principalKind = ResolvePrincipalKind(request.PrincipalKind);
        return membershipCatalog.Memberships.FirstOrDefault(membership =>
            string.Equals(membership.TenantId, request.TenantId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(membership.PrincipalKind, principalKind, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(membership.PrincipalId, request.PrincipalId, StringComparison.OrdinalIgnoreCase));
    }

    private TenantInvitationDescriptor? FindInvitation(TenantAdministrationWorkflowRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InvitationId))
        {
            return null;
        }

        return invitationCatalog.Invitations.FirstOrDefault(invitation =>
            string.Equals(invitation.TenantId, request.TenantId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(invitation.InvitationId, request.InvitationId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool CanMoveInvitation(
        TenantInvitationDescriptor invitation,
        string targetStatus,
        DateTimeOffset occurredAtUtc)
    {
        if (string.Equals(targetStatus, TenantInvitationStatuses.Expired, StringComparison.OrdinalIgnoreCase))
        {
            return !string.Equals(invitation.Status, TenantInvitationStatuses.Accepted, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.Equals(invitation.Status, TenantInvitationStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !string.Equals(targetStatus, TenantInvitationStatuses.Accepted, StringComparison.OrdinalIgnoreCase) ||
            invitation.ExpiresAtUtc is null ||
            invitation.ExpiresAtUtc > occurredAtUtc;
    }

    private TenantAdministrationWorkflowResult StoreFailed(
        TenantAdministrationWorkflowRequest request,
        string targetKind,
        string? targetId,
        DateTimeOffset occurredAtUtc,
        string? previousStatus,
        string? currentStatus,
        TenantMembershipDescriptor? membership,
        TenantInvitationDescriptor? invitation,
        string storeKind,
        Exception exception)
    {
        var reason = "Tenant-administration workflow state could not be persisted.";
        MultiTenancyGovernanceLoggerMessages.TenantAdministrationWorkflowDenied(
            logger,
            request.TenantId,
            targetKind,
            targetId ?? "unknown",
            request.Command,
            TenantAdministrationWorkflowOutcomes.StoreFailed,
            reason,
            exception);

        return new TenantAdministrationWorkflowResult(
            request.TenantId,
            request.Command,
            targetKind,
            targetId,
            TenantAdministrationWorkflowOutcomes.StoreFailed,
            applied: false,
            occurredAtUtc,
            previousStatus,
            currentStatus,
            membership,
            invitation,
            reason,
            BuildDeniedMetadata(request, TenantAdministrationWorkflowOutcomes.StoreFailed, occurredAtUtc, storeKind));
    }

    private TenantAdministrationWorkflowResult Denied(
        TenantAdministrationWorkflowRequest request,
        string targetKind,
        string? targetId,
        string outcome,
        DateTimeOffset occurredAtUtc,
        string? previousStatus,
        string? currentStatus,
        TenantMembershipDescriptor? membership,
        TenantInvitationDescriptor? invitation,
        string reason)
    {
        MultiTenancyGovernanceLoggerMessages.TenantAdministrationWorkflowDenied(
            logger,
            request.TenantId,
            targetKind,
            targetId ?? "unknown",
            request.Command,
            outcome,
            reason,
            null);

        return new TenantAdministrationWorkflowResult(
            request.TenantId,
            request.Command,
            targetKind,
            targetId,
            outcome,
            applied: false,
            occurredAtUtc,
            previousStatus,
            currentStatus,
            membership,
            invitation,
            reason,
            BuildDeniedMetadata(request, outcome, occurredAtUtc));
    }

    private static TenantAdministrationWorkflowResult Applied(
        TenantAdministrationWorkflowRequest request,
        string targetKind,
        string targetId,
        DateTimeOffset occurredAtUtc,
        string? previousStatus,
        string currentStatus,
        TenantMembershipDescriptor? membership,
        TenantInvitationDescriptor? invitation,
        string reason,
        IReadOnlyDictionary<string, string> metadata)
    {
        return new TenantAdministrationWorkflowResult(
            request.TenantId,
            request.Command,
            targetKind,
            targetId,
            TenantAdministrationWorkflowOutcomes.Applied,
            applied: true,
            occurredAtUtc,
            previousStatus,
            currentStatus,
            membership,
            invitation,
            reason,
            metadata);
    }

    private static Dictionary<string, string> BuildMetadata(
        TenantAdministrationWorkflowRequest request,
        IReadOnlyDictionary<string, string>? sourceMetadata,
        string outcome,
        DateTimeOffset occurredAtUtc)
    {
        var metadata = CopyMetadata(sourceMetadata);
        foreach (var pair in request.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        metadata[TenantAdministrationWorkflowMetadataKeys.LastAdministrationCommand] = request.Command;
        metadata[TenantAdministrationWorkflowMetadataKeys.LastAdministrationOutcome] = outcome;
        metadata[TenantAdministrationWorkflowMetadataKeys.LastAdministrationOccurredAtUtc] = occurredAtUtc.ToString("O", CultureInfo.InvariantCulture);
        metadata[TenantAdministrationWorkflowMetadataKeys.AdministrationWorkflowOwnership] = "cephalon-managed";

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            metadata[TenantAdministrationWorkflowMetadataKeys.LastAdministrationActor] = request.Actor;
        }

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            metadata[TenantAdministrationWorkflowMetadataKeys.LastAdministrationReason] = request.Reason;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata[TenantAdministrationWorkflowMetadataKeys.LastAdministrationCorrelationId] = request.CorrelationId;
        }

        return metadata;
    }

    private static Dictionary<string, string> BuildDeniedMetadata(
        TenantAdministrationWorkflowRequest request,
        string outcome,
        DateTimeOffset occurredAtUtc,
        string? storeKind = null)
    {
        var metadata = BuildMetadata(request, null, outcome, occurredAtUtc);
        metadata[TenantAdministrationWorkflowMetadataKeys.AdministrationWorkflowOwnership] = "not-applied";
        if (!string.IsNullOrWhiteSpace(storeKind))
        {
            metadata["storeKind"] = storeKind;
        }

        return metadata;
    }

    private static string ResolvePrincipalKind(string? principalKind)
    {
        return string.IsNullOrWhiteSpace(principalKind)
            ? "user"
            : principalKind.Trim();
    }

    private static string ResolveTargetKind(string command)
    {
        return command.Contains("membership", StringComparison.Ordinal)
            ? "membership"
            : "invitation";
    }

    private static string? ResolveTargetId(TenantAdministrationWorkflowRequest request)
    {
        return request.Command.Contains("membership", StringComparison.Ordinal)
            ? request.PrincipalId
            : request.InvitationId;
    }

    private static Dictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in metadata)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                copy[pair.Key.Trim()] = pair.Value;
            }
        }

        return copy;
    }
}
