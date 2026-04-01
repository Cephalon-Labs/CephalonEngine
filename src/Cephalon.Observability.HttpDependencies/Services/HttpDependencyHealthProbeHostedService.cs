using Cephalon.Abstractions.Health;
using Cephalon.Observability.HttpDependencies.Configuration;
using Cephalon.Observability.HttpDependencies.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.HttpDependencies.Services;

internal sealed class HttpDependencyHealthProbeHostedService(
    IHttpClientFactory httpClientFactory,
    HttpDependencyHealthOptions options,
    HttpDependencyHealthStore store,
    ILogger<HttpDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.HttpDependencies";
    private const string UserAgent = "Cephalon.DependencyHealth/1.0";

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
        var client = httpClientFactory.CreateClient(HttpDependencyHealthServiceCollectionExtensions.HttpClientName);
        var reports = await Task
            .WhenAll(options.Dependencies.Select(dependency => ProbeDependencyAsync(client, dependency, cancellationToken)))
            .ConfigureAwait(false);

        store.SetReports(reports
            .OrderBy(static report => report.Required ? 0 : 1)
            .ThenBy(static report => report.Source, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static report => report.Id, StringComparer.OrdinalIgnoreCase));
    }

    private async Task<DependencyHealthReport> ProbeDependencyAsync(
        HttpClient client,
        HttpDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "http-dependency"
            : dependency.Id.Trim();
        var displayName = string.IsNullOrWhiteSpace(dependency.DisplayName)
            ? id
            : dependency.DisplayName.Trim();
        var endpointText = dependency.Endpoint?.Trim() ?? string.Empty;
        var methodText = string.IsNullOrWhiteSpace(dependency.Method)
            ? "GET"
            : dependency.Method.Trim().ToUpperInvariant();

        if (!Uri.TryCreate(endpointText, UriKind.Absolute, out var endpoint))
        {
            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Probe endpoint '{endpointText}' is not a valid absolute URI.",
                Required: dependency.Required,
                Source: SourceName);
        }

        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using var request = new HttpRequestMessage(new HttpMethod(methodText), endpoint);
            request.Headers.UserAgent.ParseAdd(UserAgent);

            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token)
                .ConfigureAwait(false);

            var statusCode = (int)response.StatusCode;
            var isHealthy = dependency.ExpectedStatusCodes.Count > 0
                ? dependency.ExpectedStatusCodes.Contains(statusCode)
                : response.IsSuccessStatusCode;
            var state = isHealthy
                ? HealthState.Healthy
                : statusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.RequestTimeout
                    ? HealthState.Unhealthy
                    : HealthState.Degraded;
            var description = isHealthy
                ? $"{methodText} {endpoint} responded with {(int)response.StatusCode} {response.ReasonPhrase}."
                : $"{methodText} {endpoint} responded with {(int)response.StatusCode} {response.ReasonPhrase}.";

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: state,
                Description: description,
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            HttpDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds, endpoint);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"{methodText} {endpoint} timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            HttpDependencyHealthLogs.ProbeFailed(logger, exception, id, endpoint);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"{methodText} {endpoint} failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
}

internal static class HttpDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Uri, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int, Uri>(
        LogLevel.Warning,
        new EventId(3100, nameof(ProbeTimedOut)),
        "HTTP dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s against {Endpoint}.");

    private static readonly Action<ILogger, string, Uri, Exception?> ProbeFailedMessage = LoggerMessage.Define<string, Uri>(
        LogLevel.Warning,
        new EventId(3101, nameof(ProbeFailed)),
        "HTTP dependency probe '{DependencyId}' failed against {Endpoint}.");

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds, Uri endpoint) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, endpoint, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId, Uri endpoint) =>
        ProbeFailedMessage(logger, dependencyId, endpoint, exception);
}
