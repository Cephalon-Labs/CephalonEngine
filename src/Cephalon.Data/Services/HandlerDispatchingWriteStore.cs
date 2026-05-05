using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class HandlerDispatchingWriteStore(
    IServiceProvider services,
    DataDispatchRegistry dispatchRegistry) : IWriteStore
{
    public ValueTask ExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dispatcher = dispatchRegistry.GetCommand(command.GetType());
        return dispatcher.DispatchAsync(services, command, cancellationToken);
    }

    public ValueTask<TResult> ExecuteAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dispatcher = dispatchRegistry.GetResultCommand(command.GetType(), typeof(TResult));
        return DispatchResultAsync<TResult>(dispatcher, command, cancellationToken);
    }

    private async ValueTask<TResult> DispatchResultAsync<TResult>(
        DataCommandDispatchDescriptor dispatcher,
        ICommand<TResult> command,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.DispatchResultAsync(services, command, cancellationToken).ConfigureAwait(false);
        return result is null ? default! : (TResult)result;
    }
}
