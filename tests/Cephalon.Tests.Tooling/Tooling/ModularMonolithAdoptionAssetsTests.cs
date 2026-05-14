namespace Cephalon.Tests.Tooling;

public sealed class ModularMonolithAdoptionAssetsTests
{
    [Fact]
    public void ModularMonolithAdoptionAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var gettingStartedPath = Path.Combine(repositoryRoot, "docs", "getting-started.md");
        var operationsPath = Path.Combine(repositoryRoot, "docs", "operations.md");
        var cliComponentPath = Path.Combine(repositoryRoot, "docs", "components", "cli.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-modular-monolith-adoption.ps1");
        var adoptionSmokeManifestPath = Path.Combine(repositoryRoot, "scripts", "adoption-smoke-support.json");

        Assert.True(File.Exists(gettingStartedPath), "Expected the getting-started guide.");
        Assert.True(File.Exists(operationsPath), "Expected the operations guide.");
        Assert.True(File.Exists(cliComponentPath), "Expected the CLI component guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the modular-monolith adoption validation script.");
        Assert.True(File.Exists(adoptionSmokeManifestPath), "Expected the adoption smoke support manifest.");

        var gettingStarted = File.ReadAllText(gettingStartedPath);
        var operations = File.ReadAllText(operationsPath);
        var cliComponent = File.ReadAllText(cliComponentPath);
        var validationScript = File.ReadAllText(validationScriptPath);
        var adoptionSmokeManifest = File.ReadAllText(adoptionSmokeManifestPath);

        Assert.Contains("validate-modular-monolith-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-modular-monolith-adoption.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-modular-monolith-adoption.ps1", cliComponent, StringComparison.Ordinal);
        Assert.Contains("\"CQRS\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"Outbox\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Analyzers", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Data", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Ids.Sfid", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Worker", validationScript, StringComparison.Ordinal);
        Assert.Contains("generic-host-worker-probe", validationScript, StringComparison.Ordinal);
        Assert.Contains("Write-WorkerProbeProject", validationScript, StringComparison.Ordinal);
        Assert.Contains("builder.AddCephalonProjectConfigurations();", validationScript, StringComparison.Ordinal);
        Assert.Contains("engine.AddData();", validationScript, StringComparison.Ordinal);
        Assert.Contains("engine.AddSfidIds();", validationScript, StringComparison.Ordinal);
        Assert.Contains("ReportPath", validationScript, StringComparison.Ordinal);
        Assert.Contains("Write-ModularMonolithAdoptionExecutionReport", validationScript, StringComparison.Ordinal);
        Assert.Contains("Modular-monolith adoption execution report:", validationScript, StringComparison.Ordinal);
        Assert.Contains("modular-monolith-rest-worker-data", validationScript, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/modular-monolith-adoption.json", validationScript, StringComparison.Ordinal);
        Assert.Contains("/health/ready", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/dependencies", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/runtime-story", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/snapshot", validationScript, StringComparison.Ordinal);
        Assert.Contains("/scalar", validationScript, StringComparison.Ordinal);
        Assert.Contains("Modular-monolith REST/Worker/data adoption validation completed successfully.", validationScript, StringComparison.Ordinal);

        Assert.Contains("modular-monolith-rest-worker-data", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("Modular monolith REST, Worker, and data foundation", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("validate-modular-monolith-adoption.ps1", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/modular-monolith-adoption.json", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"executionReport\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"generic-host-worker-probe\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"CQRS and outbox data foundation\"", adoptionSmokeManifest, StringComparison.Ordinal);
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
