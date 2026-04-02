using System.Collections.Concurrent;
using Cephalon.Observability.Serilog.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Cephalon.Tests.Hosting;

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
