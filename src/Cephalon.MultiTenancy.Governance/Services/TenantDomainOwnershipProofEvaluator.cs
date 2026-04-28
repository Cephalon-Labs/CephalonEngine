using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipProofEvaluator(
    MultiTenancyGovernanceOptions options,
    ITenantDomainOwnershipCatalog catalog,
    ITenantDomainOwnershipVerificationWorkflow verificationWorkflow,
    TimeProvider timeProvider,
    ILogger<TenantDomainOwnershipProofEvaluator> logger) : ITenantDomainOwnershipProofEvaluator
{
    public async ValueTask<TenantDomainOwnershipProofEvaluationResult> EvaluateAsync(
        TenantDomainOwnershipProofEvaluationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var evaluatedAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        var result = options.EnableDomainOwnershipProofEvaluation
            ? await EvaluateCoreAsync(request, evaluatedAtUtc, cancellationToken).ConfigureAwait(false)
            : CreateResult(
                request,
                TenantDomainOwnershipProofEvaluationOutcomes.Disabled,
                matched: false,
                applied: false,
                evaluatedAtUtc,
                verificationMethod: request.VerificationMethod,
                observedProofFingerprint: null,
                expectedProofFingerprint: null,
                domainOwnership: null,
                workflowResult: null,
                reason: "Tenant-domain ownership proof evaluation is disabled.",
                metadata: request.Metadata);

        if (result.Matched && result.Applied)
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofEvaluationVerified(
                logger,
                request.TenantId,
                request.DomainName,
                result.VerificationMethod ?? "unknown",
                null);
        }
        else
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofEvaluationDenied(
                logger,
                request.TenantId,
                request.DomainName,
                result.Outcome,
                result.Reason,
                null);
        }

        return result;
    }

    private async ValueTask<TenantDomainOwnershipProofEvaluationResult> EvaluateCoreAsync(
        TenantDomainOwnershipProofEvaluationRequest request,
        DateTimeOffset evaluatedAtUtc,
        CancellationToken cancellationToken)
    {
        var observedProof = request.ObservedProof;
        if (string.IsNullOrWhiteSpace(observedProof))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofEvaluationOutcomes.MissingObservedProof,
                matched: false,
                applied: false,
                evaluatedAtUtc,
                request.VerificationMethod,
                observedProofFingerprint: null,
                expectedProofFingerprint: null,
                domainOwnership: null,
                workflowResult: null,
                reason: "Observed proof is required before tenant-domain ownership proof evaluation can run.",
                metadata: request.Metadata);
        }

        observedProof = observedProof.Trim();
        var domainOwnerships = catalog.GetByTenantAndDomain(request.TenantId, request.DomainName);
        if (domainOwnerships.Count == 0)
        {
            var sameDomainOwnerships = catalog.GetByDomainName(request.DomainName);
            if (sameDomainOwnerships.Count > 0)
            {
                var sameDomainOwnership = sameDomainOwnerships[0];
                return CreateResult(
                    request,
                    TenantDomainOwnershipProofEvaluationOutcomes.TenantMismatch,
                    matched: false,
                    applied: false,
                    evaluatedAtUtc,
                    sameDomainOwnership.VerificationMethod,
                    observedProofFingerprint: ComputeFingerprint(observedProof),
                    expectedProofFingerprint: null,
                    sameDomainOwnership,
                    workflowResult: null,
                    reason: "Matching tenant-domain ownership belongs to a different tenant.",
                    metadata: BuildResultMetadata(request, sameDomainOwnership, TenantDomainOwnershipProofEvaluationOutcomes.TenantMismatch, observedProof, expectedProof: null));
            }

            return CreateResult(
                request,
                TenantDomainOwnershipProofEvaluationOutcomes.NotFound,
                matched: false,
                applied: false,
                evaluatedAtUtc,
                request.VerificationMethod,
                observedProofFingerprint: ComputeFingerprint(observedProof),
                expectedProofFingerprint: null,
                domainOwnership: null,
                workflowResult: null,
                reason: "No tenant-domain ownership descriptor matched the supplied domain.",
                metadata: request.Metadata);
        }

        var domainOwnership = domainOwnerships[0];
        var verificationMethod = request.VerificationMethod ?? domainOwnership.VerificationMethod;
        if (request.VerificationMethod is not null &&
            !string.Equals(domainOwnership.VerificationMethod, request.VerificationMethod, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofEvaluationOutcomes.VerificationMethodMismatch,
                matched: false,
                applied: false,
                evaluatedAtUtc,
                domainOwnership.VerificationMethod,
                observedProofFingerprint: ComputeFingerprint(observedProof),
                expectedProofFingerprint: null,
                domainOwnership,
                workflowResult: null,
                reason: "Matching tenant-domain ownership uses a different verification method.",
                metadata: BuildResultMetadata(request, domainOwnership, TenantDomainOwnershipProofEvaluationOutcomes.VerificationMethodMismatch, observedProof, expectedProof: null));
        }

        var expectedProof = ResolveExpectedProof(request, domainOwnership, verificationMethod);
        if (string.IsNullOrWhiteSpace(expectedProof))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofEvaluationOutcomes.MissingExpectedProof,
                matched: false,
                applied: false,
                evaluatedAtUtc,
                verificationMethod,
                observedProofFingerprint: ComputeFingerprint(observedProof),
                expectedProofFingerprint: null,
                domainOwnership,
                workflowResult: null,
                reason: "Expected proof metadata is required before tenant-domain ownership proof evaluation can run.",
                metadata: BuildResultMetadata(request, domainOwnership, TenantDomainOwnershipProofEvaluationOutcomes.MissingExpectedProof, observedProof, expectedProof: null));
        }

        expectedProof = expectedProof.Trim();
        var matched = string.Equals(observedProof, expectedProof, StringComparison.Ordinal);
        var outcome = matched
            ? TenantDomainOwnershipProofEvaluationOutcomes.Verified
            : TenantDomainOwnershipProofEvaluationOutcomes.Rejected;
        var metadata = BuildResultMetadata(request, domainOwnership, outcome, observedProof, expectedProof);
        var command = matched
            ? TenantDomainOwnershipVerificationWorkflowCommands.Verify
            : TenantDomainOwnershipVerificationWorkflowCommands.Reject;
        var workflowResult = await verificationWorkflow.ApplyAsync(
            new TenantDomainOwnershipVerificationWorkflowRequest(
                command,
                request.TenantId,
                request.DomainName,
                verificationMethod: verificationMethod,
                actor: request.Actor,
                reason: matched
                    ? "Tenant-domain ownership proof matched the expected value."
                    : "Tenant-domain ownership proof did not match the expected value.",
                evidence: CreateEvidenceSummary(request, observedProof, expectedProof),
                atUtc: evaluatedAtUtc,
                expiresAtUtc: request.ExpiresAtUtc,
                correlationId: request.CorrelationId,
                metadata: metadata),
            cancellationToken).ConfigureAwait(false);

        if (!workflowResult.Applied)
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofEvaluationOutcomes.WorkflowDenied,
                matched,
                applied: false,
                evaluatedAtUtc,
                verificationMethod,
                observedProofFingerprint: ComputeFingerprint(observedProof),
                expectedProofFingerprint: ComputeFingerprint(expectedProof),
                workflowResult.DomainOwnership ?? domainOwnership,
                workflowResult,
                $"Tenant-domain ownership proof evaluation could not apply workflow transition '{command}'. {workflowResult.Reason}",
                metadata: workflowResult.Metadata);
        }

        return CreateResult(
            request,
            outcome,
            matched,
            applied: true,
            evaluatedAtUtc,
            verificationMethod,
            observedProofFingerprint: ComputeFingerprint(observedProof),
            expectedProofFingerprint: ComputeFingerprint(expectedProof),
            workflowResult.DomainOwnership ?? domainOwnership,
            workflowResult,
            matched
                ? "Observed proof matched expected proof and domain ownership was verified."
                : "Observed proof did not match expected proof and domain ownership was rejected.",
            metadata: workflowResult.Metadata);
    }

    private static string? ResolveExpectedProof(
        TenantDomainOwnershipProofEvaluationRequest request,
        TenantDomainOwnershipDescriptor domainOwnership,
        string verificationMethod)
    {
        if (!string.IsNullOrWhiteSpace(request.ExpectedProof))
        {
            return request.ExpectedProof;
        }

        if (string.Equals(verificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase) &&
            domainOwnership.Metadata.TryGetValue(TenantDomainOwnershipProofMetadataKeys.ExpectedDnsTxtProof, out var dnsProof) &&
            !string.IsNullOrWhiteSpace(dnsProof))
        {
            return dnsProof;
        }

        if (string.Equals(verificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase) &&
            domainOwnership.Metadata.TryGetValue(TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof, out var httpProof) &&
            !string.IsNullOrWhiteSpace(httpProof))
        {
            return httpProof;
        }

        return domainOwnership.Metadata.TryGetValue(TenantDomainOwnershipProofMetadataKeys.ExpectedProof, out var proof) &&
            !string.IsNullOrWhiteSpace(proof)
            ? proof
            : null;
    }

    private static Dictionary<string, string> BuildResultMetadata(
        TenantDomainOwnershipProofEvaluationRequest request,
        TenantDomainOwnershipDescriptor? domainOwnership,
        string outcome,
        string? observedProof,
        string? expectedProof)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (domainOwnership is not null)
        {
            foreach (var pair in domainOwnership.Metadata)
            {
                metadata[pair.Key] = pair.Value;
            }
        }

        foreach (var pair in request.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationOutcome] = outcome;
        metadata[TenantDomainOwnershipProofMetadataKeys.ProofEvaluationOwnership] = "cephalon-managed";
        metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationSource] = request.Source ?? "application";
        if (!string.IsNullOrWhiteSpace(observedProof))
        {
            metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationObservedFingerprint] = ComputeFingerprint(observedProof);
        }

        if (!string.IsNullOrWhiteSpace(expectedProof))
        {
            metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationExpectedFingerprint] = ComputeFingerprint(expectedProof);
        }

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationActor] = request.Actor;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationCorrelationId] = request.CorrelationId;
        }

        return metadata;
    }

    private static string CreateEvidenceSummary(
        TenantDomainOwnershipProofEvaluationRequest request,
        string observedProof,
        string expectedProof)
    {
        return $"source={request.Source ?? "application"};observedSha256={ComputeFingerprint(observedProof)};expectedSha256={ComputeFingerprint(expectedProof)}";
    }

    private static string ComputeFingerprint(string proof)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(proof.Trim()))).ToLowerInvariant();
    }

    private static TenantDomainOwnershipProofEvaluationResult CreateResult(
        TenantDomainOwnershipProofEvaluationRequest request,
        string outcome,
        bool matched,
        bool applied,
        DateTimeOffset evaluatedAtUtc,
        string? verificationMethod,
        string? observedProofFingerprint,
        string? expectedProofFingerprint,
        TenantDomainOwnershipDescriptor? domainOwnership,
        TenantDomainOwnershipVerificationWorkflowResult? workflowResult,
        string reason,
        IReadOnlyDictionary<string, string>? metadata)
    {
        return new TenantDomainOwnershipProofEvaluationResult(
            request.TenantId,
            request.DomainName,
            verificationMethod,
            outcome,
            matched,
            applied,
            evaluatedAtUtc,
            observedProofFingerprint,
            expectedProofFingerprint,
            domainOwnership,
            workflowResult,
            reason,
            metadata);
    }
}
