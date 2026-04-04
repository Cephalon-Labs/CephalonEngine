using System.Collections.Concurrent;
using System.Reflection;
using Cephalon.Abstractions.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Data.Services;

internal sealed class HandlerDispatchingWriteStore(IServiceProvider services) : IWriteStore
{
    private static readonly MethodInfo DispatchCommandMethod = typeof(HandlerDispatchingWriteStore)
        .GetMethod(nameof(DispatchCommandAsync), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Could not find command dispatch method.");
    private static readonly MethodInfo DispatchResultCommandMethod = typeof(HandlerDispatchingWriteStore)
        .GetMethod(nameof(DispatchResultCommandAsync), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Could not find result command dispatch method.");

    public ValueTask ExecuteAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dispatcher = CommandDispatcherCache.Dispatchers.GetOrAdd(
            command.GetType(),
            static commandType => CreateCommandDispatcher(commandType));

        return dispatcher(services, command, cancellationToken);
    }

    public ValueTask<TResult> ExecuteAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dispatcher = ResultCommandDispatcherCache<TResult>.Dispatchers.GetOrAdd(
            command.GetType(),
            static commandType => CreateResultCommandDispatcher<TResult>(commandType));

        return dispatcher(services, command, cancellationToken);
    }

    private static Func<IServiceProvider, ICommand, CancellationToken, ValueTask> CreateCommandDispatcher(Type commandType)
    {
        var closedMethod = DispatchCommandMethod.MakeGenericMethod(commandType);
        return closedMethod.CreateDelegate<Func<IServiceProvider, ICommand, CancellationToken, ValueTask>>();
    }

    private static Func<IServiceProvider, ICommand<TResult>, CancellationToken, ValueTask<TResult>> CreateResultCommandDispatcher<TResult>(Type commandType)
    {
        var closedMethod = DispatchResultCommandMethod.MakeGenericMethod(commandType, typeof(TResult));
        return closedMethod.CreateDelegate<Func<IServiceProvider, ICommand<TResult>, CancellationToken, ValueTask<TResult>>>();
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
            throw new InvalidOperationException(
                $"No command handler was registered for '{typeof(TCommand).FullName}'. Register '{typeof(ICommandHandler<TCommand>).FullName}'.");
        }

        return handler.HandleAsync((TCommand)command, cancellationToken);
    }

    private static ValueTask<TResult> DispatchResultCommandAsync<TCommand, TResult>(
        IServiceProvider services,
        ICommand<TResult> command,
        CancellationToken cancellationToken)
        where TCommand : ICommand<TResult>
    {
        var handler = services.GetService<ICommandHandler<TCommand, TResult>>();
        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No result command handler was registered for '{typeof(TCommand).FullName}'. Register '{typeof(ICommandHandler<TCommand, TResult>).FullName}'.");
        }

        return handler.HandleAsync((TCommand)command, cancellationToken);
    }

    private static class CommandDispatcherCache
    {
        internal static readonly ConcurrentDictionary<Type, Func<IServiceProvider, ICommand, CancellationToken, ValueTask>> Dispatchers = new();
    }

    private static class ResultCommandDispatcherCache<TResult>
    {
        internal static readonly ConcurrentDictionary<Type, Func<IServiceProvider, ICommand<TResult>, CancellationToken, ValueTask<TResult>>> Dispatchers = new();
    }
}
