using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Services;

internal sealed class AmazonSesSnsSignatureVerifier(
    AmazonSesInvitationDeliveryAspNetCoreOptions options,
    AmazonSesSnsSigningCertificateDownloader certificateDownloader)
{
    public async ValueTask<AmazonSesSnsSignatureVerificationResult> VerifyAsync(
        JsonElement root,
        CancellationToken cancellationToken)
    {
        if (!options.RequireSnsSignatureVerification)
        {
            return AmazonSesSnsSignatureVerificationResult.NotConfigured();
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return AmazonSesSnsSignatureVerificationResult.Fail(
                "sns-envelope-required",
                "Amazon SES SNS signature verification requires an SNS JSON object envelope.",
                StatusCodes.Status401Unauthorized);
        }

        if (!TryReadString(root, "Type", out var messageType) ||
            !IsSupportedMessageType(messageType))
        {
            return AmazonSesSnsSignatureVerificationResult.Fail(
                "message-type-unsupported",
                "The SNS message Type must be Notification, SubscriptionConfirmation, or UnsubscribeConfirmation.",
                StatusCodes.Status401Unauthorized);
        }

        if (!TryReadString(root, "TopicArn", out var topicArn))
        {
            return AmazonSesSnsSignatureVerificationResult.Fail(
                "topic-arn-missing",
                "The SNS message must include TopicArn before its signature can be verified.",
                StatusCodes.Status401Unauthorized);
        }

        var allowedTopicArns = options.GetAllowedSnsTopicArns();
        if (options.RequireAllowedSnsTopicArn)
        {
            if (allowedTopicArns.Count == 0)
            {
                return AmazonSesSnsSignatureVerificationResult.Fail(
                    "allowed-topic-arns-missing",
                    "Configure AllowedSnsTopicArns before requiring Amazon SNS signature verification.",
                    StatusCodes.Status500InternalServerError);
            }

            if (!allowedTopicArns.Contains(topicArn))
            {
                return AmazonSesSnsSignatureVerificationResult.Fail(
                    "topic-arn-not-allowed",
                    "The SNS message TopicArn is not allowed by this callback endpoint.",
                    StatusCodes.Status401Unauthorized,
                    topicArn: topicArn,
                    messageType: messageType);
            }
        }

        if (!TryReadString(root, "SignatureVersion", out var signatureVersion))
        {
            return AmazonSesSnsSignatureVerificationResult.Fail(
                "signature-version-missing",
                "The SNS message must include SignatureVersion before it can be verified.",
                StatusCodes.Status401Unauthorized,
                topicArn: topicArn,
                messageType: messageType);
        }

        if (!TryResolveHashAlgorithm(signatureVersion, options.RequireSnsSignatureVersion2, out var hashAlgorithm, out var algorithmName, out var versionOutcome))
        {
            return AmazonSesSnsSignatureVerificationResult.Fail(
                versionOutcome,
                options.RequireSnsSignatureVersion2
                    ? "The SNS message must use SignatureVersion 2 because RequireSnsSignatureVersion2 is enabled."
                    : "The SNS message SignatureVersion must be 1 or 2.",
                StatusCodes.Status401Unauthorized,
                topicArn: topicArn,
                messageType: messageType,
                signatureVersion: signatureVersion);
        }

        if (!TryReadString(root, "Signature", out var signatureValue))
        {
            return AmazonSesSnsSignatureVerificationResult.Fail(
                "signature-missing",
                "The SNS message must include Signature before it can be verified.",
                StatusCodes.Status401Unauthorized,
                topicArn: topicArn,
                messageType: messageType,
                signatureVersion: signatureVersion);
        }

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(signatureValue);
        }
        catch (FormatException)
        {
            return AmazonSesSnsSignatureVerificationResult.Fail(
                "signature-invalid",
                "The SNS message Signature must be Base64 encoded.",
                StatusCodes.Status401Unauthorized,
                topicArn: topicArn,
                messageType: messageType,
                signatureVersion: signatureVersion,
                algorithm: algorithmName);
        }

        if (!TryCreateStringToSign(root, messageType, out var stringToSign, out var messageId, out var timestamp, out var canonicalOutcome))
        {
            return AmazonSesSnsSignatureVerificationResult.Fail(
                canonicalOutcome,
                "The SNS message is missing a required signed field for its Type.",
                StatusCodes.Status401Unauthorized,
                topicArn: topicArn,
                messageType: messageType,
                signatureVersion: signatureVersion,
                algorithm: algorithmName);
        }

        if (!TryGetSigningCertificateUrl(root, out var signingCertificateUrl, out var certificateUrlOutcome))
        {
            return AmazonSesSnsSignatureVerificationResult.Fail(
                certificateUrlOutcome,
                "The SNS SigningCertURL must be an HTTPS Amazon SNS certificate URL.",
                StatusCodes.Status401Unauthorized,
                topicArn: topicArn,
                messageType: messageType,
                messageId: messageId,
                signatureVersion: signatureVersion,
                algorithm: algorithmName);
        }

        var certificateResult = await ResolveCertificateAsync(signingCertificateUrl, cancellationToken).ConfigureAwait(false);
        if (!certificateResult.Loaded)
        {
            return AmazonSesSnsSignatureVerificationResult.Fail(
                certificateResult.Outcome,
                certificateResult.Detail,
                certificateResult.StatusCode,
                topicArn: topicArn,
                messageType: messageType,
                messageId: messageId,
                signatureVersion: signatureVersion,
                algorithm: algorithmName,
                signingCertificateUrlHost: signingCertificateUrl.Host);
        }

        using var certificate = certificateResult.Certificate!;
        var certificateValidation = ValidateCertificate(certificate);
        if (!certificateValidation.Valid)
        {
            return AmazonSesSnsSignatureVerificationResult.Fail(
                certificateValidation.Outcome,
                certificateValidation.Detail,
                StatusCodes.Status401Unauthorized,
                topicArn: topicArn,
                messageType: messageType,
                messageId: messageId,
                signatureVersion: signatureVersion,
                algorithm: algorithmName,
                signingCertificateUrlHost: signingCertificateUrl.Host,
                certificateThumbprint: certificate.Thumbprint);
        }

        using var publicKey = certificate.GetRSAPublicKey();
        if (publicKey is null)
        {
            return AmazonSesSnsSignatureVerificationResult.Fail(
                "certificate-public-key-invalid",
                "The SNS signing certificate did not expose an RSA public key.",
                StatusCodes.Status401Unauthorized,
                topicArn: topicArn,
                messageType: messageType,
                messageId: messageId,
                signatureVersion: signatureVersion,
                algorithm: algorithmName,
                signingCertificateUrlHost: signingCertificateUrl.Host,
                certificateThumbprint: certificate.Thumbprint);
        }

        var stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);
        if (!publicKey.VerifyData(stringToSignBytes, signature, hashAlgorithm, RSASignaturePadding.Pkcs1))
        {
            return AmazonSesSnsSignatureVerificationResult.Fail(
                "signature-invalid",
                "The SNS message Signature did not match the canonical string-to-sign.",
                StatusCodes.Status401Unauthorized,
                topicArn: topicArn,
                messageType: messageType,
                messageId: messageId,
                signatureVersion: signatureVersion,
                algorithm: algorithmName,
                signingCertificateUrlHost: signingCertificateUrl.Host,
                certificateThumbprint: certificate.Thumbprint);
        }

        return AmazonSesSnsSignatureVerificationResult.CreateVerified(
            topicArn,
            messageType,
            messageId,
            timestamp,
            signatureVersion,
            algorithmName,
            signingCertificateUrl.Host,
            CreateSha256Fingerprint(signatureValue),
            certificate.Thumbprint);
    }

    private async ValueTask<AmazonSesSnsSigningCertificateLoadResult> ResolveCertificateAsync(
        Uri signingCertificateUrl,
        CancellationToken cancellationToken)
    {
        var pinnedCertificate = options.GetPinnedSnsSigningCertificatePem();
        if (pinnedCertificate is not null)
        {
            try
            {
                return AmazonSesSnsSigningCertificateLoadResult.FromCertificate(
                    LoadCertificateFromPem(pinnedCertificate));
            }
            catch (Exception ex) when (ex is ArgumentException or CryptographicException)
            {
                return AmazonSesSnsSigningCertificateLoadResult.Fail(
                    "pinned-certificate-invalid",
                    "The configured PinnedSnsSigningCertificatePem value could not be loaded as an X.509 certificate.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        try
        {
            return AmazonSesSnsSigningCertificateLoadResult.FromCertificate(
                await certificateDownloader.DownloadAsync(signingCertificateUrl, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or CryptographicException)
        {
            return AmazonSesSnsSigningCertificateLoadResult.Fail(
                "signing-certificate-unavailable",
                "The SNS signing certificate could not be retrieved or loaded.",
                StatusCodes.Status401Unauthorized);
        }
    }

    private AmazonSesSnsSigningCertificateValidationResult ValidateCertificate(X509Certificate2 certificate)
    {
        var now = DateTime.UtcNow;
        if (now < certificate.NotBefore.ToUniversalTime() ||
            now > certificate.NotAfter.ToUniversalTime())
        {
            return AmazonSesSnsSigningCertificateValidationResult.Fail(
                "certificate-time-invalid",
                "The SNS signing certificate is not valid at the current UTC time.");
        }

        if (!options.ValidateSnsSigningCertificateChain)
        {
            return AmazonSesSnsSigningCertificateValidationResult.Success();
        }

        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationTime = DateTime.UtcNow;
        return chain.Build(certificate)
            ? AmazonSesSnsSigningCertificateValidationResult.Success()
            : AmazonSesSnsSigningCertificateValidationResult.Fail(
                "certificate-chain-invalid",
                "The SNS signing certificate chain could not be validated.");
    }

    private static bool TryCreateStringToSign(
        JsonElement root,
        string messageType,
        out string stringToSign,
        out string? messageId,
        out string? timestamp,
        out string outcome)
    {
        messageId = null;
        timestamp = null;
        outcome = "verified";
        var fields = new List<(string Name, string Value)>();
        if (!TryAddField(root, fields, "Message", required: true, out outcome) ||
            !TryAddField(root, fields, "MessageId", required: true, out outcome))
        {
            stringToSign = string.Empty;
            return false;
        }

        messageId = fields[^1].Value;
        if (string.Equals(messageType, "Notification", StringComparison.Ordinal))
        {
            if (!TryAddField(root, fields, "Subject", required: false, out outcome))
            {
                stringToSign = string.Empty;
                return false;
            }
        }
        else if (!TryAddField(root, fields, "SubscribeURL", required: true, out outcome))
        {
            stringToSign = string.Empty;
            return false;
        }

        if (!TryAddField(root, fields, "Timestamp", required: true, out outcome))
        {
            stringToSign = string.Empty;
            return false;
        }

        timestamp = fields[^1].Value;
        if (!string.Equals(messageType, "Notification", StringComparison.Ordinal) &&
            !TryAddField(root, fields, "Token", required: true, out outcome))
        {
            stringToSign = string.Empty;
            return false;
        }

        if (!TryAddField(root, fields, "TopicArn", required: true, out outcome) ||
            !TryAddField(root, fields, "Type", required: true, out outcome))
        {
            stringToSign = string.Empty;
            return false;
        }

        var builder = new StringBuilder();
        for (var index = 0; index < fields.Count; index++)
        {
            if (index > 0)
            {
                builder.Append('\n');
            }

            builder
                .Append(fields[index].Name)
                .Append('\n')
                .Append(fields[index].Value);
        }

        stringToSign = builder.ToString();
        return true;
    }

    private static bool TryAddField(
        JsonElement root,
        List<(string Name, string Value)> fields,
        string name,
        bool required,
        out string outcome)
    {
        outcome = "verified";
        if (!TryReadRawString(root, name, out var value))
        {
            if (!required)
            {
                return true;
            }

            outcome = "signed-field-missing";
            return false;
        }

        fields.Add((name, value));
        return true;
    }

    private static bool TryGetSigningCertificateUrl(JsonElement root, out Uri url, out string outcome)
    {
        url = null!;
        if (!TryReadString(root, "SigningCertURL", out var value))
        {
            outcome = "signing-certificate-url-missing";
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed))
        {
            outcome = "signing-certificate-url-invalid";
            return false;
        }

        if (!string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            outcome = "signing-certificate-url-not-https";
            return false;
        }

        var host = parsed.IdnHost.ToLowerInvariant();
        if (!IsTrustedSnsSigningCertificateHost(host))
        {
            outcome = "signing-certificate-url-host-untrusted";
            return false;
        }

        if (!parsed.AbsolutePath.StartsWith("/SimpleNotificationService", StringComparison.Ordinal) ||
            !parsed.AbsolutePath.EndsWith(".pem", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(parsed.Query) ||
            !string.IsNullOrEmpty(parsed.Fragment))
        {
            outcome = "signing-certificate-url-path-untrusted";
            return false;
        }

        url = parsed;
        outcome = "verified";
        return true;
    }

    private static bool IsTrustedSnsSigningCertificateHost(string host)
    {
        const string AmazonAwsSuffix = ".amazonaws.com";
        const string AmazonAwsChinaSuffix = ".amazonaws.com.cn";

        return IsSingleRegionSnsHost(host, AmazonAwsSuffix) ||
            IsSingleRegionSnsHost(host, AmazonAwsChinaSuffix);

        static bool IsSingleRegionSnsHost(string host, string suffix)
        {
            if (!host.StartsWith("sns.", StringComparison.Ordinal) ||
                !host.EndsWith(suffix, StringComparison.Ordinal))
            {
                return false;
            }

            var region = host["sns.".Length..^suffix.Length];
            return region.Length > 0 &&
                region.IndexOf('.', StringComparison.Ordinal) < 0 &&
                region.Count(static character => character == '-') >= 2 &&
                char.IsAsciiDigit(region[^1]) &&
                region.All(static character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-');
        }
    }

    private static bool TryResolveHashAlgorithm(
        string? signatureVersion,
        bool requireVersion2,
        out HashAlgorithmName hashAlgorithm,
        out string algorithmName,
        out string outcome)
    {
        hashAlgorithm = default;
        algorithmName = string.Empty;
        outcome = "signature-version-unsupported";
        if (requireVersion2 && !string.Equals(signatureVersion, "2", StringComparison.Ordinal))
        {
            outcome = "signature-version-not-allowed";
            return false;
        }

        if (string.Equals(signatureVersion, "2", StringComparison.Ordinal))
        {
            hashAlgorithm = HashAlgorithmName.SHA256;
            algorithmName = "rsa-sha256";
            outcome = "verified";
            return true;
        }

        if (string.Equals(signatureVersion, "1", StringComparison.Ordinal))
        {
            hashAlgorithm = HashAlgorithmName.SHA1;
            algorithmName = "rsa-sha1";
            outcome = "verified";
            return true;
        }

        return false;
    }

    private static bool IsSupportedMessageType(string value) =>
        string.Equals(value, "Notification", StringComparison.Ordinal) ||
        string.Equals(value, "SubscriptionConfirmation", StringComparison.Ordinal) ||
        string.Equals(value, "UnsubscribeConfirmation", StringComparison.Ordinal);

    private static bool TryReadString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!TryReadRawString(element, propertyName, out var raw) ||
            string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        value = raw.Trim();
        return true;
    }

    private static bool TryReadRawString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return true;
    }

    private static string CreateSha256Fingerprint(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static X509Certificate2 LoadCertificateFromPem(string pem)
    {
        const string BeginMarker = "-----BEGIN CERTIFICATE-----";
        const string EndMarker = "-----END CERTIFICATE-----";

        var begin = pem.IndexOf(BeginMarker, StringComparison.Ordinal);
        var end = pem.IndexOf(EndMarker, StringComparison.Ordinal);
        if (begin < 0 || end <= begin)
        {
            throw new CryptographicException("The PEM certificate markers were not found.");
        }

        var base64 = pem[(begin + BeginMarker.Length)..end];
        var certificateBytes = Convert.FromBase64String(RemoveWhitespace(base64));
        return X509CertificateLoader.LoadCertificate(certificateBytes);
    }

    private static string RemoveWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (!char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private sealed record AmazonSesSnsSigningCertificateLoadResult(
        bool Loaded,
        X509Certificate2? Certificate,
        string Outcome,
        string Detail,
        int StatusCode)
    {
        public static AmazonSesSnsSigningCertificateLoadResult FromCertificate(X509Certificate2 certificate) =>
            new(true, certificate, "loaded", string.Empty, StatusCodes.Status200OK);

        public static AmazonSesSnsSigningCertificateLoadResult Fail(string outcome, string detail, int statusCode) =>
            new(false, null, outcome, detail, statusCode);
    }

    private sealed record AmazonSesSnsSigningCertificateValidationResult(
        bool Valid,
        string Outcome,
        string Detail)
    {
        public static AmazonSesSnsSigningCertificateValidationResult Success() =>
            new(true, "valid", string.Empty);

        public static AmazonSesSnsSigningCertificateValidationResult Fail(string outcome, string detail) =>
            new(false, outcome, detail);
    }
}

internal sealed class AmazonSesSnsSigningCertificateDownloader
{
    private const int MaxCertificateBytes = 64 * 1024;
    private static readonly HttpClient Client = new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    });
    private readonly HttpClient client = Client;

    public async ValueTask<X509Certificate2> DownloadAsync(Uri signingCertificateUrl, CancellationToken cancellationToken)
    {
        using var response = await client
            .GetAsync(signingCertificateUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength > MaxCertificateBytes)
        {
            throw new CryptographicException("The SNS signing certificate response was too large.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        while (true)
        {
            var bytesRead = await stream.ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            if (buffer.Length + bytesRead > MaxCertificateBytes)
            {
                throw new CryptographicException("The SNS signing certificate response was too large.");
            }

            buffer.Write(chunk, 0, bytesRead);
        }

        return X509CertificateLoader.LoadCertificate(buffer.ToArray());
    }
}

internal sealed record AmazonSesSnsSignatureVerificationResult(
    bool Configured,
    bool Verified,
    string Outcome,
    string? Detail,
    int? FailureStatusCode,
    string? TopicArn,
    string? MessageType,
    string? MessageId,
    string? Timestamp,
    string? SignatureVersion,
    string? Algorithm,
    string? SigningCertificateUrlHost,
    string? SignatureFingerprint,
    string? CertificateThumbprint)
{
    public static AmazonSesSnsSignatureVerificationResult NotConfigured() =>
        new(false, false, "not-configured", null, null, null, null, null, null, null, null, null, null, null);

    public static AmazonSesSnsSignatureVerificationResult CreateVerified(
        string topicArn,
        string messageType,
        string? messageId,
        string? timestamp,
        string signatureVersion,
        string algorithm,
        string signingCertificateUrlHost,
        string signatureFingerprint,
        string? certificateThumbprint) =>
        new(
            true,
            true,
            "verified",
            null,
            null,
            topicArn,
            messageType,
            messageId,
            timestamp,
            signatureVersion,
            algorithm,
            signingCertificateUrlHost,
            signatureFingerprint,
            certificateThumbprint);

    public static AmazonSesSnsSignatureVerificationResult Fail(
        string outcome,
        string detail,
        int statusCode,
        string? topicArn = null,
        string? messageType = null,
        string? messageId = null,
        string? signatureVersion = null,
        string? algorithm = null,
        string? signingCertificateUrlHost = null,
        string? certificateThumbprint = null) =>
        new(
            true,
            false,
            outcome,
            detail,
            statusCode,
            topicArn,
            messageType,
            messageId,
            null,
            signatureVersion,
            algorithm,
            signingCertificateUrlHost,
            null,
            certificateThumbprint);
}
