using System.Net.Sockets;
using System.Text;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.MemcachedDependencies.Configuration;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.MemcachedDependencies.Services;

internal sealed class MemcachedDependencyHealthProbeHostedService(
    MemcachedDependencyHealthOptions options,
    DependencyHealthStore store,
    ILogger<MemcachedDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<MemcachedDependencyHealthOptions, MemcachedDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.MemcachedDependencies";
    protected override string DefaultDependencyId => "memcached-dependency";
    protected override string ProviderLabel => "Memcached";

    protected override string? ValidateDependency(MemcachedDependencyDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.Host?.Trim())
            ? "Memcached host is not configured."
            : null;
    }

    protected override async ValueTask<string> ProbeAsync(MemcachedDependencyDefinition dependency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var host = dependency.Host.Trim();
        var port = dependency.Port > 0 ? dependency.Port : 11211;

        using var client = new TcpClient();
        await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

        await using var stream = client.GetStream();
        await WriteCommandAsync(stream, "version", cancellationToken).ConfigureAwait(false);
        var response = await ReadLineAsync(stream, cancellationToken).ConfigureAwait(false);

        if (!response.StartsWith("VERSION ", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Expected VERSION response but received '{response}'.");
        }

        var version = response["VERSION ".Length..].Trim();
        return $"Memcached endpoint '{host}:{port}' responded to version probe with '{version}'.";
    }

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        MemcachedDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        MemcachedDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);

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
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(MemcachedDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, MemcachedDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        "Memcached dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.");

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(MemcachedDependencyHealthDiagnosticsConventions.ProbeFailed.Id, MemcachedDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        "Memcached dependency probe '{DependencyId}' failed.");

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
