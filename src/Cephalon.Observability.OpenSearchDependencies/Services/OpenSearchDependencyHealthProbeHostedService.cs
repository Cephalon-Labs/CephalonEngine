using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cephalon.Abstractions.Health;
using Cephalon.Observability.OpenSearchDependencies.Configuration;
using Cephalon.Observability.OpenSearchDependencies.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.OpenSearchDependencies.Services;

internal sealed class OpenSearchDependencyHealthProbeHostedService(
    OpenSearchDependencyHealthOptions options,
    IOpenSearchDependencyProbeClient probeClient,
    OpenSearchDependencyHealthStore store,
    ILogger<OpenSearchDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.OpenSearchDependencies";
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
        OpenSearchDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "opensearch-dependency"
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
                Description: "OpenSearch endpoint is not configured.",
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
            OpenSearchDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds, endpoint);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"OpenSearch dependency '{displayName}' timed out after {timeoutSeconds} seconds against {endpoint}.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            var endpoint = dependency.Endpoint.Trim();
            OpenSearchDependencyHealthLogs.ProbeFailed(logger, exception, id, endpoint);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"OpenSearch dependency '{displayName}' failed against {endpoint}: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
}

internal interface IOpenSearchDependencyProbeClient
{
    ValueTask<OpenSearchProbeResult> ProbeAsync(OpenSearchDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class OpenSearchDependencyProbeClient(IHttpClientFactory httpClientFactory) : IOpenSearchDependencyProbeClient
{
    public async ValueTask<OpenSearchProbeResult> ProbeAsync(
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
            return new OpenSearchProbeResult(
                HealthState.Unhealthy,
                $"OpenSearch cluster '{clusterName}' at {endpoint} timed out while reporting {status} health{nodeSuffix}.");
        }

        if (discoveredClusterManager.HasValue && !discoveredClusterManager.Value)
        {
            return new OpenSearchProbeResult(
                HealthState.Unhealthy,
                $"OpenSearch cluster '{clusterName}' at {endpoint} did not report a discovered cluster manager while reporting {status} health{nodeSuffix}.");
        }

        var state = status switch
        {
            "green" => HealthState.Healthy,
            "yellow" => HealthState.Degraded,
            "red" => HealthState.Unhealthy,
            _ => HealthState.Unhealthy
        };

        return new OpenSearchProbeResult(
            state,
            $"OpenSearch cluster '{clusterName}' at {endpoint} reported {status} health{nodeSuffix}.");
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

internal sealed record OpenSearchProbeResult(HealthState State, string Description);

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
    private static readonly Action<ILogger, string, int, string, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int, string>(
        LogLevel.Warning,
        new EventId(OpenSearchDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, OpenSearchDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        OpenSearchDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(OpenSearchDependencyHealthDiagnosticsConventions.ProbeFailed.Id, OpenSearchDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        OpenSearchDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds, string endpoint) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, endpoint, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId, string endpoint) =>
        ProbeFailedMessage(logger, dependencyId, endpoint, exception);
}
