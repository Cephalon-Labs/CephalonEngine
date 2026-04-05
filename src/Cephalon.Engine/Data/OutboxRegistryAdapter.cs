using Cephalon.Abstractions.Data;

namespace Cephalon.Engine.Data;

internal sealed class OutboxRegistryAdapter(
    string moduleId,
    List<OutboxDescriptor> outboxes) : IOutboxRegistry
{
    public void Add(OutboxDescriptor outbox)
    {
        ArgumentNullException.ThrowIfNull(outbox);

        if (!string.Equals(outbox.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Outbox '{outbox.Id}' declared source module '{outbox.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        outboxes.Add(outbox);
    }
}
