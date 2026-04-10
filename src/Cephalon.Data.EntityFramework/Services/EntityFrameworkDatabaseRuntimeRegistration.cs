using Cephalon.Abstractions.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Data.EntityFramework.Services;

internal static class EntityFrameworkDatabaseRuntimeRegistration
{
    public static void AddEngineDatabaseTopologyServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<EntityFrameworkDatabaseRoleRuntimeContributor>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseRoleRuntimeContributor, EntityFrameworkDatabaseRoleRuntimeContributorAdapter>());

        services.TryAddSingleton<EntityFrameworkDatabaseMigrationCatalog>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseMigrationContributor, EntityFrameworkDatabaseMigrationContributorAdapter>());

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EntityFrameworkDatabaseMigrationHostedService>());
    }
}
