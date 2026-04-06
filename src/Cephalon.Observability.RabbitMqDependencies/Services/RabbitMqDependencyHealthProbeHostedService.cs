using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.RabbitMqDependencies.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Cephalon.Observability.RabbitMqDependencies.Services;

internal sealed class RabbitMqDependencyHealthProbeHostedService(
    RabbitMqDependencyHealthOptions options,
    IRabbitMqDependencyProbeClient probeClient,
    DependencyHealthStore store,
    ILogger<RabbitMqDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<RabbitMqDependencyHealthOptions, RabbitMqDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.RabbitMqDependencies";
    protected override string DefaultDependencyId => "rabbitmq-dependency";
    protected override string ProviderLabel => "RabbitMQ";

    protected override string? ValidateDependency(RabbitMqDependencyDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.ConnectionString) && string.IsNullOrWhiteSpace(definition.Host)
            ? "RabbitMQ host or connection string is not configured."
            : null;
    }

    protected override ValueTask<string> ProbeAsync(RabbitMqDependencyDefinition definition, CancellationToken cancellationToken) =>
        probeClient.ProbeAsync(definition, cancellationToken);

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        RabbitMqDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        RabbitMqDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
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
