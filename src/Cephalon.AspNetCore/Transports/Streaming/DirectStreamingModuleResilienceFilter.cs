using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Cephalon.AspNetCore.Transports.Streaming;

internal sealed class DirectStreamingModuleResilienceFilter(
    DirectStreamingModuleTransportKind transportKind,
    DirectStreamingModuleResilienceOptions options,
    DirectStreamingModuleCircuitBreakerState circuitBreakerState,
    DirectStreamingModuleBulkheadState bulkheadState,
    DirectStreamingModuleTimeoutState timeoutState) : IEndpointFilter
{
    private const string BrokenCircuitExceptionTypeName = "Polly.CircuitBreaker.BrokenCircuitException";
    private const string TimeoutRejectedExceptionTypeName = "Polly.Timeout.TimeoutRejectedException";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!options.HasEnforcedStrategies)
        {
            return await next(context).ConfigureAwait(false);
        }

        var httpContext = context.HttpContext;
        if (!TryEnterCircuit(out var retryAfterSeconds))
        {
            return CreateFaultResult(
                httpContext,
                CreateCircuitBreakerFault(retryAfterSeconds));
        }

        DirectStreamingModuleBulkheadState.Lease bulkheadLease = default;
        if (options.BulkheadEnabled)
        {
            var lease = await bulkheadState.TryEnterAsync(httpContext.RequestAborted).ConfigureAwait(false);
            if (lease is null)
            {
                return CreateFaultResult(
                    httpContext,
                    CreateBulkheadRejectedFault());
            }

            bulkheadLease = lease.Value;
        }

        using (bulkheadLease)
        {
            return await ExecuteAfterAdmissionAsync(context, next).ConfigureAwait(false);
        }
    }

    private async ValueTask<object?> ExecuteAfterAdmissionAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
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
            return CreateFaultResult(context.HttpContext, CreateTimeoutFault());
        }
        catch (TimeoutException exception)
        {
            circuitBreakerState.RecordFailure(exception);
            timeoutState.RecordTimeout();
            return CreateFaultResult(context.HttpContext, CreateTimeoutFault());
        }
        catch (Exception exception) when (IsPollyBrokenCircuitException(exception))
        {
            circuitBreakerState.RecordFailure(exception);
            return CreateFaultResult(context.HttpContext, CreateCircuitBreakerFault(circuitBreakerState.RetryAfterSeconds));
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
            throw new TimeoutException($"The {ResolveDisplayName()} request exceeded the configured Cephalon execution timeout.", exception);
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

    private IResult CreateFaultResult(HttpContext httpContext, DirectStreamingModuleFaultDescriptor fault)
    {
        if (transportKind == DirectStreamingModuleTransportKind.WebSocket)
        {
            if (httpContext.WebSockets.IsWebSocketRequest && !httpContext.Response.HasStarted)
            {
                return new WebSocketFaultResult(fault);
            }

            return new JsonFaultResult(fault);
        }

        return new ServerSentEventsFaultResult(fault);
    }

    private DirectStreamingModuleFaultDescriptor CreateTimeoutFault()
        => new(
            Message: $"The {ResolveDisplayName()} request exceeded the configured Cephalon execution timeout.",
            Code: $"{ResolveCodePrefix()}_execution_timeout",
            StatusCode: StatusCodes.Status503ServiceUnavailable,
            RetryAfterSeconds: null);

    private DirectStreamingModuleFaultDescriptor CreateCircuitBreakerFault(int retryAfterSeconds)
        => new(
            Message: $"The {ResolveDisplayName()} request was rejected because the configured Cephalon circuit breaker is open.",
            Code: $"{ResolveCodePrefix()}_circuit_breaker_open",
            StatusCode: StatusCodes.Status503ServiceUnavailable,
            RetryAfterSeconds: Math.Max(1, retryAfterSeconds));

    private DirectStreamingModuleFaultDescriptor CreateBulkheadRejectedFault()
        => new(
            Message: $"The {ResolveDisplayName()} request exceeded the configured Cephalon concurrency limit.",
            Code: $"{ResolveCodePrefix()}_bulkhead_rejected",
            StatusCode: StatusCodes.Status429TooManyRequests,
            RetryAfterSeconds: null);

    private string ResolveDisplayName()
        => transportKind == DirectStreamingModuleTransportKind.WebSocket ? "WebSocket" : "SSE";

    private string ResolveCodePrefix()
        => transportKind == DirectStreamingModuleTransportKind.WebSocket ? "websocket" : "sse";

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

    private static Dictionary<string, object?> CreatePayload(DirectStreamingModuleFaultDescriptor fault)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["error"] = fault.Message,
            ["code"] = fault.Code,
            ["fault"] = "resilience",
            ["statusCode"] = fault.StatusCode
        };

        if (fault.RetryAfterSeconds.HasValue)
        {
            payload["retryAfterSeconds"] = fault.RetryAfterSeconds.Value;
        }

        return payload;
    }

    private sealed class ServerSentEventsFaultResult(DirectStreamingModuleFaultDescriptor fault) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            if (fault.RetryAfterSeconds.HasValue)
            {
                httpContext.Response.Headers.RetryAfter = fault.RetryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture);
            }

            httpContext.Response.ContentType = "text/event-stream";
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Connection = "keep-alive";
            httpContext.Response.Headers["X-Accel-Buffering"] = "no";
            httpContext.Response.StatusCode = StatusCodes.Status200OK;

            var json = JsonSerializer.Serialize(CreatePayload(fault));
            await httpContext.Response.WriteAsync($"event: error\ndata: {json}\n\n", Encoding.UTF8, CancellationToken.None)
                .ConfigureAwait(false);
            await httpContext.Response.Body.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private sealed class WebSocketFaultResult(DirectStreamingModuleFaultDescriptor fault) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            using var socket = await httpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            var json = JsonSerializer.Serialize(CreatePayload(fault));
            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None)
                .ConfigureAwait(false);

            var closeStatus = fault.StatusCode == StatusCodes.Status429TooManyRequests
                ? WebSocketCloseStatus.PolicyViolation
                : WebSocketCloseStatus.InternalServerError;
            await socket.CloseAsync(closeStatus, fault.Code, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private sealed class JsonFaultResult(DirectStreamingModuleFaultDescriptor fault) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            if (fault.RetryAfterSeconds.HasValue)
            {
                httpContext.Response.Headers.RetryAfter = fault.RetryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture);
            }

            return Results.Json(CreatePayload(fault), statusCode: fault.StatusCode).ExecuteAsync(httpContext);
        }
    }

    private readonly record struct DirectStreamingModuleFaultDescriptor(
        string Message,
        string Code,
        int StatusCode,
        int? RetryAfterSeconds);
}
