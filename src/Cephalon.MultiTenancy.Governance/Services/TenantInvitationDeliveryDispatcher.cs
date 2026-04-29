using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantInvitationDeliveryDispatcher(
    MultiTenancyGovernanceOptions options,
    ITenantInvitationCatalog invitationCatalog,
    ITenantInvitationStore invitationStore,
    ITenantInvitationDeliveryRetryStore retryQueue,
    IEnumerable<ITenantInvitationDeliverySender> senders,
    TenantInvitationDeliveryRunReporter runReporter,
    TimeProvider timeProvider,
    ILogger<TenantInvitationDeliveryDispatcher> logger) : ITenantInvitationDeliveryDispatcher
{
    private readonly ITenantInvitationDeliverySender[] senders = senders
        .Where(static sender => !string.IsNullOrWhiteSpace(sender.SenderId))
        .OrderBy(static sender => sender.SenderId, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public async ValueTask<TenantInvitationDeliveryResult> DispatchAsync(
        TenantInvitationDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var dispatchedAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        if (!options.EnableInvitationDeliveryDispatch)
        {
            return Complete(Denied(
                request,
                TenantInvitationDeliveryOutcomes.Disabled,
                dispatchedAtUtc,
                invitation: null,
                senderId: null,
                providerMessageId: null,
                reason: "Tenant invitation delivery dispatch is disabled.",
                metadata: BuildMetadata(request, null, null, TenantInvitationDeliveryOutcomes.Disabled, dispatchedAtUtc, null, null)),
                request);
        }

        var invitation = FindInvitation(request);
        if (invitation is null)
        {
            return Complete(Denied(
                request,
                TenantInvitationDeliveryOutcomes.InvitationNotFound,
                dispatchedAtUtc,
                invitation: null,
                senderId: null,
                providerMessageId: null,
                reason: "The targeted tenant invitation was not found.",
                metadata: BuildMetadata(request, null, null, TenantInvitationDeliveryOutcomes.InvitationNotFound, dispatchedAtUtc, null, null)),
                request);
        }

        if (!string.Equals(invitation.Status, TenantInvitationStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return Complete(RecordableDenied(
                request,
                invitation,
                TenantInvitationDeliveryOutcomes.InvitationNotPending,
                dispatchedAtUtc,
                senderId: null,
                providerMessageId: null,
                reason: "The targeted tenant invitation is no longer pending.",
                senderMetadata: null),
                request);
        }

        if (invitation.ExpiresAtUtc is not null && invitation.ExpiresAtUtc <= dispatchedAtUtc)
        {
            return Complete(RecordableDenied(
                request,
                invitation,
                TenantInvitationDeliveryOutcomes.InvitationExpired,
                dispatchedAtUtc,
                senderId: null,
                providerMessageId: null,
                reason: "The targeted tenant invitation expired before dispatch.",
                senderMetadata: null),
                request);
        }

        var sender = ResolveSender(request);
        if (sender is null)
        {
            return Complete(RecordableDenied(
                request,
                invitation,
                TenantInvitationDeliveryOutcomes.SenderNotConfigured,
                dispatchedAtUtc,
                senderId: null,
                providerMessageId: null,
                reason: string.IsNullOrWhiteSpace(request.SenderId)
                    ? "No tenant invitation delivery sender is registered."
                    : $"Tenant invitation delivery sender '{request.SenderId}' is not registered.",
                senderMetadata: null),
                request);
        }

        TenantInvitationDeliverySenderResult senderResult;
        try
        {
            senderResult = await sender.SendAsync(
                new TenantInvitationDeliveryContext(
                    invitation,
                    request.Channel,
                    sender.SenderId,
                    request.Source,
                    request.Actor,
                    dispatchedAtUtc,
                    request.CorrelationId,
                    request.Metadata),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return Complete(RecordableDenied(
                request,
                invitation,
                TenantInvitationDeliveryOutcomes.SenderFailed,
                dispatchedAtUtc,
                sender.SenderId,
                providerMessageId: null,
                reason: "Tenant invitation delivery sender failed before returning a dispatch outcome.",
                senderMetadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["senderError"] = exception.Message
                },
                exception),
                request);
        }

        var outcome = ResolveDispatcherOutcome(senderResult);
        if (!senderResult.Dispatched || outcome != TenantInvitationDeliveryOutcomes.Dispatched)
        {
            return Complete(RecordableDenied(
                request,
                invitation,
                outcome,
                senderResult.DispatchedAtUtc ?? dispatchedAtUtc,
                sender.SenderId,
                senderResult.ProviderMessageId,
                string.IsNullOrWhiteSpace(senderResult.Reason)
                    ? "Tenant invitation delivery sender did not accept dispatch."
                    : senderResult.Reason,
                senderResult.Metadata),
                request);
        }

        var metadata = BuildMetadata(
            request,
            invitation.Metadata,
            senderResult.Metadata,
            TenantInvitationDeliveryOutcomes.Dispatched,
            senderResult.DispatchedAtUtc ?? dispatchedAtUtc,
            sender.SenderId,
            senderResult.ProviderMessageId);
        var recordedInvitation = CreateRecordedInvitation(invitation, metadata);

        if (request.RecordDelivery)
        {
            try
            {
                invitationStore.Upsert(recordedInvitation);
            }
            catch (Exception exception)
            {
                return Complete(Denied(
                    request,
                    TenantInvitationDeliveryOutcomes.StoreFailed,
                    senderResult.DispatchedAtUtc ?? dispatchedAtUtc,
                    invitation,
                    sender.SenderId,
                    senderResult.ProviderMessageId,
                    "Tenant invitation delivery outcome could not be persisted.",
                    BuildMetadata(
                        request,
                        metadata,
                        senderResult.Metadata,
                        TenantInvitationDeliveryOutcomes.StoreFailed,
                        senderResult.DispatchedAtUtc ?? dispatchedAtUtc,
                        sender.SenderId,
                        senderResult.ProviderMessageId),
                    dispatched: true,
                    exception: exception),
                    request);
            }
        }

        var result = new TenantInvitationDeliveryResult(
            request.TenantId,
            request.InvitationId,
            TenantInvitationDeliveryOutcomes.Dispatched,
            dispatched: true,
            recorded: request.RecordDelivery,
            senderResult.DispatchedAtUtc ?? dispatchedAtUtc,
            request.Channel,
            sender.SenderId,
            senderResult.ProviderMessageId,
            request.RecordDelivery ? recordedInvitation : invitation,
            request.RecordDelivery
                ? "Tenant invitation delivery was dispatched and recorded."
                : "Tenant invitation delivery was dispatched without recording metadata.",
            metadata);

        MultiTenancyGovernanceLoggerMessages.TenantInvitationDeliveryDispatched(
            logger,
            result.TenantId,
            result.InvitationId,
            result.SenderId ?? "unknown",
            result.Channel ?? "default",
            null);

        return Complete(result, request);
    }

    private TenantInvitationDescriptor? FindInvitation(TenantInvitationDeliveryRequest request)
    {
        var invitations = invitationCatalog.GetByTenantAndInvitation(request.TenantId, request.InvitationId);
        return invitations.Count == 0 ? null : invitations[0];
    }

    private ITenantInvitationDeliverySender? ResolveSender(TenantInvitationDeliveryRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.SenderId))
        {
            return senders.FirstOrDefault(sender =>
                string.Equals(sender.SenderId, request.SenderId, StringComparison.OrdinalIgnoreCase));
        }

        return senders.FirstOrDefault();
    }

    private TenantInvitationDeliveryResult RecordableDenied(
        TenantInvitationDeliveryRequest request,
        TenantInvitationDescriptor invitation,
        string outcome,
        DateTimeOffset dispatchedAtUtc,
        string? senderId,
        string? providerMessageId,
        string reason,
        IReadOnlyDictionary<string, string>? senderMetadata,
        Exception? exception = null)
    {
        var metadata = BuildMetadata(
            request,
            invitation.Metadata,
            senderMetadata,
            outcome,
            dispatchedAtUtc,
            senderId,
            providerMessageId);
        var recordedInvitation = CreateRecordedInvitation(invitation, metadata);
        var recorded = false;

        if (request.RecordDelivery)
        {
            try
            {
                invitationStore.Upsert(recordedInvitation);
                recorded = true;
            }
            catch (Exception storeException)
            {
                return Denied(
                    request,
                    TenantInvitationDeliveryOutcomes.StoreFailed,
                    dispatchedAtUtc,
                    invitation,
                    senderId,
                    providerMessageId,
                    "Tenant invitation delivery outcome could not be persisted.",
                    BuildMetadata(
                        request,
                        metadata,
                        senderMetadata,
                        TenantInvitationDeliveryOutcomes.StoreFailed,
                        dispatchedAtUtc,
                        senderId,
                        providerMessageId),
                    exception: storeException);
            }
        }

        return Denied(
            request,
            outcome,
            dispatchedAtUtc,
            recorded ? recordedInvitation : invitation,
            senderId,
            providerMessageId,
            reason,
            metadata,
            exception: exception,
            recorded: recorded);
    }

    private TenantInvitationDeliveryResult Denied(
        TenantInvitationDeliveryRequest request,
        string outcome,
        DateTimeOffset dispatchedAtUtc,
        TenantInvitationDescriptor? invitation,
        string? senderId,
        string? providerMessageId,
        string reason,
        IReadOnlyDictionary<string, string> metadata,
        bool dispatched = false,
        bool recorded = false,
        Exception? exception = null)
    {
        MultiTenancyGovernanceLoggerMessages.TenantInvitationDeliveryDispatchDenied(
            logger,
            request.TenantId,
            request.InvitationId,
            outcome,
            reason,
            exception);

        return new TenantInvitationDeliveryResult(
            request.TenantId,
            request.InvitationId,
            outcome,
            dispatched,
            recorded,
            dispatchedAtUtc,
            request.Channel,
            senderId,
            providerMessageId,
            invitation,
            reason,
            metadata);
    }

    private TenantInvitationDeliveryResult Complete(TenantInvitationDeliveryResult result, TenantInvitationDeliveryRequest request)
    {
        var completed = QueueRetryIfNeeded(result, request);
        runReporter.Record(completed);
        return completed;
    }

    private TenantInvitationDeliveryResult QueueRetryIfNeeded(
        TenantInvitationDeliveryResult result,
        TenantInvitationDeliveryRequest request)
    {
        if (!options.EnableInvitationDeliveryRetryQueue ||
            !string.Equals(result.Outcome, TenantInvitationDeliveryOutcomes.SenderFailed, StringComparison.OrdinalIgnoreCase) ||
            request.Metadata.ContainsKey(TenantInvitationDeliveryMetadataKeys.DeliveryRetryExecution))
        {
            return result;
        }

        var metadata = CopyMetadata(result.Metadata);
        var retryId = CreateRetryId(request);
        var maxAttempts = TenantInvitationDeliveryRetryQueueStores.ResolveMaxAttempts(options);
        var existing = retryQueue.GetById(retryId);
        var attemptCount = Math.Max(1, (existing?.AttemptCount ?? 0) + 1);
        var status = attemptCount >= maxAttempts
            ? TenantInvitationDeliveryRetryStatuses.Exhausted
            : TenantInvitationDeliveryRetryStatuses.Pending;
        var nextAttemptAtUtc = status == TenantInvitationDeliveryRetryStatuses.Pending
            ? result.DispatchedAtUtc.AddSeconds(TenantInvitationDeliveryRetryQueueStores.ResolveRetryDelaySeconds(options))
            : result.DispatchedAtUtc;

        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueOwnership] = retryQueue.Ownership;
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueStoreKind] = retryQueue.StoreKind;
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueStoreDurable] = retryQueue.IsDurable.ToString().ToLowerInvariant();
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueEntryId] = retryId;
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueAttempt] = attemptCount.ToString(CultureInfo.InvariantCulture);
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueMaxAttempts] = maxAttempts.ToString(CultureInfo.InvariantCulture);
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueDelaySeconds] =
            TenantInvitationDeliveryRetryQueueStores.ResolveRetryDelaySeconds(options).ToString(CultureInfo.InvariantCulture);
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueNextAttemptAtUtc] =
            nextAttemptAtUtc.ToString("O", CultureInfo.InvariantCulture);
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueLastOutcome] = result.Outcome;
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueLastReason] = result.Reason;
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueLastAttemptAtUtc] =
            result.DispatchedAtUtc.ToString("O", CultureInfo.InvariantCulture);

        try
        {
            retryQueue.Upsert(new TenantInvitationDeliveryRetryDescriptor(
                retryId,
                request.TenantId,
                request.InvitationId,
                request.Channel,
                result.SenderId ?? request.SenderId,
                request.Source,
                request.Actor,
                request.CorrelationId,
                request.RecordDelivery,
                status,
                attemptCount,
                maxAttempts,
                existing?.CreatedAtUtc ?? result.DispatchedAtUtc,
                nextAttemptAtUtc,
                result.DispatchedAtUtc,
                result.Outcome,
                result.Reason,
                metadata));
            metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueOutcome] = status == TenantInvitationDeliveryRetryStatuses.Pending
                ? "queued"
                : "exhausted";
        }
        catch (Exception exception)
        {
            metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueOutcome] = "store-failed";
            metadata["deliveryRetryQueueExceptionType"] = exception.GetType().Name;
        }

        return new TenantInvitationDeliveryResult(
            result.TenantId,
            result.InvitationId,
            result.Outcome,
            result.Dispatched,
            result.Recorded,
            result.DispatchedAtUtc,
            result.Channel,
            result.SenderId,
            result.ProviderMessageId,
            result.Invitation,
            result.Reason,
            metadata);
    }

    private static string ResolveDispatcherOutcome(TenantInvitationDeliverySenderResult senderResult)
    {
        return senderResult.Outcome switch
        {
            TenantInvitationDeliveryOutcomes.Dispatched when senderResult.Dispatched => TenantInvitationDeliveryOutcomes.Dispatched,
            TenantInvitationDeliveryOutcomes.Suppressed => TenantInvitationDeliveryOutcomes.Suppressed,
            _ => TenantInvitationDeliveryOutcomes.SenderFailed
        };
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
        TenantInvitationDeliveryRequest request,
        IReadOnlyDictionary<string, string>? invitationMetadata,
        IReadOnlyDictionary<string, string>? senderMetadata,
        string outcome,
        DateTimeOffset dispatchedAtUtc,
        string? senderId,
        string? providerMessageId)
    {
        var metadata = CopyMetadata(invitationMetadata);
        foreach (var pair in request.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        if (senderMetadata is not null)
        {
            foreach (var pair in senderMetadata.Where(static pair => !string.IsNullOrWhiteSpace(pair.Key)))
            {
                metadata[pair.Key.Trim()] = pair.Value;
            }
        }

        metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryOutcome] = outcome;
        metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryDispatchedAtUtc] = dispatchedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryChannel] = request.Channel;
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryDispatchOwnership] = "cephalon-managed";
        metadata[TenantInvitationDeliveryMetadataKeys.ExternalDeliveryOwnership] =
            string.IsNullOrWhiteSpace(senderId) ? "application-managed" : "provider-managed";

        if (!string.IsNullOrWhiteSpace(senderId))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.LastDeliverySenderId] = senderId;
        }

        if (!string.IsNullOrWhiteSpace(providerMessageId))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId] = providerMessageId;
        }

        if (!string.IsNullOrWhiteSpace(request.Source))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.LastDeliverySource] = request.Source;
        }

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryActor] = request.Actor;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryCorrelationId] = request.CorrelationId;
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

    private static string CreateRetryId(TenantInvitationDeliveryRequest request)
    {
        var senderId = string.IsNullOrWhiteSpace(request.SenderId) ? "default" : request.SenderId.Trim();
        var value = $"{request.TenantId.Trim()}|{request.InvitationId.Trim()}|{request.Channel.Trim()}|{senderId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
