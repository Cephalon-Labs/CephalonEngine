using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Cephalon.MultiTenancy.Registration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        var domainWorkflow = provider.GetRequiredService<ITenantDomainOwnershipVerificationWorkflow>();
        var domainProofEvaluator = provider.GetRequiredService<ITenantDomainOwnershipProofEvaluator>();
        var domainProofChallengeIssuer = provider.GetRequiredService<ITenantDomainOwnershipProofChallengeIssuer>();
        var domainProofPublicationPlanner = provider.GetRequiredService<ITenantDomainOwnershipProofPublicationPlanner>();
        var domainHttpProofCollector = provider.GetRequiredService<ITenantDomainOwnershipHttpProofCollector>();
        var domainDnsTxtProofCollector = provider.GetRequiredService<ITenantDomainOwnershipDnsTxtProofCollector>();
        var domainProofVerificationRunner = provider.GetRequiredService<ITenantDomainOwnershipProofVerificationRunner>();
        var domainProofPollingRunner = provider.GetRequiredService<ITenantDomainOwnershipProofPollingRunner>();
        var domainProofPollingRuntimeCatalog = provider.GetRequiredService<ITenantDomainOwnershipProofPollingRuntimeCatalog>();
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
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.membership.store");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.membership.evaluation");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.invitation.catalog");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.invitation.store");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.invitation.validation");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.domain-ownership.catalog");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.domain-ownership.store");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.domain-ownership.validation");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.domain-ownership.workflow");
        var proofEvaluationCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.domain-ownership.proof-evaluation");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.domain-ownership.proof-challenge");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.domain-ownership.proof-publication-plan");
        var httpProofCollectionCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.domain-ownership.http-proof-collection");
        var dnsTxtProofCollectionCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.domain-ownership.dns-txt-proof-collection");
        var proofVerificationRunnerCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.domain-ownership.proof-verification-runner");
        var proofPollingRunnerCapability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.domain-ownership.proof-polling-runner");
        var defaultProofPollingRuntime = domainProofPollingRuntimeCatalog.Current;
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.governance-action.catalog");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.governance-action.store");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.governance-action.decision");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.governance-action.workflow");
        Assert.Equal("mixed", proofEvaluationCapability.Metadata["proofCollectionOwnership"]);
        Assert.Equal("cephalon-managed", proofEvaluationCapability.Metadata["httpProofCollectionOwnership"]);
        Assert.Equal("not-configured", proofEvaluationCapability.Metadata["dnsTxtProofCollectionOwnership"]);
        Assert.Equal("false", proofEvaluationCapability.Metadata["dnsTxtProofResolverConfigured"]);
        Assert.Equal("cephalon-managed", proofEvaluationCapability.Metadata["externalProofPollingOwnership"]);
        Assert.Equal("application-managed", proofEvaluationCapability.Metadata["backgroundProofPollingOwnership"]);
        Assert.Equal("false", proofEvaluationCapability.Metadata["backgroundProofPollingEnabled"]);
        Assert.Equal("300", proofEvaluationCapability.Metadata["backgroundProofPollingIntervalSeconds"]);
        Assert.Equal("50", proofEvaluationCapability.Metadata["backgroundProofPollingBatchLimit"]);
        Assert.Equal("cephalon-managed", httpProofCollectionCapability.Metadata["httpProofCollectionOwnership"]);
        Assert.Equal("not-configured", httpProofCollectionCapability.Metadata["dnsTxtProofCollectionOwnership"]);
        Assert.Equal("false", httpProofCollectionCapability.Metadata["dnsTxtProofResolverConfigured"]);
        Assert.Equal("cephalon-managed", httpProofCollectionCapability.Metadata["externalProofPollingOwnership"]);
        Assert.Equal("application-managed", httpProofCollectionCapability.Metadata["backgroundProofPollingOwnership"]);
        Assert.Equal("not-configured", dnsTxtProofCollectionCapability.Metadata["dnsTxtProofCollectionOwnership"]);
        Assert.Equal("false", dnsTxtProofCollectionCapability.Metadata["dnsTxtProofResolverConfigured"]);
        Assert.Equal("cephalon-managed", proofVerificationRunnerCapability.Metadata["proofVerificationRunnerOwnership"]);
        Assert.Equal("cephalon-managed", proofVerificationRunnerCapability.Metadata["proofPollingRunnerOwnership"]);
        Assert.Equal("cephalon-managed", proofVerificationRunnerCapability.Metadata["httpProofCollectionOwnership"]);
        Assert.Equal("not-configured", proofVerificationRunnerCapability.Metadata["dnsTxtProofCollectionOwnership"]);
        Assert.Equal("false", proofVerificationRunnerCapability.Metadata["dnsTxtProofResolverConfigured"]);
        Assert.Equal("cephalon-managed", proofVerificationRunnerCapability.Metadata["externalProofPollingOwnership"]);
        Assert.Equal("application-managed", proofVerificationRunnerCapability.Metadata["backgroundProofPollingOwnership"]);
        Assert.Equal("cephalon-managed", proofPollingRunnerCapability.Metadata["proofPollingRunnerOwnership"]);
        Assert.Equal("cephalon-managed", proofPollingRunnerCapability.Metadata["externalProofPollingOwnership"]);
        Assert.Equal("application-managed", proofPollingRunnerCapability.Metadata["backgroundProofPollingOwnership"]);
        Assert.Equal("false", proofPollingRunnerCapability.Metadata["backgroundProofPollingEnabled"]);
        Assert.Equal("300", proofPollingRunnerCapability.Metadata["backgroundProofPollingIntervalSeconds"]);
        Assert.Equal("50", proofPollingRunnerCapability.Metadata["backgroundProofPollingBatchLimit"]);
        Assert.Equal("application-managed", proofPollingRunnerCapability.Metadata["proofPublicationOwnership"]);
        Assert.Equal("50", proofPollingRunnerCapability.Metadata["defaultBatchLimit"]);
        Assert.False(defaultProofPollingRuntime.Enabled);
        Assert.Equal("application-managed", defaultProofPollingRuntime.Ownership);
        Assert.Equal(300, defaultProofPollingRuntime.IntervalSeconds);
        Assert.Equal(50, defaultProofPollingRuntime.BatchLimit);
        Assert.Equal(0, defaultProofPollingRuntime.RunCount);
        Assert.DoesNotContain(provider.GetServices<IHostedService>(), static service =>
            string.Equals(service.GetType().Name, "TenantDomainOwnershipProofPollingHostedService", StringComparison.Ordinal));
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["ownership"]);
        Assert.Equal("Cephalon.MultiTenancy.Governance", summaryEntry.Metadata["package"]);
        Assert.Equal("2", summaryEntry.Metadata["membershipCount"]);
        Assert.Equal("0", summaryEntry.Metadata["runtimeMembershipCount"]);
        Assert.Equal("in-memory", summaryEntry.Metadata["membershipStoreKind"]);
        Assert.Equal("false", summaryEntry.Metadata["membershipStoreDurable"]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["membershipStoreOwnership"]);
        Assert.Equal("application-managed", summaryEntry.Metadata["durableStoreOwnership"]);
        Assert.Equal("true", summaryEntry.Metadata["evaluationEnabled"]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["evaluationOwnership"]);
        Assert.Equal("2", tenantEntry.Metadata["membershipCount"]);
        Assert.Equal("1", tenantEntry.Metadata["activeMembershipCount"]);
        Assert.Equal("1", tenantEntry.Metadata["suspendedMembershipCount"]);
        Assert.Equal("admin,member", tenantEntry.Metadata["roles"]);
        Assert.Equal("cephalon-managed", invitationSummaryEntry.Metadata["ownership"]);
        Assert.Equal("Cephalon.MultiTenancy.Governance", invitationSummaryEntry.Metadata["package"]);
        Assert.Equal("2", invitationSummaryEntry.Metadata["invitationCount"]);
        Assert.Equal("0", invitationSummaryEntry.Metadata["runtimeInvitationCount"]);
        Assert.Equal("in-memory", invitationSummaryEntry.Metadata["invitationStoreKind"]);
        Assert.Equal("false", invitationSummaryEntry.Metadata["invitationStoreDurable"]);
        Assert.Equal("cephalon-managed", invitationSummaryEntry.Metadata["invitationStoreOwnership"]);
        Assert.Equal("application-managed", invitationSummaryEntry.Metadata["durableStoreOwnership"]);
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
        Assert.Equal("0", domainSummaryEntry.Metadata["runtimeDomainOwnershipCount"]);
        Assert.Equal("in-memory", domainSummaryEntry.Metadata["domainOwnershipStoreKind"]);
        Assert.Equal("false", domainSummaryEntry.Metadata["domainOwnershipStoreDurable"]);
        Assert.Equal("cephalon-managed", domainSummaryEntry.Metadata["domainOwnershipStoreOwnership"]);
        Assert.Equal("application-managed", domainSummaryEntry.Metadata["durableStoreOwnership"]);
        Assert.Equal("true", domainSummaryEntry.Metadata["validationEnabled"]);
        Assert.Equal("cephalon-managed", domainSummaryEntry.Metadata["validationOwnership"]);
        Assert.Equal("true", domainSummaryEntry.Metadata["verificationWorkflowEnabled"]);
        Assert.Equal("cephalon-managed", domainSummaryEntry.Metadata["verificationWorkflowOwnership"]);
        Assert.Equal("true", domainSummaryEntry.Metadata["proofEvaluationEnabled"]);
        Assert.Equal("cephalon-managed", domainSummaryEntry.Metadata["proofEvaluationOwnership"]);
        Assert.Equal("true", domainSummaryEntry.Metadata["proofChallengeIssuanceEnabled"]);
        Assert.Equal("cephalon-managed", domainSummaryEntry.Metadata["proofChallengeIssuanceOwnership"]);
        Assert.Equal("cephalon-managed", domainSummaryEntry.Metadata["proofChallengeGenerationOwnership"]);
        Assert.Equal("true", domainSummaryEntry.Metadata["proofPublicationPlanningEnabled"]);
        Assert.Equal("cephalon-managed", domainSummaryEntry.Metadata["proofPublicationPlanningOwnership"]);
        Assert.Equal("true", domainSummaryEntry.Metadata["httpProofCollectionEnabled"]);
        Assert.Equal("cephalon-managed", domainSummaryEntry.Metadata["httpProofCollectionOwnership"]);
        Assert.Equal("true", domainSummaryEntry.Metadata["dnsTxtProofCollectionEnabled"]);
        Assert.Equal("false", domainSummaryEntry.Metadata["dnsTxtProofResolverConfigured"]);
        Assert.Equal("not-configured", domainSummaryEntry.Metadata["dnsTxtProofCollectionOwnership"]);
        Assert.Equal("true", domainSummaryEntry.Metadata["proofVerificationRunnerEnabled"]);
        Assert.Equal("cephalon-managed", domainSummaryEntry.Metadata["proofVerificationRunnerOwnership"]);
        Assert.Equal("true", domainSummaryEntry.Metadata["proofPollingRunnerEnabled"]);
        Assert.Equal("cephalon-managed", domainSummaryEntry.Metadata["proofPollingRunnerOwnership"]);
        Assert.Equal("cephalon-managed", domainSummaryEntry.Metadata["externalProofPollingOwnership"]);
        Assert.Equal("false", domainSummaryEntry.Metadata["backgroundProofPollingEnabled"]);
        Assert.Equal("application-managed", domainSummaryEntry.Metadata["backgroundProofPollingOwnership"]);
        Assert.Equal("300", domainSummaryEntry.Metadata["backgroundProofPollingIntervalSeconds"]);
        Assert.Equal("50", domainSummaryEntry.Metadata["backgroundProofPollingBatchLimit"]);
        Assert.Equal("true", domainSummaryEntry.Metadata["backgroundProofPollingRunOnStartup"]);
        Assert.Equal("0", domainSummaryEntry.Metadata["backgroundProofPollingRunCount"]);
        Assert.Equal("none", domainSummaryEntry.Metadata["backgroundProofPollingLastOutcome"]);
        Assert.Equal("50", domainSummaryEntry.Metadata["proofPollingDefaultBatchLimit"]);
        Assert.Equal("application-managed", domainSummaryEntry.Metadata["proofPublicationOwnership"]);
        Assert.Equal("application-managed", domainSummaryEntry.Metadata["verificationExecutionOwnership"]);
        Assert.Equal("mixed", domainSummaryEntry.Metadata["dnsHttpProofCollectionOwnership"]);
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
        Assert.NotNull(domainWorkflow);
        Assert.NotNull(domainProofEvaluator);
        Assert.NotNull(domainProofChallengeIssuer);
        Assert.NotNull(domainProofPublicationPlanner);
        Assert.NotNull(domainHttpProofCollector);
        Assert.NotNull(domainDnsTxtProofCollector);
        Assert.NotNull(domainProofVerificationRunner);
        Assert.NotNull(domainProofPollingRunner);
        Assert.NotNull(governanceActionWorkflow);
        Assert.Equal(4543, diagnosticsConvention.MaximumEventId);
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
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4522 && entry.Name == "TenantDomainOwnershipVerificationWorkflowApplied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4523 && entry.Name == "TenantDomainOwnershipVerificationWorkflowDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4524 && entry.Name == "TenantDomainOwnershipStorePersisted");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4525 && entry.Name == "TenantDomainOwnershipStorePersistenceFailed");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4526 && entry.Name == "TenantDomainOwnershipProofEvaluationVerified");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4527 && entry.Name == "TenantDomainOwnershipProofEvaluationDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4528 && entry.Name == "TenantDomainOwnershipProofChallengeIssued");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4529 && entry.Name == "TenantDomainOwnershipProofChallengeDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4530 && entry.Name == "TenantDomainOwnershipProofPublicationPlanned");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4531 && entry.Name == "TenantDomainOwnershipProofPublicationPlanDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4532 && entry.Name == "TenantDomainOwnershipHttpProofCollected");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4533 && entry.Name == "TenantDomainOwnershipHttpProofCollectionDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4534 && entry.Name == "TenantDomainOwnershipProofVerificationCompleted");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4535 && entry.Name == "TenantDomainOwnershipProofVerificationDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4536 && entry.Name == "TenantDomainOwnershipDnsTxtProofCollected");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4537 && entry.Name == "TenantDomainOwnershipDnsTxtProofCollectionDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4538 && entry.Name == "TenantDomainOwnershipProofPollingCompleted");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4539 && entry.Name == "TenantDomainOwnershipProofPollingDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4540 && entry.Name == "TenantDomainOwnershipProofBackgroundPollingStarted");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4541 && entry.Name == "TenantDomainOwnershipProofBackgroundPollingCompleted");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4542 && entry.Name == "TenantDomainOwnershipProofBackgroundPollingFailed");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4543 && entry.Name == "TenantDomainOwnershipProofBackgroundPollingStopped");
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
    public async Task TenantMembershipCatalogReadsRuntimeMembershipStoreUpdates()
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
        var store = provider.GetRequiredService<ITenantMembershipStore>();
        var catalog = provider.GetRequiredService<ITenantMembershipCatalog>();
        var evaluator = provider.GetRequiredService<ITenantMembershipEvaluator>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        store.Upsert(new TenantMembershipDescriptor(
            tenantId: "tenant-001",
            principalId: "user-001",
            displayName: "Runtime Admin",
            roles: ["admin"],
            sourceModuleId: "runtime-test"));

        var result = await evaluator.EvaluateAsync(new TenantMembershipEvaluationRequest(
            tenantId: "tenant-001",
            principalId: "user-001",
            requiredRoles: ["admin"]));
        var membershipsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-memberships");
        var summaryEntry = Assert.Single(membershipsSurface.Entries, entry => entry.Id == "tenant-membership-runtime");
        var tenantEntry = Assert.Single(membershipsSurface.Entries, entry => entry.Id == "tenant-membership:tenant-001");

        Assert.Single(catalog.Memberships);
        Assert.Single(catalog.GetByTenantAndPrincipal("tenant-001", "user-001"));
        Assert.True(result.Allowed);
        Assert.Equal(TenantMembershipEvaluationOutcomes.Allowed, result.Outcome);
        Assert.Equal("1", summaryEntry.Metadata["membershipCount"]);
        Assert.Equal("1", summaryEntry.Metadata["runtimeMembershipCount"]);
        Assert.Equal("in-memory", summaryEntry.Metadata["membershipStoreKind"]);
        Assert.Equal("false", summaryEntry.Metadata["membershipStoreDurable"]);
        Assert.Equal("application-managed", summaryEntry.Metadata["durableStoreOwnership"]);
        Assert.Equal("1", tenantEntry.Metadata["activeMembershipCount"]);
    }

    [Fact]
    public async Task TenantMembershipCatalogPersistsThroughFileBackedStore()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"cephalon-memberships-{Guid.NewGuid():N}");
        var storePath = Path.Combine(tempRoot, "tenant-memberships.json");
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
                    options.MembershipStoreFilePath = storePath;
                });
            });

            await using (var provider = services.BuildServiceProvider())
            {
                var store = provider.GetRequiredService<ITenantMembershipStore>();

                store.Upsert(new TenantMembershipDescriptor(
                    tenantId: "tenant-001",
                    principalId: "user-001",
                    displayName: "Durable Admin",
                    roles: ["admin"],
                    sourceModuleId: "runtime-test"));

                Assert.Equal(1, store.Count);
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
                    options.MembershipStoreFilePath = storePath;
                });
            });

            await using var restartedProvider = restartedServices.BuildServiceProvider();
            var catalog = restartedProvider.GetRequiredService<ITenantMembershipCatalog>();
            var evaluator = restartedProvider.GetRequiredService<ITenantMembershipEvaluator>();
            var technologyCatalog = restartedProvider.GetRequiredService<ITechnologyRuntimeCatalog>();

            var result = await evaluator.EvaluateAsync(new TenantMembershipEvaluationRequest(
                tenantId: "tenant-001",
                principalId: "user-001",
                requiredRoles: ["admin"]));
            var membershipsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-memberships");
            var summaryEntry = Assert.Single(membershipsSurface.Entries, entry => entry.Id == "tenant-membership-runtime");

            var membership = Assert.Single(catalog.Memberships);
            Assert.Equal("Durable Admin", membership.DisplayName);
            Assert.True(result.Allowed);
            Assert.Equal("file", summaryEntry.Metadata["membershipStoreKind"]);
            Assert.Equal("true", summaryEntry.Metadata["membershipStoreDurable"]);
            Assert.Equal("cephalon-managed", summaryEntry.Metadata["durableStoreOwnership"]);
            Assert.Equal("1", summaryEntry.Metadata["runtimeMembershipCount"]);
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
    public async Task TenantInvitationCatalogReadsRuntimeInvitationStoreUpdates()
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
        var store = provider.GetRequiredService<ITenantInvitationStore>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();
        var validator = provider.GetRequiredService<ITenantInvitationValidator>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        store.Upsert(new TenantInvitationDescriptor(
            invitationId: "invite-runtime-001",
            tenantId: "tenant-001",
            inviteeId: "user-001",
            displayName: "Runtime Invite",
            roles: ["member"],
            expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
            sourceModuleId: "runtime-test"));

        var result = await validator.ValidateAsync(new TenantInvitationValidationRequest(
            tenantId: "tenant-001",
            invitationId: "invite-runtime-001",
            inviteeId: "user-001",
            requiredRoles: ["member"],
            atUtc: new DateTimeOffset(2026, 04, 29, 0, 0, 0, TimeSpan.Zero)));
        var invitationsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-invitations");
        var summaryEntry = Assert.Single(invitationsSurface.Entries, entry => entry.Id == "tenant-invitation-runtime");
        var tenantEntry = Assert.Single(invitationsSurface.Entries, entry => entry.Id == "tenant-invitations:tenant-001");

        Assert.Single(catalog.Invitations);
        Assert.Single(catalog.GetByTenantAndInvitation("tenant-001", "invite-runtime-001"));
        Assert.True(result.Valid);
        Assert.Equal(TenantInvitationValidationOutcomes.Valid, result.Outcome);
        Assert.Equal("1", summaryEntry.Metadata["invitationCount"]);
        Assert.Equal("1", summaryEntry.Metadata["runtimeInvitationCount"]);
        Assert.Equal("in-memory", summaryEntry.Metadata["invitationStoreKind"]);
        Assert.Equal("false", summaryEntry.Metadata["invitationStoreDurable"]);
        Assert.Equal("application-managed", summaryEntry.Metadata["durableStoreOwnership"]);
        Assert.Equal("1", tenantEntry.Metadata["pendingInvitationCount"]);
    }

    [Fact]
    public async Task TenantInvitationCatalogPersistsThroughFileBackedStore()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"cephalon-invitations-{Guid.NewGuid():N}");
        var storePath = Path.Combine(tempRoot, "tenant-invitations.json");
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
                    options.InvitationStoreFilePath = storePath;
                });
            });

            await using (var provider = services.BuildServiceProvider())
            {
                var store = provider.GetRequiredService<ITenantInvitationStore>();

                store.Upsert(new TenantInvitationDescriptor(
                    invitationId: "invite-durable-001",
                    tenantId: "tenant-001",
                    inviteeId: "user-001",
                    displayName: "Durable Invite",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    sourceModuleId: "runtime-test"));

                Assert.Equal(1, store.Count);
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
                    options.InvitationStoreFilePath = storePath;
                });
            });

            await using var restartedProvider = restartedServices.BuildServiceProvider();
            var catalog = restartedProvider.GetRequiredService<ITenantInvitationCatalog>();
            var validator = restartedProvider.GetRequiredService<ITenantInvitationValidator>();
            var technologyCatalog = restartedProvider.GetRequiredService<ITechnologyRuntimeCatalog>();

            var result = await validator.ValidateAsync(new TenantInvitationValidationRequest(
                tenantId: "tenant-001",
                invitationId: "invite-durable-001",
                inviteeId: "user-001",
                requiredRoles: ["member"],
                atUtc: new DateTimeOffset(2026, 04, 29, 0, 0, 0, TimeSpan.Zero)));
            var invitationsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-invitations");
            var summaryEntry = Assert.Single(invitationsSurface.Entries, entry => entry.Id == "tenant-invitation-runtime");

            var invitation = Assert.Single(catalog.Invitations);
            Assert.Equal("Durable Invite", invitation.DisplayName);
            Assert.True(result.Valid);
            Assert.Equal("file", summaryEntry.Metadata["invitationStoreKind"]);
            Assert.Equal("true", summaryEntry.Metadata["invitationStoreDurable"]);
            Assert.Equal("cephalon-managed", summaryEntry.Metadata["durableStoreOwnership"]);
            Assert.Equal("1", summaryEntry.Metadata["runtimeInvitationCount"]);
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
    public async Task TenantDomainOwnershipCatalogReadsRuntimeDomainOwnershipStoreUpdates()
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
            engine.AddMultiTenancy();
            engine.AddMultiTenancyGovernance();
        });

        await using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<ITenantDomainOwnershipStore>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();
        var validator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        store.Upsert(new TenantDomainOwnershipDescriptor(
            tenantId: "tenant-001",
            domainName: "Runtime.Example.",
            displayName: "Runtime Domain",
            status: TenantDomainOwnershipStatuses.Verified,
            verificationMethod: TenantDomainVerificationMethods.Manual,
            verifiedAtUtc: new DateTimeOffset(2026, 04, 29, 0, 0, 0, TimeSpan.Zero),
            expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
            sourceModuleId: "runtime-test"));

        var result = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "runtime.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 0, 0, 0, TimeSpan.Zero)));
        var domainsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-domain-ownership");
        var summaryEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership-runtime");
        var tenantEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership:tenant-001");

        var domainOwnership = Assert.Single(catalog.DomainOwnerships);
        Assert.Equal("runtime.example", domainOwnership.DomainName);
        Assert.Single(catalog.GetByTenantAndDomain("tenant-001", "runtime.example"));
        Assert.True(result.Valid);
        Assert.Equal(TenantDomainOwnershipValidationOutcomes.Valid, result.Outcome);
        Assert.Equal("1", summaryEntry.Metadata["domainOwnershipCount"]);
        Assert.Equal("1", summaryEntry.Metadata["runtimeDomainOwnershipCount"]);
        Assert.Equal("in-memory", summaryEntry.Metadata["domainOwnershipStoreKind"]);
        Assert.Equal("false", summaryEntry.Metadata["domainOwnershipStoreDurable"]);
        Assert.Equal("application-managed", summaryEntry.Metadata["durableStoreOwnership"]);
        Assert.Equal("manual:1", summaryEntry.Metadata["verificationMethodBreakdown"]);
        Assert.Equal("1", tenantEntry.Metadata["verifiedDomainOwnershipCount"]);
    }

    [Fact]
    public async Task TenantDomainOwnershipCatalogPersistsThroughFileBackedStore()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"cephalon-domain-ownerships-{Guid.NewGuid():N}");
        var storePath = Path.Combine(tempRoot, "tenant-domain-ownerships.json");
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
                    options.DomainOwnershipStoreFilePath = storePath;
                });
            });

            await using (var provider = services.BuildServiceProvider())
            {
                var store = provider.GetRequiredService<ITenantDomainOwnershipStore>();

                store.Upsert(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "Durable.Example.",
                    displayName: "Durable Domain",
                    status: TenantDomainOwnershipStatuses.Verified,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt,
                    verifiedAtUtc: new DateTimeOffset(2026, 04, 29, 1, 0, 0, TimeSpan.Zero),
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    sourceModuleId: "runtime-test"));

                Assert.Equal(1, store.Count);
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
                    options.DomainOwnershipStoreFilePath = storePath;
                });
            });

            await using var restartedProvider = restartedServices.BuildServiceProvider();
            var catalog = restartedProvider.GetRequiredService<ITenantDomainOwnershipCatalog>();
            var validator = restartedProvider.GetRequiredService<ITenantDomainOwnershipValidator>();
            var technologyCatalog = restartedProvider.GetRequiredService<ITechnologyRuntimeCatalog>();

            var result = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
                tenantId: "tenant-001",
                domainName: "durable.example",
                atUtc: new DateTimeOffset(2026, 04, 29, 1, 5, 0, TimeSpan.Zero)));
            var domainsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-domain-ownership");
            var summaryEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership-runtime");

            var domainOwnership = Assert.Single(catalog.DomainOwnerships);
            Assert.Equal("Durable Domain", domainOwnership.DisplayName);
            Assert.True(result.Valid);
            Assert.Equal("file", summaryEntry.Metadata["domainOwnershipStoreKind"]);
            Assert.Equal("true", summaryEntry.Metadata["domainOwnershipStoreDurable"]);
            Assert.Equal("cephalon-managed", summaryEntry.Metadata["durableStoreOwnership"]);
            Assert.Equal("1", summaryEntry.Metadata["runtimeDomainOwnershipCount"]);
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
    public async Task TenantDomainOwnershipVerificationWorkflowTransitionsFeedCatalogValidationAndSurface()
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
        var workflow = provider.GetRequiredService<ITenantDomainOwnershipVerificationWorkflow>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();
        var validator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var created = await workflow.ApplyAsync(new TenantDomainOwnershipVerificationWorkflowRequest(
            command: TenantDomainOwnershipVerificationWorkflowCommands.Request,
            tenantId: "tenant-001",
            domainName: "Workflow.Example.",
            displayName: "Workflow Domain",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt,
            actor: "operator-001",
            reason: "Tenant requested a custom domain.",
            atUtc: new DateTimeOffset(2026, 04, 29, 2, 0, 0, TimeSpan.Zero)));
        var pending = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "workflow.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 2, 1, 0, TimeSpan.Zero)));
        var verified = await workflow.ApplyAsync(new TenantDomainOwnershipVerificationWorkflowRequest(
            command: TenantDomainOwnershipVerificationWorkflowCommands.Verify,
            tenantId: "tenant-001",
            domainName: "workflow.example",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt,
            actor: "tenant-owner",
            evidence: "TXT cephalon=verified",
            correlationId: "corr-domain-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 2, 5, 0, TimeSpan.Zero)));
        var valid = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "WORKFLOW.EXAMPLE.",
            atUtc: new DateTimeOffset(2026, 04, 29, 2, 10, 0, TimeSpan.Zero)));
        var domainsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-domain-ownership");
        var summaryEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership-runtime");
        var tenantEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership:tenant-001");

        Assert.True(created.Applied);
        Assert.Equal(TenantDomainOwnershipVerificationWorkflowOutcomes.Created, created.Outcome);
        Assert.Null(created.PreviousStatus);
        Assert.Equal(TenantDomainOwnershipStatuses.Pending, created.CurrentStatus);
        Assert.False(pending.Valid);
        Assert.Equal(TenantDomainOwnershipValidationOutcomes.Pending, pending.Outcome);
        Assert.True(verified.Applied);
        Assert.Equal(TenantDomainOwnershipVerificationWorkflowOutcomes.Applied, verified.Outcome);
        Assert.Equal(TenantDomainOwnershipStatuses.Pending, verified.PreviousStatus);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, verified.CurrentStatus);
        Assert.True(valid.Valid);
        Assert.Equal(TenantDomainOwnershipValidationOutcomes.Valid, valid.Outcome);
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, domainOwnership.Status);
        Assert.Equal(TenantDomainVerificationMethods.DnsTxt, domainOwnership.VerificationMethod);
        Assert.Equal(new DateTimeOffset(2026, 04, 29, 2, 5, 0, TimeSpan.Zero), domainOwnership.VerifiedAtUtc);
        Assert.Equal("verify", domainOwnership.Metadata["lastVerificationWorkflowCommand"]);
        Assert.Equal("tenant-owner", domainOwnership.Metadata["lastVerificationWorkflowActor"]);
        Assert.Equal("TXT cephalon=verified", domainOwnership.Metadata["lastVerificationWorkflowEvidence"]);
        Assert.Equal("corr-domain-001", domainOwnership.Metadata["lastVerificationWorkflowCorrelationId"]);
        Assert.Equal("1", summaryEntry.Metadata["domainOwnershipCount"]);
        Assert.Equal("1", summaryEntry.Metadata["runtimeDomainOwnershipCount"]);
        Assert.Equal("true", summaryEntry.Metadata["verificationWorkflowEnabled"]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["verificationWorkflowOwnership"]);
        Assert.Equal("mixed", summaryEntry.Metadata["dnsHttpProofCollectionOwnership"]);
        Assert.Equal("verified:1", summaryEntry.Metadata["statusBreakdown"]);
        Assert.Equal("1", tenantEntry.Metadata["verifiedDomainOwnershipCount"]);
    }

    [Fact]
    public async Task TenantDomainOwnershipProofChallengeIssuerIssuesDnsChallengeAndEvaluatorVerifiesReportedProof()
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
        var issuer = provider.GetRequiredService<ITenantDomainOwnershipProofChallengeIssuer>();
        var evaluator = provider.GetRequiredService<ITenantDomainOwnershipProofEvaluator>();
        var validator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var challenge = await issuer.IssueAsync(new TenantDomainOwnershipProofChallengeRequest(
            tenantId: "tenant-001",
            domainName: "Challenge.Example.",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt,
            challengeValue: "cephalon-proof-token",
            source: "operator-portal",
            actor: "operator-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 5, 0, 0, TimeSpan.Zero),
            expiresAtUtc: new DateTimeOffset(2026, 04, 30, 5, 0, 0, TimeSpan.Zero),
            correlationId: "corr-challenge-001"));
        var pending = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "challenge.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 5, 5, 0, TimeSpan.Zero)));
        var evaluated = await evaluator.EvaluateAsync(new TenantDomainOwnershipProofEvaluationRequest(
            tenantId: "tenant-001",
            domainName: "challenge.example",
            observedProof: challenge.ChallengeValue,
            verificationMethod: TenantDomainVerificationMethods.DnsTxt,
            source: "dns-provider",
            atUtc: new DateTimeOffset(2026, 04, 29, 5, 10, 0, TimeSpan.Zero)));
        var valid = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "challenge.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 5, 15, 0, TimeSpan.Zero)));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);
        var domainsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-domain-ownership");
        var summaryEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership-runtime");

        Assert.True(challenge.Issued);
        Assert.Equal(TenantDomainOwnershipProofChallengeOutcomes.Issued, challenge.Outcome);
        Assert.Equal("challenge.example", challenge.DomainName);
        Assert.Equal(TenantDomainVerificationMethods.DnsTxt, challenge.VerificationMethod);
        Assert.Equal("cephalon-proof-token", challenge.ChallengeValue);
        Assert.Equal("_cephalon-domain-verification.challenge.example", challenge.DnsTxtRecordName);
        Assert.Null(challenge.HttpFilePath);
        Assert.NotNull(challenge.ChallengeFingerprint);
        Assert.False(pending.Valid);
        Assert.Equal(TenantDomainOwnershipValidationOutcomes.Pending, pending.Outcome);
        Assert.True(evaluated.Matched);
        Assert.True(evaluated.Applied);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.Verified, evaluated.Outcome);
        Assert.True(valid.Valid);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, domainOwnership.Status);
        Assert.Equal("cephalon-proof-token", domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.ExpectedProof]);
        Assert.Equal("cephalon-proof-token", domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.ExpectedDnsTxtProof]);
        Assert.False(domainOwnership.Metadata.ContainsKey(TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof));
        Assert.Equal(TenantDomainOwnershipProofChallengeOutcomes.Issued, domainOwnership.Metadata[TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeOutcome]);
        Assert.Equal("operator-portal", domainOwnership.Metadata[TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeSource]);
        Assert.Equal("operator-001", domainOwnership.Metadata[TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeActor]);
        Assert.Equal("corr-challenge-001", domainOwnership.Metadata[TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeCorrelationId]);
        Assert.Equal(challenge.ChallengeFingerprint, domainOwnership.Metadata[TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeFingerprint]);
        Assert.Equal("_cephalon-domain-verification.challenge.example", domainOwnership.Metadata[TenantDomainOwnershipProofChallengeMetadataKeys.DnsTxtRecordName]);
        Assert.Equal("cephalon-managed", domainOwnership.Metadata[TenantDomainOwnershipProofChallengeMetadataKeys.ProofChallengeOwnership]);
        Assert.Equal("1", summaryEntry.Metadata["domainOwnershipCount"]);
        Assert.Equal("true", summaryEntry.Metadata["proofChallengeIssuanceEnabled"]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["proofChallengeIssuanceOwnership"]);
        Assert.Equal("application-managed", summaryEntry.Metadata["proofPublicationOwnership"]);
        Assert.Equal("mixed", summaryEntry.Metadata["dnsHttpProofCollectionOwnership"]);
    }

    [Fact]
    public async Task TenantDomainOwnershipProofChallengeIssuerRefreshesHttpChallengeThroughStore()
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
                    domainName: "refresh.example",
                    status: TenantDomainOwnershipStatuses.Rejected,
                    verificationMethod: TenantDomainVerificationMethods.HttpFile,
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof] = "old-proof"
                    }));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var issuer = provider.GetRequiredService<ITenantDomainOwnershipProofChallengeIssuer>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();

        var challenge = await issuer.IssueAsync(new TenantDomainOwnershipProofChallengeRequest(
            tenantId: "tenant-001",
            domainName: "refresh.example",
            verificationMethod: TenantDomainVerificationMethods.HttpFile,
            challengeValue: "new-http-proof",
            httpFilePath: ".well-known/custom-domain-proof.txt",
            atUtc: new DateTimeOffset(2026, 04, 29, 5, 30, 0, TimeSpan.Zero)));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);

        Assert.True(challenge.Issued);
        Assert.Equal(TenantDomainOwnershipProofChallengeOutcomes.Issued, challenge.Outcome);
        Assert.Equal("/.well-known/custom-domain-proof.txt", challenge.HttpFilePath);
        Assert.Null(challenge.DnsTxtRecordName);
        Assert.Equal(TenantDomainOwnershipStatuses.Pending, domainOwnership.Status);
        Assert.Equal("new-http-proof", domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.ExpectedProof]);
        Assert.Equal("new-http-proof", domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof]);
        Assert.False(domainOwnership.Metadata.ContainsKey(TenantDomainOwnershipProofMetadataKeys.ExpectedDnsTxtProof));
        Assert.Equal("/.well-known/custom-domain-proof.txt", domainOwnership.Metadata[TenantDomainOwnershipProofChallengeMetadataKeys.HttpFilePath]);
    }

    [Fact]
    public async Task TenantDomainOwnershipProofChallengeIssuerReportsBoundariesWithoutMutatingState()
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
                    domainName: "verified.example",
                    status: TenantDomainOwnershipStatuses.Verified,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt));
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "suspended.example",
                    status: TenantDomainOwnershipStatuses.Suspended,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt));
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-002",
                    domainName: "shared.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt));
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "method.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.HttpFile));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var issuer = provider.GetRequiredService<ITenantDomainOwnershipProofChallengeIssuer>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();

        var alreadyVerified = await issuer.IssueAsync(new TenantDomainOwnershipProofChallengeRequest(
            tenantId: "tenant-001",
            domainName: "verified.example",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt));
        var invalidStatus = await issuer.IssueAsync(new TenantDomainOwnershipProofChallengeRequest(
            tenantId: "tenant-001",
            domainName: "suspended.example",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt));
        var tenantMismatch = await issuer.IssueAsync(new TenantDomainOwnershipProofChallengeRequest(
            tenantId: "tenant-001",
            domainName: "shared.example",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt));
        var methodMismatch = await issuer.IssueAsync(new TenantDomainOwnershipProofChallengeRequest(
            tenantId: "tenant-001",
            domainName: "method.example",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt));

        Assert.False(alreadyVerified.Issued);
        Assert.Equal(TenantDomainOwnershipProofChallengeOutcomes.AlreadyVerified, alreadyVerified.Outcome);
        Assert.False(invalidStatus.Issued);
        Assert.Equal(TenantDomainOwnershipProofChallengeOutcomes.InvalidStatus, invalidStatus.Outcome);
        Assert.False(tenantMismatch.Issued);
        Assert.Equal(TenantDomainOwnershipProofChallengeOutcomes.TenantMismatch, tenantMismatch.Outcome);
        Assert.False(methodMismatch.Issued);
        Assert.Equal(TenantDomainOwnershipProofChallengeOutcomes.VerificationMethodMismatch, methodMismatch.Outcome);
        Assert.Equal(4, catalog.DomainOwnerships.Count);
        Assert.DoesNotContain(catalog.DomainOwnerships, domainOwnership =>
            domainOwnership.Metadata.ContainsKey(TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeFingerprint));
    }

    [Fact]
    public async Task TenantDomainOwnershipProofChallengeIssuerReportsStoreFailuresWithoutApplyingChallenge()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantDomainOwnershipStore>(new FailingTenantDomainOwnershipStore());
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
        var issuer = provider.GetRequiredService<ITenantDomainOwnershipProofChallengeIssuer>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();

        var result = await issuer.IssueAsync(new TenantDomainOwnershipProofChallengeRequest(
            tenantId: "tenant-001",
            domainName: "failing-challenge.example",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt,
            challengeValue: "cephalon-proof-token"));

        Assert.False(result.Issued);
        Assert.Equal(TenantDomainOwnershipProofChallengeOutcomes.StoreFailed, result.Outcome);
        Assert.Null(result.ChallengeValue);
        Assert.Null(result.ChallengeFingerprint);
        Assert.False(result.Metadata.ContainsKey(TenantDomainOwnershipProofMetadataKeys.ExpectedProof));
        Assert.False(result.Metadata.ContainsKey(TenantDomainOwnershipProofChallengeMetadataKeys.LastProofChallengeFingerprint));
        Assert.Empty(catalog.DomainOwnerships);
    }

    [Fact]
    public async Task TenantDomainOwnershipProofPublicationPlannerPlansDnsInstructionsAndEvaluatorVerifiesPublishedProof()
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
        var issuer = provider.GetRequiredService<ITenantDomainOwnershipProofChallengeIssuer>();
        var planner = provider.GetRequiredService<ITenantDomainOwnershipProofPublicationPlanner>();
        var evaluator = provider.GetRequiredService<ITenantDomainOwnershipProofEvaluator>();
        var validator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var challenge = await issuer.IssueAsync(new TenantDomainOwnershipProofChallengeRequest(
            tenantId: "tenant-001",
            domainName: "Publish.Example.",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt,
            challengeValue: "cephalon-proof-token",
            source: "operator-portal",
            actor: "operator-001",
            correlationId: "corr-challenge-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 7, 0, 0, TimeSpan.Zero)));
        var plan = await planner.PlanAsync(new TenantDomainOwnershipProofPublicationPlanRequest(
            tenantId: "tenant-001",
            domainName: "publish.example",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt,
            source: "operator-portal",
            actor: "operator-001",
            correlationId: "corr-publication-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 7, 5, 0, TimeSpan.Zero)));
        var evaluation = await evaluator.EvaluateAsync(new TenantDomainOwnershipProofEvaluationRequest(
            tenantId: "tenant-001",
            domainName: "publish.example",
            observedProof: plan.DnsTxtRecordValue!,
            verificationMethod: TenantDomainVerificationMethods.DnsTxt,
            source: "dns-provider",
            actor: "operator-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 7, 10, 0, TimeSpan.Zero)));
        var validation = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "publish.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 7, 15, 0, TimeSpan.Zero)));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);
        var domainsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-domain-ownership");
        var summaryEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership-runtime");

        Assert.True(challenge.Issued);
        Assert.True(plan.Planned);
        Assert.True(plan.Recorded);
        Assert.Equal(TenantDomainOwnershipProofPublicationPlanOutcomes.Planned, plan.Outcome);
        Assert.Equal(TenantDomainVerificationMethods.DnsTxt, plan.VerificationMethod);
        Assert.Equal("cephalon-proof-token", plan.ProofValue);
        Assert.Equal(plan.ProofValue, plan.DnsTxtRecordValue);
        Assert.Equal("_cephalon-domain-verification.publish.example", plan.DnsTxtRecordName);
        Assert.Null(plan.HttpFilePath);
        Assert.Null(plan.HttpFileContent);
        Assert.Equal(plan.ProofFingerprint, domainOwnership.Metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlanFingerprint]);
        Assert.Equal(plan.DnsTxtRecordName, domainOwnership.Metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.DnsTxtRecordName]);
        Assert.Equal(plan.ProofFingerprint, domainOwnership.Metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.DnsTxtRecordValueFingerprint]);
        Assert.Equal("operator-portal", domainOwnership.Metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlanSource]);
        Assert.Equal("operator-001", domainOwnership.Metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlanActor]);
        Assert.Equal("corr-publication-001", domainOwnership.Metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlanCorrelationId]);
        Assert.Equal("cephalon-managed", domainOwnership.Metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.ProofPublicationPlanningOwnership]);
        Assert.Equal("application-managed", domainOwnership.Metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.ExternalPublicationOwnership]);
        Assert.True(evaluation.Matched);
        Assert.True(evaluation.Applied);
        Assert.True(validation.Valid);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, domainOwnership.Status);
        Assert.Equal("true", summaryEntry.Metadata["proofPublicationPlanningEnabled"]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["proofPublicationPlanningOwnership"]);
        Assert.Equal("application-managed", summaryEntry.Metadata["proofPublicationOwnership"]);
        Assert.Equal("mixed", summaryEntry.Metadata["dnsHttpProofCollectionOwnership"]);
    }

    [Fact]
    public async Task TenantDomainOwnershipProofPublicationPlannerPlansHttpInstructionsWithoutRecordingWhenPreviewed()
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
                    domainName: "preview.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.HttpFile,
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof] = "http-preview-proof",
                        [TenantDomainOwnershipProofChallengeMetadataKeys.HttpFilePath] = "tenant-proof.txt"
                    }));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var planner = provider.GetRequiredService<ITenantDomainOwnershipProofPublicationPlanner>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();

        var plan = await planner.PlanAsync(new TenantDomainOwnershipProofPublicationPlanRequest(
            tenantId: "tenant-001",
            domainName: "preview.example",
            verificationMethod: TenantDomainVerificationMethods.HttpFile,
            recordPlan: false));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);

        Assert.True(plan.Planned);
        Assert.False(plan.Recorded);
        Assert.Equal(TenantDomainOwnershipProofPublicationPlanOutcomes.Planned, plan.Outcome);
        Assert.Equal("/tenant-proof.txt", plan.HttpFilePath);
        Assert.Equal("http-preview-proof", plan.HttpFileContent);
        Assert.Equal("text/plain; charset=utf-8", plan.HttpContentType);
        Assert.Null(plan.DnsTxtRecordName);
        Assert.Null(plan.DnsTxtRecordValue);
        Assert.False(domainOwnership.Metadata.ContainsKey(TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlanOutcome));
        Assert.False(domainOwnership.Metadata.ContainsKey(TenantDomainOwnershipProofPublicationPlanMetadataKeys.HttpFileContentFingerprint));
    }

    [Fact]
    public async Task TenantDomainOwnershipProofPublicationPlannerReportsBoundariesWithoutPublishing()
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
                    tenantId: "tenant-002",
                    domainName: "shared.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt,
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedDnsTxtProof] = "shared-proof"
                    }));
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "manual.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.Manual,
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedProof] = "manual-proof"
                    }));
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "missing-proof.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var planner = provider.GetRequiredService<ITenantDomainOwnershipProofPublicationPlanner>();

        var notFound = await planner.PlanAsync(new TenantDomainOwnershipProofPublicationPlanRequest(
            tenantId: "tenant-001",
            domainName: "absent.example"));
        var tenantMismatch = await planner.PlanAsync(new TenantDomainOwnershipProofPublicationPlanRequest(
            tenantId: "tenant-001",
            domainName: "shared.example"));
        var methodMismatch = await planner.PlanAsync(new TenantDomainOwnershipProofPublicationPlanRequest(
            tenantId: "tenant-002",
            domainName: "shared.example",
            verificationMethod: TenantDomainVerificationMethods.HttpFile));
        var unsupported = await planner.PlanAsync(new TenantDomainOwnershipProofPublicationPlanRequest(
            tenantId: "tenant-001",
            domainName: "manual.example"));
        var missingProof = await planner.PlanAsync(new TenantDomainOwnershipProofPublicationPlanRequest(
            tenantId: "tenant-001",
            domainName: "missing-proof.example"));

        Assert.False(notFound.Planned);
        Assert.Equal(TenantDomainOwnershipProofPublicationPlanOutcomes.NotFound, notFound.Outcome);
        Assert.Equal(TenantDomainOwnershipProofPublicationPlanOutcomes.NotFound, notFound.Metadata[TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlanOutcome]);
        Assert.False(tenantMismatch.Planned);
        Assert.Equal(TenantDomainOwnershipProofPublicationPlanOutcomes.TenantMismatch, tenantMismatch.Outcome);
        Assert.False(methodMismatch.Planned);
        Assert.Equal(TenantDomainOwnershipProofPublicationPlanOutcomes.VerificationMethodMismatch, methodMismatch.Outcome);
        Assert.False(unsupported.Planned);
        Assert.Equal(TenantDomainOwnershipProofPublicationPlanOutcomes.UnsupportedVerificationMethod, unsupported.Outcome);
        Assert.False(missingProof.Planned);
        Assert.Equal(TenantDomainOwnershipProofPublicationPlanOutcomes.MissingExpectedProof, missingProof.Outcome);
        Assert.Null(missingProof.ProofValue);
        Assert.Null(missingProof.DnsTxtRecordValue);
        Assert.Null(missingProof.HttpFileContent);
    }

    [Fact]
    public async Task TenantDomainOwnershipProofPublicationPlannerReportsStoreFailuresWithoutReturningInstructions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantDomainOwnershipStore>(new FailingTenantDomainOwnershipStore());
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
                    domainName: "failing-publication.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt,
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedDnsTxtProof] = "failing-publication-proof"
                    }));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var planner = provider.GetRequiredService<ITenantDomainOwnershipProofPublicationPlanner>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();

        var result = await planner.PlanAsync(new TenantDomainOwnershipProofPublicationPlanRequest(
            tenantId: "tenant-001",
            domainName: "failing-publication.example",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);

        Assert.False(result.Planned);
        Assert.False(result.Recorded);
        Assert.Equal(TenantDomainOwnershipProofPublicationPlanOutcomes.StoreFailed, result.Outcome);
        Assert.Null(result.ProofValue);
        Assert.Null(result.ProofFingerprint);
        Assert.Null(result.DnsTxtRecordName);
        Assert.Null(result.DnsTxtRecordValue);
        Assert.False(domainOwnership.Metadata.ContainsKey(TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlanOutcome));
        Assert.False(result.Metadata.ContainsKey(TenantDomainOwnershipProofPublicationPlanMetadataKeys.LastProofPublicationPlanFingerprint));
    }

    [Fact]
    public async Task TenantDomainOwnershipHttpProofCollectorCollectsPublishedHttpProofAndEvaluatesIt()
    {
        var handler = new TestHttpProofMessageHandler(static _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("http-proof-token")
        });
        var services = new ServiceCollection();
        services.AddSingleton(new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        });
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
        var issuer = provider.GetRequiredService<ITenantDomainOwnershipProofChallengeIssuer>();
        var collector = provider.GetRequiredService<ITenantDomainOwnershipHttpProofCollector>();
        var validator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var challenge = await issuer.IssueAsync(new TenantDomainOwnershipProofChallengeRequest(
            tenantId: "tenant-001",
            domainName: "Collect.Example.",
            verificationMethod: TenantDomainVerificationMethods.HttpFile,
            challengeValue: "http-proof-token",
            httpFilePath: "tenant-proof.txt",
            source: "operator-portal",
            actor: "operator-001",
            correlationId: "corr-http-challenge-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 8, 0, 0, TimeSpan.Zero)));
        var collection = await collector.CollectAsync(new TenantDomainOwnershipHttpProofCollectionRequest(
            tenantId: "tenant-001",
            domainName: "collect.example",
            collectionBaseUri: new Uri("https://collect.example"),
            source: "http-proof-collector-test",
            actor: "operator-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 8, 5, 0, TimeSpan.Zero),
            expiresAtUtc: new DateTimeOffset(2026, 05, 29, 8, 5, 0, TimeSpan.Zero),
            correlationId: "corr-http-collection-001"));
        var validation = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "collect.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 8, 10, 0, TimeSpan.Zero)));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);
        var domainsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-domain-ownership");
        var summaryEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership-runtime");

        Assert.True(challenge.Issued);
        Assert.True(collection.Collected);
        Assert.True(collection.Evaluated);
        Assert.Equal(TenantDomainOwnershipHttpProofCollectionOutcomes.Collected, collection.Outcome);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.Verified, collection.EvaluationResult?.Outcome);
        Assert.Equal(new Uri("https://collect.example/tenant-proof.txt"), collection.CollectionUri);
        Assert.Equal(new Uri("https://collect.example/tenant-proof.txt"), Assert.Single(handler.RequestedUris));
        Assert.Equal(200, collection.StatusCode);
        Assert.Equal(16, collection.ContentLength);
        Assert.True(validation.Valid);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, domainOwnership.Status);
        Assert.Equal(TenantDomainOwnershipHttpProofCollectionOutcomes.Collected, domainOwnership.Metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionOutcome]);
        Assert.Equal("http-proof-collector-test", domainOwnership.Metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionSource]);
        Assert.Equal("operator-001", domainOwnership.Metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionActor]);
        Assert.Equal("corr-http-collection-001", domainOwnership.Metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionCorrelationId]);
        Assert.Equal("https://collect.example/tenant-proof.txt", domainOwnership.Metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionUri]);
        Assert.Equal("200", domainOwnership.Metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionStatusCode]);
        Assert.Equal("16", domainOwnership.Metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionContentLength]);
        Assert.Equal(collection.ObservedProofFingerprint, domainOwnership.Metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionObservedFingerprint]);
        Assert.Equal("cephalon-managed", domainOwnership.Metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.HttpProofCollectionOwnership]);
        Assert.Equal("not-configured", domainOwnership.Metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.DnsTxtProofCollectionOwnership]);
        Assert.Equal("cephalon-managed", domainOwnership.Metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.ExternalProofPollingOwnership]);
        Assert.Equal("application-managed", domainOwnership.Metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.BackgroundProofPollingOwnership]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["httpProofCollectionOwnership"]);
        Assert.Equal("not-configured", summaryEntry.Metadata["dnsTxtProofCollectionOwnership"]);
        Assert.Equal("false", summaryEntry.Metadata["dnsTxtProofResolverConfigured"]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["externalProofPollingOwnership"]);
        Assert.Equal("application-managed", summaryEntry.Metadata["backgroundProofPollingOwnership"]);
        Assert.Equal("mixed", summaryEntry.Metadata["dnsHttpProofCollectionOwnership"]);
    }

    [Fact]
    public async Task TenantDomainOwnershipHttpProofCollectorReportsCollectionFailuresWithoutMutatingEvaluationState()
    {
        var handler = new TestHttpProofMessageHandler(static _ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        var services = new ServiceCollection();
        services.AddSingleton(new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        });
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
        var issuer = provider.GetRequiredService<ITenantDomainOwnershipProofChallengeIssuer>();
        var collector = provider.GetRequiredService<ITenantDomainOwnershipHttpProofCollector>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();

        var challenge = await issuer.IssueAsync(new TenantDomainOwnershipProofChallengeRequest(
            tenantId: "tenant-001",
            domainName: "missing-http-proof.example",
            verificationMethod: TenantDomainVerificationMethods.HttpFile,
            challengeValue: "http-proof-token",
            httpFilePath: "tenant-proof.txt"));
        var statusFailure = await collector.CollectAsync(new TenantDomainOwnershipHttpProofCollectionRequest(
            tenantId: "tenant-001",
            domainName: "missing-http-proof.example",
            collectionBaseUri: new Uri("https://missing-http-proof.example"),
            atUtc: new DateTimeOffset(2026, 04, 29, 8, 20, 0, TimeSpan.Zero)));
        var unsupported = await collector.CollectAsync(new TenantDomainOwnershipHttpProofCollectionRequest(
            tenantId: "tenant-001",
            domainName: "missing-http-proof.example",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt,
            atUtc: new DateTimeOffset(2026, 04, 29, 8, 25, 0, TimeSpan.Zero)));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);

        Assert.True(challenge.Issued);
        Assert.False(statusFailure.Collected);
        Assert.False(statusFailure.Evaluated);
        Assert.Equal(TenantDomainOwnershipHttpProofCollectionOutcomes.UnexpectedStatusCode, statusFailure.Outcome);
        Assert.Equal(404, statusFailure.StatusCode);
        Assert.NotNull(statusFailure.PublicationPlanResult);
        Assert.Equal(TenantDomainOwnershipHttpProofCollectionOutcomes.UnsupportedVerificationMethod, unsupported.Outcome);
        Assert.Single(handler.RequestedUris);
        Assert.Equal(TenantDomainOwnershipStatuses.Pending, domainOwnership.Status);
        Assert.False(domainOwnership.Metadata.ContainsKey(TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationOutcome));
    }

    [Fact]
    public async Task TenantDomainOwnershipDnsTxtProofCollectorCollectsPublishedDnsTxtProofAndEvaluatesIt()
    {
        var handler = new TestHttpProofMessageHandler(static _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"Status\":0,\"Answer\":[{\"name\":\"_cephalon-domain-verification.collect-dns.example\",\"type\":16,\"data\":\"\\\"dns-proof-token\\\"\"}]}")
        });
        var services = new ServiceCollection();
        services.AddSingleton(new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        });
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
                options.DomainOwnershipDnsTxtProofResolverEndpoint = new Uri("https://resolver.example/dns-query");
            });
        });

        await using var provider = services.BuildServiceProvider();
        var issuer = provider.GetRequiredService<ITenantDomainOwnershipProofChallengeIssuer>();
        var collector = provider.GetRequiredService<ITenantDomainOwnershipDnsTxtProofCollector>();
        var validator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var challenge = await issuer.IssueAsync(new TenantDomainOwnershipProofChallengeRequest(
            tenantId: "tenant-001",
            domainName: "Collect-Dns.Example.",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt,
            challengeValue: "dns-proof-token",
            source: "operator-portal",
            actor: "operator-001",
            correlationId: "corr-dns-challenge-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 8, 30, 0, TimeSpan.Zero)));
        var collection = await collector.CollectAsync(new TenantDomainOwnershipDnsTxtProofCollectionRequest(
            tenantId: "tenant-001",
            domainName: "collect-dns.example",
            source: "dns-txt-proof-collector-test",
            actor: "operator-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 8, 35, 0, TimeSpan.Zero),
            expiresAtUtc: new DateTimeOffset(2026, 05, 29, 8, 35, 0, TimeSpan.Zero),
            correlationId: "corr-dns-collection-001"));
        var validation = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "collect-dns.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 8, 40, 0, TimeSpan.Zero)));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);
        var domainsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-domain-ownership");
        var summaryEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership-runtime");

        Assert.True(challenge.Issued);
        Assert.True(collection.Collected);
        Assert.True(collection.Evaluated);
        Assert.Equal(TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Collected, collection.Outcome);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.Verified, collection.EvaluationResult?.Outcome);
        Assert.Equal("_cephalon-domain-verification.collect-dns.example", collection.DnsTxtRecordName);
        Assert.Equal(new Uri("https://resolver.example/dns-query?name=_cephalon-domain-verification.collect-dns.example&type=TXT"), collection.ResolverUri);
        Assert.Equal(new Uri("https://resolver.example/dns-query?name=_cephalon-domain-verification.collect-dns.example&type=TXT"), Assert.Single(handler.RequestedUris));
        Assert.Equal(200, collection.StatusCode);
        Assert.Equal(1, collection.ObservedTxtRecordCount);
        Assert.True(validation.Valid);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, domainOwnership.Status);
        Assert.Equal(TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Collected, domainOwnership.Metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionOutcome]);
        Assert.Equal("dns-txt-proof-collector-test", domainOwnership.Metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionSource]);
        Assert.Equal("operator-001", domainOwnership.Metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionActor]);
        Assert.Equal("corr-dns-collection-001", domainOwnership.Metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionCorrelationId]);
        Assert.Equal("_cephalon-domain-verification.collect-dns.example", domainOwnership.Metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionRecordName]);
        Assert.Equal("200", domainOwnership.Metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionStatusCode]);
        Assert.Equal("1", domainOwnership.Metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionObservedTxtRecordCount]);
        Assert.Equal(collection.ObservedProofFingerprint, domainOwnership.Metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionObservedFingerprint]);
        Assert.Equal("cephalon-managed", domainOwnership.Metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.DnsTxtProofCollectionOwnership]);
        Assert.Equal("cephalon-managed", domainOwnership.Metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.ExternalProofPollingOwnership]);
        Assert.Equal("application-managed", domainOwnership.Metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.BackgroundProofPollingOwnership]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["dnsTxtProofCollectionOwnership"]);
        Assert.Equal("true", summaryEntry.Metadata["dnsTxtProofResolverConfigured"]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["dnsHttpProofCollectionOwnership"]);
        Assert.DoesNotContain(collection.Metadata, pair => string.Equals(pair.Value, "dns-proof-token", StringComparison.Ordinal));
        Assert.DoesNotContain(domainOwnership.Metadata, pair => pair.Key.Contains("Observed", StringComparison.OrdinalIgnoreCase) && string.Equals(pair.Value, "dns-proof-token", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TenantDomainOwnershipProofVerificationRunnerIssuesChallengeAndPublicationPlanWhenProofIsMissing()
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
        var runner = provider.GetRequiredService<ITenantDomainOwnershipProofVerificationRunner>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();

        var result = await runner.VerifyAsync(new TenantDomainOwnershipProofVerificationRequest(
            tenantId: "tenant-001",
            domainName: "Runner-Challenge.Example.",
            verificationMethod: TenantDomainVerificationMethods.HttpFile,
            source: "operator-portal",
            actor: "operator-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 9, 0, 0, TimeSpan.Zero),
            correlationId: "corr-runner-challenge-001"));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);

        Assert.True(result.ChallengeIssued);
        Assert.True(result.PublicationPlanned);
        Assert.False(result.ProofCollected);
        Assert.False(result.ProofEvaluated);
        Assert.Equal(TenantDomainOwnershipProofVerificationOutcomes.ChallengeIssued, result.Outcome);
        Assert.Equal(TenantDomainOwnershipProofChallengeOutcomes.Issued, result.ChallengeResult?.Outcome);
        Assert.Equal(TenantDomainOwnershipProofPublicationPlanOutcomes.Planned, result.PublicationPlanResult?.Outcome);
        Assert.Equal(TenantDomainVerificationMethods.HttpFile, result.VerificationMethod);
        Assert.Equal("runner-challenge.example", domainOwnership.DomainName);
        Assert.Equal(TenantDomainOwnershipStatuses.Pending, domainOwnership.Status);
        Assert.Equal(TenantDomainOwnershipProofVerificationOutcomes.ChallengeIssued, result.Metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationOutcome]);
        Assert.Equal("operator-portal", result.Metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationSource]);
        Assert.Equal("operator-001", result.Metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationActor]);
        Assert.Equal("corr-runner-challenge-001", result.Metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationCorrelationId]);
        Assert.Equal("cephalon-managed", result.Metadata[TenantDomainOwnershipProofVerificationMetadataKeys.ProofVerificationRunnerOwnership]);
        Assert.Equal("not-configured", result.Metadata[TenantDomainOwnershipProofVerificationMetadataKeys.DnsTxtProofCollectionOwnership]);
        Assert.NotNull(result.PublicationPlanResult?.HttpFilePath);
        Assert.NotNull(result.PublicationPlanResult?.HttpFileContent);
    }

    [Fact]
    public async Task TenantDomainOwnershipProofVerificationRunnerCollectsHttpProofAndEvaluatesIt()
    {
        var handler = new TestHttpProofMessageHandler(static _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("runner-http-proof")
        });
        var services = new ServiceCollection();
        services.AddSingleton(new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        });
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
                    domainName: "runner.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.HttpFile,
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof] = "runner-http-proof",
                        [TenantDomainOwnershipProofChallengeMetadataKeys.HttpFilePath] = "runner-proof.txt"
                    }));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<ITenantDomainOwnershipProofVerificationRunner>();
        var validator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();

        var result = await runner.VerifyAsync(new TenantDomainOwnershipProofVerificationRequest(
            tenantId: "tenant-001",
            domainName: "RUNNER.EXAMPLE.",
            collectionBaseUri: new Uri("https://runner.example"),
            source: "runner-test",
            actor: "operator-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 9, 10, 0, TimeSpan.Zero),
            correlationId: "corr-runner-http-001"));
        var validation = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "runner.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 9, 15, 0, TimeSpan.Zero)));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);

        Assert.True(result.Verified);
        Assert.False(result.Rejected);
        Assert.True(result.ProofCollected);
        Assert.True(result.ProofEvaluated);
        Assert.Equal(TenantDomainOwnershipProofVerificationOutcomes.Verified, result.Outcome);
        Assert.Equal(TenantDomainOwnershipHttpProofCollectionOutcomes.Collected, result.HttpProofCollectionResult?.Outcome);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.Verified, result.EvaluationResult?.Outcome);
        Assert.Equal(new Uri("https://runner.example/runner-proof.txt"), Assert.Single(handler.RequestedUris));
        Assert.True(validation.Valid);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, domainOwnership.Status);
        Assert.Equal(TenantDomainOwnershipHttpProofCollectionOutcomes.Collected, domainOwnership.Metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionOutcome]);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.Verified, domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationOutcome]);
        Assert.Equal(TenantDomainOwnershipProofVerificationOutcomes.Verified, result.Metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationOutcome]);
        Assert.Equal(TenantDomainOwnershipHttpProofCollectionOutcomes.Collected, result.Metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationHttpCollectionOutcome]);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.Verified, result.Metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationEvaluationOutcome]);
        Assert.DoesNotContain(result.Metadata, pair => string.Equals(pair.Value, "runner-http-proof", StringComparison.Ordinal));
        Assert.DoesNotContain(domainOwnership.Metadata, pair => pair.Key.Contains("Observed", StringComparison.OrdinalIgnoreCase) && string.Equals(pair.Value, "runner-http-proof", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TenantDomainOwnershipProofVerificationRunnerCollectsDnsTxtProofAndEvaluatesIt()
    {
        var handler = new TestHttpProofMessageHandler(static _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"Status\":0,\"Answer\":[{\"name\":\"_cephalon-domain-verification.runner-dns.example\",\"type\":16,\"data\":\"\\\"runner-dns-proof\\\"\"}]}")
        });
        var services = new ServiceCollection();
        services.AddSingleton(new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        });
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
                options.DomainOwnershipDnsTxtProofResolverEndpoint = new Uri("https://resolver.example/dns-query");
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "runner-dns.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt,
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedDnsTxtProof] = "runner-dns-proof"
                    }));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<ITenantDomainOwnershipProofVerificationRunner>();
        var validator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();

        var result = await runner.VerifyAsync(new TenantDomainOwnershipProofVerificationRequest(
            tenantId: "tenant-001",
            domainName: "RUNNER-DNS.EXAMPLE.",
            source: "runner-test",
            actor: "operator-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 9, 20, 0, TimeSpan.Zero),
            correlationId: "corr-runner-dns-001"));
        var validation = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "runner-dns.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 9, 25, 0, TimeSpan.Zero)));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);

        Assert.True(result.Verified);
        Assert.False(result.Rejected);
        Assert.True(result.ProofCollected);
        Assert.True(result.ProofEvaluated);
        Assert.Equal(TenantDomainOwnershipProofVerificationOutcomes.Verified, result.Outcome);
        Assert.Equal(TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Collected, result.DnsTxtProofCollectionResult?.Outcome);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.Verified, result.EvaluationResult?.Outcome);
        Assert.Equal(new Uri("https://resolver.example/dns-query?name=_cephalon-domain-verification.runner-dns.example&type=TXT"), Assert.Single(handler.RequestedUris));
        Assert.True(validation.Valid);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, domainOwnership.Status);
        Assert.Equal(TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Collected, domainOwnership.Metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionOutcome]);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.Verified, domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationOutcome]);
        Assert.Equal(TenantDomainOwnershipProofVerificationOutcomes.Verified, result.Metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationOutcome]);
        Assert.Equal(TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Collected, result.Metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationDnsTxtCollectionOutcome]);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.Verified, result.Metadata[TenantDomainOwnershipProofVerificationMetadataKeys.LastProofVerificationEvaluationOutcome]);
        Assert.Equal("cephalon-managed", result.Metadata[TenantDomainOwnershipProofVerificationMetadataKeys.DnsTxtProofCollectionOwnership]);
        Assert.DoesNotContain(result.Metadata, pair => string.Equals(pair.Value, "runner-dns-proof", StringComparison.Ordinal));
        Assert.DoesNotContain(domainOwnership.Metadata, pair => pair.Key.Contains("Observed", StringComparison.OrdinalIgnoreCase) && string.Equals(pair.Value, "runner-dns-proof", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TenantDomainOwnershipProofPollingRunnerPollsPendingHttpAndRejectedDnsTxtProofs()
    {
        var handler = new TestHttpProofMessageHandler(static request =>
        {
            var uri = request.RequestUri ?? throw new InvalidOperationException("Expected a proof polling request URI.");
            if (string.Equals(uri.Host, "poll-http.example", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("poll-http-proof")
                };
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"Status\":0,\"Answer\":[{\"name\":\"_cephalon-domain-verification.poll-dns.example\",\"type\":16,\"data\":\"\\\"poll-dns-proof\\\"\"}]}")
            };
        });
        var services = new ServiceCollection();
        services.AddSingleton(new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        });
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
                options.DomainOwnershipDnsTxtProofResolverEndpoint = new Uri("https://resolver.example/dns-query");
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "poll-http.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.HttpFile,
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof] = "poll-http-proof",
                        [TenantDomainOwnershipProofChallengeMetadataKeys.HttpFilePath] = "poll-proof.txt"
                    }));
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "poll-dns.example",
                    status: TenantDomainOwnershipStatuses.Rejected,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt,
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedDnsTxtProof] = "poll-dns-proof"
                    }));
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "poll-manual.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.Manual,
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedProof] = "manual-proof"
                    }));
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "poll-verified.example",
                    status: TenantDomainOwnershipStatuses.Verified,
                    verificationMethod: TenantDomainVerificationMethods.HttpFile,
                    verifiedAtUtc: new DateTimeOffset(2026, 04, 29, 9, 45, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof] = "already-verified"
                    }));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var poller = provider.GetRequiredService<ITenantDomainOwnershipProofPollingRunner>();
        var validator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var result = await poller.PollAsync(new TenantDomainOwnershipProofPollingRequest(
            collectionBaseUri: new Uri("https://poll-http.example"),
            source: "proof-polling-test",
            actor: "operator-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 10, 0, 0, TimeSpan.Zero),
            expiresAtUtc: new DateTimeOffset(2026, 05, 29, 10, 0, 0, TimeSpan.Zero),
            correlationId: "corr-proof-polling-001",
            maxItems: 10));
        var httpValidation = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "poll-http.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 10, 5, 0, TimeSpan.Zero)));
        var dnsValidation = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "poll-dns.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 10, 5, 0, TimeSpan.Zero)));
        var httpDomainOwnership = Assert.Single(catalog.GetByTenantAndDomain("tenant-001", "poll-http.example"));
        var dnsDomainOwnership = Assert.Single(catalog.GetByTenantAndDomain("tenant-001", "poll-dns.example"));
        var manualDomainOwnership = Assert.Single(catalog.GetByTenantAndDomain("tenant-001", "poll-manual.example"));
        var domainsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-domain-ownership");
        var summaryEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership-runtime");

        Assert.True(result.Polled);
        Assert.Equal(TenantDomainOwnershipProofPollingOutcomes.Completed, result.Outcome);
        Assert.Equal(2, result.CandidateCount);
        Assert.Equal(2, result.VerificationCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(2, result.VerifiedCount);
        Assert.Equal(0, result.RejectedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(10, result.BatchLimit);
        Assert.All(result.VerificationResults, static verification => Assert.True(verification.Verified));
        Assert.Contains(result.VerificationResults, static verification => string.Equals(verification.VerificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.VerificationResults, static verification => string.Equals(verification.VerificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase));
        Assert.Equal("completed", result.Metadata[TenantDomainOwnershipProofPollingMetadataKeys.LastProofPollingOutcome]);
        Assert.Equal("proof-polling-test", result.Metadata[TenantDomainOwnershipProofPollingMetadataKeys.LastProofPollingSource]);
        Assert.Equal("operator-001", result.Metadata[TenantDomainOwnershipProofPollingMetadataKeys.LastProofPollingActor]);
        Assert.Equal("corr-proof-polling-001", result.Metadata[TenantDomainOwnershipProofPollingMetadataKeys.LastProofPollingCorrelationId]);
        Assert.Equal("cephalon-managed", result.Metadata[TenantDomainOwnershipProofPollingMetadataKeys.ProofPollingRunnerOwnership]);
        Assert.Equal("cephalon-managed", result.Metadata[TenantDomainOwnershipProofPollingMetadataKeys.ExternalProofPollingOwnership]);
        Assert.Equal("application-managed", result.Metadata[TenantDomainOwnershipProofPollingMetadataKeys.BackgroundProofPollingOwnership]);
        Assert.Equal("2", result.Metadata[TenantDomainOwnershipProofPollingMetadataKeys.CandidateCount]);
        Assert.Equal("2", result.Metadata[TenantDomainOwnershipProofPollingMetadataKeys.VerificationCount]);
        Assert.Equal("10", result.Metadata[TenantDomainOwnershipProofPollingMetadataKeys.BatchLimit]);
        Assert.Equal(2, handler.RequestedUris.Count);
        Assert.Contains(handler.RequestedUris, static uri => string.Equals(uri.Host, "poll-http.example", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(handler.RequestedUris, static uri => string.Equals(uri.Host, "resolver.example", StringComparison.OrdinalIgnoreCase));
        Assert.True(httpValidation.Valid);
        Assert.True(dnsValidation.Valid);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, httpDomainOwnership.Status);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, dnsDomainOwnership.Status);
        Assert.Equal(TenantDomainOwnershipStatuses.Pending, manualDomainOwnership.Status);
        Assert.Equal("true", summaryEntry.Metadata["proofPollingRunnerEnabled"]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["proofPollingRunnerOwnership"]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["externalProofPollingOwnership"]);
        Assert.Equal("application-managed", summaryEntry.Metadata["backgroundProofPollingOwnership"]);
    }

    [Fact]
    public async Task TenantDomainOwnershipProofBackgroundPollingRunsStartupPassAndReportsRuntimeState()
    {
        var handler = new TestHttpProofMessageHandler(static request =>
        {
            var uri = request.RequestUri ?? throw new InvalidOperationException("Expected a proof background polling request URI.");
            if (string.Equals(uri.Host, "background-http.example", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("background-http-proof")
                };
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });
        var services = new ServiceCollection();
        services.AddSingleton(new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        });
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
                options.EnableDomainOwnershipProofBackgroundPolling = true;
                options.DomainOwnershipProofBackgroundPollingIntervalSeconds = 3600;
                options.DomainOwnershipProofBackgroundPollingSource = "background-proof-polling-test";
                options.DomainOwnerships.Add(new TenantDomainOwnershipDescriptor(
                    tenantId: "tenant-001",
                    domainName: "background-http.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.HttpFile,
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof] = "background-http-proof",
                        [TenantDomainOwnershipProofChallengeMetadataKeys.HttpFilePath] = "background-proof.txt"
                    }));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var hostedService = Assert.Single(provider.GetServices<IHostedService>(), static service =>
            string.Equals(service.GetType().Name, "TenantDomainOwnershipProofPollingHostedService", StringComparison.Ordinal));
        var pollingRuntimeCatalog = provider.GetRequiredService<ITenantDomainOwnershipProofPollingRuntimeCatalog>();
        var validator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();
        var runtime = provider.GetRequiredService<global::Cephalon.Engine.Runtime.IRuntime>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        try
        {
            await hostedService.StartAsync(CancellationToken.None);
            await WaitUntilAsync(() => pollingRuntimeCatalog.Current.RunCount > 0);

            var validation = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
                tenantId: "tenant-001",
                domainName: "background-http.example",
                atUtc: DateTimeOffset.UtcNow));
            var domainOwnership = Assert.Single(catalog.GetByTenantAndDomain("tenant-001", "background-http.example"));
            var pollingRuntime = pollingRuntimeCatalog.Current;
            var backgroundCapability = Assert.Single(runtime.Manifest.Capabilities, capability =>
                capability.Key == "tenancy.domain-ownership.proof-background-polling");
            var domainsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface =>
                surface.SurfaceId == "tenant-domain-ownership");
            var summaryEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership-runtime");

            Assert.Single(handler.RequestedUris);
            Assert.Contains(handler.RequestedUris, static uri =>
                string.Equals(uri.Host, "background-http.example", StringComparison.OrdinalIgnoreCase));
            Assert.True(validation.Valid);
            Assert.Equal(TenantDomainOwnershipStatuses.Verified, domainOwnership.Status);
            Assert.True(pollingRuntime.Enabled);
            Assert.Equal("cephalon-managed", pollingRuntime.Ownership);
            Assert.Equal(3600, pollingRuntime.IntervalSeconds);
            Assert.Equal(50, pollingRuntime.BatchLimit);
            Assert.True(pollingRuntime.RunOnStartup);
            Assert.Equal(1, pollingRuntime.RunCount);
            Assert.Equal(1, pollingRuntime.SuccessfulRunCount);
            Assert.Equal(0, pollingRuntime.FailedRunCount);
            Assert.NotNull(pollingRuntime.LastStartedAtUtc);
            Assert.NotNull(pollingRuntime.LastCompletedAtUtc);
            Assert.Equal(TenantDomainOwnershipProofPollingOutcomes.Completed, pollingRuntime.LastOutcome);
            Assert.Equal(1, pollingRuntime.LastCandidateCount);
            Assert.Equal(1, pollingRuntime.LastVerificationCount);
            Assert.Equal(1, pollingRuntime.LastVerifiedCount);
            Assert.Equal(0, pollingRuntime.LastRejectedCount);
            Assert.Equal(0, pollingRuntime.LastFailedCount);
            Assert.Null(pollingRuntime.LastError);
            Assert.Equal("cephalon-managed", backgroundCapability.Metadata["backgroundProofPollingOwnership"]);
            Assert.Equal("true", backgroundCapability.Metadata["backgroundProofPollingEnabled"]);
            Assert.Equal("3600", backgroundCapability.Metadata["backgroundProofPollingIntervalSeconds"]);
            Assert.Equal("50", backgroundCapability.Metadata["backgroundProofPollingBatchLimit"]);
            Assert.Equal("cephalon-managed", summaryEntry.Metadata["backgroundProofPollingOwnership"]);
            Assert.Equal("true", summaryEntry.Metadata["backgroundProofPollingEnabled"]);
            Assert.Equal("3600", summaryEntry.Metadata["backgroundProofPollingIntervalSeconds"]);
            Assert.Equal("50", summaryEntry.Metadata["backgroundProofPollingBatchLimit"]);
            Assert.Equal("1", summaryEntry.Metadata["backgroundProofPollingRunCount"]);
            Assert.Equal("1", summaryEntry.Metadata["backgroundProofPollingSuccessfulRunCount"]);
            Assert.Equal("0", summaryEntry.Metadata["backgroundProofPollingFailedRunCount"]);
            Assert.Equal(TenantDomainOwnershipProofPollingOutcomes.Completed, summaryEntry.Metadata["backgroundProofPollingLastOutcome"]);
            Assert.Equal("1", summaryEntry.Metadata["backgroundProofPollingLastCandidateCount"]);
            Assert.Equal("1", summaryEntry.Metadata["backgroundProofPollingLastVerificationCount"]);
            Assert.Equal("1", summaryEntry.Metadata["backgroundProofPollingLastVerifiedCount"]);
        }
        finally
        {
            await hostedService.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task TenantDomainOwnershipProofEvaluatorVerifiesMatchingReportedProofAndUpdatesCatalog()
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
                    domainName: "proof.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var evaluator = provider.GetRequiredService<ITenantDomainOwnershipProofEvaluator>();
        var validator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var result = await evaluator.EvaluateAsync(new TenantDomainOwnershipProofEvaluationRequest(
            tenantId: "tenant-001",
            domainName: "PROOF.EXAMPLE.",
            observedProof: "cephalon-proof-token",
            expectedProof: "cephalon-proof-token",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt,
            source: "dns-provider",
            actor: "operator-001",
            atUtc: new DateTimeOffset(2026, 04, 29, 4, 0, 0, TimeSpan.Zero),
            correlationId: "corr-proof-001"));
        var validation = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "proof.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 4, 5, 0, TimeSpan.Zero)));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);
        var domainsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-domain-ownership");
        var summaryEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership-runtime");

        Assert.True(result.Matched);
        Assert.True(result.Applied);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.Verified, result.Outcome);
        Assert.Equal(TenantDomainOwnershipVerificationWorkflowCommands.Verify, result.WorkflowResult?.Command);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, result.WorkflowResult?.CurrentStatus);
        Assert.True(validation.Valid);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, domainOwnership.Status);
        Assert.Equal(new DateTimeOffset(2026, 04, 29, 4, 0, 0, TimeSpan.Zero), domainOwnership.VerifiedAtUtc);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.Verified, domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationOutcome]);
        Assert.Equal("dns-provider", domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationSource]);
        Assert.Equal("cephalon-managed", domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.ProofEvaluationOwnership]);
        Assert.Equal(result.ObservedProofFingerprint, domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationObservedFingerprint]);
        Assert.Equal(result.ExpectedProofFingerprint, domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationExpectedFingerprint]);
        Assert.Equal("corr-proof-001", domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationCorrelationId]);
        Assert.DoesNotContain(domainOwnership.Metadata, pair => string.Equals(pair.Value, "cephalon-proof-token", StringComparison.Ordinal));
        Assert.Equal("true", summaryEntry.Metadata["proofEvaluationEnabled"]);
        Assert.Equal("cephalon-managed", summaryEntry.Metadata["proofEvaluationOwnership"]);
        Assert.Equal("mixed", summaryEntry.Metadata["dnsHttpProofCollectionOwnership"]);
    }

    [Fact]
    public async Task TenantDomainOwnershipProofEvaluatorRejectsMismatchedProofThroughWorkflow()
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
                    domainName: "mismatch.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.HttpFile,
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof] = "expected-http-proof"
                    }));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var evaluator = provider.GetRequiredService<ITenantDomainOwnershipProofEvaluator>();
        var validator = provider.GetRequiredService<ITenantDomainOwnershipValidator>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();

        var result = await evaluator.EvaluateAsync(new TenantDomainOwnershipProofEvaluationRequest(
            tenantId: "tenant-001",
            domainName: "mismatch.example",
            observedProof: "observed-http-proof",
            verificationMethod: TenantDomainVerificationMethods.HttpFile,
            source: "http-provider",
            atUtc: new DateTimeOffset(2026, 04, 29, 4, 30, 0, TimeSpan.Zero)));
        var validation = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
            tenantId: "tenant-001",
            domainName: "mismatch.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 4, 35, 0, TimeSpan.Zero)));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);

        Assert.False(result.Matched);
        Assert.True(result.Applied);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.Rejected, result.Outcome);
        Assert.Equal(TenantDomainOwnershipVerificationWorkflowCommands.Reject, result.WorkflowResult?.Command);
        Assert.Equal(TenantDomainOwnershipStatuses.Rejected, result.WorkflowResult?.CurrentStatus);
        Assert.False(validation.Valid);
        Assert.Equal(TenantDomainOwnershipValidationOutcomes.Rejected, validation.Outcome);
        Assert.Equal(TenantDomainOwnershipStatuses.Rejected, domainOwnership.Status);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.Rejected, domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationOutcome]);
        Assert.Equal("http-provider", domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationSource]);
        Assert.Equal(result.ObservedProofFingerprint, domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationObservedFingerprint]);
        Assert.Equal(result.ExpectedProofFingerprint, domainOwnership.Metadata[TenantDomainOwnershipProofMetadataKeys.LastProofEvaluationExpectedFingerprint]);
    }

    [Fact]
    public async Task TenantDomainOwnershipProofEvaluatorReportsWorkflowDeniedWithoutMutatingState()
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
                    domainName: "verified.example",
                    status: TenantDomainOwnershipStatuses.Verified,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt,
                    verifiedAtUtc: new DateTimeOffset(2026, 04, 29, 3, 30, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>
                    {
                        [TenantDomainOwnershipProofMetadataKeys.ExpectedDnsTxtProof] = "expected-dns-proof"
                    }));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var evaluator = provider.GetRequiredService<ITenantDomainOwnershipProofEvaluator>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();

        var result = await evaluator.EvaluateAsync(new TenantDomainOwnershipProofEvaluationRequest(
            tenantId: "tenant-001",
            domainName: "verified.example",
            observedProof: "wrong-dns-proof",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt,
            source: "dns-provider",
            atUtc: new DateTimeOffset(2026, 04, 29, 4, 45, 0, TimeSpan.Zero)));
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);

        Assert.False(result.Matched);
        Assert.False(result.Applied);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.WorkflowDenied, result.Outcome);
        Assert.Equal(TenantDomainOwnershipVerificationWorkflowOutcomes.InvalidTransition, result.WorkflowResult?.Outcome);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, result.WorkflowResult?.CurrentStatus);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, domainOwnership.Status);
        Assert.Equal(new DateTimeOffset(2026, 04, 29, 3, 30, 0, TimeSpan.Zero), domainOwnership.VerifiedAtUtc);
    }

    [Fact]
    public async Task TenantDomainOwnershipProofEvaluatorReportsMissingProofAndBoundaryFailuresWithoutMutatingState()
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
                    domainName: "boundary.example",
                    status: TenantDomainOwnershipStatuses.Pending,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var evaluator = provider.GetRequiredService<ITenantDomainOwnershipProofEvaluator>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();

        var missingObserved = await evaluator.EvaluateAsync(new TenantDomainOwnershipProofEvaluationRequest(
            tenantId: "tenant-001",
            domainName: "boundary.example",
            expectedProof: "expected-proof",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt));
        var missingExpected = await evaluator.EvaluateAsync(new TenantDomainOwnershipProofEvaluationRequest(
            tenantId: "tenant-001",
            domainName: "boundary.example",
            observedProof: "observed-proof",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt));
        var tenantMismatch = await evaluator.EvaluateAsync(new TenantDomainOwnershipProofEvaluationRequest(
            tenantId: "tenant-002",
            domainName: "BOUNDARY.EXAMPLE.",
            observedProof: "expected-proof",
            expectedProof: "expected-proof",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt));
        var methodMismatch = await evaluator.EvaluateAsync(new TenantDomainOwnershipProofEvaluationRequest(
            tenantId: "tenant-001",
            domainName: "boundary.example",
            observedProof: "expected-proof",
            expectedProof: "expected-proof",
            verificationMethod: TenantDomainVerificationMethods.HttpFile));

        Assert.False(missingObserved.Applied);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.MissingObservedProof, missingObserved.Outcome);
        Assert.False(missingExpected.Applied);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.MissingExpectedProof, missingExpected.Outcome);
        Assert.False(tenantMismatch.Applied);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.TenantMismatch, tenantMismatch.Outcome);
        Assert.False(methodMismatch.Applied);
        Assert.Equal(TenantDomainOwnershipProofEvaluationOutcomes.VerificationMethodMismatch, methodMismatch.Outcome);
        var domainOwnership = Assert.Single(catalog.DomainOwnerships);
        Assert.Equal(TenantDomainOwnershipStatuses.Pending, domainOwnership.Status);
    }

    [Fact]
    public async Task TenantDomainOwnershipVerificationWorkflowPersistsThroughFileBackedStore()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"cephalon-domain-workflow-{Guid.NewGuid():N}");
        var storePath = Path.Combine(tempRoot, "tenant-domain-ownership.json");
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
                    options.DomainOwnershipStoreFilePath = storePath;
                });
            });

            await using (var provider = services.BuildServiceProvider())
            {
                var workflow = provider.GetRequiredService<ITenantDomainOwnershipVerificationWorkflow>();

                var created = await workflow.ApplyAsync(new TenantDomainOwnershipVerificationWorkflowRequest(
                    command: TenantDomainOwnershipVerificationWorkflowCommands.Request,
                    tenantId: "tenant-001",
                    domainName: "durable.example",
                    verificationMethod: TenantDomainVerificationMethods.HttpFile,
                    actor: "operator-001",
                    atUtc: new DateTimeOffset(2026, 04, 29, 3, 0, 0, TimeSpan.Zero)));
                var verified = await workflow.ApplyAsync(new TenantDomainOwnershipVerificationWorkflowRequest(
                    command: TenantDomainOwnershipVerificationWorkflowCommands.Verify,
                    tenantId: "tenant-001",
                    domainName: "durable.example",
                    verificationMethod: TenantDomainVerificationMethods.HttpFile,
                    actor: "tenant-owner",
                    atUtc: new DateTimeOffset(2026, 04, 29, 3, 5, 0, TimeSpan.Zero)));

                Assert.True(created.Applied);
                Assert.True(verified.Applied);
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
                    options.DomainOwnershipStoreFilePath = storePath;
                });
            });

            await using var restartedProvider = restartedServices.BuildServiceProvider();
            var catalog = restartedProvider.GetRequiredService<ITenantDomainOwnershipCatalog>();
            var validator = restartedProvider.GetRequiredService<ITenantDomainOwnershipValidator>();
            var technologyCatalog = restartedProvider.GetRequiredService<ITechnologyRuntimeCatalog>();

            var domainOwnership = Assert.Single(catalog.DomainOwnerships);
            var validation = await validator.ValidateAsync(new TenantDomainOwnershipValidationRequest(
                tenantId: "tenant-001",
                domainName: "DURABLE.EXAMPLE.",
                atUtc: new DateTimeOffset(2026, 04, 29, 3, 10, 0, TimeSpan.Zero)));
            var domainsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-domain-ownership");
            var summaryEntry = Assert.Single(domainsSurface.Entries, entry => entry.Id == "tenant-domain-ownership-runtime");

            Assert.Equal(TenantDomainOwnershipStatuses.Verified, domainOwnership.Status);
            Assert.True(validation.Valid);
            Assert.Equal("file", summaryEntry.Metadata["domainOwnershipStoreKind"]);
            Assert.Equal("true", summaryEntry.Metadata["domainOwnershipStoreDurable"]);
            Assert.Equal("cephalon-managed", summaryEntry.Metadata["durableStoreOwnership"]);
            Assert.Equal("1", summaryEntry.Metadata["runtimeDomainOwnershipCount"]);
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
    public async Task TenantDomainOwnershipVerificationWorkflowReportsStoreFailuresWithoutApplyingTransition()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantDomainOwnershipStore>(new FailingTenantDomainOwnershipStore());
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
        var workflow = provider.GetRequiredService<ITenantDomainOwnershipVerificationWorkflow>();
        var catalog = provider.GetRequiredService<ITenantDomainOwnershipCatalog>();

        var result = await workflow.ApplyAsync(new TenantDomainOwnershipVerificationWorkflowRequest(
            command: TenantDomainOwnershipVerificationWorkflowCommands.Request,
            tenantId: "tenant-001",
            domainName: "failing.example",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt));

        Assert.False(result.Applied);
        Assert.Equal(TenantDomainOwnershipVerificationWorkflowOutcomes.StoreFailed, result.Outcome);
        Assert.Empty(catalog.DomainOwnerships);
    }

    [Fact]
    public async Task TenantDomainOwnershipVerificationWorkflowRejectsInvalidTransitionsAndBoundaries()
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
        var workflow = provider.GetRequiredService<ITenantDomainOwnershipVerificationWorkflow>();

        var tenantMismatch = await workflow.ApplyAsync(new TenantDomainOwnershipVerificationWorkflowRequest(
            command: TenantDomainOwnershipVerificationWorkflowCommands.Suspend,
            tenantId: "tenant-002",
            domainName: "SHARED.EXAMPLE.",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt));
        var methodMismatch = await workflow.ApplyAsync(new TenantDomainOwnershipVerificationWorkflowRequest(
            command: TenantDomainOwnershipVerificationWorkflowCommands.Suspend,
            tenantId: "tenant-001",
            domainName: "shared.example",
            verificationMethod: TenantDomainVerificationMethods.HttpFile));
        var notFound = await workflow.ApplyAsync(new TenantDomainOwnershipVerificationWorkflowRequest(
            command: TenantDomainOwnershipVerificationWorkflowCommands.Verify,
            tenantId: "tenant-001",
            domainName: "missing.example"));
        var invalidTransition = await workflow.ApplyAsync(new TenantDomainOwnershipVerificationWorkflowRequest(
            command: TenantDomainOwnershipVerificationWorkflowCommands.Verify,
            tenantId: "tenant-001",
            domainName: "shared.example",
            verificationMethod: TenantDomainVerificationMethods.DnsTxt));

        Assert.False(tenantMismatch.Applied);
        Assert.Equal(TenantDomainOwnershipVerificationWorkflowOutcomes.TenantMismatch, tenantMismatch.Outcome);
        Assert.False(methodMismatch.Applied);
        Assert.Equal(TenantDomainOwnershipVerificationWorkflowOutcomes.VerificationMethodMismatch, methodMismatch.Outcome);
        Assert.False(notFound.Applied);
        Assert.Equal(TenantDomainOwnershipVerificationWorkflowOutcomes.NotFound, notFound.Outcome);
        Assert.False(invalidTransition.Applied);
        Assert.Equal(TenantDomainOwnershipVerificationWorkflowOutcomes.InvalidTransition, invalidTransition.Outcome);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, invalidTransition.PreviousStatus);
        Assert.Equal(TenantDomainOwnershipStatuses.Verified, invalidTransition.CurrentStatus);
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

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        Assert.True(condition());
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

    private sealed class TestHttpProofMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        public List<Uri> RequestedUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RequestedUris.Add(request.RequestUri!);
            return Task.FromResult(handler(request));
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

    private sealed class FailingTenantDomainOwnershipStore : ITenantDomainOwnershipStore
    {
        public string StoreKind => "failing-test";

        public bool IsDurable => true;

        public string Ownership => "application-managed";

        public IReadOnlyList<TenantDomainOwnershipDescriptor> DomainOwnerships => [];

        public int Count => 0;

        public void Upsert(TenantDomainOwnershipDescriptor domainOwnership)
        {
            throw new InvalidOperationException("Test store failure.");
        }
    }
}
