using System.IO.Compression;
using Cephalon.Tests.Support;

namespace Cephalon.Tests.Tooling;

public sealed class PackagePublishingTests
{
    [Fact]
    public void ReleasePackagePublishingScriptProducesOnlyTheIntendedArtifacts()
    {
        var scriptPath = RepositoryPaths.GetFile("scripts", "publish-package-artifacts.ps1");
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-package-artifacts-{Guid.NewGuid():N}");

        Directory.CreateDirectory(outputPath);

        try
        {
            var result = RunProcess(
                "powershell",
                $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -Configuration {GetCurrentBuildConfiguration()} -OutputPath \"{outputPath}\" -SkipBuild",
                workingDirectory: Path.GetDirectoryName(scriptPath)!);

            Assert.Equal(0, result.ExitCode);

            var packageFiles = Directory.GetFiles(outputPath, "*.nupkg", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => !name!.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.Contains(packageFiles, name => name!.StartsWith("Cephalon.Abstractions.", StringComparison.Ordinal));
            Assert.Contains(packageFiles, name => name!.StartsWith("Cephalon.Engine.", StringComparison.Ordinal));
            Assert.Contains(packageFiles, name => name!.StartsWith("Cephalon.ReferenceModule.Operations.", StringComparison.Ordinal));
            Assert.Contains(packageFiles, name => name!.StartsWith("Cephalon.TemplatePack.", StringComparison.Ordinal));

            Assert.DoesNotContain(packageFiles, name => name!.StartsWith("Cephalon.Benchmarks.", StringComparison.Ordinal));
            Assert.DoesNotContain(packageFiles, name => name!.StartsWith("Cephalon.Cli.", StringComparison.Ordinal));
            Assert.DoesNotContain(packageFiles, name => name!.StartsWith("Cephalon.WorkerPlayground.", StringComparison.Ordinal));
            Assert.DoesNotContain(packageFiles, name => name!.StartsWith("Cephalon.Sample.MicroserviceSuite.Governance.", StringComparison.Ordinal));
            Assert.DoesNotContain(packageFiles, name => name!.StartsWith("Cephalon.Sample.MicroserviceSuite.Shared.Foundation.", StringComparison.Ordinal));

            var manifestPath = Path.Combine(outputPath, "package-artifacts-manifest.json");
            Assert.True(File.Exists(manifestPath));

            var abstractionsPackagePath = Directory.GetFiles(outputPath, "Cephalon.Abstractions.*.nupkg", SearchOption.TopDirectoryOnly)
                .Single(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));

            using var package = ZipFile.OpenRead(abstractionsPackagePath);
            Assert.Contains(package.Entries, entry => entry.FullName.Equals("PACKAGE.md", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    [Fact]
    public void ExcludedProjectsDisablePackingByDefault()
    {
        foreach (var relativePath in new[]
        {
            Path.Combine("src", "Cephalon.Cli", "Cephalon.Cli.csproj"),
            Path.Combine("benchmarks", "Cephalon.Benchmarks", "Cephalon.Benchmarks.csproj"),
            Path.Combine("playground", "Cephalon.WorkerPlayground", "Cephalon.WorkerPlayground.csproj"),
            Path.Combine("samples", "Cephalon.Sample.MicroserviceSuite", "shared", "Cephalon.Sample.MicroserviceSuite.Governance", "Cephalon.Sample.MicroserviceSuite.Governance.csproj"),
            Path.Combine("samples", "Cephalon.Sample.MicroserviceSuite", "shared", "Cephalon.Sample.MicroserviceSuite.Shared.Foundation", "Cephalon.Sample.MicroserviceSuite.Shared.Foundation.csproj")
        })
        {
            var projectContents = File.ReadAllText(RepositoryPaths.GetFile(relativePath));
            Assert.Contains("<IsPackable>false</IsPackable>", projectContents, StringComparison.Ordinal);
        }
    }

    private static string GetCurrentBuildConfiguration()
    {
#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }

    private static ProcessResult RunProcess(string fileName, string arguments, string workingDirectory)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{fileName}'.");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, output, error);
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
