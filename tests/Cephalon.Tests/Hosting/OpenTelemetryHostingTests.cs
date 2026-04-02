using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Cephalon.Engine.Configuration;
using Cephalon.Observability.Hosting;
using Cephalon.Observability.OpenTelemetry.Hosting;
using Cephalon.Tests.Support;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cephalon.Tests.Hosting;

public sealed class OpenTelemetryHostingTests
{
    [Fact]
    public async Task AddCephalonOpenTelemetryExportsConfiguredSignalsOverHttpProtobuf()
    {
        await using var collector = new OtlpHttpCaptureServer("/collector");

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "OpenTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp/http";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = collector.Endpoint;
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportLogs"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportMetrics"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:ExportTraces"] = "true";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new DiscoveryTestModule());
        });
        builder.Services.AddCephalonObservability(builder.Configuration);
        builder.AddCephalonOpenTelemetry();

        using var host = builder.Build();

        await host.StartAsync();
        host.Services.GetService<TracerProvider>()?.ForceFlush();
        host.Services.GetService<MeterProvider>()?.ForceFlush();
        await host.StopAsync();

        var exported = await collector.WaitForRequestsAsync(
            "/collector/v1/logs",
            "/collector/v1/metrics",
            "/collector/v1/traces");

        Assert.True(
            exported,
            $"Expected OTLP HTTP requests for logs, metrics, and traces but captured: {string.Join(", ", collector.RequestPaths)}");
    }

    [Fact]
    public void AddCephalonOpenTelemetryRejectsUnsupportedProviders()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Provider"] = "CustomTelemetry";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Protocol"] = "otlp";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:Telemetry:Endpoint"] = "http://127.0.0.1:4317";

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCephalonOpenTelemetry());

        Assert.Contains("OpenTelemetry", exception.Message, StringComparison.Ordinal);
        Assert.Contains("CustomTelemetry", exception.Message, StringComparison.Ordinal);
    }

    private sealed class OtlpHttpCaptureServer : IAsyncDisposable
    {
        private readonly HttpListener listener = new();
        private readonly CancellationTokenSource cancellationSource = new();
        private readonly ConcurrentQueue<string> requestPaths = new();
        private readonly Task listenLoop;

        public OtlpHttpCaptureServer(string endpointPath)
        {
            var port = ReservePort();
            var normalizedPath = endpointPath.Trim('/');
            Endpoint = $"http://127.0.0.1:{port}/{normalizedPath}";
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();
            listenLoop = Task.Run(ListenAsync);
        }

        public string Endpoint { get; }

        public IReadOnlyList<string> RequestPaths => requestPaths.ToArray();

        public async Task<bool> WaitForRequestsAsync(params string[] expectedPaths)
        {
            var deadline = DateTimeOffset.UtcNow.AddSeconds(20);

            while (DateTimeOffset.UtcNow < deadline)
            {
                var captured = RequestPaths;
                if (expectedPaths.All(expectedPath => captured.Any(path =>
                        path.EndsWith(expectedPath, StringComparison.OrdinalIgnoreCase))))
                {
                    return true;
                }

                await Task.Delay(250);
            }

            return false;
        }

        public async ValueTask DisposeAsync()
        {
            cancellationSource.Cancel();
            listener.Stop();
            listener.Close();

            try
            {
                await listenLoop;
            }
            catch (OperationCanceledException)
            {
            }
            catch (HttpListenerException)
            {
            }

            cancellationSource.Dispose();
        }

        private async Task ListenAsync()
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync();
                }
                catch (HttpListenerException) when (cancellationSource.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationSource.IsCancellationRequested)
                {
                    break;
                }

                requestPaths.Enqueue(context.Request.RawUrl ?? string.Empty);

                await using (context.Request.InputStream)
                {
                    await context.Request.InputStream.CopyToAsync(Stream.Null, cancellationSource.Token);
                }

                context.Response.StatusCode = 200;
                context.Response.Close();
            }
        }

        private static int ReservePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
