using Cephalon.Abstractions.Behaviors;
using Cephalon.AspNetCore.Hosting;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Resolves canonical HTTP routes for behavior transport bindings from the shared API surface descriptor.
/// </summary>
internal sealed class BehaviorApiSurfaceRouteResolver
{
    private readonly ApiRoutesOptions options;

    public BehaviorApiSurfaceRouteResolver(ApiRoutesOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IReadOnlyList<string> ResolveRoutes(string transportId, BehaviorTopologyDescriptor descriptor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);
        ArgumentNullException.ThrowIfNull(descriptor);

        return [ResolveCanonicalRoute(transportId, descriptor)];
    }

    private string ResolveCanonicalRoute(string transportId, BehaviorTopologyDescriptor descriptor)
    {
        var prefix = transportId switch
        {
            "http.jsonrpc" => options.JsonRpcPrefix,
            "http.sse" => options.SsePrefix,
            "http.ws" => options.WsPrefix,
            "http.graphql" => options.GraphQLPrefix,
            "http.graphql-sse" => options.GraphQLSsePrefix,
            "http.graphql-ws" => options.GraphQLWsPrefix,
            _ => throw new InvalidOperationException(
                $"Transport '{transportId}' does not participate in the shared behavior API surface route policy.")
        };

        return JoinSegments(
            prefix,
            options.DefaultBehaviorDocumentName,
            [descriptor.ApiSurface.GroupPath, descriptor.ApiSurface.OperationPath]);
    }

    private static string JoinSegments(params string[] segments)
    {
        var normalizedSegments = segments
            .Where(static segment => !string.IsNullOrWhiteSpace(segment))
            .Select(static segment => segment.Trim('/'))
            .Where(static segment => segment.Length > 0)
            .ToArray();

        return normalizedSegments.Length == 0
            ? "/"
            : "/" + string.Join("/", normalizedSegments);
    }

    private static string JoinSegments(string prefix, string documentName, string[] surfaceSegments)
    {
        return JoinSegments([prefix, documentName, .. surfaceSegments]);
    }
}
