namespace Cephalon.Abstractions.Data;

/// <summary>
/// Executes read-side requests against the active data implementation.
/// </summary>
public interface IReadStore
{
    /// <summary>
    /// Executes the supplied query on the read side.
    /// </summary>
    /// <typeparam name="TResult">The result type returned by the query.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes with the requested result.</returns>
    ValueTask<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}
