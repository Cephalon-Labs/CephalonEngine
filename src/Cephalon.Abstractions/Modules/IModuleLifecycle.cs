namespace Cephalon.Abstractions.Modules;

public interface IModuleLifecycle
{
    Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken);

    Task StartAsync(ModuleContext context, CancellationToken cancellationToken);

    Task StopAsync(ModuleContext context, CancellationToken cancellationToken);
}
