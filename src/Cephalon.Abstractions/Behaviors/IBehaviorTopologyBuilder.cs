namespace Cephalon.Abstractions.Behaviors;

/// <summary>Fluent builder for declaring behavior topology: pattern, transports, and feature options.</summary>
public interface IBehaviorTopologyBuilder
{
    // Pattern — As* prefix declares the behavioral role (single value, replaces previous)
    /// <summary>Declares this behavior as CQRS-shaped (command/query split, 200/202 HTTP semantics).</summary>
    IBehaviorTopologyBuilder AsCqrs();
    /// <summary>Declares this behavior as event-driven (fire-and-forget, 202 HTTP, fanout).</summary>
    IBehaviorTopologyBuilder AsEventDriven();
    /// <summary>Declares this behavior as a saga step (stateful, compensation chain).</summary>
    IBehaviorTopologyBuilder AsSaga();
    /// <summary>Declares this behavior as a process manager step (long-running, durable checkpoint).</summary>
    IBehaviorTopologyBuilder AsProcessManager();
    /// <summary>Declares this behavior as direct (no architectural pattern — input → handler → output, 200/204 HTTP).</summary>
    IBehaviorTopologyBuilder AsDirect();

    // Transport — Via* prefix is additive (multiple calls union the transport set)
    /// <summary>Adds the HTTP REST transport (GET/POST routing).</summary>
    IBehaviorTopologyBuilder ViaHttpRest();
    /// <summary>Adds the JSON-RPC 2.0 over HTTP transport.</summary>
    IBehaviorTopologyBuilder ViaHttpJsonRpc();
    /// <summary>Adds the GraphQL over HTTP transport (queries and mutations).</summary>
    IBehaviorTopologyBuilder ViaHttpGraphQl();
    /// <summary>Adds the GraphQL subscriptions via Server-Sent Events transport.</summary>
    IBehaviorTopologyBuilder ViaHttpGraphQlSse();
    /// <summary>Adds the GraphQL subscriptions via WebSocket transport.</summary>
    IBehaviorTopologyBuilder ViaHttpGraphQlWs();
    /// <summary>Adds the raw Server-Sent Events push transport.</summary>
    IBehaviorTopologyBuilder ViaHttpSse();
    /// <summary>Adds the raw WebSocket bi-directional transport.</summary>
    IBehaviorTopologyBuilder ViaWebSocket();
    /// <summary>Adds the RabbitMQ (AMQP) transport.</summary>
    IBehaviorTopologyBuilder ViaRabbitMq();
    /// <summary>Adds the Kafka transport.</summary>
    IBehaviorTopologyBuilder ViaKafka();
    /// <summary>Adds the in-memory transport (zero-infra, for tests and local dev).</summary>
    IBehaviorTopologyBuilder ViaInMemory();
    /// <summary>Adds the gRPC transport.</summary>
    IBehaviorTopologyBuilder ViaGrpc();
    /// <summary>
    /// Overrides the logical API surface projected by route-shaped transport adapters.
    /// </summary>
    /// <remarks>
    /// This primarily affects the shared HTTP behavior route contract used by generic REST,
    /// JSON-RPC, Server-Sent Events, and WebSocket bindings. GraphQL remains schema-owned and
    /// therefore stays outside this route-shaped API-surface contract.
    /// </remarks>
    IBehaviorTopologyBuilder WithApiSurface(string groupPath, string operationPath);

    /// <summary>Configures optional feature flags for this behavior (outbox, inbox, event sourcing).</summary>
    IBehaviorTopologyBuilder WithOptions(Action<BehaviorTopologyOptions> configure);

    /// <summary>
    /// Adds or replaces arbitrary topology metadata for companion packs that need extra routing or runtime hints.
    /// </summary>
    /// <param name="key">The stable metadata key.</param>
    /// <param name="value">
    /// The metadata value. Pass <see langword="null" /> to remove the key from the topology descriptor.
    /// </param>
    /// <returns>The same builder for fluent chaining.</returns>
    IBehaviorTopologyBuilder WithMetadata(string key, string? value);

    /// <summary>Builds the final descriptor. Called internally by the engine — do not call directly.</summary>
    BehaviorTopologyDescriptor Build(string behaviorId);
}

/// <summary>Optional feature flags for a behavior topology entry.</summary>
public sealed class BehaviorTopologyOptions
{
    /// <summary>Initializes a new instance of <see cref="BehaviorTopologyOptions"/>.</summary>
    public BehaviorTopologyOptions() { }

    /// <summary>Gets or sets a value indicating whether outbox staging is enabled.</summary>
    public bool OutboxEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether inbox deduplication is enabled.</summary>
    public bool InboxEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether event sourcing is wired into the behavior context.</summary>
    public bool EventSourcingEnabled { get; set; }
}
