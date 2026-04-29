namespace Cephalon.Eventing.Services;

internal interface IEventPublicationRuntimeReporter
{
    ValueTask ReportAsync(EventPublicationRuntimeReport report, CancellationToken cancellationToken = default);
}
