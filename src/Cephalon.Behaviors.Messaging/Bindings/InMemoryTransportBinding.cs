using System.Threading.Channels;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Messaging.Abstractions;
using Cephalon.Behaviors.Messaging.Options;
using Cephalon.Behaviors.Services;
using Microsoft.Extensions.Logging;

namespace Cephalon.Behaviors.Messaging.Bindings;

/// <summary>
/// In-memory messaging transport binding (transport ID: <c>"in-memory"</c>).
/// Uses a bounded <see cref="System.Threading.Channels.Channel{T}" /> with
/// <see cref="BoundedChannelFullMode.Wait" /> backpressure internally.
/// No external dependencies — suitable for testing and local development.
/// </summary>
public sealed class InMemoryTransportBinding : IMessagingBehaviorBinding, IAsyncDisposable
{
    private static readonly Action<ILogger, Exception?> LogConsumerTimeout =
        LoggerMessage.Define(LogLevel.Warning, default,
            "InMemory consumer task did not complete within 5 s timeout during StopAsync.");

    private static readonly Action<ILogger, Exception?> LogConsumerFaultOnStop =
        LoggerMessage.Define(LogLevel.Error, default,
            "InMemory consumer task faulted during StopAsync.");

    private static readonly Action<ILogger, Exception?> LogConsumerFaultOnDispose =
        LoggerMessage.Define(LogLevel.Error, default,
            "InMemory consumer task faulted during DisposeAsync.");

    private static readonly Action<ILogger, string, Exception?> LogConsumerFault =
        LoggerMessage.Define<string>(LogLevel.Error, default,
            "InMemory consumer loop for behavior '{BehaviorId}' encountered an unrecoverable error.");

    private readonly InMemoryTransportOptions _options;
    private readonly ILogger<InMemoryTransportBinding> _logger;
    private Channel<(object Input, IBehaviorContext Context)>? _channel;
    private CancellationTokenSource? _cts;
    private Task? _consumerTask;
    private Exception? _consumerFault;
    private int _stopped;

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryTransportBinding" /> with the supplied options.
    /// </summary>
    /// <param name="options">The in-memory transport options.</param>
    /// <param name="logger">The logger for this binding.</param>
    public InMemoryTransportBinding(InMemoryTransportOptions options, ILogger<InMemoryTransportBinding> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public string TransportId => "in-memory";

    /// <summary>
    /// Enqueues a message for dispatch. Safe to call from multiple threads.
    /// Will apply backpressure (async wait) when the channel reaches its configured capacity.
    /// </summary>
    /// <param name="input">The input message object.</param>
    /// <param name="context">The behavior execution context.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when the message has been written to the channel.</returns>
    public async Task SendAsync(object input, IBehaviorContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (_channel is null)
        {
            throw new InvalidOperationException("StartAsync must be called before SendAsync.");
        }

        await _channel.Writer.WriteAsync((input, context), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StartAsync(BehaviorTopologyDescriptor descriptor, BehaviorDispatcher dispatcher, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(dispatcher);

        _channel = Channel.CreateBounded<(object Input, IBehaviorContext Context)>(
            new BoundedChannelOptions(_options.Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            });

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _consumerTask = Task.Factory.StartNew(
            () => ConsumeLoopAsync(descriptor.Id, dispatcher, _cts.Token),
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

        _channel?.Writer.TryComplete();

        if (_consumerTask is not null)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await _consumerTask.WaitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                LogConsumerTimeout(_logger, null);
            }
            catch (Exception ex)
            {
                LogConsumerFaultOnStop(_logger, ex);
                throw;
            }
        }

        if (_consumerFault is not null)
        {
            throw new InvalidOperationException(
                "InMemory consumer loop faulted — see inner exception.", _consumerFault);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            _cts.Dispose();
        }

        _channel?.Writer.TryComplete();

        if (_consumerTask is not null)
        {
            try
            {
                await _consumerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on disposal.
            }
            catch (Exception ex)
            {
                LogConsumerFaultOnDispose(_logger, ex);
            }
        }
    }

    private async Task ConsumeLoopAsync(string behaviorId, BehaviorDispatcher dispatcher, CancellationToken ct)
    {
        var reader = _channel!.Reader;

        try
        {
            await foreach (var (input, context) in reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                await dispatcher.DispatchAsync(behaviorId, input, context, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested — exit cleanly.
        }
        catch (Exception ex)
        {
            LogConsumerFault(_logger, behaviorId, ex);
            _consumerFault = ex;
        }
    }
}
