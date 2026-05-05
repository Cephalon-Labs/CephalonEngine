namespace Cephalon.EventSourcing.Services;

internal sealed class EventTypeRegistrationContributor(EventTypeDescriptor descriptor) : IEventTypeContributor
{
    public IReadOnlyList<EventTypeDescriptor> Contribute() => [descriptor];
}
