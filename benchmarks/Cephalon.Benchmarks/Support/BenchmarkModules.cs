using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.Support;

internal interface IBenchmarkClock
{
    DateTimeOffset GetUtcNow();
}

internal sealed class BenchmarkClock : IBenchmarkClock
{
    private static readonly DateTimeOffset FixedUtcNow = new(2042, 4, 2, 10, 0, 0, TimeSpan.Zero);

    public DateTimeOffset GetUtcNow()
    {
        return FixedUtcNow;
    }
}

internal sealed class BenchmarkClockModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "platform",
        displayName: "Platform",
        description: "Benchmark foundation services.",
        tags: ["foundation", "benchmark"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "foundation"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IBenchmarkClock, BenchmarkClock>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "platform.clock",
            displayName: "Platform Clock",
            description: "Deterministic benchmark clock.",
            metadata: new Dictionary<string, string>
            {
                ["kind"] = "clock"
            }));
    }
}

internal sealed class BenchmarkDiscoveryModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "discovery",
        displayName: "Discovery",
        description: "Benchmark discovery surface.",
        dependsOn: [typeof(BenchmarkClockModule)],
        tags: ["experience", "benchmark"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "experience"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "discovery.greetings",
            displayName: "Greeting Discovery",
            description: "Discovery greeting capability.",
            metadata: new Dictionary<string, string>
            {
                ["dependsOn"] = "platform.clock"
            }));
    }
}

internal sealed class BenchmarkOperationsModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "operations",
        displayName: "Operations",
        description: "Benchmark operations workflows.",
        dependsOn: [typeof(BenchmarkClockModule), typeof(BenchmarkDiscoveryModule)],
        tags: ["operations", "benchmark"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "application"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "operations.dispatch",
            displayName: "Operations Dispatch",
            description: "Dispatch workflow capability.",
            metadata: new Dictionary<string, string>
            {
                ["dependsOn"] = "discovery.greetings"
            }));
    }
}

internal sealed class BenchmarkExperienceModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "experience",
        displayName: "Experience",
        description: "Benchmark experience endpoints.",
        dependsOn: [typeof(BenchmarkOperationsModule)],
        tags: ["experience", "benchmark"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "delivery"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "experience.portal",
            displayName: "Experience Portal",
            description: "Delivery-facing capability for benchmark scenarios.",
            metadata: new Dictionary<string, string>
            {
                ["dependsOn"] = "operations.dispatch"
            }));
    }
}
