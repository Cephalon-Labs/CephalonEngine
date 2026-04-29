using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class MultiTenancyGovernanceInvitationRuntimeSurfaceContributor(
    MultiTenancyGovernanceOptions options,
    ITenantInvitationCatalog catalog,
    ITenantInvitationStore invitationStore,
    ITenantInvitationDeliveryStatusObservationStore observationStore,
    ITenantInvitationDeliveryRetryStore retryQueue,
    IEnumerable<ITenantInvitationContributor> contributors,
    IEnumerable<ITenantInvitationDeliverySender> deliverySenders,
    ITenantInvitationDeliveryRunCatalog deliveryRunCatalog) : ITechnologyRuntimeContributor
{
    private readonly ITenantInvitationContributor[] contributors = contributors.ToArray();
    private readonly ITenantInvitationDeliverySender[] deliverySenders = deliverySenders
        .Where(static sender => !string.IsNullOrWhiteSpace(sender.SenderId))
        .OrderBy(static sender => sender.SenderId, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var invitations = catalog.Invitations;
        var observations = observationStore.Observations;
        var deliveryRetryEntries = retryQueue.Entries;
        var entries = new List<TechnologyRuntimeEntry>
        {
            CreateSummaryEntry(invitations, observations, deliveryRetryEntries)
        };

        entries.AddRange(invitations
            .GroupBy(static invitation => invitation.TenantId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => CreateTenantEntry(group, observations, deliveryRetryEntries)));

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-invitations",
            displayName: "Tenant Invitations",
            description: "Projects tenant invitation catalog, validation, delivery dispatch, retry queue, delivery status reconciliation, delivery status observation storage, and delivery outcome truth from the governance companion pack.",
            entries: entries);
    }

    private TechnologyRuntimeEntry CreateSummaryEntry(
        IReadOnlyList<TenantInvitationDescriptor> invitations,
        IReadOnlyList<TenantInvitationDeliveryStatusObservationDescriptor> observations,
        IReadOnlyList<TenantInvitationDeliveryRetryDescriptor> deliveryRetryEntries)
    {
        var latestDeliveryStatusInvitation = FindLatestDeliveryStatusInvitation(invitations);
        var latestObservation = FindLatestDeliveryStatusObservation(observations);
        var latestRetryEntry = FindLatestDeliveryRetryEntry(deliveryRetryEntries);
        var deliveryRetryPendingCount = CountDeliveryRetryEntries(deliveryRetryEntries, TenantInvitationDeliveryRetryStatuses.Pending);
        var deliveryRetryExhaustedCount = CountDeliveryRetryEntries(deliveryRetryEntries, TenantInvitationDeliveryRetryStatuses.Exhausted);
        var deliveryRetryTerminalCount = CountDeliveryRetryEntries(deliveryRetryEntries, TenantInvitationDeliveryRetryStatuses.Terminal);
        var statusBreakdown = invitations
            .GroupBy(static invitation => invitation.Status, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => $"{group.Key}:{group.Count().ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "cephalon-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance",
            ["runtimeState"] = invitations.Count > 0 ? "configured" : "empty",
            ["invitationCount"] = invitations.Count.ToString(CultureInfo.InvariantCulture),
            ["tenantCount"] = invitations
                .Select(static invitation => invitation.TenantId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count()
                .ToString(CultureInfo.InvariantCulture),
            ["contributorCount"] = contributors.Length.ToString(CultureInfo.InvariantCulture),
            ["configuredInvitationCount"] = options.Invitations.Count.ToString(CultureInfo.InvariantCulture),
            ["runtimeInvitationCount"] = invitationStore.Count.ToString(CultureInfo.InvariantCulture),
            ["invitationStoreKind"] = invitationStore.StoreKind,
            ["invitationStoreDurable"] = invitationStore.IsDurable.ToString().ToLowerInvariant(),
            ["invitationStoreOwnership"] = invitationStore.Ownership,
            ["validationEnabled"] = options.EnableInvitationValidation.ToString().ToLowerInvariant(),
            ["validationOwnership"] = options.EnableInvitationValidation ? "cephalon-managed" : "not-configured",
            ["deliveryDispatchEnabled"] = options.EnableInvitationDeliveryDispatch.ToString().ToLowerInvariant(),
            ["deliveryDispatchOwnership"] = options.EnableInvitationDeliveryDispatch ? "cephalon-managed" : "not-configured",
            ["deliveryStatusReconciliationEnabled"] = options.EnableInvitationDeliveryStatusReconciliation.ToString().ToLowerInvariant(),
            ["deliveryStatusReconciliationOwnership"] = options.EnableInvitationDeliveryStatusReconciliation ? "cephalon-managed" : "not-configured",
            ["externalDeliveryStatusOwnership"] = options.EnableInvitationDeliveryStatusReconciliation ? "provider-managed" : "application-managed",
            ["deliveryStatusObservationStoreEnabled"] = options.EnableInvitationDeliveryStatusObservationStore.ToString().ToLowerInvariant(),
            ["deliveryStatusObservationStoreKind"] = observationStore.StoreKind,
            ["deliveryStatusObservationStoreDurable"] = observationStore.IsDurable.ToString().ToLowerInvariant(),
            ["deliveryStatusObservationStoreOwnership"] = options.EnableInvitationDeliveryStatusObservationStore ? observationStore.Ownership : "not-configured",
            ["deliveryStatusObservationStoreScope"] = observationStore.IsDurable ? "local-file" : "process-local",
            ["deliveryStatusObservationStoreDurability"] = observationStore.IsDurable ? "local-file" : "none",
            ["deliveryStatusObservationHistoryLimit"] =
                TenantInvitationDeliveryStatusObservationStores.ResolveHistoryLimit(options).ToString(CultureInfo.InvariantCulture),
            ["deliveryStatusObservationCount"] = options.EnableInvitationDeliveryStatusObservationStore
                ? observationStore.Count.ToString(CultureInfo.InvariantCulture)
                : "0",
            ["latestDeliveryStatusObservationId"] = latestObservation?.ObservationId ?? "none",
            ["latestDeliveryStatusObservationOutcome"] = latestObservation?.Outcome ?? "none",
            ["latestDeliveryStatusObservationAtUtc"] =
                latestObservation?.ObservedAtUtc.ToString("O", CultureInfo.InvariantCulture) ?? "none",
            ["deliveryStatusReportedCount"] = CountDeliveryStatusReports(invitations).ToString(CultureInfo.InvariantCulture),
            ["latestDeliveryStatus"] = GetDeliveryStatus(latestDeliveryStatusInvitation),
            ["latestDeliveryStatusObservedAtUtc"] = GetDeliveryStatusObservedAtUtc(latestDeliveryStatusInvitation),
            ["deliverySenderCount"] = deliverySenders.Length.ToString(CultureInfo.InvariantCulture),
            ["deliverySenderIds"] = deliverySenders.Length == 0 ? "none" : string.Join(",", deliverySenders.Select(static sender => sender.SenderId)),
            ["deliverySenderOwnership"] = deliverySenders.Length == 0 ? "not-configured" : "provider-managed",
            ["externalDeliveryOwnership"] = deliverySenders.Length == 0 ? "application-managed" : "provider-managed",
            ["invitationDeliveryOwnership"] = options.EnableInvitationDeliveryDispatch && deliverySenders.Length > 0 ? "mixed" : "application-managed",
            ["deliveryRunCount"] = deliveryRunCatalog.Count.ToString(CultureInfo.InvariantCulture),
            ["deliveryRunHistoryLimit"] = Math.Max(1, options.InvitationDeliveryRunHistoryLimit).ToString(CultureInfo.InvariantCulture),
            ["latestDeliveryOutcome"] = deliveryRunCatalog.LatestRun?.Outcome ?? "none",
            ["latestDeliveryAtUtc"] = deliveryRunCatalog.LatestRun?.DispatchedAtUtc.ToString("O", CultureInfo.InvariantCulture) ?? "none",
            ["deliveryRetryQueueEnabled"] = options.EnableInvitationDeliveryRetryQueue.ToString().ToLowerInvariant(),
            ["deliveryRetryQueueOwnership"] = options.EnableInvitationDeliveryRetryQueue ? retryQueue.Ownership : "not-configured",
            ["deliveryRetryQueueStoreKind"] = retryQueue.StoreKind,
            ["deliveryRetryQueueStoreDurable"] = retryQueue.IsDurable.ToString().ToLowerInvariant(),
            ["deliveryRetryQueueScope"] = retryQueue.IsDurable ? "local-file" : "process-local",
            ["deliveryRetryQueueDurability"] = retryQueue.IsDurable ? "local-file" : "none",
            ["deliveryRetryQueueCount"] = options.EnableInvitationDeliveryRetryQueue
                ? deliveryRetryEntries.Count.ToString(CultureInfo.InvariantCulture)
                : "0",
            ["deliveryRetryQueuePendingCount"] = options.EnableInvitationDeliveryRetryQueue
                ? deliveryRetryPendingCount.ToString(CultureInfo.InvariantCulture)
                : "0",
            ["deliveryRetryQueueExhaustedCount"] = options.EnableInvitationDeliveryRetryQueue
                ? deliveryRetryExhaustedCount.ToString(CultureInfo.InvariantCulture)
                : "0",
            ["deliveryRetryQueueTerminalCount"] = options.EnableInvitationDeliveryRetryQueue
                ? deliveryRetryTerminalCount.ToString(CultureInfo.InvariantCulture)
                : "0",
            ["deliveryRetryMaxAttempts"] = TenantInvitationDeliveryRetryQueueStores.ResolveMaxAttempts(options).ToString(CultureInfo.InvariantCulture),
            ["deliveryRetryDelaySeconds"] = TenantInvitationDeliveryRetryQueueStores.ResolveRetryDelaySeconds(options).ToString(CultureInfo.InvariantCulture),
            ["deliveryRetryMaxItems"] = TenantInvitationDeliveryRetryQueueStores.ResolveMaxItems(options).ToString(CultureInfo.InvariantCulture),
            ["latestDeliveryRetryOutcome"] = latestRetryEntry?.LastOutcome ?? "none",
            ["latestDeliveryRetryStatus"] = latestRetryEntry?.Status ?? "none",
            ["latestDeliveryRetryNextAttemptAtUtc"] = latestRetryEntry?.NextAttemptAtUtc.ToString("O", CultureInfo.InvariantCulture) ?? "none",
            ["latestDeliveryRetryLastAttemptAtUtc"] =
                latestRetryEntry?.LastAttemptAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "none",
            ["durableRetryQueueOwnership"] = !options.EnableInvitationDeliveryRetryQueue
                ? "not-configured"
                : retryQueue.IsDurable ? retryQueue.Ownership : "application-managed",
            ["durableStoreOwnership"] = invitationStore.IsDurable ? invitationStore.Ownership : "application-managed",
            ["basePackageOwnership"] = "separate-companion",
            ["statusBreakdown"] = statusBreakdown.Length == 0 ? "none" : string.Join(",", statusBreakdown)
        };

        return new TechnologyRuntimeEntry(
            id: "tenant-invitation-runtime",
            displayName: "Tenant Invitation Runtime",
            description: "Summarizes tenant invitation catalog size, contributor count, runtime store posture, invitation status posture, delivery dispatch, retry queue, delivery status reconciliation, delivery status observation storage, and managed validation ownership.",
            metadata: metadata);
    }

    private TechnologyRuntimeEntry CreateTenantEntry(
        IGrouping<string, TenantInvitationDescriptor> group,
        IReadOnlyList<TenantInvitationDeliveryStatusObservationDescriptor> observations,
        IReadOnlyList<TenantInvitationDeliveryRetryDescriptor> deliveryRetryEntries)
    {
        var invitations = group.ToArray();
        var tenantObservations = observations
            .Where(observation => string.Equals(observation.TenantId, group.Key, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var tenantDeliveryRetries = deliveryRetryEntries
            .Where(entry => string.Equals(entry.TenantId, group.Key, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var deliveryRuns = deliveryRunCatalog.GetByTenantId(group.Key);
        var latestDeliveryRun = deliveryRuns
            .OrderByDescending(static run => run.DispatchedAtUtc)
            .FirstOrDefault();
        var latestDeliveryStatusInvitation = FindLatestDeliveryStatusInvitation(invitations);
        var latestObservation = FindLatestDeliveryStatusObservation(tenantObservations);
        var latestRetryEntry = FindLatestDeliveryRetryEntry(tenantDeliveryRetries);
        var pendingCount = invitations.Count(static invitation =>
            string.Equals(invitation.Status, TenantInvitationStatuses.Pending, StringComparison.OrdinalIgnoreCase));
        var acceptedCount = invitations.Count(static invitation =>
            string.Equals(invitation.Status, TenantInvitationStatuses.Accepted, StringComparison.OrdinalIgnoreCase));
        var revokedCount = invitations.Count(static invitation =>
            string.Equals(invitation.Status, TenantInvitationStatuses.Revoked, StringComparison.OrdinalIgnoreCase));
        var expiredCount = invitations.Count(static invitation =>
            string.Equals(invitation.Status, TenantInvitationStatuses.Expired, StringComparison.OrdinalIgnoreCase));
        var roles = invitations
            .SelectMany(static invitation => invitation.Roles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static role => role, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var inviteeKindBreakdown = invitations
            .GroupBy(static invitation => invitation.InviteeKind, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static kind => kind.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static kind => $"{kind.Key}:{kind.Count().ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        var sourceModuleIds = invitations
            .Select(static invitation => invitation.SourceModuleId)
            .Where(static sourceModuleId => !string.IsNullOrWhiteSpace(sourceModuleId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static sourceModuleId => sourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "cephalon-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance",
            ["tenantId"] = group.Key,
            ["invitationCount"] = invitations.Length.ToString(CultureInfo.InvariantCulture),
            ["pendingInvitationCount"] = pendingCount.ToString(CultureInfo.InvariantCulture),
            ["acceptedInvitationCount"] = acceptedCount.ToString(CultureInfo.InvariantCulture),
            ["revokedInvitationCount"] = revokedCount.ToString(CultureInfo.InvariantCulture),
            ["expiredInvitationCount"] = expiredCount.ToString(CultureInfo.InvariantCulture),
            ["roleCount"] = roles.Length.ToString(CultureInfo.InvariantCulture),
            ["roles"] = roles.Length == 0 ? "none" : string.Join(",", roles),
            ["inviteeKindBreakdown"] = inviteeKindBreakdown.Length == 0 ? "none" : string.Join(",", inviteeKindBreakdown),
            ["sourceModuleIds"] = sourceModuleIds.Length == 0 ? "none" : string.Join(",", sourceModuleIds),
            ["deliveryRunCount"] = deliveryRuns.Count.ToString(CultureInfo.InvariantCulture),
            ["latestDeliveryOutcome"] = latestDeliveryRun?.Outcome ?? "none",
            ["latestDeliveryAtUtc"] = latestDeliveryRun?.DispatchedAtUtc.ToString("O", CultureInfo.InvariantCulture) ?? "none",
            ["deliveryStatusObservationCount"] = options.EnableInvitationDeliveryStatusObservationStore
                ? tenantObservations.Length.ToString(CultureInfo.InvariantCulture)
                : "0",
            ["latestDeliveryStatusObservationId"] = latestObservation?.ObservationId ?? "none",
            ["latestDeliveryStatusObservationOutcome"] = latestObservation?.Outcome ?? "none",
            ["latestDeliveryStatusObservationAtUtc"] =
                latestObservation?.ObservedAtUtc.ToString("O", CultureInfo.InvariantCulture) ?? "none",
            ["deliveryStatusReportedCount"] = CountDeliveryStatusReports(invitations).ToString(CultureInfo.InvariantCulture),
            ["latestDeliveryStatus"] = GetDeliveryStatus(latestDeliveryStatusInvitation),
            ["latestDeliveryStatusObservedAtUtc"] = GetDeliveryStatusObservedAtUtc(latestDeliveryStatusInvitation),
            ["deliveryRetryQueueCount"] = options.EnableInvitationDeliveryRetryQueue
                ? tenantDeliveryRetries.Length.ToString(CultureInfo.InvariantCulture)
                : "0",
            ["deliveryRetryQueuePendingCount"] = options.EnableInvitationDeliveryRetryQueue
                ? CountDeliveryRetryEntries(tenantDeliveryRetries, TenantInvitationDeliveryRetryStatuses.Pending).ToString(CultureInfo.InvariantCulture)
                : "0",
            ["deliveryRetryQueueExhaustedCount"] = options.EnableInvitationDeliveryRetryQueue
                ? CountDeliveryRetryEntries(tenantDeliveryRetries, TenantInvitationDeliveryRetryStatuses.Exhausted).ToString(CultureInfo.InvariantCulture)
                : "0",
            ["latestDeliveryRetryOutcome"] = latestRetryEntry?.LastOutcome ?? "none",
            ["latestDeliveryRetryStatus"] = latestRetryEntry?.Status ?? "none",
            ["latestDeliveryRetryNextAttemptAtUtc"] = latestRetryEntry?.NextAttemptAtUtc.ToString("O", CultureInfo.InvariantCulture) ?? "none"
        };

        return new TechnologyRuntimeEntry(
            id: $"tenant-invitations:{group.Key}",
            displayName: $"Tenant Invitations: {group.Key}",
            description: "Summarizes invitation posture for one tenant without exposing individual invitee identifiers.",
            metadata: metadata);
    }

    private static int CountDeliveryStatusReports(IEnumerable<TenantInvitationDescriptor> invitations)
    {
        return invitations.Count(static invitation =>
            invitation.Metadata.ContainsKey(TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus));
    }

    private static TenantInvitationDescriptor? FindLatestDeliveryStatusInvitation(IEnumerable<TenantInvitationDescriptor> invitations)
    {
        return invitations
            .Where(static invitation => invitation.Metadata.ContainsKey(TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus))
            .OrderByDescending(GetDeliveryStatusSortValue)
            .FirstOrDefault();
    }

    private static TenantInvitationDeliveryStatusObservationDescriptor? FindLatestDeliveryStatusObservation(
        IEnumerable<TenantInvitationDeliveryStatusObservationDescriptor> observations)
    {
        return observations
            .OrderByDescending(static observation => observation.ObservedAtUtc)
            .ThenByDescending(static observation => observation.RecordedAtUtc)
            .FirstOrDefault();
    }

    private static TenantInvitationDeliveryRetryDescriptor? FindLatestDeliveryRetryEntry(
        IEnumerable<TenantInvitationDeliveryRetryDescriptor> entries)
    {
        return entries
            .OrderByDescending(static entry => entry.LastAttemptAtUtc ?? entry.CreatedAtUtc)
            .ThenByDescending(static entry => entry.NextAttemptAtUtc)
            .FirstOrDefault();
    }

    private static int CountDeliveryRetryEntries(
        IEnumerable<TenantInvitationDeliveryRetryDescriptor> entries,
        string status)
    {
        return entries.Count(entry => string.Equals(entry.Status, status, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetDeliveryStatus(TenantInvitationDescriptor? invitation)
    {
        if (invitation is null ||
            !invitation.Metadata.TryGetValue(TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus, out var status) ||
            string.IsNullOrWhiteSpace(status))
        {
            return "none";
        }

        return status;
    }

    private static string GetDeliveryStatusObservedAtUtc(TenantInvitationDescriptor? invitation)
    {
        if (invitation is null ||
            !invitation.Metadata.TryGetValue(TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusObservedAtUtc, out var observedAtUtc) ||
            string.IsNullOrWhiteSpace(observedAtUtc))
        {
            return "none";
        }

        return observedAtUtc;
    }

    private static DateTimeOffset GetDeliveryStatusSortValue(TenantInvitationDescriptor invitation)
    {
        if (invitation.Metadata.TryGetValue(TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusObservedAtUtc, out var observedAtUtc) &&
            DateTimeOffset.TryParse(observedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.MinValue;
    }
}
