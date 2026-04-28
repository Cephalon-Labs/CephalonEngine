using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Tenancy;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Registration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class MultiTenancyPackTests
{
    [Fact]
    public async Task AddMultiTenancyRegistersResolverDiagnosticsAndRuntimeSurface()
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
            engine.AddMultiTenancy(options =>
            {
                options.DefaultTenantId = "tenant-001";
                options.Tenants.Add(new TenantContext(
                    tenantId: "tenant-001",
                    tenantKey: "acme",
                    displayName: "Acme",
                    domains: ["acme.example.test"]));
                options.Tenants.Add(new TenantContext(
                    tenantId: "tenant-002",
                    tenantKey: "fabrikam",
                    displayName: "Fabrikam",
                    domains: ["portal.fabrikam.test"]));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<ITenantResolver>();
        var contextAccessor = provider.GetRequiredService<ITenantContextAccessor>();
        var runtime = provider.GetRequiredService<global::Cephalon.Engine.Runtime.IRuntime>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var tenancySurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-resolution");
        var tenancyEntry = Assert.Single(tenancySurface.Entries, entry => entry.Id == "tenant-runtime");
        var governanceBoundarySurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-governance-boundaries");
        var membershipBoundary = Assert.Single(governanceBoundarySurface.Entries, entry => entry.Id == "tenant-membership");
        var administrationBoundary = Assert.Single(governanceBoundarySurface.Entries, entry => entry.Id == "tenant-administration");
        var resolutionCore = Assert.Single(governanceBoundarySurface.Entries, entry => entry.Id == "tenant-resolution-core");
        var diagnosticsConvention = Assert.Single(diagnosticsCatalog.GetBySource("Cephalon.MultiTenancy"));

        Assert.NotNull(resolver);
        Assert.NotNull(contextAccessor);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.resolution");
        Assert.Equal("enabled", tenancyEntry.Metadata["tenancySelection"]);
        Assert.Equal("SharedDatabase", tenancyEntry.Metadata["selectedMode"]);
        Assert.Equal("2", tenancyEntry.Metadata["configuredTenantCount"]);
        Assert.Equal("tenant-001", tenancyEntry.Metadata["defaultTenantId"]);
        Assert.Equal("configured", tenancyEntry.Metadata["domainResolution"]);
        Assert.Equal("configured", tenancyEntry.Metadata["tenantKeyResolution"]);
        Assert.Equal("cephalon-managed", resolutionCore.Metadata["ownership"]);
        Assert.Equal("tenant-resolution", resolutionCore.Metadata["surfaceId"]);
        Assert.Equal("companion-shipped", membershipBoundary.Metadata["ownership"]);
        Assert.Equal("companion-available", membershipBoundary.Metadata["plannedOwnership"]);
        Assert.Equal("not-owned", membershipBoundary.Metadata["basePackageOwnership"]);
        Assert.Equal("Cephalon.MultiTenancy.Governance", membershipBoundary.Metadata["suggestedPackage"]);
        Assert.Equal("tenant-memberships", membershipBoundary.Metadata["surfaceId"]);
        Assert.Equal("requires-companion-registration", membershipBoundary.Metadata["runtimeState"]);
        Assert.Equal("companion-shipped", administrationBoundary.Metadata["ownership"]);
        Assert.Equal("Cephalon.MultiTenancy.Governance", administrationBoundary.Metadata["suggestedPackage"]);
        Assert.Equal("tenant-administration", administrationBoundary.Metadata["surfaceId"]);
        Assert.Equal("tenancy.administration.workflow", administrationBoundary.Metadata["capabilityKey"]);
        Assert.Equal("requires-companion-registration", administrationBoundary.Metadata["runtimeState"]);
        Assert.Equal(4500, diagnosticsConvention.MinimumEventId);
        Assert.Equal(4502, diagnosticsConvention.MaximumEventId);
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4500 && entry.Name == "TenantResolved");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4501 && entry.Name == "TenantResolutionDefaulted");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4502 && entry.Name == "TenantResolutionMissed");
    }

    [Fact]
    public async Task AddMultiTenancyResolvesByRequestedTenantKeyAndHostNameAndSeedsAmbientContext()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddModule(new PlatformTestModule());
            engine.AddMultiTenancy(options =>
            {
                options.Tenants.Add(new TenantContext(
                    tenantId: "tenant-001",
                    tenantKey: "acme",
                    displayName: "Acme"));
                options.Tenants.Add(new TenantContext(
                    tenantId: "tenant-002",
                    tenantKey: "fabrikam",
                    displayName: "Fabrikam",
                    domains: ["portal.fabrikam.test"]));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<ITenantResolver>();
        var contextAccessor = provider.GetRequiredService<ITenantContextAccessor>();

        var requestedTenantResult = await resolver.ResolveAsync(new TenantResolutionRequest(
            requestedTenantKey: "acme",
            userId: "user-001"));
        var hostNameResult = await resolver.ResolveAsync(new TenantResolutionRequest(
            hostName: "portal.fabrikam.test",
            userId: "user-002"));

        Assert.True(requestedTenantResult.IsResolved);
        Assert.Equal("tenant-001", requestedTenantResult.Tenant!.TenantId);
        Assert.Equal("requested-tenant-key", requestedTenantResult.Source);

        Assert.True(hostNameResult.IsResolved);
        Assert.Equal("tenant-002", hostNameResult.Tenant!.TenantId);
        Assert.Equal("host-name", hostNameResult.Source);
        Assert.NotNull(contextAccessor.Current);
        Assert.Equal("tenant-002", contextAccessor.Current!.TenantId);
    }

    [Fact]
    public async Task AddMultiTenancyFallsBackToDefaultTenantAndReportsMisses()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddModule(new PlatformTestModule());
            engine.AddMultiTenancy(options =>
            {
                options.DefaultTenantId = "tenant-001";
                options.Tenants.Add(new TenantContext(
                    tenantId: "tenant-001",
                    tenantKey: "acme",
                    displayName: "Acme"));
                options.Tenants.Add(new TenantContext(
                    tenantId: "tenant-002",
                    tenantKey: "fabrikam",
                    displayName: "Fabrikam"));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<ITenantResolver>();
        var fallbackResult = await resolver.ResolveAsync(new TenantResolutionRequest(
            pathBase: "/backoffice",
            userId: "user-003"));
        var missResult = await resolver.ResolveAsync(new TenantResolutionRequest(
            requestedTenantId: "tenant-999",
            requestedTenantKey: "missing",
            hostName: "missing.example.test",
            userId: "user-004"));

        Assert.True(fallbackResult.IsResolved);
        Assert.Equal("tenant-001", fallbackResult.Tenant!.TenantId);
        Assert.Equal("default-tenant", fallbackResult.Source);

        Assert.False(missResult.IsResolved);
        Assert.Equal("configured-directory", missResult.Source);
        Assert.Contains("matched", missResult.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddMultiTenancyCanDisableTheBuiltInConfigurationDrivenResolver()
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
            engine.AddMultiTenancy(options =>
            {
                options.EnableDefaultResolver = false;
                options.Tenants.Add(new TenantContext(
                    tenantId: "tenant-001",
                    tenantKey: "acme",
                    displayName: "Acme"));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<ITenantResolver>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var tenancySurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-resolution");
        var tenancyEntry = Assert.Single(tenancySurface.Entries, entry => entry.Id == "tenant-runtime");

        var result = await resolver.ResolveAsync(new TenantResolutionRequest(
            requestedTenantId: "tenant-001",
            userId: "user-005"));

        Assert.False(result.IsResolved);
        Assert.Equal("disabled", result.Source);
        Assert.Contains("disabled", result.Reason!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("false", tenancyEntry.Metadata["defaultResolverEnabled"]);
        Assert.Equal("disabled", tenancyEntry.Metadata["resolutionStrategies"]);
    }
}
