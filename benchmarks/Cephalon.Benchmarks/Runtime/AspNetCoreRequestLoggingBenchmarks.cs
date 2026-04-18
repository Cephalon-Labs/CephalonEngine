using System.Net.Http.Headers;
using System.Text;
using BenchmarkDotNet.Attributes;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Benchmarks.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

namespace Cephalon.Benchmarks.Runtime;

/// <summary>
/// Measures ASP.NET Core request logging overhead for the shipped correlated request/response path.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class AspNetCoreRequestLoggingBenchmarks
{
    private const int RequestsPerIteration = 256;
    private const int ConcurrentRequestBatchSize = 32;
    private static readonly byte[] RequestBodyBytes = """{"message":"hello","source":"benchmark"}"""u8.ToArray();
    private static readonly byte[] LargeRequestBodyBytes = Encoding.UTF8.GetBytes(
        $$"""{"message":"{{new string('x', 8192)}}","source":"benchmark","mode":"truncated"}""");
    private const string TraceParent = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";

    private WebApplication? app;
    private HttpClient? client;

    /// <summary>
    /// Builds the prepared ASP.NET Core host once for the benchmark session.
    /// </summary>
    [GlobalSetup]
    public async Task SetupAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddProvider(new BenchmarkLoggerProvider(LogLevel.Information));
        builder.Configuration["Engine:Observability:HttpLogging:Enabled"] = "true";
        builder.Configuration["Engine:Observability:HttpLogging:LogRequestBody"] = "true";
        builder.Configuration["Engine:Observability:HttpLogging:LogResponseBody"] = "true";
        builder.Configuration["Engine:Observability:HttpLogging:RequestBodyLimit"] = "4096";
        builder.Configuration["Engine:Observability:HttpLogging:ResponseBodyLimit"] = "4096";
        builder.AddCephalon(BenchmarkScenarioFactory.ConfigureAspNetCoreEngine);

        app = builder.Build();
        app.MapCephalon();
        app.MapPost("/benchmark/echo", async context =>
        {
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var payload = await reader.ReadToEndAsync();
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(payload);
        });

        await app.StartAsync().ConfigureAwait(false);
        client = app.GetTestClient();
    }

    /// <summary>
    /// Disposes the prepared ASP.NET Core host and client captured for the benchmark session.
    /// </summary>
    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        client?.Dispose();

        if (app is not null)
        {
            await app.StopAsync().ConfigureAwait(false);
            await app.DisposeAsync().ConfigureAwait(false);
        }

        client = null;
        app = null;
    }

    /// <summary>
    /// Sends a prepared JSON request through the correlated ASP.NET Core request logging pipeline.
    /// </summary>
    /// <returns>
    /// The total number of response-body bytes returned across the measured requests.
    /// </returns>
    [Benchmark(OperationsPerInvoke = RequestsPerIteration)]
    public async Task<int> HandleLoggedJsonRequest()
    {
        return await ExecuteRequestsAsync(RequestBodyBytes, "/benchmark/echo?mode=measure").ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a prepared oversized JSON request through the correlated logging pipeline to cover bounded truncation.
    /// </summary>
    /// <returns>
    /// The total number of response-body bytes returned across the measured requests.
    /// </returns>
    [Benchmark(OperationsPerInvoke = RequestsPerIteration)]
    public async Task<int> HandleTruncatedJsonRequest()
    {
        return await ExecuteRequestsAsync(LargeRequestBodyBytes, "/benchmark/echo?mode=truncated").ConfigureAwait(false);
    }

    /// <summary>
    /// Sends prepared JSON requests through the correlated logging pipeline in concurrent batches to cover multi-request pressure.
    /// </summary>
    /// <returns>
    /// The total number of response-body bytes returned across the measured concurrent requests.
    /// </returns>
    [Benchmark(OperationsPerInvoke = RequestsPerIteration)]
    public async Task<int> HandleConcurrentLoggedJsonRequest()
    {
        return await ExecuteConcurrentRequestsAsync(
            RequestBodyBytes,
            "/benchmark/echo?mode=concurrent",
            ConcurrentRequestBatchSize).ConfigureAwait(false);
    }

    private async Task<int> ExecuteRequestsAsync(byte[] requestBodyBytes, string requestPath)
    {
        if (client is null)
        {
            throw new InvalidOperationException("Benchmark host was not initialized.");
        }

        var totalBytes = 0;

        for (var index = 0; index < RequestsPerIteration; index++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestPath);
            request.Headers.TryAddWithoutValidation("traceparent", TraceParent);

            var content = new ByteArrayContent(requestBodyBytes);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
            request.Content = content;

            using var response = await client.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            totalBytes += (await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false)).Length;
        }

        return totalBytes;
    }

    private async Task<int> ExecuteConcurrentRequestsAsync(byte[] requestBodyBytes, string requestPath, int concurrency)
    {
        if (concurrency <= 0 || RequestsPerIteration % concurrency != 0)
        {
            throw new InvalidOperationException(
                $"Concurrent request batch size must be a positive divisor of {RequestsPerIteration}.");
        }

        var totalBytes = 0;

        for (var index = 0; index < RequestsPerIteration; index += concurrency)
        {
            var requests = new Task<int>[concurrency];
            for (var batchOffset = 0; batchOffset < requests.Length; batchOffset++)
            {
                requests[batchOffset] = SendRequestAsync(requestBodyBytes, requestPath);
            }

            var responseLengths = await Task.WhenAll(requests).ConfigureAwait(false);
            for (var responseIndex = 0; responseIndex < responseLengths.Length; responseIndex++)
            {
                totalBytes += responseLengths[responseIndex];
            }
        }

        return totalBytes;
    }

    private async Task<int> SendRequestAsync(byte[] requestBodyBytes, string requestPath)
    {
        if (client is null)
        {
            throw new InvalidOperationException("Benchmark host was not initialized.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, requestPath);
        request.Headers.TryAddWithoutValidation("traceparent", TraceParent);

        var content = new ByteArrayContent(requestBodyBytes);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
        request.Content = content;

        using var response = await client.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false)).Length;
    }
}
