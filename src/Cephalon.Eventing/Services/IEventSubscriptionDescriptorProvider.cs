namespace Cephalon.Eventing.Services;

/// <summary>
/// Allows an in-process subscription executor to provide its declared subscription descriptor.
/// </summary>
/// <remarks>
/// Implement this optional interface on an <see cref="IEventSubscriptionExecutor" /> when the
/// executor owns enough code-first metadata for the native eventing pack to register its
/// descriptor automatically. This keeps subscription authoring typed and dependency-injection
/// owned without binding handlers from configuration.
/// </remarks>
public interface IEventSubscriptionDescriptorProvider
{
    /// <summary>
    /// Gets the code-owned subscription descriptor associated with the executor.
    /// </summary>
    EventSubscriptionDescriptor SubscriptionDescriptor { get; }
}
