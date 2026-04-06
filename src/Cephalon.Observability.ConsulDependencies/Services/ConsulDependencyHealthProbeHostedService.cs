using System.Text.Json;
using Cephalon.Observability.ConsulDependencies.Configuration;
using Cephalon.Observability.ConsulDependencies.Hosting;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.ConsulDependencies.Services;

internal sealed class ConsulDependencyHealthProbeHostedService(
    ConsulDependencyHealthOptions options,
    IConsulDependencyProbeClient probeClient,
    DependencyHealthStore store,
    ILogger<ConsulDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<ConsulDependencyHealthOptions, ConsulDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.ConsulDependencies";
    protected override string DefaultDependencyId => "consul-dependency";
    protected override string ProviderLabel => "Consul";

    protected override string? ValidateDependency(ConsulDependencyDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.Endpoint)
            ? "Consul endpoint is not configured."
            : null;
    }

    protected override ValueTask<string> ProbeAsync(ConsulDependencyDefinition definition, CancellationToken cancellationToken) =>
        probeClient.ProbeAsync(definition, cancellationToken);

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        ConsulDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        ConsulDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
}

internal interface IConsulDependencyProbeClient
{
    ValueTask<string> ProbeAsync(ConsulDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class ConsulDependencyProbeClient(IHttpClientFactory httpClientFactory) : IConsulDependencyProbeClient
{
    public async ValueTask<string> ProbeAsync(
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
            throw new InvalidOperationException(
                dependency.Datacenter is null
                    ? $"Consul endpoint '{endpoint}' reported no active leader."
                    : $"Consul endpoint '{endpoint}' reported no active leader for datacenter '{dependency.Datacenter.Trim()}'.");
        }

        var datacenterSuffix = string.IsNullOrWhiteSpace(dependency.Datacenter)
            ? string.Empty
            : $" for datacenter '{dependency.Datacenter.Trim()}'";

        return $"Consul endpoint '{endpoint}' reported leader '{leader}'{datacenterSuffix}.";
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

internal static class ConsulDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(ConsulDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, ConsulDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        "Consul dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.");

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(ConsulDependencyHealthDiagnosticsConventions.ProbeFailed.Id, ConsulDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        "Consul dependency probe '{DependencyId}' failed.");

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
