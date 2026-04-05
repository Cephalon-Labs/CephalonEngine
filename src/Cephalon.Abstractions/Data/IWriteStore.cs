namespace Cephalon.Abstractions.Data;

/// <summary>
/// Executes write-side requests against the active data implementation.
/// </summary>
public interface IWriteStore
{
    /// <summary>
    /// Executes the supplied command on the write side.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the command has finished running.</returns>
    ValueTask ExecuteAsync(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the supplied command on the write side and returns the resulting value.
    /// </summary>
    /// <typeparam name="TResult">The result type returned by the command.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes with the result produced by the command.</returns>
    ValueTask<TResult> ExecuteAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}
