using Cephalon.Engine.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.WorkerPlayground.Services;

internal sealed class HeartbeatService : BackgroundService
{
    private static readonly Action<ILogger, RuntimeStatus, int, int, DateTimeOffset?, Exception?> LogWorkerHeartbeatMessage =
        LoggerMessage.Define<RuntimeStatus, int, int, DateTimeOffset?>(
            LogLevel.Information,
            new EventId(1020, nameof(LogWorkerHeartbeat)),
            "Worker heartbeat. Status {Status}. Modules {ModuleCount}. Capabilities {CapabilityCount}. Started {StartedAtUtc}.");

    private readonly ILogger<HeartbeatService> logger;
    private readonly IRuntime runtime;
    private readonly TimeSpan interval;

    public HeartbeatService(
        ILogger<HeartbeatService> logger,
        IRuntime runtime,
        IConfiguration configuration)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        ArgumentNullException.ThrowIfNull(configuration);

        var seconds = configuration.GetValue("Worker:HeartbeatSeconds", 10);
        interval = TimeSpan.FromSeconds(Math.Max(seconds, 1));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var snapshot = runtime.StatusSnapshot;
            var moduleCount = runtime.Manifest.Modules.Count;
            var capabilityCount = runtime.Manifest.Capabilities.Count;

            LogWorkerHeartbeat(
                logger,
                snapshot.Status,
                moduleCount,
                capabilityCount,
                snapshot.StartedAtUtc);
        }
    }

    private static void LogWorkerHeartbeat(
        ILogger logger,
        RuntimeStatus status,
        int moduleCount,
        int capabilityCount,
        DateTimeOffset? startedAtUtc)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogWorkerHeartbeatMessage(logger, status, moduleCount, capabilityCount, startedAtUtc, null);
    }
}
