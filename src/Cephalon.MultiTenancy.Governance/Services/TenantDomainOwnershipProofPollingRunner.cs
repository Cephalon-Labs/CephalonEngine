using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipProofPollingRunner(
    MultiTenancyGovernanceOptions options,
    ITenantDomainOwnershipCatalog catalog,
    ITenantDomainOwnershipProofVerificationRunner verificationRunner,
    TimeProvider timeProvider,
    ILogger<TenantDomainOwnershipProofPollingRunner> logger) : ITenantDomainOwnershipProofPollingRunner
{
    public async ValueTask<TenantDomainOwnershipProofPollingResult> PollAsync(
        TenantDomainOwnershipProofPollingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var ranAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        var result = options.EnableDomainOwnershipProofPollingRunner
            ? await PollCoreAsync(request, ranAtUtc, cancellationToken).ConfigureAwait(false)
            : CreateResult(
                request,
                TenantDomainOwnershipProofPollingOutcomes.Disabled,
                polled: false,
                ranAtUtc,
                candidateCount: 0,
                verificationResults: [],
                skippedCount: 0,
                reason: "Tenant-domain ownership proof polling runner is disabled.");

        if (result.Polled)
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofPollingCompleted(
                logger,
                result.Outcome,
                result.VerificationCount,
                result.VerifiedCount,
                result.RejectedCount,
                result.FailedCount,
                null);
        }
        else
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofPollingDenied(
                logger,
                result.Outcome,
                result.Reason,
                null);
        }

        return result;
    }

    private async ValueTask<TenantDomainOwnershipProofPollingResult> PollCoreAsync(
        TenantDomainOwnershipProofPollingRequest request,
        DateTimeOffset ranAtUtc,
        CancellationToken cancellationToken)
    {
        var batchLimit = ResolveBatchLimit(request);
        var eligible = catalog.DomainOwnerships
            .Where(domainOwnership => MatchesRequest(request, domainOwnership, ranAtUtc))
            .ToArray();
        var candidates = eligible
            .Where(domainOwnership => request.IncludeMissingExpectedProof || HasExpectedProof(domainOwnership))
            .Take(batchLimit)
            .ToArray();

        if (candidates.Length == 0)
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofPollingOutcomes.NoCandidates,
                polled: false,
                ranAtUtc,
                candidateCount: eligible.Length,
                verificationResults: [],
                skippedCount: eligible.Length,
                "No tenant-domain ownership declarations matched the proof polling criteria.");
        }

        var results = new List<TenantDomainOwnershipProofVerificationResult>(candidates.Length);
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await verificationRunner.VerifyAsync(
                new TenantDomainOwnershipProofVerificationRequest(
                    candidate.TenantId,
                    candidate.DomainName,
                    verificationMethod: candidate.VerificationMethod,
                    collectionBaseUri: request.CollectionBaseUri,
                    dnsTxtResolverEndpoint: request.DnsTxtResolverEndpoint,
                    source: request.Source ?? "proof-polling-runner",
                    actor: request.Actor,
                    atUtc: ranAtUtc,
                    expiresAtUtc: request.ExpiresAtUtc,
                    correlationId: request.CorrelationId,
                    issueChallengeWhenMissingExpectedProof: false,
                    collectHttpProof: request.IncludeHttpFile,
                    collectDnsTxtProof: request.IncludeDnsTxt,
                    recordPublicationPlan: request.RecordPublicationPlan,
                    timeout: request.Timeout,
                    metadata: BuildVerificationMetadata(request, candidate)),
                cancellationToken).ConfigureAwait(false);

            results.Add(result);
        }

        var failedCount = results.Count(static result => IsFailedAttempt(result));
        var outcome = failedCount == 0
            ? TenantDomainOwnershipProofPollingOutcomes.Completed
            : TenantDomainOwnershipProofPollingOutcomes.PartialFailure;
        return CreateResult(
            request,
            outcome,
            polled: true,
            ranAtUtc,
            candidateCount: eligible.Length,
            verificationResults: results,
            skippedCount: Math.Max(0, eligible.Length - results.Count),
            failedCount == 0
                ? "Tenant-domain ownership proof polling completed for all selected declarations."
                : "Tenant-domain ownership proof polling completed with one or more non-terminal attempts.");
    }

    private static bool MatchesRequest(
        TenantDomainOwnershipProofPollingRequest request,
        TenantDomainOwnershipDescriptor domainOwnership,
        DateTimeOffset ranAtUtc)
    {
        if (request.TenantIds.Count > 0 &&
            !request.TenantIds.Contains(domainOwnership.TenantId, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (request.DomainNames.Count > 0 &&
            !request.DomainNames.Contains(domainOwnership.DomainName, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (request.VerificationMethods.Count > 0 &&
            !request.VerificationMethods.Contains(domainOwnership.VerificationMethod, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(domainOwnership.Status, TenantDomainOwnershipStatuses.Verified, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(domainOwnership.Status, TenantDomainOwnershipStatuses.Suspended, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(domainOwnership.Status, TenantDomainOwnershipStatuses.Expired, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (domainOwnership.ExpiresAtUtc is not null && domainOwnership.ExpiresAtUtc <= ranAtUtc)
        {
            return false;
        }

        if (!request.IncludeRejected &&
            string.Equals(domainOwnership.Status, TenantDomainOwnershipStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return IsPollableVerificationMethod(request, domainOwnership.VerificationMethod);
    }

    private static bool IsPollableVerificationMethod(
        TenantDomainOwnershipProofPollingRequest request,
        string verificationMethod)
    {
        return request.IncludeHttpFile &&
                string.Equals(verificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase) ||
            request.IncludeDnsTxt &&
                string.Equals(verificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasExpectedProof(TenantDomainOwnershipDescriptor domainOwnership)
    {
        if (string.Equals(domainOwnership.VerificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase) &&
            domainOwnership.Metadata.TryGetValue(TenantDomainOwnershipProofMetadataKeys.ExpectedDnsTxtProof, out var dnsProof) &&
            !string.IsNullOrWhiteSpace(dnsProof))
        {
            return true;
        }

        if (string.Equals(domainOwnership.VerificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase) &&
            domainOwnership.Metadata.TryGetValue(TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof, out var httpProof) &&
            !string.IsNullOrWhiteSpace(httpProof))
        {
            return true;
        }

        return domainOwnership.Metadata.TryGetValue(TenantDomainOwnershipProofMetadataKeys.ExpectedProof, out var proof) &&
            !string.IsNullOrWhiteSpace(proof);
    }

    private static bool IsFailedAttempt(TenantDomainOwnershipProofVerificationResult result)
    {
        return !result.Verified &&
            !result.Rejected &&
            !string.Equals(result.Outcome, TenantDomainOwnershipProofVerificationOutcomes.AlreadyVerified, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> BuildVerificationMetadata(
        TenantDomainOwnershipProofPollingRequest request,
        TenantDomainOwnershipDescriptor domainOwnership)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in request.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        metadata[TenantDomainOwnershipProofPollingMetadataKeys.ProofPollingRunnerOwnership] = "cephalon-managed";
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.ExternalProofPollingOwnership] = "cephalon-managed";
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.BackgroundProofPollingOwnership] = ResolveBackgroundProofPollingOwnership(request);
        metadata["pollingTenantId"] = domainOwnership.TenantId;
        metadata["pollingVerificationMethod"] = domainOwnership.VerificationMethod;
        return metadata;
    }

    private int ResolveBatchLimit(TenantDomainOwnershipProofPollingRequest request)
    {
        return TenantDomainOwnershipProofPollingConfiguration.ResolveBatchLimit(options, request.MaxItems);
    }

    private TenantDomainOwnershipProofPollingResult CreateResult(
        TenantDomainOwnershipProofPollingRequest request,
        string outcome,
        bool polled,
        DateTimeOffset ranAtUtc,
        int candidateCount,
        List<TenantDomainOwnershipProofVerificationResult> verificationResults,
        int skippedCount,
        string reason)
    {
        var verifiedCount = verificationResults.Count(static result => result.Verified);
        var rejectedCount = verificationResults.Count(static result => result.Rejected);
        var failedCount = verificationResults.Count(static result => IsFailedAttempt(result));
        var batchLimit = ResolveBatchLimit(request);
        return new TenantDomainOwnershipProofPollingResult(
            outcome,
            polled,
            ranAtUtc,
            candidateCount,
            verificationResults.Count,
            skippedCount,
            verifiedCount,
            rejectedCount,
            failedCount,
            batchLimit,
            verificationResults,
            reason,
            BuildResultMetadata(
                request,
                outcome,
                ranAtUtc,
                candidateCount,
                verificationResults.Count,
                skippedCount,
                verifiedCount,
                rejectedCount,
                failedCount,
                batchLimit));
    }

    private Dictionary<string, string> BuildResultMetadata(
        TenantDomainOwnershipProofPollingRequest request,
        string outcome,
        DateTimeOffset ranAtUtc,
        int candidateCount,
        int verificationCount,
        int skippedCount,
        int verifiedCount,
        int rejectedCount,
        int failedCount,
        int batchLimit)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in request.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        metadata[TenantDomainOwnershipProofPollingMetadataKeys.LastProofPollingOutcome] = outcome;
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.LastProofPollingRanAtUtc] = ranAtUtc.ToString("O", CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.LastProofPollingSource] = request.Source ?? "proof-polling-runner";
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.ProofPollingRunnerOwnership] = options.EnableDomainOwnershipProofPollingRunner ? "cephalon-managed" : "not-configured";
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.ExternalProofPollingOwnership] = options.EnableDomainOwnershipProofPollingRunner ? "cephalon-managed" : "application-managed";
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.BackgroundProofPollingOwnership] = ResolveBackgroundProofPollingOwnership(request);
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.CandidateCount] = candidateCount.ToString(CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.VerificationCount] = verificationCount.ToString(CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.SkippedCount] = skippedCount.ToString(CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.VerifiedCount] = verifiedCount.ToString(CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.RejectedCount] = rejectedCount.ToString(CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.FailedCount] = failedCount.ToString(CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipProofPollingMetadataKeys.BatchLimit] = batchLimit.ToString(CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            metadata[TenantDomainOwnershipProofPollingMetadataKeys.LastProofPollingActor] = request.Actor;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata[TenantDomainOwnershipProofPollingMetadataKeys.LastProofPollingCorrelationId] = request.CorrelationId;
        }

        return metadata;
    }

    private static string ResolveBackgroundProofPollingOwnership(TenantDomainOwnershipProofPollingRequest request)
    {
        return request.Metadata.TryGetValue(
            TenantDomainOwnershipProofPollingMetadataKeys.BackgroundProofPollingOwnership,
            out var ownership) &&
            !string.IsNullOrWhiteSpace(ownership)
                ? ownership.Trim()
                : "application-managed";
    }
}
