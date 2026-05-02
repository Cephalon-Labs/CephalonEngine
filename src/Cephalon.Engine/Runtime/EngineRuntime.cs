using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Diagnostics;
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
    private const int MaxTimelineEntries = 256;
    private static readonly Action<ILogger, string, string, string, int, Exception?> LogRuntimeTransitionMessage =
        LoggerMessage.Define<string, string, string, int>(
            LogLevel.Information,
            new EventId(EngineRuntimeDiagnosticsConventions.RuntimeTransition.Id, EngineRuntimeDiagnosticsConventions.RuntimeTransition.Name),
            EngineRuntimeDiagnosticsConventions.RuntimeTransition.MessageTemplate);
    private static readonly Action<ILogger, string, string, string, Exception?> LogModuleTransitionMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(EngineRuntimeDiagnosticsConventions.ModuleTransition.Id, EngineRuntimeDiagnosticsConventions.ModuleTransition.Name),
            EngineRuntimeDiagnosticsConventions.ModuleTransition.MessageTemplate);
    private static readonly Action<ILogger, string, string, string, string, Exception?> LogExecutionGraphTransitionMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(EngineRuntimeDiagnosticsConventions.ExecutionGraphTransition.Id, EngineRuntimeDiagnosticsConventions.ExecutionGraphTransition.Name),
            EngineRuntimeDiagnosticsConventions.ExecutionGraphTransition.MessageTemplate);
    private static readonly Action<ILogger, string, string, string, string, string, string, Exception?> LogHostedExecutionTransitionMessage =
        LoggerMessage.Define<string, string, string, string, string, string>(
            LogLevel.Information,
            new EventId(EngineRuntimeDiagnosticsConventions.HostedExecutionTransition.Id, EngineRuntimeDiagnosticsConventions.HostedExecutionTransition.Name),
            EngineRuntimeDiagnosticsConventions.HostedExecutionTransition.MessageTemplate);
    private static readonly Action<ILogger, string, string, Exception> LogRuntimeFailureMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(EngineRuntimeDiagnosticsConventions.RuntimeFailure.Id, EngineRuntimeDiagnosticsConventions.RuntimeFailure.Name),
            EngineRuntimeDiagnosticsConventions.RuntimeFailure.MessageTemplate);
    private static readonly Action<ILogger, string, string, Exception> LogModuleFailureMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(EngineRuntimeDiagnosticsConventions.ModuleFailure.Id, EngineRuntimeDiagnosticsConventions.ModuleFailure.Name),
            EngineRuntimeDiagnosticsConventions.ModuleFailure.MessageTemplate);

    private readonly SemaphoreSlim lifecycleLock = new(1, 1);
    private readonly object stateGate = new();
    private readonly IReadOnlyList<ExecutionGraphDescriptor> executionGraphs;
    private readonly IReadOnlyList<HostedExecutionDescriptor> hostedExecutions;
    private readonly List<IModule> initializedModules = [];
    private readonly List<IModule> startedModules = [];
    private readonly Dictionary<string, string?> moduleVersionsById;
    private readonly Dictionary<string, DateTimeOffset> moduleLoadedAtUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> moduleInitializedAtUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> moduleStartedAtUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> moduleStoppedAtUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> executionGraphLoadedAtUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> executionGraphActivatedAtUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> executionGraphDeactivatedAtUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> hostedExecutionLoadedAtUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> hostedExecutionActivatedAtUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> hostedExecutionDeactivatedAtUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RuntimeFailureInfo> moduleFailures = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RuntimeLifecycleEvent> timeline = [];
    private ModuleContext? moduleContext;
    private ILogger? logger;
    private RuntimeStatus status = RuntimeStatus.Created;
    private DateTimeOffset? initializedAtUtc;
    private DateTimeOffset? startedAtUtc;
    private DateTimeOffset? stoppingAtUtc;
    private DateTimeOffset? stoppedAtUtc;
    private RuntimeFailureInfo? lastFailure;
    private int restartCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineRuntime" /> class.
    /// </summary>
    /// <param name="modules">The modules that participate in runtime lifecycle transitions.</param>
    /// <param name="manifest">The runtime manifest that describes the built runtime shape.</param>
    /// <param name="failurePolicy">The failure policy that governs startup, stop, and restart behavior.</param>
    /// <param name="executionGraphs">The execution graphs visible to the runtime story and diagnostics surface.</param>
    /// <param name="hostedExecutions">The hosted executions visible to the runtime story and operator-facing introspection surfaces.</param>
    public EngineRuntime(
        IReadOnlyList<IModule> modules,
        RuntimeManifest manifest,
        FailurePolicy failurePolicy,
        IReadOnlyList<ExecutionGraphDescriptor>? executionGraphs = null,
        IReadOnlyList<HostedExecutionDescriptor>? hostedExecutions = null)
    {
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        FailurePolicy = failurePolicy ?? throw new ArgumentNullException(nameof(failurePolicy));
        this.executionGraphs = executionGraphs?.ToArray() ?? [];
        this.hostedExecutions = hostedExecutions?.ToArray() ?? [];
        moduleVersionsById = Manifest.Modules.ToDictionary(
            static module => module.Id,
            static module => (string?)module.Version,
            StringComparer.OrdinalIgnoreCase);
        SeedLoadedStoryFromManifest();
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
    public RuntimeStatus Status
    {
        get
        {
            lock (stateGate)
            {
                return status;
            }
        }
    }

    /// <summary>
    /// Gets a serialization-friendly snapshot of the current runtime status.
    /// </summary>
    public RuntimeStatusSnapshot StatusSnapshot
    {
        get
        {
            lock (stateGate)
            {
                return CreateStatusSnapshotUnsafe();
            }
        }
    }

    /// <summary>
    /// Gets the richer operator-facing lifecycle story for the runtime.
    /// </summary>
    public RuntimeOperationalStory OperationalStory
    {
        get
        {
            lock (stateGate)
            {
                return CreateOperationalStoryUnsafe();
            }
        }
    }

    /// <summary>
    /// Gets the last captured lifecycle failure when one is available.
    /// </summary>
    public RuntimeFailureInfo? LastFailure
    {
        get
        {
            lock (stateGate)
            {
                return lastFailure;
            }
        }
    }

    /// <summary>
    /// Gets the number of completed manual restarts.
    /// </summary>
    public int RestartCount
    {
        get
        {
            lock (stateGate)
            {
                return restartCount;
            }
        }
    }

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

            lock (stateGate)
            {
                restartCount++;
            }
            EngineDiagnostics.RuntimeRestartCounter.Add(1, new TagList
            {
                { "cephalon.blueprint", Manifest.AppProfile.BlueprintId }
            });

            if (status == RuntimeStatus.Stopped && initializedModules.Count > 0)
            {
                lock (stateGate)
                {
                    status = RuntimeStatus.Initialized;
                }
            }
            else if (status == RuntimeStatus.Failed)
            {
                lock (stateGate)
                {
                    status = initializedModules.Count > 0
                        ? RuntimeStatus.Initialized
                        : RuntimeStatus.Created;
                }
            }

            if (status == RuntimeStatus.Created || initializedAtUtc is null)
            {
                await InitializeCoreAsync(cancellationToken);
            }

            if (status is RuntimeStatus.Initialized or RuntimeStatus.Stopped)
            {
                await StartCoreAsync(cancellationToken);
            }

            var restartRecordedAtUtc = DateTimeOffset.UtcNow;
            RecordLifecycleEvent(
                RuntimeLifecycleEventScope.Runtime,
                phase: "restart",
                outcome: RuntimeLifecycleEventOutcome.Succeeded,
                runtimeStatus: status,
                subjectId: "runtime",
                subjectVersion: Manifest.EngineVersion,
                message: $"Runtime completed restart with status {status}.",
                occurredAtUtc: restartRecordedAtUtc);
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
                lock (stateGate)
                {
                    stoppedAtUtc ??= DateTimeOffset.UtcNow;
                    status = RuntimeStatus.Stopped;
                }
                return;
            }

            if (status == RuntimeStatus.Initialized)
            {
                var stopRecordedAtUtc = DateTimeOffset.UtcNow;
                lock (stateGate)
                {
                    stoppingAtUtc = stopRecordedAtUtc;
                    stoppedAtUtc = stopRecordedAtUtc;
                    status = RuntimeStatus.Stopped;
                }
                RecordLifecycleEvent(
                    RuntimeLifecycleEventScope.Runtime,
                    phase: "stop",
                    outcome: RuntimeLifecycleEventOutcome.Succeeded,
                    runtimeStatus: RuntimeStatus.Stopped,
                    subjectId: "runtime",
                    subjectVersion: Manifest.EngineVersion,
                    message: "Runtime completed stop with status Stopped.",
                    occurredAtUtc: stopRecordedAtUtc);
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

        if (lastFailure?.RestartAvailableAtUtc is DateTimeOffset restartAvailableAtUtc &&
            DateTimeOffset.UtcNow < restartAvailableAtUtc)
        {
            throw new InvalidOperationException(
                $"Runtime restart is backed off until '{restartAvailableAtUtc:O}'.");
        }
    }

    private async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        lock (stateGate)
        {
            status = RuntimeStatus.Initializing;
        }

        try
        {
            await ExecuteModulePhaseAsync(
                phase: "initialize",
                modules: Modules,
                execute: static (lifecycle, context, token) => lifecycle.InitializeAsync(context, token),
                onSuccess: TrackInitializedModule,
                continueOnFailure: false,
                cancellationToken);

            var initializeRecordedAtUtc = DateTimeOffset.UtcNow;
            lock (stateGate)
            {
                initializedAtUtc = initializeRecordedAtUtc;
                lastFailure = null;
                status = RuntimeStatus.Initialized;
            }
            RecordLifecycleEvent(
                RuntimeLifecycleEventScope.Runtime,
                phase: "initialize",
                outcome: RuntimeLifecycleEventOutcome.Succeeded,
                runtimeStatus: RuntimeStatus.Initialized,
                subjectId: "runtime",
                subjectVersion: Manifest.EngineVersion,
                message: "Runtime completed initialize with status Initialized.",
                occurredAtUtc: initializeRecordedAtUtc);
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
        lock (stateGate)
        {
            status = RuntimeStatus.Starting;
        }

        try
        {
            await ExecuteModulePhaseAsync(
                phase: "start",
                modules: Modules,
                execute: static (lifecycle, context, token) => lifecycle.StartAsync(context, token),
                onSuccess: TrackStartedModule,
                continueOnFailure: false,
                cancellationToken);

            var startRecordedAtUtc = DateTimeOffset.UtcNow;
            lock (stateGate)
            {
                startedAtUtc = startRecordedAtUtc;
                stoppingAtUtc = null;
                stoppedAtUtc = null;
                lastFailure = null;
                status = RuntimeStatus.Started;
            }
            RecordLifecycleEvent(
                RuntimeLifecycleEventScope.Runtime,
                phase: "start",
                outcome: RuntimeLifecycleEventOutcome.Succeeded,
                runtimeStatus: RuntimeStatus.Started,
                subjectId: "runtime",
                subjectVersion: Manifest.EngineVersion,
                message: "Runtime completed start with status Started.",
                occurredAtUtc: startRecordedAtUtc);
            ActivateExecutionGraphs(startRecordedAtUtc);
            ActivateHostedExecutions(startRecordedAtUtc);
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
            var stopRecordedAtUtc = DateTimeOffset.UtcNow;
            lock (stateGate)
            {
                stoppingAtUtc = stopRecordedAtUtc;
                stoppedAtUtc = stopRecordedAtUtc;
                lastFailure = null;
                status = RuntimeStatus.Stopped;
            }
            RecordLifecycleEvent(
                RuntimeLifecycleEventScope.Runtime,
                phase: "stop",
                outcome: RuntimeLifecycleEventOutcome.Succeeded,
                runtimeStatus: RuntimeStatus.Stopped,
                subjectId: "runtime",
                subjectVersion: Manifest.EngineVersion,
                message: "Runtime completed stop with status Stopped.",
                occurredAtUtc: stopRecordedAtUtc);
            DeactivateExecutionGraphs(stopRecordedAtUtc);
            DeactivateHostedExecutions(stopRecordedAtUtc);
            LogRuntimeTransition(logger, "stop", status.ToString(), Manifest.AppProfile.BlueprintId, Modules.Count);
            return;
        }

        lock (stateGate)
        {
            stoppingAtUtc = DateTimeOffset.UtcNow;
            status = RuntimeStatus.Stopping;
        }

        try
        {
            await ExecuteModulePhaseAsync(
                phase: "stop",
                modules: startedModules.AsEnumerable().Reverse().ToArray(),
                execute: static (lifecycle, context, token) => lifecycle.StopAsync(context, token),
                onSuccess: TrackStoppedModule,
                continueOnFailure: FailurePolicy.StopFailureBehavior == StopFailureBehavior.BestEffortContinue,
                cancellationToken);

            var stopRecordedAtUtc = DateTimeOffset.UtcNow;
            lock (stateGate)
            {
                stoppedAtUtc = stopRecordedAtUtc;
                lastFailure = null;
                status = RuntimeStatus.Stopped;
            }
            RecordLifecycleEvent(
                RuntimeLifecycleEventScope.Runtime,
                phase: "stop",
                outcome: RuntimeLifecycleEventOutcome.Succeeded,
                runtimeStatus: RuntimeStatus.Stopped,
                subjectId: "runtime",
                subjectVersion: Manifest.EngineVersion,
                message: "Runtime completed stop with status Stopped.",
                occurredAtUtc: stopRecordedAtUtc);
            DeactivateExecutionGraphs(stopRecordedAtUtc);
            DeactivateHostedExecutions(stopRecordedAtUtc);
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
        Action<IModule, DateTimeOffset> onSuccess,
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
                    var completedAtUtc = await ExecuteLifecycleModuleAsync(module, lifecycle, phase, execute, cancellationToken);
                    onSuccess(module, completedAtUtc);
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

    private async Task<DateTimeOffset> ExecuteLifecycleModuleAsync(
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
        moduleActivity?.SetTag(CephalonDiagnosticsAttributeKeys.ModuleId, module.Descriptor.Id);
        moduleActivity?.SetTag("cephalon.module.version", moduleVersion);

        var tags = new TagList
        {
            { "cephalon.phase", phase },
            { CephalonDiagnosticsAttributeKeys.ModuleId, module.Descriptor.Id }
        };

        try
        {
            EngineDiagnostics.ModuleTransitionCounter.Add(1, tags);
            await execute(lifecycle, moduleContext!, cancellationToken);
            var completedAtUtc = DateTimeOffset.UtcNow;
            RecordLifecycleEvent(
                RuntimeLifecycleEventScope.Module,
                phase,
                RuntimeLifecycleEventOutcome.Succeeded,
                runtimeStatus: status,
                subjectId: module.Descriptor.Id,
                subjectVersion: moduleVersion,
                message: $"Module '{module.Descriptor.Id}' completed {phase}.",
                occurredAtUtc: completedAtUtc);
            LogModuleTransition(logger, module.Descriptor.Id, phase, moduleVersion);
            return completedAtUtc;
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
            RecordLifecycleEvent(
                RuntimeLifecycleEventScope.Module,
                phase,
                RuntimeLifecycleEventOutcome.Failed,
                runtimeStatus: status,
                subjectId: module.Descriptor.Id,
                subjectVersion: moduleVersion,
                message: $"Module '{module.Descriptor.Id}' failed during {phase}: {exception.Message}",
                exceptionType: exception.GetType().FullName ?? exception.GetType().Name,
                occurredAtUtc: DateTimeOffset.UtcNow);
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
        var occurredAtUtc = DateTimeOffset.UtcNow;
        var restartAvailableAtUtc = canRestart && FailurePolicy.ManualRestartBackoff > TimeSpan.Zero
            ? (DateTimeOffset?)(occurredAtUtc + FailurePolicy.ManualRestartBackoff)
            : null;

        var failure = new RuntimeFailureInfo(
            Phase: moduleException?.Phase ?? phase,
            ModuleId: moduleException?.ModuleId,
            ModuleVersion: moduleException?.ModuleVersion,
            StatusBeforeFailure: statusBeforeFailure,
            ExceptionType: moduleException?.InnerException?.GetType().FullName ?? exception.GetType().FullName ?? exception.GetType().Name,
            Message: moduleException?.InnerException?.Message ?? exception.Message,
            OccurredAtUtc: occurredAtUtc,
            CanRestart: canRestart,
            RestartAvailableAtUtc: restartAvailableAtUtc,
            StartupFailureBehavior: startupFailureBehavior,
            StopFailureBehavior: stopFailureBehavior);

        lock (stateGate)
        {
            lastFailure = failure;
            if (!string.IsNullOrWhiteSpace(failure.ModuleId))
            {
                moduleFailures[failure.ModuleId] = failure;
            }

            stoppedAtUtc = string.Equals(phase, "stop", StringComparison.OrdinalIgnoreCase)
                ? occurredAtUtc
                : stoppedAtUtc;
            status = RuntimeStatus.Failed;
        }

        EngineDiagnostics.RuntimeFailureCounter.Add(1, new TagList
        {
            { "cephalon.phase", failure.Phase },
            { "cephalon.blueprint", Manifest.AppProfile.BlueprintId }
        });
        RecordLifecycleEvent(
            RuntimeLifecycleEventScope.Runtime,
            phase,
            RuntimeLifecycleEventOutcome.Failed,
            runtimeStatus: RuntimeStatus.Failed,
            subjectId: failure.ModuleId ?? "runtime",
            subjectVersion: failure.ModuleVersion ?? Manifest.EngineVersion,
            message: $"Runtime failed during {failure.Phase}: {failure.Message}",
            exceptionType: failure.ExceptionType,
            occurredAtUtc: occurredAtUtc);
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

    private RuntimeStatusSnapshot CreateStatusSnapshotUnsafe()
    {
        return new(status, initializedAtUtc, startedAtUtc, stoppingAtUtc, stoppedAtUtc, restartCount, lastFailure);
    }

    private RuntimeOperationalStory CreateOperationalStoryUnsafe()
    {
        var timelineSnapshot = timeline.ToArray();
        var loadedSnapshot = moduleLoadedAtUtc.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var initializedSnapshot = moduleInitializedAtUtc.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var startedSnapshot = moduleStartedAtUtc.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var stoppedSnapshot = moduleStoppedAtUtc.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var graphLoadedSnapshot = executionGraphLoadedAtUtc.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var graphActivatedSnapshot = executionGraphActivatedAtUtc.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var graphDeactivatedSnapshot = executionGraphDeactivatedAtUtc.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var hostedExecutionLoadedSnapshot = hostedExecutionLoadedAtUtc.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var hostedExecutionActivatedSnapshot = hostedExecutionActivatedAtUtc.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var hostedExecutionDeactivatedSnapshot = hostedExecutionDeactivatedAtUtc.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var failureSnapshot = moduleFailures.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        var modules = Manifest.Modules
            .Select(module =>
            {
                var lastObserved = timelineSnapshot
                    .Where(entry => entry.Scope == RuntimeLifecycleEventScope.Module &&
                        string.Equals(entry.SubjectId, module.Id, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(static entry => entry.OccurredAtUtc)
                    .FirstOrDefault();

                return new RuntimeModuleLifecycleState(
                    ModuleId: module.Id,
                    DisplayName: module.DisplayName,
                    Version: module.Version,
                    AssemblyName: module.AssemblyName,
                    PackageId: module.PackageId,
                    LoadedAtUtc: loadedSnapshot.TryGetValue(module.Id, out var loadedAtUtc) ? loadedAtUtc : null,
                    InitializedAtUtc: initializedSnapshot.TryGetValue(module.Id, out var initializedAtUtcValue) ? initializedAtUtcValue : null,
                    StartedAtUtc: startedSnapshot.TryGetValue(module.Id, out var startedAtUtcValue) ? startedAtUtcValue : null,
                    StoppedAtUtc: stoppedSnapshot.TryGetValue(module.Id, out var stoppedAtUtcValue) ? stoppedAtUtcValue : null,
                    LastObservedPhase: lastObserved?.Phase,
                    LastObservedAtUtc: lastObserved?.OccurredAtUtc,
                    LastFailure: failureSnapshot.TryGetValue(module.Id, out var failure) ? failure : null);
            })
            .ToArray();

        var graphs = executionGraphs
            .Select(graph =>
            {
                var lastObserved = timelineSnapshot
                    .Where(entry => entry.Scope == RuntimeLifecycleEventScope.ExecutionGraph &&
                        string.Equals(entry.SubjectId, graph.Id, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(static entry => entry.OccurredAtUtc)
                    .FirstOrDefault();

                return new RuntimeExecutionGraphState(
                    GraphId: graph.Id,
                    DisplayName: graph.DisplayName,
                    Description: graph.Description,
                    SourceModuleId: graph.SourceModuleId,
                    SourceModuleVersion: TryGetSourceModuleVersion(graph.SourceModuleId),
                    EntryNodeId: graph.EntryNodeId,
                    LoadedAtUtc: graphLoadedSnapshot.TryGetValue(graph.Id, out var graphLoadedAtUtcValue) ? graphLoadedAtUtcValue : null,
                    ActivatedAtUtc: graphActivatedSnapshot.TryGetValue(graph.Id, out var activatedAtUtcValue) ? activatedAtUtcValue : null,
                    DeactivatedAtUtc: graphDeactivatedSnapshot.TryGetValue(graph.Id, out var deactivatedAtUtcValue) ? deactivatedAtUtcValue : null,
                    LastObservedPhase: lastObserved?.Phase,
                    LastObservedAtUtc: lastObserved?.OccurredAtUtc);
            })
            .ToArray();

        var hostedExecutionStates = hostedExecutions
            .Select(hostedExecution =>
            {
                var lastObserved = timelineSnapshot
                    .Where(entry => entry.Scope == RuntimeLifecycleEventScope.HostedExecution &&
                        string.Equals(entry.SubjectId, hostedExecution.Id, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(static entry => entry.OccurredAtUtc)
                    .FirstOrDefault();

                return new RuntimeHostedExecutionState(
                    HostedExecutionId: hostedExecution.Id,
                    DisplayName: hostedExecution.DisplayName,
                    Description: hostedExecution.Description,
                    SourceModuleId: hostedExecution.SourceModuleId,
                    SourceModuleVersion: TryGetSourceModuleVersion(hostedExecution.SourceModuleId),
                    Kind: hostedExecution.Kind,
                    ExecutionGraphId: hostedExecution.ExecutionGraphId,
                    StartsWithHost: hostedExecution.StartsWithHost,
                    LoadedAtUtc: hostedExecutionLoadedSnapshot.TryGetValue(hostedExecution.Id, out var hostedExecutionLoadedAtUtcValue) ? hostedExecutionLoadedAtUtcValue : null,
                    ActivatedAtUtc: hostedExecutionActivatedSnapshot.TryGetValue(hostedExecution.Id, out var hostedExecutionActivatedAtUtcValue) ? hostedExecutionActivatedAtUtcValue : null,
                    DeactivatedAtUtc: hostedExecutionDeactivatedSnapshot.TryGetValue(hostedExecution.Id, out var hostedExecutionDeactivatedAtUtcValue) ? hostedExecutionDeactivatedAtUtcValue : null,
                    LastObservedPhase: lastObserved?.Phase,
                    LastObservedAtUtc: lastObserved?.OccurredAtUtc);
            })
            .ToArray();

        return new RuntimeOperationalStory(
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Status: CreateStatusSnapshotUnsafe(),
            LoadedPackages: Manifest.Packages,
            Modules: modules,
            Timeline: timelineSnapshot)
        {
            ExecutionGraphs = graphs,
            HostedExecutions = hostedExecutionStates
        };
    }

    private void SeedLoadedStoryFromManifest()
    {
        foreach (var package in Manifest.Packages)
        {
            RecordLifecycleEvent(
                RuntimeLifecycleEventScope.Package,
                phase: "load",
                outcome: RuntimeLifecycleEventOutcome.Succeeded,
                runtimeStatus: RuntimeStatus.Created,
                subjectId: package.Id,
                subjectVersion: package.Version,
                message: $"Package '{package.Id}' loaded via {package.Kind} from {package.SourcePath}.",
                occurredAtUtc: Manifest.GeneratedAtUtc);
        }

        foreach (var module in Manifest.Modules)
        {
            lock (stateGate)
            {
                moduleLoadedAtUtc[module.Id] = Manifest.GeneratedAtUtc;
            }

            var message = string.IsNullOrWhiteSpace(module.PackageId)
                ? $"Module '{module.Id}' loaded from assembly {module.AssemblyName}."
                : $"Module '{module.Id}' loaded from package '{module.PackageId}'.";
            RecordLifecycleEvent(
                RuntimeLifecycleEventScope.Module,
                phase: "load",
                outcome: RuntimeLifecycleEventOutcome.Succeeded,
                runtimeStatus: RuntimeStatus.Created,
                subjectId: module.Id,
                subjectVersion: module.Version,
                message: message,
                occurredAtUtc: Manifest.GeneratedAtUtc);
        }

        foreach (var graph in executionGraphs)
        {
            RecordExecutionGraphTransition(
                graph,
                phase: "load",
                runtimeStatus: RuntimeStatus.Created,
                occurredAtUtc: Manifest.GeneratedAtUtc,
                message: $"Execution graph '{graph.Id}' loaded from module '{graph.SourceModuleId}'.");
        }

        foreach (var hostedExecution in hostedExecutions)
        {
            RecordHostedExecutionLifecycle(
                hostedExecution,
                phase: "load",
                runtimeStatus: RuntimeStatus.Created,
                occurredAtUtc: Manifest.GeneratedAtUtc,
                message: $"Hosted execution '{hostedExecution.Id}' loaded from module '{hostedExecution.SourceModuleId}'.");
        }
    }

    private void RecordLifecycleEvent(
        RuntimeLifecycleEventScope scope,
        string phase,
        RuntimeLifecycleEventOutcome outcome,
        RuntimeStatus runtimeStatus,
        string? subjectId,
        string? subjectVersion,
        string message,
        string? exceptionType = null,
        DateTimeOffset? occurredAtUtc = null)
    {
        lock (stateGate)
        {
            timeline.Add(new RuntimeLifecycleEvent(
                OccurredAtUtc: occurredAtUtc ?? DateTimeOffset.UtcNow,
                Scope: scope,
                Phase: phase,
                Outcome: outcome,
                RuntimeStatus: runtimeStatus,
                SubjectId: subjectId,
                SubjectVersion: subjectVersion,
                Message: message,
                ExceptionType: exceptionType));

            if (timeline.Count > MaxTimelineEntries)
            {
                timeline.RemoveRange(0, timeline.Count - MaxTimelineEntries);
            }
        }
    }

    private void TrackInitializedModule(IModule module, DateTimeOffset occurredAtUtc)
    {
        if (!initializedModules.Contains(module))
        {
            initializedModules.Add(module);
        }

        lock (stateGate)
        {
            moduleInitializedAtUtc[module.Descriptor.Id] = occurredAtUtc;
            moduleFailures.Remove(module.Descriptor.Id);
        }
    }

    private void TrackStartedModule(IModule module, DateTimeOffset occurredAtUtc)
    {
        if (!startedModules.Contains(module))
        {
            startedModules.Add(module);
        }

        lock (stateGate)
        {
            moduleStartedAtUtc[module.Descriptor.Id] = occurredAtUtc;
            moduleFailures.Remove(module.Descriptor.Id);
        }
    }

    private void TrackStoppedModule(IModule module, DateTimeOffset occurredAtUtc)
    {
        startedModules.Remove(module);

        lock (stateGate)
        {
            moduleStoppedAtUtc[module.Descriptor.Id] = occurredAtUtc;
        }
    }

    private void ActivateExecutionGraphs(DateTimeOffset occurredAtUtc)
    {
        foreach (var graph in executionGraphs)
        {
            RecordExecutionGraphTransition(
                graph,
                phase: "activate",
                runtimeStatus: RuntimeStatus.Started,
                occurredAtUtc: occurredAtUtc,
                message: $"Execution graph '{graph.Id}' became active from module '{graph.SourceModuleId}'.");
        }
    }

    private void DeactivateExecutionGraphs(DateTimeOffset occurredAtUtc)
    {
        foreach (var graph in executionGraphs)
        {
            RecordExecutionGraphTransition(
                graph,
                phase: "deactivate",
                runtimeStatus: RuntimeStatus.Stopped,
                occurredAtUtc: occurredAtUtc,
                message: $"Execution graph '{graph.Id}' became inactive because the runtime stopped.");
        }
    }

    private void ActivateHostedExecutions(DateTimeOffset occurredAtUtc)
    {
        foreach (var hostedExecution in hostedExecutions.Where(static hostedExecution => hostedExecution.StartsWithHost))
        {
            RecordHostedExecutionLifecycle(
                hostedExecution,
                phase: "activate",
                runtimeStatus: RuntimeStatus.Started,
                occurredAtUtc: occurredAtUtc,
                message: $"Hosted execution '{hostedExecution.Id}' became active with the runtime host.");
        }
    }

    private void DeactivateHostedExecutions(DateTimeOffset occurredAtUtc)
    {
        foreach (var hostedExecution in hostedExecutions.Where(static hostedExecution => hostedExecution.StartsWithHost))
        {
            RecordHostedExecutionLifecycle(
                hostedExecution,
                phase: "deactivate",
                runtimeStatus: RuntimeStatus.Stopped,
                occurredAtUtc: occurredAtUtc,
                message: $"Hosted execution '{hostedExecution.Id}' became inactive because the runtime stopped.");
        }
    }

    private void RecordExecutionGraphTransition(
        ExecutionGraphDescriptor graph,
        string phase,
        RuntimeStatus runtimeStatus,
        DateTimeOffset occurredAtUtc,
        string message)
    {
        lock (stateGate)
        {
            if (string.Equals(phase, "load", StringComparison.OrdinalIgnoreCase))
            {
                executionGraphLoadedAtUtc[graph.Id] = occurredAtUtc;
            }
            else if (string.Equals(phase, "activate", StringComparison.OrdinalIgnoreCase))
            {
                executionGraphActivatedAtUtc[graph.Id] = occurredAtUtc;
            }
            else if (string.Equals(phase, "deactivate", StringComparison.OrdinalIgnoreCase))
            {
                executionGraphDeactivatedAtUtc[graph.Id] = occurredAtUtc;
            }
        }

        EngineDiagnostics.ExecutionGraphTransitionCounter.Add(1, new TagList
        {
            { "cephalon.phase", phase },
            { "cephalon.execution-graph.id", graph.Id },
            { "cephalon.module.id", graph.SourceModuleId }
        });
        RecordLifecycleEvent(
            RuntimeLifecycleEventScope.ExecutionGraph,
            phase: phase,
            outcome: RuntimeLifecycleEventOutcome.Succeeded,
            runtimeStatus: runtimeStatus,
            subjectId: graph.Id,
            subjectVersion: TryGetSourceModuleVersion(graph.SourceModuleId),
            message: message,
            occurredAtUtc: occurredAtUtc);
        LogExecutionGraphTransition(logger, graph.Id, phase, graph.SourceModuleId, runtimeStatus.ToString());
    }

    private string? TryGetSourceModuleVersion(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return null;
        }

        return moduleVersionsById.TryGetValue(sourceModuleId, out var version)
            ? version
            : null;
    }

    private void RecordHostedExecutionLifecycle(
        HostedExecutionDescriptor hostedExecution,
        string phase,
        RuntimeStatus runtimeStatus,
        DateTimeOffset occurredAtUtc,
        string message)
    {
        lock (stateGate)
        {
            if (string.Equals(phase, "load", StringComparison.OrdinalIgnoreCase))
            {
                hostedExecutionLoadedAtUtc[hostedExecution.Id] = occurredAtUtc;
            }
            else if (string.Equals(phase, "activate", StringComparison.OrdinalIgnoreCase))
            {
                hostedExecutionActivatedAtUtc[hostedExecution.Id] = occurredAtUtc;
            }
            else if (string.Equals(phase, "deactivate", StringComparison.OrdinalIgnoreCase))
            {
                hostedExecutionDeactivatedAtUtc[hostedExecution.Id] = occurredAtUtc;
            }
        }

        EngineDiagnostics.HostedExecutionTransitionCounter.Add(1, new TagList
        {
            { "cephalon.phase", phase },
            { "cephalon.hosted-execution.id", hostedExecution.Id },
            { "cephalon.hosted-execution.kind", hostedExecution.Kind },
            { "cephalon.execution-graph.id", hostedExecution.ExecutionGraphId },
            { "cephalon.module.id", hostedExecution.SourceModuleId }
        });
        RecordLifecycleEvent(
            RuntimeLifecycleEventScope.HostedExecution,
            phase: phase,
            outcome: RuntimeLifecycleEventOutcome.Succeeded,
            runtimeStatus: runtimeStatus,
            subjectId: hostedExecution.Id,
            subjectVersion: TryGetSourceModuleVersion(hostedExecution.SourceModuleId),
            message: message,
            occurredAtUtc: occurredAtUtc);
        LogHostedExecutionTransition(
            logger,
            hostedExecution.Id,
            phase,
            hostedExecution.SourceModuleId,
            hostedExecution.Kind,
            hostedExecution.ExecutionGraphId ?? "(none)",
            runtimeStatus.ToString());
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

    private static void LogExecutionGraphTransition(
        ILogger? logger,
        string graphId,
        string phase,
        string sourceModuleId,
        string status)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogExecutionGraphTransitionMessage(logger, graphId, phase, sourceModuleId, status, null);
    }

    private static void LogHostedExecutionTransition(
        ILogger? logger,
        string hostedExecutionId,
        string phase,
        string sourceModuleId,
        string kind,
        string executionGraphId,
        string status)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogHostedExecutionTransitionMessage(logger, hostedExecutionId, phase, sourceModuleId, kind, executionGraphId, status, null);
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
