using Cephalon.Audit.EntityFramework.Configuration;
using Cephalon.Audit.EntityFramework.Modules;
using Cephalon.Data.EntityFramework.Configuration;
using Cephalon.Engine.Composition;
using Microsoft.EntityFrameworkCore;

namespace Cephalon.Audit.EntityFramework.Registration;

/// <summary>
/// Registers the Entity Framework durable audit-history provider with an <see cref="EngineBuilder" />.
/// </summary>
public static class EntityFrameworkAuditHistoryEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Entity Framework durable audit-history provider with a host-owned <see cref="DbContext" /> callback.
    /// </summary>
    /// <typeparam name="TDbContext">The <see cref="DbContext" /> type that persists audit-history rows.</typeparam>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configureDbContext">The callback that configures the durable audit-history <see cref="DbContext" />.</param>
    /// <param name="configure">An optional callback that configures host-owned audit-history provider options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddEntityFrameworkAuditHistory<TDbContext>(
        this EngineBuilder builder,
        Action<DbContextOptionsBuilder> configureDbContext,
        Action<EntityFrameworkAuditHistoryOptions>? configure = null)
        where TDbContext : DbContext, IEntityFrameworkAuditHistoryContext
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        return AddEntityFrameworkAuditHistoryCore<TDbContext, Action<DbContextOptionsBuilder>>(
            builder,
            configureDbContext: (_, optionsBuilder, configureDbContext) => configureDbContext(optionsBuilder),
            state: configureDbContext,
            configure: configure);
    }

    /// <summary>
    /// Adds the Entity Framework durable audit-history provider with the configured audit-history role resolved from <c>Engine:Databases</c>.
    /// </summary>
    /// <typeparam name="TDbContext">The <see cref="DbContext" /> type that persists audit-history rows.</typeparam>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configureDbContext">
    /// The callback that selects the EF Core provider for the resolved audit-history role and applies provider-specific tuning.
    /// </param>
    /// <param name="configure">An optional callback that configures host-owned audit-history provider options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddEntityFrameworkAuditHistory<TDbContext>(
        this EngineBuilder builder,
        Action<EntityFrameworkDatabaseRoleContext, DbContextOptionsBuilder> configureDbContext,
        Action<EntityFrameworkAuditHistoryOptions>? configure = null)
        where TDbContext : DbContext, IEntityFrameworkAuditHistoryContext
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        return AddEntityFrameworkAuditHistoryCore<TDbContext, Action<EntityFrameworkDatabaseRoleContext, DbContextOptionsBuilder>>(
            builder,
            configureDbContext: (serviceProvider, optionsBuilder, configureDbContext) =>
            {
                var role = EntityFrameworkDatabaseRoleResolver.ResolveHistory(serviceProvider);
                ApplyRuntimeDefaults(optionsBuilder, role);
                configureDbContext(role, optionsBuilder);
            },
            state: configureDbContext,
            configure: options =>
            {
                options.UsesEngineDatabaseTopology = true;
                configure?.Invoke(options);
            });
    }

    private static EngineBuilder AddEntityFrameworkAuditHistoryCore<TDbContext, TState>(
        this EngineBuilder builder,
        Action<IServiceProvider, DbContextOptionsBuilder, TState> configureDbContext,
        TState state,
        Action<EntityFrameworkAuditHistoryOptions>? configure = null)
        where TDbContext : DbContext, IEntityFrameworkAuditHistoryContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        var options = new EntityFrameworkAuditHistoryOptions(typeof(TDbContext));
        configure?.Invoke(options);

        builder.AddModule(new EntityFrameworkAuditHistoryModule<TDbContext>(
            options,
            (serviceProvider, optionsBuilder) => configureDbContext(serviceProvider, optionsBuilder, state)));
        return builder;
    }

    private static void ApplyRuntimeDefaults(
        DbContextOptionsBuilder optionsBuilder,
        EntityFrameworkDatabaseRoleContext role)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(role);

        if (role.Runtime.EnableDetailedErrors.HasValue)
        {
            optionsBuilder.EnableDetailedErrors(role.Runtime.EnableDetailedErrors.Value);
        }

        if (role.Runtime.EnableSensitiveDataLogging.HasValue)
        {
            optionsBuilder.EnableSensitiveDataLogging(role.Runtime.EnableSensitiveDataLogging.Value);
        }
    }
}
