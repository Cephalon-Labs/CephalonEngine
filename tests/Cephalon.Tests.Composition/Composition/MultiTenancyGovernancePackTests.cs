using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Cephalon.MultiTenancy.Registration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class MultiTenancyGovernancePackTests
{
    [Fact]
    public async Task AddMultiTenancyGovernanceRegistersMembershipCatalogEvaluatorDiagnosticsAndRuntimeSurface()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddModule(new PlatformTestModule());
            engine.AddMultiTenancy();
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Memberships.Add(new TenantMembershipDescriptor(
                    tenantId: "tenant-001",
                    principalId: "user-001",
                    displayName: "Acme Admin",
                    roles: ["admin", "member"],
                    sourceModuleId: "platform-test"));
                options.Memberships.Add(new TenantMembershipDescriptor(
                    tenantId: "tenant-001",
                    principalId: "user-002",
                    displayName: "Suspended Member",
                    roles: ["member"],
                    status: TenantMembershipStatuses.Suspended,
                    sourceModuleId: "platform-test"));
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-001",
                    tenantId: "tenant-001",
                    inviteeId: "user-003",
                    displayName: "Acme Invite",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    sourceModuleId: "platform-test"));
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-002",
                    tenantId: "tenant-001",
                    inviteeId: "user-004",
                    displayName: "Revoked Invite",
                    roles: ["member"],
                    status: TenantInvitationStatuses.Revoked,
                    sourceModuleId: "platform-test"));
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "Acme.Example.",
                    displayName: "Acme Primary Domain",
                    status: TenantDomainOwnershipStatuses.Verified,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt,
                    verifiedAtUtc: new DateTimeOffset(2026, 04, 01, 0, 0, 0, TimeSpan.Zero),
                    sourceModuleId: "platform-test"));
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "pending.example",
                    displayName: "Pending Domain",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.HttpFile,
                    sourceModuleId: "platform-test"));
                options.GovernanceActions.Add(new TenantGovernanceActionDescriptor(
                    actionId: "approve-membership-001",
                    tenantId: "tenant-001",
                    actionKind: TenantGovernanceActionKinds.MembershipChange,
                    subjectKind: "user",
                    subjectId: "user-001",
                    displayName: "Approve Acme Admin",
                    status: TenantGovernanceActionStatuses.Approved,
                    requestedBy: "operator-001",
                    approvedBy: "tenant-owner",
                    decidedAtUtc: new DateTimeOffset(2026, 04, 20, 0, 0, 0, TimeSpan.Zero),
                    sourceModuleId: "platform-test"));
                options.GovernanceActions.Add(new TenantGovernanceActionDescriptor(
                    actionId: "remediate-domain-001",
                    tenantId: "tenant-001",
                    actionKind: TenantGovernanceActionKinds.Remediation,
                    subjectKind: "domain",
                    subjectId: "pending.example",
                    displayName: "Remediate Pending Domain",
                    status: TenantGovernanceActionStatuses.RemediationRequired,
                    sourceModuleId: "platform-test"));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<ITenantMembershipCatalog>();
        var evaluator = provider.GetRequiredService<ITenantMembershipEvaluator>();
        var invitationCatalog = provider.GetRequiredService<ITenantInvitationCatalog>();
        var invitationValidator = provider.GetRequiredService<ITenantInvitationValidator>();
        var domainCatalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();
        var domainValidator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();
        var governanceActionCatalog = provider.GetRequiredService<ITenantGovernanceActionCatalog>();
        var governanceActionDecider = provider.GetRequiredService<ITenantGovernanceActionDecider>();
        var governanceActionWorkflow = provider.GetRequiredService<ITenantGovernanceActionWorkflow>();
        var runtime = provider.GetRequiredService<global::Cephalon.Engine.Runtime.IRuntime>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var membershipsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-memberships");
        var invitationsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-invitations");
        var domainsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-domain-ownership");
        var governanceActionsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-governance-actions");
        var summaryEntry = Assert.Single(membershipsSurface.Entries, entry => entry.Id == "tenant-membership-runtime");
        var tenantEntry = Assert.Single(membershipsSurface.Entries, entry => entry.Id == "tenant-membership:tenant-001");
        var invitationSummaryEntry = Assert.Single(invitationsSurface.Entries, entry => entry.Id == "tenant-invitation-runtime");
        var tenantInvitationEntry = Assert.Single(invitationsSurface.Entries, entry => entry.Id == "tenant-invitations:tenant-001");
        var domainSummaryEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership-runtime");
        var tenantDomainEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership:tenant-001");
        var governanceActionSummaryEntry = Assert.Single(governanceActionsSurface.Entries, entry => entry.Id == "tenant-governance-action-runtime");
        var tenantGovernanceActionEntry = Assert.Single(governanceActionsSurface.Entries, entry => entry.Id == "tenant-governance-actions:tenant-001");
        var diagnosticsConvention = Assert.Single(diagnosticsCatalog.GetBySource("Cephalon.MultiTenancy.Governance"));

        var allowed = await evaluator.EvaluateAsync(new TenantMembershipEvaluationRequest(
            tenantId: "tenant-001",
            principalId: "user-001",
            requiredRoles: ["admin"]));
        var missingRole = await evaluator.EvaluateAsync(new TenantMembershipEvaluationRequest(
            tenantId: "tenant-001",
            principalId: "user-001",
            requiredRoles: ["owner"]));
        var suspended = await evaluator.EvaluateAsync(new TenantMembershipEvaluationRequest(
            tenantId: "tenant-001",
            principalId: "user-002"));
        var validInvitation = await invitationValidator.ValidateAsync(new TenantInvitationValidationRequest(
            tenantId: "tenant-001",
            invitationId: "invite-001",
            inviteeId: "user-003",
            requiredRoles: ["member"],
            atUtc: new DateTimeOffset(2026, 04, 28, 0, 0, 0, TimeSpan.Zero)));
        var revokedInvitation = await invitationValidator.ValidateAsync(new TenantInvitationValidationRequest(
            tenantId: "tenant-001",
            invitationId: "invite-002",
            atUtc: new DateTimeOffset(2026, 04, 28, 0, 0, 0, TimeSpan.Zero)));
        var validDomain = await domainValidator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "ACME.EXAMPLE.",
            atUtc: new DateTimeOffset(2026, 04, 28, 0, 0, 0, TimeSpan.Zero)));
        var pendingDomain = await domainValidator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "pending.example",
            atUtc: new DateTimeOffset(2026, 04, 28, 0, 0, 0, TimeSpan.Zero)));
        var approvedAction = await governanceActionDecider.DecideAsync(new TenantGovernanceActionDecisionRequest(
            tenantId: "tenant-001",
            actionId: "approve-membership-001",
            actionKind: TenantGovernanceActionKinds.MembershipChange,
            subjectKind: "user",
            subjectId: "user-001",
            atUtc: new DateTimeOffset(2026, 04, 28, 0, 0, 0, TimeSpan.Zero)));
        var remediationRequiredAction = await governanceActionDecider.DecideAsync(new TenantGovernanceActionDecisionRequest(
            tenantId: "tenant-001",
            actionId: "remediate-domain-001",
            actionKind: TenantGovernanceActionKinds.Remediation,
            subjectKind: "domain",
            subjectId: "pending.example",
            atUtc: new DateTimeOffset(2026, 04, 28, 0, 0, 0, TimeSpan.Zero)));

        Assert.Equal(2, catalog.Memberships.Count);
        Assert.Equal(2, invitationCatalog.Invitations.Count);
        Assert.Equal(2, domainCatalog.DomainOwnerships.Count);
        Assert.Equal(2, governanceActionCatalog.Actions.Count);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.membership.catalog");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.membership.evaluation");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.invitation.catalog");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.invitation.validation");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.domain-ownership.catalog");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.domain-ownership.validation");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.governance-action.catalog");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.governance-action.store");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.governance-action.decision");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.governance-action.workflow");
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["ownership"]);
        Assert.Equal("Cephalon.MultiTenancy.Governance", summaryEntry.Metadata["package"]);
        Assert.Equal("2", summaryEntry.Metadata["membershipCount"]);
        Assert.Equal("true", summaryEntry.Metadata["evaluationEnabled"]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["evaluationOwnership"]);
        Assert.Equal("2", tenantEntry.Metadata["membershipCount"]);
        Assert.Equal("1", tenantEntry.Metadata["activeMembershipCount"]);
        Assert.Equal("1", tenantEntry.Metadata["suspendedMembershipCount"]);
        Assert.Equal("admin,member", tenantEntry.Metadata["roles"]);
        Assert.Equal("cephalon-managed", invitationSummaryEntry.Metadata["ownership"]);
        Assert.Equal("Cephalon.MultiTenancy.Governance", invitationSummaryEntry.Metadata["package"]);
        Assert.Equal("2", invitationSummaryEntry.Metadata["invitationCount"]);
        Assert.Equal("true", invitationSummaryEntry.Metadata["validationEnabled"]);
        Assert.Equal("cephalon-managed", invitationSummaryEntry.Metadata["validationOwnership"]);
        Assert.Equal("pending:1,revoked:1", invitationSummaryEntry.Metadata["statusBreakdown"]);
        Assert.Equal("2", tenantInvitationEntry.Metadata["invitationCount"]);
        Assert.Equal("1", tenantInvitationEntry.Metadata["pendingInvitationCount"]);
        Assert.Equal("1", tenantInvitationEntry.Metadata["revokedInvitationCount"]);
        Assert.Equal("member", tenantInvitationEntry.Metadata["roles"]);
        Assert.Equal("cephalon-managed", domainSummaryEntry.Metadata["ownership"]);
        Assert.Equal("Cephalon.MultiTenancy.Governance", domainSummaryEntry.Metadata["package"]);
        Assert.Equal("2", domainSummaryEntry.Metadata["domainOwnershipCount"]);
        Assert.Equal("true", domainSummaryEntry.Metadata["validationEnabled"]);
        Assert.Equal("cephalon-managed", domainSummaryEntry.Metadata["validationOwnership"]);
        Assert.Equal("application-managed", domainSummaryEntry.Metadata["verificationExecutionOwnership"]);
        Assert.Equal("pending:1,verified:1", domainSummaryEntry.Metadata["statusBreakdown"]);
        Assert.Equal("dns-txt:1,http-file:1", domainSummaryEntry.Metadata["verificationMethodBreakdown"]);
        Assert.Equal("2", tenantDomainEntry.Metadata["domainOwnershipCount"]);
        Assert.Equal("1", tenantDomainEntry.Metadata["verifiedDomainOwnershipCount"]);
        Assert.Equal("1", tenantDomainEntry.Metadata["pendingDomainOwnershipCount"]);
        Assert.Equal("dns-txt:1,http-file:1", tenantDomainEntry.Metadata["verificationMethodBreakdown"]);
        Assert.Equal("cephalon-managed", governanceActionSummaryEntry.Metadata["ownership"]);
        Assert.Equal("Cephalon.MultiTenancy.Governance", governanceActionSummaryEntry.Metadata["package"]);
        Assert.Equal("2", governanceActionSummaryEntry.Metadata["actionCount"]);
        Assert.Equal("true", governanceActionSummaryEntry.Metadata["decisionEnabled"]);
        Assert.Equal("cephalon-managed", governanceActionSummaryEntry.Metadata["decisionOwnership"]);
        Assert.Equal("true", governanceActionSummaryEntry.Metadata["workflowEnabled"]);
        Assert.Equal("cephalon-managed", governanceActionSummaryEntry.Metadata["workflowExecutionOwnership"]);
        Assert.Equal("0", governanceActionSummaryEntry.Metadata["runtimeActionCount"]);
        Assert.Equal("in-memory", governanceActionSummaryEntry.Metadata["actionStoreKind"]);
        Assert.Equal("false", governanceActionSummaryEntry.Metadata["actionStoreDurable"]);
        Assert.Equal("cephalon-managed", governanceActionSummaryEntry.Metadata["actionStoreOwnership"]);
        Assert.Equal("application-managed", governanceActionSummaryEntry.Metadata["durableStoreOwnership"]);
        Assert.Equal("application-managed", governanceActionSummaryEntry.Metadata["notificationDeliveryOwnership"]);
        Assert.Equal("approved:1,remediation-required:1", governanceActionSummaryEntry.Metadata["statusBreakdown"]);
        Assert.Equal("membership-change:1,remediation:1", governanceActionSummaryEntry.Metadata["actionKindBreakdown"]);
        Assert.Equal("2", tenantGovernanceActionEntry.Metadata["actionCount"]);
        Assert.Equal("1", tenantGovernanceActionEntry.Metadata["approvedActionCount"]);
        Assert.Equal("1", tenantGovernanceActionEntry.Metadata["remediationRequiredActionCount"]);
        Assert.Equal("membership-change:1,remediation:1", tenantGovernanceActionEntry.Metadata["actionKindBreakdown"]);
        Assert.True(allowed.Allowed);
        Assert.Equal(TenantMembershipEvaluationOutcomes.Allowed, allowed.Outcome);
        Assert.False(missingRole.Allowed);
        Assert.Equal(TenantMembershipEvaluationOutcomes.MissingRole, missingRole.Outcome);
        Assert.Equal(["owner"], missingRole.MissingRoles);
        Assert.False(suspended.Allowed);
        Assert.Equal(TenantMembershipEvaluationOutcomes.Suspended, suspended.Outcome);
        Assert.True(validInvitation.Valid);
        Assert.Equal(TenantInvitationValidationOutcomes.Valid, validInvitation.Outcome);
        Assert.False(revokedInvitation.Valid);
        Assert.Equal(TenantInvitationValidationOutcomes.Revoked, revokedInvitation.Outcome);
        Assert.True(validDomain.Valid);
        Assert.Equal("acme.example", validDomain.DomainName);
        Assert.Equal(TenantDomainOwnershipValidationOutcomes.Valid, validDomain.Outcome);
        Assert.False(pendingDomain.Valid);
        Assert.Equal(TenantDomainOwnershipValidationOutcomes.Pending, pendingDomain.Outcome);
        Assert.True(approvedAction.Allowed);
        Assert.Equal(TenantGovernanceActionDecisionOutcomes.Allowed, approvedAction.Outcome);
        Assert.False(remediationRequiredAction.Allowed);
        Assert.Equal(TenantGovernanceActionDecisionOutcomes.RemediationRequired, remediationRequiredAction.Outcome);
        Assert.Equal(4510, diagnosticsConvention.MinimumEventId);
        Assert.NotNull(governanceActionWorkflow);
        Assert.Equal(4521, diagnosticsConvention.MaximumEventId);
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4510 && entry.Name == "TenantMembershipEvaluationAllowed");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4511 && entry.Name == "TenantMembershipEvaluationDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4512 && entry.Name == "TenantInvitationValidationAllowed");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4513 && entry.Name == "TenantInvitationValidationDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4514 && entry.Name == "TenantDomainOwnershipValidationAllowed");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4515 && entry.Name == "TenantDomainOwnershipValidationDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4516 && entry.Name == "TenantGovernanceActionDecisionAllowed");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4517 && entry.Name == "TenantGovernanceActionDecisionDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4518 && entry.Name == "TenantGovernanceActionWorkflowApplied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4519 && entry.Name == "TenantGovernanceActionWorkflowDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4520 && entry.Name == "TenantGovernanceActionStorePersisted");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4521 && entry.Name == "TenantGovernanceActionStorePersistenceFailed");
    }

    [Fact]
    public async Task TenantMembershipCatalogMergesContributorMemberships()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantMembershipContributor>(new TestTenantMembershipContributor());
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddModule(new PlatformTestModule());
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Memberships.Add(new TenantMembershipDescriptor(
                    tenantId: "tenant-001",
                    principalId: "user-001",
                    roles: ["member"]));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<ITenantMembershipCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var membershipsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-memberships");
        var summaryEntry = Assert.Single(membershipsSurface.Entries, entry => entry.Id == "tenant-membership-runtime");

        Assert.Equal(2, catalog.Memberships.Count);
        Assert.Single(catalog.GetByTenantId("tenant-002"));
        Assert.Single(catalog.GetByPrincipalId("group-001"));
        Assert.Single(catalog.GetByTenantAndPrincipal("tenant-002", "group-001"));
        Assert.Single(catalog.GetByTenantPrincipalAndKind("tenant-002", "group", "group-001"));
        Assert.Empty(catalog.GetByTenantPrincipalAndKind("tenant-002", "user", "group-001"));
        Assert.Equal("1", summaryEntry.Metadata["contributorCount"]);
        Assert.Contains(membershipsSurface.Entries, entry =>
            entry.Id == "tenant-membership:tenant-002" &&
            entry.Metadata["principalKindBreakdown"] == "group:1");
    }

    [Fact]
    public async Task TenantMembershipEvaluationUsesPrincipalKindToPreventCrossKindRoleBleed()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Memberships.Add(new TenantMembershipDescriptor(
                    tenantId: "tenant-001",
                    principalId: "shared-principal",
                    principalKind: "user",
                    roles: ["member"]));
                options.Memberships.Add(new TenantMembershipDescriptor(
                    tenantId: "tenant-001",
                    principalId: "shared-principal",
                    principalKind: "service",
                    roles: ["admin"]));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var evaluator = provider.GetRequiredService<ITenantMembershipEvaluator>();

        var userResult = await evaluator.EvaluateAsync(new TenantMembershipEvaluationRequest(
            tenantId: "tenant-001",
            principalId: "shared-principal",
            requiredRoles: ["admin"]));
        var serviceResult = await evaluator.EvaluateAsync(new TenantMembershipEvaluationRequest(
            tenantId: "tenant-001",
            principalId: "shared-principal",
            requiredRoles: ["admin"],
            principalKind: "service"));

        Assert.False(userResult.Allowed);
        Assert.Equal("user", userResult.PrincipalKind);
        Assert.Equal(TenantMembershipEvaluationOutcomes.MissingRole, userResult.Outcome);
        Assert.True(serviceResult.Allowed);
        Assert.Equal("service", serviceResult.PrincipalKind);
        Assert.Equal(TenantMembershipEvaluationOutcomes.Allowed, serviceResult.Outcome);
    }

    [Fact]
    public async Task TenantInvitationCatalogMergesContributorInvitations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantInvitationContributor>(new TestTenantInvitationContributor());
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddModule(new PlatformTestModule());
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-001",
                    tenantId: "tenant-001",
                    inviteeId: "user-001",
                    roles: ["member"]));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var invitationsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-invitations");
        var summaryEntry = Assert.Single(invitationsSurface.Entries, entry => entry.Id == "tenant-invitation-runtime");

        Assert.Equal(2, catalog.Invitations.Count);
        Assert.Single(catalog.GetByTenantId("tenant-002"));
        Assert.Single(catalog.GetByInviteeId("group-001"));
        Assert.Single(catalog.GetByInvitationId("invite-002"));
        Assert.Single(catalog.GetByTenantAndInvitation("tenant-002", "invite-002"));
        Assert.Equal("1", summaryEntry.Metadata["contributorCount"]);
        Assert.Contains(invitationsSurface.Entries, entry =>
            entry.Id == "tenant-invitations:tenant-002" &&
            entry.Metadata["inviteeKindBreakdown"] == "group:1");
    }

    [Fact]
    public async Task TenantInvitationValidationUsesInviteeKindToPreventCrossKindInvitationUse()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-service",
                    tenantId: "tenant-001",
                    inviteeId: "shared-invitee",
                    inviteeKind: "service",
                    roles: ["admin"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<ITenantInvitationValidator>();

        var userResult = await validator.ValidateAsync(new TenantInvitationValidationRequest(
            tenantId: "tenant-001",
            invitationId: "invite-service",
            inviteeId: "shared-invitee",
            inviteeKind: "user",
            requiredRoles: ["admin"],
            atUtc: new DateTimeOffset(2026, 04, 28, 0, 0, 0, TimeSpan.Zero)));
        var serviceResult = await validator.ValidateAsync(new TenantInvitationValidationRequest(
            tenantId: "tenant-001",
            invitationId: "invite-service",
            inviteeId: "shared-invitee",
            inviteeKind: "service",
            requiredRoles: ["admin"],
            atUtc: new DateTimeOffset(2026, 04, 28, 0, 0, 0, TimeSpan.Zero)));

        Assert.False(userResult.Valid);
        Assert.Equal("user", userResult.InviteeKind);
        Assert.Equal(TenantInvitationValidationOutcomes.InviteeMismatch, userResult.Outcome);
        Assert.True(serviceResult.Valid);
        Assert.Equal("service", serviceResult.InviteeKind);
        Assert.Equal(TenantInvitationValidationOutcomes.Valid, serviceResult.Outcome);
    }

    [Fact]
    public async Task TenantDomainOwnershipCatalogMergesContributorDomains()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantDomainOwnershipContributor>(new TestTenantDomainOwnershipContributor());
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddModule(new PlatformTestModule());
            engine.AddMultiTenancyGovernance(options =>
            {
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "app.example",
                    status: TenantDomainOwnershipStatuses.Verified,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var domainsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-domain-ownership");
        var summaryEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership-runtime");

        Assert.Equal(2, catalog.DomainOwnerships.Count);
        Assert.Single(catalog.GetByTenantId("tenant-002"));
        Assert.Single(catalog.GetByDomainName("Docs.Example."));
        Assert.Single(catalog.GetByTenantAndDomain("tenant-002", "DOCS.EXAMPLE"));
        Assert.Empty(catalog.GetByTenantAndDomain("tenant-001", "docs.example"));
        Assert.Equal("1", summaryEntry.Metadata["contributorCount"]);
        Assert.Contains(domainsSurface.Entries, entry =>
            entry.Id == "tenant-domain-ownership:tenant-002" &&
            entry.Metadata["verificationMethodBreakdown"] == "http-file:1");
    }

    [Fact]
    public async Task TenantDomainOwnershipValidationPreventsCrossTenantDomainUse()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddMultiTenancyGovernance(options =>
            {
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "shared.example",
                    status: TenantDomainOwnershipStatuses.Verified,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();

        var mismatchResult = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-002",
            domainName: "SHARED.EXAMPLE.",
            atUtc: new DateTimeOffset(2026, 04, 28, 0, 0, 0, TimeSpan.Zero)));
        var validResult = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "shared.example",
            atUtc: new DateTimeOffset(2026, 04, 28, 0, 0, 0, TimeSpan.Zero)));

        Assert.False(mismatchResult.Valid);
        Assert.Equal("shared.example", mismatchResult.DomainName);
        Assert.Equal(TenantDomainOwnershipValidationOutcomes.TenantMismatch, mismatchResult.Outcome);
        Assert.True(validResult.Valid);
        Assert.Equal(TenantDomainOwnershipValidationOutcomes.Valid, validResult.Outcome);
    }

    [Fact]
    public async Task TenantGovernanceActionCatalogMergesContributorActions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantGovernanceActionContributor>(new TestTenantGovernanceActionContributor());
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddModule(new PlatformTestModule());
            engine.AddMultiTenancyGovernance(options =>
            {
                options.GovernanceActions.Add(new TenantGovernanceActionDescriptor(
                    actionId: "action-001",
                    tenantId: "tenant-001",
                    actionKind: TenantGovernanceActionKinds.MembershipChange,
                    subjectKind: "user",
                    subjectId: "user-001",
                    status: TenantGovernanceActionStatuses.Approved));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<ITenantGovernanceActionCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var actionsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-governance-actions");
        var summaryEntry = Assert.Single(actionsSurface.Entries, entry => entry.Id == "tenant-governance-action-runtime");

        Assert.Equal(2, catalog.Actions.Count);
        Assert.Single(catalog.GetByTenantId("tenant-002"));
        Assert.Single(catalog.GetByActionId("action-002"));
        Assert.Single(catalog.GetByTenantAndAction("tenant-002", "action-002"));
        Assert.Empty(catalog.GetByTenantAndAction("tenant-001", "action-002"));
        Assert.Equal("1", summaryEntry.Metadata["contributorCount"]);
        Assert.Contains(actionsSurface.Entries, entry =>
            entry.Id == "tenant-governance-actions:tenant-002" &&
            entry.Metadata["actionKindBreakdown"] == "remediation:1");
    }

    [Fact]
    public async Task TenantGovernanceActionDecisionPreventsCrossTenantOrSubjectUse()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddMultiTenancyGovernance(options =>
            {
                options.GovernanceActions.Add(new TenantGovernanceActionDescriptor(
                    actionId: "shared-action",
                    tenantId: "tenant-001",
                    actionKind: TenantGovernanceActionKinds.MembershipChange,
                    subjectKind: "user",
                    subjectId: "user-001",
                    status: TenantGovernanceActionStatuses.Approved));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var decider = provider.GetRequiredService<ITenantGovernanceActionDecider>();

        var tenantMismatch = await decider.DecideAsync(new TenantGovernanceActionDecisionRequest(
            tenantId: "tenant-002",
            actionId: "shared-action",
            actionKind: TenantGovernanceActionKinds.MembershipChange,
            subjectKind: "user",
            subjectId: "user-001",
            atUtc: new DateTimeOffset(2026, 04, 28, 0, 0, 0, TimeSpan.Zero)));
        var subjectMismatch = await decider.DecideAsync(new TenantGovernanceActionDecisionRequest(
            tenantId: "tenant-001",
            actionId: "shared-action",
            actionKind: TenantGovernanceActionKinds.MembershipChange,
            subjectKind: "user",
            subjectId: "user-002",
            atUtc: new DateTimeOffset(2026, 04, 28, 0, 0, 0, TimeSpan.Zero)));
        var allowed = await decider.DecideAsync(new TenantGovernanceActionDecisionRequest(
            tenantId: "tenant-001",
            actionId: "shared-action",
            actionKind: TenantGovernanceActionKinds.MembershipChange,
            subjectKind: "user",
            subjectId: "user-001",
            atUtc: new DateTimeOffset(2026, 04, 28, 0, 0, 0, TimeSpan.Zero)));

        Assert.False(tenantMismatch.Allowed);
        Assert.Equal(TenantGovernanceActionDecisionOutcomes.TenantMismatch, tenantMismatch.Outcome);
        Assert.False(subjectMismatch.Allowed);
        Assert.Equal(TenantGovernanceActionDecisionOutcomes.SubjectMismatch, subjectMismatch.Outcome);
        Assert.True(allowed.Allowed);
        Assert.Equal(TenantGovernanceActionDecisionOutcomes.Allowed, allowed.Outcome);
    }

    [Fact]
    public async Task TenantGovernanceActionWorkflowTransitionsFeedCatalogDecisionAndSurface()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddMultiTenancyGovernance();
        });

        await using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<ITenantGovernanceActionWorkflow>();
        var catalog = provider.GetRequiredService<ITenantGovernanceActionCatalog>();
        var decider = provider.GetRequiredService<ITenantGovernanceActionDecider>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var created = await workflow.ApplyAsync(new TenantGovernanceActionWorkflowRequest(
            command: TenantGovernanceActionWorkflowCommands.Request,
            tenantId: "tenant-001",
            actionId: "workflow-action-001",
            actionKind: TenantGovernanceActionKinds.MembershipChange,
            subjectKind: "user",
            subjectId: "user-001",
            actor: "operator-001",
            reason: "Membership change requires owner approval.",
            atUtc: new DateTimeOffset(2026, 04, 29, 0, 0, 0, TimeSpan.Zero)));
        var pendingDecision = await decider.DecideAsync(new TenantGovernanceActionDecisionRequest(
            tenantId: "tenant-001",
            actionId: "workflow-action-001",
            actionKind: TenantGovernanceActionKinds.MembershipChange,
            subjectKind: "user",
            subjectId: "user-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 0, 0, 0, TimeSpan.Zero)));
        var approved = await workflow.ApplyAsync(new TenantGovernanceActionWorkflowRequest(
            command: TenantGovernanceActionWorkflowCommands.Approve,
            tenantId: "tenant-001",
            actionId: "workflow-action-001",
            actionKind: TenantGovernanceActionKinds.MembershipChange,
            subjectKind: "user",
            subjectId: "user-001",
            actor: "tenant-owner",
            correlationId: "corr-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 0, 5, 0, TimeSpan.Zero)));
        var allowedDecision = await decider.DecideAsync(new TenantGovernanceActionDecisionRequest(
            tenantId: "tenant-001",
            actionId: "workflow-action-001",
            actionKind: TenantGovernanceActionKinds.MembershipChange,
            subjectKind: "user",
            subjectId: "user-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 0, 10, 0, TimeSpan.Zero)));
        var actionsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-governance-actions");
        var summaryEntry = Assert.Single(actionsSurface.Entries, entry => entry.Id == "tenant-governance-action-runtime");
        var tenantEntry = Assert.Single(actionsSurface.Entries, entry => entry.Id == "tenant-governance-actions:tenant-001");

        Assert.True(created.Applied);
        Assert.Equal(TenantGovernanceActionWorkflowOutcomes.Created, created.Outcome);
        Assert.Null(created.PreviousStatus);
        Assert.Equal(TenantGovernanceActionStatuses.PendingApproval, created.CurrentStatus);
        Assert.False(pendingDecision.Allowed);
        Assert.Equal(TenantGovernanceActionDecisionOutcomes.PendingApproval, pendingDecision.Outcome);
        Assert.True(approved.Applied);
        Assert.Equal(TenantGovernanceActionWorkflowOutcomes.Applied, approved.Outcome);
        Assert.Equal(TenantGovernanceActionStatuses.PendingApproval, approved.PreviousStatus);
        Assert.Equal(TenantGovernanceActionStatuses.Approved, approved.CurrentStatus);
        Assert.True(allowedDecision.Allowed);
        Assert.Equal(TenantGovernanceActionDecisionOutcomes.Allowed, allowedDecision.Outcome);
        var action = Assert.Single(catalog.Actions);
        Assert.Equal(TenantGovernanceActionStatuses.Approved, action.Status);
        Assert.Equal("tenant-owner", action.ApprovedBy);
        Assert.Equal("approve", action.Metadata["lastWorkflowCommand"]);
        Assert.Equal("corr-001", action.Metadata["lastWorkflowCorrelationId"]);
        Assert.Equal("1", summaryEntry.Metadata["actionCount"]);
        Assert.Equal("1", summaryEntry.Metadata["runtimeActionCount"]);
        Assert.Equal("true", summaryEntry.Metadata["workflowEnabled"]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["workflowExecutionOwnership"]);
        Assert.Equal("approved:1", summaryEntry.Metadata["statusBreakdown"]);
        Assert.Equal("1", tenantEntry.Metadata["approvedActionCount"]);
    }

    [Fact]
    public async Task TenantGovernanceActionWorkflowPersistsThroughFileBackedStore()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"cephalon-governance-{Guid.NewGuid():N}");
        var storePath = Path.Combine(tempRoot, "tenant-governance-actions.json");
        try
        {
            var services = new ServiceCollection();
            services.AddCephalon(engine =>
            {
                engine.UseSettings(new EngineSettings(
                    blueprint: "Microservice",
                    technologies: ["MultiTenancy"],
                    tenancy: new TenancySettings(
                        enabled: true,
                        mode: "SharedDatabase")));
                engine.AddMultiTenancyGovernance(options =>
                {
                    options.GovernanceActionStoreFilePath = storePath;
                });
            });

            await using (var provider = services.BuildServiceProvider())
            {
                var workflow = provider.GetRequiredService<ITenantGovernanceActionWorkflow>();

                var created = await workflow.ApplyAsync(new TenantGovernanceActionWorkflowRequest(
                    command: TenantGovernanceActionWorkflowCommands.Request,
                    tenantId: "tenant-001",
                    actionId: "durable-action-001",
                    actionKind: TenantGovernanceActionKinds.Remediation,
                    subjectKind: "domain",
                    subjectId: "docs.example",
                    actor: "operator-001",
                    atUtc: new DateTimeOffset(2026, 04, 29, 1, 0, 0, TimeSpan.Zero)));
                var approved = await workflow.ApplyAsync(new TenantGovernanceActionWorkflowRequest(
                    command: TenantGovernanceActionWorkflowCommands.Approve,
                    tenantId: "tenant-001",
                    actionId: "durable-action-001",
                    actionKind: TenantGovernanceActionKinds.Remediation,
                    subjectKind: "domain",
                    subjectId: "docs.example",
                    actor: "tenant-owner",
                    atUtc: new DateTimeOffset(2026, 04, 29, 1, 5, 0, TimeSpan.Zero)));

                Assert.True(created.Applied);
                Assert.True(approved.Applied);
            }

            var restartedServices = new ServiceCollection();
            restartedServices.AddCephalon(engine =>
            {
                engine.UseSettings(new EngineSettings(
                    blueprint: "Microservice",
                    technologies: ["MultiTenancy"],
                    tenancy: new TenancySettings(
                        enabled: true,
                        mode: "SharedDatabase")));
                engine.AddMultiTenancyGovernance(options =>
                {
                    options.GovernanceActionStoreFilePath = storePath;
                });
            });

            await using var restartedProvider = restartedServices.BuildServiceProvider();
            var catalog = restartedProvider.GetRequiredService<ITenantGovernanceActionCatalog>();
            var decider = restartedProvider.GetRequiredService<ITenantGovernanceActionDecider>();
            var technologyCatalog = restartedProvider.GetRequiredService<ITechnologyRuntimeCatalog>();

            var action = Assert.Single(catalog.Actions);
            var decision = await decider.DecideAsync(new TenantGovernanceActionDecisionRequest(
                tenantId: "tenant-001",
                actionId: "durable-action-001",
                actionKind: TenantGovernanceActionKinds.Remediation,
                subjectKind: "domain",
                subjectId: "docs.example",
                atUtc: new DateTimeOffset(2026, 04, 29, 1, 10, 0, TimeSpan.Zero)));
            var actionsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-governance-actions");
            var summaryEntry = Assert.Single(actionsSurface.Entries, entry => entry.Id == "tenant-governance-action-runtime");

            Assert.Equal(TenantGovernanceActionStatuses.Approved, action.Status);
            Assert.Equal("tenant-owner", action.ApprovedBy);
            Assert.True(decision.Allowed);
            Assert.Equal("file", summaryEntry.Metadata["actionStoreKind"]);
            Assert.Equal("true", summaryEntry.Metadata["actionStoreDurable"]);
            Assert.Equal("cephalon-managed", summaryEntry.Metadata["durableStoreOwnership"]);
            Assert.Equal("1", summaryEntry.Metadata["runtimeActionCount"]);
            Assert.True(File.Exists(storePath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task TenantGovernanceActionWorkflowReportsStoreFailuresWithoutApplyingTransition()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantGovernanceActionStore>(new FailingTenantGovernanceActionStore());
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddMultiTenancyGovernance();
        });

        await using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<ITenantGovernanceActionWorkflow>();
        var catalog = provider.GetRequiredService<ITenantGovernanceActionCatalog>();

        var result = await workflow.ApplyAsync(new TenantGovernanceActionWorkflowRequest(
            command: TenantGovernanceActionWorkflowCommands.Request,
            tenantId: "tenant-001",
            actionId: "failing-action-001",
            actionKind: TenantGovernanceActionKinds.MembershipChange));

        Assert.False(result.Applied);
        Assert.Equal(TenantGovernanceActionWorkflowOutcomes.StoreFailed, result.Outcome);
        Assert.Empty(catalog.Actions);
    }

    [Fact]
    public async Task TenantGovernanceActionWorkflowRejectsInvalidTransitionsAndBoundaries()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddMultiTenancyGovernance(options =>
            {
                options.GovernanceActions.Add(new TenantGovernanceActionDescriptor(
                    actionId: "shared-action",
                    tenantId: "tenant-001",
                    actionKind: TenantGovernanceActionKinds.Remediation,
                    subjectKind: "domain",
                    subjectId: "docs.example",
                    status: TenantGovernanceActionStatuses.Approved));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<ITenantGovernanceActionWorkflow>();

        var tenantMismatch = await workflow.ApplyAsync(new TenantGovernanceActionWorkflowRequest(
            command: TenantGovernanceActionWorkflowCommands.RequireRemediation,
            tenantId: "tenant-002",
            actionId: "shared-action",
            actionKind: TenantGovernanceActionKinds.Remediation));
        var subjectMismatch = await workflow.ApplyAsync(new TenantGovernanceActionWorkflowRequest(
            command: TenantGovernanceActionWorkflowCommands.RequireRemediation,
            tenantId: "tenant-001",
            actionId: "shared-action",
            actionKind: TenantGovernanceActionKinds.Remediation,
            subjectKind: "domain",
            subjectId: "other.example"));
        var invalidTransition = await workflow.ApplyAsync(new TenantGovernanceActionWorkflowRequest(
            command: TenantGovernanceActionWorkflowCommands.MarkRemediated,
            tenantId: "tenant-001",
            actionId: "shared-action",
            actionKind: TenantGovernanceActionKinds.Remediation,
            subjectKind: "domain",
            subjectId: "docs.example"));

        Assert.False(tenantMismatch.Applied);
        Assert.Equal(TenantGovernanceActionWorkflowOutcomes.TenantMismatch, tenantMismatch.Outcome);
        Assert.False(subjectMismatch.Applied);
        Assert.Equal(TenantGovernanceActionWorkflowOutcomes.SubjectMismatch, subjectMismatch.Outcome);
        Assert.False(invalidTransition.Applied);
        Assert.Equal(TenantGovernanceActionWorkflowOutcomes.InvalidTransition, invalidTransition.Outcome);
        Assert.Equal(TenantGovernanceActionStatuses.Approved, invalidTransition.PreviousStatus);
        Assert.Equal(TenantGovernanceActionStatuses.Approved, invalidTransition.CurrentStatus);
    }

    private sealed class TestTenantMembershipContributor : ITenantMembershipContributor
    {
        public void RegisterMemberships(ITenantMembershipRegistry memberships)
        {
            memberships.Add(new TenantMembershipDescriptor(
                tenantId: "tenant-002",
                principalId: "group-001",
                principalKind: "group",
                roles: ["support"],
                sourceModuleId: "test-module"));
        }
    }

    private sealed class TestTenantInvitationContributor : ITenantInvitationContributor
    {
        public void RegisterInvitations(ITenantInvitationRegistry invitations)
        {
            invitations.Add(new TenantInvitationDescriptor(
                invitationId: "invite-002",
                tenantId: "tenant-002",
                inviteeId: "group-001",
                inviteeKind: "group",
                roles: ["support"],
                sourceModuleId: "test-module"));
        }
    }

    private sealed class TestTenantDomainOwnershipContributor : ITenantDomainOwnershipContributor
    {
        public void RegisterDomainOwnerships(ITenantDomainOwnershipRegistry domainOwnerships)
        {
            domainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                tenantId: "tenant-002",
                domainName: "docs.example",
                status: TenantDomainOwnershipStatuses.Verified,
                verificationMethod: TenantDomainVerificationMethods.HttpFile,
                sourceModuleId: "test-module"));
        }
    }

    private sealed class TestTenantGovernanceActionContributor : ITenantGovernanceActionContributor
    {
        public void RegisterGovernanceActions(ITenantGovernanceActionRegistry actions)
        {
            actions.Add(new TenantGovernanceActionDescriptor(
                actionId: "action-002",
                tenantId: "tenant-002",
                actionKind: TenantGovernanceActionKinds.Remediation,
                subjectKind: "domain",
                subjectId: "docs.example",
                status: TenantGovernanceActionStatuses.Remediated,
                sourceModuleId: "test-module"));
        }
    }

    private sealed class FailingTenantGovernanceActionStore : ITenantGovernanceActionStore
    {
        public string StoreKind => "failing-test";

        public bool IsDurable => true;

        public string Ownership => "application-managed";

        public IReadOnlyList<TenantGovernanceActionDescriptor> Actions => [];

        public int Count => 0;

        public void Upsert(TenantGovernanceActionDescriptor action)
        {
            throw new InvalidOperationException("Test store failure.");
        }
    }
}
