using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Configuration;

/// <summary>
/// Configures ASP.NET Core SendGrid Event Webhook callback translation for tenant-invitation delivery status updates.
/// </summary>
public sealed class SendGridInvitationDeliveryAspNetCoreOptions
{
    internal const string DefaultRoutePattern = "/engine/tenant-invitations/delivery-status/sendgrid";
    internal const int DefaultMaxRequestBodyBytes = 256 * 1024;
    internal const int DefaultMaxEventsPerRequest = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendGridInvitationDeliveryAspNetCoreOptions" /> class.
    /// </summary>
    public SendGridInvitationDeliveryAspNetCoreOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the SendGrid Event Webhook callback endpoint should be mapped.
    /// </summary>
    public bool EnableStatusCallbackEndpoint { get; set; } = true;

    /// <summary>
    /// Gets or sets the ASP.NET Core route pattern used for SendGrid Event Webhook callbacks.
    /// </summary>
    /// <remarks>
    /// The default route stays under <c>/engine</c> because this endpoint is a provider-adapter ingress surface, not an
    /// application-owned onboarding API.
    /// </remarks>
    public string StatusCallbackRoutePattern { get; set; } = DefaultRoutePattern;

    /// <summary>
    /// Gets or sets a value indicating whether the SendGrid callback endpoint should require authorization.
    /// </summary>
    /// <remarks>
    /// The endpoint performs an in-handler authorization check by default. Hosts can satisfy it with ASP.NET Core
    /// authentication, a SendGrid OAuth policy, a gateway, or deliberately disable it for trusted test hosts.
    /// </remarks>
    public bool RequireStatusCallbackAuthorization { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional ASP.NET Core authorization policy required by the SendGrid callback endpoint.
    /// </summary>
    public string? StatusCallbackAuthorizationPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the SendGrid callback endpoint should be excluded from OpenAPI descriptions.
    /// </summary>
    public bool ExcludeStatusCallbackEndpointFromDescription { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether translated SendGrid events must match an existing provider message id.
    /// </summary>
    public bool RequireProviderMessageMatch { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether translated delivery status should be recorded on the invitation.
    /// </summary>
    public bool RecordStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets the source value recorded on translated SendGrid delivery status observations.
    /// </summary>
    public string Source { get; set; } = "sendgrid-event-webhook";

    /// <summary>
    /// Gets or sets the actor value recorded on translated SendGrid delivery status observations.
    /// </summary>
    public string Actor { get; set; } = "sendgrid";

    /// <summary>
    /// Gets or sets the maximum request body size accepted by the SendGrid callback endpoint, in bytes.
    /// </summary>
    public int MaxRequestBodyBytes { get; set; } = DefaultMaxRequestBodyBytes;

    /// <summary>
    /// Gets or sets the maximum number of SendGrid events accepted in one callback request.
    /// </summary>
    public int MaxEventsPerRequest { get; set; } = DefaultMaxEventsPerRequest;

    /// <summary>
    /// Gets or sets a value indicating whether SendGrid engagement events such as open and click should be recorded as delivered.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="false" /> so the endpoint records deliverability events only. Enable this when a
    /// host deliberately wants engagement events to update invitation delivery status.
    /// </remarks>
    public bool MapEngagementEventsAsDelivered { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the prefix before the first dot in <c>sg_message_id</c> should be used for provider matching.
    /// </summary>
    /// <remarks>
    /// Twilio SendGrid recommends storing the Mail Send API <c>X-Message-ID</c> response header to correlate Event
    /// Webhook posts. Event Webhook payloads carry <c>sg_message_id</c>; the prefix commonly matches the stored
    /// <c>X-Message-ID</c>, so this option keeps Cephalon's provider-message guard usable without weakening it.
    /// </remarks>
    public bool NormalizeProviderMessageIdFromSgMessageId { get; set; } = true;

    /// <summary>
    /// Reads SendGrid ASP.NET Core callback options from configuration.
    /// </summary>
    /// <param name="configuration">The root configuration that contains the engine section.</param>
    /// <param name="sectionPath">The engine root section path to read from.</param>
    /// <returns>The parsed SendGrid ASP.NET Core callback options.</returns>
    public static SendGridInvitationDeliveryAspNetCoreOptions FromConfiguration(
        IConfiguration? configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        var options = new SendGridInvitationDeliveryAspNetCoreOptions();
        if (configuration is null)
        {
            return options;
        }

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("MultiTenancy")
            .GetSection("Governance")
            .GetSection("SendGridInvitationDelivery")
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
        options.NormalizeProviderMessageIdFromSgMessageId = ParseBoolean(section["NormalizeProviderMessageIdFromSgMessageId"], options.NormalizeProviderMessageIdFromSgMessageId);
        return options;
    }

    internal int GetMaxRequestBodyBytes() => Math.Clamp(MaxRequestBodyBytes, 1, 10 * 1024 * 1024);

    internal int GetMaxEventsPerRequest() => Math.Clamp(MaxEventsPerRequest, 1, 100_000);

    internal string GetRoutePattern() => Normalize(StatusCallbackRoutePattern) ?? DefaultRoutePattern;

    internal string GetSource() => Normalize(Source) ?? "sendgrid-event-webhook";

    internal string GetActor() => Normalize(Actor) ?? "sendgrid";

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
