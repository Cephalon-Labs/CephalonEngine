using System.Globalization;
using Cephalon.Abstractions.Data;
using Cephalon.Data.MySql.Configuration;
using Cephalon.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Data.MySql.Services;

internal sealed class MySqlBinlogCaptureHostedService(
    IServiceScopeFactory scopeFactory,
    MySqlDataOptions options,
    ILogger<MySqlBinlogCaptureHostedService> logger) : BackgroundService
{
    private static readonly Action<ILogger, string, Exception?> LogMissingDataRuntimeServicesMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(7000, nameof(LogMissingDataRuntimeServices)),
            "MySQL binlog capture '{CdcCaptureId}' requires Cephalon.Data runtime services. Pair AddMySqlData(...) with AddData(...) when provider-native CDC is enabled.");
    private static readonly Action<ILogger, string, Exception?> LogMissingDescriptorMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(7001, nameof(LogMissingDescriptor)),
            "MySQL binlog capture '{CdcCaptureId}' is configured, but no active CDC descriptor was contributed.");
    private static readonly Action<ILogger, string, Exception?> LogCaptureLoopFailureMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(7002, nameof(LogCaptureLoopFailure)),
            "MySQL provider-native CDC capture '{CdcCaptureId}' failed and will retry.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.CdcCaptures.Count == 0)
        {
            return;
        }

        var tasks = options.CdcCaptures
            .Select(capture => RunCaptureLoopAsync(capture, stoppingToken))
            .ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task RunCaptureLoopAsync(
        MySqlBinlogCaptureOptions captureOptions,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ICdcCaptureRuntimeReporter? reporter = null;
            CdcCaptureDescriptor? descriptor = null;
            IOutbox? outbox = null;

            try
            {
                using var scope = scopeFactory.CreateScope();
                reporter = scope.ServiceProvider.GetService<ICdcCaptureRuntimeReporter>();
                var catalog = scope.ServiceProvider.GetService<ICdcCaptureCatalog>();
                var transport = scope.ServiceProvider.GetRequiredService<IMySqlBinlogTransport>();

                if (reporter is null || catalog is null)
                {
                    LogMissingDataRuntimeServices(logger, captureOptions.Id);
                    return;
                }

                descriptor = catalog.GetById(captureOptions.Id);
                if (descriptor is null)
                {
                    LogMissingDescriptor(logger, captureOptions.Id);
                    return;
                }

                if (!string.Equals(
                        descriptor.ExecutionBinding.EffectiveExecutionRuntimeId,
                        MySqlDataRuntimeIds.CdcExecutionRuntimeId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                outbox = scope.ServiceProvider.GetServices<IOutbox>()
                    .SingleOrDefault(candidate => string.Equals(candidate.OutboxId, descriptor.OutboxId, StringComparison.OrdinalIgnoreCase));

                if (outbox is null)
                {
                    await ReportFailureAsync(
                            reporter,
                            descriptor,
                            captureOptions,
                            null,
                            "The MySQL provider-native binlog runner could not resolve the linked outbox binding.",
                            "missing-outbox",
                            null,
                            null,
                            0,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await RunIterationAsync(
                            descriptor,
                            captureOptions,
                            outbox,
                            reporter,
                            transport,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                if (reporter is not null && descriptor is not null)
                {
                    var failure = ResolveCaptureFailure(exception);
                    var failureKind = failure is not null
                        ? failure.FailureKind
                        : "capture";
                    var additionalMetadata = failure is not null
                        ? failure.Metadata
                        : null;

                    await ReportFailureAsync(
                            reporter,
                            descriptor,
                            captureOptions,
                            outbox,
                            exception.Message,
                            failureKind,
                            null,
                            null,
                            0,
                            cancellationToken,
                            additionalMetadata)
                        .ConfigureAwait(false);
                }

                LogCaptureLoopFailure(logger, captureOptions.Id, exception);
            }

            await Task.Delay(
                    TimeSpan.FromSeconds(Math.Max(1, captureOptions.PollingIntervalSeconds)),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static MySqlBinlogCaptureFailure? ResolveCaptureFailure(Exception exception)
    {
        foreach (var current in EnumerateExceptionChain(exception))
        {
            if (TryResolveCaptureFailure(current, out var failure))
            {
                return failure;
            }
        }

        return null;
    }

    private static IEnumerable<Exception> EnumerateExceptionChain(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
        }

        var baseException = exception.GetBaseException();
        if (!ReferenceEquals(baseException, exception))
        {
            yield return baseException;
        }
    }

    private static bool TryResolveCaptureFailure(
        Exception exception,
        out MySqlBinlogCaptureFailure? failure)
    {
        if (exception is IMySqlBinlogCaptureFailureMetadata captureFailure
            && !string.IsNullOrWhiteSpace(captureFailure.FailureKind))
        {
            failure = new MySqlBinlogCaptureFailure(captureFailure.FailureKind.Trim(), captureFailure.Metadata);
            return true;
        }

        failure = null;
        return false;
    }

    private async Task RunIterationAsync(
        CdcCaptureDescriptor descriptor,
        MySqlBinlogCaptureOptions captureOptions,
        IOutbox outbox,
        ICdcCaptureRuntimeReporter reporter,
        IMySqlBinlogTransport transport,
        CancellationToken cancellationToken)
    {
        await reporter.ReportAsync(
                new CdcCaptureExecutionReport(
                    cdcCaptureId: descriptor.Id,
                    outcome: CdcCaptureRuntimeOutcomes.Started,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    metadata: CreateExecutionMetadata(descriptor, captureOptions, outbox, null, null)),
                cancellationToken)
            .ConfigureAwait(false);

        var batch = await transport.ReadBatchAsync(captureOptions, descriptor, cancellationToken).ConfigureAwait(false);
        if (batch.Changes.Count == 0)
        {
            await reporter.ReportAsync(
                    new CdcCaptureExecutionReport(
                        cdcCaptureId: descriptor.Id,
                        outcome: CdcCaptureRuntimeOutcomes.Idle,
                        observedAtUtc: DateTimeOffset.UtcNow,
                        freshness: CreateFreshness(captureOptions),
                        lag: CreateLag(batch.HasMoreChanges),
                        metadata: CreateExecutionMetadata(
                            descriptor,
                            captureOptions,
                            outbox,
                            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["acknowledgement"] = "provider-native"
                            },
                            batch.Metadata)),
                    cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var stagedMessages = new List<OutboxMessage>(batch.Changes.Count);
        MySqlBinlogCapturedChange? lastStagedChange = null;
        try
        {
            foreach (var change in batch.Changes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await outbox.EnqueueAsync(change.Message, cancellationToken).ConfigureAwait(false);
                stagedMessages.Add(change.Message);
                lastStagedChange = change;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(
                    reporter,
                    descriptor,
                    captureOptions,
                    outbox,
                    exception.Message,
                    "outbox-stage",
                    lastStagedChange?.ChangeId,
                    lastStagedChange?.CheckpointToken.Serialize(),
                    stagedMessages.Count,
                    cancellationToken,
                    batch.Metadata)
                .ConfigureAwait(false);
            throw;
        }

        var committedCheckpoint = lastStagedChange?.CheckpointToken;
        try
        {
            if (committedCheckpoint is not null)
            {
                await transport.CommitCheckpointAsync(
                        captureOptions,
                        descriptor,
                        committedCheckpoint.Value,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(
                    reporter,
                    descriptor,
                    captureOptions,
                    outbox,
                    exception.Message,
                    "checkpoint",
                    lastStagedChange?.ChangeId,
                    lastStagedChange?.CheckpointToken.Serialize(),
                    stagedMessages.Count,
                    cancellationToken,
                    batch.Metadata)
                .ConfigureAwait(false);
            throw;
        }

        await reporter.ReportAsync(
                new CdcCaptureExecutionReport(
                    cdcCaptureId: descriptor.Id,
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    capturedChangeCount: batch.Changes.Count,
                    producedMessageCount: stagedMessages.Count,
                    changeId: lastStagedChange?.ChangeId,
                    checkpoint: lastStagedChange?.CheckpointToken.Serialize(),
                    freshness: CreateFreshness(captureOptions),
                    lag: CreateLag(batch.HasMoreChanges),
                    publication: CreatePendingPublication(stagedMessages.Count),
                    metadata: CreateExecutionMetadata(
                        descriptor,
                        captureOptions,
                        outbox,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["acknowledgement"] = "provider-native",
                            ["lastOperationType"] = lastStagedChange?.OperationName ?? "unknown"
                        },
                        batch.Metadata)),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task ReportFailureAsync(
        ICdcCaptureRuntimeReporter reporter,
        CdcCaptureDescriptor descriptor,
        MySqlBinlogCaptureOptions captureOptions,
        IOutbox? outbox,
        string error,
        string failureKind,
        string? changeId,
        string? checkpoint,
        int stagedMessageCount,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? additionalMetadata = null)
    {
        var failureMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["failureKind"] = failureKind,
            ["acknowledgement"] = "provider-native"
        };

        if (!string.IsNullOrWhiteSpace(changeId))
        {
            failureMetadata["pendingChangeId"] = changeId;
        }

        if (!string.IsNullOrWhiteSpace(checkpoint))
        {
            failureMetadata["pendingCheckpoint"] = checkpoint;
        }

        await reporter.ReportAsync(
                new CdcCaptureExecutionReport(
                    cdcCaptureId: descriptor.Id,
                    outcome: CdcCaptureRuntimeOutcomes.Failed,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    producedMessageCount: stagedMessageCount,
                    error: error,
                    publication: CreatePendingPublication(stagedMessageCount),
                    metadata: CreateExecutionMetadata(
                        descriptor,
                        captureOptions,
                        outbox,
                        failureMetadata,
                        additionalMetadata)),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private Dictionary<string, string> CreateExecutionMetadata(
        CdcCaptureDescriptor descriptor,
        MySqlBinlogCaptureOptions captureOptions,
        IOutbox? outbox,
        IReadOnlyDictionary<string, string>? metadata,
        IReadOnlyDictionary<string, string>? transportMetadata)
    {
        var normalizedSchema = descriptor.Metadata.TryGetValue("tableSchema", out var tableSchema) && !string.IsNullOrWhiteSpace(tableSchema)
            ? tableSchema.Trim()
            : options.DatabaseName.Trim();

        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["captureExecution"] = "mysql-provider-native-runtime",
            ["cdcCaptureExecutionRuntimeId"] = MySqlDataRuntimeIds.CdcExecutionRuntimeId,
            ["executionOwnership"] = descriptor.ExecutionBinding.ExecutionOwnership,
            ["executionResolutionMode"] = descriptor.ExecutionBinding.ResolutionMode,
            ["provider"] = descriptor.Provider,
            ["outboxId"] = descriptor.OutboxId,
            ["hostedExecutionId"] = MySqlDataRuntimeIds.CdcHostedExecutionId,
            ["executionGraphId"] = MySqlDataRuntimeIds.CdcExecutionGraphId,
            ["databaseName"] = options.DatabaseName.Trim(),
            ["tableSchema"] = normalizedSchema,
            ["tableName"] = captureOptions.TableName.Trim(),
            ["serverId"] = captureOptions.ServerId.ToString(CultureInfo.InvariantCulture),
            ["channelId"] = captureOptions.ChannelId.Trim(),
            ["messageType"] = captureOptions.MessageType.Trim(),
            ["initialPosition"] = captureOptions.InitialPosition.Trim(),
            ["maxChangesPerRead"] = captureOptions.MaxChangesPerRead.ToString(CultureInfo.InvariantCulture),
            ["maxAwaitTimeSeconds"] = captureOptions.MaxAwaitTimeSeconds.ToString(CultureInfo.InvariantCulture),
            ["pollingIntervalSeconds"] = captureOptions.PollingIntervalSeconds.ToString(CultureInfo.InvariantCulture),
            ["checkpointStore"] = $"{options.DatabaseName.Trim()}.{options.CheckpointTableName.Trim()}",
            ["binlogCheckpointSource"] = "cephalon-checkpoint-table"
        };

        if (outbox is not null)
        {
            merged["outboxServiceType"] = outbox.GetType().FullName ?? outbox.GetType().Name;
        }

        if (!string.IsNullOrWhiteSpace(descriptor.ExecutionBinding.RequestedExecutionRuntimeId))
        {
            merged["requestedExecutionRuntimeId"] = descriptor.ExecutionBinding.RequestedExecutionRuntimeId;
        }

        if (transportMetadata is not null)
        {
            foreach (var pair in transportMetadata)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        if (metadata is not null)
        {
            foreach (var pair in metadata)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        return merged;
    }

    private static CdcCaptureFreshnessStatus CreateFreshness(MySqlBinlogCaptureOptions captureOptions)
    {
        return new CdcCaptureFreshnessStatus(
            CdcCaptureFreshnessStates.Fresh,
            DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, captureOptions.PollingIntervalSeconds + captureOptions.MaxAwaitTimeSeconds)),
            "The MySQL provider-native binlog runner is still within the configured hosted-service polling window.");
    }

    private static CdcCaptureLagStatus CreateLag(bool hasMoreChanges)
    {
        return hasMoreChanges
            ? new CdcCaptureLagStatus(
                CdcCaptureLagStates.Lagging,
                pendingChangeCount: 1,
                description: "The MySQL provider-native binlog runner stopped with additional buffered row events still pending.")
            : new CdcCaptureLagStatus(
                CdcCaptureLagStates.Current,
                pendingChangeCount: 0,
                description: "The MySQL provider-native binlog runner drained the current row-event batch.");
    }

    private static CdcCapturePublicationStatus? CreatePendingPublication(int stagedMessageCount)
    {
        return stagedMessageCount <= 0
            ? null
            : new CdcCapturePublicationStatus(
                CdcCapturePublicationStates.PendingPublication,
                pendingPublicationCount: stagedMessageCount,
                description: "The provider-native MySQL binlog runner staged publications through the linked outbox.");
    }

    private static void LogMissingDataRuntimeServices(ILogger logger, string cdcCaptureId) =>
        LogMissingDataRuntimeServicesMessage(logger, cdcCaptureId, null);

    private static void LogMissingDescriptor(ILogger logger, string cdcCaptureId) =>
        LogMissingDescriptorMessage(logger, cdcCaptureId, null);

    private static void LogCaptureLoopFailure(ILogger logger, string cdcCaptureId, Exception exception) =>
        LogCaptureLoopFailureMessage(logger, cdcCaptureId, exception);

    private sealed record MySqlBinlogCaptureFailure(
        string FailureKind,
        IReadOnlyDictionary<string, string>? Metadata);
}
