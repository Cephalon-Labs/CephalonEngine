using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Cephalon.Eventing.Wolverine.Services;

/// <summary>
/// Infrastructure retry handler used by the Wolverine eventing pack for managed subscription executions.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WolverineManagedEventSubscriptionExecutionHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WolverineManagedEventSubscriptionExecutionHandler" /> class.
    /// </summary>
    public WolverineManagedEventSubscriptionExecutionHandler()
    {
    }

    /// <summary>
    /// Replays one managed subscription execution attempt from Wolverine's scheduled-message pipeline.
    /// </summary>
    /// <param name="request">The infrastructure retry message describing the managed subscription attempt.</param>
    /// <param name="envelope">The Wolverine envelope that carries retry-attempt metadata.</param>
    /// <param name="services">The current service provider scope.</param>
    /// <param name="messageBus">The active Wolverine message bus.</param>
    /// <param name="cancellationToken">The cancellation token for the current retry attempt.</param>
    /// <returns>A task that completes when the managed retry attempt finishes.</returns>
    public static Task Handle(
        WolverineManagedEventSubscriptionExecutionRequest request,
        Envelope envelope,
        IServiceProvider services,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var processor = services.GetRequiredService<WolverineManagedEventSubscriptionExecutionProcessor>();
        return processor.ProcessAsync(
            request,
            Math.Max(request.Attempt, envelope.Attempts),
            messageBus,
            cancellationToken);
    }
}
