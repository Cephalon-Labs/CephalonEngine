using Cephalon.Abstractions.Authorization;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Identity.Configuration;
using Cephalon.Identity.Registration;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

/// <summary>
/// Direct decision-matrix coverage for the built-in <c>MetadataDrivenAuthorizationEvaluator</c>.
/// The existing <see cref="IdentityPackTests"/> shape exercises the evaluator's happy path through
/// the bundled <see cref="IdentityAuthorizationTestModule"/> policies; these tests pin every
/// individual deny branch and the metadata aggregation surface that the evaluator advertises.
/// </summary>
public sealed class MetadataDrivenAuthorizationDecisionMatrixTests
{
    [Fact]
    public async Task EvaluateAsync_WhenPolicyIdMissingAndExplicitNotRequired_RecordsRelaxedReason()
    {
        await using var provider = await BuildAsync(requireExplicitPolicy: false);
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001"),
            NewResource(),
            new AuthorizationContext(action: "read"));

        Assert.False(decision.IsAllowed);
        Assert.Null(decision.PolicyId);
        Assert.Equal("denied", decision.Metadata["outcome"]);
        Assert.Equal("none", decision.Metadata["policyId"]);
        Assert.Contains("does not infer", decision.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_WhenPolicyIdMissingAndExplicitRequired_RecordsStrictReason()
    {
        await using var provider = await BuildAsync(requireExplicitPolicy: true);
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001"),
            NewResource(),
            new AuthorizationContext(action: "read"));

        Assert.False(decision.IsAllowed);
        Assert.Null(decision.PolicyId);
        Assert.Equal("denied", decision.Metadata["outcome"]);
        Assert.DoesNotContain("does not infer", decision.Reason!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("policy id", decision.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_WhenPolicyIdNotRegistered_DeniesWithMissingPolicyReason()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001"),
            NewResource(),
            new AuthorizationContext(action: "read", policyId: "policy-that-does-not-exist"));

        Assert.False(decision.IsAllowed);
        Assert.Equal("policy-that-does-not-exist", decision.PolicyId);
        Assert.Contains("not active", decision.Reason!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("0", decision.Metadata["ruleCount"]);
    }

    [Fact]
    public async Task EvaluateAsync_WhenRequiredRolesAnyMatch_AllowsAndAdvertisesPolicyModes()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001", roles: ["manager"]),
            NewResource(),
            ContextFor(IdentityDecisionMatrixTestModule.RolesAnyDefaultPolicyId));

        Assert.True(decision.IsAllowed);
        Assert.Equal(IdentityDecisionMatrixTestModule.RolesAnyDefaultPolicyId, decision.PolicyId);
        Assert.Equal("1", decision.Metadata["ruleCount"]);
        Assert.Equal("rbac,policy", decision.Metadata["modes"]);
        Assert.Equal("metadata-driven", decision.Metadata["evaluator"]);
    }

    [Fact]
    public async Task EvaluateAsync_WhenRequiredRolesAllSatisfied_Allows()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001", roles: ["admin", "manager"]),
            NewResource(),
            ContextFor(IdentityDecisionMatrixTestModule.RolesAllExplicitPolicyId));

        Assert.True(decision.IsAllowed);
        Assert.Equal("1", decision.Metadata["ruleCount"]);
    }

    [Fact]
    public async Task EvaluateAsync_WhenRequiredRolesAllMissingOne_DeniesWithMatchAllReason()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001", roles: ["admin"]),
            NewResource(),
            ContextFor(IdentityDecisionMatrixTestModule.RolesAllExplicitPolicyId));

        Assert.False(decision.IsAllowed);
        Assert.Contains("required role match 'all' was not satisfied", decision.Reason!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("1", decision.Metadata["ruleCount"]);
    }

    [Fact]
    public async Task EvaluateAsync_WhenRequiredRolesAreEmpty_DeniesBeforeIncrementingRuleCount()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001", roles: ["admin"]),
            NewResource(),
            ContextFor(IdentityDecisionMatrixTestModule.RolesEmptyPolicyId));

        Assert.False(decision.IsAllowed);
        Assert.Contains("required-roles rule without any roles", decision.Reason!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("0", decision.Metadata["ruleCount"]);
    }

    [Fact]
    public async Task EvaluateAsync_WhenSubjectAttributeMissing_DeniesWithSubjectKey()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001"),
            NewResource(),
            ContextFor(IdentityDecisionMatrixTestModule.SubjectAttributePolicyId));

        Assert.False(decision.IsAllowed);
        Assert.Contains("subject.region", decision.Reason!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("1", decision.Metadata["ruleCount"]);
    }

    [Fact]
    public async Task EvaluateAsync_WhenSubjectAttributeRequiredValueIsEmpty_DeniesWithEmptyValueReason()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001", attributes: new Dictionary<string, string> { ["region"] = "apac" }),
            NewResource(),
            ContextFor(IdentityDecisionMatrixTestModule.SubjectAttributeEmptyValuePolicyId));

        Assert.False(decision.IsAllowed);
        Assert.Contains("empty required value", decision.Reason!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("subject.region", decision.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_WhenSubjectSpecialDisplayNameMatches_Allows()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001", displayName: "Ada"),
            NewResource(),
            ContextFor(IdentityDecisionMatrixTestModule.SubjectSpecialDisplayNamePolicyId));

        Assert.True(decision.IsAllowed);
        Assert.Equal("1", decision.Metadata["ruleCount"]);
    }

    [Fact]
    public async Task EvaluateAsync_WhenResourceAttributeMatchesAcrossSpecialKey_Allows()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001"),
            new AuthorizationResource(
                resourceType: "document",
                resourceId: "doc-001"),
            ContextFor(IdentityDecisionMatrixTestModule.ResourceSpecialResourceTypePolicyId));

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task EvaluateAsync_WhenResourceAttributeMismatches_DeniesWithResourceKey()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001"),
            new AuthorizationResource(
                resourceType: "document",
                resourceId: "doc-001",
                attributes: new Dictionary<string, string> { ["classification"] = "public" }),
            ContextFor(IdentityDecisionMatrixTestModule.ResourceAttributePolicyId));

        Assert.False(decision.IsAllowed);
        Assert.Contains("resource.classification", decision.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_WhenContextAttributeMatchesAcrossSpecialKey_Allows()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001"),
            NewResource(),
            new AuthorizationContext(
                action: "read",
                policyId: IdentityDecisionMatrixTestModule.ContextSpecialActionPolicyId));

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task EvaluateAsync_WhenContextAttributeMismatches_DeniesWithContextKey()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001"),
            NewResource(),
            new AuthorizationContext(
                action: "read",
                policyId: IdentityDecisionMatrixTestModule.ContextAttributePolicyId,
                attributes: new Dictionary<string, string> { ["operation"] = "query" }));

        Assert.False(decision.IsAllowed);
        Assert.Contains("context.operation", decision.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_WhenRequireOwnerWithoutOwnerSubjectId_Denies()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001"),
            new AuthorizationResource(resourceType: "document", resourceId: "doc-001"),
            ContextFor(IdentityDecisionMatrixTestModule.RequireOwnerPolicyId));

        Assert.False(decision.IsAllowed);
        Assert.Contains("own the resource", decision.Reason!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("1", decision.Metadata["ruleCount"]);
    }

    [Fact]
    public async Task EvaluateAsync_WhenRequireOwnerWithDifferentOwner_Denies()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001"),
            new AuthorizationResource(
                resourceType: "document",
                resourceId: "doc-001",
                ownerSubjectId: "user-002"),
            ContextFor(IdentityDecisionMatrixTestModule.RequireOwnerPolicyId));

        Assert.False(decision.IsAllowed);
        Assert.Contains("own the resource", decision.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_WhenRequireTenantMatchWithoutContextTenant_Denies()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001", tenantIds: ["tenant-001"]),
            new AuthorizationResource(
                resourceType: "document",
                resourceId: "doc-001",
                tenantId: "tenant-001"),
            new AuthorizationContext(
                action: "read",
                policyId: IdentityDecisionMatrixTestModule.RequireTenantMatchPolicyId));

        Assert.False(decision.IsAllowed);
        Assert.Contains("tenant boundary", decision.Reason!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("1", decision.Metadata["ruleCount"]);
    }

    [Fact]
    public async Task EvaluateAsync_WhenSubjectMissingFromTenantList_DeniesAcrossTenantMatch()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001", tenantIds: ["tenant-other"]),
            new AuthorizationResource(
                resourceType: "document",
                resourceId: "doc-001",
                tenantId: "tenant-001"),
            new AuthorizationContext(
                action: "read",
                policyId: IdentityDecisionMatrixTestModule.RequireTenantMatchPolicyId,
                tenantId: "tenant-001"));

        Assert.False(decision.IsAllowed);
        Assert.Contains("tenant boundary", decision.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_WhenCompositeRulesAllSatisfied_AggregatesRuleCount()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject(
                "user-001",
                tenantIds: ["tenant-001"],
                attributes: new Dictionary<string, string> { ["region"] = "apac" }),
            new AuthorizationResource(
                resourceType: "document",
                resourceId: "doc-001",
                tenantId: "tenant-001"),
            new AuthorizationContext(
                action: "read",
                policyId: IdentityDecisionMatrixTestModule.CompositeTenantAndAttributePolicyId,
                tenantId: "tenant-001"));

        Assert.True(decision.IsAllowed);
        Assert.Equal("2", decision.Metadata["ruleCount"]);
    }

    [Fact]
    public async Task EvaluateAsync_WhenPolicyHasNoMetadataRules_DeniesAndReportsZeroRules()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001"),
            NewResource(),
            ContextFor(IdentityDecisionMatrixTestModule.NoMetadataRulesPolicyId));

        Assert.False(decision.IsAllowed);
        Assert.Equal("0", decision.Metadata["ruleCount"]);
        Assert.Contains("does not declare any metadata-driven rules", decision.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_WhenPolicyDeclaresNoModes_FallsBackToConfiguredModes()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        var decision = await evaluator.EvaluateAsync(
            NewSubject("user-001", roles: ["auditor"]),
            NewResource(),
            ContextFor(IdentityDecisionMatrixTestModule.ModeFallbackPolicyId));

        Assert.True(decision.IsAllowed);
        Assert.Equal("3", decision.Metadata["modeCount"]);
        Assert.Equal("rbac,abac,policy", decision.Metadata["modes"]);
    }

    [Fact]
    public async Task EvaluateAsync_WhenCancellationRequested_ThrowsOperationCanceled()
    {
        await using var provider = await BuildAsync();
        var evaluator = provider.GetRequiredService<IAuthorizationEvaluator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            evaluator.EvaluateAsync(
                NewSubject("user-001"),
                NewResource(),
                ContextFor(IdentityDecisionMatrixTestModule.RolesAnyDefaultPolicyId),
                cts.Token).AsTask());
    }

    private static AuthorizationContext ContextFor(string policyId)
    {
        return new AuthorizationContext(action: "evaluate", policyId: policyId);
    }

    private static AuthorizationSubject NewSubject(
        string subjectId,
        string? displayName = null,
        IReadOnlyList<string>? roles = null,
        IReadOnlyList<string>? tenantIds = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        return new AuthorizationSubject(
            subjectId: subjectId,
            displayName: displayName,
            roles: roles,
            tenantIds: tenantIds,
            attributes: attributes);
    }

    private static AuthorizationResource NewResource()
    {
        return new AuthorizationResource(
            resourceType: "document",
            resourceId: "doc-default");
    }

    private static ValueTask<ServiceProvider> BuildAsync(bool requireExplicitPolicy = true)
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                technologies: ["IdentityAccess"],
                identity: new IdentitySettings(
                    enabled: true,
                    authorizationModes: ["RBAC", "ABAC", "Policy"])));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new IdentityDecisionMatrixTestModule());
            engine.AddIdentityAccess(options =>
            {
                options.RequireExplicitPolicy = requireExplicitPolicy;
            });
        });

        return new ValueTask<ServiceProvider>(services.BuildServiceProvider());
    }
}
