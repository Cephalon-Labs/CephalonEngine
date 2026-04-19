using System.Globalization;
using Cephalon.Behaviors.Patterns.Abstractions;

namespace Cephalon.Behaviors.Patterns.Runtime;

/// <summary>
/// Describes one reported live publication observation for an active saga-choreography behavior.
/// </summary>
internal sealed class SagaChoreographyPublicationExecutionReport
{
    public SagaChoreographyPublicationExecutionReport(
        string behaviorId,
        SagaChoreographyPublication publication,
        string outcome,
        DateTimeOffset observedAtUtc,
        string? publisherType = null,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            throw new ArgumentException("Behavior id is required.", nameof(behaviorId));
        }

        ArgumentNullException.ThrowIfNull(publication);

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        BehaviorId = behaviorId.Trim();
        Id = CreateRuntimeStateId(BehaviorId, publication);
        PublicationId = publication.Id;
        ChannelId = publication.ChannelId;
        EventType = publication.EventType;
        OccurredAtUtc = publication.OccurredAtUtc;
        CorrelationId = publication.CorrelationId;
        TenantId = publication.TenantId;
        ContentType = publication.ContentType;
        IsCompensation = publication.IsCompensation;
        Outcome = outcome.Trim();
        ObservedAtUtc = observedAtUtc;
        PublisherType = string.IsNullOrWhiteSpace(publisherType) ? null : publisherType.Trim();
        Error = string.IsNullOrWhiteSpace(error) ? null : error.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    public string Id { get; }

    public string BehaviorId { get; }

    public string PublicationId { get; }

    public string ChannelId { get; }

    public string EventType { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public string? CorrelationId { get; }

    public string? TenantId { get; }

    public string? ContentType { get; }

    public bool IsCompensation { get; }

    public string Outcome { get; }

    public DateTimeOffset ObservedAtUtc { get; }

    public string? PublisherType { get; }

    public string? Error { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string CreateRuntimeStateId(string behaviorId, SagaChoreographyPublication publication)
    {
        var correlationSegment = string.IsNullOrWhiteSpace(publication.CorrelationId)
            ? "uncorrelated"
            : publication.CorrelationId.Trim();

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{behaviorId}:{publication.ChannelId}:{publication.Id}:{publication.OccurredAtUtc.UtcTicks}:{correlationSegment}");
    }
}
