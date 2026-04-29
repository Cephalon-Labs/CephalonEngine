using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantInvitationDeliveryStatusReconciler(
    MultiTenancyGovernanceOptions options,
    ITenantInvitationCatalog invitationCatalog,
    ITenantInvitationStore invitationStore,
    TimeProvider timeProvider,
    ILogger<TenantInvitationDeliveryStatusReconciler> logger) : ITenantInvitationDeliveryStatusReconciler
{
    private const string ExpectedProviderMessageIdKey = "expectedDeliveryProviderMessageId";

    public ValueTask<TenantInvitationDeliveryStatusReconciliationResult> ReconcileAsync(
        TenantInvitationDeliveryStatusReconciliationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var observedAtUtc = request.ObservedAtUtc ?? timeProvider.GetUtcNow();
        if (!options.EnableInvitationDeliveryStatusReconciliation)
        {
            return ValueTask.FromResult(Denied(
                request,
                TenantInvitationDeliveryStatusReconciliationOutcomes.Disabled,
                observedAtUtc,
                invitation: null,
                reason: "Tenant invitation delivery status reconciliation is disabled.",
                BuildMetadata(
                    request,
                    invitationMetadata: null,
                    TenantInvitationDeliveryStatusReconciliationOutcomes.Disabled,
                    observedAtUtc,
                    includeObservedStatus: false,
                    reconciliationOwnership: "not-configured")));
        }

        var invitation = FindInvitation(request);
        if (invitation is null)
        {
            return ValueTask.FromResult(Denied(
                request,
                TenantInvitationDeliveryStatusReconciliationOutcomes.InvitationNotFound,
                observedAtUtc,
                invitation: null,
                reason: "The targeted tenant invitation was not found.",
                BuildMetadata(
                    request,
                    invitationMetadata: null,
                    TenantInvitationDeliveryStatusReconciliationOutcomes.InvitationNotFound,
                    observedAtUtc,
                    includeObservedStatus: false,
                    reconciliationOwnership: "cephalon-managed")));
        }

        if (!CanReconcileProviderMessage(request, invitation, out var expectedProviderMessageId, out var mismatchOutcome, out var mismatchReason))
        {
            var metadata = BuildMetadata(
                request,
                invitation.Metadata,
                mismatchOutcome,
                observedAtUtc,
                includeObservedStatus: false,
                reconciliationOwnership: "cephalon-managed");
            metadata[ExpectedProviderMessageIdKey] = expectedProviderMessageId!;

            return ValueTask.FromResult(Denied(
                request,
                mismatchOutcome,
                observedAtUtc,
                invitation,
                mismatchReason,
                metadata));
        }

        var reconciledMetadata = BuildMetadata(
            request,
            invitation.Metadata,
            TenantInvitationDeliveryStatusReconciliationOutcomes.Reconciled,
            observedAtUtc,
            includeObservedStatus: true,
            reconciliationOwnership: "cephalon-managed");
        var recordedInvitation = CreateRecordedInvitation(invitation, reconciledMetadata);

        if (request.RecordStatus)
        {
            try
            {
                invitationStore.Upsert(recordedInvitation);
            }
            catch (Exception exception)
            {
                return ValueTask.FromResult(Denied(
                    request,
                    TenantInvitationDeliveryStatusReconciliationOutcomes.StoreFailed,
                    observedAtUtc,
                    invitation,
                    "Tenant invitation delivery status could not be persisted.",
                    BuildMetadata(
                        request,
                        reconciledMetadata,
                        TenantInvitationDeliveryStatusReconciliationOutcomes.StoreFailed,
                        observedAtUtc,
                        includeObservedStatus: true,
                        reconciliationOwnership: "cephalon-managed"),
                    exception));
            }
        }

        var result = new TenantInvitationDeliveryStatusReconciliationResult(
            request.TenantId,
            request.InvitationId,
            request.Status,
            TenantInvitationDeliveryStatusReconciliationOutcomes.Reconciled,
            reconciled: true,
            recorded: request.RecordStatus,
            observedAtUtc,
            request.ProviderMessageId,
            request.SenderId,
            request.Channel,
            request.RecordStatus ? recordedInvitation : invitation,
            request.RecordStatus
                ? "Tenant invitation delivery status was reconciled and recorded."
                : "Tenant invitation delivery status was reconciled without recording metadata.",
            reconciledMetadata);

        MultiTenancyGovernanceLoggerMessages.TenantInvitationDeliveryStatusReconciled(
            logger,
            result.TenantId,
            result.InvitationId,
            result.Status,
            null);

        return ValueTask.FromResult(result);
    }

    private TenantInvitationDescriptor? FindInvitation(TenantInvitationDeliveryStatusReconciliationRequest request)
    {
        var invitations = invitationCatalog.GetByTenantAndInvitation(request.TenantId, request.InvitationId);
        return invitations.Count == 0 ? null : invitations[0];
    }

    private static bool CanReconcileProviderMessage(
        TenantInvitationDeliveryStatusReconciliationRequest request,
        TenantInvitationDescriptor invitation,
        out string? expectedProviderMessageId,
        out string outcome,
        out string reason)
    {
        expectedProviderMessageId = null;
        outcome = TenantInvitationDeliveryStatusReconciliationOutcomes.Reconciled;
        reason = string.Empty;

        if (!request.RequireProviderMessageMatch ||
            !invitation.Metadata.TryGetValue(TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId, out var existingProviderMessageId) ||
            string.IsNullOrWhiteSpace(existingProviderMessageId))
        {
            return true;
        }

        expectedProviderMessageId = existingProviderMessageId.Trim();
        if (string.IsNullOrWhiteSpace(request.ProviderMessageId))
        {
            outcome = TenantInvitationDeliveryStatusReconciliationOutcomes.ProviderMessageMissing;
            reason = "The invitation has a recorded provider message identifier, but the status observation did not include one.";
            return false;
        }

        if (!string.Equals(expectedProviderMessageId, request.ProviderMessageId, StringComparison.OrdinalIgnoreCase))
        {
            outcome = TenantInvitationDeliveryStatusReconciliationOutcomes.ProviderMessageMismatch;
            reason = "The status observation provider message identifier does not match the recorded dispatch provider message identifier.";
            return false;
        }

        return true;
    }

    private TenantInvitationDeliveryStatusReconciliationResult Denied(
        TenantInvitationDeliveryStatusReconciliationRequest request,
        string outcome,
        DateTimeOffset observedAtUtc,
        TenantInvitationDescriptor? invitation,
        string reason,
        IReadOnlyDictionary<string, string> metadata,
        Exception? exception = null)
    {
        MultiTenancyGovernanceLoggerMessages.TenantInvitationDeliveryStatusReconciliationDenied(
            logger,
            request.TenantId,
            request.InvitationId,
            outcome,
            reason,
            exception);

        return new TenantInvitationDeliveryStatusReconciliationResult(
            request.TenantId,
            request.InvitationId,
            request.Status,
            outcome,
            reconciled: false,
            recorded: false,
            observedAtUtc,
            request.ProviderMessageId,
            request.SenderId,
            request.Channel,
            invitation,
            reason,
            metadata);
    }

    private static TenantInvitationDescriptor CreateRecordedInvitation(
        TenantInvitationDescriptor invitation,
        IReadOnlyDictionary<string, string> metadata)
    {
        return new TenantInvitationDescriptor(
            invitation.InvitationId,
            invitation.TenantId,
            invitation.InviteeId,
            invitation.InviteeKind,
            invitation.DisplayName,
            invitation.Roles,
            invitation.Status,
            invitation.CreatedAtUtc,
            invitation.ExpiresAtUtc,
            invitation.SourceModuleId,
            metadata);
    }

    private static Dictionary<string, string> BuildMetadata(
        TenantInvitationDeliveryStatusReconciliationRequest request,
        IReadOnlyDictionary<string, string>? invitationMetadata,
        string outcome,
        DateTimeOffset observedAtUtc,
        bool includeObservedStatus,
        string reconciliationOwnership)
    {
        var metadata = CopyMetadata(invitationMetadata);
        foreach (var pair in request.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusReconciliationOutcome] = outcome;
        metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusObservedAtUtc] = observedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryStatusReconciliationOwnership] = reconciliationOwnership;
        metadata[TenantInvitationDeliveryMetadataKeys.ExternalDeliveryStatusOwnership] =
            reconciliationOwnership == "cephalon-managed" ? "provider-managed" : "application-managed";

        if (!includeObservedStatus)
        {
            return metadata;
        }

        metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus] = request.Status;

        if (!string.IsNullOrWhiteSpace(request.ProviderMessageId))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusProviderMessageId] = request.ProviderMessageId;
        }

        if (!string.IsNullOrWhiteSpace(request.SenderId))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusSenderId] = request.SenderId;
        }

        if (!string.IsNullOrWhiteSpace(request.Channel))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusChannel] = request.Channel;
        }

        if (!string.IsNullOrWhiteSpace(request.Source))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusSource] = request.Source;
        }

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusActor] = request.Actor;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusCorrelationId] = request.CorrelationId;
        }

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusReason] = request.Reason;
        }

        return metadata;
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
