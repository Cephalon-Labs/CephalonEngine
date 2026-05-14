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
        Assert.Contains("Cephalon.Behaviors.SourceGen", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Diagnostics", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Resilience", validationScript, StringComparison.Ordinal);
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
        Assert.Contains("ReportPath", validationScript, StringComparison.Ordinal);
        Assert.Contains("Write-AdoptionSmokeExecutionReport", validationScript, StringComparison.Ordinal);
        Assert.Contains("Adoption smoke execution report:", validationScript, StringComparison.Ordinal);
        Assert.Contains("Out-of-tree package adoption validation completed successfully.", validationScript, StringComparison.Ordinal);

        Assert.Contains("out-of-tree-generated-app-package-stage", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("execution-report-ready", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("goldenUseCases", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("out-of-tree-package-adoption", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("generated-app-runtime-foundation", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("template-pack-dotnet-new-parity", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("modular-monolith-rest-worker-data", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("vertical-slice-eventing-outbox", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("microservice-multi-transport-operations", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("saas-tenant-governance-audit", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("requiredEngineCapabilities", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("proofTargets", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("validate-out-of-tree-package-adoption.ps1", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("validate-modular-monolith-adoption.ps1", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("validate-vertical-slice-eventing-adoption.ps1", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("validate-saas-tenant-governance-audit-adoption.ps1", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Behaviors.SourceGen", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Diagnostics", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Resilience", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("Cephalon.ReferenceModule.Operations.csproj", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("docs/package-publishing.md", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("runsOutsideRepository", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("stagesReferenceModulePackage", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("/engine/packages", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("/engine/package-policy", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("/engine/trust-policy", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("/engine/snapshot", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("/api/operations/status", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("executionReport", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/out-of-tree-package-adoption.json", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/generated-app-adoption.json", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/template-pack-adoption.json", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/modular-monolith-adoption.json", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/vertical-slice-eventing-adoption.json", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/microservice-multi-transport-adoption.json", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/saas-tenant-governance-audit.json", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("RuntimeProbes", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("Paths", adoptionSmokeManifest, StringComparison.Ordinal);
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
