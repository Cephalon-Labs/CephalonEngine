using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Documentation;

internal static class OpenApiDocumentNames
{
    public const string DefaultDocumentName = "v1";

    public static IReadOnlyList<string> Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var resolvedNames = ResolveFromEnabledVersions(configuration)
            ?? ResolveLegacyDocumentNames(configuration)
            ?? [DefaultDocumentName];

        var defaultDocumentName = ResolveDefault(configuration, resolvedNames);
        return resolvedNames.Contains(defaultDocumentName, StringComparer.OrdinalIgnoreCase)
            ? resolvedNames
            : [defaultDocumentName, .. resolvedNames];
    }

    public static string ResolveDefault(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return ResolveDefault(configuration, configuredNames: null);
    }

    private static string ResolveDefault(IConfiguration configuration, string[]? configuredNames)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredDefaultVersion = TryResolveConfiguredDefaultVersion(configuration);
        if (!string.IsNullOrWhiteSpace(configuredDefaultVersion))
        {
            return configuredDefaultVersion;
        }

        var configuredDefault = configuration["OpenApi:DefaultDocument"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configuredDefault))
        {
            return configuredDefault;
        }

        if (configuredNames is not null && configuredNames.Length > 0)
        {
            return configuredNames[0];
        }

        return DefaultDocumentName;
    }

    public static bool HasSingleResolvedDocument(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return Resolve(configuration).Count == 1;
    }

    private static string[]? ResolveFromEnabledVersions(IConfiguration configuration)
    {
        var configuredVersions = configuration
            .GetSection("OpenApi:EnabledVersions")
            .Get<string[]>()
            ?? configuration.GetSection("OpenApi:EnableVersions").Get<string[]>();

        if (configuredVersions is null || configuredVersions.Length == 0)
        {
            return null;
        }

        var resolvedNames = configuredVersions
            .Select(TryNormalizeVersionDocumentName)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return resolvedNames.Length == 0
            ? null
            : resolvedNames;
    }

    private static string[]? ResolveLegacyDocumentNames(IConfiguration configuration)
    {
        var configuredNames = configuration
            .GetSection("OpenApi:Documents")
            .Get<string[]>();

        if (configuredNames is null || configuredNames.Length == 0)
        {
            return null;
        }

        var resolvedNames = configuredNames
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
            .Select(static candidate => candidate.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return resolvedNames.Length == 0
            ? null
            : resolvedNames;
    }

    private static string? TryResolveConfiguredDefaultVersion(IConfiguration configuration)
    {
        return TryNormalizeVersionDocumentName(configuration["OpenApi:DefaultVersion"]);
    }

    private static string? TryNormalizeVersionDocumentName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[1..];
        }

        if (!int.TryParse(normalized, out var major) || major <= 0)
        {
            return null;
        }

        return $"v{major}";
    }
}
