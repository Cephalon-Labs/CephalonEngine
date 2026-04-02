namespace Cephalon.Abstractions.Localization;

/// <summary>
/// Contributes localized resources to the runtime localization catalog.
/// </summary>
public interface ILocalizedResourceContributor
{
    /// <summary>
    /// Registers the contributor's localized resources.
    /// </summary>
    /// <param name="resources">The registry that accepts localized resources.</param>
    void RegisterResources(ILocalizedResourceRegistry resources);
}
