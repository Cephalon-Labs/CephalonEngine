using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Governance.HttpDelivery.Configuration;

/// <summary>
/// Configures HTTP webhook delivery for tenant invitations dispatched by the governance companion pack.
/// </summary>
/// <remarks>
/// The HTTP sender queues or sends a delivery request to a configured endpoint. It does not guarantee final recipient
/// delivery and does not encode product-specific email, SMS, chat, or identity-provider semantics.
/// </remarks>
public sealed class HttpInvitationDeliveryOptions
{
    private static readonly int[] DefaultRetryStatusCodes = [408, 429, 500, 502, 503, 504];

    /// <summary>
    /// Creates HTTP invitation delivery options with the default sender identifier and timeout.
    /// </summary>
    public HttpInvitationDeliveryOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the HTTP invitation sender should be registered.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the sender identifier used by `TenantInvitationDeliveryRequest.SenderId`.
    /// </summary>
    public string SenderId { get; set; } = "http-webhook";

    /// <summary>
    /// Gets or sets the absolute HTTP endpoint that receives invitation delivery payloads.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method used for delivery requests.
    /// </summary>
    public string Method { get; set; } = "POST";

    /// <summary>
    /// Gets or sets the maximum time allowed for the HTTP delivery request.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the total number of HTTP dispatch attempts for transient delivery failures.
    /// </summary>
    /// <remarks>
    /// The value is clamped to the supported range of 1 through 10. The default preserves single-attempt behavior.
    /// </remarks>
    public int MaxAttempts { get; set; } = 1;

    /// <summary>
    /// Gets or sets the fixed delay, in milliseconds, between retry attempts.
    /// </summary>
    /// <remarks>
    /// The value is clamped to the supported range of 0 through 60000 milliseconds.
    /// </remarks>
    public int RetryDelayMilliseconds { get; set; } = 250;

    /// <summary>
    /// Gets or sets response status codes that should be retried when the dispatch has attempts remaining.
    /// </summary>
    /// <remarks>
    /// The default covers common transient HTTP responses: 408, 429, 500, 502, 503, and 504.
    /// </remarks>
    public IReadOnlyList<int> RetryStatusCodes { get; set; } = DefaultRetryStatusCodes;

    /// <summary>
    /// Gets or sets a value indicating whether transient transport failures should be retried when attempts remain.
    /// </summary>
    public bool RetryTransportFailures { get; set; } = true;

    /// <summary>
    /// Gets or sets explicit response status codes that indicate the webhook accepted the dispatch.
    /// </summary>
    /// <remarks>
    /// When empty, any successful 2xx response is accepted.
    /// </remarks>
    public IReadOnlyList<int> ExpectedStatusCodes { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Gets or sets delivery channels accepted by this sender.
    /// </summary>
    /// <remarks>
    /// When empty, the sender accepts every requested channel.
    /// </remarks>
    public IReadOnlyList<string> SupportedChannels { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional HTTP headers added to every delivery request.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the shared secret used to sign webhook payloads with HMAC-SHA256.
    /// </summary>
    /// <remarks>
    /// When empty, the sender does not add Cephalon webhook signature headers.
    /// </remarks>
    public string? SigningSecret { get; set; }

    /// <summary>
    /// Gets or sets an optional key identifier sent with signed webhook requests.
    /// </summary>
    public string? SigningKeyId { get; set; }

    /// <summary>
    /// Gets or sets the request header that carries the webhook signature.
    /// </summary>
    public string SignatureHeaderName { get; set; } = "X-Cephalon-Webhook-Signature";

    /// <summary>
    /// Gets or sets the request header that carries the Unix timestamp included in the webhook signature.
    /// </summary>
    public string SignatureTimestampHeaderName { get; set; } = "X-Cephalon-Webhook-Signature-Timestamp";

    /// <summary>
    /// Gets or sets the request header that carries the optional signing key identifier.
    /// </summary>
    public string SignatureKeyIdHeaderName { get; set; } = "X-Cephalon-Webhook-Key-Id";

    /// <summary>
    /// Gets or sets the response header that contains the provider message identifier.
    /// </summary>
    public string? ProviderMessageIdHeaderName { get; set; } = "X-Cephalon-Provider-Message-Id";

    /// <summary>
    /// Gets or sets a value indicating whether invitation metadata should be included in the webhook payload.
    /// </summary>
    public bool IncludeInvitationMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether dispatch request metadata should be included in the webhook payload.
    /// </summary>
    public bool IncludeRequestMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a bounded response body excerpt should be copied into sender metadata.
    /// </summary>
    public bool IncludeResponseBodyInMetadata { get; set; }

    /// <summary>
    /// Gets or sets the maximum response body characters copied into sender metadata when enabled.
    /// </summary>
    public int ResponseBodyMetadataLimit { get; set; } = 1024;

    /// <summary>
    /// Binds HTTP invitation delivery options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound HTTP invitation delivery options.</returns>
    public static HttpInvitationDeliveryOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("MultiTenancy")
            .GetSection("Governance")
            .GetSection("HttpInvitationDelivery");

        var options = new HttpInvitationDeliveryOptions
        {
            Enabled = GetBoolean(section["Enabled"], defaultValue: true),
            SenderId = section["SenderId"]?.Trim() ?? "http-webhook",
            Endpoint = section["Endpoint"]?.Trim(),
            Method = section["Method"]?.Trim() ?? "POST",
            TimeoutSeconds = GetInt32(section["TimeoutSeconds"], defaultValue: 10),
            MaxAttempts = GetInt32(section["MaxAttempts"], defaultValue: 1),
            RetryDelayMilliseconds = GetInt32(section["RetryDelayMilliseconds"], defaultValue: 250),
            RetryStatusCodes = ParseInt32List(section.GetSection("RetryStatusCodes"), DefaultRetryStatusCodes),
            RetryTransportFailures = GetBoolean(section["RetryTransportFailures"], defaultValue: true),
            ExpectedStatusCodes = ParseInt32List(section.GetSection("ExpectedStatusCodes")),
            SupportedChannels = ParseStringList(section.GetSection("SupportedChannels")),
            Headers = ParseDictionary(section.GetSection("Headers")),
            SigningSecret = section["SigningSecret"]?.Trim(),
            SigningKeyId = section["SigningKeyId"]?.Trim(),
            SignatureHeaderName = section["SignatureHeaderName"]?.Trim() ?? "X-Cephalon-Webhook-Signature",
            SignatureTimestampHeaderName = section["SignatureTimestampHeaderName"]?.Trim() ?? "X-Cephalon-Webhook-Signature-Timestamp",
            SignatureKeyIdHeaderName = section["SignatureKeyIdHeaderName"]?.Trim() ?? "X-Cephalon-Webhook-Key-Id",
            ProviderMessageIdHeaderName = section["ProviderMessageIdHeaderName"]?.Trim() ?? "X-Cephalon-Provider-Message-Id",
            IncludeInvitationMetadata = GetBoolean(section["IncludeInvitationMetadata"], defaultValue: true),
            IncludeRequestMetadata = GetBoolean(section["IncludeRequestMetadata"], defaultValue: true),
            IncludeResponseBodyInMetadata = GetBoolean(section["IncludeResponseBodyInMetadata"], defaultValue: false),
            ResponseBodyMetadataLimit = GetInt32(section["ResponseBodyMetadataLimit"], defaultValue: 1024)
        };

        return options;
    }

    internal Uri? TryGetEndpoint()
    {
        return Uri.TryCreate(Endpoint?.Trim(), UriKind.Absolute, out var endpoint) &&
            (endpoint.Scheme == Uri.UriSchemeHttp || endpoint.Scheme == Uri.UriSchemeHttps)
            ? endpoint
            : null;
    }

    internal HttpMethod GetHttpMethod()
    {
        var method = string.IsNullOrWhiteSpace(Method) ? "POST" : Method.Trim().ToUpperInvariant();
        return new HttpMethod(method);
    }

    internal bool IsSigningEnabled => !string.IsNullOrWhiteSpace(SigningSecret);

    internal TimeSpan GetTimeout() => TimeSpan.FromSeconds(Math.Max(1, TimeoutSeconds));

    internal int GetMaxAttempts() => Math.Clamp(MaxAttempts, 1, 10);

    internal TimeSpan GetRetryDelay() => TimeSpan.FromMilliseconds(Math.Clamp(RetryDelayMilliseconds, 0, 60_000));

    internal int GetResponseBodyMetadataLimit() => Math.Clamp(ResponseBodyMetadataLimit, 0, 16 * 1024);

    private static bool GetBoolean(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static int GetInt32(string? value, int defaultValue)
    {
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static int[] ParseInt32List(IConfigurationSection section, IReadOnlyList<int>? defaultValues = null)
    {
        var parsedValues = ParseConfiguredInt32List(section);
        return parsedValues.Length > 0 || section.Exists() || defaultValues is null
            ? parsedValues
            : defaultValues.ToArray();
    }

    private static int[] ParseConfiguredInt32List(IConfigurationSection section)
    {
        if (!string.IsNullOrWhiteSpace(section.Value))
        {
            return section.Value!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.TryParse(value, out var parsed) ? parsed : -1)
                .Where(static value => value > 0)
                .Distinct()
                .Order()
                .ToArray();
        }

        return section
            .GetChildren()
            .Select(child => int.TryParse(child.Value, out var parsed) ? parsed : -1)
            .Where(static value => value > 0)
            .Distinct()
            .Order()
            .ToArray();
    }

    private static string[] ParseStringList(IConfigurationSection section)
    {
        if (!string.IsNullOrWhiteSpace(section.Value))
        {
            return section.Value!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return section
            .GetChildren()
            .Select(static child => child.Value?.Trim())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Dictionary<string, string> ParseDictionary(IConfigurationSection section)
    {
        return section
            .GetChildren()
            .Where(static child => !string.IsNullOrWhiteSpace(child.Key) && !string.IsNullOrWhiteSpace(child.Value))
            .ToDictionary(
                static child => child.Key.Trim(),
                static child => child.Value!.Trim(),
                StringComparer.OrdinalIgnoreCase);
    }
}
