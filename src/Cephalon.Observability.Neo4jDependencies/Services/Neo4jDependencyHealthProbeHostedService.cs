using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.Neo4jDependencies.Configuration;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Cephalon.Observability.Neo4jDependencies.Services;

internal sealed class Neo4jDependencyHealthProbeHostedService(
    Neo4jDependencyHealthOptions options,
    INeo4jDependencyProbeClient probeClient,
    DependencyHealthStore store,
    ILogger<Neo4jDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<Neo4jDependencyHealthOptions, Neo4jDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.Neo4jDependencies";
    protected override string DefaultDependencyId => "neo4j-dependency";
    protected override string ProviderLabel => "Neo4j";

    protected override string? ValidateDependency(Neo4jDependencyDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.Uri) && string.IsNullOrWhiteSpace(definition.Host)
            ? "Neo4j URI or host is not configured."
            : null;
    }

    protected override ValueTask<string> ProbeAsync(Neo4jDependencyDefinition definition, CancellationToken cancellationToken) =>
        probeClient.ProbeAsync(definition, cancellationToken);

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        Neo4jDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        Neo4jDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
}

internal interface INeo4jDependencyProbeClient
{
    ValueTask<string> ProbeAsync(Neo4jDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class Neo4jDependencyProbeClient : INeo4jDependencyProbeClient
{
    public async ValueTask<string> ProbeAsync(Neo4jDependencyDefinition dependency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);
        var query = string.IsNullOrWhiteSpace(dependency.HealthQuery)
            ? "RETURN 1 AS health"
            : dependency.HealthQuery.Trim();
        var endpoint = CreateEndpointUri(dependency);

        await using var driver = CreateDriver(endpoint, dependency, timeoutSeconds);
        await using var session = CreateSession(driver, dependency);

        var cursor = await session
            .RunAsync(query, builder => builder.WithTimeout(TimeSpan.FromSeconds(timeoutSeconds)))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        await cursor.ConsumeAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

        return $"Neo4j endpoint '{DescribeTarget(endpoint, dependency.Database)}' responded to health query.";
    }

    internal static Uri CreateEndpointUri(Neo4jDependencyDefinition dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        if (!string.IsNullOrWhiteSpace(dependency.Uri))
        {
            return new Uri(dependency.Uri.Trim(), UriKind.Absolute);
        }

        var host = dependency.Host?.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("Neo4j host must be configured when no explicit URI is supplied.");
        }

        var scheme = string.IsNullOrWhiteSpace(dependency.Scheme) ? "neo4j" : dependency.Scheme.Trim();
        var port = dependency.Port > 0 ? dependency.Port : 7687;
        return new UriBuilder(scheme, host, port).Uri;
    }

    private static IDriver CreateDriver(Uri endpoint, Neo4jDependencyDefinition dependency, int timeoutSeconds)
    {
        var configure = new Action<ConfigBuilder>(builder =>
        {
            builder.WithConnectionTimeout(TimeSpan.FromSeconds(timeoutSeconds));
            builder.WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(timeoutSeconds));
        });

        return string.IsNullOrWhiteSpace(dependency.Username)
            ? GraphDatabase.Driver(endpoint, configure)
            : GraphDatabase.Driver(
                endpoint,
                AuthTokens.Basic(dependency.Username.Trim(), dependency.Password ?? string.Empty),
                configure);
    }

    private static IAsyncSession CreateSession(IDriver driver, Neo4jDependencyDefinition dependency)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(dependency);

        return driver.AsyncSession(builder =>
        {
            builder.WithDefaultAccessMode(AccessMode.Read);
            if (!string.IsNullOrWhiteSpace(dependency.Database))
            {
                builder.WithDatabase(dependency.Database.Trim());
            }
        });
    }

    private static string DescribeTarget(Uri endpoint, string? database)
    {
        return string.IsNullOrWhiteSpace(database)
            ? endpoint.ToString()
            : $"{endpoint}/{database.Trim()}";
    }
}

internal static class Neo4jDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(Neo4jDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, Neo4jDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        Neo4jDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(Neo4jDependencyHealthDiagnosticsConventions.ProbeFailed.Id, Neo4jDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        Neo4jDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
