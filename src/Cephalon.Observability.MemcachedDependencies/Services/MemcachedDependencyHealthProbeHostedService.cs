using System.IO;
using System.Net.Sockets;
using System.Text;
using Cephalon.Abstractions.Health;
using Cephalon.Observability.MemcachedDependencies.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.MemcachedDependencies.Services;

internal sealed class MemcachedDependencyHealthProbeHostedService(
    MemcachedDependencyHealthOptions options,
    MemcachedDependencyHealthStore store,
    ILogger<MemcachedDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.MemcachedDependencies";
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
        MemcachedDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "memcached-dependency"
            : dependency.Id.Trim();
        var displayName = string.IsNullOrWhiteSpace(dependency.DisplayName)
            ? id
            : dependency.DisplayName.Trim();
        var host = dependency.Host?.Trim() ?? string.Empty;
        var port = dependency.Port > 0 ? dependency.Port : 11211;
        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);

        if (string.IsNullOrWhiteSpace(host))
        {
            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: "Memcached host is not configured.",
                Required: dependency.Required,
                Source: SourceName);
        }

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using var client = new TcpClient();
            await client.ConnectAsync(host, port, timeoutSource.Token).ConfigureAwait(false);

            await using var stream = client.GetStream();
            await WriteCommandAsync(stream, "version", timeoutSource.Token).ConfigureAwait(false);
            var response = await ReadLineAsync(stream, timeoutSource.Token).ConfigureAwait(false);

            if (!response.StartsWith("VERSION ", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Expected VERSION response but received '{response}'.");
            }

            var version = response["VERSION ".Length..].Trim();
            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Healthy,
                Description: $"Memcached endpoint '{host}:{port}' responded to version probe with '{version}'.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            MemcachedDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds, host, port);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Memcached endpoint '{host}:{port}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            MemcachedDependencyHealthLogs.ProbeFailed(logger, exception, id, host, port);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Memcached endpoint '{host}:{port}' failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }

    private static async Task WriteCommandAsync(NetworkStream stream, string command, CancellationToken cancellationToken)
    {
        var payload = Encoding.ASCII.GetBytes($"{command}\r\n");
        await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> ReadLineAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        var buffer = new byte[1];
        var sawCarriageReturn = false;

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new IOException("Memcached endpoint closed the connection before returning a response.");
            }

            var current = (char)buffer[0];
            if (sawCarriageReturn)
            {
                if (current == '\n')
                {
                    break;
                }

                builder.Append('\r');
                sawCarriageReturn = false;
            }

            if (current == '\r')
            {
                sawCarriageReturn = true;
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}

internal static class MemcachedDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int, string, int>(
        LogLevel.Warning,
        new EventId(MemcachedDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, MemcachedDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        MemcachedDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, string, int, Exception?> ProbeFailedMessage = LoggerMessage.Define<string, string, int>(
        LogLevel.Warning,
        new EventId(MemcachedDependencyHealthDiagnosticsConventions.ProbeFailed.Id, MemcachedDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        MemcachedDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds, string host, int port) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, host, port, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId, string host, int port) =>
        ProbeFailedMessage(logger, dependencyId, host, port, exception);
}
