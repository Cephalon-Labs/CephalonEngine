using System.Security.Claims;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Identity.AspNetCore.Configuration;

/// <summary>
/// Describes ASP.NET Core-specific identity and authorization adapter options for Cephalon.
/// </summary>
public sealed class IdentityAspNetCoreOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityAspNetCoreOptions" /> class.
    /// </summary>
    public IdentityAspNetCoreOptions()
    {
        SubjectIdClaimTypes =
        [
            ClaimTypes.NameIdentifier,
            "sub",
            "subject",
            "user_id"
        ];
        DisplayNameClaimTypes =
        [
            ClaimTypes.Name,
            "name",
            "preferred_username",
            "email"
        ];
        RoleClaimTypes =
        [
            ClaimTypes.Role,
            "role",
            "roles"
        ];
        TenantClaimTypes =
        [
            "tenant_id",
            "tenant",
            "tenantId",
            "tid"
        ];
        TenantRouteKeys =
        [
            "tenantId",
            "tenant-id",
            "tenant"
        ];
        TenantHeaderNames =
        [
            "X-Tenant-Id"
        ];
        ResourceIdRouteKeys =
        [
            "id",
            "resourceId",
            "resource-id"
        ];
        OwnerSubjectIdRouteKeys =
        [
            "ownerSubjectId",
            "owner-subject-id",
            "ownerId",
            "owner-id"
        ];
    }

    /// <summary>
    /// Gets the claim types that can provide the stable Cephalon authorization subject identifier.
    /// </summary>
    public List<string> SubjectIdClaimTypes { get; }

    /// <summary>
    /// Gets the claim types that can provide the human-readable display name for the current subject.
    /// </summary>
    public List<string> DisplayNameClaimTypes { get; }

    /// <summary>
    /// Gets the claim types that can provide role memberships for the current subject.
    /// </summary>
    public List<string> RoleClaimTypes { get; }

    /// <summary>
    /// Gets the claim types that can provide tenant memberships for the current subject.
    /// </summary>
    public List<string> TenantClaimTypes { get; }

    /// <summary>
    /// Gets the route-value keys that can provide the current tenant identifier.
    /// </summary>
    public List<string> TenantRouteKeys { get; }

    /// <summary>
    /// Gets the request-header names that can provide the current tenant identifier when route values do not.
    /// </summary>
    public List<string> TenantHeaderNames { get; }

    /// <summary>
    /// Gets the route-value keys that can provide the current resource identifier.
    /// </summary>
    public List<string> ResourceIdRouteKeys { get; }

    /// <summary>
    /// Gets the route-value keys that can provide the owning subject identifier for the current resource.
    /// </summary>
    public List<string> OwnerSubjectIdRouteKeys { get; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ClaimsIdentity.Name" /> can be used as the subject id
    /// fallback when none of the configured claim types are present.
    /// </summary>
    public bool AllowIdentityNameAsSubjectIdFallback { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether unmatched claims should be projected into
    /// <see cref="Cephalon.Abstractions.Authorization.AuthorizationSubject.Attributes" />.
    /// </summary>
    public bool IncludeAllClaimsAsSubjectAttributes { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether route values should be projected into
    /// <see cref="Cephalon.Abstractions.Authorization.AuthorizationResource.Attributes" />.
    /// </summary>
    public bool IncludeRouteValuesAsResourceAttributes { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether query-string values should be projected into
    /// <see cref="Cephalon.Abstractions.Authorization.AuthorizationContext.Attributes" />.
    /// </summary>
    public bool IncludeQueryStringAsContextAttributes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether request headers should be projected into
    /// <see cref="Cephalon.Abstractions.Authorization.AuthorizationContext.Attributes" />.
    /// </summary>
    public bool IncludeHeadersAsContextAttributes { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Microsoft.AspNetCore.Http.HttpContext.Items" /> key that stores the most recent
    /// Cephalon authorization decision for the current request.
    /// </summary>
    public string AuthorizationDecisionItemKey { get; set; } = "cephalon.authorization.decision";

    /// <summary>
    /// Reads ASP.NET Core identity adapter options from configuration.
    /// </summary>
    /// <param name="configuration">The root configuration that contains the engine section.</param>
    /// <param name="sectionPath">The engine root section path to read from.</param>
    /// <returns>The parsed ASP.NET Core identity adapter options.</returns>
    public static IdentityAspNetCoreOptions FromConfiguration(
        IConfiguration? configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        var options = new IdentityAspNetCoreOptions();
        if (configuration is null)
        {
            return options;
        }

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Identity")
            .GetSection("AspNetCore");

        Apply(section, "SubjectIdClaimTypes", options.SubjectIdClaimTypes);
        Apply(section, "DisplayNameClaimTypes", options.DisplayNameClaimTypes);
        Apply(section, "RoleClaimTypes", options.RoleClaimTypes);
        Apply(section, "TenantClaimTypes", options.TenantClaimTypes);
        Apply(section, "TenantRouteKeys", options.TenantRouteKeys);
        Apply(section, "TenantHeaderNames", options.TenantHeaderNames);
        Apply(section, "ResourceIdRouteKeys", options.ResourceIdRouteKeys);
        Apply(section, "OwnerSubjectIdRouteKeys", options.OwnerSubjectIdRouteKeys);
        options.AllowIdentityNameAsSubjectIdFallback = ParseBoolean(section["AllowIdentityNameAsSubjectIdFallback"], true);
        options.IncludeAllClaimsAsSubjectAttributes = ParseBoolean(section["IncludeAllClaimsAsSubjectAttributes"], true);
        options.IncludeRouteValuesAsResourceAttributes = ParseBoolean(section["IncludeRouteValuesAsResourceAttributes"], true);
        options.IncludeQueryStringAsContextAttributes = ParseBoolean(section["IncludeQueryStringAsContextAttributes"], false);
        options.IncludeHeadersAsContextAttributes = ParseBoolean(section["IncludeHeadersAsContextAttributes"], false);
        options.AuthorizationDecisionItemKey = Normalize(section["AuthorizationDecisionItemKey"]) ?? options.AuthorizationDecisionItemKey;
        return options;
    }

    private static void Apply(IConfiguration section, string key, List<string> target)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(target);

        var values = ReadValues(section, key);
        if (values.Length == 0)
        {
            return;
        }

        target.Clear();
        foreach (var value in values)
        {
            target.Add(value);
        }
    }

    private static string[] ReadValues(IConfiguration section, string key)
    {
        var children = section
            .GetSection(key)
            .GetChildren()
            .Select(static child => Normalize(child.Value))
            .Where(static value => value is not null)
            .Select(static value => value!)
            .ToArray();
        if (children.Length > 0)
        {
            return children;
        }

        var configuredValue = section[key];
        return configuredValue?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static value => Normalize(value))
            .Where(static value => value is not null)
            .Select(static value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
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
