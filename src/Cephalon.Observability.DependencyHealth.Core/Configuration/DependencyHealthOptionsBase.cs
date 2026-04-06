namespace Cephalon.Observability.DependencyHealth.Core.Configuration;

/// <summary>Base class for provider-specific dependency-health options.</summary>
/// <typeparam name="TDefinition">The provider-specific dependency definition type.</typeparam>
public abstract class DependencyHealthOptionsBase<TDefinition>
    where TDefinition : DependencyDefinitionBase
{
    /// <summary>Gets or sets the interval in seconds between background refresh attempts.</summary>
    public int RefreshIntervalSeconds { get; set; } = 30;

    /// <summary>Gets or sets the configured dependencies.</summary>
    public IReadOnlyList<TDefinition> Dependencies { get; set; } = Array.Empty<TDefinition>();

    /// <summary>Parses a boolean value, returning <paramref name="defaultValue"/> when absent or unparseable.</summary>
    protected static bool GetBoolean(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;

    /// <summary>Parses a nullable boolean value, returning <see langword="null"/> when absent or unparseable.</summary>
    protected static bool? GetNullableBoolean(string? value) =>
        bool.TryParse(value, out var parsed) ? parsed : null;

    /// <summary>Parses a positive integer value, returning <paramref name="defaultValue"/> when absent or non-positive.</summary>
    protected static int GetInt32(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
}
