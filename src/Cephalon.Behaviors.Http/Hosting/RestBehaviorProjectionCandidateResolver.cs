using System.Reflection;
using System.Text;
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
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    internal static IReadOnlyList<ResolvedRestBehaviorEndpointProjectionCandidate> ResolveCandidates(
        ModuleDescriptor moduleDescriptor,
        ApiRoutesOptions apiRoutesOptions,
        IReadOnlyList<RestBehaviorRouteGroupProjection> groups,
        IReadOnlyList<RestEndpointSuppressionOptions>? suppressions = null,
        IReadOnlyList<RestEndpointOverrideOptions>? overrides = null,
        IReadOnlyList<RestEndpointPublicationGroupAuthoringPolicyDescriptor>? authoringPolicies = null)
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

        var authoringPoliciesByBehaviorId = (authoringPolicies ?? [])
            .Where(static policy => policy is not null)
            .ToDictionary(static policy => policy.BehaviorId, Comparer);
        var matchedSuppressionsByCandidateId = candidates.ToDictionary(
            static candidate => candidate.Candidate.Id,
            candidate => ResolveMatchingSuppressions(candidate, suppressions),
            Comparer);
        var skippedSuppressionsByCandidateId = candidates.ToDictionary(
            static candidate => candidate.Candidate.Id,
            candidate => ResolveSkippedSuppressions(candidate, suppressions),
            Comparer);
        var skippedOverridesByCandidateId = candidates.ToDictionary(
            static candidate => candidate.Candidate.Id,
            candidate => ResolveSkippedOverrides(candidate, overrides),
            Comparer);
        var suppressionByCandidateId = matchedSuppressionsByCandidateId.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.FirstOrDefault(),
            Comparer);
        var authoringPolicySuppressionsByCandidateId = candidates
            .Where(candidate => suppressionByCandidateId[candidate.Candidate.Id] is null)
            .GroupBy(
                static candidate => candidate.Candidate.ProjectedEndpoint.BehaviorId ?? string.Empty,
                Comparer)
            .SelectMany(group => ResolveAuthoringPolicySuppressions(
                group.Key,
                group,
                ResolveAuthoringPolicy(group.Key, authoringPoliciesByBehaviorId)))
            .ToDictionary(
                static suppression => suppression.CandidateId,
                static suppression => suppression,
                Comparer);

        var winnersByBehavior = candidates
            .Where(candidate => suppressionByCandidateId[candidate.Candidate.Id] is null)
            .Where(candidate => !authoringPolicySuppressionsByCandidateId.ContainsKey(candidate.Candidate.Id))
            .GroupBy(
                static candidate => candidate.Candidate.ProjectedEndpoint.BehaviorId ?? string.Empty,
                Comparer)
            .ToDictionary(
                static group => group.Key,
                group => ResolvePublishedCandidates(
                    group,
                    ResolveAuthoringPolicy(group.Key, authoringPoliciesByBehaviorId)),
                Comparer);

        var resolvedCandidates = EnsureUniquePublishedEndpointNames(
            candidates.Select(candidate => ResolvePublication(
                candidate,
                matchedSuppressionsByCandidateId,
                skippedSuppressionsByCandidateId,
                skippedOverridesByCandidateId,
                authoringPolicySuppressionsByCandidateId,
                winnersByBehavior)));

        return resolvedCandidates
            .OrderBy(static candidate => candidate.Candidate.ProjectedEndpoint.RoutePattern, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.Candidate.ProjectedEndpoint.Method, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.Candidate.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ResolvedRestBehaviorEndpointProjectionCandidate[] ResolvePublishedCandidates(
        IEnumerable<ResolvedRestBehaviorEndpointProjectionCandidate> candidates,
        RestEndpointPublicationGroupAuthoringPolicyDescriptor? authoringPolicy)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var orderedCandidates = candidates
            .OrderBy(static candidate => candidate.Candidate.PrecedenceRank)
            .ThenBy(static candidate => candidate.Candidate.Id, Comparer)
            .ToArray();
        if (orderedCandidates.Length == 0)
        {
            return [];
        }

        if (AllowMultiplePublishedCandidates(authoringPolicy))
        {
            return orderedCandidates;
        }

        var winningRank = orderedCandidates[0].Candidate.PrecedenceRank;
        return orderedCandidates
            .Where(candidate => candidate.Candidate.PrecedenceRank == winningRank)
            .ToArray();
    }

    private static bool AllowMultiplePublishedCandidates(
        RestEndpointPublicationGroupAuthoringPolicyDescriptor? authoringPolicy)
    {
        return authoringPolicy?.AllowMultiplePublishedCandidates == true;
    }

    private static ResolvedRestBehaviorEndpointProjectionCandidate[] EnsureUniquePublishedEndpointNames(
        IEnumerable<ResolvedRestBehaviorEndpointProjectionCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var candidateArray = candidates.ToArray();
        if (candidateArray.Length <= 1)
        {
            return candidateArray;
        }

        var replacementNamesByCandidateId = new Dictionary<string, string>(Comparer);
        foreach (var duplicateGroup in candidateArray
                     .Where(static candidate =>
                         candidate.Candidate.Status == RestEndpointCandidateStatus.Published &&
                         !string.IsNullOrWhiteSpace(candidate.Candidate.ProjectedEndpoint.EndpointName))
                     .GroupBy(static candidate => candidate.Candidate.ProjectedEndpoint.EndpointName!, Comparer)
                     .Where(static group => group.Count() > 1))
        {
            var usedNames = new HashSet<string>(Comparer);
            foreach (var candidate in duplicateGroup
                         .OrderBy(static item => item.Candidate.PrecedenceRank)
                         .ThenBy(static item => item.Candidate.AuthoringStyle, Comparer)
                         .ThenBy(static item => item.Candidate.ProjectedEndpoint.RoutePattern, Comparer)
                         .ThenBy(static item => item.Candidate.Id, Comparer))
            {
                var uniqueEndpointName = BuildUniquePublishedEndpointName(candidate, usedNames);
                replacementNamesByCandidateId[candidate.Candidate.Id] = uniqueEndpointName;
            }
        }

        if (replacementNamesByCandidateId.Count == 0)
        {
            return candidateArray;
        }

        return candidateArray
            .Select(candidate => replacementNamesByCandidateId.TryGetValue(candidate.Candidate.Id, out var endpointName)
                ? UpdateProjectedEndpointName(candidate, endpointName)
                : candidate)
            .ToArray();
    }

    private static string BuildUniquePublishedEndpointName(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        HashSet<string> usedNames)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(usedNames);

        var baseEndpointName = candidate.Candidate.ProjectedEndpoint.EndpointName
            ?? throw new InvalidOperationException("Published REST endpoint candidates must expose an endpoint name before disambiguation.");
        var authoringStyleSuffix = NormalizeEndpointNameSegment(candidate.Candidate.AuthoringStyle);
        var preferredName = $"{baseEndpointName}.{authoringStyleSuffix}";
        if (usedNames.Add(preferredName))
        {
            return preferredName;
        }

        var routeSuffix = NormalizeEndpointNameSegment(
            candidate.Candidate.ProjectedEndpoint.RelativePattern ?? candidate.Candidate.ProjectedEndpoint.RoutePattern);
        var routeQualifiedName = $"{preferredName}.{routeSuffix}";
        if (usedNames.Add(routeQualifiedName))
        {
            return routeQualifiedName;
        }

        var candidateIdSuffix = candidate.Candidate.Id.Length > 8
            ? candidate.Candidate.Id[..8]
            : candidate.Candidate.Id;
        var candidateQualifiedName = $"{preferredName}.{NormalizeEndpointNameSegment(candidateIdSuffix)}";
        if (usedNames.Add(candidateQualifiedName))
        {
            return candidateQualifiedName;
        }

        var counter = 2;
        while (true)
        {
            var numberedName = $"{candidateQualifiedName}.{counter}";
            if (usedNames.Add(numberedName))
            {
                return numberedName;
            }

            counter++;
        }
    }

    private static string NormalizeEndpointNameSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "candidate";
        }

        var builder = new StringBuilder(value.Length);
        var pendingSeparator = false;
        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(character));
                pendingSeparator = false;
            }
            else
            {
                pendingSeparator = true;
            }
        }

        return builder.Length == 0
            ? "candidate"
            : builder.ToString();
    }

    private static ResolvedRestBehaviorEndpointProjectionCandidate UpdateProjectedEndpointName(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        string endpointName)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);

        var updatedProjectedEndpoint = new RestEndpointRuntimeDescriptor(
            candidate.Candidate.ProjectedEndpoint.Id,
            candidate.Candidate.ProjectedEndpoint.TransportId,
            candidate.Candidate.ProjectedEndpoint.SourceKind,
            candidate.Candidate.ProjectedEndpoint.Method,
            candidate.Candidate.ProjectedEndpoint.RoutePattern,
            candidate.Candidate.ProjectedEndpoint.SourceModuleId,
            candidate.Candidate.ProjectedEndpoint.SourceModuleVersion,
            candidate.Candidate.ProjectedEndpoint.SourceModuleVersionMajor,
            candidate.Candidate.ProjectedEndpoint.BehaviorId,
            endpointName,
            candidate.Candidate.ProjectedEndpoint.OpenApiDocumentName,
            candidate.Candidate.ProjectedEndpoint.ApiVersionMajor,
            candidate.Candidate.ProjectedEndpoint.Tags,
            candidate.Candidate.ProjectedEndpoint.Summary,
            candidate.Candidate.ProjectedEndpoint.Description,
            candidate.Candidate.ProjectedEndpoint.OriginalEndpointName,
            candidate.Candidate.ProjectedEndpoint.OriginalSummary,
            candidate.Candidate.ProjectedEndpoint.OriginalDescription,
            candidate.Candidate.ProjectedEndpoint.CandidateId,
            candidate.Candidate.ProjectedEndpoint.OriginalProjection,
            candidate.Candidate.ProjectedEndpoint.BindingDescriptors,
            candidate.Candidate.ProjectedEndpoint.BindingFallbackMode,
            candidate.Candidate.ProjectedEndpoint.Metadata,
            candidate.Candidate.ProjectedEndpoint.AuthoringStyle,
            candidate.Candidate.ProjectedEndpoint.RouteGroupPrefix,
            candidate.Candidate.ProjectedEndpoint.RelativePattern,
            candidate.Candidate.ProjectedEndpoint.BehaviorType,
            candidate.Candidate.ProjectedEndpoint.SourceId,
            candidate.Candidate.ProjectedEndpoint.RequiredCapabilityKey,
            candidate.Candidate.ProjectedEndpoint.OriginalRequiredCapabilityKey,
            candidate.Candidate.ProjectedEndpoint.AppliedOverrideId,
            candidate.Candidate.ProjectedEndpoint.MatchedOverrideIds,
            candidate.Candidate.ProjectedEndpoint.SelectedOverrideId,
            candidate.Candidate.ProjectedEndpoint.OverrideSelectionBasis,
            candidate.Candidate.ProjectedEndpoint.SkippedSuppressionIds,
            candidate.Candidate.ProjectedEndpoint.SkippedOverrideIds,
            candidate.Candidate.ProjectedEndpoint.SelectedOverrideActionKinds,
            candidate.Candidate.ProjectedEndpoint.AppliedOverrideActionKinds);

        return candidate with
        {
            Candidate = new RestEndpointCandidateRuntimeDescriptor(
                candidate.Candidate.Id,
                updatedProjectedEndpoint,
                candidate.Candidate.OriginalProjection,
                candidate.Candidate.AuthoringStyle,
                candidate.Candidate.PrecedenceRank,
                candidate.Candidate.Status,
                suppressedByCandidateId: candidate.Candidate.SuppressedByCandidateId,
                suppressedBySuppressionId: candidate.Candidate.SuppressedBySuppressionId,
                appliedOverrideId: candidate.Candidate.AppliedOverrideId,
                matchedSuppressionIds: candidate.Candidate.MatchedSuppressionIds,
                matchedOverrideIds: candidate.Candidate.MatchedOverrideIds,
                suppressionReason: candidate.Candidate.SuppressionReason,
                suppressedByAuthoringPolicyKind: candidate.Candidate.SuppressedByAuthoringPolicyKind,
                selectedOverrideId: candidate.Candidate.SelectedOverrideId,
                suppressionSelectionBasis: candidate.Candidate.SuppressionSelectionBasis,
                overrideSelectionBasis: candidate.Candidate.OverrideSelectionBasis,
                skippedSuppressionIds: candidate.Candidate.SkippedSuppressionIds,
                skippedOverrideIds: candidate.Candidate.SkippedOverrideIds,
                selectedOverrideActionKinds: candidate.Candidate.SelectedOverrideActionKinds,
                appliedOverrideActionKinds: candidate.Candidate.AppliedOverrideActionKinds)
        };
    }

    private static ResolvedRestBehaviorEndpointProjectionCandidate ApplySkippedGovernanceRuleIds(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        IReadOnlyList<string>? skippedSuppressionIds,
        IReadOnlyList<string>? skippedOverrideIds)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var normalizedSkippedSuppressionIds = NormalizeOrderedIdentifiers(skippedSuppressionIds);
        var normalizedSkippedOverrideIds = NormalizeOrderedIdentifiers(skippedOverrideIds);
        if (candidate.Candidate.SkippedSuppressionIds.SequenceEqual(normalizedSkippedSuppressionIds, Comparer) &&
            candidate.Candidate.SkippedOverrideIds.SequenceEqual(normalizedSkippedOverrideIds, Comparer))
        {
            return candidate;
        }

        var updatedProjectedEndpoint = new RestEndpointRuntimeDescriptor(
            candidate.Candidate.ProjectedEndpoint.Id,
            candidate.Candidate.ProjectedEndpoint.TransportId,
            candidate.Candidate.ProjectedEndpoint.SourceKind,
            candidate.Candidate.ProjectedEndpoint.Method,
            candidate.Candidate.ProjectedEndpoint.RoutePattern,
            candidate.Candidate.ProjectedEndpoint.SourceModuleId,
            candidate.Candidate.ProjectedEndpoint.SourceModuleVersion,
            candidate.Candidate.ProjectedEndpoint.SourceModuleVersionMajor,
            candidate.Candidate.ProjectedEndpoint.BehaviorId,
            candidate.Candidate.ProjectedEndpoint.EndpointName,
            candidate.Candidate.ProjectedEndpoint.OpenApiDocumentName,
            candidate.Candidate.ProjectedEndpoint.ApiVersionMajor,
            candidate.Candidate.ProjectedEndpoint.Tags,
            candidate.Candidate.ProjectedEndpoint.Summary,
            candidate.Candidate.ProjectedEndpoint.Description,
            candidate.Candidate.ProjectedEndpoint.OriginalEndpointName,
            candidate.Candidate.ProjectedEndpoint.OriginalSummary,
            candidate.Candidate.ProjectedEndpoint.OriginalDescription,
            candidate.Candidate.ProjectedEndpoint.CandidateId,
            candidate.Candidate.ProjectedEndpoint.OriginalProjection,
            candidate.Candidate.ProjectedEndpoint.BindingDescriptors,
            candidate.Candidate.ProjectedEndpoint.BindingFallbackMode,
            candidate.Candidate.ProjectedEndpoint.Metadata,
            candidate.Candidate.ProjectedEndpoint.AuthoringStyle,
            candidate.Candidate.ProjectedEndpoint.RouteGroupPrefix,
            candidate.Candidate.ProjectedEndpoint.RelativePattern,
            candidate.Candidate.ProjectedEndpoint.BehaviorType,
            candidate.Candidate.ProjectedEndpoint.SourceId,
            candidate.Candidate.ProjectedEndpoint.RequiredCapabilityKey,
            candidate.Candidate.ProjectedEndpoint.OriginalRequiredCapabilityKey,
            candidate.Candidate.ProjectedEndpoint.AppliedOverrideId,
            candidate.Candidate.ProjectedEndpoint.MatchedOverrideIds,
            candidate.Candidate.ProjectedEndpoint.SelectedOverrideId,
            candidate.Candidate.ProjectedEndpoint.OverrideSelectionBasis,
            normalizedSkippedSuppressionIds,
            normalizedSkippedOverrideIds,
            candidate.Candidate.ProjectedEndpoint.SelectedOverrideActionKinds,
            candidate.Candidate.ProjectedEndpoint.AppliedOverrideActionKinds);

        return candidate with
        {
            Candidate = new RestEndpointCandidateRuntimeDescriptor(
                candidate.Candidate.Id,
                updatedProjectedEndpoint,
                candidate.Candidate.OriginalProjection,
                candidate.Candidate.AuthoringStyle,
                candidate.Candidate.PrecedenceRank,
                candidate.Candidate.Status,
                suppressedByCandidateId: candidate.Candidate.SuppressedByCandidateId,
                suppressedBySuppressionId: candidate.Candidate.SuppressedBySuppressionId,
                appliedOverrideId: candidate.Candidate.AppliedOverrideId,
                matchedSuppressionIds: candidate.Candidate.MatchedSuppressionIds,
                matchedOverrideIds: candidate.Candidate.MatchedOverrideIds,
                suppressionReason: candidate.Candidate.SuppressionReason,
                suppressedByAuthoringPolicyKind: candidate.Candidate.SuppressedByAuthoringPolicyKind,
                selectedOverrideId: candidate.Candidate.SelectedOverrideId,
                suppressionSelectionBasis: candidate.Candidate.SuppressionSelectionBasis,
                overrideSelectionBasis: candidate.Candidate.OverrideSelectionBasis,
                skippedSuppressionIds: normalizedSkippedSuppressionIds,
                skippedOverrideIds: normalizedSkippedOverrideIds,
                selectedOverrideActionKinds: candidate.Candidate.SelectedOverrideActionKinds,
                appliedOverrideActionKinds: candidate.Candidate.AppliedOverrideActionKinds)
        };
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
        var originalOpenApiDocumentName = ResolveGroupOpenApiDocumentName(group, defaultApiVersionMajor);
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
                originalOpenApiDocumentName,
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
        string originalOpenApiDocumentName,
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

        var originalBindingFallbackMode = RestBehaviorBindingFallbackModeResolver.ResolveForBehavior(
            endpointProjection.BehaviorType,
            endpointProjection.Method,
            endpointProjection.Pattern,
            endpointProjection.Bindings,
            endpointProjection.PreserveImplicitQueryFallback);
        var originalProjection = CreateCandidateProjectionDescriptor(
            defaultApiVersionMajor,
            endpointProjection.Method.ToString().ToUpperInvariant(),
            originalRouteGroupPrefix,
            endpointProjection.Pattern,
            originalOpenApiDocumentName,
            RestEndpointBindingDescriptorAdapter.ToRuntimeDescriptors(endpointProjection.Bindings),
            originalBindingFallbackMode,
            tagName,
            AllowsHostGovernance(group, endpointProjection),
            group.HostGovernanceScope);
        var candidateId = BuildCandidateId(
            moduleDescriptor.Id,
            endpointProjection.BehaviorId,
            endpointProjection.AuthoringStyle,
            originalProjection.Method,
            originalProjection.RoutePattern);
        var originalOperationName = RestBehaviorEndpointMetadataConventions.BuildOperationName(
            moduleDescriptor.Id,
            defaultApiVersionMajor,
            endpointProjection.BehaviorId);
        var overrideDecision = ResolveOverrideDecision(
            moduleDescriptor.Id,
            candidateId,
            endpointProjection,
            defaultApiVersionMajor,
            apiRoutesOptions.RestPrefix,
            originalOpenApiDocumentName,
            originalRouteGroupPrefix,
            originalProjection.BindingDescriptors,
            originalBindingFallbackMode,
            tagName,
            originalOperationName,
            originalProjection.HostGovernanceScope,
            originalProjection.AllowsHostGovernance,
            group,
            overrides);
        var selectedOverride = overrideDecision.SelectedOverride;
        var appliedOverride = overrideDecision.AppliedOverride;
        var effectiveEndpointProjection = appliedOverride?.EffectiveEndpointProjection ?? endpointProjection;
        var effectiveApiVersionMajor = appliedOverride?.EffectiveApiVersionMajor ?? defaultApiVersionMajor;
        var effectiveOpenApiDocumentName = appliedOverride?.EffectiveOpenApiDocumentName ?? originalOpenApiDocumentName;
        var effectiveTagName = appliedOverride?.EffectiveTagName ?? tagName;
        var publishedRouteGroupPrefix = appliedOverride?.EffectiveRouteGroupPrefix ??
                                        RestEndpointRuntimeDescriptorFactory.CombinePaths(
                                            apiRoutesOptions.RestPrefix,
                                            ResolveRouteGroupPrefix(group.Prefix, effectiveApiVersionMajor));
        var method = effectiveEndpointProjection.Method.ToString().ToUpperInvariant();
        var routePattern = RestEndpointRuntimeDescriptorFactory.CombinePaths(
            publishedRouteGroupPrefix,
            effectiveEndpointProjection.Pattern);
        var runtimeBindings = RestEndpointBindingDescriptorAdapter.ToRuntimeDescriptors(effectiveEndpointProjection.Bindings);
        var projectedBindingFallbackMode = RestBehaviorBindingFallbackModeResolver.ResolveForBehavior(
            effectiveEndpointProjection.BehaviorType,
            effectiveEndpointProjection.Method,
            effectiveEndpointProjection.Pattern,
            effectiveEndpointProjection.Bindings,
            effectiveEndpointProjection.PreserveImplicitQueryFallback);
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
        var selectedOverrideActionKinds = selectedOverride?.ActionKinds ?? [];
        var appliedOverrideActionKinds = ResolveAppliedActionKinds(
            appliedOverride?.ActionKinds,
            appliedMetadataOverride?.ActionKinds,
            appliedCapabilityOverride?.ActionKinds);
        var endpointName = ResolveEffectiveMetadataValue(
            selectedOverride?.EndpointName,
            selectedOverride?.ClearEndpointName == true,
            operationName);
        var summary = ResolveEffectiveMetadataValue(
            selectedOverride?.Summary,
            selectedOverride?.ClearSummary == true,
            documentation.Summary);
        var description = ResolveEffectiveMetadataValue(
            selectedOverride?.Description,
            selectedOverride?.ClearDescription == true,
            documentation.Description);
        var projectedEndpoint = RestEndpointRuntimeDescriptorFactory.CreateBehaviorDescriptor(
            sourceKind: RestEndpointRuntimeMetadata.ModuleDslSourceKind,
            method: method,
            routePattern: routePattern,
            sourceModuleId: moduleDescriptor.Id,
            sourceModuleVersion: moduleDescriptor.Version,
            sourceModuleVersionMajor: ResolveModuleMajorVersion(moduleDescriptor.Version),
            behaviorId: effectiveEndpointProjection.BehaviorId,
            endpointName: endpointName,
            openApiDocumentName: effectiveOpenApiDocumentName,
            apiVersionMajor: effectiveApiVersionMajor,
            tags: [effectiveTagName],
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
            bindingFallbackMode: projectedBindingFallbackMode,
            requiredCapabilityKey: appliedCapabilityOverride?.ClearRequiredCapability == true
                ? null
                : appliedCapabilityOverride?.RequiredCapabilityKey,
            appliedOverrideId: appliedOverride?.Id ?? appliedCapabilityOverride?.OverrideId ?? appliedMetadataOverride?.OverrideId,
            matchedOverrideIds: overrideDecision.MatchedOverrideIds,
            selectedOverrideId: selectedOverride?.Id,
            selectedOverrideActionKinds: selectedOverrideActionKinds,
            appliedOverrideActionKinds: appliedOverrideActionKinds,
            overrideSelectionBasis: overrideDecision.SelectionBasis);
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
                matchedOverrideIds: overrideDecision.MatchedOverrideIds,
                selectedOverrideId: selectedOverride?.Id,
                selectedOverrideActionKinds: selectedOverrideActionKinds,
                appliedOverrideActionKinds: appliedOverrideActionKinds,
                overrideSelectionBasis: overrideDecision.SelectionBasis),
            appliedCapabilityOverride,
            appliedMetadataOverride);
    }

    private static ResolvedRestBehaviorEndpointProjectionCandidate ResolvePublication(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        Dictionary<string, RestEndpointSuppressionOptions[]> matchedSuppressionsByCandidateId,
        Dictionary<string, string[]> skippedSuppressionsByCandidateId,
        Dictionary<string, string[]> skippedOverridesByCandidateId,
        Dictionary<string, ResolvedRestEndpointAuthoringPolicySuppression> authoringPolicySuppressionsByCandidateId,
        Dictionary<string, ResolvedRestBehaviorEndpointProjectionCandidate[]> winnersByBehavior)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(matchedSuppressionsByCandidateId);
        ArgumentNullException.ThrowIfNull(skippedSuppressionsByCandidateId);
        ArgumentNullException.ThrowIfNull(skippedOverridesByCandidateId);
        ArgumentNullException.ThrowIfNull(authoringPolicySuppressionsByCandidateId);
        ArgumentNullException.ThrowIfNull(winnersByBehavior);

        candidate = ApplySkippedGovernanceRuleIds(
            candidate,
            skippedSuppressionsByCandidateId[candidate.Candidate.Id],
            skippedOverridesByCandidateId[candidate.Candidate.Id]);

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
                suppressionReason: $"Suppressed by REST endpoint suppression rule '{suppression.Id}'.",
                selectedOverrideId: candidate.Candidate.SelectedOverrideId,
                suppressionSelectionBasis: ResolveSuppressionSelectionBasis(matchedSuppressions),
                overrideSelectionBasis: candidate.Candidate.OverrideSelectionBasis,
                skippedSuppressionIds: candidate.Candidate.SkippedSuppressionIds,
                skippedOverrideIds: candidate.Candidate.SkippedOverrideIds,
                selectedOverrideActionKinds: candidate.Candidate.SelectedOverrideActionKinds,
                appliedOverrideActionKinds: candidate.Candidate.AppliedOverrideActionKinds)
            };
        }

        if (authoringPolicySuppressionsByCandidateId.TryGetValue(candidate.Candidate.Id, out var authoringPolicySuppression))
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
                    appliedOverrideId: candidate.Candidate.AppliedOverrideId,
                    matchedOverrideIds: candidate.Candidate.MatchedOverrideIds,
                    suppressionReason: authoringPolicySuppression.SuppressionReason,
                    suppressedByAuthoringPolicyKind: authoringPolicySuppression.Kind,
                    selectedOverrideId: candidate.Candidate.SelectedOverrideId,
                    overrideSelectionBasis: candidate.Candidate.OverrideSelectionBasis,
                    skippedSuppressionIds: candidate.Candidate.SkippedSuppressionIds,
                    skippedOverrideIds: candidate.Candidate.SkippedOverrideIds,
                    selectedOverrideActionKinds: candidate.Candidate.SelectedOverrideActionKinds,
                    appliedOverrideActionKinds: candidate.Candidate.AppliedOverrideActionKinds)
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
                suppressionReason: $"Suppressed because behavior '{behaviorId}' is also mapped by higher-precedence authoring style '{winningCandidate.AuthoringStyle}'.",
                selectedOverrideId: candidate.Candidate.SelectedOverrideId,
                overrideSelectionBasis: candidate.Candidate.OverrideSelectionBasis,
                skippedSuppressionIds: candidate.Candidate.SkippedSuppressionIds,
                skippedOverrideIds: candidate.Candidate.SkippedOverrideIds,
                selectedOverrideActionKinds: candidate.Candidate.SelectedOverrideActionKinds,
                appliedOverrideActionKinds: candidate.Candidate.AppliedOverrideActionKinds)
        };
    }

    private static RestEndpointPublicationGroupAuthoringPolicyDescriptor? ResolveAuthoringPolicy(
        string behaviorId,
        Dictionary<string, RestEndpointPublicationGroupAuthoringPolicyDescriptor> authoringPoliciesByBehaviorId)
    {
        ArgumentNullException.ThrowIfNull(authoringPoliciesByBehaviorId);

        return !string.IsNullOrWhiteSpace(behaviorId) &&
               authoringPoliciesByBehaviorId.TryGetValue(behaviorId.Trim(), out var authoringPolicy)
            ? authoringPolicy
            : null;
    }

    private static ResolvedRestEndpointAuthoringPolicySuppression[] ResolveAuthoringPolicySuppressions(
        string behaviorId,
        IEnumerable<ResolvedRestBehaviorEndpointProjectionCandidate> candidates,
        RestEndpointPublicationGroupAuthoringPolicyDescriptor? authoringPolicy)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (authoringPolicy is null)
        {
            return [];
        }

        var orderedCandidates = candidates
            .OrderBy(static candidate => candidate.Candidate.PrecedenceRank)
            .ThenBy(static candidate => candidate.Candidate.AuthoringStyle, Comparer)
            .ThenBy(static candidate => candidate.Candidate.Id, Comparer)
            .ToArray();
        if (orderedCandidates.Length == 0)
        {
            return [];
        }

        var suppressionsByCandidateId = new Dictionary<string, ResolvedRestEndpointAuthoringPolicySuppression>(Comparer);

        ApplyDisallowedAuthoringStyleSuppressions(
            behaviorId,
            orderedCandidates,
            authoringPolicy,
            suppressionsByCandidateId);
        ApplyNotAllowedAuthoringStyleSuppressions(
            behaviorId,
            orderedCandidates,
            authoringPolicy,
            suppressionsByCandidateId);
        ApplyPreferredAuthoringStyleSuppressions(
            behaviorId,
            orderedCandidates,
            authoringPolicy,
            suppressionsByCandidateId);

        return suppressionsByCandidateId.Values
            .OrderBy(static suppression => suppression.CandidateId, Comparer)
            .ToArray();
    }

    private static void ApplyDisallowedAuthoringStyleSuppressions(
        string behaviorId,
        IReadOnlyList<ResolvedRestBehaviorEndpointProjectionCandidate> orderedCandidates,
        RestEndpointPublicationGroupAuthoringPolicyDescriptor authoringPolicy,
        Dictionary<string, ResolvedRestEndpointAuthoringPolicySuppression> suppressionsByCandidateId)
    {
        ArgumentNullException.ThrowIfNull(orderedCandidates);
        ArgumentNullException.ThrowIfNull(authoringPolicy);
        ArgumentNullException.ThrowIfNull(suppressionsByCandidateId);

        if (authoringPolicy.DisallowedAuthoringStyles.Count == 0)
        {
            return;
        }

        foreach (var candidate in orderedCandidates.Where(CanBeSuppressedByAuthoringPolicy))
        {
            if (!authoringPolicy.DisallowedAuthoringStyles.Contains(candidate.Candidate.AuthoringStyle, Comparer))
            {
                continue;
            }

            suppressionsByCandidateId[candidate.Candidate.Id] = new ResolvedRestEndpointAuthoringPolicySuppression(
                candidate.Candidate.Id,
                RestEndpointAuthoringPolicySuppressionKind.DisallowedAuthoringStyle,
                $"Suppressed because behavior '{behaviorId}' authoring policy disallows authoring style '{candidate.Candidate.AuthoringStyle}'.");
        }
    }

    private static void ApplyNotAllowedAuthoringStyleSuppressions(
        string behaviorId,
        IReadOnlyList<ResolvedRestBehaviorEndpointProjectionCandidate> orderedCandidates,
        RestEndpointPublicationGroupAuthoringPolicyDescriptor authoringPolicy,
        Dictionary<string, ResolvedRestEndpointAuthoringPolicySuppression> suppressionsByCandidateId)
    {
        ArgumentNullException.ThrowIfNull(orderedCandidates);
        ArgumentNullException.ThrowIfNull(authoringPolicy);
        ArgumentNullException.ThrowIfNull(suppressionsByCandidateId);

        if (authoringPolicy.AllowedAuthoringStyles.Count == 0)
        {
            return;
        }

        var allowedAuthoringStyles = string.Join(", ", authoringPolicy.AllowedAuthoringStyles);
        foreach (var candidate in orderedCandidates
                     .Where(CanBeSuppressedByAuthoringPolicy)
                     .Where(candidate => !suppressionsByCandidateId.ContainsKey(candidate.Candidate.Id)))
        {
            if (authoringPolicy.AllowedAuthoringStyles.Contains(candidate.Candidate.AuthoringStyle, Comparer))
            {
                continue;
            }

            suppressionsByCandidateId[candidate.Candidate.Id] = new ResolvedRestEndpointAuthoringPolicySuppression(
                candidate.Candidate.Id,
                RestEndpointAuthoringPolicySuppressionKind.NotAllowedAuthoringStyle,
                $"Suppressed because behavior '{behaviorId}' authoring policy allows only [{allowedAuthoringStyles}], and authoring style '{candidate.Candidate.AuthoringStyle}' is outside that set.");
        }
    }

    private static void ApplyPreferredAuthoringStyleSuppressions(
        string behaviorId,
        IReadOnlyList<ResolvedRestBehaviorEndpointProjectionCandidate> orderedCandidates,
        RestEndpointPublicationGroupAuthoringPolicyDescriptor authoringPolicy,
        Dictionary<string, ResolvedRestEndpointAuthoringPolicySuppression> suppressionsByCandidateId)
    {
        ArgumentNullException.ThrowIfNull(orderedCandidates);
        ArgumentNullException.ThrowIfNull(authoringPolicy);
        ArgumentNullException.ThrowIfNull(suppressionsByCandidateId);

        if (string.IsNullOrWhiteSpace(authoringPolicy.PreferredAuthoringStyle))
        {
            return;
        }

        var preferredAuthoringStyle = authoringPolicy.PreferredAuthoringStyle.Trim();
        var remainingCandidates = orderedCandidates
            .Where(candidate => !suppressionsByCandidateId.ContainsKey(candidate.Candidate.Id))
            .ToArray();
        if (!remainingCandidates.Any(candidate =>
                string.Equals(candidate.Candidate.AuthoringStyle, preferredAuthoringStyle, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        foreach (var candidate in remainingCandidates.Where(CanBeSuppressedByAuthoringPolicy))
        {
            if (string.Equals(candidate.Candidate.AuthoringStyle, preferredAuthoringStyle, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            suppressionsByCandidateId[candidate.Candidate.Id] = new ResolvedRestEndpointAuthoringPolicySuppression(
                candidate.Candidate.Id,
                RestEndpointAuthoringPolicySuppressionKind.PreferredAuthoringStyleSelected,
                $"Suppressed because behavior '{behaviorId}' authoring policy prefers authoring style '{preferredAuthoringStyle}' when it is present.");
        }
    }

    private static bool CanBeSuppressedByAuthoringPolicy(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return RestEndpointRuntimeMetadata.IsShorthandAuthoringStyle(candidate.Candidate.AuthoringStyle);
    }

    private static ResolvedRestEndpointOverrideDecision ResolveOverrideDecision(
        string sourceModuleId,
        string originalCandidateId,
        RestBehaviorEndpointProjection endpointProjection,
        int? defaultApiVersionMajor,
        string restPrefix,
        string originalOpenApiDocumentName,
        string originalRouteGroupPrefix,
        IReadOnlyList<RestEndpointBindingDescriptor> originalBindingDescriptors,
        RestEndpointBindingFallbackMode? originalBindingFallbackMode,
        string originalTagName,
        string originalEndpointName,
        string? originalHostGovernanceScope,
        bool allowsHostGovernance,
        RestBehaviorRouteGroupProjection group,
        IReadOnlyList<RestEndpointOverrideOptions>? overrides)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalCandidateId);
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentNullException.ThrowIfNull(restPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalRouteGroupPrefix);
        ArgumentNullException.ThrowIfNull(originalBindingDescriptors);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalTagName);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalEndpointName);
        ArgumentNullException.ThrowIfNull(group);

        if (!allowsHostGovernance || overrides is null || overrides.Count == 0)
        {
            return new ResolvedRestEndpointOverrideDecision([], null, null, null);
        }

        var matchedOverrides = ResolveMatchingOverrides(
            sourceModuleId,
            originalCandidateId,
            endpointProjection,
            defaultApiVersionMajor,
            originalOpenApiDocumentName,
            originalRouteGroupPrefix,
            originalBindingDescriptors,
            originalBindingFallbackMode,
            originalTagName,
            originalEndpointName,
            originalHostGovernanceScope,
            allowsHostGovernance,
            overrides);
        var matchedOverride = matchedOverrides.FirstOrDefault();
        if (matchedOverride is null)
        {
            return new ResolvedRestEndpointOverrideDecision([], null, null, null);
        }

        var effectiveEndpointProjection = endpointProjection;
        var effectiveApiVersionMajor = defaultApiVersionMajor;
        var effectiveOpenApiDocumentName = originalOpenApiDocumentName;
        var effectiveTagName = originalTagName;
        string? effectiveRouteGroupPrefix = null;
        var wasApplied = false;
        var appliedActionKinds = new HashSet<RestEndpointOverrideActionKind>();
        var shouldRevalidateBindings = false;

        if (!string.IsNullOrWhiteSpace(matchedOverride.Pattern) &&
            !string.Equals(matchedOverride.Pattern, endpointProjection.Pattern, StringComparison.Ordinal))
        {
            effectiveEndpointProjection = effectiveEndpointProjection.WithPattern(matchedOverride.Pattern);
            wasApplied = true;
            appliedActionKinds.Add(RestEndpointOverrideActionKind.Pattern);
            shouldRevalidateBindings = true;
        }

        if (!string.IsNullOrWhiteSpace(matchedOverride.Method))
        {
            var overrideMethod = RestBehaviorHttpMethodParser.Parse(matchedOverride.Method);
            if (overrideMethod != effectiveEndpointProjection.Method)
            {
                effectiveEndpointProjection = effectiveEndpointProjection.WithMethod(overrideMethod);
                wasApplied = true;
                appliedActionKinds.Add(RestEndpointOverrideActionKind.Method);
                shouldRevalidateBindings = true;
            }
        }

        if (matchedOverride.ClearBindings)
        {
            effectiveEndpointProjection = effectiveEndpointProjection.WithBindings([]);
            shouldRevalidateBindings = true;
        }
        else if (matchedOverride.Bindings.Count > 0 || matchedOverride.RemovedBindingProperties.Count > 0)
        {
            var overrideBindings = RestEndpointBindingDescriptorAdapter.ToBehaviorDescriptors(matchedOverride.Bindings);
            IReadOnlyList<BehaviorRestBindingDescriptor> effectiveBindings = matchedOverride.BindingMode == RestEndpointOverrideBindingMode.MergeExplicit
                ? MergeBindings(
                    matchedOverride.Id,
                    effectiveEndpointProjection.Bindings,
                    overrideBindings,
                    matchedOverride.RemovedBindingProperties)
                : overrideBindings;
            effectiveEndpointProjection = effectiveEndpointProjection.WithBindings(effectiveBindings);
            shouldRevalidateBindings = true;
        }

        IReadOnlyList<BehaviorRestBindingDescriptor>? normalizedBindings = null;
        if (shouldRevalidateBindings)
        {
            normalizedBindings = BehaviorRestBindingPlanNormalizer.Normalize(
                $"REST endpoint override rule '{matchedOverride.Id}' for behavior '{endpointProjection.BehaviorId}'",
                effectiveEndpointProjection.BehaviorType,
                effectiveEndpointProjection.Method,
                effectiveEndpointProjection.Pattern,
                effectiveEndpointProjection.Bindings);
        }

        if (!string.Equals(effectiveEndpointProjection.Pattern, endpointProjection.Pattern, StringComparison.Ordinal))
        {
            ValidatePatternOverride(
                matchedOverride.Id,
                endpointProjection,
                effectiveEndpointProjection.Pattern,
                effectiveEndpointProjection.Bindings);
        }

        if (matchedOverride.ClearBindings &&
            endpointProjection.Bindings.Any(static binding => binding.Source == BehaviorRestBindingSource.Route))
        {
            ValidateClearBindingsOverride(
                matchedOverride.Id,
                endpointProjection,
                effectiveEndpointProjection.Pattern);
        }

        var preserveImplicitQueryFallback = shouldRevalidateBindings && normalizedBindings is not null
            ? normalizedBindings.Count > 0 &&
              (endpointProjection.PreserveImplicitQueryFallback || endpointProjection.Bindings.Count == 0)
            : endpointProjection.PreserveImplicitQueryFallback;
        if (shouldRevalidateBindings && normalizedBindings is not null)
        {
            var bindingPlanChanged = !RestBehaviorBindingDescriptorSetComparer.Equivalent(endpointProjection.Bindings, normalizedBindings) ||
                                     endpointProjection.PreserveImplicitQueryFallback != preserveImplicitQueryFallback;
            effectiveEndpointProjection = bindingPlanChanged
                ? effectiveEndpointProjection.WithBindings(normalizedBindings)
                : effectiveEndpointProjection.WithBindings(endpointProjection.Bindings);
            if (bindingPlanChanged)
            {
                wasApplied = true;
                appliedActionKinds.UnionWith(ResolveBindingActionKinds(matchedOverride));
            }
        }

        effectiveEndpointProjection = effectiveEndpointProjection.WithPreserveImplicitQueryFallback(
            preserveImplicitQueryFallback);

        if (!group.HasExplicitApiVersion &&
            matchedOverride.ApiVersionMajor is int overrideApiVersionMajor &&
            overrideApiVersionMajor != defaultApiVersionMajor)
        {
            effectiveApiVersionMajor = overrideApiVersionMajor;
            wasApplied = true;
            appliedActionKinds.Add(RestEndpointOverrideActionKind.ApiVersionMajor);
        }

        if (!string.IsNullOrWhiteSpace(matchedOverride.OpenApiDocumentName))
        {
            if (!string.Equals(matchedOverride.OpenApiDocumentName, effectiveOpenApiDocumentName, StringComparison.Ordinal))
            {
                effectiveOpenApiDocumentName = matchedOverride.OpenApiDocumentName;
                wasApplied = true;
                appliedActionKinds.Add(RestEndpointOverrideActionKind.OpenApiDocumentName);
            }
        }
        else if (!group.HasExplicitOpenApiDocumentName &&
                 effectiveApiVersionMajor != defaultApiVersionMajor)
        {
            var versionDerivedOpenApiDocumentName = ResolveOpenApiDocumentName(effectiveApiVersionMajor);
            if (!string.Equals(versionDerivedOpenApiDocumentName, effectiveOpenApiDocumentName, StringComparison.Ordinal))
            {
                effectiveOpenApiDocumentName = versionDerivedOpenApiDocumentName;
            }
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
                appliedActionKinds.Add(RestEndpointOverrideActionKind.RouteGroupPrefix);
            }
        }

        if (!string.IsNullOrWhiteSpace(matchedOverride.TagName) &&
            !string.Equals(matchedOverride.TagName, effectiveTagName, StringComparison.Ordinal))
        {
            effectiveTagName = matchedOverride.TagName;
            wasApplied = true;
            appliedActionKinds.Add(RestEndpointOverrideActionKind.TagName);
        }

        return new ResolvedRestEndpointOverrideDecision(
            matchedOverrides.Select(static overrideOptions => overrideOptions.Id).ToArray(),
            matchedOverride,
            ResolveOverrideSelectionBasis(matchedOverrides),
            wasApplied
                ? new AppliedRestEndpointOverride(
                    matchedOverride.Id,
                    effectiveEndpointProjection,
                    effectiveApiVersionMajor,
                    effectiveOpenApiDocumentName,
                    effectiveRouteGroupPrefix,
                    effectiveTagName,
                    appliedActionKinds.OrderBy(static actionKind => actionKind).ToArray())
                : null);
    }

    private static RestEndpointOverrideOptions[] ResolveMatchingOverrides(
        string sourceModuleId,
        string originalCandidateId,
        RestBehaviorEndpointProjection endpointProjection,
        int? defaultApiVersionMajor,
        string originalOpenApiDocumentName,
        string originalRouteGroupPrefix,
        IReadOnlyList<RestEndpointBindingDescriptor> originalBindingDescriptors,
        RestEndpointBindingFallbackMode? originalBindingFallbackMode,
        string originalTagName,
        string originalEndpointName,
        string? originalHostGovernanceScope,
        bool allowsHostGovernance,
        IReadOnlyList<RestEndpointOverrideOptions>? overrides)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalCandidateId);
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalOpenApiDocumentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalRouteGroupPrefix);
        ArgumentNullException.ThrowIfNull(originalBindingDescriptors);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalTagName);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalEndpointName);

        if (!allowsHostGovernance || overrides is null || overrides.Count == 0)
        {
            return [];
        }

        return OrderOverrides(overrides.Where(overrideOptions => MatchesOverride(
            sourceModuleId,
            originalCandidateId,
            endpointProjection,
            defaultApiVersionMajor,
            originalOpenApiDocumentName,
            originalRouteGroupPrefix,
            originalBindingDescriptors,
            originalBindingFallbackMode,
            originalTagName,
            originalEndpointName,
            originalHostGovernanceScope,
            allowsHostGovernance,
            overrideOptions)));
    }

    private static string[] ResolveSkippedOverrides(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        IReadOnlyList<RestEndpointOverrideOptions>? overrides)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (candidate.Candidate.OriginalProjection.AllowsHostGovernance ||
            overrides is null ||
            overrides.Count == 0)
        {
            return [];
        }

        var originalProjection = candidate.Candidate.OriginalProjection;
        var endpointProjection = candidate.EffectiveEndpointProjection;
        var sourceModuleId = candidate.Candidate.ProjectedEndpoint.SourceModuleId
            ?? throw new InvalidOperationException(
                $"REST endpoint candidate '{candidate.Candidate.Id}' is missing the source module id required to evaluate skipped governance rules.");
        if (string.IsNullOrWhiteSpace(originalProjection.OpenApiDocumentName) ||
            string.IsNullOrWhiteSpace(originalProjection.RouteGroupPrefix) ||
            string.IsNullOrWhiteSpace(originalProjection.TagName) ||
            string.IsNullOrWhiteSpace(candidate.Candidate.ProjectedEndpoint.OriginalEndpointName))
        {
            return [];
        }

        return OrderOverrides(overrides.Where(overrideOptions => MatchesOverrideSelectors(
                sourceModuleId,
                candidate.Candidate.Id,
                endpointProjection,
                originalProjection.ApiVersionMajor,
                originalProjection.OpenApiDocumentName,
                originalProjection.RouteGroupPrefix,
                originalProjection.BindingDescriptors,
                originalProjection.BindingFallbackMode,
                originalProjection.TagName,
                candidate.Candidate.ProjectedEndpoint.OriginalEndpointName,
                originalProjection.HostGovernanceScope,
                overrideOptions)))
            .Select(static overrideOptions => overrideOptions.Id)
            .ToArray();
    }

    private static RestEndpointGovernanceRuleSelectionBasis? ResolveOverrideSelectionBasis(
        RestEndpointOverrideOptions[] matchedOverrides)
    {
        ArgumentNullException.ThrowIfNull(matchedOverrides);

        if (matchedOverrides.Length == 0)
        {
            return null;
        }

        if (matchedOverrides.Length == 1)
        {
            return RestEndpointGovernanceRuleSelectionBasis.SingleMatch;
        }

        var winner = matchedOverrides[0];
        var runnerUp = matchedOverrides[1];
        return ResolveSelectionBasis(
            winner.CandidateIds.Count > 0,
            winner.CandidateIds.Count,
            CountTargetDimensions(winner),
            winner.BehaviorIds.Count > 0,
            winner.AuthoringStyles.Count,
            CountTargetValues(winner),
            runnerUp.CandidateIds.Count > 0,
            runnerUp.CandidateIds.Count,
            CountTargetDimensions(runnerUp),
            runnerUp.BehaviorIds.Count > 0,
            runnerUp.AuthoringStyles.Count,
            CountTargetValues(runnerUp));
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

    private static void ValidateClearBindingsOverride(
        string overrideId,
        RestBehaviorEndpointProjection endpointProjection,
        string effectivePattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(overrideId);
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentException.ThrowIfNullOrWhiteSpace(effectivePattern);

        var inputType = ResolveBehaviorInputType(endpointProjection.BehaviorType);
        if (inputType is null)
        {
            return;
        }

        var effectiveInputType = Nullable.GetUnderlyingType(inputType) ?? inputType;
        if (IsSimpleInputType(effectiveInputType))
        {
            return;
        }

        var routePlaceholders = ExtractRoutePlaceholders(effectivePattern);
        if (routePlaceholders.Count == 0)
        {
            return;
        }

        var inputProperties = ResolveBehaviorInputProperties(endpointProjection.BehaviorType);
        var unresolvedPlaceholders = routePlaceholders
            .Where(placeholder => !inputProperties.Contains(placeholder))
            .OrderBy(static placeholder => placeholder, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unresolvedPlaceholders.Length == 0)
        {
            return;
        }

        var explicitRouteBindings = endpointProjection.Bindings
            .Where(static binding => binding.Source == BehaviorRestBindingSource.Route)
            .Select(static binding =>
            {
                var sourceName = string.IsNullOrWhiteSpace(binding.Name)
                    ? binding.PropertyName
                    : binding.Name.Trim();
                return $"{binding.PropertyName}->{sourceName}";
            })
            .ToArray();
        var explicitRouteBindingSummary = explicitRouteBindings.Length == 0
            ? "none"
            : string.Join(", ", explicitRouteBindings);
        throw new InvalidOperationException(
            $"REST endpoint override rule '{overrideId}' cannot clear explicit bindings for behavior '{endpointProjection.BehaviorId}' because the effective route pattern '{effectivePattern}' would rely on implicit property-name inference for placeholder(s) '{string.Join("', '", unresolvedPlaceholders)}', but input type '{effectiveInputType.FullName}' does not expose matching property names. The source shorthand candidate currently covers route placeholders through explicit route bindings ({explicitRouteBindingSummary}). Keep explicit Bindings or rename the placeholders to match the input contract before using ClearBindings.");
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

        if (!endpointProjection.PreserveImplicitQueryFallback &&
            endpointProjection.Method is not (RestBehaviorHttpMethod.Post or RestBehaviorHttpMethod.Put or RestBehaviorHttpMethod.Patch))
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

        var inputType = ResolveBehaviorInputType(behaviorType);
        if (inputType is null)
        {
            return [];
        }

        var effectiveInputType = Nullable.GetUnderlyingType(inputType) ?? inputType;
        if (IsSimpleInputType(effectiveInputType))
        {
            return [];
        }

        return effectiveInputType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.CanRead)
            .Select(static property => property.Name.Trim())
            .Where(static propertyName => !string.IsNullOrWhiteSpace(propertyName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static Type? ResolveBehaviorInputType(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        var contractInterface = behaviorType.GetInterfaces()
            .FirstOrDefault(static candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == typeof(IAppBehavior<,>));
        return contractInterface is null
            ? null
            : contractInterface.GetGenericArguments()[0];
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

        return OrderSuppressions(suppressions.Where(suppression => MatchesSuppression(candidate, suppression)));
    }

    private static string[] ResolveSkippedSuppressions(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        IReadOnlyList<RestEndpointSuppressionOptions>? suppressions)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (candidate.Candidate.OriginalProjection.AllowsHostGovernance ||
            suppressions is null ||
            suppressions.Count == 0)
        {
            return [];
        }

        return OrderSuppressions(suppressions.Where(suppression => MatchesSuppressionSelectors(candidate, suppression)))
            .Select(static suppression => suppression.Id)
            .ToArray();
    }

    private static RestEndpointGovernanceRuleSelectionBasis? ResolveSuppressionSelectionBasis(
        RestEndpointSuppressionOptions[] matchedSuppressions)
    {
        ArgumentNullException.ThrowIfNull(matchedSuppressions);

        if (matchedSuppressions.Length == 0)
        {
            return null;
        }

        if (matchedSuppressions.Length == 1)
        {
            return RestEndpointGovernanceRuleSelectionBasis.SingleMatch;
        }

        var winner = matchedSuppressions[0];
        var runnerUp = matchedSuppressions[1];
        return ResolveSelectionBasis(
            winner.CandidateIds.Count > 0,
            winner.CandidateIds.Count,
            CountTargetDimensions(winner),
            winner.BehaviorIds.Count > 0,
            winner.AuthoringStyles.Count,
            CountTargetValues(winner),
            runnerUp.CandidateIds.Count > 0,
            runnerUp.CandidateIds.Count,
            CountTargetDimensions(runnerUp),
            runnerUp.BehaviorIds.Count > 0,
            runnerUp.AuthoringStyles.Count,
            CountTargetValues(runnerUp));
    }

    private static bool MatchesSuppression(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        RestEndpointSuppressionOptions suppression)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(suppression);

        if (!candidate.Candidate.OriginalProjection.AllowsHostGovernance)
        {
            return false;
        }

        return MatchesSuppressionSelectors(candidate, suppression);
    }

    private static bool MatchesSuppressionSelectors(
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

        if (suppression.OpenApiDocumentNames.Count > 0)
        {
            var openApiDocumentName = candidate.Candidate.OriginalProjection.OpenApiDocumentName;
            if (string.IsNullOrWhiteSpace(openApiDocumentName) ||
                !suppression.OpenApiDocumentNames.Contains(openApiDocumentName, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (suppression.TagNames.Count > 0)
        {
            var tagName = candidate.Candidate.OriginalProjection.TagName;
            if (string.IsNullOrWhiteSpace(tagName) ||
                !suppression.TagNames.Contains(tagName, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (suppression.EndpointNames.Count > 0)
        {
            var endpointName = candidate.Candidate.ProjectedEndpoint.OriginalEndpointName;
            if (string.IsNullOrWhiteSpace(endpointName) ||
                !suppression.EndpointNames.Contains(endpointName, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (suppression.HostGovernanceScopes.Count > 0)
        {
            var hostGovernanceScope = candidate.Candidate.OriginalProjection.HostGovernanceScope;
            if (string.IsNullOrWhiteSpace(hostGovernanceScope) ||
                !suppression.HostGovernanceScopes.Contains(hostGovernanceScope, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (suppression.BindingFallbackModes.Count > 0)
        {
            var bindingFallbackMode = candidate.Candidate.OriginalProjection.BindingFallbackMode;
            if (!bindingFallbackMode.HasValue ||
                !suppression.BindingFallbackModes.Contains(bindingFallbackMode.Value))
            {
                return false;
            }
        }

        if (suppression.TargetBindings.Count > 0 &&
            !RestBehaviorBindingDescriptorSetComparer.Equivalent(
                suppression.TargetBindings,
                candidate.Candidate.OriginalProjection.BindingDescriptors))
        {
            return false;
        }

        return true;
    }

    private static bool MatchesOverride(
        string sourceModuleId,
        string originalCandidateId,
        RestBehaviorEndpointProjection endpointProjection,
        int? defaultApiVersionMajor,
        string originalOpenApiDocumentName,
        string originalRouteGroupPrefix,
        IReadOnlyList<RestEndpointBindingDescriptor> originalBindingDescriptors,
        RestEndpointBindingFallbackMode? originalBindingFallbackMode,
        string originalTagName,
        string originalEndpointName,
        string? originalHostGovernanceScope,
        bool allowsHostGovernance,
        RestEndpointOverrideOptions overrideOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalCandidateId);
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalOpenApiDocumentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalRouteGroupPrefix);
        ArgumentNullException.ThrowIfNull(originalBindingDescriptors);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalTagName);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalEndpointName);
        ArgumentNullException.ThrowIfNull(overrideOptions);

        if (!allowsHostGovernance)
        {
            return false;
        }

        return MatchesOverrideSelectors(
            sourceModuleId,
            originalCandidateId,
            endpointProjection,
            defaultApiVersionMajor,
            originalOpenApiDocumentName,
            originalRouteGroupPrefix,
            originalBindingDescriptors,
            originalBindingFallbackMode,
            originalTagName,
            originalEndpointName,
            originalHostGovernanceScope,
            overrideOptions);
    }

    private static bool MatchesOverrideSelectors(
        string sourceModuleId,
        string originalCandidateId,
        RestBehaviorEndpointProjection endpointProjection,
        int? defaultApiVersionMajor,
        string originalOpenApiDocumentName,
        string originalRouteGroupPrefix,
        IReadOnlyList<RestEndpointBindingDescriptor> originalBindingDescriptors,
        RestEndpointBindingFallbackMode? originalBindingFallbackMode,
        string originalTagName,
        string originalEndpointName,
        string? originalHostGovernanceScope,
        RestEndpointOverrideOptions overrideOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalCandidateId);
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalOpenApiDocumentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalRouteGroupPrefix);
        ArgumentNullException.ThrowIfNull(originalBindingDescriptors);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalTagName);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalEndpointName);
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

        if (overrideOptions.OpenApiDocumentNames.Count > 0 &&
            !overrideOptions.OpenApiDocumentNames.Contains(originalOpenApiDocumentName, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (overrideOptions.TagNames.Count > 0 &&
            !overrideOptions.TagNames.Contains(originalTagName, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (overrideOptions.EndpointNames.Count > 0 &&
            !overrideOptions.EndpointNames.Contains(originalEndpointName, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (overrideOptions.HostGovernanceScopes.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(originalHostGovernanceScope) ||
                !overrideOptions.HostGovernanceScopes.Contains(originalHostGovernanceScope, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (overrideOptions.BindingFallbackModes.Count > 0 &&
            (!originalBindingFallbackMode.HasValue ||
             !overrideOptions.BindingFallbackModes.Contains(originalBindingFallbackMode.Value)))
        {
            return false;
        }

        if (overrideOptions.TargetBindings.Count > 0 &&
            !RestBehaviorBindingDescriptorSetComparer.Equivalent(
                overrideOptions.TargetBindings,
                originalBindingDescriptors))
        {
            return false;
        }

        return true;
    }

    private static RestEndpointSuppressionOptions[] OrderSuppressions(
        IEnumerable<RestEndpointSuppressionOptions> suppressions)
    {
        ArgumentNullException.ThrowIfNull(suppressions);

        return suppressions
            .OrderByDescending(static suppression => suppression.CandidateIds.Count > 0)
            .ThenBy(static suppression => suppression.CandidateIds.Count == 0 ? int.MaxValue : suppression.CandidateIds.Count)
            .ThenByDescending(static suppression => CountTargetDimensions(suppression))
            .ThenByDescending(static suppression => suppression.BehaviorIds.Count > 0)
            .ThenBy(static suppression => suppression.AuthoringStyles.Count)
            .ThenBy(static suppression => CountTargetValues(suppression))
            .ThenBy(static suppression => suppression.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static RestEndpointOverrideOptions[] OrderOverrides(
        IEnumerable<RestEndpointOverrideOptions> overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);

        return overrides
            .OrderByDescending(static overrideOptions => overrideOptions.CandidateIds.Count > 0)
            .ThenBy(static overrideOptions => overrideOptions.CandidateIds.Count == 0 ? int.MaxValue : overrideOptions.CandidateIds.Count)
            .ThenByDescending(static overrideOptions => CountTargetDimensions(overrideOptions))
            .ThenByDescending(static overrideOptions => overrideOptions.BehaviorIds.Count > 0)
            .ThenBy(static overrideOptions => overrideOptions.AuthoringStyles.Count)
            .ThenBy(static overrideOptions => CountTargetValues(overrideOptions))
            .ThenBy(static overrideOptions => overrideOptions.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[] NormalizeOrderedIdentifiers(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        var normalized = new List<string>(values.Count);
        var seen = new HashSet<string>(Comparer);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var trimmed = value.Trim();
            if (seen.Add(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized.ToArray();
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

        if (suppression.OpenApiDocumentNames.Count > 0)
        {
            count++;
        }

        if (suppression.TagNames.Count > 0)
        {
            count++;
        }

        if (suppression.EndpointNames.Count > 0)
        {
            count++;
        }

        if (suppression.HostGovernanceScopes.Count > 0)
        {
            count++;
        }

        if (suppression.BindingFallbackModes.Count > 0)
        {
            count++;
        }

        if (suppression.TargetBindings.Count > 0)
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

        if (overrideOptions.OpenApiDocumentNames.Count > 0)
        {
            count++;
        }

        if (overrideOptions.TagNames.Count > 0)
        {
            count++;
        }

        if (overrideOptions.EndpointNames.Count > 0)
        {
            count++;
        }

        if (overrideOptions.HostGovernanceScopes.Count > 0)
        {
            count++;
        }

        if (overrideOptions.BindingFallbackModes.Count > 0)
        {
            count++;
        }

        if (overrideOptions.TargetBindings.Count > 0)
        {
            count++;
        }

        return count;
    }

    private static RestEndpointGovernanceRuleSelectionBasis ResolveSelectionBasis(
        bool winnerTargetsCandidates,
        int winnerCandidateCount,
        int winnerTargetDimensionCount,
        bool winnerTargetsBehaviors,
        int winnerAuthoringStyleCount,
        int winnerTargetValueCount,
        bool runnerUpTargetsCandidates,
        int runnerUpCandidateCount,
        int runnerUpTargetDimensionCount,
        bool runnerUpTargetsBehaviors,
        int runnerUpAuthoringStyleCount,
        int runnerUpTargetValueCount)
    {
        if (winnerTargetsCandidates != runnerUpTargetsCandidates)
        {
            return RestEndpointGovernanceRuleSelectionBasis.CandidateTargeting;
        }

        if (winnerTargetsCandidates &&
            winnerCandidateCount != runnerUpCandidateCount)
        {
            return RestEndpointGovernanceRuleSelectionBasis.NarrowerCandidateSet;
        }

        if (winnerTargetDimensionCount != runnerUpTargetDimensionCount)
        {
            return RestEndpointGovernanceRuleSelectionBasis.MoreTargetDimensions;
        }

        if (winnerTargetsBehaviors != runnerUpTargetsBehaviors)
        {
            return RestEndpointGovernanceRuleSelectionBasis.BehaviorTargeting;
        }

        if (winnerAuthoringStyleCount != runnerUpAuthoringStyleCount)
        {
            return RestEndpointGovernanceRuleSelectionBasis.NarrowerAuthoringStyleScope;
        }

        if (winnerTargetValueCount != runnerUpTargetValueCount)
        {
            return RestEndpointGovernanceRuleSelectionBasis.FewerTargetValues;
        }

        return RestEndpointGovernanceRuleSelectionBasis.StableRuleId;
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
        if (endpointName is null &&
            summary is null &&
            description is null &&
            !selectedOverride.ClearEndpointName &&
            !selectedOverride.ClearSummary &&
            !selectedOverride.ClearDescription)
        {
            return null;
        }

        var endpointNameChanged = selectedOverride.ClearEndpointName
            ? defaultEndpointName is not null
            : endpointName is not null &&
              !string.Equals(endpointName, defaultEndpointName, StringComparison.Ordinal);
        var summaryChanged = selectedOverride.ClearSummary
            ? defaultSummary is not null
            : summary is not null &&
              !string.Equals(summary, defaultSummary, StringComparison.Ordinal);
        var descriptionChanged = selectedOverride.ClearDescription
            ? defaultDescription is not null
            : description is not null &&
              !string.Equals(description, defaultDescription, StringComparison.Ordinal);
        if (!endpointNameChanged && !summaryChanged && !descriptionChanged)
        {
            return null;
        }

        return new AppliedRestEndpointMetadataOverride(
            selectedOverride.Id,
            endpointNameChanged ? endpointName : null,
            summaryChanged ? summary : null,
            descriptionChanged ? description : null,
            endpointNameChanged && selectedOverride.ClearEndpointName,
            summaryChanged && selectedOverride.ClearSummary,
            descriptionChanged && selectedOverride.ClearDescription,
            ResolveAppliedMetadataActionKinds(
                endpointNameChanged,
                selectedOverride.ClearEndpointName,
                summaryChanged,
                selectedOverride.ClearSummary,
                descriptionChanged,
                selectedOverride.ClearDescription));
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
            selectedOverride.ClearRequiredCapability,
            selectedOverride.ClearRequiredCapability
                ? [RestEndpointOverrideActionKind.ClearRequiredCapability]
                : [RestEndpointOverrideActionKind.RequiredCapabilityKey]);
    }

    private static RestEndpointOverrideActionKind[] ResolveAppliedMetadataActionKinds(
        bool endpointNameChanged,
        bool clearEndpointName,
        bool summaryChanged,
        bool clearSummary,
        bool descriptionChanged,
        bool clearDescription)
    {
        var actionKinds = new List<RestEndpointOverrideActionKind>(3);
        if (endpointNameChanged)
        {
            actionKinds.Add(clearEndpointName
                ? RestEndpointOverrideActionKind.ClearEndpointName
                : RestEndpointOverrideActionKind.EndpointName);
        }

        if (summaryChanged)
        {
            actionKinds.Add(clearSummary
                ? RestEndpointOverrideActionKind.ClearSummary
                : RestEndpointOverrideActionKind.Summary);
        }

        if (descriptionChanged)
        {
            actionKinds.Add(clearDescription
                ? RestEndpointOverrideActionKind.ClearDescription
                : RestEndpointOverrideActionKind.Description);
        }

        return actionKinds
            .Distinct()
            .OrderBy(static actionKind => actionKind)
            .ToArray();
    }

    private static RestEndpointOverrideActionKind[] ResolveAppliedActionKinds(
        IReadOnlyList<RestEndpointOverrideActionKind>? structuralActionKinds,
        IReadOnlyList<RestEndpointOverrideActionKind>? metadataActionKinds,
        IReadOnlyList<RestEndpointOverrideActionKind>? capabilityActionKinds)
    {
        return (structuralActionKinds ?? [])
            .Concat(metadataActionKinds ?? [])
            .Concat(capabilityActionKinds ?? [])
            .Distinct()
            .OrderBy(static actionKind => actionKind)
            .ToArray();
    }

    private static RestEndpointOverrideActionKind[] ResolveBindingActionKinds(RestEndpointOverrideOptions overrideOptions)
    {
        ArgumentNullException.ThrowIfNull(overrideOptions);

        return overrideOptions.ActionKinds
            .Where(static actionKind =>
                actionKind == RestEndpointOverrideActionKind.ReplaceBindings ||
                actionKind == RestEndpointOverrideActionKind.MergeBindings ||
                actionKind == RestEndpointOverrideActionKind.RemoveBindingProperties ||
                actionKind == RestEndpointOverrideActionKind.ClearBindings)
            .ToArray();
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
               suppression.RouteGroupPrefixes.Count +
               suppression.OpenApiDocumentNames.Count +
               suppression.TagNames.Count +
               suppression.EndpointNames.Count +
               suppression.HostGovernanceScopes.Count +
               suppression.BindingFallbackModes.Count +
               suppression.TargetBindings.Count;
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
               overrideOptions.RouteGroupPrefixes.Count +
               overrideOptions.OpenApiDocumentNames.Count +
               overrideOptions.TagNames.Count +
               overrideOptions.EndpointNames.Count +
               overrideOptions.HostGovernanceScopes.Count +
               overrideOptions.BindingFallbackModes.Count +
               overrideOptions.TargetBindings.Count;
    }

    private static string? NormalizeOverrideMetadataValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? ResolveEffectiveMetadataValue(
        string? overrideValue,
        bool clearValue,
        string? defaultValue)
    {
        if (clearValue)
        {
            return null;
        }

        return NormalizeOverrideMetadataValue(overrideValue) ?? defaultValue;
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

    private static string ResolveGroupOpenApiDocumentName(
        RestBehaviorRouteGroupProjection group,
        int? apiVersionMajor)
    {
        ArgumentNullException.ThrowIfNull(group);

        if (!string.IsNullOrWhiteSpace(group.OpenApiDocumentName))
        {
            return group.OpenApiDocumentName.Trim();
        }

        return ResolveOpenApiDocumentName(apiVersionMajor);
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
        string openApiDocumentName,
        IReadOnlyList<RestEndpointBindingDescriptor> bindingDescriptors,
        RestEndpointBindingFallbackMode? bindingFallbackMode,
        string tagName,
        bool allowsHostGovernance,
        string? hostGovernanceScope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(routeGroupPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(openApiDocumentName);
        ArgumentNullException.ThrowIfNull(bindingDescriptors);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);

        return new RestEndpointCandidateProjectionDescriptor(
            method,
            RestEndpointRuntimeDescriptorFactory.CombinePaths(routeGroupPrefix, relativePattern),
            routeGroupPrefix,
            relativePattern,
            apiVersionMajor,
            openApiDocumentName,
            bindingDescriptors,
            bindingFallbackMode,
            tagName,
            allowsHostGovernance,
            hostGovernanceScope);
    }

    private static bool AllowsHostGovernance(
        RestBehaviorRouteGroupProjection group,
        RestBehaviorEndpointProjection endpointProjection)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(endpointProjection);

        return endpointProjection.AuthoringStyle switch
        {
            RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle => true,
            RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle => true,
            RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle => group.AllowHostGovernance,
            _ => false
        };
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
    string EffectiveOpenApiDocumentName,
    string? EffectiveRouteGroupPrefix,
    string EffectiveTagName,
    IReadOnlyList<RestEndpointOverrideActionKind> ActionKinds);

internal sealed record AppliedRestEndpointMetadataOverride(
    string OverrideId,
    string? EndpointName,
    string? Summary,
    string? Description,
    bool ClearEndpointName,
    bool ClearSummary,
    bool ClearDescription,
    IReadOnlyList<RestEndpointOverrideActionKind> ActionKinds);

internal sealed record AppliedRestEndpointCapabilityOverride(
    string OverrideId,
    string? RequiredCapabilityKey,
    bool ClearRequiredCapability,
    IReadOnlyList<RestEndpointOverrideActionKind> ActionKinds);

internal sealed record ResolvedRestEndpointAuthoringPolicySuppression(
    string CandidateId,
    RestEndpointAuthoringPolicySuppressionKind Kind,
    string SuppressionReason);

internal sealed record ResolvedRestEndpointOverrideDecision(
    IReadOnlyList<string> MatchedOverrideIds,
    RestEndpointOverrideOptions? SelectedOverride,
    RestEndpointGovernanceRuleSelectionBasis? SelectionBasis,
    AppliedRestEndpointOverride? AppliedOverride);
