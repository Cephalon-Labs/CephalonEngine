using Cephalon.Abstractions.Data;
using Cephalon.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Data.Registration;

/// <summary>
/// Registers trim-friendly Cephalon.Data command and query dispatch descriptors.
/// </summary>
public static class DataDispatchServiceCollectionExtensions
{
    /// <summary>
    /// Adds a dispatch descriptor for a command handled by <see cref="ICommandHandler{TCommand}" />.
    /// </summary>
    /// <typeparam name="TCommand">The command type that should be dispatchable through <see cref="IWriteStore" />.</typeparam>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonDataCommand<TCommand>(this IServiceCollection services)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<DataDispatchRegistry>();
        services.AddSingleton(DataCommandDispatchDescriptor.ForCommand<TCommand>());
        return services;
    }

    /// <summary>
    /// Adds a dispatch descriptor for a result-returning command handled by <see cref="ICommandHandler{TCommand, TResult}" />.
    /// </summary>
    /// <typeparam name="TCommand">The command type that should be dispatchable through <see cref="IWriteStore" />.</typeparam>
    /// <typeparam name="TResult">The result type returned by the command handler.</typeparam>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonDataCommand<TCommand, TResult>(this IServiceCollection services)
        where TCommand : ICommand<TResult>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<DataDispatchRegistry>();
        services.AddSingleton(DataCommandDispatchDescriptor.ForResultCommand<TCommand, TResult>());
        return services;
    }

    /// <summary>
    /// Adds a dispatch descriptor for a query handled by <see cref="IQueryHandler{TQuery, TResult}" />.
    /// </summary>
    /// <typeparam name="TQuery">The query type that should be dispatchable through <see cref="IReadStore" />.</typeparam>
    /// <typeparam name="TResult">The result type returned by the query handler.</typeparam>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonDataQuery<TQuery, TResult>(this IServiceCollection services)
        where TQuery : IQuery<TResult>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<DataDispatchRegistry>();
        services.AddSingleton(DataQueryDispatchDescriptor.ForQuery<TQuery, TResult>());
        return services;
    }
}
