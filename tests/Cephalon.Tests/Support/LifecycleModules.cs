using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Support;

internal sealed class LifecycleRecorder
{
    private readonly List<string> events = [];

    public IReadOnlyList<string> Events => events;

    public void Record(string value)
    {
        events.Add(value);
    }
}

internal sealed class LifecyclePlatformModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "lifecycle-platform",
        displayName: "Lifecycle Platform",
        description: "Lifecycle ordering test platform module.",
        version: "0.1.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<LifecycleRecorder>().Record("initialize:platform");
        return Task.CompletedTask;
    }

    public override Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<LifecycleRecorder>().Record("start:platform");
        return Task.CompletedTask;
    }

    public override Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<LifecycleRecorder>().Record("stop:platform");
        return Task.CompletedTask;
    }
}

internal sealed class LifecycleDiscoveryModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "lifecycle-discovery",
        displayName: "Lifecycle Discovery",
        description: "Lifecycle ordering test discovery module.",
        dependsOn: [typeof(LifecyclePlatformModule)],
        version: "0.1.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<LifecycleRecorder>().Record("initialize:discovery");
        return Task.CompletedTask;
    }

    public override Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<LifecycleRecorder>().Record("start:discovery");
        return Task.CompletedTask;
    }

    public override Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<LifecycleRecorder>().Record("stop:discovery");
        return Task.CompletedTask;
    }
}
