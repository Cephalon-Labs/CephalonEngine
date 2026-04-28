using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipProofVerificationRunner(
    MultiTenancyGovernanceOptions options,
    ITenantDomainOwnershipCatalog catalog,
    ITenantDomainOwnershipProofChallengeIssuer proofChallengeIssuer,
    ITenantDomainOwnershipProofPublicationPlanner publicationPlanner,
    ITenantDomainOwnershipProofEvaluator proofEvaluator,
    IEnumerable<ITenantDomainOwnershipHttpProofCollector> httpProofCollectors,
    IEnumerable<ITenantDomainOwnershipDnsTxtProofCollector> dnsTxtProofCollectors,
    TimeProvider timeProvider,
    ILogger<TenantDomainOwnershipProofVerificationRunner> logger) : ITenantDomainOwnershipProofVerificationRunner
{
    private readonly ITenantDomainOwnershipHttpProofCollector? httpProofCollector = httpProofCollectors.FirstOrDefault();
    private readonly ITenantDomainOwnershipDnsTxtProofCollector? dnsTxtProofCollector = dnsTxtProofCollectors.FirstOrDefault();

    public async ValueTask<TenantDomainOwnershipProofVerificationResult> VerifyAsync(
        TenantDomainOwnershipProofVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var ranAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        var result = options.EnableDomainOwnershipProofVerificationRunner
            ? await VerifyCoreAsync(request, ranAtUtc, cancellationToken).ConfigureAwait(false)
            : CreateResult(
                request,
                TenantDomainOwnershipProofVerificationOutcomes.Disabled,
                verified: false,
                rejected: false,
                challengeIssued: false,
                publicationPlanned: false,
                proofCollected: false,
                proofEvaluated: false,
                ranAtUtc,
                verificationMethod: request.VerificationMethod,
                challengeResult: null,
                publicationPlanResult: null,
                httpProofCollectionResult: null,
                dnsTxtProofCollectionResult: null,
                evaluationResult: null,
                domainOwnership: null,
                reason: "Tenant-domain ownership proof verification runner is disabled.",
                metadata: BuildResultMetadata(
                    request,
                    TenantDomainOwnershipProofVerificationOutcomes.Disabled,
                    ranAtUtc,
                    challengeResult: null,
                    publicationPlanResult: null,
                    httpProofCollectionResult: null,
                    dnsTxtProofCollectionResult: null,
                    evaluationResult: null));

        if (result.Verified || result.Rejected || result.ChallengeIssued || result.PublicationPlanned)
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofVerificationCompleted(
                logger,
                result.TenantId,
                result.DomainName,
                result.Outcome,
                null);
        }
        else
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofVerificationDenied(
                logger,
                result.TenantId,
                result.DomainName,
                result.Outcome,
                result.Reason,
                null);
        }

        return result;
    }

    private async ValueTask<TenantDomainOwnershipProofVerificationResult> VerifyCoreAsync(
        TenantDomainOwnershipProofVerificationRequest request,
        DateTimeOffset ranAtUtc,
        CancellationToken cancellationToken)
    {
        var existingDomainOwnership = ResolveDomainOwnership(request);
        var verificationMethod = ResolveVerificationMethod(request, existingDomainOwnership);

        if (!IsSupportedVerificationMethod(verificationMethod))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofVerificationOutcomes.UnsupportedVerificationMethod,
                verified: false,
                rejected: false,
                challengeIssued: false,
                publicationPlanned: false,
                proofCollected: false,
                proofEvaluated: false,
                ranAtUtc,
                verificationMethod,
                challengeResult: null,
                publicationPlanResult: null,
                httpProofCollectionResult: null,
                dnsTxtProofCollectionResult: null,
                evaluationResult: null,
                domainOwnership: existingDomainOwnership,
                reason: "Only manual, DNS TXT, and HTTP file tenant-domain ownership verification methods are supported by the runner.",
                metadata: BuildResultMetadata(
                    request,
                    TenantDomainOwnershipProofVerificationOutcomes.UnsupportedVerificationMethod,
                    ranAtUtc,
                    challengeResult: null,
                    publicationPlanResult: null,
                    httpProofCollectionResult: null,
                    dnsTxtProofCollectionResult: null,
                    evaluationResult: null));
        }

        if (existingDomainOwnership is not null &&
            string.IsNullOrWhiteSpace(request.ObservedProof) &&
            string.Equals(existingDomainOwnership.Status, TenantDomainOwnershipStatuses.Verified, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofVerificationOutcomes.AlreadyVerified,
                verified: true,
                rejected: false,
                challengeIssued: false,
                publicationPlanned: false,
                proofCollected: false,
                proofEvaluated: false,
                ranAtUtc,
                verificationMethod,
                challengeResult: null,
                publicationPlanResult: null,
                httpProofCollectionResult: null,
                dnsTxtProofCollectionResult: null,
                evaluationResult: null,
                existingDomainOwnership,
                "Tenant-domain ownership is already verified.",
                BuildResultMetadata(
                    request,
                    TenantDomainOwnershipProofVerificationOutcomes.AlreadyVerified,
                    ranAtUtc,
                    challengeResult: null,
                    publicationPlanResult: null,
                    httpProofCollectionResult: null,
                    dnsTxtProofCollectionResult: null,
                    evaluationResult: null));
        }

        if (!string.IsNullOrWhiteSpace(request.ObservedProof))
        {
            return await EvaluateObservedProofAsync(request, ranAtUtc, verificationMethod, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(verificationMethod, TenantDomainVerificationMethods.Manual, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofVerificationOutcomes.MissingObservedProof,
                verified: false,
                rejected: false,
                challengeIssued: false,
                publicationPlanned: false,
                proofCollected: false,
                proofEvaluated: false,
                ranAtUtc,
                verificationMethod,
                challengeResult: null,
                publicationPlanResult: null,
                httpProofCollectionResult: null,
                dnsTxtProofCollectionResult: null,
                evaluationResult: null,
                existingDomainOwnership,
                "Observed proof is required for manual tenant-domain ownership proof verification.",
                BuildResultMetadata(
                    request,
                    TenantDomainOwnershipProofVerificationOutcomes.MissingObservedProof,
                    ranAtUtc,
                    challengeResult: null,
                    publicationPlanResult: null,
                    httpProofCollectionResult: null,
                    dnsTxtProofCollectionResult: null,
                    evaluationResult: null));
        }

        if (request.IssueChallengeWhenMissingExpectedProof &&
            (existingDomainOwnership is null || !HasExpectedProof(existingDomainOwnership, verificationMethod)))
        {
            return await IssueChallengeAndPlanAsync(request, ranAtUtc, verificationMethod, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(verificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase) &&
            request.CollectHttpProof)
        {
            return await CollectHttpProofAsync(request, ranAtUtc, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(verificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase) &&
            request.CollectDnsTxtProof)
        {
            return await CollectDnsTxtProofAsync(request, ranAtUtc, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(verificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase))
        {
            return await PlanPublicationAsync(request, ranAtUtc, verificationMethod, cancellationToken).ConfigureAwait(false);
        }

        return CreateResult(
            request,
            TenantDomainOwnershipProofVerificationOutcomes.MissingObservedProof,
            verified: false,
            rejected: false,
            challengeIssued: false,
            publicationPlanned: false,
            proofCollected: false,
            proofEvaluated: false,
            ranAtUtc,
            verificationMethod,
            challengeResult: null,
            publicationPlanResult: null,
            httpProofCollectionResult: null,
            dnsTxtProofCollectionResult: null,
            evaluationResult: null,
            existingDomainOwnership,
            "Observed proof is required or the requested tenant-domain ownership proof collection path is not enabled.",
            BuildResultMetadata(
                request,
                TenantDomainOwnershipProofVerificationOutcomes.MissingObservedProof,
                ranAtUtc,
                challengeResult: null,
                publicationPlanResult: null,
                httpProofCollectionResult: null,
                dnsTxtProofCollectionResult: null,
                evaluationResult: null));
    }

    private async ValueTask<TenantDomainOwnershipProofVerificationResult> EvaluateObservedProofAsync(
        TenantDomainOwnershipProofVerificationRequest request,
        DateTimeOffset ranAtUtc,
        string verificationMethod,
        CancellationToken cancellationToken)
    {
        var evaluation = await proofEvaluator.EvaluateAsync(
            new TenantDomainOwnershipProofEvaluationRequest(
                request.TenantId,
                request.DomainName,
                request.ObservedProof!,
                verificationMethod,
                source: request.Source ?? "proof-verification-runner",
                actor: request.Actor,
                atUtc: ranAtUtc,
                expiresAtUtc: request.ExpiresAtUtc,
                correlationId: request.CorrelationId,
                metadata: request.Metadata),
            cancellationToken).ConfigureAwait(false);

        return CreateEvaluationResult(
            request,
            ranAtUtc,
            verificationMethod,
            evaluation,
            httpProofCollectionResult: null,
            dnsTxtProofCollectionResult: null);
    }

    private async ValueTask<TenantDomainOwnershipProofVerificationResult> IssueChallengeAndPlanAsync(
        TenantDomainOwnershipProofVerificationRequest request,
        DateTimeOffset ranAtUtc,
        string verificationMethod,
        CancellationToken cancellationToken)
    {
        var challenge = await proofChallengeIssuer.IssueAsync(
            new TenantDomainOwnershipProofChallengeRequest(
                request.TenantId,
                request.DomainName,
                verificationMethod,
                source: request.Source ?? "proof-verification-runner",
                actor: request.Actor,
                atUtc: ranAtUtc,
                expiresAtUtc: request.ExpiresAtUtc,
                correlationId: request.CorrelationId,
                metadata: request.Metadata),
            cancellationToken).ConfigureAwait(false);

        if (!challenge.Issued)
        {
            var outcome = MapChallengeOutcome(challenge.Outcome);
            return CreateResult(
                request,
                outcome,
                verified: string.Equals(outcome, TenantDomainOwnershipProofVerificationOutcomes.AlreadyVerified, StringComparison.OrdinalIgnoreCase),
                rejected: false,
                challengeIssued: false,
                publicationPlanned: false,
                proofCollected: false,
                proofEvaluated: false,
                ranAtUtc,
                verificationMethod: challenge.VerificationMethod ?? verificationMethod,
                challenge,
                publicationPlanResult: null,
                httpProofCollectionResult: null,
                dnsTxtProofCollectionResult: null,
                evaluationResult: null,
                challenge.DomainOwnership,
                $"Tenant-domain ownership proof verification could not issue a challenge. {challenge.Reason}",
                BuildResultMetadata(
                    request,
                    outcome,
                    ranAtUtc,
                    challenge,
                    publicationPlanResult: null,
                    httpProofCollectionResult: null,
                    dnsTxtProofCollectionResult: null,
                    evaluationResult: null));
        }

        var publicationPlan = await publicationPlanner.PlanAsync(
            new TenantDomainOwnershipProofPublicationPlanRequest(
                request.TenantId,
                request.DomainName,
                challenge.VerificationMethod ?? verificationMethod,
                source: request.Source ?? "proof-verification-runner",
                actor: request.Actor,
                atUtc: ranAtUtc,
                correlationId: request.CorrelationId,
                recordPlan: request.RecordPublicationPlan,
                metadata: challenge.Metadata),
            cancellationToken).ConfigureAwait(false);

        var planOutcome = publicationPlan.Planned
            ? TenantDomainOwnershipProofVerificationOutcomes.ChallengeIssued
            : MapPublicationPlanOutcome(publicationPlan.Outcome);
        return CreateResult(
            request,
            planOutcome,
            verified: false,
            rejected: false,
            challengeIssued: true,
            publicationPlanned: publicationPlan.Planned,
            proofCollected: false,
            proofEvaluated: false,
            ranAtUtc,
            verificationMethod: challenge.VerificationMethod ?? verificationMethod,
            challenge,
            publicationPlan,
            httpProofCollectionResult: null,
            dnsTxtProofCollectionResult: null,
            evaluationResult: null,
            publicationPlan.DomainOwnership ?? challenge.DomainOwnership,
            publicationPlan.Planned
                ? "Tenant-domain ownership proof challenge was issued and publication instructions were produced."
                : $"Tenant-domain ownership proof challenge was issued, but publication planning failed. {publicationPlan.Reason}",
            BuildResultMetadata(
                request,
                planOutcome,
                ranAtUtc,
                challenge,
                publicationPlan,
                httpProofCollectionResult: null,
                dnsTxtProofCollectionResult: null,
                evaluationResult: null));
    }

    private async ValueTask<TenantDomainOwnershipProofVerificationResult> CollectHttpProofAsync(
        TenantDomainOwnershipProofVerificationRequest request,
        DateTimeOffset ranAtUtc,
        CancellationToken cancellationToken)
    {
        if (httpProofCollector is null)
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofVerificationOutcomes.HttpCollectionUnavailable,
                verified: false,
                rejected: false,
                challengeIssued: false,
                publicationPlanned: false,
                proofCollected: false,
                proofEvaluated: false,
                ranAtUtc,
                verificationMethod: TenantDomainVerificationMethods.HttpFile,
                challengeResult: null,
                publicationPlanResult: null,
                httpProofCollectionResult: null,
                dnsTxtProofCollectionResult: null,
                evaluationResult: null,
                domainOwnership: ResolveDomainOwnership(request),
                reason: "Tenant-domain ownership HTTP proof collection is not registered.",
                metadata: BuildResultMetadata(
                    request,
                    TenantDomainOwnershipProofVerificationOutcomes.HttpCollectionUnavailable,
                    ranAtUtc,
                    challengeResult: null,
                    publicationPlanResult: null,
                    httpProofCollectionResult: null,
                    dnsTxtProofCollectionResult: null,
                    evaluationResult: null));
        }

        var collection = await httpProofCollector.CollectAsync(
            new TenantDomainOwnershipHttpProofCollectionRequest(
                request.TenantId,
                request.DomainName,
                TenantDomainVerificationMethods.HttpFile,
                request.CollectionBaseUri,
                source: request.Source ?? "proof-verification-runner",
                actor: request.Actor,
                atUtc: ranAtUtc,
                expiresAtUtc: request.ExpiresAtUtc,
                correlationId: request.CorrelationId,
                recordPublicationPlan: request.RecordPublicationPlan,
                timeout: request.Timeout,
                metadata: request.Metadata),
            cancellationToken).ConfigureAwait(false);

        if (collection.EvaluationResult is not null)
        {
            return CreateEvaluationResult(
                request,
                ranAtUtc,
                TenantDomainVerificationMethods.HttpFile,
                collection.EvaluationResult,
                collection,
                dnsTxtProofCollectionResult: null);
        }

        var outcome = MapHttpCollectionOutcome(collection.Outcome);
        return CreateResult(
            request,
            outcome,
            verified: false,
            rejected: false,
            challengeIssued: false,
            publicationPlanned: collection.PublicationPlanResult?.Planned == true,
            proofCollected: collection.Collected,
            proofEvaluated: false,
            ranAtUtc,
            verificationMethod: TenantDomainVerificationMethods.HttpFile,
            challengeResult: null,
            publicationPlanResult: collection.PublicationPlanResult,
            httpProofCollectionResult: collection,
            dnsTxtProofCollectionResult: null,
            evaluationResult: null,
            collection.DomainOwnership,
            $"Tenant-domain ownership proof verification could not collect HTTP proof. {collection.Reason}",
            BuildResultMetadata(
                request,
                outcome,
                ranAtUtc,
                challengeResult: null,
                collection.PublicationPlanResult,
                collection,
                dnsTxtProofCollectionResult: null,
                evaluationResult: null));
    }

    private async ValueTask<TenantDomainOwnershipProofVerificationResult> CollectDnsTxtProofAsync(
        TenantDomainOwnershipProofVerificationRequest request,
        DateTimeOffset ranAtUtc,
        CancellationToken cancellationToken)
    {
        if (dnsTxtProofCollector is null)
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofVerificationOutcomes.DnsTxtCollectionUnavailable,
                verified: false,
                rejected: false,
                challengeIssued: false,
                publicationPlanned: false,
                proofCollected: false,
                proofEvaluated: false,
                ranAtUtc,
                verificationMethod: TenantDomainVerificationMethods.DnsTxt,
                challengeResult: null,
                publicationPlanResult: null,
                httpProofCollectionResult: null,
                dnsTxtProofCollectionResult: null,
                evaluationResult: null,
                domainOwnership: ResolveDomainOwnership(request),
                reason: "Tenant-domain ownership DNS TXT proof collection is not registered.",
                metadata: BuildResultMetadata(
                    request,
                    TenantDomainOwnershipProofVerificationOutcomes.DnsTxtCollectionUnavailable,
                    ranAtUtc,
                    challengeResult: null,
                    publicationPlanResult: null,
                    httpProofCollectionResult: null,
                    dnsTxtProofCollectionResult: null,
                    evaluationResult: null));
        }

        var collection = await dnsTxtProofCollector.CollectAsync(
            new TenantDomainOwnershipDnsTxtProofCollectionRequest(
                request.TenantId,
                request.DomainName,
                TenantDomainVerificationMethods.DnsTxt,
                request.DnsTxtResolverEndpoint,
                source: request.Source ?? "proof-verification-runner",
                actor: request.Actor,
                atUtc: ranAtUtc,
                expiresAtUtc: request.ExpiresAtUtc,
                correlationId: request.CorrelationId,
                recordPublicationPlan: request.RecordPublicationPlan,
                timeout: request.Timeout,
                metadata: request.Metadata),
            cancellationToken).ConfigureAwait(false);

        if (collection.EvaluationResult is not null)
        {
            return CreateEvaluationResult(
                request,
                ranAtUtc,
                TenantDomainVerificationMethods.DnsTxt,
                collection.EvaluationResult,
                httpProofCollectionResult: null,
                dnsTxtProofCollectionResult: collection);
        }

        var outcome = MapDnsTxtCollectionOutcome(collection.Outcome);
        return CreateResult(
            request,
            outcome,
            verified: false,
            rejected: false,
            challengeIssued: false,
            publicationPlanned: collection.PublicationPlanResult?.Planned == true,
            proofCollected: collection.Collected,
            proofEvaluated: false,
            ranAtUtc,
            verificationMethod: TenantDomainVerificationMethods.DnsTxt,
            challengeResult: null,
            publicationPlanResult: collection.PublicationPlanResult,
            httpProofCollectionResult: null,
            dnsTxtProofCollectionResult: collection,
            evaluationResult: null,
            collection.DomainOwnership,
            $"Tenant-domain ownership proof verification could not collect DNS TXT proof. {collection.Reason}",
            BuildResultMetadata(
                request,
                outcome,
                ranAtUtc,
                challengeResult: null,
                collection.PublicationPlanResult,
                httpProofCollectionResult: null,
                dnsTxtProofCollectionResult: collection,
                evaluationResult: null));
    }

    private async ValueTask<TenantDomainOwnershipProofVerificationResult> PlanPublicationAsync(
        TenantDomainOwnershipProofVerificationRequest request,
        DateTimeOffset ranAtUtc,
        string verificationMethod,
        CancellationToken cancellationToken)
    {
        var publicationPlan = await publicationPlanner.PlanAsync(
            new TenantDomainOwnershipProofPublicationPlanRequest(
                request.TenantId,
                request.DomainName,
                verificationMethod,
                source: request.Source ?? "proof-verification-runner",
                actor: request.Actor,
                atUtc: ranAtUtc,
                correlationId: request.CorrelationId,
                recordPlan: request.RecordPublicationPlan,
                metadata: request.Metadata),
            cancellationToken).ConfigureAwait(false);

        var outcome = publicationPlan.Planned
            ? TenantDomainOwnershipProofVerificationOutcomes.PublicationPlanned
            : MapPublicationPlanOutcome(publicationPlan.Outcome);
        return CreateResult(
            request,
            outcome,
            verified: false,
            rejected: false,
            challengeIssued: false,
            publicationPlanned: publicationPlan.Planned,
            proofCollected: false,
            proofEvaluated: false,
            ranAtUtc,
            verificationMethod,
            challengeResult: null,
            publicationPlan,
            httpProofCollectionResult: null,
            dnsTxtProofCollectionResult: null,
            evaluationResult: null,
            publicationPlan.DomainOwnership,
            publicationPlan.Planned
                ? "Tenant-domain ownership proof publication instructions were produced; an observed proof must still be reported for evaluation."
                : $"Tenant-domain ownership proof verification could not produce publication instructions. {publicationPlan.Reason}",
            BuildResultMetadata(
                request,
                outcome,
                ranAtUtc,
                challengeResult: null,
                publicationPlan,
                httpProofCollectionResult: null,
                dnsTxtProofCollectionResult: null,
                evaluationResult: null));
    }

    private TenantDomainOwnershipProofVerificationResult CreateEvaluationResult(
        TenantDomainOwnershipProofVerificationRequest request,
        DateTimeOffset ranAtUtc,
        string verificationMethod,
        TenantDomainOwnershipProofEvaluationResult evaluation,
        TenantDomainOwnershipHttpProofCollectionResult? httpProofCollectionResult,
        TenantDomainOwnershipDnsTxtProofCollectionResult? dnsTxtProofCollectionResult)
    {
        var verified = evaluation.Applied &&
            string.Equals(evaluation.Outcome, TenantDomainOwnershipProofEvaluationOutcomes.Verified, StringComparison.OrdinalIgnoreCase);
        var rejected = evaluation.Applied &&
            string.Equals(evaluation.Outcome, TenantDomainOwnershipProofEvaluationOutcomes.Rejected, StringComparison.OrdinalIgnoreCase);
        var outcome = verified
            ? TenantDomainOwnershipProofVerificationOutcomes.Verified
            : rejected
                ? TenantDomainOwnershipProofVerificationOutcomes.Rejected
                : MapEvaluationOutcome(evaluation.Outcome);

        return CreateResult(
            request,
            outcome,
            verified,
            rejected,
            challengeIssued: false,
            publicationPlanned: httpProofCollectionResult?.PublicationPlanResult?.Planned == true ||
                dnsTxtProofCollectionResult?.PublicationPlanResult?.Planned == true,
            proofCollected: httpProofCollectionResult?.Collected == true ||
                dnsTxtProofCollectionResult?.Collected == true,
            proofEvaluated: true,
            ranAtUtc,
            verificationMethod,
            challengeResult: null,
            publicationPlanResult: httpProofCollectionResult?.PublicationPlanResult ?? dnsTxtProofCollectionResult?.PublicationPlanResult,
            httpProofCollectionResult,
            dnsTxtProofCollectionResult,
            evaluation,
            evaluation.DomainOwnership ?? httpProofCollectionResult?.DomainOwnership ?? dnsTxtProofCollectionResult?.DomainOwnership,
            verified
                ? "Tenant-domain ownership proof matched and the declaration was verified."
                : rejected
                    ? "Tenant-domain ownership proof mismatched and the declaration was rejected."
                    : $"Tenant-domain ownership proof evaluation did not apply a terminal workflow outcome. {evaluation.Reason}",
            BuildResultMetadata(
                request,
                outcome,
                ranAtUtc,
                challengeResult: null,
                httpProofCollectionResult?.PublicationPlanResult ?? dnsTxtProofCollectionResult?.PublicationPlanResult,
                httpProofCollectionResult,
                dnsTxtProofCollectionResult,
                evaluation));
    }

    private TenantDomainOwnershipDescriptor? ResolveDomainOwnership(TenantDomainOwnershipProofVerificationRequest request)
    {
        var matches = catalog.GetByTenantAndDomain(request.TenantId, request.DomainName);
        return matches.Count > 0 ? matches[0] : null;
    }

    private static string ResolveVerificationMethod(
        TenantDomainOwnershipProofVerificationRequest request,
        TenantDomainOwnershipDescriptor? domainOwnership)
    {
        return request.VerificationMethod ??
            domainOwnership?.VerificationMethod ??
            TenantDomainVerificationMethods.HttpFile;
    }

    private static bool HasExpectedProof(TenantDomainOwnershipDescriptor domainOwnership, string verificationMethod)
    {
        if (string.Equals(verificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase) &&
            domainOwnership.Metadata.TryGetValue(TenantDomainOwnershipProofMetadataKeys.ExpectedDnsTxtProof, out var dnsProof) &&
            !string.IsNullOrWhiteSpace(dnsProof))
        {
            return true;
        }

        if (string.Equals(verificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase) &&
            domainOwnership.Metadata.TryGetValue(TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof, out var httpProof) &&
            !string.IsNullOrWhiteSpace(httpProof))
        {
            return true;
        }

        return domainOwnership.Metadata.TryGetValue(TenantDomainOwnershipProofMetadataKeys.ExpectedProof, out var proof) &&
            !string.IsNullOrWhiteSpace(proof);
    }

    private static bool IsSupportedVerificationMethod(string verificationMethod)
    {
        return string.Equals(verificationMethod, TenantDomainVerificationMethods.Manual, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(verificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(verificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase);
    }

    private static string MapChallengeOutcome(string outcome)
    {
        return outcome switch
        {
            TenantDomainOwnershipProofChallengeOutcomes.Disabled => TenantDomainOwnershipProofVerificationOutcomes.Disabled,
            TenantDomainOwnershipProofChallengeOutcomes.TenantMismatch => TenantDomainOwnershipProofVerificationOutcomes.TenantMismatch,
            TenantDomainOwnershipProofChallengeOutcomes.VerificationMethodMismatch => TenantDomainOwnershipProofVerificationOutcomes.VerificationMethodMismatch,
            TenantDomainOwnershipProofChallengeOutcomes.AlreadyVerified => TenantDomainOwnershipProofVerificationOutcomes.AlreadyVerified,
            TenantDomainOwnershipProofChallengeOutcomes.StoreFailed => TenantDomainOwnershipProofVerificationOutcomes.StoreFailed,
            _ => TenantDomainOwnershipProofVerificationOutcomes.ChallengeFailed
        };
    }

    private static string MapPublicationPlanOutcome(string outcome)
    {
        return outcome switch
        {
            TenantDomainOwnershipProofPublicationPlanOutcomes.Disabled => TenantDomainOwnershipProofVerificationOutcomes.Disabled,
            TenantDomainOwnershipProofPublicationPlanOutcomes.NotFound => TenantDomainOwnershipProofVerificationOutcomes.NotFound,
            TenantDomainOwnershipProofPublicationPlanOutcomes.TenantMismatch => TenantDomainOwnershipProofVerificationOutcomes.TenantMismatch,
            TenantDomainOwnershipProofPublicationPlanOutcomes.VerificationMethodMismatch => TenantDomainOwnershipProofVerificationOutcomes.VerificationMethodMismatch,
            TenantDomainOwnershipProofPublicationPlanOutcomes.MissingExpectedProof => TenantDomainOwnershipProofVerificationOutcomes.MissingExpectedProof,
            TenantDomainOwnershipProofPublicationPlanOutcomes.UnsupportedVerificationMethod => TenantDomainOwnershipProofVerificationOutcomes.UnsupportedVerificationMethod,
            TenantDomainOwnershipProofPublicationPlanOutcomes.StoreFailed => TenantDomainOwnershipProofVerificationOutcomes.StoreFailed,
            _ => TenantDomainOwnershipProofVerificationOutcomes.PublicationPlanFailed
        };
    }

    private static string MapHttpCollectionOutcome(string outcome)
    {
        return outcome switch
        {
            TenantDomainOwnershipHttpProofCollectionOutcomes.Disabled => TenantDomainOwnershipProofVerificationOutcomes.Disabled,
            TenantDomainOwnershipHttpProofCollectionOutcomes.NotFound => TenantDomainOwnershipProofVerificationOutcomes.NotFound,
            TenantDomainOwnershipHttpProofCollectionOutcomes.TenantMismatch => TenantDomainOwnershipProofVerificationOutcomes.TenantMismatch,
            TenantDomainOwnershipHttpProofCollectionOutcomes.VerificationMethodMismatch => TenantDomainOwnershipProofVerificationOutcomes.VerificationMethodMismatch,
            TenantDomainOwnershipHttpProofCollectionOutcomes.MissingExpectedProof => TenantDomainOwnershipProofVerificationOutcomes.MissingExpectedProof,
            TenantDomainOwnershipHttpProofCollectionOutcomes.UnsupportedVerificationMethod => TenantDomainOwnershipProofVerificationOutcomes.UnsupportedVerificationMethod,
            TenantDomainOwnershipHttpProofCollectionOutcomes.StoreFailed => TenantDomainOwnershipProofVerificationOutcomes.StoreFailed,
            TenantDomainOwnershipHttpProofCollectionOutcomes.EvaluationFailed => TenantDomainOwnershipProofVerificationOutcomes.EvaluationFailed,
            _ => TenantDomainOwnershipProofVerificationOutcomes.HttpCollectionFailed
        };
    }

    private static string MapDnsTxtCollectionOutcome(string outcome)
    {
        return outcome switch
        {
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Disabled => TenantDomainOwnershipProofVerificationOutcomes.Disabled,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.ResolverNotConfigured => TenantDomainOwnershipProofVerificationOutcomes.DnsTxtCollectionUnavailable,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.NotFound => TenantDomainOwnershipProofVerificationOutcomes.NotFound,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.TenantMismatch => TenantDomainOwnershipProofVerificationOutcomes.TenantMismatch,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.VerificationMethodMismatch => TenantDomainOwnershipProofVerificationOutcomes.VerificationMethodMismatch,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.MissingExpectedProof => TenantDomainOwnershipProofVerificationOutcomes.MissingExpectedProof,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.UnsupportedVerificationMethod => TenantDomainOwnershipProofVerificationOutcomes.UnsupportedVerificationMethod,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.StoreFailed => TenantDomainOwnershipProofVerificationOutcomes.StoreFailed,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.EvaluationFailed => TenantDomainOwnershipProofVerificationOutcomes.EvaluationFailed,
            _ => TenantDomainOwnershipProofVerificationOutcomes.DnsTxtCollectionFailed
        };
    }

    private static string MapEvaluationOutcome(string outcome)
    {
        return outcome switch
        {
            TenantDomainOwnershipProofEvaluationOutcomes.Disabled => TenantDomainOwnershipProofVerificationOutcomes.Disabled,
            TenantDomainOwnershipProofEvaluationOutcomes.NotFound => TenantDomainOwnershipProofVerificationOutcomes.NotFound,
            TenantDomainOwnershipProofEvaluationOutcomes.TenantMismatch => TenantDomainOwnershipProofVerificationOutcomes.TenantMismatch,
            TenantDomainOwnershipProofEvaluationOutcomes.VerificationMethodMismatch => TenantDomainOwnershipProofVerificationOutcomes.VerificationMethodMismatch,
            TenantDomainOwnershipProofEvaluationOutcomes.MissingExpectedProof => TenantDomainOwnershipProofVerificationOutcomes.MissingExpectedProof,
            TenantDomainOwnershipProofEvaluationOutcomes.MissingObservedProof => TenantDomainOwnershipProofVerificationOutcomes.MissingObservedProof,
            _ => TenantDomainOwnershipProofVerificationOutcomes.EvaluationFailed
        };
    }

    private string ResolveDnsTxtProofCollectionOwnership(TenantDomainOwnershipProofVerificationRequest request)
    {
        return dnsTxtProofCollector is not null &&
            (request.DnsTxtResolverEndpoint is not null || options.DomainOwnershipDnsTxtProofResolverEndpoint is not null)
            ? "cephalon-managed"
            : "not-configured";
    }

    private Dictionary<string, string> BuildResultMetadata(
        TenantDomainOwnershipProofVerificationRequest request,
        string outcome,
        DateTimeOffset ranAtUtc,
        TenantDomainOwnershipProofChallengeResult? challengeResult,
        TenantDomainOwnershipProofPublicationPlanResult? publicationPlanResult,
        TenantDomainOwnershipHttpProofCollectionResult? httpProofCollectionResult,
        TenantDomainOwnershipDnsTxtProofCollectionResult? dnsTxtProofCollectionResult,
        TenantDomainOwnershipProofEvaluationResult? evaluationResult)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in request.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationOutcome] = outcome;
        metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationRanAtUtc] = ranAtUtc.ToString("O", CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationSource] = request.Source ?? "proof-verification-runner";
        metadata[TenantDomainOwnershipProofVerificationMetadataKeys.ProofVerificationRunnerOwnership] = options.EnableDomainOwnershipProofVerificationRunner ? "cephalon-managed" : "not-configured";
        metadata[TenantDomainOwnershipProofVerificationMetadataKeys.ProofPollingRunnerOwnership] = ResolveProofPollingRunnerOwnership();
        metadata[TenantDomainOwnershipProofVerificationMetadataKeys.HttpProofCollectionOwnership] = httpProofCollector is null ? "not-configured" : "cephalon-managed";
        metadata[TenantDomainOwnershipProofVerificationMetadataKeys.DnsTxtProofCollectionOwnership] = ResolveDnsTxtProofCollectionOwnership(request);
        metadata[TenantDomainOwnershipProofVerificationMetadataKeys.ExternalProofPollingOwnership] = ResolveExternalProofPollingOwnership();
        metadata[TenantDomainOwnershipProofVerificationMetadataKeys.BackgroundProofPollingOwnership] = "application-managed";

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationActor] = request.Actor;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationCorrelationId] = request.CorrelationId;
        }

        if (challengeResult is not null)
        {
            metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationChallengeOutcome] = challengeResult.Outcome;
        }

        if (publicationPlanResult is not null)
        {
            metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationPublicationPlanOutcome] = publicationPlanResult.Outcome;
        }

        if (httpProofCollectionResult is not null)
        {
            metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationHttpCollectionOutcome] = httpProofCollectionResult.Outcome;
        }

        if (dnsTxtProofCollectionResult is not null)
        {
            metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationDnsTxtCollectionOutcome] = dnsTxtProofCollectionResult.Outcome;
        }

        if (evaluationResult is not null)
        {
            metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationEvaluationOutcome] = evaluationResult.Outcome;
        }

        return metadata;
    }

    private string ResolveProofPollingRunnerOwnership()
    {
        return options.EnableDomainOwnershipProofPollingRunner && options.EnableDomainOwnershipProofVerificationRunner
            ? "cephalon-managed"
            : "not-configured";
    }

    private string ResolveExternalProofPollingOwnership()
    {
        return options.EnableDomainOwnershipProofPollingRunner && options.EnableDomainOwnershipProofVerificationRunner
            ? "cephalon-managed"
            : "application-managed";
    }

    private static TenantDomainOwnershipProofVerificationResult CreateResult(
        TenantDomainOwnershipProofVerificationRequest request,
        string outcome,
        bool verified,
        bool rejected,
        bool challengeIssued,
        bool publicationPlanned,
        bool proofCollected,
        bool proofEvaluated,
        DateTimeOffset ranAtUtc,
        string? verificationMethod,
        TenantDomainOwnershipProofChallengeResult? challengeResult,
        TenantDomainOwnershipProofPublicationPlanResult? publicationPlanResult,
        TenantDomainOwnershipHttpProofCollectionResult? httpProofCollectionResult,
        TenantDomainOwnershipDnsTxtProofCollectionResult? dnsTxtProofCollectionResult,
        TenantDomainOwnershipProofEvaluationResult? evaluationResult,
        TenantDomainOwnershipDescriptor? domainOwnership,
        string reason,
        IReadOnlyDictionary<string, string>? metadata)
    {
        return new TenantDomainOwnershipProofVerificationResult(
            request.TenantId,
            request.DomainName,
            verificationMethod,
            outcome,
            verified,
            rejected,
            challengeIssued,
            publicationPlanned,
            proofCollected,
            proofEvaluated,
            ranAtUtc,
            challengeResult,
            publicationPlanResult,
            httpProofCollectionResult,
            dnsTxtProofCollectionResult,
            evaluationResult,
            domainOwnership,
            reason,
            metadata);
    }
}
