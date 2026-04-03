using Cephalon.Engine.Configuration;
using System.Security.Cryptography;
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
                IsVerified: false,
                Reason: "Package declared signature metadata but no signature value was provided.");
        }

        if (!trustPolicy.TryResolveTrustedSignaturePublicKey(
            signature.KeyId,
            signature.Fingerprint,
            out var trustedPublicKey))
        {
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
                IsVerified: false,
                Reason: $"No trusted public key was configured for {identity}.");
        }

        using var rsa = CreateRsa(trustedPublicKey, packageId);
        var publicKeyFingerprint = ComputePublicKeyFingerprint(rsa);
        if (!string.IsNullOrWhiteSpace(signature.Fingerprint) &&
            !string.Equals(publicKeyFingerprint, NormalizeFingerprint(signature.Fingerprint), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Package '{packageId}' declared signature fingerprint '{signature.Fingerprint}', but the trusted public key fingerprint resolved to '{publicKeyFingerprint}'.");
        }

        var signatureBytes = DecodeSignatureValue(signature.Value, packageId);
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

        return new PackageSignatureVerificationEntry(
            Type: signature.Type,
            Signer: signature.Signer,
            KeyId: signature.KeyId,
            Fingerprint: signature.Fingerprint ?? publicKeyFingerprint,
            Algorithm: signature.Algorithm,
            IsVerified: true,
            Reason: $"Package signature was verified with trusted key '{signature.KeyId ?? publicKeyFingerprint}'.");
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

    private static string ComputePublicKeyFingerprint(RSA rsa)
    {
        var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        return Convert.ToHexString(SHA256.HashData(publicKeyBytes)).ToLowerInvariant();
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
}
