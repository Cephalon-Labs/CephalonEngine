namespace Cephalon.AspNetCore.Hosting;

internal sealed class EventDispatchRemediationHttpRequest
{
    public string? CommandId { get; init; }

    public string? MessageId { get; init; }

    public string? ChannelId { get; init; }

    public DateTimeOffset? NextAttemptAtUtc { get; init; }

    public string? Reason { get; init; }

    public string? ActorId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
