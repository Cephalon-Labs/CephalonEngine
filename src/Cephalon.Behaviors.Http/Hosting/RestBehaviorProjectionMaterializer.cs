using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;
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
        var candidateRegistry = endpoints.ServiceProvider.GetService<IRestEndpointCandidateRuntimeRegistry>();
        var governanceOptions = endpoints.ServiceProvider.GetService<RestApiGovernanceOptions>()
            ?? RestApiGovernanceOptions.FromConfiguration(configuration);
        var candidates = RestBehaviorProjectionCandidateResolver.ResolveCandidates(
            module.Descriptor,
            ApiRoutesOptions.FromConfiguration(configuration),
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

            MapGroup(endpoints, module, projection.Groups[groupIndex], publishedCandidates);
        }
    }

    internal static void MapGroup(
        IEndpointRouteBuilder endpoints,
        IModule module,
        RestBehaviorRouteGroupProjection projection,
        IReadOnlyList<ResolvedRestBehaviorEndpointProjectionCandidate> publishedCandidates)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(publishedCandidates);

        foreach (var versionGroup in publishedCandidates
                     .GroupBy(static candidate => candidate.Candidate.ProjectedEndpoint.ApiVersionMajor)
                     .OrderBy(static group => group.Key ?? int.MinValue))
        {
            var group = endpoints.MapBehaviorRestGroup(module, projection.Prefix);
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

            if (versionGroup.Key.HasValue)
            {
                group.ApiVersion(versionGroup.Key.Value);
            }

            foreach (var convention in projection.GroupConventions)
            {
                convention(group.Routes);
            }

            foreach (var endpointProjection in versionGroup.Select(static candidate => candidate.EndpointProjection))
            {
                endpointProjection.Apply(group);
            }
        }
    }
}
