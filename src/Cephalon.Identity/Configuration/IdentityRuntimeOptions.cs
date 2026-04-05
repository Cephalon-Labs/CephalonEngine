using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Identity.Configuration;

/// <summary>
/// Describes host-agnostic runtime options for the Cephalon identity companion pack.
/// </summary>
public sealed class IdentityRuntimeOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityRuntimeOptions" /> class.
    /// </summary>
    /// <param name="enableDefaultEvaluator">
    /// Whether the built-in metadata-driven <c>IAuthorizationEvaluator</c> should stay active.
    /// </param>
    /// <param name="enableRuntimeSurface">
    /// Whether the companion pack should project identity and authorization runtime metadata through the
    /// shared technology surface set.
    /// </param>
    /// <param name="requireExplicitPolicy">
    /// Whether callers must provide an explicit <c>AuthorizationContext.PolicyId</c> when using the built-in
    /// metadata-driven evaluator.
    /// </param>
    public IdentityRuntimeOptions(
        bool enableDefaultEvaluator = true,
        bool enableRuntimeSurface = true,
        bool requireExplicitPolicy = true)
    {
        EnableDefaultEvaluator = enableDefaultEvaluator;
        EnableRuntimeSurface = enableRuntimeSurface;
        RequireExplicitPolicy = requireExplicitPolicy;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the built-in metadata-driven authorization evaluator is active.
    /// </summary>
    public bool EnableDefaultEvaluator { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the pack should publish a runtime surface under
    /// <c>identity-access</c>.
    /// </summary>
    public bool EnableRuntimeSurface { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether authorization calls must provide an explicit policy identifier.
    /// </summary>
    public bool RequireExplicitPolicy { get; set; } = true;

    /// <summary>
    /// Reads identity runtime options from configuration.
    /// </summary>
    /// <param name="configuration">The root configuration that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed identity runtime options.</returns>
    public static IdentityRuntimeOptions FromConfiguration(
        IConfiguration? configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        if (configuration is null)
        {
            return new IdentityRuntimeOptions();
        }

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Identity");

        return new IdentityRuntimeOptions(
            enableDefaultEvaluator: ParseBoolean(section["EnableDefaultEvaluator"], defaultValue: true),
            enableRuntimeSurface: ParseBoolean(section["EnableRuntimeSurface"], defaultValue: true),
            requireExplicitPolicy: ParseBoolean(section["RequireExplicitPolicy"], defaultValue: true));
    }

    private static bool ParseBoolean(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return bool.TryParse(value.Trim(), out var parsed)
            ? parsed
            : defaultValue;
    }
}
