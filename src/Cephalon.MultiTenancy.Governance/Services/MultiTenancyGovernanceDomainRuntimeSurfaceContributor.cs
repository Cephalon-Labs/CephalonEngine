using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class MultiTenancyGovernanceDomainRuntimeSurfaceContributor(
    MultiTenancyGovernanceOptions options,
    ITenantDomainOwnershipCatalog catalog,
    ITenantDomainOwnershipStore domainOwnershipStore,
    ITenantDomainOwnershipProofPollingRuntimeCatalog proofPollingRuntimeCatalog,
    ITenantDomainOwnershipHttpProofPublicationCatalog httpProofPublicationCatalog,
    IEnumerable<ITenantDomainOwnershipContributor> contributors) : ITechnologyRuntimeContributor
{
    private readonly ITenantDomainOwnershipContributor[] contributors = contributors.ToArray();

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var domainOwnerships = catalog.DomainOwnerships;
        var entries = new List<TechnologyRuntimeEntry>
        {
            CreateSummaryEntry(domainOwnerships)
        };

        entries.AddRange(domainOwnerships
            .GroupBy(static domainOwnership => domainOwnership.TenantId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(CreateTenantEntry));

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-domain-ownership",
            displayName: "Tenant Domain Ownership",
            description: "Projects tenant-domain ownership catalog, Cephalon-managed declared-domain validation, in-process verification workflow truth, and background proof-polling runtime state from the governance companion pack.",
            entries: entries);
    }

    private TechnologyRuntimeEntry CreateSummaryEntry(IReadOnlyList<TenantDomainOwnershipDescriptor> domainOwnerships)
    {
        var statusBreakdown = domainOwnerships
            .GroupBy(static domainOwnership => domainOwnership.Status, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => $"{group.Key}:{group.Count().ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        var verificationMethodBreakdown = domainOwnerships
            .GroupBy(static domainOwnership => domainOwnership.VerificationMethod, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => $"{group.Key}:{group.Count().ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        var httpProofCollectionEnabled = options.EnableDomainOwnershipHttpProofCollection &&
            options.EnableDomainOwnershipProofPublicationPlanning &&
            options.EnableDomainOwnershipProofEvaluation &&
            options.EnableDomainOwnershipVerificationWorkflow;
        var httpProofPublicationEnabled = options.EnableDomainOwnershipHttpProofPublication &&
            options.EnableDomainOwnershipProofPublicationPlanning;
        var dnsTxtProofCollectionEnabled = options.EnableDomainOwnershipDnsTxtProofCollection &&
            options.EnableDomainOwnershipProofPublicationPlanning &&
            options.EnableDomainOwnershipProofEvaluation &&
            options.EnableDomainOwnershipVerificationWorkflow;
        var dnsTxtProofCollectionConfigured = dnsTxtProofCollectionEnabled &&
            options.DomainOwnershipDnsTxtProofResolverEndpoint is not null;
        var proofVerificationRunnerEnabled = options.EnableDomainOwnershipProofVerificationRunner &&
            options.EnableDomainOwnershipProofChallengeIssuance &&
            options.EnableDomainOwnershipProofPublicationPlanning &&
            options.EnableDomainOwnershipProofEvaluation &&
            options.EnableDomainOwnershipVerificationWorkflow;
        var proofPollingRunnerEnabled = options.EnableDomainOwnershipProofPollingRunner &&
            proofVerificationRunnerEnabled;
        var backgroundProofPolling = proofPollingRuntimeCatalog.Current;
        var dnsHttpProofCollectionOwnership = httpProofCollectionEnabled && dnsTxtProofCollectionConfigured
            ? "cephalon-managed"
            : httpProofCollectionEnabled || dnsTxtProofCollectionConfigured
                ? "mixed"
                : "application-managed";
        var proofPollingRunnerOwnership = proofPollingRunnerEnabled ? "cephalon-managed" : "not-configured";
        var externalProofPollingOwnership = proofPollingRunnerEnabled ? "cephalon-managed" : "application-managed";
        var publishedHttpProofs = httpProofPublicationCatalog.PublishedProofs;
        var httpProofPublicationOwnership = httpProofPublicationEnabled ? "cephalon-managed" : "not-configured";
        var proofPublicationOwnership = httpProofPublicationEnabled ? "mixed" : "application-managed";

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "cephalon-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance",
            ["runtimeState"] = domainOwnerships.Count > 0 ? "configured" : "empty",
            ["domainOwnershipCount"] = domainOwnerships.Count.ToString(CultureInfo.InvariantCulture),
            ["tenantCount"] = domainOwnerships
                .Select(static domainOwnership => domainOwnership.TenantId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count()
                .ToString(CultureInfo.InvariantCulture),
            ["contributorCount"] = contributors.Length.ToString(CultureInfo.InvariantCulture),
            ["configuredDomainOwnershipCount"] = options.DomainOwnerships.Count.ToString(CultureInfo.InvariantCulture),
            ["runtimeDomainOwnershipCount"] = domainOwnershipStore.Count.ToString(CultureInfo.InvariantCulture),
            ["domainOwnershipStoreKind"] = domainOwnershipStore.StoreKind,
            ["domainOwnershipStoreDurable"] = domainOwnershipStore.IsDurable.ToString().ToLowerInvariant(),
            ["domainOwnershipStoreOwnership"] = domainOwnershipStore.Ownership,
            ["validationEnabled"] = options.EnableDomainOwnershipValidation.ToString().ToLowerInvariant(),
            ["validationOwnership"] = options.EnableDomainOwnershipValidation ? "cephalon-managed" : "not-configured",
            ["verificationWorkflowEnabled"] = options.EnableDomainOwnershipVerificationWorkflow.ToString().ToLowerInvariant(),
            ["verificationWorkflowOwnership"] = options.EnableDomainOwnershipVerificationWorkflow ? "cephalon-managed" : "not-configured",
            ["proofEvaluationEnabled"] = (options.EnableDomainOwnershipProofEvaluation && options.EnableDomainOwnershipVerificationWorkflow).ToString().ToLowerInvariant(),
            ["proofEvaluationOwnership"] = options.EnableDomainOwnershipProofEvaluation && options.EnableDomainOwnershipVerificationWorkflow ? "cephalon-managed" : "not-configured",
            ["proofChallengeIssuanceEnabled"] = options.EnableDomainOwnershipProofChallengeIssuance.ToString().ToLowerInvariant(),
            ["proofChallengeIssuanceOwnership"] = options.EnableDomainOwnershipProofChallengeIssuance ? "cephalon-managed" : "not-configured",
            ["proofChallengeGenerationOwnership"] = options.EnableDomainOwnershipProofChallengeIssuance ? "cephalon-managed" : "not-configured",
            ["proofPublicationPlanningEnabled"] = options.EnableDomainOwnershipProofPublicationPlanning.ToString().ToLowerInvariant(),
            ["proofPublicationPlanningOwnership"] = options.EnableDomainOwnershipProofPublicationPlanning ? "cephalon-managed" : "not-configured",
            ["httpProofPublicationEnabled"] = httpProofPublicationEnabled.ToString().ToLowerInvariant(),
            ["httpProofPublicationOwnership"] = httpProofPublicationOwnership,
            ["httpProofPublicationCount"] = publishedHttpProofs.Count.ToString(CultureInfo.InvariantCulture),
            ["httpProofCollectionEnabled"] = httpProofCollectionEnabled.ToString().ToLowerInvariant(),
            ["httpProofCollectionOwnership"] = httpProofCollectionEnabled ? "cephalon-managed" : "not-configured",
            ["dnsTxtProofCollectionEnabled"] = dnsTxtProofCollectionEnabled.ToString().ToLowerInvariant(),
            ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
            ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionConfigured ? "cephalon-managed" : "not-configured",
            ["proofVerificationRunnerEnabled"] = proofVerificationRunnerEnabled.ToString().ToLowerInvariant(),
            ["proofVerificationRunnerOwnership"] = proofVerificationRunnerEnabled ? "cephalon-managed" : "not-configured",
            ["proofPollingRunnerEnabled"] = proofPollingRunnerEnabled.ToString().ToLowerInvariant(),
            ["proofPollingRunnerOwnership"] = proofPollingRunnerOwnership,
            ["externalProofPollingOwnership"] = externalProofPollingOwnership,
            ["backgroundProofPollingEnabled"] = backgroundProofPolling.Enabled.ToString().ToLowerInvariant(),
            ["backgroundProofPollingOwnership"] = backgroundProofPolling.Ownership,
            ["backgroundProofPollingIntervalSeconds"] = backgroundProofPolling.IntervalSeconds.ToString(CultureInfo.InvariantCulture),
            ["backgroundProofPollingBatchLimit"] = backgroundProofPolling.BatchLimit.ToString(CultureInfo.InvariantCulture),
            ["backgroundProofPollingRunOnStartup"] = backgroundProofPolling.RunOnStartup.ToString().ToLowerInvariant(),
            ["backgroundProofPollingDnsTxtResolverConfigured"] = backgroundProofPolling.DnsTxtResolverConfigured.ToString().ToLowerInvariant(),
            ["backgroundProofPollingRunCount"] = backgroundProofPolling.RunCount.ToString(CultureInfo.InvariantCulture),
            ["backgroundProofPollingSuccessfulRunCount"] = backgroundProofPolling.SuccessfulRunCount.ToString(CultureInfo.InvariantCulture),
            ["backgroundProofPollingFailedRunCount"] = backgroundProofPolling.FailedRunCount.ToString(CultureInfo.InvariantCulture),
            ["backgroundProofPollingLastOutcome"] = backgroundProofPolling.LastOutcome ?? "none",
            ["backgroundProofPollingLastReason"] = backgroundProofPolling.LastReason ?? "none",
            ["backgroundProofPollingLastCandidateCount"] = backgroundProofPolling.LastCandidateCount.ToString(CultureInfo.InvariantCulture),
            ["backgroundProofPollingLastVerificationCount"] = backgroundProofPolling.LastVerificationCount.ToString(CultureInfo.InvariantCulture),
            ["backgroundProofPollingLastVerifiedCount"] = backgroundProofPolling.LastVerifiedCount.ToString(CultureInfo.InvariantCulture),
            ["backgroundProofPollingLastRejectedCount"] = backgroundProofPolling.LastRejectedCount.ToString(CultureInfo.InvariantCulture),
            ["backgroundProofPollingLastFailedCount"] = backgroundProofPolling.LastFailedCount.ToString(CultureInfo.InvariantCulture),
            ["proofPollingDefaultBatchLimit"] = TenantDomainOwnershipProofPollingConfiguration.ResolveBatchLimit(options).ToString(CultureInfo.InvariantCulture),
            ["proofPublicationOwnership"] = proofPublicationOwnership,
            ["durableStoreOwnership"] = domainOwnershipStore.IsDurable ? domainOwnershipStore.Ownership : "application-managed",
            ["basePackageOwnership"] = "separate-companion",
            ["verificationExecutionOwnership"] = "application-managed",
            ["dnsHttpProofCollectionOwnership"] = dnsHttpProofCollectionOwnership,
            ["statusBreakdown"] = statusBreakdown.Length == 0 ? "none" : string.Join(",", statusBreakdown),
            ["verificationMethodBreakdown"] = verificationMethodBreakdown.Length == 0 ? "none" : string.Join(",", verificationMethodBreakdown)
        };

        if (backgroundProofPolling.LastStartedAtUtc is not null)
        {
            metadata["backgroundProofPollingLastStartedAtUtc"] = backgroundProofPolling.LastStartedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        if (backgroundProofPolling.LastCompletedAtUtc is not null)
        {
            metadata["backgroundProofPollingLastCompletedAtUtc"] = backgroundProofPolling.LastCompletedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(backgroundProofPolling.LastError))
        {
            metadata["backgroundProofPollingLastError"] = backgroundProofPolling.LastError;
        }

        return new TechnologyRuntimeEntry(
            id: "tenant-domain-ownership-runtime",
            displayName: "Tenant Domain Ownership Runtime",
            description: "Summarizes declared domain ownership catalog size, contributor count, runtime store posture, status posture, verification-method posture, managed validation ownership, managed in-process verification workflow ownership, managed proof-evaluation ownership, managed proof-challenge issuance ownership, managed proof-publication planning ownership, managed HTTP proof-publication ownership, managed HTTP proof-collection ownership, configured DNS TXT proof-collection ownership, managed proof-verification runner ownership, managed on-demand proof-polling runner ownership, and automatic background proof-polling runtime state.",
            metadata: metadata);
    }

    private static TechnologyRuntimeEntry CreateTenantEntry(IGrouping<string, TenantDomainOwnershipDescriptor> group)
    {
        var domainOwnerships = group.ToArray();
        var verifiedCount = domainOwnerships.Count(static domainOwnership =>
            string.Equals(domainOwnership.Status, TenantDomainOwnershipStatuses.Verified, StringComparison.OrdinalIgnoreCase));
        var pendingCount = domainOwnerships.Count(static domainOwnership =>
            string.Equals(domainOwnership.Status, TenantDomainOwnershipStatuses.Pending, StringComparison.OrdinalIgnoreCase));
        var rejectedCount = domainOwnerships.Count(static domainOwnership =>
            string.Equals(domainOwnership.Status, TenantDomainOwnershipStatuses.Rejected, StringComparison.OrdinalIgnoreCase));
        var suspendedCount = domainOwnerships.Count(static domainOwnership =>
            string.Equals(domainOwnership.Status, TenantDomainOwnershipStatuses.Suspended, StringComparison.OrdinalIgnoreCase));
        var expiredCount = domainOwnerships.Count(static domainOwnership =>
            string.Equals(domainOwnership.Status, TenantDomainOwnershipStatuses.Expired, StringComparison.OrdinalIgnoreCase));
        var verificationMethodBreakdown = domainOwnerships
            .GroupBy(static domainOwnership => domainOwnership.VerificationMethod, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static method => method.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static method => $"{method.Key}:{method.Count().ToString(CultureInfo.InvariantCulture)}")
            .ToArray();
        var sourceModuleIds = domainOwnerships
            .Select(static domainOwnership => domainOwnership.SourceModuleId)
            .Where(static sourceModuleId => !string.IsNullOrWhiteSpace(sourceModuleId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static sourceModuleId => sourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "cephalon-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance",
            ["tenantId"] = group.Key,
            ["domainOwnershipCount"] = domainOwnerships.Length.ToString(CultureInfo.InvariantCulture),
            ["verifiedDomainOwnershipCount"] = verifiedCount.ToString(CultureInfo.InvariantCulture),
            ["pendingDomainOwnershipCount"] = pendingCount.ToString(CultureInfo.InvariantCulture),
            ["rejectedDomainOwnershipCount"] = rejectedCount.ToString(CultureInfo.InvariantCulture),
            ["suspendedDomainOwnershipCount"] = suspendedCount.ToString(CultureInfo.InvariantCulture),
            ["expiredDomainOwnershipCount"] = expiredCount.ToString(CultureInfo.InvariantCulture),
            ["verificationMethodBreakdown"] = verificationMethodBreakdown.Length == 0 ? "none" : string.Join(",", verificationMethodBreakdown),
            ["sourceModuleIds"] = sourceModuleIds.Length == 0 ? "none" : string.Join(",", sourceModuleIds)
        };

        return new TechnologyRuntimeEntry(
            id: $"tenant-domain-ownership:{group.Key}",
            displayName: $"Tenant Domain Ownership: {group.Key}",
            description: "Summarizes domain ownership posture for one tenant without exposing individual domain names.",
            metadata: metadata);
    }
}
