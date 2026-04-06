using Cephalon.Abstractions.Localization;
using Cephalon.Abstractions.Modules;

namespace Cephalon.Tests.Support;

internal sealed class LocalizationPackTestModule : ModuleBase, ILocalizedResourceContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "localization-pack",
        displayName: "Localization Pack",
        description: "Adds language resources for installed package scenarios.",
        tags: ["foundation", "localization"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "foundation",
            ["surface"] = "localization"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public void RegisterResources(ILocalizedResourceRegistry resources)
    {
        resources.Add("es", new Dictionary<string, string>
        {
            ["engine.docs.rest.title"] = "API REST de Cephalon",
            ["engine.docs.rest.description"] = "Superficie REST expuesta por el host ASP.NET Core de Cephalon.",
            ["engine.docs.scalar.title"] = "Referencia REST de Cephalon"
        });
    }
}
