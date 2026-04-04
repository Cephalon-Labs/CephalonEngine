using System.Collections.Concurrent;
using System.Reflection;
using Cephalon.Abstractions.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Data.Services;

internal sealed class HandlerDispatchingReadStore(IServiceProvider services) : IReadStore
{
    private static readonly MethodInfo DispatchQueryMethod = typeof(HandlerDispatchingReadStore)
        .GetMethod(nameof(DispatchQueryAsync), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Could not find query dispatch method.");

    public ValueTask<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var dispatcher = QueryDispatcherCache<TResult>.Dispatchers.GetOrAdd(
            query.GetType(),
            static queryType => CreateDispatcher<TResult>(queryType));

        return dispatcher(services, query, cancellationToken);
    }

    private static Func<IServiceProvider, IQuery<TResult>, CancellationToken, ValueTask<TResult>> CreateDispatcher<TResult>(Type queryType)
    {
        var closedMethod = DispatchQueryMethod.MakeGenericMethod(queryType, typeof(TResult));
        return closedMethod.CreateDelegate<Func<IServiceProvider, IQuery<TResult>, CancellationToken, ValueTask<TResult>>>();
    }

    private static ValueTask<TResult> DispatchQueryAsync<TQuery, TResult>(
        IServiceProvider services,
        IQuery<TResult> query,
        CancellationToken cancellationToken)
        where TQuery : IQuery<TResult>
    {
        var handler = services.GetService<IQueryHandler<TQuery, TResult>>();
        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No query handler was registered for '{typeof(TQuery).FullName}'. Register '{typeof(IQueryHandler<TQuery, TResult>).FullName}'.");
        }

        return handler.HandleAsync((TQuery)query, cancellationToken);
    }

    private static class QueryDispatcherCache<TResult>
    {
        internal static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IQuery<TResult>, CancellationToken, ValueTask<TResult>>> Dispatchers = new();
    }
}
