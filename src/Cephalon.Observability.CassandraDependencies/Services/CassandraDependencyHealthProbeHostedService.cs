using Cassandra;
using Cephalon.Observability.CassandraDependencies.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.CassandraDependencies.Services;

internal sealed class CassandraDependencyHealthProbeHostedService(
    CassandraDependencyHealthOptions options,
    ICassandraDependencyProbeClient probeClient,
    DependencyHealthStore store,
    ILogger<CassandraDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<CassandraDependencyHealthOptions, CassandraDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.CassandraDependencies";
    protected override string DefaultDependencyId => "cassandra-dependency";
    protected override string ProviderLabel => "Cassandra";

    protected override string? ValidateDependency(CassandraDependencyDefinition definition)
    {
        var contactPoints = CassandraDependencyProbeClient.NormalizeContactPoints(definition.ContactPoints);
        return contactPoints.Count == 0 ? "Cassandra contact points are not configured." : null;
    }

    protected override ValueTask<string> ProbeAsync(CassandraDependencyDefinition definition, CancellationToken cancellationToken) =>
        probeClient.ProbeAsync(definition, cancellationToken);

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        CassandraDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        CassandraDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
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
