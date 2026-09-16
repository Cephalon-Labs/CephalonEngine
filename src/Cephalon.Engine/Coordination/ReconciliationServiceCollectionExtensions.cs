using Cephalon.Engine.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Engine.Coordination;

/// <summary>Registers the opt-in coordination service and its observation section.</summary>
public static class ReconciliationServiceCollectionExtensions
{
    /// <summary>Registers one executor per service provider and contributes its redacted runtime section.</summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="options">Immutable limits; first registration wins.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddCephalonReconciliation(this IServiceCollection services,
        ReconciliationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(options ?? new ReconciliationOptions());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ReconciliationExecutor>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRuntimeIntrospectionSectionContributor, SectionContributor>());
        return services;
    }

    private sealed class SectionContributor(ReconciliationExecutor executor) : IRuntimeIntrospectionSectionContributor
    {
        public RuntimeIntrospectionSection DescribeSection() => executor.DescribeSection();
    }
}
