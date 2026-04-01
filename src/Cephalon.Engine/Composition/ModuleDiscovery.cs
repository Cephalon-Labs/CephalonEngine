using System.Reflection;
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
            foreach (var moduleType in GetModuleTypes(assembly))
            {
                if (filter is not null && !filter(moduleType))
                {
                    continue;
                }

                discovered.Add(CreateModule(assembly, moduleType));
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

    private static Type[] GetModuleTypes(Assembly assembly)
    {
        try
        {
            return assembly.DefinedTypes
                .Select(typeInfo => typeInfo.AsType())
                .Where(IsCandidate)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
        }
        catch (ReflectionTypeLoadException exception)
        {
            var loaderMessages = exception.LoaderExceptions
                .Where(loaderException => loaderException is not null)
                .Select(loaderException => loaderException!.Message)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var suffix = loaderMessages.Length == 0
                ? string.Empty
                : $" Loader errors: {string.Join(" | ", loaderMessages)}";

            throw new InvalidOperationException(
                $"Module discovery failed while scanning assembly '{GetAssemblyIdentity(assembly)}'.{suffix}",
                exception);
        }
    }

    private static bool IsCandidate(Type type)
    {
        return type.IsClass &&
            !type.IsAbstract &&
            !type.ContainsGenericParameters &&
            typeof(IModule).IsAssignableFrom(type);
    }

    private static IModule CreateModule(Assembly assembly, Type moduleType)
    {
        try
        {
            return (IModule?)Activator.CreateInstance(moduleType, nonPublic: true)
                ?? throw new InvalidOperationException(
                    $"Module discovery could not create '{moduleType.FullName}' from assembly '{GetAssemblyIdentity(assembly)}'.");
        }
        catch (Exception exception) when (exception is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Module discovery could not create '{moduleType.FullName}' from assembly '{GetAssemblyIdentity(assembly)}'. Modules must provide a parameterless constructor.",
                exception);
        }
    }

    private static string GetAssemblyIdentity(Assembly assembly)
    {
        return assembly.FullName ?? assembly.GetName().Name ?? assembly.ManifestModule.Name;
    }
}
