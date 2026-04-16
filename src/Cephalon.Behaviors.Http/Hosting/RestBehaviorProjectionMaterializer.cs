using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class RestBehaviorProjectionMaterializer
{
    internal static void MapModule(
        IEndpointRouteBuilder endpoints,
        IModule module,
        RestBehaviorModuleProjection projection)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(projection);

        var configuration = endpoints.ServiceProvider.GetRequiredService<IConfiguration>();
        var apiRoutesOptions = ApiRoutesOptions.FromConfiguration(configuration);
        var candidateRegistry = endpoints.ServiceProvider.GetService<IRestEndpointCandidateRuntimeRegistry>();
        var governanceOptions = endpoints.ServiceProvider.GetService<RestApiGovernanceOptions>()
            ?? RestApiGovernanceOptions.FromConfiguration(configuration);
        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            module.Descriptor,
            apiRoutesOptions,
            projection.Groups,
            governanceOptions.Suppressions,
            governanceOptions.Overrides);

        foreach (var candidate in candidates)
        {
            candidateRegistry?.Register(candidate.Candidate);
        }

        for (var groupIndex = 0; groupIndex < projection.Groups.Count; groupIndex++)
        {
            var publishedCandidates = candidates
                .Where(candidate =>
                    candidate.GroupIndex == groupIndex &&
                    candidate.Candidate.Status == RestEndpointCandidateStatus.Published)
                .ToArray();
            if (publishedCandidates.Length == 0)
            {
                continue;
            }

            MapGroup(endpoints, module, projection.Groups[groupIndex], publishedCandidates, apiRoutesOptions);
        }
    }

    internal static void MapGroup(
        IEndpointRouteBuilder endpoints,
        IModule module,
        RestBehaviorRouteGroupProjection projection,
        IReadOnlyList<ResolvedRestBehaviorEndpointProjectionCandidate> publishedCandidates,
        ApiRoutesOptions apiRoutesOptions)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(publishedCandidates);
        ArgumentNullException.ThrowIfNull(apiRoutesOptions);

        foreach (var routeGroup in publishedCandidates
                     .GroupBy(candidate => ResolvePublishedRouteGroupPrefix(candidate.Candidate))
                     .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var firstCandidate = routeGroup.First();
            var group = endpoints.MapBehaviorRestGroup(
                module,
                ResolveMaterializationGroupPrefix(routeGroup.Key, apiRoutesOptions.RestPrefix));
            group.UseRuntimeSourceKind(RestEndpointRuntimeMetadata.ModuleDslSourceKind);
            group.UseRuntimeAuthoringStyle(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle);
            if (!string.IsNullOrWhiteSpace(projection.TagName))
            {
                group.WithTagName(projection.TagName);
            }

            if (projection.HasExplicitTagDescription)
            {
                group.WithTagDescription(projection.TagDescription);
            }

            if (firstCandidate.Candidate.ProjectedEndpoint.ApiVersionMajor is int apiVersionMajor)
            {
                group.ApiVersion(apiVersionMajor);
            }

            foreach (var convention in projection.GroupConventions)
            {
                convention(group.Routes);
            }

            foreach (var candidate in routeGroup)
            {
                group.UseRuntimeCandidateId(candidate.Candidate.Id);
                var builder = candidate.EffectiveEndpointProjection.Apply(group);
                ApplyRequiredCapabilityOverride(builder, candidate.AppliedCapabilityOverride);
                ApplyEndpointMetadataOverride(builder, candidate.AppliedMetadataOverride);
            }
        }
    }

    private static void ApplyRequiredCapabilityOverride(
        RouteHandlerBuilder builder,
        AppliedRestEndpointCapabilityOverride? capabilityOverride)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (capabilityOverride is null)
        {
            return;
        }

        builder.RequireCapability(capabilityOverride.RequiredCapabilityKey);
    }

    private static void ApplyEndpointMetadataOverride(
        RouteHandlerBuilder builder,
        AppliedRestEndpointMetadataOverride? metadataOverride)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (metadataOverride is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(metadataOverride.EndpointName))
        {
            builder.WithName(metadataOverride.EndpointName);
        }

        if (!string.IsNullOrWhiteSpace(metadataOverride.Summary))
        {
            builder.WithSummary(metadataOverride.Summary);
        }

        if (!string.IsNullOrWhiteSpace(metadataOverride.Description))
        {
            builder.WithDescription(metadataOverride.Description);
        }
    }

    private static string ResolvePublishedRouteGroupPrefix(RestEndpointCandidateRuntimeDescriptor candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (!string.IsNullOrWhiteSpace(candidate.ProjectedEndpoint.RouteGroupPrefix))
        {
            return candidate.ProjectedEndpoint.RouteGroupPrefix.Trim();
        }

        throw new InvalidOperationException(
            $"REST endpoint candidate '{candidate.Id}' is missing the projected route-group prefix required for endpoint materialization.");
    }

    private static string ResolveMaterializationGroupPrefix(
        string publishedRouteGroupPrefix,
        string restPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publishedRouteGroupPrefix);
        ArgumentNullException.ThrowIfNull(restPrefix);

        var normalizedPublishedPrefix = NormalizePath(publishedRouteGroupPrefix);
        var normalizedRestPrefix = NormalizePath(restPrefix);
        if (string.Equals(normalizedRestPrefix, "/", StringComparison.Ordinal))
        {
            return normalizedPublishedPrefix;
        }

        if (string.Equals(normalizedPublishedPrefix, normalizedRestPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return "/";
        }

        if (!normalizedPublishedPrefix.StartsWith($"{normalizedRestPrefix}/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"REST endpoint materialization cannot place projected route-group prefix '{normalizedPublishedPrefix}' beneath active REST root '{normalizedRestPrefix}'.");
        }

        return normalizedPublishedPrefix[normalizedRestPrefix.Length..];
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalized = path.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        return normalized.Length > 1
            ? normalized.TrimEnd('/')
            : normalized;
    }
}
