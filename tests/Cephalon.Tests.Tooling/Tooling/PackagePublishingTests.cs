using System.IO.Compression;
using System.Text.Json.Nodes;
using Cephalon.Tests.Support;

namespace Cephalon.Tests.Tooling;

[Collection(ToolingProcessCollectionDefinition.Name)]
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
                GetPowerShellExecutable(),
                $"-File \"{scriptPath}\" -Configuration {GetCurrentBuildConfiguration()} -OutputPath \"{outputPath}\" -SkipBuild",
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
            Assert.Contains(packageFiles, name => name!.StartsWith("Cephalon.EventSourcing.", StringComparison.Ordinal));
            Assert.Contains(packageFiles, name => name!.StartsWith("Cephalon.EventSourcing.EntityFramework.", StringComparison.Ordinal));
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
                .Single(file => file["FileName"]?.GetValue<string>() is string name &&
                    name.StartsWith("Cephalon.Cli.", StringComparison.Ordinal) &&
                    name.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase) &&
                    !name.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));

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
    public void ReleasePackagePublishingScriptPreservesExistingReadmeInOutputPath()
    {
        var scriptPath = RepositoryPaths.GetFile("scripts", "publish-package-artifacts.ps1");
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-package-artifacts-preserve-readme-{Guid.NewGuid():N}");
        var readmePath = Path.Combine(outputPath, "README.md");
        var abstractionsProject = RepositoryPaths.GetFile("src", "Cephalon.Abstractions", "Cephalon.Abstractions.csproj");

        Directory.CreateDirectory(outputPath);
        File.WriteAllText(readmePath, """
            # Cephalon local package feed

            Keep this README in place when package artifacts are refreshed into the generated app feed.
            """);

        try
        {
            var result = RunProcess(
                GetPowerShellExecutable(),
                $"-File \"{scriptPath}\" -Configuration {GetCurrentBuildConfiguration()} -OutputPath \"{outputPath}\" -SkipBuild -ProjectPaths \"{abstractionsProject}\"",
                workingDirectory: Path.GetDirectoryName(scriptPath)!);

            Assert.True(
                result.ExitCode == 0,
                $"publish-package-artifacts.ps1 failed with exit code {result.ExitCode}.{Environment.NewLine}Output:{Environment.NewLine}{result.Output}{Environment.NewLine}Error:{Environment.NewLine}{result.Error}");

            Assert.True(File.Exists(readmePath), "Expected the existing README to stay in the output path.");
            var readmeContents = File.ReadAllText(readmePath);
            Assert.Contains("Cephalon local package feed", readmeContents, StringComparison.Ordinal);
            Assert.Contains(
                Directory.GetFiles(outputPath, "*.nupkg", SearchOption.TopDirectoryOnly),
                path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));
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
    public void ReleasePackagePublishingScriptCanFocusOnExplicitProjectPaths()
    {
        var scriptPath = RepositoryPaths.GetFile("scripts", "publish-package-artifacts.ps1");
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-package-artifacts-focused-{Guid.NewGuid():N}");
        var abstractionsProject = RepositoryPaths.GetFile("src", "Cephalon.Abstractions", "Cephalon.Abstractions.csproj");
        var cliProject = RepositoryPaths.GetFile("src", "Cephalon.Cli", "Cephalon.Cli.csproj");

        Directory.CreateDirectory(outputPath);

        try
        {
            var abstractionsBuildResult = RunProcess(
                "dotnet",
                $"build \"{abstractionsProject}\" -c {GetCurrentBuildConfiguration()} --no-restore -m:1 /p:UseSharedCompilation=false",
                workingDirectory: RepositoryPaths.GetRepositoryRoot());

            Assert.Equal(0, abstractionsBuildResult.ExitCode);

            var cliBuildResult = RunProcess(
                "dotnet",
                $"build \"{cliProject}\" -c {GetCurrentBuildConfiguration()} --no-restore -m:1 /p:UseSharedCompilation=false",
                workingDirectory: RepositoryPaths.GetRepositoryRoot());

            Assert.Equal(0, cliBuildResult.ExitCode);

            var result = RunProcess(
                GetPowerShellExecutable(),
                $"-File \"{scriptPath}\" -Configuration {GetCurrentBuildConfiguration()} -OutputPath \"{outputPath}\" -SkipBuild -ProjectPaths \"{abstractionsProject}\",\"{cliProject}\"",
                workingDirectory: Path.GetDirectoryName(scriptPath)!);

            Assert.True(
                result.ExitCode == 0,
                $"publish-package-artifacts.ps1 failed with exit code {result.ExitCode}.{Environment.NewLine}Output:{Environment.NewLine}{result.Output}{Environment.NewLine}Error:{Environment.NewLine}{result.Error}");

            var packageFiles = Directory.GetFiles(outputPath, "*.nupkg", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => !name!.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(2, packageFiles.Length);
            Assert.Contains(packageFiles, name => name!.StartsWith("Cephalon.Abstractions.", StringComparison.Ordinal));
            Assert.Contains(packageFiles, name => name!.StartsWith("Cephalon.Cli.", StringComparison.Ordinal));

            var manifestPath = Path.Combine(outputPath, "package-artifacts-manifest.json");
            var manifest = Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(manifestPath)));
            var artifacts = Assert.IsType<JsonArray>(manifest["Artifacts"]);

            Assert.Equal(2, artifacts.Count);
            Assert.Contains(
                artifacts.Select(node => Assert.IsType<JsonObject>(node)["Project"]?.GetValue<string>()),
                project => string.Equals(project, "src/Cephalon.Abstractions/Cephalon.Abstractions.csproj", StringComparison.Ordinal));
            Assert.Contains(
                artifacts.Select(node => Assert.IsType<JsonObject>(node)["Project"]?.GetValue<string>()),
                project => string.Equals(project, "src/Cephalon.Cli/Cephalon.Cli.csproj", StringComparison.Ordinal));
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
                $"tool install --tool-path \"{toolPath}\" Cephalon.Cli --add-source \"{outputPath}\" --ignore-failed-sources --no-cache --prerelease",
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

    private static string GetPowerShellExecutable() => "pwsh";

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

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit((int)TimeSpan.FromMinutes(10).TotalMilliseconds))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process exited between the timeout check and the kill request.
            }

            System.Threading.Tasks.Task.WaitAll(
                new System.Threading.Tasks.Task[] { outputTask, errorTask },
                TimeSpan.FromSeconds(5));

            var timeoutOutput = outputTask.IsCompletedSuccessfully ? outputTask.Result : string.Empty;
            var timeoutError = errorTask.IsCompletedSuccessfully ? errorTask.Result : string.Empty;
            timeoutError = string.IsNullOrWhiteSpace(timeoutError)
                ? "Process timed out after 10 minutes."
                : $"{timeoutError}{Environment.NewLine}Process timed out after 10 minutes.";

            return new ProcessResult(-1, timeoutOutput, timeoutError);
        }

        var output = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult();

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
