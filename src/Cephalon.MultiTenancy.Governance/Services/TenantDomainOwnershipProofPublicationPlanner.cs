using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipProofPublicationPlanner(
    MultiTenancyGovernanceOptions options,
    ITenantDomainOwnershipCatalog catalog,
    ITenantDomainOwnershipStore domainOwnershipStore,
    TimeProvider timeProvider,
    ILogger<TenantDomainOwnershipProofPublicationPlanner> logger) : ITenantDomainOwnershipProofPublicationPlanner
{
    private const string HttpContentType = "text/plain; charset=utf-8";

    public ValueTask<TenantDomainOwnershipProofPublicationPlanResult> PlanAsync(
        TenantDomainOwnershipProofPublicationPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var plannedAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        var result = options.EnableDomainOwnershipProofPublicationPlanning
            ? Plan(request, plannedAtUtc)
            : CreateResult(
                request,
                TenantDomainOwnershipProofPublicationPlanOutcomes.Disabled,
                planned: false,
                recorded: false,
                plannedAtUtc,
                verificationMethod: request.VerificationMethod,
                proofValue: null,
                proofFingerprint: null,
                dnsTxtRecordName: null,
                dnsTxtRecordValue: null,
                httpFilePath: null,
                httpFileContent: null,
                httpContentType: null,
                domainOwnership: null,
                reason: "Tenant-domain ownership proof publication planning is disabled.",
                metadata: BuildResultMetadata(
                    request,
                    domainOwnership: null,
                    TenantDomainOwnershipProofPublicationPlanOutcomes.Disabled,
                    plannedAtUtc,
                    proofValue: null,
                    dnsTxtRecordName: null,
                    httpFilePath: null,
                    httpContentType: null));

        if (result.Planned)
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofPublicationPlanned(
                logger,
                result.TenantId,
                result.DomainName,
                result.VerificationMethod ?? "unknown",
                null);
        }
        else
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofPublicationPlanDenied(
                logger,
                result.TenantId,
                result.DomainName,
                result.Outcome,
                result.Reason,
                null);
        }

        return ValueTask.FromResult(result);
    }

    private TenantDomainOwnershipProofPublicationPlanResult Plan(
        TenantDomainOwnershipProofPublicationPlanRequest request,
        DateTimeOffset plannedAtUtc)
    {
        var domainOwnerships = catalog.GetByTenantAndDomain(request.TenantId, request.DomainName);
        if (domainOwnerships.Count == 0)
        {
            var sameDomainOwnerships = catalog.GetByDomainName(request.DomainName);
            if (sameDomainOwnerships.Count > 0)
            {
                var sameDomainOwnership = sameDomainOwnerships[0];
                return CreateResult(
                    request,
                    TenantDomainOwnershipProofPublicationPlanOutcomes.TenantMismatch,
                    planned: false,
                    recorded: false,
                    plannedAtUtc,
                    verificationMethod: sameDomainOwnership.VerificationMethod,
                    proofValue: null,
                    proofFingerprint: null,
                    dnsTxtRecordName: null,
                    dnsTxtRecordValue: null,
                    httpFilePath: null,
                    httpFileContent: null,
                    httpContentType: null,
                    domainOwnership: sameDomainOwnership,
                    reason: "Matching tenant-domain ownership belongs to a different tenant.",
                    metadata: BuildResultMetadata(request, sameDomainOwnership, TenantDomainOwnershipProofPublicationPlanOutcomes.TenantMismatch, plannedAtUtc, proofValue: null, dnsTxtRecordName: null, httpFilePath: null, httpContentType: null));
            }

            return CreateResult(
                request,
                TenantDomainOwnershipProofPublicationPlanOutcomes.NotFound,
                planned: false,
                recorded: false,
                plannedAtUtc,
                verificationMethod: request.VerificationMethod,
                proofValue: null,
                proofFingerprint: null,
                dnsTxtRecordName: null,
                dnsTxtRecordValue: null,
                httpFilePath: null,
                httpFileContent: null,
                httpContentType: null,
                domainOwnership: null,
                reason: "No tenant-domain ownership descriptor matched the supplied domain.",
                metadata: BuildResultMetadata(request, domainOwnership: null, TenantDomainOwnershipProofPublicationPlanOutcomes.NotFound, plannedAtUtc, proofValue: null, dnsTxtRecordName: null, httpFilePath: null, httpContentType: null));
        }

        var domainOwnership = domainOwnerships[0];
        if (request.VerificationMethod is not null &&
            !string.Equals(domainOwnership.VerificationMethod, request.VerificationMethod, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofPublicationPlanOutcomes.VerificationMethodMismatch,
                planned: false,
                recorded: false,
                plannedAtUtc,
                verificationMethod: domainOwnership.VerificationMethod,
                proofValue: null,
                proofFingerprint: null,
                dnsTxtRecordName: null,
                dnsTxtRecordValue: null,
                httpFilePath: null,
                httpFileContent: null,
                httpContentType: null,
                domainOwnership,
                reason: "Matching tenant-domain ownership uses a different verification method.",
                metadata: BuildResultMetadata(request, domainOwnership, TenantDomainOwnershipProofPublicationPlanOutcomes.VerificationMethodMismatch, plannedAtUtc, proofValue: null, dnsTxtRecordName: null, httpFilePath: null, httpContentType: null));
        }

        var verificationMethod = domainOwnership.VerificationMethod;
        if (!IsSupportedPublicationMethod(verificationMethod))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofPublicationPlanOutcomes.UnsupportedVerificationMethod,
                planned: false,
                recorded: false,
                plannedAtUtc,
                verificationMethod,
                proofValue: null,
                proofFingerprint: null,
                dnsTxtRecordName: null,
                dnsTxtRecordValue: null,
                httpFilePath: null,
                httpFileContent: null,
                httpContentType: null,
                domainOwnership,
                reason: "Only DNS TXT and HTTP file verification methods have built-in publication instructions.",
                metadata: BuildResultMetadata(request, domainOwnership, TenantDomainOwnershipProofPublicationPlanOutcomes.UnsupportedVerificationMethod, plannedAtUtc, proofValue: null, dnsTxtRecordName: null, httpFilePath: null, httpContentType: null));
        }

        var proofValue = ResolveExpectedProof(domainOwnership, verificationMethod);
        if (string.IsNullOrWhiteSpace(proofValue))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofPublicationPlanOutcomes.MissingExpectedProof,
                planned: false,
                recorded: false,
                plannedAtUtc,
                verificationMethod,
                proofValue: null,
                proofFingerprint: null,
                dnsTxtRecordName: null,
                dnsTxtRecordValue: null,
                httpFilePath: null,
                httpFileContent: null,
                httpContentType: null,
                domainOwnership,
                reason: "Expected proof metadata is required before tenant-domain ownership proof publication planning can run.",
                metadata: BuildResultMetadata(request, domainOwnership, TenantDomainOwnershipProofPublicationPlanOutcomes.MissingExpectedProof, plannedAtUtc, proofValue: null, dnsTxtRecordName: null, httpFilePath: null, httpContentType: null));
        }

        proofValue = proofValue.Trim();
        var dnsTxtRecordName = ResolveDnsTxtRecordName(domainOwnership, verificationMethod);
        var dnsTxtRecordValue = string.Equals(verificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase)
            ? proofValue
            : null;
        var httpFilePath = ResolveHttpFilePath(domainOwnership, verificationMethod);
        var httpFileContent = string.Equals(verificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase)
            ? proofValue
            : null;
        var httpContentType = httpFileContent is null ? null : HttpContentType;
        var metadata = BuildResultMetadata(
            request,
            domainOwnership,
            TenantDomainOwnershipProofPublicationPlanOutcomes.Planned,
            plannedAtUtc,
            proofValue,
            dnsTxtRecordName,
            httpFilePath,
            httpContentType);
        var plannedDomainOwnership = new TenantDomainOwnershipDescriptor(
            tenantId: domainOwnership.TenantId,
            domainName: domainOwnership.DomainName,
            displayName: domainOwnership.DisplayName,
            status: domainOwnership.Status,
            verificationMethod: domainOwnership.VerificationMethod,
            verifiedAtUtc: domainOwnership.VerifiedAtUtc,
            expiresAtUtc: domainOwnership.ExpiresAtUtc,
            sourceModuleId: domainOwnership.SourceModuleId,
            metadata: metadata);

        if (request.RecordPlan)
        {
            try
            {
                domainOwnershipStore.Upsert(plannedDomainOwnership);
                MultiTenancyGovernanceLoggerMessages.DomainOwnershipStorePersisted(
                    logger,
                    plannedDomainOwnership.TenantId,
                    plannedDomainOwnership.DomainName,
                    domainOwnershipStore.StoreKind,
                    domainOwnershipStore.IsDurable.ToString().ToLowerInvariant(),
                    null);
            }
            catch (Exception exception)
            {
                var reason = "Tenant-domain ownership proof publication plan could not persist domain ownership state.";
                MultiTenancyGovernanceLoggerMessages.DomainOwnershipStorePersistenceFailed(
                    logger,
                    request.TenantId,
                    request.DomainName,
                    domainOwnershipStore.StoreKind,
                    reason,
                    exception);

                return CreateResult(
                    request,
                    TenantDomainOwnershipProofPublicationPlanOutcomes.StoreFailed,
                    planned: false,
                    recorded: false,
                    plannedAtUtc,
                    verificationMethod,
                    proofValue: null,
                    proofFingerprint: null,
                    dnsTxtRecordName: null,
                    dnsTxtRecordValue: null,
                    httpFilePath: null,
                    httpFileContent: null,
                    httpContentType: null,
                    domainOwnership,
                    reason,
                    BuildResultMetadata(
                        request,
                        domainOwnership,
                        TenantDomainOwnershipProofPublicationPlanOutcomes.StoreFailed,
                        plannedAtUtc,
                        proofValue: null,
                        dnsTxtRecordName: null,
                        httpFilePath: null,
                        httpContentType: null));
            }
        }

        return CreateResult(
            request,
            TenantDomainOwnershipProofPublicationPlanOutcomes.Planned,
            planned: true,
            recorded: request.RecordPlan,
            plannedAtUtc,
            verificationMethod,
            proofValue,
            ComputeFingerprint(proofValue),
            dnsTxtRecordName,
            dnsTxtRecordValue,
            httpFilePath,
            httpFileContent,
            httpContentType,
            request.RecordPlan ? plannedDomainOwnership : domainOwnership,
            request.RecordPlan
                ? "Tenant-domain ownership proof publication plan was generated and recorded."
                : "Tenant-domain ownership proof publication plan was generated without recording metadata.",
            metadata);
    }

    private string ResolveDnsTxtRecordName(
        TenantDomainOwnershipDescriptor domainOwnership,
        string verificationMethod)
    {
        if (!string.Equals(verificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (domainOwnership.Metadata.TryGetValue(TenantDomainOwnershipProofChallengeMetadataKeys.DnsTxtRecordName, out var plannedName) &&
            !string.IsNullOrWhiteSpace(plannedName))
        {
            return TenantDomainOwnershipDescriptor.NormalizeDomainName(plannedName);
        }

        var prefix = string.IsNullOrWhiteSpace(options.DomainOwnershipProofChallengeDnsTxtRecordPrefix)
            ? "_cephalon-domain-verification"
            : options.DomainOwnershipProofChallengeDnsTxtRecordPrefix.Trim().TrimEnd('.');
        return TenantDomainOwnershipDescriptor.NormalizeDomainName($"{prefix}.{domainOwnership.DomainName}");
    }

    private string ResolveHttpFilePath(
        TenantDomainOwnershipDescriptor domainOwnership,
        string verificationMethod)
    {
        if (!string.Equals(verificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (domainOwnership.Metadata.TryGetValue(TenantDomainOwnershipProofChallengeMetadataKeys.HttpFilePath, out var plannedPath) &&
            !string.IsNullOrWhiteSpace(plannedPath))
        {
            return NormalizeHttpFilePath(plannedPath);
        }

        var path = string.IsNullOrWhiteSpace(options.DomainOwnershipProofChallengeHttpFilePath)
            ? "/.well-known/cephalon/domain-ownership.txt"
            : options.DomainOwnershipProofChallengeHttpFilePath.Trim();
        return NormalizeHttpFilePath(path);
    }

    private static string? ResolveExpectedProof(
        TenantDomainOwnershipDescriptor domainOwnership,
        string verificationMethod)
    {
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
        TenantDomainOwnershipProofPublicationPlanRequest request,
        TenantDomainOwnershipDescriptor? domainOwnership,
        string outcome,
        DateTimeOffset plannedAtUtc,
        string? proofValue,
        string? dnsTxtRecordName,
        string? httpFilePath,
        string? httpContentType)
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

        metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlanOutcome] = outcome;
        metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlannedAtUtc] = plannedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlanSource] = request.Source ?? "application";
        metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.ProofPublicationPlanningOwnership] = "cephalon-managed";
        metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.ExternalPublicationOwnership] = "application-managed";

        if (!string.IsNullOrWhiteSpace(proofValue))
        {
            var fingerprint = ComputeFingerprint(proofValue);
            metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlanFingerprint] = fingerprint;
            if (!string.IsNullOrWhiteSpace(dnsTxtRecordName))
            {
                metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.DnsTxtRecordName] = dnsTxtRecordName;
                metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.DnsTxtRecordValueFingerprint] = fingerprint;
            }

            if (!string.IsNullOrWhiteSpace(httpFilePath))
            {
                metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.HttpFilePath] = NormalizeHttpFilePath(httpFilePath);
                metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.HttpFileContentFingerprint] = fingerprint;
            }
        }

        if (!string.IsNullOrWhiteSpace(httpContentType))
        {
            metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.HttpContentType] = httpContentType;
        }

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlanActor] = request.Actor;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlanCorrelationId] = request.CorrelationId;
        }

        return metadata;
    }

    private static bool IsSupportedPublicationMethod(string verificationMethod)
    {
        return string.Equals(verificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(verificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeHttpFilePath(string httpFilePath)
    {
        var normalized = httpFilePath.Trim();
        return normalized.Length > 0 && normalized[0] == '/'
            ? normalized
            : $"/{normalized}";
    }

    private static string ComputeFingerprint(string proof)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(proof.Trim()))).ToLowerInvariant();
    }

    private static TenantDomainOwnershipProofPublicationPlanResult CreateResult(
        TenantDomainOwnershipProofPublicationPlanRequest request,
        string outcome,
        bool planned,
        bool recorded,
        DateTimeOffset plannedAtUtc,
        string? verificationMethod,
        string? proofValue,
        string? proofFingerprint,
        string? dnsTxtRecordName,
        string? dnsTxtRecordValue,
        string? httpFilePath,
        string? httpFileContent,
        string? httpContentType,
        TenantDomainOwnershipDescriptor? domainOwnership,
        string reason,
        IReadOnlyDictionary<string, string>? metadata)
    {
        return new TenantDomainOwnershipProofPublicationPlanResult(
            request.TenantId,
            request.DomainName,
            verificationMethod,
            outcome,
            planned,
            recorded,
            plannedAtUtc,
            proofValue,
            proofFingerprint,
            dnsTxtRecordName,
            dnsTxtRecordValue,
            httpFilePath,
            httpFileContent,
            httpContentType,
            domainOwnership,
            reason,
            metadata);
    }
}
