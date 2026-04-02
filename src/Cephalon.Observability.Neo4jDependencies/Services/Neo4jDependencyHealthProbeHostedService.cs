using Cephalon.Abstractions.Health;
using Cephalon.Observability.Neo4jDependencies.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Cephalon.Observability.Neo4jDependencies.Services;

internal sealed class Neo4jDependencyHealthProbeHostedService(
    Neo4jDependencyHealthOptions options,
    INeo4jDependencyProbeClient probeClient,
    Neo4jDependencyHealthStore store,
    ILogger<Neo4jDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.Neo4jDependencies";
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
        Neo4jDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "neo4j-dependency"
            : dependency.Id.Trim();
        var displayName = string.IsNullOrWhiteSpace(dependency.DisplayName)
            ? id
            : dependency.DisplayName.Trim();
        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);

        if (string.IsNullOrWhiteSpace(dependency.Uri) && string.IsNullOrWhiteSpace(dependency.Host))
        {
            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: "Neo4j URI or host is not configured.",
                Required: dependency.Required,
                Source: SourceName);
        }

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var description = await probeClient.ProbeAsync(dependency, timeoutSource.Token).ConfigureAwait(false);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Healthy,
                Description: description,
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Neo4jDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Neo4j dependency '{displayName}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            Neo4jDependencyHealthLogs.ProbeFailed(logger, exception, id);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Neo4j dependency '{displayName}' failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
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
