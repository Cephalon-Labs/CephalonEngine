using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Governance.AspNetCore.Configuration;

/// <summary>
/// Configures ASP.NET Core-specific multi-tenancy governance endpoints.
/// </summary>
public sealed class MultiTenancyGovernanceAspNetCoreOptions
{
    internal const string DefaultTenantAdministrationCommandRoutePattern = "/engine/tenant-administration/commands";

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
        return options;
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
