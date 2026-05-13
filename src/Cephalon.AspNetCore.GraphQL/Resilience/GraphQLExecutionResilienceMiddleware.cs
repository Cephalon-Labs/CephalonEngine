using System.Data.Common;
using System.Net;
using System.Net.Sockets;
using HotChocolate;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;

namespace Cephalon.AspNetCore.GraphQL.Resilience;

internal static class GraphQLExecutionResilienceMiddleware
{
    private const string BrokenCircuitExceptionTypeName = "Polly.CircuitBreaker.BrokenCircuitException";
    private const string TimeoutRejectedExceptionTypeName = "Polly.Timeout.TimeoutRejectedException";

    public static FieldDelegate Create(FieldDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return async context =>
        {
            if (!ShouldEnforce(context))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            var options = context.Service<GraphQLExecutionResilienceOptions>();
            if (!options.HasEnforcedStrategies)
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            var stateRegistry = context.Service<GraphQLExecutionResilienceStateRegistry>();
            var circuitBreaker = stateRegistry.CircuitBreaker;
            if (!TryEnterCircuit(options, circuitBreaker, out var retryAfterSeconds))
            {
                context.Result = CreateCircuitBreakerError(context, retryAfterSeconds);
                return;
            }

            GraphQLExecutionBulkheadState.Lease bulkheadLease = default;
            if (options.BulkheadEnabled)
            {
                var lease = await stateRegistry.Bulkhead.TryEnterAsync(context.RequestAborted).ConfigureAwait(false);
                if (lease is null)
                {
                    context.Result = CreateBulkheadRejectedError(context);
                    return;
                }

                bulkheadLease = lease.Value;
            }

            using (bulkheadLease)
            {
                await ExecuteAfterAdmissionAsync(context, next, options, circuitBreaker).ConfigureAwait(false);
            }
        };
    }

    private static async ValueTask ExecuteAfterAdmissionAsync(
        IMiddlewareContext context,
        FieldDelegate next,
        GraphQLExecutionResilienceOptions options,
        GraphQLExecutionCircuitBreakerState circuitBreaker)
    {
        try
        {
            await ExecuteWithTimeoutAsync(context, next, options).ConfigureAwait(false);
            circuitBreaker.RecordSuccess();
        }
        catch (Exception exception) when (IsPollyTimeoutRejectedException(exception))
        {
            circuitBreaker.RecordFailure(exception);
            context.Result = CreateTimeoutError(context);
        }
        catch (TimeoutException exception)
        {
            circuitBreaker.RecordFailure(exception);
            context.Result = CreateTimeoutError(context);
        }
        catch (Exception exception) when (IsPollyBrokenCircuitException(exception))
        {
            circuitBreaker.RecordFailure(exception);
            context.Result = CreateCircuitBreakerError(context, circuitBreaker.RetryAfterSeconds);
        }
        catch (Exception exception) when (ShouldTripCircuit(exception))
        {
            circuitBreaker.RecordFailure(exception);
            throw;
        }
    }

    private static async ValueTask ExecuteWithTimeoutAsync(
        IMiddlewareContext context,
        FieldDelegate next,
        GraphQLExecutionResilienceOptions options)
    {
        if (!options.TimeoutEnabled || options.Timeout is not { } timeout)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var task = next(context).AsTask();
        try
        {
            await task.WaitAsync(timeout, context.RequestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (!context.RequestAborted.IsCancellationRequested)
        {
            throw new TimeoutException("The GraphQL resolver exceeded the configured Cephalon execution timeout.", exception);
        }
        catch (TimeoutException)
        {
            ObserveFaults(task);
            throw;
        }
    }

    private static bool TryEnterCircuit(
        GraphQLExecutionResilienceOptions options,
        GraphQLExecutionCircuitBreakerState circuitBreaker,
        out int retryAfterSeconds)
    {
        if (!options.CircuitBreakerEnabled)
        {
            retryAfterSeconds = 0;
            return true;
        }

        return circuitBreaker.TryEnter(out retryAfterSeconds);
    }

    private static bool ShouldEnforce(IMiddlewareContext context)
    {
        var typeName = context.Selection.DeclaringType.Name;
        if (!string.Equals(typeName, "Query", StringComparison.Ordinal) &&
            !string.Equals(typeName, "Mutation", StringComparison.Ordinal) &&
            !string.Equals(typeName, "Subscription", StringComparison.Ordinal))
        {
            return false;
        }

        return !string.Equals(context.Selection.Field.Name, "_service", StringComparison.Ordinal);
    }

    private static IError CreateTimeoutError(IMiddlewareContext context)
        => CreateError(
            context,
            message: "The GraphQL resolver exceeded the configured Cephalon execution timeout.",
            code: "graphql_execution_timeout",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            retryAfterSeconds: null);

    private static IError CreateCircuitBreakerError(IMiddlewareContext context, int retryAfterSeconds)
        => CreateError(
            context,
            message: "The GraphQL resolver was rejected because the configured Cephalon circuit breaker is open.",
            code: "graphql_circuit_breaker_open",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            retryAfterSeconds: Math.Max(1, retryAfterSeconds));

    private static IError CreateBulkheadRejectedError(IMiddlewareContext context)
        => CreateError(
            context,
            message: "The GraphQL resolver exceeded the configured Cephalon concurrency limit.",
            code: "graphql_bulkhead_rejected",
            statusCode: StatusCodes.Status429TooManyRequests,
            retryAfterSeconds: null);

    private static IError CreateError(
        IMiddlewareContext context,
        string message,
        string code,
        int statusCode,
        int? retryAfterSeconds)
    {
        var builder = ErrorBuilder.New()
            .SetMessage(message)
            .SetCode(code)
            .SetPath(context.Path)
            .SetExtension("fault", "resilience")
            .SetExtension("statusCode", statusCode);

        if (retryAfterSeconds.HasValue)
        {
            builder.SetExtension("retryAfterSeconds", retryAfterSeconds.Value);
        }

        return builder.Build();
    }

    private static bool IsPollyTimeoutRejectedException(Exception exception)
        => IsExceptionType(exception, TimeoutRejectedExceptionTypeName);

    private static bool IsPollyBrokenCircuitException(Exception exception)
        => IsExceptionType(exception, BrokenCircuitExceptionTypeName);

    private static bool ShouldTripCircuit(Exception exception)
        => exception is TimeoutException ||
            exception is HttpRequestException ||
            exception is IOException ||
            exception is SocketException ||
            exception is WebException ||
            exception is DbException;

    private static bool IsExceptionType(Exception exception, string fullName)
    {
        for (var type = exception.GetType(); type is not null; type = type.BaseType)
        {
            if (string.Equals(type.FullName, fullName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void ObserveFaults(Task task)
    {
        if (task.IsCompleted)
        {
            _ = task.Exception;
            return;
        }

        _ = task.ContinueWith(
            static completedTask => _ = completedTask.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
