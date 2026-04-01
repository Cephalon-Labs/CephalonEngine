using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Engine.Runtime;
using Cephalon.WorkerPlayground.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cephalon.WorkerPlayground.Modules;

public sealed class OperationsModule : ModuleBase
{
    private static readonly Action<ILogger, string, string, int, Exception?> LogWorkerOperationsStartedMessage =
        LoggerMessage.Define<string, string, int>(
            LogLevel.Information,
            new EventId(1010, nameof(LogWorkerOperationsStarted)),
            "Worker operations started. Engine {EngineVersion}, blueprint {BlueprintId}, capabilities {CapabilityCount}.");
    private static readonly Action<ILogger, Exception?> LogWorkerOperationsStoppedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1011, nameof(LogWorkerOperationsStopped)),
            "Worker operations stopped.");

    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "worker-operations",
        displayName: "Worker Operations",
        description: "Runs background worker diagnostics and heartbeat reporting.",
        dependsOn: [typeof(PlatformModule)],
        tags: ["operations", "worker"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "operations",
            ["surface"] = "background-service"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddHostedService<HeartbeatService>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "worker.heartbeat",
            displayName: "Worker Heartbeat",
            description: "Emits worker runtime heartbeats for diagnostics.",
            metadata: new Dictionary<string, string>
            {
                ["category"] = "operations",
                ["mode"] = "background"
            }));
    }

    public override Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        var runtime = context.Services.GetRequiredService<IRuntime>();
        var logger = context.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<OperationsModule>();
        var engineVersion = runtime.Manifest.EngineVersion;
        var blueprintId = runtime.Manifest.AppProfile.BlueprintId;
        var capabilityCount = runtime.Manifest.Capabilities.Count;

        LogWorkerOperationsStarted(logger, engineVersion, blueprintId, capabilityCount);

        return Task.CompletedTask;
    }

    public override Task StopAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        var logger = context.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<OperationsModule>();

        LogWorkerOperationsStopped(logger);

        return Task.CompletedTask;
    }

    private static void LogWorkerOperationsStarted(
        ILogger logger,
        string engineVersion,
        string blueprintId,
        int capabilityCount)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogWorkerOperationsStartedMessage(logger, engineVersion, blueprintId, capabilityCount, null);
    }

    private static void LogWorkerOperationsStopped(ILogger logger)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogWorkerOperationsStoppedMessage(logger, null);
    }
}
