using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.Hosting;
using Cephalon.Observability.HttpDependencies.Configuration;
using Cephalon.Observability.HttpDependencies.Hosting;
using Cephalon.Observability.KafkaDependencies.Configuration;
using Cephalon.Observability.KafkaDependencies.Hosting;
using Cephalon.Observability.MongoDbDependencies.Configuration;
using Cephalon.Observability.MongoDbDependencies.Hosting;
using Cephalon.Observability.MySqlDependencies.Configuration;
using Cephalon.Observability.MySqlDependencies.Hosting;
using Cephalon.Observability.PostgresDependencies.Configuration;
using Cephalon.Observability.PostgresDependencies.Hosting;
using Cephalon.Observability.RabbitMqDependencies.Configuration;
using Cephalon.Observability.RabbitMqDependencies.Hosting;
using Cephalon.Observability.RedisDependencies.Configuration;
using Cephalon.Observability.RedisDependencies.Hosting;
using Cephalon.Observability.SqlServerDependencies.Configuration;
using Cephalon.Observability.SqlServerDependencies.Hosting;
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

    [Fact]
    public void AddCephalonBuildsRuntimeDiagnosticsCatalogForActivePackages()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
            engine.AddModule(new PlatformTestModule());
        });
        services.AddCephalonObservability();
        services.AddCephalonHttpDependencyHealth(options =>
        {
            options.Dependencies =
            [
                new HttpDependencyDefinition
                {
                    Id = "upstream-api",
                    Endpoint = "https://upstream.example/health"
                }
            ];
        });
        services.AddCephalonKafkaDependencyHealth(options =>
        {
            options.Dependencies =
            [
                new KafkaDependencyDefinition
                {
                    Id = "events-kafka",
                    BootstrapServers = "kafka.internal.example:9092",
                    Topic = "cephalon.events"
                }
            ];
        });
        services.AddCephalonPostgresDependencyHealth(options =>
        {
            options.Dependencies =
            [
                new PostgresDependencyDefinition
                {
                    Id = "primary-sql",
                    Host = "postgres.internal.example"
                }
            ];
        });
        services.AddCephalonMySqlDependencyHealth(options =>
        {
            options.Dependencies =
            [
                new MySqlDependencyDefinition
                {
                    Id = "catalog-mysql",
                    Host = "mysql.internal.example",
                    Database = "catalog"
                }
            ];
        });
        services.AddCephalonMongoDbDependencyHealth(options =>
        {
            options.Dependencies =
            [
                new MongoDbDependencyDefinition
                {
                    Id = "catalog-mongodb",
                    Host = "mongo.internal.example",
                    Database = "catalog"
                }
            ];
        });
        services.AddCephalonRabbitMqDependencyHealth(options =>
        {
            options.Dependencies =
            [
                new RabbitMqDependencyDefinition
                {
                    Id = "events-broker",
                    Host = "rabbitmq.internal.example"
                }
            ];
        });
        services.AddCephalonRedisDependencyHealth(options =>
        {
            options.Dependencies =
            [
                new RedisDependencyDefinition
                {
                    Id = "distributed-cache",
                    Host = "redis.internal.example"
                }
            ];
        });
        services.AddCephalonSqlServerDependencyHealth(options =>
        {
            options.Dependencies =
            [
                new SqlServerDependencyDefinition
                {
                    Id = "orders-sql",
                    Host = "sql.internal.example",
                    Database = "orders"
                }
            ];
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Contains(catalog.Conventions, convention => convention.Source == "Cephalon.Engine");
        Assert.Contains(catalog.Conventions, convention => convention.Source == "Cephalon.Observability");
        Assert.Contains(catalog.Conventions, convention => convention.Source == "Cephalon.Observability.HttpDependencies");
        Assert.Contains(catalog.Conventions, convention => convention.Source == "Cephalon.Observability.KafkaDependencies");
        Assert.Contains(catalog.Conventions, convention => convention.Source == "Cephalon.Observability.MongoDbDependencies");
        Assert.Contains(catalog.Conventions, convention => convention.Source == "Cephalon.Observability.MySqlDependencies");
        Assert.Contains(catalog.Conventions, convention => convention.Source == "Cephalon.Observability.PostgresDependencies");
        Assert.Contains(catalog.Conventions, convention => convention.Source == "Cephalon.Observability.RabbitMqDependencies");
        Assert.Contains(catalog.Conventions, convention => convention.Source == "Cephalon.Observability.RedisDependencies");
        Assert.Contains(catalog.Conventions, convention => convention.Source == "Cephalon.Observability.SqlServerDependencies");

        Assert.Contains(
            catalog.GetBySource("Cephalon.Engine").Single().Events,
            static entry => entry.Id == 2002 && entry.Name == "LogRuntimeFailure");
        Assert.Contains(
            catalog.GetBySource("Cephalon.Observability").Single().Events,
            static entry => entry.Id == 3006 && entry.Name == "DiagnosticsCatalogEntry");
        Assert.Contains(
            catalog.GetBySource("Cephalon.Observability.HttpDependencies").Single().Events,
            static entry => entry.Id == 3100);
        Assert.Contains(
            catalog.GetBySource("Cephalon.Observability.KafkaDependencies").Single().Events,
            static entry => entry.Id == 3132);
        Assert.Contains(
            catalog.GetBySource("Cephalon.Observability.MongoDbDependencies").Single().Events,
            static entry => entry.Id == 3130);
        Assert.Contains(
            catalog.GetBySource("Cephalon.Observability.MySqlDependencies").Single().Events,
            static entry => entry.Id == 3128);
        Assert.Contains(
            catalog.GetBySource("Cephalon.Observability.PostgresDependencies").Single().Events,
            static entry => entry.Id == 3122);
        Assert.Contains(
            catalog.GetBySource("Cephalon.Observability.RabbitMqDependencies").Single().Events,
            static entry => entry.Id == 3124);
        Assert.Contains(
            catalog.GetBySource("Cephalon.Observability.RedisDependencies").Single().Events,
            static entry => entry.Id == 3120);
        Assert.Contains(
            catalog.GetBySource("Cephalon.Observability.SqlServerDependencies").Single().Events,
            static entry => entry.Id == 3126);

        var eventIds = catalog.Conventions
            .SelectMany(static convention => convention.Events)
            .Select(static entry => entry.Id)
            .ToArray();

        Assert.Equal(eventIds.Length, eventIds.Distinct().Count());
        Assert.Equal(catalog.Conventions.Count, snapshot.DiagnosticsConventions.Count);
        Assert.Contains(snapshot.DiagnosticsConventions, convention => convention.Source == "Cephalon.Observability.KafkaDependencies");
        Assert.Contains(snapshot.DiagnosticsConventions, convention => convention.Source == "Cephalon.Observability.MongoDbDependencies");
        Assert.Contains(snapshot.DiagnosticsConventions, convention => convention.Source == "Cephalon.Observability.MySqlDependencies");
        Assert.Contains(snapshot.DiagnosticsConventions, convention => convention.Source == "Cephalon.Observability.RabbitMqDependencies");
        Assert.Contains(snapshot.DiagnosticsConventions, convention => convention.Source == "Cephalon.Observability.SqlServerDependencies");
    }
}
