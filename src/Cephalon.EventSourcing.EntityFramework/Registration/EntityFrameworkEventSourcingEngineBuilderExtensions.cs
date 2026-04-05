using Cephalon.Engine.Composition;
using Cephalon.EventSourcing.EntityFramework.Modules;
using Microsoft.EntityFrameworkCore;

namespace Cephalon.EventSourcing.EntityFramework.Registration;

/// <summary>
/// Registers the Entity Framework event-store provider with an <see cref="EngineBuilder" />.
/// </summary>
public static class EntityFrameworkEventSourcingEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Entity Framework event-store provider to the engine.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type that persists event rows.</typeparam>
    /// <param name="builder">The engine builder to extend.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddEntityFrameworkEventSourcing<TContext>(
        this EngineBuilder builder)
        where TContext : DbContext, IEntityFrameworkEventContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddModule(new EntityFrameworkEventSourcingModule<TContext>());
        return builder;
    }
}
