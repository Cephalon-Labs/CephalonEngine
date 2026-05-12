using System.Diagnostics;
using Cephalon.Diagnostics.Redaction;
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
    private RedactionPipeline? redactionPipeline;

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
        : this(new DirectScopeFactory(dispatchStore, runtimeReporter, messageBus, redactionPipeline: null), options, logger)
    {
    }

    internal WolverineEventDispatchHostedService(
        WolverineEventingOptions options,
        IEventDispatchStore dispatchStore,
        IEventDispatchRuntimeReporter runtimeReporter,
        IMessageBus messageBus,
        RedactionPipeline? redactionPipeline,
        ILogger<WolverineEventDispatchHostedService> logger)
        : this(new DirectScopeFactory(dispatchStore, runtimeReporter, messageBus, redactionPipeline), options, logger)
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
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var managedSubscriptions = scope.ServiceProvider.GetService<WolverineManagedEventSubscriptionDispatcher>();
        redactionPipeline ??= scope.ServiceProvider.GetService<RedactionPipeline>();

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
            await DispatchAsync(pendingDispatch, dispatchStore, runtimeReporter, messageBus, managedSubscriptions, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchAsync(
        EventDispatchItem item,
        IEventDispatchStore dispatchStore,
        IEventDispatchRuntimeReporter runtimeReporter,
        IMessageBus messageBus,
        WolverineManagedEventSubscriptionDispatcher? managedSubscriptions,
        CancellationToken cancellationToken)
    {
        var attempt = checked(item.DispatchAttemptCount + 1);
        var maxAttempts = WolverineEventingRetryPolicy.GetDispatchMaxAttempts(options);
        var retryDelaySeconds = WolverineEventingRetryPolicy.GetDispatchRetryDelaySeconds(options);
        var deliveryOptions = CreateDeliveryOptions(item, attempt);
        var publication = CreatePublication(item);
        var managedSubscriptionCount = managedSubscriptions?.CountForChannel(item.ChannelId) ?? 0;

        using var activity = WolverineDispatchInstrumentation.Source.StartActivity(
            "wolverine.dispatch",
            ActivityKind.Producer);

        if (activity is not null)
        {
            activity.SetTag("cephalon.message_id", Redact(activity, "cephalon.message_id", item.MessageId));
            activity.SetTag("cephalon.event_type", Redact(activity, "cephalon.event_type", item.EventType));
            activity.SetTag("cephalon.channel_id", Redact(activity, "cephalon.channel_id", item.ChannelId));
            activity.SetTag("cephalon.dispatch_attempt", Redact(activity, "cephalon.dispatch_attempt", attempt));
            if (!string.IsNullOrWhiteSpace(item.CorrelationId))
            {
                activity.SetTag("cephalon.correlation_id", Redact(activity, "cephalon.correlation_id", item.CorrelationId));
            }
            if (!string.IsNullOrWhiteSpace(item.TenantId))
            {
                activity.SetTag("cephalon.tenant_id", Redact(activity, "cephalon.tenant_id", item.TenantId));
            }
        }

        WolverineDispatchInstrumentation.DispatchAttempts.Add(1);
        var stopwatch = Stopwatch.StartNew();

        await ApplyObservationAsync(
            dispatchStore,
            runtimeReporter,
            CreateReport(
                item,
                attempt,
                EventDispatchExecutionOutcomes.Started,
                metadata: CreateObservationMetadata(item, deliveryOptions, managedSubscriptionCount, maxAttempts, retryDelaySeconds)),
            cancellationToken).ConfigureAwait(false);

        try
        {
            var destinations = messageBus.PreviewSubscriptions(publication, deliveryOptions);
            if (destinations.Count == 0 && managedSubscriptionCount == 0)
            {
                WolverineDispatchInstrumentation.DispatchRetries.Add(1);
                activity?.SetTag("cephalon.dispatch_result", Redact(activity, "cephalon.dispatch_result", "no-destinations"));
                activity?.SetStatus(ActivityStatusCode.Error, "No configured destinations");

                if (attempt >= maxAttempts)
                {
                    await ApplyObservationAsync(
                        dispatchStore,
                        runtimeReporter,
                        CreateTerminalFailureReport(
                            item,
                            attempt,
                            "Wolverine does not have any configured destinations for Cephalon event publications.",
                            deliveryOptions,
                            managedSubscriptionCount,
                            maxAttempts,
                            retryDelaySeconds,
                            null),
                        cancellationToken).ConfigureAwait(false);
                    return;
                }

                await ApplyObservationAsync(
                    dispatchStore,
                    runtimeReporter,
                    CreateRetryReport(
                        item,
                        attempt,
                        "Wolverine does not have any configured destinations for Cephalon event publications.",
                        deliveryOptions,
                        managedSubscriptionCount,
                        maxAttempts,
                        retryDelaySeconds,
                        null),
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            if (destinations.Count > 0)
            {
                await messageBus.PublishAsync(publication, deliveryOptions).ConfigureAwait(false);
            }

            if (managedSubscriptionCount > 0)
            {
                await managedSubscriptions!.DispatchAsync(publication, messageBus, cancellationToken).ConfigureAwait(false);
            }

            stopwatch.Stop();
            WolverineDispatchInstrumentation.DispatchSuccesses.Add(1);
            WolverineDispatchInstrumentation.DispatchDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
            activity?.SetTag("cephalon.dispatch_result", Redact(activity, "cephalon.dispatch_result", "succeeded"));

            await ApplyObservationAsync(
                dispatchStore,
                runtimeReporter,
                CreateReport(
                    item,
                    attempt,
                    EventDispatchExecutionOutcomes.Succeeded,
                    metadata: CreateObservationMetadata(item, deliveryOptions, managedSubscriptionCount, maxAttempts, retryDelaySeconds)),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            WolverineDispatchInstrumentation.DispatchFailures.Add(1);
            WolverineDispatchInstrumentation.DispatchRetries.Add(1);
            WolverineDispatchInstrumentation.DispatchDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.SetTag("cephalon.dispatch_result", Redact(activity, "cephalon.dispatch_result", "failed"));

            if (attempt >= maxAttempts)
            {
                await ApplyObservationAsync(
                    dispatchStore,
                    runtimeReporter,
                    CreateTerminalFailureReport(
                        item,
                        attempt,
                        exception.Message,
                        deliveryOptions,
                        managedSubscriptionCount,
                        maxAttempts,
                        retryDelaySeconds,
                        exception),
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            await ApplyObservationAsync(
                dispatchStore,
                runtimeReporter,
                CreateRetryReport(item, attempt, exception.Message, deliveryOptions, managedSubscriptionCount, maxAttempts, retryDelaySeconds, exception),
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
        var observedReport = dispatchStore is IEventDispatchProviderContextPersistenceStore contextPersistenceStore
            ? contextPersistenceStore.CreatePersistedContextReport(report)
            : report;

        try
        {
            await runtimeReporter.ReportAsync(observedReport, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogRuntimeObservationProjectionFailed(logger, observedReport.Outcome, observedReport.MessageId ?? "<not-reported>", exception);
        }
    }

    private static EventDispatchExecutionReport CreateRetryReport(
        EventDispatchItem item,
        int attempt,
        string error,
        DeliveryOptions deliveryOptions,
        int managedSubscriptionCount,
        int maxAttempts,
        int retryDelaySeconds,
        Exception? exception)
    {
        var nextRetryAtUtc = DateTimeOffset.UtcNow.AddSeconds(retryDelaySeconds);
        var retryMetadata = CreateObservationMetadata(
            item,
            deliveryOptions,
            managedSubscriptionCount,
            maxAttempts,
            retryDelaySeconds,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [EventDispatchRuntimeMetadataKeys.NextRetryAtUtc] = nextRetryAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                [EventDispatchRuntimeMetadataKeys.RetryOutcome] = "retry-scheduled",
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

    private static EventDispatchExecutionReport CreateTerminalFailureReport(
        EventDispatchItem item,
        int attempt,
        string error,
        DeliveryOptions deliveryOptions,
        int managedSubscriptionCount,
        int maxAttempts,
        int retryDelaySeconds,
        Exception? exception)
    {
        var metadata = CreateObservationMetadata(
            item,
            deliveryOptions,
            managedSubscriptionCount,
            maxAttempts,
            retryDelaySeconds,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [EventDispatchRuntimeMetadataKeys.RetryOutcome] = "max-attempts-exhausted",
                [EventDispatchRuntimeMetadataKeys.RetryExhausted] = "true",
                [EventDispatchRuntimeMetadataKeys.TerminalFailure] = "true",
                ["routing"] = exception is null ? "no-destinations" : "publish"
            });

        if (exception is not null)
        {
            metadata["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name;
        }

        return CreateReport(
            item,
            attempt,
            EventDispatchExecutionOutcomes.Failed,
            error: error,
            metadata: metadata);
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

        foreach (var pair in EventDispatchProviderBrokerContextHeaders.Create(item).OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
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
        int managedSubscriptionCount,
        int maxAttempts,
        int retryDelaySeconds,
        IReadOnlyDictionary<string, string>? overrides = null)
    {
        var deliveryMode = managedSubscriptionCount > 0
            ? "publish-and-subscribe"
            : "publish";
        var metadata = EventDispatchContextReportMetadata.Create(
            item,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["publisherId"] = WolverineEventingRuntimeIds.PublisherId,
                ["eventDispatchRuntimeId"] = WolverineEventingRuntimeIds.DispatchRuntimeId,
                ["dispatchBridge"] = "wolverine-managed",
                ["dispatchOwnership"] = "wolverine-managed",
                ["dispatchMode"] = "publish-event-publication",
                ["deliveryMode"] = deliveryMode,
                ["transport"] = "wolverine",
                ["channelId"] = item.ChannelId,
                [EventDispatchRuntimeMetadataKeys.RetryPolicy] = maxAttempts > 1 ? WolverineEventingRetryPolicy.BoundedFixedDelay : WolverineEventingRetryPolicy.None,
                [EventDispatchRuntimeMetadataKeys.RetryMaxAttempts] = maxAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [EventDispatchRuntimeMetadataKeys.RetryDelaySeconds] = retryDelaySeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [EventDispatchRuntimeMetadataKeys.RetryDurability] = "dispatch-store-delayed-eligibility",
                [EventDispatchRuntimeMetadataKeys.RetryScope] = "provider-managed",
                ["contentType"] = item.ContentType ?? "not-configured",
                ["headerCount"] = deliveryOptions.Headers.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["managedSubscriptionCount"] = managedSubscriptionCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
        EventDispatchProviderBrokerContextHeaders.ApplyReportMetadata(
            metadata,
            EventDispatchProviderBrokerContextHeaders.Create(item));

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

    private object? Redact(Activity? activity, string attributeKey, object? value)
    {
        if (redactionPipeline is null)
        {
            return value;
        }

        var context = new RedactionContext(
            ActivitySourceName: activity?.Source.Name,
            MeterName: null,
            AttributeKey: attributeKey,
            LoggerCategory: null);
        return redactionPipeline.Filter(context, value);
    }

    private sealed class DirectScopeFactory(
        IEventDispatchStore dispatchStore,
        IEventDispatchRuntimeReporter runtimeReporter,
        IMessageBus messageBus,
        RedactionPipeline? redactionPipeline) : IServiceScopeFactory, IServiceScope, IServiceProvider
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

            if (serviceType == typeof(IMessageBus))
            {
                return messageBus;
            }

            if (serviceType == typeof(RedactionPipeline))
            {
                return redactionPipeline;
            }

            return null;
        }

        public void Dispose()
        {
        }
    }
}
