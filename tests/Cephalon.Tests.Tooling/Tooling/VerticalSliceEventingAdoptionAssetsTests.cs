namespace Cephalon.Tests.Tooling;

public sealed class VerticalSliceEventingAdoptionAssetsTests
{
    [Fact]
    public void VerticalSliceEventingAdoptionAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var gettingStartedPath = Path.Combine(repositoryRoot, "docs", "getting-started.md");
        var operationsPath = Path.Combine(repositoryRoot, "docs", "operations.md");
        var appModelsPath = Path.Combine(repositoryRoot, "docs", "app-models.md");
        var eventingComponentPath = Path.Combine(repositoryRoot, "docs", "components", "eventing.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-vertical-slice-eventing-adoption.ps1");
        var adoptionSmokeManifestPath = Path.Combine(repositoryRoot, "scripts", "adoption-smoke-support.json");

        Assert.True(File.Exists(gettingStartedPath), "Expected the getting-started guide.");
        Assert.True(File.Exists(operationsPath), "Expected the operations guide.");
        Assert.True(File.Exists(appModelsPath), "Expected the app-models guide.");
        Assert.True(File.Exists(eventingComponentPath), "Expected the Eventing component guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the vertical-slice Eventing adoption validation script.");
        Assert.True(File.Exists(adoptionSmokeManifestPath), "Expected the adoption smoke support manifest.");

        var gettingStarted = File.ReadAllText(gettingStartedPath);
        var operations = File.ReadAllText(operationsPath);
        var appModels = File.ReadAllText(appModelsPath);
        var eventingComponent = File.ReadAllText(eventingComponentPath);
        var validationScript = File.ReadAllText(validationScriptPath);
        var adoptionSmokeManifest = File.ReadAllText(adoptionSmokeManifestPath);

        Assert.Contains("validate-vertical-slice-eventing-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-vertical-slice-eventing-adoption.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("AddEventingFromConfiguration(builder.Configuration)", appModels, StringComparison.Ordinal);
        Assert.Contains("AddEventingFromConfiguration(configuration)", eventingComponent, StringComparison.Ordinal);
        Assert.Contains("ModularVerticalSlice", validationScript, StringComparison.Ordinal);
        Assert.Contains("EventDrivenIntegration", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"CQRS\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"Outbox\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Eventing", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Eventing.Behaviors", validationScript, StringComparison.Ordinal);
        Assert.Contains("engine.AddEventingFromConfiguration(builder.Configuration);", validationScript, StringComparison.Ordinal);
        Assert.Contains("IOutboxContributor", validationScript, StringComparison.Ordinal);
        Assert.Contains("OutboxDescriptor", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"generated-process-local\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"application-events\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("\"application.*\"", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/event-publications/runtime", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/event-dispatches/terminal-failures", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/event-dispatch-remediation-commands/summary", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/diagnostics", validationScript, StringComparison.Ordinal);
        Assert.Contains("Write-VerticalSliceEventingAdoptionExecutionReport", validationScript, StringComparison.Ordinal);
        Assert.Contains("Vertical-slice Eventing adoption execution report:", validationScript, StringComparison.Ordinal);
        Assert.Contains("vertical-slice-eventing-outbox", validationScript, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/vertical-slice-eventing-adoption.json", validationScript, StringComparison.Ordinal);
        Assert.Contains("Vertical-slice Eventing adoption validation completed successfully.", validationScript, StringComparison.Ordinal);

        Assert.Contains("vertical-slice-eventing-outbox", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("Vertical slice behavior, Eventing, and outbox flow", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("validate-vertical-slice-eventing-adoption.ps1", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/vertical-slice-eventing-adoption.json", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"executionReport\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"event publication runtime truth\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"dispatch failure remediation\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"runtime diagnostics catalog\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"/engine/event-publications/runtime\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"/engine/event-dispatches/terminal-failures\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"/engine/diagnostics\"", adoptionSmokeManifest, StringComparison.Ordinal);
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
