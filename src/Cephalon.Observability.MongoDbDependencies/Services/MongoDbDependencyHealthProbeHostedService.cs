using System.Text;
using Cephalon.Abstractions.Health;
using Cephalon.Observability.MongoDbDependencies.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cephalon.Observability.MongoDbDependencies.Services;

internal sealed class MongoDbDependencyHealthProbeHostedService(
    MongoDbDependencyHealthOptions options,
    IMongoDbDependencyProbeClient probeClient,
    MongoDbDependencyHealthStore store,
    ILogger<MongoDbDependencyHealthProbeHostedService> logger) : IHostedService, IDisposable
{
    private const string SourceName = "Cephalon.Observability.MongoDbDependencies";
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
        MongoDbDependencyDefinition dependency,
        CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id)
            ? "mongodb-dependency"
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
                Description: "MongoDB host or connection string is not configured.",
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
            MongoDbDependencyHealthLogs.ProbeTimedOut(logger, id, timeoutSeconds);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"MongoDB dependency '{displayName}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required,
                Source: SourceName);
        }
        catch (Exception exception)
        {
            MongoDbDependencyHealthLogs.ProbeFailed(logger, exception, id);

            return new DependencyHealthReport(
                Id: id,
                DisplayName: displayName,
                State: HealthState.Unhealthy,
                Description: $"MongoDB dependency '{displayName}' failed: {exception.Message}",
                Required: dependency.Required,
                Source: SourceName);
        }
    }
}

internal interface IMongoDbDependencyProbeClient
{
    ValueTask<string> ProbeAsync(MongoDbDependencyDefinition dependency, CancellationToken cancellationToken);
}

internal sealed class MongoDbDependencyProbeClient : IMongoDbDependencyProbeClient
{
    public async ValueTask<string> ProbeAsync(MongoDbDependencyDefinition dependency, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);
        var commandName = string.IsNullOrWhiteSpace(dependency.HealthCommand)
            ? "ping"
            : dependency.HealthCommand.Trim();
        var settings = CreateClientSettings(dependency, timeoutSeconds);
        var client = new MongoClient(settings);
        var database = client.GetDatabase(GetDatabaseName(dependency));

        await database
            .RunCommandAsync<BsonDocument>(new BsonDocument(commandName, 1), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return $"MongoDB endpoint '{DescribeTarget(dependency)}' responded to health command '{commandName}'.";
    }

    internal static MongoClientSettings CreateClientSettings(
        MongoDbDependencyDefinition dependency,
        int timeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var settings = string.IsNullOrWhiteSpace(dependency.ConnectionString)
            ? MongoClientSettings.FromConnectionString(CreateConnectionString(dependency, timeoutSeconds))
            : MongoClientSettings.FromConnectionString(dependency.ConnectionString);

        settings.ApplicationName = "Cephalon.DependencyHealth.MongoDb";
        settings.ConnectTimeout = TimeSpan.FromSeconds(timeoutSeconds);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(timeoutSeconds);

        if (dependency.DirectConnection.HasValue)
        {
            settings.DirectConnection = dependency.DirectConnection.Value;
        }

        if (dependency.UseTls.HasValue)
        {
            settings.UseTls = dependency.UseTls.Value;
        }

        if (dependency.AllowInsecureTls.HasValue)
        {
            settings.AllowInsecureTls = dependency.AllowInsecureTls.Value;
        }

        if (!string.IsNullOrWhiteSpace(dependency.Username))
        {
            settings.Credential = MongoCredential.CreateCredential(
                string.IsNullOrWhiteSpace(dependency.AuthSource)
                    ? GetDatabaseName(dependency)
                    : dependency.AuthSource.Trim(),
                dependency.Username.Trim(),
                dependency.Password ?? string.Empty);
        }

        return settings;
    }

    internal static string CreateConnectionString(
        MongoDbDependencyDefinition dependency,
        int timeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        if (!string.IsNullOrWhiteSpace(dependency.ConnectionString))
        {
            return dependency.ConnectionString;
        }

        var timeoutMilliseconds = Math.Max(1, timeoutSeconds) * 1000;
        var builder = new StringBuilder();
        builder.Append("mongodb://");

        if (!string.IsNullOrWhiteSpace(dependency.Username))
        {
            builder.Append(Uri.EscapeDataString(dependency.Username.Trim()));
            if (dependency.Password is not null)
            {
                builder.Append(':');
                builder.Append(Uri.EscapeDataString(dependency.Password));
            }

            builder.Append('@');
        }

        builder.Append(dependency.Host.Trim());
        builder.Append(':');
        builder.Append(dependency.Port > 0 ? dependency.Port : 27017);
        builder.Append('/');
        builder.Append(Uri.EscapeDataString(GetDatabaseName(dependency)));

        var query = new List<string>
        {
            $"appName={Uri.EscapeDataString("Cephalon.DependencyHealth.MongoDb")}",
            $"connectTimeoutMS={timeoutMilliseconds}",
            $"serverSelectionTimeoutMS={timeoutMilliseconds}"
        };

        if (!string.IsNullOrWhiteSpace(dependency.AuthSource))
        {
            query.Add($"authSource={Uri.EscapeDataString(dependency.AuthSource.Trim())}");
        }

        if (dependency.UseTls.HasValue)
        {
            query.Add($"tls={dependency.UseTls.Value.ToString().ToLowerInvariant()}");
        }

        if (dependency.AllowInsecureTls.HasValue)
        {
            query.Add($"tlsInsecure={dependency.AllowInsecureTls.Value.ToString().ToLowerInvariant()}");
        }

        if (dependency.DirectConnection.HasValue)
        {
            query.Add($"directConnection={dependency.DirectConnection.Value.ToString().ToLowerInvariant()}");
        }

        builder.Append('?');
        builder.Append(string.Join("&", query));

        return builder.ToString();
    }

    private static string DescribeTarget(MongoDbDependencyDefinition dependency)
    {
        var host = string.IsNullOrWhiteSpace(dependency.Host)
            ? "(server unspecified)"
            : dependency.Host.Trim();
        var port = dependency.Port > 0 ? dependency.Port : 27017;
        return $"{host}:{port}/{GetDatabaseName(dependency)}";
    }

    private static string GetDatabaseName(MongoDbDependencyDefinition dependency) =>
        string.IsNullOrWhiteSpace(dependency.Database)
            ? "admin"
            : dependency.Database.Trim();
}

internal static class MongoDbDependencyHealthLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(MongoDbDependencyHealthDiagnosticsConventions.ProbeTimedOut.Id, MongoDbDependencyHealthDiagnosticsConventions.ProbeTimedOut.Name),
        MongoDbDependencyHealthDiagnosticsConventions.ProbeTimedOut.MessageTemplate);

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(MongoDbDependencyHealthDiagnosticsConventions.ProbeFailed.Id, MongoDbDependencyHealthDiagnosticsConventions.ProbeFailed.Name),
        MongoDbDependencyHealthDiagnosticsConventions.ProbeFailed.MessageTemplate);

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
