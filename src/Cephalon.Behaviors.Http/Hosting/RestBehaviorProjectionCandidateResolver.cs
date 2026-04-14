using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class RestBehaviorProjectionCandidateResolver
{
    internal static IReadOnlyList<ResolvedRestBehaviorEndpointProjectionCandidate> ResolveCandidates(
        ModuleDescriptor moduleDescriptor,
        ApiRoutesOptions apiRoutesOptions,
        IReadOnlyList<RestBehaviorRouteGroupProjection> groups)
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
                groupIndex))
            .ToArray();

        if (candidates.Length == 0)
        {
            return [];
        }

        var winnersByBehavior = candidates
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
            .Select(candidate => ResolvePublication(candidate, winnersByBehavior))
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
        int groupIndex)
    {
        ArgumentNullException.ThrowIfNull(moduleDescriptor);
        ArgumentNullException.ThrowIfNull(apiRoutesOptions);
        ArgumentNullException.ThrowIfNull(group);

        if (group.Endpoints.Count == 0)
        {
            return [];
        }

        var effectiveApiVersionMajor = group.ApiVersionMajor ?? moduleVersionMajor;
        var resolvedRouteGroupPrefix = ResolveRouteGroupPrefix(group.Prefix, effectiveApiVersionMajor);
        var publishedRouteGroupPrefix = RestEndpointRuntimeDescriptorFactory.CombinePaths(
            apiRoutesOptions.RestPrefix,
            resolvedRouteGroupPrefix);
        var openApiDocumentName = ResolveOpenApiDocumentName(effectiveApiVersionMajor);
        var tagName = string.IsNullOrWhiteSpace(group.TagName)
            ? moduleDescriptor.DisplayName
            : group.TagName.Trim();

        return group.Endpoints
            .Select(endpoint => CreateCandidate(
                moduleDescriptor,
                groupIndex,
                endpoint,
                tagName,
                openApiDocumentName,
                effectiveApiVersionMajor,
                publishedRouteGroupPrefix))
            .ToArray();
    }

    private static ResolvedRestBehaviorEndpointProjectionCandidate CreateCandidate(
        ModuleDescriptor moduleDescriptor,
        int groupIndex,
        RestBehaviorEndpointProjection endpointProjection,
        string tagName,
        string openApiDocumentName,
        int? apiVersionMajor,
        string publishedRouteGroupPrefix)
    {
        ArgumentNullException.ThrowIfNull(moduleDescriptor);
        ArgumentNullException.ThrowIfNull(endpointProjection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
        ArgumentException.ThrowIfNullOrWhiteSpace(openApiDocumentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(publishedRouteGroupPrefix);

        var method = endpointProjection.Method.ToString().ToUpperInvariant();
        var routePattern = RestEndpointRuntimeDescriptorFactory.CombinePaths(
            publishedRouteGroupPrefix,
            endpointProjection.Pattern);
        var runtimeBindings = RestEndpointBindingDescriptorAdapter.ToRuntimeDescriptors(endpointProjection.Bindings);
        var projectedEndpoint = RestEndpointRuntimeDescriptorFactory.CreateBehaviorDescriptor(
            sourceKind: RestEndpointRuntimeMetadata.ModuleDslSourceKind,
            method: method,
            routePattern: routePattern,
            sourceModuleId: moduleDescriptor.Id,
            sourceModuleVersion: moduleDescriptor.Version,
            sourceModuleVersionMajor: ResolveModuleMajorVersion(moduleDescriptor.Version),
            behaviorId: endpointProjection.BehaviorId,
            endpointName: null,
            openApiDocumentName: openApiDocumentName,
            apiVersionMajor: apiVersionMajor,
            tags: [tagName],
            summary: null,
            description: null,
            authoringStyle: endpointProjection.AuthoringStyle,
            behaviorType: endpointProjection.BehaviorType.FullName ?? endpointProjection.BehaviorType.Name,
            routeGroupPrefix: publishedRouteGroupPrefix,
            relativePattern: endpointProjection.Pattern,
            bindingDescriptors: runtimeBindings);
        var candidateId = RestEndpointRuntimeDescriptorFactory.BuildEndpointId(
            $"{moduleDescriptor.Id}:{endpointProjection.BehaviorId}:{endpointProjection.AuthoringStyle}:{method}:{routePattern}");
        var precedenceRank = RestEndpointRuntimeMetadata.ResolvePrecedenceRank(endpointProjection.AuthoringStyle);

        return new ResolvedRestBehaviorEndpointProjectionCandidate(
            groupIndex,
            endpointProjection,
            new RestEndpointCandidateRuntimeDescriptor(
                candidateId,
                projectedEndpoint,
                endpointProjection.AuthoringStyle,
                precedenceRank,
                RestEndpointCandidateStatus.Published));
    }

    private static ResolvedRestBehaviorEndpointProjectionCandidate ResolvePublication(
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        Dictionary<string, ResolvedRestBehaviorEndpointProjectionCandidate[]> winnersByBehavior)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(winnersByBehavior);

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
                suppressionReason: $"Suppressed because behavior '{behaviorId}' is also mapped by higher-precedence authoring style '{winningCandidate.AuthoringStyle}'.")
        };
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
    RestBehaviorEndpointProjection EndpointProjection,
    RestEndpointCandidateRuntimeDescriptor Candidate);
