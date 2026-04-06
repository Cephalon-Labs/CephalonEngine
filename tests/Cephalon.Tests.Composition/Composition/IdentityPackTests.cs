using Cephalon.Abstractions.Authorization;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.Identity.Registration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class IdentityPackTests
{
    [Fact]
    public async Task AddIdentityAccessRegistersDefaultEvaluatorDiagnosticsAndRuntimeSurface()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                technologies: ["IdentityAccess"],
                identity: new IdentitySettings(
                    enabled: true,
                    authorizationModes: ["Policy", "RBAC", "ABAC"])));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new IdentityAuthorizationTestModule());
            engine.AddIdentityAccess();
        });

        await using var provider = services.BuildServiceProvider();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();
        var runtime = provider.GetRequiredService<global::Cephalon.Engine.Runtime.IRuntime>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var identitySurfaces = technologyCatalog.GetByTechnology("identity-access");
        var authorizationSurface = Assert.Single(identitySurfaces, surface => surface.SurfaceId == "identity-authorization");
        var authorizationEntry = Assert.Single(authorizationSurface.Entries, entry => entry.Id == "identity-runtime");
        var diagnosticsConvention = Assert.Single(diagnosticsCatalog.GetBySource("Cephalon.Identity"));

        Assert.NotNull(evaluator);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "identity.authorization");
        Assert.Equal("enabled", authorizationEntry.Metadata["identitySelection"]);
        Assert.Equal("3", authorizationEntry.Metadata["selectedAuthorizationModeCount"]);
        Assert.Equal("3", authorizationEntry.Metadata["policyCount"]);
        Assert.Equal("1", authorizationEntry.Metadata["rbacPolicyCount"]);
        Assert.Equal("1", authorizationEntry.Metadata["abacPolicyCount"]);
        Assert.Equal("3", authorizationEntry.Metadata["policyModeCount"]);
        Assert.Equal("configured", authorizationEntry.Metadata["defaultEvaluator"]);
        Assert.Equal("true", authorizationEntry.Metadata["defaultEvaluatorEnabled"]);
        Assert.Equal("true", authorizationEntry.Metadata["requireExplicitPolicy"]);
        Assert.Contains("requiredRoles", authorizationEntry.Metadata["declarativeConventions"], StringComparison.Ordinal);
        Assert.Equal(4400, diagnosticsConvention.MinimumEventId);
        Assert.Equal(4401, diagnosticsConvention.MaximumEventId);
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4400 && entry.Name == "IdentityAuthorizationAllowed");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4401 && entry.Name == "IdentityAuthorizationDenied");
    }

    [Fact]
    public async Task AddIdentityAccessEvaluatesDeclarativePoliciesThroughDefaultEvaluator()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["IdentityAccess"],
                identity: new IdentitySettings(
                    enabled: true,
                    authorizationModes: ["Policy", "RBAC", "ABAC"])));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new IdentityAuthorizationTestModule());
            engine.AddIdentityAccess();
        });

        await using var provider = services.BuildServiceProvider();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var tenantAdminDecision = await evaluator.EvaluateAsync(
            new AuthorizationSubject(
                subjectId: "user-001",
                displayName: "Ada",
                roles: ["tenant-admin"],
                tenantIds: ["tenant-001"]),
            new AuthorizationResource(
                resourceType: "tenant",
                resourceId: "tenant-001",
                tenantId: "tenant-001",
                ownerSubjectId: "user-001"),
            new AuthorizationContext(
                action: "manage",
                policyId: "tenant-admin",
                tenantId: "tenant-001"));

        var tenantBoundaryDecision = await evaluator.EvaluateAsync(
            new AuthorizationSubject(
                subjectId: "user-002",
                roles: ["member"],
                tenantIds: ["tenant-001"],
                attributes: new Dictionary<string, string>
                {
                    ["region"] = "apac"
                }),
            new AuthorizationResource(
                resourceType: "document",
                resourceId: "doc-001",
                tenantId: "tenant-001",
                ownerSubjectId: "user-005",
                attributes: new Dictionary<string, string>
                {
                    ["classification"] = "internal"
                }),
            new AuthorizationContext(
                action: "read",
                policyId: "tenant-boundary",
                tenantId: "tenant-001",
                attributes: new Dictionary<string, string>
                {
                    ["operation"] = "query"
                }));

        var ownerDecision = await evaluator.EvaluateAsync(
            new AuthorizationSubject(
                subjectId: "user-003",
                tenantIds: ["tenant-001"]),
            new AuthorizationResource(
                resourceType: "document",
                resourceId: "doc-002",
                tenantId: "tenant-001",
                ownerSubjectId: "user-003"),
            new AuthorizationContext(
                action: "update",
                policyId: "document-owner",
                tenantId: "tenant-001",
                attributes: new Dictionary<string, string>
                {
                    ["operation"] = "update"
                }));

        var deniedDecision = await evaluator.EvaluateAsync(
            new AuthorizationSubject(
                subjectId: "user-004",
                roles: ["member"],
                tenantIds: ["tenant-999"],
                attributes: new Dictionary<string, string>
                {
                    ["region"] = "apac"
                }),
            new AuthorizationResource(
                resourceType: "document",
                resourceId: "doc-004",
                tenantId: "tenant-001",
                ownerSubjectId: "user-010",
                attributes: new Dictionary<string, string>
                {
                    ["classification"] = "internal"
                }),
            new AuthorizationContext(
                action: "read",
                policyId: "tenant-boundary",
                tenantId: "tenant-001",
                attributes: new Dictionary<string, string>
                {
                    ["operation"] = "query"
                }));

        Assert.True(tenantAdminDecision.IsAllowed);
        Assert.Equal("tenant-admin", tenantAdminDecision.PolicyId);
        Assert.Equal("allowed", tenantAdminDecision.Metadata["outcome"]);
        Assert.Equal("1", tenantAdminDecision.Metadata["ruleCount"]);
        Assert.Equal("rbac,policy", tenantAdminDecision.Metadata["modes"]);

        Assert.True(tenantBoundaryDecision.IsAllowed);
        Assert.Equal("tenant-boundary", tenantBoundaryDecision.PolicyId);
        Assert.Equal("allowed", tenantBoundaryDecision.Metadata["outcome"]);
        Assert.Equal("3", tenantBoundaryDecision.Metadata["ruleCount"]);
        Assert.Equal("abac,policy", tenantBoundaryDecision.Metadata["modes"]);

        Assert.True(ownerDecision.IsAllowed);
        Assert.Equal("document-owner", ownerDecision.PolicyId);
        Assert.Equal("2", ownerDecision.Metadata["ruleCount"]);
        Assert.Equal("policy", ownerDecision.Metadata["modes"]);

        Assert.False(deniedDecision.IsAllowed);
        Assert.Equal("tenant-boundary", deniedDecision.PolicyId);
        Assert.Equal("denied", deniedDecision.Metadata["outcome"]);
        Assert.Contains("tenant boundary", deniedDecision.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddIdentityAccessRequiresExplicitPolicyByDefault()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                technologies: ["IdentityAccess"],
                identity: new IdentitySettings(
                    enabled: true,
                    authorizationModes: ["Policy"])));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new IdentityAuthorizationTestModule());
            engine.AddIdentityAccess();
        });

        await using var provider = services.BuildServiceProvider();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            new AuthorizationSubject(
                subjectId: "user-005",
                tenantIds: ["tenant-001"]),
            new AuthorizationResource(
                resourceType: "document",
                resourceId: "doc-005",
                tenantId: "tenant-001",
                ownerSubjectId: "user-005"),
            new AuthorizationContext(
                action: "read",
                tenantId: "tenant-001"));

        Assert.False(decision.IsAllowed);
        Assert.Null(decision.PolicyId);
        Assert.Equal("denied", decision.Metadata["outcome"]);
        Assert.Equal("none", decision.Metadata["policyId"]);
        Assert.Contains("policy id", decision.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddIdentityAccessCanDisableTheBuiltInEvaluatorWithoutBreakingResolution()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["IdentityAccess"],
                identity: new IdentitySettings(
                    enabled: true,
                    authorizationModes: ["Policy", "RBAC", "ABAC"])));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new IdentityAuthorizationTestModule());
            engine.AddIdentityAccess(options => options.EnableDefaultEvaluator = false);
        });

        await using var provider = services.BuildServiceProvider();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();
        var identitySurface = Assert.Single(technologyCatalog.GetByTechnology("identity-access"), surface => surface.SurfaceId == "identity-authorization");
        var identityEntry = Assert.Single(identitySurface.Entries, entry => entry.Id == "identity-runtime");

        var decision = await evaluator.EvaluateAsync(
            new AuthorizationSubject(
                subjectId: "user-006",
                roles: ["tenant-admin"],
                tenantIds: ["tenant-001"]),
            new AuthorizationResource(
                resourceType: "tenant",
                resourceId: "tenant-001",
                tenantId: "tenant-001"),
            new AuthorizationContext(
                action: "manage",
                policyId: "tenant-admin",
                tenantId: "tenant-001"));

        Assert.False(decision.IsAllowed);
        Assert.Equal("disabled", identityEntry.Metadata["defaultEvaluator"]);
        Assert.Equal("false", identityEntry.Metadata["defaultEvaluatorEnabled"]);
        Assert.Equal("none", identityEntry.Metadata["defaultEvaluatorType"]);
        Assert.Contains("disabled", decision.Reason!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("disabled", decision.Metadata["evaluator"]);
        Assert.Contains("DisabledAuthorizationEvaluator", identityEntry.Metadata["authorizationEvaluatorTypes"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddIdentityAccessCanDisableRuntimeSurfaceProjection()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                technologies: ["IdentityAccess"],
                identity: new IdentitySettings(
                    enabled: true,
                    authorizationModes: ["Policy"])));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new IdentityAuthorizationTestModule());
            engine.AddIdentityAccess(options => options.EnableRuntimeSurface = false);
        });

        await using var provider = services.BuildServiceProvider();
        var technologyCatalog = provider.GetRequiredService<global::Cephalon.Abstractions.Technologies.ITechnologyRuntimeCatalog>();

        Assert.Empty(technologyCatalog.GetByTechnology("identity-access"));
    }
}
