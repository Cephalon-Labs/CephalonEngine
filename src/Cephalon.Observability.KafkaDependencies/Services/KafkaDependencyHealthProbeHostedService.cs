using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.KafkaDependencies.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.KafkaDependencies.Services;

internal sealed class KafkaDependencyHealthProbeHostedService(
    KafkaDependencyHealthOptions options,
    IKafkaDependencyProbeClient probeClient,
    DependencyHealthStore store,
    ILogger<KafkaDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<KafkaDependencyHealthOptions, KafkaDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.KafkaDependencies";
    protected override string DefaultDependencyId => "kafka-dependency";
    protected override string ProviderLabel => "Kafka";

    protected override string? ValidateDependency(KafkaDependencyDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.BootstrapServers)
            ? "Kafka bootstrap servers are not configured."
            : null;
    }

    protected override ValueTask<string> ProbeAsync(KafkaDependencyDefinition definition, CancellationToken cancellationToken) =>
        probeClient.ProbeAsync(definition, cancellationToken);

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        KafkaDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        KafkaDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
}

internal interface IKafkaDependencyProbeClient
{
    ValueTask<string> ProbeAsync(KafkaDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class KafkaDependencyProbeClient : IKafkaDependencyProbeClient
{
    public async ValueTask<string> ProbeAsync(KafkaDependencyDefinition dependency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);
        var config = CreateAdminClientConfig(dependency, timeoutSeconds);
        using var client = new AdminClientBuilder(config).Build();

        Metadata metadata;
        var topic = dependency.Topic?.Trim();
        if (string.IsNullOrWhiteSpace(topic))
        {
            metadata = await Task
                .Run(() => client.GetMetadata(TimeSpan.FromSeconds(timeoutSeconds)), cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            metadata = await Task
                .Run(() => client.GetMetadata(topic, TimeSpan.FromSeconds(timeoutSeconds)), cancellationToken)
                .ConfigureAwait(false);

            var topicMetadata = metadata.Topics.FirstOrDefault(candidate =>
                string.Equals(candidate.Topic, topic, StringComparison.Ordinal));
            if (topicMetadata is null)
            {
                throw new InvalidOperationException($"Kafka topic '{topic}' metadata was not returned.");
            }

            if (topicMetadata.Error.IsError)
            {
                throw new InvalidOperationException($"Kafka topic '{topic}' metadata failed: {topicMetadata.Error.Reason}");
            }

            return $"Kafka cluster '{DescribeTarget(dependency)}' returned metadata for topic '{topic}' using {metadata.Brokers.Count} broker(s).";
        }

        if (metadata.Brokers.Count == 0)
        {
            throw new InvalidOperationException("Kafka cluster metadata returned no brokers.");
        }

        return $"Kafka cluster '{DescribeTarget(dependency)}' returned metadata for {metadata.Brokers.Count} broker(s).";
    }

    internal static AdminClientConfig CreateAdminClientConfig(
        KafkaDependencyDefinition dependency,
        int timeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var timeoutMilliseconds = Math.Max(1, timeoutSeconds) * 1000;
        var config = new AdminClientConfig
        {
            BootstrapServers = dependency.BootstrapServers.Trim(),
            ClientId = string.IsNullOrWhiteSpace(dependency.ClientId)
                ? "Cephalon.DependencyHealth.Kafka"
                : dependency.ClientId.Trim(),
            SocketTimeoutMs = timeoutMilliseconds
        };

        if (!string.IsNullOrWhiteSpace(dependency.SecurityProtocol))
        {
            config.SecurityProtocol = ParseSecurityProtocol(dependency.SecurityProtocol);
        }

        if (!string.IsNullOrWhiteSpace(dependency.SaslMechanism))
        {
            config.SaslMechanism = ParseSaslMechanism(dependency.SaslMechanism);
        }

        if (!string.IsNullOrWhiteSpace(dependency.Username))
        {
            config.SaslUsername = dependency.Username.Trim();
        }

        if (dependency.Password is not null)
        {
            config.SaslPassword = dependency.Password;
        }

        return config;
    }

    private static string DescribeTarget(KafkaDependencyDefinition dependency) =>
        string.IsNullOrWhiteSpace(dependency.BootstrapServers)
            ? "(bootstrap servers unspecified)"
            : dependency.BootstrapServers.Trim();

    private static SecurityProtocol ParseSecurityProtocol(string value) =>
        Enum.TryParse<SecurityProtocol>(value, ignoreCase: true, out var parsed)
            ? parsed
            : SecurityProtocol.Plaintext;

    private static SaslMechanism ParseSaslMechanism(string value) =>
        Enum.TryParse<SaslMechanism>(value, ignoreCase: true, out var parsed)
            ? parsed
            : SaslMechanism.Plain;
}

internal static class KafkaDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(KafkaDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, KafkaDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        KafkaDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(KafkaDependencyHealthDiagnosticsConventions.ProbeFailed.Id, KafkaDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        KafkaDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
