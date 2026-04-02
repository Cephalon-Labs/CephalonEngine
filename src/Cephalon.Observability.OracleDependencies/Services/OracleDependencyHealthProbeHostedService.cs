using Cephalon.Abstractions.Health;
using Cephalon.Observability.OracleDependencies.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace Cephalon.Observability.OracleDependencies.Services;

internal sealed class OracleDependencyHealthProbeHostedService(
    OracleDependencyHealthOptions options,
    IOracleDependencyProbeClient probeClient,
    OracleDependencyHealthStore store,
    ILogger<OracleDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.OracleDependencies";
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
        OracleDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "oracle-dependency"
            : dependency.Id.Trim();
        var displayName = string.IsNullOrWhiteSpace(dependency.DisplayName)
            ? id
            : dependency.DisplayName.Trim();
        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);

        if (string.IsNullOrWhiteSpace(dependency.ConnectionString) &&
            (string.IsNullOrWhiteSpace(dependency.Host) || string.IsNullOrWhiteSpace(dependency.ServiceName)))
        {
            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: "Oracle host/service name or connection string is not configured.",
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
            OracleDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Oracle dependency '{displayName}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            OracleDependencyHealthLogs.ProbeFailed(logger, exception, id);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Oracle dependency '{displayName}' failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
}

internal interface IOracleDependencyProbeClient
{
    ValueTask<string> ProbeAsync(OracleDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class OracleDependencyProbeClient : IOracleDependencyProbeClient
{
    public async ValueTask<string> ProbeAsync(OracleDependencyDefinition dependency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);
        var query = string.IsNullOrWhiteSpace(dependency.HealthQuery)
            ? "SELECT 1 FROM DUAL"
            : dependency.HealthQuery.Trim();
        var builder = CreateConnectionStringBuilder(dependency, timeoutSeconds);

        await using var connection = new OracleConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = timeoutSeconds;

        await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return $"Oracle endpoint '{DescribeTarget(builder)}' responded to health query.";
    }

    internal static OracleConnectionStringBuilder CreateConnectionStringBuilder(
        OracleDependencyDefinition dependency,
        int timeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        OracleConnectionStringBuilder builder = string.IsNullOrWhiteSpace(dependency.ConnectionString)
            ? new OracleConnectionStringBuilder
            {
                DataSource = CreateDataSource(dependency),
                UserID = dependency.Username?.Trim(),
                Password = dependency.Password
            }
            : new OracleConnectionStringBuilder(dependency.ConnectionString);

        if (!string.IsNullOrWhiteSpace(dependency.Username))
        {
            builder.UserID = dependency.Username.Trim();
        }

        if (dependency.Password is not null)
        {
            builder.Password = dependency.Password;
        }

        builder["Connection Timeout"] = timeoutSeconds;
        builder["Pooling"] = false;

        return builder;
    }

    private static string CreateDataSource(OracleDependencyDefinition dependency)
    {
        var host = dependency.Host?.Trim() ?? string.Empty;
        var port = dependency.Port > 0 ? dependency.Port : 1521;
        var serviceName = dependency.ServiceName?.Trim() ?? string.Empty;

        return string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(serviceName)
            ? string.Empty
            : $"{host}:{port}/{serviceName}";
    }

    private static string DescribeTarget(OracleConnectionStringBuilder builder)
    {
        var dataSource = builder.DataSource;
        return string.IsNullOrWhiteSpace(dataSource)
            ? "(server unspecified)"
            : dataSource;
    }
}

internal static class OracleDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(OracleDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, OracleDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        OracleDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(OracleDependencyHealthDiagnosticsConventions.ProbeFailed.Id, OracleDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        OracleDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
