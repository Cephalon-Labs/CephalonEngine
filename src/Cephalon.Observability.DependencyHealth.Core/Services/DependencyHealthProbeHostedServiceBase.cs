using Cephalon.Abstractions.Health;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.DependencyHealth.Core.Services;

/// <summary>
/// Abstract base class for dependency-health probe hosted services.
/// Encapsulates the refresh loop, PeriodicTimer scheduling, Task.WhenAll fan-out,
/// report sorting, and timeout/exception handling.
/// </summary>
internal abstract class DependencyHealthProbeHostedServiceBase<TOptions, TDefinition> : IHostedService, IDisposable
    where TOptions : DependencyHealthOptionsBase<TDefinition>
    where TDefinition : DependencyDefinitionBase
{
    private readonly TOptions options;
    private readonly DependencyHealthStore store;
    private readonly ILogger logger;
    private CancellationTokenSource? loopCancellation;
    private Task? loopTask;

    /// <summary>Initializes the base hosted service.</summary>
    protected DependencyHealthProbeHostedServiceBase(TOptions options, DependencyHealthStore store, ILogger logger)
    {
        this.options = options;
        this.store = store;
        this.logger = logger;
    }

    /// <summary>The diagnostics source name written into every DependencyHealthReport.Source.</summary>
    protected abstract string SourceName { get; }

    /// <summary>The fallback dependency id when a definition's Id is blank, e.g. "cassandra-dependency".</summary>
    protected abstract string DefaultDependencyId { get; }

    /// <summary>The human-readable provider label for timeout/failure messages, e.g. "Cassandra".</summary>
    protected abstract string ProviderLabel { get; }

    /// <summary>Validates a dependency definition before probing. Returns null if valid, or an error description.</summary>
    protected abstract string? ValidateDependency(TDefinition definition);

    /// <summary>Executes the provider-specific connectivity check and returns a success description.</summary>
    protected abstract ValueTask<string> ProbeAsync(TDefinition definition, CancellationToken cancellationToken);

    /// <summary>Emits the probe-timed-out log entry. Override to use provider-specific event IDs.</summary>
    protected virtual void LogProbeTimedOut(string dependencyId, int timeoutSeconds) =>
        DependencyHealthProbeBaseLogs.ProbeTimedOut(logger, dependencyId, timeoutSeconds);

    /// <summary>Emits the probe-failed log entry. Override to use provider-specific event IDs.</summary>
    protected virtual void LogProbeFailed(Exception exception, string dependencyId) =>
        DependencyHealthProbeBaseLogs.ProbeFailed(logger, exception, dependencyId);

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Dependencies.Count == 0) return;
        await RefreshAsync(cancellationToken).ConfigureAwait(false);
        loopCancellation = new CancellationTokenSource();
        loopTask = Task.Run(() => RunLoopAsync(loopCancellation.Token), CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (loopCancellation is null || loopTask is null) return;
        loopCancellation.Cancel();
        try { await loopTask.WaitAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
    }

    /// <inheritdoc />
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
                await RefreshAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var reports = await Task
            .WhenAll(options.Dependencies.Select(d => ProbeDependencyAsync(d, cancellationToken)))
            .ConfigureAwait(false);

        store.SetReports(reports
            .OrderBy(static r => r.Required ? 0 : 1)
            .ThenBy(static r => r.Source, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static r => r.Id, StringComparer.OrdinalIgnoreCase));
    }

    private async Task<DependencyHealthReport> ProbeDependencyAsync(TDefinition dependency, CancellationToken cancellationToken)
    {
        var id = string.IsNullOrWhiteSpace(dependency.Id) ? DefaultDependencyId : dependency.Id.Trim();
        var displayName = string.IsNullOrWhiteSpace(dependency.DisplayName) ? id : dependency.DisplayName.Trim();
        var timeoutSeconds = Math.Max(1, dependency.TimeoutSeconds);

        var validationError = ValidateDependency(dependency);
        if (validationError is not null)
        {
            return new DependencyHealthReport(
                Id: id, DisplayName: displayName, State: HealthState.Unhealthy,
                Description: validationError, Required: dependency.Required, Source: SourceName);
        }

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            var description = await ProbeAsync(dependency, timeoutSource.Token).ConfigureAwait(false);
            return new DependencyHealthReport(
                Id: id, DisplayName: displayName, State: HealthState.Healthy,
                Description: description, Required: dependency.Required, Source: SourceName);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LogProbeTimedOut(id, timeoutSeconds);
            return new DependencyHealthReport(
                Id: id, DisplayName: displayName, State: HealthState.Unhealthy,
                Description: $"{ProviderLabel} dependency '{displayName}' timed out after {timeoutSeconds} seconds.",
                Required: dependency.Required, Source: SourceName);
        }
        catch (Exception exception)
        {
            LogProbeFailed(exception, id);
            return new DependencyHealthReport(
                Id: id, DisplayName: displayName, State: HealthState.Unhealthy,
                Description: $"{ProviderLabel} dependency '{displayName}' failed: {exception.Message}",
                Required: dependency.Required, Source: SourceName);
        }
    }
}

internal static class DependencyHealthProbeBaseLogs
{
    private static readonly Action<ILogger, string, int, Exception?> ProbeTimedOutMessage =
        LoggerMessage.Define<string, int>(LogLevel.Warning, new EventId(0, "ProbeTimedOut"),
            "Dependency probe '{DependencyId}' timed out after {TimeoutSeconds}s.");

    private static readonly Action<ILogger, string, Exception?> ProbeFailedMessage =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(0, "ProbeFailed"),
            "Dependency probe '{DependencyId}' failed.");

    public static void ProbeTimedOut(ILogger logger, string dependencyId, int timeoutSeconds) =>
        ProbeTimedOutMessage(logger, dependencyId, timeoutSeconds, null);

    public static void ProbeFailed(ILogger logger, Exception exception, string dependencyId) =>
        ProbeFailedMessage(logger, dependencyId, exception);
}
