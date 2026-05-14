namespace Cephalon.Tests.Tooling;

public sealed class GeneratedAppAdoptionAssetsTests
{
    [Fact]
    public void GeneratedAppAdoptionAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var gettingStartedPath = Path.Combine(repositoryRoot, "docs", "getting-started.md");
        var operationsPath = Path.Combine(repositoryRoot, "docs", "operations.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-generated-app-adoption.ps1");
        var adoptionSmokeManifestPath = Path.Combine(repositoryRoot, "scripts", "adoption-smoke-support.json");
        var analyzerPropsPath = Path.Combine(repositoryRoot, "src", "Cephalon.Analyzers", "buildTransitive", "Cephalon.Analyzers.props");
        var bannedSymbolsPath = Path.Combine(repositoryRoot, "src", "Cephalon.Analyzers", "BannedSymbols.txt");

        Assert.True(File.Exists(gettingStartedPath), "Expected the getting-started guide.");
        Assert.True(File.Exists(operationsPath), "Expected the operations guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the generated-app adoption validation script.");
        Assert.True(File.Exists(adoptionSmokeManifestPath), "Expected the adoption smoke support manifest.");
        Assert.True(File.Exists(analyzerPropsPath), "Expected the analyzer buildTransitive props.");
        Assert.True(File.Exists(bannedSymbolsPath), "Expected the analyzer banned symbols file.");

        var gettingStarted = File.ReadAllText(gettingStartedPath);
        var operations = File.ReadAllText(operationsPath);
        var validationScript = File.ReadAllText(validationScriptPath);
        var adoptionSmokeManifest = File.ReadAllText(adoptionSmokeManifestPath);
        var analyzerProps = File.ReadAllText(analyzerPropsPath);
        var bannedSymbolLines = File.ReadAllLines(bannedSymbolsPath);

        Assert.Contains("validate-generated-app-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-adoption.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("\"tool\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"install\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("--tool-path", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Cli", validationScript, StringComparison.Ordinal);
        Assert.Contains("src/Cephalon.Analyzers/Cephalon.Analyzers.csproj", validationScript, StringComparison.Ordinal);
        Assert.Contains("src/Cephalon.Diagnostics/Cephalon.Diagnostics.csproj", validationScript, StringComparison.Ordinal);
        Assert.Contains("src/Cephalon.Resilience/Cephalon.Resilience.csproj", validationScript, StringComparison.Ordinal);
        Assert.Contains("publish-package-artifacts.ps1", validationScript, StringComparison.Ordinal);
        Assert.Contains("ProjectPaths", validationScript, StringComparison.Ordinal);
        Assert.Contains("Invoke-Cephalon", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"doctor\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("--app-root", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"restore\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"build\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("NUGET_PACKAGES", validationScript, StringComparison.Ordinal);
        Assert.Contains("Restoring repo package assets back to the default NuGet cache...", validationScript, StringComparison.Ordinal);
        Assert.Contains("ReportPath", validationScript, StringComparison.Ordinal);
        Assert.Contains("Write-GeneratedAppAdoptionExecutionReport", validationScript, StringComparison.Ordinal);
        Assert.Contains("Generated app adoption execution report:", validationScript, StringComparison.Ordinal);
        Assert.Contains("generated-app-runtime-foundation", validationScript, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/generated-app-adoption.json", validationScript, StringComparison.Ordinal);
        Assert.Contains("/health/ready", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/snapshot", validationScript, StringComparison.Ordinal);
        Assert.Contains("/scalar", validationScript, StringComparison.Ordinal);
        Assert.Contains("Generated app adoption validation completed successfully.", validationScript, StringComparison.Ordinal);

        Assert.Contains("generated-app-runtime-foundation", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("Generated app REST and operator foundation", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-adoption.ps1", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/generated-app-adoption.json", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"executionReport\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"RuntimeProbes\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"Paths\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("CephalonAnalyzersEnablePublicApiTracking", analyzerProps, StringComparison.Ordinal);
        Assert.Contains("RS0016", analyzerProps, StringComparison.Ordinal);
        Assert.DoesNotContain(bannedSymbolLines, string.IsNullOrWhiteSpace);
        Assert.DoesNotContain(bannedSymbolLines, line => line.Length > 0 && line[0] == '#');
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
    }
}
