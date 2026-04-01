using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cephalon.Tests.Composition;

public sealed class EngineDiagnosticsTests
{
    [Fact]
    public async Task BuildAndLifecycleEmitActivitiesAndMetrics()
    {
        var sync = new object();
        var activities = new List<string>();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == EngineDiagnostics.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (sync)
                {
                    activities.Add(activity.OperationName);
                }
            }
        };
        ActivitySource.AddActivityListener(activityListener);

        var measurements = new List<string>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == EngineDiagnostics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            lock (sync)
            {
                measurements.Add(instrument.Name);
            }
        });
        meterListener.Start();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<LifecycleRecorder>();
        services.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new LifecycleDiscoveryModule());
            cephalon.AddModule(new LifecyclePlatformModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();

        await runtime.StartAsync(provider);
        await runtime.StopAsync();

        string[] activitySnapshot;
        string[] measurementSnapshot;
        lock (sync)
        {
            activitySnapshot = activities.ToArray();
            measurementSnapshot = measurements.ToArray();
        }

        Assert.Contains(EngineDiagnostics.BuildActivityName, activitySnapshot);
        Assert.Contains("runtime.initialize", activitySnapshot);
        Assert.Contains("runtime.start", activitySnapshot);
        Assert.Contains("runtime.stop", activitySnapshot);
        Assert.Contains("module.initialize", activitySnapshot);
        Assert.Contains("module.start", activitySnapshot);
        Assert.Contains("module.stop", activitySnapshot);

        Assert.Contains(EngineDiagnostics.EngineBuildCounterName, measurementSnapshot);
        Assert.Contains(EngineDiagnostics.RuntimeTransitionCounterName, measurementSnapshot);
        Assert.Contains(EngineDiagnostics.ModuleTransitionCounterName, measurementSnapshot);
    }

    [Fact]
    public async Task FailureAndRestartPathsEmitFailureAndRestartMetrics()
    {
        var sync = new object();
        var measurements = new List<string>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == EngineDiagnostics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            lock (sync)
            {
                measurements.Add(instrument.Name);
            }
        });
        meterListener.Start();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<FailurePolicyRecorder>();
        services.AddCephalon(engine =>
        {
            engine.UseFailurePolicy(new FailurePolicy(
                startupFailureBehavior: StartupFailureBehavior.CaptureOnly,
                stopFailureBehavior: StopFailureBehavior.BestEffortContinue,
                allowManualRestart: true,
                maxRestartAttempts: 2));
            engine.AddModule(new FailurePolicyPlatformModule());
            engine.AddModule(new FlakyStartModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();

        await runtime.StartAsync(provider);
        Assert.Equal(RuntimeStatus.Failed, runtime.Status);

        await runtime.RestartAsync(provider);
        Assert.Equal(RuntimeStatus.Started, runtime.Status);

        string[] measurementSnapshot;
        lock (sync)
        {
            measurementSnapshot = measurements.ToArray();
        }

        Assert.Contains(EngineDiagnostics.RuntimeFailureCounterName, measurementSnapshot);
        Assert.Contains(EngineDiagnostics.ModuleFailureCounterName, measurementSnapshot);
        Assert.Contains(EngineDiagnostics.RuntimeRestartCounterName, measurementSnapshot);
    }
}
