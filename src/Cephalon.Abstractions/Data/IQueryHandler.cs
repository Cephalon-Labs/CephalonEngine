namespace Cephalon.Abstractions.Data;

/// <summary>
/// Handles a read-side request and returns the requested result.
/// </summary>
/// <typeparam name="TQuery">The query type handled by the contract.</typeparam>
/// <typeparam name="TResult">The result type returned by the query.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Handles the supplied query.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes with the result produced by the query.</returns>
    ValueTask<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
