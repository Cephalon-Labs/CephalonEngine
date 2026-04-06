using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.RedisDependencies.Configuration;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.RedisDependencies.Services;

internal sealed class RedisDependencyHealthProbeHostedService(
    RedisDependencyHealthOptions options,
    DependencyHealthStore store,
    ILogger<RedisDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<RedisDependencyHealthOptions, RedisDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.RedisDependencies";
    protected override string DefaultDependencyId => "redis-dependency";
    protected override string ProviderLabel => "Redis";

    protected override string? ValidateDependency(RedisDependencyDefinition definition)
    {
        var host = definition.Host?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(host) ? "Redis host is not configured." : null;
    }

    protected override async ValueTask<string> ProbeAsync(RedisDependencyDefinition definition, CancellationToken cancellationToken)
    {
        var host = definition.Host.Trim();
        var port = definition.Port > 0 ? definition.Port : 6379;

        using var client = new TcpClient();
        await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

        await using var stream = client.GetStream();

        if (!string.IsNullOrWhiteSpace(definition.Password))
        {
            await SendCommandAsync(
                stream,
                string.IsNullOrWhiteSpace(definition.Username)
                    ? ["AUTH", definition.Password]
                    : ["AUTH", definition.Username!, definition.Password],
                cancellationToken).ConfigureAwait(false);

            var authResponse = await ReadSimpleResponseAsync(stream, cancellationToken).ConfigureAwait(false);
            EnsureOkResponse(authResponse, "AUTH");
        }

        if (definition.Database is int database && database > 0)
        {
            await SendCommandAsync(stream, ["SELECT", database.ToString(CultureInfo.InvariantCulture)], cancellationToken).ConfigureAwait(false);
            var selectResponse = await ReadSimpleResponseAsync(stream, cancellationToken).ConfigureAwait(false);
            EnsureOkResponse(selectResponse, "SELECT");
        }

        await SendCommandAsync(stream, ["PING"], cancellationToken).ConfigureAwait(false);
        var pingResponse = await ReadSimpleResponseAsync(stream, cancellationToken).ConfigureAwait(false);

        if (!string.Equals(pingResponse, "PONG", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Expected PONG but received '{pingResponse}'.");
        }

        return $"Redis endpoint '{host}:{port}' responded to PING.";
    }

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        RedisDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        RedisDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);

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
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(RedisDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, RedisDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        "Redis dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.");

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(RedisDependencyHealthDiagnosticsConventions.ProbeFailed.Id, RedisDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        "Redis dependency probe '{DependencyId}' failed.");

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
