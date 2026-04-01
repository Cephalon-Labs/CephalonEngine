using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.Json;

namespace Cephalon.ReferenceDocs.Generation;

internal sealed class DocumentationLoadContext : AssemblyLoadContext, IDisposable
{
    private readonly Dictionary<string, string> _assemblyPaths;
    private readonly IReadOnlyList<AssemblyDependencyResolver> _dependencyResolvers;

    private DocumentationLoadContext(
        Dictionary<string, string> assemblyPaths,
        IReadOnlyList<AssemblyDependencyResolver> dependencyResolvers)
        : base(nameof(DocumentationLoadContext), isCollectible: true)
    {
        _assemblyPaths = assemblyPaths;
        _dependencyResolvers = dependencyResolvers;
    }

    internal static DocumentationLoadContext Create(
        ReferenceDocsRequest request,
        IReadOnlyList<string> rootAssemblyNames)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(rootAssemblyNames);

        var assemblyPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in EnumerateSearchDirectories(request))
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var assemblyPath in Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                assemblyPaths.TryAdd(assemblyName, assemblyPath);
            }
        }

        foreach (var rootAssemblyName in rootAssemblyNames)
        {
            AddPackageAssembliesFromDependencyManifest(request, rootAssemblyName, assemblyPaths);
        }

        var dependencyResolvers = rootAssemblyNames
            .Select(assemblyName => Path.Combine(
                request.RootPath,
                "src",
                assemblyName,
                "bin",
                request.Configuration,
                request.TargetFramework,
                $"{assemblyName}.dll"))
            .Where(File.Exists)
            .Select(static assemblyPath => new AssemblyDependencyResolver(assemblyPath))
            .ToArray();

        return new DocumentationLoadContext(assemblyPaths, dependencyResolvers);
    }

    internal Assembly LoadAssembly(string assemblyPath)
    {
        return LoadFromAssemblyPath(assemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var defaultAssembly = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(
            candidate => string.Equals(candidate.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
        if (defaultAssembly is not null)
        {
            return defaultAssembly;
        }

        foreach (var resolver in _dependencyResolvers)
        {
            var resolvedPath = resolver.ResolveAssemblyToPath(assemblyName);
            if (!string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath))
            {
                return LoadFromAssemblyPath(resolvedPath);
            }
        }

        if (!string.IsNullOrWhiteSpace(assemblyName.Name) &&
            _assemblyPaths.TryGetValue(assemblyName.Name, out var assemblyPath))
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    public void Dispose()
    {
        Unload();
        GC.SuppressFinalize(this);
    }

    private static IEnumerable<string> EnumerateSearchDirectories(ReferenceDocsRequest request)
    {
        yield return Path.Combine(request.RootPath, "src", "Cephalon.ReferenceDocs", "bin", request.Configuration, request.TargetFramework);

        foreach (var projectDirectory in Directory.EnumerateDirectories(Path.Combine(request.RootPath, "src")))
        {
            yield return Path.Combine(projectDirectory, "bin", request.Configuration, request.TargetFramework);
        }

        foreach (var sharedFrameworkDirectory in EnumerateSharedFrameworkDirectories())
        {
            yield return sharedFrameworkDirectory;
        }
    }

    private static void AddPackageAssembliesFromDependencyManifest(
        ReferenceDocsRequest request,
        string rootAssemblyName,
        Dictionary<string, string> assemblyPaths)
    {
        var depsPath = Path.Combine(
            request.RootPath,
            "src",
            rootAssemblyName,
            "bin",
            request.Configuration,
            request.TargetFramework,
            $"{rootAssemblyName}.deps.json");
        if (!File.Exists(depsPath))
        {
            return;
        }

        using var stream = File.OpenRead(depsPath);
        using var document = JsonDocument.Parse(stream);

        if (!document.RootElement.TryGetProperty("libraries", out var librariesElement) ||
            !document.RootElement.TryGetProperty("targets", out var targetsElement))
        {
            return;
        }

        var packageRoots = BuildPackagePathMap(librariesElement);
        var target = targetsElement.EnumerateObject().FirstOrDefault().Value;
        if (target.ValueKind is JsonValueKind.Undefined)
        {
            return;
        }

        var packagesRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (string.IsNullOrWhiteSpace(packagesRoot))
        {
            packagesRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget",
                "packages");
        }

        foreach (var library in target.EnumerateObject())
        {
            if (!packageRoots.TryGetValue(library.Name, out var packageRoot) ||
                !library.Value.TryGetProperty("runtime", out var runtimeElement))
            {
                continue;
            }

            foreach (var runtimeAsset in runtimeElement.EnumerateObject())
            {
                if (!runtimeAsset.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fullPath = Path.Combine(
                    packagesRoot,
                    NormalizeRelativePath(packageRoot),
                    NormalizeRelativePath(runtimeAsset.Name));
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                var assemblyName = Path.GetFileNameWithoutExtension(runtimeAsset.Name);
                assemblyPaths.TryAdd(assemblyName, fullPath);
            }
        }
    }

    private static Dictionary<string, string> BuildPackagePathMap(JsonElement librariesElement)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var library in librariesElement.EnumerateObject())
        {
            if (library.Value.TryGetProperty("path", out var pathElement) &&
                pathElement.ValueKind is JsonValueKind.String)
            {
                var path = pathElement.GetString();
                if (!string.IsNullOrWhiteSpace(path))
                {
                    result[library.Name] = path;
                }
            }
        }

        return result;
    }

    private static string NormalizeRelativePath(string value)
    {
        return value.Replace('/', Path.DirectorySeparatorChar);
    }

    private static IEnumerable<string> EnumerateSharedFrameworkDirectories()
    {
        var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
        if (!string.IsNullOrWhiteSpace(runtimeDirectory))
        {
            yield return runtimeDirectory;
        }

        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (string.IsNullOrWhiteSpace(dotnetRoot) && !string.IsNullOrWhiteSpace(runtimeDirectory))
        {
            dotnetRoot = Path.GetFullPath(Path.Combine(runtimeDirectory, "..", "..", ".."));
        }

        if (string.IsNullOrWhiteSpace(dotnetRoot))
        {
            yield break;
        }

        foreach (var frameworkDirectory in EnumerateSharedFrameworkFamily(dotnetRoot, "Microsoft.AspNetCore.App"))
        {
            yield return frameworkDirectory;
        }
    }

    private static IEnumerable<string> EnumerateSharedFrameworkFamily(string dotnetRoot, string frameworkName)
    {
        var frameworkRoot = Path.Combine(dotnetRoot, "shared", frameworkName);
        if (!Directory.Exists(frameworkRoot))
        {
            yield break;
        }

        foreach (var versionDirectory in Directory.EnumerateDirectories(frameworkRoot))
        {
            yield return versionDirectory;
        }
    }
}
