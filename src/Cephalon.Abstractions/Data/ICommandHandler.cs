namespace Cephalon.Abstractions.Data;

/// <summary>
/// Handles a write-side request that does not return a result value.
/// </summary>
/// <typeparam name="TCommand">The command type handled by the contract.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Handles the supplied command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the command has finished running.</returns>
    ValueTask HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles a write-side request that returns a result value.
/// </summary>
/// <typeparam name="TCommand">The command type handled by the contract.</typeparam>
/// <typeparam name="TResult">The result type returned by the command.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>
    /// Handles the supplied command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes with the result produced by the command.</returns>
    ValueTask<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
