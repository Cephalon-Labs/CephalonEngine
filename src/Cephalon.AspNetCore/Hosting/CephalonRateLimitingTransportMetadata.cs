namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Carries the Cephalon transport identifier onto an endpoint so the shared
/// rate-limiting rejection writer can emit a transport-native envelope when the
/// configured ASP.NET Core endpoint limiter rejects a request.
/// </summary>
/// <remarks>
/// This metadata is attached automatically by
/// <see cref="CephalonRateLimitingEndpointConventionBuilderExtensions.ApplyCephalonRateLimiting{TBuilder}(TBuilder, IServiceProvider, string, string?)" />
/// so transport mappers do not need to wire it manually.
/// </remarks>
public sealed class CephalonRateLimitingTransportMetadata
{
    /// <summary>
    /// Initializes a new <see cref="CephalonRateLimitingTransportMetadata" />.
    /// </summary>
    /// <param name="transportId">
    /// The Cephalon transport identifier (for example <c>rest-api</c>, <c>json-rpc</c>,
    /// <c>grpc</c>, <c>http.jsonrpc</c>).
    /// </param>
    public CephalonRateLimitingTransportMetadata(string transportId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);
        TransportId = transportId;
    }

    /// <summary>Gets the Cephalon transport identifier the rejected request belongs to.</summary>
    public string TransportId { get; }
}
