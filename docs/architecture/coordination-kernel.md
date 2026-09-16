# Coordination kernel decision and adoption

Decision: accepted for the bounded, instance-local execution slice on September 16, 2026. Owners: ENG-719 / ENG-720 under [ENG-714](https://github.com/Cephalon-Labs/CephalonEngine/issues/1405). Durable storage and operator authorization remain separate acceptance gates, ENG-721 / ENG-722. This decision does not promote any package to M3 or M4.

## Existing contract inventory

The inventory examines executable source, not just descriptive flags.

| Family | Contract and executable source | Atomicity and ownership today | Additive migration bridge |
| --- | --- | --- | --- |
| Eventing | [`IEventDispatchRemediationCommandJournal`](../../src/Cephalon.Abstractions/Data/IEventDispatchRemediationCommandJournal.cs), [`EntityFrameworkEventDispatchRemediationCommandJournal`](../../src/Cephalon.Data.EntityFramework/Services/EntityFrameworkEventDispatchRemediationCommandJournal.cs) | Reservation inserts an EF journal entry; duplicate insert reads the existing command after `DbUpdateException`. Final result writes are separate. This is not a demonstrated atomic transaction with a remote broker effect or a monotonic fencing token. | Retain command ids, existing journals and typed snapshots. Bind outbox/message/channel/action to immutable desired intent. ENG-721/724 must prove reservation conflicts, crash windows and protected writes before replacing this path. |
| CDC | [`CdcCaptureExecutionRuntimeCatalog`](../../src/Cephalon.Data/Services/CdcCaptureExecutionRuntimeCatalog.cs), [`CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStatus`](../../src/Cephalon.Abstractions/Data/CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStatus.cs) | `CreateManagedConnectorDistributedRetryLease` derives posture from reported ownership, coordination and persisted history. An `IdempotentSafe` catalog state alone does not acquire a distributed lease or enforce CAS at a provider write. | Map command identity, connector revision and provider observations; keep reporting separate from ownership. ENG-721/724 must identify the actual provider transaction and fencing enforcement. |
| Governance | [`TenantInvitationDeliveryRetryExecutionCoordinator`](../../src/Cephalon.MultiTenancy.Governance/Services/TenantInvitationDeliveryRetryExecutionCoordinator.cs), [`TenantInvitationDeliveryStatusReconciler`](../../src/Cephalon.MultiTenancy.Governance/Services/TenantInvitationDeliveryStatusReconciler.cs) | Retry overlap uses a process-local `SemaphoreSlim`; delivery reconciliation remains provider-specific. Neither is a cross-node lease. Actor assertions are not authorization grants. | Preserve delivery callback semantics and existing snapshots. Bind tenant, invitation, action and immutable intent; use safe no-effect retries only where the sender can prove them. Approval and scoped authority belong to ENG-722/725. |
| Edge | [`KubernetesGatewayTrafficObservationSource`](../../src/Cephalon.Edge.KubernetesGateway/Services/KubernetesGatewayTrafficObservationSource.cs), [`TraefikTrafficObservationSource`](../../src/Cephalon.Edge.Traefik/Services/TraefikTrafficObservationSource.cs) | Kubernetes replacement carries the observed HTTPRoute `resourceVersion`, providing a provider revision precondition; this is not an engine lease or a transaction across every related resource. Traefik file/control-plane observations are not proof of shared distributed CAS. | Preserve provider projections, resource versions and drift results. ENG-725 binds plan intent to provider revisions and proves stale-plan rejection across the actual write boundary. |

## Contract and safety decisions

`Cephalon.Abstractions.Coordination` holds immutable request/plan/result/attempt contracts and the provider effect seam. `Cephalon.Engine.Coordination` owns bounded execution and opt-in observation. Neither adds ASP.NET Core, EF, broker, Kubernetes, cloud or authentication dependencies to Abstractions. Public XML explains each boundary.

An operation key is `(TenantId, OperationId)` with ordinal case-sensitive comparison. Actor, action, target, desired revision, expected revision, observation and validity times bind the plan. The SHA-256 fingerprint is versioned by its canonical input domain and uses length-prefixed UTF-8 strings plus invariant UTC ticks. It prevents accidental ambiguous concatenation; it is not a signature or a secret. The provider must bind `DesiredRevision` to the *complete immutable payload*. Changing a payload under the same revision violates the contract. Replanning requires a new operation id.

Constructing `ReconciliationPlan` is pure: supplied desired/observed equality yields `Converged`, a failed expected-revision comparison yields `Stale`, otherwise `Ready`. Validity is positive and at most one day. UTC offsets canonicalize to the same fingerprint. Identifier lengths are bounded at 256 characters. There is no ambient discovery, clock read, random id, network call or write while planning.

`IReconciliationEffect` owns action binding, tenant/actor authorization and atomic revision verification at the protected write. A read followed by an unconditional write is insufficient. No generic provider success is inferred from a descriptor. Applications must register trusted implementations; the kernel exposes no HTTP apply route and no actionable operator link. ENG-722 will add shared approval/authorization and redacted durable audit; request actor/tenant fields do not substitute for it.

## Execution and recovery truth

| Trigger | Outcome | Allowed next step |
| --- | --- | --- |
| Same key, same plan, currently active | `Running` | Read again; do not invoke twice. A duplicate caller cannot cancel the original. |
| Same key, different fingerprint | `Conflict` | Reject the changed intent; investigate or create a new authorized operation. |
| Already observed desired revision | `Converged` | No effect. This is a supplied observation, not a fresh provider read. |
| Precondition mismatch in planning or at provider write | `Stale` | Observe again and create a new plan/id. |
| Before validity start, or deadline before invocation | `Expired` | No effect; replan with new evidence. |
| Cancellation before invocation / between confirmed no-effect retries | `Canceled` | No outstanding effect; retained reservation still prevents replay. |
| Provider confirms no effect and safe retry | retry, bounded by attempts/deadline | Exponential delay with stable per-plan jitter, capped at configured maximum. |
| All safe attempts consumed | `Exhausted` | Diagnose and replan; no automatic loop outside the budget. |
| Provider confirms desired revision / rejects without effects | `Applied` / `Rejected` | Terminal result retained. |
| Exception, unknown outcome, cancellation or deadline while effect is outstanding | `InDoubt` | Stop. Reconcile provider truth externally before any new action. Late success cannot silently replace uncertainty. |
| Reservation capacity reached | `CapacityExceeded` | Reject new operations; never evict an old idempotency key to make room. |
| Process or executor restart | no retained state | No durable replay/recovery guarantee. ENG-721 must supply durable reservation, fencing and crash recovery before production automation relies on them. |

Default bounds: 3 attempts, 1,024 retained operations, 100 ms initial delay and 5 s delay ceiling. Limits are immutable. `TimeProvider` controls both time and timers. A deadline token bounds asynchronous waiting even when a provider ignores cancellation, leaving the outcome uncertain and the reservation retained. Arbitrary synchronous blocking provider code cannot be preempted; adapters must be asynchronous and cooperative. Fencing remote effects remains a provider responsibility. Results copy attempt collections and never expose provider exception messages.

## Adoption and observation

```csharp
services.AddCephalonReconciliation(); // opt-in, one executor per service provider
// Register a verified IReconciliationEffect implementation in the owning companion.
var now = clock.GetUtcNow();
var request = new ReconciliationRequest(
    "operation-42", "tenant-7", "operator-9", "apply-route", "route-12", "revision-8", "revision-7");
var plan = new ReconciliationPlan(request, observedRevision: "revision-7", now, now.AddMinutes(1));
var result = await executor.ExecuteAsync(plan, authorizedEffect, cancellationToken);
```

The second real non-health `ExtensionSections` consumer is `coordination` schema `1.0`, contributed only when registered. It reports opaque plan fingerprints, outcomes, attempt counts, timestamps and explicit instance-local/non-durable ownership. It omits tenant/actor/target/payload/exception text and advertises no operator actions. It is an aggregate operational surface, not a tenant-scoped API; the host must protect snapshot access. Existing typed snapshot properties and the dependency-health section are unchanged. Registering twice does not duplicate a contributor.

## Verification and next gates

[`ReconciliationTests`](../../tests/Cephalon.Tests.Composition/Composition/ReconciliationTests.cs) executes the transition examples above, fingerprint binding, serialization, concurrent duplicate rejection, tenant separation, no-eviction capacity, cancellation, deadline, safe retries, late effects and snapshot compatibility. Tests use explicit timers rather than fixed sleeps. [`ReconciliationBenchmarks`](../../benchmarks/Cephalon.Benchmarks/Runtime/ReconciliationBenchmarks.cs) measures ready-plan construction including fingerprint allocation. See [delivery evidence](../coordination-kernel-delivery-2026-09.md) for measured results and limitations.

ENG-721 adds a transactional durable provider and two-process stale-writer/restart proof. ENG-722 adds shared apply-time authorization, approval binding and audit. ENG-723–725 integrate owned family loops, without wrapping existing unsafe effects and calling them safe. ENG-726 reviews individual maturity dossiers. Existing families are inventoried and migration seams selected here; none is silently switched to the new executor.

The design follows [Kubernetes controller desired/observed reconciliation](https://kubernetes.io/docs/concepts/architecture/controller/) and its [resource-version concurrency mechanism](https://kubernetes.io/docs/reference/using-api/api-concepts/#resource-versions), while keeping provider transactions explicit. .NET [TimeProvider](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview) provides replaceable clocks and timers. These sources motivate boundaries; passing Cephalon's executable proofs is still required.
