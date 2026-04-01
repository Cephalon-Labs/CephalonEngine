using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Engine.Runtime;
using Cephalon.WorkerPlayground.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cephalon.WorkerPlayground.Modules;

public sealed class PlatformModule : ModuleBase
{
    private static readonly Action<ILogger, DateTimeOffset, string, int, Exception?> LogWorkerPlatformStartedMessage =
        LoggerMessage.Define<DateTimeOffset, string, int>(
            LogLevel.Information,
            new EventId(1000, nameof(LogWorkerPlatformStarted)),
            "Worker platform started at {UtcNow}. Manifest {ManifestVersion} with {ModuleCount} modules.");

    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "worker-platform",
        displayName: "Worker Platform",
        description: "Provides worker-host operational services.",
        tags: ["foundation", "worker"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "foundation",
            ["host"] = "worker"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IWorkerClock, SystemWorkerClock>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "worker.clock",
            displayName: "Worker Clock",
            description: "Provides runtime timestamps for worker modules.",
            metadata: new Dictionary<string, string>
            {
                ["category"] = "foundation",
                ["surface"] = "service"
            }));
    }

    public override Task StartAsync(ModuleContext context, CancellationToken cancellationToken)
    {
        var runtime = context.Services.GetRequiredService<IRuntime>();
        var clock = context.Services.GetRequiredService<IWorkerClock>();
        var logger = context.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<PlatformModule>();
        var utcNow = clock.GetUtcNow();
        var manifestVersion = runtime.Manifest.ManifestVersion;
        var moduleCount = runtime.Manifest.Modules.Count;

        LogWorkerPlatformStarted(logger, utcNow, manifestVersion, moduleCount);

        return Task.CompletedTask;
    }

    private static void LogWorkerPlatformStarted(
        ILogger logger,
        DateTimeOffset utcNow,
        string manifestVersion,
        int moduleCount)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogWorkerPlatformStartedMessage(logger, utcNow, manifestVersion, moduleCount, null);
    }
}
