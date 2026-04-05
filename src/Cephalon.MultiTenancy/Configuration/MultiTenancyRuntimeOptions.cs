using Cephalon.Abstractions.Tenancy;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.MultiTenancy.Configuration;

/// <summary>
/// Describes host-agnostic runtime options for the Cephalon multi-tenancy companion pack.
/// </summary>
public sealed class MultiTenancyRuntimeOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultiTenancyRuntimeOptions" /> class.
    /// </summary>
    public MultiTenancyRuntimeOptions()
    {
        Tenants = [];
    }

    /// <summary>
    /// Gets or sets a value indicating whether the built-in configuration-driven tenant resolver is active.
    /// </summary>
    public bool EnableDefaultResolver { get; set; } = true;

    /// <summary>
    /// Gets or sets the default tenant identifier used when no explicit request hint resolves a tenant.
    /// </summary>
    public string? DefaultTenantId { get; set; }

    /// <summary>
    /// Gets the configured tenants that the built-in resolver can match by id, key, or domain.
    /// </summary>
    public List<TenantContext> Tenants { get; }

    /// <summary>
    /// Reads multi-tenancy runtime options from configuration.
    /// </summary>
    /// <param name="configuration">The root configuration that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed multi-tenancy runtime options.</returns>
    public static MultiTenancyRuntimeOptions FromConfiguration(
        IConfiguration? configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        var options = new MultiTenancyRuntimeOptions();
        if (configuration is null)
        {
            return options;
        }

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Tenancy");

        options.EnableDefaultResolver = ParseBoolean(section["EnableDefaultResolver"], true);
        options.DefaultTenantId = Normalize(section["DefaultTenantId"]);

        foreach (var tenantSection in section.GetSection("Tenants").GetChildren())
        {
            var tenantId = Normalize(tenantSection["TenantId"]);
            if (tenantId is null)
            {
                continue;
            }

            options.Tenants.Add(new TenantContext(
                tenantId: tenantId,
                tenantKey: tenantSection["TenantKey"],
                displayName: tenantSection["DisplayName"],
                parentTenantId: tenantSection["ParentTenantId"],
                domains: ReadValues(tenantSection.GetSection("Domains")),
                attributes: ReadAttributes(tenantSection.GetSection("Attributes"))));
        }

        return options;
    }

    private static string[] ReadValues(IConfiguration section)
    {
        return section
            .GetChildren()
            .Select(static child => Normalize(child.Value))
            .Where(static value => value is not null)
            .Select(static value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Dictionary<string, string> ReadAttributes(IConfiguration section)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in section.GetChildren())
        {
            var value = Normalize(child.Value);
            if (value is null)
            {
                continue;
            }

            attributes[child.Key] = value;
        }

        return attributes;
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
