using Cephalon.Eventing.Services;
using Cephalon.Eventing.Wolverine.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Cephalon.Eventing.Wolverine.Services;

internal sealed class WolverineEventDispatchHostedService(
    IServiceScopeFactory scopeFactory,
    WolverineEventingOptions options,
    IMessageBus messageBus,
    ILogger<WolverineEventDispatchHostedService> logger) : BackgroundService
{
    private static readonly Action<ILogger, int, int, Exception?> LogDispatchLoopStartedMessage =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(WolverineEventingDiagnosticsConventions.DispatchLoopStarted.Id, WolverineEventingDiagnosticsConventions.DispatchLoopStarted.Name),
            WolverineEventingDiagnosticsConventions.DispatchLoopStarted.MessageTemplate);
    private static readonly Action<ILogger, Exception?> LogDispatchLoopStoppedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(WolverineEventingDiagnosticsConventions.DispatchLoopStopped.Id, WolverineEventingDiagnosticsConventions.DispatchLoopStopped.Name),
            WolverineEventingDiagnosticsConventions.DispatchLoopStopped.MessageTemplate);
    private static readonly Action<ILogger, Exception?> LogPendingDispatchReadFailedMessage =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(WolverineEventingDiagnosticsConventions.DispatchReadFailed.Id, WolverineEventingDiagnosticsConventions.DispatchReadFailed.Name),
            WolverineEventingDiagnosticsConventions.DispatchReadFailed.MessageTemplate);
    private static readonly Action<ILogger, string, string, Exception?> LogRuntimeObservationProjectionFailedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(WolverineEventingDiagnosticsConventions.DispatchObservationProjectionFailed.Id, WolverineEventingDiagnosticsConventions.DispatchObservationProjectionFailed.Name),
            WolverineEventingDiagnosticsConventions.DispatchObservationProjectionFailed.MessageTemplate);

    internal WolverineEventDispatchHostedService(
        WolverineEventingOptions options,
        IEventDispatchStore dispatchStore,
        IEventDispatchRuntimeReporter runtimeReporter,
        IMessageBus messageBus,
        ILogger<WolverineEventDispatchHostedService> logger)
        : this(new DirectScopeFactory(dispatchStore, runtimeReporter), options, messageBus, logger)
    {
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.EnableDispatchLoop)
        {
            return;
        }

        LogDispatchLoopStarted(logger, Math.Max(1, options.DispatchBatchSize), Math.Max(1, options.DispatchPollingIntervalSeconds));
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        LogDispatchLoopStopped(logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.EnableDispatchLoop)
        {
            return;
        }

        await DispatchAvailableAsync(stoppingToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, options.DispatchPollingIntervalSeconds)));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await DispatchAvailableAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    internal Task DispatchOnceAsync(CancellationToken cancellationToken = default) =>
        DispatchAvailableAsync(cancellationToken);

    private async Task DispatchAvailableAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<EventDispatchItem> pendingDispatches;
        using var scope = scopeFactory.CreateScope();
        var dispatchStore = scope.ServiceProvider.GetRequiredService<IEventDispatchStore>();
        var runtimeReporter = scope.ServiceProvider.GetRequiredService<IEventDispatchRuntimeReporter>();

        try
        {
            pendingDispatches = await dispatchStore
                .ReadPendingAsync(Math.Max(1, options.DispatchBatchSize), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogPendingDispatchReadFailed(logger, exception);
            return;
        }

        foreach (var pendingDispatch in pendingDispatches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await DispatchAsync(pendingDispatch, dispatchStore, runtimeReporter, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchAsync(
        EventDispatchItem item,
        IEventDispatchStore dispatchStore,
        IEventDispatchRuntimeReporter runtimeReporter,
        CancellationToken cancellationToken)
    {
        var attempt = checked(item.DispatchAttemptCount + 1);
        var deliveryOptions = CreateDeliveryOptions(item, attempt);
        var publication = CreatePublication(item);

        await ApplyObservationAsync(
            dispatchStore,
            runtimeReporter,
            CreateReport(
                item,
                attempt,
                EventDispatchExecutionOutcomes.Started,
                metadata: CreateObservationMetadata(item, deliveryOptions)),
            cancellationToken).ConfigureAwait(false);

        try
        {
            var destinations = messageBus.PreviewSubscriptions(publication, deliveryOptions);
            if (destinations.Count == 0)
            {
                await ApplyObservationAsync(
                    dispatchStore,
                    runtimeReporter,
                    CreateRetryReport(
                        item,
                        attempt,
                        "Wolverine does not have any configured destinations for Cephalon event publications.",
                        deliveryOptions,
                        null),
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            await messageBus.PublishAsync(publication, deliveryOptions).ConfigureAwait(false);

            await ApplyObservationAsync(
                dispatchStore,
                runtimeReporter,
                CreateReport(
                    item,
                    attempt,
                    EventDispatchExecutionOutcomes.Succeeded,
                    metadata: CreateObservationMetadata(item, deliveryOptions)),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await ApplyObservationAsync(
                dispatchStore,
                runtimeReporter,
                CreateRetryReport(item, attempt, exception.Message, deliveryOptions, exception),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ApplyObservationAsync(
        IEventDispatchStore dispatchStore,
        IEventDispatchRuntimeReporter runtimeReporter,
        EventDispatchExecutionReport report,
        CancellationToken cancellationToken)
    {
        await dispatchStore.ApplyReportAsync(report, cancellationToken).ConfigureAwait(false);

        try
        {
            await runtimeReporter.ReportAsync(report, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogRuntimeObservationProjectionFailed(logger, report.Outcome, report.MessageId ?? "<not-reported>", exception);
        }
    }

    private EventDispatchExecutionReport CreateRetryReport(
        EventDispatchItem item,
        int attempt,
        string error,
        DeliveryOptions deliveryOptions,
        Exception? exception)
    {
        var nextRetryAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, options.RetryDelaySeconds));
        var retryMetadata = CreateObservationMetadata(
            item,
            deliveryOptions,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["nextRetryAtUtc"] = nextRetryAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                ["retryPolicy"] = "fixed-delay",
                ["routing"] = exception is null ? "no-destinations" : "publish"
            });
        if (exception is not null)
        {
            retryMetadata["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name;
        }

        return CreateReport(
            item,
            attempt,
            EventDispatchExecutionOutcomes.RetryScheduled,
            error: error,
            metadata: retryMetadata);
    }

    private static EventPublication CreatePublication(EventDispatchItem item)
    {
        return new EventPublication(
            id: item.MessageId,
            channelId: item.ChannelId,
            eventType: item.EventType,
            payload: item.Payload,
            occurredAtUtc: item.OccurredAtUtc,
            contentType: item.ContentType,
            correlationId: item.CorrelationId,
            tenantId: item.TenantId,
            headers: item.Headers,
            metadata: item.Metadata);
    }

    private static EventDispatchExecutionReport CreateReport(
        EventDispatchItem item,
        int attempt,
        string outcome,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new EventDispatchExecutionReport(
            outboxId: item.OutboxId,
            channelId: item.ChannelId,
            outcome: outcome,
            observedAtUtc: DateTimeOffset.UtcNow,
            messageId: item.MessageId,
            attempt: attempt,
            error: error,
            metadata: metadata);
    }

    private static DeliveryOptions CreateDeliveryOptions(EventDispatchItem item, int attempt)
    {
        var options = new DeliveryOptions
        {
            ContentType = item.ContentType,
            DeduplicationId = item.MessageId,
            TenantId = item.TenantId
        };

        foreach (var pair in item.Headers.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            options.Headers[pair.Key] = pair.Value;
        }

        options.Headers["cephalon.message-id"] = item.MessageId;
        options.Headers["cephalon.channel-id"] = item.ChannelId;
        options.Headers["cephalon.event-type"] = item.EventType;
        options.Headers["cephalon.dispatch-attempt"] = attempt.ToString(System.Globalization.CultureInfo.InvariantCulture);
        options.Headers["cephalon.occurred-at-utc"] = item.OccurredAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(item.CorrelationId))
        {
            options.Headers["cephalon.correlation-id"] = item.CorrelationId;
        }

        foreach (var pair in item.Metadata.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            options.Headers[$"cephalon.metadata.{pair.Key}"] = pair.Value;
        }

        return options;
    }

    private static Dictionary<string, string> CreateObservationMetadata(
        EventDispatchItem item,
        DeliveryOptions deliveryOptions,
        IReadOnlyDictionary<string, string>? overrides = null)
    {
        var metadata = new Dictionary<string, string>(item.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["publisherId"] = WolverineEventingRuntimeIds.PublisherId,
            ["eventDispatchRuntimeId"] = WolverineEventingRuntimeIds.DispatchRuntimeId,
            ["dispatchBridge"] = "wolverine-managed",
            ["dispatchOwnership"] = "wolverine-managed",
            ["dispatchMode"] = "publish-event-publication",
            ["deliveryMode"] = "publish",
            ["transport"] = "wolverine",
            ["channelId"] = item.ChannelId,
            ["contentType"] = item.ContentType ?? "not-configured",
            ["headerCount"] = deliveryOptions.Headers.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(item.TenantId))
        {
            metadata["tenantId"] = item.TenantId;
        }

        if (!string.IsNullOrWhiteSpace(item.CorrelationId))
        {
            metadata["correlationId"] = item.CorrelationId;
        }

        if (overrides is not null)
        {
            foreach (var pair in overrides)
            {
                metadata[pair.Key] = pair.Value;
            }
        }

        return metadata;
    }

    private static void LogDispatchLoopStarted(ILogger logger, int batchSize, int pollingIntervalSeconds) =>
        LogDispatchLoopStartedMessage(logger, batchSize, pollingIntervalSeconds, null);

    private static void LogDispatchLoopStopped(ILogger logger) =>
        LogDispatchLoopStoppedMessage(logger, null);

    private static void LogPendingDispatchReadFailed(ILogger logger, Exception exception) =>
        LogPendingDispatchReadFailedMessage(logger, exception);

    private static void LogRuntimeObservationProjectionFailed(ILogger logger, string outcome, string messageId, Exception exception) =>
        LogRuntimeObservationProjectionFailedMessage(logger, outcome, messageId, exception);

    private sealed class DirectScopeFactory(
        IEventDispatchStore dispatchStore,
        IEventDispatchRuntimeReporter runtimeReporter) : IServiceScopeFactory, IServiceScope, IServiceProvider
    {
        public IServiceScope CreateScope() => this;

        public IServiceProvider ServiceProvider => this;

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IEventDispatchStore))
            {
                return dispatchStore;
            }

            if (serviceType == typeof(IEventDispatchRuntimeReporter))
            {
                return runtimeReporter;
            }

            return null;
        }

        public void Dispose()
        {
        }
    }
}
