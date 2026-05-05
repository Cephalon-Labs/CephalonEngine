using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal static class DataDispatchExceptions
{
    public static InvalidOperationException MissingCommandDescriptor(Type commandType)
    {
        ArgumentNullException.ThrowIfNull(commandType);

        return new InvalidOperationException(
            $"No Cephalon.Data command dispatch descriptor is registered for '{commandType.FullName}'. Register the command with AddCephalonDataCommand<{commandType.Name}>() when configuring services.");
    }

    public static InvalidOperationException MissingResultCommandDescriptor(Type commandType, Type resultType)
    {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentNullException.ThrowIfNull(resultType);

        return new InvalidOperationException(
            $"No Cephalon.Data result-command dispatch descriptor is registered for '{commandType.FullName}' returning '{resultType.FullName}'. Register the command with AddCephalonDataCommand<{commandType.Name}, {resultType.Name}>() when configuring services.");
    }

    public static InvalidOperationException MissingQueryDescriptor(Type queryType, Type resultType)
    {
        ArgumentNullException.ThrowIfNull(queryType);
        ArgumentNullException.ThrowIfNull(resultType);

        return new InvalidOperationException(
            $"No Cephalon.Data query dispatch descriptor is registered for '{queryType.FullName}' returning '{resultType.FullName}'. Register the query with AddCephalonDataQuery<{queryType.Name}, {resultType.Name}>() when configuring services.");
    }

    public static InvalidOperationException MissingCommandHandler<TCommand>()
        where TCommand : ICommand
    {
        return new InvalidOperationException(
            $"No {typeof(ICommandHandler<TCommand>).FullName} service is registered for command '{typeof(TCommand).FullName}'. Register the handler service before dispatching the command.");
    }

    public static InvalidOperationException MissingResultCommandHandler<TCommand, TResult>()
        where TCommand : ICommand<TResult>
    {
        return new InvalidOperationException(
            $"No {typeof(ICommandHandler<TCommand, TResult>).FullName} service is registered for command '{typeof(TCommand).FullName}' returning '{typeof(TResult).FullName}'. Register the handler service before dispatching the command.");
    }

    public static InvalidOperationException MissingQueryHandler<TQuery, TResult>()
        where TQuery : IQuery<TResult>
    {
        return new InvalidOperationException(
            $"No {typeof(IQueryHandler<TQuery, TResult>).FullName} service is registered for query '{typeof(TQuery).FullName}' returning '{typeof(TResult).FullName}'. Register the handler service before dispatching the query.");
    }

    public static InvalidOperationException DuplicateCommandDescriptor(Type commandType)
    {
        ArgumentNullException.ThrowIfNull(commandType);

        return new InvalidOperationException(
            $"Multiple Cephalon.Data command dispatch descriptors were registered for '{commandType.FullName}'. Register each command shape once.");
    }

    public static InvalidOperationException DuplicateResultCommandDescriptor(Type commandType, Type resultType)
    {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentNullException.ThrowIfNull(resultType);

        return new InvalidOperationException(
            $"Multiple Cephalon.Data result-command dispatch descriptors were registered for '{commandType.FullName}' returning '{resultType.FullName}'. Register each command/result shape once.");
    }

    public static InvalidOperationException DuplicateQueryDescriptor(Type queryType, Type resultType)
    {
        ArgumentNullException.ThrowIfNull(queryType);
        ArgumentNullException.ThrowIfNull(resultType);

        return new InvalidOperationException(
            $"Multiple Cephalon.Data query dispatch descriptors were registered for '{queryType.FullName}' returning '{resultType.FullName}'. Register each query/result shape once.");
    }
}
