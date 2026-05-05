using Cephalon.Abstractions.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Data.Services;

internal sealed class DataQueryDispatchDescriptor
{
    private readonly Func<IServiceProvider, object, CancellationToken, ValueTask<object?>> queryDispatcher;

    private DataQueryDispatchDescriptor(
        Type queryType,
        Type resultType,
        Func<IServiceProvider, object, CancellationToken, ValueTask<object?>> queryDispatcher)
    {
        QueryType = queryType;
        ResultType = resultType;
        this.queryDispatcher = queryDispatcher;
    }

    public Type QueryType { get; }

    public Type ResultType { get; }

    public static DataQueryDispatchDescriptor ForQuery<TQuery, TResult>()
        where TQuery : IQuery<TResult>
    {
        return new DataQueryDispatchDescriptor(
            typeof(TQuery),
            typeof(TResult),
            static async (services, query, cancellationToken) =>
            {
                var handler = services.GetService<IQueryHandler<TQuery, TResult>>();
                if (handler is null)
                {
                    throw DataDispatchExceptions.MissingQueryHandler<TQuery, TResult>();
                }

                return await handler.HandleAsync((TQuery)query, cancellationToken).ConfigureAwait(false);
            });
    }

    public ValueTask<object?> DispatchAsync(
        IServiceProvider services,
        object query,
        CancellationToken cancellationToken)
    {
        return queryDispatcher(services, query, cancellationToken);
    }
}
