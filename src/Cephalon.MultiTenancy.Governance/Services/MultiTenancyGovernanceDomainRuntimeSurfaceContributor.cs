using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class MultiTenancyGovernanceDomainRuntimeSurfaceContributor(
    MultiTenancyGovernanceOptions options,
    ITenantDomainOwnershipCatalog catalog,
    ITenantDomainOwnershipStore domainOwnershipStore,
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
            description: "Projects tenant-domain ownership catalog, Cephalon-managed declared-domain validation, and in-process verification workflow truth from the governance companion pack.",
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
        var dnsHttpProofCollectionOwnership = httpProofCollectionEnabled && dnsTxtProofCollectionConfigured
            ? "cephalon-managed"
            : httpProofCollectionEnabled || dnsTxtProofCollectionConfigured
                ? "mixed"
                : "application-managed";

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
            ["httpProofCollectionEnabled"] = httpProofCollectionEnabled.ToString().ToLowerInvariant(),
            ["httpProofCollectionOwnership"] = httpProofCollectionEnabled ? "cephalon-managed" : "not-configured",
            ["dnsTxtProofCollectionEnabled"] = dnsTxtProofCollectionEnabled.ToString().ToLowerInvariant(),
            ["dnsTxtProofResolverConfigured"] = dnsTxtProofCollectionConfigured.ToString().ToLowerInvariant(),
            ["dnsTxtProofCollectionOwnership"] = dnsTxtProofCollectionConfigured ? "cephalon-managed" : "not-configured",
            ["proofVerificationRunnerEnabled"] = proofVerificationRunnerEnabled.ToString().ToLowerInvariant(),
            ["proofVerificationRunnerOwnership"] = proofVerificationRunnerEnabled ? "cephalon-managed" : "not-configured",
            ["externalProofPollingOwnership"] = "application-managed",
            ["proofPublicationOwnership"] = "application-managed",
            ["durableStoreOwnership"] = domainOwnershipStore.IsDurable ? domainOwnershipStore.Ownership : "application-managed",
            ["basePackageOwnership"] = "separate-companion",
            ["verificationExecutionOwnership"] = "application-managed",
            ["dnsHttpProofCollectionOwnership"] = dnsHttpProofCollectionOwnership,
            ["statusBreakdown"] = statusBreakdown.Length == 0 ? "none" : string.Join(",", statusBreakdown),
            ["verificationMethodBreakdown"] = verificationMethodBreakdown.Length == 0 ? "none" : string.Join(",", verificationMethodBreakdown)
        };

        return new TechnologyRuntimeEntry(
            id: "tenant-domain-ownership-runtime",
            displayName: "Tenant Domain Ownership Runtime",
            description: "Summarizes declared domain ownership catalog size, contributor count, runtime store posture, status posture, verification-method posture, managed validation ownership, managed in-process verification workflow ownership, managed proof-evaluation ownership, managed proof-challenge issuance ownership, managed proof-publication planning ownership, managed HTTP proof-collection ownership, configured DNS TXT proof-collection ownership, and managed proof-verification runner ownership.",
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
