using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cephalon.Abstractions.Data;
using Cephalon.Data.MongoDB.Configuration;
using Cephalon.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace Cephalon.Data.MongoDB.Services;

internal sealed class MongoDbChangeStreamCaptureHostedService(
    IServiceScopeFactory scopeFactory,
    MongoDbDataOptions options,
    IMongoClient mongoClient,
    ILogger<MongoDbChangeStreamCaptureHostedService> logger) : BackgroundService
{
    private const string ContentType = "application/vnd.cephalon.mongodb.change-stream+json";
    private static readonly Action<ILogger, string, Exception?> LogMissingDataRuntimeServicesMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(6920, nameof(LogMissingDataRuntimeServices)),
            "MongoDB change-stream capture '{CdcCaptureId}' requires Cephalon.Data runtime services. Pair AddMongoDbData(...) with AddData(...) when provider-native CDC is enabled.");
    private static readonly Action<ILogger, string, Exception?> LogMissingDescriptorMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(6921, nameof(LogMissingDescriptor)),
            "MongoDB change-stream capture '{CdcCaptureId}' is configured, but no active CDC descriptor was contributed.");
    private static readonly Action<ILogger, string, Exception?> LogCaptureLoopFailureMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(6922, nameof(LogCaptureLoopFailure)),
            "MongoDB provider-native change-stream capture '{CdcCaptureId}' failed and will retry.");
    private readonly JsonWriterSettings jsonWriterSettings = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    public static string GetCheckpointCollectionName(string collectionPrefix)
    {
        return $"{collectionPrefix ?? string.Empty}cdc_change_stream_checkpoints";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.ChangeStreamCaptures.Count == 0)
        {
            return;
        }

        var tasks = options.ChangeStreamCaptures
            .Select(capture => RunCaptureLoopAsync(capture, stoppingToken))
            .ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task RunCaptureLoopAsync(
        MongoDbChangeStreamCaptureOptions captureOptions,
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
                        MongoDbDataRuntimeIds.ChangeStreamExecutionRuntimeId,
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
                            "The MongoDB provider-native change-stream runner could not resolve the linked outbox binding.",
                            "missing-outbox",
                            null,
                            null,
                            0,
                            cancellationToken)
                        .ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, captureOptions.MaxAwaitTimeSeconds)), cancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                await RunCursorLoopAsync(
                        descriptor,
                        captureOptions,
                        outbox,
                        reporter,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                if (reporter is not null && descriptor is not null)
                {
                    await ReportFailureAsync(
                            reporter,
                            descriptor,
                            captureOptions,
                            outbox,
                            exception.Message,
                            "change-stream",
                            null,
                            null,
                            0,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                LogCaptureLoopFailure(logger, captureOptions.Id, exception);
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, captureOptions.MaxAwaitTimeSeconds)), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task RunCursorLoopAsync(
        CdcCaptureDescriptor descriptor,
        MongoDbChangeStreamCaptureOptions captureOptions,
        IOutbox outbox,
        ICdcCaptureRuntimeReporter reporter,
        CancellationToken cancellationToken)
    {
        var database = mongoClient.GetDatabase(ResolveDatabaseName(captureOptions));
        var collection = database.GetCollection<BsonDocument>(captureOptions.CollectionName.Trim());
        var checkpoints = database.GetCollection<MongoDbChangeStreamCheckpointEntry>(GetCheckpointCollectionName(options.CollectionPrefix));
        var checkpointEntry = await checkpoints.Find(entry => entry.CdcCaptureId == descriptor.Id)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var changeStreamOptions = CreateChangeStreamOptions(captureOptions, checkpointEntry);

        await reporter.ReportAsync(
                new CdcCaptureExecutionReport(
                    cdcCaptureId: descriptor.Id,
                    outcome: CdcCaptureRuntimeOutcomes.Started,
                    observedAtUtc: DateTimeOffset.UtcNow,
                    metadata: CreateExecutionMetadata(descriptor, captureOptions, outbox, null, null)),
                cancellationToken)
            .ConfigureAwait(false);

        using var cursor = await collection.WatchAsync(changeStreamOptions, cancellationToken).ConfigureAwait(false);
        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            var batch = cursor.Current.ToArray();
            if (batch.Length == 0)
            {
                await reporter.ReportAsync(
                        new CdcCaptureExecutionReport(
                            cdcCaptureId: descriptor.Id,
                            outcome: CdcCaptureRuntimeOutcomes.Idle,
                            observedAtUtc: DateTimeOffset.UtcNow,
                            freshness: CreateFreshness(captureOptions),
                            lag: CreateCurrentLag(),
                            metadata: CreateExecutionMetadata(
                                descriptor,
                                captureOptions,
                                outbox,
                                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["acknowledgement"] = "provider-native"
                                },
                                null)),
                        cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            var stagedMessages = new List<OutboxMessage>(batch.Length);
            string? changeId = null;
            string? checkpoint = null;
            try
            {
                foreach (var change in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var message = CreateOutboxMessage(descriptor, captureOptions, change, cursor.GetResumeToken());
                    await outbox.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
                    stagedMessages.Add(message);
                    var resumeTokenJson = SerializeResumeToken(change.ResumeToken ?? cursor.GetResumeToken());
                    if (!string.IsNullOrWhiteSpace(resumeTokenJson))
                    {
                        checkpoint = resumeTokenJson;
                        changeId = CreateResumeTokenHash(resumeTokenJson);
                    }
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
                        changeId,
                        checkpoint,
                        stagedMessages.Count,
                        cancellationToken)
                    .ConfigureAwait(false);
                throw;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(checkpoint))
                {
                    var persistedCheckpoint = new MongoDbChangeStreamCheckpointEntry
                    {
                        CdcCaptureId = descriptor.Id,
                        ResumeTokenJson = checkpoint,
                        ChangeId = changeId ?? string.Empty,
                        UpdatedAtUtc = DateTime.UtcNow
                    };

                    _ = await checkpoints.ReplaceOneAsync(
                            candidate => candidate.CdcCaptureId == descriptor.Id,
                            persistedCheckpoint,
                            new ReplaceOptions { IsUpsert = true },
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
                        changeId,
                        checkpoint,
                        stagedMessages.Count,
                        cancellationToken)
                    .ConfigureAwait(false);
                throw;
            }

            await reporter.ReportAsync(
                    new CdcCaptureExecutionReport(
                        cdcCaptureId: descriptor.Id,
                        outcome: CdcCaptureRuntimeOutcomes.Captured,
                        observedAtUtc: DateTimeOffset.UtcNow,
                        capturedChangeCount: batch.Length,
                        producedMessageCount: stagedMessages.Count,
                        changeId: changeId,
                        checkpoint: checkpoint,
                        freshness: CreateFreshness(captureOptions),
                        lag: CreateCurrentLag(),
                        publication: CreatePendingPublication(stagedMessages.Count),
                        metadata: CreateExecutionMetadata(
                            descriptor,
                            captureOptions,
                            outbox,
                            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["acknowledgement"] = "provider-native",
                                ["lastOperationType"] = batch[^1].OperationType.ToString().ToLowerInvariant(),
                                ["checkpointCollection"] = checkpoints.CollectionNamespace.CollectionName
                            },
                            null)),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static ChangeStreamOptions CreateChangeStreamOptions(
        MongoDbChangeStreamCaptureOptions captureOptions,
        MongoDbChangeStreamCheckpointEntry? checkpointEntry)
    {
        var options = new ChangeStreamOptions
        {
            FullDocument = ParseFullDocumentMode(captureOptions.FullDocumentMode),
            MaxAwaitTime = TimeSpan.FromSeconds(Math.Max(1, captureOptions.MaxAwaitTimeSeconds))
        };

        if (captureOptions.BatchSize is > 0)
        {
            options.BatchSize = captureOptions.BatchSize.Value;
        }

        if (!string.IsNullOrWhiteSpace(checkpointEntry?.ResumeTokenJson))
        {
            options.ResumeAfter = BsonDocument.Parse(checkpointEntry.ResumeTokenJson);
        }

        return options;
    }

    private static ChangeStreamFullDocumentOption ParseFullDocumentMode(string? fullDocumentMode)
    {
        var normalized = string.IsNullOrWhiteSpace(fullDocumentMode)
            ? "update-lookup"
            : fullDocumentMode.Trim().ToLowerInvariant();

        return normalized switch
        {
            "default" => ChangeStreamFullDocumentOption.Default,
            "update-lookup" => ChangeStreamFullDocumentOption.UpdateLookup,
            "when-available" => ChangeStreamFullDocumentOption.WhenAvailable,
            "required" => ChangeStreamFullDocumentOption.Required,
            _ => throw new InvalidOperationException(
                $"MongoDB full-document mode '{fullDocumentMode}' is not supported. Use default, update-lookup, when-available, or required.")
        };
    }

    private string ResolveDatabaseName(MongoDbChangeStreamCaptureOptions captureOptions)
    {
        return string.IsNullOrWhiteSpace(captureOptions.DatabaseName)
            ? options.DatabaseName
            : captureOptions.DatabaseName.Trim();
    }

    private OutboxMessage CreateOutboxMessage(
        CdcCaptureDescriptor descriptor,
        MongoDbChangeStreamCaptureOptions captureOptions,
        ChangeStreamDocument<BsonDocument> change,
        BsonDocument? cursorResumeToken)
    {
        var resumeTokenJson = SerializeResumeToken(change.ResumeToken ?? cursorResumeToken);
        var changeId = CreateResumeTokenHash(resumeTokenJson);
        var payload = change.BackingDocument.ToJson(jsonWriterSettings);
        var databaseName = ResolveDatabaseName(captureOptions);

        return new OutboxMessage(
            id: $"{descriptor.Id}:{changeId}",
            channelId: captureOptions.ChannelId.Trim(),
            messageType: captureOptions.MessageType.Trim(),
            payload: payload,
            occurredAtUtc: DateTimeOffset.UtcNow,
            contentType: ContentType,
            headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["provider"] = MongoDbDataOptions.ProviderId,
                ["cdcCaptureId"] = descriptor.Id,
                ["databaseName"] = databaseName,
                ["collectionName"] = captureOptions.CollectionName.Trim(),
                ["operationType"] = change.OperationType.ToString().ToLowerInvariant()
            },
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sourceId"] = descriptor.SourceId,
                ["eventFormat"] = descriptor.EventFormat,
                ["resumeTokenHash"] = changeId
            });
    }

    private async Task ReportFailureAsync(
        ICdcCaptureRuntimeReporter reporter,
        CdcCaptureDescriptor descriptor,
        MongoDbChangeStreamCaptureOptions captureOptions,
        IOutbox? outbox,
        string error,
        string failureKind,
        string? changeId,
        string? checkpoint,
        int stagedMessageCount,
        CancellationToken cancellationToken)
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
                        null,
                        failureMetadata)),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static CdcCaptureFreshnessStatus CreateFreshness(MongoDbChangeStreamCaptureOptions captureOptions)
    {
        return new CdcCaptureFreshnessStatus(
            CdcCaptureFreshnessStates.Fresh,
            DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, captureOptions.MaxAwaitTimeSeconds)),
            "The MongoDB provider-native change stream is still within the configured await window.");
    }

    private static CdcCaptureLagStatus CreateCurrentLag()
    {
        return new CdcCaptureLagStatus(
            CdcCaptureLagStates.Current,
            pendingChangeCount: 0,
            description: "The MongoDB provider-native change stream did not report buffered lag for the latest batch.");
    }

    private static CdcCapturePublicationStatus? CreatePendingPublication(int stagedMessageCount)
    {
        return stagedMessageCount <= 0
            ? null
            : new CdcCapturePublicationStatus(
                CdcCapturePublicationStates.PendingPublication,
                pendingPublicationCount: stagedMessageCount,
                description: "The provider-native MongoDB change stream staged publications through the linked outbox.");
    }

    private Dictionary<string, string> CreateExecutionMetadata(
        CdcCaptureDescriptor descriptor,
        MongoDbChangeStreamCaptureOptions captureOptions,
        IOutbox? outbox,
        IReadOnlyDictionary<string, string>? metadata,
        IReadOnlyDictionary<string, string>? overrides)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["captureExecution"] = "mongodb-provider-native-runtime",
            ["cdcCaptureExecutionRuntimeId"] = MongoDbDataRuntimeIds.ChangeStreamExecutionRuntimeId,
            ["executionOwnership"] = descriptor.ExecutionBinding.ExecutionOwnership,
            ["executionResolutionMode"] = descriptor.ExecutionBinding.ResolutionMode,
            ["provider"] = descriptor.Provider,
            ["outboxId"] = descriptor.OutboxId,
            ["hostedExecutionId"] = MongoDbDataRuntimeIds.ChangeStreamHostedExecutionId,
            ["executionGraphId"] = MongoDbDataRuntimeIds.ChangeStreamExecutionGraphId,
            ["databaseName"] = ResolveDatabaseName(captureOptions),
            ["collectionName"] = captureOptions.CollectionName.Trim(),
            ["channelId"] = captureOptions.ChannelId.Trim(),
            ["messageType"] = captureOptions.MessageType.Trim(),
            ["fullDocumentMode"] = captureOptions.FullDocumentMode.Trim(),
            ["checkpointCollection"] = GetCheckpointCollectionName(options.CollectionPrefix)
        };

        if (outbox is not null)
        {
            merged["outboxServiceType"] = outbox.GetType().FullName ?? outbox.GetType().Name;
        }

        if (!string.IsNullOrWhiteSpace(descriptor.ExecutionBinding.RequestedExecutionRuntimeId))
        {
            merged["requestedExecutionRuntimeId"] = descriptor.ExecutionBinding.RequestedExecutionRuntimeId;
        }

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

    private static string SerializeResumeToken(BsonDocument? resumeToken)
    {
        return resumeToken is null
            ? string.Empty
            : resumeToken.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson });
    }

    private static string CreateResumeTokenHash(string? resumeTokenJson)
    {
        var normalized = string.IsNullOrWhiteSpace(resumeTokenJson)
            ? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)
            : resumeTokenJson.Trim();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void LogMissingDataRuntimeServices(ILogger logger, string cdcCaptureId) =>
        LogMissingDataRuntimeServicesMessage(logger, cdcCaptureId, null);

    private static void LogMissingDescriptor(ILogger logger, string cdcCaptureId) =>
        LogMissingDescriptorMessage(logger, cdcCaptureId, null);

    private static void LogCaptureLoopFailure(ILogger logger, string cdcCaptureId, Exception exception) =>
        LogCaptureLoopFailureMessage(logger, cdcCaptureId, exception);
}
