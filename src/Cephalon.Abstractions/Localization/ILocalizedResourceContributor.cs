namespace Cephalon.Abstractions.Localization;

public interface ILocalizedResourceContributor
{
    void RegisterResources(ILocalizedResourceRegistry resources);
}
