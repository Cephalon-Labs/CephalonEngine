using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cephalon.Abstractions.Health;
using Cephalon.Observability.ElasticsearchDependencies.Configuration;
using Cephalon.Observability.ElasticsearchDependencies.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.ElasticsearchDependencies.Services;

internal sealed class ElasticsearchDependencyHealthProbeHostedService(
    ElasticsearchDependencyHealthOptions options,
    IElasticsearchDependencyProbeClient probeClient,
    ElasticsearchDependencyHealthStore store,
    ILogger<ElasticsearchDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.ElasticsearchDependencies";
    private CancellationTokenSource? loopCancellation;
    private Task? loopTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Dependencies.Count == 0)
        {
            return;
        }

        await RefreshAsync(cancellationToken).ConfigureAwait(false);

        loopCancellation = new CancellationTokenSource();
        loopTask = Task.Run(() => RunLoopAsync(loopCancellation.Token), CancellationToken.None);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (loopCancellation is null || loopTask is null)
        {
            return;
        }

        loopCancellation.Cancel();

        try
        {
            await loopTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        loopCancellation?.Cancel();
        loopCancellation?.Dispose();
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, options.RefreshIntervalSeconds)));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await RefreshAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var reports = await Task
            .WhenAll(options.Dependencies.Select(dependency => ProbeDependencyAsync(dependency, cancellationToken)))
            .ConfigureAwait(false);

        store.SetReports(reports
            .OrderBy(static report => report.Required ? 0 : 1)
            .ThenBy(static report => report.Source, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static report => report.Id, StringComparer.OrdinalIgnoreCase));
    }

    private async Task<DependencyHealthReport> ProbeDependencyAsync(
        ElasticsearchDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "elasticsearch-dependency"
            : dependency.Id.Trim();
        var displayName = string.IsNullOrWhiteSpace(dependency.DisplayName)
            ? id
            : dependency.DisplayName.Trim();
        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);

        if (string.IsNullOrWhiteSpace(dependency.Endpoint))
        {
            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: "Elasticsearch endpoint is not configured.",
                Required: dependency.Required,
                Source: SourceName);
        }

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var result = await probeClient.ProbeAsync(dependency, timeoutSource.Token).ConfigureAwait(false);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: result.State,
                Description: result.Description,
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            var endpoint = dependency.Endpoint.Trim();
            ElasticsearchDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds, endpoint);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Elasticsearch dependency '{displayName}' timed out after {timeoutSeconds} seconds against {endpoint}.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            var endpoint = dependency.Endpoint.Trim();
            ElasticsearchDependencyHealthLogs.ProbeFailed(logger, exception, id, endpoint);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Elasticsearch dependency '{displayName}' failed against {endpoint}: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
}

internal interface IElasticsearchDependencyProbeClient
{
    ValueTask<ElasticsearchProbeResult> ProbeAsync(ElasticsearchDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class ElasticsearchDependencyProbeClient(IHttpClientFactory httpClientFactory) : IElasticsearchDependencyProbeClient
{
    public async ValueTask<ElasticsearchProbeResult> ProbeAsync(
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
            return new ElasticsearchProbeResult(
                HealthState.Unhealthy,
                $"Elasticsearch cluster '{clusterName}' at {endpoint} timed out while reporting {status} health{nodeSuffix}.");
        }

        var state = status switch
        {
            "green" => HealthState.Healthy,
            "yellow" => HealthState.Degraded,
            "red" => HealthState.Unhealthy,
            _ => HealthState.Unhealthy
        };

        return new ElasticsearchProbeResult(
            state,
            $"Elasticsearch cluster '{clusterName}' at {endpoint} reported {status} health{nodeSuffix}.");
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

internal sealed record ElasticsearchProbeResult(HealthState State, string Description);

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
    private static readonly Action<ILogger, string, int, string, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int, string>(
        LogLevel.Warning,
        new EventId(ElasticsearchDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, ElasticsearchDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        ElasticsearchDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(ElasticsearchDependencyHealthDiagnosticsConventions.ProbeFailed.Id, ElasticsearchDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        ElasticsearchDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds, string endpoint) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, endpoint, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId, string endpoint) =>
        ProbeFailedMessage(logger, dependencyId, endpoint, exception);
}
