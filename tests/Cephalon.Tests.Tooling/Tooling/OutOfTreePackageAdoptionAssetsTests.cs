namespace Cephalon.Tests.Tooling;

public sealed class OutOfTreePackageAdoptionAssetsTests
{
    [Fact]
    public void OutOfTreePackageAdoptionAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var gettingStartedPath = Path.Combine(repositoryRoot, "docs", "getting-started.md");
        var operationsPath = Path.Combine(repositoryRoot, "docs", "operations.md");
        var packageLifecyclePath = Path.Combine(repositoryRoot, "docs", "external-package-lifecycle.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-out-of-tree-package-adoption.ps1");
        var adoptionSmokeManifestPath = Path.Combine(repositoryRoot, "scripts", "adoption-smoke-support.json");

        Assert.True(File.Exists(gettingStartedPath), "Expected the getting-started guide.");
        Assert.True(File.Exists(operationsPath), "Expected the operations guide.");
        Assert.True(File.Exists(packageLifecyclePath), "Expected the external package lifecycle guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the out-of-tree package adoption validation script.");
        Assert.True(File.Exists(adoptionSmokeManifestPath), "Expected the adoption smoke support manifest.");

        var gettingStarted = File.ReadAllText(gettingStartedPath);
        var operations = File.ReadAllText(operationsPath);
        var packageLifecycle = File.ReadAllText(packageLifecyclePath);
        var validationScript = File.ReadAllText(validationScriptPath);
        var adoptionSmokeManifest = File.ReadAllText(adoptionSmokeManifestPath);

        Assert.Contains("validate-out-of-tree-package-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", packageLifecycle, StringComparison.Ordinal);
        Assert.Contains("\"tool\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"install\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Cli", validationScript, StringComparison.Ordinal);
        Assert.Contains("cephalon package stage", validationScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"package\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"stage\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.ReferenceModule.Operations", validationScript, StringComparison.Ordinal);
        Assert.Contains("PackageDirectories", validationScript, StringComparison.Ordinal);
        Assert.Contains("RequireTrustedPackages", validationScript, StringComparison.Ordinal);
        Assert.Contains("TrustedPublishers", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/packages", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/package-policy", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/trust-policy", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/snapshot", validationScript, StringComparison.Ordinal);
        Assert.Contains("/api/operations/status", validationScript, StringComparison.Ordinal);
        Assert.Contains("Out-of-tree package adoption validation completed successfully.", validationScript, StringComparison.Ordinal);

        Assert.Contains("out-of-tree-generated-app-package-stage", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("Cephalon.ReferenceModule.Operations.csproj", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("docs/package-publishing.md", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("runsOutsideRepository", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("stagesReferenceModulePackage", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("/engine/packages", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("/engine/package-policy", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("/engine/trust-policy", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("/engine/snapshot", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("/api/operations/status", adoptionSmokeManifest, StringComparison.Ordinal);
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
