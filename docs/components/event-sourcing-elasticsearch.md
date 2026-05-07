# Cephalon.EventSourcing.Elasticsearch

> **Maturity:** `M1` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.EventSourcing.Elasticsearch` is the Elasticsearch event-store provider for Cephalon, following the same provider pattern as `Cephalon.EventSourcing.MongoDB` and `Cephalon.EventSourcing.Redis`. It delivers the `IEventStore` contract against Elasticsearch search indices using compound document ids for uniqueness and application-layer optimistic concurrency.

## What it owns

- an Elasticsearch-backed implementation of `IEventStore` registered through `AddCephalonElasticsearchEventSourcing()`
- a sanitized `IEventStoreContributor` entry projected through `IEventStoreCatalog` and the `event-sourcing` runtime surface
- the `ElasticsearchEventEntry` POCO representing the fields stored in each event document
- compound document id `{streamId}#{streamVersion}` — ensures document uniqueness at the Elasticsearch level via `op_type=create`
- optimistic-version append semantics: reads the current stream version before every `AppendAsync`, compares against `expectedVersion`, and throws `EventStreamConcurrencyException` before writing if they differ
- a fallback concurrency guard via `op_type=create` — if a concurrent writer commits the same version between the read and the index, Elasticsearch returns HTTP 409 and the provider re-reads the actual version before rethrowing `EventStreamConcurrencyException`
- stream replay through `ReadStreamAsync` returning events ordered by `stream_version` ascending, using a `bool/must` query on `stream_id` + `range` on `stream_version`
- `GetVersionAsync` returning `-1` for a stream that does not yet exist (no hits for the `term` query)
- event payload serialization through the shared Cephalon event-type registry
- event type round-tripping through stable registry names, with descriptor aliases for legacy `AssemblyQualifiedName` rows

## Main surfaces

- `ElasticsearchEventEntry.cs`
- `ElasticsearchEventStore.cs`
- `Hosting/ElasticsearchEventSourcingServiceCollectionExtensions.cs`
- `Services/ElasticsearchEventStoreContributor.cs`

## How it fits

This pack sits on top of `Cephalon.EventSourcing`, not in place of it. `Cephalon.EventSourcing` owns the `IEventStore` contract, the `IDomainEvent` marker, and `EventStreamConcurrencyException`. `Cephalon.EventSourcing.Elasticsearch` supplies the Elasticsearch search-index implementation of that contract so event-sourced aggregates can keep the same `IEventStore` injection point while using Elasticsearch as the backing store.

## Registration

```csharp
builder.Services.AddCephalonElasticsearchEventSourcing(
    uri: "http://localhost:9200",
    indexName: "event-streams");
```

`ElasticsearchClient` is registered using `TryAdd` semantics — a host that already registered a shared client keeps its own instance. `IEventStore` is registered as a singleton, and the shared event-type registry is registered if it is not already present. The host still registers concrete event payloads through `AddCephalonEventType<TEvent>(...)` or `AddCephalonEventTypeWithJsonTypeInfo<TEvent>(...)`.

## Event document schema

Documents are stored with `_id = {streamId}#{streamVersion}`.

| Field | JSON name | Type | Notes |
|-------|-----------|------|-------|
| `StreamId` | `stream_id` | string | Logical aggregate / stream identifier |
| `StreamVersion` | `stream_version` | long | Per-stream monotonic version (zero-based; stream starts at version 0) |
| `EventType` | `event_type` | string | Stable Cephalon event-type registry name |
| `Payload` | `payload` | string | Serialized event body produced by the registered event-type descriptor |
| `OccurredAtUtc` | `occurred_at_utc` | DateTime | UTC timestamp when the domain event occurred |
| `AppendedAtUtc` | `appended_at_utc` | DateTime | UTC wall-clock time of the index call |
| `CorrelationId` | `correlation_id` | string? | Optional causality tracking identifier |
| `TenantId` | `tenant_id` | string? | Optional tenant identifier |

## Concurrency semantics

| Scenario | Behaviour |
|----------|-----------|
| `GetVersionAsync` on empty stream | Returns `-1` (no hits for `term: stream_id`) |
| `AppendAsync(..., expectedVersion: -1)` on empty stream | Succeeds — assigns versions starting at `0` |
| `AppendAsync(..., expectedVersion: N)` when stream is at `N` | Succeeds — appends events at versions `N+1, N+2, ...` |
| `AppendAsync` with wrong `expectedVersion` | `EventStreamConcurrencyException` thrown before insert |
| Concurrent writer commits same version (race after version read) | `op_type=create` returns HTTP 409; provider re-reads actual version and throws `EventStreamConcurrencyException` |
| Event's `StreamVersion` does not match expected sequential assignment | `InvalidOperationException` thrown |
| Event's `StreamId` does not match the `streamId` argument | `InvalidOperationException` thrown |

## Stream replay

`ReadStreamAsync(streamId, fromVersion)` executes a `bool/must` query:

```json
{
  "query": {
    "bool": {
      "must": [
        { "term": { "stream_id": "<streamId>" } },
        { "range": { "stream_version": { "gte": <fromVersion> } } }
      ]
    }
  },
  "sort": [{ "stream_version": "asc" }],
  "size": 10000
}
```

and returns an `IAsyncEnumerable<IDomainEvent>`, yielding events one by one. The event payload is resolved through `IEventTypeRegistry` by `event_type`; a missing descriptor throws `InvalidOperationException`. Descriptors include legacy `AssemblyQualifiedName` aliases by default so older documents can still be read after hosts register the concrete event type.

## Not shipped in this slice

This provider intentionally does not claim:

- snapshot persistence (`ISnapshotStore` is not implemented)
- projection rebuild orchestration or archival
- background stream replay workers
- Elasticsearch data streams or index lifecycle management
- transport or event-bus integration
- multi-tenancy discriminator population

## Related docs

- [Cephalon.EventSourcing](event-sourcing.md)
- [Cephalon.Data.Elasticsearch](data-elasticsearch.md)
- [Cephalon.EventSourcing.OpenSearch](event-sourcing-opensearch.md)
- [Cephalon.EventSourcing.MongoDB](event-sourcing-mongodb.md)
- [Cephalon.Engine](engine.md)
- [Technology packs](../technology-packs.md)
