using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration;

/// <summary>
/// Configures ASP.NET Core Amazon SES over SNS callback translation for tenant-invitation delivery status updates.
/// </summary>
/// <remarks>
/// This adapter translates SNS-wrapped Amazon SES event publishing payloads into Cephalon delivery-status
/// reconciliation requests. It does not own AWS account setup, SES identity verification, SNS topic/subscription
/// creation, SNS signature verification, durable callback inboxes, distributed replay protection, or provider polling.
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
        return options;
    }

    internal int GetMaxRequestBodyBytes() => Math.Clamp(MaxRequestBodyBytes, 1, 10 * 1024 * 1024);

    internal int GetMaxEventsPerRequest() => Math.Clamp(MaxEventsPerRequest, 1, 100_000);

    internal string GetRoutePattern() => Normalize(StatusCallbackRoutePattern) ?? DefaultRoutePattern;

    internal string GetSource() => Normalize(Source) ?? "amazon-ses-sns";

    internal string GetActor() => Normalize(Actor) ?? "amazon-ses";

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
