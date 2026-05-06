using Cephalon.Abstractions.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Behaviors.Services;

/// <summary>
/// Registers behavior implementation descriptors into an <see cref="IServiceCollection" />.
/// </summary>
/// <remarks>
/// This helper keeps source-generated and explicit behavior registration on the same descriptor
/// path without exposing a mutable runtime registry.
/// </remarks>
public static class BehaviorImplementationRegistration
{
    /// <summary>
    /// Adds a behavior implementation descriptor when the behavior identifier is not already present.
    /// </summary>
    /// <param name="services">The service collection receiving the descriptor.</param>
    /// <param name="id">The stable behavior identifier.</param>
    /// <param name="behaviorType">The concrete behavior implementation type.</param>
    /// <param name="idempotencyMode">The declared idempotency mode for behavior execution.</param>
    /// <returns>
    /// <see langword="true" /> when a descriptor was added; <see langword="false" /> when an
    /// equivalent descriptor already existed.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the same behavior identifier is already registered for a different type.
    /// </exception>
    public static bool TryRegister(
        IServiceCollection services,
        string id,
        Type behaviorType,
        BehaviorIdempotencyMode idempotencyMode = BehaviorIdempotencyMode.Unknown)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(behaviorType);

        var normalizedId = id.Trim();
        foreach (var descriptor in GetRegisteredDescriptors(services))
        {
            if (!string.Equals(descriptor.Id, normalizedId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (descriptor.BehaviorType != behaviorType)
            {
                throw new InvalidOperationException(
                    $"Cannot register behavior id '{normalizedId}' for '{behaviorType.FullName}' because it is already registered by '{descriptor.BehaviorType.FullName}'.");
            }

            return false;
        }

        services.AddSingleton(new BehaviorImplementationDescriptor(
            normalizedId,
            behaviorType,
            idempotencyMode));
        return true;
    }

    internal static IReadOnlyList<BehaviorImplementationDescriptor> GetRegisteredDescriptors(
        IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var descriptors = new List<BehaviorImplementationDescriptor>();
        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(BehaviorImplementationDescriptor) &&
                descriptor.ImplementationInstance is BehaviorImplementationDescriptor implementation)
            {
                descriptors.Add(implementation);
            }
        }

        return descriptors;
    }
}
