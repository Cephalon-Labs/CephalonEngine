using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Messaging.Abstractions;
using Cephalon.Behaviors.Messaging.Options;
using Cephalon.Behaviors.Services;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Cephalon.Behaviors.Messaging.Bindings;

/// <summary>
/// Kafka messaging transport binding (transport ID: <c>"kafka"</c>).
/// Consumes messages from a Kafka topic and dispatches them through the ABT behavior dispatcher.
/// Manually commits offsets after successful dispatch. Supports partition assignment/revocation logging.
/// </summary>
public sealed class KafkaTransportBinding : IMessagingBehaviorBinding, IAsyncDisposable
{
    private static readonly Action<ILogger, object, Exception?> LogPartitionsAssigned =
        LoggerMessage.Define<object>(LogLevel.Information, default, "Kafka partitions assigned: {Partitions}");

    private static readonly Action<ILogger, object, Exception?> LogPartitionsRevoked =
        LoggerMessage.Define<object>(LogLevel.Information, default, "Kafka partitions revoked: {Partitions}");

    private static readonly Action<ILogger, string, Exception?> LogSubscribed =
        LoggerMessage.Define<string>(LogLevel.Information, default, "Kafka consumer subscribed to topic '{Topic}'.");

    private static readonly Action<ILogger, string, Exception?> LogSubscribeFailed =
        LoggerMessage.Define<string>(LogLevel.Error, default, "Kafka subscribe failed for topic '{Topic}'.");

    private static readonly Action<ILogger, string, Exception?> LogConsumeLoopCancelled =
        LoggerMessage.Define<string>(LogLevel.Information, default, "Kafka consumer loop cancelled for behavior '{BehaviorId}'.");

    private static readonly Action<ILogger, string, string, Exception?> LogFatalConsumeError =
        LoggerMessage.Define<string, string>(LogLevel.Critical, default, "Kafka fatal consume error for behavior '{BehaviorId}', topic '{Topic}'. Stopping consumer loop.");

    private static readonly Action<ILogger, string, string, Exception?> LogNonFatalConsumeError =
        LoggerMessage.Define<string, string>(LogLevel.Warning, default, "Kafka non-fatal consume error for behavior '{BehaviorId}', topic '{Topic}'. Continuing.");

    private static readonly Action<ILogger, string, int, long, Exception?> LogMessageReceived =
        LoggerMessage.Define<string, int, long>(LogLevel.Information, default, "Kafka message received from topic '{Topic}' partition {Partition} offset {Offset}.");

    private static readonly Action<ILogger, string, long, Exception?> LogDeserializeFailed =
        LoggerMessage.Define<string, long>(LogLevel.Error, default, "Kafka message deserialization failed at topic '{Topic}' offset {Offset}. Skipping.");

    private static readonly Action<ILogger, string, long, Exception?> LogNullInput =
        LoggerMessage.Define<string, long>(LogLevel.Error, default, "Kafka message deserialized to null at topic '{Topic}' offset {Offset}. Skipping.");

    private static readonly Action<ILogger, string, int, long, Exception?> LogOffsetCommitted =
        LoggerMessage.Define<string, int, long>(LogLevel.Information, default, "Kafka offset committed: topic '{Topic}' partition {Partition} offset {Offset}.");

    private static readonly Action<ILogger, string, Exception?> LogDispatchCancelled =
        LoggerMessage.Define<string>(LogLevel.Information, default, "Kafka dispatch cancelled for behavior '{BehaviorId}'.");

    private static readonly Action<ILogger, string, string, long, Exception?> LogDispatchFailed =
        LoggerMessage.Define<string, string, long>(LogLevel.Error, default, "Kafka dispatch failed for behavior '{BehaviorId}' at topic '{Topic}' offset {Offset}.");

    private static readonly Action<ILogger, string, Exception?> LogConsumeLoopExited =
        LoggerMessage.Define<string>(LogLevel.Information, default, "Kafka consumer loop exited for behavior '{BehaviorId}'.");

    private static readonly Action<ILogger, Exception?> LogStopTimeout =
        LoggerMessage.Define(LogLevel.Warning, default, "Kafka consumer task did not complete within 5 s timeout during StopAsync.");

    private static readonly Action<ILogger, Exception?> LogStopped =
        LoggerMessage.Define(LogLevel.Information, default, "Kafka transport binding stopped.");

    private readonly KafkaTransportOptions _options;
    private readonly ILogger<KafkaTransportBinding> _logger;
    private IConsumer<Ignore, string>? _consumer;
    private Task? _consumeTask;
    private CancellationTokenSource? _cts;
    private int _stopped;

    /// <summary>
    /// Initializes a new instance of <see cref="KafkaTransportBinding" /> with the supplied options and logger.
    /// </summary>
    /// <param name="options">The Kafka transport options.</param>
    /// <param name="logger">The logger for this binding.</param>
    public KafkaTransportBinding(KafkaTransportOptions options, ILogger<KafkaTransportBinding> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public string TransportId => "kafka";

    /// <inheritdoc />
    public Task StartAsync(BehaviorTopologyDescriptor descriptor, BehaviorDispatcher dispatcher, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(dispatcher);

        var topic = _options.Topic ?? descriptor.Id;

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = _options.AutoOffsetReset,
        };

        var consumerBuilder = new ConsumerBuilder<Ignore, string>(config);

        consumerBuilder.SetPartitionsAssignedHandler((_, p) =>
            LogPartitionsAssigned(_logger, p, null));
        consumerBuilder.SetPartitionsRevokedHandler((_, p) =>
            LogPartitionsRevoked(_logger, p, null));

        _consumer = consumerBuilder.Build();

        try
        {
            _consumer.Subscribe(topic);
            LogSubscribed(_logger, topic, null);
        }
        catch (KafkaException ex)
        {
            LogSubscribeFailed(_logger, topic, ex);
            throw;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _consumeTask = Task.Factory.StartNew(
            () => ConsumeLoopAsync(descriptor.Id, topic, dispatcher, _cts.Token),
            _cts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).Unwrap();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken ct)
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return;
        }

        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
        }

        if (_consumeTask is not null)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _consumeTask.WaitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                LogStopTimeout(_logger, null);
            }
        }

        _consumer?.Dispose();
        _consumer = null;

        LogStopped(_logger, null);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            _cts.Dispose();
        }

        if (_consumeTask is not null)
        {
            try
            {
                await _consumeTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on disposal.
            }
        }

        _consumer?.Dispose();
        _consumer = null;
    }

    private async Task ConsumeLoopAsync(
        string behaviorId,
        string topic,
        BehaviorDispatcher dispatcher,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            ConsumeResult<Ignore, string> result;
            try
            {
                result = _consumer!.Consume(ct);
            }
            catch (OperationCanceledException)
            {
                LogConsumeLoopCancelled(_logger, behaviorId, null);
                break;
            }
            catch (ConsumeException ex)
            {
                if (ex.Error.IsFatal)
                {
                    LogFatalConsumeError(_logger, behaviorId, topic, ex);
                    break;
                }
                else
                {
                    LogNonFatalConsumeError(_logger, behaviorId, topic, ex);
                    continue;
                }
            }

            if (result.IsPartitionEOF)
            {
                continue;
            }

            LogMessageReceived(_logger, result.Topic, result.Partition.Value, result.Offset.Value, null);

            object? input;
            try
            {
                input = JsonSerializer.Deserialize<object>(result.Message.Value);
            }
            catch (JsonException ex)
            {
                LogDeserializeFailed(_logger, result.Topic, result.Offset.Value, ex);
                _consumer!.Commit(result);
                continue;
            }

            if (input is null)
            {
                LogNullInput(_logger, result.Topic, result.Offset.Value, null);
                _consumer!.Commit(result);
                continue;
            }

            var context = new KafkaBehaviorContext(behaviorId, result);

            try
            {
                await dispatcher.DispatchAsync(behaviorId, input, context, ct).ConfigureAwait(false);

                _consumer!.Commit(result);
                LogOffsetCommitted(_logger, result.Topic, result.Partition.Value, result.Offset.Value, null);
            }
            catch (OperationCanceledException)
            {
                LogDispatchCancelled(_logger, behaviorId, null);
                break;
            }
            catch (Exception ex)
            {
                LogDispatchFailed(_logger, behaviorId, result.Topic, result.Offset.Value, ex);
            }
        }

        LogConsumeLoopExited(_logger, behaviorId, null);
    }

    /// <summary>
    /// Minimal <see cref="IBehaviorContext" /> implementation for Kafka messages.
    /// </summary>
    private sealed class KafkaBehaviorContext : IBehaviorContext
    {
        internal KafkaBehaviorContext(string behaviorId, ConsumeResult<Ignore, string> result)
        {
            BehaviorId = behaviorId;

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Topic"] = result.Topic,
                ["Partition"] = result.Partition.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Offset"] = result.Offset.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };

            if (result.Message.Headers is not null)
            {
                foreach (var h in result.Message.Headers)
                {
                    metadata[h.Key] = System.Text.Encoding.UTF8.GetString(h.GetValueBytes());
                }
            }

            Metadata = metadata;
        }

        /// <inheritdoc />
        public string BehaviorId { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Metadata { get; }

        /// <inheritdoc />
        public Task ReplyAsync(object reply, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("ReplyAsync is not supported for Kafka messaging bindings.");
    }
}
