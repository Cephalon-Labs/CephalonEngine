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
                var sourceCapabilityCapture = CaptureSourceCapability(builder);
                ApplyRequiredCapabilityOverride(builder, candidate.AppliedCapabilityOverride);
                ApplyEndpointMetadataOverride(builder, candidate.AppliedMetadataOverride);
                ApplyPublishedOverrideProvenance(builder, candidate, sourceCapabilityCapture);
            }
        }
    }

    private static CapturedEndpointCapabilityState CaptureSourceCapability(RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var capture = new CapturedEndpointCapabilityState();
        builder.Add(endpointBuilder =>
        {
            var sourceRequiredCapabilityKey = RestEndpointRuntimeMetadata.ResolveEffectiveRequiredCapabilityKey(
                endpointBuilder.Metadata.OfType<RestEndpointCapabilityMetadata>());
            capture.RequiredCapabilityKey = sourceRequiredCapabilityKey;
            endpointBuilder.Metadata.Add(new RestEndpointSourceCapabilityMetadata(sourceRequiredCapabilityKey));
        });

        return capture;
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

        if (capabilityOverride.ClearRequiredCapability)
        {
            builder.ClearRequiredCapability();
            return;
        }

        builder.RequireCapability(capabilityOverride.RequiredCapabilityKey!);
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

    private static void ApplyPublishedOverrideProvenance(
        RouteHandlerBuilder builder,
        ResolvedRestBehaviorEndpointProjectionCandidate candidate,
        CapturedEndpointCapabilityState sourceCapabilityCapture)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(sourceCapabilityCapture);

        if (string.IsNullOrWhiteSpace(candidate.Candidate.AppliedOverrideId))
        {
            return;
        }

        if (HasStructuralOverride(candidate.Candidate) || candidate.AppliedMetadataOverride is not null)
        {
            builder.WithMetadata(new RestEndpointAppliedOverrideMetadata(candidate.Candidate.AppliedOverrideId));
            return;
        }

        if (candidate.AppliedCapabilityOverride is null)
        {
            return;
        }

        builder.Add(endpointBuilder =>
        {
            var effectiveRequiredCapabilityKey = RestEndpointRuntimeMetadata.ResolveEffectiveRequiredCapabilityKey(
                endpointBuilder.Metadata.OfType<RestEndpointCapabilityMetadata>());
            if (!string.Equals(
                    sourceCapabilityCapture.RequiredCapabilityKey,
                    effectiveRequiredCapabilityKey,
                    StringComparison.Ordinal))
            {
                endpointBuilder.Metadata.Add(new RestEndpointAppliedOverrideMetadata(candidate.Candidate.AppliedOverrideId));
            }
        });
    }

    private static bool HasStructuralOverride(RestEndpointCandidateRuntimeDescriptor candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var originalProjection = candidate.OriginalProjection;
        var projectedEndpoint = candidate.ProjectedEndpoint;
        return !string.Equals(originalProjection.Method, projectedEndpoint.Method, StringComparison.Ordinal) ||
               !string.Equals(originalProjection.RoutePattern, projectedEndpoint.RoutePattern, StringComparison.Ordinal) ||
               !string.Equals(originalProjection.RouteGroupPrefix, projectedEndpoint.RouteGroupPrefix, StringComparison.Ordinal) ||
               !string.Equals(originalProjection.RelativePattern, projectedEndpoint.RelativePattern, StringComparison.Ordinal) ||
               originalProjection.ApiVersionMajor != projectedEndpoint.ApiVersionMajor ||
               !string.Equals(originalProjection.OpenApiDocumentName, projectedEndpoint.OpenApiDocumentName, StringComparison.Ordinal) ||
               originalProjection.BindingFallbackMode != projectedEndpoint.BindingFallbackMode ||
               !BindingDescriptorsMatch(originalProjection.BindingDescriptors, projectedEndpoint.BindingDescriptors);
    }

    private static bool BindingDescriptorsMatch(
        IReadOnlyList<RestEndpointBindingDescriptor> originalBindings,
        IReadOnlyList<RestEndpointBindingDescriptor> projectedBindings)
    {
        ArgumentNullException.ThrowIfNull(originalBindings);
        ArgumentNullException.ThrowIfNull(projectedBindings);

        if (originalBindings.Count != projectedBindings.Count)
        {
            return false;
        }

        for (var index = 0; index < originalBindings.Count; index++)
        {
            var original = originalBindings[index];
            var projected = projectedBindings[index];
            if (!string.Equals(original.PropertyName, projected.PropertyName, StringComparison.Ordinal) ||
                original.Source != projected.Source ||
                !string.Equals(original.Name, projected.Name, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
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

    private sealed class CapturedEndpointCapabilityState
    {
        internal string? RequiredCapabilityKey { get; set; }
    }
}
