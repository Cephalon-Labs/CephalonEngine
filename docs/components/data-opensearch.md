# Cephalon.Data.OpenSearch

`Cephalon.Data.OpenSearch` is the OpenSearch search-store companion pack for Cephalon, mirroring the pattern established by `Cephalon.Data.Elasticsearch` against the OpenSearch.Client 1.9.0 API surface.

## What it owns

- registers a singleton `OpenSearchClient` (from `OpenSearch.Client` 1.9.0) using `TryAdd` semantics so a host-owned client is never displaced
- optionally configures Basic authentication via `ConnectionSettings.BasicAuthentication(...)` when `Username` is set
- registers a scoped `IOutbox` backed by an OpenSearch index when `RegisterOutbox` is enabled; documents are staged with `OpType.Create` for idempotency — HTTP 409 (`ServerError.Status == 409`) is swallowed silently
- registers a scoped `IEventDispatchStore` over that same outbox index when `RegisterOutbox` is enabled, so staged OpenSearch events can be read and durable dispatch outcomes can be written back through the runtime-neutral eventing contract
- registers a scoped `IInbox` backed by a separate OpenSearch index when `RegisterInbox` is enabled; existence is checked via `GetAsync` by document id; marking uses `OpType.Create` with 409 swallow
- exposes operator-facing outbox and inbox descriptors through the engine runtime surfaces
- projects outbox and inbox descriptors through the `event-driven-integration` technology surface when that technology is active
- publishes capability metadata `data.opensearch`, `data.search-store`, and optionally `data.outbox.opensearch` and `data.inbox.opensearch` introspectable at runtime through the manifest

## Main surfaces

- `Configuration/OpenSearchDataOptions.cs`
- `Modules/OpenSearchDataModule.cs`
- `Registration/OpenSearchDataEngineBuilderExtensions.cs`
- `Services/OpenSearchOutbox.cs`
- `Services/OpenSearchEventDispatchStore.cs`
- `Services/OpenSearchOutboxRuntimeSurfaceContributor.cs`
- `Services/OpenSearchInbox.cs`
- `Services/OpenSearchInboxRuntimeSurfaceContributor.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data.OpenSearch` adds OpenSearch-backed outbox and inbox persistence paths that let event-driven workloads stage and track messages against a search index. It is a drop-in sibling to `Cephalon.Data.Elasticsearch` for teams running OpenSearch clusters, and it now also exposes the same staged outbox through `IEventDispatchStore` so consumer-managed or adapter-owned dispatch loops can read pending items and persist durable dispatch outcomes without OpenSearch-specific host glue.

## Registration

```csharp
engine.AddOpenSearchData("http://localhost:9200");
```

To enable outbox and inbox paths:

```csharp
engine.AddOpenSearchData("http://localhost:9200", options =>
{
    options.RegisterOutbox = true;
    options.RegisterInbox = true;
    options.IndexPrefix = "myapp-";  // optional — prefix all Cephalon-managed index names
});
```

With Basic authentication:

```csharp
engine.AddOpenSearchData("https://my-cluster.opensearch.example.com:9200", options =>
{
    options.Username = "admin";
    options.Password = "secret";
    options.RegisterOutbox = true;
});
```

To resolve the node URI from the root `Uris` section:

```csharp
engine.AddOpenSearchData(options =>
{
    options.UriName = "SearchCluster";
    options.Username = "admin";
    options.Password = "secret";
    options.RegisterOutbox = true;
});
```

```json
{
  "Uris": {
    "SearchCluster": "https://my-cluster.opensearch.example.com:9200"
  },
  "Engine": {
    "Data": {
      "OpenSearch": {
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
| `UriName` | `string?` | `null` | Root `Uris` key to resolve for OpenSearch |
| `Uri` | `string?` | `null` | Inline OpenSearch node URI |
| `Username` | `string?` | `null` | Optional username for Basic authentication |
| `Password` | `string?` | `null` | Optional password for Basic authentication |
| `IndexPrefix` | `string` | `""` | Prefix applied to all Cephalon-managed index names |
| `RegisterOutbox` | `bool` | `false` | Register `IOutbox` backed by `{IndexPrefix}outbox-messages` |
| `RegisterInbox` | `bool` | `false` | Register `IInbox` backed by `{IndexPrefix}inbox-receipts` |

## Outbox document schema (`{IndexPrefix}outbox-messages`)

| Field | JSON name | Type | Notes |
|-------|-----------|------|-------|
| `MessageId` | `message_id` | string | Document `_id`; idempotency key via `OpType.Create` |
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

Both `IOutbox.EnqueueAsync` and `IInbox.MarkProcessedAsync` use `OpType.Create` when indexing documents:

- HTTP 409 (`response.ServerError?.Status == 409`) is swallowed silently — making repeated calls with the same message id safe no-ops
- Any other non-success response (`!response.IsValid`) is a real error and throws `InvalidOperationException`

`IInbox.HasProcessedAsync` uses `GetAsync` by document id — `response.IsValid && response.Found` determines whether the message has been processed.

## Runtime capabilities

When `OpenSearchDataModule` is active, the following capability keys appear in the runtime manifest:

| Capability key | When registered |
|----------------|-----------------|
| `data.opensearch` | Always |
| `data.search-store` | Always |
| `data.outbox.opensearch` | `RegisterOutbox = true` |
| `data.inbox.opensearch` | `RegisterInbox = true` |

## Event store

The companion event-store pack is `Cephalon.EventSourcing.OpenSearch`. It registers `IEventStore` with compound document ids `{streamId}#{streamVersion}` for uniqueness and application-layer optimistic concurrency.

```csharp
builder.Services.AddCephalonOpenSearchEventSourcing(
    uri: "http://localhost:9200",
    indexName: "event-streams");
```

## Not shipped in this slice

This pack intentionally does not claim:

- full-text search, aggregations, or `IReadStore` / `IWriteStore` dispatch backed by OpenSearch
- index lifecycle management or template provisioning
- cross-cluster search or multi-index querying
- pack-owned dispatch loops or broker-specific retry scheduling beyond the runtime-neutral `IEventDispatchStore` bridge
- bulk indexing or pipeline ingestion

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.EventSourcing.OpenSearch](event-sourcing-opensearch.md)
- [Cephalon.Data.Elasticsearch](data-elasticsearch.md)
- [Cephalon.Data.MongoDB](data-mongodb.md)
- [Cephalon.Engine](engine.md)
- [Architecture](../architecture.md)
