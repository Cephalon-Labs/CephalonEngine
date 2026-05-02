using System.Diagnostics;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Diagnostics.Redaction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Tests.Diagnostics.Redaction;

public sealed class HttpRequestResponseLoggingMiddlewareRedactionTests
{
    [Fact]
    public async Task Middleware_RoutesEmittedAttributeValues_ThroughRedactionPipeline()
    {
        var observed = new List<(string AttributeKey, object? Value)>();
        var trackingFilter = new TrackingRedactionFilter(observed);
        var pipeline = new RedactionPipeline([trackingFilter]);

        var middleware = new HttpRequestResponseLoggingMiddleware(
            next: static _ =>
            {
                _.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            options: new HttpRequestResponseLoggingOptions { Enabled = true, LogRequestBody = false, LogResponseBody = false },
            logger: NullLogger<HttpRequestResponseLoggingMiddleware>.Instance,
            redactionPipeline: pipeline);

        using var listener = new ActivityListener
        {
            ShouldListenTo = static _ => true,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        var activitySource = new ActivitySource("Cephalon.Tests.AspNetCore");
        using var activity = activitySource.StartActivity("test.request");
        Assert.NotNull(activity);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/things/42";

        await middleware.InvokeAsync(context);

        var keys = observed.Select(o => o.AttributeKey).ToHashSet();

        Assert.Contains("cephalon.http.request_id", keys);
        Assert.Contains("http.request.method", keys);
        Assert.Contains("url.path", keys);
        Assert.Contains("http.response.status_code", keys);
        Assert.Contains("cephalon.http.elapsed_ms", keys);
        Assert.Contains("cephalon.log.event_id", keys);

        var methodEntry = observed.First(o => o.AttributeKey == "http.request.method");
        Assert.Equal("GET", methodEntry.Value);

        var pathEntry = observed.First(o => o.AttributeKey == "url.path");
        Assert.Equal("/api/things/42", pathEntry.Value);
    }

    [Fact]
    public async Task Middleware_AppliesRedactionReplacement_BeforeTaggingActivity()
    {
        var pipeline = new RedactionPipeline([new ReplaceUrlPathFilter("[REDACTED-PATH]")]);

        var middleware = new HttpRequestResponseLoggingMiddleware(
            next: static _ =>
            {
                _.Response.StatusCode = 200;
                return Task.CompletedTask;
            },
            options: new HttpRequestResponseLoggingOptions { Enabled = true, LogRequestBody = false, LogResponseBody = false },
            logger: NullLogger<HttpRequestResponseLoggingMiddleware>.Instance,
            redactionPipeline: pipeline);

        using var listener = new ActivityListener
        {
            ShouldListenTo = static _ => true,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        var activitySource = new ActivitySource("Cephalon.Tests.AspNetCore");
        using var activity = activitySource.StartActivity("test.request");
        Assert.NotNull(activity);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/customers/12345/secret";

        await middleware.InvokeAsync(context);

        var requestStartedEvent = activity.Events.FirstOrDefault(e => e.Name == "cephalon.http.request.started");
        Assert.NotEqual(default, requestStartedEvent);

        var urlPathTag = requestStartedEvent.Tags.FirstOrDefault(t => t.Key == "url.path");
        Assert.Equal("[REDACTED-PATH]", urlPathTag.Value);
    }

    private sealed class TrackingRedactionFilter : IRedactionFilter
    {
        private readonly List<(string, object?)> sink;

        public TrackingRedactionFilter(List<(string, object?)> sink) => this.sink = sink;

        public object? Filter(RedactionContext context, object? value)
        {
            sink.Add((context.AttributeKey, value));
            return value;
        }
    }

    private sealed class ReplaceUrlPathFilter : IRedactionFilter
    {
        private readonly string replacement;

        public ReplaceUrlPathFilter(string replacement) => this.replacement = replacement;

        public object? Filter(RedactionContext context, object? value)
            => context.AttributeKey == "url.path" ? replacement : value;
    }
}
