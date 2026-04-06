using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.OracleDependencies.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace Cephalon.Observability.OracleDependencies.Services;

internal sealed class OracleDependencyHealthProbeHostedService(
    OracleDependencyHealthOptions options,
    IOracleDependencyProbeClient probeClient,
    DependencyHealthStore store,
    ILogger<OracleDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<OracleDependencyHealthOptions, OracleDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.OracleDependencies";
    protected override string DefaultDependencyId => "oracle-dependency";
    protected override string ProviderLabel => "Oracle";

    protected override string? ValidateDependency(OracleDependencyDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.ConnectionString) &&
               (string.IsNullOrWhiteSpace(definition.Host) || string.IsNullOrWhiteSpace(definition.ServiceName))
            ? "Oracle host/service name or connection string is not configured."
            : null;
    }

    protected override ValueTask<string> ProbeAsync(OracleDependencyDefinition definition, CancellationToken cancellationToken) =>
        probeClient.ProbeAsync(definition, cancellationToken);

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        OracleDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        OracleDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
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
