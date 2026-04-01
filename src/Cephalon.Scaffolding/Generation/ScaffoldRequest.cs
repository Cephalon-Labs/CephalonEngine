namespace Cephalon.Scaffolding.Generation;

/// <summary>
/// Describes the user input required to turn an app profile into a concrete scaffold.
/// </summary>
public sealed class ScaffoldRequest
{
    /// <summary>
    /// Creates a new scaffold request.
    /// </summary>
    /// <param name="appName">The application name to scaffold.</param>
    /// <param name="modules">The module names to materialize in the scaffold.</param>
    /// <param name="features">The feature or slice names to materialize in the scaffold.</param>
    /// <param name="targetFramework">The target framework for generated projects.</param>
    /// <param name="cephalonPackageVersion">The Cephalon package version to write into the scaffold.</param>
    public ScaffoldRequest(
        string appName,
        IReadOnlyList<string>? modules = null,
        IReadOnlyList<string>? features = null,
        string targetFramework = "net10.0",
        string cephalonPackageVersion = "0.1.0-preview")
    {
        if (string.IsNullOrWhiteSpace(appName))
        {
            throw new ArgumentException("App name is required.", nameof(appName));
        }

        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            throw new ArgumentException("Target framework is required.", nameof(targetFramework));
        }

        if (string.IsNullOrWhiteSpace(cephalonPackageVersion))
        {
            throw new ArgumentException("Cephalon package version is required.", nameof(cephalonPackageVersion));
        }

        AppName = appName.Trim();
        Modules = Normalize(modules);
        Features = Normalize(features);
        TargetFramework = targetFramework.Trim();
        CephalonPackageVersion = cephalonPackageVersion.Trim();
        RootNamespace = ToNamespace(AppName);
    }

    /// <summary>
    /// Gets the application name to scaffold.
    /// </summary>
    public string AppName { get; }

    /// <summary>
    /// Gets the root namespace derived from <see cref="AppName" />.
    /// </summary>
    public string RootNamespace { get; }

    /// <summary>
    /// Gets the module names to materialize in the scaffold.
    /// </summary>
    public IReadOnlyList<string> Modules { get; }

    /// <summary>
    /// Gets the feature or slice names to materialize in the scaffold.
    /// </summary>
    public IReadOnlyList<string> Features { get; }

    /// <summary>
    /// Gets the target framework for generated projects.
    /// </summary>
    public string TargetFramework { get; }

    /// <summary>
    /// Gets the Cephalon package version written into the scaffold.
    /// </summary>
    public string CephalonPackageVersion { get; }

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    internal static string ToNamespace(string value)
    {
        var namespaceSegments = value
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(segment => ToIdentifier(segment, "Generated"))
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();

        return namespaceSegments.Length == 0
            ? "Generated"
            : string.Join('.', namespaceSegments);
    }

    internal static string ToIdentifier(string value, string fallback)
    {
        var words = value
            .Split(['.', '-', '_', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word =>
            {
                var letters = new string(word.Where(char.IsLetterOrDigit).ToArray());
                if (letters.Length == 0)
                {
                    return string.Empty;
                }

                return char.ToUpperInvariant(letters[0]) + letters[1..];
            })
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .ToArray();

        if (words.Length == 0)
        {
            return fallback;
        }

        var candidate = string.Concat(words);
        return char.IsDigit(candidate[0]) ? $"{fallback}{candidate}" : candidate;
    }

    internal static string ToSlug(string value, string fallback)
    {
        var words = value
            .Split(['.', '-', '_', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => new string(word.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant())
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .ToArray();

        return words.Length == 0 ? fallback : string.Join('-', words);
    }
}
