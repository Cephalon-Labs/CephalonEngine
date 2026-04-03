using Cephalon.Engine.Configuration;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Cephalon.Engine.Composition.Packages;

internal static class PackageSignatureVerifier
{
    public static PackageSignatureVerificationResult Verify(
        PackageLoadRequest request,
        byte[] assemblyHash,
        TrustPolicy trustPolicy)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(assemblyHash);
        ArgumentNullException.ThrowIfNull(trustPolicy);

        if (request.Signatures.Count == 0)
        {
            return new PackageSignatureVerificationResult(
                IsVerified: false,
                Reason: "Package did not declare a cryptographic signature.",
                Signatures: []);
        }

        var verifiedCount = 0;
        var signatureResults = new List<PackageSignatureVerificationEntry>(request.Signatures.Count);
        foreach (var signature in request.Signatures)
        {
            var signatureResult = VerifySignature(request.Id, signature, assemblyHash, trustPolicy);
            signatureResults.Add(signatureResult);
            if (signatureResult.IsVerified)
            {
                verifiedCount++;
            }
        }

        if (verifiedCount > 0)
        {
            return new PackageSignatureVerificationResult(
                IsVerified: true,
                Reason: verifiedCount == signatureResults.Count
                    ? $"Package signatures were verified for all {verifiedCount} signer(s)."
                    : $"Package signatures were verified for {verifiedCount} of {signatureResults.Count} signer(s).",
                Signatures: signatureResults);
        }

        return new PackageSignatureVerificationResult(
            IsVerified: false,
            Reason: signatureResults.Count == 1
                ? signatureResults[0].Reason
                : $"Package declared {signatureResults.Count} cryptographic signatures, but none could be verified with the current trust policy.",
            Signatures: signatureResults);
    }

    private static PackageSignatureVerificationEntry VerifySignature(
        string packageId,
        PackageSignatureLoadRequest signature,
        byte[] assemblyHash,
        TrustPolicy trustPolicy)
    {
        if (string.IsNullOrWhiteSpace(signature.Value))
        {
            return new PackageSignatureVerificationEntry(
                Type: signature.Type,
                Signer: signature.Signer,
                KeyId: signature.KeyId,
                Fingerprint: signature.Fingerprint,
                Algorithm: signature.Algorithm,
                VerificationSource: null,
                CertificateThumbprint: null,
                IsVerified: false,
                Reason: "Package declared signature metadata but no signature value was provided.");
        }

        if (trustPolicy.TryResolveTrustedSignaturePublicKey(
            signature.KeyId,
            signature.Fingerprint,
            out var trustedPublicKey))
        {
            return VerifySignatureWithTrustedPublicKey(packageId, signature, assemblyHash, trustedPublicKey);
        }

        if (trustPolicy.TryResolveTrustedSignatureCertificate(
            signature.KeyId,
            signature.Fingerprint,
            out var trustedCertificate))
        {
            return VerifySignatureWithTrustedCertificate(packageId, signature, assemblyHash, trustPolicy, trustedCertificate);
        }

        var identity = !string.IsNullOrWhiteSpace(signature.KeyId)
            ? $"signature key '{signature.KeyId}'"
            : !string.IsNullOrWhiteSpace(signature.Fingerprint)
                ? $"signature fingerprint '{signature.Fingerprint}'"
                : "declared signature identity";

        return new PackageSignatureVerificationEntry(
            Type: signature.Type,
            Signer: signature.Signer,
            KeyId: signature.KeyId,
            Fingerprint: signature.Fingerprint,
            Algorithm: signature.Algorithm,
            VerificationSource: null,
            CertificateThumbprint: null,
            IsVerified: false,
            Reason: $"No trusted public key or signing certificate was configured for {identity}.");
    }

    private static PackageSignatureVerificationEntry VerifySignatureWithTrustedPublicKey(
        string packageId,
        PackageSignatureLoadRequest signature,
        byte[] assemblyHash,
        string trustedPublicKey)
    {
        using var rsa = CreateRsa(trustedPublicKey, packageId);
        var publicKeyFingerprint = ComputePublicKeyFingerprint(rsa);
        EnsureFingerprintMatches(packageId, signature.Fingerprint, publicKeyFingerprint);

        VerifySignatureHash(packageId, signature, assemblyHash, rsa);

        return new PackageSignatureVerificationEntry(
            Type: signature.Type,
            Signer: signature.Signer,
            KeyId: signature.KeyId,
            Fingerprint: signature.Fingerprint ?? publicKeyFingerprint,
            Algorithm: signature.Algorithm,
            VerificationSource: "trusted-public-key",
            CertificateThumbprint: null,
            IsVerified: true,
            Reason: $"Package signature was verified with trusted key '{signature.KeyId ?? publicKeyFingerprint}'.");
    }

    private static PackageSignatureVerificationEntry VerifySignatureWithTrustedCertificate(
        string packageId,
        PackageSignatureLoadRequest signature,
        byte[] assemblyHash,
        TrustPolicy trustPolicy,
        string trustedCertificate)
    {
        using var certificate = LoadCertificate(trustedCertificate, packageId);
        ValidateCertificateChain(certificate, trustPolicy, packageId);

        using var rsa = certificate.GetRSAPublicKey()
            ?? throw new InvalidOperationException(
                $"Package '{packageId}' configured a trusted signing certificate that does not expose an RSA public key.");

        var publicKeyFingerprint = ComputePublicKeyFingerprint(rsa);
        EnsureFingerprintMatches(packageId, signature.Fingerprint, publicKeyFingerprint);

        VerifySignatureHash(packageId, signature, assemblyHash, rsa);

        var certificateThumbprint = NormalizeCertificateThumbprint(certificate.Thumbprint);
        return new PackageSignatureVerificationEntry(
            Type: signature.Type,
            Signer: signature.Signer,
            KeyId: signature.KeyId,
            Fingerprint: signature.Fingerprint ?? publicKeyFingerprint,
            Algorithm: signature.Algorithm,
            VerificationSource: "trusted-certificate-chain",
            CertificateThumbprint: certificateThumbprint,
            IsVerified: true,
            Reason: $"Package signature was verified with trusted certificate '{certificateThumbprint}' after certificate-chain validation.");
    }

    private static RSA CreateRsa(string trustedPublicKey, string packageId)
    {
        try
        {
            var publicKeyPem = ResolvePublicKeyMaterial(trustedPublicKey);
            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            return rsa;
        }
        catch (Exception exception) when (exception is IOException or CryptographicException or ArgumentException)
        {
            throw new InvalidOperationException(
                $"Package '{packageId}' could not load the configured trusted public key for signature verification.",
                exception);
        }
    }

    private static string ResolvePublicKeyMaterial(string configuredValue)
    {
        var trimmed = configuredValue.Trim();
        if (trimmed.StartsWith("-----BEGIN", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var resolvedPath = Path.IsPathRooted(trimmed)
            ? Path.GetFullPath(trimmed)
            : Path.GetFullPath(trimmed, AppContext.BaseDirectory);
        if (!File.Exists(resolvedPath))
        {
            throw new IOException($"Trusted public key path '{resolvedPath}' was not found.");
        }

        return File.ReadAllText(resolvedPath, Encoding.UTF8);
    }

    private static X509Certificate2 LoadCertificate(string configuredValue, string packageId)
    {
        try
        {
            var trimmed = configuredValue.Trim();
            if (trimmed.StartsWith("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal))
            {
                return X509Certificate2.CreateFromPem(trimmed);
            }

            var resolvedPath = Path.IsPathRooted(trimmed)
                ? Path.GetFullPath(trimmed)
                : Path.GetFullPath(trimmed, AppContext.BaseDirectory);
            if (!File.Exists(resolvedPath))
            {
                throw new IOException($"Trusted signing certificate path '{resolvedPath}' was not found.");
            }

            var certificateContents = File.ReadAllText(resolvedPath, Encoding.UTF8);
            if (certificateContents.Contains("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal))
            {
                return X509Certificate2.CreateFromPem(certificateContents);
            }

            return X509CertificateLoader.LoadCertificateFromFile(resolvedPath);
        }
        catch (Exception exception) when (exception is IOException or CryptographicException or ArgumentException)
        {
            throw new InvalidOperationException(
                $"Package '{packageId}' could not load the configured trusted signing certificate for signature verification.",
                exception);
        }
    }

    private static void ValidateCertificateChain(
        X509Certificate2 certificate,
        TrustPolicy trustPolicy,
        string packageId)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

        var authorityCertificates = trustPolicy.TrustedSignatureCertificateAuthorities
            .Select(authority => LoadCertificate(authority, packageId))
            .ToArray();

        try
        {
            if (authorityCertificates.Length > 0)
            {
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                foreach (var authority in authorityCertificates)
                {
                    chain.ChainPolicy.ExtraStore.Add(authority);
                    if (IsSelfSigned(authority))
                    {
                        chain.ChainPolicy.CustomTrustStore.Add(authority);
                    }
                }
            }

            if (!chain.Build(certificate))
            {
                var reasons = chain.ChainStatus.Length == 0
                    ? "No certificate-chain status information was returned."
                    : string.Join(
                        "; ",
                        chain.ChainStatus
                            .Select(static status => status.StatusInformation?.Trim())
                            .Where(static value => !string.IsNullOrWhiteSpace(value)));

                throw new InvalidOperationException(
                    $"Package '{packageId}' failed trusted signing certificate validation: {reasons}");
            }
        }
        finally
        {
            foreach (var authority in authorityCertificates)
            {
                authority.Dispose();
            }
        }
    }

    private static bool IsSelfSigned(X509Certificate2 certificate)
    {
        return string.Equals(
            certificate.SubjectName.Name,
            certificate.IssuerName.Name,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputePublicKeyFingerprint(RSA rsa)
    {
        var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        return Convert.ToHexString(SHA256.HashData(publicKeyBytes)).ToLowerInvariant();
    }

    private static void EnsureFingerprintMatches(
        string packageId,
        string? declaredFingerprint,
        string resolvedFingerprint)
    {
        if (!string.IsNullOrWhiteSpace(declaredFingerprint) &&
            !string.Equals(resolvedFingerprint, NormalizeFingerprint(declaredFingerprint), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Package '{packageId}' declared signature fingerprint '{declaredFingerprint}', but the trusted signing identity fingerprint resolved to '{resolvedFingerprint}'.");
        }
    }

    private static byte[] DecodeSignatureValue(string signatureValue, string packageId)
    {
        try
        {
            return Convert.FromBase64String(signatureValue.Trim());
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                $"Package '{packageId}' declared an invalid Base64 signature value.",
                exception);
        }
    }

    private static void VerifySignatureHash(
        string packageId,
        PackageSignatureLoadRequest signature,
        byte[] assemblyHash,
        RSA rsa)
    {
        var signatureBytes = DecodeSignatureValue(signature.Value!, packageId);
        var hashAlgorithm = ResolveHashAlgorithm(signature.Algorithm, packageId);
        var verified = rsa.VerifyHash(
            assemblyHash,
            signatureBytes,
            hashAlgorithm,
            RSASignaturePadding.Pkcs1);

        if (!verified)
        {
            throw new InvalidOperationException(
                $"Package '{packageId}' failed cryptographic signature verification for signer '{signature.Signer ?? signature.KeyId ?? signature.Fingerprint ?? "unknown"}'.");
        }
    }

    private static HashAlgorithmName ResolveHashAlgorithm(string? algorithm, string packageId)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
        {
            return HashAlgorithmName.SHA256;
        }

        return algorithm.Trim().ToUpperInvariant() switch
        {
            "SHA256" or "RSA-SHA256" or "RS256" => HashAlgorithmName.SHA256,
            "SHA384" or "RSA-SHA384" or "RS384" => HashAlgorithmName.SHA384,
            "SHA512" or "RSA-SHA512" or "RS512" => HashAlgorithmName.SHA512,
            _ => throw new InvalidOperationException(
                $"Package '{packageId}' declared unsupported signature algorithm '{algorithm}'.")
        };
    }

    private static string NormalizeFingerprint(string fingerprint)
    {
        var normalized = fingerprint.Trim();
        if (normalized.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["sha256:".Length..];
        }

        return normalized.Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }

    private static string? NormalizeCertificateThumbprint(string? thumbprint)
    {
        if (string.IsNullOrWhiteSpace(thumbprint))
        {
            return null;
        }

        return thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }
}
