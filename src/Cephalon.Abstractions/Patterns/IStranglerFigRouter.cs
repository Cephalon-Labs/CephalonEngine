namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Resolves requests against the active strangler-fig migration routes.
/// </summary>
public interface IStranglerFigRouter
{
    /// <summary>
    /// Resolves the migration boundary that should receive the supplied request.
    /// </summary>
    /// <param name="request">The request to evaluate.</param>
    /// <param name="cancellationToken">The token that cancels the evaluation.</param>
    /// <returns>
    /// The matched route resolution when the request is covered by an active strangler-fig route;
    /// otherwise, <see langword="null" />.
    /// </returns>
    ValueTask<StranglerFigRouteResolution?> ResolveAsync(
        StranglerFigRequest request,
        CancellationToken cancellationToken = default);
}
