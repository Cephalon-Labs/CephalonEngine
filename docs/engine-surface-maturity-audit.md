# Engine Surface Maturity Audit

Surface maturity in this document reflects the repository state as of `May 5, 2026` (post-`ENG-371` Cephalon.Eventing OTel adapter activity emission and post-`ENG-402` Cephalon.Retrieval OTel adapter activity emission, with redaction-emission count now at seven sites including the new Eventing in-process publication-dispatch site).

## Why this document exists

Cephalon now ships a mix of:

- taxonomy and descriptor surfaces
- truthful runtime catalogs
- managed execution or provisioning runtimes
- adoption-ready tooling and operator experiences

That is healthy, but only if the repository says which kind of value each surface provides.

The current risk is not "metadata exists." The risk is letting descriptor-first work read like execution ownership when the package does not yet own the runtime path.

This audit is the repo-owned answer for that distinction.

## Surface maturity model

Every meaningful package or public/runtime surface should declare a current maturity target.

### `M0` Taxonomy

- defines vocabulary, descriptors, or selection semantics
- may shape scaffolding or planning language
- does not claim runtime ownership by itself

### `M1` Catalog + runtime truth

- publishes truthful descriptors, catalogs, runtime snapshots, or introspection
- may validate configuration or authored intent
- still does not claim managed execution or provisioning ownership

### `M2` Managed execution or provisioning

- owns one real execution, orchestration, or provisioning path
- publishes runtime state for that path
- documents boundaries, failure modes, and ownership clearly

### `M3` Operator automation

- supports real operator workflows, reconciliation, or automation loops
- exposes drill-down routes, lifecycle posture, and remediation-friendly runtime answers
- proves recovery, drift, or live-state handling beyond a happy path

### `M4` Adoption proof

- includes adoption-quality docs, samples/templates, and validation evidence
- is ready to be described as a shipped baseline for downstream teams
- keeps docs, runtime truth, tests, and packaging aligned

## Ownership modes

Maturity and ownership are related but different. Every surface should also describe who owns the real work:

- `taxonomy-only`: vocabulary or modeling only
- `application-managed`: consumer code or another runtime executes the work; Cephalon models or observes it
- `cephalon-managed`: Cephalon owns the execution or provisioning path
- `provider-managed`: a provider-specific Cephalon pack owns the execution or provisioning path

Intentional `taxonomy-only` and `application-managed` surfaces are valid. They just need to be labeled honestly.

## Current audit

| Surface | Primary role | Ownership mode | Current maturity | Next proof needed |
| --- | --- | --- | --- | --- |
| `Cephalon.Engine` app model, manifest, runtime introspection, policy composition | Core runtime contract and composition | `cephalon-managed` | `M4` | Keep compatibility, docs, and generated surfaces aligned as new packs land |
| `Cephalon.Engine.SourceGen` | Compiler-only module discovery descriptor generation | `cephalon-managed` | `M1` | Keep generated descriptor output aligned with `ModuleDiscoveryRegistry`, templates, scaffolding, package publishing, and deployment-mode inventory |
| `Cephalon.Cli`, `Cephalon.Scaffolding`, `Cephalon.TemplatePack`, `Cephalon.ReferenceDocs` | Adoption and packaging surface | `cephalon-managed` | `M4` | Maintain package/version/template/reference-doc alignment |
| `Cephalon.Behaviors` core runtime and durable execution | Behavior execution substrate | `cephalon-managed` | `M4` | Continue adoption polish and guardrail coverage rather than adding parallel execution stories |
| `Cephalon.Behaviors.Http` module-owned behavior REST projection | REST profile metadata plus explicit module-owned public REST activation, Cephalon-managed materialization, governance, and runtime catalogs | mixed: `application-managed` profile/publication activation plus `cephalon-managed` materialization/runtime catalog truth | `M2` | Keep profile metadata explicitly non-publishing and continue adoption/operator automation proof without inventing ambient REST publication |
| `Cephalon.Data` shared CDC runtime plus provider-native pumps | Shared and provider-native data execution truth, including `data-management` technology runtime surfaces for active CDC captures and CDC execution runtimes with sanitized metadata plus dedicated live-provider CDC integration evidence for MongoDB, SQL Server, PostgreSQL, MySQL, and Oracle | `cephalon-managed` plus `provider-managed` | `M3` | More package-level external adoption proof and operator docs per provider family |
| `Cephalon.Data.MySql.SciSharpReplication` | Optional SciSharp-backed MySQL binlog transport adapter for `Cephalon.Data.MySql` CDC captures | `provider-managed` | `M0` | Replace the current third-party non-public SciSharp adapter path with a published public upstream API or a first-party public binlog transport before claiming trim, Native AOT, or single-file readiness for this adapter |
| `Cephalon.Edge.KubernetesGateway` and `Cephalon.Edge.Traefik` | Provider-specific control-plane automation | `provider-managed` | `M3` | More adoption-quality samples and package publishing guidance outside the repo |
| `Cephalon.Eventing` core package | Config-driven channel descriptors, code-first subscription descriptors, descriptor-bearing executors through attributes or providers, code-first executor/middleware registration helpers, and direct execution middleware, staged publication, subscription runtime truth, public managed-execution binding catalog, abstraction-level subscription execution-readiness catalog, abstraction-level publication dispatcher, abstraction-level publication runtime-state catalog, abstraction-level dispatch-state/summary terminal-failure posture, abstraction-level dispatch-remediation dispatcher, `event-dispatch-remediations` command-readiness posture, `/engine/event-subscription-readiness`, `POST /engine/event-publications`, `/engine/event-publications/runtime*`, `/engine/event-dispatches/terminal-failures`, `POST /engine/event-dispatches/{outboxId}/commands/{operationId}`, `snapshot.EventSubscriptionExecutionReadiness`, `snapshot.EventPublicationStates`, `snapshot.EventDispatchStates`, stable subscription runtime metadata vocabulary, stable dispatch dead-letter boundary metadata vocabulary, stable remediation command-idempotency metadata vocabulary, opt-in direct in-process subscription execution, bounded process-local retry, bounded process-local duplicate-completed execution suppression, config-driven event-type-to-channel publication routing, and bounded process-local scheduled publication acceptance | mixed: `application-managed` descriptors/runtime reports plus `cephalon-managed` direct in-process execution, code-owned subscription descriptor/executor/attribute/provider/middleware registration, bounded process-local retry, bounded process-local idempotency, config-driven publication routing, bounded process-local scheduled publication acceptance, bounded publication action truth, publication runtime-state truth for direct execution, scheduled delayed handoff, routed/validated channel selection, or accepted outbox handoff, typed dispatch terminal-failure operator truth over reported dispatch state, and provider-neutral `retry-now`/`retry-later`/`skip`/`quarantine`/dispatch-store `dead-letter` remediation commands over supported dispatch stores | `M3` | Keep the in-process lane, attribute/provider descriptor discovery, code-first registration helpers, code-first middleware chain, retry loop, idempotency guard, route table, process-local scheduler, operator publication route, publication runtime-state catalog, dispatch terminal-failure posture, and remediation commands bounded to reported state plus mutable dispatch-store ownership; keep duplicate command ids fail-closed and keep `Engine:Messaging:Subscriptions` / `SubscriptionHandlers` out of runtime behavior; only claim generic broker/inbox/durable retry/downstream delivery ownership, inbound broker consumer loops, inbound acknowledgements, consumer offset checkpoints, provider delivery receipts, subscriber acknowledgement, destination commits, exactly-once delivery guarantees, broker topology materialization, provider partition ownership, subscription concurrency, prefetch, backpressure, provider concurrency, leases, distributed work sharing, handler ordering, local fan-out ordering, per-key ordering, partition ordering, causal ordering, replay ordering, cross-node ordering, durable process-manager state, saga persistence, saga correlation, timeout scheduling, compensation workflow, process-manager concurrency, process-manager recovery, durable/distributed scheduling, or broker dead-letter/replay ownership when a package truly owns those paths |

ENG-579 raises the eventing remediation command maturity bar inside that M3 row: duplicate command ids must fail closed from a reserved command journal row before dispatch-store mutation, not only from a completed command-result record. The `reserved` outcome is an in-doubt audit state and not an accepted dispatch-store mutation. ENG-581 keeps that audit state operator-ready by requiring summaries and runtime-surface metadata to identify the oldest retained reserved command id and timestamp.
ENG-580 keeps the aggregate side equally explicit: command summaries must expose reserved command count and `HasInDoubtCommands` directly, with matching technology-surface metadata, so operators do not have to scan records to notice a stuck reservation.
ENG-582 adds the detail side for the same state: command catalogs must expose retained in-doubt records directly through `GetInDoubt()` and `/engine/event-dispatch-remediation-commands/in-doubt`, with the same limit/paging posture as other list/filter reads.
ENG-583 adds stale in-doubt filtering: command catalogs must expose retained in-doubt records at or before an inclusive observed-UTC cutoff through `GetInDoubtBefore(...)` and `/engine/event-dispatch-remediation-commands/in-doubt?beforeUtc={beforeUtc}`.
ENG-584 adds the single-record stale read: command catalogs must expose the oldest retained in-doubt record through `GetOldestInDoubtBefore(...)` and `/engine/event-dispatch-remediation-commands/in-doubt/oldest?beforeUtc={beforeUtc}`, returning `404` rather than a synthetic empty command.
ENG-585 adds the aggregate stale read: command catalogs must expose retained in-doubt summaries at or before an inclusive observed-UTC cutoff through `GetInDoubtSummaryBefore(...)` and `/engine/event-dispatch-remediation-commands/in-doubt/summary?beforeUtc={beforeUtc}` without route paging.
ENG-586 adds retained filter summaries: command catalogs must expose `EventDispatchRemediationRuntimeSummary` for outbox, message, channel, operation, actor, correlation, reason, outcome, and dispatch-outcome filters without materializing list routes first.
ENG-587 adds retained filter latest reads: command catalogs must expose `EventDispatchRemediationRuntimeState` for the newest retained outbox, message, channel, operation, actor, correlation, reason, outcome, and dispatch-outcome match without materializing list routes first.
ENG-588 adds retained filter oldest reads: command catalogs must expose `EventDispatchRemediationRuntimeState` for the oldest retained outbox, message, channel, operation, actor, correlation, reason, outcome, and dispatch-outcome match without materializing list routes first.
ENG-589 adds retained filter retention reads: command catalogs must expose `EventDispatchRemediationRuntimeRetention` for the retained outbox, message, channel, operation, actor, correlation, reason, outcome, and dispatch-outcome posture without materializing list routes first.
ENG-592 adds the superiority-profile readback for the active command journal: `durable-remediation-command-audit` can claim durable cross-node audit only from the journal descriptor, while broker dead-letter ownership remains outside the current maturity claim. ENG-593 adds the command-journal replay cursor maturity proof for durable providers through `IEventDispatchRemediationCommandReplayCursorCatalog`, with EF reporting durable oldest-first replay over `(ObservedAtUtc, CommandId)`. ENG-594 makes that proof a separate profile dimension through `durable-command-journal-replay-cursor`, so command-journal replay maturity is claimed independently from durable audit. ENG-595 adds `broker-dead-letter-replay-ownership`, keeping broker DLQ/replay ownership `not-claimed` until a provider owns the broker path. ENG-596 adds `broker-topology-materialization-ownership`, keeping route/channel governance separate from broker exchange/queue/topic/partition provisioning or verification. ENG-597 adds `provider-partition-ownership`, keeping provider partition assignment, affinity, rebalancing, and ordering guarantees outside routing and topology claims until a provider owns them. ENG-598 adds `downstream-delivery-completion-ownership`, keeping destination delivery completion, provider delivery receipts, subscriber acknowledgement, destination commits, and exactly-once delivery guarantees outside publication accepted handoff, dispatch reports, routing, topology, partition, or provider-managed dispatch evidence until a provider owns them. ENG-599 adds `broker-inbound-consumption-ownership`, keeping inbound broker consumer loops, acknowledgements, leases, retry/poison handling, and offset checkpoints outside subscription descriptors, direct in-process execution, inbox duplicate suppression, hosted binding metadata, optional Wolverine binding evidence, and routing/topology/delivery claims until a provider owns them. ENG-600 adds `serialization-and-contract-versioning-ownership`, keeping serializer selection, wire-envelope schema, schema registry ownership, contract-version negotiation, upcaster pipelines, and compatibility validation outside event type/channel metadata, route evidence, subscription catalogs, and publication runtime claims until a provider or engine package owns them. ENG-609 adds the descriptor-backed event contract catalog under that boundary: code-first descriptors can make the dimension `partial` through `IEventContractCatalog`, `event-contracts`, and `eventing.contracts`, while wire serialization runtime, schema registry, and upcasters remain unclaimed. ENG-610 adds the descriptor-backed event serializer catalog under the same boundary: code-first serializer descriptors can make serializer selection catalog-backed through `IEventSerializerCatalog`, `event-serializers`, and `eventing.serializers`, while executable payload serialization, schema registry, upcasters, and provider compatibility validation remain unclaimed. ENG-611 adds the descriptor-backed event schema registry catalog under the same boundary: code-first registry descriptors can make schema registry evidence catalog-backed through `IEventSchemaRegistryCatalog`, `event-schema-registries`, and `eventing.schema-registries`, while executable schema lookup, payload serialization, upcasters, and provider compatibility validation remain unclaimed. ENG-612 adds the descriptor-backed event upcaster catalog under the same boundary: code-first upcaster descriptors can make version-transition evidence catalog-declared through `IEventUpcasterCatalog`, `event-upcasters`, and `eventing.upcasters`, while executable upcaster execution, payload serialization, schema lookup, and provider compatibility validation remain unclaimed. ENG-601 adds `tenant-and-correlation-context-ownership`, keeping tenant context propagation, correlation/causation propagation, baggage propagation, and message-header policy outside operator correlation metadata, diagnostic tags, route evidence, subscription catalogs, and publication runtime claims until a provider or engine package owns them. ENG-613 adds the descriptor-backed event context policy catalog under that boundary: code-first context policy descriptors can make context policy evidence `partial` through `IEventContextPolicyCatalog`, `event-context-policies`, and `eventing.context-policies`. ENG-614 makes that context proof executable on native publisher paths by enforcing required context headers before direct execution or outbox enqueue and by reporting publisher-enforced validation/direct in-process propagation metadata, while durable dispatch context propagation, provider/broker headers, consumer extraction, and cross-node context handoff remain unclaimed. ENG-602 adds `scheduled-and-delayed-delivery-ownership`, keeping durable scheduled delivery, provider delay queues, broker scheduled delivery, cross-node schedule coordination, and schedule recovery outside the bounded process-local scheduler claim until a provider or engine package owns them. ENG-603 adds `durable-retry-queue-ownership`, keeping durable retry queues, retry persistence, broker error queues, poison queue ownership, cross-node retry coordination, and retry leases outside bounded in-process retry, dispatch runtime reports, and provider-managed retry observations until a provider or engine package owns them.
ENG-604 adds `idempotency-ownership`, keeping broker deduplication, exactly-once delivery, durable inbox command ownership, generic inbox command ownership, provider idempotency semantics, and cross-node idempotency leases outside process-local or inbox-backed completed-execution duplicate suppression until a provider or engine package owns those paths.
ENG-605 adds `subscription-concurrency-ownership`, keeping per-subscription concurrency limits, parallel handler execution, consumer prefetch, backpressure, provider concurrency, consumer leases, and distributed work sharing outside declared subscriptions, direct in-process execution, code-first middleware, hosted binding metadata, and optional provider binding evidence until a provider or engine package owns those paths.
ENG-606 adds `subscription-ordering-ownership`, keeping handler ordering, local fan-out ordering, per-key ordering, partition ordering, causal ordering, replay ordering, and cross-node ordering outside declared subscriptions, direct in-process execution, code-first middleware, hosted binding metadata, and optional provider binding evidence until a provider or engine package owns those paths.
ENG-607 adds `process-manager-state-ownership`, keeping durable process-manager state, saga persistence, saga correlation, timeout scheduling, compensation workflow, process-manager concurrency, and recovery outside declared subscriptions, direct in-process execution, code-first middleware, choreography bridge handoff, outbox publication, hosted binding metadata, and optional provider binding evidence until a provider or engine package owns those paths.
ENG-608 adds `choreography-handoff-ownership`, keeping behavior choreography catalogs, live publication-state observations, explicit Eventing bridge activation, and outbox-backed handoff separate from durable saga/process-manager state ownership until a provider or engine package owns those state paths.
| `Cephalon.Eventing` remediation command-result catalog | Abstraction-level remediation command-result read model, duplicate command-id rejection, `IEventDispatchRemediationRuntimeCatalog`, `IEventDispatchRemediationCommandJournal`, `IEventDispatchRemediationCommandReplayCursorCatalog`, `EventDispatchRemediationCommandJournalDescriptor`, `EventDispatchRemediationCommandReplayCursor`, `EventDispatchRemediationRuntimeState`, `EventDispatchRemediationRuntimeSummary`, `EventDispatchRemediationRuntimeRetention`, `event-dispatch-remediation-commands` catalog and command-result entries, and `/engine/event-dispatch-remediation-commands*` summary/latest/retention/in-doubt/stale-in-doubt/stale-in-doubt-summary/oldest-in-doubt/observed-window plus observed-window-summary plus filter-summary plus filter-retention plus filter-latest plus filter-oldest plus reserved/in-doubt summary metadata including oldest reserved command id/timestamp plus metadata-advertised list/result/in-doubt/in-doubt-summary/oldest-in-doubt/outbox/filter-summary/filter-retention/filter-latest/filter-oldest route discovery, inclusive in-doubt cutoff policy, inclusive observed-UTC window policy, newest-first `limit` on list/filter drill-downs, and provider-owned oldest-first command-journal replay cursor reads for accepted/rejected/reserved command results by command id, outbox id, message id, channel id, operation id, actor id, correlation id, reason, dispatch outcome, and command outcome | `cephalon-managed` process-local fallback plus provider-neutral journal seam; durable cross-node history and command-journal replay cursor reads when a provider such as `Cephalon.Data.EntityFramework` registers the active journal | `M3` | Keep command-result history truthful, duplicate-safe, latest-readable, summary-readable with reserved/in-doubt age anchor, dropped-command, and incomplete-warning evidence when bounded, retention/truncation-readable for process-local history, unbounded-retention-readback for durable journals, command-journal replay-cursor-readable for durable provider journals, in-doubt-queryable, stale-in-doubt-queryable by cutoff, stale-in-doubt-summary-readable as a direct aggregate, oldest-in-doubt-queryable as a direct single-record read, observation-window-queryable, observation-window-summary-readable, filter-summary-readable, filter-retention-readable, filter-latest-readable, and filter-oldest-readable by outbox/message/channel/operation/actor/correlation/reason/outcome/dispatch-outcome, catalog-discoverable before history exists, list/result/in-doubt/in-doubt-summary/oldest-in-doubt/outbox/filter-summary/filter-retention/filter-latest/filter-oldest-route-discoverable, message/channel/operation/actor/correlation/reason/dispatch-outcome/command-outcome-queryable with introspectable read-side limits/window/journal/replay-cursor policy, and separate from broker dead-letter/replay ownership |
| `Cephalon.Eventing.Wolverine` | Optional Wolverine-managed staged dispatch, bounded managed-dispatch retry with terminal dispatch-store semantics, publish-exception terminal proof, first-class terminal dispatch operator posture through the shared eventing state/summary surfaces, subscription execution, bounded managed-subscription retry, and terminal exhausted-attempt failure baseline | `provider-managed` | `M3` | Broaden inbound-consumption, broker-specific dead-letter, and operator-automation proof only when the runtime truly owns those paths |
| `Cephalon.Eventing` event contract catalog | Code-first `EventContractDescriptor` registrations through `EventingOptions.Contracts` and `IEventContractContributor`, `IEventContractCatalog` lookup by contract id/event type/event type version, `event-contracts` technology surface, `eventing.contracts` capability, and `serialization-and-contract-versioning-ownership` descriptor-backed partial evidence | `cephalon-managed` descriptor catalog over host/module-authored contract metadata | `M2` | Keep contract metadata code-owned and Wolverine-free, keep duplicate contract id overrides deterministic, expose version/content-type/serializer/envelope/compatibility metadata to operators, and do not claim wire serialization runtime, executable schema lookup, upcaster pipelines, or provider-owned compatibility validation until a provider or engine package owns those paths |
| `Cephalon.Eventing` event serializer catalog | Code-first `EventSerializerDescriptor` registrations through `EventingOptions.Serializers` and `IEventSerializerContributor`, `IEventSerializerCatalog` lookup by serializer id/content type/contract serializer id, `event-serializers` technology surface, `eventing.serializers` capability, and `serialization-and-contract-versioning-ownership` serializer-catalog-backed partial evidence | `cephalon-managed` descriptor catalog over host/module-authored serializer metadata | `M2` | Keep serializer availability metadata code-owned and Wolverine-free, keep duplicate serializer id overrides deterministic, expose content-type/format/runtime-kind/read/write/schema-registry-required metadata to operators, and do not claim executable payload serialization, executable schema lookup, upcaster pipelines, or provider-owned compatibility validation until a provider or engine package owns those paths |
| `Cephalon.Eventing` event schema registry catalog | Code-first `EventSchemaRegistryDescriptor` registrations through `EventingOptions.SchemaRegistries` and `IEventSchemaRegistryContributor`, `IEventSchemaRegistryCatalog` lookup by registry id/provider/format/serializer schema registry id, `event-schema-registries` technology surface, `eventing.schema-registries` capability, and `serialization-and-contract-versioning-ownership` schema-registry-catalog-backed partial evidence | `cephalon-managed` descriptor catalog over host/module-authored schema registry metadata | `M2` | Keep schema registry availability metadata code-owned and Wolverine-free, keep duplicate registry id overrides deterministic, expose provider/endpoint/runtime/read/write/compatibility/format metadata to operators, and do not claim executable schema lookup, executable payload serialization, upcaster pipelines, or provider-owned compatibility validation until a provider or engine package owns those paths |
| `Cephalon.Eventing` event upcaster catalog | Code-first `EventUpcasterDescriptor` registrations through `EventingOptions.Upcasters` and `IEventUpcasterContributor`, `IEventUpcasterCatalog` lookup by upcaster id/event type/source version/target version, `event-upcasters` technology surface, `eventing.upcasters` capability, and `serialization-and-contract-versioning-ownership` upcaster-catalog-declared partial evidence | `cephalon-managed` descriptor catalog over host/module-authored version-transition metadata | `M2` | Keep version-transition metadata code-owned and Wolverine-free, keep duplicate upcaster id overrides deterministic, expose source/target contract resolution and transition metadata to operators, and do not claim executable upcaster execution, executable payload serialization, executable schema lookup, or provider-owned compatibility validation until a provider or engine package owns those paths |
| `Cephalon.Eventing` event context policy catalog and publisher enforcement | Code-first `EventContextPolicyDescriptor` registrations through `EventingOptions.ContextPolicies` and `IEventContextPolicyContributor`, `IEventContextPolicyCatalog` lookup by policy id/header name, stable `EventContextHeaderNames`, publisher-enforced required-header validation when `ValidatesMessageHeaders = true`, `event-context-policies` technology surface, `eventing.context-policies` capability, and `tenant-and-correlation-context-ownership` partial evidence | `cephalon-managed` descriptor catalog and native publisher enforcement over host/module-authored context policy metadata | `M2` | Keep tenant/correlation/causation/baggage/header policy metadata code-owned and Wolverine-free, keep duplicate policy id overrides deterministic, expose header coverage, validation failure, and direct in-process forwarding evidence to operators, and do not claim durable dispatch context propagation, provider/broker headers, consumer extraction, or cross-node context handoff until a provider or engine package owns those paths |
| `Cephalon.Agentics` | Tool descriptors, managed tool dispatch, abstraction-level run-state catalog, bounded operator action route, bounded process-local executor retry posture, bounded process-local duplicate-completed suppression, approval-required and terminal-failure operator filters, `/engine/agent-tool-runs`, `/engine/agent-tool-runs/retry-pending`, `/engine/agent-tool-runs/idempotency-duplicates`, `/engine/agent-tool-runs/approval-required`, `/engine/agent-tool-runs/terminal-failures`, `POST /engine/agent-tools/{toolId}/runs`, `snapshot.AgentToolRuns`, and agent-workload runtime surface | mixed: `application-managed` descriptors plus `cephalon-managed` dispatcher/run-state/operator-action/bounded-process-local-retry/bounded-process-local-idempotency/read-filter baseline | `M3` | Broader agent operator workflows, durable approval workflows, durable retry queues, durable inboxes, memory persistence, dead-letter systems, cross-node exactly-once delivery, distributed scheduling, and provider-specific AI orchestration only after a package truly owns those paths |
| `Cephalon.Retrieval` | Knowledge collection descriptors plus managed lexical indexing, query execution, freshness state, abstraction-level index-state catalog, abstraction-level bounded query command seam, manual reindex command seam, opt-in background reindex scheduler, `/engine/knowledge-indexes`, `POST /engine/knowledge-indexes/{collectionId}/queries`, `POST /engine/knowledge-indexes/{collectionId}/reindex`, `snapshot.KnowledgeIndexes`, and the public `RetrievalDiagnostics` OpenTelemetry adapter declared against `CephalonActivitySources.Retrieval` and `CephalonMeters.Retrieval` (one `retrieval.knowledge.index` activity per `IKnowledgeIndexer.IndexAsync` and one `retrieval.knowledge.query` activity per `IKnowledgeQueryEngine.QueryAsync` with stable `cephalon.retrieval.*` tags routed through the registered `RedactionPipeline`, plus `cephalon.retrieval.index_runs` and `cephalon.retrieval.queries` counters; the query span never carries the raw query text — only its character length is emitted) | mixed: `application-managed` source documents plus `cephalon-managed` index/query/operator-read/operator-query/manual-remediation/background-automation/OTel-emission baseline | `M3` | Provider-specific vector/search engines, durable or distributed indexes, and distributed scheduler coordination only after a package truly owns those paths |
| `Cephalon.MultiTenancy` core package | Narrow tenant-resolution plus explicit governance-boundary runtime truth | mixed: `cephalon-managed` tenant-resolution core plus boundary entries for companion-owned or planned workflows | `M2` | Keep the base package focused on resolution while companion packages own concrete governance workflows |
| `Cephalon.MultiTenancy.Governance` | Tenant membership, invitation, delivery dispatch/retry/status reconciliation, delivery-status observation storage, tenant-administration, domain-ownership verification/proof collection/polling, governance action workflows, optional ASP.NET Core governance endpoints with bounded delivery-status observation reads, filtered rollup summaries, attention-category drill-downs, provider-message drill-downs, remediation-action filters, and remediation hints, optional HTTP/SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph senders, optional Microsoft Graph Azure Identity token provider, optional ASP.NET Core SendGrid callback translation plus signed-webhook verification, process-local replay protection, and observation-store-backed event-id idempotency, optional ASP.NET Core Mailgun callback translation plus HMAC signed-webhook verification, bounded process-local replay-token protection, and observation-store-backed event-id idempotency, and optional ASP.NET Core Amazon SES over SNS callback translation plus SNS signature verification, bounded process-local SNS replay protection, observation-store-backed SNS message-id idempotency, verified SNS subscription confirmation, and verified SNS unsubscribe-confirmation observation | mixed: `cephalon-managed` governance core, ASP.NET Core adapter endpoints, filtered observation rollup summaries, attention-category drill-downs, provider-message drill-down filters, remediation-action filters, remediation hints over matched normalized observations, provider-neutral callback signatures, bounded process-local replay protection, local durable stores, HTTP/SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph sender companions, Microsoft Graph Azure Identity token-provider companion, SendGrid callback translation/signature verification/process-local replay/event-id-idempotency protection, Mailgun callback translation/signature verification/process-local replay-token/event-id-idempotency protection, and Amazon SES over SNS callback translation, opt-in SNS signature verification, bounded process-local SNS replay protection, observation-store-backed SNS message-id idempotency, opt-in verified SNS subscription confirmation, and opt-in verified SNS unsubscribe-confirmation observation; external provider delivery/status, distributed remediation execution, Microsoft Entra tenant governance, AWS account/identity governance, SNS topic/subscription creation, automatic resubscribe/restore, and subscription lifecycle governance remain `provider-managed` | `M2` | Actual DNS proof publication, provider-backed proof publication or mutation, remediation execution beyond state transitions, distributed or provider-backed membership/invitation/domain/action-store backends, additional provider-specific email API senders beyond the shipped SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph set, SMS/chat/CRM/identity-provider invitation senders, Microsoft Entra app registration/permission consent/mailbox access policy, AWS account/IAM/identity verification, DKIM/SPF/DMARC, SES sandbox/configuration-set event destination setup, SNS topic/subscription creation, automatic resubscribe/restore, subscription lifecycle governance, distributed retry queues, cross-node retry leases, provider-specific or distributed callback inboxes, cross-node callback replay protection, distributed event-id ledgers, provider-specific callback payload translation beyond shipped SendGrid/Mailgun/Amazon SES translators, provider-specific callback signature verification beyond shipped SendGrid/Mailgun/Amazon SNS hardening, provider polling, identity-provider synchronization, public onboarding, and tenant-admin UI/backoffice flows only when the package truly owns those paths |
| `Cephalon.Diagnostics` | Engine-level OpenTelemetry semantic-convention adapter publishing canonical `ActivitySource` and `Meter` names (`Cephalon.Engine` / `Cephalon.AspNetCore` / `Cephalon.Worker` / `Cephalon.Eventing` / `Cephalon.MultiTenancy.Governance` / `Cephalon.Agentics` / `Cephalon.Retrieval`) plus stable `cephalon.*` attribute keys (`cephalon.module.id`, `cephalon.behavior.id`, `cephalon.cell.id`, `cephalon.app.blueprint`, `cephalon.tenant.id`); `Cephalon.Engine` consumes the canonical names for `engine.build`, `module.{phase}`, and `runtime.*` spans / metrics; `Cephalon.AspNetCore` declares its diagnostics convention against `CephalonActivitySources.AspNetCore` and exposes `/engine/diagnostics-conventions` projecting the canonical name set as a `DiagnosticsConventionsSurface` record; `Cephalon.Worker` declares `WorkerDiagnostics.ActivitySource` against `CephalonActivitySources.Worker` and emits `worker.lifecycle.start` / `worker.lifecycle.stop` spans around hosted-service lifecycle; `Cephalon.Agentics` declares the public `AgenticsDiagnostics` adapter against `CephalonActivitySources.Agentics` and `CephalonMeters.Agentics`, emits one `agentics.tool.dispatch` activity per `IAgentToolDispatcher.ExecuteAsync` call (with stable Cephalon-prefix tags for dispatcher / tool / run / actor / correlation / attempt / outcome) plus a `cephalon.agentics.tool_executions` counter, all routed through the registered `RedactionPipeline`; `Cephalon.Eventing` declares the public `EventingDiagnostics` adapter against `CephalonActivitySources.Eventing` and `CephalonMeters.Eventing`, emits one `eventing.publication.dispatch` activity per `IEventPublisher.PublishAsync` call (with stable Cephalon-prefix tags for publisher / publication / channel / event-type / outcome / matched-subscription-count) plus a `cephalon.eventing.publications` counter, all routed through the registered `RedactionPipeline`; `Cephalon.MultiTenancy.Governance` declares the public `GovernanceDiagnostics` adapter against `CephalonActivitySources.MultiTenancyGovernance` and `CephalonMeters.MultiTenancyGovernance`, emits one `multitenancy.governance.invitation.delivery.dispatch` activity per invitation-delivery dispatch attempt (with stable Cephalon-prefix tags for tenant / invitation / delivery channel / sender / outcome) plus a `cephalon.multitenancy_governance.invitation_dispatches` counter, all routed through the registered `RedactionPipeline`; `Cephalon.Observability.OpenTelemetry` subscribes to all seven canonical activity sources and meters as the OTLP companion pack's real subscription contract (`.Retrieval` now emits one `retrieval.knowledge.index` activity per `IKnowledgeIndexer.IndexAsync` and one `retrieval.knowledge.query` activity per `IKnowledgeQueryEngine.QueryAsync` plus the matching `cephalon.retrieval.index_runs` / `cephalon.retrieval.queries` counters via the `RetrievalDiagnostics` adapter shipped through `ENG-402`; `.Eventing` emits one `eventing.publication.dispatch` activity per `IEventPublisher.PublishAsync` shipped through `ENG-371`; `.MultiTenancyGovernance` emits one `multitenancy.governance.invitation.delivery.dispatch` activity per invitation-delivery dispatch attempt shipped through `ENG-379`); ships the `IRedactionFilter` + `RedactionContext` + `RedactionPipeline` redaction surface with `KeyMatchRedactionFilter` / `RegexRedactionFilter` starters and `IServiceCollection.AddRedactionPipeline()` registration; M1 wired at eight real engine emission sites where attribute values route through the registered `RedactionPipeline` before exporter dispatch: (1) the `Cephalon.AspNetCore` HTTP request/response logging middleware (`HttpRequestResponseLoggingMiddleware`, `ENG-365`), (2) the `Cephalon.Engine` runtime module-phase activity tags (`EngineRuntime` `runtime.{phase}` + `module.{phase}` spans during initialize/start/stop, `ENG-366`), (3) the `Cephalon.Eventing.Wolverine` in-process dispatch activity tags (`WolverineEventDispatchHostedService` `wolverine.dispatch` spans including `cephalon.tenant_id` / `cephalon.correlation_id` / `cephalon.message_id`, `ENG-374`), (4) the `Cephalon.Agentics` in-process tool-dispatch activity tags (`AgentToolDispatcher` `agentics.tool.dispatch` spans, `ENG-401`), (5) the `Cephalon.Retrieval` knowledge-index and knowledge-query activity tags (`KnowledgeIndexer` + `KnowledgeQueryEngine` `retrieval.knowledge.index` + `retrieval.knowledge.query` spans, `ENG-402`), (6) the `Cephalon.Worker` hosted-service lifecycle activity tags (`RuntimeHostedService` `worker.lifecycle.start` + `worker.lifecycle.stop` spans with `cephalon.lifecycle.phase` / `cephalon.blueprint` / `cephalon.module.count` tags, `ENG-412`), (7) the `Cephalon.Eventing` in-process publication-dispatch activity tags (`InProcessEventPublisher` `eventing.publication.dispatch` spans, `ENG-371`), and (8) the `Cephalon.MultiTenancy.Governance` invitation-delivery dispatch activity tags (`TenantInvitationDeliveryDispatcher` `multitenancy.governance.invitation.delivery.dispatch` spans, `ENG-379`); the canonical recipe is documented in `docs/components/diagnostics.md` *Redaction quick start* and adopted by all five samples | `cephalon-managed` | `M4` | Adoption-quality; `LoggerMessage` source-generated factories aligned with the per-package diagnostic-id range discipline, `EngineBuilder` build-time activity tag redaction, and the per-companion-pack OTel adapter emission baseline for `Cephalon.Data` CDC remain follow-up |
| `Cephalon.Analyzers` | Curated consumer-facing analyzer meta-package bundling `Microsoft.CodeAnalysis.BannedApiAnalyzers`, `Microsoft.CodeAnalysis.PublicApiAnalyzers`, `Roslynator.Analyzers`, `Meziantou.Analyzer`, and `Microsoft.VisualStudio.Threading.Analyzers`; ships curated `BannedSymbols.txt` (auto-wired through `buildTransitive/Cephalon.Analyzers.props`) and curated `cephalon-analyzers.editorconfig`; `Cephalon.Abstractions` consumes the meta-package as the proof-of-concept | `cephalon-managed` | `M1` | Promote to `M2` when the meta-package is the documented adoption path in `getting-started.md` and the template-pack starter projects reference it by default |
| Engine-level resilience runtime (descriptors in `Cephalon.Abstractions/Resilience/*` plus `Cephalon.Resilience/Resilience/*` policy resolver, circuit-breaker state registry, exception classifier, and execution context keys consumed by `Cephalon.Behaviors/Resilience/*` for behavior dispatch enforcement, all over `Microsoft.Extensions.Resilience` / Polly v8) | Declarative retry / circuit-breaker / bulkhead / timeout / hedging / rate-limit descriptors plus engine-managed policy/circuit-breaker-state/exception-classifier runtime in `Cephalon.Resilience`, plus the behavior-coupled idempotency resolver, runtime catalog, and execution middleware that remain in `Cephalon.Behaviors` because they implement the internal behavior dispatch contract and consume registered `BehaviorImplementationDescriptor` metadata | mixed: `application-managed` resilience descriptors (selected at app-profile level) plus `cephalon-managed` execution runtime split across `Cephalon.Resilience` (policy resolver, circuit-breaker state, exception classifier, execution context keys) and `Cephalon.Behaviors` (idempotency resolver, runtime catalog, dispatch middleware) over the in-box `Microsoft.Extensions.Resilience` integration | `M2` | Promote to `M3` when an explicit operator surface (catalog routes, snapshot keys) lands and `Cephalon.Resilience` owns that operator-facing runtime |
| `Cephalon.Resilience` policy resolver, circuit-breaker state registry, default exception classifier, and shared execution context keys | Engine-managed resilience runtime that consumes `Cephalon.Abstractions.Resilience` plus `Cephalon.Abstractions.AppModel` resilience descriptors over `Microsoft.Extensions.Resilience` (Polly v8) | mixed: `application-managed` descriptors plus `cephalon-managed` runtime | `M2` | Promote to `M3` when an operator-facing catalog route plus snapshot key for resolved resilience policies lands in this package |
| `Cephalon.Data` non-relational provider packs (`Cephalon.Data.Redis`, `Cephalon.Data.Neo4j`, `Cephalon.Data.Cassandra`, `Cephalon.Data.ClickHouse`, `Cephalon.Data.Elasticsearch`, `Cephalon.Data.OpenSearch`, `Cephalon.Data.Qdrant`, `Cephalon.Data.Nats`, `Cephalon.Data.Debezium`) | Provider-specific store descriptors and persistence surfaces over Redis, Neo4j, Cassandra, ClickHouse, Elasticsearch, OpenSearch, Qdrant, and NATS now project provider outbox/inbox truth through `IOutboxCatalog`, `IInboxCatalog`, and the `outbox-producers` / `inbox-stores` runtime technology surfaces; dispatch-store bridges are shipped where the provider exposes `IEventDispatchStore`. Redis has opt-in live-provider evidence for data persistence plus Redis Streams event sourcing; Cassandra, ClickHouse, Elasticsearch, NATS, Neo4j, OpenSearch, and Qdrant now have opt-in live-provider evidence for data outbox/inbox runtime behavior through either pre-provisioned services or disposable Testcontainers-backed runtimes, with ClickHouse deliberately preserving its unsupported dispatch-store boundary. URI-based providers redact inline URI user-info and query credentials in capability metadata. `Cephalon.Data.Debezium` contributes external managed-connector CDC descriptors and execution-runtime truth through `ICdcCaptureCatalog`, `ICdcCaptureExecutionRuntimeCatalog`, and the shared `data-management` CDC technology surfaces while keeping connector ownership outside the engine | `provider-managed` | `M1` family-level | Per-pack managed-execution proof (read/write/projection lifecycle and provider-owned provisioning for store packs; managed connector orchestration for Debezium) before promoting any individual pack from `M1` provider-visible runtime truth to `M2` managed execution. `Cephalon.Data.MongoDB` remains covered by its separate `M2` provider row because it owns the provider-native change-stream runner and default-running disposable data-provider proof in addition to the shared outbox/inbox surface; relational providers (`SqlServer`, `Postgres`, `MySql`, `Oracle`) remain covered by the existing `Cephalon.Data` shared CDC runtime row, including the aggregate `data.relational-store` capability family and shared CDC technology-surface projection |
| `Cephalon.EventSourcing` core plus the ten provider packs (`Cephalon.EventSourcing.EntityFramework`, `Cephalon.EventSourcing.MongoDB`, `Cephalon.EventSourcing.Redis`, `Cephalon.EventSourcing.Neo4j`, `Cephalon.EventSourcing.Cassandra`, `Cephalon.EventSourcing.ClickHouse`, `Cephalon.EventSourcing.Elasticsearch`, `Cephalon.EventSourcing.OpenSearch`, `Cephalon.EventSourcing.Qdrant`, `Cephalon.EventSourcing.Nats`) | Event-sourced aggregate contracts, stable event-type registry, provider-specific append/read event stores, merged `IEventStoreCatalog`, and sanitized `event-sourcing` runtime-surface entries for all ten provider stores. Redis now has opt-in live-provider evidence in `tests/Cephalon.Tests.ProviderIntegration` for stream append/read/version/concurrency behavior against a real Redis runtime | `application-managed` aggregate logic plus `provider-managed` per-pack append/read store contracts and catalog/runtime-surface contributions | `M1` family-level | Keep this family at `M1` until Cephalon owns managed event-store execution beyond append/read, such as projection rebuild orchestration, snapshot lifecycle, retention, archival, or background replay workers. The current proof is now real provider catalog/runtime truth for all ten stores, with secret redaction for connection strings, passwords, URI user-info, and provider credential values; promote an individual provider to `M2` only when that provider owns managed execution beyond the current append/read + catalog/runtime-truth baseline |
| `Cephalon.Audit` | Host-agnostic audit-recording baseline: `IAuditRecorder` service, `IAuditActorAccessor` ambient actor contract, default in-memory `IAuditWriter` baseline (configurable through `Engine:Audit:EnableInMemoryWriter`), `IAuditStoreCatalog` projection through `/engine/audit-stores` and `snapshot.AuditStores`, additive `IAuditHistoryReader` and `IAuditHistoryExporter` contracts that durable provider packs may implement, stable diagnostics conventions for audit-entry write success/failure | mixed: `cephalon-managed` in-memory writer baseline plus catalog projection plus diagnostics; `application-managed` consumer-supplied actor accessors and durable storage opinion | `M1` | The `/engine/audit-*` routes are `M1` catalog projections of `IAuditStoreCatalog` / `IAuditHistoryReader` / `IAuditHistoryExporter` truth, not managed runtime ownership. Durable provider-pack proof beyond `Cephalon.Audit.EntityFramework`, distributed audit ledgers, query-server adapters, and any cross-host actor governance only after a package owns those paths |
| `Cephalon.Audit.EntityFramework` | Durable Entity Framework audit history: `Engine:Audit:History` configuration plus `Engine:Databases` role contract, queryable `IAuditHistoryReader` implementation, bounded `IAuditHistoryExporter` NDJSON path, retention guidance through `Engine:Audit:History:Retention` | `provider-managed` | `M1` | Distributed audit-history backends, replication-aware reader/exporter coordination, and cross-host retention enforcement only after the runtime owns those paths |
| `Cephalon.Identity` | Host-agnostic identity and authorization baseline: metadata-driven `IAuthorizationEvaluator` default (configurable through `Engine:Identity:EnableDefaultEvaluator`), `IAuthorizationPolicyCatalog` projection through `/engine/authorization-policies` and `snapshot.AuthorizationPolicies`, `identity-authorization` technology runtime surface (configurable through `Engine:Identity:EnableRuntimeSurface`), dedicated diagnostics convention | mixed: `cephalon-managed` default metadata-driven evaluator, runtime surface, catalog projection, and diagnostics conventions; `application-managed` identity scheme, principal flow, and any product-specific policy engine | `M1` | The `/engine/authorization-policies` route is the `M1` catalog projection of `IAuthorizationPolicyCatalog` truth; `identity-authorization` is the matching runtime surface. Broader runtime authorization-decision proof, durable policy stores, distributed evaluation caches, and provider-specific identity-provider sync only after a package owns those paths |
| `Cephalon.Identity.AspNetCore` | ASP.NET Core integration baseline: minimal-API and controller-action policy mapping over the host-agnostic evaluator, optional projection of authenticated `ClaimsPrincipal` into the ambient `IAuditActorAccessor` contract when `Cephalon.Audit` is active | `application-managed` | `M1` | Deeper host/runtime truth such as endpoint-policy catalogs, scheme-aware diagnostics, and per-request decision projection only after the adapter package owns those paths; do not push ASP.NET Core concerns back into `Cephalon.Abstractions` |
| Multi-tenancy invitation delivery sender family (`Cephalon.MultiTenancy.Governance.HttpDelivery`, `.SmtpDelivery`, `.SendGridDelivery`, `.MailgunDelivery`, `.AmazonSesDelivery`, `.MicrosoftGraphDelivery`, plus `.SendGridDelivery.AspNetCore`, `.MailgunDelivery.AspNetCore`, `.AmazonSesDelivery.AspNetCore`, `.MicrosoftGraphDelivery.AzureIdentity`) | Provider-neutral host-agnostic invitation-sender contract plus shipped HTTP webhook / SMTP relay / SaaS API / Microsoft Graph senders; sanitized outbound provider runtime surfaces for HTTP, SMTP, SendGrid, Mailgun, Amazon SES, Microsoft Graph, and Microsoft Graph Azure Identity (`tenant-invitation-delivery-http`, `tenant-invitation-delivery-smtp`, `tenant-invitation-delivery-sendgrid`, `tenant-invitation-delivery-mailgun`, `tenant-invitation-delivery-amazon-ses`, `tenant-invitation-delivery-microsoft-graph`, `tenant-invitation-delivery-microsoft-graph-azure-identity`); optional ASP.NET Core callback translation, signed-webhook verification, bounded process-local replay protection, and observation-store-backed event-id idempotency for SendGrid / Mailgun / Amazon SES | `cephalon-managed` shared dispatch contract, callback translation, signed-webhook verification, replay protection, and idempotency stores; `provider-managed` outbound sender/token-provider packs project active provider wiring, accepted status contracts, channel posture, and credential-configured flags without exposing API keys, access tokens, passwords, signing secrets, header values, query secrets, or credential identifiers | `M2` family-wide; seven outbound sender/token-provider packs now have sanitized provider runtime surfaces; promote a single sender above the family floor only when that sender owns adoption-quality proof beyond the shared dispatch + runtime-truth + callback contract that justifies independent maturity language | All ten packs share the `M2` label by design: each owns managed dispatch or callback/token-provider behavior over a different upstream API, and the seven outbound packs now project runtime truth through `tenant-invitation-delivery-*` technology surfaces. Per-sender promotion requires (a) provider-specific adoption proof unique to one sender (deeper reconciler, distributed retry queue, identity-provider sync, etc.), and (b) matching matrix row + component doc + planning record updates in one slice. Until then, treat the family floor as `M2` and the per-sender component docs as the per-sender adoption truth |
| `Cephalon.Observability` core plus exporter and dependency-health companion family | Shared observability options, startup summary projection, telemetry-export configuration intent, sanitized observability runtime surfaces for the shared telemetry guidance plus optional OpenTelemetry / Serilog / Kubernetes / AWS / GCP / Azure Monitor / Alibaba Cloud / Huawei Cloud / Oracle Cloud / DigitalOcean / OpenShift / Tanzu / Grafana Cloud / New Relic configuration packs, optional dependency-health probe core plus per-provider probe packs (Cassandra, ClickHouse, Consul, Elasticsearch, Http, Kafka, Memcached, MongoDB, MQTT, MySQL, NATS, Neo4j, OpenSearch, Oracle, PostgreSQL, RabbitMQ, Redis, SqlServer) | `cephalon-managed` for the observability core, configuration packs, sanitized runtime-truth projection, and dependency-health probe infrastructure baseline; `provider-managed` for per-provider dependency-health probe packs that own a concrete connectivity probe and publish cached runtime truth through the shared dependency-health contract | `Cephalon.Observability` core at `M2`; the fourteen cloud / exporter configuration packs and `Cephalon.Observability.DependencyHealth.Core` at `M1`; the eighteen per-provider dependency-health probe packs at `M2` | Genuine telemetry collection happens in the host runtime's OpenTelemetry or logging provider stack; the configuration packs now project active-pack runtime truth but still only bind/options-wire telemetry rather than owning collection. Promote a configuration pack above `M1` only when it owns runtime collection or a managed exporter loop. Promote a dependency-health probe pack above `M2` only when it adds operator automation, live reconciliation, or adoption evidence beyond the current managed probe loop, timeout/failure posture, diagnostics event ids, and `/engine/dependencies` / readiness integration. |

## Immediate planning consequences

- stop expanding descriptor-first surfaces inside mixed-maturity families unless the work is explicitly labeled `M0` or `M1`
- do not describe `M0` or `M1` packages as if they already own execution, orchestration, or provisioning
- treat the `Cephalon.Eventing` in-process direct subscription lane plus attribute/provider descriptor discovery, code-first executor/middleware registration helpers, code-first execution middleware, bounded process-local retry, bounded process-local idempotency, config-driven publication routing, bounded process-local publication scheduling, `POST /engine/event-publications`, `/engine/event-publications/runtime*`, and first-class terminal dispatch-state operator posture, the optional `Cephalon.Eventing.Wolverine` provider-managed dispatch/subscription lane plus bounded terminal dispatch and subscription retry proof, the `Cephalon.Agentics` dispatcher/run-state lane plus direct operator read/action seams, bounded process-local executor retry posture, bounded process-local duplicate-completed suppression, approval-required filter, and terminal-failure filter, and the `Cephalon.Retrieval` lexical index/query/freshness lane plus direct operator read, bounded query action, manual reindex, and opt-in background reindex seams as current managed vertical proofs instead of widening descriptor breadth before ownership is real
- treat the native `Cephalon.Eventing` retry proof as fixed/exponential process-local backoff plus deterministic jitter metadata over the direct in-process lane; `durable-retry-queue-ownership` must stay `not-claimed` for durable retry-queue, retry-persistence, broker error-policy, poison queue, cross-node lease, or distributed scheduling ownership until a package actually owns those paths
- treat the native `Cephalon.Eventing` idempotency proof as completed-execution duplicate suppression over the direct in-process lane; `idempotency-ownership` must stay `partial` only when that suppression is enabled and must keep broker deduplication, exactly-once delivery, durable inbox command ownership, generic inbox command ownership, provider idempotency semantics, and cross-node idempotency leases `not-claimed` until a package actually owns those paths
- treat `Engine:Messaging:Channels` as configuration-owned channel descriptor discovery, while `Engine:Messaging:Subscriptions` and `Engine:Messaging:SubscriptionHandlers` stay unsupported for runtime behavior; subscriptions, executors, descriptor providers, and middleware are code-owned for performance and type safety, with helper extensions available only to reduce DI ceremony
- keep `Cephalon.MultiTenancy` intentionally thin in the base package; the governance family now owns the concrete governance workflows plus optional ASP.NET Core routes, HTTP/SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph sender companions, Microsoft Graph Azure Identity token-provider companion, provider-neutral callback signatures/replay protection, filtered observation rollup summaries, attention-category drill-downs, provider-message drill-down filters, remediation-action filters, remediation hints over matched normalized observations, optional SendGrid callback translation plus signed-webhook verification, bounded process-local signed-callback replay protection, observation-store-backed SendGrid event-id idempotency, optional Mailgun callback translation plus HMAC signed-webhook verification, bounded process-local replay-token protection, observation-store-backed Mailgun event-id idempotency, optional Amazon SES over SNS callback translation, opt-in SNS signature verification, bounded process-local SNS replay protection, observation-store-backed Amazon SNS message-id idempotency, opt-in verified SNS subscription confirmation, and opt-in verified SNS unsubscribe-confirmation observation. Actual DNS proof publication, provider-backed proof publication or mutation, remediation execution beyond state transitions, distributed or provider-backed membership/invitation/domain/action stores, additional provider-specific email API senders beyond the shipped SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph set, SMS/chat/CRM/identity-provider senders, Microsoft Entra app registration/permission consent/mailbox access policy, AWS account/IAM/identity verification, DKIM/SPF/DMARC, SES sandbox/configuration-set event destination setup, SNS topic/subscription creation, automatic resubscribe/restore, subscription lifecycle governance, distributed retry queues, cross-node retry leases, provider-specific or distributed callback inboxes, cross-node callback replay protection, distributed event-id ledgers, provider-specific callback payload translation beyond shipped SendGrid/Mailgun/Amazon SES translators, provider-specific callback signature verification beyond shipped SendGrid/Mailgun/Amazon SNS hardening, provider polling, identity-provider synchronization, public onboarding, and tenant-admin UI/backoffice flows remain explicitly outside the current claim
- treat the ASP.NET Core invitation delivery dispatch endpoint as a bounded `M2` action seam over the host-agnostic dispatcher, and treat the delivery-status observation read endpoint plus filtered rollup summaries, attention-category drill-downs, provider-message drill-down filters, remediation-action filters, and remediation hints as a bounded `M2` operator/audit projection over normalized store records; do not promote either endpoint into provider-specific sender ownership, distributed retry queues, callback-inbox, provider-polling, distributed remediation execution, distributed-replay, or exactly-once language until a package owns those paths
- use `Cephalon.Behaviors`, `Cephalon.Behaviors.Http`, `Cephalon.Data`, and the shipped edge provider packs as the current examples of truthful runtime ownership

## Planned next sequence

### Sprint 42

- `ENG-230` Engine surface maturity model and audit baseline (shipped)

### Sprint 43

- `ENG-231` Truthful managed event-subscription execution baseline (shipped)

### Sprint 44

- `ENG-232` Agentics tool execution and run-state baseline (shipped)

### Sprint 45

- `ENG-233` Retrieval indexing, query execution, and freshness baseline (shipped)

### Sprint 46

- `ENG-234` Multi-tenancy governance, membership, and domain workflow companion split (shipped)

### Sprint 47

- `ENG-235` Multi-tenancy governance membership evaluation baseline (shipped)

### Sprint 48

- `ENG-236` Multi-tenancy governance invitation validation baseline (shipped)

### Sprint 49

- `ENG-237` Multi-tenancy governance domain ownership validation baseline (shipped)

### Sprint 50

- `ENG-238` Multi-tenancy governance action decision baseline (shipped)

### Sprint 51

- `ENG-239` Multi-tenancy governance action workflow execution baseline (shipped)

### Sprint 52

- `ENG-240` Multi-tenancy governance durable action store baseline (shipped)

### Sprint 53

- `ENG-241` Multi-tenancy governance durable membership store baseline (shipped)

### Sprint 54

- `ENG-242` Multi-tenancy governance durable invitation store baseline (shipped)

### Sprint 55

- `ENG-243` Multi-tenancy governance durable domain ownership store baseline (shipped)

### Sprint 56

- `ENG-244` Multi-tenancy governance domain ownership verification workflow baseline (shipped)

### Sprint 57

- `ENG-245` Multi-tenancy governance domain ownership proof evaluation baseline (shipped)

### Sprint 58

- `ENG-246` Multi-tenancy governance domain ownership proof challenge issuance baseline (shipped)

### Sprint 59

- `ENG-247` Multi-tenancy governance domain ownership proof publication planning baseline (shipped)

### Sprint 60

- `ENG-248` Multi-tenancy governance domain ownership HTTP proof collection baseline (shipped)

### Sprint 61

- `ENG-249` Multi-tenancy governance domain ownership proof verification runner baseline (shipped)

### Sprint 62

- `ENG-250` Multi-tenancy governance domain ownership DNS TXT proof collection baseline (shipped)

### Sprint 63

- `ENG-251` Multi-tenancy governance domain ownership proof polling runner baseline (shipped)

### Sprint 64

- `ENG-252` Multi-tenancy governance automatic background proof polling baseline (shipped)

### Sprint 65

- `ENG-253` Multi-tenancy governance HTTP proof publication baseline (shipped)

### Sprint 66

- `ENG-254` Multi-tenancy governance tenant administration workflow baseline (shipped)

### Sprint 67

- `ENG-255` Multi-tenancy governance ASP.NET Core tenant administration endpoint baseline (shipped)

### Sprint 68

- `ENG-256` Multi-tenancy governance invitation delivery dispatch baseline (shipped)

### Sprint 69

- `ENG-257` Multi-tenancy governance HTTP invitation delivery sender baseline (shipped)

### Sprint 70

- `ENG-258` Multi-tenancy governance HTTP invitation delivery webhook signing baseline (shipped)

### Sprint 71

- `ENG-259` Multi-tenancy governance HTTP invitation delivery retry baseline (shipped)

### Sprint 72

- `ENG-260` Multi-tenancy governance HTTP invitation delivery idempotency baseline (shipped)

### Sprint 73

- `ENG-261` Multi-tenancy governance invitation delivery status reconciliation baseline (shipped)

### Sprint 74

- `ENG-262` Behavior REST profile runtime ownership metadata baseline (shipped)

### Sprint 75

- `ENG-263` Eventing subscription execution binding catalog baseline (shipped)

### Sprint 76

- `ENG-264` Eventing subscription execution readiness catalog baseline (shipped)

### Sprint 77

- `ENG-265` Eventing subscription readiness operator-surface baseline (shipped, issue #774)

### Sprint 78

- `ENG-266` Agentics tool-run operator-surface baseline (shipped, issue #775)

### Sprint 79

- `ENG-267` Retrieval knowledge-index operator-surface baseline (shipped, issue #776)

### Sprint 80

- `ENG-268` Retrieval reindex operator-action baseline (shipped, issue #777)

### Sprint 81

- `ENG-269` Agentics tool execution operator-action baseline (shipped, issue #778)

### Sprint 82

- `ENG-270` Retrieval background reindex scheduler baseline (shipped, issue #779)

### Sprint 83

- `ENG-271` Eventing in-process subscription execution baseline (shipped, issue #780)

### Sprint 84

- `ENG-272` Eventing publication operator action baseline (shipped, issue #781)

### Sprint 85

- `ENG-273` Eventing in-process subscription retry baseline (shipped, issue #782)

### Sprint 86

- `ENG-274` Eventing in-process subscription idempotency baseline (shipped, issue #783)

### Sprint 87

- `ENG-275` Eventing publication runtime operator-state baseline (shipped, issue #784)
- `ENG-535` Eventing code-first subscription execution middleware baseline (shipped)
- `ENG-536` Eventing code-first descriptor-provider discovery baseline (shipped)
- `ENG-554` Eventing attribute descriptor discovery baseline (shipped)

### Sprint 88

- `ENG-276` Wolverine bounded subscription retry terminal failure (shipped, issue #785)

### Sprint 89

- `ENG-277` Wolverine dispatch terminal retry failure (shipped, issue #786)

### Sprint 90

- `ENG-278` Wolverine dispatch publish-exception terminal proof (shipped, issue #787)

### Sprint 91

- `ENG-279` first-class event-dispatch terminal-failure runtime posture (shipped, issue #788)

### Sprint 92

- `ENG-280` Agentics bounded in-process retry posture (shipped, issue #789)

### Sprint 93

- `ENG-281` Agentics process-local duplicate-run idempotency posture (shipped, issue #790)

### Sprint 94

- `ENG-282` Agentics approval-required and terminal-failure operator posture (shipped, issue #791)

### Sprint 95

- `ENG-283` Retrieval query operator action seam (shipped, issue #792)
- `ENG-284` Multi-tenancy invitation delivery status callback endpoint baseline (shipped, issue #793)
- `ENG-285` Multi-tenancy invitation delivery status callback signature verification baseline (shipped, issue #800)
- `ENG-286` Multi-tenancy invitation delivery status callback replay protection baseline (shipped, issue #801)
- `ENG-287` Multi-tenancy invitation delivery status observation store baseline (shipped, issue #802)
- `ENG-288` Multi-tenancy invitation delivery status observation endpoint baseline (shipped, issue #803)
- `ENG-289` Multi-tenancy invitation delivery dispatch endpoint baseline (shipped, issue #804)
- `ENG-290` Multi-tenancy invitation delivery durable retry queue baseline (shipped, issue #805)

### Sprint 96

- `ENG-291` Multi-tenancy invitation delivery background retry scheduling baseline (shipped, issue #806)

### Sprint 97

- `ENG-292` Multi-tenancy invitation delivery retry execution coordination baseline (shipped, issue #807)

### Sprint 98

- `ENG-293` Multi-tenancy invitation delivery SMTP sender baseline (shipped, issue #808)

### Sprint 99

- `ENG-294` Multi-tenancy invitation delivery SendGrid sender baseline (shipped, issue #809)

### Sprint 100

- `ENG-295` Multi-tenancy invitation delivery SendGrid Event Webhook callback translation baseline (shipped, issue #810)

### Sprint 101

- `ENG-296` Multi-tenancy invitation delivery SendGrid signed Event Webhook verification (shipped, issue #811)

### Sprint 102

- `ENG-297` Multi-tenancy invitation delivery SendGrid signed Event Webhook replay protection baseline (shipped, issue #812)

### Sprint 103

- `ENG-298` Multi-tenancy invitation delivery SendGrid event-id idempotency baseline (shipped, issue #813)

### Sprint 104

- `ENG-299` Multi-tenancy invitation delivery Mailgun sender baseline (shipped, issue #814)

### Sprint 105

- `ENG-300` Multi-tenancy invitation delivery Mailgun webhook callback translation baseline (shipped, issue #815)

### Sprint 106

- `ENG-301` Multi-tenancy invitation delivery Mailgun signed webhook verification baseline (shipped, issue #816)

### Sprint 107

- `ENG-302` Multi-tenancy invitation delivery Mailgun signed webhook replay protection baseline (shipped, issue #817)

### Sprint 108

- `ENG-303` Multi-tenancy invitation delivery Mailgun event-id idempotency baseline (shipped, issue #818)

### Sprint 109

- `ENG-304` Multi-tenancy invitation delivery Microsoft Graph sender baseline (shipped, issue #819)

### Sprint 110

- `ENG-305` Multi-tenancy invitation delivery Microsoft Graph Azure Identity token provider baseline (shipped, issue #820)

### Sprint 111

- `ENG-306` Multi-tenancy invitation delivery Amazon SES sender baseline (shipped, issue #821)

### Sprint 112

- `ENG-307` Multi-tenancy invitation delivery Amazon SES SNS callback translation baseline (shipped, issue #822)

### Sprint 113

- `ENG-308` Multi-tenancy invitation delivery Amazon SES SNS signature verification baseline (shipped, issue #823)

### Sprint 114

- `ENG-309` Multi-tenancy invitation delivery Amazon SES SNS replay protection baseline (shipped, issue #824)

### Sprint 115

- `ENG-310` Multi-tenancy invitation delivery Amazon SES SNS message-id idempotency baseline (shipped, issue #825)

### Sprint 116

- `ENG-311` Multi-tenancy invitation delivery Amazon SES SNS subscription confirmation baseline (shipped, issue #826)

### Sprint 117

- `ENG-312` Multi-tenancy invitation delivery Amazon SES SNS unsubscribe confirmation observation baseline (shipped, issue #827)

### Sprint 118

- `ENG-313` Multi-tenancy delivery status observation rollup operator summary (shipped, issue #828)

### Sprint 119

- `ENG-314` Multi-tenancy delivery status observation attention drill-down baseline (shipped, issue #829)

### Sprint 120

- `ENG-315` Multi-tenancy delivery status observation remediation hints baseline (shipped, issue #830)

### Sprint 121

- `ENG-316` Multi-tenancy delivery status observation remediation action filter baseline (shipped, issue #831)

### Sprint 122

- `ENG-317` Multi-tenancy delivery status observation provider-message drill-down baseline (shipped, issue #832)

### Later / not scheduled yet

- actual DNS proof publication, provider-backed proof publication or mutation, remediation execution beyond state transitions, distributed or provider-backed membership/invitation/domain/action-store backends, additional provider-specific email API senders beyond the shipped SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph set, SMS/chat/CRM/identity-provider invitation senders, Microsoft Entra app registration/permission consent/mailbox access policy, AWS account/IAM/identity verification, DKIM/SPF/DMARC, SES sandbox/configuration-set event destination setup, SNS topic/subscription creation, automatic resubscribe/restore, subscription lifecycle governance, distributed retry queues, cross-node retry leases, provider-specific or distributed callback inboxes, cross-node callback replay protection, provider-specific callback payload translation beyond shipped SendGrid/Mailgun/Amazon SES translators, provider-specific callback signature verification beyond shipped SendGrid/Mailgun/Amazon SNS hardening, provider polling, identity-provider synchronization, public onboarding, and tenant-admin UI/backoffice flows when `Cephalon.MultiTenancy.Governance` or provider packs truly own those paths

## Promotion checklist

Before a surface claims the next maturity level, confirm the proof is real:

1. `M0 -> M1`: truthful runtime or catalog answer exists and docs say what is not owned yet.
2. `M1 -> M2`: one managed path exists end to end, including runtime state, failure posture, and ownership language.
3. `M2 -> M3`: operators can observe, reconcile, or remediate the path without relying on hidden knowledge.
4. `M3 -> M4`: samples, package docs, validation, compatibility/readiness guidance, and planning truth all match the shipped behavior.

## Definition of done for future planning

When a new runtime or package surface lands, the same slice should update:

- source
- component docs
- this audit when maturity or ownership changes
- roadmap and backlog entries
- compatibility or readiness docs if the public/package contract changes
- samples, tests, and benchmarks when the claimed maturity requires them

The goal is not to force every surface to become `M4`.

The goal is to make each surface honest, intentional, and easy to adopt.
