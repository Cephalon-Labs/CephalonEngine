using System.Collections.Concurrent;
using Cephalon.Abstractions.Technologies;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Observability.Serilog.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Cephalon.Tests.Support;

namespace Cephalon.Tests.Hosting;

[Collection(SerilogHostingCollectionDefinition.Name)]
public sealed class SerilogHostingTests
{
    private static readonly Action<ILogger, string, Exception?> LogBlueprintMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            eventId: new EventId(3600, nameof(LogBlueprintMessage)),
            formatString: "Host started for {Blueprint}");

    private static readonly Action<ILogger, int, Exception?> LogIgnoredAttemptMessage =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            eventId: new EventId(3601, nameof(LogIgnoredAttemptMessage)),
            formatString: "Ignored {Attempt}");

    private static readonly Action<ILogger, int, Exception?> LogCapturedAttemptMessage =
        LoggerMessage.Define<int>(
            LogLevel.Error,
            eventId: new EventId(3602, nameof(LogCapturedAttemptMessage)),
            formatString: "Captured {Attempt}");

    private static readonly Action<ILogger, string, Exception?> LogTransportMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            eventId: new EventId(3603, nameof(LogTransportMessage)),
            formatString: "ASP.NET Core host for {Transport}");

    private static readonly Action<ILogger, Exception?> LogEndpointDetailMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            eventId: new EventId(3604, nameof(LogEndpointDetailMessage)),
            formatString: "Endpoint detail emitted");

    [Fact]
    public void AddCephalonSerilogDoesNothingWhenNoConfigurationOrCodeOverridesAreProvided()
    {
        var builder = Host.CreateApplicationBuilder();
        var descriptorCount = builder.Services.Count;

        var returned = builder.AddCephalonSerilog();

        Assert.Same(builder, returned);
        Assert.Equal(descriptorCount, builder.Services.Count);
    }

    [Fact]
    public void AddCephalonSerilogRoutesStructuredILoggerEventsThroughTheSharedPipeline()
    {
        var sink = new TestSerilogSink();
        var builder = Host.CreateApplicationBuilder();
        builder.AddCephalonSerilog((_, loggerConfiguration) => loggerConfiguration
            .MinimumLevel.Information()
            .WriteTo.Sink(sink));

        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<SerilogHostingTests>>();

        LogBlueprintMessage(logger, "ModularMonolith", null);

        var entry = Assert.Single(sink.Events);
        Assert.Equal(LogEventLevel.Information, entry.Level);
        Assert.Equal("Host started for {Blueprint}", entry.MessageTemplate.Text);

        var blueprint = Assert.IsType<ScalarValue>(entry.Properties["Blueprint"]);
        Assert.Equal("ModularMonolith", blueprint.Value);
    }

    [Fact]
    public void AddCephalonSerilogReadsTheStandardSerilogConfigurationSection()
    {
        var sink = new TestSerilogSink();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["Serilog:MinimumLevel:Default"] = "Error";
        builder.AddCephalonSerilog((_, loggerConfiguration) => loggerConfiguration.WriteTo.Sink(sink));

        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<SerilogHostingTests>>();

        LogIgnoredAttemptMessage(logger, 1, null);
        LogCapturedAttemptMessage(logger, 2, null);

        var entry = Assert.Single(sink.Events);
        Assert.Equal(LogEventLevel.Error, entry.Level);
        Assert.Equal("Captured {Attempt}", entry.MessageTemplate.Text);

        var attempt = Assert.IsType<ScalarValue>(entry.Properties["Attempt"]);
        Assert.Equal(2, attempt.Value);
    }

    [Fact]
    public void AddCephalonSerilogSupportsAspNetCoreBuilders()
    {
        var sink = new TestSerilogSink();
        var builder = WebApplication.CreateBuilder();
        builder.AddCephalonSerilog((_, loggerConfiguration) => loggerConfiguration
            .MinimumLevel.Information()
            .WriteTo.Sink(sink));

        using var app = builder.Build();
        var logger = app.Services.GetRequiredService<ILogger<SerilogHostingTests>>();

        LogTransportMessage(logger, "RestApi", null);

        var entry = Assert.Single(sink.Events);
        var transport = Assert.IsType<ScalarValue>(entry.Properties["Transport"]);
        Assert.Equal("RestApi", transport.Value);
    }

    [Fact]
    public void AddCephalonSerilogProjectsLoggingProviderRuntimeSurface()
    {
        var sink = new TestSerilogSink();
        var builder = Host.CreateApplicationBuilder();
        builder.AddCephalonSerilog((_, loggerConfiguration) => loggerConfiguration
            .MinimumLevel.Information()
            .WriteTo.Sink(sink));

        using var host = builder.Build();
        var surface = Assert.Single(
            host.Services.GetServices<ITechnologyRuntimeContributor>()
                .Select(contributor => contributor.DescribeRuntimeSurface()),
            surface => surface.SurfaceId == "logging-provider-serilog");
        var entry = Assert.Single(surface.Entries);

        Assert.Equal("observability", surface.TechnologyId);
        Assert.Equal("serilog", entry.Id);
        Assert.Equal("Cephalon.Observability.Serilog", entry.Metadata["pack"]);
        Assert.Equal("logging-provider", entry.Metadata["integrationKind"]);
        Assert.Equal("redacted", entry.Metadata["secretProjection"]);
    }

    [Fact]
    public async Task AddCephalonSerilogCarriesHttpLoggingCorrelationScopesIntoRequestLogs()
    {
        var sink = new TestSerilogSink();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["Engine:Observability:HttpLogging:Enabled"] = "true";
        builder.AddCephalonSerilog((_, loggerConfiguration) => loggerConfiguration
            .MinimumLevel.Information()
            .WriteTo.Sink(sink));
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();
        app.MapGet("/correlated-log", (ILogger<SerilogHostingTests> logger) =>
        {
            LogEndpointDetailMessage(logger, null);
            return Results.Ok(new { status = "ok" });
        });

        await app.StartAsync();
        var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/correlated-log");
        request.Headers.TryAddWithoutValidation("traceparent", "00-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-bbbbbbbbbbbbbbbb-01");

        var response = await client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
        var entry = await WaitForLogEventAsync(sink, static item => item.MessageTemplate.Text == "Endpoint detail emitted");
        var requestId = Assert.IsType<ScalarValue>(entry.Properties["RequestId"]);
        Assert.False(string.IsNullOrWhiteSpace(requestId.Value?.ToString()));
        var traceParent = Assert.IsType<ScalarValue>(entry.Properties["TraceParent"]);
        Assert.Equal("00-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-bbbbbbbbbbbbbbbb-01", traceParent.Value);
        var traceId = Assert.IsType<ScalarValue>(entry.Properties["TraceId"]);
        Assert.Equal("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", traceId.Value);
    }

    private static async Task<LogEvent> WaitForLogEventAsync(
        TestSerilogSink sink,
        Func<LogEvent, bool> predicate)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var matches = sink.Events.Where(predicate).ToArray();
            if (matches.Length == 1)
            {
                return matches[0];
            }

            if (matches.Length > 1)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Expected one Serilog event matching the predicate, but found {matches.Length}.");
            }

            try
            {
                await Task.Delay(50, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException("Timed out while waiting for the expected Serilog event.");
    }

    private sealed class TestSerilogSink : ILogEventSink
    {
        private readonly ConcurrentQueue<LogEvent> events = new();

        public IReadOnlyList<LogEvent> Events => events.ToArray();

        public void Emit(LogEvent logEvent)
        {
            events.Enqueue(logEvent);
        }
    }
}
