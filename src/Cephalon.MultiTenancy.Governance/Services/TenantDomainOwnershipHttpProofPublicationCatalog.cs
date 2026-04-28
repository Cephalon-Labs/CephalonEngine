using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipHttpProofPublicationCatalog(
    ITenantDomainOwnershipCatalog domainOwnershipCatalog) : ITenantDomainOwnershipHttpProofPublicationCatalog
{
    private const string DefaultHttpContentType = "text/plain; charset=utf-8";

    public IReadOnlyList<TenantDomainOwnershipHttpProofPublicationDescriptor> PublishedProofs =>
        domainOwnershipCatalog.DomainOwnerships
            .Select(TryCreatePublishedProof)
            .Where(static proof => proof is not null)
            .Select(static proof => proof!)
            .OrderBy(static proof => proof.DomainName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static proof => proof.HttpFilePath, StringComparer.Ordinal)
            .ToArray();

    public TenantDomainOwnershipHttpProofPublicationDescriptor? GetByHostAndPath(string hostName, string httpFilePath)
    {
        if (!TryNormalizeHost(hostName, out var normalizedHost) ||
            !TryNormalizeHttpFilePath(httpFilePath, out var normalizedPath))
        {
            return null;
        }

        return PublishedProofs.FirstOrDefault(proof =>
            string.Equals(proof.DomainName, normalizedHost, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(proof.HttpFilePath, normalizedPath, StringComparison.Ordinal));
    }

    private static TenantDomainOwnershipHttpProofPublicationDescriptor? TryCreatePublishedProof(
        TenantDomainOwnershipDescriptor domainOwnership)
    {
        if (!string.Equals(domainOwnership.VerificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var metadata = domainOwnership.Metadata;
        if (!metadata.TryGetValue(TenantDomainOwnershipHttpProofPublicationMetadataKeys.LastHttpProofPublicationOutcome, out var outcome) ||
            !string.Equals(outcome, TenantDomainOwnershipHttpProofPublicationOutcomes.Published, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var httpFilePath = ReadFirst(
            metadata,
            TenantDomainOwnershipHttpProofPublicationMetadataKeys.HttpProofPublicationPath,
            TenantDomainOwnershipProofPublicationPlanMetadataKeys.HttpFilePath);
        var httpFileContent = ReadFirst(
            metadata,
            TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof,
            TenantDomainOwnershipProofMetadataKeys.ExpectedProof);
        var httpContentType = ReadFirst(
            metadata,
            TenantDomainOwnershipHttpProofPublicationMetadataKeys.HttpProofPublicationContentType,
            TenantDomainOwnershipProofPublicationPlanMetadataKeys.HttpContentType) ?? DefaultHttpContentType;
        var proofFingerprint = ReadFirst(
            metadata,
            TenantDomainOwnershipHttpProofPublicationMetadataKeys.HttpProofPublicationContentFingerprint,
            TenantDomainOwnershipProofPublicationPlanMetadataKeys.HttpFileContentFingerprint);

        if (string.IsNullOrWhiteSpace(httpFilePath) ||
            string.IsNullOrWhiteSpace(httpFileContent))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(
                ReadFirst(metadata, TenantDomainOwnershipHttpProofPublicationMetadataKeys.LastHttpProofPublishedAtUtc),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var publishedAtUtc))
        {
            return null;
        }

        proofFingerprint = string.IsNullOrWhiteSpace(proofFingerprint)
            ? ComputeFingerprint(httpFileContent)
            : proofFingerprint.Trim();

        return new TenantDomainOwnershipHttpProofPublicationDescriptor(
            domainOwnership.TenantId,
            domainOwnership.DomainName,
            httpFilePath,
            httpFileContent,
            httpContentType,
            proofFingerprint,
            publishedAtUtc,
            metadata);
    }

    private static string? ReadFirst(IReadOnlyDictionary<string, string> metadata, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (metadata.TryGetValue(key, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static bool TryNormalizeHost(string hostName, out string normalizedHost)
    {
        normalizedHost = string.Empty;
        if (string.IsNullOrWhiteSpace(hostName))
        {
            return false;
        }

        var candidate = hostName.Trim();
        var colonIndex = candidate.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex > 0 && !candidate.Contains(']', StringComparison.Ordinal))
        {
            candidate = candidate[..colonIndex];
        }

        try
        {
            normalizedHost = TenantDomainOwnershipDescriptor.NormalizeDomainName(candidate);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryNormalizeHttpFilePath(string httpFilePath, out string normalizedPath)
    {
        normalizedPath = string.Empty;
        if (string.IsNullOrWhiteSpace(httpFilePath))
        {
            return false;
        }

        normalizedPath = TenantDomainOwnershipHttpProofPublicationDescriptor.NormalizeHttpFilePath(httpFilePath);
        return normalizedPath.Length > 1;
    }

    private static string ComputeFingerprint(string proof)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(proof.Trim()))).ToLowerInvariant();
    }
}
