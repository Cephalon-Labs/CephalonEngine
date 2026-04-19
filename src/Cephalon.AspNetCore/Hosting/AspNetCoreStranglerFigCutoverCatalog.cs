using Cephalon.Abstractions.Patterns;
using Microsoft.AspNetCore.Http;

namespace Cephalon.AspNetCore.Hosting;

internal sealed class AspNetCoreStranglerFigCutoverCatalog
{
    internal const string DisabledHandlingMode = "disabled";
    internal const string PassThroughHandlingMode = "pass-through";
    internal const string RewriteLocalPathHandlingMode = "rewrite-local-path";
    internal const string RedirectAbsoluteUriHandlingMode = "redirect-absolute-uri";
    internal const string ProxyAbsoluteUriHandlingMode = "proxy-absolute-uri";
    internal const string UnsupportedEndpointHandlingMode = "unsupported-endpoint";

    internal const string AbsoluteUriEndpointKind = "absolute-uri";
    internal const string LocalPathEndpointKind = "local-path";
    internal const string UnsupportedEndpointKind = "unsupported";

    private readonly AspNetCoreStranglerFigCutoverOptions options;
    private readonly RouteEntry[] entries;
    private readonly Dictionary<string, RouteEntry> entriesByRouteId;

    public AspNetCoreStranglerFigCutoverCatalog(
        IStranglerFigMigrationRuntimeCatalog runtimeCatalog,
        AspNetCoreStranglerFigCutoverOptions options)
    {
        ArgumentNullException.ThrowIfNull(runtimeCatalog);
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
        entries = runtimeCatalog.Routes
            .Select(CreateEntry)
            .OrderBy(static entry => entry.Descriptor.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.Descriptor.PathPrefix, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.Descriptor.RouteId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        entriesByRouteId = entries.ToDictionary(
            static entry => entry.Descriptor.RouteId,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<AspNetCoreStranglerFigCutoverDescriptor> Routes =>
        entries.Select(static entry => entry.Descriptor).ToArray();

    public bool HasActiveHandlers =>
        entries.Any(static entry => entry.Descriptor.InterceptsRequest);

    public AspNetCoreStranglerFigCutoverDescriptor? GetById(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            return null;
        }

        return entriesByRouteId.TryGetValue(routeId.Trim(), out var entry)
            ? entry.Descriptor
            : null;
    }

    public AspNetCoreStranglerFigCutoverDecision CreateDecision(
        StranglerFigRouteResolution resolution,
        QueryString requestQuery)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        if (!entriesByRouteId.TryGetValue(resolution.RouteId, out var entry))
        {
            return new AspNetCoreStranglerFigCutoverDecision(
                resolution,
                UnsupportedEndpointKind,
                DisabledHandlingMode,
                CutoverEnabled: false,
                InterceptsRequest: false,
                DestinationPath: null,
                DestinationQuery: null,
                DestinationUri: null,
                ResponseStatusCode: null,
                FailureReason: "The matched strangler-fig route is not available in the ASP.NET Core cutover catalog.");
        }

        var suffix = ResolveRemainingPath(
            resolution.RequestedPath,
            resolution.MatchedPathPrefix);
        var descriptor = entry.Descriptor;

        return descriptor.HandlingMode switch
        {
            RewriteLocalPathHandlingMode => CreateRewriteDecision(resolution, requestQuery, descriptor, entry.Endpoint, suffix),
            RedirectAbsoluteUriHandlingMode => CreateRedirectDecision(resolution, requestQuery, descriptor, entry.Endpoint, suffix),
            ProxyAbsoluteUriHandlingMode => CreateProxyDecision(resolution, requestQuery, descriptor, entry.Endpoint, suffix),
            UnsupportedEndpointHandlingMode => CreateUnsupportedDecision(resolution, descriptor),
            _ => new AspNetCoreStranglerFigCutoverDecision(
                resolution,
                descriptor.SelectedEndpointKind,
                descriptor.HandlingMode,
                descriptor.CutoverEnabled,
                descriptor.InterceptsRequest,
                DestinationPath: null,
                DestinationQuery: null,
                DestinationUri: null,
                ResponseStatusCode: null,
                FailureReason: null)
        };
    }

    private RouteEntry CreateEntry(StranglerFigMigrationRuntimeDescriptor route)
    {
        ArgumentNullException.ThrowIfNull(route);

        var endpoint = ResolveEndpoint(route.SelectedEndpoint);
        var handlingMode = ResolveHandlingMode(route, endpoint);
        var intercepts = IsInterceptingHandlingMode(handlingMode);
        int? redirectStatusCode = string.Equals(
                handlingMode,
                RedirectAbsoluteUriHandlingMode,
                StringComparison.OrdinalIgnoreCase)
            ? options.RedirectStatusCode
            : null;

        return new RouteEntry(
            new AspNetCoreStranglerFigCutoverDescriptor(
                RouteId: route.RouteId,
                SourceModuleId: route.SourceModuleId,
                DisplayName: route.DisplayName,
                PathPrefix: route.PathPrefix,
                RequestedTarget: route.RequestedTarget,
                EffectiveTarget: route.EffectiveTarget,
                RequestedTargetSource: route.RequestedTargetSource,
                SelectionMode: route.SelectionMode,
                SelectedEndpoint: route.SelectedEndpoint,
                SelectedEndpointKind: endpoint.Kind,
                HandlingMode: handlingMode,
                CutoverEnabled: options.Enabled,
                InterceptsRequest: intercepts,
                RedirectStatusCode: redirectStatusCode,
                ProgressState: route.ProgressState,
                ProgressPercent: route.ProgressPercent,
                Metadata: route.Metadata,
                RuntimeMetadata: route.RuntimeMetadata),
            endpoint);
    }

    private string ResolveHandlingMode(
        StranglerFigMigrationRuntimeDescriptor route,
        ResolvedEndpoint endpoint)
    {
        if (!options.Enabled)
        {
            return DisabledHandlingMode;
        }

        return endpoint.Kind switch
        {
            LocalPathEndpointKind when PathsEquivalent(endpoint.LocalPath, route.PathPrefix) &&
                string.IsNullOrEmpty(endpoint.Query) => PassThroughHandlingMode,
            LocalPathEndpointKind => RewriteLocalPathHandlingMode,
            AbsoluteUriEndpointKind when options.UsesProxyForAbsoluteEndpoints => ProxyAbsoluteUriHandlingMode,
            AbsoluteUriEndpointKind => RedirectAbsoluteUriHandlingMode,
            _ => UnsupportedEndpointHandlingMode
        };
    }

    private static AspNetCoreStranglerFigCutoverDecision CreateRewriteDecision(
        StranglerFigRouteResolution resolution,
        QueryString requestQuery,
        AspNetCoreStranglerFigCutoverDescriptor descriptor,
        ResolvedEndpoint endpoint,
        string suffix)
    {
        var destinationPath = CombinePath(endpoint.LocalPath, suffix);
        var destinationQuery = CombineQueryStrings(endpoint.Query, requestQuery.Value);

        return new AspNetCoreStranglerFigCutoverDecision(
            resolution,
            descriptor.SelectedEndpointKind,
            descriptor.HandlingMode,
            descriptor.CutoverEnabled,
            descriptor.InterceptsRequest,
            DestinationPath: destinationPath,
            DestinationQuery: destinationQuery,
            DestinationUri: null,
            ResponseStatusCode: null,
            FailureReason: null);
    }

    private AspNetCoreStranglerFigCutoverDecision CreateRedirectDecision(
        StranglerFigRouteResolution resolution,
        QueryString requestQuery,
        AspNetCoreStranglerFigCutoverDescriptor descriptor,
        ResolvedEndpoint endpoint,
        string suffix)
    {
        var destinationUri = BuildAbsoluteDestination(endpoint, suffix, requestQuery.Value);

        return new AspNetCoreStranglerFigCutoverDecision(
            resolution,
            descriptor.SelectedEndpointKind,
            descriptor.HandlingMode,
            descriptor.CutoverEnabled,
            descriptor.InterceptsRequest,
            DestinationPath: null,
            DestinationQuery: null,
            DestinationUri: destinationUri.AbsoluteUri,
            ResponseStatusCode: options.RedirectStatusCode,
            FailureReason: null);
    }

    private static AspNetCoreStranglerFigCutoverDecision CreateProxyDecision(
        StranglerFigRouteResolution resolution,
        QueryString requestQuery,
        AspNetCoreStranglerFigCutoverDescriptor descriptor,
        ResolvedEndpoint endpoint,
        string suffix)
    {
        var destinationUri = BuildAbsoluteDestination(endpoint, suffix, requestQuery.Value);

        return new AspNetCoreStranglerFigCutoverDecision(
            resolution,
            descriptor.SelectedEndpointKind,
            descriptor.HandlingMode,
            descriptor.CutoverEnabled,
            descriptor.InterceptsRequest,
            DestinationPath: null,
            DestinationQuery: null,
            DestinationUri: destinationUri.AbsoluteUri,
            ResponseStatusCode: null,
            FailureReason: null);
    }

    private static AspNetCoreStranglerFigCutoverDecision CreateUnsupportedDecision(
        StranglerFigRouteResolution resolution,
        AspNetCoreStranglerFigCutoverDescriptor descriptor)
    {
        return new AspNetCoreStranglerFigCutoverDecision(
            resolution,
            descriptor.SelectedEndpointKind,
            descriptor.HandlingMode,
            descriptor.CutoverEnabled,
            descriptor.InterceptsRequest,
            DestinationPath: null,
            DestinationQuery: null,
            DestinationUri: null,
            ResponseStatusCode: StatusCodes.Status502BadGateway,
            FailureReason: $"Selected endpoint '{descriptor.SelectedEndpoint}' does not map to a rooted local path or absolute HTTP/HTTPS URI.");
    }

    private static ResolvedEndpoint ResolveEndpoint(string selectedEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedEndpoint);

        if (Uri.TryCreate(selectedEndpoint, UriKind.Absolute, out var absoluteUri))
        {
            if (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return new ResolvedEndpoint(
                    Kind: AbsoluteUriEndpointKind,
                    LocalPath: NormalizePath(absoluteUri.AbsolutePath),
                    Query: NormalizeOptionalQuery(absoluteUri.Query),
                    AbsoluteUri: absoluteUri);
            }

            return new ResolvedEndpoint(
                Kind: UnsupportedEndpointKind,
                LocalPath: null,
                Query: null,
                AbsoluteUri: null);
        }

        if (!selectedEndpoint.StartsWith('/'))
        {
            return new ResolvedEndpoint(
                Kind: UnsupportedEndpointKind,
                LocalPath: null,
                Query: null,
                AbsoluteUri: null);
        }

        var normalized = selectedEndpoint.Trim();
        var hashIndex = normalized.IndexOf('#');
        if (hashIndex >= 0)
        {
            normalized = normalized[..hashIndex];
        }

        var queryIndex = normalized.IndexOf('?');
        var path = queryIndex >= 0
            ? normalized[..queryIndex]
            : normalized;
        var query = queryIndex >= 0
            ? normalized[queryIndex..]
            : null;

        return new ResolvedEndpoint(
            Kind: LocalPathEndpointKind,
            LocalPath: NormalizePath(path),
            Query: NormalizeOptionalQuery(query),
            AbsoluteUri: null);
    }

    private static bool IsInterceptingHandlingMode(string handlingMode)
    {
        return string.Equals(handlingMode, RewriteLocalPathHandlingMode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(handlingMode, RedirectAbsoluteUriHandlingMode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(handlingMode, ProxyAbsoluteUriHandlingMode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(handlingMode, UnsupportedEndpointHandlingMode, StringComparison.OrdinalIgnoreCase);
    }

    private static Uri BuildAbsoluteDestination(
        ResolvedEndpoint endpoint,
        string suffix,
        string? requestQuery)
    {
        if (endpoint.AbsoluteUri is null)
        {
            throw new InvalidOperationException("Absolute strangler-fig destinations require an absolute URI endpoint.");
        }

        var builder = new UriBuilder(endpoint.AbsoluteUri)
        {
            Path = CombinePath(endpoint.LocalPath, suffix),
            Query = TrimQueryPrefix(CombineQueryStrings(endpoint.Query, requestQuery))
        };

        return builder.Uri;
    }

    private static string ResolveRemainingPath(string requestedPath, string matchedPathPrefix)
    {
        if (string.Equals(matchedPathPrefix, "/", StringComparison.Ordinal))
        {
            return requestedPath;
        }

        if (string.Equals(requestedPath, matchedPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return requestedPath.StartsWith(matchedPathPrefix, StringComparison.OrdinalIgnoreCase)
            ? requestedPath[matchedPathPrefix.Length..]
            : string.Empty;
    }

    private static string CombinePath(string? basePath, string suffix)
    {
        var normalizedBasePath = NormalizePath(basePath);
        if (string.IsNullOrEmpty(suffix))
        {
            return normalizedBasePath;
        }

        var normalizedSuffix = suffix.StartsWith('/')
            ? suffix
            : "/" + suffix;

        return normalizedBasePath.Length == 1
            ? normalizedSuffix
            : normalizedBasePath + normalizedSuffix;
    }

    private static string? CombineQueryStrings(string? left, string? right)
    {
        var leftValue = TrimQueryPrefix(left);
        var rightValue = TrimQueryPrefix(right);

        if (string.IsNullOrWhiteSpace(leftValue) && string.IsNullOrWhiteSpace(rightValue))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(leftValue))
        {
            return "?" + rightValue;
        }

        if (string.IsNullOrWhiteSpace(rightValue))
        {
            return "?" + leftValue;
        }

        return "?" + leftValue + "&" + rightValue;
    }

    private static string NormalizePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "/";
        }

        var normalized = value.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized.TrimStart('/');
        }

        normalized = normalized.Length > 1
            ? normalized.TrimEnd('/')
            : normalized;

        return string.IsNullOrWhiteSpace(normalized)
            ? "/"
            : normalized;
    }

    private static string? NormalizeOptionalQuery(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || string.Equals(trimmed, "?", StringComparison.Ordinal))
        {
            return null;
        }

        return trimmed.StartsWith('?')
            ? trimmed
            : "?" + trimmed;
    }

    private static bool PathsEquivalent(string? left, string? right)
    {
        return string.Equals(
            NormalizePath(left),
            NormalizePath(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string TrimQueryPrefix(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().TrimStart('?');
    }

    private sealed record RouteEntry(
        AspNetCoreStranglerFigCutoverDescriptor Descriptor,
        ResolvedEndpoint Endpoint);

    private sealed record ResolvedEndpoint(
        string Kind,
        string? LocalPath,
        string? Query,
        Uri? AbsoluteUri);
}
