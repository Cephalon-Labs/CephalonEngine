using Cephalon.Abstractions.Health;
using Cephalon.Observability.PostgresDependencies.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cephalon.Observability.PostgresDependencies.Services;

internal sealed class PostgresDependencyHealthProbeHostedService(
    PostgresDependencyHealthOptions options,
    IPostgresDependencyProbeClient probeClient,
    PostgresDependencyHealthStore store,
    ILogger<PostgresDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.PostgresDependencies";
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
        PostgresDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "postgres-dependency"
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
                Description: "Postgres host or connection string is not configured.",
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
            PostgresDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Postgres dependency '{displayName}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            PostgresDependencyHealthLogs.ProbeFailed(logger, exception, id);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"Postgres dependency '{displayName}' failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
}

internal interface IPostgresDependencyProbeClient
{
    ValueTask<string> ProbeAsync(PostgresDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class NpgsqlPostgresDependencyProbeClient : IPostgresDependencyProbeClient
{
    public async ValueTask<string> ProbeAsync(PostgresDependencyDefinition dependency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);
        var query = string.IsNullOrWhiteSpace(dependency.HealthQuery)
            ? "SELECT 1;"
            : dependency.HealthQuery.Trim();
        var builder = CreateConnectionStringBuilder(dependency, timeoutSeconds);

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = new NpgsqlCommand(query, connection)
        {
            CommandTimeout = timeoutSeconds
        };

        await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return $"Postgres endpoint '{DescribeTarget(builder)}' responded to health query.";
    }

    internal static NpgsqlConnectionStringBuilder CreateConnectionStringBuilder(
        PostgresDependencyDefinition dependency,
        int timeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        NpgsqlConnectionStringBuilder builder = string.IsNullOrWhiteSpace(dependency.ConnectionString)
            ? new NpgsqlConnectionStringBuilder
            {
                Host = dependency.Host?.Trim() ?? string.Empty,
                Port = dependency.Port > 0 ? dependency.Port : 5432,
                Database = string.IsNullOrWhiteSpace(dependency.Database) ? "postgres" : dependency.Database.Trim(),
                Username = dependency.Username?.Trim(),
                Password = dependency.Password,
                SslMode = ParseSslMode(dependency.SslMode)
            }
            : new NpgsqlConnectionStringBuilder(dependency.ConnectionString);

        if (!string.IsNullOrWhiteSpace(dependency.SslMode))
        {
            builder.SslMode = ParseSslMode(dependency.SslMode);
        }

        builder.ApplicationName = "Cephalon.DependencyHealth.Postgres";
        builder.Timeout = timeoutSeconds;
        builder.CommandTimeout = timeoutSeconds;
        builder.Pooling = false;

        return builder;
    }

    private static string DescribeTarget(NpgsqlConnectionStringBuilder builder)
    {
        var host = string.IsNullOrWhiteSpace(builder.Host) ? "(host unspecified)" : builder.Host;
        var database = string.IsNullOrWhiteSpace(builder.Database) ? "(database unspecified)" : builder.Database;
        return $"{host}:{builder.Port}/{database}";
    }

    private static SslMode ParseSslMode(string? value) =>
        Enum.TryParse<SslMode>(value, ignoreCase: true, out var parsed)
            ? parsed
            : SslMode.Prefer;
}

internal static class PostgresDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(PostgresDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, PostgresDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        PostgresDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(PostgresDependencyHealthDiagnosticsConventions.ProbeFailed.Id, PostgresDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        PostgresDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
