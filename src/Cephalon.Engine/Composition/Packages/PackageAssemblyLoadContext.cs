using System.Reflection;
using System.Runtime.Loader;

namespace Cephalon.Engine.Composition.Packages;

internal sealed class PackageAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver resolver;
    private readonly string mainAssemblyPath;

    public PackageAssemblyLoadContext(string mainAssemblyPath)
        : base(
            name: $"package:{Path.GetFileNameWithoutExtension(mainAssemblyPath)}:{Guid.NewGuid():N}",
            isCollectible: false)
    {
        this.mainAssemblyPath = mainAssemblyPath ?? throw new ArgumentNullException(nameof(mainAssemblyPath));
        resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    public Assembly LoadMainAssembly()
    {
        return LoadFromAssemblyPath(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var sharedAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(assembly =>
                string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        if (sharedAssembly is not null)
        {
            return sharedAssembly;
        }

        var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath is null ? 0 : LoadUnmanagedDllFromPath(libraryPath);
    }
}
