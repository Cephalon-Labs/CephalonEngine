# Diagnostic ID registry

This document is the authoritative registry of `EventId` ranges allocated to each `Cephalon.*` package. The engine emits structured logs through `LoggerMessage.Define<>` factories or `[LoggerMessage]` source-generated factories, both of which require a stable numeric `EventId`. Without a central registry, two packages can pick the same range, and observability companion packs that aggregate events by ID alone lose the ability to disambiguate the source.

The registry reflects the repository state as of `May 3, 2026`.

## Allocation discipline

- **One range per package.** A package owns one numeric range; every `EventId` it emits falls inside that range.
- **Contiguous, ascending allocation.** New events take the next available ID inside the package's range. Don't skip IDs to "leave room" — if a range fills up, allocate a new range and document the bridge.
- **Range size 10 by default.** A package allocates a 10-event range (e.g. `5100`–`5109`). Packages with high event density (e.g. `Cephalon.Data` CDC capture and retry) allocate two adjacent ranges of 10 each. Packages with very high density (e.g. CDC providers across MongoDB/Postgres/SqlServer/MySql/Oracle) allocate a per-provider range inside a shared 1000-block.
- **Gaps are reservations.** When you see `5110`–`5499 unallocated`, that range is reserved for future Behaviors-adjacent or pattern-execution-adjacent allocations. Don't squat on a reserved gap; pick a new range above the highest current allocation.
- **EventId.Name uses `nameof(...)`.** The string name is the C# constant name of the corresponding `EventId` field, so the diagnostic-id and the source-code symbol stay in lockstep through refactors.
- **Cross-package collisions on IDs `0`, `1`, `2`.** Pattern-execution strategies and dependency-health probe events historically used IDs `0`/`1`/`2` because they were authored before the registry existed. This is tolerated for now because the EventId.Name disambiguates within the consuming observability pack, but new allocations should avoid IDs below `2000` to keep the bottom range collision-free.

## Allocated ranges

| Range | Owning package | Events | Defining file |
|---|---|---|---|
| `0`–`2` (collisions) | Cross-package pattern-execution strategies and dependency-health probes | several | `Cephalon.Behaviors.Patterns/SagaExecutionStrategy.cs`, `EventDrivenExecutionStrategy.cs`, `ProcessManagerExecutionStrategy.cs`, `ChoreographySagaExecutionStrategy.cs`; `Cephalon.Eventing.Behaviors/EventingSagaChoreographyPublisher.cs`; `Cephalon.Observability.DependencyHealth.Core/DependencyHealthProbeHostedServiceBase.cs` |
| `2000`–`2005` | `Cephalon.Engine` runtime lifecycle | 6 (RuntimeTransition, ModuleTransition, ExecutionGraphTransition, HostedExecutionTransition, RuntimeFailure, ModuleFailure) | `src/Cephalon.Engine/Runtime/EngineRuntime.cs` |
| `4200`–`4210` | `Cephalon.Eventing` | 11 (publication / subscription lifecycle + dispatch + retry / skip) | `src/Cephalon.Eventing/Diagnostics/EventingLoggerMessages.cs` |
| `4306` | `Cephalon.Eventing.Wolverine` | 1 (Wolverine subscription observation projection failed) | `src/Cephalon.Eventing.Wolverine/Services/WolverineManagedEventSubscriptionExecutionProcessor.cs` |
| `4600`–`4611` | `Cephalon.Audit` family | 12 (4600–4601 core audit write; 4610–4611 history retention) | `src/Cephalon.Audit/Diagnostics/AuditDiagnosticsConventionContributor.cs`; `src/Cephalon.Audit.EntityFramework/Hosting/EntityFrameworkAuditHistoryRetentionHostedService.cs` |
| `5100`–`5109` | `Cephalon.Behaviors` | 10 (Dispatching, Dispatched, DispatchFailed, CompatibilityViolation, TopologyResolved, BehaviorRegistered, TransportBound, TransportBindFailed, AdvisoryRaised, SlotCompiled) | `src/Cephalon.Behaviors/Diagnostics/BehaviorDiagnostics.cs` |
| `6200`–`6207` | `Cephalon.Data` CDC capture | 8 (capture loop start/stop, missing implementation/binding, unknown impl, batch succeeded/failed, ack failed) | `src/Cephalon.Data/Services/CdcCaptureHostedService.cs` |
| `6240`–`6243` | `Cephalon.Data` managed-connector retry | 4 (retry loop start/stop, attempt succeeded/failed) | `src/Cephalon.Data/Services/ManagedConnectorAutomaticRetryHostedService.cs` |
| `6920`–`6922` | `Cephalon.Data.MongoDB` change-stream capture | 3 (missing services, missing descriptor, capture loop failure) | `src/Cephalon.Data.MongoDB/Services/MongoDbChangeStreamCaptureHostedService.cs` |
| `6940`–`6942`, `6960` | `Cephalon.Data.SqlServer` CDC | 4 (missing services, missing descriptor, capture loop failure; `6960` latest-checkpoint bootstrap on the transport) | `src/Cephalon.Data.SqlServer/Services/SqlServerCdcCaptureHostedService.cs`; `SqlServerCdcTransport.cs` |
| `6980`–`6982`, `6990` | `Cephalon.Data.Postgres` logical replication | 4 (missing services, missing descriptor, capture loop failure; `6990` replication slot created on the transport) | `src/Cephalon.Data.Postgres/Services/PostgresLogicalReplicationCaptureHostedService.cs`; `PostgresLogicalReplicationTransport.cs` |
| `7000`–`7002` | `Cephalon.Data.MySql` binlog capture | 3 (missing services, missing descriptor, capture loop failure) | `src/Cephalon.Data.MySql/Services/MySqlBinlogCaptureHostedService.cs` |
| `7300`, `7400`–`7402` | `Cephalon.Data.Oracle` LogMiner | 4 (`7300` ignored end-LogMiner failure on the transport; `7400`–`7402` capture missing services / descriptor / loop failure) | `src/Cephalon.Data.Oracle/Services/OracleLogMinerCaptureHostedService.cs`; `OracleLogMinerTransport.cs` |
| `21001`–`21013` | `Cephalon.Engine` cell-traffic automation | 7 (`21001`–`21003` provider materialization; `21011`–`21013` edge materialization) | `src/Cephalon.Engine/Services/CellTrafficAutomationProviderMaterializationHostedService.cs`; `CellTrafficAutomationEdgeMaterializationHostedService.cs` |
| `21400`–`21403` | `Cephalon.Edge.KubernetesGateway` | 4 (observation loop start/stop, observation failed, cleanup sweep failed) | `src/Cephalon.Edge.KubernetesGateway/Services/KubernetesGatewayTrafficObservationHostedService.cs` |
| `21500`–`21503` | `Cephalon.Edge.Traefik` | 4 (observation loop start/stop, observation failed, cleanup sweep failed) | `src/Cephalon.Edge.Traefik/Services/TraefikTrafficObservationHostedService.cs` |

## Reserved gaps

The gaps below are deliberately reserved for adjacent allocations the named package is likely to claim next. Don't use a reserved gap from a different family — pick a fresh range above the highest current allocation.

| Reserved range | Reason |
|---|---|
| `2006`–`4199` | Engine and engine-adjacent ranges. New engine-runtime events take `2006`+; new engine-composition events take `2100`+ (when needed) |
| `4211`–`4305`, `4307`–`4599` | Eventing-adjacent ranges. New Eventing core events take `4211`+; per-provider Eventing packs (Kafka, RabbitMQ, NATS) take `4307`+ when allocated |
| `4612`–`5099` | Audit-adjacent ranges. New audit-provider packs allocate inside `4700`+ (e.g. `Cephalon.Audit.MongoDB` would take `4700`–`4709`) |
| `5110`–`6199` | Behaviors-adjacent ranges. `Cephalon.Behaviors.Http` / `.Messaging` / `.Patterns` allocate inside `5110`–`5199` when they need their own diagnostic factories |
| `6244`–`6919`, `6923`–`6939`, `6943`–`6959`, `6961`–`6979`, `6983`–`6989`, `6991`–`6999`, `7003`–`7299`, `7301`–`7399`, `7403`–`20999` | CDC / data-provider-adjacent ranges. Each new data provider takes its own contiguous slot above the highest current provider |
| `21014`–`21399`, `21404`–`21499`, `21504`+ | Edge / cell-traffic-adjacent ranges. Each new edge gateway provider takes its own slot above the highest current provider |

## Convention

When you allocate a new range:

1. Read this file. Pick the next contiguous range above the highest current allocation in your package family.
2. Add a row to *Allocated ranges* with the package, event count, and defining file.
3. Update the matching *Reserved gaps* row so a future maintainer doesn't double-allocate.
4. Reference the registry from your package's `Diagnostics` class (typical XML doc: `// EventId range 5500-5509 allocated per docs/diagnostic-id-registry.md`).

## Cross-references

- [Engineering standards](engineering-standards.md) — the per-package diagnostic-id range discipline this registry instantiates
- [Cephalon.Diagnostics component doc](components/diagnostics.md) — the `ActivitySource`, `Meter`, and attribute-key surfaces that complement these `EventId` ranges
- [SRE posture](sre-posture.md) — the SLI catalogue that reads structured-logging events emitted under these IDs
