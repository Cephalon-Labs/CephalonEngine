# Cephalon.MultiTenancy.Governance

`Cephalon.MultiTenancy.Governance` is the tenant-governance companion package for Cephalon multi-tenancy workloads.

## What it owns

- tenant membership descriptors, registries, catalogs, and contributor contracts
- host-defined and module-contributed membership merge behavior
- one Cephalon-managed tenant-membership evaluation baseline
- one Cephalon-managed tenant-membership store baseline, with an in-memory default and opt-in local JSON durability
- tenant invitation descriptors, registries, catalogs, and contributor contracts
- host-defined and module-contributed invitation merge behavior
- one Cephalon-managed tenant-invitation store baseline, with an in-memory default and opt-in local JSON durability
- one Cephalon-managed tenant-invitation validation baseline
- one Cephalon-managed tenant-invitation delivery dispatch baseline that invokes registered sender extensions and records delivery outcome truth
- one opt-in Cephalon-managed tenant-invitation delivery retry queue baseline, with an in-memory default, opt-in local JSON durability, and a bounded retry runner for retryable sender failures
- one opt-in Cephalon-managed automatic tenant-invitation delivery retry scheduling baseline that schedules the bounded retry runner, reports latest run state, and stays disabled by default to avoid surprise delivery attempts
- one Cephalon-managed tenant-invitation delivery status reconciliation baseline that records provider or receiver status observations while provider-specific webhook translation, provider signature verification, and provider polling stay in provider packs or applications
- one Cephalon-managed tenant-invitation delivery status observation store baseline, with an in-memory default and opt-in local JSON durability for normalized reconciliation observations
- one provider-extension boundary for real invitation senders and callback translators, with `Cephalon.MultiTenancy.Governance.HttpDelivery` supplying the optional first-party HTTP webhook sender, `Cephalon.MultiTenancy.Governance.SmtpDelivery` supplying the optional first-party SMTP relay sender, `Cephalon.MultiTenancy.Governance.SendGridDelivery` supplying the optional first-party SendGrid Mail Send API sender, `Cephalon.MultiTenancy.Governance.MailgunDelivery` supplying the optional first-party Mailgun Messages API sender, `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore` supplying the optional first-party SendGrid Event Webhook translator for ASP.NET Core hosts, and `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore` supplying the optional first-party Mailgun webhook translator for ASP.NET Core hosts
- one Cephalon-managed host-driven tenant-administration workflow baseline over membership and invitation stores
- tenant-domain ownership descriptors, registries, catalogs, and contributor contracts
- host-defined and module-contributed domain-ownership merge behavior
- one Cephalon-managed tenant-domain ownership store baseline, with an in-memory default and opt-in local JSON durability
- one Cephalon-managed declared tenant-domain ownership validation baseline
- one Cephalon-managed in-process tenant-domain ownership verification workflow baseline
- one Cephalon-managed tenant-domain ownership proof-challenge issuance baseline
- one Cephalon-managed tenant-domain ownership proof-publication planning baseline for DNS TXT and HTTP file instructions
- one Cephalon-managed tenant-domain ownership HTTP file proof-publication baseline that materializes published HTTP proof state for host adapters
- one Cephalon-managed tenant-domain ownership proof-evaluation baseline over application/provider-reported evidence
- one Cephalon-managed on-demand tenant-domain ownership HTTP file proof-collection baseline
- one Cephalon-managed on-demand tenant-domain ownership DNS TXT proof-collection baseline when a DNS-over-HTTPS resolver endpoint is explicitly configured
- one Cephalon-managed tenant-domain ownership proof-verification runner baseline that orchestrates challenge issuance, publication planning, reported-proof evaluation, optional HTTP file collection, and configured DNS TXT collection
- one Cephalon-managed bounded on-demand tenant-domain ownership proof-polling runner baseline that scans pending or rejected HTTP/DNS declarations and delegates each attempt to the proof-verification runner
- one opt-in Cephalon-managed automatic tenant-domain ownership proof-polling hosted-service baseline that schedules the bounded polling runner, reports latest run state, and stays disabled by default to avoid surprise network I/O
- tenant-governance action descriptors, registries, catalogs, and contributor contracts
- host-defined and module-contributed approval/remediation action merge behavior
- one Cephalon-managed tenant-governance action decision baseline
- one Cephalon-managed in-process tenant-governance action workflow baseline
- one Cephalon-managed tenant-governance action store baseline, with an in-memory default and opt-in local JSON durability
- `tenancy.membership.catalog`, `tenancy.membership.store`, and `tenancy.membership.evaluation` capabilities when the `MultiTenancy` technology is active
- `tenancy.invitation.catalog`, `tenancy.invitation.store`, `tenancy.invitation.validation`, `tenancy.invitation.delivery-dispatch`, `tenancy.invitation.delivery-retry-queue`, `tenancy.invitation.delivery-retry-background-scheduling`, `tenancy.invitation.delivery-status-reconciliation`, and `tenancy.invitation.delivery-status-observation-store` capabilities when the `MultiTenancy` technology is active and the related runtime path is enabled
- `tenancy.administration.workflow` capability when the `MultiTenancy` technology is active and the tenant-administration workflow is enabled
- `tenancy.domain-ownership.catalog`, `tenancy.domain-ownership.store`, `tenancy.domain-ownership.validation`, `tenancy.domain-ownership.workflow`, `tenancy.domain-ownership.proof-challenge`, `tenancy.domain-ownership.proof-publication-plan`, `tenancy.domain-ownership.http-proof-publication`, `tenancy.domain-ownership.proof-evaluation`, `tenancy.domain-ownership.http-proof-collection`, `tenancy.domain-ownership.dns-txt-proof-collection`, `tenancy.domain-ownership.proof-verification-runner`, `tenancy.domain-ownership.proof-polling-runner`, and `tenancy.domain-ownership.proof-background-polling` capabilities when the `MultiTenancy` technology is active and the related runtime path is enabled
- `tenancy.governance-action.catalog`, `tenancy.governance-action.store`, `tenancy.governance-action.decision`, and `tenancy.governance-action.workflow` capabilities when the `MultiTenancy` technology is active
- the `tenant-memberships` technology runtime surface under `multi-tenancy`
- the `tenant-invitations` technology runtime surface under `multi-tenancy`
- the `tenant-administration` technology runtime surface under `multi-tenancy`
- the `tenant-domain-ownership` technology runtime surface under `multi-tenancy`
- the `tenant-governance-actions` technology runtime surface under `multi-tenancy`
- stable governance diagnostics for allowed and denied membership evaluation, invitation validation, invitation delivery dispatch, invitation delivery retry background scheduling, invitation delivery status reconciliation, tenant-administration workflow commands, domain-ownership validation, domain-ownership verification workflow transitions, domain-ownership proof challenge issuance, domain-ownership proof publication planning, domain-ownership HTTP proof publication, domain-ownership proof evaluation, domain-ownership HTTP proof collection, domain-ownership DNS TXT proof collection, domain-ownership proof verification runs, on-demand and automatic background domain-ownership proof polling passes, domain-ownership store persistence, governance-action decisions, governance-action workflow transitions, and governance-action store persistence
- a separate runtime ownership boundary from the base `Cephalon.MultiTenancy` tenant-resolution pack

## Main surfaces

- `Configuration/MultiTenancyGovernanceOptions.cs`
- `Modules/MultiTenancyGovernanceModule.cs`
- `Registration/MultiTenancyGovernanceEngineBuilderExtensions.cs`
- `Services/TenantMembershipDescriptor.cs`
- `Services/ITenantMembershipContributor.cs`
- `Services/ITenantMembershipRegistry.cs`
- `Services/ITenantMembershipCatalog.cs`
- `Services/ITenantMembershipStore.cs`
- `Services/ITenantMembershipEvaluator.cs`
- `Services/TenantMembershipEvaluationRequest.cs`
- `Services/TenantMembershipEvaluationResult.cs`
- `Services/TenantMembershipStatuses.cs`
- `Services/TenantMembershipEvaluationOutcomes.cs`
- `Services/TenantInvitationDescriptor.cs`
- `Services/ITenantInvitationContributor.cs`
- `Services/ITenantInvitationRegistry.cs`
- `Services/ITenantInvitationCatalog.cs`
- `Services/ITenantInvitationStore.cs`
- `Services/ITenantInvitationValidator.cs`
- `Services/ITenantInvitationDeliveryDispatcher.cs`
- `Services/ITenantInvitationDeliverySender.cs`
- `Services/ITenantInvitationDeliveryRetryStore.cs`
- `Services/ITenantInvitationDeliveryRetryRunner.cs`
- `Services/ITenantInvitationDeliveryRetryRuntimeCatalog.cs`
- `Services/ITenantInvitationDeliveryStatusReconciler.cs`
- `Services/ITenantInvitationDeliveryStatusObservationStore.cs`
- `Services/ITenantInvitationDeliveryRunCatalog.cs`
- `Services/TenantInvitationValidationRequest.cs`
- `Services/TenantInvitationValidationResult.cs`
- `Services/TenantInvitationStatuses.cs`
- `Services/TenantInvitationValidationOutcomes.cs`
- `Services/TenantInvitationDeliveryRequest.cs`
- `Services/TenantInvitationDeliveryResult.cs`
- `Services/TenantInvitationDeliveryContext.cs`
- `Services/TenantInvitationDeliverySenderResult.cs`
- `Services/TenantInvitationDeliveryRunDescriptor.cs`
- `Services/TenantInvitationDeliveryRetryDescriptor.cs`
- `Services/TenantInvitationDeliveryRetryRequest.cs`
- `Services/TenantInvitationDeliveryRetryResult.cs`
- `Services/TenantInvitationDeliveryRetryRuntimeSnapshot.cs`
- `Services/TenantInvitationDeliveryRetryOutcomes.cs`
- `Services/TenantInvitationDeliveryRetryStatuses.cs`
- `Services/TenantInvitationDeliveryOutcomes.cs`
- `Services/TenantInvitationDeliveryStatuses.cs`
- `Services/TenantInvitationDeliveryStatusReconciliationOutcomes.cs`
- `Services/TenantInvitationDeliveryStatusReconciliationRequest.cs`
- `Services/TenantInvitationDeliveryStatusReconciliationResult.cs`
- `Services/TenantInvitationDeliveryStatusObservationDescriptor.cs`
- `Services/TenantInvitationDeliveryMetadataKeys.cs`
- `Services/ITenantAdministrationWorkflow.cs`
- `Services/TenantAdministrationWorkflowRequest.cs`
- `Services/TenantAdministrationWorkflowResult.cs`
- `Services/TenantAdministrationWorkflowCommands.cs`
- `Services/TenantAdministrationWorkflowOutcomes.cs`
- `Services/TenantAdministrationWorkflowMetadataKeys.cs`
- `Services/TenantDomainOwnershipDescriptor.cs`
- `Services/ITenantDomainOwnershipContributor.cs`
- `Services/ITenantDomainOwnershipRegistry.cs`
- `Services/ITenantDomainOwnershipCatalog.cs`
- `Services/ITenantDomainOwnershipStore.cs`
- `Services/ITenantDomainOwnershipValidator.cs`
- `Services/ITenantDomainOwnershipVerificationWorkflow.cs`
- `Services/ITenantDomainOwnershipProofChallengeIssuer.cs`
- `Services/ITenantDomainOwnershipProofPublicationPlanner.cs`
- `Services/ITenantDomainOwnershipHttpProofPublisher.cs`
- `Services/ITenantDomainOwnershipHttpProofPublicationCatalog.cs`
- `Services/ITenantDomainOwnershipProofEvaluator.cs`
- `Services/ITenantDomainOwnershipHttpProofCollector.cs`
- `Services/ITenantDomainOwnershipDnsTxtProofCollector.cs`
- `Services/TenantDomainOwnershipValidationRequest.cs`
- `Services/TenantDomainOwnershipValidationResult.cs`
- `Services/TenantDomainOwnershipVerificationWorkflowRequest.cs`
- `Services/TenantDomainOwnershipVerificationWorkflowResult.cs`
- `Services/TenantDomainOwnershipVerificationWorkflowCommands.cs`
- `Services/TenantDomainOwnershipVerificationWorkflowOutcomes.cs`
- `Services/TenantDomainOwnershipProofChallengeRequest.cs`
- `Services/TenantDomainOwnershipProofChallengeResult.cs`
- `Services/TenantDomainOwnershipProofChallengeOutcomes.cs`
- `Services/TenantDomainOwnershipProofChallengeMetadataKeys.cs`
- `Services/TenantDomainOwnershipProofPublicationPlanRequest.cs`
- `Services/TenantDomainOwnershipProofPublicationPlanResult.cs`
- `Services/TenantDomainOwnershipProofPublicationPlanOutcomes.cs`
- `Services/TenantDomainOwnershipProofPublicationPlanMetadataKeys.cs`
- `Services/TenantDomainOwnershipHttpProofPublicationRequest.cs`
- `Services/TenantDomainOwnershipHttpProofPublicationResult.cs`
- `Services/TenantDomainOwnershipHttpProofPublicationDescriptor.cs`
- `Services/TenantDomainOwnershipHttpProofPublicationOutcomes.cs`
- `Services/TenantDomainOwnershipHttpProofPublicationMetadataKeys.cs`
- `Services/TenantDomainOwnershipProofEvaluationRequest.cs`
- `Services/TenantDomainOwnershipProofEvaluationResult.cs`
- `Services/TenantDomainOwnershipProofEvaluationOutcomes.cs`
- `Services/TenantDomainOwnershipProofMetadataKeys.cs`
- `Services/TenantDomainOwnershipHttpProofCollectionRequest.cs`
- `Services/TenantDomainOwnershipHttpProofCollectionResult.cs`
- `Services/TenantDomainOwnershipHttpProofCollectionOutcomes.cs`
- `Services/TenantDomainOwnershipHttpProofCollectionMetadataKeys.cs`
- `Services/TenantDomainOwnershipDnsTxtProofCollectionRequest.cs`
- `Services/TenantDomainOwnershipDnsTxtProofCollectionResult.cs`
- `Services/TenantDomainOwnershipDnsTxtProofCollectionOutcomes.cs`
- `Services/TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.cs`
- `Services/ITenantDomainOwnershipProofVerificationRunner.cs`
- `Services/TenantDomainOwnershipProofVerificationRequest.cs`
- `Services/TenantDomainOwnershipProofVerificationResult.cs`
- `Services/TenantDomainOwnershipProofVerificationOutcomes.cs`
- `Services/TenantDomainOwnershipProofVerificationMetadataKeys.cs`
- `Services/ITenantDomainOwnershipProofPollingRunner.cs`
- `Services/ITenantDomainOwnershipProofPollingRuntimeCatalog.cs`
- `Services/TenantDomainOwnershipProofPollingRequest.cs`
- `Services/TenantDomainOwnershipProofPollingResult.cs`
- `Services/TenantDomainOwnershipProofPollingRuntimeSnapshot.cs`
- `Services/TenantDomainOwnershipProofPollingOutcomes.cs`
- `Services/TenantDomainOwnershipProofPollingMetadataKeys.cs`
- `Services/TenantDomainOwnershipStatuses.cs`
- `Services/TenantDomainVerificationMethods.cs`
- `Services/TenantDomainOwnershipValidationOutcomes.cs`
- `Services/TenantGovernanceActionDescriptor.cs`
- `Services/ITenantGovernanceActionContributor.cs`
- `Services/ITenantGovernanceActionRegistry.cs`
- `Services/ITenantGovernanceActionCatalog.cs`
- `Services/ITenantGovernanceActionDecider.cs`
- `Services/ITenantGovernanceActionStore.cs`
- `Services/ITenantGovernanceActionWorkflow.cs`
- `Services/TenantGovernanceActionDecisionRequest.cs`
- `Services/TenantGovernanceActionDecisionResult.cs`
- `Services/TenantGovernanceActionWorkflowRequest.cs`
- `Services/TenantGovernanceActionWorkflowResult.cs`
- `Services/TenantGovernanceActionWorkflowCommands.cs`
- `Services/TenantGovernanceActionWorkflowOutcomes.cs`
- `Services/TenantGovernanceActionStatuses.cs`
- `Services/TenantGovernanceActionKinds.cs`
- `Services/TenantGovernanceActionDecisionOutcomes.cs`
- `Services/MultiTenancyGovernanceRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyGovernanceInvitationRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyGovernanceAdministrationRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyGovernanceDomainRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyGovernanceActionRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyGovernanceDiagnosticsConventionContributor.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

The base `Cephalon.MultiTenancy` package intentionally stays focused on tenant resolution and ambient tenant context. This companion package owns the first concrete governance runtime proofs beside that base package: a tenant membership catalog plus deterministic evaluator, an opt-in durable tenant-membership store, a tenant invitation catalog plus deterministic validator, an opt-in durable tenant-invitation store, a host-agnostic invitation delivery dispatcher that invokes registered sender extensions and records outcome truth, an opt-in local tenant-invitation delivery retry store plus bounded retry runner for retryable sender failures, an opt-in background invitation retry hosted service that schedules the same bounded runner and reports latest run state, a host-agnostic invitation delivery status reconciler that records provider or receiver status observations against the same invitation store, an opt-in durable delivery-status observation store for normalized reconciliation observations, a host-driven tenant-administration workflow that issues, accepts, revokes, or expires invitations and grants, suspends, or expires memberships over the same stores, a declared tenant-domain ownership catalog plus deterministic validator, an opt-in durable tenant-domain ownership store, an in-process tenant-domain ownership verification workflow that can request, verify, reject, suspend, or expire domain ownership declarations, a tenant-domain ownership proof challenge issuer that creates expected DNS TXT or HTTP file proof values for later publication, a tenant-domain ownership proof publication planner that turns those expected values into concrete DNS TXT or HTTP file instructions, an HTTP file proof publisher and publication catalog that materialize published HTTP proof state for host adapters, a tenant-domain ownership proof evaluator that can verify or reject application/provider-reported evidence through that workflow, an on-demand HTTP file proof collector that fetches and evaluates HTTP proof content safely, an on-demand DNS TXT proof collector that queries an explicitly configured DNS-over-HTTPS resolver and evaluates matching TXT proof safely, a proof-verification runner that composes challenge, publication-plan, evaluation, HTTP collection, and configured DNS TXT collection paths through one low-glue entry point, a bounded proof-polling runner that scans pending or rejected HTTP/DNS declarations and delegates each attempt to the verification runner, an opt-in background proof-polling hosted service that schedules the same bounded runner and reports latest run state, an approval/remediation action catalog plus deterministic decider, an in-process action workflow that can request, approve, reject, require remediation, mark remediated, or expire governance actions, and an opt-in durable action store for workflow-created runtime action state.

Memberships can come from the runtime `ITenantMembershipStore`, host options, or installed module contributors through `ITenantMembershipContributor`. The catalog de-duplicates memberships by tenant id, principal kind, and principal id, and the evaluator uses that same tenant/principal-kind/principal-id boundary so user, group, service, and organization identifiers cannot bleed roles across kinds. Evaluation checks active, suspended, expired, and missing-role states without leaking host-specific identity APIs into the engine core.

The default `ITenantMembershipStore` is in-memory and process-local. Set `MultiTenancyGovernanceOptions.MembershipStoreFilePath` to opt into the built-in JSON file store when a host wants Cephalon-managed tenant membership state to survive process restarts. Stored memberships merge into the same catalog used by `ITenantMembershipEvaluator`, so runtime `Upsert(...)` calls become observable through catalog reads and the `tenant-memberships` runtime surface without requiring the consumer app to rebuild descriptors.

Invitations can come from the runtime `ITenantInvitationStore`, host options, or installed module contributors through `ITenantInvitationContributor`. The catalog de-duplicates invitations by tenant id and invitation id, while the validator checks pending, accepted, revoked, expired, invitee-mismatch, and missing-role states. Invitee matching uses invitee kind and invitee id so a service invitation cannot be reused as a user invitation.

The default `ITenantInvitationStore` is in-memory and process-local. Set `MultiTenancyGovernanceOptions.InvitationStoreFilePath` to opt into the built-in JSON file store when a host wants Cephalon-managed tenant invitation state to survive process restarts. Stored invitations merge into the same catalog used by `ITenantInvitationValidator`, so runtime `Upsert(...)` calls become observable through catalog reads and the `tenant-invitations` runtime surface without requiring the consumer app to rebuild descriptors.

`ITenantInvitationDeliveryDispatcher` owns the host-agnostic invitation-delivery dispatch path. It resolves the same merged invitation catalog, requires pending and unexpired invitation state, invokes a registered `ITenantInvitationDeliverySender`, records outcome metadata such as `lastDeliveryOutcome`, sender id, provider message id, channel, source, actor, correlation id, dispatch ownership, and external delivery ownership through `ITenantInvitationStore`, and reports attempts through `ITenantInvitationDeliveryRunCatalog`. When no sender is registered it returns `sender-not-configured`, records that truth when possible, and keeps external delivery `application-managed` instead of pretending the engine sent email, SMS, chat, or identity-provider invitations. Consumer apps can register their own senders, the optional `Cephalon.MultiTenancy.Governance.HttpDelivery` package provides a first-party `http-webhook` sender that posts a bounded JSON payload to a configured HTTP or HTTPS endpoint, the optional `Cephalon.MultiTenancy.Governance.SmtpDelivery` package provides a first-party `smtp-email` sender that prepares a templated email and hands it to a configured SMTP relay through a replaceable client seam, the optional `Cephalon.MultiTenancy.Governance.SendGridDelivery` package provides a first-party `sendgrid-email` sender that posts a Mail Send API payload to SendGrid through a replaceable client seam, and the optional `Cephalon.MultiTenancy.Governance.MailgunDelivery` package provides a first-party `mailgun-email` sender that posts a multipart Messages API payload to Mailgun through a replaceable client seam. ASP.NET Core hosts that want a real operator action endpoint can install `Cephalon.MultiTenancy.Governance.AspNetCore` and explicitly map `MapCephalonTenantInvitationDeliveryDispatches()` over this dispatcher; that adapter keeps authorization fail-closed and reports route/auth/contract posture through `tenant-invitation-delivery-http-endpoints`. SES, Microsoft Graph, SMS, chat, CRM, or identity-provider senders can still land as future companion packs without moving provider APIs into the governance core.

`ITenantInvitationDeliveryRetryStore` and `ITenantInvitationDeliveryRetryRunner` own the opt-in retry queue lane for retryable sender failures. Set `MultiTenancyGovernanceOptions.EnableInvitationDeliveryRetryQueue = true` to have `sender-failed` dispatch outcomes queued with retry id, tenant/invitation/channel/sender boundary, attempt count, max attempts, next-attempt timestamp, latest outcome/reason, source/actor/correlation metadata, and retry-store ownership metadata. The default store is in-memory and process-local; set `InvitationDeliveryRetryQueueFilePath` to opt into local JSON durability. `RetryPendingAsync(...)` runs a bounded pass over pending due entries and delegates each attempt back through `ITenantInvitationDeliveryDispatcher`, then removes successfully dispatched entries or marks repeated sender failures as pending/exhausted. `EnableInvitationDeliveryRetryExecutionCoordination` is enabled by default when the retry runner is effectively enabled; it wraps manual and background retry passes in a process-local skip-overlap guard so an overlapping pass returns `already-running` without dispatching entries or mutating the retry queue. `ITenantInvitationDeliveryRetryExecutionCoordinationCatalog` reports that guard's enabled state, ownership, scope, mode, in-progress state, accepted/skipped/completed counts, latest timestamps, latest outcome, and latest error through the `tenant-invitations` runtime surface. `ITenantInvitationDeliveryRetryRuntimeCatalog` reports the opt-in automatic background retry hosted-service posture. Automatic scheduling is disabled by default through `MultiTenancyGovernanceOptions.EnableInvitationDeliveryRetryBackgroundScheduling = false` so installing the package does not start recurring delivery attempts without a host decision. When enabled, the generic-host `BackgroundService` can run once at startup, honors `InvitationDeliveryRetryBackgroundIntervalSeconds`, uses the same bounded retry batch limit, records `InvitationDeliveryRetryBackgroundSource` on scheduled retry requests, and reports run counts, latest outcome, timestamps, latest attempted/dispatched/failed/exhausted/terminal/remaining counts, and latest unhandled error through the `tenant-invitations` runtime surface. This is a Cephalon-managed local queue plus process-local execution coordination plus opt-in process-local scheduler, not a distributed queue, cross-node lease, provider-specific notification connector, provider callback inbox, or exactly-once delivery system.

`ITenantInvitationDeliveryStatusReconciler` owns the host-agnostic status-observation path after dispatch. It resolves the same merged invitation catalog, optionally requires the incoming provider message id to match the id recorded by dispatch, records `lastDeliveryStatus`, `lastDeliveryStatusObservedAtUtc`, status provider message id, sender id, channel, source, actor, correlation id, reason, reconciliation outcome, reconciliation ownership, and external status ownership through `ITenantInvitationStore`, and returns `provider-message-missing` or `provider-message-mismatch` instead of attaching a provider callback to the wrong invitation. It does not reference ASP.NET Core, translate provider-specific webhook payloads, verify provider-specific signatures, poll delivery providers, or translate provider-specific status vocabularies beyond normalized metadata. ASP.NET Core hosts that want a generic engine-owned ingress can install `Cephalon.MultiTenancy.Governance.AspNetCore` and explicitly map `MapCephalonTenantInvitationDeliveryStatusCallbacks()` over this reconciler; that adapter can also verify a provider-neutral Cephalon HMAC signature over the normalized JSON body and reject bounded process-local signed replays before the reconciler is invoked. ASP.NET Core hosts that want first-party SendGrid Event Webhook translation can additionally install `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore` and map `MapCephalonSendGridInvitationDeliveryStatusCallbacks()` so SendGrid event arrays can verify optional SendGrid signatures, reject duplicate verified signatures inside a bounded process-local replay window, skip duplicate `sg_event_id` observations through `ITenantInvitationDeliveryStatusObservationStore`, and flow into the same reconciler.

`ITenantInvitationDeliveryStatusObservationStore` records the normalized observation result produced by the reconciler, including the stable observation id, tenant/invitation boundary, normalized delivery status, reconciliation outcome, observed timestamp, recorded timestamp, provider message id, sender id, channel, source, actor, correlation id, reason, and safe metadata. The default store is in-memory and process-local. Set `MultiTenancyGovernanceOptions.InvitationDeliveryStatusObservationStoreFilePath` to opt into the built-in JSON file store when a host wants Cephalon-managed observation history to survive process restarts, and tune `InvitationDeliveryStatusObservationHistoryLimit` to bound the retained history. This store is an audit/operator record for normalized observations; it is not a provider-specific callback inbox, provider-specific signature verifier, provider poller, distributed replay guard, or exactly-once delivery system.

`ITenantAdministrationWorkflow` owns explicit host-driven membership and invitation administration commands. It mutates `ITenantMembershipStore` and `ITenantInvitationStore`, records command, outcome, actor, reason, correlation, and ownership metadata, reports `store-failed` instead of pretending failed persistence succeeded, and feeds the same catalogs, evaluator, validator, capabilities, diagnostics, and `tenant-administration` runtime surface. It deliberately does not send invitation notifications itself, expose backoffice/admin UI, create identity-provider users, or run public onboarding; accepting an invitation only changes invitation state, and membership grant remains a separate command so callers do not get hidden cross-store partial mutations. ASP.NET Core hosts that want an engine-owned command endpoint can install `Cephalon.MultiTenancy.Governance.AspNetCore` and explicitly map `MapCephalonTenantAdministrationCommands()` over this workflow without making the governance core reference ASP.NET Core.

Tenant-domain ownership descriptors can come from the runtime `ITenantDomainOwnershipStore`, host options, or installed module contributors through `ITenantDomainOwnershipContributor`. The catalog de-duplicates ownership by tenant id and canonical domain name, while the validator checks tenant-domain match, pending, rejected, suspended, expired, and verified states. Domain names are canonicalized to lower-case host names without trailing dots so `Acme.Example.` and `acme.example` resolve to the same declared ownership boundary.

A descriptor without an explicit status defaults to `pending`, so declaring a domain does not validate ownership until the host or module deliberately marks that ownership `verified`.

The default `ITenantDomainOwnershipStore` is in-memory and process-local. Set `MultiTenancyGovernanceOptions.DomainOwnershipStoreFilePath` to opt into the built-in JSON file store when a host wants Cephalon-managed tenant-domain ownership state to survive process restarts. Stored domain ownership declarations merge into the same catalog used by `ITenantDomainOwnershipValidator`, so runtime `Upsert(...)` calls become observable through catalog reads and the `tenant-domain-ownership` runtime surface without requiring the consumer app to rebuild descriptors.

`ITenantDomainOwnershipVerificationWorkflow` owns in-process state transitions for runtime tenant-domain ownership declarations. It creates pending declarations with `request`, moves pending or rejected declarations to `verified`, rejects pending declarations, suspends verified declarations, and expires non-expired declarations. It rejects cross-tenant domain reuse and verification-method mismatches before mutating runtime state, records workflow metadata such as last command, status, actor, evidence, reason, and correlation id, persists through `ITenantDomainOwnershipStore`, and feeds the same merged catalog used by `ITenantDomainOwnershipValidator`.

`ITenantDomainOwnershipProofChallengeIssuer` owns challenge generation without pretending to publish or observe the outside world. It issues a secure random or caller-supplied challenge value, records `expectedProof` plus method-specific `expectedDnsTxtProof` or `expectedHttpFileProof` metadata, stores DNS TXT record or HTTP file-path publication hints, moves new, rejected, expired, or pending declarations into pending verification state through `ITenantDomainOwnershipStore`, and keeps verified or suspended declarations protected from accidental re-challenge. This lets a consumer app ask the engine for the exact proof to publish while DNS publication remains application-managed or future provider-pack work and HTTP file publication can flow through the engine-owned publisher plus an opt-in host adapter.

`ITenantDomainOwnershipProofPublicationPlanner` owns the instruction-generation step between challenge issuance and outside-world publication. It reads the expected proof metadata and challenge hints from the domain-ownership catalog, emits the exact DNS TXT record name/value or HTTP file path/content/content-type to publish, optionally records proof-publication plan metadata through `ITenantDomainOwnershipStore`, and reports store failures without returning stale publication instructions as successfully planned. This keeps publication planning Cephalon-managed while actual DNS record mutation, provider API calls, and provider-backed publication remain application-managed or future provider-pack work.

`ITenantDomainOwnershipHttpProofPublisher` owns the HTTP file materialization step for `http-file` declarations. It reuses the publication planner to resolve the canonical HTTP proof path, content, and content type, records publication metadata through `ITenantDomainOwnershipStore`, exposes published proof files through `ITenantDomainOwnershipHttpProofPublicationCatalog`, and keeps raw proof lookups constrained to host/path matches so host adapters can serve only explicitly published proof state. The companion `Cephalon.MultiTenancy.Governance.AspNetCore` package can then map `/.well-known/cephalon/{**proofPath}` and serve those catalog entries without making the governance core reference ASP.NET Core. DNS record mutation, DNS provider APIs, domain-provider mutation, public onboarding, and tenant-admin UI remain outside this proof.

`ITenantDomainOwnershipHttpProofCollector` owns the on-demand HTTP file proof collection step for `http-file` declarations. It reuses the publication planner to resolve the expected HTTP file path and proof content, requires HTTPS by default, rejects collection base URIs whose host does not match the declared domain, performs bounded GET requests without following redirects in the default client, limits response size, records URI/status/length/fingerprint metadata instead of raw observed proof text, and feeds the collected body into `ITenantDomainOwnershipProofEvaluator`. This makes HTTP file proof collection Cephalon-managed while DNS mutation, provider DNS APIs, and provider-backed publication remain application-managed or future provider-pack work.

`ITenantDomainOwnershipDnsTxtProofCollector` owns the on-demand DNS TXT proof collection step for `dns-txt` declarations when a host supplies `MultiTenancyGovernanceOptions.DomainOwnershipDnsTxtProofResolverEndpoint` or a per-request resolver endpoint. It reuses the publication planner to resolve the expected TXT record name and proof value, requires an explicit HTTPS DNS-over-HTTPS resolver instead of silently choosing a public resolver, performs bounded resolver requests, parses DNS JSON `Answer` records, normalizes quoted TXT chunks, records resolver/status/record/count/fingerprint metadata instead of raw observed TXT values, and feeds only a matching TXT value into `ITenantDomainOwnershipProofEvaluator`. If no resolver is configured, the collector returns `resolver-not-configured` and the runner reports `dns-txt-collection-unavailable`, keeping runtime ownership truthful.

`ITenantDomainOwnershipProofEvaluator` owns the proof evaluation step without pretending every collection path is engine-owned. Applications, provider packs, the built-in HTTP proof collector, the configured built-in DNS TXT proof collector, the bounded proof-polling runner, or the opt-in background polling hosted service report observed proof evidence into the evaluator. The evaluator resolves the matching domain declaration, obtains the expected proof from the request or descriptor metadata keys such as `expectedProof`, `expectedDnsTxtProof`, or `expectedHttpFileProof`, compares trimmed values exactly, records proof-evaluation metadata with SHA-256 fingerprints instead of adding observed raw proof text, and applies `verify` or `reject` through `ITenantDomainOwnershipVerificationWorkflow`. This makes proof evaluation and state mutation Cephalon-managed while actual DNS publication and provider-specific mutation remain application-managed or future provider-pack work.

`ITenantDomainOwnershipProofVerificationRunner` owns the orchestration step that consumer apps otherwise have to hand-write around the narrower proof services. A runner call can issue a missing expected-proof challenge, produce DNS TXT or HTTP file publication instructions, evaluate an explicitly supplied observed proof, invoke the built-in HTTP file collector for `http-file` declarations, or invoke the built-in DNS TXT collector for `dns-txt` declarations when a resolver endpoint is configured. The runner returns nested challenge, publication-plan, HTTP-collection, DNS-TXT-collection, and evaluation results plus safe ownership metadata so callers can show one operator-facing answer without parsing each lower-level service separately. This reduces consumer glue while keeping DNS publication, provider mutation, public onboarding, and tenant-admin UI outside the current managed proof.

`ITenantDomainOwnershipProofPollingRunner` owns one bounded on-demand polling pass over the tenant-domain ownership catalog. It filters pending or rejected HTTP file and DNS TXT declarations, skips verified/suspended/expired/manual declarations by default, avoids issuing new challenges unless the caller deliberately opts into missing expected-proof handling through the nested verifier, and delegates each selected attempt to `ITenantDomainOwnershipProofVerificationRunner`. It returns aggregate counts, nested verification results, batch-limit metadata, `externalProofPollingOwnership = cephalon-managed`, and caller-supplied background ownership metadata. This gives consumer apps a low-glue operator action for "check all pending proofs now" without claiming DNS mutation, HTTP hosting, provider control-plane mutation, public onboarding, or tenant-admin UI.

`ITenantDomainOwnershipProofPollingRuntimeCatalog` reports the opt-in automatic background polling hosted-service posture. Automatic polling is disabled by default through `MultiTenancyGovernanceOptions.EnableDomainOwnershipProofBackgroundPolling = false` so installing the package does not start recurring HTTP or DNS checks without a host decision. When enabled, the generic-host `BackgroundService` schedules `ITenantDomainOwnershipProofPollingRunner`, can run once at startup, honors `DomainOwnershipProofBackgroundPollingIntervalSeconds`, uses the same bounded batch limit, and reports run counts, latest outcome, latest reason, timestamps, and latest candidate/verification/verified/rejected/failed counts through the `tenant-domain-ownership` runtime surface. This makes scheduling and run-state Cephalon-managed while DNS proof publication, provider mutation, public onboarding, and tenant-admin UI remain outside this proof.

Tenant-governance actions can come from host options, installed module contributors through `ITenantGovernanceActionContributor`, or the in-process `ITenantGovernanceActionWorkflow`. The catalog de-duplicates actions by tenant id and action id, while the decider checks tenant/action match, optional action-kind match, optional subject match, pending approval, rejected, remediation-required, expired, approved, and remediated states. Action descriptors default to `pending-approval`, so declaring an action does not let it proceed until the host, module, or workflow deliberately moves it to `approved` or `remediated`.

`ITenantGovernanceActionWorkflow` owns state transitions for runtime actions. It creates pending actions with `request`, moves pending actions to `approved` or `rejected`, moves pending or approved actions to `remediation-required`, moves remediation-required actions to `remediated`, and expires non-terminal actions. It rejects tenant, action-kind, and subject-boundary mismatches before mutating runtime state, records workflow metadata such as last command, status, actor, reason, and correlation id, persists through `ITenantGovernanceActionStore`, and feeds the same merged catalog used by `ITenantGovernanceActionDecider`.

The default `ITenantGovernanceActionStore` is in-memory and process-local. Set `MultiTenancyGovernanceOptions.GovernanceActionStoreFilePath` to opt into the built-in JSON file store when a host wants Cephalon-managed action state to survive process restarts. Workflow commands return `store-failed` without reporting the transition as applied when persistence fails, so operators and callers do not mistake an uncommitted action transition for durable runtime truth.

The resulting operator-facing answers flow through `/engine/technology-surfaces` and `/engine/snapshot` as the `tenant-memberships`, `tenant-invitations`, `tenant-administration`, `tenant-domain-ownership`, and `tenant-governance-actions` surfaces. The membership summary reports membership count, tenant count, contributor count, configured membership count, runtime membership count, membership-store kind, membership-store durability, membership-store ownership, durable-store ownership, and evaluation ownership. The invitation summary reports invitation count, tenant count, contributor count, configured invitation count, runtime invitation count, invitation-store kind, invitation-store durability, invitation-store ownership, durable-store ownership, validation ownership, delivery-dispatch enablement/ownership, delivery retry queue enablement/ownership/kind/durability/scope/counts/latest retry outcome, delivery retry max attempts/delay/max items, process-local retry execution coordination enablement/ownership/scope/mode/in-progress state/counts/latest outcome/timestamps, opt-in background retry scheduling enablement/ownership/interval/batch/run counts/latest outcome/timestamps, delivery-status reconciliation enablement/ownership, delivery-status observation-store enablement/kind/durability/scope/retention/count/latest observation, external delivery-status ownership, delivery sender count/ids/ownership, external delivery ownership, delivery run count/history limit/latest outcome/latest timestamp, delivery-status reported count/latest status/latest observed timestamp, and invitation status posture. Sender companion metadata from HTTP, SMTP, SendGrid, and Mailgun delivery packages appears on the same invitation run and sender readiness surfaces instead of creating parallel runtime surfaces. The tenant-administration summary reports workflow enablement, membership/invitation administration ownership, store kinds/durability/counts, supported commands, and core-package boundaries for public onboarding, host-adapter endpoint ownership, tenant-admin UI, invitation delivery dispatch, invitation delivery status reconciliation, provider-specific sender ownership, external delivery/status ownership, and identity-provider sync. ASP.NET Core hosts that install and map `Cephalon.MultiTenancy.Governance.AspNetCore` get separate `tenant-administration-http-endpoints`, `tenant-invitation-delivery-http-endpoints`, and `tenant-invitation-delivery-status-http-endpoints` surfaces for optional command, dispatch action, normalized callback, and bounded observation-read endpoint routes, authorization posture, mapped/configured/disabled state, provider-message-match posture, provider-neutral callback signature verification posture, bounded process-local signed replay posture, observation read default/max limits, and provider-specific sender/callback inbox/translation/signature/polling boundaries. ASP.NET Core hosts that install and map `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore` also get the `tenant-invitation-delivery-sendgrid-status-callbacks` surface for SendGrid Event Webhook route, authorization, body/event limits, provider-message matching, translation ownership, explicit signature/replay/idempotency posture, and inbox/polling boundaries. ASP.NET Core hosts that install and map `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore` also get the `tenant-invitation-delivery-mailgun-status-callbacks` surface for Mailgun webhook route, authorization, body/event limits, provider-message matching, translation ownership, explicit signature/replay non-ownership, and inbox/polling boundaries. The domain-ownership summary reports declared domain ownership count, tenant count, contributor count, configured domain count, runtime domain ownership count, domain-ownership-store kind, domain-ownership-store durability, domain-ownership-store ownership, durable-store ownership, validation ownership, in-process verification-workflow ownership, proof-challenge issuance ownership, proof-publication planning ownership, HTTP proof publication ownership, published HTTP proof count, proof-evaluation ownership, HTTP proof collection ownership, DNS TXT proof collection enablement, DNS TXT resolver configuration, proof-verification runner ownership, proof-polling runner ownership, on-demand external proof polling ownership, opt-in background proof polling ownership, background polling interval/batch/run counts/latest outcome, status posture, verification-method posture, and the fact that DNS/provider proof publication remains application-managed. The governance-action summary reports action count, tenant count, contributor count, configured action count, runtime action count, decision ownership, workflow execution ownership, action-store kind, action-store durability, action-store ownership, durable-store ownership, notification-delivery ownership, status posture, action-kind posture, and subject-kind posture. Per-tenant entries summarize membership, invitation, retry, observation, domain, or action posture without exposing individual principal, invitee, domain, or action metadata.

This is intentionally a narrow managed proof. The package now owns membership cataloging/evaluation, opt-in local durable membership state, invitation cataloging/validation, opt-in local durable invitation state, host-agnostic invitation delivery dispatch/run-state/outcome persistence over registered sender extensions, opt-in local invitation delivery retry storage plus bounded retry execution for retryable sender failures, process-local retry execution coordination for manual/background overlap, opt-in automatic background invitation retry scheduling/run-state over that bounded runner, host-agnostic invitation delivery status reconciliation over provider or receiver observations, opt-in local durable delivery-status observation storage for normalized reconciliation records, host-driven tenant-administration workflow commands over membership and invitation stores, declared domain-ownership cataloging/validation, opt-in local durable domain-ownership state, in-process domain-ownership verification workflow transitions, proof challenge issuance, proof publication planning, HTTP file proof publication state, proof evaluation over reported domain evidence, on-demand HTTP file proof collection, configured on-demand DNS TXT proof collection, proof-verification runner orchestration, bounded on-demand proof-polling runner orchestration, opt-in automatic background proof polling scheduling/run-state, approval/remediation action cataloging/decision, approval/remediation action workflow transitions, and opt-in local durable action state. It does not own ASP.NET Core routing itself; that lives in `Cephalon.MultiTenancy.Governance.AspNetCore`, including the optional tenant-administration command endpoint, tenant-invitation delivery dispatch endpoint, normalized invitation delivery-status callback endpoint, opt-in provider-neutral HMAC callback signature verification, bounded process-local signed-callback replay protection for that normalized ingress, and a bounded delivery-status observation read endpoint over the host-agnostic observation store. The optional `Cephalon.MultiTenancy.Governance.HttpDelivery` package owns the generic HTTP webhook sender proof plus provider-neutral idempotency headers, optional HMAC request signing, and bounded in-process retry/backoff, the optional `Cephalon.MultiTenancy.Governance.SmtpDelivery` package owns SMTP relay handoff for templated invitation email through a replaceable SMTP client seam, the optional `Cephalon.MultiTenancy.Governance.SendGridDelivery` package owns SendGrid Mail Send API handoff through a replaceable SendGrid client seam, the optional `Cephalon.MultiTenancy.Governance.MailgunDelivery` package owns Mailgun Messages API handoff through a replaceable Mailgun client seam, the optional `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore` package owns ASP.NET Core SendGrid Event Webhook payload translation, optional SendGrid signed-webhook verification, bounded process-local signed-callback replay protection, and observation-store-backed SendGrid event-id duplicate suppression into the existing reconciler, and the optional `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore` package owns ASP.NET Core Mailgun webhook payload translation into the existing reconciler. The governance family still does not own actual DNS proof publication, provider-backed proof publication or mutation, domain lifecycle automation beyond status transitions, distributed or provider-backed membership/invitation/domain/action-store backends, external human-task inboxes, SES/Microsoft Graph or other additional provider-specific email API senders, SMS/chat/CRM/identity-provider invitation senders, distributed retry queues, cross-node delivery leases, exactly-once delivery, provider-specific or distributed callback inboxes, cross-node callback replay protection, distributed event-id ledgers, remediation execution beyond state transitions, tenant-admin UI, public-site onboarding, provider control-plane mutation, Mailgun signature verification/replay-token protection, other non-SendGrid/non-Mailgun provider-specific delivery-status callback translation, provider-specific callback signature verification beyond shipped SendGrid hardening, provider polling, or provider-specific identity synchronization. Those should land as later governance slices only when the package truly owns those paths.

## Related docs

- [Cephalon.MultiTenancy](multi-tenancy.md)
- [Cephalon.MultiTenancy.Governance.AspNetCore](multi-tenancy-governance-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.HttpDelivery](multi-tenancy-governance-httpdelivery.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery](multi-tenancy-governance-mailgundelivery.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore](multi-tenancy-governance-mailgundelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore](multi-tenancy-governance-sendgriddelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery](multi-tenancy-governance-sendgriddelivery.md)
- [Cephalon.MultiTenancy.Governance.SmtpDelivery](multi-tenancy-governance-smtpdelivery.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
