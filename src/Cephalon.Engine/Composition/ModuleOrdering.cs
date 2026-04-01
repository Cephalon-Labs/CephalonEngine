using Cephalon.Abstractions.Modules;

namespace Cephalon.Engine.Composition;

internal static class ModuleOrdering
{
    public static List<IModule> Order(List<IModule> modules)
    {
        var ordered = new List<IModule>(modules.Count);
        var modulesByType = modules.ToDictionary(module => module.GetType());
        var visited = new HashSet<Type>();
        var activePath = new List<Type>();

        foreach (var module in modules)
        {
            Visit(module);
        }

        return ordered;

        void Visit(IModule module)
        {
            var moduleType = module.GetType();

            if (visited.Contains(moduleType))
            {
                return;
            }

            var cycleIndex = activePath.IndexOf(moduleType);
            if (cycleIndex >= 0)
            {
                var cycle = activePath
                    .Skip(cycleIndex)
                    .Append(moduleType)
                    .Select(type => type.Name);

                throw new InvalidOperationException(
                    $"Module dependency cycle detected: {string.Join(" -> ", cycle)}.");
            }

            activePath.Add(moduleType);

            foreach (var dependencyType in module.Descriptor.DependsOn)
            {
                if (!modulesByType.TryGetValue(dependencyType, out var dependency))
                {
                    throw new InvalidOperationException(
                        $"Module '{module.Descriptor.Id}' depends on '{dependencyType.Name}', but that module was not registered.");
                }

                Visit(dependency);
            }

            activePath.RemoveAt(activePath.Count - 1);
            visited.Add(moduleType);
            ordered.Add(module);
        }
    }
}
