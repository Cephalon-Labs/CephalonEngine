using Cephalon.MultiTenancy.Governance.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantInvitationDeliveryRetryRunner(
    MultiTenancyGovernanceOptions options,
    ITenantInvitationDeliveryRetryStore retryQueue,
    ITenantInvitationDeliveryDispatcher dispatcher,
    TimeProvider timeProvider) : ITenantInvitationDeliveryRetryRunner
{
    internal const string DefaultRetrySource = "invitation-delivery-retry-runner";

    public async ValueTask<TenantInvitationDeliveryRetryResult> RetryPendingAsync(
        TenantInvitationDeliveryRetryRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var effectiveRequest = request ?? new TenantInvitationDeliveryRetryRequest();
        var atUtc = effectiveRequest.AtUtc ?? timeProvider.GetUtcNow();
        if (!options.EnableInvitationDeliveryRetryQueue)
        {
            return CreateResult(
                TenantInvitationDeliveryRetryOutcomes.Disabled,
                attemptedCount: 0,
                dispatchedCount: 0,
                failedCount: 0,
                exhaustedCount: 0,
                terminalCount: 0,
                atUtc,
                deliveryResults: []);
        }

        var maxItems = effectiveRequest.MaxItems is > 0
            ? effectiveRequest.MaxItems.Value
            : TenantInvitationDeliveryRetryQueueStores.ResolveMaxItems(options);
        var entries = retryQueue.GetPending(atUtc, maxItems, effectiveRequest.DueOnly);
        if (entries.Count == 0)
        {
            return CreateResult(
                TenantInvitationDeliveryRetryOutcomes.NoPendingRetries,
                attemptedCount: 0,
                dispatchedCount: 0,
                failedCount: 0,
                exhaustedCount: 0,
                terminalCount: 0,
                atUtc,
                deliveryResults: []);
        }

        var deliveryResults = new List<TenantInvitationDeliveryResult>(entries.Count);
        var dispatchedCount = 0;
        var failedCount = 0;
        var exhaustedCount = 0;
        var terminalCount = 0;

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadata = BuildRetryMetadata(entry, effectiveRequest, atUtc);
            var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
                entry.TenantId,
                entry.InvitationId,
                entry.Channel,
                entry.SenderId,
                effectiveRequest.Source ?? entry.Source ?? DefaultRetrySource,
                effectiveRequest.Actor ?? entry.Actor,
                atUtc,
                effectiveRequest.CorrelationId ?? entry.CorrelationId,
                entry.RecordDelivery,
                metadata), cancellationToken).ConfigureAwait(false);
            deliveryResults.Add(result);

            if (result.Dispatched && string.Equals(result.Outcome, TenantInvitationDeliveryOutcomes.Dispatched, StringComparison.OrdinalIgnoreCase))
            {
                retryQueue.Remove(entry.RetryId);
                dispatchedCount++;
                continue;
            }

            var attemptCount = entry.AttemptCount + 1;
            var status = ResolveStatus(result, attemptCount, entry.MaxAttempts);
            var nextAttemptAtUtc = status == TenantInvitationDeliveryRetryStatuses.Pending
                ? atUtc.AddSeconds(TenantInvitationDeliveryRetryQueueStores.ResolveRetryDelaySeconds(options))
                : atUtc;
            var updatedMetadata = CopyMetadata(entry.Metadata);
            updatedMetadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueLastOutcome] = result.Outcome;
            updatedMetadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueLastReason] = result.Reason;
            updatedMetadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueLastAttemptAtUtc] = atUtc.ToString("O", CultureInfo.InvariantCulture);

            retryQueue.Upsert(entry.WithRetryState(
                status,
                attemptCount,
                nextAttemptAtUtc,
                atUtc,
                result.Outcome,
                result.Reason,
                updatedMetadata));

            if (status == TenantInvitationDeliveryRetryStatuses.Exhausted)
            {
                exhaustedCount++;
            }
            else if (status == TenantInvitationDeliveryRetryStatuses.Terminal)
            {
                terminalCount++;
            }
            else
            {
                failedCount++;
            }
        }

        var outcome = dispatchedCount == entries.Count
            ? TenantInvitationDeliveryRetryOutcomes.Retried
            : dispatchedCount > 0 ? TenantInvitationDeliveryRetryOutcomes.Partial : TenantInvitationDeliveryRetryOutcomes.Failed;

        return CreateResult(
            outcome,
            entries.Count,
            dispatchedCount,
            failedCount,
            exhaustedCount,
            terminalCount,
            atUtc,
            deliveryResults);
    }

    private TenantInvitationDeliveryRetryResult CreateResult(
        string outcome,
        int attemptedCount,
        int dispatchedCount,
        int failedCount,
        int exhaustedCount,
        int terminalCount,
        DateTimeOffset atUtc,
        IReadOnlyList<TenantInvitationDeliveryResult> deliveryResults)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueOwnership] = options.EnableInvitationDeliveryRetryQueue ? retryQueue.Ownership : "not-configured",
            [TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueStoreKind] = retryQueue.StoreKind,
            [TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueStoreDurable] = retryQueue.IsDurable.ToString().ToLowerInvariant(),
            [TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueMaxAttempts] =
                TenantInvitationDeliveryRetryQueueStores.ResolveMaxAttempts(options).ToString(CultureInfo.InvariantCulture),
            [TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueDelaySeconds] =
                TenantInvitationDeliveryRetryQueueStores.ResolveRetryDelaySeconds(options).ToString(CultureInfo.InvariantCulture),
            [TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueuePendingCount] =
                retryQueue.Entries.Count(static entry => string.Equals(entry.Status, TenantInvitationDeliveryRetryStatuses.Pending, StringComparison.OrdinalIgnoreCase)).ToString(CultureInfo.InvariantCulture),
            [TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueEntryCount] = retryQueue.Count.ToString(CultureInfo.InvariantCulture)
        };

        return new TenantInvitationDeliveryRetryResult(
            outcome,
            attemptedCount,
            dispatchedCount,
            failedCount,
            exhaustedCount,
            terminalCount,
            retryQueue.Entries.Count(static entry => string.Equals(entry.Status, TenantInvitationDeliveryRetryStatuses.Pending, StringComparison.OrdinalIgnoreCase)),
            atUtc,
            deliveryResults,
            metadata);
    }

    private static string ResolveStatus(TenantInvitationDeliveryResult result, int attemptCount, int maxAttempts)
    {
        if (string.Equals(result.Outcome, TenantInvitationDeliveryOutcomes.SenderFailed, StringComparison.OrdinalIgnoreCase) &&
            attemptCount < maxAttempts)
        {
            return TenantInvitationDeliveryRetryStatuses.Pending;
        }

        if (string.Equals(result.Outcome, TenantInvitationDeliveryOutcomes.SenderFailed, StringComparison.OrdinalIgnoreCase))
        {
            return TenantInvitationDeliveryRetryStatuses.Exhausted;
        }

        return TenantInvitationDeliveryRetryStatuses.Terminal;
    }

    private static Dictionary<string, string> BuildRetryMetadata(
        TenantInvitationDeliveryRetryDescriptor entry,
        TenantInvitationDeliveryRetryRequest request,
        DateTimeOffset atUtc)
    {
        var metadata = CopyMetadata(entry.Metadata);
        foreach (var pair in request.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryExecution] = "true";
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueEntryId] = entry.RetryId;
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueAttempt] =
            (entry.AttemptCount + 1).ToString(CultureInfo.InvariantCulture);
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueMaxAttempts] =
            entry.MaxAttempts.ToString(CultureInfo.InvariantCulture);
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueLastAttemptAtUtc] =
            atUtc.ToString("O", CultureInfo.InvariantCulture);
        metadata[TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueOwnership] = "cephalon-managed";
        return metadata;
    }

    private static Dictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return metadata
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
