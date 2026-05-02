using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Net;
using Cephalon.Diagnostics.Redaction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Cephalon.AspNetCore.Hosting;

internal sealed class HttpRequestResponseLoggingMiddleware(
    RequestDelegate next,
    HttpRequestResponseLoggingOptions options,
    ILogger<HttpRequestResponseLoggingMiddleware> logger,
    RedactionPipeline redactionPipeline)
{
    private const string TraceParentHeaderName = "traceparent";
    private const string TraceStateHeaderName = "tracestate";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlation = RequestCorrelation.Create(context);
        var activity = Activity.Current;
        var redactedQueryString = RedactQueryString(context.Request.QueryString.Value, options);

        ApplyCorrelationToActivity(activity, correlation);

        using var scope = logger.BeginScope(correlation.CreateScope());

        LogRequestStarted(context, correlation, redactedQueryString);
        AddLogReferenceEvent(
            activity,
            "cephalon.http.request.started",
            AspNetCoreDiagnosticsConventions.HttpRequestStarted.Id,
            correlation,
            tags =>
            {
                tags["http.request.method"] = Redact(activity, "http.request.method", context.Request.Method);
                tags["url.path"] = Redact(activity, "url.path", context.Request.Path.Value ?? "/");
            });

        var requestBody = options.LogRequestBody
            ? await TryReadRequestBodyAsync(context.Request, options.RequestBodyLimit, context.RequestAborted).ConfigureAwait(false)
            : BodyCaptureResult.None;
        requestBody = RedactBodyCaptureResult(requestBody, options);

        if (requestBody.ShouldLog)
        {
            LogRequestBody(context, correlation, redactedQueryString, requestBody);
            AddLogReferenceEvent(
                activity,
                "cephalon.http.request.body.logged",
                AspNetCoreDiagnosticsConventions.HttpRequestBodyCaptured.Id,
                correlation,
                tags =>
                {
                    tags["cephalon.http.body.truncated"] = Redact(activity, "cephalon.http.body.truncated", requestBody.IsTruncated);
                    tags["http.request.body.content_type"] = Redact(activity, "http.request.body.content_type", requestBody.ContentType);
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
            LogResponseCompleted(context, correlation, redactedQueryString, elapsedMilliseconds, responseCapture);
            AddLogReferenceEvent(
                activity,
                "cephalon.http.response.completed",
                AspNetCoreDiagnosticsConventions.HttpResponseCompleted.Id,
                correlation,
                tags =>
                {
                    tags["http.response.status_code"] = Redact(activity, "http.response.status_code", context.Response.StatusCode);
                    tags["cephalon.http.elapsed_ms"] = Redact(activity, "cephalon.http.elapsed_ms", elapsedMilliseconds);
                });

            var responseBody = RedactBodyCaptureResult(
                responseCapture?.CreateResult(context.Response.ContentType) ?? BodyCaptureResult.None,
                options);
            if (responseBody is { ShouldLog: true })
            {
                LogResponseBody(context, correlation, redactedQueryString, responseBody);
                AddLogReferenceEvent(
                    activity,
                    "cephalon.http.response.body.logged",
                    AspNetCoreDiagnosticsConventions.HttpResponseBodyCaptured.Id,
                    correlation,
                    tags =>
                    {
                        tags["cephalon.http.body.truncated"] = Redact(activity, "cephalon.http.body.truncated", responseBody.IsTruncated);
                        tags["http.response.body.content_type"] = Redact(activity, "http.response.body.content_type", responseBody.ContentType);
                    });
            }
        }
        catch (Exception exception)
        {
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            LogRequestFailed(context, correlation, redactedQueryString, elapsedMilliseconds, exception);
            AddLogReferenceEvent(
                activity,
                "cephalon.http.request.failed",
                AspNetCoreDiagnosticsConventions.HttpRequestFailed.Id,
                correlation,
                tags =>
                {
                    tags["http.response.status_code"] = Redact(activity, "http.response.status_code", context.Response.StatusCode);
                    tags["cephalon.http.elapsed_ms"] = Redact(activity, "cephalon.http.elapsed_ms", elapsedMilliseconds);
                    tags["exception.type"] = Redact(activity, "exception.type", exception.GetType().FullName);
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

    private void LogRequestStarted(HttpContext context, RequestCorrelation correlation, string? queryString)
    {
        HttpRequestResponseLoggingLogs.RequestStarted(
            logger,
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            queryString,
            correlation.RequestId,
            correlation.TraceId,
            correlation.SpanId,
            context.Request.ContentType,
            correlation.TraceParent,
            context.Request.ContentLength);
    }

    private void LogRequestBody(
        HttpContext context,
        RequestCorrelation correlation,
        string? queryString,
        BodyCaptureResult requestBody)
    {
        HttpRequestResponseLoggingLogs.RequestBodyCaptured(
            logger,
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            queryString,
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
        string? queryString,
        double elapsedMilliseconds,
        LimitedBodyCaptureStream? responseCapture)
    {
        HttpRequestResponseLoggingLogs.ResponseCompleted(
            logger,
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            queryString,
            context.Response.StatusCode,
            elapsedMilliseconds,
            correlation.RequestId,
            correlation.TraceId,
            correlation.SpanId,
            context.Response.ContentType,
            context.Response.ContentLength ?? responseCapture?.TotalBytesWritten);
    }

    private void LogResponseBody(
        HttpContext context,
        RequestCorrelation correlation,
        string? queryString,
        BodyCaptureResult responseBody)
    {
        HttpRequestResponseLoggingLogs.ResponseBodyCaptured(
            logger,
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            queryString,
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
        string? queryString,
        double elapsedMilliseconds,
        Exception exception)
    {
        HttpRequestResponseLoggingLogs.RequestFailed(
            logger,
            exception,
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            queryString,
            elapsedMilliseconds,
            correlation.RequestId,
            correlation.TraceId,
            correlation.SpanId,
            context.Response.StatusCode);
    }

    private void ApplyCorrelationToActivity(Activity? activity, RequestCorrelation correlation)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("cephalon.http.request_id", Redact(activity, "cephalon.http.request_id", correlation.RequestId));

        if (!string.IsNullOrWhiteSpace(correlation.TraceParent))
        {
            activity.SetTag("cephalon.http.traceparent", Redact(activity, "cephalon.http.traceparent", correlation.TraceParent));
        }
    }

    private void AddLogReferenceEvent(
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
            ["cephalon.log.event_id"] = Redact(activity, "cephalon.log.event_id", eventId),
            ["cephalon.http.request_id"] = Redact(activity, "cephalon.http.request_id", correlation.RequestId),
        };

        if (!string.IsNullOrWhiteSpace(correlation.TraceParent))
        {
            tags["cephalon.http.traceparent"] = Redact(activity, "cephalon.http.traceparent", correlation.TraceParent);
        }

        configureTags?.Invoke(tags);
        activity.AddEvent(new ActivityEvent(name, tags: tags));
    }

    /// <summary>
    /// Routes <paramref name="value"/> through the redaction pipeline before it reaches the
    /// activity span / log event. The pipeline is empty by default (when no consumer registered
    /// any <see cref="IRedactionFilter"/>) and short-circuits to passthrough; consumer apps that
    /// register filters via DI redact every value emitted from this middleware in one place.
    /// </summary>
    private object? Redact(Activity? activity, string attributeKey, object? value)
    {
        var context = new RedactionContext(
            ActivitySourceName: activity?.Source.Name,
            MeterName: null,
            AttributeKey: attributeKey,
            LoggerCategory: null);
        return redactionPipeline.Filter(context, value);
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
        var captureLength = Math.Max(1, byteLimit + 1);
        var captureBuffer = ArrayPool<byte>.Shared.Rent(captureLength);
        var capturedBytes = 0;

        try
        {
            while (capturedBytes < captureLength)
            {
                var read = await stream.ReadAsync(
                    captureBuffer.AsMemory(capturedBytes, captureLength - capturedBytes),
                    cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                capturedBytes += read;
            }

            if (capturedBytes == 0)
            {
                return BodyCaptureResult.Empty(contentType);
            }

            return CreateBodyCaptureResult(captureBuffer, capturedBytes, contentType, byteLimit);
        }
        finally
        {
            Array.Clear(captureBuffer, 0, capturedBytes);
            ArrayPool<byte>.Shared.Return(captureBuffer);
        }
    }

    private static BodyCaptureResult CreateBodyCaptureResult(
        byte[] buffer,
        int capturedBytes,
        string? contentType,
        int bodyLimit)
    {
        var truncated = capturedBytes > bodyLimit;
        var effectiveLength = truncated ? bodyLimit : capturedBytes;
        var encoding = ResolveEncoding(contentType);
        var text = effectiveLength == 0
            ? string.Empty
            : encoding.GetString(buffer, 0, effectiveLength);

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

    private static BodyCaptureResult RedactBodyCaptureResult(
        BodyCaptureResult captureResult,
        HttpRequestResponseLoggingOptions options)
    {
        if (!captureResult.ShouldLog)
        {
            return captureResult;
        }

        var redactedBody = RedactText(captureResult.Body, captureResult.ContentType, options);
        return string.Equals(redactedBody, captureResult.Body, StringComparison.Ordinal)
            ? captureResult
            : new BodyCaptureResult(redactedBody, captureResult.ContentType, captureResult.IsTruncated);
    }

    private static string? RedactQueryString(string? queryString, HttpRequestResponseLoggingOptions options)
    {
        if (string.IsNullOrWhiteSpace(queryString) || !options.RedactSensitiveValues)
        {
            return queryString;
        }

        return RedactDelimitedKeyValuePairs(queryString, separator: '&', hasLeadingQuestionMark: true, options);
    }

    private static string RedactText(
        string body,
        string? contentType,
        HttpRequestResponseLoggingOptions options)
    {
        if (string.IsNullOrEmpty(body) || !options.RedactSensitiveValues)
        {
            return body;
        }

        if (LooksLikeJson(contentType, body))
        {
            return ContainsSensitiveJsonPropertyName(body, options)
                ? TryRedactJson(body, options) ?? body
                : body;
        }

        if (LooksLikeFormUrlEncoded(contentType, body))
        {
            return RedactDelimitedKeyValuePairs(body, separator: '&', hasLeadingQuestionMark: false, options);
        }

        return TryRedactPlainTextKeyValuePairs(body, options) ?? body;
    }

    private static bool LooksLikeJson(string? contentType, string body)
    {
        if (!string.IsNullOrWhiteSpace(contentType) &&
            contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var character in body)
        {
            if (!char.IsWhiteSpace(character))
            {
                return character is '{' or '[';
            }
        }

        return false;
    }

    private static bool ContainsSensitiveJsonPropertyName(string body, HttpRequestResponseLoggingOptions options)
    {
        for (var index = 0; index < body.Length; index++)
        {
            if (body[index] != '"')
            {
                continue;
            }

            var propertyStart = index + 1;
            var cursor = propertyStart;

            while (cursor < body.Length)
            {
                if (body[cursor] == '\\')
                {
                    cursor += 2;
                    continue;
                }

                if (body[cursor] == '"')
                {
                    break;
                }

                cursor++;
            }

            if (cursor >= body.Length)
            {
                return false;
            }

            var lookAhead = cursor + 1;
            while (lookAhead < body.Length && char.IsWhiteSpace(body[lookAhead]))
            {
                lookAhead++;
            }

            if (lookAhead < body.Length &&
                body[lookAhead] == ':' &&
                IsSensitiveFieldName(body[propertyStart..cursor], options))
            {
                return true;
            }

            index = cursor;
        }

        return false;
    }

    private static bool LooksLikeFormUrlEncoded(string? contentType, string body)
    {
        return (!string.IsNullOrWhiteSpace(contentType) &&
                contentType.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)) ||
            (body.Contains('=') && body.Contains('&'));
    }

    private static string? TryRedactPlainTextKeyValuePairs(string text, HttpRequestResponseLoggingOptions options)
    {
        if (text.IndexOfAny(':', '=') < 0)
        {
            return null;
        }

        StringBuilder? builder = null;
        var copiedUntil = 0;
        var position = 0;

        while (position < text.Length)
        {
            var lineStart = position;
            while (position < text.Length && text[position] is not '\r' and not '\n')
            {
                position++;
            }

            var redactedLine = TryRedactPlainTextLine(text.AsSpan(lineStart, position - lineStart), options);
            if (redactedLine is not null)
            {
                builder ??= new StringBuilder(text.Length + Math.Max(options.RedactionValue.Length, 16));
                builder.Append(text.AsSpan(copiedUntil, lineStart - copiedUntil));
                builder.Append(redactedLine);
                copiedUntil = position;
            }

            if (position < text.Length)
            {
                position++;
                if (position < text.Length && text[position - 1] == '\r' && text[position] == '\n')
                {
                    position++;
                }
            }
        }

        if (builder is null)
        {
            return null;
        }

        builder.Append(text.AsSpan(copiedUntil));
        return builder.ToString();
    }

    private static string? TryRedactPlainTextLine(ReadOnlySpan<char> line, HttpRequestResponseLoggingOptions options)
    {
        if (line.IsEmpty)
        {
            return null;
        }

        var colonIndex = line.IndexOf(':');
        var equalsIndex = line.IndexOf('=');
        var delimiterIndex = colonIndex switch
        {
            < 0 => equalsIndex,
            _ when equalsIndex < 0 => colonIndex,
            _ => Math.Min(colonIndex, equalsIndex)
        };

        if (delimiterIndex <= 0)
        {
            return null;
        }

        var keyStart = 0;
        while (keyStart < delimiterIndex && char.IsWhiteSpace(line[keyStart]))
        {
            keyStart++;
        }

        var keyEnd = delimiterIndex;
        while (keyEnd > keyStart && char.IsWhiteSpace(line[keyEnd - 1]))
        {
            keyEnd--;
        }

        if (keyEnd <= keyStart || !IsSensitiveFieldName(line[keyStart..keyEnd], options))
        {
            return null;
        }

        var valueStart = delimiterIndex + 1;
        while (valueStart < line.Length && char.IsWhiteSpace(line[valueStart]))
        {
            valueStart++;
        }

        if (valueStart >= line.Length)
        {
            return null;
        }

        var valueEnd = line.Length;
        while (valueEnd > valueStart && char.IsWhiteSpace(line[valueEnd - 1]))
        {
            valueEnd--;
        }

        return string.Concat(line[..valueStart], options.RedactionValue.AsSpan(), line[valueEnd..]);
    }

    private static string? TryRedactJson(string body, HttpRequestResponseLoggingOptions options)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var buffer = new ArrayBufferWriter<byte>(Encoding.UTF8.GetByteCount(body));
            using var writer = new Utf8JsonWriter(buffer);
            WriteRedactedJsonElement(writer, document.RootElement, options);
            writer.Flush();
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void WriteRedactedJsonElement(
        Utf8JsonWriter writer,
        JsonElement element,
        HttpRequestResponseLoggingOptions options)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    if (IsSensitiveFieldName(property.Name, options))
                    {
                        writer.WriteStringValue(options.RedactionValue);
                    }
                    else
                    {
                        WriteRedactedJsonElement(writer, property.Value, options);
                    }
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteRedactedJsonElement(writer, item, options);
                }

                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static string RedactDelimitedKeyValuePairs(
        string text,
        char separator,
        bool hasLeadingQuestionMark,
        HttpRequestResponseLoggingOptions options)
    {
        var offset = hasLeadingQuestionMark && text.Length > 0 && text[0] == '?' ? 1 : 0;
        var prefix = offset == 1 ? "?" : string.Empty;
        var segments = text[offset..].Split(separator);
        var changed = false;

        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            if (string.IsNullOrEmpty(segment))
            {
                continue;
            }

            var delimiterIndex = segment.IndexOf('=');
            var rawKey = delimiterIndex >= 0 ? segment[..delimiterIndex] : segment;
            var decodedKey = WebUtility.UrlDecode(rawKey.Replace('+', ' '));
            if (!IsSensitiveFieldName(decodedKey, options))
            {
                continue;
            }

            segments[index] = delimiterIndex >= 0
                ? $"{rawKey}={options.RedactionValue}"
                : rawKey;
            changed = true;
        }

        return changed ? prefix + string.Join(separator, segments) : text;
    }

    private static bool IsSensitiveFieldName(string? fieldName, HttpRequestResponseLoggingOptions options)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return false;
        }

        var normalizedFieldName = fieldName.Trim();
        return options.RedactedFieldNames.Any(candidate =>
            string.Equals(candidate, normalizedFieldName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSensitiveFieldName(ReadOnlySpan<char> fieldName, HttpRequestResponseLoggingOptions options)
    {
        var normalizedFieldName = fieldName.Trim();
        if (normalizedFieldName.IsEmpty)
        {
            return false;
        }

        foreach (var candidate in options.RedactedFieldNames)
        {
            if (normalizedFieldName.Equals(candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
        private readonly int bodyLimit = Math.Max(0, bodyLimit);
        private readonly byte[] captureBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(1, Math.Max(0, bodyLimit) + 1));
        private readonly int captureLimit = Math.Max(1, Math.Max(0, bodyLimit) + 1);
        private int capturedBytes;
        private bool disposed;

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

            if (capturedBytes == 0)
            {
                return BodyCaptureResult.Empty(contentType);
            }

            return CreateBodyCaptureResult(captureBuffer, capturedBytes, contentType, bodyLimit);
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
            if (disposing && !disposed)
            {
                Array.Clear(captureBuffer, 0, capturedBytes);
                ArrayPool<byte>.Shared.Return(captureBuffer);
                disposed = true;
            }

            base.Dispose(disposing);
        }

        private void Capture(ReadOnlySpan<byte> buffer)
        {
            TotalBytesWritten += buffer.Length;
            if (capturedBytes >= captureLimit)
            {
                return;
            }

            var remaining = captureLimit - capturedBytes;
            if (remaining <= 0)
            {
                return;
            }

            var length = Math.Min(buffer.Length, remaining);
            if (length > 0)
            {
                buffer[..length].CopyTo(captureBuffer.AsSpan(capturedBytes));
                capturedBytes += length;
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
