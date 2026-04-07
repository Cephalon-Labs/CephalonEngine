using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Builders;

/// <summary>
/// Fluent builder for constructing a <see cref="BehaviorTopologyDescriptor" />.
/// Implements <see cref="IBehaviorTopologyBuilder" /> so behaviors can declare
/// topology from both manual <c>Register&lt;T&gt;</c> callbacks and auto-discovered
/// static <c>ConfigureTopology</c> methods.
/// </summary>
public sealed class BehaviorTopologyBuilder : IBehaviorTopologyBuilder
{
    private string _pattern = "direct";
    private readonly HashSet<string> _transportIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly BehaviorTopologyOptions _options = new();

    // ── Interface pattern methods (As* prefix) ──────────────────────────

    /// <inheritdoc />
    public IBehaviorTopologyBuilder AsDirect()
    {
        _pattern = "direct";
        return this;
    }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder AsCqrs()
    {
        _pattern = "cqrs";
        return this;
    }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder AsEventDriven()
    {
        _pattern = "event-driven";
        return this;
    }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder AsSaga()
    {
        _pattern = "saga-step";
        return this;
    }

    /// <inheritdoc />
    public IBehaviorTopologyBuilder AsProcessManager()
    {
        _pattern = "process-manager";
        return this;
    }

    // ── Legacy pattern methods (With*Pattern — kept for backward compat) ─

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

    // ── Transport methods (shared by both APIs) ─────────────────────────

    /// <inheritdoc />
    IBehaviorTopologyBuilder IBehaviorTopologyBuilder.ViaHttpRest() => ViaHttpRest();

    /// <inheritdoc />
    IBehaviorTopologyBuilder IBehaviorTopologyBuilder.ViaHttpJsonRpc() => ViaHttpJsonRpc();

    /// <inheritdoc />
    IBehaviorTopologyBuilder IBehaviorTopologyBuilder.ViaHttpGraphQl() => ViaHttpGraphQl();

    /// <inheritdoc />
    IBehaviorTopologyBuilder IBehaviorTopologyBuilder.ViaHttpGraphQlSse() => ViaHttpGraphQlSse();

    /// <inheritdoc />
    IBehaviorTopologyBuilder IBehaviorTopologyBuilder.ViaHttpGraphQlWs() => ViaHttpGraphQlWs();

    /// <inheritdoc />
    IBehaviorTopologyBuilder IBehaviorTopologyBuilder.ViaHttpSse() => ViaHttpSse();

    /// <inheritdoc />
    IBehaviorTopologyBuilder IBehaviorTopologyBuilder.ViaWebSocket() => ViaWebSocket();

    /// <inheritdoc />
    IBehaviorTopologyBuilder IBehaviorTopologyBuilder.ViaRabbitMq() => ViaRabbitMq();

    /// <inheritdoc />
    IBehaviorTopologyBuilder IBehaviorTopologyBuilder.ViaKafka() => ViaKafka();

    /// <inheritdoc />
    IBehaviorTopologyBuilder IBehaviorTopologyBuilder.ViaInMemory() => ViaInMemory();

    /// <inheritdoc />
    IBehaviorTopologyBuilder IBehaviorTopologyBuilder.ViaGrpc() => ViaGrpc();

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

    // ── Options ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public IBehaviorTopologyBuilder WithOptions(Action<BehaviorTopologyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_options);
        return this;
    }

    // ── Build ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the immutable <see cref="BehaviorTopologyDescriptor" /> for the given behavior identifier.
    /// </summary>
    /// <param name="behaviorId">The stable behavior identifier.</param>
    /// <returns>The resolved topology descriptor.</returns>
    public BehaviorTopologyDescriptor Build(string behaviorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        return new BehaviorTopologyDescriptor(
            behaviorId,
            _pattern,
            _transportIds.OrderBy(static t => t, StringComparer.OrdinalIgnoreCase).ToArray(),
            inboxEnabled: _options.InboxEnabled,
            outboxEnabled: _options.OutboxEnabled,
            eventSourcingEnabled: _options.EventSourcingEnabled);
    }
}
