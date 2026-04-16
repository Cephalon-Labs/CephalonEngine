using Microsoft.Extensions.Configuration;
using Cephalon.Abstractions.Transports;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Configures host-level governance for public REST endpoint publication in Cephalon ASP.NET Core hosts.
/// </summary>
/// <remarks>
/// These settings describe host-level publication governance for module-owned REST shorthand paths.
/// They intentionally stay out of the engine core because they govern the ASP.NET Core public REST surface.
/// </remarks>
public sealed class RestApiGovernanceOptions
{
    /// <summary>
    /// Gets the root configuration section used for REST API governance settings.
    /// </summary>
    public const string SectionName = "RestApi";

    /// <summary>
    /// Initializes a new instance of the <see cref="RestApiGovernanceOptions" /> class.
    /// </summary>
    /// <param name="suppressions">The configured suppression rules for shorthand REST candidates.</param>
    /// <param name="overrides">The configured override rules for shorthand REST candidates.</param>
    public RestApiGovernanceOptions(
        IReadOnlyList<RestEndpointSuppressionOptions>? suppressions = null,
        IReadOnlyList<RestEndpointOverrideOptions>? overrides = null)
    {
        Suppressions = suppressions?
            .Where(static value => value is not null)
            .ToArray() ?? [];
        Overrides = overrides?
            .Where(static value => value is not null)
            .ToArray() ?? [];
    }

    /// <summary>
    /// Gets the configured suppression rules for descriptor-backed REST shorthand candidates.
    /// </summary>
    public IReadOnlyList<RestEndpointSuppressionOptions> Suppressions { get; }

    /// <summary>
    /// Gets the configured override rules for descriptor-backed REST shorthand candidates.
    /// </summary>
    public IReadOnlyList<RestEndpointOverrideOptions> Overrides { get; }

    /// <summary>
    /// Gets a value indicating whether any REST governance values were explicitly supplied.
    /// </summary>
    public bool HasValues => Suppressions.Count > 0 || Overrides.Count > 0;

    /// <summary>
    /// Binds and normalizes REST governance settings from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">The configuration section path to bind.</param>
    /// <returns>The normalized REST governance settings.</returns>
    public static RestApiGovernanceOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var suppressions = configuration.GetSection(sectionPath)
            .GetSection("Suppressions")
            .GetChildren()
            .Select(static child => new RestEndpointSuppressionOptions(
                id: child.Key,
                candidateIds: ReadStringArray(child.GetSection("CandidateIds")),
                behaviorIds: ReadStringArray(child.GetSection("Behaviors")),
                sourceModuleIds: ReadStringArray(child.GetSection("Modules")),
                authoringStyles: ReadStringArray(child.GetSection("AuthoringStyles")),
                apiVersionMajors: ReadPositiveIntArray(child.GetSection("ApiVersionMajors")),
                methods: ReadStringArray(child.GetSection("Methods")),
                relativePatterns: ReadStringArray(child.GetSection("RelativePatterns")),
                routeGroupPrefixes: ReadStringArray(child.GetSection("RouteGroupPrefixes"))))
            .ToArray();
        var overrides = configuration.GetSection(sectionPath)
            .GetSection("Overrides")
            .GetChildren()
            .Select(child => new RestEndpointOverrideOptions(
                id: child.Key,
                candidateIds: ReadStringArray(child.GetSection("CandidateIds")),
                behaviorIds: ReadStringArray(child.GetSection("Behaviors")),
                sourceModuleIds: ReadStringArray(child.GetSection("Modules")),
                authoringStyles: ReadStringArray(child.GetSection("AuthoringStyles")),
                apiVersionMajors: ReadPositiveIntArray(child.GetSection("ApiVersionMajors")),
                methods: ReadStringArray(child.GetSection("Methods")),
                relativePatterns: ReadStringArray(child.GetSection("RelativePatterns")),
                routeGroupPrefixes: ReadStringArray(child.GetSection("RouteGroupPrefixes")),
                apiVersionMajor: ReadPositiveInt(child, "ApiVersionMajor"),
                method: child["Method"]?.Trim(),
                pattern: child["Pattern"]?.Trim(),
                routeGroupPrefix: child["RouteGroupPrefix"]?.Trim(),
                endpointName: child["EndpointName"]?.Trim(),
                summary: child["Summary"]?.Trim(),
                description: child["Description"]?.Trim(),
                requiredCapabilityKey: child["RequiredCapabilityKey"]?.Trim(),
                clearRequiredCapability: ReadBoolean(child, "ClearRequiredCapability"),
                bindings: ReadBindings(child.GetSection("Bindings")),
                removedBindingProperties: ReadStringArray(child.GetSection("RemovedBindingProperties")),
                bindingMode: ReadBindingMode(child)))
            .ToArray();

        return new RestApiGovernanceOptions(suppressions, overrides);
    }

    private static string[] ReadStringArray(IConfiguration section)
    {
        return section.GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int? ReadPositiveInt(IConfigurationSection section, string key)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var rawValue = section[key]?.Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (!int.TryParse(rawValue, out var parsedValue) || parsedValue <= 0)
        {
            throw new InvalidOperationException(
                $"REST API governance value '{section.Path}:{key}' must be a positive integer.");
        }

        return parsedValue;
    }

    private static int[] ReadPositiveIntArray(IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return section.GetChildren()
            .Select(child =>
            {
                var rawValue = child.Value?.Trim();
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    throw new InvalidOperationException(
                        $"REST API governance value '{child.Path}' must be a positive integer.");
                }

                if (!int.TryParse(rawValue, out var parsedValue) || parsedValue <= 0)
                {
                    throw new InvalidOperationException(
                        $"REST API governance value '{child.Path}' must be a positive integer.");
                }

                return parsedValue;
            })
            .Distinct()
            .OrderBy(static value => value)
            .ToArray();
    }

    private static bool ReadBoolean(IConfigurationSection section, string key)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var rawValue = section[key]?.Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        if (!bool.TryParse(rawValue, out var parsedValue))
        {
            throw new InvalidOperationException(
                $"REST API governance value '{section.Path}:{key}' must be true or false.");
        }

        return parsedValue;
    }

    private static RestEndpointBindingDescriptor[] ReadBindings(IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return section.GetChildren()
            .Select(child => new RestEndpointBindingDescriptor(
                propertyName: child["PropertyName"]?.Trim()
                    ?? throw new InvalidOperationException(
                        $"REST API governance value '{child.Path}:PropertyName' is required."),
                source: ReadBindingSource(child),
                name: child["Name"]?.Trim()))
            .ToArray();
    }

    private static RestEndpointBindingSource ReadBindingSource(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        var rawValue = section["Source"]?.Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new InvalidOperationException(
                $"REST API governance value '{section.Path}:Source' is required.");
        }

        if (!Enum.TryParse<RestEndpointBindingSource>(rawValue, ignoreCase: true, out var parsedValue) ||
            parsedValue == RestEndpointBindingSource.Unspecified)
        {
            throw new InvalidOperationException(
                $"REST API governance value '{section.Path}:Source' must be one of Route, Query, Header, or Body.");
        }

        return parsedValue;
    }

    private static RestEndpointOverrideBindingMode ReadBindingMode(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        var rawValue = section["BindingMode"]?.Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return RestEndpointOverrideBindingMode.Unspecified;
        }

        return rawValue.ToUpperInvariant() switch
        {
            "REPLACEEXPLICIT" or "REPLACE-EXPLICIT" => RestEndpointOverrideBindingMode.ReplaceExplicit,
            "MERGEEXPLICIT" or "MERGE-EXPLICIT" => RestEndpointOverrideBindingMode.MergeExplicit,
            _ => throw new InvalidOperationException(
                $"REST API governance value '{section.Path}:BindingMode' must be ReplaceExplicit or MergeExplicit.")
        };
    }
}
