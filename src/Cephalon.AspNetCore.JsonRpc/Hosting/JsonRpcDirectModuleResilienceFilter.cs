using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Cephalon.AspNetCore.JsonRpc.Hosting;

internal sealed class JsonRpcDirectModuleResilienceFilter(
    JsonRpcDirectModuleResilienceOptions options,
    JsonRpcDirectModuleCircuitBreakerState circuitBreakerState,
    JsonRpcDirectModuleBulkheadState bulkheadState,
    JsonRpcDirectModuleTimeoutState timeoutState) : IEndpointFilter
{
    private const string ProtocolVersion = "2.0";
    private const string BrokenCircuitExceptionTypeName = "Polly.CircuitBreaker.BrokenCircuitException";
    private const string TimeoutRejectedExceptionTypeName = "Polly.Timeout.TimeoutRejectedException";
    private const int ServiceUnavailableJsonRpcCode = -32053;
    private const int TooManyRequestsJsonRpcCode = -32029;
    private const string TimeoutCode = "jsonrpc_execution_timeout";
    private const string CircuitBreakerOpenCode = "jsonrpc_circuit_breaker_open";
    private const string BulkheadRejectedCode = "jsonrpc_bulkhead_rejected";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!options.HasEnforcedStrategies)
        {
            return await next(context).ConfigureAwait(false);
        }

        var httpContext = context.HttpContext;
        var requestId = await TryReadRequestIdAsync(httpContext).ConfigureAwait(false);

        if (!TryEnterCircuit(out var retryAfterSeconds))
        {
            return CreateCircuitBreakerResult(requestId, retryAfterSeconds);
        }

        JsonRpcDirectModuleBulkheadState.Lease bulkheadLease = default;
        if (options.BulkheadEnabled)
        {
            var lease = await bulkheadState.TryEnterAsync(httpContext.RequestAborted).ConfigureAwait(false);
            if (lease is null)
            {
                return CreateBulkheadRejectedResult(requestId);
            }

            bulkheadLease = lease.Value;
        }

        using (bulkheadLease)
        {
            return await ExecuteAfterAdmissionAsync(context, next, requestId).ConfigureAwait(false);
        }
    }

    private async ValueTask<object?> ExecuteAfterAdmissionAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next,
        object? requestId)
    {
        try
        {
            var result = await ExecuteWithTimeoutAsync(context, next).ConfigureAwait(false);
            circuitBreakerState.RecordSuccess();
            return result;
        }
        catch (Exception exception) when (IsPollyTimeoutRejectedException(exception))
        {
            circuitBreakerState.RecordFailure(exception);
            timeoutState.RecordTimeout();
            return CreateTimeoutResult(requestId);
        }
        catch (TimeoutException exception)
        {
            circuitBreakerState.RecordFailure(exception);
            timeoutState.RecordTimeout();
            return CreateTimeoutResult(requestId);
        }
        catch (Exception exception) when (IsPollyBrokenCircuitException(exception))
        {
            circuitBreakerState.RecordFailure(exception);
            return CreateCircuitBreakerResult(requestId, circuitBreakerState.RetryAfterSeconds);
        }
        catch (Exception exception) when (ShouldTripCircuit(exception))
        {
            circuitBreakerState.RecordFailure(exception);
            throw;
        }
    }

    private bool TryEnterCircuit(out int retryAfterSeconds)
    {
        if (!options.CircuitBreakerEnabled)
        {
            retryAfterSeconds = 0;
            return true;
        }

        return circuitBreakerState.TryEnter(out retryAfterSeconds);
    }

    private async ValueTask<object?> ExecuteWithTimeoutAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (!options.TimeoutEnabled || options.Timeout is not { } timeout)
        {
            return await next(context).ConfigureAwait(false);
        }

        var httpContext = context.HttpContext;
        var originalRequestAborted = httpContext.RequestAborted;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(originalRequestAborted);
        timeoutCts.CancelAfter(timeout);
        httpContext.RequestAborted = timeoutCts.Token;

        var task = next(context).AsTask();
        try
        {
            return await task.WaitAsync(timeout, originalRequestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (timeoutCts.IsCancellationRequested && !originalRequestAborted.IsCancellationRequested)
        {
            throw new TimeoutException("The JSON-RPC request exceeded the configured Cephalon execution timeout.", exception);
        }
        catch (TimeoutException)
        {
            timeoutCts.Cancel();
            throw;
        }
        finally
        {
            httpContext.RequestAborted = originalRequestAborted;
        }
    }

    private static IResult CreateTimeoutResult(object? requestId)
        => CreateErrorResult(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            jsonRpcCode: ServiceUnavailableJsonRpcCode,
            message: "Service unavailable: the JSON-RPC request exceeded the configured Cephalon execution timeout.",
            cephalonCode: TimeoutCode,
            requestId: requestId,
            retryAfterSeconds: null);

    private static IResult CreateCircuitBreakerResult(object? requestId, int retryAfterSeconds)
        => CreateErrorResult(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            jsonRpcCode: ServiceUnavailableJsonRpcCode,
            message: "Service unavailable: the configured Cephalon circuit breaker is open.",
            cephalonCode: CircuitBreakerOpenCode,
            requestId: requestId,
            retryAfterSeconds: Math.Max(1, retryAfterSeconds));

    private static IResult CreateBulkheadRejectedResult(object? requestId)
        => CreateErrorResult(
            statusCode: StatusCodes.Status429TooManyRequests,
            jsonRpcCode: TooManyRequestsJsonRpcCode,
            message: "Too many requests: the configured Cephalon bulkhead concurrency limit is full.",
            cephalonCode: BulkheadRejectedCode,
            requestId: requestId,
            retryAfterSeconds: null);

    private static IResult CreateErrorResult(
        int statusCode,
        int jsonRpcCode,
        string message,
        string cephalonCode,
        object? requestId,
        int? retryAfterSeconds)
    {
        var data = retryAfterSeconds is null
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["cephalonCode"] = cephalonCode,
                ["fault"] = "resilience",
                ["statusCode"] = statusCode
            }
            : new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["cephalonCode"] = cephalonCode,
                ["fault"] = "resilience",
                ["statusCode"] = statusCode,
                ["retryAfterSeconds"] = retryAfterSeconds.Value
            };

        var envelope = new
        {
            jsonRpc = ProtocolVersion,
            result = (object?)null,
            error = new
            {
                code = jsonRpcCode,
                message,
                data
            },
            id = requestId
        };

        return retryAfterSeconds is null
            ? Results.Json(envelope, statusCode: statusCode)
            : new RetryAfterJsonResult(envelope, statusCode, retryAfterSeconds.Value);
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

    private static async ValueTask<object?> TryReadRequestIdAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method) ||
            context.Request.Body is null)
        {
            return null;
        }

        try
        {
            context.Request.EnableBuffering();
            using var document = await JsonDocument.ParseAsync(
                context.Request.Body,
                cancellationToken: context.RequestAborted).ConfigureAwait(false);

            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("id", out var idElement))
            {
                return null;
            }

            return idElement.ValueKind switch
            {
                JsonValueKind.String => idElement.GetString(),
                JsonValueKind.Number when idElement.TryGetInt64(out var integerId) => integerId,
                JsonValueKind.Number => idElement.GetDouble(),
                JsonValueKind.Null => null,
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            return null;
        }
        finally
        {
            if (context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
            }
        }
    }

    private sealed class RetryAfterJsonResult(object envelope, int statusCode, int retryAfterSeconds) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
            return Results.Json(envelope, statusCode: statusCode).ExecuteAsync(httpContext);
        }
    }
}
