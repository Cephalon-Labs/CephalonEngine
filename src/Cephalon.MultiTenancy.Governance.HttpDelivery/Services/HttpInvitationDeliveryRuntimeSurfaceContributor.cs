using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.HttpDelivery.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.HttpDelivery.Services;

internal sealed class HttpInvitationDeliveryRuntimeSurfaceContributor(HttpInvitationDeliveryOptions options) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var endpoint = options.TryGetEndpoint();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "provider-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance.HttpDelivery",
            ["provider"] = "http-webhook",
            ["transport"] = "http",
            ["runtimeState"] = endpoint is null ? "invalid" : "configured",
            ["senderId"] = Normalize(options.SenderId, "http-webhook"),
            ["senderOwnership"] = "provider-managed",
            ["supportedChannels"] = Join(options.SupportedChannels, "all"),
            ["endpointScheme"] = endpoint?.Scheme ?? "invalid",
            ["endpointHost"] = endpoint?.Host ?? "invalid",
            ["endpointPort"] = endpoint?.IsDefaultPort == false
                ? endpoint.Port.ToString(CultureInfo.InvariantCulture)
                : "default",
            ["endpointPathConfigured"] = (endpoint is not null && !string.IsNullOrWhiteSpace(endpoint.AbsolutePath) && endpoint.AbsolutePath != "/")
                .ToString()
                .ToLowerInvariant(),
            ["endpointQueryConfigured"] = (endpoint is not null && !string.IsNullOrWhiteSpace(endpoint.Query))
                .ToString()
                .ToLowerInvariant(),
            ["httpMethod"] = options.GetHttpMethod().Method,
            ["timeoutSeconds"] = ((int)options.GetTimeout().TotalSeconds).ToString(CultureInfo.InvariantCulture),
            ["maxAttempts"] = options.GetMaxAttempts().ToString(CultureInfo.InvariantCulture),
            ["retryDelayMilliseconds"] = ((int)options.GetRetryDelay().TotalMilliseconds).ToString(CultureInfo.InvariantCulture),
            ["retryStatusCodes"] = Join(options.RetryStatusCodes, "none"),
            ["retryTransportFailures"] = options.RetryTransportFailures.ToString().ToLowerInvariant(),
            ["expectedStatusCodes"] = options.ExpectedStatusCodes.Count == 0 ? "2xx" : Join(options.ExpectedStatusCodes, "2xx"),
            ["headerNames"] = JoinKeys(options.Headers, "none"),
            ["idempotencyHeaderEnabled"] = options.EnableIdempotencyHeader.ToString().ToLowerInvariant(),
            ["idempotencyHeaderName"] = Normalize(options.IdempotencyHeaderName, "none"),
            ["idempotencyMetadataKey"] = Normalize(options.IdempotencyMetadataKey, "none"),
            ["signedWebhookEnabled"] = options.IsSigningEnabled.ToString().ToLowerInvariant(),
            ["signingKeyIdConfigured"] = (!string.IsNullOrWhiteSpace(options.SigningKeyId)).ToString().ToLowerInvariant(),
            ["signatureHeaderName"] = Normalize(options.SignatureHeaderName, "none"),
            ["signatureTimestampHeaderName"] = Normalize(options.SignatureTimestampHeaderName, "none"),
            ["signatureKeyIdHeaderName"] = Normalize(options.SignatureKeyIdHeaderName, "none"),
            ["providerMessageIdHeaderName"] = Normalize(options.ProviderMessageIdHeaderName, "none"),
            ["includeInvitationMetadata"] = options.IncludeInvitationMetadata.ToString().ToLowerInvariant(),
            ["includeRequestMetadata"] = options.IncludeRequestMetadata.ToString().ToLowerInvariant(),
            ["includeResponseBodyInMetadata"] = options.IncludeResponseBodyInMetadata.ToString().ToLowerInvariant(),
            ["responseBodyMetadataLimit"] = options.GetResponseBodyMetadataLimit().ToString(CultureInfo.InvariantCulture),
            ["secretProjection"] = "redacted"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-invitation-delivery-http",
            displayName: "HTTP Tenant Invitation Delivery",
            description: "Projects the configured HTTP webhook sender for tenant invitation delivery without exposing webhook secrets or header values.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "http-webhook-sender",
                    displayName: "HTTP Webhook Sender",
                    description: "Summarizes the configured HTTP webhook delivery endpoint, retry policy, idempotency posture, signing posture, and accepted response contract.",
                    metadata: metadata)
            ]);
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string Join(IEnumerable<string> values, string emptyValue)
    {
        var normalized = values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized.Length == 0 ? emptyValue : string.Join(",", normalized);
    }

    private static string Join(IEnumerable<int> values, string emptyValue)
    {
        var normalized = values
            .Distinct()
            .Order()
            .Select(static value => value.ToString(CultureInfo.InvariantCulture))
            .ToArray();

        return normalized.Length == 0 ? emptyValue : string.Join(",", normalized);
    }

    private static string JoinKeys(IReadOnlyDictionary<string, string> values, string emptyValue)
    {
        var keys = values.Keys
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Select(static key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return keys.Length == 0 ? emptyValue : string.Join(",", keys);
    }
}
