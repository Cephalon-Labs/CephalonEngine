using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Cephalon.Abstractions.Health;
using Cephalon.Observability.RedisDependencies.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.RedisDependencies.Services;

internal sealed class RedisDependencyHealthProbeHostedService(
    RedisDependencyHealthOptions options,
    RedisDependencyHealthStore store,
    ILogger<RedisDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.RedisDependencies";
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
        RedisDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "redis-dependency"
            : dependency.Id.Trim();
        var displayName = string.IsNullOrWhiteSpace(dependency.DisplayName)
            ? id
            : dependency.DisplayName.Trim();
        var host = dependency.Host?.Trim() ?? string.Empty;
        var port = dependency.Port > 0 ? dependency.Port : 6379;
        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);

        if (string.IsNullOrWhiteSpace(host))
        {
            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: "Redis host is not configured.",
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

            if (!string.IsNullOrWhiteSpace(dependency.Password))
            {
                await SendCommandAsync(
                    stream,
                    string.IsNullOrWhiteSpace(dependency.Username)
                        ? ["AUTH", dependency.Password]
                        : ["AUTH", dependency.Username!, dependency.Password],
                    timeoutSource.Token).ConfigureAwait(false);

                var authResponse = await ReadSimpleResponseAsync(stream, timeoutSource.Token).ConfigureAwait(false);
                EnsureOkResponse(authResponse, "AUTH");
            }

            if (dependency.Database is int database && database > 0)
            {
                await SendCommandAsync(stream, ["SELECT", database.ToString(CultureInfo.InvariantCulture)], timeoutSource.Token).ConfigureAwait(false);
                var selectResponse = await ReadSimpleResponseAsync(stream, timeoutSource.Token).ConfigureAwait(false);
                EnsureOkResponse(selectResponse, "SELECT");
            }

            await SendCommandAsync(stream, ["PING"], timeoutSource.Token).ConfigureAwait(false);
            var pingResponse = await ReadSimpleResponseAsync(stream, timeoutSource.Token).ConfigureAwait(false);

            if (!string.Equals(pingResponse, "PONG", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Expected PONG but received '{pingResponse}'.");
            }

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Healthy,
                Description: $"Redis endpoint '{host}:{port}' responded to PING.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            RedisDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds, host, port);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Redis endpoint '{host}:{port}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            RedisDependencyHealthLogs.ProbeFailed(logger, exception, id, host, port);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Redis endpoint '{host}:{port}' failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }

    private static async Task SendCommandAsync(NetworkStream stream, IReadOnlyList<string> parts, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.Append('*').Append(parts.Count).Append("\r\n");
        foreach (var part in parts)
        {
            var value = part ?? string.Empty;
            var valueBytes = Encoding.UTF8.GetByteCount(value);
            builder.Append('$').Append(valueBytes).Append("\r\n").Append(value).Append("\r\n");
        }

        var payload = Encoding.UTF8.GetBytes(builder.ToString());
        await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> ReadSimpleResponseAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        var buffer = new byte[1];
        var prefix = '\0';
        var sawCarriageReturn = false;

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new IOException("Redis endpoint closed the connection before returning a response.");
            }

            var current = (char)buffer[0];
            if (prefix == '\0')
            {
                prefix = current;
                continue;
            }

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

        if (prefix == '-')
        {
            throw new InvalidOperationException(builder.ToString());
        }

        return builder.ToString();
    }

    private static void EnsureOkResponse(string response, string commandName)
    {
        if (!string.Equals(response, "OK", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{commandName} did not return OK. Received '{response}'.");
        }
    }
}

internal static class RedisDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int, string, int>(
        LogLevel.Warning,
        new EventId(3120, nameof(ProbeTimedOut)),
        "Redis dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s against {Host}:{Port}.");

    private static readonly Action<ILogger, string, string, int, Exception?> ProbeFailedMessage = LoggerMessage.Define<string, string, int>(
        LogLevel.Warning,
        new EventId(3121, nameof(ProbeFailed)),
        "Redis dependency probe '{DependencyId}' failed against {Host}:{Port}.");

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds, string host, int port) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, host, port, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId, string host, int port) =>
        ProbeFailedMessage(logger, dependencyId, host, port, exception);
}
