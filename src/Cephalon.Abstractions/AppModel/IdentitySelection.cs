using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the active identity and authorization inputs resolved for a Cephalon app.
/// </summary>
public sealed class IdentitySelection
{
    /// <summary>
    /// Gets an empty identity-selection instance.
    /// </summary>
    public static IdentitySelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentitySelection" /> class.
    /// </summary>
    /// <param name="enabled">Whether identity and authorization support was explicitly enabled.</param>
    /// <param name="authorizationModes">The selected authorization modes.</param>
    [JsonConstructor]
    public IdentitySelection(
        bool? enabled = null,
        IReadOnlyList<string>? authorizationModes = null)
    {
        Enabled = enabled;
        AuthorizationModes = authorizationModes?
            .Where(mode => !string.IsNullOrWhiteSpace(mode))
            .Select(mode => mode.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    /// <summary>
    /// Gets a value indicating whether identity and authorization support was explicitly enabled.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the selected authorization modes.
    /// </summary>
    public IReadOnlyList<string> AuthorizationModes { get; }

    /// <summary>
    /// Gets a value indicating whether any identity-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Enabled.HasValue ||
        AuthorizationModes.Count > 0;
}
