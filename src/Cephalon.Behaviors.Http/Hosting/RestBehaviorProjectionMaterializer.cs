using Cephalon.Abstractions.Transports;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

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

        var configuration = endpoints.ServiceProvider.GetService(typeof(IConfiguration)) as IConfiguration;
        var apiRoutesOptions = configuration is null
            ? new ApiRoutesOptions()
            : ApiRoutesOptions.FromConfiguration(configuration);
        var registry = endpoints.ServiceProvider.GetService(typeof(IRestEndpointRuntimeRegistry)) as IRestEndpointRuntimeRegistry;

        foreach (var groupProjection in projection.Groups)
        {
            MapGroup(endpoints, module, groupProjection, apiRoutesOptions, registry);
        }
    }

    internal static void MapGroup(
        IEndpointRouteBuilder endpoints,
        IModule module,
        RestBehaviorRouteGroupProjection projection,
        ApiRoutesOptions apiRoutesOptions,
        IRestEndpointRuntimeRegistry? registry)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(apiRoutesOptions);

        var group = endpoints.MapBehaviorRestGroup(module, projection.Prefix);
        if (!string.IsNullOrWhiteSpace(projection.TagName))
        {
            group.WithTagName(projection.TagName);
        }

        if (projection.HasExplicitTagDescription)
        {
            group.WithTagDescription(projection.TagDescription);
        }

        if (projection.HasExplicitApiVersion && projection.ApiVersionMajor.HasValue)
        {
            group.ApiVersion(projection.ApiVersionMajor.Value);
        }

        foreach (var convention in projection.GroupConventions)
        {
            convention(group.Routes);
        }

        foreach (var endpointProjection in projection.Endpoints)
        {
            registry?.Register(CreateRuntimeDescriptor(
                apiRoutesOptions,
                module,
                group,
                endpointProjection));
            endpointProjection.Apply(group);
        }
    }

    private static RestEndpointRuntimeDescriptor CreateRuntimeDescriptor(
        ApiRoutesOptions apiRoutesOptions,
        IModule module,
        BehaviorRestEndpointGroup group,
        RestBehaviorEndpointProjection projection)
    {
        ArgumentNullException.ThrowIfNull(apiRoutesOptions);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(projection);

        var finalRoutePattern = CombinePaths(
            apiRoutesOptions.RestPrefix,
            group.ResolvedRoutePrefix,
            projection.Pattern);
        var routeGroupPrefix = CombinePaths(apiRoutesOptions.RestPrefix, group.ResolvedRoutePrefix);
        var operationVersionMajor = group.ApiVersionMajor ?? group.ModuleVersionMajor;
        var endpointName = BuildOperationName(module.Descriptor.Id, operationVersionMajor, projection.BehaviorId);
        var endpointId = BuildEndpointId(
            module.Descriptor.Id,
            projection.BehaviorId,
            projection.Method,
            finalRoutePattern);
        var xmlSummary = BehaviorXmlDocumentation.GetSummary(projection.BehaviorType);
        var summary = xmlSummary ?? projection.BehaviorId;
        var description = BehaviorXmlDocumentation.GetDescription(projection.BehaviorType);
        if (string.IsNullOrWhiteSpace(description) && string.IsNullOrWhiteSpace(xmlSummary))
        {
            description = module.Descriptor.Description;
        }

        return new RestEndpointRuntimeDescriptor(
            id: endpointId,
            transportId: "rest-api",
            sourceKind: "module-dsl",
            method: projection.Method.ToString(),
            routePattern: finalRoutePattern,
            sourceModuleId: module.Descriptor.Id,
            sourceModuleVersion: module.Descriptor.Version,
            sourceModuleVersionMajor: group.ModuleVersionMajor,
            endpointName: endpointName,
            openApiDocumentName: group.OpenApiDocumentName,
            apiVersionMajor: operationVersionMajor,
            tags: [group.TagName],
            summary: summary,
            description: description,
            behaviorId: projection.BehaviorId,
            metadata: CreateMetadata(projection, routeGroupPrefix));
    }

    private static Dictionary<string, string> CreateMetadata(
        RestBehaviorEndpointProjection projection,
        string routeGroupPrefix)
    {
        var httpMethod = projection.Method.ToString().ToUpperInvariant();

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["method"] = httpMethod,
            ["behaviorType"] = projection.BehaviorType.FullName ?? projection.BehaviorType.Name,
            ["routeGroupPrefix"] = routeGroupPrefix,
            ["relativePattern"] = projection.Pattern,
            ["sourceId"] = $"{projection.BehaviorId}:{httpMethod}:{projection.Pattern}"
        };
    }

    private static string BuildEndpointId(
        string moduleId,
        string behaviorId,
        RestBehaviorHttpMethod method,
        string routePattern)
    {
        var normalized = string.Concat($"{moduleId}:{behaviorId}:{method}:{routePattern}".Select(static ch =>
            char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_'));

        return string.IsNullOrWhiteSpace(normalized)
            ? "rest_endpoint"
            : normalized;
    }

    private static string BuildOperationName(string moduleId, int? operationVersionMajor, string behaviorId)
    {
        var versionSegment = operationVersionMajor.HasValue
            ? $"v{operationVersionMajor.Value}"
            : "v0";
        return $"{NormalizeSegment(moduleId)}.{versionSegment}.{NormalizeSegment(behaviorId)}";
    }

    private static string NormalizeSegment(string value)
    {
        return string.Concat(value.Select(static ch =>
            char.IsLetterOrDigit(ch) ? ch : '_'));
    }

    private static string CombinePaths(params string?[] segments)
    {
        var normalizedSegments = segments
            .Where(static segment => !string.IsNullOrWhiteSpace(segment))
            .Select(static segment => segment!.Trim())
            .Select(static segment => segment.Trim('/'))
            .Where(static segment => segment.Length > 0)
            .ToArray();

        if (normalizedSegments.Length == 0)
        {
            return "/";
        }

        return "/" + string.Join("/", normalizedSegments);
    }
}
