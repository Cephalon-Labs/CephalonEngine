using Cephalon.Abstractions.Modules;
using Cephalon.Engine.Configuration;

namespace Cephalon.Engine.Composition;

internal static class ModuleActivation
{
    public static List<IModule> ApplyOptions(
        IReadOnlyList<IModule> registeredModules,
        EngineOptions options)
    {
        if (!options.HasValues)
        {
            return registeredModules.ToList();
        }

        var modulesByType = registeredModules.ToDictionary(module => module.GetType());
        var activeModules = registeredModules
            .Where(module => options.IsModuleEnabled(module.Descriptor.Id))
            .ToList();
        var activeTypes = activeModules
            .Select(module => module.GetType())
            .ToHashSet();

        foreach (var module in activeModules)
        {
            foreach (var dependencyType in module.Descriptor.DependsOn)
            {
                if (activeTypes.Contains(dependencyType))
                {
                    continue;
                }

                if (modulesByType.TryGetValue(dependencyType, out var dependency))
                {
                    throw new InvalidOperationException(
                        $"Module '{module.Descriptor.Id}' depends on '{dependency.Descriptor.Id}', but that module is disabled by engine options.");
                }
            }
        }

        return activeModules;
    }
}
