namespace Cephalon.Data.Services;

internal sealed class DataDispatchRegistry
{
    private readonly Dictionary<Type, DataCommandDispatchDescriptor> commands;
    private readonly Dictionary<(Type CommandType, Type ResultType), DataCommandDispatchDescriptor> resultCommands;
    private readonly Dictionary<(Type QueryType, Type ResultType), DataQueryDispatchDescriptor> queries;

    public DataDispatchRegistry(
        IEnumerable<DataCommandDispatchDescriptor> commandDescriptors,
        IEnumerable<DataQueryDispatchDescriptor> queryDescriptors)
    {
        ArgumentNullException.ThrowIfNull(commandDescriptors);
        ArgumentNullException.ThrowIfNull(queryDescriptors);

        var commandMap = new Dictionary<Type, DataCommandDispatchDescriptor>();
        var resultCommandMap = new Dictionary<(Type CommandType, Type ResultType), DataCommandDispatchDescriptor>();
        foreach (var descriptor in commandDescriptors)
        {
            if (descriptor.ReturnsResult)
            {
                var key = (CommandType: descriptor.CommandType, ResultType: descriptor.ResultType!);
                if (!resultCommandMap.TryAdd(key, descriptor))
                {
                    throw DataDispatchExceptions.DuplicateResultCommandDescriptor(key.CommandType, key.ResultType);
                }

                continue;
            }

            if (!commandMap.TryAdd(descriptor.CommandType, descriptor))
            {
                throw DataDispatchExceptions.DuplicateCommandDescriptor(descriptor.CommandType);
            }
        }

        var queryMap = new Dictionary<(Type QueryType, Type ResultType), DataQueryDispatchDescriptor>();
        foreach (var descriptor in queryDescriptors)
        {
            var key = (descriptor.QueryType, descriptor.ResultType);
            if (!queryMap.TryAdd(key, descriptor))
            {
                throw DataDispatchExceptions.DuplicateQueryDescriptor(key.QueryType, key.ResultType);
            }
        }

        commands = commandMap;
        resultCommands = resultCommandMap;
        queries = queryMap;
    }

    public DataCommandDispatchDescriptor GetCommand(Type commandType)
    {
        ArgumentNullException.ThrowIfNull(commandType);

        if (!commands.TryGetValue(commandType, out var descriptor))
        {
            throw DataDispatchExceptions.MissingCommandDescriptor(commandType);
        }

        return descriptor;
    }

    public DataCommandDispatchDescriptor GetResultCommand(Type commandType, Type resultType)
    {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentNullException.ThrowIfNull(resultType);

        if (!resultCommands.TryGetValue((commandType, resultType), out var descriptor))
        {
            throw DataDispatchExceptions.MissingResultCommandDescriptor(commandType, resultType);
        }

        return descriptor;
    }

    public DataQueryDispatchDescriptor GetQuery(Type queryType, Type resultType)
    {
        ArgumentNullException.ThrowIfNull(queryType);
        ArgumentNullException.ThrowIfNull(resultType);

        if (!queries.TryGetValue((queryType, resultType), out var descriptor))
        {
            throw DataDispatchExceptions.MissingQueryDescriptor(queryType, resultType);
        }

        return descriptor;
    }
}
