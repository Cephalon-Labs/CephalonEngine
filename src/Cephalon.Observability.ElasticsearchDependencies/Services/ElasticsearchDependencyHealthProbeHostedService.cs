using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.ElasticsearchDependencies.Configuration;
using Cephalon.Observability.ElasticsearchDependencies.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.ElasticsearchDependencies.Services;

internal sealed class ElasticsearchDependencyHealthProbeHostedService(
    ElasticsearchDependencyHealthOptions options,
    IElasticsearchDependencyProbeClient probeClient,
    DependencyHealthStore store,
    ILogger<ElasticsearchDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<ElasticsearchDependencyHealthOptions, ElasticsearchDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.ElasticsearchDependencies";
    protected override string DefaultDependencyId => "elasticsearch-dependency";
    protected override string ProviderLabel => "Elasticsearch";

    protected override string? ValidateDependency(ElasticsearchDependencyDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.Endpoint)
            ? "Elasticsearch endpoint is not configured."
            : null;
    }

    protected override ValueTask<string> ProbeAsync(ElasticsearchDependencyDefinition definition, CancellationToken cancellationToken) =>
        probeClient.ProbeAsync(definition, cancellationToken);

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        ElasticsearchDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        ElasticsearchDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
}

internal interface IElasticsearchDependencyProbeClient
{
    ValueTask<string> ProbeAsync(ElasticsearchDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class ElasticsearchDependencyProbeClient(IHttpClientFactory httpClientFactory) : IElasticsearchDependencyProbeClient
{
    public async ValueTask<string> ProbeAsync(
        ElasticsearchDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var endpoint = ResolveClusterHealthEndpoint(dependency.Endpoint);
        var client = httpClientFactory.CreateClient(ElasticsearchDependencyHealthServiceCollectionExtensions.HttpClientName);

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
        var payload = await JsonSerializer.DeserializeAsync<ElasticsearchClusterHealthResponse>(stream, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Elasticsearch cluster-health response body was empty.");

        var clusterName = string.IsNullOrWhiteSpace(payload.ClusterName)
            ? endpoint.Host
            : payload.ClusterName.Trim();
        var status = string.IsNullOrWhiteSpace(payload.Status)
            ? "unknown"
            : payload.Status.Trim().ToLowerInvariant();
        var nodeSuffix = payload.NumberOfNodes > 0
            ? $" across {payload.NumberOfNodes} node{(payload.NumberOfNodes == 1 ? string.Empty : "s")}"
            : string.Empty;

        if (payload.TimedOut)
        {
            throw new InvalidOperationException(
                $"Elasticsearch cluster '{clusterName}' at {endpoint} timed out while reporting {status} health{nodeSuffix}.");
        }

        if (status is "yellow" or "red" or "unknown")
        {
            throw new InvalidOperationException(
                $"Elasticsearch cluster '{clusterName}' at {endpoint} reported {status} health{nodeSuffix}.");
        }

        return $"Elasticsearch cluster '{clusterName}' at {endpoint} reported {status} health{nodeSuffix}.";
    }

    internal static Uri ResolveClusterHealthEndpoint(string endpointText)
    {
        var trimmed = endpointText?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException($"Elasticsearch endpoint '{trimmed}' is not a valid absolute URI.");
        }

        if (string.IsNullOrEmpty(endpoint.AbsolutePath) || endpoint.AbsolutePath == "/")
        {
            return new Uri(endpoint, "/_cluster/health");
        }

        return endpoint;
    }

    private static void ApplyAuthorization(HttpRequestMessage request, ElasticsearchDependencyDefinition dependency)
    {
        if (!string.IsNullOrWhiteSpace(dependency.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", dependency.ApiKey.Trim());
            return;
        }

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

internal sealed class ElasticsearchClusterHealthResponse
{
    [JsonPropertyName("cluster_name")]
    public string? ClusterName { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("timed_out")]
    public bool TimedOut { get; init; }

    [JsonPropertyName("number_of_nodes")]
    public int NumberOfNodes { get; init; }
}

internal static class ElasticsearchDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(ElasticsearchDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, ElasticsearchDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        $"Elasticsearch dependency probe '{{DependencyId}}' timed out after {{TimeoutSeconds}}s.");

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(ElasticsearchDependencyHealthDiagnosticsConventions.ProbeFailed.Id, ElasticsearchDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        "Elasticsearch dependency probe '{DependencyId}' failed.");

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
