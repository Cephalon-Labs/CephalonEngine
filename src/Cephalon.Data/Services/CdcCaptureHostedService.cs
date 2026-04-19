using System.Globalization;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Data.Services;

internal sealed class CdcCaptureHostedService(
    IServiceScopeFactory scopeFactory,
    DataRuntimeOptions options,
    ILogger<CdcCaptureHostedService> logger) : BackgroundService
{
    private static readonly Action<ILogger, int, Exception?> LogCaptureLoopStartedMessage =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(6200, nameof(LogCaptureLoopStarted)),
            "Shared CDC capture loop started with polling interval {PollingIntervalSeconds}s.");
    private static readonly Action<ILogger, Exception?> LogCaptureLoopStoppedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(6201, nameof(LogCaptureLoopStopped)),
            "Shared CDC capture loop stopped.");
    private static readonly Action<ILogger, string, string, Exception?> LogMissingCaptureImplementationMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(6202, nameof(LogMissingCaptureImplementation)),
            "CDC capture '{CdcCaptureId}' is active in the runtime catalog, but no ICdcCapture implementation was registered for provider '{Provider}'.");
    private static readonly Action<ILogger, string, string, Exception?> LogMissingOutboxBindingMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(6203, nameof(LogMissingOutboxBinding)),
            "CDC capture '{CdcCaptureId}' resolved, but outbox '{OutboxId}' does not have an active IOutbox binding.");
    private static readonly Action<ILogger, string, Exception?> LogUnknownCaptureImplementationMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(6204, nameof(LogUnknownCaptureImplementation)),
            "ICdcCapture implementation '{CdcCaptureId}' is registered, but no matching CdcCaptureDescriptor is active in the runtime catalog.");
    private static readonly Action<ILogger, string, string, int, int, Exception?> LogCaptureBatchSucceededMessage =
        LoggerMessage.Define<string, string, int, int>(
            LogLevel.Debug,
            new EventId(6205, nameof(LogCaptureBatchSucceeded)),
            "CDC capture '{CdcCaptureId}' staged {ProducedMessageCount} outbox publication(s) from {CapturedChangeCount} captured change(s) through outbox '{OutboxId}'.");
    private static readonly Action<ILogger, string, string, Exception?> LogCaptureBatchFailedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(6206, nameof(LogCaptureBatchFailed)),
            "CDC capture '{CdcCaptureId}' failed while running through outbox '{OutboxId}'.");

    private readonly HashSet<string> reportedConfigurationFailures = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> reportedImplementationWarnings = new(StringComparer.OrdinalIgnoreCase);

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.EnableCdcExecution)
        {
            return;
        }

        LogCaptureLoopStarted(logger, Math.Max(1, options.CdcPollingIntervalSeconds));
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!options.EnableCdcExecution)
        {
            return;
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        LogCaptureLoopStopped(logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.EnableCdcExecution)
        {
            return;
        }

        await CaptureAvailableAsync(stoppingToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, options.CdcPollingIntervalSeconds)));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await CaptureAvailableAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task CaptureAvailableAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var capturesById = IndexCaptures(scope.ServiceProvider.GetServices<ICdcCapture>());
        var outboxesById = IndexOutboxes(scope.ServiceProvider.GetServices<IOutbox>());
        var captureCatalog = scope.ServiceProvider.GetRequiredService<ICdcCaptureCatalog>();
        var reporter = scope.ServiceProvider.GetRequiredService<ICdcCaptureRuntimeReporter>();
        var descriptors = captureCatalog.CdcCaptures
            .OrderBy(static descriptor => descriptor.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static descriptor => descriptor.Provider, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var capture in capturesById.Keys.Except(
                     descriptors.Select(static descriptor => descriptor.Id),
                     StringComparer.OrdinalIgnoreCase))
        {
            if (reportedImplementationWarnings.Add(capture))
            {
                LogUnknownCaptureImplementation(logger, capture);
            }
        }

        foreach (var descriptor in descriptors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await CaptureDescriptorAsync(descriptor, capturesById, outboxesById, reporter, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task CaptureDescriptorAsync(
        CdcCaptureDescriptor descriptor,
        Dictionary<string, ICdcCapture> capturesById,
        Dictionary<string, IOutbox> outboxesById,
        ICdcCaptureRuntimeReporter reporter,
        CancellationToken cancellationToken)
    {
        if (!capturesById.TryGetValue(descriptor.Id, out var capture))
        {
            if (reportedConfigurationFailures.Add($"{descriptor.Id}:missing-capture"))
            {
                LogMissingCaptureImplementation(logger, descriptor.Id, descriptor.Provider);
                await reporter.ReportAsync(
                    new CdcCaptureExecutionReport(
                        cdcCaptureId: descriptor.Id,
                        outcome: CdcCaptureRuntimeOutcomes.Failed,
                        observedAtUtc: DateTimeOffset.UtcNow,
                        error: $"CDC capture '{descriptor.Id}' is active in the runtime catalog, but no ICdcCapture implementation is registered.",
                        metadata: CreateConfigurationMetadata(descriptor, "missing-capture")),
                    cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (!outboxesById.TryGetValue(descriptor.OutboxId, out var outbox))
        {
            if (reportedConfigurationFailures.Add($"{descriptor.Id}:missing-outbox"))
            {
                LogMissingOutboxBinding(logger, descriptor.Id, descriptor.OutboxId);
                await reporter.ReportAsync(
                    new CdcCaptureExecutionReport(
                        cdcCaptureId: descriptor.Id,
                        outcome: CdcCaptureRuntimeOutcomes.Failed,
                        observedAtUtc: DateTimeOffset.UtcNow,
                        error: $"CDC capture '{descriptor.Id}' resolved, but outbox '{descriptor.OutboxId}' does not have an active IOutbox binding.",
                        metadata: CreateConfigurationMetadata(descriptor, "missing-outbox")),
                    cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        await reporter.ReportAsync(
            new CdcCaptureExecutionReport(
                cdcCaptureId: descriptor.Id,
                outcome: CdcCaptureRuntimeOutcomes.Started,
                observedAtUtc: DateTimeOffset.UtcNow,
                metadata: CreateExecutionMetadata(descriptor, capture, outbox, null)),
            cancellationToken).ConfigureAwait(false);

        CdcCaptureExecutionResult result;
        try
        {
            result = await capture.CaptureAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogCaptureBatchFailed(logger, descriptor.Id, descriptor.OutboxId, exception);
            await reporter.ReportAsync(
                new CdcCaptureExecutionReport(
                    cdcCaptureId: descriptor.Id,
                    outcome: CdcCaptureRuntimeOutcomes.Failed,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    error: exception.Message,
                    metadata: CreateExecutionMetadata(
                        descriptor,
                        capture,
                        outbox,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["failureKind"] = "capture"
                        })),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var stagedMessageCount = 0;
        try
        {
            foreach (var message in result.Messages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await outbox.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
                stagedMessageCount++;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogCaptureBatchFailed(logger, descriptor.Id, descriptor.OutboxId, exception);
            await reporter.ReportAsync(
                new CdcCaptureExecutionReport(
                    cdcCaptureId: descriptor.Id,
                    outcome: CdcCaptureRuntimeOutcomes.Failed,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    capturedChangeCount: result.CapturedChangeCount,
                    producedMessageCount: stagedMessageCount,
                    changeId: result.ChangeId,
                    checkpoint: result.Checkpoint,
                    error: exception.Message,
                    freshness: result.Freshness,
                    lag: result.Lag,
                    publication: CreateDefaultPublication(result.Publication, stagedMessageCount),
                    metadata: CreateExecutionMetadata(
                        descriptor,
                        capture,
                        outbox,
                        result.Metadata,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["failureKind"] = "outbox-stage"
                        })),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var outcome = result.CapturedChangeCount > 0 || stagedMessageCount > 0
            ? CdcCaptureRuntimeOutcomes.Captured
            : CdcCaptureRuntimeOutcomes.Idle;
        var publication = CreateDefaultPublication(result.Publication, stagedMessageCount);

        await reporter.ReportAsync(
            new CdcCaptureExecutionReport(
                cdcCaptureId: descriptor.Id,
                outcome: outcome,
                observedAtUtc: DateTimeOffset.UtcNow,
                capturedChangeCount: result.CapturedChangeCount,
                producedMessageCount: stagedMessageCount,
                changeId: result.ChangeId,
                checkpoint: result.Checkpoint,
                freshness: result.Freshness,
                lag: result.Lag,
                publication: publication,
                metadata: CreateExecutionMetadata(
                    descriptor,
                    capture,
                    outbox,
                    result.Metadata,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["capturedChangeCount"] = result.CapturedChangeCount.ToString(CultureInfo.InvariantCulture),
                        ["producedMessageCount"] = stagedMessageCount.ToString(CultureInfo.InvariantCulture)
                    })),
            cancellationToken).ConfigureAwait(false);

        if (outcome == CdcCaptureRuntimeOutcomes.Captured)
        {
            LogCaptureBatchSucceeded(logger, descriptor.Id, descriptor.OutboxId, result.CapturedChangeCount, stagedMessageCount);
        }
    }

    private static Dictionary<string, ICdcCapture> IndexCaptures(IEnumerable<ICdcCapture> captures)
    {
        ArgumentNullException.ThrowIfNull(captures);

        var index = new Dictionary<string, ICdcCapture>(StringComparer.OrdinalIgnoreCase);
        foreach (var capture in captures)
        {
            if (!index.TryAdd(capture.CdcCaptureId, capture))
            {
                throw new InvalidOperationException(
                    $"CDC capture '{capture.CdcCaptureId}' is implemented by multiple ICdcCapture services.");
            }
        }

        return index;
    }

    private static Dictionary<string, IOutbox> IndexOutboxes(IEnumerable<IOutbox> outboxes)
    {
        ArgumentNullException.ThrowIfNull(outboxes);

        var index = new Dictionary<string, IOutbox>(StringComparer.OrdinalIgnoreCase);
        foreach (var outbox in outboxes)
        {
            if (!index.TryAdd(outbox.OutboxId, outbox))
            {
                throw new InvalidOperationException(
                    $"Outbox '{outbox.OutboxId}' is implemented by multiple IOutbox services.");
            }
        }

        return index;
    }

    private static CdcCapturePublicationStatus? CreateDefaultPublication(
        CdcCapturePublicationStatus? publication,
        int stagedMessageCount)
    {
        if (publication is not null || stagedMessageCount <= 0)
        {
            return publication;
        }

        return new CdcCapturePublicationStatus(
            CdcCapturePublicationStates.PendingPublication,
            pendingPublicationCount: stagedMessageCount,
            description: "The shared CDC execution pump staged publications through the linked outbox.");
    }

    private static Dictionary<string, string> CreateConfigurationMetadata(
        CdcCaptureDescriptor descriptor,
        string failureKind)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["captureExecution"] = "shared-data-runtime",
            ["failureKind"] = failureKind,
            ["provider"] = descriptor.Provider,
            ["outboxId"] = descriptor.OutboxId,
            ["hostedExecutionId"] = DataRuntimeIds.CdcHostedExecutionId,
            ["executionGraphId"] = DataRuntimeIds.CdcExecutionGraphId
        };
    }

    private static Dictionary<string, string> CreateExecutionMetadata(
        CdcCaptureDescriptor descriptor,
        ICdcCapture capture,
        IOutbox outbox,
        IReadOnlyDictionary<string, string>? metadata,
        IReadOnlyDictionary<string, string>? overrides = null)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["captureExecution"] = "shared-data-runtime",
            ["provider"] = descriptor.Provider,
            ["outboxId"] = descriptor.OutboxId,
            ["captureServiceType"] = capture.GetType().FullName ?? capture.GetType().Name,
            ["outboxServiceType"] = outbox.GetType().FullName ?? outbox.GetType().Name,
            ["hostedExecutionId"] = DataRuntimeIds.CdcHostedExecutionId,
            ["executionGraphId"] = DataRuntimeIds.CdcExecutionGraphId
        };

        if (metadata is not null)
        {
            foreach (var pair in metadata)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        if (overrides is not null)
        {
            foreach (var pair in overrides)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        return merged;
    }

    private static void LogCaptureLoopStarted(ILogger logger, int pollingIntervalSeconds) =>
        LogCaptureLoopStartedMessage(logger, pollingIntervalSeconds, null);

    private static void LogCaptureLoopStopped(ILogger logger) =>
        LogCaptureLoopStoppedMessage(logger, null);

    private static void LogMissingCaptureImplementation(ILogger logger, string cdcCaptureId, string provider) =>
        LogMissingCaptureImplementationMessage(logger, cdcCaptureId, provider, null);

    private static void LogMissingOutboxBinding(ILogger logger, string cdcCaptureId, string outboxId) =>
        LogMissingOutboxBindingMessage(logger, cdcCaptureId, outboxId, null);

    private static void LogUnknownCaptureImplementation(ILogger logger, string cdcCaptureId) =>
        LogUnknownCaptureImplementationMessage(logger, cdcCaptureId, null);

    private static void LogCaptureBatchSucceeded(
        ILogger logger,
        string cdcCaptureId,
        string outboxId,
        int capturedChangeCount,
        int producedMessageCount) =>
        LogCaptureBatchSucceededMessage(logger, cdcCaptureId, outboxId, capturedChangeCount, producedMessageCount, null);

    private static void LogCaptureBatchFailed(ILogger logger, string cdcCaptureId, string outboxId, Exception exception) =>
        LogCaptureBatchFailedMessage(logger, cdcCaptureId, outboxId, exception);
}
