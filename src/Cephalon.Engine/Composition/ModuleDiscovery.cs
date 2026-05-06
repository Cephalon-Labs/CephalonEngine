using System.Reflection;
using System.Runtime.CompilerServices;
using Cephalon.Abstractions.Modules;

namespace Cephalon.Engine.Composition;

internal static class ModuleDiscovery
{
    public static IReadOnlyList<IModule> DiscoverModules(
        IEnumerable<Assembly> assemblies,
        Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var discovered = new List<IModule>();
        var uniqueAssemblies = assemblies
            .GroupBy(GetAssemblyIdentity, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(assembly => GetAssemblyIdentity(assembly), StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in uniqueAssemblies)
        {
            foreach (var descriptor in GetGeneratedModuleDescriptors(assembly))
            {
                if (filter is not null && !filter(descriptor.ModuleType))
                {
                    continue;
                }

                discovered.Add(CreateModule(assembly, descriptor));
            }
        }

        return discovered;
    }

    public static IReadOnlyList<Assembly> ResolveAssemblies(IEnumerable<string> assemblyNames)
    {
        ArgumentNullException.ThrowIfNull(assemblyNames);

        return assemblyNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(ResolveAssembly)
            .ToArray();
    }

    private static Assembly ResolveAssembly(string assemblyName)
    {
        var loadedAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(assembly =>
                string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(assembly.FullName, assemblyName, StringComparison.OrdinalIgnoreCase));

        if (loadedAssembly is not null)
        {
            return loadedAssembly;
        }

        try
        {
            return Assembly.Load(new AssemblyName(assemblyName));
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Module discovery could not load assembly '{assemblyName}'.",
                exception);
        }
    }

    private static IReadOnlyList<ModuleDiscoveryDescriptor> GetGeneratedModuleDescriptors(Assembly assembly)
    {
        try
        {
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Module discovery could not initialize generated descriptors for assembly '{GetAssemblyIdentity(assembly)}'.",
                exception);
        }

        var descriptors = ModuleDiscoveryRegistry.GetDescriptors(assembly);
        if (descriptors.Count == 0)
        {
            throw new InvalidOperationException(
                $"Module discovery could not find generated descriptors for assembly '{GetAssemblyIdentity(assembly)}'. Reference Cephalon.Engine.SourceGen as an analyzer, rebuild the module assembly, or register modules explicitly with AddModule(...).");
        }

        return descriptors;
    }

    private static IModule CreateModule(Assembly assembly, ModuleDiscoveryDescriptor descriptor)
    {
        try
        {
            return descriptor.CreateModule();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Module discovery could not create '{descriptor.ModuleType.FullName}' from assembly '{GetAssemblyIdentity(assembly)}'. Generated descriptors require an accessible parameterless constructor; use explicit AddModule(...) registration for custom factories.",
                exception);
        }
    }

    private static string GetAssemblyIdentity(Assembly assembly)
    {
        var assemblyName = assembly.GetName();
        return assembly.FullName ?? assemblyName.Name ?? assemblyName.FullName ?? "unknown assembly";
    }
}
