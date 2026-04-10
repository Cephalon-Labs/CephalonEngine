using Cephalon.Data.EntityFramework.Configuration;
using Cephalon.Data.EntityFramework.Modules;
using Cephalon.Engine.Composition;
using Microsoft.EntityFrameworkCore;

namespace Cephalon.Data.EntityFramework.Registration;

/// <summary>
/// Registers the Entity Framework Core data companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class EntityFrameworkDataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Entity Framework Core data pack with one shared <see cref="DbContext" /> type for both read and write workloads.
    /// </summary>
    /// <typeparam name="TDbContext">The shared <see cref="DbContext" /> type.</typeparam>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configureDbContext">The callback that configures the shared Entity Framework Core <see cref="DbContext" />.</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned Entity Framework pack options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Pair this pack with <c>AddData()</c> when you want Cephalon-managed <c>IReadStore</c> and <c>IWriteStore</c>
    /// dispatching on top of the registered <see cref="DbContext" /> services.
    /// </remarks>
    public static EngineBuilder AddEntityFrameworkData<TDbContext>(
        this EngineBuilder builder,
        Action<DbContextOptionsBuilder> configureDbContext,
        Action<EntityFrameworkDataOptions>? configure = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        return builder.AddEntityFrameworkData<TDbContext, TDbContext>(
            configureReadDbContext: (_, options) => configureDbContext(options),
            configureWriteDbContext: (_, options) => configureDbContext(options),
            configure: configure);
    }

    /// <summary>
    /// Adds the Entity Framework Core data pack with one shared <see cref="DbContext" /> type configured from
    /// the engine-owned <c>Engine:Databases</c> topology.
    /// </summary>
    /// <typeparam name="TDbContext">The shared <see cref="DbContext" /> type.</typeparam>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configureDbContext">
    /// The callback that selects the EF Core provider for the resolved role and applies provider-specific tuning
    /// such as retries.
    /// </param>
    /// <param name="configure">An optional callback that configures the host-owned Entity Framework pack options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// This overload keeps the physical connection topology inside <c>Engine:Databases</c> while leaving the provider
    /// package selection with the consuming host or provider companion pack.
    /// </remarks>
    public static EngineBuilder AddEntityFrameworkData<TDbContext>(
        this EngineBuilder builder,
        Action<EntityFrameworkDatabaseRoleContext, DbContextOptionsBuilder> configureDbContext,
        Action<EntityFrameworkDataOptions>? configure = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        return builder.AddEntityFrameworkData<TDbContext, TDbContext>(
            configureReadDbContext: (serviceProvider, optionsBuilder) =>
            {
                var role = EntityFrameworkDatabaseRoleResolver.ResolveSharedWrite(serviceProvider);
                ApplyRuntimeDefaults(optionsBuilder, role);
                configureDbContext(role, optionsBuilder);
            },
            configureWriteDbContext: (serviceProvider, optionsBuilder) =>
            {
                var role = EntityFrameworkDatabaseRoleResolver.ResolveSharedWrite(serviceProvider);
                ApplyRuntimeDefaults(optionsBuilder, role);
                configureDbContext(role, optionsBuilder);
            },
            configure: options =>
            {
                options.UsesEngineDatabaseTopology = true;
                configure?.Invoke(options);
            });
    }

    /// <summary>
    /// Adds the Entity Framework Core data pack with explicit read and write <see cref="DbContext" /> types.
    /// </summary>
    /// <typeparam name="TReadDbContext">The read-side <see cref="DbContext" /> type.</typeparam>
    /// <typeparam name="TWriteDbContext">The write-side <see cref="DbContext" /> type.</typeparam>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configureReadDbContext">The callback that configures the read-side Entity Framework Core <see cref="DbContext" />.</param>
    /// <param name="configureWriteDbContext">The callback that configures the write-side Entity Framework Core <see cref="DbContext" />.</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned Entity Framework pack options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Pair this pack with <c>AddData()</c> when you want Cephalon-managed <c>IReadStore</c> and <c>IWriteStore</c>
    /// dispatching on top of the registered <see cref="DbContext" /> services.
    /// </remarks>
    public static EngineBuilder AddEntityFrameworkData<TReadDbContext, TWriteDbContext>(
        this EngineBuilder builder,
        Action<DbContextOptionsBuilder> configureReadDbContext,
        Action<DbContextOptionsBuilder> configureWriteDbContext,
        Action<EntityFrameworkDataOptions>? configure = null)
        where TReadDbContext : DbContext
        where TWriteDbContext : DbContext
    {
        return builder.AddEntityFrameworkData<TReadDbContext, TWriteDbContext>(
            configureReadDbContext: (_, options) => configureReadDbContext(options),
            configureWriteDbContext: (_, options) => configureWriteDbContext(options),
            configure: configure);
    }

    /// <summary>
    /// Adds the Entity Framework Core data pack with distinct read and write <see cref="DbContext" /> types configured
    /// from the engine-owned <c>Engine:Databases</c> topology.
    /// </summary>
    /// <typeparam name="TReadDbContext">The read-side <see cref="DbContext" /> type.</typeparam>
    /// <typeparam name="TWriteDbContext">The write-side <see cref="DbContext" /> type.</typeparam>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configureDbContext">
    /// The callback that selects the EF Core provider for each resolved role and applies provider-specific tuning such
    /// as retries.
    /// </param>
    /// <param name="configure">An optional callback that configures the host-owned Entity Framework pack options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddEntityFrameworkData<TReadDbContext, TWriteDbContext>(
        this EngineBuilder builder,
        Action<EntityFrameworkDatabaseRoleContext, DbContextOptionsBuilder> configureDbContext,
        Action<EntityFrameworkDataOptions>? configure = null)
        where TReadDbContext : DbContext
        where TWriteDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        return builder.AddEntityFrameworkData<TReadDbContext, TWriteDbContext>(
            configureReadDbContext: (serviceProvider, optionsBuilder) =>
            {
                var role = EntityFrameworkDatabaseRoleResolver.ResolveRead(serviceProvider);
                ApplyRuntimeDefaults(optionsBuilder, role);
                configureDbContext(role, optionsBuilder);
            },
            configureWriteDbContext: (serviceProvider, optionsBuilder) =>
            {
                var role = EntityFrameworkDatabaseRoleResolver.ResolveWrite(serviceProvider);
                ApplyRuntimeDefaults(optionsBuilder, role);
                configureDbContext(role, optionsBuilder);
            },
            configure: options =>
            {
                options.UsesEngineDatabaseTopology = true;
                configure?.Invoke(options);
            });
    }

    private static EngineBuilder AddEntityFrameworkData<TReadDbContext, TWriteDbContext>(
        this EngineBuilder builder,
        Action<IServiceProvider, DbContextOptionsBuilder> configureReadDbContext,
        Action<IServiceProvider, DbContextOptionsBuilder> configureWriteDbContext,
        Action<EntityFrameworkDataOptions>? configure = null)
        where TReadDbContext : DbContext
        where TWriteDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureReadDbContext);
        ArgumentNullException.ThrowIfNull(configureWriteDbContext);

        var options = new EntityFrameworkDataOptions(
            typeof(TReadDbContext),
            typeof(TWriteDbContext));
        configure?.Invoke(options);

        builder.AddModule(new EntityFrameworkDataModule<TReadDbContext, TWriteDbContext>(
            options,
            configureReadDbContext,
            configureWriteDbContext));
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
