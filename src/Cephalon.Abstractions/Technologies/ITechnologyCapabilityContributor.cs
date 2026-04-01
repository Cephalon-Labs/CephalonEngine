using Cephalon.Abstractions.Capabilities;

namespace Cephalon.Abstractions.Technologies;

public interface ITechnologyCapabilityContributor
{
    void RegisterTechnologyCapabilities(
        ICapabilityRegistry capabilities,
        TechnologySelection technologies);
}
