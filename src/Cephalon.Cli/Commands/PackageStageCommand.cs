using Cephalon.Cli.Console;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cephalon.Cli.Commands;

/// <summary>
/// Implements the <c>cephalon package stage</c> command.
/// </summary>
internal static class PackageStageCommand
{
    private const string DefaultTargetFramework = "net10.0";
    private const string ManifestFileName = "cephalon.package.json";
    private const string PackageReadmeFileName = "PACKAGE.md";

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Executes the package stage command with the supplied options.
    /// </summary>
    /// <param name="options">The parsed command options.</param>
    /// <param name="console">The console abstraction used for user-facing output.</param>
    /// <param name="cancellationToken">A token that can cancel command execution.</param>
    /// <returns>The process exit code.</returns>
    internal static async Task<int> RunAsync(
        PackageStageOptions options,
        CliConsole console,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(console);

        var result = Stage(options, cancellationToken);

        await console.WriteOutputAsync(
            $"Staged package '{result.PackageId}' to '{result.OutputPath}'.",
            cancellationToken);
        await console.WriteOutputAsync(
            $"Target framework: {result.TargetFramework}",
            cancellationToken);
        await console.WriteOutputAsync(
            $"Manifest: {Path.Combine(result.OutputPath, ManifestFileName)}",
            cancellationToken);
        await console.WriteOutputAsync(
            $"Assembly: {Path.Combine(result.OutputPath, ConvertToCurrentPlatformPath(result.AssemblyRelativePath))}",
            cancellationToken);
        await console.WriteOutputAsync(
            "Next step: point Engine:Discovery:PackageDirectories or Engine:Discovery:Packages:ManifestPath at the staged directory.",
            cancellationToken);
        return 0;
    }

    /// <summary>
    /// Parses raw command-line arguments into a <see cref="PackageStageOptions" /> instance.
    /// </summary>
    /// <param name="args">The raw arguments that follow the <c>package stage</c> command.</param>
    /// <param name="options">When this method returns, contains the parsed options if parsing succeeded.</param>
    /// <param name="error">When this method returns, contains the parse error if parsing failed.</param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    internal static bool TryParse(
        string[] args,
        out PackageStageOptions? options,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);

        options = null;
        error = null;

        string? packagePath = null;
        string? outputPath = null;
        var targetFramework = DefaultTargetFramework;
        var force = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            switch (argument)
            {
                case "--package":
                    if (!TryReadValue(args, ref index, out var packageValue, out error))
                    {
                        return false;
                    }

                    packagePath = Path.GetFullPath(packageValue!);
                    break;
                case "--output":
                    if (!TryReadValue(args, ref index, out var outputValue, out error))
                    {
                        return false;
                    }

                    outputPath = Path.GetFullPath(outputValue!);
                    break;
                case "--target-framework":
                    if (!TryReadValue(args, ref index, out var targetFrameworkValue, out error))
                    {
                        return false;
                    }

                    targetFramework = targetFrameworkValue!;
                    break;
                case "--force":
                    force = true;
                    break;
                default:
                    error = $"Unknown option '{argument}'.";
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(packagePath))
        {
            error = "Option '--package <path>' is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            error = "Option '--output <path>' is required.";
            return false;
        }

        options = new PackageStageOptions
        {
            PackagePath = packagePath,
            OutputPath = outputPath,
            TargetFramework = targetFramework,
            Force = force
        };
        return true;
    }

    private static StageResult Stage(PackageStageOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(options.PackagePath))
        {
            throw new InvalidOperationException(
                $"Package '{options.PackagePath}' does not exist.");
        }

        if (!string.Equals(Path.GetExtension(options.PackagePath), ".nupkg", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Package '{options.PackagePath}' must point to a published '.nupkg' artifact.");
        }

        EnsureOutputDirectoryIsWritable(options.OutputPath, options.Force);

        var extractionPath = Path.Combine(Path.GetTempPath(), $"cephalon-package-stage-{Guid.NewGuid():N}");
        Directory.CreateDirectory(extractionPath);

        try
        {
            ZipFile.ExtractToDirectory(options.PackagePath, extractionPath);

            var manifestPath = ResolveManifestPath(extractionPath, options.TargetFramework);
            var libraryDirectory = ResolveLibraryDirectory(extractionPath, options.TargetFramework);
            var packageReadmePath = Path.Combine(extractionPath, PackageReadmeFileName);

            Directory.CreateDirectory(options.OutputPath);
            CopyDirectoryContents(libraryDirectory, options.OutputPath);

            if (File.Exists(packageReadmePath))
            {
                File.Copy(
                    packageReadmePath,
                    Path.Combine(options.OutputPath, PackageReadmeFileName),
                    overwrite: true);
            }

            var manifest = ReadManifest(manifestPath);
            var packageId = manifest["id"]?.GetValue<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new InvalidOperationException(
                    $"Package manifest '{manifestPath}' must declare an 'id'.");
            }

            var declaredAssemblyPath = manifest["assembly"]?.GetValue<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(declaredAssemblyPath))
            {
                throw new InvalidOperationException(
                    $"Package manifest '{manifestPath}' must declare an 'assembly' path.");
            }

            var stagedAssemblyRelativePath = ResolveStagedAssemblyRelativePath(
                options.OutputPath,
                declaredAssemblyPath);
            manifest["assembly"] = stagedAssemblyRelativePath;

            var stagedManifestPath = Path.Combine(options.OutputPath, ManifestFileName);
            File.WriteAllText(
                stagedManifestPath,
                manifest.ToJsonString(ManifestJsonOptions) + Environment.NewLine);

            return new StageResult(
                PackageId: packageId,
                OutputPath: options.OutputPath,
                TargetFramework: options.TargetFramework,
                AssemblyRelativePath: stagedAssemblyRelativePath);
        }
        finally
        {
            if (Directory.Exists(extractionPath))
            {
                Directory.Delete(extractionPath, recursive: true);
            }
        }
    }

    private static void EnsureOutputDirectoryIsWritable(string outputPath, bool force)
    {
        if (!Directory.Exists(outputPath))
        {
            return;
        }

        if (!Directory.EnumerateFileSystemEntries(outputPath).Any())
        {
            return;
        }

        if (!force)
        {
            throw new InvalidOperationException(
                $"Output directory '{outputPath}' already exists and is not empty. Use '--force' to replace it.");
        }

        Directory.Delete(outputPath, recursive: true);
    }

    private static string ResolveManifestPath(string extractionPath, string targetFramework)
    {
        var contentManifestPath = Path.Combine(extractionPath, "content", ManifestFileName);
        if (File.Exists(contentManifestPath))
        {
            return contentManifestPath;
        }

        var contentFilesManifestPath = Path.Combine(
            extractionPath,
            "contentFiles",
            "any",
            targetFramework,
            ManifestFileName);
        if (File.Exists(contentFilesManifestPath))
        {
            return contentFilesManifestPath;
        }

        var discoveredPaths = Directory
            .EnumerateFiles(extractionPath, ManifestFileName, SearchOption.AllDirectories)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (discoveredPaths.Length == 1)
        {
            return discoveredPaths[0];
        }

        throw new InvalidOperationException(
            discoveredPaths.Length == 0
                ? $"Package did not contain '{ManifestFileName}' in a supported location."
                : $"Package contained multiple '{ManifestFileName}' files and the stage command could not choose one automatically.");
    }

    private static string ResolveLibraryDirectory(string extractionPath, string targetFramework)
    {
        var libraryDirectory = Path.Combine(extractionPath, "lib", targetFramework);
        if (Directory.Exists(libraryDirectory))
        {
            return libraryDirectory;
        }

        var libRoot = Path.Combine(extractionPath, "lib");
        var availableFrameworks = Directory.Exists(libRoot)
            ? Directory.GetDirectories(libRoot)
                .Select(Path.GetFileName)
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray()
            : [];

        var availableFrameworksMessage = availableFrameworks.Length == 0
            ? "No lib/<tfm> directories were found."
            : $"Available target frameworks: {string.Join(", ", availableFrameworks)}.";

        throw new InvalidOperationException(
            $"Package does not contain a 'lib/{targetFramework}' directory. {availableFrameworksMessage}");
    }

    private static void CopyDirectoryContents(string sourceDirectory, string destinationDirectory)
    {
        foreach (var filePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, filePath);
            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            var destinationParent = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationParent))
            {
                Directory.CreateDirectory(destinationParent);
            }

            File.Copy(filePath, destinationPath, overwrite: true);
        }
    }

    private static JsonObject ReadManifest(string manifestPath)
    {
        var parsed = JsonNode.Parse(File.ReadAllText(manifestPath));
        return parsed as JsonObject
            ?? throw new InvalidOperationException(
                $"Package manifest '{manifestPath}' could not be parsed as a JSON object.");
    }

    private static string ResolveStagedAssemblyRelativePath(string outputPath, string declaredAssemblyPath)
    {
        var declaredRelativePath = NormalizeRelativePath(declaredAssemblyPath);
        var declaredOutputPath = Path.Combine(outputPath, ConvertToCurrentPlatformPath(declaredRelativePath));
        if (File.Exists(declaredOutputPath))
        {
            return declaredRelativePath;
        }

        var assemblyFileName = Path.GetFileName(ConvertToCurrentPlatformPath(declaredRelativePath));
        if (string.IsNullOrWhiteSpace(assemblyFileName))
        {
            throw new InvalidOperationException(
                $"Declared package assembly path '{declaredAssemblyPath}' is invalid.");
        }

        var matches = Directory
            .EnumerateFiles(outputPath, assemblyFileName, SearchOption.AllDirectories)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return matches.Length switch
        {
            1 => NormalizeRelativePath(Path.GetRelativePath(outputPath, matches[0])),
            0 => throw new InvalidOperationException(
                $"Staged package output did not contain the declared assembly '{assemblyFileName}'."),
            _ => throw new InvalidOperationException(
                $"Staged package output contained multiple assemblies named '{assemblyFileName}', so the manifest assembly path must be set explicitly before staging.")
        };
    }

    private static string NormalizeRelativePath(string path)
    {
        return path
            .Replace('\\', '/')
            .Replace("//", "/", StringComparison.Ordinal);
    }

    private static string ConvertToCurrentPlatformPath(string path)
    {
        return path
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
    }

    private static bool TryReadValue(
        string[] args,
        ref int index,
        out string? value,
        out string? error)
    {
        if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
        {
            value = null;
            error = $"Option '{args[index]}' requires a value.";
            return false;
        }

        index++;
        value = args[index].Trim();
        error = null;
        return true;
    }

    private sealed record StageResult(
        string PackageId,
        string OutputPath,
        string TargetFramework,
        string AssemblyRelativePath);
}
