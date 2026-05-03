# Engine Surface Maturity Audit

Surface maturity in this document reflects the repository state as of `May 3, 2026`.

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
| `Cephalon.Cli`, `Cephalon.Scaffolding`, `Cephalon.TemplatePack`, `Cephalon.ReferenceDocs` | Adoption and packaging surface | `cephalon-managed` | `M4` | Maintain package/version/template/reference-doc alignment |
| `Cephalon.Behaviors` core runtime and durable execution | Behavior execution substrate | `cephalon-managed` | `M4` | Continue adoption polish and guardrail coverage rather than adding parallel execution stories |
| `Cephalon.Behaviors.Http` module-owned behavior REST projection | REST profile metadata plus explicit module-owned public REST activation, Cephalon-managed materialization, governance, and runtime catalogs | mixed: `application-managed` profile/publication activation plus `cephalon-managed` materialization/runtime catalog truth | `M2` | Keep profile metadata explicitly non-publishing and continue adoption/operator automation proof without inventing ambient REST publication |
| `Cephalon.Data` shared CDC runtime plus provider-native pumps | Shared and provider-native data execution truth | `cephalon-managed` plus `provider-managed` | `M3` | More package-level external adoption proof and operator docs per provider family |
| `Cephalon.Edge.KubernetesGateway` and `Cephalon.Edge.Traefik` | Provider-specific control-plane automation | `provider-managed` | `M3` | More adoption-quality samples and package publishing guidance outside the repo |
| `Cephalon.Eventing` core package | Channel descriptors, staged publication, subscription runtime truth, public managed-execution binding catalog, abstraction-level subscription execution-readiness catalog, abstraction-level publication dispatcher, abstraction-level publication runtime-state catalog, abstraction-level dispatch-state/summary terminal-failure posture, `/engine/event-subscription-readiness`, `POST /engine/event-publications`, `/engine/event-publications/runtime*`, `/engine/event-dispatches/terminal-failures`, `snapshot.EventSubscriptionExecutionReadiness`, `snapshot.EventPublicationStates`, `snapshot.EventDispatchStates`, stable subscription runtime metadata vocabulary, opt-in direct in-process subscription execution, bounded process-local retry, and bounded process-local duplicate-completed execution suppression | mixed: `application-managed` descriptors/runtime reports plus `cephalon-managed` direct in-process execution, bounded process-local retry, bounded process-local idempotency, bounded publication action truth, publication runtime-state truth for direct execution or accepted outbox handoff, and typed dispatch terminal-failure operator truth over reported dispatch state | `M3` | Keep the in-process lane, retry loop, idempotency guard, operator publication route, publication runtime-state catalog, and dispatch terminal-failure posture bounded to reported state; only claim generic broker/inbox/durable retry/downstream delivery ownership when a package truly owns those paths |
| `Cephalon.Eventing.Wolverine` | Optional Wolverine-managed staged dispatch, bounded managed-dispatch retry with terminal dispatch-store semantics, publish-exception terminal proof, first-class terminal dispatch operator posture through the shared eventing state/summary surfaces, subscription execution, bounded managed-subscription retry, and terminal exhausted-attempt failure baseline | `provider-managed` | `M3` | Broaden inbound-consumption, broker-specific dead-letter, and operator-automation proof only when the runtime truly owns those paths |
| `Cephalon.Agentics` | Tool descriptors, managed tool dispatch, abstraction-level run-state catalog, bounded operator action route, bounded process-local executor retry posture, bounded process-local duplicate-completed suppression, approval-required and terminal-failure operator filters, `/engine/agent-tool-runs`, `/engine/agent-tool-runs/retry-pending`, `/engine/agent-tool-runs/idempotency-duplicates`, `/engine/agent-tool-runs/approval-required`, `/engine/agent-tool-runs/terminal-failures`, `POST /engine/agent-tools/{toolId}/runs`, `snapshot.AgentToolRuns`, and agent-workload runtime surface | mixed: `application-managed` descriptors plus `cephalon-managed` dispatcher/run-state/operator-action/bounded-process-local-retry/bounded-process-local-idempotency/read-filter baseline | `M3` | Broader agent operator workflows, durable approval workflows, durable retry queues, durable inboxes, memory persistence, dead-letter systems, cross-node exactly-once delivery, distributed scheduling, and provider-specific AI orchestration only after a package truly owns those paths |
| `Cephalon.Retrieval` | Knowledge collection descriptors plus managed lexical indexing, query execution, freshness state, abstraction-level index-state catalog, abstraction-level bounded query command seam, manual reindex command seam, opt-in background reindex scheduler, `/engine/knowledge-indexes`, `POST /engine/knowledge-indexes/{collectionId}/queries`, `POST /engine/knowledge-indexes/{collectionId}/reindex`, and `snapshot.KnowledgeIndexes` | mixed: `application-managed` source documents plus `cephalon-managed` index/query/operator-read/operator-query/manual-remediation/background-automation baseline | `M3` | Provider-specific vector/search engines, durable or distributed indexes, and distributed scheduler coordination only after a package truly owns those paths |
| `Cephalon.MultiTenancy` core package | Narrow tenant-resolution plus explicit governance-boundary runtime truth | mixed: `cephalon-managed` tenant-resolution core plus boundary entries for companion-owned or planned workflows | `M2` | Keep the base package focused on resolution while companion packages own concrete governance workflows |
| `Cephalon.MultiTenancy.Governance` | Tenant membership, invitation, delivery dispatch/retry/status reconciliation, delivery-status observation storage, tenant-administration, domain-ownership verification/proof collection/polling, governance action workflows, optional ASP.NET Core governance endpoints with bounded delivery-status observation reads, filtered rollup summaries, attention-category drill-downs, provider-message drill-downs, remediation-action filters, and remediation hints, optional HTTP/SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph senders, optional Microsoft Graph Azure Identity token provider, optional ASP.NET Core SendGrid callback translation plus signed-webhook verification, process-local replay protection, and observation-store-backed event-id idempotency, optional ASP.NET Core Mailgun callback translation plus HMAC signed-webhook verification, bounded process-local replay-token protection, and observation-store-backed event-id idempotency, and optional ASP.NET Core Amazon SES over SNS callback translation plus SNS signature verification, bounded process-local SNS replay protection, observation-store-backed SNS message-id idempotency, verified SNS subscription confirmation, and verified SNS unsubscribe-confirmation observation | mixed: `cephalon-managed` governance core, ASP.NET Core adapter endpoints, filtered observation rollup summaries, attention-category drill-downs, provider-message drill-down filters, remediation-action filters, remediation hints over matched normalized observations, provider-neutral callback signatures, bounded process-local replay protection, local durable stores, HTTP/SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph sender companions, Microsoft Graph Azure Identity token-provider companion, SendGrid callback translation/signature verification/process-local replay/event-id-idempotency protection, Mailgun callback translation/signature verification/process-local replay-token/event-id-idempotency protection, and Amazon SES over SNS callback translation, opt-in SNS signature verification, bounded process-local SNS replay protection, observation-store-backed SNS message-id idempotency, opt-in verified SNS subscription confirmation, and opt-in verified SNS unsubscribe-confirmation observation; external provider delivery/status, distributed remediation execution, Microsoft Entra tenant governance, AWS account/identity governance, SNS topic/subscription creation, automatic resubscribe/restore, and subscription lifecycle governance remain `provider-managed` | `M2` | Actual DNS proof publication, provider-backed proof publication or mutation, remediation execution beyond state transitions, distributed or provider-backed membership/invitation/domain/action-store backends, additional provider-specific email API senders beyond the shipped SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph set, SMS/chat/CRM/identity-provider invitation senders, Microsoft Entra app registration/permission consent/mailbox access policy, AWS account/IAM/identity verification, DKIM/SPF/DMARC, SES sandbox/configuration-set event destination setup, SNS topic/subscription creation, automatic resubscribe/restore, subscription lifecycle governance, distributed retry queues, cross-node retry leases, provider-specific or distributed callback inboxes, cross-node callback replay protection, distributed event-id ledgers, provider-specific callback payload translation beyond shipped SendGrid/Mailgun/Amazon SES translators, provider-specific callback signature verification beyond shipped SendGrid/Mailgun/Amazon SNS hardening, provider polling, identity-provider synchronization, public onboarding, and tenant-admin UI/backoffice flows only when the package truly owns those paths |
| `Cephalon.Diagnostics` | Engine-level OpenTelemetry semantic-convention adapter publishing canonical `ActivitySource` and `Meter` names (`Cephalon.Engine` / `Cephalon.AspNetCore` / `Cephalon.Worker` / `Cephalon.Eventing` / `Cephalon.MultiTenancy.Governance` / `Cephalon.Agentics` / `Cephalon.Retrieval`) plus stable `cephalon.*` attribute keys (`cephalon.module.id`, `cephalon.behavior.id`, `cephalon.cell.id`, `cephalon.app.blueprint`, `cephalon.tenant.id`); `Cephalon.Engine` consumes the canonical names for `engine.build`, `module.{phase}`, and `runtime.*` spans / metrics; `Cephalon.AspNetCore` declares its diagnostics convention against `CephalonActivitySources.AspNetCore` and exposes `/engine/diagnostics-conventions` projecting the canonical name set as a `DiagnosticsConventionsSurface` record; `Cephalon.Worker` declares `WorkerDiagnostics.ActivitySource` against `CephalonActivitySources.Worker` and emits `worker.lifecycle.start` / `worker.lifecycle.stop` spans around hosted-service lifecycle; `Cephalon.Observability.OpenTelemetry` subscribes to all seven canonical activity sources and meters as the OTLP companion pack's real subscription contract (`.Agentics` and `.Retrieval` are pre-declared canonical names — the matching companion packs do not yet emit, so the subscription is a no-op until those packs ship their first `ActivitySource` / `Meter` instances); ships the `IRedactionFilter` + `RedactionContext` + `RedactionPipeline` redaction surface with `KeyMatchRedactionFilter` / `RegexRedactionFilter` starters and `IServiceCollection.AddRedactionPipeline()` registration; the AspNetCore HTTP request/response logging middleware and the engine runtime module-phase activity tags route attribute values through the registered `RedactionPipeline` before exporter dispatch; the canonical recipe is documented in `docs/components/diagnostics.md` *Redaction quick start* and adopted by all five samples | `cephalon-managed` | `M4` | Adoption-quality; `LoggerMessage` source-generated factories aligned with the per-package diagnostic-id range discipline, `EngineBuilder` build-time activity tag redaction, and per-companion-pack OTel adapter emission baseline for CDC / agentics / retrieval (canonical names pre-declared for agentics + retrieval; eventing emission tracked under the in-flight ENG-371 sibling, multi-tenancy governance emission tracked under the in-flight ENG-379 sibling, CDC remains canonical-name-only follow-up) remain follow-up |
| `Cephalon.Analyzers` | Curated consumer-facing analyzer meta-package bundling `Microsoft.CodeAnalysis.BannedApiAnalyzers`, `Microsoft.CodeAnalysis.PublicApiAnalyzers`, `Roslynator.Analyzers`, `Meziantou.Analyzer`, and `Microsoft.VisualStudio.Threading.Analyzers`; ships curated `BannedSymbols.txt` (auto-wired through `buildTransitive/Cephalon.Analyzers.props`) and curated `cephalon-analyzers.editorconfig`; `Cephalon.Abstractions` consumes the meta-package as the proof-of-concept | `cephalon-managed` | `M1` | Promote to `M2` when the meta-package is the documented adoption path in `getting-started.md` and the template-pack starter projects reference it by default |
| Engine-level resilience runtime (descriptors in `Cephalon.Abstractions/Resilience/*` plus `Cephalon.Behaviors/Resilience/*` runtime over `Microsoft.Extensions.Resilience` / Polly v8) | Declarative retry / circuit-breaker / bulkhead / timeout / hedging / rate-limit descriptors plus behavior-coupled execution runtime, behavior idempotency resolver, exception classifier, runtime catalog, and middleware | mixed: `application-managed` resilience descriptors (selected at app-profile level) plus `cephalon-managed` execution runtime inside `Cephalon.Behaviors` over the in-box `Microsoft.Extensions.Resilience` integration | `M2` | Extract the resilience runtime into a dedicated `Cephalon.Resilience` package so consumers can adopt the engine's resilience surface without taking the full `Cephalon.Behaviors` runtime; promote to `M3` when an explicit operator surface (catalog routes, snapshot keys) lands and the dedicated package owns the runtime |

## Immediate planning consequences

- stop expanding descriptor-first surfaces inside mixed-maturity families unless the work is explicitly labeled `M0` or `M1`
- do not describe `M0` or `M1` packages as if they already own execution, orchestration, or provisioning
- treat the `Cephalon.Eventing` in-process direct subscription lane plus bounded process-local retry, bounded process-local idempotency, `POST /engine/event-publications`, `/engine/event-publications/runtime*`, and first-class terminal dispatch-state operator posture, the optional `Cephalon.Eventing.Wolverine` provider-managed dispatch/subscription lane plus bounded terminal dispatch and subscription retry proof, the `Cephalon.Agentics` dispatcher/run-state lane plus direct operator read/action seams, bounded process-local executor retry posture, bounded process-local duplicate-completed suppression, approval-required filter, and terminal-failure filter, and the `Cephalon.Retrieval` lexical index/query/freshness lane plus direct operator read, bounded query action, manual reindex, and opt-in background reindex seams as current managed vertical proofs instead of widening descriptor breadth before ownership is real
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
