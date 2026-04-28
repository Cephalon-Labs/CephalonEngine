# Engine Surface Maturity Audit

Surface maturity in this document reflects the repository state as of `April 28, 2026`.

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
| `Cephalon.Behaviors.Http` metadata-only REST profiles | REST profile shorthand over behavior surfaces | `application-managed` | `M1` | Either keep them explicitly metadata-only or prove a managed authoring/runtime lane |
| `Cephalon.Data` shared CDC runtime plus provider-native pumps | Shared and provider-native data execution truth | `cephalon-managed` plus `provider-managed` | `M3` | More package-level external adoption proof and operator docs per provider family |
| `Cephalon.Edge.KubernetesGateway` and `Cephalon.Edge.Traefik` | Provider-specific control-plane automation | `provider-managed` | `M3` | More adoption-quality samples and package publishing guidance outside the repo |
| `Cephalon.Eventing` core package | Channel descriptors, staged publication, subscription runtime truth, and managed-execution binding vocabulary | mixed: `application-managed` baseline plus companion-bound execution truth | `M1` | Keep the adapter-neutral execution seam narrow and truthful without claiming generic broker/inbox ownership in the core pack |
| `Cephalon.Eventing.Wolverine` | Optional Wolverine-managed staged dispatch and subscription execution baseline | `provider-managed` | `M2` | Broaden inbound-consumption, retry-policy, and operator-automation proof only when the runtime truly owns those paths |
| `Cephalon.Agentics` | Tool descriptors, managed tool dispatch, and agent-workload runtime surface | mixed: `application-managed` descriptors plus `cephalon-managed` dispatcher/run-state baseline | `M2` | Broader operator automation, retry/queue semantics, memory persistence, and provider-specific AI orchestration only after a package truly owns those paths |
| `Cephalon.Retrieval` | Knowledge collection descriptors plus managed lexical indexing, query execution, and freshness state | mixed: `application-managed` source documents plus `cephalon-managed` index/query baseline | `M2` | Provider-specific vector/search engines, durable or distributed indexes, reindex automation, and operator remediation only after a package truly owns those paths |
| `Cephalon.MultiTenancy` core package | Narrow tenant-resolution plus explicit governance-boundary runtime truth | mixed: `cephalon-managed` tenant-resolution core plus boundary entries for companion-owned or planned workflows | `M2` | Keep the base package focused on resolution while companion packages own concrete governance workflows |
| `Cephalon.MultiTenancy.Governance` | Tenant membership catalog/evaluation, tenant invitation catalog/validation, declared tenant-domain ownership catalog/validation, and governance runtime surfaces | `cephalon-managed` membership, invitation, and declared domain-ownership proofs | `M2` | DNS/HTTP verification execution, approval/remediation, durable membership/invitation/domain storage, invitation delivery, identity-provider synchronization, and tenant administration only when the package truly owns those paths |

## Immediate planning consequences

- stop expanding descriptor-first surfaces inside mixed-maturity families unless the work is explicitly labeled `M0` or `M1`
- do not describe `M0` or `M1` packages as if they already own execution, orchestration, or provisioning
- treat `Cephalon.Eventing.Wolverine`, the `Cephalon.Agentics` dispatcher/run-state lane, and the `Cephalon.Retrieval` lexical index/query/freshness lane as current managed vertical proofs instead of widening descriptor breadth before ownership is real
- keep `Cephalon.MultiTenancy` intentionally thin in the base package; `Cephalon.MultiTenancy.Governance` now owns membership catalog/evaluation, invitation catalog/validation, and declared domain-ownership catalog/validation proofs, while broader governance workflows remain explicitly outside the current claim
- use `Cephalon.Behaviors`, `Cephalon.Data`, and the shipped edge provider packs as the current examples of truthful runtime ownership

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

### Later / not scheduled yet

- DNS/HTTP domain verification execution, approval/remediation, durable membership/invitation/domain storage, invitation delivery, identity-provider synchronization, and tenant-administration proof when `Cephalon.MultiTenancy.Governance` truly owns those paths

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
