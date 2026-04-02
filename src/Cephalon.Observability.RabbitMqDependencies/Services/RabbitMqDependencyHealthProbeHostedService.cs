using Cephalon.Abstractions.Health;
using Cephalon.Observability.RabbitMqDependencies.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Cephalon.Observability.RabbitMqDependencies.Services;

internal sealed class RabbitMqDependencyHealthProbeHostedService(
    RabbitMqDependencyHealthOptions options,
    IRabbitMqDependencyProbeClient probeClient,
    RabbitMqDependencyHealthStore store,
    ILogger<RabbitMqDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.RabbitMqDependencies";
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
        RabbitMqDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "rabbitmq-dependency"
            : dependency.Id.Trim();
        var displayName = string.IsNullOrWhiteSpace(dependency.DisplayName)
            ? id
            : dependency.DisplayName.Trim();
        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);

        if (string.IsNullOrWhiteSpace(dependency.ConnectionString) && string.IsNullOrWhiteSpace(dependency.Host))
        {
            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: "RabbitMQ host or connection string is not configured.",
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
            RabbitMqDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"RabbitMQ dependency '{displayName}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            RabbitMqDependencyHealthLogs.ProbeFailed(logger, exception, id);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"RabbitMQ dependency '{displayName}' failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
}

internal interface IRabbitMqDependencyProbeClient
{
    ValueTask<string> ProbeAsync(RabbitMqDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class RabbitMqDependencyProbeClient : IRabbitMqDependencyProbeClient
{
    public async ValueTask<string> ProbeAsync(RabbitMqDependencyDefinition dependency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);
        var factory = CreateConnectionFactory(dependency, timeoutSeconds);

        await using var connection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        return $"RabbitMQ endpoint '{DescribeTarget(factory)}' accepted an AMQP connection.";
    }

    internal static ConnectionFactory CreateConnectionFactory(
        RabbitMqDependencyDefinition dependency,
        int timeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        ConnectionFactory factory;
        if (string.IsNullOrWhiteSpace(dependency.ConnectionString))
        {
            factory = new ConnectionFactory
            {
                HostName = dependency.Host?.Trim() ?? string.Empty,
                Port = dependency.Port > 0 ? dependency.Port : 5672,
                VirtualHost = string.IsNullOrWhiteSpace(dependency.VirtualHost) ? "/" : dependency.VirtualHost.Trim()
            };

            if (!string.IsNullOrWhiteSpace(dependency.Username))
            {
                factory.UserName = dependency.Username.Trim();
            }

            if (dependency.Password is not null)
            {
                factory.Password = dependency.Password;
            }
        }
        else
        {
            factory = new ConnectionFactory
            {
                Uri = new Uri(dependency.ConnectionString, UriKind.Absolute)
            };
        }

        if (dependency.UseTls)
        {
            factory.Ssl.Enabled = true;
            if (string.IsNullOrWhiteSpace(factory.Ssl.ServerName) && !string.IsNullOrWhiteSpace(factory.HostName))
            {
                factory.Ssl.ServerName = factory.HostName;
            }
        }

        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        factory.ClientProvidedName = "Cephalon.DependencyHealth.RabbitMq";
        factory.RequestedConnectionTimeout = timeout;
        factory.HandshakeContinuationTimeout = timeout;
        factory.SocketReadTimeout = timeout;
        factory.SocketWriteTimeout = timeout;
        factory.RequestedHeartbeat = timeout;
        factory.AutomaticRecoveryEnabled = false;
        factory.TopologyRecoveryEnabled = false;

        return factory;
    }

    private static string DescribeTarget(ConnectionFactory factory)
    {
        var host = string.IsNullOrWhiteSpace(factory.HostName) ? "(host unspecified)" : factory.HostName;
        var virtualHost = string.IsNullOrWhiteSpace(factory.VirtualHost) ? "/" : factory.VirtualHost;
        return $"{host}:{factory.Port}{virtualHost}";
    }
}

internal static class RabbitMqDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(RabbitMqDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, RabbitMqDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        RabbitMqDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(RabbitMqDependencyHealthDiagnosticsConventions.ProbeFailed.Id, RabbitMqDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        RabbitMqDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
