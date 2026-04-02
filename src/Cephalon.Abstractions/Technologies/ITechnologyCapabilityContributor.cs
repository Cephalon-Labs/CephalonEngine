using Cephalon.Abstractions.Capabilities;

namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Contributes capabilities when specific technology profiles are active.
/// </summary>
public interface ITechnologyCapabilityContributor
{
    /// <summary>
    /// Registers capabilities for the active technology selection.
    /// </summary>
    /// <param name="capabilities">The capability registry receiving technology capabilities.</param>
    /// <param name="technologies">The active technology selection.</param>
    void RegisterTechnologyCapabilities(
        ICapabilityRegistry capabilities,
        TechnologySelection technologies);
}
