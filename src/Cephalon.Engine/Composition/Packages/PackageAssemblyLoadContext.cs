using System.Diagnostics.CodeAnalysis;
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

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Dynamic package loading is an explicit Cephalon package boundary. The no-package deployment path does not require this path, and Native AOT hosts fail fast before loading packages.")]
    public Assembly LoadMainAssembly()
    {
        return LoadFromAssemblyPath(mainAssemblyPath);
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Dynamic package dependency loading is scoped to external package assemblies and remains outside the global trim/AOT claim.")]
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
