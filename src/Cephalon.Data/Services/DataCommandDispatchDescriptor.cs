using Cephalon.Abstractions.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Data.Services;

internal sealed class DataCommandDispatchDescriptor
{
    private readonly Func<IServiceProvider, ICommand, CancellationToken, ValueTask>? commandDispatcher;
    private readonly Func<IServiceProvider, ICommand, CancellationToken, ValueTask<object?>>? resultCommandDispatcher;

    private DataCommandDispatchDescriptor(
        Type commandType,
        Type? resultType,
        Func<IServiceProvider, ICommand, CancellationToken, ValueTask>? commandDispatcher,
        Func<IServiceProvider, ICommand, CancellationToken, ValueTask<object?>>? resultCommandDispatcher)
    {
        CommandType = commandType;
        ResultType = resultType;
        this.commandDispatcher = commandDispatcher;
        this.resultCommandDispatcher = resultCommandDispatcher;
    }

    public Type CommandType { get; }

    public Type? ResultType { get; }

    public bool ReturnsResult => ResultType is not null;

    public static DataCommandDispatchDescriptor ForCommand<TCommand>()
        where TCommand : ICommand
    {
        return new DataCommandDispatchDescriptor(
            typeof(TCommand),
            resultType: null,
            static (services, command, cancellationToken) => DispatchCommandAsync<TCommand>(
                services,
                command,
                cancellationToken),
            resultCommandDispatcher: null);
    }

    public static DataCommandDispatchDescriptor ForResultCommand<TCommand, TResult>()
        where TCommand : ICommand<TResult>
    {
        return new DataCommandDispatchDescriptor(
            typeof(TCommand),
            typeof(TResult),
            commandDispatcher: null,
            static async (services, command, cancellationToken) =>
            {
                var handler = services.GetService<ICommandHandler<TCommand, TResult>>();
                if (handler is null)
                {
                    throw DataDispatchExceptions.MissingResultCommandHandler<TCommand, TResult>();
                }

                return await handler.HandleAsync((TCommand)command, cancellationToken).ConfigureAwait(false);
            });
    }

    public ValueTask DispatchAsync(
        IServiceProvider services,
        ICommand command,
        CancellationToken cancellationToken)
    {
        if (commandDispatcher is null)
        {
            throw new InvalidOperationException(
                $"The dispatch descriptor for '{CommandType.FullName}' is registered for a result-returning command. Use ExecuteAsync<TResult>(ICommand<TResult>, ...) for this command.");
        }

        return commandDispatcher(services, command, cancellationToken);
    }

    public ValueTask<object?> DispatchResultAsync(
        IServiceProvider services,
        ICommand command,
        CancellationToken cancellationToken)
    {
        if (resultCommandDispatcher is null)
        {
            throw new InvalidOperationException(
                $"The dispatch descriptor for '{CommandType.FullName}' is registered for a command without a result. Use ExecuteAsync(ICommand, ...) for this command.");
        }

        return resultCommandDispatcher(services, command, cancellationToken);
    }

    private static ValueTask DispatchCommandAsync<TCommand>(
        IServiceProvider services,
        ICommand command,
        CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        var handler = services.GetService<ICommandHandler<TCommand>>();
        if (handler is null)
        {
            throw DataDispatchExceptions.MissingCommandHandler<TCommand>();
        }

        return handler.HandleAsync((TCommand)command, cancellationToken);
    }
}
