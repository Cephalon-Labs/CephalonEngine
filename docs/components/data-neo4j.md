# Cephalon.Data.Neo4j

> **Maturity:** `M1` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Data.Neo4j` is the Neo4j graph-store companion pack for Cephalon, proving that the companion-pack pattern established by `Cephalon.Data.MongoDB` and `Cephalon.Data.Redis` extends cleanly to graph-oriented databases without any changes to `Cephalon.Engine` or `Cephalon.Abstractions`.

## What it owns

- registers a singleton `IDriver` from a Bolt URI and credentials using `TryAdd` semantics so a host-owned driver is never displaced
- registers a scoped `IOutbox` backed by Neo4j `:OutboxMessage` graph nodes when `RegisterOutbox` is enabled; node labels honour the optional `LabelPrefix`
- registers a scoped `IEventDispatchStore` against the same Neo4j outbox nodes when `RegisterOutbox` is enabled so consumer-managed or adapter-managed dispatch can read pending staged messages and persist durable dispatch outcomes without a relational bridge
- registers a scoped `IInbox` backed by Neo4j `:InboxReceipt` graph nodes when `RegisterInbox` is enabled; node labels honour the optional `LabelPrefix`
- ensures that outbox staging is idempotent via Cypher `MERGE` on `messageId` — the `ON CREATE SET` clause fires only for new nodes, making repeated `EnqueueAsync` calls with the same message id safe no-ops
- ensures that inbox tracking is idempotent via the same `MERGE` pattern on `messageId`
- bootstraps a uniqueness constraint on each managed label on first use (via `CREATE CONSTRAINT ... IF NOT EXISTS`)
- exposes operator-facing outbox and inbox descriptors through the engine runtime surfaces
- projects the outbox descriptor through the `event-driven-integration` technology surface as outbox producers when that technology is active
- projects the inbox descriptor through the same technology surface as inbox stores when the technology is active
- publishes capability metadata `data.neo4j`, `data.graph-store`, and optionally `data.outbox.neo4j` and `data.inbox.neo4j` introspectable at runtime through the manifest
- redacts inline URI user-info and query secrets in capability metadata; `uri` contains only scheme, host, port, and path, while `uriCredentialsConfigured` and `secretProjection = redacted` preserve operator posture without leaking credentials

## Main surfaces

- `Configuration/Neo4jDataOptions.cs`
- `Modules/Neo4jDataModule.cs`
- `Registration/Neo4jDataEngineBuilderExtensions.cs`
- `Services/Neo4jOutbox.cs`
- `Services/Neo4jEventDispatchStore.cs`
- `Services/Neo4jOutboxRuntimeSurfaceContributor.cs`
- `Services/Neo4jInbox.cs`
- `Services/Neo4jInboxRuntimeSurfaceContributor.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral `IReadStore` / `IWriteStore` dispatching surface. `Cephalon.Data.Neo4j` adds the Neo4j-backed outbox and inbox persistence paths that let event-driven workloads stage and track messages against a graph store without switching to a relational or document-oriented provider.

The slice is intentionally narrow: it proves the companion-pack pattern works for graph-oriented stores, ships idempotent outbox and inbox implementations, adds a consumer-managed dispatch-store baseline for staged outboxes, and exposes the same runtime introspection surfaces as the MongoDB and Redis providers.

## Registration

```csharp
engine.AddNeo4jData(
    uri: "bolt://localhost:7687",
    username: "neo4j",
    password: "secret");
```

To enable the outbox and inbox paths:

```csharp
engine.AddNeo4jData(
    uri: "bolt://localhost:7687",
    username: "neo4j",
    password: "secret",
    configure: options =>
    {
        options.RegisterOutbox = true;
        options.RegisterInbox = true;
        options.LabelPrefix = "MyApp";  // optional — prefix all Cephalon node labels
    });
```

To resolve the Bolt URI from the root `Uris` section:

```csharp
engine.AddNeo4jData(options =>
{
    options.UriName = "Graph";
    options.Username = "neo4j";
    options.Password = "secret";
    options.RegisterOutbox = true;
});
```

```json
{
  "Uris": {
    "Graph": "bolt://localhost:7687"
  },
  "Engine": {
    "Data": {
      "Neo4j": {
        "UriName": "Graph",
        "Username": "neo4j",
        "Password": "secret"
      }
    }
  }
}
```

`UriName` and `Uri` are mutually exclusive. If both are set, the pack throws during service
registration. Leaving both unset falls back to `bolt://localhost:7687`.

## Configuration options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `UriName` | `string?` | `null` | Root `Uris` key to resolve for Neo4j |
| `Uri` | `string?` | `null` | Inline Neo4j Bolt URI |
| `Username` | `string` | `"neo4j"` | Neo4j username |
| `Password` | `string` | `""` | Neo4j password |
| `LabelPrefix` | `string` | `"Cephalon"` | Prefix applied to all managed node labels |
| `RegisterOutbox` | `bool` | `false` | Register `IOutbox` backed by `:{LabelPrefix}OutboxMessage` nodes |
| `RegisterInbox` | `bool` | `false` | Register `IInbox` backed by `:{LabelPrefix}InboxReceipt` nodes |

## OutboxMessage node schema (`:{LabelPrefix}OutboxMessage`)

The node label is `{LabelPrefix}OutboxMessage` (default: `:CephalonOutboxMessage`).

| Property | Type | Notes |
|----------|------|-------|
| `messageId` | string | Unique idempotency key; uniqueness constraint prevents duplicate staging |
| `channelId` | string | Target channel identifier |
| `messageType` | string | Fully qualified CLR message type name |
| `payload` | string | `System.Text.Json`-serialized message body |
| `contentType` | string | MIME content type (empty string when not provided) |
| `correlationId` | string | Optional causality tracking identifier |
| `tenantId` | string | Optional multi-tenancy discriminator |
| `occurredAtUtc` | string | ISO 8601 UTC timestamp when the domain event occurred |
| `createdAtUtc` | string | ISO 8601 UTC timestamp when the message was staged |
| `dispatchAttemptCount` | int | Initialized to `0`; incremented by dispatch adapters |
| `dispatchedAtUtc` | string | Optional ISO 8601 UTC timestamp written when dispatch succeeds or is skipped |
| `nextAttemptAtUtc` | string | Optional ISO 8601 UTC timestamp written when dispatch is delayed for retry |
| `headersJson` | string | `System.Text.Json`-serialized headers dictionary |
| `metadataJson` | string | `System.Text.Json`-serialized metadata dictionary |

## InboxReceipt node schema (`:{LabelPrefix}InboxReceipt`)

The node label is `{LabelPrefix}InboxReceipt` (default: `:CephalonInboxReceipt`).

| Property | Type | Notes |
|----------|------|-------|
| `messageId` | string | Processed message id; uniqueness constraint enforces exactly-once semantics |
| `channelId` | string | Channel the message arrived on |
| `messageType` | string | Fully qualified CLR message type name |
| `correlationId` | string | Optional causality tracking identifier |
| `tenantId` | string | Optional multi-tenancy discriminator |
| `receivedAtUtc` | string | ISO 8601 UTC timestamp when the message was received |
| `processedAtUtc` | string | ISO 8601 UTC timestamp when the message was first processed |

## Idempotency semantics

Both `IOutbox.EnqueueAsync` and `IInbox.MarkProcessedAsync` use Cypher `MERGE` on `messageId`:

```cypher
MERGE (m:CephalonOutboxMessage {messageId: $messageId})
ON CREATE SET m.channelId = $channelId, ...
```

`ON CREATE SET` fires only when the `MERGE` creates a new node. When the node already exists (i.e., the message was already staged or processed), `MERGE` matches it and the `ON CREATE SET` block is skipped — making repeated calls with the same id safe no-ops. No exception is thrown and no special error code handling is required.

A uniqueness constraint is created on first use via `CREATE CONSTRAINT ... IF NOT EXISTS`, ensuring the `MERGE` key is indexed and enforced at the database level.

## Runtime capabilities

When `Neo4jDataModule` is active, the following capability keys appear in the runtime manifest:

| Capability key | When registered |
|----------------|-----------------|
| `data.neo4j` | Always |
| `data.graph-store` | Always |
| `data.outbox.neo4j` | `RegisterOutbox = true` |
| `data.inbox.neo4j` | `RegisterInbox = true` |

The provider capability sanitizes inline URI metadata. A URI such as
`bolt://user:password@localhost:7687/db?token=secret` is reported as
`bolt://localhost:7687/db` with `uriCredentialsConfigured = true` and
`secretProjection = redacted`.

## Runtime surface entries

When the `event-driven-integration` technology is active, the following entries appear under `/engine/snapshot`:

| Surface | Entry id | `provider` metadata |
|---------|----------|---------------------|
| `outbox-producers` | `neo4j-outbox` | `neo4j` |
| `inbox-stores` | `neo4j-inbox` | `neo4j` |

## Not shipped in this slice

This pack intentionally does not claim:

- graph traversal queries or relationship modeling between domain entities
- full-text search or spatial index queries
- APOC procedure integration
- `IReadStore` / `IWriteStore` dispatch backed by Neo4j — query and command handlers should use `IDriver` directly
- transaction-scoped outbox staging spanning multiple graph operations
- adapter-owned dispatch loops or broker retry scheduling beyond the shipped consumer-managed dispatch-store baseline
- change-data-capture or Neo4j streams integration

These remain explicit later slices to keep the initial provider claim honest.

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.EventSourcing.Neo4j](event-sourcing-neo4j.md)
- [Cephalon.Data.MongoDB](data-mongodb.md)
- [Cephalon.Data.Redis](data-redis.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
