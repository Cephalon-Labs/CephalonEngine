using System.Collections.Specialized;
using System.Text;
using Cephalon.Abstractions.Health;
using Cephalon.Observability.ClickHouseDependencies.Configuration;
using ClickHouse.Driver.ADO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.ClickHouseDependencies.Services;

internal sealed class ClickHouseDependencyHealthProbeHostedService(
    ClickHouseDependencyHealthOptions options,
    IClickHouseDependencyProbeClient probeClient,
    ClickHouseDependencyHealthStore store,
    ILogger<ClickHouseDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.ClickHouseDependencies";
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
        ClickHouseDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "clickhouse-dependency"
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
                Description: "ClickHouse host or connection string is not configured.",
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
            ClickHouseDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"ClickHouse dependency '{displayName}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            ClickHouseDependencyHealthLogs.ProbeFailed(logger, exception, id);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"ClickHouse dependency '{displayName}' failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
}

internal interface IClickHouseDependencyProbeClient
{
    ValueTask<string> ProbeAsync(ClickHouseDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class ClickHouseDependencyProbeClient : IClickHouseDependencyProbeClient
{
    public async ValueTask<string> ProbeAsync(ClickHouseDependencyDefinition dependency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);
        var query = string.IsNullOrWhiteSpace(dependency.HealthQuery)
            ? "SELECT 1"
            : dependency.HealthQuery.Trim();
        var connectionString = CreateConnectionString(dependency);

        using var connection = new ClickHouseConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = timeoutSeconds;

        _ = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return $"ClickHouse endpoint '{DescribeTarget(connectionString)}' responded to health query.";
    }

    internal static string CreateConnectionString(ClickHouseDependencyDefinition dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var values = ParseConnectionString(dependency.ConnectionString);

        Apply(values, "Host", dependency.Host);
        Apply(values, "Protocol", dependency.Protocol);
        Apply(values, "Port", dependency.Port > 0 ? dependency.Port.ToString(System.Globalization.CultureInfo.InvariantCulture) : null);
        Apply(values, "Database", dependency.Database);
        Apply(values, "Username", dependency.Username);

        if (dependency.Password is not null)
        {
            values["Password"] = dependency.Password;
        }

        if (values.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        AppendOrdered(builder, values, "Host");
        AppendOrdered(builder, values, "Protocol");
        AppendOrdered(builder, values, "Port");
        AppendOrdered(builder, values, "Database");
        AppendOrdered(builder, values, "Username");
        AppendOrdered(builder, values, "Password");

        foreach (var key in values.AllKeys
                     .Where(static key => key is not null)
                     .Cast<string>()
                     .OrderBy(static key => key, StringComparer.OrdinalIgnoreCase))
        {
            AppendSegment(builder, key, values[key]);
        }

        return builder.ToString();
    }

    private static NameValueCollection ParseConnectionString(string? connectionString)
    {
        var values = new NameValueCollection(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return values;
        }

        var segments = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = segment[..separatorIndex].Trim();
            var value = segment[(separatorIndex + 1)..].Trim();

            if (key.Length > 0)
            {
                values[key] = value;
            }
        }

        return values;
    }

    private static void Apply(NameValueCollection values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values[key] = value.Trim();
        }
    }

    private static void AppendOrdered(StringBuilder builder, NameValueCollection values, string key)
    {
        if (values[key] is not null)
        {
            AppendSegment(builder, key, values[key]);
            values.Remove(key);
        }
    }

    private static void AppendSegment(StringBuilder builder, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key) || value is null)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append(';');
        }

        builder.Append(key).Append('=').Append(value);
    }

    private static string DescribeTarget(string connectionString)
    {
        var values = ParseConnectionString(connectionString);
        var protocol = values["Protocol"] ?? "http";
        var host = values["Host"] ?? "(server unspecified)";
        var port = values["Port"];
        var database = values["Database"];

        var builder = new StringBuilder();
        builder.Append(protocol).Append("://").Append(host);

        if (!string.IsNullOrWhiteSpace(port))
        {
            builder.Append(':').Append(port);
        }

        if (!string.IsNullOrWhiteSpace(database))
        {
            builder.Append('/').Append(database);
        }

        return builder.ToString();
    }
}

internal static class ClickHouseDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(ClickHouseDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, ClickHouseDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        ClickHouseDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(ClickHouseDependencyHealthDiagnosticsConventions.ProbeFailed.Id, ClickHouseDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        ClickHouseDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
