using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Services;

/// <summary>Fluent builder for declaring behavior topology: pattern, transports, and feature options.</summary>
public sealed class BehaviorTopologyBuilder : IBehaviorTopologyBuilder
{
    private string _pattern = "direct";
    private readonly List<string> _transports = [];
    private readonly BehaviorTopologyOptions _options = new();

    /// <inheritdoc />
    public IBehaviorTopologyBuilder AsCqrs() { _pattern = "cqrs"; return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder AsEventDriven() { _pattern = "event-driven"; return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder AsSaga() { _pattern = "saga-step"; return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder AsProcessManager() { _pattern = "process-manager"; return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder AsDirect() { _pattern = "direct"; return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder ViaHttpRest() { _transports.Add("http.rest"); return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder ViaHttpJsonRpc() { _transports.Add("http.json-rpc"); return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder ViaHttpGraphQl() { _transports.Add("http.graphql"); return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder ViaHttpGraphQlSse() { _transports.Add("http.graphql-sse"); return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder ViaHttpGraphQlWs() { _transports.Add("http.graphql-ws"); return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder ViaHttpSse() { _transports.Add("http.sse"); return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder ViaWebSocket() { _transports.Add("websocket"); return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder ViaRabbitMq() { _transports.Add("rabbitmq"); return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder ViaKafka() { _transports.Add("kafka"); return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder ViaInMemory() { _transports.Add("in-memory"); return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder ViaGrpc() { _transports.Add("grpc"); return this; }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder WithOptions(Action<BehaviorTopologyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_options);
        return this;
    }

    /// <inheritdoc />
    public BehaviorTopologyDescriptor Build(string behaviorId)
    {
        return new BehaviorTopologyDescriptor(
            id: behaviorId,
            pattern: _pattern,
            transportIds: _transports.ToArray(),
            inboxEnabled: _options.InboxEnabled,
            outboxEnabled: _options.OutboxEnabled,
            eventSourcingEnabled: _options.EventSourcingEnabled);
    }
}
