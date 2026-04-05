using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.EntityFramework.Services;

internal sealed class EntityFrameworkEventStoreContributor<TContext> : IEventStoreContributor
    where TContext : IEntityFrameworkEventContext
{
    public IReadOnlyList<EventStreamDescriptor> Contribute()
    {
        return
        [
            new EventStreamDescriptor(
                id: "entity-framework-event-store",
                displayName: "Entity Framework Event Store",
                description: "Appends and replays domain events through the active Entity Framework Core DbContext.",
                sourceModuleId: "entity-framework-event-sourcing",
                provider: "entity-framework",
                mode: "append-only",
                tags: ["event-sourcing", "entity-framework", "relational"],
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["dbContext"] = typeof(TContext).FullName ?? typeof(TContext).Name,
                    ["storage"] = "CephalonEvents",
                    ["concurrency"] = "optimistic-version"
                })
        ];
    }
}
