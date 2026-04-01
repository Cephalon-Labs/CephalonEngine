using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Health;
using Cephalon.Abstractions.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Support;

internal sealed class DependencyHealthModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "dependency-health",
        displayName: "Dependency Health",
        description: "Provides dependency health coverage for tests.",
        tags: ["operations"]);

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDependencyHealthContributor, TestDependencyHealthContributor>();
    }
}

internal sealed class ThrowingDependencyHealthModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "throwing-dependency-health",
        displayName: "Throwing Dependency Health",
        description: "Simulates a broken dependency health contributor.",
        tags: ["operations"]);

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDependencyHealthContributor, ThrowingDependencyHealthContributor>();
    }
}

internal sealed class TestDependencyHealthContributor : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth()
    {
        return
        [
            new DependencyHealthReport(
                Id: "primary-sql",
                DisplayName: "Primary SQL",
                State: HealthState.Unhealthy,
                Description: "Primary SQL is unreachable.",
                Required: true,
                Source: "dependency-health"),
            new DependencyHealthReport(
                Id: "search-index",
                DisplayName: "Search Index",
                State: HealthState.Degraded,
                Description: "Search index is rebuilding.",
                Required: false,
                Source: "dependency-health")
        ];
    }
}

internal sealed class ThrowingDependencyHealthContributor : IDependencyHealthContributor
{
    public IReadOnlyList<DependencyHealthReport> GetDependencyHealth()
    {
        throw new InvalidOperationException("Simulated dependency health contributor failure.");
    }
}
