using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Cephalon.AspNetCore.Grpc.Hosting;

internal sealed class CephalonGrpcResilienceInterceptor(
    CephalonGrpcDirectModuleResilienceOptions options,
    CephalonGrpcDirectModuleCircuitBreakerState circuitBreakerState,
    CephalonGrpcDirectModuleBulkheadState bulkheadState) : Interceptor
{
    private const string BrokenCircuitExceptionTypeName = "Polly.CircuitBreaker.BrokenCircuitException";
    private const string TimeoutRejectedExceptionTypeName = "Polly.Timeout.TimeoutRejectedException";
    private const string TimeoutCode = "grpc_execution_timeout";
    private const string CircuitBreakerOpenCode = "grpc_circuit_breaker_open";
    private const string BulkheadRejectedCode = "grpc_bulkhead_rejected";

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(continuation);

        return ExecuteAsync(() => continuation(request, context), context);
    }

    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(continuation);

        return ExecuteAsync(() => continuation(requestStream, context), context);
    }

    public override Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(continuation);

        return ExecuteAsync(() => continuation(request, responseStream, context), context);
    }

    public override Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(continuation);

        return ExecuteAsync(() => continuation(requestStream, responseStream, context), context);
    }

    private async Task<TResponse> ExecuteAsync<TResponse>(
        Func<Task<TResponse>> execute,
        ServerCallContext context)
    {
        if (!TryEnterCircuit())
        {
            throw CreateCircuitBreakerException();
        }

        CephalonGrpcDirectModuleBulkheadState.Lease bulkheadLease = default;
        if (options.BulkheadEnabled)
        {
            var lease = await bulkheadState.TryEnterAsync(context.CancellationToken).ConfigureAwait(false);
            if (lease is null)
            {
                throw CreateBulkheadRejectedException();
            }

            bulkheadLease = lease.Value;
        }

        using (bulkheadLease)
        {
            return await ExecuteAfterAdmissionAsync(execute, context).ConfigureAwait(false);
        }
    }

    private async Task<TResponse> ExecuteAfterAdmissionAsync<TResponse>(
        Func<Task<TResponse>> execute,
        ServerCallContext context)
    {
        try
        {
            var response = await ExecuteWithTimeoutAsync(execute, context).ConfigureAwait(false);
            circuitBreakerState.RecordSuccess();
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception exception) when (IsPollyTimeoutRejectedException(exception))
        {
            circuitBreakerState.RecordFailure(exception);
            throw CreateTimeoutException();
        }
        catch (TimeoutException exception)
        {
            circuitBreakerState.RecordFailure(exception);
            throw CreateTimeoutException();
        }
        catch (Exception exception) when (IsPollyBrokenCircuitException(exception))
        {
            circuitBreakerState.RecordFailure(exception);
            throw CreateCircuitBreakerException();
        }
        catch (Exception exception) when (ShouldTripCircuit(exception))
        {
            circuitBreakerState.RecordFailure(exception);
            throw;
        }
    }

    private async Task ExecuteAsync(
        Func<Task> execute,
        ServerCallContext context)
    {
        if (!TryEnterCircuit())
        {
            throw CreateCircuitBreakerException();
        }

        CephalonGrpcDirectModuleBulkheadState.Lease bulkheadLease = default;
        if (options.BulkheadEnabled)
        {
            var lease = await bulkheadState.TryEnterAsync(context.CancellationToken).ConfigureAwait(false);
            if (lease is null)
            {
                throw CreateBulkheadRejectedException();
            }

            bulkheadLease = lease.Value;
        }

        using (bulkheadLease)
        {
            await ExecuteAfterAdmissionAsync(execute, context).ConfigureAwait(false);
        }
    }

    private async Task ExecuteAfterAdmissionAsync(
        Func<Task> execute,
        ServerCallContext context)
    {
        try
        {
            await ExecuteWithTimeoutAsync(execute, context).ConfigureAwait(false);
            circuitBreakerState.RecordSuccess();
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception exception) when (IsPollyTimeoutRejectedException(exception))
        {
            circuitBreakerState.RecordFailure(exception);
            throw CreateTimeoutException();
        }
        catch (TimeoutException exception)
        {
            circuitBreakerState.RecordFailure(exception);
            throw CreateTimeoutException();
        }
        catch (Exception exception) when (IsPollyBrokenCircuitException(exception))
        {
            circuitBreakerState.RecordFailure(exception);
            throw CreateCircuitBreakerException();
        }
        catch (Exception exception) when (ShouldTripCircuit(exception))
        {
            circuitBreakerState.RecordFailure(exception);
            throw;
        }
    }

    private bool TryEnterCircuit()
    {
        if (!options.CircuitBreakerEnabled)
        {
            return true;
        }

        return circuitBreakerState.TryEnter(out _);
    }

    private async Task<TResponse> ExecuteWithTimeoutAsync<TResponse>(
        Func<Task<TResponse>> execute,
        ServerCallContext context)
    {
        var task = execute();
        if (!options.TimeoutEnabled || options.Timeout is not { } timeout)
        {
            return await task.ConfigureAwait(false);
        }

        return await task.WaitAsync(timeout, context.CancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteWithTimeoutAsync(
        Func<Task> execute,
        ServerCallContext context)
    {
        var task = execute();
        if (!options.TimeoutEnabled || options.Timeout is not { } timeout)
        {
            await task.ConfigureAwait(false);
            return;
        }

        await task.WaitAsync(timeout, context.CancellationToken).ConfigureAwait(false);
    }

    private static bool IsPollyTimeoutRejectedException(Exception exception)
        => IsExceptionType(exception, TimeoutRejectedExceptionTypeName);

    private static bool IsPollyBrokenCircuitException(Exception exception)
        => IsExceptionType(exception, BrokenCircuitExceptionTypeName);

    private static bool ShouldTripCircuit(Exception exception)
        => exception is TimeoutException ||
            exception is HttpRequestException ||
            exception is IOException ||
            exception is System.Net.Sockets.SocketException ||
            exception is System.Data.Common.DbException;

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

    private static RpcException CreateTimeoutException()
        => new(
            new Status(
                StatusCode.DeadlineExceeded,
                "The gRPC request exceeded the configured Cephalon execution timeout."),
            CreateTrailers(TimeoutCode));

    private static RpcException CreateCircuitBreakerException()
        => new(
            new Status(
                StatusCode.Unavailable,
                "The gRPC request was rejected because the configured Cephalon circuit breaker is open."),
            CreateTrailers(CircuitBreakerOpenCode));

    private static RpcException CreateBulkheadRejectedException()
        => new(
            new Status(
                StatusCode.ResourceExhausted,
                "The gRPC request was rejected because the configured Cephalon bulkhead concurrency limit is full."),
            CreateTrailers(BulkheadRejectedCode));

    private static Metadata CreateTrailers(string code)
        => new()
        {
            { "cephalon-code", code },
            { "cephalon-fault", "resilience" }
        };
}
