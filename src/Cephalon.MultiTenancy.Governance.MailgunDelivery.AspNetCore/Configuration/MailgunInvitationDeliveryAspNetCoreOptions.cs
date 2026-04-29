using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Configuration;

/// <summary>
/// Configures ASP.NET Core Mailgun webhook callback translation for tenant-invitation delivery status updates.
/// </summary>
/// <remarks>
/// This adapter translates Mailgun webhook payloads and can require Mailgun HMAC-SHA256 webhook signature
/// verification before reconciliation. Replay-token protection, durable callback inboxes, and provider polling are
/// intentionally separate slices.
/// </remarks>
public sealed class MailgunInvitationDeliveryAspNetCoreOptions
{
    internal const string DefaultRoutePattern = "/engine/tenant-invitations/delivery-status/mailgun";
    internal const int DefaultMaxRequestBodyBytes = 256 * 1024;
    internal const int DefaultMaxEventsPerRequest = 1000;
    internal const int DefaultSignedWebhookSignatureToleranceSeconds = 300;

    /// <summary>
    /// Initializes a new instance of the <see cref="MailgunInvitationDeliveryAspNetCoreOptions" /> class.
    /// </summary>
    public MailgunInvitationDeliveryAspNetCoreOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Mailgun webhook callback endpoint should be mapped.
    /// </summary>
    public bool EnableStatusCallbackEndpoint { get; set; } = true;

    /// <summary>
    /// Gets or sets the ASP.NET Core route pattern used for Mailgun webhook callbacks.
    /// </summary>
    /// <remarks>
    /// The default route stays under <c>/engine</c> because this endpoint is a provider-adapter ingress surface, not an
    /// application-owned onboarding API.
    /// </remarks>
    public string StatusCallbackRoutePattern { get; set; } = DefaultRoutePattern;

    /// <summary>
    /// Gets or sets a value indicating whether the Mailgun callback endpoint should require authorization.
    /// </summary>
    /// <remarks>
    /// The endpoint performs an in-handler authorization check by default. Hosts can satisfy it with ASP.NET Core
    /// authentication, a gateway, or deliberately disable it for trusted test hosts.
    /// </remarks>
    public bool RequireStatusCallbackAuthorization { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional ASP.NET Core authorization policy required by the Mailgun callback endpoint.
    /// </summary>
    public string? StatusCallbackAuthorizationPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Mailgun callback endpoint should be excluded from OpenAPI descriptions.
    /// </summary>
    public bool ExcludeStatusCallbackEndpointFromDescription { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether translated Mailgun events must match an existing provider message id.
    /// </summary>
    /// <remarks>
    /// Mailgun webhook payloads expose the message identifier through <c>message.headers.message-id</c>. The default
    /// keeps provider-message guarding enabled and wraps header values in angle brackets to match the Messages API
    /// response id shape used by the sender package.
    /// </remarks>
    public bool RequireProviderMessageMatch { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether translated delivery status should be recorded on the invitation.
    /// </summary>
    public bool RecordStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets the source value recorded on translated Mailgun delivery status observations.
    /// </summary>
    public string Source { get; set; } = "mailgun-webhook";

    /// <summary>
    /// Gets or sets the actor value recorded on translated Mailgun delivery status observations.
    /// </summary>
    public string Actor { get; set; } = "mailgun";

    /// <summary>
    /// Gets or sets the maximum request body size accepted by the Mailgun callback endpoint, in bytes.
    /// </summary>
    public int MaxRequestBodyBytes { get; set; } = DefaultMaxRequestBodyBytes;

    /// <summary>
    /// Gets or sets the maximum number of Mailgun events accepted in one callback request.
    /// </summary>
    /// <remarks>
    /// Mailgun posts one JSON event object by default. The endpoint also accepts a JSON array for controlled replay and
    /// test harness scenarios while keeping the same bounded parsing posture.
    /// </remarks>
    public int MaxEventsPerRequest { get; set; } = DefaultMaxEventsPerRequest;

    /// <summary>
    /// Gets or sets a value indicating whether Mailgun engagement events such as opened and clicked should be recorded as delivered.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="false" /> so the endpoint records deliverability events only. Enable this when a
    /// host deliberately wants engagement events to update invitation delivery status.
    /// </remarks>
    public bool MapEngagementEventsAsDelivered { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Mailgun <c>message.headers.message-id</c> values should be wrapped in angle brackets.
    /// </summary>
    /// <remarks>
    /// Mailgun Messages API responses commonly return an angle-bracketed message id, while webhook headers may surface
    /// the same id without brackets. This normalization keeps Cephalon's provider-message guard useful.
    /// </remarks>
    public bool NormalizeProviderMessageIdWithAngleBrackets { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Mailgun webhook requests must carry a valid Mailgun signature before
    /// payload translation and reconciliation can run.
    /// </summary>
    /// <remarks>
    /// When enabled, the endpoint verifies the Mailgun HMAC-SHA256 hex digest over <c>timestamp + token</c> using the
    /// configured webhook signing key. Signature verification is separate from replay-token caching so hosts can adopt
    /// authentication first without claiming durable inbox or distributed replay ownership.
    /// </remarks>
    public bool RequireSignedWebhook { get; set; }

    /// <summary>
    /// Gets or sets the Mailgun webhook signing key used for HMAC-SHA256 verification.
    /// </summary>
    /// <remarks>
    /// This is the Mailgun Send webhook signing key, not the Mailgun API key or an Alerts webhook signing key.
    /// </remarks>
    public string? WebhookSigningKey { get; set; }

    /// <summary>
    /// Gets or sets the allowed clock skew, in seconds, for signed Mailgun webhook timestamps.
    /// </summary>
    /// <remarks>
    /// The endpoint clamps the effective tolerance to at least one second. The default is five minutes.
    /// </remarks>
    public int SignedWebhookSignatureToleranceSeconds { get; set; } = DefaultSignedWebhookSignatureToleranceSeconds;

    /// <summary>
    /// Gets or sets a value indicating whether Mailgun <c>parent-signature</c> should be accepted for subaccount
    /// webhook events.
    /// </summary>
    /// <remarks>
    /// Mailgun includes <c>parent-signature</c> for subaccount events so receivers can validate with the parent account
    /// signing key. Disable this only when a host deliberately requires the child account signature field.
    /// </remarks>
    public bool AcceptParentSignature { get; set; } = true;

    /// <summary>
    /// Reads Mailgun ASP.NET Core callback options from configuration.
    /// </summary>
    /// <param name="configuration">The root configuration that contains the engine section.</param>
    /// <param name="sectionPath">The engine root section path to read from.</param>
    /// <returns>The parsed Mailgun ASP.NET Core callback options.</returns>
    public static MailgunInvitationDeliveryAspNetCoreOptions FromConfiguration(
        IConfiguration? configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        var options = new MailgunInvitationDeliveryAspNetCoreOptions();
        if (configuration is null)
        {
            return options;
        }

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("MultiTenancy")
            .GetSection("Governance")
            .GetSection("MailgunInvitationDelivery")
            .GetSection("AspNetCore");

        options.EnableStatusCallbackEndpoint = ParseBoolean(section["EnableStatusCallbackEndpoint"], options.EnableStatusCallbackEndpoint);
        options.StatusCallbackRoutePattern = Normalize(section["StatusCallbackRoutePattern"]) ?? options.StatusCallbackRoutePattern;
        options.RequireStatusCallbackAuthorization = ParseBoolean(section["RequireStatusCallbackAuthorization"], options.RequireStatusCallbackAuthorization);
        options.StatusCallbackAuthorizationPolicy = Normalize(section["StatusCallbackAuthorizationPolicy"]);
        options.ExcludeStatusCallbackEndpointFromDescription = ParseBoolean(section["ExcludeStatusCallbackEndpointFromDescription"], options.ExcludeStatusCallbackEndpointFromDescription);
        options.RequireProviderMessageMatch = ParseBoolean(section["RequireProviderMessageMatch"], options.RequireProviderMessageMatch);
        options.RecordStatus = ParseBoolean(section["RecordStatus"], options.RecordStatus);
        options.Source = Normalize(section["Source"]) ?? options.Source;
        options.Actor = Normalize(section["Actor"]) ?? options.Actor;
        options.MaxRequestBodyBytes = ParseInt32(section["MaxRequestBodyBytes"], options.MaxRequestBodyBytes);
        options.MaxEventsPerRequest = ParseInt32(section["MaxEventsPerRequest"], options.MaxEventsPerRequest);
        options.MapEngagementEventsAsDelivered = ParseBoolean(section["MapEngagementEventsAsDelivered"], options.MapEngagementEventsAsDelivered);
        options.NormalizeProviderMessageIdWithAngleBrackets = ParseBoolean(section["NormalizeProviderMessageIdWithAngleBrackets"], options.NormalizeProviderMessageIdWithAngleBrackets);
        options.RequireSignedWebhook = ParseBoolean(section["RequireSignedWebhook"], options.RequireSignedWebhook);
        options.WebhookSigningKey = Normalize(section["WebhookSigningKey"]);
        options.SignedWebhookSignatureToleranceSeconds = ParseInt32(section["SignedWebhookSignatureToleranceSeconds"], options.SignedWebhookSignatureToleranceSeconds);
        options.AcceptParentSignature = ParseBoolean(section["AcceptParentSignature"], options.AcceptParentSignature);
        return options;
    }

    internal int GetMaxRequestBodyBytes() => Math.Clamp(MaxRequestBodyBytes, 1, 10 * 1024 * 1024);

    internal int GetMaxEventsPerRequest() => Math.Clamp(MaxEventsPerRequest, 1, 100_000);

    internal string GetRoutePattern() => Normalize(StatusCallbackRoutePattern) ?? DefaultRoutePattern;

    internal string GetSource() => Normalize(Source) ?? "mailgun-webhook";

    internal string GetActor() => Normalize(Actor) ?? "mailgun";

    internal string? GetWebhookSigningKey() => Normalize(WebhookSigningKey);

    internal int GetSignedWebhookSignatureToleranceSeconds() =>
        Math.Clamp(SignedWebhookSignatureToleranceSeconds, 1, 86_400);

    private static int ParseInt32(string? value, int defaultValue)
    {
        var normalizedValue = Normalize(value);
        return normalizedValue is not null && int.TryParse(normalizedValue, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static bool ParseBoolean(string? value, bool defaultValue)
    {
        var normalizedValue = Normalize(value);
        return normalizedValue is not null && bool.TryParse(normalizedValue, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
