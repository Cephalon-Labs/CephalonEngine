using Cassandra;
using Cephalon.Abstractions.Health;
using Cephalon.Observability.CassandraDependencies.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.CassandraDependencies.Services;

internal sealed class CassandraDependencyHealthProbeHostedService(
    CassandraDependencyHealthOptions options,
    ICassandraDependencyProbeClient probeClient,
    CassandraDependencyHealthStore store,
    ILogger<CassandraDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.CassandraDependencies";
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
        CassandraDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "cassandra-dependency"
            : dependency.Id.Trim();
        var displayName = string.IsNullOrWhiteSpace(dependency.DisplayName)
            ? id
            : dependency.DisplayName.Trim();
        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);
        var contactPoints = CassandraDependencyProbeClient.NormalizeContactPoints(dependency.ContactPoints);

        if (contactPoints.Count == 0)
        {
            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: "Cassandra contact points are not configured.",
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
            CassandraDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Cassandra dependency '{displayName}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            CassandraDependencyHealthLogs.ProbeFailed(logger, exception, id);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Cassandra dependency '{displayName}' failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
}

internal interface ICassandraDependencyProbeClient
{
    ValueTask<string> ProbeAsync(CassandraDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class CassandraDependencyProbeClient : ICassandraDependencyProbeClient
{
    public async ValueTask<string> ProbeAsync(CassandraDependencyDefinition dependency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);
        var contactPoints = NormalizeContactPoints(dependency.ContactPoints);
        if (contactPoints.Count == 0)
        {
            throw new InvalidOperationException("At least one Cassandra contact point must be configured.");
        }

        var query = string.IsNullOrWhiteSpace(dependency.HealthQuery)
            ? "SELECT release_version FROM system.local;"
            : dependency.HealthQuery.Trim();
        var port = dependency.Port > 0 ? dependency.Port : 9042;
        var clusterBuilder = Cluster.Builder()
            .AddContactPoints(contactPoints)
            .WithPort(port)
            .WithSocketOptions(new SocketOptions().SetConnectTimeoutMillis(timeoutSeconds * 1000));

        if (!string.IsNullOrWhiteSpace(dependency.Username))
        {
            clusterBuilder = clusterBuilder.WithCredentials(dependency.Username.Trim(), dependency.Password ?? string.Empty);
        }

        var cluster = clusterBuilder.Build();
        try
        {
            var session = string.IsNullOrWhiteSpace(dependency.Keyspace)
                ? await cluster.ConnectAsync().WaitAsync(cancellationToken).ConfigureAwait(false)
                : await cluster.ConnectAsync(dependency.Keyspace.Trim()).WaitAsync(cancellationToken).ConfigureAwait(false);

            await session.ExecuteAsync(new SimpleStatement(query)).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await cluster.ShutdownAsync().ConfigureAwait(false);
        }

        return $"Cassandra endpoints '{DescribeTarget(contactPoints, port, dependency.Keyspace)}' responded to health query.";
    }

    internal static IReadOnlyList<string> NormalizeContactPoints(IEnumerable<string>? contactPoints)
    {
        if (contactPoints is null)
        {
            return Array.Empty<string>();
        }

        return contactPoints
            .Select(static contactPoint => contactPoint?.Trim())
            .Where(static contactPoint => !string.IsNullOrWhiteSpace(contactPoint))
            .Cast<string>()
            .ToArray();
    }

    private static string DescribeTarget(IReadOnlyList<string> contactPoints, int port, string? keyspace)
    {
        var joinedContactPoints = string.Join(", ", contactPoints.Select(contactPoint => $"{contactPoint}:{port}"));
        return string.IsNullOrWhiteSpace(keyspace)
            ? joinedContactPoints
            : $"{joinedContactPoints}/{keyspace.Trim()}";
    }
}

internal static class CassandraDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(CassandraDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, CassandraDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        CassandraDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(CassandraDependencyHealthDiagnosticsConventions.ProbeFailed.Id, CassandraDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        CassandraDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
