using System.Reflection;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Http.Abstractions;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class RestBehaviorProjectionCandidateResolver
{
    internal static IReadOnlyList<ResolvedRestBehaviorEndpointProjectionCandidate> ResolveCandidates(
        ModuleDescriptor moduleDescriptor,
        ApiRoutesOptions apiRoutesOptions,
        IReadOnlyList<RestBehaviorRouteGroupProjection> groups,
        IReadOnlyList<RestEndpointSuppressionOptions>? suppressions = null,
        IReadOnlyList<RestEndpointOverrideOptions>? overrides = null)
    {
        ArgumentNullException.ThrowIfNull(moduleDescriptor);
        ArgumentNullException.ThrowIfNull(apiRoutesOptions);
        ArgumentNullException.ThrowIfNull(groups);

        var moduleVersionMajor = ResolveModuleMajorVersion(moduleDescriptor.Version);
        var candidates = groups
            .SelectMany((group, groupIndex) => BuildGroupCandidates(
                moduleDescriptor,
                moduleVersionMajor,
                apiRoutesOptions,
                group,
                groupIndex,
                overrides))
            .ToArray();

        if (candidates.Length == 0)
        {
            return [];
        }

        var matchedSuppressionsByCandidateId = candidates.ToDictionary(
            static candidate => candidate.Candidate.Id,
            candidate => ResolveMatchingSuppressions(candidate, suppressions),
            StringComparer.OrdinalIgnoreCase);
        var suppressionByCandidateId = matchedSuppressionsByCandidateId.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.FirstOrDefault(),
            StringComparer.OrdinalIgnoreCase);

        var winnersByBehavior = candidates
            .Where(candidate => suppressionByCandidateId[candidate.Candidate.Id] is null)
            .GroupBy(
                static candidate => candidate.Candidate.ProjectedEndpoint.BehaviorId ?? string.Empty,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group =>
                {
                    var winningRank = group.Min(static candidate => candidate.Candidate.PrecedenceRank);
                    return group
                        .Where(candidate => candidate.Candidate.PrecedenceRank == winningRank)
                        .OrderBy(static candidate => candidate.Candidate.Id, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                },
                StringComparer.OrdinalIgnoreCase);

        return candidates
            .Select(candidate => ResolvePublication(candidate, matchedSuppressionsByCandidateId, winnersByBehavior))
            .OrderBy(static candidate => candidate.Candidate.ProjectedEndpoint.RoutePattern, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.Candidate.ProjectedEndpoint.Method, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.Candidate.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ResolvedRestBehaviorEndpointProjectionCandidate[] BuildGroupCandidates(
        ModuleDescriptor moduleDescriptor,
        int? moduleVersionMajor,
        ApiRoutesOptions apiRoutesOptions,
        RestBehaviorRouteGroupProjection group,
        int groupIndex,
        IReadOnlyList<RestEndpointOverrideOptions>? overrides)
    {
        ArgumentNullException.ThrowIfNull(moduleDescriptor);
        ArgumentNullException.ThrowIfNull(apiRoutesOptions);
        ArgumentNullException.ThrowIfNull(group);

        if (group.Endpoints.Count == 0)
        {
            return [];
        }

        var tagName = string.IsNullOrWhiteSpace(group.TagName)
            ? moduleDescriptor.DisplayName
            : group.TagName.Trim();
        var defaultApiVersionMajor = group.ApiVersionMajor ?? moduleVersionMajor;
        var originalRouteGroupPrefix = RestEndpointRuntimeDescriptorFactory.CombinePaths(
            apiRoutesOptions.RestPrefix,
            ResolveRouteGroupPrefix(group.Prefix, defaultApiVersionMajor));

        return group.Endpoints
            .Select(endpoint => CreateCandidate(
                moduleDescriptor,
                groupIndex,
                endpoint,
                tagName,
                defaultApiVersionMajor,
                originalRouteGroupPrefix,
                apiRoutesOptions,
                group,
                overrides))
            .ToArray();
    }

    private static ResolvedRestBehaviorEndpointProjectionCandidate CreateCandidate(
        ModuleDescriptor moduleDescriptor,
        int groupIndex,
        RestBehaviorEndpointProjection endpointProjection,
        string tagName,
        int? defaultApiVersionMajor,
        string originalRouteGroupPrefix,
        ApiRoutesOptions apiRoutesOptions,
        RestBehaviorRouteGroupProjection group,
        IReadOnlyList<RestEndpointOverrideOptions>? overrides)
    {
        ArgumentNullException.ThrowIfNull(moduleDescriptor);
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
        ArgumentNullException.ThrowIfNull(apiRoutesOptions);
        ArgumentNullException.ThrowIfNull(group);

        var originalProjection = CreateCandidateProjectionDescriptor(
            defaultApiVersionMajor,
            endpointProjection.Method.ToString().ToUpperInvariant(),
            originalRouteGroupPrefix,
            endpointProjection.Pattern,
            RestEndpointBindingDescriptorAdapter.ToRuntimeDescriptors(endpointProjection.Bindings),
            endpointProjection.PreserveImplicitQueryFallback);
        var candidateId = BuildCandidateId(
            moduleDescriptor.Id,
            endpointProjection.BehaviorId,
            endpointProjection.AuthoringStyle,
            originalProjection.Method,
            originalProjection.RoutePattern);
        var overrideDecision = ResolveOverrideDecision(
            moduleDescriptor.Id,
            candidateId,
            endpointProjection,
            defaultApiVersionMajor,
            apiRoutesOptions.RestPrefix,
            originalRouteGroupPrefix,
            group,
            overrides);
        var selectedOverride = overrideDecision.SelectedOverride;
        var appliedOverride = overrideDecision.AppliedOverride;
        var effectiveEndpointProjection = appliedOverride?.EffectiveEndpointProjection ?? endpointProjection;
        var effectiveApiVersionMajor = appliedOverride?.EffectiveApiVersionMajor ?? defaultApiVersionMajor;
        var publishedRouteGroupPrefix = appliedOverride?.EffectiveRouteGroupPrefix ??
                                        RestEndpointRuntimeDescriptorFactory.CombinePaths(
                                            apiRoutesOptions.RestPrefix,
                                            ResolveRouteGroupPrefix(group.Prefix, effectiveApiVersionMajor));
        var openApiDocumentName = ResolveOpenApiDocumentName(effectiveApiVersionMajor);
        var method = effectiveEndpointProjection.Method.ToString().ToUpperInvariant();
        var routePattern = RestEndpointRuntimeDescriptorFactory.CombinePaths(
            publishedRouteGroupPrefix,
            effectiveEndpointProjection.Pattern);
        var runtimeBindings = RestEndpointBindingDescriptorAdapter.ToRuntimeDescriptors(effectiveEndpointProjection.Bindings);
        var originalOperationName = RestBehaviorEndpointMetadataConventions.BuildOperationName(
            moduleDescriptor.Id,
            defaultApiVersionMajor,
            endpointProjection.BehaviorId);
        var originalDocumentation = RestBehaviorEndpointMetadataConventions.ResolveOperationDocumentation(
            endpointProjection.BehaviorType,
            moduleDescriptor,
            endpointProjection.BehaviorId);
        var operationName = RestBehaviorEndpointMetadataConventions.BuildOperationName(
            moduleDescriptor.Id,
            effectiveApiVersionMajor ?? ResolveModuleMajorVersion(moduleDescriptor.Version),
            effectiveEndpointProjection.BehaviorId);
        var documentation = RestBehaviorEndpointMetadataConventions.ResolveOperationDocumentation(
            effectiveEndpointProjection.BehaviorType,
            moduleDescriptor,
            effectiveEndpointProjection.BehaviorId);
        var appliedMetadataOverride = CreateAppliedEndpointMetadataOverride(
            selectedOverride,
            operationName,
            documentation.Summary,
            documentation.Description);
        var appliedCapabilityOverride = CreateAppliedRequiredCapabilityOverride(selectedOverride);
        var endpointName = NormalizeOverrideMetadataValue(selectedOverride?.EndpointName) ?? operationName;
        var summary = NormalizeOverrideMetadataValue(selectedOverride?.Summary) ?? documentation.Summary;
        var description = NormalizeOverrideMetadataValue(selectedOverride?.Description) ?? documentation.Description;
        var projectedEndpoint = RestEndpointRuntimeDescriptorFactory.CreateBehaviorDescriptor(
            sourceKind: RestEndpointRuntimeMetadata.ModuleDslSourceKind,
            method: method,
            routePattern: routePattern,
            sourceModuleId: moduleDescriptor.Id,
            sourceModuleVersion: moduleDescriptor.Version,
            sourceModuleVersionMajor: ResolveModuleMajorVersion(moduleDescriptor.Version),
            behaviorId: effectiveEndpointProjection.BehaviorId,
            endpointName: endpointName,
            openApiDocumentName: openApiDocumentName,
            apiVersionMajor: effectiveApiVersionMajor,
            tags: [tagName],
            summary: summary,
            description: description,
            originalEndpointName: originalOperationName,
            originalSummary: originalDocumentation.Summary,
            originalDescription: originalDocumentation.Description,
            candidateId: candidateId,
            originalProjection: null,
            authoringStyle: effectiveEndpointProjection.AuthoringStyle,
            behaviorType: effectiveEndpointProjection.BehaviorType.FullName ?? effectiveEndpointProjection.BehaviorType.Name,
            routeGroupPrefix: publishedRouteGroupPrefix,
            relativePattern: effectiveEndpointProjection.Pattern,
            bindingDescriptors: runtimeBindings,
            preserveImplicitQueryFallback: effectiveEndpointProjection.PreserveImplicitQueryFallback,
            requiredCapabilityKey: appliedCapabilityOverride?.ClearRequiredCapability == true
                ? null
                : appliedCapabilityOverride?.RequiredCapabilityKey,
            matchedOverrideIds: overrideDecision.MatchedOverrideIds);
        var precedenceRank = RestEndpointRuntimeMetadata.ResolvePrecedenceRank(effectiveEndpointProjection.AuthoringStyle);

        return new ResolvedRestBehaviorEndpointProjectionCandidate(
            groupIndex,
            effectiveEndpointProjection,
            new RestEndpointCandidateRuntimeDescriptor(
                candidateId,
                projectedEndpoint,
                originalProjection,
                effectiveEndpointProjection.AuthoringStyle,
                precedenceRank,
                RestEndpointCandidateStatus.Published,
                appliedOverrideId: appliedOverride?.Id ?? appliedCapabilityOverride?.OverrideId ?? appliedMetadataOverride?.OverrideId,
                matchedOverrideIds: overrideDecision.MatchedOverrideIds),
            appliedCapabilityOverride,
            appliedMetadataOverride);
    }

    private static ResolvedRestBehaviorEndpointProjectionCandidate ResolvePublication(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        Dictionary<string, RestEndpointSuppressionOptions[]> matchedSuppressionsByCandidateId,
        Dictionary<string, ResolvedRestBehaviorEndpointProjectionCandidate[]> winnersByBehavior)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(matchedSuppressionsByCandidateId);
        ArgumentNullException.ThrowIfNull(winnersByBehavior);

        var matchedSuppressions = matchedSuppressionsByCandidateId[candidate.Candidate.Id];
        var suppression = matchedSuppressions.FirstOrDefault();
        if (suppression is not null)
        {
            return candidate with
            {
                Candidate = new RestEndpointCandidateRuntimeDescriptor(
                    candidate.Candidate.Id,
                    candidate.Candidate.ProjectedEndpoint,
                    candidate.Candidate.OriginalProjection,
                    candidate.Candidate.AuthoringStyle,
                    candidate.Candidate.PrecedenceRank,
                    RestEndpointCandidateStatus.Suppressed,
                    suppressedBySuppressionId: suppression.Id,
                    appliedOverrideId: candidate.Candidate.AppliedOverrideId,
                    matchedSuppressionIds: matchedSuppressions.Select(static item => item.Id).ToArray(),
                    matchedOverrideIds: candidate.Candidate.MatchedOverrideIds,
                    suppressionReason: $"Suppressed by REST endpoint suppression rule '{suppression.Id}'.")
            };
        }

        var behaviorId = candidate.Candidate.ProjectedEndpoint.BehaviorId ?? string.Empty;
        var winners = winnersByBehavior[behaviorId];
        if (winners.Any(winner => string.Equals(winner.Candidate.Id, candidate.Candidate.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return candidate;
        }

        var winningCandidate = winners[0].Candidate;
        return candidate with
        {
            Candidate = new RestEndpointCandidateRuntimeDescriptor(
                candidate.Candidate.Id,
                candidate.Candidate.ProjectedEndpoint,
                candidate.Candidate.OriginalProjection,
                candidate.Candidate.AuthoringStyle,
                candidate.Candidate.PrecedenceRank,
                RestEndpointCandidateStatus.Suppressed,
                suppressedByCandidateId: winningCandidate.Id,
                appliedOverrideId: candidate.Candidate.AppliedOverrideId,
                matchedOverrideIds: candidate.Candidate.MatchedOverrideIds,
                suppressionReason: $"Suppressed because behavior '{behaviorId}' is also mapped by higher-precedence authoring style '{winningCandidate.AuthoringStyle}'.")
        };
    }

    private static ResolvedRestEndpointOverrideDecision ResolveOverrideDecision(
        string sourceModuleId,
        string originalCandidateId,
        RestBehaviorEndpointProjection endpointProjection,
        int? defaultApiVersionMajor,
        string restPrefix,
        string originalRouteGroupPrefix,
        RestBehaviorRouteGroupProjection group,
        IReadOnlyList<RestEndpointOverrideOptions>? overrides)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalCandidateId);
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentNullException.ThrowIfNull(restPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalRouteGroupPrefix);
        ArgumentNullException.ThrowIfNull(group);

        if (overrides is null || overrides.Count == 0)
        {
            return new ResolvedRestEndpointOverrideDecision([], null, null);
        }

        var matchedOverrides = ResolveMatchingOverrides(
            sourceModuleId,
            originalCandidateId,
            endpointProjection,
            defaultApiVersionMajor,
            originalRouteGroupPrefix,
            overrides);
        var matchedOverride = matchedOverrides.FirstOrDefault();
        if (matchedOverride is null)
        {
            return new ResolvedRestEndpointOverrideDecision([], null, null);
        }

        var effectiveEndpointProjection = endpointProjection;
        var effectiveApiVersionMajor = defaultApiVersionMajor;
        string? effectiveRouteGroupPrefix = null;
        var wasApplied = false;
        var shouldRevalidateBindings = false;

        if (!string.IsNullOrWhiteSpace(matchedOverride.Pattern) &&
            !string.Equals(matchedOverride.Pattern, endpointProjection.Pattern, StringComparison.Ordinal))
        {
            effectiveEndpointProjection = effectiveEndpointProjection.WithPattern(matchedOverride.Pattern);
            wasApplied = true;
            shouldRevalidateBindings = true;
        }

        if (!string.IsNullOrWhiteSpace(matchedOverride.Method))
        {
            var overrideMethod = RestBehaviorHttpMethodParser.Parse(matchedOverride.Method);
            if (overrideMethod != effectiveEndpointProjection.Method)
            {
                effectiveEndpointProjection = effectiveEndpointProjection.WithMethod(overrideMethod);
                wasApplied = true;
                shouldRevalidateBindings = true;
            }
        }

        if (matchedOverride.Bindings.Count > 0 || matchedOverride.RemovedBindingProperties.Count > 0)
        {
            var overrideBindings = RestEndpointBindingDescriptorAdapter.ToBehaviorDescriptors(matchedOverride.Bindings);
            IReadOnlyList<BehaviorRestBindingDescriptor> effectiveBindings = matchedOverride.BindingMode == RestEndpointOverrideBindingMode.MergeExplicit
                ? MergeBindings(
                    matchedOverride.Id,
                    effectiveEndpointProjection.Bindings,
                    overrideBindings,
                    matchedOverride.RemovedBindingProperties)
                : overrideBindings;
            if (!effectiveEndpointProjection.Bindings.SequenceEqual(effectiveBindings))
            {
                effectiveEndpointProjection = effectiveEndpointProjection.WithBindings(effectiveBindings);
                wasApplied = true;
            }

            shouldRevalidateBindings = true;
        }

        if (shouldRevalidateBindings)
        {
            var normalizedBindings = BehaviorRestBindingPlanNormalizer.Normalize(
                $"REST endpoint override rule '{matchedOverride.Id}' for behavior '{endpointProjection.BehaviorId}'",
                effectiveEndpointProjection.BehaviorType,
                effectiveEndpointProjection.Method,
                effectiveEndpointProjection.Pattern,
                effectiveEndpointProjection.Bindings);
            effectiveEndpointProjection = effectiveEndpointProjection.WithBindings(normalizedBindings);
        }

        effectiveEndpointProjection = effectiveEndpointProjection.WithPreserveImplicitQueryFallback(
            endpointProjection.Bindings.Count == 0 &&
            effectiveEndpointProjection.Bindings.Count > 0);

        if (!string.Equals(effectiveEndpointProjection.Pattern, endpointProjection.Pattern, StringComparison.Ordinal))
        {
            ValidatePatternOverride(
                matchedOverride.Id,
                endpointProjection,
                effectiveEndpointProjection.Pattern,
                effectiveEndpointProjection.Bindings);
        }

        if (!group.HasExplicitApiVersion &&
            matchedOverride.ApiVersionMajor is int overrideApiVersionMajor &&
            overrideApiVersionMajor != defaultApiVersionMajor)
        {
            effectiveApiVersionMajor = overrideApiVersionMajor;
            wasApplied = true;
        }

        if (!string.IsNullOrWhiteSpace(matchedOverride.RouteGroupPrefix))
        {
            ValidateRouteGroupPrefixOverride(
                matchedOverride.Id,
                endpointProjection.BehaviorId,
                restPrefix,
                matchedOverride.RouteGroupPrefix,
                effectiveApiVersionMajor);

            if (!string.Equals(matchedOverride.RouteGroupPrefix, originalRouteGroupPrefix, StringComparison.Ordinal))
            {
                effectiveRouteGroupPrefix = matchedOverride.RouteGroupPrefix;
                wasApplied = true;
            }
        }

        return new ResolvedRestEndpointOverrideDecision(
            matchedOverrides.Select(static overrideOptions => overrideOptions.Id).ToArray(),
            matchedOverride,
            wasApplied
                ? new AppliedRestEndpointOverride(
                    matchedOverride.Id,
                    effectiveEndpointProjection,
                    effectiveApiVersionMajor,
                    effectiveRouteGroupPrefix)
                : null);
    }

    private static RestEndpointOverrideOptions[] ResolveMatchingOverrides(
        string sourceModuleId,
        string originalCandidateId,
        RestBehaviorEndpointProjection endpointProjection,
        int? defaultApiVersionMajor,
        string originalRouteGroupPrefix,
        IReadOnlyList<RestEndpointOverrideOptions>? overrides)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalCandidateId);
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalRouteGroupPrefix);

        if (overrides is null || overrides.Count == 0)
        {
            return [];
        }

        return overrides
            .Where(overrideOptions => MatchesOverride(
                sourceModuleId,
                originalCandidateId,
                endpointProjection,
                defaultApiVersionMajor,
                originalRouteGroupPrefix,
                overrideOptions))
            .OrderByDescending(static overrideOptions => overrideOptions.CandidateIds.Count > 0)
            .ThenBy(static overrideOptions => overrideOptions.CandidateIds.Count == 0 ? int.MaxValue : overrideOptions.CandidateIds.Count)
            .ThenByDescending(static overrideOptions => CountTargetDimensions(overrideOptions))
            .ThenByDescending(static overrideOptions => overrideOptions.BehaviorIds.Count > 0)
            .ThenBy(static overrideOptions => overrideOptions.AuthoringStyles.Count)
            .ThenBy(static overrideOptions => CountTargetValues(overrideOptions))
            .ThenBy(static overrideOptions => overrideOptions.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void ValidateRouteGroupPrefixOverride(
        string overrideId,
        string behaviorId,
        string restPrefix,
        string overrideRouteGroupPrefix,
        int? effectiveApiVersionMajor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(overrideId);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentNullException.ThrowIfNull(restPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(overrideRouteGroupPrefix);

        if (ExtractRoutePlaceholders(overrideRouteGroupPrefix).Count > 0)
        {
            throw new InvalidOperationException(
                $"REST endpoint override rule '{overrideId}' cannot rewrite behavior '{behaviorId}' to route-group prefix '{overrideRouteGroupPrefix}' because shorthand RouteGroupPrefix overrides cannot declare route placeholders. Keep placeholder changes in the relative Pattern plus explicit Bindings instead.");
        }

        var restSegments = SplitPathSegments(restPrefix);
        var overrideSegments = SplitPathSegments(overrideRouteGroupPrefix);
        if (overrideSegments.Length < restSegments.Length ||
            !overrideSegments.Take(restSegments.Length)
                .SequenceEqual(restSegments, StringComparer.OrdinalIgnoreCase))
        {
            var displayRestPrefix = string.IsNullOrWhiteSpace(restPrefix) ? "/" : restPrefix;
            throw new InvalidOperationException(
                $"REST endpoint override rule '{overrideId}' cannot rewrite behavior '{behaviorId}' to route-group prefix '{overrideRouteGroupPrefix}' because shorthand RouteGroupPrefix overrides must stay beneath the active REST root prefix '{displayRestPrefix}'.");
        }

        var remainingSegments = overrideSegments.Skip(restSegments.Length).ToArray();
        if (effectiveApiVersionMajor.HasValue)
        {
            var expectedVersionSegment = $"v{effectiveApiVersionMajor.Value}";
            if (remainingSegments.Length == 0 ||
                !string.Equals(remainingSegments[0], expectedVersionSegment, StringComparison.OrdinalIgnoreCase))
            {
                var expectedRouteGroupPrefix = RestEndpointRuntimeDescriptorFactory.CombinePaths(
                    restPrefix,
                    $"/{expectedVersionSegment}");
                throw new InvalidOperationException(
                    $"REST endpoint override rule '{overrideId}' cannot rewrite behavior '{behaviorId}' to route-group prefix '{overrideRouteGroupPrefix}' because the effective API major version is '{expectedVersionSegment}'. Keep RouteGroupPrefix beneath '{expectedRouteGroupPrefix}' or change ApiVersionMajor explicitly.");
            }

            return;
        }

        if (remainingSegments.Length > 0 && IsApiVersionSegment(remainingSegments[0]))
        {
            throw new InvalidOperationException(
                $"REST endpoint override rule '{overrideId}' cannot rewrite behavior '{behaviorId}' to route-group prefix '{overrideRouteGroupPrefix}' because RouteGroupPrefix cannot implicitly introduce a version segment when the shorthand candidate does not already resolve one. Set ApiVersionMajor explicitly or keep the prefix versionless.");
        }
    }

    private static void ValidatePatternOverride(
        string overrideId,
        RestBehaviorEndpointProjection endpointProjection,
        string overridePattern,
        IReadOnlyList<BehaviorRestBindingDescriptor> effectiveBindings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(overrideId);
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentException.ThrowIfNullOrWhiteSpace(overridePattern);
        ArgumentNullException.ThrowIfNull(effectiveBindings);

        var originalPlaceholders = ExtractRoutePlaceholders(endpointProjection.Pattern);
        var overridePlaceholders = ExtractRoutePlaceholders(overridePattern);
        if (originalPlaceholders.SetEquals(overridePlaceholders))
        {
            return;
        }

        var originalRouteBindings = endpointProjection.Bindings
            .Where(static binding => binding.Source == BehaviorRestBindingSource.Route)
            .ToArray();
        var effectiveRouteBindings = effectiveBindings
            .Where(static binding => binding.Source == BehaviorRestBindingSource.Route)
            .ToArray();
        var effectiveRouteBindingPlaceholders = effectiveRouteBindings
            .Select(static binding => string.IsNullOrWhiteSpace(binding.Name)
                ? binding.PropertyName
                : binding.Name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (originalPlaceholders.Count == overridePlaceholders.Count)
        {
            if (effectiveRouteBindingPlaceholders.SetEquals(overridePlaceholders))
            {
                return;
            }

            throw new InvalidOperationException(
                $"REST endpoint override rule '{overrideId}' cannot rewrite behavior '{endpointProjection.BehaviorId}' from pattern '{endpointProjection.Pattern}' to '{overridePattern}' because renamed placeholders require an effective explicit route-binding plan that covers the full renamed placeholder set. Add matching route bindings through the effective profile or RestApi:Overrides:*:Bindings, or keep the same placeholder names.");
        }

        if (overridePlaceholders.Count > originalPlaceholders.Count)
        {
            if (!effectiveRouteBindingPlaceholders.SetEquals(overridePlaceholders))
            {
                throw new InvalidOperationException(
                    $"REST endpoint override rule '{overrideId}' cannot rewrite behavior '{endpointProjection.BehaviorId}' from pattern '{endpointProjection.Pattern}' to '{overridePattern}' because adding placeholders requires an effective explicit route-binding plan that covers the full final placeholder set. Add matching route bindings through the effective profile or RestApi:Overrides:*:Bindings.");
            }

            var originalExplicitRouteBoundProperties = originalRouteBindings
                .Select(static binding => binding.PropertyName.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var effectiveRouteBoundProperties = effectiveRouteBindings
                .Select(static binding => binding.PropertyName.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var newlyRouteBoundProperties = effectiveRouteBoundProperties
                .Except(originalExplicitRouteBoundProperties, StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var originalExplicitlyBoundProperties = endpointProjection.Bindings
                .Select(static binding => binding.PropertyName.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var originalImplicitFallbackEligibleProperties = ResolveImplicitFallbackEligibleProperties(
                endpointProjection,
                originalExplicitlyBoundProperties,
                originalPlaceholders);
            var originallyPromotableProperties = new HashSet<string>(
                originalExplicitlyBoundProperties,
                StringComparer.OrdinalIgnoreCase);
            originallyPromotableProperties.UnionWith(originalImplicitFallbackEligibleProperties);
            if (!originallyPromotableProperties.IsSupersetOf(newlyRouteBoundProperties))
            {
                throw new InvalidOperationException(
                    $"REST endpoint override rule '{overrideId}' cannot rewrite behavior '{endpointProjection.BehaviorId}' from pattern '{endpointProjection.Pattern}' to '{overridePattern}' because adding placeholders requires every newly route-bound property to already be explicitly bound in the original projection or be eligible for the source shorthand candidate's deterministic implicit fallback surface. Promote only properties that the source profile already binds explicitly or could already read through that fallback surface before adding the placeholder.");
            }

            return;
        }

        var originalRouteBindingPlaceholders = originalRouteBindings
            .Select(static binding => string.IsNullOrWhiteSpace(binding.Name)
                ? binding.PropertyName
                : binding.Name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!originalRouteBindingPlaceholders.SetEquals(originalPlaceholders))
        {
            throw new InvalidOperationException(
                $"REST endpoint override rule '{overrideId}' cannot rewrite behavior '{endpointProjection.BehaviorId}' from pattern '{endpointProjection.Pattern}' to '{overridePattern}' because removing placeholders requires the original projection to already expose an explicit route-binding plan that covers the full original placeholder set. Add matching route bindings to the source profile before removing placeholders.");
        }

        if (!effectiveRouteBindingPlaceholders.SetEquals(overridePlaceholders))
        {
            throw new InvalidOperationException(
                $"REST endpoint override rule '{overrideId}' cannot rewrite behavior '{endpointProjection.BehaviorId}' from pattern '{endpointProjection.Pattern}' to '{overridePattern}' because removing placeholders requires an effective explicit route-binding plan that covers the full remaining placeholder set. Add matching route bindings through the effective profile or RestApi:Overrides:*:Bindings.");
        }

        var originalRouteBoundProperties = originalRouteBindings
            .Select(static binding => binding.PropertyName.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var effectiveExplicitlyBoundProperties = effectiveBindings
            .Select(static binding => binding.PropertyName.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!effectiveExplicitlyBoundProperties.IsSupersetOf(originalRouteBoundProperties))
        {
            throw new InvalidOperationException(
                $"REST endpoint override rule '{overrideId}' cannot rewrite behavior '{endpointProjection.BehaviorId}' from pattern '{endpointProjection.Pattern}' to '{overridePattern}' because removing placeholders requires every affected original route-bound property to remain explicitly bound in the effective plan. Rebind those properties through route, query, header, or body before removing the placeholder.");
        }
    }

    private static HashSet<string> ResolveImplicitFallbackEligibleProperties(
        RestBehaviorEndpointProjection endpointProjection,
        HashSet<string> originalExplicitlyBoundProperties,
        HashSet<string> originalPlaceholders)
    {
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentNullException.ThrowIfNull(originalExplicitlyBoundProperties);
        ArgumentNullException.ThrowIfNull(originalPlaceholders);

        var inputProperties = ResolveBehaviorInputProperties(endpointProjection.BehaviorType);
        if (inputProperties.Count == 0)
        {
            return [];
        }

        if (endpointProjection.Bindings.Count == 0)
        {
            return inputProperties
                .Where(propertyName => !originalPlaceholders.Contains(propertyName))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        if (endpointProjection.Method is not (RestBehaviorHttpMethod.Post or RestBehaviorHttpMethod.Put or RestBehaviorHttpMethod.Patch))
        {
            return [];
        }

        return inputProperties
            .Where(propertyName => !originalExplicitlyBoundProperties.Contains(propertyName))
            .Where(propertyName => !originalPlaceholders.Contains(propertyName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static List<BehaviorRestBindingDescriptor> MergeBindings(
        string overrideId,
        IReadOnlyList<BehaviorRestBindingDescriptor> originalBindings,
        IReadOnlyList<BehaviorRestBindingDescriptor> overrideBindings,
        IReadOnlyList<string> removedBindingProperties)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(overrideId);
        ArgumentNullException.ThrowIfNull(originalBindings);
        ArgumentNullException.ThrowIfNull(overrideBindings);
        ArgumentNullException.ThrowIfNull(removedBindingProperties);

        var merged = originalBindings
            .Where(static binding => binding is not null)
            .Select(static binding => new BehaviorRestBindingDescriptor(
                binding.PropertyName,
                binding.Source,
                binding.Name))
            .ToList();
        var indexesByProperty = merged
            .Select((binding, index) => new KeyValuePair<string, int>(binding.PropertyName.Trim(), index))
            .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var removalSet = removedBindingProperties
            .Where(static propertyName => !string.IsNullOrWhiteSpace(propertyName))
            .Select(static propertyName => propertyName.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seenOverrideProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var propertyName in removedBindingProperties)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                continue;
            }

            var normalizedPropertyName = propertyName.Trim();
            if (!indexesByProperty.ContainsKey(normalizedPropertyName))
            {
                throw new InvalidOperationException(
                    $"REST endpoint override rule '{overrideId}' cannot remove explicit binding property '{normalizedPropertyName}' because the source shorthand candidate does not explicitly bind that property.");
            }
        }

        foreach (var binding in overrideBindings)
        {
            if (binding is null)
            {
                continue;
            }

            var propertyName = binding.PropertyName.Trim();
            if (!seenOverrideProperties.Add(propertyName))
            {
                throw new InvalidOperationException(
                    $"REST endpoint override rule '{overrideId}' cannot merge more than one explicit binding override for property '{propertyName}'.");
            }

            if (removalSet.Contains(propertyName))
            {
                throw new InvalidOperationException(
                    $"REST endpoint override rule '{overrideId}' cannot both remove and override explicit binding property '{propertyName}' in the same merge rule.");
            }
        }

        if (removalSet.Count > 0)
        {
            merged = merged
                .Where(binding => !removalSet.Contains(binding.PropertyName.Trim()))
                .ToList();
            indexesByProperty = merged
                .Select((binding, index) => new KeyValuePair<string, int>(binding.PropertyName.Trim(), index))
                .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        foreach (var binding in overrideBindings)
        {
            if (binding is null)
            {
                continue;
            }

            var propertyName = binding.PropertyName.Trim();
            var clonedBinding = new BehaviorRestBindingDescriptor(
                binding.PropertyName,
                binding.Source,
                binding.Name);
            if (indexesByProperty.TryGetValue(propertyName, out var index))
            {
                merged[index] = clonedBinding;
            }
            else
            {
                indexesByProperty[propertyName] = merged.Count;
                merged.Add(clonedBinding);
            }
        }

        return merged;
    }

    private static HashSet<string> ResolveBehaviorInputProperties(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        var contractInterface = behaviorType.GetInterfaces()
            .FirstOrDefault(static candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == typeof(IAppBehavior<,>));
        if (contractInterface is null)
        {
            return [];
        }

        var inputType = Nullable.GetUnderlyingType(contractInterface.GetGenericArguments()[0]) ??
                        contractInterface.GetGenericArguments()[0];
        if (IsSimpleInputType(inputType))
        {
            return [];
        }

        return inputType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.CanRead)
            .Select(static property => property.Name.Trim())
            .Where(static propertyName => !string.IsNullOrWhiteSpace(propertyName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSimpleInputType(Type inputType)
    {
        ArgumentNullException.ThrowIfNull(inputType);

        var type = Nullable.GetUnderlyingType(inputType) ?? inputType;
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(Guid) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(DateOnly) ||
               type == typeof(TimeOnly);
    }

    private static RestEndpointSuppressionOptions[] ResolveMatchingSuppressions(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        IReadOnlyList<RestEndpointSuppressionOptions>? suppressions)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (suppressions is null || suppressions.Count == 0)
        {
            return [];
        }

        return suppressions
            .Where(suppression => MatchesSuppression(candidate, suppression))
            .OrderByDescending(static suppression => suppression.CandidateIds.Count > 0)
            .ThenBy(static suppression => suppression.CandidateIds.Count == 0 ? int.MaxValue : suppression.CandidateIds.Count)
            .ThenByDescending(static suppression => CountTargetDimensions(suppression))
            .ThenByDescending(static suppression => suppression.BehaviorIds.Count > 0)
            .ThenBy(static suppression => suppression.AuthoringStyles.Count)
            .ThenBy(static suppression => CountTargetValues(suppression))
            .ThenBy(static suppression => suppression.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool MatchesSuppression(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        RestEndpointSuppressionOptions suppression)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(suppression);

        if (suppression.CandidateIds.Count > 0 &&
            !suppression.CandidateIds.Contains(candidate.Candidate.Id, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!suppression.AuthoringStyles.Contains(candidate.Candidate.AuthoringStyle, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (suppression.SourceModuleIds.Count > 0)
        {
            var sourceModuleId = candidate.Candidate.ProjectedEndpoint.SourceModuleId;
            if (string.IsNullOrWhiteSpace(sourceModuleId) ||
                !suppression.SourceModuleIds.Contains(sourceModuleId, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (suppression.BehaviorIds.Count > 0)
        {
            var behaviorId = candidate.Candidate.ProjectedEndpoint.BehaviorId;
            if (string.IsNullOrWhiteSpace(behaviorId) ||
                !suppression.BehaviorIds.Contains(behaviorId, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (suppression.ApiVersionMajors.Count > 0)
        {
            var apiVersionMajor = candidate.Candidate.OriginalProjection.ApiVersionMajor;
            if (!apiVersionMajor.HasValue || !suppression.ApiVersionMajors.Contains(apiVersionMajor.Value))
            {
                return false;
            }
        }

        if (suppression.Methods.Count > 0 &&
            !suppression.Methods.Contains(candidate.Candidate.OriginalProjection.Method, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (suppression.RelativePatterns.Count > 0)
        {
            var relativePattern = candidate.Candidate.OriginalProjection.RelativePattern;
            if (string.IsNullOrWhiteSpace(relativePattern) ||
                !suppression.RelativePatterns.Contains(relativePattern, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (suppression.RouteGroupPrefixes.Count > 0)
        {
            var routeGroupPrefix = candidate.Candidate.OriginalProjection.RouteGroupPrefix;
            if (string.IsNullOrWhiteSpace(routeGroupPrefix) ||
                !suppression.RouteGroupPrefixes.Contains(routeGroupPrefix, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesOverride(
        string sourceModuleId,
        string originalCandidateId,
        RestBehaviorEndpointProjection endpointProjection,
        int? defaultApiVersionMajor,
        string originalRouteGroupPrefix,
        RestEndpointOverrideOptions overrideOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalCandidateId);
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalRouteGroupPrefix);
        ArgumentNullException.ThrowIfNull(overrideOptions);

        if (overrideOptions.CandidateIds.Count > 0 &&
            !overrideOptions.CandidateIds.Contains(originalCandidateId, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!overrideOptions.AuthoringStyles.Contains(endpointProjection.AuthoringStyle, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (overrideOptions.SourceModuleIds.Count > 0 &&
            !overrideOptions.SourceModuleIds.Contains(sourceModuleId, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (overrideOptions.BehaviorIds.Count > 0 &&
            !overrideOptions.BehaviorIds.Contains(endpointProjection.BehaviorId, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (overrideOptions.ApiVersionMajors.Count > 0 &&
            (!defaultApiVersionMajor.HasValue || !overrideOptions.ApiVersionMajors.Contains(defaultApiVersionMajor.Value)))
        {
            return false;
        }

        var originalMethod = endpointProjection.Method.ToString().ToUpperInvariant();
        if (overrideOptions.Methods.Count > 0 &&
            !overrideOptions.Methods.Contains(originalMethod, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (overrideOptions.RelativePatterns.Count > 0 &&
            !overrideOptions.RelativePatterns.Contains(endpointProjection.Pattern, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (overrideOptions.RouteGroupPrefixes.Count > 0 &&
            !overrideOptions.RouteGroupPrefixes.Contains(originalRouteGroupPrefix, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static int CountTargetDimensions(RestEndpointSuppressionOptions suppression)
    {
        ArgumentNullException.ThrowIfNull(suppression);

        var count = 0;
        if (suppression.CandidateIds.Count > 0)
        {
            count++;
        }

        if (suppression.BehaviorIds.Count > 0)
        {
            count++;
        }

        if (suppression.SourceModuleIds.Count > 0)
        {
            count++;
        }

        if (suppression.ApiVersionMajors.Count > 0)
        {
            count++;
        }

        if (suppression.Methods.Count > 0)
        {
            count++;
        }

        if (suppression.RelativePatterns.Count > 0)
        {
            count++;
        }

        if (suppression.RouteGroupPrefixes.Count > 0)
        {
            count++;
        }

        return count;
    }

    private static int CountTargetDimensions(RestEndpointOverrideOptions overrideOptions)
    {
        ArgumentNullException.ThrowIfNull(overrideOptions);

        var count = 0;
        if (overrideOptions.CandidateIds.Count > 0)
        {
            count++;
        }

        if (overrideOptions.BehaviorIds.Count > 0)
        {
            count++;
        }

        if (overrideOptions.SourceModuleIds.Count > 0)
        {
            count++;
        }

        if (overrideOptions.ApiVersionMajors.Count > 0)
        {
            count++;
        }

        if (overrideOptions.Methods.Count > 0)
        {
            count++;
        }

        if (overrideOptions.RelativePatterns.Count > 0)
        {
            count++;
        }

        if (overrideOptions.RouteGroupPrefixes.Count > 0)
        {
            count++;
        }

        return count;
    }

    private static AppliedRestEndpointMetadataOverride? CreateAppliedEndpointMetadataOverride(
        RestEndpointOverrideOptions? selectedOverride,
        string? defaultEndpointName,
        string? defaultSummary,
        string? defaultDescription)
    {
        if (selectedOverride is null)
        {
            return null;
        }

        var endpointName = NormalizeOverrideMetadataValue(selectedOverride.EndpointName);
        var summary = NormalizeOverrideMetadataValue(selectedOverride.Summary);
        var description = NormalizeOverrideMetadataValue(selectedOverride.Description);
        if (endpointName is null && summary is null && description is null)
        {
            return null;
        }

        var endpointNameChanged = endpointName is not null &&
                                  !string.Equals(endpointName, defaultEndpointName, StringComparison.Ordinal);
        var summaryChanged = summary is not null &&
                             !string.Equals(summary, defaultSummary, StringComparison.Ordinal);
        var descriptionChanged = description is not null &&
                                 !string.Equals(description, defaultDescription, StringComparison.Ordinal);
        if (!endpointNameChanged && !summaryChanged && !descriptionChanged)
        {
            return null;
        }

        return new AppliedRestEndpointMetadataOverride(
            selectedOverride.Id,
            endpointNameChanged ? endpointName : null,
            summaryChanged ? summary : null,
            descriptionChanged ? description : null);
    }

    private static AppliedRestEndpointCapabilityOverride? CreateAppliedRequiredCapabilityOverride(
        RestEndpointOverrideOptions? selectedOverride)
    {
        if (selectedOverride is null)
        {
            return null;
        }

        var requiredCapabilityKey = NormalizeOverrideMetadataValue(selectedOverride.RequiredCapabilityKey);
        if (requiredCapabilityKey is null && !selectedOverride.ClearRequiredCapability)
        {
            return null;
        }

        return new AppliedRestEndpointCapabilityOverride(
            selectedOverride.Id,
            requiredCapabilityKey,
            selectedOverride.ClearRequiredCapability);
    }

    private static int CountTargetValues(RestEndpointSuppressionOptions suppression)
    {
        ArgumentNullException.ThrowIfNull(suppression);

        return suppression.CandidateIds.Count +
               suppression.BehaviorIds.Count +
               suppression.SourceModuleIds.Count +
               suppression.ApiVersionMajors.Count +
               suppression.Methods.Count +
               suppression.RelativePatterns.Count +
               suppression.RouteGroupPrefixes.Count;
    }

    private static int CountTargetValues(RestEndpointOverrideOptions overrideOptions)
    {
        ArgumentNullException.ThrowIfNull(overrideOptions);

        return overrideOptions.CandidateIds.Count +
               overrideOptions.BehaviorIds.Count +
               overrideOptions.SourceModuleIds.Count +
               overrideOptions.ApiVersionMajors.Count +
               overrideOptions.Methods.Count +
               overrideOptions.RelativePatterns.Count +
               overrideOptions.RouteGroupPrefixes.Count;
    }

    private static string? NormalizeOverrideMetadataValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static HashSet<string> ExtractRoutePlaceholders(string pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        try
        {
            return RoutePatternFactory.Parse(pattern)
                .Parameters
                .Select(static parameter => parameter.Name)
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"REST behavior route pattern '{pattern}' is not a valid ASP.NET Core route pattern.",
                ex);
        }
    }

    private static string[] SplitPathSegments(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? []
            : path.Trim()
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool IsApiVersionSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment) || segment.Length <= 1 || segment[0] is not ('v' or 'V'))
        {
            return false;
        }

        return int.TryParse(segment[1..], out var parsedMajor) && parsedMajor > 0;
    }

    private static int? ResolveModuleMajorVersion(string? moduleVersion)
    {
        return Version.TryParse(moduleVersion, out var parsedVersion)
            ? parsedVersion.Major
            : null;
    }

    private static string ResolveRouteGroupPrefix(string prefix, int? apiVersionMajor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var normalizedPrefix = prefix.Trim();
        if (!apiVersionMajor.HasValue)
        {
            return normalizedPrefix;
        }

        var versionPrefix = $"/v{apiVersionMajor.Value}";
        if (normalizedPrefix.Equals(versionPrefix, StringComparison.OrdinalIgnoreCase) ||
            normalizedPrefix.StartsWith($"{versionPrefix}/", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPrefix;
        }

        return $"{versionPrefix}{normalizedPrefix}";
    }

    private static string ResolveOpenApiDocumentName(int? apiVersionMajor)
    {
        return apiVersionMajor.HasValue
            ? $"v{apiVersionMajor.Value}"
            : "v1";
    }

    private static string BuildCandidateId(
        string sourceModuleId,
        string behaviorId,
        string authoringStyle,
        string method,
        string routePattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(authoringStyle);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(routePattern);

        return RestEndpointRuntimeDescriptorFactory.BuildEndpointId(
            $"{sourceModuleId}:{behaviorId}:{authoringStyle}:{method}:{routePattern}");
    }

    private static RestEndpointCandidateProjectionDescriptor CreateCandidateProjectionDescriptor(
        int? apiVersionMajor,
        string method,
        string routeGroupPrefix,
        string relativePattern,
        IReadOnlyList<RestEndpointBindingDescriptor> bindingDescriptors,
        bool preserveImplicitQueryFallback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(routeGroupPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePattern);
        ArgumentNullException.ThrowIfNull(bindingDescriptors);

        return new RestEndpointCandidateProjectionDescriptor(
            method,
            RestEndpointRuntimeDescriptorFactory.CombinePaths(routeGroupPrefix, relativePattern),
            routeGroupPrefix,
            relativePattern,
            apiVersionMajor,
            ResolveOpenApiDocumentName(apiVersionMajor),
            bindingDescriptors,
            preserveImplicitQueryFallback
                ? RestEndpointBindingFallbackMode.PreserveSourceImplicitFallback
                : null);
    }
}

internal sealed record ResolvedRestBehaviorEndpointProjectionCandidate(
    int GroupIndex,
    RestBehaviorEndpointProjection EffectiveEndpointProjection,
    RestEndpointCandidateRuntimeDescriptor Candidate,
    AppliedRestEndpointCapabilityOverride? AppliedCapabilityOverride = null,
    AppliedRestEndpointMetadataOverride? AppliedMetadataOverride = null);

internal sealed record AppliedRestEndpointOverride(
    string Id,
    RestBehaviorEndpointProjection EffectiveEndpointProjection,
    int? EffectiveApiVersionMajor,
    string? EffectiveRouteGroupPrefix);

internal sealed record AppliedRestEndpointMetadataOverride(
    string OverrideId,
    string? EndpointName,
    string? Summary,
    string? Description);

internal sealed record AppliedRestEndpointCapabilityOverride(
    string OverrideId,
    string? RequiredCapabilityKey,
    bool ClearRequiredCapability);

internal sealed record ResolvedRestEndpointOverrideDecision(
    IReadOnlyList<string> MatchedOverrideIds,
    RestEndpointOverrideOptions? SelectedOverride,
    AppliedRestEndpointOverride? AppliedOverride);
