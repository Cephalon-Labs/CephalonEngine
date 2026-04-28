# Cephalon.MultiTenancy.Governance

`Cephalon.MultiTenancy.Governance` is the tenant-governance companion package for Cephalon multi-tenancy workloads.

## What it owns

- tenant membership descriptors, registries, catalogs, and contributor contracts
- host-defined and module-contributed membership merge behavior
- one Cephalon-managed tenant-membership evaluation baseline
- tenant invitation descriptors, registries, catalogs, and contributor contracts
- host-defined and module-contributed invitation merge behavior
- one Cephalon-managed tenant-invitation validation baseline
- `tenancy.membership.catalog` and `tenancy.membership.evaluation` capabilities when the `MultiTenancy` technology is active
- `tenancy.invitation.catalog` and `tenancy.invitation.validation` capabilities when the `MultiTenancy` technology is active
- the `tenant-memberships` technology runtime surface under `multi-tenancy`
- the `tenant-invitations` technology runtime surface under `multi-tenancy`
- stable governance diagnostics for allowed and denied membership evaluation plus invitation validation
- a separate runtime ownership boundary from the base `Cephalon.MultiTenancy` tenant-resolution pack

## Main surfaces

- `Configuration/MultiTenancyGovernanceOptions.cs`
- `Modules/MultiTenancyGovernanceModule.cs`
- `Registration/MultiTenancyGovernanceEngineBuilderExtensions.cs`
- `Services/TenantMembershipDescriptor.cs`
- `Services/ITenantMembershipContributor.cs`
- `Services/ITenantMembershipRegistry.cs`
- `Services/ITenantMembershipCatalog.cs`
- `Services/ITenantMembershipEvaluator.cs`
- `Services/TenantMembershipEvaluationRequest.cs`
- `Services/TenantMembershipEvaluationResult.cs`
- `Services/TenantMembershipStatuses.cs`
- `Services/TenantMembershipEvaluationOutcomes.cs`
- `Services/TenantInvitationDescriptor.cs`
- `Services/ITenantInvitationContributor.cs`
- `Services/ITenantInvitationRegistry.cs`
- `Services/ITenantInvitationCatalog.cs`
- `Services/ITenantInvitationValidator.cs`
- `Services/TenantInvitationValidationRequest.cs`
- `Services/TenantInvitationValidationResult.cs`
- `Services/TenantInvitationStatuses.cs`
- `Services/TenantInvitationValidationOutcomes.cs`
- `Services/MultiTenancyGovernanceRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyGovernanceInvitationRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyGovernanceDiagnosticsConventionContributor.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

The base `Cephalon.MultiTenancy` package intentionally stays focused on tenant resolution and ambient tenant context. This companion package owns the first concrete governance runtime proofs beside that base package: a tenant membership catalog plus deterministic evaluator, and a tenant invitation catalog plus deterministic validator.

Memberships can come from host options or installed module contributors through `ITenantMembershipContributor`. The catalog de-duplicates memberships by tenant id, principal kind, and principal id, and the evaluator uses that same tenant/principal-kind/principal-id boundary so user, group, service, and organization identifiers cannot bleed roles across kinds. Evaluation checks active, suspended, expired, and missing-role states without leaking host-specific identity APIs into the engine core.

Invitations can come from host options or installed module contributors through `ITenantInvitationContributor`. The catalog de-duplicates invitations by tenant id and invitation id, while the validator checks pending, accepted, revoked, expired, invitee-mismatch, and missing-role states. Invitee matching uses invitee kind and invitee id so a service invitation cannot be reused as a user invitation.

The resulting operator-facing answers flow through `/engine/technology-surfaces` and `/engine/snapshot` as the `tenant-memberships` and `tenant-invitations` surfaces. The membership summary reports membership count, tenant count, contributor count, configured membership count, and evaluation ownership. The invitation summary reports invitation count, tenant count, contributor count, configured invitation count, validation ownership, and status posture. Per-tenant entries summarize membership or invitation posture, role names, principal or invitee-kind breakdown, and contributing modules without exposing individual principal or invitee identifiers.

This is intentionally a narrow managed proof. The package now owns membership cataloging, membership evaluation, invitation cataloging, and invitation validation, but it does not yet own domain ownership verification, approval workflows, remediation workflows, backoffice tenant administration, public-site onboarding, durable membership or invitation stores, invitation delivery, or provider-specific identity synchronization. Those should land as later governance slices only when the package truly owns those paths.

## Related docs

- [Cephalon.MultiTenancy](multi-tenancy.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
