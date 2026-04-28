using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Cephalon.MultiTenancy.Registration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class MultiTenancyGovernancePackTests
{
    [Fact]
    public async Task AddMultiTenancyGovernanceRegistersMembershipCatalogEvaluatorDiagnosticsAndRuntimeSurface()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddModule(new PlatformTestModule());
            engine.AddMultiTenancy();
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Memberships.Add(new TenantMembershipDescriptor(
                    tenantId: "tenant-001",
                    principalId: "user-001",
                    displayName: "Acme Admin",
                    roles: ["admin", "member"],
                    sourceModuleId: "platform-test"));
                options.Memberships.Add(new TenantMembershipDescriptor(
                    tenantId: "tenant-001",
                    principalId: "user-002",
                    displayName: "Suspended Member",
                    roles: ["member"],
                    status: TenantMembershipStatuses.Suspended,
                    sourceModuleId: "platform-test"));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<ITenantMembershipCatalog>();
        var evaluator = provider.GetRequiredService<ITenantMembershipEvaluator>();
        var runtime = provider.GetRequiredService<global::Cephalon.Engine.Runtime.IRuntime>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var membershipsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-memberships");
        var summaryEntry = Assert.Single(membershipsSurface.Entries, entry => entry.Id == "tenant-membership-runtime");
        var tenantEntry = Assert.Single(membershipsSurface.Entries, entry => entry.Id == "tenant-membership:tenant-001");
        var diagnosticsConvention = Assert.Single(diagnosticsCatalog.GetBySource("Cephalon.MultiTenancy.Governance"));

        var allowed = await evaluator.EvaluateAsync(new TenantMembershipEvaluationRequest(
            tenantId: "tenant-001",
            principalId: "user-001",
            requiredRoles: ["admin"]));
        var missingRole = await evaluator.EvaluateAsync(new TenantMembershipEvaluationRequest(
            tenantId: "tenant-001",
            principalId: "user-001",
            requiredRoles: ["owner"]));
        var suspended = await evaluator.EvaluateAsync(new TenantMembershipEvaluationRequest(
            tenantId: "tenant-001",
            principalId: "user-002"));

        Assert.Equal(2, catalog.Memberships.Count);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.membership.catalog");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.membership.evaluation");
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["ownership"]);
        Assert.Equal("Cephalon.MultiTenancy.Governance", summaryEntry.Metadata["package"]);
        Assert.Equal("2", summaryEntry.Metadata["membershipCount"]);
        Assert.Equal("true", summaryEntry.Metadata["evaluationEnabled"]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["evaluationOwnership"]);
        Assert.Equal("2", tenantEntry.Metadata["membershipCount"]);
        Assert.Equal("1", tenantEntry.Metadata["activeMembershipCount"]);
        Assert.Equal("1", tenantEntry.Metadata["suspendedMembershipCount"]);
        Assert.Equal("admin,member", tenantEntry.Metadata["roles"]);
        Assert.True(allowed.Allowed);
        Assert.Equal(TenantMembershipEvaluationOutcomes.Allowed, allowed.Outcome);
        Assert.False(missingRole.Allowed);
        Assert.Equal(TenantMembershipEvaluationOutcomes.MissingRole, missingRole.Outcome);
        Assert.Equal(["owner"], missingRole.MissingRoles);
        Assert.False(suspended.Allowed);
        Assert.Equal(TenantMembershipEvaluationOutcomes.Suspended, suspended.Outcome);
        Assert.Equal(4510, diagnosticsConvention.MinimumEventId);
        Assert.Equal(4511, diagnosticsConvention.MaximumEventId);
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4510 && entry.Name == "TenantMembershipEvaluationAllowed");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4511 && entry.Name == "TenantMembershipEvaluationDenied");
    }

    [Fact]
    public async Task TenantMembershipCatalogMergesContributorMemberships()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantMembershipContributor>(new TestTenantMembershipContributor());
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddModule(new PlatformTestModule());
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Memberships.Add(new TenantMembershipDescriptor(
                    tenantId: "tenant-001",
                    principalId: "user-001",
                    roles: ["member"]));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<ITenantMembershipCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var membershipsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-memberships");
        var summaryEntry = Assert.Single(membershipsSurface.Entries, entry => entry.Id == "tenant-membership-runtime");

        Assert.Equal(2, catalog.Memberships.Count);
        Assert.Single(catalog.GetByTenantId("tenant-002"));
        Assert.Single(catalog.GetByPrincipalId("group-001"));
        Assert.Single(catalog.GetByTenantAndPrincipal("tenant-002", "group-001"));
        Assert.Single(catalog.GetByTenantPrincipalAndKind("tenant-002", "group", "group-001"));
        Assert.Empty(catalog.GetByTenantPrincipalAndKind("tenant-002", "user", "group-001"));
        Assert.Equal("1", summaryEntry.Metadata["contributorCount"]);
        Assert.Contains(membershipsSurface.Entries, entry =>
            entry.Id == "tenant-membership:tenant-002" &&
            entry.Metadata["principalKindBreakdown"] == "group:1");
    }

    [Fact]
    public async Task TenantMembershipEvaluationUsesPrincipalKindToPreventCrossKindRoleBleed()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Memberships.Add(new TenantMembershipDescriptor(
                    tenantId: "tenant-001",
                    principalId: "shared-principal",
                    principalKind: "user",
                    roles: ["member"]));
                options.Memberships.Add(new TenantMembershipDescriptor(
                    tenantId: "tenant-001",
                    principalId: "shared-principal",
                    principalKind: "service",
                    roles: ["admin"]));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var evaluator = provider.GetRequiredService<ITenantMembershipEvaluator>();

        var userResult = await evaluator.EvaluateAsync(new TenantMembershipEvaluationRequest(
            tenantId: "tenant-001",
            principalId: "shared-principal",
            requiredRoles: ["admin"]));
        var serviceResult = await evaluator.EvaluateAsync(new TenantMembershipEvaluationRequest(
            tenantId: "tenant-001",
            principalId: "shared-principal",
            requiredRoles: ["admin"],
            principalKind: "service"));

        Assert.False(userResult.Allowed);
        Assert.Equal("user", userResult.PrincipalKind);
        Assert.Equal(TenantMembershipEvaluationOutcomes.MissingRole, userResult.Outcome);
        Assert.True(serviceResult.Allowed);
        Assert.Equal("service", serviceResult.PrincipalKind);
        Assert.Equal(TenantMembershipEvaluationOutcomes.Allowed, serviceResult.Outcome);
    }

    private sealed class TestTenantMembershipContributor : ITenantMembershipContributor
    {
        public void RegisterMemberships(ITenantMembershipRegistry memberships)
        {
            memberships.Add(new TenantMembershipDescriptor(
                tenantId: "tenant-002",
                principalId: "group-001",
                principalKind: "group",
                roles: ["support"],
                sourceModuleId: "test-module"));
        }
    }
}
