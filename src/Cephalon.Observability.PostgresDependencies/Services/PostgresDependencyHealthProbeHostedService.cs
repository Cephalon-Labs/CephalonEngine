using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.PostgresDependencies.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cephalon.Observability.PostgresDependencies.Services;

internal sealed class PostgresDependencyHealthProbeHostedService(
    PostgresDependencyHealthOptions options,
    IPostgresDependencyProbeClient probeClient,
    DependencyHealthStore store,
    ILogger<PostgresDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<PostgresDependencyHealthOptions, PostgresDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.PostgresDependencies";
    protected override string DefaultDependencyId => "postgres-dependency";
    protected override string ProviderLabel => "Postgres";

    protected override string? ValidateDependency(PostgresDependencyDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.ConnectionString) && string.IsNullOrWhiteSpace(definition.Host)
            ? "Postgres host or connection string is not configured."
            : null;
    }

    protected override ValueTask<string> ProbeAsync(PostgresDependencyDefinition definition, CancellationToken cancellationToken) =>
        probeClient.ProbeAsync(definition, cancellationToken);

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        PostgresDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        PostgresDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
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
