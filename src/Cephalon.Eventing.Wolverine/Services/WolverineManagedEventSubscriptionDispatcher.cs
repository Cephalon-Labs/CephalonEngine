using Cephalon.Eventing.Services;
using Wolverine;

namespace Cephalon.Eventing.Wolverine.Services;

internal sealed class WolverineManagedEventSubscriptionDispatcher(
    WolverineManagedEventSubscriptionExecutorCatalog executors,
    WolverineManagedEventSubscriptionExecutionProcessor processor)
{
    public int CountForChannel(string channelId) =>
        executors.GetByChannelId(channelId).Count;

    public async Task DispatchAsync(
        EventPublication publication,
        IMessageBus messageBus,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publication);
        ArgumentNullException.ThrowIfNull(messageBus);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var entry in executors.GetByChannelId(publication.ChannelId))
        {
            await processor.ProcessAsync(
                new WolverineManagedEventSubscriptionExecutionRequest(entry.Subscription.Id, publication),
                attempt: 1,
                messageBus,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
