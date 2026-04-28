using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipProofChallengeIssuer(
    MultiTenancyGovernanceOptions options,
    ITenantDomainOwnershipCatalog catalog,
    ITenantDomainOwnershipStore domainOwnershipStore,
    TimeProvider timeProvider,
    ILogger<TenantDomainOwnershipProofChallengeIssuer> logger) : ITenantDomainOwnershipProofChallengeIssuer
{
    public ValueTask<TenantDomainOwnershipProofChallengeResult> IssueAsync(
        TenantDomainOwnershipProofChallengeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var issuedAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        var result = options.EnableDomainOwnershipProofChallengeIssuance
            ? Issue(request, issuedAtUtc)
            : CreateResult(
                request,
                TenantDomainOwnershipProofChallengeOutcomes.Disabled,
                issued: false,
                issuedAtUtc,
                verificationMethod: request.VerificationMethod,
                challengeValue: null,
                challengeFingerprint: null,
                dnsTxtRecordName: null,
                httpFilePath: null,
                domainOwnership: null,
                reason: "Tenant-domain ownership proof challenge issuance is disabled.",
                metadata: BuildResultMetadata(
                    request,
                    domainOwnership: null,
                    TenantDomainOwnershipProofChallengeOutcomes.Disabled,
                    issuedAtUtc,
                    challengeValue: null,
                    dnsTxtRecordName: null,
                    httpFilePath: null));

        if (result.Issued)
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofChallengeIssued(
                logger,
                result.TenantId,
                result.DomainName,
                result.VerificationMethod ?? "unknown",
                null);
        }
        else
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofChallengeDenied(
                logger,
                result.TenantId,
                result.DomainName,
                result.Outcome,
                result.Reason,
                null);
        }

        return ValueTask.FromResult(result);
    }

    private TenantDomainOwnershipProofChallengeResult Issue(
        TenantDomainOwnershipProofChallengeRequest request,
        DateTimeOffset issuedAtUtc)
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
                    TenantDomainOwnershipProofChallengeOutcomes.TenantMismatch,
                    issued: false,
                    issuedAtUtc,
                    verificationMethod: sameDomainOwnership.VerificationMethod,
                    challengeValue: null,
                    challengeFingerprint: null,
                    dnsTxtRecordName: null,
                    httpFilePath: null,
                    domainOwnership: sameDomainOwnership,
                    reason: "Matching tenant-domain ownership belongs to a different tenant.",
                    metadata: BuildResultMetadata(request, sameDomainOwnership, TenantDomainOwnershipProofChallengeOutcomes.TenantMismatch, issuedAtUtc, challengeValue: null, dnsTxtRecordName: null, httpFilePath: null));
            }

            return CreateOrRefresh(request, domainOwnership: null, issuedAtUtc);
        }

        var domainOwnership = domainOwnerships[0];
        if (request.VerificationMethod is not null &&
            !string.Equals(domainOwnership.VerificationMethod, request.VerificationMethod, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofChallengeOutcomes.VerificationMethodMismatch,
                issued: false,
                issuedAtUtc,
                verificationMethod: domainOwnership.VerificationMethod,
                challengeValue: null,
                challengeFingerprint: null,
                dnsTxtRecordName: null,
                httpFilePath: null,
                domainOwnership,
                reason: "Matching tenant-domain ownership uses a different verification method.",
                metadata: BuildResultMetadata(request, domainOwnership, TenantDomainOwnershipProofChallengeOutcomes.VerificationMethodMismatch, issuedAtUtc, challengeValue: null, dnsTxtRecordName: null, httpFilePath: null));
        }

        if (IsStatus(domainOwnership.Status, TenantDomainOwnershipStatuses.Verified))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofChallengeOutcomes.AlreadyVerified,
                issued: false,
                issuedAtUtc,
                verificationMethod: domainOwnership.VerificationMethod,
                challengeValue: null,
                challengeFingerprint: null,
                dnsTxtRecordName: null,
                httpFilePath: null,
                domainOwnership,
                reason: "Tenant-domain ownership is already verified.",
                metadata: BuildResultMetadata(request, domainOwnership, TenantDomainOwnershipProofChallengeOutcomes.AlreadyVerified, issuedAtUtc, challengeValue: null, dnsTxtRecordName: null, httpFilePath: null));
        }

        if (IsStatus(domainOwnership.Status, TenantDomainOwnershipStatuses.Suspended))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipProofChallengeOutcomes.InvalidStatus,
                issued: false,
                issuedAtUtc,
                verificationMethod: domainOwnership.VerificationMethod,
                challengeValue: null,
                challengeFingerprint: null,
                dnsTxtRecordName: null,
                httpFilePath: null,
                domainOwnership,
                reason: "Tenant-domain ownership is suspended and cannot receive a new proof challenge.",
                metadata: BuildResultMetadata(request, domainOwnership, TenantDomainOwnershipProofChallengeOutcomes.InvalidStatus, issuedAtUtc, challengeValue: null, dnsTxtRecordName: null, httpFilePath: null));
        }

        return CreateOrRefresh(request, domainOwnership, issuedAtUtc);
    }

    private TenantDomainOwnershipProofChallengeResult CreateOrRefresh(
        TenantDomainOwnershipProofChallengeRequest request,
        TenantDomainOwnershipDescriptor? domainOwnership,
        DateTimeOffset issuedAtUtc)
    {
        var verificationMethod = request.VerificationMethod ??
            domainOwnership?.VerificationMethod ??
            TenantDomainVerificationMethods.DnsTxt;
        var challengeValue = request.ChallengeValue ?? GenerateChallengeValue();
        var dnsTxtRecordName = ResolveDnsTxtRecordName(request, verificationMethod);
        var httpFilePath = ResolveHttpFilePath(request, verificationMethod);
        var metadata = BuildResultMetadata(
            request,
            domainOwnership,
            TenantDomainOwnershipProofChallengeOutcomes.Issued,
            issuedAtUtc,
            challengeValue,
            dnsTxtRecordName,
            httpFilePath);
        var issuedDomainOwnership = new TenantDomainOwnershipDescriptor(
            tenantId: request.TenantId,
            domainName: request.DomainName,
            displayName: request.DisplayName ?? domainOwnership?.DisplayName,
            status: TenantDomainOwnershipStatuses.Pending,
            verificationMethod: verificationMethod,
            verifiedAtUtc: null,
            expiresAtUtc: request.ExpiresAtUtc ?? ResolveExpiresAtUtc(domainOwnership),
            sourceModuleId: domainOwnership?.SourceModuleId,
            metadata: metadata);

        try
        {
            domainOwnershipStore.Upsert(issuedDomainOwnership);
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipStorePersisted(
                logger,
                issuedDomainOwnership.TenantId,
                issuedDomainOwnership.DomainName,
                domainOwnershipStore.StoreKind,
                domainOwnershipStore.IsDurable.ToString().ToLowerInvariant(),
                null);
        }
        catch (Exception exception)
        {
            var reason = "Tenant-domain ownership proof challenge could not persist domain ownership state.";
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipStorePersistenceFailed(
                logger,
                request.TenantId,
                request.DomainName,
                domainOwnershipStore.StoreKind,
                reason,
                exception);

            return CreateResult(
                request,
                TenantDomainOwnershipProofChallengeOutcomes.StoreFailed,
                issued: false,
                issuedAtUtc,
                verificationMethod,
                challengeValue: null,
                challengeFingerprint: null,
                dnsTxtRecordName: null,
                httpFilePath: null,
                domainOwnership,
                reason,
                BuildResultMetadata(
                    request,
                    domainOwnership,
                    TenantDomainOwnershipProofChallengeOutcomes.StoreFailed,
                    issuedAtUtc,
                    challengeValue: null,
                    dnsTxtRecordName: null,
                    httpFilePath: null));
        }

        return CreateResult(
            request,
            TenantDomainOwnershipProofChallengeOutcomes.Issued,
            issued: true,
            issuedAtUtc,
            verificationMethod,
            challengeValue,
            ComputeFingerprint(challengeValue),
            dnsTxtRecordName,
            httpFilePath,
            issuedDomainOwnership,
            "Tenant-domain ownership proof challenge was issued and stored.",
            metadata);
    }

    private string? ResolveDnsTxtRecordName(TenantDomainOwnershipProofChallengeRequest request, string verificationMethod)
    {
        if (!string.Equals(verificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.DnsTxtRecordName))
        {
            return TenantDomainOwnershipDescriptor.NormalizeDomainName(request.DnsTxtRecordName);
        }

        var prefix = string.IsNullOrWhiteSpace(options.DomainOwnershipProofChallengeDnsTxtRecordPrefix)
            ? "_cephalon-domain-verification"
            : options.DomainOwnershipProofChallengeDnsTxtRecordPrefix.Trim().TrimEnd('.');
        return TenantDomainOwnershipDescriptor.NormalizeDomainName($"{prefix}.{request.DomainName}");
    }

    private static DateTimeOffset? ResolveExpiresAtUtc(TenantDomainOwnershipDescriptor? domainOwnership)
    {
        if (domainOwnership is null ||
            IsStatus(domainOwnership.Status, TenantDomainOwnershipStatuses.Expired))
        {
            return null;
        }

        return domainOwnership.ExpiresAtUtc;
    }

    private string? ResolveHttpFilePath(TenantDomainOwnershipProofChallengeRequest request, string verificationMethod)
    {
        if (!string.Equals(verificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.HttpFilePath))
        {
            return request.HttpFilePath;
        }

        var path = string.IsNullOrWhiteSpace(options.DomainOwnershipProofChallengeHttpFilePath)
            ? "/.well-known/cephalon/domain-ownership.txt"
            : options.DomainOwnershipProofChallengeHttpFilePath.Trim();
        return path.Length > 0 && path[0] == '/' ? path : $"/{path}";
    }

    private static Dictionary<string, string> BuildResultMetadata(
        TenantDomainOwnershipProofChallengeRequest request,
        TenantDomainOwnershipDescriptor? domainOwnership,
        string outcome,
        DateTimeOffset issuedAtUtc,
        string? challengeValue,
        string? dnsTxtRecordName,
        string? httpFilePath)
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

        metadata[TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeOutcome] = outcome;
        metadata[TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeIssuedAtUtc] = issuedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeSource] = request.Source ?? "application";
        metadata[TenantDomainOwnershipProofChallengeMetadataKeys.ProofChallengeOwnership] = "cephalon-managed";
        if (request.ExpiresAtUtc is not null)
        {
            metadata[TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeExpiresAtUtc] = request.ExpiresAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(challengeValue))
        {
            metadata.Remove(TenantDomainOwnershipProofMetadataKeys.ExpectedDnsTxtProof);
            metadata.Remove(TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof);
            metadata[TenantDomainOwnershipProofMetadataKeys.ExpectedProof] = challengeValue;
            metadata[TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeFingerprint] = ComputeFingerprint(challengeValue);
        }

        var verificationMethod = request.VerificationMethod ?? domainOwnership?.VerificationMethod ?? TenantDomainVerificationMethods.DnsTxt;
        if (!string.IsNullOrWhiteSpace(challengeValue) &&
            string.Equals(verificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase))
        {
            metadata[TenantDomainOwnershipProofMetadataKeys.ExpectedDnsTxtProof] = challengeValue;
        }

        if (!string.IsNullOrWhiteSpace(challengeValue) &&
            string.Equals(verificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase))
        {
            metadata[TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof] = challengeValue;
        }

        if (!string.IsNullOrWhiteSpace(dnsTxtRecordName))
        {
            metadata[TenantDomainOwnershipProofChallengeMetadataKeys.DnsTxtRecordName] = dnsTxtRecordName;
        }

        if (!string.IsNullOrWhiteSpace(httpFilePath))
        {
            metadata[TenantDomainOwnershipProofChallengeMetadataKeys.HttpFilePath] = httpFilePath;
        }

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            metadata[TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeActor] = request.Actor;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata[TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeCorrelationId] = request.CorrelationId;
        }

        return metadata;
    }

    private static string GenerateChallengeValue()
    {
        return $"cephalon-domain-proof-{Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant()}";
    }

    private static string ComputeFingerprint(string proof)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(proof.Trim()))).ToLowerInvariant();
    }

    private static TenantDomainOwnershipProofChallengeResult CreateResult(
        TenantDomainOwnershipProofChallengeRequest request,
        string outcome,
        bool issued,
        DateTimeOffset issuedAtUtc,
        string? verificationMethod,
        string? challengeValue,
        string? challengeFingerprint,
        string? dnsTxtRecordName,
        string? httpFilePath,
        TenantDomainOwnershipDescriptor? domainOwnership,
        string reason,
        IReadOnlyDictionary<string, string>? metadata)
    {
        return new TenantDomainOwnershipProofChallengeResult(
            request.TenantId,
            request.DomainName,
            verificationMethod,
            outcome,
            issued,
            issuedAtUtc,
            challengeValue,
            challengeFingerprint,
            dnsTxtRecordName,
            httpFilePath,
            domainOwnership,
            reason,
            metadata);
    }

    private static bool IsStatus(string status, string expected)
    {
        return string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);
    }
}
