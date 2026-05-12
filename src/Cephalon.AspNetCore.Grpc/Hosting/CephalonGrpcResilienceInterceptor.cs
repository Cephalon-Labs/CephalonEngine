using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Cephalon.AspNetCore.Grpc.Hosting;

internal sealed class CephalonGrpcResilienceInterceptor : Interceptor
{
    private const string BrokenCircuitExceptionTypeName = "Polly.CircuitBreaker.BrokenCircuitException";
    private const string TimeoutRejectedExceptionTypeName = "Polly.Timeout.TimeoutRejectedException";
    private const string TimeoutCode = "grpc_execution_timeout";
    private const string CircuitBreakerOpenCode = "grpc_circuit_breaker_open";

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(continuation);

        return MapResilienceFaultsAsync(() => continuation(request, context));
    }

    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(continuation);

        return MapResilienceFaultsAsync(() => continuation(requestStream, context));
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

        return MapResilienceFaultsAsync(() => continuation(request, responseStream, context));
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

        return MapResilienceFaultsAsync(() => continuation(requestStream, responseStream, context));
    }

    private static async Task<TResponse> MapResilienceFaultsAsync<TResponse>(Func<Task<TResponse>> execute)
    {
        try
        {
            return await execute().ConfigureAwait(false);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception exception) when (IsPollyTimeoutRejectedException(exception))
        {
            throw CreateTimeoutException();
        }
        catch (TimeoutException)
        {
            throw CreateTimeoutException();
        }
        catch (Exception exception) when (IsPollyBrokenCircuitException(exception))
        {
            throw CreateCircuitBreakerException();
        }
    }

    private static async Task MapResilienceFaultsAsync(Func<Task> execute)
    {
        try
        {
            await execute().ConfigureAwait(false);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception exception) when (IsPollyTimeoutRejectedException(exception))
        {
            throw CreateTimeoutException();
        }
        catch (TimeoutException)
        {
            throw CreateTimeoutException();
        }
        catch (Exception exception) when (IsPollyBrokenCircuitException(exception))
        {
            throw CreateCircuitBreakerException();
        }
    }

    private static bool IsPollyTimeoutRejectedException(Exception exception)
        => IsExceptionType(exception, TimeoutRejectedExceptionTypeName);

    private static bool IsPollyBrokenCircuitException(Exception exception)
        => IsExceptionType(exception, BrokenCircuitExceptionTypeName);

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

    private static Metadata CreateTrailers(string code)
        => new()
        {
            { "cephalon-code", code },
            { "cephalon-fault", "resilience" }
        };
}
