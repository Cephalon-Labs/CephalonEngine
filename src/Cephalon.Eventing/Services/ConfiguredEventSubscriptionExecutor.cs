using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Eventing.Services;

internal sealed class ConfiguredEventSubscriptionExecutor : IEventSubscriptionExecutor
{
    private readonly EventSubscriptionHandlerDescriptor descriptor;
    private readonly IServiceScopeFactory scopeFactory;

    public ConfiguredEventSubscriptionExecutor(
        EventSubscriptionHandlerDescriptor descriptor,
        IServiceScopeFactory scopeFactory)
    {
        this.descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        this.scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        HandlerType = ResolveHandlerType(descriptor.HandlerTypeName);

        if (!typeof(IEventSubscriptionHandler).IsAssignableFrom(HandlerType))
        {
            throw new InvalidOperationException(
                $"Configured event subscription handler type '{descriptor.HandlerTypeName}' for subscription '{descriptor.SubscriptionId}' must implement {typeof(IEventSubscriptionHandler).FullName}.");
        }

        if (HandlerType.IsAbstract || HandlerType.IsInterface)
        {
            throw new InvalidOperationException(
                $"Configured event subscription handler type '{descriptor.HandlerTypeName}' for subscription '{descriptor.SubscriptionId}' must be a concrete type.");
        }
    }

    public string SubscriptionId => descriptor.SubscriptionId;

    public string HandlerTypeName => descriptor.HandlerTypeName;

    public Type HandlerType { get; }

    public string Source => descriptor.Source;

    public string ConfigurationPath => descriptor.ConfigurationPath;

    public async ValueTask ExecuteAsync(
        EventSubscriptionExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetService(HandlerType);
        var ownsHandler = service is null;
        var handler = (IEventSubscriptionHandler)(service ?? ActivatorUtilities.CreateInstance(scope.ServiceProvider, HandlerType));

        try
        {
            await handler.HandleAsync(context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (ownsHandler)
            {
                await DisposeOwnedHandlerAsync(handler).ConfigureAwait(false);
            }
        }
    }

    private static Type ResolveHandlerType(string handlerTypeName)
    {
        var handlerType = Type.GetType(handlerTypeName, throwOnError: false, ignoreCase: false);
        if (handlerType is not null)
        {
            return handlerType;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            handlerType = assembly.GetType(handlerTypeName, throwOnError: false, ignoreCase: false);
            if (handlerType is not null)
            {
                return handlerType;
            }
        }

        throw new InvalidOperationException(
            $"Configured event subscription handler type '{handlerTypeName}' could not be resolved. Use an assembly-qualified type name or ensure the handler assembly is loaded.");
    }

    private static async ValueTask DisposeOwnedHandlerAsync(IEventSubscriptionHandler handler)
    {
        if (handler is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            return;
        }

        if (handler is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
