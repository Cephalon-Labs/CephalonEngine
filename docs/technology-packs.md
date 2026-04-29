# Cephalon Technology Packs

Technology packs are the runtime companion pattern for future-facing workloads in Cephalon.

They sit between pure metadata and full application blueprints:

- `Blueprints` define project shape
- `Patterns` define supporting design behavior
- `Technologies` define workload posture
- `Technology packs` provide reusable runtime primitives for those technologies

## Why this exists

Technology profiles should not stop at documentation or scaffold hints. When a workload becomes important enough to need reusable services, capabilities, or conventions, it should move into a companion package instead of forcing changes into the engine core.

That keeps the engine extensible without making every new trend a built-in subsystem.
Some profiles can also start as engine-owned contract baselines before a dedicated companion pack
exists. The new `cell-based-architecture` profile now follows that path: the engine ships
`CellBoundaryDescriptor`, `CellRouteDescriptor`, `CellHealthIsolationDescriptor`,
`CellTrafficAutomationRuntimeDescriptor`, `/engine/cells`, `/engine/cell-routes`,
`/engine/cell-health-isolations`, `/engine/cell-traffic-automations`, and the
`cell-boundaries`, `cell-routes`, `cell-health-isolations`, plus `cell-traffic-automations`
technology runtime surfaces today. That engine-owned baseline now also carries additive
`providerId` plus `edgeNodeIds` targeting on the shared automation catalog. The shared
materialization seam is now also explicit through `CellTrafficAutomationMaterializationResult`,
`CellTrafficAutomationMaterializationStates`, `ICellTrafficAutomationProviderMaterializer`, and
`ICellTrafficAutomationEdgeMaterializer`; `Cephalon.Edge` now ships the first concrete
edge-runtime materializer while the provider-named result/state types stay available as
compatibility helpers over the same contract. Future service-mesh, gateway, or provider companion
packs can reconcile provider-owned or edge-owned traffic posture back onto the same shared
automation catalog instead of publishing a second materialization registry. The first concrete
provider-specific control-plane follow-through is now also shipped through
`Cephalon.Edge.KubernetesGateway`, which projects Kubernetes Gateway API `Gateway` plus `HTTPRoute`
intent back onto the same shared automation catalog and publishes that view through the
`kubernetes-gateway-traffic-materializations` technology surface. That same pack now also supports
opt-in live `observe-only` Gateway API polling plus `apply-and-reconcile` ownership-aware
`HTTPRoute` writes so projected intent, write posture, and observed control-plane status can stay
on one runtime truth instead of spawning a second provider-local view.

## Shipped baseline packs

Current baseline packages:

- `Cephalon.Agentics`
  - runtime services and capability activation for `AgenticWorkloads`
  - registers `IAgentToolCatalog` when the profile is selected
  - registers the Cephalon-managed abstraction-level `IAgentToolDispatcher`, abstraction-level `IAgentToolRunCatalog`, and `IAgentToolRunReporter` when execution is enabled
  - lets ASP.NET Core hosts trigger one bounded managed run through `POST /engine/agent-tools/{toolId}/runs` without taking a dependency on implementation types
  - can retry failed executor attempts inside the same process when `ExecutionMaxAttempts` is greater than `1`, reporting `retry-scheduled` run-state posture instead of claiming durable retry ownership
  - can suppress duplicate completed `toolId + runId` executions inside the same process when `EnableExecutionIdempotency` is enabled, reporting `skipped` duplicate-completed posture instead of claiming durable inbox or cross-node exactly-once ownership
  - lets operators filter approval-blocked and terminal-failed runs through `/engine/agent-tool-runs/approval-required` and `/engine/agent-tool-runs/terminal-failures`
  - lets modules add `IAgentToolExecutor`, `IAgentToolExecutionPolicy`, and `IAgentToolExecutionObserver` services without making the host own the tool loop
- `Cephalon.Eventing`
  - runtime services and capability activation for `EventDrivenIntegration`
  - registers `IEventChannelCatalog`, `IEventSubscriptionCatalog`, `IEventSubscriptionExecutionBindingCatalog`, the abstraction-level `IEventSubscriptionExecutionReadinessCatalog`, the abstraction-level `IEventPublicationRuntimeCatalog`, the abstraction-level event-dispatch runtime descriptor/state catalogs, and stable event subscription plus dispatch runtime metadata keys when the profile and options enable those paths
  - lets `Cephalon.Engine`, host adapters, and operator tooling read subscription execution readiness through `/engine/event-subscription-readiness` and `snapshot.EventSubscriptionExecutionReadiness` without taking a direct dependency on the eventing pack
  - registers the abstraction-level `IEventPublicationDispatcher` when a real publishing path exists, allowing host adapters to expose bounded publication actions such as `POST /engine/event-publications` without depending on eventing implementation types
  - lets `Cephalon.Engine`, host adapters, and operator tooling read publication runtime state through `/engine/event-publications/runtime*` and `snapshot.EventPublicationStates`, with in-process outcomes separated from outbox `accepted` handoff truth
  - can opt into a Cephalon-managed direct in-process subscription execution lane through `EnableInProcessSubscriptionExecution`, registered `IEventSubscriptionExecutor` services, and `IEventPublisher`, including bounded process-local retries through `InProcessSubscriptionMaxAttempts` / `InProcessSubscriptionRetryDelayMilliseconds` and duplicate-completed execution suppression through `EnableInProcessSubscriptionIdempotency` / `InProcessSubscriptionIdempotencyRetentionMinutes` when explicitly configured, without claiming durable broker, durable inbox, cross-node exactly-once, durable retry-queue, or distributed retry ownership
- `Cephalon.Eventing.Wolverine`
  - optional companion adapter proof for managed dispatch over `EventDrivenIntegration`
  - projects runtime truth for the current Wolverine-backed outbox, dispatch loop, bounded provider-managed dispatch retry with terminal storage semantics, first-class terminal dispatch-state/summary posture, managed subscription bindings, bounded provider-managed subscription retry, terminal exhausted-attempt failure posture, and subscription readiness without turning Wolverine into an engine-core dependency
- `Cephalon.Retrieval`
  - runtime services and capability activation for `KnowledgeRetrieval`
  - registers `IKnowledgeCatalog`, the abstraction-level `IKnowledgeIndexCatalog`, `IKnowledgeIndexer`, and `IKnowledgeQueryEngine` when the profile and options enable those paths
  - lets modules add `IKnowledgeDocumentProvider` services so Cephalon can build a managed lexical index, bounded query path, operator reindex path, and opt-in background reindex scheduler without making hosts own the retrieval loop
- `Cephalon.MultiTenancy`
  - runtime services and capability activation for `MultiTenancy`
  - registers `ITenantResolver` and `ITenantContextAccessor` when the profile is selected
  - projects `tenant-resolution` for the shipped core and `tenant-governance-boundaries` for companion-owned or companion-planned governance workflows
- `Cephalon.MultiTenancy.Governance`
  - tenant-governance companion pack for `MultiTenancy`
  - registers `ITenantMembershipStore` with an in-memory default or opt-in file-backed durable store
  - registers `ITenantMembershipCatalog` and `ITenantMembershipEvaluator` when membership evaluation is enabled
  - registers `ITenantInvitationStore` with an in-memory default or opt-in file-backed durable store
  - registers `ITenantInvitationCatalog` and `ITenantInvitationValidator` when invitation validation is enabled
  - registers `ITenantInvitationDeliveryRunCatalog` and `ITenantInvitationDeliveryDispatcher` when invitation delivery dispatch is enabled; external delivery requires a registered `ITenantInvitationDeliverySender`
  - registers `ITenantInvitationDeliveryRetryStore`, `ITenantInvitationDeliveryRetryRunner`, `ITenantInvitationDeliveryRetryExecutionCoordinationCatalog`, and `ITenantInvitationDeliveryRetryRuntimeCatalog` when the opt-in invitation delivery retry queue is enabled, with an in-memory default, opt-in file-backed durable store, process-local execution coordination, and opt-in background scheduling hosted service
  - registers `ITenantInvitationDeliveryStatusReconciler` when invitation delivery status reconciliation is enabled; provider-specific callback translation, provider-specific signature verification, and provider polling stay outside the core pack
  - registers `ITenantInvitationDeliveryStatusObservationStore` with an in-memory default or opt-in file-backed durable store for normalized reconciliation observations
  - registers `ITenantAdministrationWorkflow` when tenant-administration workflow execution is enabled
  - registers `ITenantDomainOwnershipStore` with an in-memory default or opt-in file-backed durable store
  - registers `ITenantDomainOwnershipCatalog` and `ITenantDomainOwnershipValidator` when domain-ownership validation is enabled
  - registers `ITenantDomainOwnershipVerificationWorkflow` when in-process domain-ownership verification workflows are enabled
  - registers `ITenantDomainOwnershipProofChallengeIssuer` when domain proof challenge issuance is enabled
  - registers `ITenantDomainOwnershipProofPublicationPlanner` when domain proof publication planning is enabled
  - registers `ITenantDomainOwnershipHttpProofPublisher` and `ITenantDomainOwnershipHttpProofPublicationCatalog` when HTTP proof publication is enabled
  - registers `ITenantDomainOwnershipProofEvaluator` when domain proof evaluation and the verification workflow are enabled
  - registers `ITenantDomainOwnershipHttpProofCollector` when HTTP proof collection, publication planning, proof evaluation, and the verification workflow are enabled
  - registers `ITenantDomainOwnershipDnsTxtProofCollector` when DNS TXT proof collection, publication planning, proof evaluation, and the verification workflow are enabled; collection becomes Cephalon-managed only when a DNS-over-HTTPS resolver endpoint is configured
  - registers `ITenantDomainOwnershipProofPollingRunner` when the proof verification runner is enabled so hosts can run bounded on-demand polling over pending or rejected HTTP/DNS declarations without owning the loop themselves
  - registers `ITenantDomainOwnershipProofVerificationRunner` when challenge issuance, publication planning, proof evaluation, and the verification workflow are enabled
  - registers `ITenantGovernanceActionCatalog` and `ITenantGovernanceActionDecider` when governance-action decisions are enabled
  - registers `ITenantGovernanceActionStore` with an in-memory default or opt-in file-backed durable store
  - registers `ITenantGovernanceActionWorkflow` when in-process governance-action workflows are enabled
  - projects `tenant-memberships` for the Cephalon-managed membership catalog, store, and evaluation proof
  - projects `tenant-invitations` for the Cephalon-managed invitation catalog, store, validation, delivery dispatch, sender readiness, delivery-run, opt-in retry queue, process-local retry execution coordination, delivery-status reconciliation, and normalized observation-store proof
  - projects `tenant-domain-ownership` for the Cephalon-managed declared domain-ownership catalog, store, validation, in-process verification-workflow, proof-challenge issuance, proof-publication planning, HTTP file proof-publication state, reported-proof evaluation, HTTP file proof-collection proof, and configured DNS TXT proof-collection proof
  - projects `tenant-governance-actions` for the Cephalon-managed approval/remediation action catalog, decision, in-process workflow, and action-store proof
- `Cephalon.MultiTenancy.Governance.AspNetCore`
  - optional ASP.NET Core adapter for `Cephalon.MultiTenancy.Governance`
  - maps `MapCephalonTenantDomainOwnershipHttpProofs()` so published HTTP proof files can be served from ASP.NET Core hosts
  - maps `MapCephalonTenantAdministrationCommands()` so tenant-administration workflow commands can be exposed through a fail-closed ASP.NET Core endpoint
  - maps `MapCephalonTenantInvitationDeliveryDispatches()` so tenant-invitation delivery dispatch can be requested through a fail-closed ASP.NET Core endpoint over the host-agnostic dispatcher
  - maps `MapCephalonTenantInvitationDeliveryStatusCallbacks()` so normalized invitation delivery-status observations can be reconciled through a fail-closed ASP.NET Core endpoint
  - maps `MapCephalonTenantInvitationDeliveryStatusObservations()` so normalized observation history can be read through a bounded fail-closed ASP.NET Core endpoint
  - can verify provider-neutral HMAC-SHA256 callback signatures over the exact normalized JSON body when `TenantInvitationDeliveryStatusCallbackSigningSecret` is configured
  - can reject duplicate signed normalized callbacks inside a bounded process-local replay window without claiming durable inboxes or cross-node exactly-once delivery
  - keeps endpoint enablement, route pattern, cache-control, authorization, provider-message-match enforcement, callback signature headers/key id/tolerance, signed-callback replay retention/cache limits, observation read default/max limits, and endpoint-description posture configurable through `Engine:MultiTenancy:Governance:AspNetCore`
- `Cephalon.MultiTenancy.Governance.HttpDelivery`
  - optional HTTP webhook sender companion for `Cephalon.MultiTenancy.Governance`
  - registers a provider-managed `ITenantInvitationDeliverySender` with sender id `http-webhook` by default
  - reads `Engine:MultiTenancy:Governance:HttpInvitationDelivery` or code-first options for endpoint, method, headers, accepted status codes, timeout, supported channels, provider-neutral idempotency headers, bounded in-process retry/backoff, and optional HMAC-SHA256 webhook signing
  - emits `Cephalon.MultiTenancy.Governance.HttpDelivery` diagnostics while leaving SendGrid Mail Send API handoff to `Cephalon.MultiTenancy.Governance.SendGridDelivery` and leaving Mailgun, SES, Microsoft Graph, SMS, chat, CRM, identity-provider, automatic background delivery workers, distributed retry queues, provider-specific delivery-status callback translation, provider-specific callback signature verification, provider polling, and provider-reconciliation semantics to future provider packs or applications
- `Cephalon.MultiTenancy.Governance.SendGridDelivery`
  - optional SendGrid Mail Send API sender companion for `Cephalon.MultiTenancy.Governance`
  - registers a provider-managed `ITenantInvitationDeliverySender` with sender id `sendgrid-email` by default
  - reads `Engine:MultiTenancy:Governance:SendGridInvitationDelivery` or code-first options for API base URL, API key, sender email, recipient metadata key, supported channels, templates, categories, custom arguments, safe headers, sandbox mode, accepted status codes, and provider message id header capture
  - exposes `ISendGridInvitationDeliveryClient`, `SendGridInvitationDeliveryMessage`, and `SendGridInvitationDeliveryClientResult` so hosts can test or replace the default SendGrid HTTP client without changing the core dispatcher
  - emits `Cephalon.MultiTenancy.Governance.SendGridDelivery` diagnostics while leaving SendGrid Event Webhook callback translation to `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore` and leaving SendGrid webhook signature verification, durable callback inboxes, bounce orchestration beyond status translation, provider polling, dynamic-template lifecycle management, Mailgun, SES, Microsoft Graph, SMS, chat, CRM, identity-provider onboarding, and provider-specific reconciliation semantics to future provider packs or applications
- `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore`
  - optional ASP.NET Core SendGrid Event Webhook translator companion for `Cephalon.MultiTenancy.Governance`
  - maps `MapCephalonSendGridInvitationDeliveryStatusCallbacks()` so SendGrid Event Webhook arrays can be translated into `ITenantInvitationDeliveryStatusReconciler`
  - reads `Engine:MultiTenancy:Governance:SendGridInvitationDelivery:AspNetCore` or code-first options for route, authorization, provider-message matching, body/event limits, event source/actor, engagement-event mapping, and `sg_message_id` normalization
  - translates SendGrid `processed`, `delivered`, `deferred`, `bounce`, `dropped`, `spamreport`, `unsubscribe`, and `group_unsubscribe` events into Cephalon delivery statuses while skipping engagement events by default
  - projects `tenant-invitation-delivery-sendgrid-status-callbacks` with route/auth/limit/translation posture while leaving SendGrid signed-webhook verification, durable callback inboxes, distributed replay protection, provider polling, and non-SendGrid provider translation to future provider packs or applications
- `Cephalon.MultiTenancy.Governance.SmtpDelivery`
  - optional SMTP relay sender companion for `Cephalon.MultiTenancy.Governance`
  - registers a provider-managed `ITenantInvitationDeliverySender` with sender id `smtp-email` by default
  - reads `Engine:MultiTenancy:Governance:SmtpInvitationDelivery` or code-first options for relay host, port, TLS posture, credentials, sender address, recipient metadata key, supported channels, deterministic message-id domain, templates, and safe custom headers
  - exposes `ISmtpInvitationDeliveryClient`, `SmtpInvitationDeliveryMessage`, and `SmtpInvitationDeliveryClientResult` so hosts can test or replace the default SMTP relay client without changing the core dispatcher
  - emits `Cephalon.MultiTenancy.Governance.SmtpDelivery` diagnostics while leaving SendGrid Mail Send API handoff to `Cephalon.MultiTenancy.Governance.SendGridDelivery` and leaving Mailgun, SES, Microsoft Graph, SMS, chat, CRM, identity-provider onboarding, bounce handling, provider polling, callback inboxes, and provider-specific reconciliation semantics to future provider packs or applications
- `Cephalon.Edge`
  - runtime services and capability activation for `EdgeNativeDelivery`
  - registers `IEdgeNodeCatalog` when the profile is selected
- `Cephalon.Edge.KubernetesGateway`
  - first provider-specific control-plane materializer over the shared `cell-based-architecture` traffic-automation baseline
  - projects Kubernetes Gateway API intent and can now overlay live Gateway API observation plus owned `HTTPRoute` apply-and-reconcile without moving cluster-specific ownership or reconcile policy into `Cephalon.Engine`
- `Cephalon.Edge.Traefik`
  - second provider-specific control-plane materializer over the shared `cell-based-architecture` traffic-automation baseline
  - projects deterministic Traefik `IngressRoute` intent and can now also overlay opt-in live observe-only CRD polling for route existence, dependency readiness, ownership, drift, and freshness back onto the same shared automation catalog without moving Traefik CRD semantics into `Cephalon.Engine`

These packages are also used as scaffold hints for the matching built-in technology profiles.
The phase-8 data packs are companion packages rather than technology packs, but they can now enrich `EventDrivenIntegration` truth by projecting staged outbox producers and application-managed inbox stores into the eventing runtime surfaces when both baselines are active.

## Runtime pattern

The expected layering is:

1. select technology profiles through `Engine:Technologies`
2. install companion packages that understand those profiles
3. register the companion package in startup
4. let module/package runtime behavior activate only when the technology is actually selected
5. let installed modules contribute pack-specific descriptors through the pack's contributor services instead of pushing every descriptor into host startup

The shared cell traffic-materialization seam now also carries one stable lifecycle vocabulary that
provider and edge packs can reuse instead of inventing provider-local status taxonomies:
`CellTrafficAutomationOwnershipStates`, `CellTrafficAutomationDependencyStates`,
`CellTrafficAutomationDriftStates`, and `CellTrafficAutomationLifecycleActions`. The shared runtime
catalog projects those values back onto `providerMaterialization.*`, `edgeMaterialization.*`, and
derived `materialization.*` metadata so requested, observed, conflicted, drifted, or dependency-missing
posture stays comparable across `Cephalon.Edge`, `Cephalon.Edge.KubernetesGateway`, and
`Cephalon.Edge.Traefik`.

Example:

```csharp
builder.AddCephalon(engine =>
{
    engine.AddAgentics(options =>
    {
        options.Tools.Add(new AgentToolDescriptor(
            id: "planner",
            displayName: "Planner",
            description: "Builds agent plans.",
            capabilityKeys: ["workflow.approval.request", "workflow.approval.record"],
            executionGraphId: "approval-flow",
            hostedExecutionId: "approval-pump"));
    });

    engine.AddRetrieval(options =>
    {
        options.Collections.Add(new KnowledgeCollectionDescriptor(
            id: "docs",
            displayName: "Docs",
            description: "Knowledge base for retrieval."));

        options.DefaultQueryLimit = 10;
        options.MaximumQueryLimit = 25;
        options.FreshnessStaleAfterSeconds = 3600;
    });

    engine.AddMultiTenancy(options =>
    {
        options.DefaultTenantId = "tenant-001";
        options.Tenants.Add(new TenantContext(
            tenantId: "tenant-001",
            tenantKey: "acme",
            displayName: "Acme",
            domains: ["acme.example.test"]));
    });

    engine.AddMultiTenancyGovernance(options =>
    {
        options.Memberships.Add(new TenantMembershipDescriptor(
            tenantId: "tenant-001",
            principalId: "user-001",
            displayName: "Acme Admin",
            roles: ["admin", "member"]));
    });

    engine.AddEventing(options =>
    {
        options.Channels.Add(new EventChannelDescriptor(
            id: "orders",
            displayName: "Orders",
            description: "Integration events for the order domain."));
    });

    engine.AddEdge(options =>
    {
        options.Nodes.Add(new EdgeNodeDescriptor(
            id: "storefront-edge",
            displayName: "Storefront Edge",
            description: "Regional node serving intermittently connected experiences."));
    });
});
```

```json
{
  "Engine": {
    "Technologies": ["AgenticWorkloads", "EventDrivenIntegration", "KnowledgeRetrieval", "MultiTenancy", "EdgeNativeDelivery"]
  }
}
```

## Authoring pattern

A technology pack should usually contain:

- `Configuration/`
  - options or defaults for the workload
- `Services/`
  - runtime contracts the app can consume
- `Modules/`
  - one or more modules that bridge the pack into Cephalon
- `Registration/`
  - `EngineBuilder` extensions for easy startup registration

Recommended contracts to use:

- `ITechnologyContributor`
  - when the pack adds new technology descriptors to the runtime catalog
- `ITechnologyServiceContributor`
  - when services should only activate for selected technologies
- `ITechnologyCapabilityContributor`
  - when capabilities should only activate for selected technologies

Shipped pack-specific extension points:

- `Cephalon.Agentics`
  - `IAgentToolContributor` and `IAgentToolRegistry` for descriptor contribution
  - `IAgentToolExecutor` for the managed execution implementation of one registered tool
  - `IAgentToolExecutionPolicy` for approval-required or deny decisions before an executor runs
  - `IAgentToolExecutionObserver` for audit, telemetry, or projection hooks after each run report is recorded
  - abstraction-level `Cephalon.Abstractions.Agentics.IAgentToolDispatcher`, `AgentToolExecutionRequest`, and `AgentToolExecutionResult` for host-neutral tool-run actions
  - abstraction-level `Cephalon.Abstractions.Agentics.IAgentToolRunCatalog` for runtime-state reads and `IAgentToolRunReporter` for controlled report writes
- `Cephalon.Retrieval`
  - `IKnowledgeCollectionContributor` and `IKnowledgeCollectionRegistry`
  - `IKnowledgeDocumentProvider` for module-owned source documents
  - abstraction-level `Cephalon.Abstractions.Retrieval.IKnowledgeIndexer` for manual reindex actions
  - abstraction-level `Cephalon.Abstractions.Retrieval.IKnowledgeQueryEngine`, `KnowledgeQueryRequest`, `KnowledgeQueryResult`, and `KnowledgeQueryMatch` for bounded host-neutral query actions through `POST /engine/knowledge-indexes/{collectionId}/queries`
  - abstraction-level `Cephalon.Abstractions.Retrieval.IKnowledgeIndexCatalog` for runtime-state reads through `/engine/knowledge-indexes` and `snapshot.KnowledgeIndexes`
  - `RetrievalOptions.EnableBackgroundReindexing`, `RunBackgroundReindexOnStartup`, `BackgroundReindexInitialDelaySeconds`, `BackgroundReindexIntervalSeconds`, and `BackgroundReindexCollectionIds` for the opt-in in-process background reindex scheduler over the same `IKnowledgeIndexer` path
- `Cephalon.MultiTenancy`
  - `ITenantResolver` for host-neutral tenant resolution
  - `ITenantContextAccessor` for ambient tenant context in the current async flow
  - `MultiTenancyRuntimeOptions` for configuration-driven tenants, domains, default tenant, and resolver enablement
- `Cephalon.MultiTenancy.Governance`
    - `ITenantMembershipContributor` and `ITenantMembershipRegistry` for module-contributed memberships
    - `ITenantMembershipCatalog` for the merged membership read model
    - `ITenantMembershipStore` for runtime tenant-membership state, with in-memory and file-backed baselines
    - `ITenantMembershipEvaluator` for the current Cephalon-managed membership evaluation path
    - `ITenantGovernanceActionContributor` and `ITenantGovernanceActionRegistry` for module-contributed approval/remediation actions
    - `ITenantGovernanceActionStore` for runtime approval/remediation action state, with in-memory and file-backed baselines
    - `ITenantGovernanceActionCatalog` for the merged action read model
    - `ITenantGovernanceActionDecider` for the current Cephalon-managed action decision path
    - `ITenantGovernanceActionWorkflow` for the current Cephalon-managed in-process action status-transition path
    - `ITenantInvitationContributor` and `ITenantInvitationRegistry` for module-contributed invitations
    - `ITenantInvitationCatalog` for the merged invitation read model
    - `ITenantInvitationStore` for runtime tenant-invitation state, with in-memory and file-backed baselines
    - `ITenantInvitationValidator` for the current Cephalon-managed invitation validation path
    - `ITenantInvitationDeliveryDispatcher`, `ITenantInvitationDeliverySender`, and `ITenantInvitationDeliveryRunCatalog` for the current host-agnostic invitation delivery dispatch path over registered sender extensions
    - `ITenantInvitationDeliveryRetryStore`, `ITenantInvitationDeliveryRetryRunner`, `ITenantInvitationDeliveryRetryExecutionCoordinationCatalog`, and `ITenantInvitationDeliveryRetryRuntimeCatalog` for the opt-in local retry queue, bounded retry path, process-local overlap coordination, and opt-in background retry scheduling state over retryable sender failures
    - `ITenantInvitationDeliveryStatusReconciler` for the current host-agnostic delivery status observation path over provider or receiver reports
    - `ITenantInvitationDeliveryStatusObservationStore` for normalized delivery-status observation history, with in-memory and file-backed baselines
    - `Cephalon.MultiTenancy.Governance.HttpDelivery` plus `AddCephalonHttpInvitationDelivery(...)` when a host wants the first-party signed/retrying HTTP webhook sender implementation for that dispatch path
    - `Cephalon.MultiTenancy.Governance.SendGridDelivery` plus `AddCephalonSendGridInvitationDelivery(...)` when a host wants the first-party SendGrid Mail Send API sender implementation for that dispatch path
    - `Cephalon.MultiTenancy.Governance.SmtpDelivery` plus `AddCephalonSmtpInvitationDelivery(...)` when a host wants the first-party SMTP relay sender implementation for that dispatch path
    - `ITenantAdministrationWorkflow` for the current Cephalon-managed host-driven membership and invitation administration path
    - `ITenantDomainOwnershipProofChallengeIssuer` for the current Cephalon-managed proof-challenge issuance path that creates expected proof values and publication hints
    - `ITenantDomainOwnershipProofPublicationPlanner` for the current Cephalon-managed proof-publication planning path that emits DNS TXT or HTTP file instructions
    - `ITenantDomainOwnershipProofEvaluator` for the current Cephalon-managed proof-evaluation path over application/provider-reported evidence
    - `ITenantDomainOwnershipHttpProofCollector` for the current Cephalon-managed on-demand HTTP file proof-collection path
    - `ITenantDomainOwnershipDnsTxtProofCollector` for the current Cephalon-managed on-demand DNS TXT proof-collection path when an explicit DNS-over-HTTPS resolver endpoint is configured
    - `ITenantDomainOwnershipProofVerificationRunner` for the current Cephalon-managed proof-verification orchestration path over challenge issuance, publication planning, reported-proof evaluation, optional HTTP file collection, and configured DNS TXT collection
- `MultiTenancyGovernanceOptions` for host-defined memberships, invitations, domain ownerships, governance actions, store paths, tenant-administration workflow enablement, invitation delivery dispatch/run-history enablement, invitation delivery retry queue enablement/max attempts/delay/batch limit/file durability/process-local execution coordination/background scheduling, invitation delivery status reconciliation enablement, delivery-status observation-store enablement/history/file durability, proof challenge defaults, proof publication planning, HTTP proof collection, DNS TXT proof collection resolver/timeout/size limits, proof verification runner orchestration, bounded proof polling enablement/batch limits, proof evaluation, and validation/evaluation/decision/workflow enablement
- `Cephalon.Eventing`
  - `IEventChannelContributor`, `IEventChannelRegistry`, `IEventSubscriptionExecutionBindingContributor`, `IEventSubscriptionExecutionBindingCatalog`, the abstraction-level `IEventSubscriptionExecutionReadinessCatalog`, and the abstraction-level `IEventPublicationRuntimeCatalog`
- `Cephalon.Edge`
  - `IEdgeNodeContributor` and `IEdgeNodeRegistry`

Those contributor interfaces are the preferred way for installed modules to add descriptors into a selected technology pack. Project-level code can still replace the final catalog service through DI when it needs full control.

For `Cephalon.Agentics`, `AgentToolDescriptor` can now also link back to:

- published capability keys through `capabilityKeys`
- one execution graph through `executionGraphId`
- one hosted execution through `hostedExecutionId`

That keeps AI-facing tool metadata anchored in the same module, capability, execution-graph, hosted-execution, and runtime-story contracts the engine already exposes.

When execution is enabled, `Cephalon.Agentics` also owns one narrow tool-dispatch loop. The dispatcher is still host-agnostic: modules contribute descriptors through `IAgentToolContributor`, register exactly one `IAgentToolExecutor` for each executable tool id, and optionally add policy or observer services. Runtime surfaces then report whether each tool is `cephalon-managed`, `awaiting-executor`, or `not-configured`, plus retry policy, idempotency policy, latest run outcome, retry-scheduled counts, retry-pending posture, approval-required posture, duplicate-completed posture, terminal-failure posture, and totals. When `ExecutionMaxAttempts` is greater than `1`, failed executor attempts are retried inside the same process and reported as `retry-scheduled`; the default remains a single attempt. When `EnableExecutionIdempotency` is enabled, duplicate completed `toolId + runId` executions observed inside the configured process-local retention window are reported as `skipped` with `idempotencyPolicy = completed-run`, `idempotencyDurability = none`, and `idempotencyScope = process-local`; the default preserves repeated execution behavior. The direct run-state read seam and the bounded dispatch action contract now live in `Cephalon.Abstractions.Agentics`, so ASP.NET Core hosts and tooling can read `/engine/agent-tool-runs`, `/engine/agent-tool-runs/retry-pending`, `/engine/agent-tool-runs/idempotency-duplicates`, `/engine/agent-tool-runs/approval-required`, `/engine/agent-tool-runs/terminal-failures`, `/engine/agent-tool-runs/{runId}`, `/engine/agent-tool-runs/by-tool/{toolId}`, trigger `POST /engine/agent-tools/{toolId}/runs`, and read `snapshot.AgentToolRuns` without depending on `Cephalon.Agentics` implementation types. That is the boundary of the current managed proof; broader autonomous planning, memory stores, durable approval workflows, durable retry queues, durable inboxes, dead-letter systems, cross-node exactly-once delivery, distributed schedulers, or provider-specific AI orchestration remain future companion work unless another package explicitly owns them.

When ingestion and querying are enabled, `Cephalon.Retrieval` now owns one narrow lexical retrieval loop. Modules still own the source material through `IKnowledgeDocumentProvider`, while the pack owns indexing, bounded query execution, index state, freshness reporting, manual operator reindexing, and an opt-in in-process background reindex scheduler for registered collections. Runtime surfaces report `indexingOwnership`, `queryOwnership`, `backgroundReindexingOwnership`, `backgroundReindexingScheduled`, provider readiness, latest index outcome, document count, query count, freshness state, scheduler scope/timing metadata, and a query fingerprint rather than raw query text. ASP.NET Core hosts can request the same bounded query and indexer paths through `POST /engine/knowledge-indexes/{collectionId}/queries` and `POST /engine/knowledge-indexes/{collectionId}/reindex` without referencing retrieval implementation types. That is the boundary of the current managed proof; vector databases, embeddings, distributed indexes, durable search clusters, rerankers, provider-specific semantic search, distributed scheduler coordination, and leader-election semantics remain future companion work unless another package explicitly owns them.

When multi-tenancy is selected, `Cephalon.MultiTenancy` owns one narrow tenant-resolution loop. The base package resolves configured tenant ids, tenant keys, host names, defaults, and single-tenant fallback through `ITenantResolver`, then exposes the ambient answer through `ITenantContextAccessor` and the `tenant-resolution` surface. `Cephalon.MultiTenancy.Governance` now owns concrete companion proofs beside that base package: modules, hosts, or `ITenantMembershipStore` contribute `TenantMembershipDescriptor` values for `ITenantMembershipCatalog` plus `ITenantMembershipEvaluator`, modules, hosts, or `ITenantInvitationStore` contribute `TenantInvitationDescriptor` values for `ITenantInvitationCatalog` plus `ITenantInvitationValidator`, `ITenantInvitationDeliveryDispatcher` dispatches pending invitations through registered `ITenantInvitationDeliverySender` extensions and records attempts through `ITenantInvitationDeliveryRunCatalog`, `ITenantInvitationDeliveryRetryStore` can retain retryable sender failures for bounded `ITenantInvitationDeliveryRetryRunner` passes, `ITenantInvitationDeliveryRetryExecutionCoordinationCatalog` reports the process-local skip-overlap guard that prevents overlapping manual/background retry passes inside one host process, `ITenantInvitationDeliveryRetryRuntimeCatalog` reports opt-in automatic background scheduling hosted-service state when `EnableInvitationDeliveryRetryBackgroundScheduling` is enabled, `ITenantInvitationDeliveryStatusReconciler` records provider or receiver delivery status observations against the same invitation store, `ITenantInvitationDeliveryStatusObservationStore` retains those normalized observation outcomes for audit/operator review, `ITenantAdministrationWorkflow` applies explicit host-driven membership and invitation administration commands through those same stores, modules, hosts, or `ITenantDomainOwnershipStore` contribute `TenantDomainOwnershipDescriptor` values for `ITenantDomainOwnershipCatalog` plus `ITenantDomainOwnershipValidator`, `ITenantDomainOwnershipVerificationWorkflow` can request, verify, reject, suspend, or expire runtime domain ownership state through `ITenantDomainOwnershipStore`, `ITenantDomainOwnershipProofChallengeIssuer` can issue expected proof values plus DNS TXT or HTTP file publication hints into that same store, `ITenantDomainOwnershipProofPublicationPlanner` can turn issued challenge metadata into DNS TXT or HTTP file publication instructions and optional plan metadata, `ITenantDomainOwnershipHttpProofPublisher` can materialize HTTP file proof publication state for host adapters, `ITenantDomainOwnershipProofEvaluator` can verify or reject reported proof evidence through that same workflow, `ITenantDomainOwnershipHttpProofCollector` can collect bounded host-matched HTTPS HTTP file proof content and feed it into that evaluator, `ITenantDomainOwnershipDnsTxtProofCollector` can collect bounded DNS TXT proof content through an explicit HTTPS DNS-over-HTTPS resolver and feed only matching TXT values into that evaluator, `ITenantDomainOwnershipProofVerificationRunner` can orchestrate the challenge, publication-plan, observed-proof evaluation, optional HTTP collection, and configured DNS TXT collection paths through one entry point, `ITenantDomainOwnershipProofPollingRunner` can run bounded on-demand polling over pending or rejected HTTP/DNS declarations and delegate each attempt to that verifier, `ITenantDomainOwnershipProofPollingRuntimeCatalog` reports opt-in automatic background polling hosted-service state when `EnableDomainOwnershipProofBackgroundPolling` is enabled, and `TenantGovernanceActionDescriptor` values feed `ITenantGovernanceActionCatalog` plus `ITenantGovernanceActionDecider`, while `ITenantGovernanceActionWorkflow` can create and transition runtime action state through `ITenantGovernanceActionStore`. The membership, invitation, invitation-delivery retry, delivery-status observation, domain-ownership, and action stores are in-memory by default and become Cephalon-managed local JSON durable stores when `MembershipStoreFilePath`, `InvitationStoreFilePath`, `InvitationDeliveryRetryQueueFilePath`, `InvitationDeliveryStatusObservationStoreFilePath`, `DomainOwnershipStoreFilePath`, or `GovernanceActionStoreFilePath` are configured. ASP.NET Core hosts can install `Cephalon.MultiTenancy.Governance.AspNetCore`, map `MapCephalonTenantDomainOwnershipHttpProofs()` to serve published HTTP file proofs from that catalog, map `MapCephalonTenantAdministrationCommands()` to expose a fail-closed command endpoint over `ITenantAdministrationWorkflow`, map `MapCephalonTenantInvitationDeliveryDispatches()` to expose a fail-closed dispatch endpoint over `ITenantInvitationDeliveryDispatcher`, and map `MapCephalonTenantInvitationDeliveryStatusCallbacks()` to expose a fail-closed normalized callback endpoint over `ITenantInvitationDeliveryStatusReconciler`; that callback endpoint can also require provider-neutral Cephalon HMAC signatures over the exact normalized JSON body before reconciliation and reject duplicate signed requests inside a bounded process-local replay window. Hosts that want first-party outbound senders can install `Cephalon.MultiTenancy.Governance.HttpDelivery`, `Cephalon.MultiTenancy.Governance.SmtpDelivery`, or `Cephalon.MultiTenancy.Governance.SendGridDelivery` without changing the core dispatcher. ASP.NET Core hosts that want first-party SendGrid callback translation can also install `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore` and map `MapCephalonSendGridInvitationDeliveryStatusCallbacks()` so SendGrid Event Webhook arrays are translated into the same reconciler through a separate provider surface. Actual DNS proof publication, provider-backed proof publication or mutation, remediation execution beyond state transitions, distributed or provider-backed governance storage, Mailgun/SES/Microsoft Graph or other non-SendGrid provider-specific email API senders, SMS/chat/CRM/identity-provider invitation senders, distributed retry queues, cross-node delivery leases, exactly-once delivery, provider-specific or distributed callback inboxes, cross-node callback replay protection, non-SendGrid provider-specific callback payload translation, SendGrid and other provider-specific callback signature verification, provider polling, identity-provider synchronization, public onboarding, and tenant-admin UI/backoffice flows remain outside the current managed proof until the governance package or provider packs own those paths explicitly.

ASP.NET Core hosts can also map `MapCephalonTenantInvitationDeliveryStatusObservations()` to expose bounded/filterable `GET /engine/tenant-invitations/delivery-status/observations` reads over `ITenantInvitationDeliveryStatusObservationStore`; that route is an operator/audit read surface over normalized observations, not a provider-specific callback inbox, provider poller, or distributed replay ledger. The dispatch endpoint records safe adapter metadata before invoking the core dispatcher, but it still depends on registered sender extensions for actual provider delivery and does not create distributed retry queues, cross-node leases, exactly-once delivery, or provider-specific notification semantics.

Runtime introspection contract:

- `ITechnologyRuntimeContributor`
  - used by packs to project their active runtime surface into a transport-neutral snapshot
- `ITechnologyRuntimeCatalog`
  - host-agnostic abstraction for reading the merged runtime surface set in code
- `IRuntimeIntrospectionSnapshotProvider`
  - engine-level abstraction for reading one operator-facing snapshot that combines the runtime manifest, runtime status, and active technology-pack surfaces
- `GET /engine/technology-surfaces`
- returns the active pack surfaces and the merged entries visible to the runtime after host options, module contributors, and in-process runtime transitions have been applied; agentic tools now also surface linked capability keys, live execution-graph and hosted-execution state, managed execution readiness, and latest run-state truth when those links or reports exist, while the same reported runs are also available through `snapshot.AgentToolRuns`; retrieval collections now also surface provider readiness, indexing/query/background-reindexing ownership, per-collection scheduler inclusion, freshness state, document counts, query counts, latest run outcomes, scheduler scope/timing metadata, and query fingerprints when indexed, scheduled, or queried; multi-tenancy now surfaces the active tenant-resolution answer, governance companion boundaries, and the concrete `tenant-memberships`, `tenant-invitations`, `tenant-administration`, `tenant-domain-ownership`, plus `tenant-governance-actions` companion proofs, including membership-store, invitation-store, invitation delivery dispatch/sender/run-state posture, delivery status reconciliation/latest status posture, delivery-status observation store posture/count/latest observation, tenant-administration command ownership, domain-ownership-store, proof-challenge issuance, proof-publication planning, proof-evaluation, HTTP proof collection, configured DNS TXT proof collection, proof-verification runner, proof-polling runner, opt-in background proof-polling run state, and action-store durability metadata, when the governance pack is installed; ASP.NET Core governance adapters also surface `tenant-administration-http-endpoints` route, mapping, and authorization posture, `tenant-invitation-delivery-http-endpoints` dispatch route/auth/contract posture, plus `tenant-invitation-delivery-status-http-endpoints` signed-callback signature/replay posture and bounded observation-read route/auth/limit posture when registered
- `GET /engine/snapshot`
  - returns the broader runtime introspection snapshot when operators need manifest, runtime status, and technology-pack surfaces in one payload

## Guardrails

- keep packs additive; do not mutate engine core behavior globally
- prefer activating services/capabilities from `TechnologySelection` instead of branching in hosts
- prefer pack-specific contributor services over hardcoded host-owned descriptor lists when extending a shipped pack
- prefer `ITechnologyRuntimeContributor` when a pack needs an operator-facing runtime snapshot instead of inventing host-specific ad-hoc endpoints
- keep built-in technology profiles lightweight; move reusable runtime behavior into packs
- only create a new blueprint when project shape changes materially
- only add a new built-in technology profile when validation, guidance, or scaffold/package hints are distinct enough to justify it
