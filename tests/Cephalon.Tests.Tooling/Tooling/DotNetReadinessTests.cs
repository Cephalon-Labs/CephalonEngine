using System.Text.Json.Nodes;
using Cephalon.Tests.Support;

namespace Cephalon.Tests.Tooling;

public sealed class DotNetReadinessTests
{
    [Fact]
    public void ValidateDotNetReadinessScriptProducesBaselineReport()
    {
        var scriptPath = RepositoryPaths.GetFile("scripts", "validate-dotnet-readiness.ps1");
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-dotnet-readiness-{Guid.NewGuid():N}");
        const string testFilter = "FullyQualifiedName!~Cephalon.Tests.Tooling.PackagePublishingTests";

        Directory.CreateDirectory(outputPath);

        try
        {
            var result = RunProcess(
                "pwsh",
                $"-File \"{scriptPath}\" -Configuration {GetCurrentBuildConfiguration()} -OutputPath \"{outputPath}\" -SkipBuild -SkipTests -SkipReferenceDocs -SkipPackages -TestFilter \"{testFilter}\"",
                workingDirectory: Path.GetDirectoryName(scriptPath)!);

            Assert.True(
                result.ExitCode == 0,
                $"validate-dotnet-readiness.ps1 failed with exit code {result.ExitCode}.{Environment.NewLine}Output:{Environment.NewLine}{result.Output}{Environment.NewLine}Error:{Environment.NewLine}{result.Error}");

            var markdownPath = Path.Combine(outputPath, "README.md");
            var jsonPath = Path.Combine(outputPath, "dotnet-readiness-report.json");

            Assert.True(File.Exists(markdownPath));
            Assert.True(File.Exists(jsonPath));

            var report = Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(jsonPath)));
            var globalJson = Assert.IsType<JsonObject>(report["GlobalJson"]);
            Assert.Equal("10.0.201", globalJson["Version"]?.GetValue<string>());
            Assert.Equal("latestFeature", globalJson["RollForward"]?.GetValue<string>());

            var shippingBaseline = Assert.IsType<JsonObject>(report["ShippingBaseline"]);
            Assert.Equal("net10.0", shippingBaseline["StableTargetFramework"]?.GetValue<string>());
            Assert.Equal(testFilter, report["TestFilter"]?.GetValue<string>());

            var deploymentModeSupport = Assert.IsType<JsonObject>(report["DeploymentModeSupport"]);
            Assert.Equal("scripts/deployment-mode-support.json", deploymentModeSupport["ManifestPath"]?.GetValue<string>());
            Assert.Equal("net10.0", deploymentModeSupport["ShippingBaseline"]?["StableTargetFramework"]?.GetValue<string>());
            Assert.Equal("net11.0", deploymentModeSupport["ShippingBaseline"]?["ReadinessLaneTargetFramework"]?.GetValue<string>());
            Assert.Equal("docs/deployment-mode-support.md", deploymentModeSupport["Documentation"]?["GuidePath"]?.GetValue<string>());
            Assert.Equal("not-claimed", deploymentModeSupport["DeploymentModes"]?["Trim"]?["Status"]?.GetValue<string>());
            Assert.Equal("not-claimed", deploymentModeSupport["DeploymentModes"]?["NativeAot"]?["Status"]?.GetValue<string>());
            Assert.Equal("not-claimed", deploymentModeSupport["DeploymentModes"]?["SingleFile"]?["Status"]?.GetValue<string>());
            var packageScopedClaims = Assert.IsType<JsonArray>(deploymentModeSupport["PackageScopedClaims"]);
            Assert.Equal(3, packageScopedClaims.Count);
            var packageScopedClaimNames = packageScopedClaims
                .Select(claim => Assert.IsType<JsonObject>(claim)["PackageName"]?.GetValue<string>() ?? string.Empty)
                .Order(StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(
                ["Cephalon.Abstractions", "Cephalon.Diagnostics", "Cephalon.Scaffolding"],
                packageScopedClaimNames);
            foreach (var claim in packageScopedClaims.Select(claim => Assert.IsType<JsonObject>(claim)))
            {
                Assert.Contains("singleFile", claim["SupportedModes"]!.AsArray().Select(mode => mode!.GetValue<string>()));
            }

            var claims = Assert.IsType<JsonObject>(report["Claims"]);
            Assert.Equal("not-claimed", claims["Trim"]?["Status"]?.GetValue<string>());
            Assert.Equal("not-claimed", claims["NativeAot"]?["Status"]?.GetValue<string>());
            Assert.Equal("not-claimed", claims["SingleFile"]?["Status"]?.GetValue<string>());
            Assert.Equal("not-claimed", claims["Trim"]?["SupportContractStatus"]?.GetValue<string>());
            Assert.Equal("not-claimed", claims["NativeAot"]?["SupportContractStatus"]?.GetValue<string>());
            Assert.Equal("not-claimed", claims["SingleFile"]?["SupportContractStatus"]?.GetValue<string>());

            var markdown = File.ReadAllText(markdownPath);
            Assert.Contains("Stable shipping floor remains `net10.0`.", markdown, StringComparison.Ordinal);
            Assert.Contains("Source manifest: `scripts/deployment-mode-support.json`", markdown, StringComparison.Ordinal);
            Assert.Contains("Support guide: `docs/deployment-mode-support.md`", markdown, StringComparison.Ordinal);
            Assert.Contains("Trim status: **not-claimed**", markdown, StringComparison.Ordinal);
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
    public void ReleaseValidationScriptTracksSplitTestsAndReadinessArtifact()
    {
        var scriptContents = File.ReadAllText(RepositoryPaths.GetFile("scripts", "validate-release.ps1"));

        Assert.Contains("Cephalon.Tests.Composition", scriptContents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Tests.Hosting", scriptContents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Tests.Tooling", scriptContents, StringComparison.Ordinal);
        Assert.Contains("dotnet-readiness-release", scriptContents, StringComparison.Ordinal);
        Assert.Contains("$SkipDotNetReadiness", scriptContents, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseValidationWorkflowIncludesMasterAndDotNet11ReadinessJob()
    {
        var workflowContents = File.ReadAllText(RepositoryPaths.GetFile(".github", "workflows", "release-validation.yml"));

        Assert.Contains("- master", workflowContents, StringComparison.Ordinal);
        Assert.Contains("global-json-file: global.json", workflowContents, StringComparison.Ordinal);
        Assert.Contains("dotnet11-readiness:", workflowContents, StringComparison.Ordinal);
        Assert.Contains("dotnet-version: 11.0.x", workflowContents, StringComparison.Ordinal);
        Assert.Contains("validate-dotnet-readiness.ps1", workflowContents, StringComparison.Ordinal);
        Assert.Contains("dotnet-readiness-sdk11", workflowContents, StringComparison.Ordinal);
        Assert.Contains("-TestFilter", workflowContents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Tests.Tooling.TemplatePackTests", workflowContents, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Tests.Tooling.PackagePublishingTests", workflowContents, StringComparison.Ordinal);
        Assert.Contains("RunAsyncStagesPublishedModulePackageIntoLoadableDirectory", workflowContents, StringComparison.Ordinal);
    }

    [Fact]
    public void PlanningSyncScriptTracksEng097AndCurrentSprintHistoryShape()
    {
        var scriptContents = File.ReadAllText(RepositoryPaths.GetFile("scripts", "sync-planning-github.ps1"));

        Assert.Contains("\"ENG-097\" = 1", scriptContents, StringComparison.Ordinal);
        Assert.Contains("Sprint history and next 4 sprints", scriptContents, StringComparison.Ordinal);
        Assert.Contains("labels?per_page=100&page=$page", scriptContents, StringComparison.Ordinal);
        Assert.Contains("[AllowEmptyCollection()][string[]]$DesiredIterationTitles", scriptContents, StringComparison.Ordinal);
        Assert.Contains("Start-Sleep -Seconds 2", scriptContents, StringComparison.Ordinal);
        Assert.Contains("\"1000\"", scriptContents, StringComparison.Ordinal);
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
