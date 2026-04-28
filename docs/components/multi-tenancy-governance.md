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
- tenant-domain ownership descriptors, registries, catalogs, and contributor contracts
- host-defined and module-contributed domain-ownership merge behavior
- one Cephalon-managed tenant-domain ownership store baseline, with an in-memory default and opt-in local JSON durability
- one Cephalon-managed declared tenant-domain ownership validation baseline
- one Cephalon-managed in-process tenant-domain ownership verification workflow baseline
- tenant-governance action descriptors, registries, catalogs, and contributor contracts
- host-defined and module-contributed approval/remediation action merge behavior
- one Cephalon-managed tenant-governance action decision baseline
- one Cephalon-managed in-process tenant-governance action workflow baseline
- one Cephalon-managed tenant-governance action store baseline, with an in-memory default and opt-in local JSON durability
- `tenancy.membership.catalog`, `tenancy.membership.store`, and `tenancy.membership.evaluation` capabilities when the `MultiTenancy` technology is active
- `tenancy.invitation.catalog`, `tenancy.invitation.store`, and `tenancy.invitation.validation` capabilities when the `MultiTenancy` technology is active
- `tenancy.domain-ownership.catalog`, `tenancy.domain-ownership.store`, `tenancy.domain-ownership.validation`, and `tenancy.domain-ownership.workflow` capabilities when the `MultiTenancy` technology is active
- `tenancy.governance-action.catalog`, `tenancy.governance-action.store`, `tenancy.governance-action.decision`, and `tenancy.governance-action.workflow` capabilities when the `MultiTenancy` technology is active
- the `tenant-memberships` technology runtime surface under `multi-tenancy`
- the `tenant-invitations` technology runtime surface under `multi-tenancy`
- the `tenant-domain-ownership` technology runtime surface under `multi-tenancy`
- the `tenant-governance-actions` technology runtime surface under `multi-tenancy`
- stable governance diagnostics for allowed and denied membership evaluation, invitation validation, domain-ownership validation, domain-ownership verification workflow transitions, domain-ownership store persistence, governance-action decisions, governance-action workflow transitions, and governance-action store persistence
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
- `Services/TenantInvitationValidationRequest.cs`
- `Services/TenantInvitationValidationResult.cs`
- `Services/TenantInvitationStatuses.cs`
- `Services/TenantInvitationValidationOutcomes.cs`
- `Services/TenantDomainOwnershipDescriptor.cs`
- `Services/ITenantDomainOwnershipContributor.cs`
- `Services/ITenantDomainOwnershipRegistry.cs`
- `Services/ITenantDomainOwnershipCatalog.cs`
- `Services/ITenantDomainOwnershipStore.cs`
- `Services/ITenantDomainOwnershipValidator.cs`
- `Services/ITenantDomainOwnershipVerificationWorkflow.cs`
- `Services/TenantDomainOwnershipValidationRequest.cs`
- `Services/TenantDomainOwnershipValidationResult.cs`
- `Services/TenantDomainOwnershipVerificationWorkflowRequest.cs`
- `Services/TenantDomainOwnershipVerificationWorkflowResult.cs`
- `Services/TenantDomainOwnershipVerificationWorkflowCommands.cs`
- `Services/TenantDomainOwnershipVerificationWorkflowOutcomes.cs`
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
- `Services/MultiTenancyGovernanceDomainRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyGovernanceActionRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyGovernanceDiagnosticsConventionContributor.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

The base `Cephalon.MultiTenancy` package intentionally stays focused on tenant resolution and ambient tenant context. This companion package owns the first concrete governance runtime proofs beside that base package: a tenant membership catalog plus deterministic evaluator, an opt-in durable tenant-membership store, a tenant invitation catalog plus deterministic validator, an opt-in durable tenant-invitation store, a declared tenant-domain ownership catalog plus deterministic validator, an opt-in durable tenant-domain ownership store, an in-process tenant-domain ownership verification workflow that can request, verify, reject, suspend, or expire domain ownership declarations, an approval/remediation action catalog plus deterministic decider, an in-process action workflow that can request, approve, reject, require remediation, mark remediated, or expire governance actions, and an opt-in durable action store for workflow-created runtime action state.

Memberships can come from the runtime `ITenantMembershipStore`, host options, or installed module contributors through `ITenantMembershipContributor`. The catalog de-duplicates memberships by tenant id, principal kind, and principal id, and the evaluator uses that same tenant/principal-kind/principal-id boundary so user, group, service, and organization identifiers cannot bleed roles across kinds. Evaluation checks active, suspended, expired, and missing-role states without leaking host-specific identity APIs into the engine core.

The default `ITenantMembershipStore` is in-memory and process-local. Set `MultiTenancyGovernanceOptions.MembershipStoreFilePath` to opt into the built-in JSON file store when a host wants Cephalon-managed tenant membership state to survive process restarts. Stored memberships merge into the same catalog used by `ITenantMembershipEvaluator`, so runtime `Upsert(...)` calls become observable through catalog reads and the `tenant-memberships` runtime surface without requiring the consumer app to rebuild descriptors.

Invitations can come from the runtime `ITenantInvitationStore`, host options, or installed module contributors through `ITenantInvitationContributor`. The catalog de-duplicates invitations by tenant id and invitation id, while the validator checks pending, accepted, revoked, expired, invitee-mismatch, and missing-role states. Invitee matching uses invitee kind and invitee id so a service invitation cannot be reused as a user invitation.

The default `ITenantInvitationStore` is in-memory and process-local. Set `MultiTenancyGovernanceOptions.InvitationStoreFilePath` to opt into the built-in JSON file store when a host wants Cephalon-managed tenant invitation state to survive process restarts. Stored invitations merge into the same catalog used by `ITenantInvitationValidator`, so runtime `Upsert(...)` calls become observable through catalog reads and the `tenant-invitations` runtime surface without requiring the consumer app to rebuild descriptors.

Tenant-domain ownership descriptors can come from the runtime `ITenantDomainOwnershipStore`, host options, or installed module contributors through `ITenantDomainOwnershipContributor`. The catalog de-duplicates ownership by tenant id and canonical domain name, while the validator checks tenant-domain match, pending, rejected, suspended, expired, and verified states. Domain names are canonicalized to lower-case host names without trailing dots so `Acme.Example.` and `acme.example` resolve to the same declared ownership boundary.

A descriptor without an explicit status defaults to `pending`, so declaring a domain does not validate ownership until the host or module deliberately marks that ownership `verified`.

The default `ITenantDomainOwnershipStore` is in-memory and process-local. Set `MultiTenancyGovernanceOptions.DomainOwnershipStoreFilePath` to opt into the built-in JSON file store when a host wants Cephalon-managed tenant-domain ownership state to survive process restarts. Stored domain ownership declarations merge into the same catalog used by `ITenantDomainOwnershipValidator`, so runtime `Upsert(...)` calls become observable through catalog reads and the `tenant-domain-ownership` runtime surface without requiring the consumer app to rebuild descriptors.

`ITenantDomainOwnershipVerificationWorkflow` owns in-process state transitions for runtime tenant-domain ownership declarations. It creates pending declarations with `request`, moves pending or rejected declarations to `verified`, rejects pending declarations, suspends verified declarations, and expires non-expired declarations. It rejects cross-tenant domain reuse and verification-method mismatches before mutating runtime state, records workflow metadata such as last command, status, actor, evidence, reason, and correlation id, persists through `ITenantDomainOwnershipStore`, and feeds the same merged catalog used by `ITenantDomainOwnershipValidator`.

Tenant-governance actions can come from host options, installed module contributors through `ITenantGovernanceActionContributor`, or the in-process `ITenantGovernanceActionWorkflow`. The catalog de-duplicates actions by tenant id and action id, while the decider checks tenant/action match, optional action-kind match, optional subject match, pending approval, rejected, remediation-required, expired, approved, and remediated states. Action descriptors default to `pending-approval`, so declaring an action does not let it proceed until the host, module, or workflow deliberately moves it to `approved` or `remediated`.

`ITenantGovernanceActionWorkflow` owns state transitions for runtime actions. It creates pending actions with `request`, moves pending actions to `approved` or `rejected`, moves pending or approved actions to `remediation-required`, moves remediation-required actions to `remediated`, and expires non-terminal actions. It rejects tenant, action-kind, and subject-boundary mismatches before mutating runtime state, records workflow metadata such as last command, status, actor, reason, and correlation id, persists through `ITenantGovernanceActionStore`, and feeds the same merged catalog used by `ITenantGovernanceActionDecider`.

The default `ITenantGovernanceActionStore` is in-memory and process-local. Set `MultiTenancyGovernanceOptions.GovernanceActionStoreFilePath` to opt into the built-in JSON file store when a host wants Cephalon-managed action state to survive process restarts. Workflow commands return `store-failed` without reporting the transition as applied when persistence fails, so operators and callers do not mistake an uncommitted action transition for durable runtime truth.

The resulting operator-facing answers flow through `/engine/technology-surfaces` and `/engine/snapshot` as the `tenant-memberships`, `tenant-invitations`, `tenant-domain-ownership`, and `tenant-governance-actions` surfaces. The membership summary reports membership count, tenant count, contributor count, configured membership count, runtime membership count, membership-store kind, membership-store durability, membership-store ownership, durable-store ownership, and evaluation ownership. The invitation summary reports invitation count, tenant count, contributor count, configured invitation count, runtime invitation count, invitation-store kind, invitation-store durability, invitation-store ownership, durable-store ownership, validation ownership, and status posture. The domain-ownership summary reports declared domain ownership count, tenant count, contributor count, configured domain count, runtime domain ownership count, domain-ownership-store kind, domain-ownership-store durability, domain-ownership-store ownership, durable-store ownership, validation ownership, in-process verification-workflow ownership, status posture, verification-method posture, and the fact that external DNS/HTTP proof collection remains application-managed. The governance-action summary reports action count, tenant count, contributor count, configured action count, runtime action count, decision ownership, workflow execution ownership, action-store kind, action-store durability, action-store ownership, durable-store ownership, notification-delivery ownership, status posture, action-kind posture, and subject-kind posture. Per-tenant entries summarize membership, invitation, domain, or action posture without exposing individual principal, invitee, domain, or action metadata.

This is intentionally a narrow managed proof. The package now owns membership cataloging/evaluation, opt-in local durable membership state, invitation cataloging/validation, opt-in local durable invitation state, declared domain-ownership cataloging/validation, opt-in local durable domain-ownership state, in-process domain-ownership verification workflow transitions, approval/remediation action cataloging/decision, approval/remediation action workflow transitions, and opt-in local durable action state. It does not yet own DNS/HTTP proof collection or external polling, domain lifecycle automation beyond status transitions, distributed or provider-backed membership/invitation/domain/action-store backends, external human-task inboxes, notification or invitation delivery, remediation execution beyond state transitions, backoffice tenant administration, public-site onboarding, or provider-specific identity synchronization. Those should land as later governance slices only when the package truly owns those paths.

## Related docs

- [Cephalon.MultiTenancy](multi-tenancy.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
