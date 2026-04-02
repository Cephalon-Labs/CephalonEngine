using Cephalon.Abstractions.Modules;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Manifest;

namespace Cephalon.Engine.Runtime;

/// <summary>
/// Represents a built Cephalon runtime that can be initialized, started, restarted, stopped,
/// and introspected by a host.
/// </summary>
public interface IRuntime
{
    /// <summary>
    /// Gets the ordered module set that participates in runtime lifecycle execution.
    /// </summary>
    IReadOnlyList<IModule> Modules { get; }

    /// <summary>
    /// Gets the immutable manifest that was produced when the runtime was built.
    /// </summary>
    RuntimeManifest Manifest { get; }

    /// <summary>
    /// Gets the failure policy that governs startup, stop, and restart behavior.
    /// </summary>
    FailurePolicy FailurePolicy { get; }

    /// <summary>
    /// Gets the current lifecycle status of the runtime.
    /// </summary>
    RuntimeStatus Status { get; }

    /// <summary>
    /// Gets the current status as a serializable snapshot.
    /// </summary>
    RuntimeStatusSnapshot StatusSnapshot { get; }

    /// <summary>
    /// Gets the richer operator-facing runtime story that explains what loaded, started, failed, and why.
    /// </summary>
    RuntimeOperationalStory OperationalStory { get; }

    /// <summary>
    /// Gets the most recent failure captured by the lifecycle state machine, if any.
    /// </summary>
    RuntimeFailureInfo? LastFailure { get; }

    /// <summary>
    /// Gets the number of successful manual restart attempts completed by the runtime.
    /// </summary>
    int RestartCount { get; }

    /// <summary>
    /// Initializes the runtime and all lifecycle-aware modules.
    /// </summary>
    /// <param name="services">The service provider that modules should use during initialization.</param>
    /// <param name="cancellationToken">A token that can cancel the initialization operation.</param>
    Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the runtime and all lifecycle-aware modules.
    /// </summary>
    /// <param name="services">The service provider that modules should use during startup.</param>
    /// <param name="cancellationToken">A token that can cancel the start operation.</param>
    /// <remarks>
    /// If the runtime has not been initialized yet, the implementation may initialize it first.
    /// </remarks>
    Task StartAsync(IServiceProvider services, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts the runtime when the configured failure policy allows it.
    /// </summary>
    /// <param name="services">The service provider that modules should use during the restart flow.</param>
    /// <param name="cancellationToken">A token that can cancel the restart operation.</param>
    Task RestartAsync(IServiceProvider services, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the runtime and all started lifecycle-aware modules.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the stop operation.</param>
    Task StopAsync(CancellationToken cancellationToken = default);
}
