using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.MySqlDependencies.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Cephalon.Observability.MySqlDependencies.Services;

internal sealed class MySqlDependencyHealthProbeHostedService(
    MySqlDependencyHealthOptions options,
    IMySqlDependencyProbeClient probeClient,
    DependencyHealthStore store,
    ILogger<MySqlDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<MySqlDependencyHealthOptions, MySqlDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.MySqlDependencies";
    protected override string DefaultDependencyId => "mysql-dependency";
    protected override string ProviderLabel => "MySQL";

    protected override string? ValidateDependency(MySqlDependencyDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.ConnectionString) && string.IsNullOrWhiteSpace(definition.Host)
            ? "MySQL host or connection string is not configured."
            : null;
    }

    protected override ValueTask<string> ProbeAsync(MySqlDependencyDefinition definition, CancellationToken cancellationToken) =>
        probeClient.ProbeAsync(definition, cancellationToken);

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        MySqlDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        MySqlDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
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
