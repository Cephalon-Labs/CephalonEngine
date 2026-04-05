using Cephalon.Abstractions.Data;

namespace Cephalon.Engine.Data;

internal sealed class InboxRegistryAdapter(
    string moduleId,
    List<InboxDescriptor> inboxes) : IInboxRegistry
{
    public void Add(InboxDescriptor inbox)
    {
        ArgumentNullException.ThrowIfNull(inbox);

        if (!string.Equals(inbox.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Inbox '{inbox.Id}' declared source module '{inbox.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        inboxes.Add(inbox);
    }
}
