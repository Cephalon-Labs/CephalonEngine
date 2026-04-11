using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

public sealed class BehaviorResilienceRestHostingTests
{
    [Fact]
    public async Task BehaviorRestTimeoutsReturn503AndOpenApiDocuments503WhenExecutionTimeoutIsEnabled()
    {
        const string route = "/api/v1/tests/resilience/timeout/tasks/alpha";
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["ApiRoutes:ResultEnvelope:Enabled"] = "true";
        builder.Configuration["Engine:Resilience:Timeout:Enabled"] = "true";
        builder.Configuration["Engine:Resilience:Timeout:TotalTimeoutSeconds"] = "1";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new TimeoutRestModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync(route);
        var payload = await response.Content.ReadFromJsonAsync<ResultModelError>();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var timeoutOperation = document.RootElement
            .GetProperty("paths")
            .EnumerateObject()
            .Single(static path =>
                path.Name.EndsWith("/tests/resilience/timeout/tasks/{taskId}", StringComparison.Ordinal))
            .Value
            .GetProperty("get");
        var responses = timeoutOperation.GetProperty("responses");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.Equal(503, payload.StatusCode);
        Assert.NotNull(payload.Errors);
        Assert.Single(payload.Errors!);
        Assert.Equal("behavior_execution_timeout", payload.Errors[0].Key);
        Assert.Contains("timeout", payload.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(responses.TryGetProperty("503", out _));
    }

    [Fact]
    public async Task BehaviorRestBulkheadRejectsWith429AndOpenApiDocuments429WhenExecutionBulkheadIsEnabled()
    {
        const string route = "/api/v1/tests/resilience/bulkhead/jobs";

        BulkheadProbe.Reset();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["Engine:Resilience:Bulkhead:Enabled"] = "true";
        builder.Configuration["Engine:Resilience:Bulkhead:MaxConcurrentExecutions"] = "1";
        builder.Configuration["Engine:Resilience:Bulkhead:MaxQueuedActions"] = "0";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new BulkheadRestModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var firstRequest = client.PostAsJsonAsync($"{route}/job-1", new { value = "alpha" });
        await BulkheadProbe.WaitUntilStartedAsync();

        var rejectedResponse = await client.PostAsJsonAsync($"{route}/job-2", new { value = "beta" });
        var rejectedPayload = await rejectedResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        BulkheadProbe.Release();

        var firstResponse = await firstRequest;
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var bulkheadOperation = document.RootElement
            .GetProperty("paths")
            .EnumerateObject()
            .Single(static path =>
                path.Name.EndsWith("/tests/resilience/bulkhead/jobs/{jobId}", StringComparison.Ordinal))
            .Value
            .GetProperty("post");
        var responses = bulkheadOperation.GetProperty("responses");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        Assert.NotNull(rejectedPayload);
        Assert.Equal(429, rejectedPayload!.Status);
        Assert.Contains("concurrency", rejectedPayload.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.True(responses.TryGetProperty("429", out _));
    }

    private sealed record TimeoutInput(string TaskId);

    private sealed record TimeoutOutput(string TaskId, string Status);

    [AppBehavior("tests.resilience.timeout")]
    private sealed class TimeoutBehavior : IAppBehavior<TimeoutInput, TimeoutOutput>
    {
        public async Task<TimeoutOutput> HandleAsync(
            TimeoutInput input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            return new TimeoutOutput(input.TaskId, "done");
        }
    }

    private sealed record BulkheadInput(string JobId, string Value);

    private sealed record BulkheadOutput(string JobId, string Value);

    [AppBehavior("tests.resilience.bulkhead")]
    private sealed class BulkheadBehavior : IAppBehavior<BulkheadInput, BulkheadOutput>
    {
        public async Task<BulkheadOutput> HandleAsync(
            BulkheadInput input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            BulkheadProbe.MarkStarted();
            await BulkheadProbe.WaitForReleaseAsync(cancellationToken);
            return new BulkheadOutput(input.JobId, input.Value);
        }
    }

    private sealed class TimeoutRestModule : RestBehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.resilience.timeout",
            displayName: "Timeout Resilience",
            description: "Test module for behavior execution timeout translation.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            var group = behaviors.Group("/tests/resilience/timeout/tasks");
            group.MapGet<TimeoutBehavior>("/{taskId}");
        }
    }

    private sealed class BulkheadRestModule : RestBehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.resilience.bulkhead",
            displayName: "Bulkhead Resilience",
            description: "Test module for behavior execution bulkhead translation.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            var group = behaviors.Group("/tests/resilience/bulkhead/jobs");
            group.MapPost<BulkheadBehavior>("/{jobId}");
        }
    }

    private static class BulkheadProbe
    {
        private static TaskCompletionSource<bool> releaseSignal = CreateCompletionSource();
        private static TaskCompletionSource<bool> startedSignal = CreateCompletionSource();

        public static void Reset()
        {
            releaseSignal = CreateCompletionSource();
            startedSignal = CreateCompletionSource();
        }

        public static void MarkStarted()
        {
            startedSignal.TrySetResult(true);
        }

        public static Task<bool> WaitUntilStartedAsync()
        {
            return startedSignal.Task;
        }

        public static Task<bool> WaitForReleaseAsync(CancellationToken cancellationToken)
        {
            return releaseSignal.Task.WaitAsync(cancellationToken);
        }

        public static void Release()
        {
            releaseSignal.TrySetResult(true);
        }

        private static TaskCompletionSource<bool> CreateCompletionSource()
        {
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
