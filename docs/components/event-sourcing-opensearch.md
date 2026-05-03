# Cephalon.EventSourcing.OpenSearch

> **Maturity:** `M1` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.EventSourcing.OpenSearch` is the OpenSearch event-store provider for Cephalon, mirroring the pattern established by `Cephalon.EventSourcing.Elasticsearch` against the OpenSearch.Client 1.9.0 API surface.

## What it owns

- an OpenSearch-backed implementation of `IEventStore` registered through `AddCephalonOpenSearchEventSourcing()`
- the `OpenSearchEventEntry` POCO representing the fields stored in each event document
- compound document id `{streamId}#{streamVersion}` — ensures document uniqueness at the OpenSearch level via `OpType.Create`
- optimistic-version append semantics: reads the current stream version before every `AppendAsync`, compares against `expectedVersion`, and throws `EventStreamConcurrencyException` before writing if they differ
- a fallback concurrency guard via `OpType.Create` — if a concurrent writer commits the same version, OpenSearch returns HTTP 409 (`ServerError.Status == 409`) and the provider re-reads the actual version before rethrowing `EventStreamConcurrencyException`
- stream replay through `ReadStreamAsync` returning events ordered by `stream_version` ascending
- `GetVersionAsync` returning `-1` for a stream that does not yet exist
- `System.Text.Json` serialization for event payloads using the concrete event CLR type
- event type round-tripping through `AssemblyQualifiedName`

## Main surfaces

- `OpenSearchEventEntry.cs`
- `OpenSearchEventStore.cs`
- `Hosting/OpenSearchEventSourcingServiceCollectionExtensions.cs`

## How it fits

This pack sits on top of `Cephalon.EventSourcing`, not in place of it. `Cephalon.EventSourcing.OpenSearch` supplies the OpenSearch implementation of `IEventStore` for teams running OpenSearch clusters rather than Elastic's managed offering. The implementation is functionally equivalent to `Cephalon.EventSourcing.Elasticsearch` — the only differences are the client type (`OpenSearchClient` vs `ElasticsearchClient`) and the response validity API (`response.IsValid` vs `response.IsSuccess()`).

## Registration

```csharp
builder.Services.AddCephalonOpenSearchEventSourcing(
    uri: "http://localhost:9200",
    indexName: "event-streams");
```

`OpenSearchClient` is registered using `TryAdd` semantics — a host that already registered a shared client keeps its own instance. `IEventStore` is registered as a singleton.

## Event document schema

Documents are stored with `_id = {streamId}#{streamVersion}`.

| Field | JSON name | Type | Notes |
|-------|-----------|------|-------|
| `StreamId` | `stream_id` | string | Logical aggregate / stream identifier |
| `StreamVersion` | `stream_version` | long | Per-stream monotonic version (1-based; stream starts at version 1) |
| `EventType` | `event_type` | string | `AssemblyQualifiedName` of the concrete event CLR type |
| `Payload` | `payload` | string | `System.Text.Json`-serialized event body using the concrete type |
| `OccurredAtUtc` | `occurred_at_utc` | DateTime | UTC timestamp when the domain event occurred |
| `AppendedAtUtc` | `appended_at_utc` | DateTime | UTC wall-clock time of the index call |
| `CorrelationId` | `correlation_id` | string? | Optional causality tracking identifier |
| `TenantId` | `tenant_id` | string? | Optional tenant identifier |

## Concurrency semantics

| Scenario | Behaviour |
|----------|-----------|
| `GetVersionAsync` on empty stream | Returns `-1` (no hits for `term: stream_id`) |
| `AppendAsync(..., expectedVersion: -1)` on empty stream | Succeeds — assigns versions starting at `1` |
| `AppendAsync(..., expectedVersion: N)` when stream is at `N` | Succeeds — appends events at versions `N+1, N+2, ...` |
| `AppendAsync` with wrong `expectedVersion` | `EventStreamConcurrencyException` thrown before insert |
| Concurrent writer commits same version (race after version read) | `OpType.Create` returns HTTP 409; provider re-reads actual version and throws `EventStreamConcurrencyException` |
| Event's `StreamVersion` does not match expected sequential assignment | `InvalidOperationException` thrown |
| Event's `StreamId` does not match the `streamId` argument | `InvalidOperationException` thrown |

## Stream replay

`ReadStreamAsync(streamId, fromVersion)` uses a `bool/must` query combining a `term` on `stream_id` and a `range` on `stream_version >= fromVersion`, sorted ascending by `stream_version`, with `size: 10000`.

## Not shipped in this slice

This provider intentionally does not claim:

- snapshot persistence (`ISnapshotStore` is not implemented)
- projection rebuild orchestration or archival
- background stream replay workers
- OpenSearch data streams or index template provisioning
- transport or event-bus integration
- multi-tenancy discriminator population

## Related docs

- [Cephalon.EventSourcing](event-sourcing.md)
- [Cephalon.Data.OpenSearch](data-opensearch.md)
- [Cephalon.EventSourcing.Elasticsearch](event-sourcing-elasticsearch.md)
- [Cephalon.EventSourcing.MongoDB](event-sourcing-mongodb.md)
- [Cephalon.Engine](engine.md)
- [Technology packs](../technology-packs.md)
