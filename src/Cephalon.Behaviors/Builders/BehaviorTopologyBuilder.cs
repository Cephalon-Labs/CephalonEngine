using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Builders;

/// <summary>
/// Fluent builder for constructing a <see cref="BehaviorTopologyDescriptor" />.
/// Call <c>Via*</c> methods to declare transport exposure, then call
/// <see cref="Build" /> to produce the immutable descriptor.
/// </summary>
public sealed class BehaviorTopologyBuilder
{
    private string _pattern = "direct";
    private readonly HashSet<string> _transportIds = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Sets the interaction pattern to <c>direct</c>.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder WithDirectPattern()
    {
        _pattern = "direct";
        return this;
    }

    /// <summary>
    /// Sets the interaction pattern to <c>cqrs</c>.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder WithCqrsPattern()
    {
        _pattern = "cqrs";
        return this;
    }

    /// <summary>
    /// Sets the interaction pattern to <c>event-driven</c>.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder WithEventDrivenPattern()
    {
        _pattern = "event-driven";
        return this;
    }

    /// <summary>
    /// Sets the interaction pattern to <c>saga-step</c>.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder WithSagaStepPattern()
    {
        _pattern = "saga-step";
        return this;
    }

    /// <summary>
    /// Sets the interaction pattern to <c>process-manager</c>.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder WithProcessManagerPattern()
    {
        _pattern = "process-manager";
        return this;
    }

    /// <summary>
    /// Declares exposure over the <c>http.rest</c> transport.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder ViaHttpRest()
    {
        _transportIds.Add("http.rest");
        return this;
    }

    /// <summary>
    /// Declares exposure over the <c>http.jsonrpc</c> transport.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder ViaHttpJsonRpc()
    {
        _transportIds.Add("http.jsonrpc");
        return this;
    }

    /// <summary>
    /// Declares exposure over the <c>http.graphql</c> transport.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder ViaHttpGraphQl()
    {
        _transportIds.Add("http.graphql");
        return this;
    }

    /// <summary>
    /// Declares exposure over the <c>http.graphql-sse</c> transport.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder ViaHttpGraphQlSse()
    {
        _transportIds.Add("http.graphql-sse");
        return this;
    }

    /// <summary>
    /// Declares exposure over the <c>http.graphql-ws</c> transport.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder ViaHttpGraphQlWs()
    {
        _transportIds.Add("http.graphql-ws");
        return this;
    }

    /// <summary>
    /// Declares exposure over the <c>http.sse</c> transport.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder ViaHttpSse()
    {
        _transportIds.Add("http.sse");
        return this;
    }

    /// <summary>
    /// Declares exposure over the <c>http.ws</c> (WebSocket) transport.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder ViaWebSocket()
    {
        _transportIds.Add("http.ws");
        return this;
    }

    /// <summary>
    /// Declares exposure over the <c>rabbitmq</c> transport.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder ViaRabbitMq()
    {
        _transportIds.Add("rabbitmq");
        return this;
    }

    /// <summary>
    /// Declares exposure over the <c>kafka</c> transport.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder ViaKafka()
    {
        _transportIds.Add("kafka");
        return this;
    }

    /// <summary>
    /// Declares exposure over the <c>in-memory</c> transport.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder ViaInMemory()
    {
        _transportIds.Add("in-memory");
        return this;
    }

    /// <summary>
    /// Declares exposure over the <c>grpc</c> transport.
    /// </summary>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorTopologyBuilder ViaGrpc()
    {
        _transportIds.Add("grpc");
        return this;
    }

    /// <summary>
    /// Builds the immutable <see cref="BehaviorTopologyDescriptor" /> for the given behavior identifier.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier.</param>
    /// <returns>The resolved topology descriptor.</returns>
    public BehaviorTopologyDescriptor Build(string behaviorId)
    {
        return new BehaviorTopologyDescriptor(
            behaviorId,
            _pattern,
            _transportIds.OrderBy(static t => t, StringComparer.OrdinalIgnoreCase).ToArray());
    }
}
