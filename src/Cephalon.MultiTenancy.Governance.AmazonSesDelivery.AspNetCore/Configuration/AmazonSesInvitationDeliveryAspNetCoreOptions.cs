using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration;

/// <summary>
/// Configures ASP.NET Core Amazon SES over SNS callback translation for tenant-invitation delivery status updates.
/// </summary>
/// <remarks>
/// This adapter translates SNS-wrapped Amazon SES event publishing payloads into Cephalon delivery-status
/// reconciliation requests. It does not own AWS account setup, SES identity verification, SNS topic/subscription
/// creation beyond optionally confirming signed subscription-confirmation callbacks, durable callback inboxes,
/// distributed replay protection, or provider polling. When configured, it can verify the Amazon SNS message signature
/// before translation, confirm verified SNS subscription requests, observe verified unsubscribe-confirmation lifecycle
/// messages without restoring subscriptions, and skip duplicate SNS message identifiers already recorded by the
/// Cephalon delivery-status observation store.
/// </remarks>
public sealed class AmazonSesInvitationDeliveryAspNetCoreOptions
{
    internal const string DefaultRoutePattern = "/engine/tenant-invitations/delivery-status/amazon-ses";
    internal const int DefaultMaxRequestBodyBytes = 256 * 1024;
    internal const int DefaultMaxEventsPerRequest = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonSesInvitationDeliveryAspNetCoreOptions" /> class.
    /// </summary>
    public AmazonSesInvitationDeliveryAspNetCoreOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Amazon SES callback endpoint should be mapped.
    /// </summary>
    public bool EnableStatusCallbackEndpoint { get; set; } = true;

    /// <summary>
    /// Gets or sets the ASP.NET Core route pattern used for SNS-wrapped Amazon SES callbacks.
    /// </summary>
    /// <remarks>
    /// The default route stays under <c>/engine</c> because this endpoint is a provider-adapter ingress surface, not an
    /// application-owned onboarding API.
    /// </remarks>
    public string StatusCallbackRoutePattern { get; set; } = DefaultRoutePattern;

    /// <summary>
    /// Gets or sets a value indicating whether the Amazon SES callback endpoint should require authorization.
    /// </summary>
    /// <remarks>
    /// The endpoint performs an in-handler authorization check by default. Hosts can satisfy it with ASP.NET Core
    /// authentication, a gateway, or deliberately disable it for trusted test hosts.
    /// </remarks>
    public bool RequireStatusCallbackAuthorization { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional ASP.NET Core authorization policy required by the Amazon SES callback endpoint.
    /// </summary>
    public string? StatusCallbackAuthorizationPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Amazon SES callback endpoint should be excluded from OpenAPI descriptions.
    /// </summary>
    public bool ExcludeStatusCallbackEndpointFromDescription { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether translated Amazon SES events must match an existing provider message id.
    /// </summary>
    /// <remarks>
    /// Amazon SES event payloads expose the SES-assigned message id through <c>mail.messageId</c>. Keeping this guard
    /// enabled makes the callback translator reconcile only the invitation dispatch previously accepted by SES.
    /// </remarks>
    public bool RequireProviderMessageMatch { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether translated delivery status should be recorded on the invitation.
    /// </summary>
    public bool RecordStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets the source value recorded on translated Amazon SES delivery status observations.
    /// </summary>
    public string Source { get; set; } = "amazon-ses-sns";

    /// <summary>
    /// Gets or sets the actor value recorded on translated Amazon SES delivery status observations.
    /// </summary>
    public string Actor { get; set; } = "amazon-ses";

    /// <summary>
    /// Gets or sets the maximum request body size accepted by the Amazon SES callback endpoint, in bytes.
    /// </summary>
    public int MaxRequestBodyBytes { get; set; } = DefaultMaxRequestBodyBytes;

    /// <summary>
    /// Gets or sets the maximum number of Amazon SES events accepted in one callback request.
    /// </summary>
    /// <remarks>
    /// SNS HTTP callbacks normally contain one SES event in the <c>Message</c> field. Arrays are accepted only for
    /// controlled replay and test harness scenarios while keeping the same bounded parsing posture.
    /// </remarks>
    public int MaxEventsPerRequest { get; set; } = DefaultMaxEventsPerRequest;

    /// <summary>
    /// Gets or sets a value indicating whether Amazon SES engagement events such as open and click should be recorded as delivered.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="false" /> so the endpoint records deliverability events only. Enable this when a
    /// host deliberately wants engagement events to update invitation delivery status.
    /// </remarks>
    public bool MapEngagementEventsAsDelivered { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether raw Amazon SES event payloads should be accepted for controlled replay.
    /// </summary>
    /// <remarks>
    /// Production SNS HTTP subscriptions post an SNS envelope whose <c>Message</c> field contains the SES event. This
    /// option lets tests or replay tools post the SES event body directly without claiming a durable callback inbox.
    /// </remarks>
    public bool AcceptRawSesEventPayloads { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether SNS message signatures must verify before translation.
    /// </summary>
    /// <remarks>
    /// When enabled, the endpoint rejects raw SES replay payloads, validates the SNS envelope, verifies the
    /// Base64-encoded RSA signature over the canonical SNS string-to-sign, and records safe verification metadata.
    /// </remarks>
    public bool RequireSnsSignatureVerification { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether verified SNS messages must use <c>SignatureVersion</c> 2.
    /// </summary>
    /// <remarks>
    /// Amazon SNS topics default to signature version 1, but version 2 uses SHA-256 and is the recommended setting for
    /// new deployments. Disable this only when a host deliberately accepts legacy SHA-1 SNS signatures.
    /// </remarks>
    public bool RequireSnsSignatureVersion2 { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether <c>TopicArn</c> must match <see cref="AllowedSnsTopicArns" /> when
    /// signature verification is required.
    /// </summary>
    /// <remarks>
    /// Keeping this enabled follows the SNS spoofing-prevention guidance that receivers reject messages from
    /// unexpected topics. Disable only for controlled multi-topic gateways that apply their own allow-list.
    /// </remarks>
    public bool RequireAllowedSnsTopicArn { get; set; } = true;

    /// <summary>
    /// Gets or sets the SNS topic ARNs accepted by this callback endpoint when topic allow-listing is required.
    /// </summary>
    public string[] AllowedSnsTopicArns { get; set; } = [];

    /// <summary>
    /// Gets or sets a pinned X.509 certificate PEM used to verify SNS signatures instead of downloading the certificate
    /// from <c>SigningCertURL</c>.
    /// </summary>
    /// <remarks>
    /// This is primarily useful for tests, controlled replay, or hosts that deliberately pin the SNS signing
    /// certificate. Production hosts usually leave this unset so the endpoint retrieves the AWS SNS signing
    /// certificate from the validated HTTPS URL in the SNS envelope.
    /// </remarks>
    public string? PinnedSnsSigningCertificatePem { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the SNS signing certificate chain and validity window should be checked.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="true" /> for production safety. Tests using self-signed pinned certificates can
    /// disable this without weakening the canonical message-signature proof.
    /// </remarks>
    public bool ValidateSnsSigningCertificateChain { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether verified SNS callbacks should be protected against replay inside the
    /// current process.
    /// </summary>
    /// <remarks>
    /// Replay protection is active only when <see cref="RequireSnsSignatureVerification" /> is enabled and the SNS
    /// envelope verifies successfully. The built-in guard stores bounded fingerprints derived from <c>TopicArn</c> and
    /// <c>MessageId</c> in memory and does not claim distributed replay protection or durable callback inbox ownership.
    /// </remarks>
    public bool EnableSnsReplayProtection { get; set; } = true;

    /// <summary>
    /// Gets or sets the process-local retention window, in seconds, for verified SNS callback replay fingerprints.
    /// </summary>
    /// <remarks>
    /// The endpoint clamps the effective retention to at least one second. The default is five minutes.
    /// </remarks>
    public int SnsReplayRetentionSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the maximum number of verified SNS callback replay fingerprints retained in the current process.
    /// </summary>
    /// <remarks>
    /// When the bounded cache is full, the oldest fingerprint is evicted before recording a new accepted signed
    /// callback.
    /// </remarks>
    public int SnsReplayCacheLimit { get; set; } = 4096;

    /// <summary>
    /// Gets or sets a value indicating whether translated SNS notifications should skip duplicate <c>MessageId</c>
    /// values that already exist in the Cephalon delivery-status observation store.
    /// </summary>
    /// <remarks>
    /// This guard uses the stable SNS <c>MessageId</c>-derived observation id emitted by the translator. It does not
    /// replace durable inboxing or distributed callback processing; the durability of the guard follows the configured
    /// <c>ITenantInvitationDeliveryStatusObservationStore</c>.
    /// </remarks>
    public bool EnableSnsMessageIdIdempotency { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether verified SNS subscription-confirmation messages should be confirmed by
    /// the callback endpoint.
    /// </summary>
    /// <remarks>
    /// This option is disabled by default. When enabled, the endpoint only confirms <c>SubscriptionConfirmation</c>
    /// envelopes after SNS signature verification has succeeded. It does not create SNS topics, configure SES event
    /// destinations, own subscription lifecycle governance, or store confirmation tokens.
    /// </remarks>
    public bool EnableSnsSubscriptionConfirmation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether verified SNS unsubscribe-confirmation messages should be reported by the
    /// callback endpoint without restoring the subscription.
    /// </summary>
    /// <remarks>
    /// This option is active only when <see cref="RequireSnsSignatureVerification" /> is enabled and the SNS envelope
    /// verifies successfully. The endpoint never visits the unsubscribe envelope's <c>SubscribeURL</c>; that URL would
    /// re-confirm the subscription and belongs to an explicit operator or provider lifecycle flow.
    /// </remarks>
    public bool EnableSnsUnsubscribeConfirmationObservation { get; set; }

    /// <summary>
    /// Gets or sets the timeout, in seconds, for an enabled SNS subscription-confirmation HTTP request.
    /// </summary>
    /// <remarks>
    /// The effective timeout is clamped between one second and five minutes.
    /// </remarks>
    public int SnsSubscriptionConfirmationTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Reads Amazon SES ASP.NET Core callback options from configuration.
    /// </summary>
    /// <param name="configuration">The root configuration that contains the engine section.</param>
    /// <param name="sectionPath">The engine root section path to read from.</param>
    /// <returns>The parsed Amazon SES ASP.NET Core callback options.</returns>
    public static AmazonSesInvitationDeliveryAspNetCoreOptions FromConfiguration(
        IConfiguration? configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        var options = new AmazonSesInvitationDeliveryAspNetCoreOptions();
        if (configuration is null)
        {
            return options;
        }

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("MultiTenancy")
            .GetSection("Governance")
            .GetSection("AmazonSesInvitationDelivery")
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
        options.AcceptRawSesEventPayloads = ParseBoolean(section["AcceptRawSesEventPayloads"], options.AcceptRawSesEventPayloads);
        options.RequireSnsSignatureVerification = ParseBoolean(section["RequireSnsSignatureVerification"], options.RequireSnsSignatureVerification);
        options.RequireSnsSignatureVersion2 = ParseBoolean(section["RequireSnsSignatureVersion2"], options.RequireSnsSignatureVersion2);
        options.RequireAllowedSnsTopicArn = ParseBoolean(section["RequireAllowedSnsTopicArn"], options.RequireAllowedSnsTopicArn);
        options.AllowedSnsTopicArns = ReadStringList(section.GetSection("AllowedSnsTopicArns")).ToArray();
        options.PinnedSnsSigningCertificatePem = Normalize(section["PinnedSnsSigningCertificatePem"])?.Replace("\\n", "\n", StringComparison.Ordinal);
        options.ValidateSnsSigningCertificateChain = ParseBoolean(section["ValidateSnsSigningCertificateChain"], options.ValidateSnsSigningCertificateChain);
        options.EnableSnsReplayProtection = ParseBoolean(section["EnableSnsReplayProtection"], options.EnableSnsReplayProtection);
        options.SnsReplayRetentionSeconds = ParseInt32(section["SnsReplayRetentionSeconds"], options.SnsReplayRetentionSeconds);
        options.SnsReplayCacheLimit = ParseInt32(section["SnsReplayCacheLimit"], options.SnsReplayCacheLimit);
        options.EnableSnsMessageIdIdempotency = ParseBoolean(section["EnableSnsMessageIdIdempotency"], options.EnableSnsMessageIdIdempotency);
        options.EnableSnsSubscriptionConfirmation = ParseBoolean(section["EnableSnsSubscriptionConfirmation"], options.EnableSnsSubscriptionConfirmation);
        options.EnableSnsUnsubscribeConfirmationObservation = ParseBoolean(section["EnableSnsUnsubscribeConfirmationObservation"], options.EnableSnsUnsubscribeConfirmationObservation);
        options.SnsSubscriptionConfirmationTimeoutSeconds = ParseInt32(section["SnsSubscriptionConfirmationTimeoutSeconds"], options.SnsSubscriptionConfirmationTimeoutSeconds);
        return options;
    }

    internal int GetMaxRequestBodyBytes() => Math.Clamp(MaxRequestBodyBytes, 1, 10 * 1024 * 1024);

    internal int GetMaxEventsPerRequest() => Math.Clamp(MaxEventsPerRequest, 1, 100_000);

    internal string GetRoutePattern() => Normalize(StatusCallbackRoutePattern) ?? DefaultRoutePattern;

    internal string GetSource() => Normalize(Source) ?? "amazon-ses-sns";

    internal string GetActor() => Normalize(Actor) ?? "amazon-ses";

    internal string? GetPinnedSnsSigningCertificatePem() =>
        Normalize(PinnedSnsSigningCertificatePem)?.Replace("\\n", "\n", StringComparison.Ordinal);

    internal IReadOnlySet<string> GetAllowedSnsTopicArns() =>
        (AllowedSnsTopicArns ?? [])
            .Select(Normalize)
            .Where(static value => value is not null)
            .Select(static value => value!)
            .ToHashSet(StringComparer.Ordinal);

    internal bool IsSnsReplayProtectionConfigured() =>
        RequireSnsSignatureVerification && EnableSnsReplayProtection;

    internal int GetSnsReplayRetentionSeconds() =>
        Math.Clamp(SnsReplayRetentionSeconds, 1, 86_400);

    internal int GetSnsReplayCacheLimit() =>
        Math.Clamp(SnsReplayCacheLimit, 1, 1_000_000);

    internal bool IsSnsMessageIdIdempotencyConfigured() => EnableSnsMessageIdIdempotency;

    internal bool IsSnsSubscriptionConfirmationConfigured() =>
        EnableSnsSubscriptionConfirmation && RequireSnsSignatureVerification;

    internal bool IsSnsUnsubscribeConfirmationObservationConfigured() =>
        EnableSnsUnsubscribeConfirmationObservation && RequireSnsSignatureVerification;

    internal TimeSpan GetSnsSubscriptionConfirmationTimeout() =>
        TimeSpan.FromSeconds(Math.Clamp(SnsSubscriptionConfirmationTimeoutSeconds, 1, 300));

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

    private static IEnumerable<string> ReadStringList(IConfigurationSection section)
    {
        foreach (var child in section.GetChildren())
        {
            var value = Normalize(child.Value);
            if (value is not null)
            {
                yield return value;
            }
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
