using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Governance.AspNetCore.Configuration;

/// <summary>
/// Configures ASP.NET Core-specific multi-tenancy governance endpoints.
/// </summary>
public sealed class MultiTenancyGovernanceAspNetCoreOptions
{
    internal const string DefaultTenantAdministrationCommandRoutePattern = "/engine/tenant-administration/commands";
    internal const string DefaultTenantInvitationDeliveryStatusCallbackRoutePattern = "/engine/tenant-invitations/delivery-status";
    internal const string DefaultTenantInvitationDeliveryStatusObservationRoutePattern = "/engine/tenant-invitations/delivery-status/observations";
    internal const string DefaultTenantInvitationDeliveryStatusCallbackSignatureHeaderName = "X-Cephalon-Callback-Signature";
    internal const string DefaultTenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName = "X-Cephalon-Callback-Signature-Timestamp";
    internal const string DefaultTenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName = "X-Cephalon-Callback-Key-Id";

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiTenancyGovernanceAspNetCoreOptions" /> class.
    /// </summary>
    public MultiTenancyGovernanceAspNetCoreOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the HTTP proof publication endpoint should be mapped.
    /// </summary>
    public bool EnableHttpProofPublicationEndpoint { get; set; } = true;

    /// <summary>
    /// Gets or sets the endpoint route pattern used for published HTTP proof files.
    /// </summary>
    /// <remarks>
    /// The default catch-all route is intentionally constrained under <c>/.well-known/cephalon/</c> so it does not
    /// compete with application-owned routes.
    /// </remarks>
    public string RoutePattern { get; set; } = "/.well-known/cephalon/{**proofPath}";

    /// <summary>
    /// Gets or sets the cache-control header written for served proof files.
    /// </summary>
    public string CacheControlHeader { get; set; } = "no-store";

    /// <summary>
    /// Gets or sets a value indicating whether the proof endpoint should be excluded from OpenAPI descriptions.
    /// </summary>
    public bool ExcludeFromDescription { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the tenant-administration command endpoint should be mapped.
    /// </summary>
    public bool EnableTenantAdministrationCommandEndpoint { get; set; } = true;

    /// <summary>
    /// Gets or sets the endpoint route pattern used for tenant-administration workflow commands.
    /// </summary>
    /// <remarks>
    /// The default route stays under <c>/engine</c> because the endpoint is an operator/admin surface, not an
    /// application-owned public onboarding API.
    /// </remarks>
    public string TenantAdministrationCommandRoutePattern { get; set; } = DefaultTenantAdministrationCommandRoutePattern;

    /// <summary>
    /// Gets or sets a value indicating whether the tenant-administration command endpoint should require authorization.
    /// </summary>
    /// <remarks>
    /// The endpoint also performs a fail-closed in-handler authorization check so accidental hosts without ASP.NET Core
    /// authorization middleware do not execute tenant-administration commands anonymously.
    /// </remarks>
    public bool RequireTenantAdministrationAuthorization { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional ASP.NET Core authorization policy required by the tenant-administration command endpoint.
    /// </summary>
    public string? TenantAdministrationAuthorizationPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tenant-administration command endpoint should be excluded from OpenAPI descriptions.
    /// </summary>
    public bool ExcludeTenantAdministrationEndpointFromDescription { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the tenant-invitation delivery status callback endpoint should be mapped.
    /// </summary>
    public bool EnableTenantInvitationDeliveryStatusCallbackEndpoint { get; set; } = true;

    /// <summary>
    /// Gets or sets the endpoint route pattern used for normalized tenant-invitation delivery status callbacks.
    /// </summary>
    /// <remarks>
    /// The default route stays under <c>/engine</c> because the endpoint is an operator/provider-adapter ingress surface,
    /// not an application-owned public onboarding API.
    /// </remarks>
    public string TenantInvitationDeliveryStatusCallbackRoutePattern { get; set; } = DefaultTenantInvitationDeliveryStatusCallbackRoutePattern;

    /// <summary>
    /// Gets or sets a value indicating whether the delivery status callback endpoint should require authorization.
    /// </summary>
    /// <remarks>
    /// The endpoint also performs a fail-closed in-handler authorization check so accidental hosts without ASP.NET Core
    /// authorization middleware do not accept provider or adapter status callbacks anonymously.
    /// </remarks>
    public bool RequireTenantInvitationDeliveryStatusCallbackAuthorization { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional ASP.NET Core authorization policy required by the delivery status callback endpoint.
    /// </summary>
    public string? TenantInvitationDeliveryStatusCallbackAuthorizationPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the delivery status callback endpoint should be excluded from OpenAPI descriptions.
    /// </summary>
    public bool ExcludeTenantInvitationDeliveryStatusCallbackEndpointFromDescription { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether callback requests must keep provider message matching enabled.
    /// </summary>
    /// <remarks>
    /// Provider message matching is enforced by default so a generic callback cannot opt out of the host-agnostic
    /// reconciliation safety check unless the host deliberately relaxes this setting.
    /// </remarks>
    public bool RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch { get; set; } = true;

    /// <summary>
    /// Gets or sets the shared secret used to verify normalized delivery-status callback request bodies with HMAC-SHA256.
    /// </summary>
    /// <remarks>
    /// When a value is configured, every callback request must include a valid Cephalon callback signature before the
    /// request is reconciled. Leave this empty when the host uses ASP.NET Core authorization or a provider-specific
    /// companion to authenticate callback ingress instead.
    /// </remarks>
    public string? TenantInvitationDeliveryStatusCallbackSigningSecret { get; set; }

    /// <summary>
    /// Gets or sets the optional signing key identifier expected on signed delivery-status callback requests.
    /// </summary>
    public string? TenantInvitationDeliveryStatusCallbackSigningKeyId { get; set; }

    /// <summary>
    /// Gets or sets the request header that carries the callback signature.
    /// </summary>
    public string TenantInvitationDeliveryStatusCallbackSignatureHeaderName { get; set; } =
        DefaultTenantInvitationDeliveryStatusCallbackSignatureHeaderName;

    /// <summary>
    /// Gets or sets the request header that carries the Unix timestamp included in the callback signature.
    /// </summary>
    public string TenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName { get; set; } =
        DefaultTenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName;

    /// <summary>
    /// Gets or sets the request header that carries the optional callback signing key identifier.
    /// </summary>
    public string TenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName { get; set; } =
        DefaultTenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName;

    /// <summary>
    /// Gets or sets the allowed clock skew, in seconds, for signed delivery-status callback timestamps.
    /// </summary>
    /// <remarks>
    /// The endpoint clamps the effective tolerance to at least one second. The default is five minutes.
    /// </remarks>
    public int TenantInvitationDeliveryStatusCallbackSignatureToleranceSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets a value indicating whether signed delivery-status callbacks should be protected against replay inside the current process.
    /// </summary>
    /// <remarks>
    /// Replay protection is active only when <see cref="TenantInvitationDeliveryStatusCallbackSigningSecret" /> is configured and
    /// the request signature verifies successfully. The built-in guard stores bounded signature fingerprints in memory and does not
    /// claim durable inbox storage, cross-node deduplication, or distributed exactly-once delivery.
    /// </remarks>
    public bool EnableTenantInvitationDeliveryStatusCallbackReplayProtection { get; set; } = true;

    /// <summary>
    /// Gets or sets the process-local retention window, in seconds, for signed callback replay fingerprints.
    /// </summary>
    /// <remarks>
    /// The endpoint clamps the effective retention to at least one second. The default matches the signature timestamp tolerance.
    /// </remarks>
    public int TenantInvitationDeliveryStatusCallbackReplayRetentionSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the maximum number of signed callback replay fingerprints retained in the current process.
    /// </summary>
    /// <remarks>
    /// When the bounded cache is full, the oldest fingerprint is evicted before recording a new accepted signed callback.
    /// </remarks>
    public int TenantInvitationDeliveryStatusCallbackReplayCacheLimit { get; set; } = 4096;

    /// <summary>
    /// Gets or sets a value indicating whether the delivery status observation read endpoint should be mapped.
    /// </summary>
    public bool EnableTenantInvitationDeliveryStatusObservationEndpoint { get; set; } = true;

    /// <summary>
    /// Gets or sets the endpoint route pattern used for reading normalized tenant-invitation delivery status observations.
    /// </summary>
    /// <remarks>
    /// The default route stays under <c>/engine</c> because the endpoint is an operator/audit surface over Cephalon's
    /// normalized observation store, not a provider-specific callback inbox.
    /// </remarks>
    public string TenantInvitationDeliveryStatusObservationRoutePattern { get; set; } =
        DefaultTenantInvitationDeliveryStatusObservationRoutePattern;

    /// <summary>
    /// Gets or sets a value indicating whether the delivery status observation read endpoint should require authorization.
    /// </summary>
    /// <remarks>
    /// The endpoint also performs a fail-closed in-handler authorization check so accidental hosts without ASP.NET Core
    /// authorization middleware do not expose invitation delivery audit data anonymously.
    /// </remarks>
    public bool RequireTenantInvitationDeliveryStatusObservationAuthorization { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional ASP.NET Core authorization policy required by the delivery status observation read endpoint.
    /// </summary>
    public string? TenantInvitationDeliveryStatusObservationAuthorizationPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the delivery status observation read endpoint should be excluded from OpenAPI descriptions.
    /// </summary>
    public bool ExcludeTenantInvitationDeliveryStatusObservationEndpointFromDescription { get; set; } = true;

    /// <summary>
    /// Gets or sets the default number of observations returned when a read request does not specify a limit.
    /// </summary>
    public int TenantInvitationDeliveryStatusObservationDefaultLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of observations returned by one read request.
    /// </summary>
    public int TenantInvitationDeliveryStatusObservationMaxLimit { get; set; } = 500;

    /// <summary>
    /// Reads ASP.NET Core governance adapter options from configuration.
    /// </summary>
    /// <param name="configuration">The root configuration that contains the engine section.</param>
    /// <param name="sectionPath">The engine root section path to read from.</param>
    /// <returns>The parsed ASP.NET Core governance adapter options.</returns>
    public static MultiTenancyGovernanceAspNetCoreOptions FromConfiguration(
        IConfiguration? configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        var options = new MultiTenancyGovernanceAspNetCoreOptions();
        if (configuration is null)
        {
            return options;
        }

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("MultiTenancy")
            .GetSection("Governance")
            .GetSection("AspNetCore");

        options.EnableHttpProofPublicationEndpoint = ParseBoolean(section["EnableHttpProofPublicationEndpoint"], options.EnableHttpProofPublicationEndpoint);
        options.RoutePattern = Normalize(section["RoutePattern"]) ?? options.RoutePattern;
        options.CacheControlHeader = Normalize(section["CacheControlHeader"]) ?? options.CacheControlHeader;
        options.ExcludeFromDescription = ParseBoolean(section["ExcludeFromDescription"], options.ExcludeFromDescription);
        options.EnableTenantAdministrationCommandEndpoint = ParseBoolean(section["EnableTenantAdministrationCommandEndpoint"], options.EnableTenantAdministrationCommandEndpoint);
        options.TenantAdministrationCommandRoutePattern = Normalize(section["TenantAdministrationCommandRoutePattern"]) ?? options.TenantAdministrationCommandRoutePattern;
        options.RequireTenantAdministrationAuthorization = ParseBoolean(section["RequireTenantAdministrationAuthorization"], options.RequireTenantAdministrationAuthorization);
        options.TenantAdministrationAuthorizationPolicy = Normalize(section["TenantAdministrationAuthorizationPolicy"]);
        options.ExcludeTenantAdministrationEndpointFromDescription = ParseBoolean(section["ExcludeTenantAdministrationEndpointFromDescription"], options.ExcludeTenantAdministrationEndpointFromDescription);
        options.EnableTenantInvitationDeliveryStatusCallbackEndpoint = ParseBoolean(section["EnableTenantInvitationDeliveryStatusCallbackEndpoint"], options.EnableTenantInvitationDeliveryStatusCallbackEndpoint);
        options.TenantInvitationDeliveryStatusCallbackRoutePattern = Normalize(section["TenantInvitationDeliveryStatusCallbackRoutePattern"]) ?? options.TenantInvitationDeliveryStatusCallbackRoutePattern;
        options.RequireTenantInvitationDeliveryStatusCallbackAuthorization = ParseBoolean(section["RequireTenantInvitationDeliveryStatusCallbackAuthorization"], options.RequireTenantInvitationDeliveryStatusCallbackAuthorization);
        options.TenantInvitationDeliveryStatusCallbackAuthorizationPolicy = Normalize(section["TenantInvitationDeliveryStatusCallbackAuthorizationPolicy"]);
        options.ExcludeTenantInvitationDeliveryStatusCallbackEndpointFromDescription = ParseBoolean(section["ExcludeTenantInvitationDeliveryStatusCallbackEndpointFromDescription"], options.ExcludeTenantInvitationDeliveryStatusCallbackEndpointFromDescription);
        options.RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch = ParseBoolean(section["RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch"], options.RequireTenantInvitationDeliveryStatusCallbackProviderMessageMatch);
        options.TenantInvitationDeliveryStatusCallbackSigningSecret = Normalize(section["TenantInvitationDeliveryStatusCallbackSigningSecret"]);
        options.TenantInvitationDeliveryStatusCallbackSigningKeyId = Normalize(section["TenantInvitationDeliveryStatusCallbackSigningKeyId"]);
        options.TenantInvitationDeliveryStatusCallbackSignatureHeaderName = Normalize(section["TenantInvitationDeliveryStatusCallbackSignatureHeaderName"]) ?? options.TenantInvitationDeliveryStatusCallbackSignatureHeaderName;
        options.TenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName = Normalize(section["TenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName"]) ?? options.TenantInvitationDeliveryStatusCallbackSignatureTimestampHeaderName;
        options.TenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName = Normalize(section["TenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName"]) ?? options.TenantInvitationDeliveryStatusCallbackSignatureKeyIdHeaderName;
        options.TenantInvitationDeliveryStatusCallbackSignatureToleranceSeconds = ParseInt32(section["TenantInvitationDeliveryStatusCallbackSignatureToleranceSeconds"], options.TenantInvitationDeliveryStatusCallbackSignatureToleranceSeconds);
        options.EnableTenantInvitationDeliveryStatusCallbackReplayProtection = ParseBoolean(section["EnableTenantInvitationDeliveryStatusCallbackReplayProtection"], options.EnableTenantInvitationDeliveryStatusCallbackReplayProtection);
        options.TenantInvitationDeliveryStatusCallbackReplayRetentionSeconds = ParseInt32(section["TenantInvitationDeliveryStatusCallbackReplayRetentionSeconds"], options.TenantInvitationDeliveryStatusCallbackReplayRetentionSeconds);
        options.TenantInvitationDeliveryStatusCallbackReplayCacheLimit = ParseInt32(section["TenantInvitationDeliveryStatusCallbackReplayCacheLimit"], options.TenantInvitationDeliveryStatusCallbackReplayCacheLimit);
        options.EnableTenantInvitationDeliveryStatusObservationEndpoint = ParseBoolean(section["EnableTenantInvitationDeliveryStatusObservationEndpoint"], options.EnableTenantInvitationDeliveryStatusObservationEndpoint);
        options.TenantInvitationDeliveryStatusObservationRoutePattern = Normalize(section["TenantInvitationDeliveryStatusObservationRoutePattern"]) ?? options.TenantInvitationDeliveryStatusObservationRoutePattern;
        options.RequireTenantInvitationDeliveryStatusObservationAuthorization = ParseBoolean(section["RequireTenantInvitationDeliveryStatusObservationAuthorization"], options.RequireTenantInvitationDeliveryStatusObservationAuthorization);
        options.TenantInvitationDeliveryStatusObservationAuthorizationPolicy = Normalize(section["TenantInvitationDeliveryStatusObservationAuthorizationPolicy"]);
        options.ExcludeTenantInvitationDeliveryStatusObservationEndpointFromDescription = ParseBoolean(section["ExcludeTenantInvitationDeliveryStatusObservationEndpointFromDescription"], options.ExcludeTenantInvitationDeliveryStatusObservationEndpointFromDescription);
        options.TenantInvitationDeliveryStatusObservationDefaultLimit = ParseInt32(section["TenantInvitationDeliveryStatusObservationDefaultLimit"], options.TenantInvitationDeliveryStatusObservationDefaultLimit);
        options.TenantInvitationDeliveryStatusObservationMaxLimit = ParseInt32(section["TenantInvitationDeliveryStatusObservationMaxLimit"], options.TenantInvitationDeliveryStatusObservationMaxLimit);
        return options;
    }

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
