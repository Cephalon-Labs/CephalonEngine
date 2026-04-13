using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.AspNetCore.Transports.Rest;

internal static class RestEndpointRuntimeMaterializer
{
    internal static void RegisterModuleOwnedEndpoints(
        IEndpointRouteBuilder endpoints,
        ApiRoutesOptions apiRoutesOptions,
        IRestEndpointRuntimeRegistry? registry)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(apiRoutesOptions);

        if (registry is null)
        {
            return;
        }

        foreach (var routeEndpoint in endpoints.DataSources
                     .SelectMany(static dataSource => dataSource.Endpoints)
                     .OfType<RouteEndpoint>()
                     .Select(endpoint => new
                     {
                         Endpoint = endpoint,
                         Module = endpoint.Metadata.GetMetadata<RestModuleEndpointMetadata>()
                     })
                     .Where(static entry => entry.Module is not null)
                     .OrderBy(static entry => NormalizeRoutePattern(entry.Endpoint), StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static entry => entry.Module!.ModuleId, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static entry => entry.Endpoint.DisplayName ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            var methods = ResolveHttpMethods(routeEndpoint.Endpoint);
            foreach (var method in methods)
            {
                registry.Register(CreateDescriptor(
                    routeEndpoint.Endpoint,
                    routeEndpoint.Module!,
                    method,
                    apiRoutesOptions));
            }
        }
    }

    private static RestEndpointRuntimeDescriptor CreateDescriptor(
        RouteEndpoint endpoint,
        RestModuleEndpointMetadata moduleMetadata,
        string method,
        ApiRoutesOptions apiRoutesOptions)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(moduleMetadata);
        ArgumentNullException.ThrowIfNull(apiRoutesOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        var routePattern = NormalizeRoutePattern(endpoint);
        var behaviorMetadata = endpoint.Metadata.GetMetadata<RestBehaviorEndpointMetadata>();
        var endpointName = behaviorMetadata?.OperationName
            ?? endpoint.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName;
        var openApiDocumentName = behaviorMetadata?.OpenApiDocumentName
            ?? endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName;
        var apiVersionMajor = behaviorMetadata?.ApiVersionMajor
            ?? TryParseApiVersionMajor(openApiDocumentName);
        var summary = behaviorMetadata?.Summary
            ?? endpoint.Metadata.OfType<IEndpointSummaryMetadata>().LastOrDefault()?.Summary;
        var description = behaviorMetadata?.Description
            ?? endpoint.Metadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault()?.Description;
        var sourceKind = behaviorMetadata?.SourceKind
            ?? RestEndpointRuntimeMetadata.ManualSourceKind;
        var tags = behaviorMetadata is null
            ? ResolveTags(endpoint)
            : [behaviorMetadata.TagName];
        var endpointId = behaviorMetadata is null
            ? BuildManualEndpointId(moduleMetadata.ModuleId, endpointName, method, routePattern)
            : BuildBehaviorEndpointId(moduleMetadata.ModuleId, behaviorMetadata.BehaviorId, method, routePattern);

        return new RestEndpointRuntimeDescriptor(
            id: endpointId,
            transportId: "rest-api",
            sourceKind: sourceKind,
            method: method,
            routePattern: routePattern,
            sourceModuleId: moduleMetadata.ModuleId,
            sourceModuleVersion: moduleMetadata.Version,
            sourceModuleVersionMajor: moduleMetadata.MajorVersion,
            behaviorId: behaviorMetadata?.BehaviorId,
            endpointName: endpointName,
            openApiDocumentName: openApiDocumentName,
            apiVersionMajor: apiVersionMajor,
            tags: tags,
            summary: summary,
            description: description,
            metadata: CreateMetadata(endpoint, moduleMetadata, behaviorMetadata, method, routePattern, apiRoutesOptions));
    }

    private static string[] ResolveHttpMethods(RouteEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var httpMethods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
        if (httpMethods is null)
        {
            return [];
        }

        return httpMethods
            .Where(static method => !string.IsNullOrWhiteSpace(method))
            .Select(static method => method.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static method => method, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[] ResolveTags(RouteEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return endpoint.Metadata
            .OfType<ITagsMetadata>()
            .SelectMany(static metadata => metadata.Tags)
            .Where(static tag => !string.IsNullOrWhiteSpace(tag))
            .Select(static tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Dictionary<string, string> CreateMetadata(
        RouteEndpoint endpoint,
        RestModuleEndpointMetadata moduleMetadata,
        RestBehaviorEndpointMetadata? behaviorMetadata,
        string method,
        string routePattern,
        ApiRoutesOptions apiRoutesOptions)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(moduleMetadata);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(routePattern);
        ArgumentNullException.ThrowIfNull(apiRoutesOptions);

        var normalizedMethod = method.Trim().ToUpperInvariant();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["method"] = normalizedMethod
        };

        if (behaviorMetadata is not null)
        {
            metadata["authoringStyle"] = string.Equals(
                behaviorMetadata.SourceKind,
                RestEndpointRuntimeMetadata.ModuleDslSourceKind,
                StringComparison.OrdinalIgnoreCase)
                ? "behavior-module-dsl"
                : "behavior-helper";
            metadata["behaviorType"] = behaviorMetadata.BehaviorType;
            metadata["routeGroupPrefix"] = CombinePaths(apiRoutesOptions.RestPrefix, behaviorMetadata.RouteGroupPrefix);
            metadata["relativePattern"] = behaviorMetadata.RelativePattern;
            metadata["sourceId"] = $"{behaviorMetadata.BehaviorId}:{normalizedMethod}:{behaviorMetadata.RelativePattern}";
            return metadata;
        }

        metadata["authoringStyle"] = "minimal-api";
        metadata["sourceId"] = $"{moduleMetadata.ModuleId}:{normalizedMethod}:{routePattern}";
        if (!string.IsNullOrWhiteSpace(endpoint.DisplayName))
        {
            metadata["endpointDisplayName"] = endpoint.DisplayName!;
        }

        return metadata;
    }

    private static int? TryParseApiVersionMajor(string? openApiDocumentName)
    {
        if (string.IsNullOrWhiteSpace(openApiDocumentName))
        {
            return null;
        }

        var normalized = openApiDocumentName.Trim();
        return normalized.Length > 1 &&
               normalized.StartsWith('v') &&
               int.TryParse(normalized[1..], out var parsedVersion)
            ? parsedVersion
            : null;
    }

    private static string NormalizeRoutePattern(RouteEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var rawText = endpoint.RoutePattern.RawText ?? endpoint.RoutePattern.ToString();
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return "/";
        }

        var normalized = rawText.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        return normalized.Length > 1
            ? normalized.TrimEnd('/')
            : normalized;
    }

    private static string BuildBehaviorEndpointId(
        string moduleId,
        string behaviorId,
        string method,
        string routePattern)
    {
        return BuildEndpointId($"{moduleId}:{behaviorId}:{method}:{routePattern}");
    }

    private static string BuildManualEndpointId(
        string moduleId,
        string? endpointName,
        string method,
        string routePattern)
    {
        var identity = string.IsNullOrWhiteSpace(endpointName)
            ? $"{moduleId}:{method}:{routePattern}"
            : $"{moduleId}:{endpointName}:{method}:{routePattern}";
        return BuildEndpointId(identity);
    }

    private static string BuildEndpointId(string value)
    {
        var normalized = string.Concat(value.Select(static ch =>
            char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_'));

        return string.IsNullOrWhiteSpace(normalized)
            ? "rest_endpoint"
            : normalized;
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

    internal static RestModuleEndpointMetadata CreateModuleMetadata(IModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        return CreateModuleMetadata(module.Descriptor);
    }

    internal static RestModuleEndpointMetadata CreateModuleMetadata(ModuleDescriptor moduleDescriptor)
    {
        ArgumentNullException.ThrowIfNull(moduleDescriptor);

        return new RestModuleEndpointMetadata(
            moduleDescriptor.Id,
            moduleDescriptor.DisplayName,
            moduleDescriptor.Description,
            moduleDescriptor.Version,
            ResolveMajorVersion(moduleDescriptor.Version));
    }

    private static int? ResolveMajorVersion(string? version)
    {
        return Version.TryParse(version, out var parsedVersion)
            ? parsedVersion.Major
            : null;
    }
}
