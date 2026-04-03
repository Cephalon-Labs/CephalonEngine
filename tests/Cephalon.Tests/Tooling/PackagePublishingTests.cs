using System.IO.Compression;
using System.Text.Json.Nodes;
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

            Assert.True(
                result.ExitCode == 0,
                $"publish-package-artifacts.ps1 failed with exit code {result.ExitCode}.{Environment.NewLine}Output:{Environment.NewLine}{result.Output}{Environment.NewLine}Error:{Environment.NewLine}{result.Error}");

            var packageFiles = Directory.GetFiles(outputPath, "*.nupkg", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => !name!.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.Contains(packageFiles, name => name!.StartsWith("Cephalon.Abstractions.", StringComparison.Ordinal));
            Assert.Contains(packageFiles, name => name!.StartsWith("Cephalon.Cli.", StringComparison.Ordinal));
            Assert.Contains(packageFiles, name => name!.StartsWith("Cephalon.Engine.", StringComparison.Ordinal));
            Assert.Contains(packageFiles, name => name!.StartsWith("Cephalon.ReferenceModule.Operations.", StringComparison.Ordinal));
            Assert.Contains(packageFiles, name => name!.StartsWith("Cephalon.TemplatePack.", StringComparison.Ordinal));

            Assert.DoesNotContain(packageFiles, name => name!.StartsWith("Cephalon.Benchmarks.", StringComparison.Ordinal));
            Assert.DoesNotContain(packageFiles, name => name!.StartsWith("Cephalon.WorkerPlayground.", StringComparison.Ordinal));
            Assert.DoesNotContain(packageFiles, name => name!.StartsWith("Cephalon.Sample.MicroserviceSuite.Governance.", StringComparison.Ordinal));
            Assert.DoesNotContain(packageFiles, name => name!.StartsWith("Cephalon.Sample.MicroserviceSuite.Shared.Foundation.", StringComparison.Ordinal));

            var manifestPath = Path.Combine(outputPath, "package-artifacts-manifest.json");
            Assert.True(File.Exists(manifestPath));
            var checksumPath = Path.Combine(outputPath, "package-artifacts.sha256");
            Assert.True(File.Exists(checksumPath));

            var manifest = JsonNode.Parse(File.ReadAllText(manifestPath));
            var manifestObject = Assert.IsType<JsonObject>(manifest);
            Assert.False(string.IsNullOrWhiteSpace(manifestObject["SourceRepository"]?.GetValue<string>()));
            Assert.False(string.IsNullOrWhiteSpace(manifestObject["SourceRevision"]?.GetValue<string>()));
            Assert.Equal("package-artifacts.sha256", manifestObject["ChecksumFile"]?.GetValue<string>());

            var artifacts = Assert.IsType<JsonArray>(manifestObject["Artifacts"]);
            var cliArtifact = artifacts
                .Select(node => Assert.IsType<JsonObject>(node))
                .Single(artifact => artifact["Project"]?.GetValue<string>() == "src/Cephalon.Cli/Cephalon.Cli.csproj");

            Assert.Equal("dotnet-tool", cliArtifact["PackageKind"]?.GetValue<string>());

            var cliPackageFile = Assert.IsType<JsonArray>(cliArtifact["PackageFiles"])
                .Select(node => Assert.IsType<JsonObject>(node))
                .Single(file => file["FileName"]?.GetValue<string>() is string name && name.StartsWith("Cephalon.Cli.", StringComparison.Ordinal));

            Assert.Equal(cliPackageFile["FileName"]?.GetValue<string>(), cliPackageFile["Path"]?.GetValue<string>());
            var cliPackagePath = Path.Combine(outputPath, cliPackageFile["FileName"]!.GetValue<string>());
            var expectedCliHash = ComputeSha256(cliPackagePath);
            Assert.Equal(expectedCliHash, cliPackageFile["Sha256"]?.GetValue<string>());
            Assert.True(cliPackageFile["SizeBytes"]!.GetValue<long>() > 0);

            var checksumContents = File.ReadAllText(checksumPath);
            Assert.Contains($"{expectedCliHash} *{Path.GetFileName(cliPackagePath)}", checksumContents, StringComparison.Ordinal);

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

    [Fact]
    public void CliPackageIsPackedAsADotNetToolAndRunsHelpFromALocalInstall()
    {
        var repositoryRoot = RepositoryPaths.GetRepositoryRoot();
        var projectPath = RepositoryPaths.GetFile("src", "Cephalon.Cli", "Cephalon.Cli.csproj");
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-tool-package-{Guid.NewGuid():N}");
        var toolPath = Path.Combine(Path.GetTempPath(), $"cephalon-cli-tool-install-{Guid.NewGuid():N}");

        Directory.CreateDirectory(outputPath);
        Directory.CreateDirectory(toolPath);

        try
        {
            var packResult = RunProcess(
                "dotnet",
                $"pack \"{projectPath}\" -c {GetCurrentBuildConfiguration()} -o \"{outputPath}\" --no-build",
                workingDirectory: repositoryRoot);

            Assert.Equal(0, packResult.ExitCode);

            var packagePath = Directory.GetFiles(outputPath, "Cephalon.Cli.*.nupkg", SearchOption.TopDirectoryOnly)
                .Single(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));

            using (var package = ZipFile.OpenRead(packagePath))
            {
                Assert.Contains(package.Entries, entry => entry.FullName.Equals("PACKAGE.md", StringComparison.OrdinalIgnoreCase));
                Assert.Contains(package.Entries, entry => entry.FullName.EndsWith("DotnetToolSettings.xml", StringComparison.OrdinalIgnoreCase));
            }

            var installResult = RunProcess(
                "dotnet",
                $"tool install --tool-path \"{toolPath}\" Cephalon.Cli --add-source \"{outputPath}\" --ignore-failed-sources --no-cache",
                workingDirectory: repositoryRoot);

            Assert.Equal(0, installResult.ExitCode);

            var executablePath = Path.Combine(toolPath, OperatingSystem.IsWindows() ? "cephalon.exe" : "cephalon");
            Assert.True(File.Exists(executablePath), $"Expected installed CLI tool at '{executablePath}'.");

            var helpResult = RunProcess(
                executablePath,
                "--help",
                workingDirectory: repositoryRoot);

            Assert.Equal(0, helpResult.ExitCode);
            Assert.Contains("Cephalon CLI", helpResult.Output, StringComparison.Ordinal);
            Assert.Contains("Usage:", helpResult.Output, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }

            if (Directory.Exists(toolPath))
            {
                Directory.Delete(toolPath, recursive: true);
            }
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

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(stream)).ToLowerInvariant();
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
