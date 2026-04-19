using Cephalon.Behaviors.Patterns.Abstractions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Behaviors.Patterns.Strategies;

/// <summary>
/// Executes behaviors that follow the choreography-based saga pattern.
/// Publications returned by the behavior are staged through <see cref="ISagaChoreographyPublisher"/>
/// so other services or modules can continue the workflow by reacting to events.
/// </summary>
public sealed class ChoreographySagaExecutionStrategy : IBehaviorExecutionStrategy
{
    private static readonly Action<ILogger, string, string, Exception?> LogPublicationAccepted =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(1, nameof(ChoreographySagaExecutionStrategy)),
            "ChoreographySagaStrategy: accepted publication {PublicationId} for behavior {BehaviorId}.");

    private readonly ISagaChoreographyPublisher _publisher;
    private readonly ILogger<ChoreographySagaExecutionStrategy> _logger;

    /// <summary>Initializes a new instance of <see cref="ChoreographySagaExecutionStrategy"/>.</summary>
    /// <param name="publisher">The publisher used to stage choreography publications.</param>
    /// <param name="logger">The logger used to report accepted publications.</param>
    public ChoreographySagaExecutionStrategy(
        ISagaChoreographyPublisher publisher,
        ILogger<ChoreographySagaExecutionStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(logger);
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>Gets the pattern identifier handled by this strategy.</summary>
    public string Pattern => "saga-choreography";

    /// <summary>
    /// Invokes the behavior and stages any returned publications through the choreography publisher.
    /// Behaviors may return a single <see cref="SagaChoreographyPublication"/>, a sequence of them,
    /// or any <see cref="ISagaChoreographyStepResult"/> when they need both local output and
    /// publications in the same step.
    /// </summary>
    /// <param name="context">The execution context for this invocation.</param>
    /// <param name="ct">A token that cancels the execution.</param>
    /// <returns>
    /// A result with HTTP 202 when at least one publication is staged; otherwise HTTP 200 or 204
    /// based on whether the behavior produced a local output.
    /// </returns>
    public async Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var rawOutput = await context.Slot
            .InvokeAsync(context.BehaviorInstance, context.Input, context.BehaviorContext, ct)
            .ConfigureAwait(false);

        var (output, publications) = NormalizeResult(rawOutput);
        if (publications.Length > 0)
        {
            foreach (var publication in publications)
            {
                var normalizedPublication = NormalizePublication(publication, context.BehaviorContext);
                await _publisher.PublishAsync(normalizedPublication, ct).ConfigureAwait(false);
                LogPublicationAccepted(_logger, normalizedPublication.Id, context.Descriptor.Id, null);
            }
        }

        return new BehaviorExecutionResult
        {
            Output = output,
            HttpStatusCode = publications.Length > 0
                ? 202
                : output is null ? 204 : 200,
            IsFireAndForget = false
        };
    }

    private static (object? Output, SagaChoreographyPublication[] Publications) NormalizeResult(object? output)
    {
        return output switch
        {
            ISagaChoreographyStepResult stepResult => (
                stepResult.Output,
                stepResult.Publications.Count == 0
                    ? []
                    : stepResult.Publications.ToArray()),
            SagaChoreographyPublication publication => (publication, [publication]),
            IEnumerable<SagaChoreographyPublication> publications => NormalizePublicationSequence(publications),
            _ => (output, [])
        };
    }

    private static (object? Output, SagaChoreographyPublication[] Publications) NormalizePublicationSequence(
        IEnumerable<SagaChoreographyPublication> publications)
    {
        ArgumentNullException.ThrowIfNull(publications);

        var materialized = publications.ToArray();
        return (materialized, materialized);
    }

    private static SagaChoreographyPublication NormalizePublication(
        SagaChoreographyPublication publication,
        Cephalon.Abstractions.Behaviors.IBehaviorContext behaviorContext)
    {
        ArgumentNullException.ThrowIfNull(publication);
        ArgumentNullException.ThrowIfNull(behaviorContext);

        var correlationId = !string.IsNullOrWhiteSpace(publication.CorrelationId)
            ? publication.CorrelationId
            : behaviorContext.CorrelationId;
        var tenantId = !string.IsNullOrWhiteSpace(publication.TenantId)
            ? publication.TenantId
            : ResolveMetadataValue(behaviorContext.Metadata, "TenantId", "tenantId", "tenant-id");

        if (string.Equals(correlationId, publication.CorrelationId, StringComparison.Ordinal) &&
            string.Equals(tenantId, publication.TenantId, StringComparison.Ordinal))
        {
            return publication;
        }

        return new SagaChoreographyPublication(
            publication.Id,
            publication.ChannelId,
            publication.EventType,
            publication.Payload,
            publication.OccurredAtUtc,
            contentType: publication.ContentType,
            correlationId: correlationId,
            tenantId: tenantId,
            isCompensation: publication.IsCompensation,
            headers: publication.Headers,
            metadata: publication.Metadata);
    }

    private static string? ResolveMetadataValue(
        IReadOnlyDictionary<string, string> metadata,
        params string[] keys)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var key in keys)
        {
            if (metadata.TryGetValue(key, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
