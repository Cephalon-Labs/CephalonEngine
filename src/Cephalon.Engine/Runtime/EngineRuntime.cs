using Cephalon.Abstractions.Modules;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Manifest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Cephalon.Engine.Runtime;

/// <summary>
/// Executes module lifecycle transitions and exposes runtime status, manifest, and failure information.
/// </summary>
public sealed class EngineRuntime : IRuntime, IDisposable
{
    private static readonly Action<ILogger, string, string, string, int, Exception?> LogRuntimeTransitionMessage =
        LoggerMessage.Define<string, string, string, int>(
            LogLevel.Information,
            new EventId(2000, nameof(LogRuntimeTransition)),
            "Runtime phase '{Phase}' completed with status {Status}. Blueprint {BlueprintId}. Modules {ModuleCount}.");
    private static readonly Action<ILogger, string, string, string, Exception?> LogModuleTransitionMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(2001, nameof(LogModuleTransition)),
            "Module '{ModuleId}' completed phase '{Phase}' with version {Version}.");
    private static readonly Action<ILogger, string, string, Exception> LogRuntimeFailureMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(2002, nameof(LogRuntimeFailure)),
            "Runtime phase '{Phase}' failed while status was {Status}.");
    private static readonly Action<ILogger, string, string, Exception> LogModuleFailureMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(2003, nameof(LogModuleFailure)),
            "Module '{ModuleId}' failed during phase '{Phase}'.");

    private readonly SemaphoreSlim lifecycleLock = new(1, 1);
    private readonly List<IModule> initializedModules = [];
    private readonly List<IModule> startedModules = [];
    private ModuleContext? moduleContext;
    private ILogger? logger;
    private RuntimeStatus status = RuntimeStatus.Created;
    private DateTimeOffset? initializedAtUtc;
    private DateTimeOffset? startedAtUtc;
    private DateTimeOffset? stoppedAtUtc;
    private RuntimeFailureInfo? lastFailure;
    private int restartCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineRuntime" /> class.
    /// </summary>
    /// <param name="modules">The modules that participate in runtime lifecycle transitions.</param>
    /// <param name="manifest">The runtime manifest that describes the built runtime shape.</param>
    /// <param name="failurePolicy">The failure policy that governs startup, stop, and restart behavior.</param>
    public EngineRuntime(
        IReadOnlyList<IModule> modules,
        RuntimeManifest manifest,
        FailurePolicy failurePolicy)
    {
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        FailurePolicy = failurePolicy ?? throw new ArgumentNullException(nameof(failurePolicy));
    }

    /// <summary>
    /// Gets the modules that participate in runtime lifecycle transitions.
    /// </summary>
    public IReadOnlyList<IModule> Modules { get; }

    /// <summary>
    /// Gets the runtime manifest that describes the built runtime shape.
    /// </summary>
    public RuntimeManifest Manifest { get; }

    /// <summary>
    /// Gets the failure policy that governs startup, stop, and restart behavior.
    /// </summary>
    public FailurePolicy FailurePolicy { get; }

    /// <summary>
    /// Gets the current lifecycle status.
    /// </summary>
    public RuntimeStatus Status => status;

    /// <summary>
    /// Gets a serialization-friendly snapshot of the current runtime status.
    /// </summary>
    public RuntimeStatusSnapshot StatusSnapshot =>
        new(status, initializedAtUtc, startedAtUtc, stoppedAtUtc, restartCount, lastFailure);

    /// <summary>
    /// Gets the last captured lifecycle failure when one is available.
    /// </summary>
    public RuntimeFailureInfo? LastFailure => lastFailure;

    /// <summary>
    /// Gets the number of completed manual restarts.
    /// </summary>
    public int RestartCount => restartCount;

    /// <summary>
    /// Initializes the runtime and its modules.
    /// </summary>
    /// <param name="services">The service provider bound to the runtime lifecycle.</param>
    /// <param name="cancellationToken">The cancellation token for the initialization operation.</param>
    /// <returns>A task that completes when initialization finishes.</returns>
    public async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            EnsureModuleContext(services);

            if (status is RuntimeStatus.Initialized or RuntimeStatus.Starting or RuntimeStatus.Started or RuntimeStatus.Stopping or RuntimeStatus.Stopped)
            {
                return;
            }

            EnsureNotFailedForDirectLifecycleCall("initialize");
            await InitializeCoreAsync(cancellationToken);
        }
        finally
        {
            lifecycleLock.Release();
        }
    }

    /// <summary>
    /// Starts the runtime and its modules.
    /// </summary>
    /// <param name="services">The service provider bound to the runtime lifecycle.</param>
    /// <param name="cancellationToken">The cancellation token for the startup operation.</param>
    /// <returns>A task that completes when startup finishes.</returns>
    public async Task StartAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            EnsureModuleContext(services);

            if (status == RuntimeStatus.Started)
            {
                return;
            }

            EnsureNotFailedForDirectLifecycleCall("start");

            if (status == RuntimeStatus.Created || initializedAtUtc is null)
            {
                await InitializeCoreAsync(cancellationToken);
            }

            if (status is RuntimeStatus.Stopped or RuntimeStatus.Initialized)
            {
                await StartCoreAsync(cancellationToken);
            }
        }
        finally
        {
            lifecycleLock.Release();
        }
    }

    /// <summary>
    /// Restarts the runtime when the current failure policy allows it.
    /// </summary>
    /// <param name="services">The service provider bound to the runtime lifecycle.</param>
    /// <param name="cancellationToken">The cancellation token for the restart operation.</param>
    /// <returns>A task that completes when the restart finishes.</returns>
    public async Task RestartAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            EnsureModuleContext(services);
            EnsureRestartAllowed();

            if (status is RuntimeStatus.Initializing or RuntimeStatus.Starting or RuntimeStatus.Stopping)
            {
                throw new InvalidOperationException(
                    $"Runtime cannot restart while it is {status}.");
            }

            if (status == RuntimeStatus.Started || status == RuntimeStatus.Failed)
            {
                await StopStartedModulesCoreAsync(cancellationToken);

                if (status == RuntimeStatus.Failed &&
                    string.Equals(lastFailure?.Phase, "stop", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Runtime cleanup failed during restart. Resolve the stop failure before retrying.");
                }
            }

            restartCount++;
            EngineDiagnostics.RuntimeRestartCounter.Add(1, new TagList
            {
                { "cephalon.blueprint", Manifest.AppProfile.BlueprintId }
            });

            if (status == RuntimeStatus.Stopped && initializedModules.Count > 0)
            {
                status = RuntimeStatus.Initialized;
            }
            else if (status == RuntimeStatus.Failed)
            {
                status = initializedModules.Count > 0
                    ? RuntimeStatus.Initialized
                    : RuntimeStatus.Created;
            }

            if (status == RuntimeStatus.Created || initializedAtUtc is null)
            {
                await InitializeCoreAsync(cancellationToken);
            }

            if (status is RuntimeStatus.Initialized or RuntimeStatus.Stopped)
            {
                await StartCoreAsync(cancellationToken);
            }

            LogRuntimeTransition(logger, "restart", status.ToString(), Manifest.AppProfile.BlueprintId, Modules.Count);
        }
        finally
        {
            lifecycleLock.Release();
        }
    }

    /// <summary>
    /// Stops started modules and transitions the runtime to a stopped state.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the stop operation.</param>
    /// <returns>A task that completes when shutdown finishes.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (status is RuntimeStatus.Created or RuntimeStatus.Stopped)
            {
                stoppedAtUtc ??= DateTimeOffset.UtcNow;
                status = RuntimeStatus.Stopped;
                return;
            }

            if (status == RuntimeStatus.Initialized)
            {
                stoppedAtUtc = DateTimeOffset.UtcNow;
                status = RuntimeStatus.Stopped;
                LogRuntimeTransition(logger, "stop", status.ToString(), Manifest.AppProfile.BlueprintId, Modules.Count);
                return;
            }

            if (status is RuntimeStatus.Started or RuntimeStatus.Failed)
            {
                await StopStartedModulesCoreAsync(cancellationToken);
            }
        }
        finally
        {
            lifecycleLock.Release();
        }
    }

    private void EnsureModuleContext(IServiceProvider services)
    {
        if (moduleContext is null)
        {
            moduleContext = new ModuleContext(services);
            logger ??= services.GetService<ILoggerFactory>()?.CreateLogger<EngineRuntime>();
            return;
        }

        if (!ReferenceEquals(moduleContext.Services, services))
        {
            throw new InvalidOperationException(
                "Runtime lifecycle was already bound to a different service provider.");
        }
    }

    private void EnsureNotFailedForDirectLifecycleCall(string phase)
    {
        if (status != RuntimeStatus.Failed)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Runtime is in the Failed state and cannot execute '{phase}' directly. Use RestartAsync if the failure policy allows it.");
    }

    private void EnsureRestartAllowed()
    {
        if (!FailurePolicy.AllowManualRestart)
        {
            throw new InvalidOperationException(
                "Runtime failure policy does not allow manual restarts.");
        }

        if (FailurePolicy.MaxRestartAttempts == 0)
        {
            throw new InvalidOperationException(
                "Runtime failure policy disables manual restarts.");
        }

        if (FailurePolicy.MaxRestartAttempts > 0 && restartCount >= FailurePolicy.MaxRestartAttempts)
        {
            throw new InvalidOperationException(
                $"Runtime restart limit '{FailurePolicy.MaxRestartAttempts}' has been reached.");
        }

        if (lastFailure is not null && !lastFailure.CanRestart)
        {
            throw new InvalidOperationException(
                $"Runtime cannot restart after a failure in phase '{lastFailure.Phase}'. Rebuild the runtime instead.");
        }
    }

    private async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        status = RuntimeStatus.Initializing;

        try
        {
            await ExecuteModulePhaseAsync(
                phase: "initialize",
                modules: Modules,
                execute: static (lifecycle, context, token) => lifecycle.InitializeAsync(context, token),
                onSuccess: TrackInitializedModule,
                continueOnFailure: false,
                cancellationToken);

            initializedAtUtc = DateTimeOffset.UtcNow;
            lastFailure = null;
            status = RuntimeStatus.Initialized;
            LogRuntimeTransition(logger, "initialize", status.ToString(), Manifest.AppProfile.BlueprintId, Modules.Count);
        }
        catch (Exception exception)
        {
            HandleFailure(
                phase: "initialize",
                statusBeforeFailure: RuntimeStatus.Initializing,
                exception: exception,
                startupFailureBehavior: FailurePolicy.StartupFailureBehavior,
                stopFailureBehavior: FailurePolicy.StopFailureBehavior);

            if (FailurePolicy.StartupFailureBehavior == StartupFailureBehavior.FailFast)
            {
                throw;
            }
        }
    }

    private async Task StartCoreAsync(CancellationToken cancellationToken)
    {
        status = RuntimeStatus.Starting;

        try
        {
            await ExecuteModulePhaseAsync(
                phase: "start",
                modules: Modules,
                execute: static (lifecycle, context, token) => lifecycle.StartAsync(context, token),
                onSuccess: TrackStartedModule,
                continueOnFailure: false,
                cancellationToken);

            startedAtUtc = DateTimeOffset.UtcNow;
            stoppedAtUtc = null;
            lastFailure = null;
            status = RuntimeStatus.Started;
            LogRuntimeTransition(logger, "start", status.ToString(), Manifest.AppProfile.BlueprintId, Modules.Count);
        }
        catch (Exception exception)
        {
            HandleFailure(
                phase: "start",
                statusBeforeFailure: RuntimeStatus.Starting,
                exception: exception,
                startupFailureBehavior: FailurePolicy.StartupFailureBehavior,
                stopFailureBehavior: FailurePolicy.StopFailureBehavior);

            if (FailurePolicy.StartupFailureBehavior == StartupFailureBehavior.FailFast)
            {
                throw;
            }
        }
    }

    private async Task StopStartedModulesCoreAsync(CancellationToken cancellationToken)
    {
        if (startedModules.Count == 0)
        {
            stoppedAtUtc = DateTimeOffset.UtcNow;
            lastFailure = null;
            status = RuntimeStatus.Stopped;
            LogRuntimeTransition(logger, "stop", status.ToString(), Manifest.AppProfile.BlueprintId, Modules.Count);
            return;
        }

        status = RuntimeStatus.Stopping;

        try
        {
            await ExecuteModulePhaseAsync(
                phase: "stop",
                modules: startedModules.AsEnumerable().Reverse().ToArray(),
                execute: static (lifecycle, context, token) => lifecycle.StopAsync(context, token),
                onSuccess: UntrackStartedModule,
                continueOnFailure: FailurePolicy.StopFailureBehavior == StopFailureBehavior.BestEffortContinue,
                cancellationToken);

            stoppedAtUtc = DateTimeOffset.UtcNow;
            lastFailure = null;
            status = RuntimeStatus.Stopped;
            LogRuntimeTransition(logger, "stop", status.ToString(), Manifest.AppProfile.BlueprintId, Modules.Count);
        }
        catch (Exception exception)
        {
            HandleFailure(
                phase: "stop",
                statusBeforeFailure: RuntimeStatus.Stopping,
                exception: exception,
                startupFailureBehavior: FailurePolicy.StartupFailureBehavior,
                stopFailureBehavior: FailurePolicy.StopFailureBehavior);

            if (FailurePolicy.StopFailureBehavior == StopFailureBehavior.FailFast)
            {
                throw;
            }
        }
    }

    private async Task ExecuteModulePhaseAsync(
        string phase,
        IEnumerable<IModule> modules,
        Func<IModuleLifecycle, ModuleContext, CancellationToken, Task> execute,
        Action<IModule> onSuccess,
        bool continueOnFailure,
        CancellationToken cancellationToken)
    {
        using var runtimeActivity = EngineDiagnostics.ActivitySource.StartActivity(
            $"runtime.{phase}",
            ActivityKind.Internal);
        runtimeActivity?.SetTag("cephalon.phase", phase);
        runtimeActivity?.SetTag("cephalon.blueprint", Manifest.AppProfile.BlueprintId);
        runtimeActivity?.SetTag("cephalon.module.count", Modules.Count);

        var runtimeTags = new TagList
        {
            { "cephalon.phase", phase },
            { "cephalon.blueprint", Manifest.AppProfile.BlueprintId }
        };

        var failures = new List<Exception>();

        try
        {
            EngineDiagnostics.RuntimeTransitionCounter.Add(1, runtimeTags);

            foreach (var module in modules)
            {
                if (module is not IModuleLifecycle lifecycle)
                {
                    continue;
                }

                try
                {
                    await ExecuteLifecycleModuleAsync(module, lifecycle, phase, execute, cancellationToken);
                    onSuccess(module);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                    if (!continueOnFailure)
                    {
                        throw;
                    }
                }
            }

            if (failures.Count > 0)
            {
                throw new AggregateException(
                    $"One or more modules failed during phase '{phase}'.",
                    failures);
            }
        }
        catch (Exception exception)
        {
            runtimeActivity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            LogRuntimeFailure(logger, phase, status.ToString(), exception);
            throw;
        }
    }

    private async Task ExecuteLifecycleModuleAsync(
        IModule module,
        IModuleLifecycle lifecycle,
        string phase,
        Func<IModuleLifecycle, ModuleContext, CancellationToken, Task> execute,
        CancellationToken cancellationToken)
    {
        var moduleVersion = module.Descriptor.Version ?? "unknown";
        using var moduleActivity = EngineDiagnostics.ActivitySource.StartActivity(
            $"module.{phase}",
            ActivityKind.Internal);
        moduleActivity?.SetTag("cephalon.phase", phase);
        moduleActivity?.SetTag("cephalon.module.id", module.Descriptor.Id);
        moduleActivity?.SetTag("cephalon.module.version", moduleVersion);

        var tags = new TagList
        {
            { "cephalon.phase", phase },
            { "cephalon.module.id", module.Descriptor.Id }
        };

        try
        {
            EngineDiagnostics.ModuleTransitionCounter.Add(1, tags);
            await execute(lifecycle, moduleContext!, cancellationToken);
            LogModuleTransition(logger, module.Descriptor.Id, phase, moduleVersion);
        }
        catch (Exception exception)
        {
            moduleActivity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            EngineDiagnostics.ModuleFailureCounter.Add(1, new TagList
            {
                { "cephalon.phase", phase },
                { "cephalon.module.id", module.Descriptor.Id }
            });
            LogModuleFailure(logger, module.Descriptor.Id, phase, exception);
            throw new ModulePhaseException(phase, module.Descriptor.Id, moduleVersion, exception);
        }
    }

    private void HandleFailure(
        string phase,
        RuntimeStatus statusBeforeFailure,
        Exception exception,
        StartupFailureBehavior startupFailureBehavior,
        StopFailureBehavior stopFailureBehavior)
    {
        var moduleException = UnwrapModuleFailure(exception);
        var canRestart = FailurePolicy.AllowManualRestart &&
            FailurePolicy.MaxRestartAttempts != 0 &&
            (lastFailure is null || restartCount < FailurePolicy.MaxRestartAttempts || FailurePolicy.MaxRestartAttempts < 0) &&
            string.Equals(moduleException?.Phase ?? phase, "start", StringComparison.OrdinalIgnoreCase);

        lastFailure = new RuntimeFailureInfo(
            Phase: moduleException?.Phase ?? phase,
            ModuleId: moduleException?.ModuleId,
            ModuleVersion: moduleException?.ModuleVersion,
            StatusBeforeFailure: statusBeforeFailure,
            ExceptionType: moduleException?.InnerException?.GetType().FullName ?? exception.GetType().FullName ?? exception.GetType().Name,
            Message: moduleException?.InnerException?.Message ?? exception.Message,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            CanRestart: canRestart,
            StartupFailureBehavior: startupFailureBehavior,
            StopFailureBehavior: stopFailureBehavior);

        EngineDiagnostics.RuntimeFailureCounter.Add(1, new TagList
        {
            { "cephalon.phase", lastFailure.Phase },
            { "cephalon.blueprint", Manifest.AppProfile.BlueprintId }
        });

        stoppedAtUtc = string.Equals(phase, "stop", StringComparison.OrdinalIgnoreCase)
            ? DateTimeOffset.UtcNow
            : stoppedAtUtc;
        status = RuntimeStatus.Failed;
    }

    private static ModulePhaseException? UnwrapModuleFailure(Exception exception)
    {
        if (exception is ModulePhaseException moduleException)
        {
            return moduleException;
        }

        if (exception is AggregateException aggregateException)
        {
            return aggregateException.Flatten().InnerExceptions
                .OfType<ModulePhaseException>()
                .FirstOrDefault();
        }

        return null;
    }

    private void TrackInitializedModule(IModule module)
    {
        if (!initializedModules.Contains(module))
        {
            initializedModules.Add(module);
        }
    }

    private void TrackStartedModule(IModule module)
    {
        if (!startedModules.Contains(module))
        {
            startedModules.Add(module);
        }
    }

    private void UntrackStartedModule(IModule module)
    {
        startedModules.Remove(module);
    }

    private static void LogRuntimeTransition(
        ILogger? logger,
        string phase,
        string status,
        string blueprintId,
        int moduleCount)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogRuntimeTransitionMessage(logger, phase, status, blueprintId, moduleCount, null);
    }

    private static void LogModuleTransition(
        ILogger? logger,
        string moduleId,
        string phase,
        string version)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogModuleTransitionMessage(logger, moduleId, phase, version, null);
    }

    private static void LogRuntimeFailure(
        ILogger? logger,
        string phase,
        string status,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Error))
        {
            return;
        }

        LogRuntimeFailureMessage(logger, phase, status, exception);
    }

    private static void LogModuleFailure(
        ILogger? logger,
        string moduleId,
        string phase,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Error))
        {
            return;
        }

        LogModuleFailureMessage(logger, moduleId, phase, exception);
    }

    /// <summary>
    /// Releases runtime resources.
    /// </summary>
    public void Dispose()
    {
        lifecycleLock.Dispose();
    }
}
