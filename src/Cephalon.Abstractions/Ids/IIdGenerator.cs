namespace Cephalon.Abstractions.Ids;

/// <summary>
/// Generates stable textual identifiers for Cephalon workloads.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Gets the stable identifier-generation strategy identifier.
    /// </summary>
    string StrategyId { get; }

    /// <summary>
    /// Generates one identifier.
    /// </summary>
    /// <param name="request">Optional generation hints supplied by the caller.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes with the generated identifier.</returns>
    ValueTask<string> GenerateAsync(
        IdGenerationRequest? request = null,
        CancellationToken cancellationToken = default);
}
