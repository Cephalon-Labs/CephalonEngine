using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cephalon.Tests.Composition;

public sealed class RuntimeHealthEvaluatorTests
{
    [Fact]
    public async Task EvaluateReadinessCapturesContributorFailuresAsDependencyReports()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCephalon(engine =>
        {
            engine.AddModule(new ThrowingDependencyHealthModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var health = provider.GetRequiredService<RuntimeHealthEvaluator>();

        await runtime.StartAsync(provider);

        var readiness = health.EvaluateReadiness();

        Assert.Equal(RuntimeHealthState.Unhealthy, readiness.State);
        Assert.Single(readiness.Dependencies);
        Assert.Contains("failed", readiness.Dependencies[0].Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ThrowingDependencyHealthContributor", readiness.Dependencies[0].Id);
    }

    [Fact]
    public async Task EvaluateReadinessHonorsConfiguredStartupWarmupWindow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<FailurePolicyRecorder>();
        services.AddCephalon(engine =>
        {
            engine.UseFailurePolicy(new FailurePolicy(
                startupReadinessDelay: TimeSpan.FromMilliseconds(150)));
            engine.AddModule(new FailurePolicyPlatformModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var health = provider.GetRequiredService<RuntimeHealthEvaluator>();

        await runtime.StartAsync(provider);

        var warmingReadiness = health.EvaluateReadiness();

        Assert.Equal(RuntimeHealthState.Unhealthy, warmingReadiness.State);
        Assert.Equal("startup-warmup", warmingReadiness.ActiveWindow);
        Assert.NotNull(warmingReadiness.ActiveWindowEndsAtUtc);

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        var readyReadiness = health.EvaluateReadiness();

        Assert.Equal(RuntimeHealthState.Healthy, readyReadiness.State);
        Assert.Null(readyReadiness.ActiveWindow);
        Assert.Null(readyReadiness.ActiveWindowEndsAtUtc);
    }

    [Fact]
    public async Task EvaluateLivenessHonorsConfiguredShutdownDrainWindow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<FailurePolicyRecorder>();
        services.AddCephalon(engine =>
        {
            engine.UseFailurePolicy(new FailurePolicy(
                shutdownLivenessGracePeriod: TimeSpan.FromMilliseconds(75)));
            engine.AddModule(new FailurePolicyPlatformModule());
            engine.AddModule(new SlowStopModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var health = provider.GetRequiredService<RuntimeHealthEvaluator>();

        await runtime.StartAsync(provider);

        var stopTask = runtime.StopAsync();
        await WaitForStatusAsync(runtime, RuntimeStatus.Stopping);

        var drainingLiveness = health.EvaluateLiveness();

        Assert.Equal(RuntimeHealthState.Healthy, drainingLiveness.State);
        Assert.Equal("shutdown-drain", drainingLiveness.ActiveWindow);
        Assert.NotNull(runtime.StatusSnapshot.StoppingAtUtc);

        await Task.Delay(TimeSpan.FromMilliseconds(125));

        var expiredDrainLiveness = health.EvaluateLiveness();

        Assert.Equal(RuntimeHealthState.Unhealthy, expiredDrainLiveness.State);
        Assert.Equal("shutdown-drain", expiredDrainLiveness.ActiveWindow);
        Assert.NotNull(expiredDrainLiveness.ActiveWindowEndsAtUtc);

        await stopTask;
    }

    [Fact]
    public async Task RestartAsyncHonorsConfiguredManualRestartBackoff()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<FailurePolicyRecorder>();
        services.AddCephalon(engine =>
        {
            engine.UseFailurePolicy(new FailurePolicy(
                startupFailureBehavior: StartupFailureBehavior.CaptureOnly,
                allowManualRestart: true,
                manualRestartBackoff: TimeSpan.FromMilliseconds(150)));
            engine.AddModule(new FailurePolicyPlatformModule());
            engine.AddModule(new FlakyStartModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var health = provider.GetRequiredService<RuntimeHealthEvaluator>();

        await runtime.StartAsync(provider);

        Assert.Equal(RuntimeStatus.Failed, runtime.Status);
        Assert.True(runtime.LastFailure?.CanRestart);
        Assert.NotNull(runtime.LastFailure?.RestartAvailableAtUtc);

        var failedLiveness = health.EvaluateLiveness();
        Assert.Equal("restart-backoff", failedLiveness.ActiveWindow);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => runtime.RestartAsync(provider));
        Assert.Contains("backed off until", exception.Message, StringComparison.Ordinal);

        await Task.Delay(TimeSpan.FromMilliseconds(200));
        await runtime.RestartAsync(provider);

        Assert.Equal(RuntimeStatus.Started, runtime.Status);
        Assert.Null(runtime.LastFailure);
    }

    private static async Task WaitForStatusAsync(
        IRuntime runtime,
        RuntimeStatus expectedStatus,
        int timeoutMilliseconds = 1_000)
    {
        var startedAt = DateTimeOffset.UtcNow;

        while (runtime.Status != expectedStatus)
        {
            if (DateTimeOffset.UtcNow - startedAt > TimeSpan.FromMilliseconds(timeoutMilliseconds))
            {
                throw new TimeoutException(
                    $"Runtime did not reach status '{expectedStatus}' within {timeoutMilliseconds}ms.");
            }

            await Task.Delay(10);
        }
    }
}
