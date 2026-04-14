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

        var suppressionByCandidateId = candidates.ToDictionary(
            static candidate => candidate.Candidate.Id,
            candidate => ResolveSuppression(candidate.Candidate, suppressions),
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
            .Select(candidate => ResolvePublication(candidate, suppressionByCandidateId, winnersByBehavior))
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

        return group.Endpoints
            .Select(endpoint => CreateCandidate(
                moduleDescriptor,
                groupIndex,
                endpoint,
                tagName,
                defaultApiVersionMajor,
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
        ApiRoutesOptions apiRoutesOptions,
        RestBehaviorRouteGroupProjection group,
        IReadOnlyList<RestEndpointOverrideOptions>? overrides)
    {
        ArgumentNullException.ThrowIfNull(moduleDescriptor);
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
        ArgumentNullException.ThrowIfNull(apiRoutesOptions);
        ArgumentNullException.ThrowIfNull(group);

        var appliedOverride = ResolveOverride(
            moduleDescriptor.Id,
            endpointProjection,
            defaultApiVersionMajor,
            group,
            overrides);
        var effectiveEndpointProjection = appliedOverride?.EffectiveEndpointProjection ?? endpointProjection;
        var effectiveApiVersionMajor = appliedOverride?.EffectiveApiVersionMajor ?? defaultApiVersionMajor;
        var resolvedRouteGroupPrefix = ResolveRouteGroupPrefix(group.Prefix, effectiveApiVersionMajor);
        var publishedRouteGroupPrefix = RestEndpointRuntimeDescriptorFactory.CombinePaths(
            apiRoutesOptions.RestPrefix,
            resolvedRouteGroupPrefix);
        var openApiDocumentName = ResolveOpenApiDocumentName(effectiveApiVersionMajor);
        var method = effectiveEndpointProjection.Method.ToString().ToUpperInvariant();
        var routePattern = RestEndpointRuntimeDescriptorFactory.CombinePaths(
            publishedRouteGroupPrefix,
            effectiveEndpointProjection.Pattern);
        var runtimeBindings = RestEndpointBindingDescriptorAdapter.ToRuntimeDescriptors(effectiveEndpointProjection.Bindings);
        var projectedEndpoint = RestEndpointRuntimeDescriptorFactory.CreateBehaviorDescriptor(
            sourceKind: RestEndpointRuntimeMetadata.ModuleDslSourceKind,
            method: method,
            routePattern: routePattern,
            sourceModuleId: moduleDescriptor.Id,
            sourceModuleVersion: moduleDescriptor.Version,
            sourceModuleVersionMajor: ResolveModuleMajorVersion(moduleDescriptor.Version),
            behaviorId: effectiveEndpointProjection.BehaviorId,
            endpointName: null,
            openApiDocumentName: openApiDocumentName,
            apiVersionMajor: effectiveApiVersionMajor,
            tags: [tagName],
            summary: null,
            description: null,
            authoringStyle: effectiveEndpointProjection.AuthoringStyle,
            behaviorType: effectiveEndpointProjection.BehaviorType.FullName ?? effectiveEndpointProjection.BehaviorType.Name,
            routeGroupPrefix: publishedRouteGroupPrefix,
            relativePattern: effectiveEndpointProjection.Pattern,
            bindingDescriptors: runtimeBindings);
        var candidateId = RestEndpointRuntimeDescriptorFactory.BuildEndpointId(
            $"{moduleDescriptor.Id}:{effectiveEndpointProjection.BehaviorId}:{effectiveEndpointProjection.AuthoringStyle}:{method}:{routePattern}");
        var precedenceRank = RestEndpointRuntimeMetadata.ResolvePrecedenceRank(effectiveEndpointProjection.AuthoringStyle);

        return new ResolvedRestBehaviorEndpointProjectionCandidate(
            groupIndex,
            effectiveEndpointProjection,
            new RestEndpointCandidateRuntimeDescriptor(
                candidateId,
                projectedEndpoint,
                effectiveEndpointProjection.AuthoringStyle,
                precedenceRank,
                RestEndpointCandidateStatus.Published,
                appliedOverrideId: appliedOverride?.Id));
    }

    private static ResolvedRestBehaviorEndpointProjectionCandidate ResolvePublication(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        Dictionary<string, RestEndpointSuppressionOptions?> suppressionByCandidateId,
        Dictionary<string, ResolvedRestBehaviorEndpointProjectionCandidate[]> winnersByBehavior)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(suppressionByCandidateId);
        ArgumentNullException.ThrowIfNull(winnersByBehavior);

        var suppression = suppressionByCandidateId[candidate.Candidate.Id];
        if (suppression is not null)
        {
            return candidate with
            {
                Candidate = new RestEndpointCandidateRuntimeDescriptor(
                    candidate.Candidate.Id,
                    candidate.Candidate.ProjectedEndpoint,
                    candidate.Candidate.AuthoringStyle,
                    candidate.Candidate.PrecedenceRank,
                    RestEndpointCandidateStatus.Suppressed,
                    suppressedBySuppressionId: suppression.Id,
                    appliedOverrideId: candidate.Candidate.AppliedOverrideId,
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
                candidate.Candidate.AuthoringStyle,
                candidate.Candidate.PrecedenceRank,
                RestEndpointCandidateStatus.Suppressed,
                suppressedByCandidateId: winningCandidate.Id,
                appliedOverrideId: candidate.Candidate.AppliedOverrideId,
                suppressionReason: $"Suppressed because behavior '{behaviorId}' is also mapped by higher-precedence authoring style '{winningCandidate.AuthoringStyle}'.")
        };
    }

    private static AppliedRestEndpointOverride? ResolveOverride(
        string sourceModuleId,
        RestBehaviorEndpointProjection endpointProjection,
        int? defaultApiVersionMajor,
        RestBehaviorRouteGroupProjection group,
        IReadOnlyList<RestEndpointOverrideOptions>? overrides)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentNullException.ThrowIfNull(group);

        if (overrides is null || overrides.Count == 0)
        {
            return null;
        }

        var matchedOverride = overrides
            .Where(overrideOptions => MatchesOverride(
                sourceModuleId,
                endpointProjection.BehaviorId,
                endpointProjection.AuthoringStyle,
                overrideOptions))
            .OrderByDescending(static overrideOptions => CountTargetDimensions(overrideOptions))
            .ThenByDescending(static overrideOptions => overrideOptions.BehaviorIds.Count > 0)
            .ThenBy(static overrideOptions => overrideOptions.AuthoringStyles.Count)
            .ThenBy(static overrideOptions => overrideOptions.BehaviorIds.Count + overrideOptions.SourceModuleIds.Count)
            .ThenBy(static overrideOptions => overrideOptions.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (matchedOverride is null)
        {
            return null;
        }

        var effectiveEndpointProjection = endpointProjection;
        var effectiveApiVersionMajor = defaultApiVersionMajor;
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

        if (matchedOverride.Bindings.Count > 0)
        {
            var overrideBindings = RestEndpointBindingDescriptorAdapter.ToBehaviorDescriptors(matchedOverride.Bindings);
            if (!effectiveEndpointProjection.Bindings.SequenceEqual(overrideBindings))
            {
                effectiveEndpointProjection = effectiveEndpointProjection.WithBindings(overrideBindings);
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

        return wasApplied
            ? new AppliedRestEndpointOverride(
                matchedOverride.Id,
                effectiveEndpointProjection,
                effectiveApiVersionMajor)
            : null;
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
            throw new InvalidOperationException(
                $"REST endpoint override rule '{overrideId}' cannot rewrite behavior '{endpointProjection.BehaviorId}' from pattern '{endpointProjection.Pattern}' to '{overridePattern}' because this slice does not yet allow placeholder additions. Additive route-shape overrides remain later work.");
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

    private static RestEndpointSuppressionOptions? ResolveSuppression(
        RestEndpointCandidateRuntimeDescriptor candidate,
        IReadOnlyList<RestEndpointSuppressionOptions>? suppressions)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (suppressions is null || suppressions.Count == 0)
        {
            return null;
        }

        return suppressions
            .Where(suppression => MatchesSuppression(candidate, suppression))
            .OrderByDescending(static suppression => CountTargetDimensions(suppression))
            .ThenByDescending(static suppression => suppression.BehaviorIds.Count > 0)
            .ThenBy(static suppression => suppression.AuthoringStyles.Count)
            .ThenBy(static suppression => suppression.BehaviorIds.Count + suppression.SourceModuleIds.Count)
            .ThenBy(static suppression => suppression.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static bool MatchesSuppression(
        RestEndpointCandidateRuntimeDescriptor candidate,
        RestEndpointSuppressionOptions suppression)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(suppression);

        if (!suppression.AuthoringStyles.Contains(candidate.AuthoringStyle, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (suppression.SourceModuleIds.Count > 0)
        {
            var sourceModuleId = candidate.ProjectedEndpoint.SourceModuleId;
            if (string.IsNullOrWhiteSpace(sourceModuleId) ||
                !suppression.SourceModuleIds.Contains(sourceModuleId, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (suppression.BehaviorIds.Count > 0)
        {
            var behaviorId = candidate.ProjectedEndpoint.BehaviorId;
            if (string.IsNullOrWhiteSpace(behaviorId) ||
                !suppression.BehaviorIds.Contains(behaviorId, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesOverride(
        string sourceModuleId,
        string behaviorId,
        string authoringStyle,
        RestEndpointOverrideOptions overrideOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(authoringStyle);
        ArgumentNullException.ThrowIfNull(overrideOptions);

        if (!overrideOptions.AuthoringStyles.Contains(authoringStyle, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (overrideOptions.SourceModuleIds.Count > 0 &&
            !overrideOptions.SourceModuleIds.Contains(sourceModuleId, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (overrideOptions.BehaviorIds.Count > 0 &&
            !overrideOptions.BehaviorIds.Contains(behaviorId, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static int CountTargetDimensions(RestEndpointSuppressionOptions suppression)
    {
        ArgumentNullException.ThrowIfNull(suppression);

        var count = 0;
        if (suppression.BehaviorIds.Count > 0)
        {
            count++;
        }

        if (suppression.SourceModuleIds.Count > 0)
        {
            count++;
        }

        return count;
    }

    private static int CountTargetDimensions(RestEndpointOverrideOptions overrideOptions)
    {
        ArgumentNullException.ThrowIfNull(overrideOptions);

        var count = 0;
        if (overrideOptions.BehaviorIds.Count > 0)
        {
            count++;
        }

        if (overrideOptions.SourceModuleIds.Count > 0)
        {
            count++;
        }

        return count;
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
}

internal sealed record ResolvedRestBehaviorEndpointProjectionCandidate(
    int GroupIndex,
    RestBehaviorEndpointProjection EffectiveEndpointProjection,
    RestEndpointCandidateRuntimeDescriptor Candidate);

internal sealed record AppliedRestEndpointOverride(
    string Id,
    RestBehaviorEndpointProjection EffectiveEndpointProjection,
    int? EffectiveApiVersionMajor);
