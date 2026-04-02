using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Health;
using Cephalon.Observability.NatsDependencies.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.NatsDependencies.Services;

internal sealed class NatsDependencyHealthProbeHostedService(
    NatsDependencyHealthOptions options,
    INatsDependencyProbeClient probeClient,
    NatsDependencyHealthStore store,
    ILogger<NatsDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.NatsDependencies";
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
        NatsDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "nats-dependency"
            : dependency.Id.Trim();
        var displayName = string.IsNullOrWhiteSpace(dependency.DisplayName)
            ? id
            : dependency.DisplayName.Trim();
        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);

        if (string.IsNullOrWhiteSpace(dependency.Host))
        {
            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: "NATS host is not configured.",
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
            NatsDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"NATS dependency '{displayName}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            NatsDependencyHealthLogs.ProbeFailed(logger, exception, id);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"NATS dependency '{displayName}' failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
}

internal interface INatsDependencyProbeClient
{
    ValueTask<string> ProbeAsync(NatsDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class NatsDependencyProbeClient : INatsDependencyProbeClient
{
    private const string DefaultClientName = "Cephalon.DependencyHealth.Nats";
    private const string ClientLanguage = "csharp";
    private static readonly string ClientVersion = typeof(NatsDependencyProbeClient).Assembly.GetName().Version?.ToString() ?? "1.0.0";

    public async ValueTask<string> ProbeAsync(NatsDependencyDefinition dependency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var host = dependency.Host.Trim();
        var port = dependency.Port > 0 ? dependency.Port : 4222;

        using var client = new TcpClient
        {
            NoDelay = true
        };

        await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

        Stream stream = client.GetStream();
        var serverInfo = ParseInfoLine(await ReadLineAsync(stream, cancellationToken).ConfigureAwait(false));

        if (serverInfo.TlsRequired && !dependency.UseTls)
        {
            throw new InvalidOperationException("NATS server requires TLS but the dependency configuration does not enable UseTls.");
        }

        if (serverInfo.AuthRequired &&
            string.IsNullOrWhiteSpace(dependency.Token) &&
            string.IsNullOrWhiteSpace(dependency.Username))
        {
            throw new InvalidOperationException("NATS server requires authentication but the dependency configuration does not declare Token or Username.");
        }

        if (dependency.UseTls)
        {
            var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
            await sslStream
                .AuthenticateAsClientAsync(
                    new SslClientAuthenticationOptions
                    {
                        TargetHost = ResolveTlsServerName(dependency),
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            stream = sslStream;
        }

        await WriteLineAsync(stream, CreateConnectCommand(dependency), cancellationToken).ConfigureAwait(false);
        await WriteLineAsync(stream, "PING", cancellationToken).ConfigureAwait(false);
        await WaitForPongAsync(stream, cancellationToken).ConfigureAwait(false);

        var target = $"{host}:{port}";
        var description = $"NATS endpoint '{target}' responded to CONNECT and PING/PONG";
        if (!string.IsNullOrWhiteSpace(serverInfo.ServerName))
        {
            description += $" on server '{serverInfo.ServerName}'";
        }

        if (!string.IsNullOrWhiteSpace(serverInfo.Version))
        {
            description += $" running version '{serverInfo.Version}'";
        }

        if (!string.IsNullOrWhiteSpace(serverInfo.Cluster))
        {
            description += $" in cluster '{serverInfo.Cluster}'";
        }

        description += ".";
        return description;
    }

    internal static string CreateConnectCommand(NatsDependencyDefinition dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["verbose"] = false,
            ["pedantic"] = false,
            ["tls_required"] = dependency.UseTls,
            ["name"] = string.IsNullOrWhiteSpace(dependency.ClientName) ? DefaultClientName : dependency.ClientName.Trim(),
            ["lang"] = ClientLanguage,
            ["version"] = ClientVersion,
            ["protocol"] = 1,
            ["echo"] = false
        };

        if (!string.IsNullOrWhiteSpace(dependency.Token))
        {
            payload["auth_token"] = dependency.Token.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(dependency.Username))
        {
            payload["user"] = dependency.Username.Trim();
            if (dependency.Password is not null)
            {
                payload["pass"] = dependency.Password;
            }
        }

        return $"CONNECT {JsonSerializer.Serialize(payload)}";
    }

    internal static NatsServerInfo ParseInfoLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            throw new InvalidOperationException("NATS server did not return the initial INFO line.");
        }

        if (!line.StartsWith("INFO ", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unexpected NATS server greeting '{line}'.");
        }

        var payload = line["INFO ".Length..].Trim();
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        return new NatsServerInfo(
            ServerName: GetString(root, "server_name") ?? GetString(root, "server_id"),
            Version: GetString(root, "version"),
            Host: GetString(root, "host"),
            Port: GetInt32(root, "port"),
            Cluster: GetString(root, "cluster"),
            AuthRequired: GetBoolean(root, "auth_required"),
            TlsRequired: GetBoolean(root, "tls_required") || GetBoolean(root, "ssl_required"));
    }

    private static async Task WaitForPongAsync(Stream stream, CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await ReadLineAsync(stream, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("INFO ", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("+OK", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.StartsWith("-ERR", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"NATS server rejected the probe: {NormalizeError(line)}");
            }

            if (string.Equals(line, "PONG", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new InvalidOperationException($"Unexpected NATS response '{line}'.");
        }
    }

    private static string NormalizeError(string line)
    {
        var value = line["-ERR".Length..].Trim();
        return value.Trim('\'', '"');
    }

    private static string ResolveTlsServerName(NatsDependencyDefinition dependency) =>
        string.IsNullOrWhiteSpace(dependency.TlsServerName)
            ? dependency.Host.Trim()
            : dependency.TlsServerName.Trim();

    private static async Task WriteLineAsync(Stream stream, string line, CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetBytes($"{line}\r\n");
        await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> ReadLineAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var singleByte = new byte[1];

        while (true)
        {
            var bytesRead = await stream.ReadAsync(singleByte, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new IOException("The NATS connection closed before the probe completed.");
            }

            if (singleByte[0] == (byte)'\n')
            {
                break;
            }

            if (singleByte[0] != (byte)'\r')
            {
                buffer.WriteByte(singleByte[0]);
            }
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static int? GetInt32(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : null;

    private static bool GetBoolean(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind is JsonValueKind.True or JsonValueKind.False &&
        property.GetBoolean();
}

internal sealed record NatsServerInfo(
    string? ServerName,
    string? Version,
    string? Host,
    int? Port,
    string? Cluster,
    bool AuthRequired,
    bool TlsRequired);

internal static class NatsDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(NatsDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, NatsDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        NatsDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(NatsDependencyHealthDiagnosticsConventions.ProbeFailed.Id, NatsDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        NatsDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
