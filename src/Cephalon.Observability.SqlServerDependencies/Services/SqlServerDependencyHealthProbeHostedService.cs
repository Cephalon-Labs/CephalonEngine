using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.SqlServerDependencies.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.SqlServerDependencies.Services;

internal sealed class SqlServerDependencyHealthProbeHostedService(
    SqlServerDependencyHealthOptions options,
    ISqlServerDependencyProbeClient probeClient,
    DependencyHealthStore store,
    ILogger<SqlServerDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<SqlServerDependencyHealthOptions, SqlServerDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.SqlServerDependencies";
    protected override string DefaultDependencyId => "sqlserver-dependency";
    protected override string ProviderLabel => "SQL Server";

    protected override string? ValidateDependency(SqlServerDependencyDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.ConnectionString) && string.IsNullOrWhiteSpace(definition.Host)
            ? "SQL Server host or connection string is not configured."
            : null;
    }

    protected override ValueTask<string> ProbeAsync(SqlServerDependencyDefinition definition, CancellationToken cancellationToken) =>
        probeClient.ProbeAsync(definition, cancellationToken);

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        SqlServerDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        SqlServerDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
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
