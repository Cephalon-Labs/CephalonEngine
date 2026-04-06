using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.Tenancy;
using Cephalon.Benchmarks.Support;
using Cephalon.Data.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Wolverine.Registration;
using Cephalon.Identity.Registration;
using Cephalon.Ids.Sfid.Registration;
using Cephalon.MultiTenancy.Registration;
using Cephalon.Audit.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.HotPath;

/// <summary>
/// Measures the per-call overhead of the built-in configuration-driven tenant resolver.
/// The resolver scans its tenant directory using various resolution strategies
/// (explicit id, tenant key, hostname). These benchmarks measure each resolution path
/// and the default-tenant fallback.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class TenantResolutionBenchmarks
{
    private const int ResolutionsPerIteration = 8192;

    private ServiceProvider provider = null!;
    private ITenantResolver resolver = null!;
    private TenantResolutionRequest requestById = null!;
    private TenantResolutionRequest requestByHostName = null!;
    private TenantResolutionRequest requestDefault = null!;

    /// <summary>
    /// Builds the engine with multi-tenancy configured with three tenants, then resolves the tenant resolver.
    /// </summary>
    [GlobalSetup]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        var builder = new EngineBuilder(services);

        builder.UseSettings(new EngineSettings(
            blueprint: "modular-vertical-slice",
            patterns: ["clean-architecture", "ddd", "cqrs"],
            transports: ["rest-api"],
            technologies: ["multi-tenancy"],
            tenancy: new TenancySettings(
                enabled: true,
                mode: "SharedDatabase")));

        builder.AddData();
        builder.AddSfidIds();
        builder.AddEventing();
        builder.AddWolverineEventing();
        builder.AddIdentityAccess();
        builder.AddMultiTenancy(opts =>
        {
            opts.DefaultTenantId = "tenant-alpha";
            opts.Tenants.Add(new TenantContext(
                tenantId: "tenant-alpha",
                tenantKey: "alpha",
                displayName: "Alpha Tenant",
                domains: ["alpha.example.com"]));
            opts.Tenants.Add(new TenantContext(
                tenantId: "tenant-beta",
                tenantKey: "beta",
                displayName: "Beta Tenant",
                domains: ["beta.example.com"]));
            opts.Tenants.Add(new TenantContext(
                tenantId: "tenant-gamma",
                tenantKey: "gamma",
                displayName: "Gamma Tenant",
                domains: ["gamma.example.com"]));
        });
        builder.AddAudit();
        builder.AddModule(new BenchmarkClockModule());

        using var runtime = builder.Build();
        provider = services.BuildServiceProvider();
        await runtime.InitializeAsync(provider);

        resolver = provider.GetRequiredService<ITenantResolver>();

        requestById = new TenantResolutionRequest(requestedTenantId: "tenant-beta");
        requestByHostName = new TenantResolutionRequest(hostName: "gamma.example.com");
        requestDefault = new TenantResolutionRequest();

        // Warm the resolver
        await resolver.ResolveAsync(requestById);
        await resolver.ResolveAsync(requestByHostName);
        await resolver.ResolveAsync(requestDefault);
    }

    /// <summary>
    /// Releases all DI resources.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        provider.Dispose();
    }

    /// <summary>
    /// Resolves a tenant by explicit tenant id, the fastest resolution path (direct id match).
    /// </summary>
    [Benchmark(OperationsPerInvoke = ResolutionsPerIteration)]
    public async Task<int> ResolveByTenantId()
    {
        var resolvedCount = 0;
        for (var i = 0; i < ResolutionsPerIteration; i++)
        {
            var result = await resolver.ResolveAsync(requestById);
            if (result.IsResolved) resolvedCount++;
        }

        return resolvedCount;
    }

    /// <summary>
    /// Resolves a tenant by hostname, which requires domain matching against the tenant directory.
    /// </summary>
    [Benchmark(OperationsPerInvoke = ResolutionsPerIteration)]
    public async Task<int> ResolveByHostName()
    {
        var resolvedCount = 0;
        for (var i = 0; i < ResolutionsPerIteration; i++)
        {
            var result = await resolver.ResolveAsync(requestByHostName);
            if (result.IsResolved) resolvedCount++;
        }

        return resolvedCount;
    }

    /// <summary>
    /// Resolves a tenant with no explicit hints, falling back to the configured default tenant.
    /// </summary>
    [Benchmark(OperationsPerInvoke = ResolutionsPerIteration)]
    public async Task<int> ResolveDefaultTenant()
    {
        var resolvedCount = 0;
        for (var i = 0; i < ResolutionsPerIteration; i++)
        {
            var result = await resolver.ResolveAsync(requestDefault);
            if (result.IsResolved) resolvedCount++;
        }

        return resolvedCount;
    }
}
