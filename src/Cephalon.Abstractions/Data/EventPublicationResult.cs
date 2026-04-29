namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the operator-facing result of one managed event-publication request.
/// </summary>
/// <param name="PublicationId">The stable publication identifier.</param>
/// <param name="ChannelId">The logical channel or destination identifier.</param>
/// <param name="EventType">The logical event type identifier.</param>
/// <param name="Outcome">The stable publication outcome identifier.</param>
/// <param name="AcceptedAtUtc">The UTC timestamp when the active runtime accepted the publication.</param>
/// <param name="Error">The operator-facing error summary when publication failed.</param>
/// <param name="Metadata">Optional operator-facing metadata captured with the result.</param>
public sealed record EventPublicationResult(
    string PublicationId,
    string ChannelId,
    string EventType,
    string Outcome,
    DateTimeOffset AcceptedAtUtc,
    string? Error,
    IReadOnlyDictionary<string, string> Metadata);
