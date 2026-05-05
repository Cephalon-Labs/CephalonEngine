using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class HandlerDispatchingReadStore(
    IServiceProvider services,
    DataDispatchRegistry dispatchRegistry) : IReadStore
{
    public ValueTask<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var dispatcher = dispatchRegistry.GetQuery(query.GetType(), typeof(TResult));
        return DispatchAsync(dispatcher, query, cancellationToken);
    }

    private async ValueTask<TResult> DispatchAsync<TResult>(
        DataQueryDispatchDescriptor dispatcher,
        IQuery<TResult> query,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.DispatchAsync(services, query, cancellationToken).ConfigureAwait(false);
        return result is null ? default! : (TResult)result;
    }
}
