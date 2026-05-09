using Cephalon.Engine.Configuration;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;

namespace Cephalon.Engine.Composition.Packages;

internal static class ModulePackageLoader
{
    private static readonly string CurrentEngineVersion = GetCurrentEngineVersion();
    private static readonly string CurrentTargetFramework = GetCurrentTargetFramework();

    public static LoadedPackage[] Load(
        IEnumerable<ModulePackageReference> packages,
        IEnumerable<ModulePackageDirectory>? packageDirectories = null,
        PackagePolicy? packagePolicy = null,
        TrustPolicy? trustPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(packages);
        packagePolicy ??= PackagePolicy.Default;
        trustPolicy ??= TrustPolicy.Default;

        var packageReferences = packages as IReadOnlyCollection<ModulePackageReference> ?? packages.ToArray();
        var directories = packageDirectories is null
            ? Array.Empty<ModulePackageDirectory>()
            : packageDirectories as IReadOnlyCollection<ModulePackageDirectory> ?? packageDirectories.ToArray();
        if (packageReferences.Count == 0 && directories.Count == 0)
        {
            return [];
        }

        EnsureDynamicPackageLoadingSupported();

        var requests = packageReferences
            .Select(ResolvePackage)
            .Concat(DiscoverPackages(directories))
            .ToArray();
        ValidateRequests(requests);

        return requests
            .Select(request => LoadPackage(request, packagePolicy, trustPolicy))
            .OrderBy(static package => package.Request.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void EnsureDynamicPackageLoadingSupported()
    {
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            return;
        }

        throw new InvalidOperationException(
            "Cephalon dynamic package loading requires runtime assembly loading and is not supported in Native AOT hosts. " +
            "Reference modules at compile time or disable Engine:Discovery:Packages and Engine:Discovery:PackageDirectories for Native AOT deployments.");
    }

    private static LoadedPackage LoadPackage(
        PackageLoadRequest request,
        PackagePolicy packagePolicy,
        TrustPolicy trustPolicy)
    {
        if (!File.Exists(request.ResolvedAssemblyPath))
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' could not be loaded because assembly path '{request.ResolvedAssemblyPath}' does not exist.");
        }

        if (!string.Equals(Path.GetExtension(request.ResolvedAssemblyPath), ".dll", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' must point to a .dll assembly path. Actual path: '{request.ResolvedAssemblyPath}'.");
        }

        ValidatePolicyRequirements(request, packagePolicy);
        ValidateCompatibility(request);
        var assemblyHash = ComputeSha256Hash(request.ResolvedAssemblyPath);
        var checksumSha256 = Convert.ToHexString(assemblyHash).ToLowerInvariant();
        ValidateIntegrity(request, checksumSha256);
        var signatureVerification = PackageSignatureVerifier.Verify(
            request,
            assemblyHash,
            trustPolicy);
        if (packagePolicy.RequireSignatureVerification && !signatureVerification.IsVerified)
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' must pass cryptographic signature verification because the current package policy requires it. {signatureVerification.Reason}");
        }

        try
        {
            var loadContext = new PackageAssemblyLoadContext(request.ResolvedAssemblyPath);
            var assembly = loadContext.LoadMainAssembly();
            var modules = ModuleDiscovery.DiscoverModules([assembly]).ToArray();

            if (modules.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Package '{request.Id}' did not expose any Cephalon modules in assembly '{request.ResolvedAssemblyPath}'.");
            }

            return new LoadedPackage(
                Request: request,
                Assembly: assembly,
                LoadContextName: loadContext.Name ?? "package-load-context",
                Modules: modules,
                ChecksumSha256: checksumSha256,
                SignatureVerification: signatureVerification);
        }
        catch (Exception exception) when (exception is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' could not be loaded from '{request.ResolvedAssemblyPath}'.",
                exception);
        }
    }

    private static PackageLoadRequest ResolvePackage(ModulePackageReference reference)
    {
        return reference.Kind switch
        {
            ModulePackageReference.AssemblyPathKind => ResolveAssemblyPathPackage(reference),
            ModulePackageReference.ManifestFileKind or ModulePackageReference.DirectoryManifestKind => ResolveManifestPackage(reference),
            _ => throw new InvalidOperationException($"Package kind '{reference.Kind}' is not supported.")
        };
    }

    private static PackageLoadRequest ResolveAssemblyPathPackage(ModulePackageReference reference)
    {
        var resolvedPath = ResolvePath(reference.Path);
        var packageId = reference.Id ?? Path.GetFileNameWithoutExtension(resolvedPath);

        return new PackageLoadRequest(
            id: packageId,
            kind: ModulePackageReference.AssemblyPathKind,
            resolvedSourcePath: resolvedPath,
            resolvedAssemblyPath: resolvedPath);
    }

    private static PackageLoadRequest ResolveManifestPackage(ModulePackageReference reference)
    {
        var resolvedManifestPath = ResolvePath(reference.Path);
        if (!File.Exists(resolvedManifestPath))
        {
            throw new InvalidOperationException(
                $"Package manifest '{resolvedManifestPath}' does not exist.");
        }

        var definition = ReadDefinition(resolvedManifestPath);
        var assemblyPath = definition.ResolveAssemblyPath();
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            throw new InvalidOperationException(
                $"Package manifest '{resolvedManifestPath}' must define an 'assembly' path.");
        }

        var packageId = reference.Id ??
            definition.Id?.Trim() ??
            Path.GetFileName(Path.GetDirectoryName(resolvedManifestPath)) ??
            Path.GetFileNameWithoutExtension(resolvedManifestPath);
        var basePath = Path.GetDirectoryName(resolvedManifestPath) ?? AppContext.BaseDirectory;
        var resolvedAssemblyPath = ResolvePath(assemblyPath, basePath);
        var signatures = definition.ResolveSignatures()
            .Select(static signature => new PackageSignatureLoadRequest(
                Type: string.IsNullOrWhiteSpace(signature.Type) ? null : signature.Type.Trim(),
                Signer: string.IsNullOrWhiteSpace(signature.Signer) ? null : signature.Signer.Trim(),
                KeyId: string.IsNullOrWhiteSpace(signature.KeyId) ? null : signature.KeyId.Trim(),
                Fingerprint: NormalizeChecksum(signature.Fingerprint),
                Algorithm: string.IsNullOrWhiteSpace(signature.Algorithm) ? null : signature.Algorithm.Trim(),
                Value: string.IsNullOrWhiteSpace(signature.Value) ? null : signature.Value.Trim()))
            .ToArray();

        return new PackageLoadRequest(
            id: packageId,
            kind: reference.Kind,
            resolvedSourcePath: resolvedManifestPath,
            resolvedAssemblyPath: resolvedAssemblyPath,
            version: definition.ResolveVersion(),
            minimumEngineVersion: definition.ResolveMinimumEngineVersion(),
            maximumEngineVersion: definition.ResolveMaximumEngineVersion(),
            supportedTargetFrameworks: definition.ResolveSupportedTargetFrameworks(),
            publisherId: definition.ResolvePublisherId(),
            publisherDisplayName: definition.ResolvePublisherDisplayName(),
            publisherWebsite: definition.ResolvePublisherWebsite(),
            distribution: definition.ResolveDistribution(),
            provenance: definition.ResolveProvenance(),
            dependencies: definition.ResolveDependencies(),
            signatures: signatures,
            expectedSha256: NormalizeChecksum(definition.ResolveSha256()));
    }

    private static PackageLoadRequest[] DiscoverPackages(IEnumerable<ModulePackageDirectory> directories)
    {
        return directories
            .SelectMany(DiscoverPackages)
            .ToArray();
    }

    private static IEnumerable<PackageLoadRequest> DiscoverPackages(ModulePackageDirectory directory)
    {
        var resolvedDirectoryPath = ResolvePath(directory.Path);
        if (!Directory.Exists(resolvedDirectoryPath))
        {
            throw new InvalidOperationException(
                $"Package directory '{resolvedDirectoryPath}' does not exist.");
        }

        var searchOption = directory.IncludeSubdirectories
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;
        var manifestPaths = Directory
            .EnumerateFiles(resolvedDirectoryPath, directory.ManifestFileName, searchOption)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (manifestPaths.Length == 0)
        {
            throw new InvalidOperationException(
                $"Package directory '{resolvedDirectoryPath}' did not contain any '{directory.ManifestFileName}' manifests.");
        }

        foreach (var manifestPath in manifestPaths)
        {
            yield return ResolveManifestPackage(new ModulePackageReference(
                manifestPath,
                kind: ModulePackageReference.DirectoryManifestKind));
        }
    }

    private static PackageDefinitionFile ReadDefinition(string manifestPath)
    {
        try
        {
            var json = File.ReadAllText(manifestPath);
            return JsonSerializer.Deserialize(json, PackageDefinitionFileJsonContext.Default.PackageDefinitionFile)
                ?? throw new InvalidOperationException(
                    $"Package manifest '{manifestPath}' could not be parsed.");
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            throw new InvalidOperationException(
                $"Package manifest '{manifestPath}' could not be read.",
                exception);
        }
    }

    private static string ResolvePath(string path, string? basePath = null)
    {
        var effectiveBasePath = string.IsNullOrWhiteSpace(basePath)
            ? AppContext.BaseDirectory
            : basePath;

        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(path, effectiveBasePath);
    }

    private static void ValidateRequests(PackageLoadRequest[] requests)
    {
        var duplicateIds = requests
            .GroupBy(static request => request.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1);

        if (duplicateIds is not null)
        {
            throw new InvalidOperationException(
                $"Package id '{duplicateIds.Key}' is registered multiple times.");
        }

        var duplicateAssemblyPaths = requests
            .GroupBy(static request => request.ResolvedAssemblyPath, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1);

        if (duplicateAssemblyPaths is not null)
        {
            throw new InvalidOperationException(
                $"Package assembly path '{duplicateAssemblyPaths.Key}' is registered multiple times.");
        }

        var duplicateSourcePaths = requests
            .GroupBy(static request => request.ResolvedSourcePath, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1);

        if (duplicateSourcePaths is not null)
        {
            throw new InvalidOperationException(
                $"Package source path '{duplicateSourcePaths.Key}' is registered multiple times.");
        }

        ValidateDependencyRequirements(requests);
    }

    private static void ValidateDependencyRequirements(PackageLoadRequest[] requests)
    {
        var requestsById = requests.ToDictionary(static request => request.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var request in requests)
        {
            var duplicateDependencyIds = request.Dependencies
                .GroupBy(static dependency => dependency.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(static group => group.Count() > 1);

            if (duplicateDependencyIds is not null)
            {
                throw new InvalidOperationException(
                    $"Package '{request.Id}' declares dependency '{duplicateDependencyIds.Key}' multiple times.");
            }

            foreach (var dependency in request.Dependencies)
            {
                ValidateDependencyRequirement(request, dependency, requestsById);
            }
        }
    }

    private static void ValidateDependencyRequirement(
        PackageLoadRequest request,
        PackageDependencyLoadRequest dependency,
        Dictionary<string, PackageLoadRequest> requestsById)
    {
        if (string.Equals(request.Id, dependency.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' cannot declare itself as a package dependency.");
        }

        if (!requestsById.TryGetValue(dependency.Id, out var resolvedDependency))
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' requires package dependency '{dependency.Id}', but that dependency was not registered.");
        }

        ValidateDependencyVersionRange(request, dependency, resolvedDependency);
    }

    private static void ValidateDependencyVersionRange(
        PackageLoadRequest request,
        PackageDependencyLoadRequest dependency,
        PackageLoadRequest resolvedDependency)
    {
        Version? minimumVersion = null;
        Version? maximumVersion = null;

        if (!string.IsNullOrWhiteSpace(dependency.MinimumVersion))
        {
            minimumVersion = ParseComparableVersion(
                dependency.MinimumVersion,
                $"package '{request.Id}' dependency '{dependency.Id}' minimumVersion '{dependency.MinimumVersion}'");
        }

        if (!string.IsNullOrWhiteSpace(dependency.MaximumVersion))
        {
            maximumVersion = ParseComparableVersion(
                dependency.MaximumVersion,
                $"package '{request.Id}' dependency '{dependency.Id}' maximumVersion '{dependency.MaximumVersion}'");
        }

        if (minimumVersion is not null && maximumVersion is not null && minimumVersion.CompareTo(maximumVersion) > 0)
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' declares an invalid version range for dependency '{dependency.Id}'. minimumVersion '{dependency.MinimumVersion}' must be less than or equal to maximumVersion '{dependency.MaximumVersion}'.");
        }

        if (!dependency.HasVersionRange)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(resolvedDependency.Version))
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' requires versioned dependency '{dependency.Id}', but package '{resolvedDependency.Id}' did not declare 'version' in cephalon.package.json.");
        }

        var dependencyVersion = ParseComparableVersion(
            resolvedDependency.Version,
            $"package '{resolvedDependency.Id}' version '{resolvedDependency.Version}'");

        if (minimumVersion is not null && dependencyVersion.CompareTo(minimumVersion) < 0)
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' requires dependency '{dependency.Id}' version '{dependency.MinimumVersion}' or newer, but the resolved package version is '{resolvedDependency.Version}'.");
        }

        if (maximumVersion is not null && dependencyVersion.CompareTo(maximumVersion) > 0)
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' requires dependency '{dependency.Id}' version '{dependency.MaximumVersion}' or older, but the resolved package version is '{resolvedDependency.Version}'.");
        }
    }

    private static void ValidateCompatibility(PackageLoadRequest request)
    {
        var currentVersion = ParseComparableVersion(
            CurrentEngineVersion,
            $"current engine version '{CurrentEngineVersion}'");

        if (!string.IsNullOrWhiteSpace(request.MinimumEngineVersion))
        {
            var minimumVersion = ParseComparableVersion(
                request.MinimumEngineVersion,
                $"package '{request.Id}' minimumEngineVersion '{request.MinimumEngineVersion}'");

            if (currentVersion.CompareTo(minimumVersion) < 0)
            {
                throw new InvalidOperationException(
                    $"Package '{request.Id}' requires engine version '{request.MinimumEngineVersion}' or newer, but the current engine version is '{CurrentEngineVersion}'.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.MaximumEngineVersion))
        {
            var maximumVersion = ParseComparableVersion(
                request.MaximumEngineVersion,
                $"package '{request.Id}' maximumEngineVersion '{request.MaximumEngineVersion}'");

            if (currentVersion.CompareTo(maximumVersion) > 0)
            {
                throw new InvalidOperationException(
                    $"Package '{request.Id}' supports engine version '{request.MaximumEngineVersion}' or older, but the current engine version is '{CurrentEngineVersion}'.");
            }
        }

        if (request.SupportedTargetFrameworks.Count == 0)
        {
            return;
        }

        var supportedTargetFrameworks = request.SupportedTargetFrameworks
            .Select(NormalizeTargetFramework)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (supportedTargetFrameworks.Length == 0)
        {
            return;
        }

        if (!supportedTargetFrameworks.Contains(CurrentTargetFramework, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' supports target frameworks [{string.Join(", ", supportedTargetFrameworks)}], but the current engine target framework is '{CurrentTargetFramework}'.");
        }
    }

    private static void ValidatePolicyRequirements(PackageLoadRequest request, PackagePolicy packagePolicy)
    {
        if (string.Equals(request.Kind, ModulePackageReference.AssemblyPathKind, StringComparison.OrdinalIgnoreCase))
        {
            if (!packagePolicy.AllowAssemblyPathPackages)
            {
                throw new InvalidOperationException(
                    $"Package '{request.Id}' was loaded from a raw assembly path, but the current package policy requires manifest-driven package loading.");
            }

            if (packagePolicy.RequiresManifestMetadata)
            {
                throw new InvalidOperationException(
                    $"Package '{request.Id}' was loaded from a raw assembly path and cannot satisfy the current package policy metadata requirements. Use a cephalon.package.json manifest instead.");
            }

            return;
        }

        if (packagePolicy.RequireVersion && string.IsNullOrWhiteSpace(request.Version))
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' must declare 'version' in cephalon.package.json because the current package policy requires it.");
        }

        if (packagePolicy.RequireMinimumEngineVersion && string.IsNullOrWhiteSpace(request.MinimumEngineVersion))
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' must declare 'compatibility.minimumEngineVersion' in cephalon.package.json because the current package policy requires it.");
        }

        if (packagePolicy.RequireMaximumEngineVersion && string.IsNullOrWhiteSpace(request.MaximumEngineVersion))
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' must declare 'compatibility.maximumEngineVersion' in cephalon.package.json because the current package policy requires it.");
        }

        if (packagePolicy.RequireSupportedTargetFrameworks && request.SupportedTargetFrameworks.Count == 0)
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' must declare 'compatibility.supportedTargetFrameworks' in cephalon.package.json because the current package policy requires it.");
        }

        if (packagePolicy.RequirePublisherId && string.IsNullOrWhiteSpace(request.PublisherId))
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' must declare 'publisher.id' in cephalon.package.json because the current package policy requires it.");
        }

        if (packagePolicy.RequireSignatureFingerprint && !request.HasSignatureFingerprint)
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' must declare 'signature.fingerprint' in cephalon.package.json because the current package policy requires it.");
        }

        if (packagePolicy.RequireSignatureKeyId && !request.HasSignatureKeyId)
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' must declare 'signature.keyId' in cephalon.package.json because the current package policy requires it.");
        }

        if (packagePolicy.RequireSignatureValue && !request.HasSignatureValue)
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' must declare 'signature.value' in cephalon.package.json because the current package policy requires it.");
        }

        if (packagePolicy.RequireIntegritySha256 && string.IsNullOrWhiteSpace(request.ExpectedSha256))
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' must declare 'integrity.sha256' in cephalon.package.json because the current package policy requires it.");
        }
    }

    private static void ValidateIntegrity(PackageLoadRequest request, string checksumSha256)
    {
        if (string.IsNullOrWhiteSpace(request.ExpectedSha256))
        {
            return;
        }

        if (!string.Equals(checksumSha256, request.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Package '{request.Id}' failed SHA-256 validation. Expected '{request.ExpectedSha256}', actual '{checksumSha256}'.");
        }
    }

    private static byte[] ComputeSha256Hash(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        return SHA256.HashData(stream);
    }

    private static Version ParseComparableVersion(string value, string displayName)
    {
        var normalizedValue = NormalizeComparableVersion(value);
        if (Version.TryParse(normalizedValue, out var parsedVersion))
        {
            return parsedVersion;
        }

        throw new InvalidOperationException(
            $"Could not parse {displayName} for package compatibility evaluation.");
    }

    private static string NormalizeComparableVersion(string value)
    {
        var trimmed = value.Trim();
        var separatorIndex = trimmed.IndexOfAny(['-', '+']);
        return separatorIndex >= 0
            ? trimmed[..separatorIndex]
            : trimmed;
    }

    private static string NormalizeChecksum(string? checksum)
    {
        if (string.IsNullOrWhiteSpace(checksum))
        {
            return string.Empty;
        }

        var normalized = checksum.Trim();
        if (normalized.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["sha256:".Length..];
        }

        return normalized.Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }

    private static string GetCurrentEngineVersion()
    {
        var assembly = typeof(ModulePackageLoader).Assembly;
        return assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }

    private static string GetCurrentTargetFramework()
    {
        var frameworkName = typeof(ModulePackageLoader)
            .Assembly
            .GetCustomAttribute<TargetFrameworkAttribute>()?
            .FrameworkName;

        return NormalizeTargetFramework(frameworkName);
    }

    private static string NormalizeTargetFramework(string? frameworkName)
    {
        if (string.IsNullOrWhiteSpace(frameworkName))
        {
            return "unknown";
        }

        var trimmed = frameworkName.Trim();
        const string netCorePrefix = ".NETCoreApp,Version=v";
        const string netStandardPrefix = ".NETStandard,Version=v";
        const string netFrameworkPrefix = ".NETFramework,Version=v";

        if (trimmed.StartsWith(netCorePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return "net" + trimmed[netCorePrefix.Length..];
        }

        if (trimmed.StartsWith(netStandardPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return "netstandard" + trimmed[netStandardPrefix.Length..];
        }

        if (trimmed.StartsWith(netFrameworkPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return "net" + trimmed[netFrameworkPrefix.Length..].Replace(".", string.Empty, StringComparison.Ordinal);
        }

        return trimmed;
    }
}
