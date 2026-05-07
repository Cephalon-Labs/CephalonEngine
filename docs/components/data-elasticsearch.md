# Cephalon.Data.Elasticsearch

> **Maturity:** `M1` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Data.Elasticsearch` is the Elasticsearch search-store companion pack for Cephalon, proving that the companion-pack pattern established by `Cephalon.Data.MongoDB` extends cleanly to search-oriented document stores without any changes to `Cephalon.Engine` or `Cephalon.Abstractions`.

## What it owns

- registers a singleton `ElasticsearchClient` (from `Elastic.Clients.Elasticsearch` 8.17.0) using `TryAdd` semantics so a host-owned client is never displaced
- optionally configures Basic authentication via `ElasticsearchClientSettings.Authentication(new BasicAuthentication(...))` when `Username` is set
- registers a scoped `IOutbox` backed by an Elasticsearch index when `RegisterOutbox` is enabled; documents are staged with `op_type=create` for idempotency — HTTP 409 is swallowed silently
- registers a scoped `IEventDispatchStore` over that same outbox index when `RegisterOutbox` is enabled, so staged Elasticsearch events can be read and durable dispatch outcomes can be written back through the runtime-neutral eventing contract
- registers a scoped `IInbox` backed by a separate Elasticsearch index when `RegisterInbox` is enabled; existence is checked via `GetAsync` by document id; marking uses `op_type=create` with 409 swallow
- exposes operator-facing outbox and inbox descriptors through the engine runtime surfaces
- projects outbox and inbox descriptors through the `event-driven-integration` technology surface when that technology is active
- publishes capability metadata `data.elasticsearch`, `data.search-store`, and optionally `data.outbox.elasticsearch` and `data.inbox.elasticsearch` introspectable at runtime through the manifest
- redacts inline URI user-info and query secrets in capability metadata; `uri` contains only scheme, host, port, and path, while `uriCredentialsConfigured` and `secretProjection = redacted` preserve operator posture without leaking credentials

## Main surfaces

- `Configuration/ElasticsearchDataOptions.cs`
- `Modules/ElasticsearchDataModule.cs`
- `Registration/ElasticsearchDataEngineBuilderExtensions.cs`
- `Services/ElasticsearchOutbox.cs`
- `Services/ElasticsearchEventDispatchStore.cs`
- `Services/ElasticsearchOutboxRuntimeSurfaceContributor.cs`
- `Services/ElasticsearchInbox.cs`
- `Services/ElasticsearchInboxRuntimeSurfaceContributor.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral `IReadStore` / `IWriteStore` dispatching surface. `Cephalon.Data.Elasticsearch` adds the Elasticsearch-backed outbox and inbox persistence paths that let event-driven workloads stage and track messages against a search index without switching to a relational or document-oriented provider, and it now also exposes the same staged outbox through `IEventDispatchStore` so consumer-managed or adapter-owned dispatch loops can read pending items and persist durable dispatch outcomes without Elasticsearch-specific host glue.

## Registration

```csharp
engine.AddElasticsearchData("http://localhost:9200");
```

To enable outbox and inbox paths:

```csharp
engine.AddElasticsearchData("http://localhost:9200", options =>
{
    options.RegisterOutbox = true;
    options.RegisterInbox = true;
    options.IndexPrefix = "myapp-";  // optional — prefix all Cephalon-managed index names
});
```

With Basic authentication:

```csharp
engine.AddElasticsearchData("https://my-cluster.es.io:9200", options =>
{
    options.Username = "elastic";
    options.Password = "secret";
    options.RegisterOutbox = true;
});
```

To resolve the node URI from the root `Uris` section:

```csharp
engine.AddElasticsearchData(options =>
{
    options.UriName = "SearchCluster";
    options.Username = "elastic";
    options.Password = "secret";
    options.RegisterOutbox = true;
});
```

```json
{
  "Uris": {
    "SearchCluster": "https://my-cluster.es.io:9200"
  },
  "Engine": {
    "Data": {
      "Elasticsearch": {
        "UriName": "SearchCluster",
        "IndexPrefix": "myapp-"
      }
    }
  }
}
```

`UriName` and `Uri` are mutually exclusive. If both are set, the pack throws during service
registration. Leaving both unset falls back to `http://localhost:9200`.

## Configuration options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `UriName` | `string?` | `null` | Root `Uris` key to resolve for Elasticsearch |
| `Uri` | `string?` | `null` | Inline Elasticsearch node URI |
| `Username` | `string?` | `null` | Optional username for Basic authentication |
| `Password` | `string?` | `null` | Optional password for Basic authentication |
| `IndexPrefix` | `string` | `""` | Prefix applied to all Cephalon-managed index names |
| `RegisterOutbox` | `bool` | `false` | Register `IOutbox` backed by `{IndexPrefix}outbox-messages` |
| `RegisterInbox` | `bool` | `false` | Register `IInbox` backed by `{IndexPrefix}inbox-receipts` |

## Outbox document schema (`{IndexPrefix}outbox-messages`)

| Field | JSON name | Type | Notes |
|-------|-----------|------|-------|
| `MessageId` | `message_id` | string | Document `_id`; idempotency key via `op_type=create` |
| `ChannelId` | `channel_id` | string | Target channel identifier |
| `MessageType` | `message_type` | string | Fully qualified CLR message type name |
| `Payload` | `payload` | string | `System.Text.Json`-serialized message body |
| `ContentType` | `content_type` | string | MIME content type |
| `CorrelationId` | `correlation_id` | string? | Optional causality tracking identifier |
| `TenantId` | `tenant_id` | string? | Optional multi-tenancy discriminator |
| `OccurredAtUtc` | `occurred_at_utc` | DateTime | When the domain event occurred |
| `CreatedAtUtc` | `created_at_utc` | DateTime | When the message was staged |
| `DispatchAttemptCount` | `dispatch_attempt_count` | int | Initialized to `0` |
| `HeadersJson` | `headers_json` | string | `System.Text.Json`-serialized headers dictionary |
| `MetadataJson` | `metadata_json` | string | `System.Text.Json`-serialized metadata dictionary |

## Inbox document schema (`{IndexPrefix}inbox-receipts`)

| Field | JSON name | Type | Notes |
|-------|-----------|------|-------|
| `MessageId` | `message_id` | string | Document `_id`; idempotency key |
| `ChannelId` | `channel_id` | string | Channel the message arrived on |
| `MessageType` | `message_type` | string | Fully qualified CLR message type name |
| `ReceivedAtUtc` | `received_at_utc` | DateTime | When the message was received |
| `ProcessedAtUtc` | `processed_at_utc` | DateTime | When the message was first processed |
| `CorrelationId` | `correlation_id` | string? | Optional causality tracking identifier |
| `TenantId` | `tenant_id` | string? | Optional multi-tenancy discriminator |

## Idempotency semantics

Both `IOutbox.EnqueueAsync` and `IInbox.MarkProcessedAsync` use `op_type=create` when indexing documents:

- HTTP 409 (document already exists) is swallowed silently — making repeated calls with the same message id safe no-ops
- Any other non-success response is a real error and throws `InvalidOperationException`

`IInbox.HasProcessedAsync` uses `GetAsync` by document id — the `Found` property on the response determines whether the message has been processed.

## Runtime capabilities

When `ElasticsearchDataModule` is active, the following capability keys appear in the runtime manifest:

| Capability key | When registered |
|----------------|-----------------|
| `data.elasticsearch` | Always |
| `data.search-store` | Always |
| `data.outbox.elasticsearch` | `RegisterOutbox = true` |
| `data.inbox.elasticsearch` | `RegisterInbox = true` |

The provider capability sanitizes inline URI metadata. A URI such as
`http://user:password@localhost:9200/search?apikey=secret` is reported as
`http://localhost:9200/search` with `uriCredentialsConfigured = true` and
`secretProjection = redacted`.

## Runtime surface entries

When the `event-driven-integration` technology is active, the following entries appear under `/engine/snapshot`:

| Surface | Entry id | `provider` metadata |
|---------|----------|---------------------|
| `outbox-producers` | `elasticsearch-outbox` | `elasticsearch` |
| `inbox-stores` | `elasticsearch-inbox` | `elasticsearch` |

## Event store

The companion event-store pack is `Cephalon.EventSourcing.Elasticsearch`. It registers `IEventStore` with compound document ids `{streamId}#{streamVersion}` for uniqueness and application-layer optimistic concurrency.

```csharp
builder.Services.AddCephalonElasticsearchEventSourcing(
    uri: "http://localhost:9200",
    indexName: "event-streams");
```

## Not shipped in this slice

This pack intentionally does not claim:

- full-text search, aggregations, or `IReadStore` / `IWriteStore` dispatch backed by Elasticsearch
- index lifecycle management (ILM) or template provisioning
- cross-cluster search or multi-index querying
- pack-owned dispatch loops or broker-specific retry scheduling beyond the runtime-neutral `IEventDispatchStore` bridge
- bulk indexing or pipeline ingestion

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.EventSourcing.Elasticsearch](event-sourcing-elasticsearch.md)
- [Cephalon.Data.OpenSearch](data-opensearch.md)
- [Cephalon.Data.MongoDB](data-mongodb.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
