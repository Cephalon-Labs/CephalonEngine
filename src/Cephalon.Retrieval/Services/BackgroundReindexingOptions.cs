using Cephalon.Retrieval.Configuration;

namespace Cephalon.Retrieval.Services;

internal static class BackgroundReindexingOptions
{
    internal static string[] ResolveConfiguredCollectionIds(RetrievalOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.BackgroundReindexCollectionIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static string ResolveCollectionScope(string[] configuredCollectionIds)
    {
        ArgumentNullException.ThrowIfNull(configuredCollectionIds);

        return configuredCollectionIds.Length == 0 ? "all" : "configured";
    }
}
