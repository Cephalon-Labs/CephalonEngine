using System.Net.Http;
using System.Text.Json;
using Cephalon.Abstractions.Health;
using Cephalon.Observability.ConsulDependencies.Configuration;
using Cephalon.Observability.ConsulDependencies.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.ConsulDependencies.Services;

internal sealed class ConsulDependencyHealthProbeHostedService(
    ConsulDependencyHealthOptions options,
    IConsulDependencyProbeClient probeClient,
    ConsulDependencyHealthStore store,
    ILogger<ConsulDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.ConsulDependencies";
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
        ConsulDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "consul-dependency"
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
                Description: "Consul endpoint is not configured.",
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
            ConsulDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds, endpoint);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Consul dependency '{displayName}' timed out after {timeoutSeconds} seconds against {endpoint}.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            var endpoint = dependency.Endpoint.Trim();
            ConsulDependencyHealthLogs.ProbeFailed(logger, exception, id, endpoint);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Consul dependency '{displayName}' failed against {endpoint}: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
}

internal interface IConsulDependencyProbeClient
{
    ValueTask<ConsulProbeResult> ProbeAsync(ConsulDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class ConsulDependencyProbeClient(IHttpClientFactory httpClientFactory) : IConsulDependencyProbeClient
{
    public async ValueTask<ConsulProbeResult> ProbeAsync(
        ConsulDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var endpoint = ResolveLeaderEndpoint(dependency.Endpoint, dependency.Datacenter);
        var client = httpClientFactory.CreateClient(ConsulDependencyHealthServiceCollectionExtensions.HttpClientName);

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        ApplyAclToken(request, dependency);

        using var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"GET {endpoint} responded with {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var leader = await JsonSerializer.DeserializeAsync<string>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (leader is null)
        {
            throw new InvalidOperationException("Consul status response body was empty.");
        }

        leader = leader.Trim();
        if (string.IsNullOrWhiteSpace(leader))
        {
            return new ConsulProbeResult(
                HealthState.Unhealthy,
                dependency.Datacenter is null
                    ? $"Consul endpoint '{endpoint}' reported no active leader."
                    : $"Consul endpoint '{endpoint}' reported no active leader for datacenter '{dependency.Datacenter.Trim()}'.");
        }

        var datacenterSuffix = string.IsNullOrWhiteSpace(dependency.Datacenter)
            ? string.Empty
            : $" for datacenter '{dependency.Datacenter.Trim()}'";

        return new ConsulProbeResult(
            HealthState.Healthy,
            $"Consul endpoint '{endpoint}' reported leader '{leader}'{datacenterSuffix}.");
    }

    internal static Uri ResolveLeaderEndpoint(string endpointText, string? datacenter)
    {
        var trimmed = endpointText?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException($"Consul endpoint '{trimmed}' is not a valid absolute URI.");
        }

        var builder = new UriBuilder(endpoint);
        if (string.IsNullOrEmpty(builder.Path) || builder.Path == "/")
        {
            builder.Path = "/v1/status/leader";
        }

        if (!string.IsNullOrWhiteSpace(datacenter))
        {
            var encoded = Uri.EscapeDataString(datacenter.Trim());
            builder.Query = string.IsNullOrWhiteSpace(builder.Query)
                ? $"dc={encoded}"
                : $"{builder.Query.TrimStart('?')}&dc={encoded}";
        }

        return builder.Uri;
    }

    private static void ApplyAclToken(HttpRequestMessage request, ConsulDependencyDefinition dependency)
    {
        if (!string.IsNullOrWhiteSpace(dependency.AclToken))
        {
            request.Headers.TryAddWithoutValidation("X-Consul-Token", dependency.AclToken.Trim());
        }
    }
}

internal sealed record ConsulProbeResult(HealthState State, string Description);

internal static class ConsulDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, string, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int, string>(
        LogLevel.Warning,
        new EventId(ConsulDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, ConsulDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        ConsulDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(ConsulDependencyHealthDiagnosticsConventions.ProbeFailed.Id, ConsulDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        ConsulDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds, string endpoint) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, endpoint, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId, string endpoint) =>
        ProbeFailedMessage(logger, dependencyId, endpoint, exception);
}
