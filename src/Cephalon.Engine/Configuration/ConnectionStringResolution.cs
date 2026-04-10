using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Resolves a provider connection string from either an inline value or a named entry under
/// the root <c>ConnectionStrings</c> section.
/// </summary>
public static class ConnectionStringResolution
{
    /// <summary>
    /// Resolves the effective connection string for a pack or provider.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="connectionString">The inline connection string value.</param>
    /// <param name="connectionStringName">
    /// The root-level <c>ConnectionStrings</c> key to resolve when an inline value is not used.
    /// </param>
    /// <param name="defaultConnectionString">
    /// The provider default to use when neither an inline value nor a named value is configured.
    /// </param>
    /// <param name="sectionPath">The logical provider options section used for diagnostics.</param>
    /// <param name="providerDisplayName">The provider name used in human-readable error messages.</param>
    /// <returns>The resolved connection string.</returns>
    public static string Resolve(
        IConfiguration? configuration,
        string? connectionString,
        string? connectionStringName,
        string defaultConnectionString,
        string sectionPath,
        string providerDisplayName)
    {
        var normalizedConnectionString = Normalize(connectionString);
        var normalizedConnectionStringName = Normalize(connectionStringName);

        if (normalizedConnectionString is not null &&
            normalizedConnectionStringName is not null)
        {
            throw new InvalidOperationException(
                $"{sectionPath} must choose either ConnectionStringName or ConnectionString for {providerDisplayName}, but not both.");
        }

        if (normalizedConnectionString is not null)
        {
            return normalizedConnectionString;
        }

        if (normalizedConnectionStringName is not null)
        {
            if (configuration is null)
            {
                throw new InvalidOperationException(
                    $"{sectionPath}:ConnectionStringName is set to '{normalizedConnectionStringName}', but IConfiguration is not available to resolve ConnectionStrings:{normalizedConnectionStringName}.");
            }

            var resolved = Normalize(configuration.GetConnectionString(normalizedConnectionStringName))
                ?? Normalize(configuration[$"ConnectionStrings:{normalizedConnectionStringName}"]);
            if (resolved is null)
            {
                throw new InvalidOperationException(
                    $"{sectionPath}:ConnectionStringName '{normalizedConnectionStringName}' could not be resolved from ConnectionStrings:{normalizedConnectionStringName}.");
            }

            return resolved;
        }

        return defaultConnectionString;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
