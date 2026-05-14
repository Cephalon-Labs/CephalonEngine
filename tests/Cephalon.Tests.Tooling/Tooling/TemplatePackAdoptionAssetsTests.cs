namespace Cephalon.Tests.Tooling;

public sealed class TemplatePackAdoptionAssetsTests
{
    [Fact]
    public void TemplatePackAdoptionAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var gettingStartedPath = Path.Combine(repositoryRoot, "docs", "getting-started.md");
        var operationsPath = Path.Combine(repositoryRoot, "docs", "operations.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-template-pack-adoption.ps1");
        var adoptionSmokeManifestPath = Path.Combine(repositoryRoot, "scripts", "adoption-smoke-support.json");

        Assert.True(File.Exists(gettingStartedPath), "Expected the getting-started guide.");
        Assert.True(File.Exists(operationsPath), "Expected the operations guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the template-pack adoption validation script.");
        Assert.True(File.Exists(adoptionSmokeManifestPath), "Expected the adoption smoke support manifest.");

        var gettingStarted = File.ReadAllText(gettingStartedPath);
        var operations = File.ReadAllText(operationsPath);
        var validationScript = File.ReadAllText(validationScriptPath);
        var adoptionSmokeManifest = File.ReadAllText(adoptionSmokeManifestPath);

        Assert.Contains("validate-template-pack-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-template-pack-adoption.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("Cephalon.TemplatePack", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"new\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"install\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("--nuget-source", validationScript, StringComparison.Ordinal);
        Assert.Contains("--debug:custom-hive", validationScript, StringComparison.Ordinal);
        Assert.Contains("CEPHALON_DOCTOR_TEMPLATE_HIVE", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"list\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"cephalon\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"doctor\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("--app-root", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"restore\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"build\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("src/Cephalon.Diagnostics/Cephalon.Diagnostics.csproj", validationScript, StringComparison.Ordinal);
        Assert.Contains("src/Cephalon.Resilience/Cephalon.Resilience.csproj", validationScript, StringComparison.Ordinal);
        Assert.Contains("ReportPath", validationScript, StringComparison.Ordinal);
        Assert.Contains("Write-TemplatePackAdoptionExecutionReport", validationScript, StringComparison.Ordinal);
        Assert.Contains("Template-pack adoption execution report:", validationScript, StringComparison.Ordinal);
        Assert.Contains("template-pack-dotnet-new-parity", validationScript, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/template-pack-adoption.json", validationScript, StringComparison.Ordinal);
        Assert.Contains("/health/ready", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/snapshot", validationScript, StringComparison.Ordinal);
        Assert.Contains("/scalar", validationScript, StringComparison.Ordinal);
        Assert.Contains("Template-pack adoption validation completed successfully.", validationScript, StringComparison.Ordinal);

        Assert.Contains("template-pack-dotnet-new-parity", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("Template pack dotnet new starter parity", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("validate-template-pack-adoption.ps1", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/template-pack-adoption.json", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"executionReport\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"RuntimeProbes\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"Paths\"", adoptionSmokeManifest, StringComparison.Ordinal);
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
