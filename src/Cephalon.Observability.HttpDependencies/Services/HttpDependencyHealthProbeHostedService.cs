using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.HttpDependencies.Configuration;
using Cephalon.Observability.HttpDependencies.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.HttpDependencies.Services;

internal sealed class HttpDependencyHealthProbeHostedService(
    IHttpClientFactory httpClientFactory,
    HttpDependencyHealthOptions options,
    DependencyHealthStore store,
    ILogger<HttpDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<HttpDependencyHealthOptions, HttpDependencyDefinition>(options, store, logger)
{
    private const string UserAgent = "Cephalon.DependencyHealth/1.0";

    protected override string SourceName => "Cephalon.Observability.HttpDependencies";
    protected override string DefaultDependencyId => "http-dependency";
    protected override string ProviderLabel => "HTTP";

    protected override string? ValidateDependency(HttpDependencyDefinition definition)
    {
        var endpointText = definition.Endpoint?.Trim() ?? string.Empty;
        return !Uri.TryCreate(endpointText, UriKind.Absolute, out _)
            ? $"Probe endpoint '{endpointText}' is not a valid absolute URI."
            : null;
    }

    protected override async ValueTask<string> ProbeAsync(HttpDependencyDefinition definition, CancellationToken cancellationToken)
    {
        var endpointText = definition.Endpoint.Trim();
        var endpoint = new Uri(endpointText, UriKind.Absolute);
        var methodText = string.IsNullOrWhiteSpace(definition.Method)
            ? "GET"
            : definition.Method.Trim().ToUpperInvariant();

        var client = httpClientFactory.CreateClient(HttpDependencyHealthServiceCollectionExtensions.HttpClientName);

        using var request = new HttpRequestMessage(new HttpMethod(methodText), endpoint);
        request.Headers.UserAgent.ParseAdd(UserAgent);

        using var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        var statusCode = (int)response.StatusCode;
        var isHealthy = definition.ExpectedStatusCodes.Count > 0
            ? definition.ExpectedStatusCodes.Contains(statusCode)
            : response.IsSuccessStatusCode;

        var description = $"{methodText} {endpoint} responded with {(int)response.StatusCode} {response.ReasonPhrase}.";

        if (!isHealthy)
        {
            throw new HttpRequestException(description, null, response.StatusCode);
        }

        return description;
    }

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        HttpDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        HttpDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
}

internal static class HttpDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(HttpDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, HttpDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        "HTTP dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.");

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(HttpDependencyHealthDiagnosticsConventions.ProbeFailed.Id, HttpDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        "HTTP dependency probe '{DependencyId}' failed.");

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
