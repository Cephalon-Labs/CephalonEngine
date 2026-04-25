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

        Assert.True(File.Exists(gettingStartedPath), "Expected the getting-started guide.");
        Assert.True(File.Exists(operationsPath), "Expected the operations guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the generated-app adoption validation script.");

        var gettingStarted = File.ReadAllText(gettingStartedPath);
        var operations = File.ReadAllText(operationsPath);
        var validationScript = File.ReadAllText(validationScriptPath);

        Assert.Contains("validate-generated-app-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-generated-app-adoption.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("\"tool\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"install\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("--tool-path", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Cli", validationScript, StringComparison.Ordinal);
        Assert.Contains("publish-package-artifacts.ps1", validationScript, StringComparison.Ordinal);
        Assert.Contains("ProjectPaths", validationScript, StringComparison.Ordinal);
        Assert.Contains("Invoke-Cephalon", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"doctor\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("--app-root", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"restore\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"build\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("NUGET_PACKAGES", validationScript, StringComparison.Ordinal);
        Assert.Contains("Restoring repo package assets back to the default NuGet cache...", validationScript, StringComparison.Ordinal);
        Assert.Contains("/health/ready", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/snapshot", validationScript, StringComparison.Ordinal);
        Assert.Contains("Generated app adoption validation completed successfully.", validationScript, StringComparison.Ordinal);
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
