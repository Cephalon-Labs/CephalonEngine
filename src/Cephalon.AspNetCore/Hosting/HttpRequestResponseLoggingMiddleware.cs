using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Cephalon.AspNetCore.Hosting;

internal sealed class HttpRequestResponseLoggingMiddleware(
    RequestDelegate next,
    HttpRequestResponseLoggingOptions options,
    ILogger<HttpRequestResponseLoggingMiddleware> logger)
{
    private const string TraceParentHeaderName = "traceparent";
    private const string TraceStateHeaderName = "tracestate";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlation = RequestCorrelation.Create(context);
        var activity = Activity.Current;

        ApplyCorrelationToActivity(activity, correlation);

        using var scope = logger.BeginScope(correlation.CreateScope());

        LogRequestStarted(context, correlation);
        AddLogReferenceEvent(
            activity,
            "cephalon.http.request.started",
            AspNetCoreDiagnosticsConventions.HttpRequestStarted.Id,
            correlation,
            tags =>
            {
                tags["http.request.method"] = context.Request.Method;
                tags["url.path"] = context.Request.Path.Value ?? "/";
            });

        var requestBody = options.LogRequestBody
            ? await TryReadRequestBodyAsync(context.Request, options.RequestBodyLimit, context.RequestAborted).ConfigureAwait(false)
            : BodyCaptureResult.None;

        if (requestBody.ShouldLog)
        {
            LogRequestBody(context, correlation, requestBody);
            AddLogReferenceEvent(
                activity,
                "cephalon.http.request.body.logged",
                AspNetCoreDiagnosticsConventions.HttpRequestBodyCaptured.Id,
                correlation,
                tags =>
                {
                    tags["cephalon.http.body.truncated"] = requestBody.IsTruncated;
                    tags["http.request.body.content_type"] = requestBody.ContentType;
                });
        }

        var started = Stopwatch.GetTimestamp();
        var originalResponseBody = context.Response.Body;
        LimitedBodyCaptureStream? responseCapture = null;

        if (options.LogResponseBody)
        {
            responseCapture = new LimitedBodyCaptureStream(originalResponseBody, options.ResponseBodyLimit);
            context.Response.Body = responseCapture;
        }

        try
        {
            await next(context).ConfigureAwait(false);

            var elapsedMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            LogResponseCompleted(context, correlation, elapsedMilliseconds, responseCapture);
            AddLogReferenceEvent(
                activity,
                "cephalon.http.response.completed",
                AspNetCoreDiagnosticsConventions.HttpResponseCompleted.Id,
                correlation,
                tags =>
                {
                    tags["http.response.status_code"] = context.Response.StatusCode;
                    tags["cephalon.http.elapsed_ms"] = elapsedMilliseconds;
                });

            var responseBody = responseCapture?.CreateResult(context.Response.ContentType);
            if (responseBody is { ShouldLog: true })
            {
                LogResponseBody(context, correlation, responseBody.Value);
                AddLogReferenceEvent(
                    activity,
                    "cephalon.http.response.body.logged",
                    AspNetCoreDiagnosticsConventions.HttpResponseBodyCaptured.Id,
                    correlation,
                    tags =>
                    {
                        tags["cephalon.http.body.truncated"] = responseBody.Value.IsTruncated;
                        tags["http.response.body.content_type"] = responseBody.Value.ContentType;
                    });
            }
        }
        catch (Exception exception)
        {
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            LogRequestFailed(context, correlation, elapsedMilliseconds, exception);
            AddLogReferenceEvent(
                activity,
                "cephalon.http.request.failed",
                AspNetCoreDiagnosticsConventions.HttpRequestFailed.Id,
                correlation,
                tags =>
                {
                    tags["http.response.status_code"] = context.Response.StatusCode;
                    tags["cephalon.http.elapsed_ms"] = elapsedMilliseconds;
                    tags["exception.type"] = exception.GetType().FullName;
                });
            throw;
        }
        finally
        {
            if (responseCapture is not null)
            {
                context.Response.Body = originalResponseBody;
                responseCapture.Dispose();
            }
        }
    }

    private void LogRequestStarted(HttpContext context, RequestCorrelation correlation)
    {
        HttpRequestResponseLoggingLogs.RequestStarted(
            logger,
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            context.Request.QueryString.Value,
            correlation.RequestId,
            correlation.TraceId,
            correlation.SpanId,
            context.Request.ContentType,
            correlation.TraceParent,
            context.Request.ContentLength);
    }

    private void LogRequestBody(HttpContext context, RequestCorrelation correlation, BodyCaptureResult requestBody)
    {
        HttpRequestResponseLoggingLogs.RequestBodyCaptured(
            logger,
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            context.Request.QueryString.Value,
            correlation.RequestId,
            correlation.TraceId,
            correlation.SpanId,
            requestBody.ContentType,
            requestBody.IsTruncated,
            requestBody.Body);
    }

    private void LogResponseCompleted(
        HttpContext context,
        RequestCorrelation correlation,
        double elapsedMilliseconds,
        LimitedBodyCaptureStream? responseCapture)
    {
        HttpRequestResponseLoggingLogs.ResponseCompleted(
            logger,
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            context.Request.QueryString.Value,
            context.Response.StatusCode,
            elapsedMilliseconds,
            correlation.RequestId,
            correlation.TraceId,
            correlation.SpanId,
            context.Response.ContentType,
            context.Response.ContentLength ?? responseCapture?.TotalBytesWritten);
    }

    private void LogResponseBody(HttpContext context, RequestCorrelation correlation, BodyCaptureResult responseBody)
    {
        HttpRequestResponseLoggingLogs.ResponseBodyCaptured(
            logger,
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            context.Request.QueryString.Value,
            correlation.RequestId,
            correlation.TraceId,
            correlation.SpanId,
            responseBody.ContentType,
            responseBody.IsTruncated,
            responseBody.Body);
    }

    private void LogRequestFailed(
        HttpContext context,
        RequestCorrelation correlation,
        double elapsedMilliseconds,
        Exception exception)
    {
        HttpRequestResponseLoggingLogs.RequestFailed(
            logger,
            exception,
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            context.Request.QueryString.Value,
            elapsedMilliseconds,
            correlation.RequestId,
            correlation.TraceId,
            correlation.SpanId,
            context.Response.StatusCode);
    }

    private static void ApplyCorrelationToActivity(Activity? activity, RequestCorrelation correlation)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("cephalon.http.request_id", correlation.RequestId);

        if (!string.IsNullOrWhiteSpace(correlation.TraceParent))
        {
            activity.SetTag("cephalon.http.traceparent", correlation.TraceParent);
        }
    }

    private static void AddLogReferenceEvent(
        Activity? activity,
        string name,
        int eventId,
        RequestCorrelation correlation,
        Action<ActivityTagsCollection>? configureTags = null)
    {
        if (activity is null)
        {
            return;
        }

        var tags = new ActivityTagsCollection
        {
            ["cephalon.log.event_id"] = eventId,
            ["cephalon.http.request_id"] = correlation.RequestId
        };

        if (!string.IsNullOrWhiteSpace(correlation.TraceParent))
        {
            tags["cephalon.http.traceparent"] = correlation.TraceParent;
        }

        configureTags?.Invoke(tags);
        activity.AddEvent(new ActivityEvent(name, tags: tags));
    }

    private static async Task<BodyCaptureResult> TryReadRequestBodyAsync(
        HttpRequest request,
        int bodyLimit,
        CancellationToken cancellationToken)
    {
        if (request.Body is null ||
            !request.Body.CanRead ||
            !ShouldCaptureTextBody(request.ContentType))
        {
            return BodyCaptureResult.None;
        }

        request.EnableBuffering();
        var result = await ReadBodyAsync(request.Body, request.ContentType, bodyLimit, cancellationToken).ConfigureAwait(false);
        request.Body.Position = 0;
        return result;
    }

    private static async Task<BodyCaptureResult> ReadBodyAsync(
        Stream stream,
        string? contentType,
        int bodyLimit,
        CancellationToken cancellationToken)
    {
        var byteLimit = Math.Max(0, bodyLimit);
        var buffer = new byte[Math.Max(1, Math.Min(byteLimit + 1, 4096))];
        using var capture = new MemoryStream(Math.Max(1, byteLimit + 1));

        while (capture.Length < byteLimit + 1)
        {
            var remaining = (int)Math.Min(buffer.Length, (byteLimit + 1) - capture.Length);
            var read = await stream.ReadAsync(buffer.AsMemory(0, remaining), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            await capture.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        if (capture.Length == 0)
        {
            return BodyCaptureResult.Empty(contentType);
        }

        var payload = capture.ToArray();
        var truncated = payload.Length > byteLimit;
        var effectiveLength = truncated ? byteLimit : payload.Length;
        var encoding = ResolveEncoding(contentType);
        var text = encoding.GetString(payload, 0, effectiveLength);

        return new BodyCaptureResult(text, contentType, truncated);
    }

    private static bool ShouldCaptureTextBody(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType) ||
            !MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
        {
            return false;
        }

        var mediaTypeText = mediaType.MediaType.Value;
        if (string.IsNullOrWhiteSpace(mediaTypeText))
        {
            return false;
        }

        return mediaTypeText.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
            mediaTypeText.Contains("json", StringComparison.OrdinalIgnoreCase) ||
            mediaTypeText.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
            mediaTypeText.Contains("graphql", StringComparison.OrdinalIgnoreCase) ||
            mediaTypeText.Contains("javascript", StringComparison.OrdinalIgnoreCase) ||
            mediaTypeText.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
    }

    private static Encoding ResolveEncoding(string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType) &&
            MediaTypeHeaderValue.TryParse(contentType, out var mediaType) &&
            !StringSegment.IsNullOrEmpty(mediaType.Charset))
        {
            try
            {
                return Encoding.GetEncoding(mediaType.Charset.Value!);
            }
            catch (ArgumentException)
            {
            }
        }

        return Encoding.UTF8;
    }

    private readonly record struct RequestCorrelation(
        string RequestId,
        string? TraceId,
        string? SpanId,
        string? TraceParent)
    {
        public static RequestCorrelation Create(HttpContext context)
        {
            var requestId = context.TraceIdentifier;
            var traceParent = context.Request.Headers[TraceParentHeaderName].ToString();
            var activity = Activity.Current;
            var traceId = activity is { } currentActivity && currentActivity.TraceId != default
                ? currentActivity.TraceId.ToString()
                : null;
            var spanId = activity is { } currentActivityWithSpan && currentActivityWithSpan.SpanId != default
                ? currentActivityWithSpan.SpanId.ToString()
                : null;

            if ((!string.IsNullOrWhiteSpace(traceId) && !string.IsNullOrWhiteSpace(spanId)) ||
                string.IsNullOrWhiteSpace(traceParent))
            {
                return new RequestCorrelation(requestId, traceId, spanId, string.IsNullOrWhiteSpace(traceParent) ? null : traceParent);
            }

            return ActivityContext.TryParse(
                traceParent,
                context.Request.Headers[TraceStateHeaderName].ToString(),
                out var parsed)
                ? new RequestCorrelation(
                    requestId,
                    parsed.TraceId.ToString(),
                    parsed.SpanId.ToString(),
                    traceParent)
                : new RequestCorrelation(requestId, null, null, traceParent);
        }

        public Dictionary<string, object?> CreateScope()
        {
            var scope = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["RequestId"] = RequestId
            };

            if (!string.IsNullOrWhiteSpace(TraceId))
            {
                scope["TraceId"] = TraceId;
            }

            if (!string.IsNullOrWhiteSpace(SpanId))
            {
                scope["SpanId"] = SpanId;
            }

            if (!string.IsNullOrWhiteSpace(TraceParent))
            {
                scope["TraceParent"] = TraceParent;
            }

            return scope;
        }
    }

    private readonly record struct BodyCaptureResult(
        string Body,
        string? ContentType,
        bool IsTruncated)
    {
        public static BodyCaptureResult None => new(string.Empty, null, false);

        public static BodyCaptureResult Empty(string? contentType) => new(string.Empty, contentType, false);

        public bool ShouldLog => ContentType is not null;
    }

    private sealed class LimitedBodyCaptureStream(Stream innerStream, int bodyLimit) : Stream
    {
        private readonly Stream innerStream = innerStream;
        private readonly MemoryStream capture = new(Math.Max(1, Math.Max(0, bodyLimit) + 1));
        private readonly int bodyLimit = Math.Max(0, bodyLimit);

        public long TotalBytesWritten { get; private set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => innerStream.CanSeek ? innerStream.Length : TotalBytesWritten;

        public override long Position
        {
            get => innerStream.CanSeek ? innerStream.Position : TotalBytesWritten;
            set => throw new NotSupportedException();
        }

        public BodyCaptureResult CreateResult(string? contentType)
        {
            if (!ShouldCaptureTextBody(contentType))
            {
                return BodyCaptureResult.None;
            }

            if (capture.Length == 0)
            {
                return BodyCaptureResult.Empty(contentType);
            }

            var payload = capture.ToArray();
            var truncated = payload.Length > bodyLimit;
            var effectiveLength = truncated ? bodyLimit : payload.Length;
            var encoding = ResolveEncoding(contentType);
            var text = encoding.GetString(payload, 0, effectiveLength);

            return new BodyCaptureResult(text, contentType, truncated);
        }

        public override void Flush()
        {
            innerStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return innerStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            innerStream.Write(buffer, offset, count);
            Capture(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            innerStream.Write(buffer);
            Capture(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Capture(buffer.AsSpan(offset, count));
            return innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Capture(buffer.Span);
            return innerStream.WriteAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                capture.Dispose();
            }

            base.Dispose(disposing);
        }

        private void Capture(ReadOnlySpan<byte> buffer)
        {
            TotalBytesWritten += buffer.Length;
            if (capture.Length > bodyLimit)
            {
                return;
            }

            var remaining = (int)Math.Max(0, (bodyLimit + 1) - capture.Length);
            if (remaining == 0)
            {
                return;
            }

            var length = Math.Min(buffer.Length, remaining);
            if (length > 0)
            {
                capture.Write(buffer[..length]);
            }
        }
    }
}

internal static partial class HttpRequestResponseLoggingLogs
{
    [LoggerMessage(
        EventId = AspNetCoreDiagnosticsConventions.HttpRequestStartedId,
        EventName = AspNetCoreDiagnosticsConventions.HttpRequestStartedName,
        Level = LogLevel.Information,
        Message = AspNetCoreDiagnosticsConventions.HttpRequestStartedMessageTemplate)]
    public static partial void RequestStarted(
        ILogger logger,
        string method,
        string path,
        string? queryString,
        string requestId,
        string? traceId,
        string? spanId,
        string? contentType,
        string? traceParent,
        long? contentLength);

    [LoggerMessage(
        EventId = AspNetCoreDiagnosticsConventions.HttpRequestBodyCapturedId,
        EventName = AspNetCoreDiagnosticsConventions.HttpRequestBodyCapturedName,
        Level = LogLevel.Information,
        Message = AspNetCoreDiagnosticsConventions.HttpRequestBodyCapturedMessageTemplate)]
    public static partial void RequestBodyCaptured(
        ILogger logger,
        string method,
        string path,
        string? queryString,
        string requestId,
        string? traceId,
        string? spanId,
        string? contentType,
        bool isTruncated,
        string body);

    [LoggerMessage(
        EventId = AspNetCoreDiagnosticsConventions.HttpResponseCompletedId,
        EventName = AspNetCoreDiagnosticsConventions.HttpResponseCompletedName,
        Level = LogLevel.Information,
        Message = AspNetCoreDiagnosticsConventions.HttpResponseCompletedMessageTemplate)]
    public static partial void ResponseCompleted(
        ILogger logger,
        string method,
        string path,
        string? queryString,
        int statusCode,
        double elapsedMilliseconds,
        string requestId,
        string? traceId,
        string? spanId,
        string? contentType,
        long? contentLength);

    [LoggerMessage(
        EventId = AspNetCoreDiagnosticsConventions.HttpResponseBodyCapturedId,
        EventName = AspNetCoreDiagnosticsConventions.HttpResponseBodyCapturedName,
        Level = LogLevel.Information,
        Message = AspNetCoreDiagnosticsConventions.HttpResponseBodyCapturedMessageTemplate)]
    public static partial void ResponseBodyCaptured(
        ILogger logger,
        string method,
        string path,
        string? queryString,
        string requestId,
        string? traceId,
        string? spanId,
        string? contentType,
        bool isTruncated,
        string body);

    [LoggerMessage(
        EventId = AspNetCoreDiagnosticsConventions.HttpRequestFailedId,
        EventName = AspNetCoreDiagnosticsConventions.HttpRequestFailedName,
        Level = LogLevel.Error,
        Message = AspNetCoreDiagnosticsConventions.HttpRequestFailedMessageTemplate)]
    public static partial void RequestFailed(
        ILogger logger,
        Exception exception,
        string method,
        string path,
        string? queryString,
        double elapsedMilliseconds,
        string requestId,
        string? traceId,
        string? spanId,
        int statusCode);
}
