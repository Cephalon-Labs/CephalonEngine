using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Documentation;

internal static class OpenApiDocumentNames
{
    public const string DefaultDocumentName = "v1";

    public static IReadOnlyList<string> Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredNames = configuration
            .GetSection("OpenApi:Documents")
            .Get<string[]>();

        var resolvedNames = configuredNames is null || configuredNames.Length == 0
            ? [DefaultDocumentName]
            : configuredNames
                .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
                .Select(static candidate => candidate.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (resolvedNames.Length == 0)
        {
            resolvedNames = [DefaultDocumentName];
        }

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
}
