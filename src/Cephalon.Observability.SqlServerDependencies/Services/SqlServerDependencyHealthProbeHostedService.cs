using Cephalon.Abstractions.Health;
using Cephalon.Observability.SqlServerDependencies.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.SqlServerDependencies.Services;

internal sealed class SqlServerDependencyHealthProbeHostedService(
    SqlServerDependencyHealthOptions options,
    ISqlServerDependencyProbeClient probeClient,
    SqlServerDependencyHealthStore store,
    ILogger<SqlServerDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.SqlServerDependencies";
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
        SqlServerDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "sqlserver-dependency"
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
                Description: "SQL Server host or connection string is not configured.",
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
            SqlServerDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"SQL Server dependency '{displayName}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            SqlServerDependencyHealthLogs.ProbeFailed(logger, exception, id);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"SQL Server dependency '{displayName}' failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
}

internal interface ISqlServerDependencyProbeClient
{
    ValueTask<string> ProbeAsync(SqlServerDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class SqlServerDependencyProbeClient : ISqlServerDependencyProbeClient
{
    public async ValueTask<string> ProbeAsync(SqlServerDependencyDefinition dependency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);
        var query = string.IsNullOrWhiteSpace(dependency.HealthQuery)
            ? "SELECT 1;"
            : dependency.HealthQuery.Trim();
        var builder = CreateConnectionStringBuilder(dependency, timeoutSeconds);

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = new SqlCommand(query, connection)
        {
            CommandTimeout = timeoutSeconds
        };

        await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return $"SQL Server endpoint '{DescribeTarget(builder)}' responded to health query.";
    }

    internal static SqlConnectionStringBuilder CreateConnectionStringBuilder(
        SqlServerDependencyDefinition dependency,
        int timeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        SqlConnectionStringBuilder builder = string.IsNullOrWhiteSpace(dependency.ConnectionString)
            ? new SqlConnectionStringBuilder
            {
                DataSource = CreateDataSource(dependency),
                InitialCatalog = string.IsNullOrWhiteSpace(dependency.Database) ? "master" : dependency.Database.Trim()
            }
            : new SqlConnectionStringBuilder(dependency.ConnectionString);

        if (!string.IsNullOrWhiteSpace(dependency.Username))
        {
            builder.UserID = dependency.Username.Trim();
        }

        if (dependency.Password is not null)
        {
            builder.Password = dependency.Password;
        }

        if (!string.IsNullOrWhiteSpace(dependency.Encrypt))
        {
            builder["Encrypt"] = dependency.Encrypt.Trim();
        }

        if (dependency.TrustServerCertificate.HasValue)
        {
            builder.TrustServerCertificate = dependency.TrustServerCertificate.Value;
        }

        builder.ApplicationName = "Cephalon.DependencyHealth.SqlServer";
        builder.ConnectTimeout = timeoutSeconds;
        builder.Pooling = false;

        return builder;
    }

    private static string CreateDataSource(SqlServerDependencyDefinition dependency)
    {
        var host = dependency.Host?.Trim() ?? string.Empty;
        var port = dependency.Port > 0 ? dependency.Port : 1433;
        return string.IsNullOrWhiteSpace(host)
            ? string.Empty
            : $"{host},{port}";
    }

    private static string DescribeTarget(SqlConnectionStringBuilder builder)
    {
        var dataSource = string.IsNullOrWhiteSpace(builder.DataSource) ? "(server unspecified)" : builder.DataSource;
        var database = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "(database unspecified)" : builder.InitialCatalog;
        return $"{dataSource}/{database}";
    }
}

internal static class SqlServerDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(SqlServerDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, SqlServerDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        SqlServerDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(SqlServerDependencyHealthDiagnosticsConventions.ProbeFailed.Id, SqlServerDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        SqlServerDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
