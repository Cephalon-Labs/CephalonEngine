using System.Collections.Specialized;
using System.Text;
using Cephalon.Observability.ClickHouseDependencies.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Services;
using ClickHouse.Driver.ADO;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.ClickHouseDependencies.Services;

internal sealed class ClickHouseDependencyHealthProbeHostedService(
    ClickHouseDependencyHealthOptions options,
    IClickHouseDependencyProbeClient probeClient,
    DependencyHealthStore store,
    ILogger<ClickHouseDependencyHealthProbeHostedService> logger)
    : DependencyHealthProbeHostedServiceBase<ClickHouseDependencyHealthOptions, ClickHouseDependencyDefinition>(options, store, logger)
{
    protected override string SourceName => "Cephalon.Observability.ClickHouseDependencies";
    protected override string DefaultDependencyId => "clickhouse-dependency";
    protected override string ProviderLabel => "ClickHouse";

    protected override string? ValidateDependency(ClickHouseDependencyDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.ConnectionString) && string.IsNullOrWhiteSpace(definition.Host)
            ? "ClickHouse host or connection string is not configured."
            : null;
    }

    protected override ValueTask<string> ProbeAsync(ClickHouseDependencyDefinition definition, CancellationToken cancellationToken) =>
        probeClient.ProbeAsync(definition, cancellationToken);

    protected override void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        ClickHouseDependencyHealthLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    protected override void LogProbeFailed(Exception exception, string dependencyId) =>
        ClickHouseDependencyHealthLogs.ProbeFailed(logger, exception, dependencyId);
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
