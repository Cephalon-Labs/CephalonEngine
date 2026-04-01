using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Support;

internal sealed class FailurePolicyRecorder
{
    private int remainingStartFailures = 1;
    private int remainingStopFailures = 1;
    private readonly List<string> events = [];

    public IReadOnlyList<string> Events => events;

    public void Record(string value)
    {
        events.Add(value);
    }

    public bool ShouldFailStart()
    {
        if (remainingStartFailures <= 0)
        {
            return false;
        }

        remainingStartFailures--;
        return true;
    }

    public bool ShouldFailStop()
    {
        if (remainingStopFailures <= 0)
        {
            return false;
        }

        remainingStopFailures--;
        return true;
    }
}

internal sealed class FailurePolicyPlatformModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "failure-platform",
        displayName: "Failure Platform",
        description: "Platform module for failure policy tests.",
        version: "0.1.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<FailurePolicyRecorder>().Record("initialize:platform");
        return Task.CompletedTask;
    }

    public override Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<FailurePolicyRecorder>().Record("start:platform");
        return Task.CompletedTask;
    }

    public override Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<FailurePolicyRecorder>().Record("stop:platform");
        return Task.CompletedTask;
    }
}

internal sealed class FlakyStartModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "flaky-start",
        displayName: "Flaky Start",
        description: "Fails the first startup attempt.",
        dependsOn: [typeof(FailurePolicyPlatformModule)],
        version: "0.1.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "flaky-start.health",
            displayName: "Flaky start health",
            description: "Capability used during failure policy tests."));
    }

    public override Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<FailurePolicyRecorder>().Record("initialize:flaky");
        return Task.CompletedTask;
    }

    public override Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        var recorder = context.Services.GetRequiredService<FailurePolicyRecorder>();
        recorder.Record("start:flaky");

        if (recorder.ShouldFailStart())
        {
            throw new InvalidOperationException("Simulated startup failure.");
        }

        return Task.CompletedTask;
    }

    public override Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<FailurePolicyRecorder>().Record("stop:flaky");
        return Task.CompletedTask;
    }
}

internal sealed class FailingStopModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "failing-stop",
        displayName: "Failing Stop",
        description: "Fails during shutdown for failure policy tests.",
        dependsOn: [typeof(FailurePolicyPlatformModule)],
        version: "0.1.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<FailurePolicyRecorder>().Record("initialize:failing-stop");
        return Task.CompletedTask;
    }

    public override Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<FailurePolicyRecorder>().Record("start:failing-stop");
        return Task.CompletedTask;
    }

    public override Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        var recorder = context.Services.GetRequiredService<FailurePolicyRecorder>();
        recorder.Record("stop:failing-stop");

        if (recorder.ShouldFailStop())
        {
            throw new InvalidOperationException("Simulated stop failure.");
        }

        return Task.CompletedTask;
    }
}

internal sealed class StopObserverModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "stop-observer",
        displayName: "Stop Observer",
        description: "Proves stop best-effort ordering after a failure.",
        dependsOn: [typeof(FailurePolicyPlatformModule)],
        version: "0.1.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<FailurePolicyRecorder>().Record("initialize:observer");
        return Task.CompletedTask;
    }

    public override Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<FailurePolicyRecorder>().Record("start:observer");
        return Task.CompletedTask;
    }

    public override Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        context.Services.GetRequiredService<FailurePolicyRecorder>().Record("stop:observer");
        return Task.CompletedTask;
    }
}
