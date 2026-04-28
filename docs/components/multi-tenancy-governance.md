# Cephalon.MultiTenancy.Governance

`Cephalon.MultiTenancy.Governance` is the tenant-governance companion package for Cephalon multi-tenancy workloads.

## What it owns

- tenant membership descriptors, registries, catalogs, and contributor contracts
- host-defined and module-contributed membership merge behavior
- one Cephalon-managed tenant-membership evaluation baseline
- tenant invitation descriptors, registries, catalogs, and contributor contracts
- host-defined and module-contributed invitation merge behavior
- one Cephalon-managed tenant-invitation validation baseline
- tenant-domain ownership descriptors, registries, catalogs, and contributor contracts
- host-defined and module-contributed domain-ownership merge behavior
- one Cephalon-managed declared tenant-domain ownership validation baseline
- `tenancy.membership.catalog` and `tenancy.membership.evaluation` capabilities when the `MultiTenancy` technology is active
- `tenancy.invitation.catalog` and `tenancy.invitation.validation` capabilities when the `MultiTenancy` technology is active
- `tenancy.domain-ownership.catalog` and `tenancy.domain-ownership.validation` capabilities when the `MultiTenancy` technology is active
- the `tenant-memberships` technology runtime surface under `multi-tenancy`
- the `tenant-invitations` technology runtime surface under `multi-tenancy`
- the `tenant-domain-ownership` technology runtime surface under `multi-tenancy`
- stable governance diagnostics for allowed and denied membership evaluation, invitation validation, and domain-ownership validation
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
- `Services/TenantDomainOwnershipDescriptor.cs`
- `Services/ITenantDomainOwnershipContributor.cs`
- `Services/ITenantDomainOwnershipRegistry.cs`
- `Services/ITenantDomainOwnershipCatalog.cs`
- `Services/ITenantDomainOwnershipValidator.cs`
- `Services/TenantDomainOwnershipValidationRequest.cs`
- `Services/TenantDomainOwnershipValidationResult.cs`
- `Services/TenantDomainOwnershipStatuses.cs`
- `Services/TenantDomainVerificationMethods.cs`
- `Services/TenantDomainOwnershipValidationOutcomes.cs`
- `Services/MultiTenancyGovernanceRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyGovernanceInvitationRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyGovernanceDomainRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyGovernanceDiagnosticsConventionContributor.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

The base `Cephalon.MultiTenancy` package intentionally stays focused on tenant resolution and ambient tenant context. This companion package owns the first concrete governance runtime proofs beside that base package: a tenant membership catalog plus deterministic evaluator, a tenant invitation catalog plus deterministic validator, and a declared tenant-domain ownership catalog plus deterministic validator.

Memberships can come from host options or installed module contributors through `ITenantMembershipContributor`. The catalog de-duplicates memberships by tenant id, principal kind, and principal id, and the evaluator uses that same tenant/principal-kind/principal-id boundary so user, group, service, and organization identifiers cannot bleed roles across kinds. Evaluation checks active, suspended, expired, and missing-role states without leaking host-specific identity APIs into the engine core.

Invitations can come from host options or installed module contributors through `ITenantInvitationContributor`. The catalog de-duplicates invitations by tenant id and invitation id, while the validator checks pending, accepted, revoked, expired, invitee-mismatch, and missing-role states. Invitee matching uses invitee kind and invitee id so a service invitation cannot be reused as a user invitation.

Tenant-domain ownership descriptors can come from host options or installed module contributors through `ITenantDomainOwnershipContributor`. The catalog de-duplicates ownership by tenant id and canonical domain name, while the validator checks tenant-domain match, pending, rejected, suspended, expired, and verified states. Domain names are canonicalized to lower-case host names without trailing dots so `Acme.Example.` and `acme.example` resolve to the same declared ownership boundary.

A descriptor without an explicit status defaults to `pending`, so declaring a domain does not validate ownership until the host or module deliberately marks that ownership `verified`.

The resulting operator-facing answers flow through `/engine/technology-surfaces` and `/engine/snapshot` as the `tenant-memberships`, `tenant-invitations`, and `tenant-domain-ownership` surfaces. The membership summary reports membership count, tenant count, contributor count, configured membership count, and evaluation ownership. The invitation summary reports invitation count, tenant count, contributor count, configured invitation count, validation ownership, and status posture. The domain-ownership summary reports declared domain ownership count, tenant count, contributor count, configured domain count, validation ownership, status posture, verification-method posture, and the fact that external verification execution remains application-managed. Per-tenant entries summarize membership, invitation, or domain posture without exposing individual principal, invitee, or domain identifiers.

This is intentionally a narrow managed proof. The package now owns membership cataloging, membership evaluation, invitation cataloging, invitation validation, declared domain-ownership cataloging, and declared domain-ownership validation, but it does not yet own DNS/HTTP verification execution, domain lifecycle automation, approval workflows, remediation workflows, backoffice tenant administration, public-site onboarding, durable membership/invitation/domain stores, invitation delivery, or provider-specific identity synchronization. Those should land as later governance slices only when the package truly owns those paths.

## Related docs

- [Cephalon.MultiTenancy](multi-tenancy.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
