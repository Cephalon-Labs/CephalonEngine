namespace Cephalon.Observability.DependencyHealth.Core.Configuration;

/// <summary>Base class for all dependency definitions that contribute to Cephalon runtime health.</summary>
public abstract class DependencyDefinitionBase
{
    /// <summary>Gets or sets the stable dependency identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable dependency name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets a value indicating whether this dependency is required for readiness.</summary>
    public bool Required { get; set; }

    /// <summary>Gets or sets the per-probe timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 5;
}
