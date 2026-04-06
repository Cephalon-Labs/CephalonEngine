using Cephalon.Abstractions.Health;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.DependencyHealth.Core.Configuration;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Observability.DependencyHealth.Core.Hosting;

/// <summary>Shared DI registration helper for dependency-health providers.</summary>
internal static class DependencyHealthServiceRegistration
{
    /// <summary>
    /// Registers dependency-health services for one provider. Creates a dedicated
    /// <see cref="DependencyHealthStore"/> instance per call so multiple providers
    /// can coexist without sharing a single store.
    /// </summary>
    public static IServiceCollection AddDependencyHealth<TOptions, TDefinition, TDiagnosticsContributor>(
        IServiceCollection services,
        TOptions options,
        Func<IServiceProvider, DependencyHealthStore, IHostedService> hostedServiceFactory)
        where TOptions : DependencyHealthOptionsBase<TDefinition>
        where TDefinition : DependencyDefinitionBase
        where TDiagnosticsContributor : class, IDiagnosticsConventionContributor
    {
        // Each provider gets its own store and contributor instances.
        var store = new DependencyHealthStore();
        var contributor = new DependencyHealthContributor(store);

        services.TryAddSingleton(options);
        // AddSingleton with instance — does NOT deduplicate. All 18 providers register their own contributor.
        services.AddSingleton<IDependencyHealthContributor>(contributor);
        // TryAddEnumerable for diagnostics contributor — deduplicated by implementation type (intentional).
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, TDiagnosticsContributor>());
        // AddSingleton with factory — captures the per-provider store via closure.
        services.AddSingleton<IHostedService>(sp => hostedServiceFactory(sp, store));

        return services;
    }
}
