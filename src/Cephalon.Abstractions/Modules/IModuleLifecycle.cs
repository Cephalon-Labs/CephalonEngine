namespace Cephalon.Abstractions.Modules;

/// <summary>
/// Defines the deterministic lifecycle hooks managed by the host runtime.
/// </summary>
public interface IModuleLifecycle
{
    /// <summary>
    /// Initializes the module before the runtime starts serving work.
    /// </summary>
    /// <param name="context">The module runtime context.</param>
    /// <param name="cancellationToken">A token that cancels initialization.</param>
    /// <returns>A task that completes when initialization finishes.</returns>
    Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Starts the module after initialization has completed.
    /// </summary>
    /// <param name="context">The module runtime context.</param>
    /// <param name="cancellationToken">A token that cancels startup.</param>
    /// <returns>A task that completes when startup finishes.</returns>
    Task StartAsync(ModuleContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Stops the module during runtime shutdown.
    /// </summary>
    /// <param name="context">The module runtime context.</param>
    /// <param name="cancellationToken">A token that cancels shutdown.</param>
    /// <returns>A task that completes when shutdown finishes.</returns>
    Task StopAsync(ModuleContext context, CancellationToken cancellationToken);
}
