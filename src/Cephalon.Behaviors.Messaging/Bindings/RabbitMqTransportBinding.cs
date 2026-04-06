using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Messaging.Abstractions;
using Cephalon.Behaviors.Messaging.Options;
using Cephalon.Behaviors.Services;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Cephalon.Behaviors.Messaging.Bindings;

/// <summary>
/// RabbitMQ messaging transport binding (transport ID: <c>"rabbitmq"</c>).
/// Uses a lazy connection strategy — the broker connection is established only
/// on <see cref="StartAsync" />, not at construction time.
/// </summary>
public sealed class RabbitMqTransportBinding : IMessagingBehaviorBinding, IAsyncDisposable
{
    private static readonly Action<ILogger, string, int, string, string, Exception?> LogConnecting =
        LoggerMessage.Define<string, int, string, string>(LogLevel.Information, default,
            "RabbitMQ binding connecting to {HostName}:{Port}{VirtualHost} for behavior '{BehaviorId}'.");

    private static readonly Action<ILogger, string, int, string, Exception?> LogConnected =
        LoggerMessage.Define<string, int, string>(LogLevel.Information, default,
            "RabbitMQ connected to {Host}:{Port}/{VHost}.");

    private static readonly Action<ILogger, int, TimeSpan, Exception?> LogConnectRetry =
        LoggerMessage.Define<int, TimeSpan>(LogLevel.Warning, default,
            "RabbitMQ connect attempt {Attempt} failed, retrying in {Delay}.");

    private static readonly Action<ILogger, string?, Exception?> LogQueueDeclared =
        LoggerMessage.Define<string?>(LogLevel.Information, default,
            "RabbitMQ queue '{Queue}' declared.");

    private static readonly Action<ILogger, string?, string?, Exception?> LogConsumerStarted =
        LoggerMessage.Define<string?, string?>(LogLevel.Information, default,
            "RabbitMQ consumer started on queue '{Queue}' (consumer tag: {ConsumerTag}).");

    private static readonly Action<ILogger, string?, Exception?> LogStopping =
        LoggerMessage.Define<string?>(LogLevel.Information, default,
            "RabbitMQ binding stopping for queue '{Queue}'.");

    private static readonly Action<ILogger, string?, Exception?> LogStopped =
        LoggerMessage.Define<string?>(LogLevel.Information, default,
            "RabbitMQ binding stopped for queue '{Queue}'.");

    private static readonly Action<ILogger, string?, Exception?> LogCancelConsumerFailed =
        LoggerMessage.Define<string?>(LogLevel.Warning, default,
            "RabbitMQ BasicCancelAsync for consumer tag '{Tag}' failed during StopAsync.");

    private static readonly Action<ILogger, string?, ulong, Exception?> LogMessageReceived =
        LoggerMessage.Define<string?, ulong>(LogLevel.Information, default,
            "RabbitMQ message received from queue '{Queue}' (delivery tag {DeliveryTag}).");

    private static readonly Action<ILogger, ulong, Exception?> LogDeserializeFailed =
        LoggerMessage.Define<ulong>(LogLevel.Error, default,
            "RabbitMQ message deserialization failed (delivery tag {DeliveryTag}); nacking without requeue.");

    private static readonly Action<ILogger, ulong, Exception?> LogNullInput =
        LoggerMessage.Define<ulong>(LogLevel.Error, default,
            "RabbitMQ message body deserialized to null (delivery tag {DeliveryTag}); nacking without requeue.");

    private static readonly Action<ILogger, string, ulong, Exception?> LogDispatchSucceeded =
        LoggerMessage.Define<string, ulong>(LogLevel.Information, default,
            "RabbitMQ dispatch succeeded for behavior '{BehaviorId}' (delivery tag {DeliveryTag}).");

    private static readonly Action<ILogger, string, Exception?> LogBehaviorNotFound =
        LoggerMessage.Define<string>(LogLevel.Warning, default,
            "Behavior '{BehaviorId}' not found; nacking message without requeue.");

    private static readonly Action<ILogger, int, string, TimeSpan, Exception?> LogDispatchRetry =
        LoggerMessage.Define<int, string, TimeSpan>(LogLevel.Warning, default,
            "RabbitMQ dispatch attempt {Attempt} failed for behavior '{BehaviorId}', retrying in {Delay}.");

    private static readonly Action<ILogger, string, int, ulong, Exception?> LogDispatchFailed =
        LoggerMessage.Define<string, int, ulong>(LogLevel.Error, default,
            "RabbitMQ dispatch failed for behavior '{BehaviorId}' after {MaxAttempts} attempts (delivery tag {DeliveryTag}).");

    private static readonly Action<ILogger, string?, ulong, Exception?> LogDeadLettered =
        LoggerMessage.Define<string?, ulong>(LogLevel.Information, default,
            "RabbitMQ message dead-lettered to exchange '{DlxExchange}' (delivery tag {DeliveryTag}).");

    private static readonly Action<ILogger, Exception?> LogChannelCloseFailed =
        LoggerMessage.Define(LogLevel.Warning, default,
            "RabbitMQ channel close failed during DisposeAsync.");

    private static readonly Action<ILogger, Exception?> LogConnectionCloseFailed =
        LoggerMessage.Define(LogLevel.Warning, default,
            "RabbitMQ connection close failed during DisposeAsync.");

    private readonly RabbitMqTransportOptions _options;
    private readonly ILogger<RabbitMqTransportBinding> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private volatile bool _initialized;
    private IConnection? _connection;
    private IChannel? _channel;
    private string? _consumerTag;
    private string? _effectiveQueueName;
    private int _stopped;

    /// <summary>
    /// Initializes a new instance of <see cref="RabbitMqTransportBinding" />.
    /// No broker connection is established at this point.
    /// </summary>
    /// <param name="options">The RabbitMQ transport options.</param>
    /// <param name="logger">The logger for this binding.</param>
    public RabbitMqTransportBinding(RabbitMqTransportOptions options, ILogger<RabbitMqTransportBinding> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public string TransportId => "rabbitmq";

    /// <inheritdoc />
    public async Task StartAsync(BehaviorTopologyDescriptor descriptor, BehaviorDispatcher dispatcher, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (_initialized) return;

        await _initLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            _effectiveQueueName = _options.QueueName ?? descriptor.Id;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                VirtualHost = _options.VirtualHost,
                UserName = _options.UserName,
                Password = _options.Password,
            };

            LogConnecting(_logger, _options.HostName, _options.Port, _options.VirtualHost, descriptor.Id, null);

            for (int attempt = 0; attempt < _options.MaxRetryAttempts; attempt++)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(ct).ConfigureAwait(false);
                    _channel = await _connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);
                    break;
                }
                catch (Exception ex) when (attempt < _options.MaxRetryAttempts - 1)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    LogConnectRetry(_logger, attempt + 1, delay, ex);
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
            }

            if (_channel is null)
            {
                throw new InvalidOperationException(
                    $"Failed to connect to RabbitMQ after {_options.MaxRetryAttempts} attempts.");
            }

            LogConnected(_logger, _options.HostName, _options.Port, _options.VirtualHost, null);

            var queueArgs = new Dictionary<string, object?>();
            if (!string.IsNullOrEmpty(_options.DeadLetterExchange))
            {
                queueArgs["x-dead-letter-exchange"] = _options.DeadLetterExchange;
            }

            await _channel.QueueDeclareAsync(
                queue: _effectiveQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs.Count > 0 ? queueArgs : null,
                cancellationToken: ct).ConfigureAwait(false);

            LogQueueDeclared(_logger, _effectiveQueueName, null);

            if (!string.IsNullOrEmpty(_options.ExchangeName))
            {
                await _channel.QueueBindAsync(
                    queue: _effectiveQueueName,
                    exchange: _options.ExchangeName,
                    routingKey: _effectiveQueueName,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                await HandleDeliveryAsync(ea, descriptor.Id, dispatcher).ConfigureAwait(false);
            };

            _consumerTag = await _channel.BasicConsumeAsync(
                queue: _effectiveQueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: ct).ConfigureAwait(false);

            _initialized = true;

            LogConsumerStarted(_logger, _effectiveQueueName, _consumerTag, null);
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken ct)
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return;
        }

        LogStopping(_logger, _effectiveQueueName, null);

        if (_channel is not null && _consumerTag is not null)
        {
            try
            {
                await _channel.BasicCancelAsync(_consumerTag, cancellationToken: ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogCancelConsumerFailed(_logger, _consumerTag, ex);
            }
        }

        await DisposeResourcesAsync().ConfigureAwait(false);

        LogStopped(_logger, _effectiveQueueName, null);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeResourcesAsync().ConfigureAwait(false);
        _initLock.Dispose();
    }

    private async Task HandleDeliveryAsync(
        BasicDeliverEventArgs ea,
        string behaviorId,
        BehaviorDispatcher dispatcher)
    {
        var deliveryTag = ea.DeliveryTag;
        var body = ea.Body.ToArray();
        var bodyStr = Encoding.UTF8.GetString(body);

        LogMessageReceived(_logger, _effectiveQueueName, deliveryTag, null);

        object? input;
        try
        {
            input = JsonSerializer.Deserialize<object>(bodyStr);
        }
        catch (JsonException ex)
        {
            LogDeserializeFailed(_logger, deliveryTag, ex);
            await _channel!.BasicNackAsync(deliveryTag, multiple: false, requeue: false, CancellationToken.None).ConfigureAwait(false);
            return;
        }

        if (input is null)
        {
            LogNullInput(_logger, deliveryTag, null);
            await _channel!.BasicNackAsync(deliveryTag, multiple: false, requeue: false, CancellationToken.None).ConfigureAwait(false);
            return;
        }

        var context = new RabbitMqBehaviorContext(behaviorId, ea);

        for (int attempt = 0; attempt < _options.MaxRetryAttempts; attempt++)
        {
            try
            {
                await dispatcher.DispatchAsync(behaviorId, input, context, CancellationToken.None).ConfigureAwait(false);
                LogDispatchSucceeded(_logger, behaviorId, deliveryTag, null);
                await _channel!.BasicAckAsync(deliveryTag, multiple: false, CancellationToken.None).ConfigureAwait(false);
                return;
            }
            catch (BehaviorNotFoundException ex)
            {
                LogBehaviorNotFound(_logger, behaviorId, ex);
                await _channel!.BasicNackAsync(deliveryTag, multiple: false, requeue: false, CancellationToken.None).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (attempt < _options.MaxRetryAttempts - 1)
            {
                var delay = TimeSpan.FromMilliseconds(_options.RetryDelayMs);
                LogDispatchRetry(_logger, attempt + 1, behaviorId, delay, ex);
                await Task.Delay(delay, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogDispatchFailed(_logger, behaviorId, _options.MaxRetryAttempts, deliveryTag, ex);
                var shouldDeadLetter = !string.IsNullOrEmpty(_options.DeadLetterExchange);
                await _channel!.BasicNackAsync(deliveryTag, multiple: false, requeue: !shouldDeadLetter, CancellationToken.None).ConfigureAwait(false);

                if (shouldDeadLetter)
                {
                    LogDeadLettered(_logger, _options.DeadLetterExchange, deliveryTag, null);
                }

                return;
            }
        }
    }

    private async Task DisposeResourcesAsync()
    {
        if (_channel is not null)
        {
            try
            {
                if (_channel.IsOpen)
                {
                    await _channel.CloseAsync().ConfigureAwait(false);
                }

                await _channel.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogChannelCloseFailed(_logger, ex);
            }

            _channel = null;
        }

        if (_connection is not null)
        {
            try
            {
                if (_connection.IsOpen)
                {
                    await _connection.CloseAsync().ConfigureAwait(false);
                }

                await _connection.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogConnectionCloseFailed(_logger, ex);
            }

            _connection = null;
        }
    }

    /// <summary>
    /// Minimal <see cref="IBehaviorContext" /> implementation for RabbitMQ messages.
    /// </summary>
    private sealed class RabbitMqBehaviorContext : IBehaviorContext
    {
        internal RabbitMqBehaviorContext(string behaviorId, BasicDeliverEventArgs ea)
        {
            BehaviorId = behaviorId;

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (ea.BasicProperties?.CorrelationId is { } corrId)
            {
                metadata["CorrelationId"] = corrId;
                CorrelationId = corrId;
            }

            if (ea.BasicProperties?.Headers is { } headers)
            {
                foreach (var h in headers)
                {
                    var val = h.Value is byte[] bytes
                        ? Encoding.UTF8.GetString(bytes)
                        : h.Value?.ToString() ?? string.Empty;
                    metadata[h.Key] = val;
                }
            }

            Metadata = metadata;
        }

        /// <inheritdoc />
        public string BehaviorId { get; }

        /// <inheritdoc />
        public string? CorrelationId { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Metadata { get; }

        /// <inheritdoc />
        public Task ReplyAsync(object reply, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("ReplyAsync is not supported for RabbitMQ messaging bindings.");
    }
}
