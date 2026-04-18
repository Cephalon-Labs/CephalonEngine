using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Benchmarks.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Cephalon.Benchmarks.Runtime;

/// <summary>
/// Measures the end-to-end ASP.NET Core REST projection path for module-owned behavior routes,
/// including generated/profile/DSL precedence, authoring policy, suppression, override, grouped
/// generated ownership, binding fallback, and runtime catalog materialization.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class RestEndpointProjectionGovernanceBenchmarks
{
    private const int HostsPerInvocation = 8;
    private const string ProfileAuthoringStyle = "behavior-module-profile";
    private const string DslAuthoringStyle = "behavior-module-dsl";
    private const string GeneratedAuthoringStyle = "behavior-module-generated";
    private readonly int hostsPerInvocation = HostsPerInvocation;

    private static readonly KeyValuePair<string, string?>[] ConfigurationEntries =
    [
        new("OpenApi:DefaultVersion", "12"),
        new("OpenApi:EnabledVersions:0", "6"),
        new("OpenApi:EnabledVersions:1", "7"),
        new("OpenApi:EnabledVersions:2", "8"),
        new("OpenApi:EnabledVersions:3", "9"),
        new("OpenApi:EnabledVersions:4", "10"),
        new("OpenApi:EnabledVersions:5", "11"),
        new("OpenApi:EnabledVersions:6", "12"),
        new($"RestApi:AuthoringPolicies:{BenchmarkRestScenarioIds.AuthoringPolicyBehaviorId}:AllowedAuthoringStyles:0", ProfileAuthoringStyle),
        new($"RestApi:Suppressions:{BenchmarkRestScenarioIds.GroupedPrefixSuppressionRuleId}:BehaviorIdPrefixes:0", BenchmarkRestScenarioIds.GroupedOrdersPrefix),
        new($"RestApi:Suppressions:{BenchmarkRestScenarioIds.GroupedLookupSuppressionRuleId}:Behaviors:0", BenchmarkRestScenarioIds.GroupedOrdersLookupBehaviorId),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.BindingOverrideRuleId}:Behaviors:0", BenchmarkRestScenarioIds.BindingOverrideBehaviorId),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.BindingOverrideRuleId}:EndpointName", BenchmarkRestScenarioIds.GovernedBindingEndpointName),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.BindingOverrideRuleId}:Summary", "Governed benchmark binding lookup."),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.BindingOverrideRuleId}:Description", "Measures binding override materialization for benchmark governance."),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.BindingOverrideRuleId}:RequiredCapabilityKey", BenchmarkRestScenarioIds.BindingGovernedCapabilityKey),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.BindingOverrideRuleId}:BindingMode", "merge-explicit"),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.BindingOverrideRuleId}:RemovedBindingProperties:0", nameof(BenchmarkBoundLookupInput.Quantity)),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.BindingOverrideRuleId}:Bindings:0:PropertyName", nameof(BenchmarkBoundLookupInput.Status)),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.BindingOverrideRuleId}:Bindings:0:Source", "query"),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.BindingOverrideRuleId}:Bindings:0:Name", "status"),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.BindingOverrideRuleId}:PreserveImplicitQueryFallback", "true"),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.ExplicitScopeOverrideRuleId}:HostGovernanceScopes:0", BenchmarkRestScenarioIds.ExplicitScope),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.ExplicitScopeOverrideRuleId}:AuthoringStyles:0", DslAuthoringStyle),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.ExplicitScopeOverrideRuleId}:TagName", "Benchmark Explicit Governed API"),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.ExplicitScopeOverrideRuleId}:RequiredCapabilityKey", BenchmarkRestScenarioIds.ExplicitScopeCapabilityKey),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.ExplicitExactOverrideRuleId}:Behaviors:0", BenchmarkRestScenarioIds.ExplicitGovernedBehaviorId),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.ExplicitExactOverrideRuleId}:AuthoringStyles:0", DslAuthoringStyle),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.ExplicitExactOverrideRuleId}:ApiVersionMajor", "12"),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.ExplicitExactOverrideRuleId}:EndpointName", BenchmarkRestScenarioIds.GovernedExplicitEndpointName),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.ExplicitExactOverrideRuleId}:Summary", "Governed benchmark explicit lookup."),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.ExplicitExactOverrideRuleId}:Description", "Measures opt-in explicit REST override materialization."),
        new($"RestApi:Overrides:{BenchmarkRestScenarioIds.ExplicitExactOverrideRuleId}:RequiredCapabilityKey", BenchmarkRestScenarioIds.ExplicitGovernedCapabilityKey)
    ];

    /// <summary>
    /// Builds a fresh ASP.NET Core host, maps Cephalon, and resolves the runtime REST catalogs for
    /// the governed benchmark scenario.
    /// </summary>
    /// <returns>A deterministic signal derived from the resolved runtime catalog state.</returns>
    [Benchmark(OperationsPerInvoke = HostsPerInvocation)]
    public async Task<int> BuildMapGovernedRestCatalogs()
    {
        var signal = 0;

        for (var index = 0; index < hostsPerInvocation; index++)
        {
            await using var app = CreateApplication();
            app.MapCephalon();
            signal += ValidateGovernedRestScenario(app.Services);
        }

        return signal;
    }

    private static WebApplication CreateApplication()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        foreach (var (key, value) in ConfigurationEntries)
        {
            builder.Configuration[key] = value;
        }

        builder.AddCephalon(BenchmarkScenarioFactory.ConfigureRestGovernanceEngine);
        return builder.Build();
    }

    private static int ValidateGovernedRestScenario(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var endpointCatalog = services.GetRequiredService<IRestEndpointRuntimeCatalog>();
        var candidateCatalog = services.GetRequiredService<IRestEndpointCandidateRuntimeCatalog>();
        var publicationGroupCatalog = services.GetRequiredService<IRestEndpointPublicationGroupRuntimeCatalog>();
        var authoringPolicyCatalog = services.GetRequiredService<IRestEndpointAuthoringPolicyRuntimeCatalog>();
        var overrideCatalog = services.GetRequiredService<IRestEndpointOverrideRuntimeCatalog>();
        var suppressionCatalog = services.GetRequiredService<IRestEndpointSuppressionRuntimeCatalog>();

        var endpoints = endpointCatalog.Endpoints;
        var candidates = candidateCatalog.Candidates;
        var publicationGroups = publicationGroupCatalog.Groups;
        var authoringPolicies = authoringPolicyCatalog.Policies;
        var overrides = overrideCatalog.OverrideRules;
        var suppressions = suppressionCatalog.Suppressions;

        try
        {
            if (endpoints.Count != 5)
            {
                throw new InvalidOperationException($"Expected 5 published REST endpoints but found {endpoints.Count}.");
            }

            if (candidates.Count != 11)
            {
                throw new InvalidOperationException($"Expected 11 REST candidates but found {candidates.Count}.");
            }

            if (publicationGroups.Count != 7)
            {
                throw new InvalidOperationException($"Expected 7 publication groups but found {publicationGroups.Count}.");
            }

            if (authoringPolicies.Count != 7)
            {
                throw new InvalidOperationException($"Expected 7 authoring policies but found {authoringPolicies.Count}.");
            }

            if (overrides.Count != 3)
            {
                throw new InvalidOperationException($"Expected 3 override rules but found {overrides.Count}.");
            }

            if (suppressions.Count != 2)
            {
                throw new InvalidOperationException($"Expected 2 suppression rules but found {suppressions.Count}.");
            }

            var threeWayCandidates = candidateCatalog.GetByBehaviorId(BenchmarkRestScenarioIds.ThreeWayBehaviorId);
            if (threeWayCandidates.Count != 3 ||
                threeWayCandidates.Count(static candidate => candidate.Status == RestEndpointCandidateStatus.Published) != 1 ||
                threeWayCandidates.Count(static candidate =>
                    candidate.Status == RestEndpointCandidateStatus.Suppressed &&
                    !string.IsNullOrWhiteSpace(candidate.SuppressedByCandidateId)) != 2)
            {
                throw new InvalidOperationException("The three-way REST precedence scenario did not resolve as expected.");
            }

            var threeWayEndpoint = GetSingle(endpointCatalog.GetByBehaviorId(BenchmarkRestScenarioIds.ThreeWayBehaviorId));
            if (!string.Equals(threeWayEndpoint.EndpointName, BenchmarkRestScenarioIds.ThreeWayPublishedEndpointName, StringComparison.Ordinal) ||
                !string.Equals(threeWayEndpoint.RoutePattern, "/api/v6/bench/runtime/three-way/explicit/{orderId}", StringComparison.Ordinal) ||
                threeWayEndpoint.CandidateId is null)
            {
                throw new InvalidOperationException(
                    $"The published three-way REST endpoint drifted from the benchmark scenario. " +
                    $"EndpointName='{threeWayEndpoint.EndpointName ?? "(null)"}', " +
                    $"RoutePattern='{threeWayEndpoint.RoutePattern}', " +
                    $"CandidateId='{threeWayEndpoint.CandidateId ?? "(null)"}'.");
            }

            var authoringCandidates = candidateCatalog.GetByBehaviorId(BenchmarkRestScenarioIds.AuthoringPolicyBehaviorId);
            if (authoringCandidates.Count != 3 ||
                authoringCandidates.Count(static candidate => candidate.Status == RestEndpointCandidateStatus.Published) != 1 ||
                authoringCandidates.Count(static candidate =>
                    candidate.Status == RestEndpointCandidateStatus.Suppressed &&
                    !string.IsNullOrWhiteSpace(candidate.SuppressedByCandidateId)) != 1 ||
                authoringCandidates.Count(static candidate =>
                    candidate.SuppressedByAuthoringPolicyKind == RestEndpointAuthoringPolicySuppressionKind.NotAllowedAuthoringStyle) != 1)
            {
                throw new InvalidOperationException("The authoring-policy REST scenario did not resolve as expected.");
            }

            var authoringPolicy = authoringPolicyCatalog.GetByBehaviorId(BenchmarkRestScenarioIds.AuthoringPolicyBehaviorId)
                ?? throw new InvalidOperationException("The authoring-policy runtime catalog is missing the benchmark behavior.");
            if (authoringPolicy.PublishedCandidateIds.Count != 1 ||
                authoringPolicy.PrecedenceSuppressedCandidateIds.Count != 1 ||
                authoringPolicy.GovernanceSuppressedCandidateIds.Count != 0 ||
                authoringPolicy.SuppressedCandidateIds.Count != 1 ||
                authoringPolicy.AllowedAuthoringStyles.Count != 1 ||
                !string.Equals(authoringPolicy.AllowedAuthoringStyles[0], ProfileAuthoringStyle, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The authoring-policy runtime catalog drifted from the benchmark scenario.");
            }

            var publishedAuthoringCandidate = GetSingle(authoringCandidates
                .Where(static candidate => candidate.Status == RestEndpointCandidateStatus.Published)
                .ToArray());
            if (!string.Equals(publishedAuthoringCandidate.AuthoringStyle, DslAuthoringStyle, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The explicit DSL authoring candidate should remain authoritative in the benchmark scenario.");
            }

            var authoringSuppressedCandidate = GetSingle(authoringCandidates
                .Where(static candidate => candidate.SuppressedByAuthoringPolicyKind.HasValue)
                .ToArray());
            if (!string.Equals(authoringSuppressedCandidate.AuthoringStyle, GeneratedAuthoringStyle, StringComparison.Ordinal) ||
                authoringSuppressedCandidate.SuppressedByAuthoringPolicyKind != RestEndpointAuthoringPolicySuppressionKind.NotAllowedAuthoringStyle)
            {
                throw new InvalidOperationException("The shorthand generated authoring candidate did not carry the expected authoring-policy suppression.");
            }

            var authoringPrecedenceSuppressedCandidate = GetSingle(authoringCandidates
                .Where(static candidate =>
                    candidate.Status == RestEndpointCandidateStatus.Suppressed &&
                    !string.IsNullOrWhiteSpace(candidate.SuppressedByCandidateId))
                .ToArray());
            if (!string.Equals(authoringPrecedenceSuppressedCandidate.AuthoringStyle, ProfileAuthoringStyle, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The shorthand profile authoring candidate should survive policy enforcement and then lose on precedence.");
            }

            var bindingEndpoint = GetSingle(endpointCatalog.GetByBehaviorId(BenchmarkRestScenarioIds.BindingOverrideBehaviorId));
            if (!string.Equals(bindingEndpoint.EndpointName, BenchmarkRestScenarioIds.GovernedBindingEndpointName, StringComparison.Ordinal) ||
                !string.Equals(bindingEndpoint.AppliedOverrideId, BenchmarkRestScenarioIds.BindingOverrideRuleId, StringComparison.Ordinal) ||
                !string.Equals(bindingEndpoint.RequiredCapabilityKey, BenchmarkRestScenarioIds.BindingGovernedCapabilityKey, StringComparison.Ordinal) ||
                bindingEndpoint.BindingDescriptors.Count != 2 ||
                bindingEndpoint.BindingFallbackMode != RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback ||
                !bindingEndpoint.BindingDescriptors.Any(static binding =>
                    string.Equals(binding.PropertyName, nameof(BenchmarkBoundLookupInput.OrderId), StringComparison.Ordinal) &&
                    binding.Source == RestEndpointBindingSource.Route &&
                    string.Equals(binding.Name, "orderId", StringComparison.Ordinal)) ||
                !bindingEndpoint.BindingDescriptors.Any(static binding =>
                    string.Equals(binding.PropertyName, nameof(BenchmarkBoundLookupInput.Status), StringComparison.Ordinal) &&
                    binding.Source == RestEndpointBindingSource.Query &&
                    string.Equals(binding.Name, "status", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("The binding-override REST endpoint drifted from the benchmark scenario.");
            }

            var explicitEndpoint = GetSingle(endpointCatalog.GetByBehaviorId(BenchmarkRestScenarioIds.ExplicitGovernedBehaviorId));
            if (!string.Equals(explicitEndpoint.EndpointName, BenchmarkRestScenarioIds.GovernedExplicitEndpointName, StringComparison.Ordinal) ||
                !string.Equals(explicitEndpoint.AppliedOverrideId, BenchmarkRestScenarioIds.ExplicitExactOverrideRuleId, StringComparison.Ordinal) ||
                !string.Equals(explicitEndpoint.RequiredCapabilityKey, BenchmarkRestScenarioIds.ExplicitGovernedCapabilityKey, StringComparison.Ordinal) ||
                explicitEndpoint.ApiVersionMajor != 11 ||
                !string.Equals(explicitEndpoint.RoutePattern, "/api/v11/bench/runtime/explicit/{orderId}", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The explicit-governance REST endpoint drifted from the benchmark scenario.");
            }

            var explicitCandidates = candidateCatalog.GetByBehaviorId(BenchmarkRestScenarioIds.ExplicitGovernedBehaviorId);
            if (explicitCandidates.Count != 1 ||
                explicitCandidates[0].MatchedOverrideIds.Count != 2 ||
                !string.Equals(explicitCandidates[0].SelectedOverrideId, BenchmarkRestScenarioIds.ExplicitExactOverrideRuleId, StringComparison.Ordinal) ||
                explicitCandidates[0].OverrideSelectionBasis != RestEndpointGovernanceRuleSelectionBasis.BehaviorTargeting ||
                !explicitCandidates[0].SelectedOverrideActionKinds.Contains(RestEndpointOverrideActionKind.ApiVersionMajor) ||
                explicitCandidates[0].AppliedOverrideActionKinds.Contains(RestEndpointOverrideActionKind.ApiVersionMajor))
            {
                throw new InvalidOperationException("The explicit-governance override selection drifted from the benchmark scenario.");
            }

            var groupedOrdersLookupCandidate = GetSingle(candidateCatalog.GetByBehaviorId(BenchmarkRestScenarioIds.GroupedOrdersLookupBehaviorId));
            if (!string.Equals(groupedOrdersLookupCandidate.SuppressedBySuppressionId, BenchmarkRestScenarioIds.GroupedLookupSuppressionRuleId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The grouped orders lookup suppression drifted from the benchmark scenario.");
            }

            var groupedOrdersCreateCandidate = GetSingle(candidateCatalog.GetByBehaviorId(BenchmarkRestScenarioIds.GroupedOrdersCreateBehaviorId));
            if (!string.Equals(groupedOrdersCreateCandidate.SuppressedBySuppressionId, BenchmarkRestScenarioIds.GroupedPrefixSuppressionRuleId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The grouped orders create suppression drifted from the benchmark scenario.");
            }

            var groupedInventoryEndpoint = GetSingle(endpointCatalog.GetByBehaviorId(BenchmarkRestScenarioIds.GroupedInventoryLookupBehaviorId));
            if (!string.Equals(groupedInventoryEndpoint.SourceModuleId, BenchmarkRestScenarioIds.GeneratedGroupsModuleId, StringComparison.Ordinal) ||
                !string.Equals(groupedInventoryEndpoint.EndpointName, BenchmarkRestScenarioIds.GroupedInventoryPublishedEndpointName, StringComparison.Ordinal) ||
                !string.Equals(groupedInventoryEndpoint.RoutePattern, "/api/v9/bench/rest/grouped/inventory/{sku}", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The grouped generated REST ownership scenario drifted from the benchmark scenario.");
            }

            var exactSuppression = suppressionCatalog.GetById(BenchmarkRestScenarioIds.GroupedLookupSuppressionRuleId)
                ?? throw new InvalidOperationException("The exact grouped suppression rule was not materialized.");
            if (exactSuppression.SuppressedCandidateIds.Count != 1 ||
                exactSuppression.SelectionBases.Count == 0)
            {
                throw new InvalidOperationException("The exact grouped suppression runtime catalog drifted from the benchmark scenario.");
            }

            var prefixSuppression = suppressionCatalog.GetById(BenchmarkRestScenarioIds.GroupedPrefixSuppressionRuleId)
                ?? throw new InvalidOperationException("The prefix grouped suppression rule was not materialized.");
            if (prefixSuppression.MatchedCandidateIds.Count != 2 ||
                prefixSuppression.SuppressedCandidateIds.Count != 1)
            {
                throw new InvalidOperationException("The prefix grouped suppression runtime catalog drifted from the benchmark scenario.");
            }

            var exactOverride = overrideCatalog.GetById(BenchmarkRestScenarioIds.ExplicitExactOverrideRuleId)
                ?? throw new InvalidOperationException("The exact explicit override rule was not materialized.");
            if (exactOverride.SelectedCandidateIds.Count != 1 ||
                exactOverride.AppliedCandidateIds.Count != 1 ||
                exactOverride.SelectionBases.Count == 0 ||
                !exactOverride.SelectedActionKinds.Contains(RestEndpointOverrideActionKind.ApiVersionMajor) ||
                exactOverride.AppliedActionKinds.Contains(RestEndpointOverrideActionKind.ApiVersionMajor) ||
                !exactOverride.AppliedActionKinds.Contains(RestEndpointOverrideActionKind.EndpointName) ||
                !exactOverride.AppliedActionKinds.Contains(RestEndpointOverrideActionKind.RequiredCapabilityKey))
            {
                throw new InvalidOperationException("The explicit override runtime catalog drifted from the benchmark scenario.");
            }

            var scopeOverride = overrideCatalog.GetById(BenchmarkRestScenarioIds.ExplicitScopeOverrideRuleId)
                ?? throw new InvalidOperationException("The scope-targeted explicit override rule was not materialized.");
            if (scopeOverride.MatchedCandidateIds.Count != 1 ||
                scopeOverride.SelectedCandidateIds.Count != 0 ||
                scopeOverride.AppliedCandidateIds.Count != 0)
            {
                throw new InvalidOperationException("The broader scope-targeted explicit override should match but lose selection in the benchmark scenario.");
            }

            var bindingOverride = overrideCatalog.GetById(BenchmarkRestScenarioIds.BindingOverrideRuleId)
                ?? throw new InvalidOperationException("The binding override rule was not materialized.");
            if (bindingOverride.AppliedCandidateIds.Count != 1 ||
                !bindingOverride.AppliedActionKinds.Contains(RestEndpointOverrideActionKind.PreserveImplicitQueryFallback))
            {
                throw new InvalidOperationException("The binding override runtime catalog drifted from the benchmark scenario.");
            }

            return endpoints.Count +
                   candidates.Count +
                   publicationGroups.Count +
                   authoringPolicies.Count +
                   overrides.Count +
                   suppressions.Count +
                   endpointCatalog.GetBySourceModule(BenchmarkRestScenarioIds.GovernanceModuleId).Count +
                   endpointCatalog.GetBySourceModule(BenchmarkRestScenarioIds.GeneratedGroupsModuleId).Count +
                   candidateCatalog.GetBySourceModule(BenchmarkRestScenarioIds.GovernanceModuleId).Count +
                   candidateCatalog.GetBySourceModule(BenchmarkRestScenarioIds.GeneratedGroupsModuleId).Count;
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                $"{ex.Message}{Environment.NewLine}{DescribeRuntimeState(endpoints, candidates, publicationGroups, authoringPolicies, overrides, suppressions)}",
                ex);
        }
    }

    private static T GetSingle<T>(IReadOnlyList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values.Count == 1
            ? values[0]
            : throw new InvalidOperationException($"Expected a single value but found {values.Count}.");
    }

    private static string DescribeRuntimeState(
        IReadOnlyList<RestEndpointRuntimeDescriptor> endpoints,
        IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> candidates,
        IReadOnlyList<RestEndpointPublicationGroupDescriptor> publicationGroups,
        IReadOnlyList<RestEndpointAuthoringPolicyDescriptor> authoringPolicies,
        IReadOnlyList<RestEndpointOverrideDescriptor> overrides,
        IReadOnlyList<RestEndpointSuppressionDescriptor> suppressions)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(publicationGroups);
        ArgumentNullException.ThrowIfNull(authoringPolicies);
        ArgumentNullException.ThrowIfNull(overrides);
        ArgumentNullException.ThrowIfNull(suppressions);

        var builder = new StringBuilder();
        builder.AppendLine("Runtime state dump:");

        builder.AppendLine("Endpoints:");
        foreach (var endpoint in endpoints
                     .OrderBy(static endpoint => endpoint.BehaviorId, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static endpoint => endpoint.RoutePattern, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append("  EP|");
            builder.Append(endpoint.BehaviorId ?? "(none)");
            builder.Append('|');
            builder.Append(endpoint.EndpointName ?? "(none)");
            builder.Append('|');
            builder.Append(endpoint.RoutePattern);
            builder.Append('|');
            builder.Append(endpoint.AuthoringStyle ?? "(none)");
            builder.Append('|');
            builder.Append(endpoint.SourceModuleId ?? "(none)");
            builder.Append('|');
            builder.Append(endpoint.CandidateId ?? "(none)");
            builder.Append('|');
            builder.Append(endpoint.AppliedOverrideId ?? "(none)");
            builder.Append('|');
            builder.Append(endpoint.RequiredCapabilityKey ?? "(none)");
            builder.Append('|');
            builder.Append(endpoint.BindingFallbackMode?.ToString() ?? "(none)");
            builder.Append('|');
            builder.Append(FormatBindings(endpoint.BindingDescriptors));
            builder.AppendLine();
        }

        builder.AppendLine("Candidates:");
        foreach (var candidate in candidates
                     .OrderBy(static candidate => candidate.ProjectedEndpoint.BehaviorId, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static candidate => candidate.PrecedenceRank))
        {
            builder.Append("  CAND|");
            builder.Append(candidate.ProjectedEndpoint.BehaviorId ?? "(none)");
            builder.Append('|');
            builder.Append(candidate.Id);
            builder.Append('|');
            builder.Append(candidate.AuthoringStyle);
            builder.Append('|');
            builder.Append(candidate.PrecedenceRank);
            builder.Append('|');
            builder.Append(candidate.Status);
            builder.Append('|');
            builder.Append(candidate.ProjectedEndpoint.EndpointName ?? "(none)");
            builder.Append('|');
            builder.Append(candidate.ProjectedEndpoint.RoutePattern);
            builder.Append('|');
            builder.Append(candidate.ProjectedEndpoint.OriginalEndpointName ?? "(none)");
            builder.Append('|');
            builder.Append(candidate.SuppressedByCandidateId ?? "(none)");
            builder.Append('|');
            builder.Append(candidate.SuppressedBySuppressionId ?? "(none)");
            builder.Append('|');
            builder.Append(candidate.SuppressedByAuthoringPolicyKind?.ToString() ?? "(none)");
            builder.Append('|');
            builder.Append(candidate.AppliedOverrideId ?? "(none)");
            builder.Append('|');
            builder.Append(candidate.SelectedOverrideId ?? "(none)");
            builder.Append('|');
            builder.Append(candidate.OverrideSelectionBasis?.ToString() ?? "(none)");
            builder.Append('|');
            builder.Append(candidate.OriginalProjection.AllowsHostGovernance);
            builder.Append('|');
            builder.Append(candidate.OriginalProjection.HostGovernanceScope ?? "(none)");
            builder.Append('|');
            builder.Append(FormatList(candidate.MatchedSuppressionIds));
            builder.Append('|');
            builder.Append(FormatList(candidate.MatchedOverrideIds));
            builder.Append('|');
            builder.Append(FormatList(candidate.SkippedSuppressionIds));
            builder.Append('|');
            builder.Append(FormatList(candidate.SkippedOverrideIds));
            builder.Append('|');
            builder.Append(FormatBindings(candidate.ProjectedEndpoint.BindingDescriptors));
            builder.AppendLine();
        }

        builder.AppendLine("PublicationGroups:");
        foreach (var group in publicationGroups.OrderBy(static group => group.BehaviorId, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append("  GROUP|");
            builder.Append(group.BehaviorId);
            builder.Append('|');
            builder.Append(FormatList(group.PublishedCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(group.PrecedenceSuppressedCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(group.GovernanceSuppressedCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(group.AuthoringPolicySuppressedCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(group.HostGovernanceEligibleCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(group.HostGovernanceIneligibleCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(group.SkippedSuppressionIds));
            builder.Append('|');
            builder.Append(FormatList(group.SkippedOverrideIds));
            builder.AppendLine();
        }

        builder.AppendLine("AuthoringPolicies:");
        foreach (var policy in authoringPolicies.OrderBy(static policy => policy.BehaviorId, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append("  POLICY|");
            builder.Append(policy.BehaviorId);
            builder.Append('|');
            builder.Append(policy.IsConfigured);
            builder.Append('|');
            builder.Append(policy.AllowMultiplePublishedCandidates);
            builder.Append('|');
            builder.Append(policy.PreferredAuthoringStyle ?? "(none)");
            builder.Append('|');
            builder.Append(FormatList(policy.AllowedAuthoringStyles));
            builder.Append('|');
            builder.Append(FormatList(policy.PublishedCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(policy.PrecedenceSuppressedCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(policy.GovernanceSuppressedCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(policy.SuppressedCandidateIds));
            builder.AppendLine();
        }

        builder.AppendLine("Overrides:");
        foreach (var item in overrides.OrderBy(static item => item.Id, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append("  OVERRIDE|");
            builder.Append(item.Id);
            builder.Append('|');
            builder.Append(FormatList(item.BehaviorIds));
            builder.Append('|');
            builder.Append(FormatList(item.BehaviorIdPrefixes));
            builder.Append('|');
            builder.Append(FormatList(item.HostGovernanceScopes));
            builder.Append('|');
            builder.Append(FormatList(item.AuthoringStyles));
            builder.Append('|');
            builder.Append(item.EndpointName ?? "(none)");
            builder.Append('|');
            builder.Append(item.RequiredCapabilityKey ?? "(none)");
            builder.Append('|');
            builder.Append(item.PreserveImplicitQueryFallback);
            builder.Append('|');
            builder.Append(FormatList(item.MatchedCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(item.SelectedCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(item.AppliedCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(item.SelectionBases.Select(static basis => basis.ToString()).ToArray()));
            builder.Append('|');
            builder.Append(FormatList(item.ActionKinds.Select(static actionKind => actionKind.ToString()).ToArray()));
            builder.AppendLine();
        }

        builder.AppendLine("Suppressions:");
        foreach (var item in suppressions.OrderBy(static item => item.Id, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append("  SUPPRESSION|");
            builder.Append(item.Id);
            builder.Append('|');
            builder.Append(FormatList(item.BehaviorIds));
            builder.Append('|');
            builder.Append(FormatList(item.BehaviorIdPrefixes));
            builder.Append('|');
            builder.Append(FormatList(item.HostGovernanceScopes));
            builder.Append('|');
            builder.Append(FormatList(item.MatchedCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(item.SuppressedCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(item.SkippedCandidateIds));
            builder.Append('|');
            builder.Append(FormatList(item.SelectionBases.Select(static basis => basis.ToString()).ToArray()));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string FormatBindings(IReadOnlyList<RestEndpointBindingDescriptor> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        return bindings.Count == 0
            ? "(none)"
            : string.Join(
                ",",
                bindings.Select(static binding => $"{binding.PropertyName}:{binding.Source}:{binding.Name ?? "(none)"}"));
    }

    private static string FormatList(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values.Count == 0
            ? "(none)"
            : string.Join(",", values);
    }
}
