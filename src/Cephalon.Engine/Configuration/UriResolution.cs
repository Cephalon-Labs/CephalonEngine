using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Resolves a provider URI from either an inline value or a named entry under
/// the root <c>Uris</c> section.
/// </summary>
public static class UriResolution
{
    /// <summary>
    /// Resolves the effective provider URI for a pack or provider.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="uri">The inline URI value.</param>
    /// <param name="uriName">
    /// The root-level <c>Uris</c> key to resolve when an inline value is not used.
    /// </param>
    /// <param name="defaultUri">
    /// The provider default to use when neither an inline value nor a named value is configured.
    /// </param>
    /// <param name="sectionPath">The logical provider options section used for diagnostics.</param>
    /// <param name="providerDisplayName">The provider name used in human-readable error messages.</param>
    /// <returns>The resolved URI string.</returns>
    public static string Resolve(
        IConfiguration? configuration,
        string? uri,
        string? uriName,
        string defaultUri,
        string sectionPath,
        string providerDisplayName)
    {
        var normalizedUri = Normalize(uri);
        var normalizedUriName = Normalize(uriName);

        if (normalizedUri is not null &&
            normalizedUriName is not null)
        {
            throw new InvalidOperationException(
                $"{sectionPath} must choose either UriName or Uri for {providerDisplayName}, but not both.");
        }

        if (normalizedUri is not null)
        {
            return normalizedUri;
        }

        if (normalizedUriName is not null)
        {
            if (configuration is null)
            {
                throw new InvalidOperationException(
                    $"{sectionPath}:UriName is set to '{normalizedUriName}', but IConfiguration is not available to resolve Uris:{normalizedUriName}.");
            }

            var resolved = Normalize(configuration[$"Uris:{normalizedUriName}"]);
            if (resolved is null)
            {
                throw new InvalidOperationException(
                    $"{sectionPath}:UriName '{normalizedUriName}' could not be resolved from Uris:{normalizedUriName}.");
            }

            return resolved;
        }

        return defaultUri;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
