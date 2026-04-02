using Cephalon.Abstractions.Health;
using Cephalon.Observability.MySqlDependencies.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Cephalon.Observability.MySqlDependencies.Services;

internal sealed class MySqlDependencyHealthProbeHostedService(
    MySqlDependencyHealthOptions options,
    IMySqlDependencyProbeClient probeClient,
    MySqlDependencyHealthStore store,
    ILogger<MySqlDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.MySqlDependencies";
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
        MySqlDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "mysql-dependency"
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
                Description: "MySQL host or connection string is not configured.",
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
            MySqlDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"MySQL dependency '{displayName}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            MySqlDependencyHealthLogs.ProbeFailed(logger, exception, id);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"MySQL dependency '{displayName}' failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
}

internal interface IMySqlDependencyProbeClient
{
    ValueTask<string> ProbeAsync(MySqlDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class MySqlDependencyProbeClient : IMySqlDependencyProbeClient
{
    public async ValueTask<string> ProbeAsync(MySqlDependencyDefinition dependency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);
        var query = string.IsNullOrWhiteSpace(dependency.HealthQuery)
            ? "SELECT 1;"
            : dependency.HealthQuery.Trim();
        var builder = CreateConnectionStringBuilder(dependency, timeoutSeconds);

        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = new MySqlCommand(query, connection)
        {
            CommandTimeout = timeoutSeconds
        };

        await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return $"MySQL endpoint '{DescribeTarget(builder)}' responded to health query.";
    }

    internal static MySqlConnectionStringBuilder CreateConnectionStringBuilder(
        MySqlDependencyDefinition dependency,
        int timeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        MySqlConnectionStringBuilder builder = string.IsNullOrWhiteSpace(dependency.ConnectionString)
            ? new MySqlConnectionStringBuilder
            {
                Server = dependency.Host?.Trim() ?? string.Empty,
                Port = (uint)(dependency.Port > 0 ? dependency.Port : 3306),
                Database = string.IsNullOrWhiteSpace(dependency.Database) ? "mysql" : dependency.Database.Trim(),
                UserID = dependency.Username?.Trim(),
                Password = dependency.Password
            }
            : new MySqlConnectionStringBuilder(dependency.ConnectionString);

        if (!string.IsNullOrWhiteSpace(dependency.Username))
        {
            builder.UserID = dependency.Username.Trim();
        }

        if (dependency.Password is not null)
        {
            builder.Password = dependency.Password;
        }

        if (!string.IsNullOrWhiteSpace(dependency.SslMode))
        {
            builder.SslMode = ParseSslMode(dependency.SslMode);
        }

        if (dependency.AllowPublicKeyRetrieval.HasValue)
        {
            builder.AllowPublicKeyRetrieval = dependency.AllowPublicKeyRetrieval.Value;
        }

        builder.ApplicationName = "Cephalon.DependencyHealth.MySql";
        builder.ConnectionTimeout = (uint)timeoutSeconds;
        builder.DefaultCommandTimeout = (uint)timeoutSeconds;
        builder.Pooling = false;

        return builder;
    }

    private static string DescribeTarget(MySqlConnectionStringBuilder builder)
    {
        var server = string.IsNullOrWhiteSpace(builder.Server) ? "(server unspecified)" : builder.Server;
        var database = string.IsNullOrWhiteSpace(builder.Database) ? "(database unspecified)" : builder.Database;
        return $"{server}:{builder.Port}/{database}";
    }

    private static MySqlSslMode ParseSslMode(string? value) =>
        Enum.TryParse<MySqlSslMode>(value, ignoreCase: true, out var parsed)
            ? parsed
            : MySqlSslMode.Preferred;
}

internal static class MySqlDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(MySqlDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, MySqlDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        MySqlDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(MySqlDependencyHealthDiagnosticsConventions.ProbeFailed.Id, MySqlDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        MySqlDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
