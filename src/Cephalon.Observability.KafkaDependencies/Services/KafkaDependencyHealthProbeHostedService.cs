using Cephalon.Abstractions.Health;
using Cephalon.Observability.KafkaDependencies.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.KafkaDependencies.Services;

internal sealed class KafkaDependencyHealthProbeHostedService(
    KafkaDependencyHealthOptions options,
    IKafkaDependencyProbeClient probeClient,
    KafkaDependencyHealthStore store,
    ILogger<KafkaDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.KafkaDependencies";
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
        KafkaDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "kafka-dependency"
            : dependency.Id.Trim();
        var displayName = string.IsNullOrWhiteSpace(dependency.DisplayName)
            ? id
            : dependency.DisplayName.Trim();
        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);

        if (string.IsNullOrWhiteSpace(dependency.BootstrapServers))
        {
            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: "Kafka bootstrap servers are not configured.",
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
            KafkaDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Kafka dependency '{displayName}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            KafkaDependencyHealthLogs.ProbeFailed(logger, exception, id);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Kafka dependency '{displayName}' failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
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
