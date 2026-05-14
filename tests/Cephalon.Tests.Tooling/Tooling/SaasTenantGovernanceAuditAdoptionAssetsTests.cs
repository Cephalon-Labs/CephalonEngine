namespace Cephalon.Tests.Tooling;

public sealed class SaasTenantGovernanceAuditAdoptionAssetsTests
{
    [Fact]
    public void SaasTenantGovernanceAuditAdoptionAssetsStayAligned()
    {
        var repositoryRoot = GetRepositoryRoot();
        var gettingStartedPath = Path.Combine(repositoryRoot, "docs", "getting-started.md");
        var operationsPath = Path.Combine(repositoryRoot, "docs", "operations.md");
        var appModelsPath = Path.Combine(repositoryRoot, "docs", "app-models.md");
        var auditComponentPath = Path.Combine(repositoryRoot, "docs", "components", "audit.md");
        var grpcComponentPath = Path.Combine(repositoryRoot, "docs", "components", "aspnetcore-grpc.md");
        var jsonRpcComponentPath = Path.Combine(repositoryRoot, "docs", "components", "aspnetcore-jsonrpc.md");
        var governanceComponentPath = Path.Combine(repositoryRoot, "docs", "components", "multi-tenancy-governance.md");
        var governanceAspNetCoreComponentPath = Path.Combine(repositoryRoot, "docs", "components", "multi-tenancy-governance-aspnetcore.md");
        var httpDependenciesComponentPath = Path.Combine(repositoryRoot, "docs", "components", "observability-http-dependencies.md");
        var validationScriptPath = Path.Combine(repositoryRoot, "scripts", "validate-saas-tenant-governance-audit-adoption.ps1");
        var adoptionSmokeManifestPath = Path.Combine(repositoryRoot, "scripts", "adoption-smoke-support.json");

        Assert.True(File.Exists(gettingStartedPath), "Expected the getting-started guide.");
        Assert.True(File.Exists(operationsPath), "Expected the operations guide.");
        Assert.True(File.Exists(appModelsPath), "Expected the app-models guide.");
        Assert.True(File.Exists(auditComponentPath), "Expected the audit component guide.");
        Assert.True(File.Exists(grpcComponentPath), "Expected the gRPC component guide.");
        Assert.True(File.Exists(jsonRpcComponentPath), "Expected the JSON-RPC component guide.");
        Assert.True(File.Exists(governanceComponentPath), "Expected the multi-tenancy governance component guide.");
        Assert.True(File.Exists(governanceAspNetCoreComponentPath), "Expected the multi-tenancy governance ASP.NET Core component guide.");
        Assert.True(File.Exists(httpDependenciesComponentPath), "Expected the HTTP dependency-health component guide.");
        Assert.True(File.Exists(validationScriptPath), "Expected the SaaS tenant governance audit adoption validation script.");
        Assert.True(File.Exists(adoptionSmokeManifestPath), "Expected the adoption smoke support manifest.");

        var gettingStarted = File.ReadAllText(gettingStartedPath);
        var operations = File.ReadAllText(operationsPath);
        var appModels = File.ReadAllText(appModelsPath);
        var auditComponent = File.ReadAllText(auditComponentPath);
        var grpcComponent = File.ReadAllText(grpcComponentPath);
        var jsonRpcComponent = File.ReadAllText(jsonRpcComponentPath);
        var governanceComponent = File.ReadAllText(governanceComponentPath);
        var governanceAspNetCoreComponent = File.ReadAllText(governanceAspNetCoreComponentPath);
        var httpDependenciesComponent = File.ReadAllText(httpDependenciesComponentPath);
        var validationScript = File.ReadAllText(validationScriptPath);
        var adoptionSmokeManifest = File.ReadAllText(adoptionSmokeManifestPath);

        Assert.Contains("validate-saas-tenant-governance-audit-adoption.ps1", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("validate-saas-tenant-governance-audit-adoption.ps1", operations, StringComparison.Ordinal);
        Assert.Contains("validate-saas-tenant-governance-audit-adoption.ps1", appModels, StringComparison.Ordinal);
        Assert.Contains("validate-saas-tenant-governance-audit-adoption.ps1", auditComponent, StringComparison.Ordinal);
        Assert.Contains("validate-saas-tenant-governance-audit-adoption.ps1", grpcComponent, StringComparison.Ordinal);
        Assert.Contains("validate-saas-tenant-governance-audit-adoption.ps1", jsonRpcComponent, StringComparison.Ordinal);
        Assert.Contains("validate-saas-tenant-governance-audit-adoption.ps1", governanceComponent, StringComparison.Ordinal);
        Assert.Contains("validate-saas-tenant-governance-audit-adoption.ps1", governanceAspNetCoreComponent, StringComparison.Ordinal);
        Assert.Contains("validate-saas-tenant-governance-audit-adoption.ps1", httpDependenciesComponent, StringComparison.Ordinal);

        Assert.Contains("Cephalon.MultiTenancy.Governance", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.MultiTenancy.Governance.AspNetCore", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Audit", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.AspNetCore.JsonRpc", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.AspNetCore.Grpc", validationScript, StringComparison.Ordinal);
        Assert.Contains("Cephalon.Observability.HttpDependencies", validationScript, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddSingleton<ITenantInvitationDeliverySender, SmokeTenantInvitationDeliverySender>();", validationScript, StringComparison.Ordinal);
        Assert.Contains("engine.AddMultiTenancyGovernance();", validationScript, StringComparison.Ordinal);
        Assert.Contains("engine.AddAudit();", validationScript, StringComparison.Ordinal);
        Assert.Contains("app.MapCephalonTenantAdministrationCommands();", validationScript, StringComparison.Ordinal);
        Assert.Contains("app.MapCephalonTenantInvitationDeliveryDispatches();", validationScript, StringComparison.Ordinal);
        Assert.Contains("app.MapCephalonTenantInvitationDeliveryStatusCallbacks();", validationScript, StringComparison.Ordinal);
        Assert.Contains("app.MapCephalonTenantInvitationDeliveryStatusObservations();", validationScript, StringComparison.Ordinal);
        Assert.Contains("/api/tenants/{tenantId}/audit-proof", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/tenant-administration/commands", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/tenant-invitations/delivery-dispatches", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/tenant-invitations/delivery-status", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/tenant-invitations/delivery-status/observations", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/audit-stores", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/technology-surfaces/multi-tenancy", validationScript, StringComparison.Ordinal);
        Assert.Contains("/engine/dependencies", validationScript, StringComparison.Ordinal);
        Assert.Contains("Write-SaasTenantGovernanceAdoptionExecutionReport", validationScript, StringComparison.Ordinal);
        Assert.Contains("saas-tenant-governance-audit", validationScript, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/saas-tenant-governance-audit.json", validationScript, StringComparison.Ordinal);
        Assert.Contains("SaaS tenant governance audit adoption validation completed successfully.", validationScript, StringComparison.Ordinal);

        Assert.Contains("saas-tenant-governance-audit", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("SaaS tenant governance, invitation delivery, and audit", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("validate-saas-tenant-governance-audit-adoption.ps1", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("artifacts/adoption-smoke/saas-tenant-governance-audit.json", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"executionReport\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"tenant administration workflow\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"invitation delivery dispatch\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"audit recording and audit-store runtime truth\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"/engine/tenant-administration/commands\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"/engine/tenant-invitations/delivery-dispatches\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"/engine/tenant-invitations/delivery-status/observations\"", adoptionSmokeManifest, StringComparison.Ordinal);
        Assert.Contains("\"/engine/audit-stores\"", adoptionSmokeManifest, StringComparison.Ordinal);
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
