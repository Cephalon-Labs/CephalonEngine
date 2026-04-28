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
            });
        });

        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<ITenantMembershipCatalog>();
        var evaluator = provider.GetRequiredService<ITenantMembershipEvaluator>();
        var invitationCatalog = provider.GetRequiredService<ITenantInvitationCatalog>();
        var invitationValidator = provider.GetRequiredService<ITenantInvitationValidator>();
        var runtime = provider.GetRequiredService<global::Cephalon.Engine.Runtime.IRuntime>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var membershipsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-memberships");
        var invitationsSurface = Assert.Single(technologyCatalog.GetByTechnology("multi-tenancy"), surface => surface.SurfaceId == "tenant-invitations");
        var summaryEntry = Assert.Single(membershipsSurface.Entries, entry => entry.Id == "tenant-membership-runtime");
        var tenantEntry = Assert.Single(membershipsSurface.Entries, entry => entry.Id == "tenant-membership:tenant-001");
        var invitationSummaryEntry = Assert.Single(invitationsSurface.Entries, entry => entry.Id == "tenant-invitation-runtime");
        var tenantInvitationEntry = Assert.Single(invitationsSurface.Entries, entry => entry.Id == "tenant-invitations:tenant-001");
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

        Assert.Equal(2, catalog.Memberships.Count);
        Assert.Equal(2, invitationCatalog.Invitations.Count);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.membership.catalog");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.membership.evaluation");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.invitation.catalog");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "tenancy.invitation.validation");
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
        Assert.Equal(4510, diagnosticsConvention.MinimumEventId);
        Assert.Equal(4513, diagnosticsConvention.MaximumEventId);
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4510 && entry.Name == "TenantMembershipEvaluationAllowed");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4511 && entry.Name == "TenantMembershipEvaluationDenied");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4512 && entry.Name == "TenantInvitationValidationAllowed");
        Assert.Contains(diagnosticsConvention.Events, entry => entry.Id == 4513 && entry.Name == "TenantInvitationValidationDenied");
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
}
