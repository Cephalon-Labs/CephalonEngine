using Cephalon.MultiTenancy.Governance.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

/// <summary>
/// Maps ASP.NET Core endpoints for normalized tenant-invitation delivery status callbacks.
/// </summary>
public static class TenantInvitationDeliveryStatusCallbackEndpointRouteBuilderExtensions
{
    private const string DefaultCallbackSource = "aspnetcore-delivery-status-callback";
    private const int MaxCallbackBodyBytes = 64 * 1024;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Maps the optional tenant-invitation delivery status callback endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder for fluent routing composition.</returns>
    /// <remarks>
    /// The endpoint is opt-in, executes the host-agnostic <see cref="ITenantInvitationDeliveryStatusReconciler" />,
    /// and performs a fail-closed authorization check by default. It accepts normalized status observations only; provider
    /// webhook payload translation, provider signature verification, provider polling, and provider-specific status
    /// vocabularies remain application-managed or future provider-pack responsibilities.
    /// </remarks>
    public static IEndpointRouteBuilder MapCephalonTenantInvitationDeliveryStatusCallbacks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetService<MultiTenancyGovernanceAspNetCoreOptions>() ??
            new MultiTenancyGovernanceAspNetCoreOptions();
        if (!options.EnableTenantInvitationDeliveryStatusCallbackEndpoint)
        {
            return endpoints;
        }

        var routePattern = Normalize(options.TenantInvitationDeliveryStatusCallbackRoutePattern) ??
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusCallbackRoutePattern;
        var builder = endpoints
            .MapPost(
                routePattern,
                (
                    HttpContext context,
                    ITenantInvitationDeliveryStatusReconciler reconciler,
                    TenantInvitationDeliveryStatusCallbackReplayGuard replayGuard,
                    CancellationToken cancellationToken) =>
                    ReconcileCallbackAsync(context, reconciler, replayGuard, options, routePattern, cancellationToken))
            .WithName("CephalonTenantInvitationDeliveryStatusCallback")
            .Accepts<TenantInvitationDeliveryStatusCallbackRequest>("application/json")
            .Produces<TenantInvitationDeliveryStatusReconciliationResult>(StatusCodes.Status200OK)
            .Produces<TenantInvitationDeliveryStatusReconciliationResult>(StatusCodes.Status400BadRequest)
            .Produces<TenantInvitationDeliveryStatusReconciliationResult>(StatusCodes.Status404NotFound)
            .Produces<TenantInvitationDeliveryStatusReconciliationResult>(StatusCodes.Status409Conflict)
            .Produces<TenantInvitationDeliveryStatusReconciliationResult>(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        if (options.ExcludeTenantInvitationDeliveryStatusCallbackEndpointFromDescription)
        {
            builder.ExcludeFromDescription();
        }

        ApplyAuthorizationMetadata(endpoints, builder, options);

        endpoints.ServiceProvider
            .GetService<TenantInvitationDeliveryStatusCallbackEndpointRuntimeCatalog>()
            ?.RecordCallbackEndpointMapped(
                routePattern,
                options.RequireTenantInvitationDeliveryStatusCallbackAuthorization,
                Normalize(options.TenantInvitationDeliveryStatusCallbackAuthorizationPolicy),
                options.ExcludeTenantInvitationDeliveryStatusCallbackEndpointFromDescription,
                options.RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch,
                IsCallbackSignatureVerificationConfigured(options),
                GetHeaderNameOrDefault(
                    options.TenantInvitationDeliveryStatusCallbackSignatureHeaderName,
                    MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusCallbackSignatureHeaderName),
                GetHeaderNameOrDefault(
                    options.TenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName,
                    MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName),
                GetHeaderNameOrDefault(
                    options.TenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName,
                    MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName),
                !string.IsNullOrWhiteSpace(options.TenantInvitationDeliveryStatusCallbackSigningKeyId),
                GetSignatureToleranceSeconds(options),
                IsCallbackReplayProtectionConfigured(options),
                GetReplayRetentionSeconds(options),
                GetReplayCacheLimit(options));

        return endpoints;
    }

    private static async Task<IResult> ReconcileCallbackAsync(
        HttpContext context,
        ITenantInvitationDeliveryStatusReconciler reconciler,
        TenantInvitationDeliveryStatusCallbackReplayGuard replayGuard,
        MultiTenancyGovernanceAspNetCoreOptions options,
        string routePattern,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await AuthorizeAsync(context, options).ConfigureAwait(false);
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var requestBody = await ReadRequestBodyAsync(context, cancellationToken).ConfigureAwait(false);
        if (requestBody.Failure is not null)
        {
            return requestBody.Failure;
        }

        var signatureVerification = VerifyCallbackSignature(context, options, requestBody.Body);
        if (signatureVerification.Failure is not null)
        {
            return signatureVerification.Failure;
        }

        TenantInvitationDeliveryStatusCallbackRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<TenantInvitationDeliveryStatusCallbackRequest>(
                requestBody.Body,
                SerializerOptions);
        }
        catch (JsonException)
        {
            return Results.Problem(
                title: "Tenant invitation delivery status callback request is invalid.",
                detail: "Send a valid JSON TenantInvitationDeliveryStatusCallbackRequest body.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (request is null)
        {
            return Results.Problem(
                title: "Tenant invitation delivery status callback request is required.",
                detail: "Send a JSON TenantInvitationDeliveryStatusCallbackRequest body.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var validationResult = Validate(request);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var replayProtection = RecordCallbackReplayProtection(options, replayGuard, signatureVerification);
        if (replayProtection.Failure is not null)
        {
            return replayProtection.Failure;
        }

        var metadata = CopyMetadata(request.Metadata);
        metadata["aspNetCoreDeliveryStatusCallback"] = "true";
        metadata["aspNetCoreDeliveryStatusCallbackRoute"] = routePattern;
        metadata["deliveryStatusCallbackIngressOwnership"] = "cephalon-managed";
        metadata["deliveryStatusCallbackSignatureVerification"] = signatureVerification.Configured ? "verified" : "not-configured";
        metadata["deliveryStatusCallbackSignatureVerificationOwnership"] = signatureVerification.Configured ? "cephalon-managed" : "not-configured";
        metadata["deliveryStatusCallbackReplayProtection"] = replayProtection.Configured ? replayProtection.Outcome : "not-configured";
        metadata["deliveryStatusCallbackReplayProtectionOwnership"] = replayProtection.Configured ? "cephalon-managed" : "not-configured";

        if (signatureVerification.Configured)
        {
            metadata["deliveryStatusCallbackSignatureTimestamp"] =
                signatureVerification.Timestamp!.Value.ToString(CultureInfo.InvariantCulture);
            metadata["deliveryStatusCallbackSignatureAgeSeconds"] =
                signatureVerification.AgeSeconds!.Value.ToString(CultureInfo.InvariantCulture);

            if (!string.IsNullOrWhiteSpace(signatureVerification.KeyId))
            {
                metadata["deliveryStatusCallbackSignatureKeyId"] = signatureVerification.KeyId!;
            }
        }

        if (replayProtection.Configured)
        {
            metadata["deliveryStatusCallbackReplayPolicy"] = "signed-callback";
            metadata["deliveryStatusCallbackReplayKey"] = "signature-fingerprint";
            metadata["deliveryStatusCallbackReplayScope"] = "process-local";
            metadata["deliveryStatusCallbackReplayDurability"] = "none";
            metadata["deliveryStatusCallbackReplayRetentionSeconds"] =
                GetReplayRetentionSeconds(options).ToString(CultureInfo.InvariantCulture);
            metadata["deliveryStatusCallbackReplayCacheLimit"] =
                GetReplayCacheLimit(options).ToString(CultureInfo.InvariantCulture);

            if (!string.IsNullOrWhiteSpace(replayProtection.ReplayFingerprint))
            {
                metadata["deliveryStatusCallbackReplayFingerprint"] = replayProtection.ReplayFingerprint!;
            }
        }

        var reconciliationRequest = new TenantInvitationDeliveryStatusReconciliationRequest(
            tenantId: request.TenantId!,
            invitationId: request.InvitationId!,
            status: request.Status!,
            providerMessageId: request.ProviderMessageId,
            senderId: request.SenderId,
            channel: request.Channel,
            reason: request.Reason,
            observedAtUtc: request.ObservedAtUtc,
            source: Normalize(request.Source) ?? DefaultCallbackSource,
            actor: request.Actor,
            correlationId: request.CorrelationId,
            recordStatus: request.RecordStatus,
            requireProviderMessageMatch: options.RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch ||
                request.RequireProviderMessageMatch,
            metadata: metadata);

        TenantInvitationDeliveryStatusReconciliationResult result;
        try
        {
            result = await reconciler.ReconcileAsync(reconciliationRequest, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            ForgetCallbackReplayProtection(replayGuard, replayProtection);
            throw;
        }

        if (!result.Reconciled)
        {
            ForgetCallbackReplayProtection(replayGuard, replayProtection);
        }

        return Results.Json(result, statusCode: ResolveStatusCode(result));
    }

    private static async Task<CallbackRequestBodyReadResult> ReadRequestBodyAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (context.Request.ContentLength > MaxCallbackBodyBytes)
        {
            return CallbackRequestBodyReadResult.Fail(Results.Problem(
                title: "Tenant invitation delivery status callback request is too large.",
                detail: $"The callback request body must be no larger than {MaxCallbackBodyBytes} bytes.",
                statusCode: StatusCodes.Status413PayloadTooLarge));
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        while (true)
        {
            var bytesRead = await context.Request.Body
                .ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken)
                .ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            if (buffer.Length + bytesRead > MaxCallbackBodyBytes)
            {
                return CallbackRequestBodyReadResult.Fail(Results.Problem(
                    title: "Tenant invitation delivery status callback request is too large.",
                    detail: $"The callback request body must be no larger than {MaxCallbackBodyBytes} bytes.",
                    statusCode: StatusCodes.Status413PayloadTooLarge));
            }

            buffer.Write(chunk, 0, bytesRead);
        }

        return CallbackRequestBodyReadResult.Success(buffer.ToArray());
    }

    private static CallbackSignatureVerificationResult VerifyCallbackSignature(
        HttpContext context,
        MultiTenancyGovernanceAspNetCoreOptions options,
        byte[] requestBody)
    {
        var secret = Normalize(options.TenantInvitationDeliveryStatusCallbackSigningSecret);
        if (secret is null)
        {
            return CallbackSignatureVerificationResult.NotConfigured();
        }

        var signatureHeaderName = GetHeaderNameOrDefault(
            options.TenantInvitationDeliveryStatusCallbackSignatureHeaderName,
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusCallbackSignatureHeaderName);
        if (!TryGetSingleHeader(context, signatureHeaderName, out var signature))
        {
            return CallbackSignatureVerificationResult.Fail(SignatureProblem(
                "Tenant invitation delivery status callback signature is required.",
                $"Set the {signatureHeaderName} header to a Cephalon callback signature."));
        }

        var timestampHeaderName = GetHeaderNameOrDefault(
            options.TenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName,
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName);
        if (!TryGetSingleHeader(context, timestampHeaderName, out var timestamp))
        {
            return CallbackSignatureVerificationResult.Fail(SignatureProblem(
                "Tenant invitation delivery status callback signature timestamp is required.",
                $"Set the {timestampHeaderName} header to the Unix timestamp included in the callback signature."));
        }

        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var timestampSeconds))
        {
            return CallbackSignatureVerificationResult.Fail(SignatureProblem(
                "Tenant invitation delivery status callback signature timestamp is invalid.",
                "The callback signature timestamp must be a Unix timestamp in seconds."));
        }

        DateTimeOffset signedAtUtc;
        try
        {
            signedAtUtc = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return CallbackSignatureVerificationResult.Fail(SignatureProblem(
                "Tenant invitation delivery status callback signature timestamp is invalid.",
                "The callback signature timestamp is outside the supported Unix timestamp range."));
        }

        var ageSeconds = (int)Math.Abs(Math.Round((DateTimeOffset.UtcNow - signedAtUtc).TotalSeconds));
        var toleranceSeconds = GetSignatureToleranceSeconds(options);
        if (ageSeconds > toleranceSeconds)
        {
            return CallbackSignatureVerificationResult.Fail(SignatureProblem(
                "Tenant invitation delivery status callback signature timestamp is outside the allowed tolerance.",
                $"The callback signature timestamp must be within {toleranceSeconds} seconds of the current UTC time."));
        }

        var expectedKeyId = Normalize(options.TenantInvitationDeliveryStatusCallbackSigningKeyId);
        var keyIdHeaderName = GetHeaderNameOrDefault(
            options.TenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName,
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName);
        TryGetSingleHeader(context, keyIdHeaderName, out var suppliedKeyId);
        if (expectedKeyId is not null &&
            !string.Equals(expectedKeyId, suppliedKeyId, StringComparison.Ordinal))
        {
            return CallbackSignatureVerificationResult.Fail(SignatureProblem(
                "Tenant invitation delivery status callback signing key id is invalid.",
                $"Set the {keyIdHeaderName} header to the configured callback signing key id."));
        }

        if (!SignatureMatches(secret, timestamp!, requestBody, signature!))
        {
            return CallbackSignatureVerificationResult.Fail(SignatureProblem(
                "Tenant invitation delivery status callback signature is invalid.",
                "The callback request body does not match the supplied Cephalon callback signature."));
        }

        return CallbackSignatureVerificationResult.Verified(
            timestampSeconds,
            ageSeconds,
            suppliedKeyId,
            CreateSha256Fingerprint(signature!));
    }

    private static CallbackReplayProtectionResult RecordCallbackReplayProtection(
        MultiTenancyGovernanceAspNetCoreOptions options,
        TenantInvitationDeliveryStatusCallbackReplayGuard replayGuard,
        CallbackSignatureVerificationResult signatureVerification)
    {
        if (!IsCallbackReplayProtectionConfigured(options) ||
            !signatureVerification.Configured ||
            string.IsNullOrWhiteSpace(signatureVerification.ReplayFingerprint))
        {
            return CallbackReplayProtectionResult.NotConfigured();
        }

        var retentionSeconds = GetReplayRetentionSeconds(options);
        var decision = replayGuard.TryRecord(
            signatureVerification.ReplayFingerprint!,
            DateTimeOffset.UtcNow,
            TimeSpan.FromSeconds(retentionSeconds),
            GetReplayCacheLimit(options));
        if (decision.Accepted)
        {
            return CallbackReplayProtectionResult.Recorded(signatureVerification.ReplayFingerprint);
        }

        return CallbackReplayProtectionResult.Fail(
            Results.Problem(
                title: "Tenant invitation delivery status callback replay was rejected.",
                detail: "The signed normalized callback request has already been accepted inside the configured process-local replay window.",
                statusCode: StatusCodes.Status409Conflict),
            decision.Outcome,
            signatureVerification.ReplayFingerprint);
    }

    private static void ForgetCallbackReplayProtection(
        TenantInvitationDeliveryStatusCallbackReplayGuard replayGuard,
        CallbackReplayProtectionResult replayProtection)
    {
        if (replayProtection.Configured &&
            !string.IsNullOrWhiteSpace(replayProtection.ReplayFingerprint))
        {
            replayGuard.Forget(replayProtection.ReplayFingerprint!);
        }
    }

    private static bool SignatureMatches(
        string secret,
        string timestamp,
        byte[] requestBody,
        string signature)
    {
        if (!TryParseV1Signature(signature, out var suppliedSignature))
        {
            return false;
        }

        var timestampBytes = Encoding.UTF8.GetBytes(timestamp);
        var signedPayload = new byte[timestampBytes.Length + 1 + requestBody.Length];
        Buffer.BlockCopy(timestampBytes, 0, signedPayload, 0, timestampBytes.Length);
        signedPayload[timestampBytes.Length] = (byte)'.';
        Buffer.BlockCopy(requestBody, 0, signedPayload, timestampBytes.Length + 1, requestBody.Length);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expectedSignature = hmac.ComputeHash(signedPayload);
        return suppliedSignature.Length == expectedSignature.Length &&
            CryptographicOperations.FixedTimeEquals(suppliedSignature, expectedSignature);
    }

    private static bool TryParseV1Signature(string signature, out byte[] signatureBytes)
    {
        signatureBytes = [];
        var normalizedSignature = Normalize(signature);
        if (normalizedSignature is null ||
            !normalizedSignature.StartsWith("v1=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            signatureBytes = Convert.FromHexString(normalizedSignature.AsSpan(3));
            return signatureBytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string CreateSha256Fingerprint(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static IResult SignatureProblem(string title, string detail)
    {
        return Results.Problem(title: title, detail: detail, statusCode: StatusCodes.Status401Unauthorized);
    }

    private static IResult? Validate(TenantInvitationDeliveryStatusCallbackRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            return Results.Problem(
                title: "Tenant id is required.",
                detail: "Set TenantId to the tenant identifier that owns the invitation.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(request.InvitationId))
        {
            return Results.Problem(
                title: "Invitation id is required.",
                detail: "Set InvitationId to the invitation identifier to reconcile.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return Results.Problem(
                title: "Delivery status is required.",
                detail: "Set Status to the provider or receiver delivery status.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }

    private static async ValueTask<IResult?> AuthorizeAsync(
        HttpContext context,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        if (!options.RequireTenantInvitationDeliveryStatusCallbackAuthorization)
        {
            return null;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Problem(
                title: "Tenant invitation delivery status callback authorization is required.",
                detail: "The Cephalon tenant-invitation delivery status callback endpoint is fail-closed by default.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var authorizationPolicy = Normalize(options.TenantInvitationDeliveryStatusCallbackAuthorizationPolicy);
        if (authorizationPolicy is null)
        {
            return null;
        }

        var authorizationService = context.RequestServices.GetService<IAuthorizationService>();
        if (authorizationService is null)
        {
            return Results.Problem(
                title: "Tenant invitation delivery status callback authorization cannot be evaluated.",
                detail: "Register ASP.NET Core authorization services or disable endpoint authorization deliberately.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var authorization = await authorizationService
            .AuthorizeAsync(context.User, context, authorizationPolicy)
            .ConfigureAwait(false);
        if (authorization.Succeeded)
        {
            return null;
        }

        return Results.Problem(
            title: "Tenant invitation delivery status callback authorization failed.",
            detail: "The authenticated principal is not authorized to reconcile tenant-invitation delivery status callbacks.",
            statusCode: StatusCodes.Status403Forbidden);
    }

    private static void ApplyAuthorizationMetadata(
        IEndpointRouteBuilder endpoints,
        IEndpointConventionBuilder builder,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        if (!options.RequireTenantInvitationDeliveryStatusCallbackAuthorization ||
            endpoints.ServiceProvider.GetService<IAuthorizationService>() is null ||
            endpoints.ServiceProvider.GetService<IAuthenticationSchemeProvider>() is null)
        {
            return;
        }

        var authorizationPolicy = Normalize(options.TenantInvitationDeliveryStatusCallbackAuthorizationPolicy);
        if (authorizationPolicy is null)
        {
            builder.RequireAuthorization();
        }
        else
        {
            builder.RequireAuthorization(authorizationPolicy);
        }
    }

    private static int ResolveStatusCode(TenantInvitationDeliveryStatusReconciliationResult result)
    {
        if (result.Reconciled)
        {
            return StatusCodes.Status200OK;
        }

        return result.Outcome switch
        {
            TenantInvitationDeliveryStatusReconciliationOutcomes.Disabled => StatusCodes.Status409Conflict,
            TenantInvitationDeliveryStatusReconciliationOutcomes.InvitationNotFound => StatusCodes.Status404NotFound,
            TenantInvitationDeliveryStatusReconciliationOutcomes.ProviderMessageMissing => StatusCodes.Status409Conflict,
            TenantInvitationDeliveryStatusReconciliationOutcomes.ProviderMessageMismatch => StatusCodes.Status409Conflict,
            TenantInvitationDeliveryStatusReconciliationOutcomes.StoreFailed => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private static Dictionary<string, string> CopyMetadata(IDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return metadata
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsCallbackSignatureVerificationConfigured(MultiTenancyGovernanceAspNetCoreOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.TenantInvitationDeliveryStatusCallbackSigningSecret);
    }

    private static bool IsCallbackReplayProtectionConfigured(MultiTenancyGovernanceAspNetCoreOptions options)
    {
        return options.EnableTenantInvitationDeliveryStatusCallbackReplayProtection &&
            IsCallbackSignatureVerificationConfigured(options);
    }

    private static int GetSignatureToleranceSeconds(MultiTenancyGovernanceAspNetCoreOptions options)
    {
        return Math.Clamp(options.TenantInvitationDeliveryStatusCallbackSignatureToleranceSeconds, 1, 86_400);
    }

    private static int GetReplayRetentionSeconds(MultiTenancyGovernanceAspNetCoreOptions options)
    {
        return Math.Clamp(options.TenantInvitationDeliveryStatusCallbackReplayRetentionSeconds, 1, 86_400);
    }

    private static int GetReplayCacheLimit(MultiTenancyGovernanceAspNetCoreOptions options)
    {
        return Math.Clamp(options.TenantInvitationDeliveryStatusCallbackReplayCacheLimit, 1, 1_000_000);
    }

    private static string GetHeaderNameOrDefault(string? headerName, string defaultHeaderName)
    {
        return Normalize(headerName) ?? defaultHeaderName;
    }

    private static bool TryGetSingleHeader(HttpContext context, string headerName, out string? value)
    {
        value = null;
        if (!context.Request.Headers.TryGetValue(headerName, out var values) ||
            values.Count != 1 ||
            string.IsNullOrWhiteSpace(values[0]))
        {
            return false;
        }

        value = values[0]!.Trim();
        return true;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private sealed record CallbackRequestBodyReadResult(byte[] Body, IResult? Failure)
    {
        public static CallbackRequestBodyReadResult Success(byte[] body) => new(body, null);

        public static CallbackRequestBodyReadResult Fail(IResult failure) => new([], failure);
    }

    private sealed record CallbackSignatureVerificationResult(
        bool Configured,
        long? Timestamp,
        int? AgeSeconds,
        string? KeyId,
        string? ReplayFingerprint,
        IResult? Failure)
    {
        public static CallbackSignatureVerificationResult NotConfigured() =>
            new(false, null, null, null, null, null);

        public static CallbackSignatureVerificationResult Verified(
            long timestamp,
            int ageSeconds,
            string? keyId,
            string replayFingerprint) =>
            new(true, timestamp, ageSeconds, keyId, replayFingerprint, null);

        public static CallbackSignatureVerificationResult Fail(IResult failure) =>
            new(true, null, null, null, null, failure);
    }

    private sealed record CallbackReplayProtectionResult(
        bool Configured,
        string Outcome,
        string? ReplayFingerprint,
        IResult? Failure)
    {
        public static CallbackReplayProtectionResult NotConfigured() =>
            new(false, "not-configured", null, null);

        public static CallbackReplayProtectionResult Recorded(string replayFingerprint) =>
            new(true, "recorded", replayFingerprint, null);

        public static CallbackReplayProtectionResult Fail(
            IResult failure,
            string outcome,
            string replayFingerprint) =>
            new(true, outcome, replayFingerprint, failure);
    }
}
