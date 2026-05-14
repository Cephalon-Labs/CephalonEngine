namespace Cephalon.Tests.Tooling;

public sealed class MicroserviceMultiTransportAdoptionAssetsTests
{
    [Fact]
    public void MicroserviceMultiTransportAdoptionAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var gettingStartedPath = Path.Combine(repositoryRoot, "docs", "getting-started.md");
        var operationsPath = Path.Combine(repositoryRoot, "docs", "operations.md");
        var appModelsPath = Path.Combine(repositoryRoot, "docs", "app-models.md");
        var jsonRpcComponentPath = Path.Combine(repositoryRoot, "docs", "components", "aspnetcore-jsonrpc.md");
        var grpcComponentPath = Path.Combine(repositoryRoot, "docs", "components", "aspnetcore-grpc.md");
        var httpDependenciesComponentPath = Path.Combine(repositoryRoot, "docs", "components", "observability-http-dependencies.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-microservice-multi-transport-adoption.ps1");
        var adoptionSmokeManifestPath = Path.Combine(repositoryRoot, "scripts", "adoption-smoke-support.json");

        Assert.True(File.Exists(gettingStartedPath), "Expected the getting-started guide.");
        Assert.True(File.Exists(operationsPath), "Expected the operations guide.");
        Assert.True(File.Exists(appModelsPath), "Expected the app-models guide.");
        Assert.True(File.Exists(jsonRpcComponentPath), "Expected the JSON-RPC component guide.");
        Assert.True(File.Exists(grpcComponentPath), "Expected the gRPC component guide.");
        Assert.True(File.Exists(httpDependenciesComponentPath), "Expected the HTTP dependency-health component guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the microservice multi-transport adoption validation script.");
        Assert.True(File.Exists(adoptionSmokeManifestPath), "Expected the adoption smoke support manifest.");

        var gettingStarted = File.ReadAllText(gettingStartedPath);
        var operations = File.ReadAllText(operationsPath);
        var appModels = File.ReadAllText(appModelsPath);
        var jsonRpcComponent = File.ReadAllText(jsonRpcComponentPath);
        var grpcComponent = File.ReadAllText(grpcComponentPath);
        var httpDependenciesComponent = File.ReadAllText(httpDependenciesComponentPath);
        var validationScript = File.ReadAllText(validationScriptPath);
        var adoptionSmokeManifest = File.ReadAllText(adoptionSmokeManifestPath);

        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", appModels, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", jsonRpcComponent, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", grpcComponent, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", httpDependenciesComponent, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Analyzers", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.AspNetCore.JsonRpc", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.AspNetCore.Grpc", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Observability.HttpDependencies", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Observability.DependencyHealth.Core", validationScript, StringComparison.Ordinal);
        Assert.Contains("builder.AddJsonRpcTransport();", validationScript, StringComparison.Ordinal);
        Assert.Contains("builder.AddGrpcTransport();", validationScript, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddCephalonHttpDependencyHealth(builder.Configuration);", validationScript, StringComparison.Ordinal);
        Assert.Contains("IJsonRpcModule", validationScript, StringComparison.Ordinal);
        Assert.Contains("IGrpcModule", validationScript, StringComparison.Ordinal);
        Assert.Contains("GrpcSubdirectoryHandler", validationScript, StringComparison.Ordinal);
        Assert.Contains("/api/v1/platform/status", validationScript, StringComparison.Ordinal);
        Assert.Contains("/json-rpc/platform", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/transports", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/snapshot", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/dependencies", validationScript, StringComparison.Ordinal);
        Assert.Contains("Write-MicroserviceMultiTransportAdoptionExecutionReport", validationScript, StringComparison.Ordinal);
        Assert.Contains("microservice-multi-transport-operations", validationScript, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/microservice-multi-transport-adoption.json", validationScript, StringComparison.Ordinal);
        Assert.Contains("Microservice multi-transport adoption validation completed successfully.", validationScript, StringComparison.Ordinal);

        Assert.Contains("microservice-multi-transport-operations", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("Microservice with multi-transport and operations posture", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("validate-microservice-multi-transport-adoption.ps1", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/microservice-multi-transport-adoption.json", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"executionReport\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"blueprint-driven generated microservice\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"HTTP dependency-health companion proof\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"/engine/transports\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"/engine/dependencies\"", adoptionSmokeManifest, StringComparison.Ordinal);
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
