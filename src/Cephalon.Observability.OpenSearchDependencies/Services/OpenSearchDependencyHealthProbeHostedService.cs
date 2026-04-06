using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.OpenSearchDependencies.Configuration;
using Cephalon.Observability.OpenSearchDependencies.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.OpenSearchDependencies.Services;

internal sealed class OpenSearchDependencyHealthProbeHostedService(
    OpenSearchDependencyHealthOptions options,
    IOpenSearchDependencyProbeClient probeClient,
    DependencyHealthStore store,
    ILogger<OpenSearchDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<OpenSearchDependencyHealthOptions, OpenSearchDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.OpenSearchDependencies";
    protected override string DefaultDependencyId => "opensearch-dependency";
    protected override string ProviderLabel => "OpenSearch";

    protected override string? ValidateDependency(OpenSearchDependencyDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.Endpoint)
            ? "OpenSearch endpoint is not configured."
            : null;
    }

    protected override ValueTask<string> ProbeAsync(OpenSearchDependencyDefinition definition, CancellationToken cancellationToken) =>
        probeClient.ProbeAsync(definition, cancellationToken);

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        OpenSearchDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        OpenSearchDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
}

internal interface IOpenSearchDependencyProbeClient
{
    ValueTask<string> ProbeAsync(OpenSearchDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class OpenSearchDependencyProbeClient(IHttpClientFactory httpClientFactory) : IOpenSearchDependencyProbeClient
{
    public async ValueTask<string> ProbeAsync(
        OpenSearchDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var endpoint = ResolveClusterHealthEndpoint(dependency);
        var client = httpClientFactory.CreateClient(OpenSearchDependencyHealthServiceCollectionExtensions.HttpClientName);

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        ApplyAuthorization(request, dependency);

        using var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"GET {endpoint} responded with {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<OpenSearchClusterHealthResponse>(stream, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("OpenSearch cluster-health response body was empty.");

        var clusterName = string.IsNullOrWhiteSpace(payload.ClusterName)
            ? endpoint.Host
            : payload.ClusterName.Trim();
        var status = string.IsNullOrWhiteSpace(payload.Status)
            ? "unknown"
            : payload.Status.Trim().ToLowerInvariant();
        var nodeSuffix = payload.NumberOfNodes > 0
            ? $" across {payload.NumberOfNodes} node{(payload.NumberOfNodes == 1 ? string.Empty : "s")}"
            : string.Empty;
        var discoveredClusterManager = payload.DiscoveredClusterManager ?? payload.DiscoveredMaster;

        if (payload.TimedOut)
        {
            throw new InvalidOperationException(
                $"OpenSearch cluster '{clusterName}' at {endpoint} timed out while reporting {status} health{nodeSuffix}.");
        }

        if (discoveredClusterManager.HasValue && !discoveredClusterManager.Value)
        {
            throw new InvalidOperationException(
                $"OpenSearch cluster '{clusterName}' at {endpoint} did not report a discovered cluster manager while reporting {status} health{nodeSuffix}.");
        }

        if (status is "yellow" or "red" or "unknown")
        {
            throw new InvalidOperationException(
                $"OpenSearch cluster '{clusterName}' at {endpoint} reported {status} health{nodeSuffix}.");
        }

        return $"OpenSearch cluster '{clusterName}' at {endpoint} reported {status} health{nodeSuffix}.";
    }

    internal static Uri ResolveClusterHealthEndpoint(OpenSearchDependencyDefinition dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var trimmed = dependency.Endpoint?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException($"OpenSearch endpoint '{trimmed}' is not a valid absolute URI.");
        }

        if (!string.IsNullOrEmpty(endpoint.AbsolutePath) && endpoint.AbsolutePath != "/")
        {
            return endpoint;
        }

        var indexPath = string.IsNullOrWhiteSpace(dependency.Index)
            ? string.Empty
            : $"/{dependency.Index.Trim().TrimStart('/')}";

        return new Uri(endpoint, $"/_cluster/health{indexPath}");
    }

    private static void ApplyAuthorization(HttpRequestMessage request, OpenSearchDependencyDefinition dependency)
    {
        if (!string.IsNullOrWhiteSpace(dependency.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", dependency.BearerToken.Trim());
            return;
        }

        if (!string.IsNullOrWhiteSpace(dependency.Username))
        {
            var password = dependency.Password ?? string.Empty;
            var value = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{dependency.Username.Trim()}:{password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", value);
        }
    }
}

internal sealed class OpenSearchClusterHealthResponse
{
    [JsonPropertyName("cluster_name")]
    public string? ClusterName { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("timed_out")]
    public bool TimedOut { get; init; }

    [JsonPropertyName("number_of_nodes")]
    public int NumberOfNodes { get; init; }

    [JsonPropertyName("discovered_cluster_manager")]
    public bool? DiscoveredClusterManager { get; init; }

    [JsonPropertyName("discovered_master")]
    public bool? DiscoveredMaster { get; init; }
}

internal static class OpenSearchDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(OpenSearchDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, OpenSearchDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        "OpenSearch dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.");

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(OpenSearchDependencyHealthDiagnosticsConventions.ProbeFailed.Id, OpenSearchDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        "OpenSearch dependency probe '{DependencyId}' failed.");

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
