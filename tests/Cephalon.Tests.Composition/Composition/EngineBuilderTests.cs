using Cephalon.Engine.AppModel;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Patterns;
using Cephalon.Engine.Runtime;
using Cephalon.Engine.Technologies;
using Cephalon.Engine.Trust;
using Cephalon.Engine.Transports;
using Cephalon.Abstractions.Agentics;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Retrieval;
using Cephalon.Agentics.Registration;
using Cephalon.Agentics.Services;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.AppModel.Scaffolding;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Localization;
using Cephalon.Abstractions.Modules;
using Cephalon.Edge.Registration;
using Cephalon.Edge.Services;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.ReferenceModule.Operations.Registration;
using Cephalon.Retrieval.Registration;
using Cephalon.Retrieval.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Cephalon.Tests.Support;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cephalon.Tests.Composition;

public sealed class EngineBuilderTests
{
    [Fact]
    public async Task RuntimeStartupFailureDefaultsToFailFastAndCapturesFailureContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<FailurePolicyRecorder>();
        services.AddCephalon(engine =>
        {
            engine.AddModule(new FailurePolicyPlatformModule());
            engine.AddModule(new FlakyStartModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => runtime.StartAsync(provider));

        Assert.Equal(RuntimeStatus.Failed, runtime.Status);
        Assert.Equal("flaky-start", runtime.LastFailure?.ModuleId);
        Assert.Equal("start", runtime.LastFailure?.Phase);
        Assert.Equal("System.InvalidOperationException", runtime.LastFailure?.ExceptionType);
        Assert.Equal("Simulated startup failure.", runtime.LastFailure?.Message);
        Assert.True(runtime.LastFailure?.CanRestart);
        Assert.Contains("flaky-start", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RuntimeCanCaptureStartupFailureAndRestartWhenPolicyAllows()
    {
        var services = new ServiceCollection();
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
        var recorder = provider.GetRequiredService<FailurePolicyRecorder>();

        await runtime.StartAsync(provider);

        Assert.Equal(RuntimeStatus.Failed, runtime.Status);
        Assert.Equal("flaky-start", runtime.LastFailure?.ModuleId);
        Assert.Equal(0, runtime.RestartCount);

        await runtime.RestartAsync(provider);

        Assert.Equal(RuntimeStatus.Started, runtime.Status);
        Assert.Null(runtime.LastFailure);
        Assert.Equal(1, runtime.RestartCount);
        Assert.Equal(
            [
                "initialize:platform",
                "initialize:flaky",
                "start:platform",
                "start:flaky",
                "stop:platform",
                "start:platform",
                "start:flaky"
            ],
            recorder.Events);
    }

    [Fact]
    public async Task RuntimeStopFailureDefaultsToBestEffortAndCapturesFailureContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<FailurePolicyRecorder>();
        services.AddCephalon(engine =>
        {
            engine.AddModule(new FailurePolicyPlatformModule());
            engine.AddModule(new FailingStopModule());
            engine.AddModule(new StopObserverModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var recorder = provider.GetRequiredService<FailurePolicyRecorder>();

        await runtime.StartAsync(provider);
        await runtime.StopAsync();

        Assert.Equal(RuntimeStatus.Failed, runtime.Status);
        Assert.Equal("failing-stop", runtime.LastFailure?.ModuleId);
        Assert.Equal("stop", runtime.LastFailure?.Phase);
        Assert.False(runtime.LastFailure?.CanRestart);
        Assert.Equal(
            [
                "initialize:platform",
                "initialize:failing-stop",
                "initialize:observer",
                "start:platform",
                "start:failing-stop",
                "start:observer",
                "stop:observer",
                "stop:failing-stop",
                "stop:platform"
            ],
            recorder.Events);
    }

    [Fact]
    public async Task RuntimeLifecycleRunsInDependencyOrder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<LifecycleRecorder>();
        services.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new LifecycleDiscoveryModule());
            cephalon.AddModule(new LifecyclePlatformModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var recorder = provider.GetRequiredService<LifecycleRecorder>();

        Assert.Equal(RuntimeStatus.Created, runtime.Status);

        await runtime.InitializeAsync(provider);
        Assert.Equal(RuntimeStatus.Initialized, runtime.Status);

        await runtime.StartAsync(provider);
        Assert.Equal(RuntimeStatus.Started, runtime.Status);

        await runtime.StopAsync();
        Assert.Equal(RuntimeStatus.Stopped, runtime.Status);

        Assert.Equal(
            [
                "initialize:platform",
                "initialize:discovery",
                "start:platform",
                "start:discovery",
                "stop:discovery",
                "stop:platform"
            ],
            recorder.Events);
    }

    [Fact]
    public async Task RuntimeOperationalStoryTracksLoadedStartedStoppedAndTimeline()
    {
        var services = new ServiceCollection();
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

        var story = runtime.OperationalStory;

        Assert.Equal(RuntimeStatus.Stopped, story.Status.Status);
        Assert.Empty(story.LoadedPackages);
        Assert.Equal(2, story.Modules.Count);

        Assert.Contains(story.Modules, module => string.Equals(module.ModuleId, "lifecycle-platform", StringComparison.Ordinal));
        var platform = story.Modules.First(module => string.Equals(module.ModuleId, "lifecycle-platform", StringComparison.Ordinal));
        Assert.True(platform.IsLoaded);
        Assert.True(platform.IsInitialized);
        Assert.True(platform.IsStopped);
        Assert.False(platform.IsStarted);
        Assert.NotNull(platform.LoadedAtUtc);
        Assert.NotNull(platform.InitializedAtUtc);
        Assert.NotNull(platform.StartedAtUtc);
        Assert.NotNull(platform.StoppedAtUtc);
        Assert.Equal("stop", platform.LastObservedPhase);

        Assert.Contains(
            story.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.Module &&
                entry.SubjectId == "lifecycle-platform" &&
                entry.Phase == "load" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);
        Assert.Contains(
            story.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.Module &&
                entry.SubjectId == "lifecycle-platform" &&
                entry.Phase == "initialize" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);
        Assert.Contains(
            story.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.Module &&
                entry.SubjectId == "lifecycle-platform" &&
                entry.Phase == "start" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);
        Assert.Contains(
            story.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.Module &&
                entry.SubjectId == "lifecycle-platform" &&
                entry.Phase == "stop" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);
        Assert.Contains(
            story.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.Runtime &&
                entry.Phase == "stop" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);
    }

    [Fact]
    public void AddCephalonUsesConfigurationWithoutExplicitConfigureCallback()
    {
        var services = new ServiceCollection();
        var testAssemblyName = typeof(PlatformTestModule).Assembly.GetName().Name
            ?? throw new InvalidOperationException("Test assembly name was not available.");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Blueprint"] = "ModularVerticalSlice",
                ["Engine:Discovery:Assemblies:0"] = testAssemblyName,
                ["Engine:Patterns:0"] = "PipelinePattern",
                ["Engine:Transports:0"] = "RestApi",
                ["Engine:Technologies:0"] = "AgenticWorkloads",
                ["Engine:Options:Modules:lifecycle-platform:Enabled"] = "false",
                ["Engine:Options:Modules:lifecycle-discovery:Enabled"] = "false",
                ["Engine:Options:Modules:failure-platform:Enabled"] = "false",
                ["Engine:Options:Modules:flaky-start:Enabled"] = "false",
                ["Engine:Options:Modules:failing-stop:Enabled"] = "false",
                ["Engine:Options:Modules:stop-observer:Enabled"] = "false",
                ["Engine:Options:Modules:slow-stop:Enabled"] = "false",
                ["Engine:Options:Modules:dependency-health:Enabled"] = "false",
                ["Engine:Options:Modules:throwing-dependency-health:Enabled"] = "false",
                ["Engine:Options:Modules:restricted:Enabled"] = "false",
                ["Engine:Options:Modules:technology-catalog:Enabled"] = "false",
                ["Engine:Options:Modules:phase8-runtime-catalogs:Enabled"] = "false",
                ["Engine:Options:Modules:invalid-phase8-data-product:Enabled"] = "false",
                ["Engine:Options:Modules:invalid-phase8-cdc:Enabled"] = "false",
                ["Engine:Options:Modules:invalid-phase8-cdc-outbox:Enabled"] = "false",
                ["Engine:Options:Modules:invalid-phase8-projection:Enabled"] = "false",
                ["Engine:Options:Modules:invalid-phase8-outbox:Enabled"] = "false",
                ["Engine:Options:Modules:invalid-phase8-inbox:Enabled"] = "false",
                ["Engine:Options:Modules:invalid-phase8-audit-store:Enabled"] = "false",
                ["Engine:Options:Modules:entity-framework-single-context-tests:Enabled"] = "false",
                ["Engine:Options:Modules:entity-framework-split-context-tests:Enabled"] = "false",
                ["Engine:Options:Modules:entity-framework-outbox-tests:Enabled"] = "false",
                ["Engine:Options:Modules:entity-framework-sfid-tests:Enabled"] = "false"
            })
            .Build();

        services.AddCephalon(configuration);

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();

        Assert.Equal("2.0", runtime.Manifest.ManifestVersion);
        Assert.False(string.IsNullOrWhiteSpace(runtime.Manifest.EngineVersion));
        Assert.True(runtime.Manifest.Modules.Count >= 3);
        var platformModule = Assert.Single(runtime.Manifest.Modules, module => module.Id == "platform");
        var discoveryModule = Assert.Single(runtime.Manifest.Modules, module => module.Id == "discovery");
        var localizationPackModule = Assert.Single(runtime.Manifest.Modules, module => module.Id == "localization-pack");

        Assert.Equal("1.2.0", platformModule.Version);
        Assert.Equal("foundation", platformModule.Metadata["layer"]);
        Assert.Contains("PlatformTestModule", platformModule.TypeName, StringComparison.Ordinal);

        Assert.Equal("2.4.0", discoveryModule.Version);
        Assert.Equal("experience", discoveryModule.Metadata["layer"]);
        Assert.Equal(["platform"], discoveryModule.DependsOn);

        Assert.Equal("1.0.0", localizationPackModule.Version);
        Assert.Equal("foundation", localizationPackModule.Metadata["layer"]);
        Assert.Contains("LocalizationPackTestModule", localizationPackModule.TypeName, StringComparison.Ordinal);
        Assert.Equal("modular-vertical-slice", runtime.Manifest.AppProfile.BlueprintId);
        Assert.Contains(runtime.Manifest.AppProfile.Patterns, pattern => pattern.Id == "shared-foundation-pattern");
        Assert.Contains(runtime.Manifest.AppProfile.Patterns, pattern => pattern.Id == "pipeline-pattern");
        Assert.Contains(runtime.Manifest.AppProfile.Technologies, technology => technology.Id == "agentic-workloads");
        Assert.Contains(runtime.Manifest.AppProfile.Transports, transport => transport.Id == "rest-api");
    }

    [Fact]
    public void BuildLoadsModulesFromPackageAssemblyPaths()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            transports: ["RestApi"]));
        builder.AddPackageAssembly(GetReferenceModuleAssemblyPath(), id: "reference-operations");

        var runtime = builder.Build();

        var operationsModule = Assert.Single(runtime.Manifest.Modules, module => module.Id == "operations");
        var package = Assert.Single(runtime.Manifest.Packages);

        Assert.Equal("reference-operations", package.Id);
        Assert.Equal("assembly-path", package.Kind);
        Assert.EndsWith("Cephalon.ReferenceModule.Operations.dll", package.Path, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(package.Path, package.SourcePath);
        Assert.Null(package.Distribution);
        Assert.Null(package.Provenance);
        Assert.Contains("operations", package.Modules);
        Assert.False(package.IsTrusted);
        Assert.Equal("reference-operations", operationsModule.PackageId);
        Assert.False(operationsModule.IsTrusted);
    }

    [Fact]
    public void BuildLoadsModulesFromPackageManifestFiles()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.AddPackageManifest(GetReferenceModuleManifestPath());

        var runtime = builder.Build();

        var operationsModule = Assert.Single(runtime.Manifest.Modules, module => module.Id == "operations");
        var package = Assert.Single(runtime.Manifest.Packages);

        Assert.Equal("reference-operations", package.Id);
        Assert.Equal(ModulePackageReference.ManifestFileKind, package.Kind);
        Assert.Equal("1.0.0", package.Version);
        Assert.Equal(GetCurrentEngineCompatibilityVersion(), package.MinimumEngineVersion);
        Assert.EndsWith("cephalon.package.json", package.SourcePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("Cephalon.ReferenceModule.Operations.dll", package.Path, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("net10.0", package.SupportedTargetFrameworks);
        Assert.Equal("cephalon-labs", package.PublisherId);
        Assert.Equal("Cephalon Labs", package.PublisherDisplayName);
        Assert.NotNull(package.Distribution);
        Assert.Equal("stable", package.Distribution.Channel);
        Assert.Equal("https://packages.example.invalid/cephalon/reference-operations/1.0.0/cephalon.package.json", package.Distribution.ManifestUri);
        Assert.Equal("https://packages.example.invalid/cephalon/reference-operations/1.0.0/Cephalon.ReferenceModule.Operations.zip", package.Distribution.PackageUri);
        Assert.NotNull(package.Provenance);
        Assert.Equal("https://github.com/Cephalon-Labs/CephalonEngine", package.Provenance.SourceRepository);
        Assert.Equal("refs/tags/reference-operations-v1.0.0", package.Provenance.SourceRevision);
        Assert.Equal("https://builds.example.invalid/cephalon/reference-operations/1.0.0", package.Provenance.BuildUri);
        Assert.Equal("https://packages.example.invalid/cephalon/reference-operations/1.0.0/provenance.json", package.Provenance.StatementUri);
        Assert.Equal("provenance-manifest", package.SignatureType);
        Assert.Equal("Cephalon Labs Build", package.SignatureSigner);
        Assert.Null(package.SignatureKeyId);
        Assert.Equal("cephalon-labs-reference-operations", package.SignatureFingerprint);
        Assert.Equal("SHA256", package.SignatureAlgorithm);
        Assert.False(package.IsSignatureVerified);
        Assert.Contains("no signature value", package.SignatureVerificationReason, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(package.ChecksumSha256));
        Assert.Empty(package.Dependencies);
        Assert.Contains("operations", package.Modules);
        Assert.Equal("reference-operations", operationsModule.PackageId);
    }

    [Fact]
    public void BuildCollectsExecutionGraphsFromActiveModules()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.AddModule(new WorkflowCatalogTestModule("builder-test"));
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var catalog = provider.GetRequiredService<IExecutionRuntimeCatalog>();
        var graph = Assert.Single(catalog.Graphs);

        Assert.Equal("approval-flow", graph.Id);
        Assert.Equal("workflow-catalog", graph.SourceModuleId);
        Assert.Equal("request-review", graph.EntryNodeId);
        Assert.Equal(3, graph.Nodes.Count);
        Assert.Equal(2, graph.Edges.Count);
        Assert.Contains(graph.Nodes, node => node.CapabilityKey == "workflow.approval.request");
        Assert.Contains(graph.Nodes, node => node.CapabilityKey == "workflow.approval.record");
        Assert.Equal(graph, catalog.GetById("approval-flow"));
        Assert.Single(catalog.GetBySourceModule("workflow-catalog"));
        Assert.Equal("modular-monolith", runtime.Manifest.AppProfile.BlueprintId);
    }

    [Fact]
    public void BuildCollectsHostedExecutionsFromActiveModules()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.AddModule(new WorkflowCatalogTestModule("builder-hosted-test"));
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var catalog = provider.GetRequiredService<IHostedExecutionRuntimeCatalog>();
        var hostedExecution = Assert.Single(catalog.HostedExecutions);

        Assert.Equal("approval-pump", hostedExecution.Id);
        Assert.Equal("workflow-catalog", hostedExecution.SourceModuleId);
        Assert.Equal("background-service", hostedExecution.Kind);
        Assert.Equal("approval-flow", hostedExecution.ExecutionGraphId);
        Assert.True(hostedExecution.StartsWithHost);
        Assert.Equal(hostedExecution, catalog.GetById("approval-pump"));
        Assert.Single(catalog.GetBySourceModule("workflow-catalog"));
        Assert.Single(catalog.GetByExecutionGraph("approval-flow"));
        Assert.Equal("modular-monolith", runtime.Manifest.AppProfile.BlueprintId);
    }

    [Fact]
    public async Task RuntimeOperationalStoryTracksExecutionGraphsAcrossLifecycleTransitions()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.AddModule(new WorkflowCatalogTestModule("story-test"));
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();

        await runtime.StartAsync(provider);

        var startedStory = runtime.OperationalStory;
        var activeGraph = Assert.Single(startedStory.ExecutionGraphs);

        Assert.Equal("approval-flow", activeGraph.GraphId);
        Assert.Equal("workflow-catalog", activeGraph.SourceModuleId);
        Assert.Equal("1.0.0", activeGraph.SourceModuleVersion);
        Assert.Equal("request-review", activeGraph.EntryNodeId);
        Assert.True(activeGraph.IsLoaded);
        Assert.True(activeGraph.IsActive);
        Assert.False(activeGraph.IsDeactivated);
        Assert.NotNull(activeGraph.LoadedAtUtc);
        Assert.NotNull(activeGraph.ActivatedAtUtc);
        Assert.Null(activeGraph.DeactivatedAtUtc);
        Assert.Equal("activate", activeGraph.LastObservedPhase);
        Assert.Contains(
            startedStory.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.ExecutionGraph &&
                entry.SubjectId == "approval-flow" &&
                entry.Phase == "load" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);
        Assert.Contains(
            startedStory.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.ExecutionGraph &&
                entry.SubjectId == "approval-flow" &&
                entry.Phase == "activate" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);

        await runtime.StopAsync();

        var stoppedStory = runtime.OperationalStory;
        var stoppedGraph = Assert.Single(stoppedStory.ExecutionGraphs);

        Assert.False(stoppedGraph.IsActive);
        Assert.True(stoppedGraph.IsDeactivated);
        Assert.NotNull(stoppedGraph.DeactivatedAtUtc);
        Assert.Equal("deactivate", stoppedGraph.LastObservedPhase);
        Assert.Contains(
            stoppedStory.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.ExecutionGraph &&
                entry.SubjectId == "approval-flow" &&
                entry.Phase == "deactivate" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);
    }

    [Fact]
    public async Task RuntimeOperationalStoryTracksHostedExecutionsAcrossLifecycleTransitions()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.AddModule(new WorkflowCatalogTestModule("hosted-story-test"));
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();

        await runtime.StartAsync(provider);

        var startedStory = runtime.OperationalStory;
        var activeHostedExecution = Assert.Single(startedStory.HostedExecutions);

        Assert.Equal("approval-pump", activeHostedExecution.HostedExecutionId);
        Assert.Equal("workflow-catalog", activeHostedExecution.SourceModuleId);
        Assert.Equal("1.0.0", activeHostedExecution.SourceModuleVersion);
        Assert.Equal("background-service", activeHostedExecution.Kind);
        Assert.Equal("approval-flow", activeHostedExecution.ExecutionGraphId);
        Assert.True(activeHostedExecution.StartsWithHost);
        Assert.True(activeHostedExecution.IsLoaded);
        Assert.True(activeHostedExecution.IsActive);
        Assert.False(activeHostedExecution.IsDeactivated);
        Assert.NotNull(activeHostedExecution.LoadedAtUtc);
        Assert.NotNull(activeHostedExecution.ActivatedAtUtc);
        Assert.Null(activeHostedExecution.DeactivatedAtUtc);
        Assert.Equal("activate", activeHostedExecution.LastObservedPhase);
        Assert.Contains(
            startedStory.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.HostedExecution &&
                entry.SubjectId == "approval-pump" &&
                entry.Phase == "load" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);
        Assert.Contains(
            startedStory.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.HostedExecution &&
                entry.SubjectId == "approval-pump" &&
                entry.Phase == "activate" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);

        await runtime.StopAsync();

        var stoppedStory = runtime.OperationalStory;
        var stoppedHostedExecution = Assert.Single(stoppedStory.HostedExecutions);

        Assert.False(stoppedHostedExecution.IsActive);
        Assert.True(stoppedHostedExecution.IsDeactivated);
        Assert.NotNull(stoppedHostedExecution.DeactivatedAtUtc);
        Assert.Equal("deactivate", stoppedHostedExecution.LastObservedPhase);
        Assert.Contains(
            stoppedStory.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.HostedExecution &&
                entry.SubjectId == "approval-pump" &&
                entry.Phase == "deactivate" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);
    }

    [Fact]
    public void BuildRejectsExecutionGraphsThatReferenceUnknownCapabilities()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCephalon(engine =>
            {
                engine.AddModule(new InvalidWorkflowCapabilityModule(publishInvalidGraph: true));
            }));

        Assert.Contains("unknown capability", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("workflow.missing", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildRejectsHostedExecutionsThatReferenceUnknownExecutionGraphs()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCephalon(engine =>
            {
                engine.AddModule(new InvalidHostedExecutionModule(publishInvalidHostedExecution: true));
            }));

        Assert.Contains("unknown execution graph", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("missing-graph", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildDiscoversModulesFromConfiguredPackageDirectories()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.AddPackageDirectory(GetReferenceModulePackageDirectory());

        var runtime = builder.Build();

        var package = Assert.Single(runtime.Manifest.Packages);

        Assert.Equal("reference-operations", package.Id);
        Assert.Equal(ModulePackageReference.DirectoryManifestKind, package.Kind);
        Assert.Equal("1.0.0", package.Version);
        Assert.EndsWith("cephalon.package.json", package.SourcePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("Cephalon.ReferenceModule.Operations.dll", package.Path, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("net10.0", package.SupportedTargetFrameworks);
        Assert.Equal("cephalon-labs", package.PublisherId);
        Assert.NotNull(package.Distribution);
        Assert.Equal("stable", package.Distribution.Channel);
        Assert.NotNull(package.Provenance);
        Assert.Equal("refs/tags/reference-operations-v1.0.0", package.Provenance.SourceRevision);
        Assert.Null(package.SignatureKeyId);
        Assert.Equal("cephalon-labs-reference-operations", package.SignatureFingerprint);
        Assert.False(package.IsSignatureVerified);
        Assert.Contains("no signature value", package.SignatureVerificationReason, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(package.Dependencies);
        Assert.Contains("operations", package.Modules);
    }

    [Fact]
    public void BuildExposesPackageDependencyRequirementsFromManifestPackages()
    {
        var dependencyManifestPath = CreateTemporaryManifest(
            """
            {
              "id": "reference-support",
              "version": "2.0.0",
              "assembly": "__PACKAGE_ASSEMBLY__",
              "dependencies": [
                {
                  "id": "reference-operations",
                  "minimumVersion": "1.0.0",
                  "maximumVersion": "1.0.0"
                }
              ],
              "compatibility": {
                "minimumEngineVersion": "0.1.0-preview"
              }
            }
            """);

        try
        {
            var builder = new EngineBuilder(new ServiceCollection());
            builder.AddPackageManifest(GetReferenceModuleManifestPath());
            builder.AddPackageManifest(dependencyManifestPath);

            var runtime = builder.Build();
            var dependencyPackage = Assert.Single(runtime.Manifest.Packages, static package => package.Id == "reference-support");
            var dependency = Assert.Single(dependencyPackage.Dependencies);

            Assert.Equal("reference-operations", dependency.Id);
            Assert.Equal("1.0.0", dependency.MinimumVersion);
            Assert.Equal("1.0.0", dependency.MaximumVersion);
        }
        finally
        {
            DeleteManifestDirectory(dependencyManifestPath);
        }
    }

    [Fact]
    public void BuildThrowsWhenPackageDependencyIsMissing()
    {
        var manifestPath = CreateTemporaryManifest(
            """
            {
              "id": "reference-support",
              "version": "2.0.0",
              "assembly": "__PACKAGE_ASSEMBLY__",
              "dependencies": [
                {
                  "id": "reference-operations"
                }
              ],
              "compatibility": {
                "minimumEngineVersion": "0.1.0-preview"
              }
            }
            """);

        try
        {
            var builder = new EngineBuilder(new ServiceCollection());
            builder.AddPackageManifest(manifestPath);

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

            Assert.Contains("reference-support", exception.Message, StringComparison.Ordinal);
            Assert.Contains("reference-operations", exception.Message, StringComparison.Ordinal);
            Assert.Contains("not registered", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteManifestDirectory(manifestPath);
        }
    }

    [Fact]
    public void BuildThrowsWhenPackageDependencyVersionDoesNotSatisfyRequirement()
    {
        var manifestPath = CreateTemporaryManifest(
            """
            {
              "id": "reference-support",
              "version": "2.0.0",
              "assembly": "__PACKAGE_ASSEMBLY__",
              "dependencies": [
                {
                  "id": "reference-operations",
                  "minimumVersion": "2.0.0"
                }
              ],
              "compatibility": {
                "minimumEngineVersion": "0.1.0-preview"
              }
            }
            """);

        try
        {
            var builder = new EngineBuilder(new ServiceCollection());
            builder.AddPackageManifest(GetReferenceModuleManifestPath());
            builder.AddPackageManifest(manifestPath);

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

            Assert.Contains("reference-support", exception.Message, StringComparison.Ordinal);
            Assert.Contains("reference-operations", exception.Message, StringComparison.Ordinal);
            Assert.Contains("2.0.0", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteManifestDirectory(manifestPath);
        }
    }

    [Fact]
    public void BuildThrowsWhenVersionedPackageDependencyTargetsAssemblyPathPackageWithoutVersion()
    {
        var manifestPath = CreateTemporaryManifest(
            """
            {
              "id": "reference-support",
              "version": "2.0.0",
              "assembly": "__PACKAGE_ASSEMBLY__",
              "dependencies": [
                {
                  "id": "reference-operations",
                  "minimumVersion": "1.0.0"
                }
              ],
              "compatibility": {
                "minimumEngineVersion": "0.1.0-preview"
              }
            }
            """);

        try
        {
            var builder = new EngineBuilder(new ServiceCollection());
            builder.AddPackageAssembly(GetReferenceModuleAssemblyPath(), id: "reference-operations");
            builder.AddPackageManifest(manifestPath);

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

            Assert.Contains("reference-support", exception.Message, StringComparison.Ordinal);
            Assert.Contains("reference-operations", exception.Message, StringComparison.Ordinal);
            Assert.Contains("did not declare 'version'", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteManifestDirectory(manifestPath);
        }
    }

    [Fact]
    public void BuildThrowsWhenPackageRequiresNewerEngineVersion()
    {
        var manifestPath = CreateTemporaryManifest(
            """
            {
              "id": "future-operations",
              "assembly": "__ASSEMBLY__",
              "compatibility": {
                "minimumEngineVersion": "99.0.0"
              }
            }
            """);

        try
        {
            var builder = new EngineBuilder(new ServiceCollection());
            builder.AddPackageManifest(manifestPath);

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

            Assert.Contains("future-operations", exception.Message, StringComparison.Ordinal);
            Assert.Contains("99.0.0", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteManifestDirectory(manifestPath);
        }
    }

    [Fact]
    public void BuildThrowsWhenPackageDoesNotSupportCurrentTargetFramework()
    {
        var manifestPath = CreateTemporaryManifest(
            """
            {
              "id": "legacy-operations",
              "assembly": "__ASSEMBLY__",
              "compatibility": {
                "supportedTargetFrameworks": [ "net8.0" ]
              }
            }
            """);

        try
        {
            var builder = new EngineBuilder(new ServiceCollection());
            builder.AddPackageManifest(manifestPath);

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

            Assert.Contains("legacy-operations", exception.Message, StringComparison.Ordinal);
            Assert.Contains("net8.0", exception.Message, StringComparison.Ordinal);
            Assert.Contains("net10.0", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteManifestDirectory(manifestPath);
        }
    }

    [Fact]
    public void BuildThrowsWhenPackageManifestSha256DoesNotMatch()
    {
        var manifestPath = CreateTemporaryManifest(
            """
            {
              "id": "tampered-operations",
              "assembly": "__ASSEMBLY__",
              "integrity": {
                "sha256": "deadbeef"
              }
            }
            """);

        try
        {
            var builder = new EngineBuilder(new ServiceCollection());
            builder.AddPackageManifest(manifestPath);

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

            Assert.Contains("tampered-operations", exception.Message, StringComparison.Ordinal);
            Assert.Contains("SHA-256", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteManifestDirectory(manifestPath);
        }
    }

    [Fact]
    public void BuildThrowsWhenPackagePolicyDisallowsRawAssemblyPathPackages()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UsePackagePolicy(new PackagePolicy(allowAssemblyPathPackages: false));
        builder.AddPackageAssembly(GetReferenceModuleAssemblyPath(), id: "reference-operations");

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("reference-operations", exception.Message, StringComparison.Ordinal);
        Assert.Contains("manifest-driven", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildThrowsWhenPackagePolicyRequiresVersionAndManifestOmitsIt()
    {
        var manifestPath = CreateTemporaryManifest(
            """
            {
              "id": "unversioned-operations",
              "assembly": "__ASSEMBLY__"
            }
            """);

        try
        {
            var builder = new EngineBuilder(new ServiceCollection());
            builder.UsePackagePolicy(new PackagePolicy(requireVersion: true));
            builder.AddPackageManifest(manifestPath);

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

            Assert.Contains("unversioned-operations", exception.Message, StringComparison.Ordinal);
            Assert.Contains("'version'", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteManifestDirectory(manifestPath);
        }
    }

    [Fact]
    public void BuildThrowsWhenPackagePolicyRequiresIntegritySha256AndManifestOmitsIt()
    {
        var manifestPath = CreateTemporaryManifest(
            """
            {
              "id": "unsigned-operations",
              "assembly": "__ASSEMBLY__",
              "version": "1.0.0",
              "compatibility": {
                "minimumEngineVersion": "0.1.0-preview",
                "supportedTargetFrameworks": [ "net10.0" ]
              }
            }
            """);

        try
        {
            var builder = new EngineBuilder(new ServiceCollection());
            builder.UsePackagePolicy(new PackagePolicy(requireIntegritySha256: true));
            builder.AddPackageManifest(manifestPath);

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

            Assert.Contains("unsigned-operations", exception.Message, StringComparison.Ordinal);
            Assert.Contains("'integrity.sha256'", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteManifestDirectory(manifestPath);
        }
    }

    [Fact]
    public void BuildThrowsWhenPackagePolicyRequiresPublisherIdAndManifestOmitsIt()
    {
        var manifestPath = CreateTemporaryManifest(
            """
            {
              "id": "anonymous-operations",
              "assembly": "__ASSEMBLY__",
              "version": "1.0.0"
            }
            """);

        try
        {
            var builder = new EngineBuilder(new ServiceCollection());
            builder.UsePackagePolicy(new PackagePolicy(requirePublisherId: true));
            builder.AddPackageManifest(manifestPath);

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

            Assert.Contains("anonymous-operations", exception.Message, StringComparison.Ordinal);
            Assert.Contains("'publisher.id'", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteManifestDirectory(manifestPath);
        }
    }

    [Fact]
    public void BuildThrowsWhenPackagePolicyRequiresSignatureFingerprintAndManifestOmitsIt()
    {
        var manifestPath = CreateTemporaryManifest(
            """
            {
              "id": "unsigned-operations",
              "assembly": "__ASSEMBLY__",
              "version": "1.0.0",
              "publisher": {
                "id": "cephalon-labs",
                "displayName": "Cephalon Labs"
              }
            }
            """);

        try
        {
            var builder = new EngineBuilder(new ServiceCollection());
            builder.UsePackagePolicy(new PackagePolicy(requireSignatureFingerprint: true));
            builder.AddPackageManifest(manifestPath);

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

            Assert.Contains("unsigned-operations", exception.Message, StringComparison.Ordinal);
            Assert.Contains("'signature.fingerprint'", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteManifestDirectory(manifestPath);
        }
    }

    [Fact]
    public void BuildThrowsWhenPackagePolicyRequiresSignatureKeyIdAndManifestOmitsIt()
    {
        using var fixture = CreateSignedPackageFixture(includeKeyId: false);

        var builder = new EngineBuilder(new ServiceCollection());
        builder.UsePackagePolicy(new PackagePolicy(requireSignatureKeyId: true));
        builder.AddPackageManifest(fixture.ManifestPath);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("signed-operations", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'signature.keyId'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildThrowsWhenPackagePolicyRequiresSignatureValueAndManifestOmitsIt()
    {
        using var fixture = CreateSignedPackageFixture(includeSignatureValue: false);

        var builder = new EngineBuilder(new ServiceCollection());
        builder.UsePackagePolicy(new PackagePolicy(requireSignatureValue: true));
        builder.AddPackageManifest(fixture.ManifestPath);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("signed-operations", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'signature.value'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildAllowsManifestPackagesThatSatisfyPackagePolicyRequirements()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UsePackagePolicy(new PackagePolicy(
            requireVersion: true,
            requireMinimumEngineVersion: true,
            requireSupportedTargetFrameworks: true,
            requirePublisherId: true,
            requireSignatureFingerprint: true));
        builder.AddPackageManifest(GetReferenceModuleManifestPath());

        var runtime = builder.Build();
        var package = Assert.Single(runtime.Manifest.Packages);

        Assert.Equal("reference-operations", package.Id);
        Assert.Equal("1.0.0", package.Version);
        Assert.Equal(GetCurrentEngineCompatibilityVersion(), package.MinimumEngineVersion);
        Assert.Contains("net10.0", package.SupportedTargetFrameworks);
        Assert.Equal("cephalon-labs", package.PublisherId);
        Assert.Equal("cephalon-labs-reference-operations", package.SignatureFingerprint);
    }

    [Fact]
    public void BuildVerifiesPackagesSignedWithTrustedPublicKey()
    {
        using var fixture = CreateSignedPackageFixture();

        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseTrustPolicy(new TrustPolicy(
            requireTrustedPackages: true,
            trustedSignaturePublicKeys: new Dictionary<string, string>
            {
                [fixture.KeyId] = fixture.PublicKeyPath
            }));
        builder.AddPackageManifest(fixture.ManifestPath);

        var runtime = builder.Build();
        var package = Assert.Single(runtime.Manifest.Packages);

        Assert.Equal(fixture.KeyId, package.SignatureKeyId);
        Assert.Equal(fixture.Fingerprint, package.SignatureFingerprint);
        var signature = Assert.Single(package.Signatures);
        Assert.Equal(fixture.KeyId, signature.KeyId);
        Assert.True(signature.IsVerified);
        Assert.True(package.IsSignatureVerified);
        Assert.Contains("verified", package.SignatureVerificationReason, StringComparison.OrdinalIgnoreCase);
        Assert.True(package.IsTrusted);
        Assert.Equal(package.SignatureVerificationReason, package.TrustReason);
    }

    [Fact]
    public void BuildVerifiesPackagesSignedWithTrustedCertificateChain()
    {
        using var fixture = CreateCertificateSignedPackageFixture();

        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseTrustPolicy(new TrustPolicy(
            requireTrustedPackages: true,
            trustedSignatureCertificates: new Dictionary<string, string>
            {
                [fixture.KeyId] = fixture.SigningCertificatePath
            },
            trustedSignatureCertificateAuthorities:
            [
                fixture.RootCertificatePath
            ]));
        builder.AddPackageManifest(fixture.ManifestPath);

        var runtime = builder.Build();
        var package = Assert.Single(runtime.Manifest.Packages);
        var trust = builder.Services.BuildServiceProvider().GetRequiredService<CapabilityPolicyEvaluator>().Snapshot;

        Assert.Equal(fixture.KeyId, package.SignatureKeyId);
        Assert.Equal(fixture.Fingerprint, package.SignatureFingerprint);
        Assert.Equal(fixture.CertificateThumbprint, package.SignatureCertificateThumbprint);
        var signature = Assert.Single(package.Signatures);
        Assert.Equal("trusted-certificate-chain", signature.VerificationSource);
        Assert.Equal(fixture.CertificateThumbprint, signature.CertificateThumbprint);
        Assert.True(signature.IsVerified);
        Assert.True(package.IsSignatureVerified);
        Assert.Contains("certificate-chain validation", signature.VerificationReason, StringComparison.OrdinalIgnoreCase);
        Assert.True(package.IsTrusted);

        var trustDecision = Assert.Single(trust.Packages);
        Assert.Equal(fixture.CertificateThumbprint, trustDecision.SignatureCertificateThumbprint);
        var trustSignature = Assert.Single(trustDecision.Signatures);
        Assert.Equal("trusted-certificate-chain", trustSignature.VerificationSource);
        Assert.Equal(fixture.CertificateThumbprint, trustSignature.CertificateThumbprint);
        Assert.True(trustSignature.IsVerified);
    }

    [Fact]
    public void BuildSupportsMultiSignerPackagesAndExposesPerSignerVerification()
    {
        using var fixture = CreateMultiSignedPackageFixture();

        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseTrustPolicy(new TrustPolicy(
            requireTrustedPackages: true,
            trustedSignaturePublicKeys: new Dictionary<string, string>
            {
                [fixture.Signers[0].KeyId] = fixture.Signers[0].PublicKeyPath
            }));
        builder.AddPackageManifest(fixture.ManifestPath);

        var runtime = builder.Build();
        var package = Assert.Single(runtime.Manifest.Packages);

        Assert.Equal(2, package.Signatures.Count);
        Assert.True(package.IsSignatureVerified);
        Assert.Contains("1 of 2", package.SignatureVerificationReason, StringComparison.OrdinalIgnoreCase);

        var verifiedSignature = Assert.Single(package.Signatures, static signature => signature.IsVerified);
        Assert.Equal(fixture.Signers[0].KeyId, verifiedSignature.KeyId);

        var unverifiedSignature = Assert.Single(package.Signatures, static signature => !signature.IsVerified);
        Assert.Equal(fixture.Signers[1].KeyId, unverifiedSignature.KeyId);
        Assert.Contains("No trusted public key", unverifiedSignature.VerificationReason, StringComparison.OrdinalIgnoreCase);

        Assert.True(package.IsTrusted);
    }

    [Fact]
    public void BuildThrowsWhenCryptographicSignatureVerificationFails()
    {
        using var fixture = CreateSignedPackageFixture(tamperSignature: true);

        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseTrustPolicy(new TrustPolicy(
            trustedSignaturePublicKeys: new Dictionary<string, string>
            {
                [fixture.KeyId] = fixture.PublicKeyPath
            }));
        builder.AddPackageManifest(fixture.ManifestPath);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("signed-operations", exception.Message, StringComparison.Ordinal);
        Assert.Contains("cryptographic signature verification", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildThrowsWhenSigningCertificateChainCannotBeValidated()
    {
        using var fixture = CreateCertificateSignedPackageFixture();

        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseTrustPolicy(new TrustPolicy(
            trustedSignatureCertificates: new Dictionary<string, string>
            {
                [fixture.KeyId] = fixture.SigningCertificatePath
            }));
        builder.AddPackageManifest(fixture.ManifestPath);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("signed-operations", exception.Message, StringComparison.Ordinal);
        Assert.Contains("trusted signing certificate validation", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TrustPolicyFromConfigurationReadsCertificateTrustSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Trust:TrustedSignatureCertificates:cephalon-labs-build"] = "trusted-signing-cert.pem",
                ["Engine:Trust:TrustedSignatureCertificateAuthorities:0"] = "trusted-root-cert.pem"
            })
            .Build();

        var policy = TrustPolicy.FromConfiguration(configuration);

        Assert.Equal("trusted-signing-cert.pem", policy.TrustedSignatureCertificates["cephalon-labs-build"]);
        Assert.Contains("trusted-root-cert.pem", policy.TrustedSignatureCertificateAuthorities);
    }

    [Fact]
    public void BuildThrowsWhenPackagePolicyRequiresCryptographicSignatureVerificationAndNoTrustedKeyExists()
    {
        using var fixture = CreateSignedPackageFixture();

        var builder = new EngineBuilder(new ServiceCollection());
        builder.UsePackagePolicy(new PackagePolicy(
            requireSignatureKeyId: true,
            requireSignatureValue: true,
            requireSignatureVerification: true));
        builder.AddPackageManifest(fixture.ManifestPath);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("signed-operations", exception.Message, StringComparison.Ordinal);
        Assert.Contains("cryptographic signature verification", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No trusted public key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildThrowsWhenPackageAssemblyPathDoesNotExist()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.AddPackageAssembly(
            Path.Combine(Path.GetTempPath(), $"cephalon-missing-{Guid.NewGuid():N}", "Missing.Module.dll"),
            id: "missing-package");

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("missing-package", exception.Message, StringComparison.Ordinal);
        Assert.Contains("does not exist", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildThrowsWhenPackageDirectoryHasNoPackageManifests()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        var directory = Path.Combine(Path.GetTempPath(), $"cephalon-empty-packages-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            builder.AddPackageDirectory(directory);

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

            Assert.Contains("did not contain any", exception.Message, StringComparison.Ordinal);
            Assert.Contains(ModulePackageDirectory.DefaultManifestFileName, exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void BuildIncludesTechnologiesAndScaffoldGuidance()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularVerticalSlice",
            transports: ["WebSocket"],
            technologies: ["AgenticWorkloads", "EventDrivenIntegration", "KnowledgeRetrieval", "RealtimeExperience", "EdgeNativeDelivery"]));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new DiscoveryTestModule());

        var runtime = builder.Build();
        var scaffold = Assert.IsType<ScaffoldPlan>(runtime.Manifest.AppProfile.Scaffold);

        Assert.Contains(runtime.Manifest.AppProfile.Technologies, technology => technology.Id == "agentic-workloads");
        Assert.Contains(runtime.Manifest.AppProfile.Technologies, technology => technology.Id == "event-driven-integration");
        Assert.Contains(runtime.Manifest.AppProfile.Technologies, technology => technology.Id == "knowledge-retrieval");
        Assert.Contains(runtime.Manifest.AppProfile.Technologies, technology => technology.Id == "realtime-experience");
        Assert.Contains(runtime.Manifest.AppProfile.Technologies, technology => technology.Id == "edge-native-delivery");
        Assert.Contains(scaffold.Conventions, convention =>
            convention.Contains("Agentic Workloads", StringComparison.Ordinal));
        Assert.Contains(scaffold.Conventions, convention =>
            convention.Contains("Realtime Experience", StringComparison.Ordinal));
        var hostProject = Assert.Single(scaffold.Projects, project =>
            project.Role == ProjectRoles.Host);
        Assert.Contains("Cephalon.Agentics", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Eventing", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Cephalon.Eventing.Wolverine", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Retrieval", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Edge", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildIncludesPhase8SelectionsInTheResolvedAppProfile()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularVerticalSlice",
            patterns: ["Hexagonal", "CleanArchitecture", "DDD", "CQRS", "Outbox"],
            transports: ["RestApi"],
            technologies: ["IdentityAccess", "MultiTenancy", "EventDrivenIntegration", "Serverless"],
            data: new DataSettings(
                provider: "EntityFramework",
                readWriteSplit: true,
                outboxEnabled: true,
                idGenerator: "Sfid"),
            databases: new DatabaseTopologySettings(
                runtime: new DatabaseRuntimeSettings(
                    enableDetailedErrors: true,
                    enableRetryOnFailure: true,
                    maxRetryCount: 5,
                    commandTimeoutSeconds: 30),
                write: new DatabaseTargetSettings(
                    provider: "PostgreSql",
                    connectionStringName: "WriteDb",
                    runtime: new DatabaseRuntimeSettings(
                        enableRetryOnFailure: false)),
                read: new DatabaseTargetSettings(
                    provider: "PostgreSql",
                    connectionStringName: "ReadDb"),
                outbox: new DatabaseTargetSettings(
                    useRole: "write",
                    schema: "outbox01"),
                history: new DatabaseTargetSettings(
                    useRole: "write",
                    schema: "audit01"),
                migrations: new DatabaseMigrationsSettings(
                    applyOnStartup: true,
                    targets: ["write", "outbox", "history"])),
            identity: new IdentitySettings(
                enabled: true,
                authorizationModes: ["RBAC", "Policy"]),
            tenancy: new TenancySettings(
                enabled: true,
                mode: "SharedDatabase"),
            audit: new AuditSettings(enabled: true),
            messaging: new MessagingSettings(provider: "Wolverine")));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new DiscoveryTestModule());

        var runtime = builder.Build();
        var appProfile = runtime.Manifest.AppProfile;
        var scaffold = Assert.IsType<ScaffoldPlan>(appProfile.Scaffold);

        Assert.Contains(appProfile.Patterns, pattern => pattern.Id == "hexagonal-architecture");
        Assert.Contains(appProfile.Patterns, pattern => pattern.Id == "clean-architecture");
        Assert.Contains(appProfile.Patterns, pattern => pattern.Id == "domain-driven-design");
        Assert.Contains(appProfile.Patterns, pattern => pattern.Id == "cqrs");
        Assert.Contains(appProfile.Patterns, pattern => pattern.Id == "outbox");
        Assert.Contains(appProfile.Technologies, technology => technology.Id == "identity-access");
        Assert.Contains(appProfile.Technologies, technology => technology.Id == "multi-tenancy");
        Assert.Contains(appProfile.Technologies, technology => technology.Id == "event-driven-integration");
        Assert.Contains(appProfile.Technologies, technology => technology.Id == "serverless-hosting");
        Assert.Equal("EntityFramework", appProfile.Data.Provider);
        Assert.True(appProfile.Data.ReadWriteSplit);
        Assert.True(appProfile.Data.OutboxEnabled);
        Assert.Equal("Sfid", appProfile.Data.IdGenerator);
        Assert.True(appProfile.Databases.Runtime.EnableDetailedErrors);
        Assert.True(appProfile.Databases.Runtime.EnableRetryOnFailure);
        Assert.Equal(5, appProfile.Databases.Runtime.MaxRetryCount);
        Assert.Equal("PostgreSql", appProfile.Databases.Write.Provider);
        Assert.Equal("WriteDb", appProfile.Databases.Write.ConnectionStringName);
        Assert.False(appProfile.Databases.Write.Runtime.EnableRetryOnFailure);
        Assert.Equal("ReadDb", appProfile.Databases.Read.ConnectionStringName);
        Assert.Equal("write", appProfile.Databases.Outbox.UseRole);
        Assert.Equal("outbox01", appProfile.Databases.Outbox.Schema);
        Assert.Equal("write", appProfile.Databases.History.UseRole);
        Assert.Equal("audit01", appProfile.Databases.History.Schema);
        Assert.True(appProfile.Databases.Migrations.ApplyOnStartup);
        Assert.Equal(["history", "outbox", "write"], appProfile.Databases.Migrations.Targets);
        Assert.True(appProfile.Identity.Enabled);
        Assert.Equal(["RBAC", "Policy"], appProfile.Identity.AuthorizationModes);
        Assert.True(appProfile.Tenancy.Enabled);
        Assert.Equal("SharedDatabase", appProfile.Tenancy.Mode);
        Assert.True(appProfile.Audit.Enabled);
        Assert.Equal("Wolverine", appProfile.Messaging.Provider);
        var hostProject = Assert.Single(scaffold.Projects, project =>
            project.Role == ProjectRoles.Host);
        Assert.Contains("Cephalon.Identity", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.MultiTenancy", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Eventing", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cephalon.Eventing.Wolverine", hostProject.Packages, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildIncludesPhase11ResilienceSelectionsAndPatternTaxonomy()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "Microservice",
            patterns: ["Onion", "DDD", "AntiCorruptionLayer"],
            resilience: new ResilienceSettings(
                retry: new RetrySettings(
                    enabled: true,
                    maxAttempts: 5,
                    baseDelayMilliseconds: 200,
                    maxDelayMilliseconds: 2000,
                    backoff: "Exponential",
                    useJitter: true),
                timeout: new TimeoutSettings(
                    enabled: true,
                    totalTimeoutSeconds: 30,
                    attemptTimeoutSeconds: 10),
                circuitBreaker: new CircuitBreakerSettings(
                    enabled: true,
                    failureRatio: 0.2m,
                    minimumThroughput: 20,
                    samplingDurationSeconds: 15,
                    breakDurationSeconds: 30),
                bulkhead: new BulkheadSettings(
                    enabled: true,
                    maxConcurrentExecutions: 64,
                    maxQueuedActions: 128),
                rateLimiting: new RateLimitingSettings(
                    enabled: true,
                    algorithm: "SlidingWindow",
                    permitLimit: 500,
                    queueLimit: 50,
                    windowSeconds: 60,
                    segmentsPerWindow: 1))));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new DiscoveryTestModule());

        var appProfile = builder.Build().Manifest.AppProfile;

        Assert.Contains(appProfile.Patterns, pattern => pattern.Id == "onion-architecture");
        Assert.Contains(appProfile.Patterns, pattern => pattern.Id == "domain-driven-design");
        Assert.Contains(appProfile.Patterns, pattern => pattern.Id == "anti-corruption-layer");
        Assert.True(appProfile.Resilience.Retry.Enabled);
        Assert.Equal(5, appProfile.Resilience.Retry.MaxAttempts);
        Assert.Equal("Exponential", appProfile.Resilience.Retry.Backoff);
        Assert.Equal(200, appProfile.Resilience.Retry.BaseDelayMilliseconds);
        Assert.Equal(2000, appProfile.Resilience.Retry.MaxDelayMilliseconds);
        Assert.True(appProfile.Resilience.Retry.UseJitter);
        Assert.True(appProfile.Resilience.Timeout.Enabled);
        Assert.Equal(30, appProfile.Resilience.Timeout.TotalTimeoutSeconds);
        Assert.Equal(10, appProfile.Resilience.Timeout.AttemptTimeoutSeconds);
        Assert.True(appProfile.Resilience.CircuitBreaker.Enabled);
        Assert.Equal(0.2m, appProfile.Resilience.CircuitBreaker.FailureRatio);
        Assert.Equal(20, appProfile.Resilience.CircuitBreaker.MinimumThroughput);
        Assert.Equal(15, appProfile.Resilience.CircuitBreaker.SamplingDurationSeconds);
        Assert.Equal(30, appProfile.Resilience.CircuitBreaker.BreakDurationSeconds);
        Assert.True(appProfile.Resilience.Bulkhead.Enabled);
        Assert.Equal(64, appProfile.Resilience.Bulkhead.MaxConcurrentExecutions);
        Assert.Equal(128, appProfile.Resilience.Bulkhead.MaxQueuedActions);
        Assert.True(appProfile.Resilience.RateLimiting.Enabled);
        Assert.Equal("SlidingWindow", appProfile.Resilience.RateLimiting.Algorithm);
        Assert.Equal(500, appProfile.Resilience.RateLimiting.PermitLimit);
        Assert.Equal(50, appProfile.Resilience.RateLimiting.QueueLimit);
        Assert.Equal(60, appProfile.Resilience.RateLimiting.WindowSeconds);
        Assert.Equal(1, appProfile.Resilience.RateLimiting.SegmentsPerWindow);
        Assert.Empty(appProfile.Resilience.BehaviorExecutionOverrides);
    }

    [Fact]
    public void BuildIncludesBehaviorExecutionResilienceOverrides()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            resilience: new ResilienceSettings(
                timeout: new TimeoutSettings(
                    enabled: true,
                    totalTimeoutSeconds: 30),
                behaviorExecutionOverrides:
                [
                    new BehaviorExecutionResilienceOverrideSettings(
                        id: "catalog-rest-fast-path",
                        behaviorIds: ["catalog.get-product"],
                        transportIds: ["rest-api"],
                        timeout: new TimeoutSettings(enabled: false),
                        bulkhead: new BulkheadSettings(
                            maxConcurrentExecutions: 128,
                            maxQueuedActions: 256))
                ])));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new DiscoveryTestModule());

        var appProfile = builder.Build().Manifest.AppProfile;

        var overrideSelection = Assert.Single(appProfile.Resilience.BehaviorExecutionOverrides);
        Assert.Equal("catalog-rest-fast-path", overrideSelection.Id);
        Assert.Equal(["catalog.get-product"], overrideSelection.BehaviorIds);
        Assert.Equal(["rest-api"], overrideSelection.TransportIds);
        Assert.False(overrideSelection.Timeout.Enabled);
        Assert.Equal(128, overrideSelection.Bulkhead.MaxConcurrentExecutions);
        Assert.Equal(256, overrideSelection.Bulkhead.MaxQueuedActions);
    }

    [Fact]
    public void BuiltInPatternsIncludePhase11TaxonomyEntries()
    {
        var onionArchitecture = BuiltInPatterns.Resolve("Onion");
        var antiCorruptionLayer = BuiltInPatterns.Resolve("ACL");

        Assert.Equal("onion-architecture", onionArchitecture.Id);
        Assert.Equal(PatternKind.Architecture, onionArchitecture.Kind);
        Assert.Equal("anti-corruption-layer", antiCorruptionLayer.Id);
        Assert.Equal(PatternKind.Domain, antiCorruptionLayer.Kind);
    }

    [Fact]
    public void BuiltInPatternsIncludePhase12TaxonomyEntries()
    {
        var stranglerFig = BuiltInPatterns.Resolve("StranglerFig");
        var backendForFrontend = BuiltInPatterns.Resolve("BFF");

        Assert.Equal("strangler-fig", stranglerFig.Id);
        Assert.Equal(PatternKind.Architecture, stranglerFig.Kind);
        Assert.Equal("backend-for-frontend", backendForFrontend.Id);
        Assert.Equal(PatternKind.Architecture, backendForFrontend.Kind);
    }

    [Fact]
    public void BuildIncludesPhase12PatternTaxonomySelections()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "Microservice",
            patterns: ["StranglerFig", "BFF"]));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new DiscoveryTestModule());

        var appProfile = builder.Build().Manifest.AppProfile;

        Assert.Contains(appProfile.Patterns, pattern => pattern.Id == "strangler-fig");
        Assert.Contains(appProfile.Patterns, pattern => pattern.Id == "backend-for-frontend");
    }

    [Fact]
    public void BuildThrowsWhenReadWriteSplitIsConfiguredWithoutCqrsPattern()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            data: new DataSettings(readWriteSplit: true)));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("cqrs", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildThrowsWhenReadDatabaseRoleIsConfiguredWithoutCqrsPattern()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            databases: new DatabaseTopologySettings(
                read: new DatabaseTargetSettings(
                    provider: "PostgreSql",
                    connectionStringName: "ReadDb"))));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("cqrs", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildThrowsWhenOutboxDatabaseRoleIsConfiguredWithoutOutboxPattern()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            databases: new DatabaseTopologySettings(
                outbox: new DatabaseTargetSettings(
                    provider: "PostgreSql",
                    connectionStringName: "WriteDb"))));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("outbox", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildThrowsWhenWriteDatabaseRoleUsesUseRole()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            databases: new DatabaseTopologySettings(
                write: new DatabaseTargetSettings(useRole: "write"))));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("Write", exception.Message, StringComparison.Ordinal);
        Assert.Contains("UseRole", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildThrowsWhenHistoryDatabaseRoleUsesUnsupportedRoleReference()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            patterns: ["Outbox"],
            databases: new DatabaseTopologySettings(
                write: new DatabaseTargetSettings(
                    provider: "PostgreSql",
                    connectionStringName: "WriteDb"),
                history: new DatabaseTargetSettings(useRole: "read")),
            audit: new AuditSettings(
                enabled: true,
                history: new AuditHistorySettings(
                    enabled: true,
                    provider: "entity-framework"))));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("History", exception.Message, StringComparison.Ordinal);
        Assert.Contains("UseRole", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("write", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildThrowsWhenDatabaseMigrationTargetsReferenceMissingRoles()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            patterns: ["Outbox"],
            databases: new DatabaseTopologySettings(
                write: new DatabaseTargetSettings(
                    provider: "PostgreSql",
                    connectionStringName: "WriteDb"),
                migrations: new DatabaseMigrationsSettings(
                    applyOnStartup: true,
                    targets: ["history"]))));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("history", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildThrowsWhenDatabaseMigrationsExitAfterApplyWithoutStartupApply()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            databases: new DatabaseTopologySettings(
                write: new DatabaseTargetSettings(
                    provider: "PostgreSql",
                    connectionStringName: "WriteDb"),
                migrations: new DatabaseMigrationsSettings(
                    applyOnStartup: false,
                    exitAfterApply: true,
                    targets: ["write"]))));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("ApplyOnStartup", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildThrowsWhenMessagingProviderIsConfiguredWithoutEventDrivenTechnology()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            messaging: new MessagingSettings(provider: "Wolverine")));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("event-driven-integration", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildThrowsWhenResilienceRetryBackoffIsUnsupported()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            resilience: new ResilienceSettings(
                retry: new RetrySettings(
                    enabled: true,
                    maxAttempts: 3,
                    backoff: "Quadratic"))));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new DiscoveryTestModule());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("backoff", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Quadratic", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildThrowsWhenIdentityUsesUnsupportedAuthorizationMode()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            technologies: ["IdentityAccess"],
            identity: new IdentitySettings(
                enabled: true,
                authorizationModes: ["CustomMode"])));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("CustomMode", exception.Message, StringComparison.Ordinal);
        Assert.Contains("RBAC", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExposesPhase8DataProductCdcProjectionInboxOutboxAndAuthorizationCatalogsThroughRuntimeSnapshot()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                patterns: ["CQRS"],
                technologies: ["IdentityAccess"],
                transports: ["RestApi"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
        });

        using var provider = services.BuildServiceProvider();
        var dataProductCatalog = provider.GetRequiredService<IDataProductCatalog>();
        var cdcCaptureCatalog = provider.GetRequiredService<ICdcCaptureCatalog>();
        var projectionCatalog = provider.GetRequiredService<IProjectionCatalog>();
        var inboxCatalog = provider.GetRequiredService<IInboxCatalog>();
        var outboxCatalog = provider.GetRequiredService<IOutboxCatalog>();
        var auditStoreCatalog = provider.GetRequiredService<IAuditStoreCatalog>();
        var authorizationCatalog = provider.GetRequiredService<IAuthorizationPolicyCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        var dataProduct = Assert.Single(dataProductCatalog.DataProducts);
        Assert.Equal("tenant-profile", dataProduct.Id);
        Assert.Equal("phase8-runtime-catalogs", dataProduct.SourceModuleId);
        Assert.Equal("tenant-management", dataProduct.DomainId);
        Assert.Equal("tenant-profile-v1", dataProduct.ContractId);
        Assert.Equal("near-real-time", dataProduct.Metadata["freshness"]);
        Assert.Same(dataProduct, dataProductCatalog.GetById("tenant-profile"));
        Assert.Single(dataProductCatalog.GetBySourceModule("phase8-runtime-catalogs"));
        Assert.Single(dataProductCatalog.GetByDomainId("tenant-management"));
        Assert.Single(dataProductCatalog.GetByContractId("tenant-profile-v1"));

        var cdcCapture = Assert.Single(cdcCaptureCatalog.CdcCaptures);
        Assert.Equal("tenant-profile-cdc", cdcCapture.Id);
        Assert.Equal("phase8-runtime-catalogs", cdcCapture.SourceModuleId);
        Assert.Equal("postgresql", cdcCapture.Provider);
        Assert.Equal("tenant-db", cdcCapture.SourceId);
        Assert.Equal("tenant-event-outbox", cdcCapture.OutboxId);
        Assert.Equal("wal", cdcCapture.Mode);
        Assert.Equal("debezium-envelope", cdcCapture.EventFormat);
        Assert.Equal("outbox", cdcCapture.Metadata["publicationMode"]);
        Assert.Same(cdcCapture, cdcCaptureCatalog.GetById("tenant-profile-cdc"));
        Assert.Single(cdcCaptureCatalog.GetBySourceModule("phase8-runtime-catalogs"));
        Assert.Single(cdcCaptureCatalog.GetByProvider("postgresql"));
        Assert.Single(cdcCaptureCatalog.GetByOutboxId("tenant-event-outbox"));
        Assert.Single(cdcCaptureCatalog.GetBySourceId("tenant-db"));
        Assert.Single(cdcCaptureCatalog.GetByResourceId("public.tenants"));

        var projection = Assert.Single(projectionCatalog.Projections);
        Assert.Equal("tenant-summary", projection.Id);
        Assert.Equal("phase8-runtime-catalogs", projection.SourceModuleId);
        Assert.Equal("tenant-summary-read-model", projection.TargetStoreId);
        Assert.Equal("eventual", projection.Metadata["consistency"]);
        Assert.Same(projection, projectionCatalog.GetById("tenant-summary"));
        Assert.Single(projectionCatalog.GetBySourceModule("phase8-runtime-catalogs"));
        Assert.Single(projectionCatalog.GetByTargetStore("tenant-summary-read-model"));

        var outbox = Assert.Single(outboxCatalog.Outboxes);
        Assert.Equal("tenant-event-outbox", outbox.Id);
        Assert.Equal("phase8-runtime-catalogs", outbox.SourceModuleId);
        Assert.Equal("relational", outbox.Provider);
        Assert.Equal(["audit", "tenant-events"], outbox.ChannelIds);
        Assert.Equal("durable", outbox.Metadata["consistency"]);
        Assert.Same(outbox, outboxCatalog.GetById("tenant-event-outbox"));
        Assert.Single(outboxCatalog.GetBySourceModule("phase8-runtime-catalogs"));
        Assert.Single(outboxCatalog.GetByProvider("relational"));
        Assert.Single(outboxCatalog.GetByChannelId("audit"));

        var inbox = Assert.Single(inboxCatalog.Inboxes);
        Assert.Equal("tenant-event-inbox", inbox.Id);
        Assert.Equal("phase8-runtime-catalogs", inbox.SourceModuleId);
        Assert.Equal("relational", inbox.Provider);
        Assert.Equal(["tenant-events"], inbox.ChannelIds);
        Assert.Equal("message-id", inbox.Metadata["idempotency"]);
        Assert.Same(inbox, inboxCatalog.GetById("tenant-event-inbox"));
        Assert.Single(inboxCatalog.GetBySourceModule("phase8-runtime-catalogs"));
        Assert.Single(inboxCatalog.GetByProvider("relational"));
        Assert.Single(inboxCatalog.GetByChannelId("tenant-events"));

        var auditStore = Assert.Single(auditStoreCatalog.AuditStores);
        Assert.Equal("tenant-audit-store", auditStore.Id);
        Assert.Equal("phase8-runtime-catalogs", auditStore.SourceModuleId);
        Assert.Equal("memory", auditStore.Provider);
        Assert.Equal("volatile-buffer", auditStore.Mode);
        Assert.Equal("application-managed", auditStore.Metadata["writeMode"]);
        Assert.Same(auditStore, auditStoreCatalog.GetById("tenant-audit-store"));
        Assert.Single(auditStoreCatalog.GetBySourceModule("phase8-runtime-catalogs"));
        Assert.Single(auditStoreCatalog.GetByProvider("memory"));

        Assert.Equal(2, authorizationCatalog.Policies.Count);
        var tenantAdminPolicy = authorizationCatalog.GetById("tenant-admin");
        Assert.NotNull(tenantAdminPolicy);
        Assert.Equal("phase8-runtime-catalogs", tenantAdminPolicy!.Metadata["sourceModuleId"]);
        Assert.Single(authorizationCatalog.GetByMode(AuthorizationMode.Rbac));
        Assert.Equal(2, authorizationCatalog.GetByMode(AuthorizationMode.Policy).Count);

        Assert.Single(snapshot.DataProducts);
        Assert.Single(snapshot.CdcCaptures);
        Assert.Single(snapshot.Projections);
        Assert.Single(snapshot.Inboxes);
        Assert.Single(snapshot.Outboxes);
        Assert.Single(snapshot.AuditStores);
        Assert.Equal(2, snapshot.AuthorizationPolicies.Count);
        Assert.Contains(snapshot.DataProducts, item => item.Id == "tenant-profile");
        Assert.Contains(snapshot.CdcCaptures, item => item.Id == "tenant-profile-cdc");
        Assert.Contains(snapshot.Projections, item => item.Id == "tenant-summary");
        Assert.Contains(snapshot.Inboxes, item => item.Id == "tenant-event-inbox");
        Assert.Contains(snapshot.Outboxes, item => item.Id == "tenant-event-outbox");
        Assert.Contains(snapshot.AuditStores, item => item.Id == "tenant-audit-store");
        Assert.Contains(snapshot.AuthorizationPolicies, item => item.Id == "tenant-boundary");
    }

    [Fact]
    public void BuildRejectsPhase8DataProductThatSpoofsItsSourceModule()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new InvalidDataProductSourceModule());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("broken-data-product", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("another-module", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildRejectsPhase8CdcCaptureThatSpoofsItsSourceModule()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new Phase8CatalogModule());
        builder.AddModule(new InvalidCdcCaptureSourceModule());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("broken-cdc-capture", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("another-module", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildRejectsPhase8CdcCaptureWhenReferencedOutboxIsMissing()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new InvalidCdcCaptureOutboxModule());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("missing-outbox-cdc-capture", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("missing-outbox", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildRejectsPhase8ProjectionThatSpoofsItsSourceModule()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new InvalidProjectionSourceModule());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("broken-projection", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("another-module", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildRejectsPhase8OutboxThatSpoofsItsSourceModule()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new InvalidOutboxSourceModule());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("broken-outbox", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("another-module", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildRejectsPhase8InboxThatSpoofsItsSourceModule()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new InvalidInboxSourceModule());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("broken-inbox", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("another-module", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildRejectsPhase8AuditStoreThatSpoofsItsSourceModule()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new InvalidAuditStoreSourceModule());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("broken-audit-store", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("another-module", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildAllowsCustomTechnologyDescriptorsAndValidatesRequirements()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            transports: ["RestApi"]));
        builder.AddTechnology(new TechnologyDescriptor(
            id: "live-orchestration",
            displayName: "Live Orchestration",
            description: "Custom future-facing technology profile used by a host.",
            kind: TechnologyKind.Intelligence,
            requiresTransports: ["websocket"]));

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("live-orchestration", exception.Message, StringComparison.Ordinal);
        Assert.Contains("websocket", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildAllowsTechnologyRegistrationAfterConfigurationSelection()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            transports: ["WebSocket"],
            technologies: ["DigitalTwinOrchestration"]));
        builder.RegisterTechnology(new TechnologyDescriptor(
            id: "digital-twin-orchestration",
            displayName: "Digital Twin Orchestration",
            description: "Project-level technology override registered after config binding.",
            kind: TechnologyKind.Experience,
            requiresTransports: ["websocket"]));

        var runtime = builder.Build();

        Assert.Contains(runtime.Manifest.AppProfile.Technologies, technology => technology.Id == "digital-twin-orchestration");
    }

    [Fact]
    public void BuildAllowsModulesToContributeTechnologyProfiles()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularVerticalSlice",
            transports: ["WebSocket"],
            technologies: ["DigitalTwinOrchestration"]));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new TechnologyCatalogTestModule());

        var runtime = builder.Build();
        var technology = Assert.Single(
            runtime.Manifest.AppProfile.Technologies,
            technology => technology.Id == "digital-twin-orchestration");
        var scaffold = Assert.IsType<ScaffoldPlan>(runtime.Manifest.AppProfile.Scaffold);

        Assert.Equal("Digital Twin Orchestration", technology.DisplayName);
        Assert.Contains(scaffold.Conventions, convention =>
            convention.Contains("Digital Twin Orchestration", StringComparison.Ordinal));
        Assert.Contains(scaffold.Projects, project =>
            project.Role == ProjectRoles.Host &&
            project.Packages.Contains("Cephalon.DigitalTwin", StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildRegistersTechnologySelectionAndActivatesTechnologyAwareModules()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["DigitalTwinOrchestration"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new TechnologyCatalogTestModule());
            engine.AddModule(new TechnologyAwareModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var technologySelection = provider.GetRequiredService<TechnologySelection>();
        var marker = provider.GetRequiredService<TechnologyActivationMarker>();

        Assert.True(technologySelection.IsSelected("DigitalTwinOrchestration"));
        Assert.True(technologySelection.IsAvailable("AgenticWorkloads"));
        Assert.Equal("digital-twin-orchestration", marker.TechnologyId);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "technology-aware.base");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "technology-aware.digital-twin");
    }

    [Fact]
    public async Task AddTechnologyPacksRegisterServicesAndCapabilitiesWhenSelectionsAreActive()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["AgenticWorkloads", "EventDrivenIntegration", "KnowledgeRetrieval", "EdgeNativeDelivery"]));
            engine.AddAgentics(options =>
            {
                options.Tools.Add(new AgentToolDescriptor(
                    id: "planner",
                    displayName: "Planner",
                    description: "Creates agent plans.",
                    tags: ["planning"]));
            });
            engine.AddRetrieval(options =>
            {
                options.DefaultQueryLimit = 1;
                options.Collections.Add(new KnowledgeCollectionDescriptor(
                    id: "docs",
                    displayName: "Docs",
                    description: "Knowledge base for retrieval.",
                    tags: ["docs"]));
            });
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "orders",
                    displayName: "Orders",
                    description: "Integration events for the order domain.",
                    tags: ["orders"]));
            });
            engine.AddEdge(options =>
            {
                options.Nodes.Add(new EdgeNodeDescriptor(
                    id: "storefront-edge",
                    displayName: "Storefront Edge",
                    description: "Regional node serving intermittently connected storefront experiences.",
                    tags: ["storefront"]));
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var toolCatalog = provider.GetRequiredService<IAgentToolCatalog>();
        var knowledgeCatalog = provider.GetRequiredService<IKnowledgeCatalog>();
        var knowledgeIndexer = provider.GetRequiredService<IKnowledgeIndexer>();
        var knowledgeQueryEngine = provider.GetRequiredService<IKnowledgeQueryEngine>();
        var knowledgeIndexCatalog = provider.GetRequiredService<IKnowledgeIndexCatalog>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        var eventChannelCatalog = provider.GetRequiredService<IEventChannelCatalog>();
        var eventSubscriptionCatalog = provider.GetRequiredService<IEventSubscriptionCatalog>();
        var eventSubscriptionBindingCatalog = provider.GetRequiredService<IEventSubscriptionExecutionBindingCatalog>();
        var eventSubscriptionReadinessCatalog = provider.GetRequiredService<IEventSubscriptionExecutionReadinessCatalog>();
        var subscriptionRuntimeCatalog = provider.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var subscriptionRuntimeReporter = provider.GetRequiredService<IEventSubscriptionRuntimeReporter>();
        var edgeNodeCatalog = provider.GetRequiredService<IEdgeNodeCatalog>();
        var technologySurfaces = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var eventPublisher = provider.GetService<IEventPublisher>();

        await subscriptionRuntimeReporter.ReportAsync(
            new EventSubscriptionExecutionReport(
                subscriptionId: "audit-projector",
                outcome: EventSubscriptionExecutionOutcomes.Started,
                observedAtUtc: new DateTimeOffset(2026, 04, 04, 9, 30, 0, TimeSpan.Zero),
                messageId: "audit-msg-001",
                attempt: 1));
        await subscriptionRuntimeReporter.ReportAsync(
            new EventSubscriptionExecutionReport(
                subscriptionId: "audit-projector",
                outcome: EventSubscriptionExecutionOutcomes.RetryScheduled,
                observedAtUtc: new DateTimeOffset(2026, 04, 04, 9, 31, 0, TimeSpan.Zero),
                messageId: "audit-msg-001",
                attempt: 2,
                error: "Transient projection failure",
                metadata: new Dictionary<string, string>
                {
                    ["nextRetryAtUtc"] = "2026-04-04T09:36:00.0000000+00:00",
                    ["retryPolicy"] = "exponential"
                }));
        var indexingResult = await knowledgeIndexer.IndexAsync(new KnowledgeIndexingRequest(
            collectionId: "runbooks",
            runId: "composition-retrieval-index-001",
            actorId: "composition-test",
            correlationId: "corr-composition-retrieval-001",
            requestedAtUtc: new DateTimeOffset(2026, 04, 04, 9, 32, 0, TimeSpan.Zero)));
        var queryResult = await knowledgeQueryEngine.QueryAsync(new KnowledgeQueryRequest(
            collectionId: "runbooks",
            queryText: "runbook",
            actorId: "composition-test",
            correlationId: "corr-composition-retrieval-query-001"));

        var snapshotProvider = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>();
        var snapshot = snapshotProvider.CreateSnapshot();

        Assert.Equal(2, toolCatalog.Tools.Count);
        Assert.Contains(toolCatalog.Tools, tool => tool.Id == "planner");
        Assert.Contains(toolCatalog.Tools, tool => tool.Id == "analyst");
        Assert.Equal(2, knowledgeCatalog.Collections.Count);
        Assert.Contains(knowledgeCatalog.Collections, collection => collection.Id == "docs");
        Assert.Contains(knowledgeCatalog.Collections, collection => collection.Id == "runbooks");
        Assert.Equal(KnowledgeIndexingOutcomes.Succeeded, indexingResult.Outcome);
        Assert.Equal(2, indexingResult.DocumentCount);
        Assert.True(queryResult.HasMatches);
        Assert.Contains(queryResult.Matches, match => match.DocumentId == "runbook.incident-response");
        Assert.Single(queryResult.Matches);
        Assert.Equal("1", queryResult.Metadata["queryLimit"]);
        var knowledgeState = Assert.Single(knowledgeIndexCatalog.States);
        Assert.Equal("runbooks", knowledgeState.CollectionId);
        Assert.Equal(KnowledgeIndexingOutcomes.Succeeded, knowledgeState.LastOutcome);
        Assert.Equal(2, knowledgeState.DocumentCount);
        Assert.Equal(1, knowledgeState.QueryCount);
        Assert.Equal(1, knowledgeState.LastQueryMatchedCount);
        Assert.Equal("composition-test", knowledgeState.LastActorId);
        Assert.Equal("corr-composition-retrieval-query-001", knowledgeState.LastCorrelationId);
        Assert.Equal("1", knowledgeState.Metadata["queryLimit"]);
        Assert.False(string.IsNullOrWhiteSpace(knowledgeState.LastQueryFingerprint));
        Assert.Equal(2, eventChannelCatalog.Channels.Count);
        Assert.Contains(eventChannelCatalog.Channels, channel => channel.Id == "orders");
        Assert.Contains(eventChannelCatalog.Channels, channel => channel.Id == "audit");
        Assert.Single(eventSubscriptionCatalog.Subscriptions);
        Assert.Contains(eventSubscriptionCatalog.Subscriptions, subscription => subscription.Id == "audit-projector");
        Assert.Empty(eventSubscriptionBindingCatalog.Bindings);
        Assert.Null(eventSubscriptionBindingCatalog.GetBySubscriptionId("audit-projector"));
        Assert.False(eventSubscriptionBindingCatalog.TryGet("audit-projector", out var unboundSubscription));
        Assert.Null(unboundSubscription);
        var subscriptionReadiness = Assert.Single(eventSubscriptionReadinessCatalog.Readiness);
        Assert.Equal("audit-projector", subscriptionReadiness.SubscriptionId);
        Assert.Equal(EventSubscriptionExecutionReadinessStates.HostedExecutionLinked, subscriptionReadiness.ReadinessState);
        Assert.True(subscriptionReadiness.HasExecutionPath);
        Assert.Equal("application-managed", subscriptionReadiness.ExecutionOwnership);
        Assert.Equal("hosted-execution", subscriptionReadiness.ExecutionMode);
        Assert.Contains("hosted-execution-linked", subscriptionReadiness.Reasons);
        Assert.Contains("runtime-state-reported", subscriptionReadiness.Reasons);
        Assert.Equal(
            subscriptionReadiness.ReadinessState,
            eventSubscriptionReadinessCatalog.GetBySubscriptionId("audit-projector")?.ReadinessState);
        Assert.Equal(2, edgeNodeCatalog.Nodes.Count);
        Assert.Contains(edgeNodeCatalog.Nodes, node => node.Id == "storefront-edge");
        Assert.Contains(edgeNodeCatalog.Nodes, node => node.Id == "warehouse-edge");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "agentics.runtime");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "agentics.tools");
        Assert.Null(eventPublisher);
        Assert.DoesNotContain(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.publish");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.channels");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.subscriptions");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "retrieval.query");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "retrieval.collections");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "edge.offline");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "edge.nodes");
        var subscriptionState = Assert.Single(subscriptionRuntimeCatalog.States);
        Assert.Equal("audit-projector", subscriptionState.SubscriptionId);
        Assert.Equal(EventSubscriptionExecutionOutcomes.RetryScheduled, subscriptionState.LastOutcome);
        Assert.Equal("audit-msg-001", subscriptionState.LastMessageId);
        Assert.Equal(2, subscriptionState.LastAttempt);
        Assert.Equal(1, subscriptionState.StartedCount);
        Assert.Equal(1, subscriptionState.RetryScheduledCount);
        Assert.Equal(2, subscriptionState.TotalReports);
        Assert.True(subscriptionState.RetryPending);
        var eventingConvention = Assert.Single(diagnosticsCatalog.Conventions, convention => convention.Source == "Cephalon.Eventing");
        Assert.Equal(4200, eventingConvention.MinimumEventId);
        Assert.Equal(4210, eventingConvention.MaximumEventId);
        Assert.Contains(eventingConvention.Events, entry => entry.Id == 4200 && entry.Name == "EventPublicationStaged");
        Assert.Contains(eventingConvention.Events, entry => entry.Id == 4204 && entry.Name == "EventSubscriptionRetryScheduled");
        Assert.Contains(eventingConvention.Events, entry => entry.Id == 4209 && entry.Name == "EventPublicationDispatchRetryScheduled");
        Assert.Equal(6, technologySurfaces.Surfaces.Count);
        Assert.Single(technologySurfaces.GetByTechnology("agentic-workloads"));
        Assert.Contains(
            technologySurfaces.GetByTechnology("event-driven-integration")
                .Single(surface => surface.SurfaceId == "event-channels")
                .Entries,
            entry => entry.Id == "audit");
        Assert.Contains(
            technologySurfaces.GetByTechnology("event-driven-integration")
                .Single(surface => surface.SurfaceId == "event-subscriptions")
                .Entries,
                entry => entry.Id == "audit-projector" &&
                    entry.Metadata[EventSubscriptionRuntimeMetadataKeys.ChannelId] == "audit" &&
                    entry.Metadata[EventSubscriptionRuntimeMetadataKeys.DispatchRuntime] == "application-managed" &&
                    entry.Metadata[EventSubscriptionRuntimeMetadataKeys.RuntimeState] == "reported" &&
                    entry.Metadata[EventSubscriptionRuntimeMetadataKeys.SubscriptionRuntime] == "hosted-execution-linked" &&
                    entry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionReadiness] == EventSubscriptionExecutionReadinessStates.HostedExecutionLinked &&
                    entry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionPath] == "observed" &&
                    entry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionReadinessReasons].Contains("hosted-execution-linked", StringComparison.OrdinalIgnoreCase) &&
                    entry.Metadata[EventSubscriptionRuntimeMetadataKeys.HostedExecutionId] == "audit-projector-pump" &&
                    entry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionGraphId] == "audit-subscription-flow" &&
                    entry.Metadata["executionGraphDisplayName"] == "Audit Subscription Flow" &&
                    entry.Metadata[EventSubscriptionRuntimeMetadataKeys.LastOutcome] == "retry-scheduled" &&
                    entry.Metadata["lastMessageId"] == "audit-msg-001" &&
                    entry.Metadata["lastAttempt"] == "2" &&
                    entry.Metadata["retryScheduledCount"] == "1" &&
                    entry.Metadata["totalReports"] == "2" &&
                    entry.Metadata[EventSubscriptionRuntimeMetadataKeys.RetryPending] == "true" &&
                    entry.Metadata[$"{EventSubscriptionRuntimeMetadataKeys.ReportedMetadataPrefix}nextRetryAtUtc"] == "2026-04-04T09:36:00.0000000+00:00" &&
                    entry.Metadata[$"{EventSubscriptionRuntimeMetadataKeys.ReportedMetadataPrefix}retryPolicy"] == "exponential" &&
                    entry.Metadata["lastError"] == "Transient projection failure");
        Assert.Same(runtime.Manifest, snapshot.Manifest);
        Assert.Equal(RuntimeStatus.Created, snapshot.Status.Status);
        Assert.Equal(6, snapshot.TechnologySurfaces.Count);
        Assert.Contains(
            snapshot.TechnologySurfaces.Single(surface => surface.TechnologyId == "knowledge-retrieval").Entries,
            entry => entry.Id == "runbooks" &&
                entry.Metadata["indexingOwnership"] == "cephalon-managed" &&
                entry.Metadata["queryOwnership"] == "cephalon-managed" &&
                entry.Metadata["runtimeState"] == "indexed" &&
                entry.Metadata["freshnessState"] == KnowledgeIndexFreshnessStates.Fresh &&
                entry.Metadata["documentCount"] == "2" &&
                entry.Metadata["queryCount"] == "1");
        Assert.Contains(
            snapshot.KnowledgeIndexes,
            state => state.CollectionId == "runbooks" &&
                state.LastOutcome == KnowledgeIndexingOutcomes.Succeeded &&
                state.FreshnessState == KnowledgeIndexFreshnessStates.Fresh &&
                state.DocumentCount == 2 &&
                state.QueryCount == 1);
        Assert.Contains(snapshot.DiagnosticsConventions, convention => convention.Source == "Cephalon.Eventing");
        Assert.Contains(
            snapshot.TechnologySurfaces.Single(surface => surface.SurfaceId == "event-subscriptions").Entries,
            entry => entry.Id == "audit-projector" &&
                entry.Metadata[EventSubscriptionRuntimeMetadataKeys.LastOutcome] == "retry-scheduled" &&
                entry.Metadata[EventSubscriptionRuntimeMetadataKeys.RetryPending] == "true");
    }

    [Fact]
    public async Task AddEventingProjectsSuperiorityProfileWithoutWolverine()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
                options.InProcessSubscriptionMaxAttempts = 2;
                options.EnableInProcessSubscriptionIdempotency = true;
            });
        });

        await using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        var profileSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "eventing-superiority-profile");
        var dimensions = profileSurface.Entries.ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var capability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.superiority-profile");
        Assert.Equal("true", capability.Metadata["wolverineOptional"]);
        Assert.Equal("claimed-only-with-runtime-evidence", capability.Metadata["claimPolicy"]);
        Assert.Equal("eventing-superiority-profile", capability.Metadata["surfaceId"]);

        Assert.Equal("claimed", dimensions["configuration-first-provider-neutrality"].Metadata["status"]);
        Assert.Equal("claimed", dimensions["native-wolverine-free-baseline"].Metadata["status"]);
        Assert.Equal("claimed", dimensions["runtime-truth-and-operator-surfaces"].Metadata["status"]);
        Assert.Equal("claimed", dimensions["recoverability-and-terminal-failure-posture"].Metadata["status"]);
        Assert.Equal("claimed", dimensions["mediator-style-in-process-low-ceremony"].Metadata["status"]);
        Assert.Equal("partial", dimensions["native-remediation-operator-read-performance"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["durable-remediation-command-audit"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["durable-command-journal-replay-cursor"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["broker-dead-letter-replay-ownership"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["broker-topology-materialization-ownership"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["provider-partition-ownership"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["downstream-delivery-completion-ownership"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["broker-inbound-consumption-ownership"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["serialization-and-contract-versioning-ownership"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["tenant-and-correlation-context-ownership"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["scheduled-and-delayed-delivery-ownership"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["durable-retry-queue-ownership"].Metadata["status"]);
        Assert.Equal("partial", dimensions["idempotency-ownership"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["subscription-concurrency-ownership"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["subscription-ordering-ownership"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["process-manager-state-ownership"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["choreography-handoff-ownership"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["durability-and-outbox-portability"].Metadata["status"]);
        Assert.Equal("not-claimed", dimensions["dead-letter-replay-and-remediation"].Metadata["status"]);
        Assert.Equal("MassTransit,NServiceBus,Wolverine,MediatR", dimensions["native-wolverine-free-baseline"].Metadata["referenceFrameworks"]);
        Assert.Contains("Wolverine remains optional", dimensions["native-wolverine-free-baseline"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("EnableInProcessSubscriptionExecution=true", dimensions["mediator-style-in-process-low-ceremony"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("FilterOperatorDashboardSelectors", dimensions["native-remediation-operator-read-performance"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("no outbox-backed command path is active", dimensions["native-remediation-operator-read-performance"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("no remediation command journal is active", dimensions["durable-remediation-command-audit"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("no remediation command journal is active", dimensions["durable-command-journal-replay-cursor"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("brokerDeadLetterQueueOwnership=not-claimed", dimensions["broker-dead-letter-replay-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("brokerReplay=not-claimed", dimensions["broker-dead-letter-replay-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["broker-dead-letter-replay-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("brokerTopologyMaterialization=not-claimed", dimensions["broker-topology-materialization-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("providerOwnedTopology=not-present", dimensions["broker-topology-materialization-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["broker-topology-materialization-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("providerPartitionOwnership=not-claimed", dimensions["provider-partition-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("providerOwnedPartitioning=not-present", dimensions["provider-partition-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["provider-partition-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("downstreamDeliveryCompletion=not-claimed", dimensions["downstream-delivery-completion-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("providerDeliveryReceipt=not-present", dimensions["downstream-delivery-completion-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["downstream-delivery-completion-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("brokerInboundConsumption=not-claimed", dimensions["broker-inbound-consumption-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("providerOwnedConsumer=not-present", dimensions["broker-inbound-consumption-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("inboundAcknowledgement=not-claimed", dimensions["broker-inbound-consumption-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["broker-inbound-consumption-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("serializerSelection=not-claimed", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("schemaRegistry=not-present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("contractVersionNegotiation=not-claimed", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("operatorCorrelationMetadata=metadata-only", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("tenantContextPropagation=not-claimed", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("correlationContextPropagation=not-claimed", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("causationIdPropagation=not-claimed", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("baggagePropagation=not-claimed", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("publicationScheduling=not-configured", dimensions["scheduled-and-delayed-delivery-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("durableScheduledDelivery=not-claimed", dimensions["scheduled-and-delayed-delivery-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("providerDelayQueue=not-present", dimensions["scheduled-and-delayed-delivery-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("crossNodeScheduleCoordination=not-claimed", dimensions["scheduled-and-delayed-delivery-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("scheduleRecovery=not-claimed", dimensions["scheduled-and-delayed-delivery-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["scheduled-and-delayed-delivery-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("inProcessRetryPolicy=bounded-in-process", dimensions["durable-retry-queue-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("inProcessRetryMaxAttempts=2", dimensions["durable-retry-queue-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("durableRetryQueue=not-claimed", dimensions["durable-retry-queue-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("retryPersistence=not-claimed", dimensions["durable-retry-queue-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("brokerErrorQueue=not-claimed", dimensions["durable-retry-queue-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("crossNodeRetryCoordination=not-claimed", dimensions["durable-retry-queue-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("retryLease=not-claimed", dimensions["durable-retry-queue-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["durable-retry-queue-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("idempotencyPolicy=completed-publication", dimensions["idempotency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("idempotencyStore=process-local", dimensions["idempotency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("idempotencyScope=process-local", dimensions["idempotency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("idempotencyDurability=none", dimensions["idempotency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("idempotencyKeyShape=subscription-publication", dimensions["idempotency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("completedExecutionDuplicateSuppression=active", dimensions["idempotency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("brokerDeduplication=not-claimed", dimensions["idempotency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("exactlyOnceDelivery=not-claimed", dimensions["idempotency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("durableInboxCommandOwnership=not-claimed", dimensions["idempotency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("crossNodeIdempotencyLease=not-claimed", dimensions["idempotency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("providerIdempotency=not-claimed", dimensions["idempotency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["idempotency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("inProcessExecution=active", dimensions["subscription-concurrency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("subscriptionExecutionPipeline=none", dimensions["subscription-concurrency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("subscriptionExecutionMiddlewareCount=0", dimensions["subscription-concurrency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("subscriptionConcurrency=not-claimed", dimensions["subscription-concurrency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("perSubscriptionConcurrencyLimit=not-claimed", dimensions["subscription-concurrency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("parallelHandlerExecution=not-claimed", dimensions["subscription-concurrency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("consumerPrefetch=not-claimed", dimensions["subscription-concurrency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("backpressure=not-claimed", dimensions["subscription-concurrency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("providerConcurrency=not-present", dimensions["subscription-concurrency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("consumerLease=not-claimed", dimensions["subscription-concurrency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("workStealing=not-claimed", dimensions["subscription-concurrency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["subscription-concurrency-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("inProcessExecution=active", dimensions["subscription-ordering-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("subscriptionExecutionPipeline=none", dimensions["subscription-ordering-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("subscriptionExecutionMiddlewareCount=0", dimensions["subscription-ordering-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("subscriptionOrdering=not-claimed", dimensions["subscription-ordering-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("handlerOrderingGuarantee=not-claimed", dimensions["subscription-ordering-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("localFanOutOrdering=not-claimed", dimensions["subscription-ordering-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("perKeyOrdering=not-claimed", dimensions["subscription-ordering-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("partitionOrdering=not-claimed", dimensions["subscription-ordering-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("causalOrdering=not-claimed", dimensions["subscription-ordering-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("replayOrdering=not-claimed", dimensions["subscription-ordering-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("crossNodeOrdering=not-claimed", dimensions["subscription-ordering-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("providerOrdering=not-present", dimensions["subscription-ordering-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["subscription-ordering-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("publicationPath=active", dimensions["process-manager-state-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("declaredSubscriptions=present", dimensions["process-manager-state-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("inProcessExecution=active", dimensions["process-manager-state-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("subscriptionExecutionPipeline=none", dimensions["process-manager-state-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("processManagerState=not-claimed", dimensions["process-manager-state-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("sagaStatePersistence=not-claimed", dimensions["process-manager-state-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("sagaCorrelation=not-claimed", dimensions["process-manager-state-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("sagaTimeouts=not-claimed", dimensions["process-manager-state-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("compensationWorkflow=not-claimed", dimensions["process-manager-state-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("processManagerConcurrency=not-claimed", dimensions["process-manager-state-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("processManagerRecovery=not-claimed", dimensions["process-manager-state-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("providerProcessManager=not-present", dimensions["process-manager-state-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["process-manager-state-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("choreographyCatalog=not-present", dimensions["choreography-handoff-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("publicationStateCatalog=not-present", dimensions["choreography-handoff-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("eventingBridge=not-active", dimensions["choreography-handoff-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("handoffDurability=not-active", dimensions["choreography-handoff-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("processManagerState=not-claimed", dimensions["choreography-handoff-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["choreography-handoff-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
    }

    [Fact]
    public void AddEventingSelectsLatestProvenWireContractEvidenceAcrossOutboxesWithoutWolverine()
    {
        var olderWireContractReport = EventDispatchWireContractMetadata.CreateReport(
            new EventDispatchExecutionReport(
                outboxId: "alpha-outbox",
                channelId: "contracts",
                outcome: EventDispatchExecutionOutcomes.Succeeded,
                observedAtUtc: new DateTimeOffset(2026, 05, 13, 7, 0, 0, TimeSpan.Zero),
                messageId: "evt-alpha-001",
                attempt: 1),
            source: "alpha-wire-runtime",
            payloadSerializationExecutionId: "alpha-payload-serialization",
            wireEnvelopeSchemaExecutionId: "alpha-wire-envelope-schema",
            schemaLookupExecutionId: "alpha-schema-lookup",
            contractVersionNegotiationExecutionId: "alpha-contract-version-negotiation",
            upcasterExecutionId: "alpha-upcaster-execution",
            compatibilityValidationExecutionId: "alpha-compatibility-validation",
            providerSerializationId: "alpha-provider-serialization",
            wireContractProofId: "alpha-wire-contract-proof");
        var newerWireContractReport = EventDispatchWireContractMetadata.CreateReport(
            new EventDispatchExecutionReport(
                outboxId: "beta-outbox",
                channelId: "contracts",
                outcome: EventDispatchExecutionOutcomes.Succeeded,
                observedAtUtc: new DateTimeOffset(2026, 05, 13, 8, 30, 0, TimeSpan.Zero),
                messageId: "evt-beta-001",
                attempt: 1),
            source: "beta-wire-runtime",
            payloadSerializationExecutionId: "beta-payload-serialization",
            wireEnvelopeSchemaExecutionId: "beta-wire-envelope-schema",
            schemaLookupExecutionId: "beta-schema-lookup",
            contractVersionNegotiationExecutionId: "beta-contract-version-negotiation",
            upcasterExecutionId: "beta-upcaster-execution",
            compatibilityValidationExecutionId: "beta-compatibility-validation",
            providerSerializationId: "beta-provider-serialization",
            wireContractProofId: "beta-wire-contract-proof");
        var services = new ServiceCollection();
        services.AddSingleton<IEventDispatchRuntimeCatalog>(new TestEventDispatchRuntimeCatalog(
            CreateDispatchRuntimeState(olderWireContractReport),
            CreateDispatchRuntimeState(newerWireContractReport)));
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new MultiOutboxEventingTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "contracts",
                    displayName: "Contracts",
                    description: "Wire-contract proof events."));
            });
        });

        using var provider = services.BuildServiceProvider();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        Assert.DoesNotContain(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var dimensions = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "eventing-superiority-profile")
            .Entries
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        var evidence = dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"];

        Assert.Equal("claimed", dimensions["serialization-and-contract-versioning-ownership"].Metadata["status"]);
        Assert.Contains("runtimeProofSelection=latest-proven-dispatch-state", evidence, StringComparison.Ordinal);
        Assert.Contains("serializationExecutionStateCount=2", evidence, StringComparison.Ordinal);
        Assert.Contains("serializationExecutionProvenCount=2", evidence, StringComparison.Ordinal);
        Assert.Contains("wireContractStateCount=2", evidence, StringComparison.Ordinal);
        Assert.Contains("wireContractProvenCount=2", evidence, StringComparison.Ordinal);
        Assert.Contains("serializationExecutionOutboxId=beta-outbox", evidence, StringComparison.Ordinal);
        Assert.Contains("serializationExecutionLastObservedAtUtc=2026-05-13T08:30:00.0000000+00:00", evidence, StringComparison.Ordinal);
        Assert.Contains("wireContractOutboxId=beta-outbox", evidence, StringComparison.Ordinal);
        Assert.Contains("wireContractOwnershipSource=beta-wire-runtime", evidence, StringComparison.Ordinal);
        Assert.Contains("wireContractProofId=beta-wire-contract-proof", evidence, StringComparison.Ordinal);
        Assert.Contains("wireContractLastObservedAtUtc=2026-05-13T08:30:00.0000000+00:00", evidence, StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("wireContractProofId=alpha-wire-contract-proof", evidence, StringComparison.Ordinal);
    }

    [Fact]
    public void AddEventingSelectsLatestProvenScheduledDeliveryEvidenceAcrossOutboxesWithoutWolverine()
    {
        var olderScheduledDeliveryReport = EventDispatchScheduledDeliveryMetadata.CreateReport(
            new EventDispatchExecutionReport(
                outboxId: "alpha-outbox",
                channelId: "contracts",
                outcome: EventDispatchExecutionOutcomes.Succeeded,
                observedAtUtc: new DateTimeOffset(2026, 05, 13, 9, 0, 0, TimeSpan.Zero),
                messageId: "evt-alpha-schedule-001",
                attempt: 1),
            source: "alpha-schedule-runtime",
            durableScheduledDeliveryId: "alpha-durable-schedule",
            providerDelayQueueId: "alpha-provider-delay",
            brokerScheduledDeliveryId: "alpha-broker-schedule",
            scheduleCoordinationId: "alpha-schedule-coordination",
            scheduleRecoveryId: "alpha-schedule-recovery");
        var newerScheduledDeliveryReport = EventDispatchScheduledDeliveryMetadata.CreateReport(
            new EventDispatchExecutionReport(
                outboxId: "beta-outbox",
                channelId: "contracts",
                outcome: EventDispatchExecutionOutcomes.Succeeded,
                observedAtUtc: new DateTimeOffset(2026, 05, 13, 9, 30, 0, TimeSpan.Zero),
                messageId: "evt-beta-schedule-001",
                attempt: 1),
            source: "beta-schedule-runtime",
            durableScheduledDeliveryId: "beta-durable-schedule",
            providerDelayQueueId: "beta-provider-delay",
            brokerScheduledDeliveryId: "beta-broker-schedule",
            scheduleCoordinationId: "beta-schedule-coordination",
            scheduleRecoveryId: "beta-schedule-recovery");
        var services = new ServiceCollection();
        services.AddSingleton<IEventDispatchRuntimeCatalog>(new TestEventDispatchRuntimeCatalog(
            CreateDispatchRuntimeState(olderScheduledDeliveryReport),
            CreateDispatchRuntimeState(newerScheduledDeliveryReport)));
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS", "Outbox"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddModule(new MultiOutboxEventingTestModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "contracts",
                    displayName: "Contracts",
                    description: "Scheduled delivery proof events."));
            });
        });

        using var provider = services.BuildServiceProvider();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        Assert.DoesNotContain(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var dimensions = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "eventing-superiority-profile")
            .Entries
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        var evidence = dimensions["scheduled-and-delayed-delivery-ownership"].Metadata["runtimeEvidence"];

        Assert.Equal("claimed", dimensions["scheduled-and-delayed-delivery-ownership"].Metadata["status"]);
        Assert.Contains("scheduledDeliveryProofSelection=latest-proven-dispatch-state", evidence, StringComparison.Ordinal);
        Assert.Contains("scheduledDeliveryStateCount=2", evidence, StringComparison.Ordinal);
        Assert.Contains("scheduledDeliveryProvenCount=2", evidence, StringComparison.Ordinal);
        Assert.Contains("scheduledDeliveryOwnershipSource=beta-schedule-runtime", evidence, StringComparison.Ordinal);
        Assert.Contains("durableScheduledDeliveryId=beta-durable-schedule", evidence, StringComparison.Ordinal);
        Assert.Contains("providerDelayQueueId=beta-provider-delay", evidence, StringComparison.Ordinal);
        Assert.Contains("brokerScheduledDeliveryId=beta-broker-schedule", evidence, StringComparison.Ordinal);
        Assert.Contains("scheduleCoordinationId=beta-schedule-coordination", evidence, StringComparison.Ordinal);
        Assert.Contains("scheduleRecoveryId=beta-schedule-recovery", evidence, StringComparison.Ordinal);
        Assert.Contains("outboxId=beta-outbox", evidence, StringComparison.Ordinal);
        Assert.Contains("lastOutcome=succeeded", evidence, StringComparison.Ordinal);
        Assert.Contains("lastObservedAtUtc=2026-05-13T09:30:00.0000000+00:00", evidence, StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("durableScheduledDeliveryId=alpha-durable-schedule", evidence, StringComparison.Ordinal);
    }

    [Fact]
    public void AddEventingProjectsContractCatalogProfileEvidenceWithoutWolverine()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventContractContributor, ContractTestEventContributor>();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventing(options =>
            {
                options.Contracts.Add(new EventContractDescriptor(
                    id: "audit.recorded.v1",
                    eventType: "audit.recorded",
                    displayName: "Audit Recorded",
                    description: "Audit event emitted when an auditable action is recorded.",
                    version: "1",
                    contentType: "application/vnd.cephalon.audit.recorded.v1+json",
                    serializerId: "system-text-json-sourcegen",
                    metadata: new Dictionary<string, string>
                    {
                        ["owner"] = "platform"
                    }));
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var contractCatalog = provider.GetRequiredService<IEventContractCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        Assert.Equal(2, contractCatalog.Contracts.Count);
        Assert.True(contractCatalog.TryGet("audit.recorded.v1", out var auditContract));
        Assert.Equal("audit.recorded", auditContract.EventType);
        Assert.Equal("application/vnd.cephalon.audit.recorded.v1+json", auditContract.ContentType);
        Assert.Equal("system-text-json-sourcegen", auditContract.SerializerId);
        Assert.True(contractCatalog.TryGetVersion("inventory.item.reserved", "1", out var inventoryContract));
        Assert.Equal("inventory.item.reserved.v1", inventoryContract.Id);
        Assert.Single(contractCatalog.GetByEventType("inventory.item.reserved"));

        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        Assert.DoesNotContain(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var contractSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-contracts");
        Assert.Contains(
            contractSurface.Entries,
            entry => entry.Id == "audit.recorded.v1" &&
                entry.Metadata["eventType"] == "audit.recorded" &&
                entry.Metadata["version"] == "1" &&
                entry.Metadata["serializerId"] == "system-text-json-sourcegen" &&
                entry.Metadata["envelopeSchema"] == "cephalon.event-envelope.v1" &&
                entry.Metadata["compatibilityPolicy"] == "backward-compatible" &&
                entry.Metadata["wolverineRequired"] == "false");

        var dimensions = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "eventing-superiority-profile")
            .Entries
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("partial", dimensions["serialization-and-contract-versioning-ownership"].Metadata["status"]);
        Assert.Contains("eventContractCatalog=present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("eventSerializerCatalog=not-present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("eventContractCount=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("serializerDescriptors=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("serializerRuntimeCount=0", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("serializerSelection=descriptor-backed", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("messageEnvelopeSchema=descriptor-backed", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("contractVersionNegotiation=descriptor-backed", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("compatibilityValidation=descriptor-backed", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wireSerializationRuntime=not-claimed", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);

        var capability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.contracts");
        Assert.Equal("event-contracts", capability.Metadata["surfaceId"]);
        Assert.Equal("options-and-contributors", capability.Metadata["contractSource"]);
        Assert.Equal("false", capability.Metadata["wolverineRequired"]);
    }

    [Fact]
    public void AddEventingProjectsSerializerCatalogProfileEvidenceWithoutWolverine()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventContractContributor, ContractTestEventContributor>();
        services.AddSingleton<IEventSerializerContributor, SerializerTestEventContributor>();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventing(options =>
            {
                options.Contracts.Add(new EventContractDescriptor(
                    id: "audit.recorded.v1",
                    eventType: "audit.recorded",
                    displayName: "Audit Recorded",
                    description: "Audit event emitted when an auditable action is recorded.",
                    version: "1",
                    contentType: "application/vnd.cephalon.audit.recorded.v1+json",
                    serializerId: "system-text-json-sourcegen"));
                options.Serializers.Add(new EventSerializerDescriptor(
                    id: "system-text-json-sourcegen",
                    displayName: "System.Text.Json Source Generated",
                    description: "Source-generated System.Text.Json serializer registered by the host.",
                    contentType: "application/json",
                    format: "json",
                    runtimeKind: "source-generated",
                    metadata: new Dictionary<string, string>
                    {
                        ["owner"] = "platform"
                    }));
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var contractCatalog = provider.GetRequiredService<IEventContractCatalog>();
        var serializerCatalog = provider.GetRequiredService<IEventSerializerCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        Assert.Equal(2, serializerCatalog.Serializers.Count);
        Assert.True(serializerCatalog.TryGet("system-text-json-sourcegen", out var jsonSerializer));
        Assert.Equal("json", jsonSerializer.Format);
        Assert.Equal("source-generated", jsonSerializer.RuntimeKind);
        Assert.True(jsonSerializer.CanRead);
        Assert.True(jsonSerializer.CanWrite);
        Assert.False(jsonSerializer.RequiresSchemaRegistry);
        Assert.Single(serializerCatalog.GetByContentType("application/json"));
        Assert.True(contractCatalog.TryGet("audit.recorded.v1", out var auditContract));
        Assert.True(serializerCatalog.TryGetForContract(auditContract, out var auditSerializer));
        Assert.Equal("system-text-json-sourcegen", auditSerializer.Id);

        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        Assert.DoesNotContain(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var serializerSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-serializers");
        Assert.Contains(
            serializerSurface.Entries,
            entry => entry.Id == "system-text-json-sourcegen" &&
                entry.Metadata["contentType"] == "application/json" &&
                entry.Metadata["format"] == "json" &&
                entry.Metadata["runtimeKind"] == "source-generated" &&
                entry.Metadata["canRead"] == "true" &&
                entry.Metadata["canWrite"] == "true" &&
                entry.Metadata["requiresSchemaRegistry"] == "false" &&
                entry.Metadata["matchingContractCount"] == "2" &&
                entry.Metadata["wolverineRequired"] == "false");

        var dimensions = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "eventing-superiority-profile")
            .Entries
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("partial", dimensions["serialization-and-contract-versioning-ownership"].Metadata["status"]);
        Assert.Contains("eventContractCatalog=present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("eventSerializerCatalog=present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("eventContractCount=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("serializerRuntimeCount=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("resolvedSerializerContracts=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("unresolvedSerializerContracts=0", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("serializerSelection=catalog-backed", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wireSerializationRuntime=serializer-catalog-declared", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("schemaRegistry=not-present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("upcasterPipeline=not-present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);

        var capability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.serializers");
        Assert.Equal("event-serializers", capability.Metadata["surfaceId"]);
        Assert.Equal("options-and-contributors", capability.Metadata["serializerSource"]);
        Assert.Equal("false", capability.Metadata["wolverineRequired"]);
    }

    [Fact]
    public void AddEventingProjectsSchemaRegistryCatalogProfileEvidenceWithoutWolverine()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventSerializerContributor, SchemaRegistrySerializerTestEventContributor>();
        services.AddSingleton<IEventSchemaRegistryContributor, SchemaRegistryTestEventContributor>();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventing(options =>
            {
                options.Contracts.Add(new EventContractDescriptor(
                    id: "audit.recorded.v1",
                    eventType: "audit.recorded",
                    displayName: "Audit Recorded",
                    description: "Audit event emitted when an auditable action is recorded.",
                    version: "1",
                    contentType: "application/vnd.cephalon.audit.recorded.v1+json",
                    serializerId: "system-text-json-sourcegen"));
                options.Serializers.Add(new EventSerializerDescriptor(
                    id: "system-text-json-sourcegen",
                    displayName: "System.Text.Json Source Generated",
                    description: "Source-generated System.Text.Json serializer registered by the host.",
                    contentType: "application/json",
                    format: "json",
                    runtimeKind: "source-generated",
                    requiresSchemaRegistry: true,
                    schemaRegistryId: "cephalon-json-schema-registry"));
                options.SchemaRegistries.Add(new EventSchemaRegistryDescriptor(
                    id: "cephalon-json-schema-registry",
                    displayName: "Cephalon JSON Schema Registry",
                    description: "Provider-neutral JSON schema registry descriptor registered by the host.",
                    provider: "cephalon",
                    endpointKind: "embedded",
                    runtimeKind: "code-first",
                    canReadSchemas: true,
                    canWriteSchemas: true,
                    validatesCompatibility: false,
                    supportedFormats: ["json"],
                    metadata: new Dictionary<string, string>
                    {
                        ["owner"] = "platform"
                    }));
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var serializerCatalog = provider.GetRequiredService<IEventSerializerCatalog>();
        var schemaRegistryCatalog = provider.GetRequiredService<IEventSchemaRegistryCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        Assert.Equal(2, schemaRegistryCatalog.Registries.Count);
        Assert.True(schemaRegistryCatalog.TryGet("cephalon-json-schema-registry", out var jsonRegistry));
        Assert.Equal("cephalon", jsonRegistry.Provider);
        Assert.Equal("embedded", jsonRegistry.EndpointKind);
        Assert.True(jsonRegistry.CanReadSchemas);
        Assert.True(jsonRegistry.CanWriteSchemas);
        Assert.False(jsonRegistry.ValidatesCompatibility);
        Assert.Single(schemaRegistryCatalog.GetByProvider("cephalon"));
        Assert.Single(schemaRegistryCatalog.GetByFormat("json"));
        Assert.True(serializerCatalog.TryGet("system-text-json-sourcegen", out var jsonSerializer));
        Assert.True(schemaRegistryCatalog.TryGetForSerializer(jsonSerializer, out var serializerRegistry));
        Assert.Equal("cephalon-json-schema-registry", serializerRegistry.Id);

        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        Assert.DoesNotContain(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var schemaRegistrySurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-schema-registries");
        Assert.Contains(
            schemaRegistrySurface.Entries,
            entry => entry.Id == "cephalon-json-schema-registry" &&
                entry.Metadata["provider"] == "cephalon" &&
                entry.Metadata["endpointKind"] == "embedded" &&
                entry.Metadata["runtimeKind"] == "code-first" &&
                entry.Metadata["canReadSchemas"] == "true" &&
                entry.Metadata["canWriteSchemas"] == "true" &&
                entry.Metadata["validatesCompatibility"] == "false" &&
                entry.Metadata["supportedFormats"] == "json" &&
                entry.Metadata["matchingSerializerCount"] == "1" &&
                entry.Metadata["wolverineRequired"] == "false");

        var dimensions = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "eventing-superiority-profile")
            .Entries
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("partial", dimensions["serialization-and-contract-versioning-ownership"].Metadata["status"]);
        Assert.Contains("eventContractCatalog=present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("eventSerializerCatalog=present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("eventSchemaRegistryCatalog=present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("serializerRuntimeCount=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("schemaRegistryRuntimeCount=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("schemaRegistryReferences=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("resolvedSchemaRegistrySerializers=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("unresolvedSchemaRegistrySerializers=0", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("schemaRegistry=catalog-backed", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wireSerializationRuntime=serializer-catalog-declared", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("upcasterPipeline=not-present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);

        var capability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.schema-registries");
        Assert.Equal("event-schema-registries", capability.Metadata["surfaceId"]);
        Assert.Equal("options-and-contributors", capability.Metadata["schemaRegistrySource"]);
        Assert.Equal("false", capability.Metadata["wolverineRequired"]);
    }

    [Fact]
    public void AddEventingProjectsUpcasterCatalogProfileEvidenceWithoutWolverine()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventContractContributor, ContractTestEventContributor>();
        services.AddSingleton<IEventSerializerContributor, SchemaRegistrySerializerTestEventContributor>();
        services.AddSingleton<IEventSchemaRegistryContributor, SchemaRegistryTestEventContributor>();
        services.AddSingleton<IEventUpcasterContributor, UpcasterTestEventContributor>();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventing(options =>
            {
                options.Contracts.Add(new EventContractDescriptor(
                    id: "audit.recorded.v1",
                    eventType: "audit.recorded",
                    displayName: "Audit Recorded V1",
                    description: "Initial audit event contract.",
                    version: "1",
                    contentType: "application/vnd.cephalon.audit.recorded.v1+json",
                    serializerId: "system-text-json-sourcegen"));
                options.Contracts.Add(new EventContractDescriptor(
                    id: "audit.recorded.v2",
                    eventType: "audit.recorded",
                    displayName: "Audit Recorded V2",
                    description: "Audit event contract with normalized actor metadata.",
                    version: "2",
                    contentType: "application/vnd.cephalon.audit.recorded.v2+json",
                    serializerId: "system-text-json-sourcegen"));
                options.Contracts.Add(new EventContractDescriptor(
                    id: "inventory.item.reserved.v2",
                    eventType: "inventory.item.reserved",
                    displayName: "Inventory Item Reserved V2",
                    description: "Inventory event contract with reservation source metadata.",
                    version: "2",
                    contentType: "application/vnd.cephalon.inventory.item-reserved.v2+json",
                    serializerId: "system-text-json-sourcegen"));
                options.Serializers.Add(new EventSerializerDescriptor(
                    id: "system-text-json-sourcegen",
                    displayName: "System.Text.Json Source Generated",
                    description: "Source-generated System.Text.Json serializer registered by the host.",
                    contentType: "application/json",
                    format: "json",
                    runtimeKind: "source-generated",
                    requiresSchemaRegistry: true,
                    schemaRegistryId: "cephalon-json-schema-registry"));
                options.SchemaRegistries.Add(new EventSchemaRegistryDescriptor(
                    id: "cephalon-json-schema-registry",
                    displayName: "Cephalon JSON Schema Registry",
                    description: "Provider-neutral JSON schema registry descriptor registered by the host.",
                    provider: "cephalon",
                    endpointKind: "embedded",
                    runtimeKind: "code-first",
                    canReadSchemas: true,
                    canWriteSchemas: true,
                    validatesCompatibility: false,
                    supportedFormats: ["json"]));
                options.Upcasters.Add(new EventUpcasterDescriptor(
                    id: "audit-recorded-v1-to-v2",
                    eventType: "audit.recorded",
                    displayName: "Audit Recorded V1 To V2",
                    description: "Provider-neutral audit event version transition registered by the host.",
                    fromVersion: "1",
                    toVersion: "2",
                    runtimeKind: "code-first",
                    metadata: new Dictionary<string, string>
                    {
                        ["owner"] = "platform"
                    }));
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var upcasterCatalog = provider.GetRequiredService<IEventUpcasterCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        Assert.Equal(2, upcasterCatalog.Upcasters.Count);
        Assert.True(upcasterCatalog.TryGet("audit-recorded-v1-to-v2", out var auditUpcaster));
        Assert.Equal("audit.recorded", auditUpcaster.EventType);
        Assert.Equal("1", auditUpcaster.FromVersion);
        Assert.Equal("2", auditUpcaster.ToVersion);
        Assert.Equal("code-first", auditUpcaster.RuntimeKind);
        Assert.True(auditUpcaster.CanUpcast);
        Assert.Single(upcasterCatalog.GetByEventType("audit.recorded"));
        Assert.Single(upcasterCatalog.GetBySourceVersion("audit.recorded", "1"));
        Assert.True(upcasterCatalog.TryGetTransition("audit.recorded", "1", "2", out var auditTransition));
        Assert.Equal("audit-recorded-v1-to-v2", auditTransition.Id);

        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        Assert.DoesNotContain(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var upcasterSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-upcasters");
        Assert.Contains(
            upcasterSurface.Entries,
            entry => entry.Id == "audit-recorded-v1-to-v2" &&
                entry.Metadata["eventType"] == "audit.recorded" &&
                entry.Metadata["fromVersion"] == "1" &&
                entry.Metadata["toVersion"] == "2" &&
                entry.Metadata["runtimeKind"] == "code-first" &&
                entry.Metadata["canUpcast"] == "true" &&
                entry.Metadata["sourceContractResolved"] == "true" &&
                entry.Metadata["targetContractResolved"] == "true" &&
                entry.Metadata["wolverineRequired"] == "false");

        var dimensions = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "eventing-superiority-profile")
            .Entries
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("partial", dimensions["serialization-and-contract-versioning-ownership"].Metadata["status"]);
        Assert.Contains("eventContractCatalog=present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("eventSerializerCatalog=present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("eventSchemaRegistryCatalog=present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("eventUpcasterCatalog=present", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("eventContractCount=4", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("serializerRuntimeCount=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("schemaRegistryRuntimeCount=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("upcasterRuntimeCount=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("upcasterTransitions=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("resolvedUpcasterSourceContracts=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("resolvedUpcasterTargetContracts=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("resolvedUpcasterTransitions=2", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("schemaRegistry=catalog-backed", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("upcasterPipeline=catalog-declared", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["serialization-and-contract-versioning-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);

        var capability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.upcasters");
        Assert.Equal("event-upcasters", capability.Metadata["surfaceId"]);
        Assert.Equal("options-and-contributors", capability.Metadata["upcasterSource"]);
        Assert.Equal("false", capability.Metadata["wolverineRequired"]);
    }

    [Fact]
    public void AddEventingProjectsContextPolicyCatalogProfileEvidenceWithoutWolverine()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventContextPolicyContributor, ContextPolicyTestEventContributor>();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventing(options =>
            {
                options.ContextPolicies.Add(new EventContextPolicyDescriptor(
                    id: "platform-context",
                    displayName: "Platform Context",
                    description: "Platform-owned event context policy registered by the host.",
                    runtimeKind: "code-first",
                    declaresTenantContext: true,
                    declaresCorrelationId: true,
                    declaresCausationId: true,
                    declaresBaggage: true,
                    validatesMessageHeaders: true,
                    headerNames:
                    [
                        "cephalon-tenant-id",
                        "cephalon-correlation-id",
                        "cephalon-causation-id"
                    ],
                    metadata: new Dictionary<string, string>
                    {
                        ["owner"] = "platform"
                    }));
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var contextPolicyCatalog = provider.GetRequiredService<IEventContextPolicyCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        Assert.Equal(2, contextPolicyCatalog.Policies.Count);
        Assert.True(contextPolicyCatalog.TryGet("platform-context", out var platformPolicy));
        Assert.Equal("Platform Context", platformPolicy.DisplayName);
        Assert.True(platformPolicy.DeclaresTenantContext);
        Assert.True(platformPolicy.DeclaresCorrelationId);
        Assert.True(platformPolicy.DeclaresCausationId);
        Assert.True(platformPolicy.DeclaresBaggage);
        Assert.True(platformPolicy.ValidatesMessageHeaders);
        Assert.Contains("cephalon-tenant-id", platformPolicy.HeaderNames);
        Assert.Single(contextPolicyCatalog.GetByHeaderName("cephalon-tenant-id"));
        Assert.Single(contextPolicyCatalog.GetByHeaderName("cephalon-message-id"));

        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        Assert.DoesNotContain(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var contextPolicySurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-context-policies");
        Assert.Contains(
            contextPolicySurface.Entries,
            entry => entry.Id == "platform-context" &&
                entry.Metadata["runtimeKind"] == "code-first" &&
                entry.Metadata["declaresTenantContext"] == "true" &&
                entry.Metadata["declaresCorrelationId"] == "true" &&
                entry.Metadata["declaresCausationId"] == "true" &&
                entry.Metadata["declaresBaggage"] == "true" &&
                entry.Metadata["validatesMessageHeaders"] == "true" &&
                entry.Metadata["headerNames"] == "cephalon-causation-id,cephalon-correlation-id,cephalon-tenant-id" &&
                entry.Metadata["wolverineRequired"] == "false");

        var dimensions = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "eventing-superiority-profile")
            .Entries
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("partial", dimensions["tenant-and-correlation-context-ownership"].Metadata["status"]);
        Assert.Contains("eventContextPolicyCatalog=present", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("contextPolicyCount=2", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("tenantPolicyCount=2", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("correlationPolicyCount=2", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("causationPolicyCount=1", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("baggagePolicyCount=1", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("headerValidationPolicyCount=1", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("declaredHeaderCount=4", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("tenantContextPropagation=policy-declared", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("correlationContextPropagation=policy-declared", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("causationIdPropagation=policy-declared", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("baggagePropagation=policy-declared", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("messageHeaderPolicy=policy-declared", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("executablePropagation=not-claimed", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);

        var capability = Assert.Single(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.context-policies");
        Assert.Equal("event-context-policies", capability.Metadata["surfaceId"]);
        Assert.Equal("options-and-contributors", capability.Metadata["contextPolicySource"]);
        Assert.Equal("false", capability.Metadata["wolverineRequired"]);
    }

    [Fact]
    public async Task AddEventingEnforcesContextPolicyHeadersWithoutWolverine()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ContextPolicyExecutionProbe>();
        services.AddSingleton<IEventSubscriptionExecutor, ContextPolicyAuditExecutor>();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["CQRS"],
                technologies: ["EventDrivenIntegration"],
                transports: ["RestApi"]));
            engine.AddEventing(options =>
            {
                options.EnableInProcessSubscriptionExecution = true;
                options.Channels.Add(new EventChannelDescriptor(
                    id: "audit",
                    displayName: "Audit",
                    description: "Audit integration events.",
                    tags: ["audit"]));
                options.Subscriptions.Add(new EventSubscriptionDescriptor(
                    id: "context-audit",
                    displayName: "Context Audit",
                    description: "Consumes context policy enforcement test publications.",
                    channelId: "audit",
                    handlerId: "context-audit-handler",
                    deliveryMode: "direct"));
                options.ContextPolicies.Add(new EventContextPolicyDescriptor(
                    id: "platform-context-enforcement",
                    displayName: "Platform Context Enforcement",
                    description: "Enforces required Cephalon context headers before direct execution.",
                    declaresTenantContext: true,
                    declaresCorrelationId: true,
                    declaresCausationId: true,
                    declaresBaggage: true,
                    validatesMessageHeaders: true,
                    headerNames:
                    [
                        EventContextHeaderNames.CausationId,
                        EventContextHeaderNames.Baggage
                    ]));
            });
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var probe = provider.GetRequiredService<ContextPolicyExecutionProbe>();
        var publicationRuntimeCatalog = provider.GetRequiredService<IEventPublicationRuntimeCatalog>();
        var subscriptionRuntimeCatalog = provider.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        var technologyCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        await publisher.PublishAsync(new EventPublication(
            id: "context-msg-001",
            channelId: "audit",
            eventType: "audit.context",
            payload: "{}",
            occurredAtUtc: new DateTimeOffset(2026, 05, 12, 9, 0, 0, TimeSpan.Zero),
            contentType: "application/json",
            correlationId: "corr-001",
            tenantId: "tenant-001",
            headers: new Dictionary<string, string>
            {
                [EventContextHeaderNames.CausationId] = "cause-001",
                [EventContextHeaderNames.Baggage] = "tier=gold"
            }));

        Assert.NotNull(probe.LastContext);
        Assert.Equal("tenant-001", probe.LastContext.Publication.TenantId);
        Assert.Equal("corr-001", probe.LastContext.Publication.CorrelationId);
        Assert.Equal("validated", probe.LastContext.Metadata["contextHeaderValidation"]);
        Assert.Equal("publisher-enforced", probe.LastContext.Metadata["executableContextPolicy"]);
        Assert.Equal("publication-field-forwarded", probe.LastContext.Metadata["tenantContextPropagation"]);
        Assert.Equal("publication-field-forwarded", probe.LastContext.Metadata["correlationContextPropagation"]);
        Assert.Equal("header-forwarded", probe.LastContext.Metadata["causationIdPropagation"]);
        Assert.Equal("header-forwarded", probe.LastContext.Metadata["baggagePropagation"]);
        Assert.Equal("extracted", probe.LastContext.Metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerContextExtraction]);
        Assert.Equal(
            "event-publication-context-headers",
            probe.LastContext.Metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerContextExtractionSource]);
        Assert.Equal("5", probe.LastContext.Metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerContextHeaderCount]);
        Assert.Equal(
            $"{EventContextHeaderNames.Baggage},{EventContextHeaderNames.CausationId},{EventContextHeaderNames.CorrelationId},{EventContextHeaderNames.MessageId},{EventContextHeaderNames.TenantId}",
            probe.LastContext.Metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerContextHeaderNames]);
        Assert.Equal("validated", probe.LastContext.Metadata["messageHeaderPolicy"]);
        Assert.Equal("false", probe.LastContext.Metadata["wolverineRequired"]);

        var succeededPublication = publicationRuntimeCatalog.GetByPublicationId("context-msg-001");
        Assert.NotNull(succeededPublication);
        Assert.Equal(EventPublicationRuntimeOutcomes.Succeeded, succeededPublication.LastOutcome);
        Assert.Equal("validated", succeededPublication.Metadata["contextHeaderValidation"]);
        Assert.Equal("0", succeededPublication.Metadata["contextMissingHeaderCount"]);
        var succeededSubscription = subscriptionRuntimeCatalog.GetById("context-audit");
        Assert.NotNull(succeededSubscription);
        Assert.Equal(EventSubscriptionExecutionOutcomes.Succeeded, succeededSubscription.LastOutcome);
        Assert.Equal("publisher-enforced", succeededSubscription.Metadata["executableContextPolicy"]);
        Assert.Equal("extracted", succeededSubscription.Metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerContextExtraction]);
        Assert.Equal("5", succeededSubscription.Metadata[EventSubscriptionRuntimeMetadataKeys.ConsumerContextHeaderCount]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await publisher.PublishAsync(new EventPublication(
                id: "context-msg-002",
                channelId: "audit",
                eventType: "audit.context",
                payload: "{}",
                occurredAtUtc: new DateTimeOffset(2026, 05, 12, 9, 1, 0, TimeSpan.Zero),
                contentType: "application/json",
                correlationId: "corr-002",
                tenantId: "tenant-002",
                headers: new Dictionary<string, string>
                {
                    [EventContextHeaderNames.CausationId] = "cause-002"
                })));
        Assert.Contains(EventContextHeaderNames.Baggage, exception.Message, StringComparison.Ordinal);

        var failedPublication = publicationRuntimeCatalog.GetByPublicationId("context-msg-002");
        Assert.NotNull(failedPublication);
        Assert.Equal(EventPublicationRuntimeOutcomes.Failed, failedPublication.LastOutcome);
        Assert.Equal("failed", failedPublication.Metadata["contextHeaderValidation"]);
        Assert.Equal("1", failedPublication.Metadata["contextMissingHeaderCount"]);
        Assert.Equal(EventContextHeaderNames.Baggage, failedPublication.Metadata["contextMissingHeaders"]);
        Assert.Equal("validation-failed", failedPublication.Metadata["messageHeaderPolicy"]);

        var eventingSurfaces = technologyCatalog.GetByTechnology("event-driven-integration");
        Assert.DoesNotContain(eventingSurfaces, surface => surface.SurfaceId == "wolverine-adapter");
        var dimensions = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "eventing-superiority-profile")
            .Entries
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("partial", dimensions["tenant-and-correlation-context-ownership"].Metadata["status"]);
        Assert.Contains("tenantContextPropagation=in-process-direct", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("correlationContextPropagation=in-process-direct", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("causationIdPropagation=in-process-direct", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("baggagePropagation=in-process-direct", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("messageHeaderPolicy=publisher-enforced", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("consumerContextExtraction=extracted", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("executablePropagation=in-process-direct", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("executableValidation=publisher-enforced", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
        Assert.Contains("wolverineRequired=false", dimensions["tenant-and-correlation-context-ownership"].Metadata["runtimeEvidence"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddRetrievalRunsOptInBackgroundReindexScheduler()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["KnowledgeRetrieval"]));
            engine.AddRetrieval(options =>
            {
                options.EnableBackgroundReindexing = true;
                options.BackgroundReindexInitialDelaySeconds = 0;
                options.BackgroundReindexIntervalSeconds = 0;
                options.BackgroundReindexCollectionIds.Add("runbooks");
            });
            engine.AddModule(new TechnologyPackContributionModule());
        });

        using var provider = services.BuildServiceProvider();
        var scheduler = Assert.Single(provider.GetServices<IHostedService>());
        var indexCatalog = provider.GetRequiredService<IKnowledgeIndexCatalog>();

        await scheduler.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(() =>
                indexCatalog.GetByCollectionId("runbooks")?.LastOutcome == KnowledgeIndexingOutcomes.Succeeded);
        }
        finally
        {
            await scheduler.StopAsync(CancellationToken.None);
        }

        var state = Assert.Single(indexCatalog.States);
        var runtime = provider.GetRequiredService<IRuntime>();
        var technologySurfaces = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var retrievalSurface = Assert.Single(technologySurfaces.GetByTechnology("knowledge-retrieval"));
        var runbooksEntry = Assert.Single(retrievalSurface.Entries, entry => entry.Id == "runbooks");

        Assert.Equal("runbooks", state.CollectionId);
        Assert.StartsWith("retrieval-background-reindex-runbooks-", state.LastRunId, StringComparison.Ordinal);
        Assert.Equal(KnowledgeIndexingOutcomes.Succeeded, state.LastOutcome);
        Assert.Equal(2, state.DocumentCount);
        Assert.Equal("cephalon-retrieval-background-scheduler", state.LastActorId);
        Assert.Equal("retrieval-background-scheduler", state.Metadata["trigger"]);
        Assert.Equal("cephalon-retrieval-background-reindex", state.Metadata["scheduler"]);
        Assert.Equal("configured", state.Metadata["collectionScope"]);
        Assert.Equal("0", state.Metadata["intervalSeconds"]);
        Assert.Contains(runtime.Manifest.Capabilities, capability =>
            capability.Key == "retrieval.background-reindexing" &&
            capability.Metadata["executionOwnership"] == "cephalon-managed" &&
            capability.Metadata["collectionScope"] == "configured");
        Assert.Equal("true", runbooksEntry.Metadata["backgroundReindexingEnabled"]);
        Assert.Equal("true", runbooksEntry.Metadata["backgroundReindexingScheduled"]);
        Assert.Equal("cephalon-managed", runbooksEntry.Metadata["backgroundReindexingOwnership"]);
        Assert.Equal("configured", runbooksEntry.Metadata["backgroundReindexingCollectionScope"]);
        Assert.Equal("1", runbooksEntry.Metadata["backgroundReindexingConfiguredCollectionCount"]);
        Assert.Equal("0", runbooksEntry.Metadata["backgroundReindexingIntervalSeconds"]);
        Assert.Equal("retrieval-background-scheduler", runbooksEntry.Metadata["reported.trigger"]);
    }

    [Fact]
    public void AddTechnologyPacksRejectHostedExecutionsThatReferenceUnknownEventSubscriptions()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["EventDrivenIntegration"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new InvalidEventSubscriptionHostedExecutionModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "audit",
                    displayName: "Audit",
                    description: "Audit event stream."));
            });
        });

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<ITechnologyRuntimeCatalog>().Surfaces);

        Assert.Contains("broken-subscription-pump", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("missing-subscription", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddTechnologyPacksProjectAgenticOrchestrationLinksThroughExistingRuntimeContracts()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["AgenticWorkloads"]));
            engine.AddAgentics();
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new WorkflowCatalogTestModule("agentic-orchestration"));
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();

        await runtime.StartAsync(provider);

        var technologySurfaces = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var snapshotProvider = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>();
        var agenticsSurface = Assert.Single(technologySurfaces.GetByTechnology("agentic-workloads"));
        var orchestrationEntry = Assert.Single(agenticsSurface.Entries, entry => entry.Id == "approval-orchestrator");
        Assert.Equal("workflow.approval.record,workflow.approval.request", orchestrationEntry.Metadata["capabilityKeys"]);
        Assert.Equal("Approval decision,Approval request", orchestrationEntry.Metadata["capabilityDisplayNames"]);
        Assert.Equal("approval-flow", orchestrationEntry.Metadata["executionGraphId"]);
        Assert.Equal("Approval Flow", orchestrationEntry.Metadata["executionGraphDisplayName"]);
        Assert.Equal("activate", orchestrationEntry.Metadata["executionGraphPhase"]);
        Assert.Equal("true", orchestrationEntry.Metadata["executionGraphIsActive"]);
        Assert.Equal("approval-pump", orchestrationEntry.Metadata["hostedExecutionId"]);
        Assert.Equal("Approval Pump", orchestrationEntry.Metadata["hostedExecutionDisplayName"]);
        Assert.Equal("background-service", orchestrationEntry.Metadata["hostedExecutionKind"]);
        Assert.Equal("activate", orchestrationEntry.Metadata["hostedExecutionPhase"]);
        Assert.Equal("true", orchestrationEntry.Metadata["hostedExecutionIsActive"]);
        Assert.Equal("true", orchestrationEntry.Metadata["orchestrationLinked"]);

        var snapshot = snapshotProvider.CreateSnapshot();
        var snapshotAgenticsSurface = Assert.Single(snapshot.TechnologySurfaces, surface => surface.TechnologyId == "agentic-workloads");
        Assert.Contains(snapshotAgenticsSurface.Entries, entry => entry.Id == "approval-orchestrator" &&
            entry.Metadata["executionGraphId"] == "approval-flow" &&
            entry.Metadata["hostedExecutionId"] == "approval-pump");
    }

    [Fact]
    public async Task AddTechnologyPacksExecuteAgentToolsAndProjectRunState()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["AgenticWorkloads"]));
            engine.AddAgentics();
            engine.AddModule(new TechnologyPackContributionModule());
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IAgentToolDispatcher>();
        var auditProbe = provider.GetRequiredService<AgentToolExecutionAuditProbe>();

        var result = await dispatcher.ExecuteAsync(new AgentToolExecutionRequest(
            toolId: "analyst",
            runId: "analysis-run-001",
            arguments: new Dictionary<string, string>
            {
                ["subject"] = "agentics runtime"
            },
            actorId: "operator",
            correlationId: "corr-agentics-001",
            metadata: new Dictionary<string, string>
            {
                ["requestSource"] = "composition-test"
            }));

        var runCatalog = provider.GetRequiredService<IAgentToolRunCatalog>();
        var technologySurfaces = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var runState = Assert.Single(runCatalog.GetByToolId("analyst"));
        var agenticsSurface = Assert.Single(technologySurfaces.GetByTechnology("agentic-workloads"));
        var analystEntry = Assert.Single(agenticsSurface.Entries, entry => entry.Id == "analyst");

        Assert.Equal(AgentToolExecutionOutcomes.Succeeded, result.Outcome);
        Assert.Equal("Analyzed agentics runtime.", result.OutputSummary);
        Assert.Equal("analysis-run-001", runState.RunId);
        Assert.Equal(AgentToolExecutionOutcomes.Succeeded, runState.LastOutcome);
        Assert.Equal("operator", runState.LastActorId);
        Assert.Equal("corr-agentics-001", runState.LastCorrelationId);
        Assert.Equal(1, runState.StartedCount);
        Assert.Equal(1, runState.SucceededCount);
        Assert.Equal(2, runState.TotalReports);
        Assert.True(runState.IsTerminal);
        Assert.False(runState.RequiresApproval);
        Assert.Contains(auditProbe.Reports, report =>
            report.RunId == "analysis-run-001" &&
            report.Outcome == AgentToolExecutionOutcomes.Started);
        Assert.Contains(auditProbe.Reports, report =>
            report.RunId == "analysis-run-001" &&
            report.Outcome == AgentToolExecutionOutcomes.Succeeded);
        Assert.Equal("true", analystEntry.Metadata["executionEnabled"]);
        Assert.Equal("cephalon-managed", analystEntry.Metadata["executionOwnership"]);
        Assert.Equal("true", analystEntry.Metadata["executorConfigured"]);
        Assert.Equal("reported", analystEntry.Metadata["runtimeState"]);
        Assert.Equal("analysis-run-001", analystEntry.Metadata["lastRunId"]);
        Assert.Equal("succeeded", analystEntry.Metadata["lastOutcome"]);
        Assert.Equal("1", analystEntry.Metadata["startedCount"]);
        Assert.Equal("1", analystEntry.Metadata["succeededCount"]);
        Assert.Equal("2", analystEntry.Metadata["totalReports"]);
        Assert.Equal("false", analystEntry.Metadata["requiresApproval"]);
        Assert.Equal("false", analystEntry.Metadata["terminalFailure"]);
        Assert.Equal("true", analystEntry.Metadata["isTerminal"]);
        Assert.Equal("operator", analystEntry.Metadata["lastActorId"]);
        Assert.Equal("corr-agentics-001", analystEntry.Metadata["lastCorrelationId"]);
        Assert.Equal("Analyzed agentics runtime.", analystEntry.Metadata["lastOutputSummary"]);
        Assert.Equal(nameof(ContributedAgentToolExecutor), analystEntry.Metadata["reported.executor"]);
    }

    [Fact]
    public async Task AddTechnologyPacksRetryAgentToolExecutorFailuresWhenConfigured()
    {
        var services = new ServiceCollection();
        services.AddSingleton<RetryingAgentToolExecutor>();
        services.AddSingleton<IAgentToolExecutor>(static serviceProvider =>
            serviceProvider.GetRequiredService<RetryingAgentToolExecutor>());
        services.AddSingleton<AgentToolExecutionAuditProbe>();
        services.AddSingleton<IAgentToolExecutionObserver>(static serviceProvider =>
            serviceProvider.GetRequiredService<AgentToolExecutionAuditProbe>());
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["AgenticWorkloads"]));
            engine.AddAgentics(options =>
            {
                options.ExecutionMaxAttempts = 3;
                options.Tools.Add(new AgentToolDescriptor(
                    id: "retrying-analyst",
                    displayName: "Retrying Analyst",
                    description: "Exercises bounded process-local retry for the managed agentics lane."));
            });
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IAgentToolDispatcher>();
        var auditProbe = provider.GetRequiredService<AgentToolExecutionAuditProbe>();

        var result = await dispatcher.ExecuteAsync(new AgentToolExecutionRequest(
            toolId: "retrying-analyst",
            runId: "agentics-retry-run-001",
            actorId: "operator",
            correlationId: "corr-agentics-retry-001"));

        var runCatalog = provider.GetRequiredService<IAgentToolRunCatalog>();
        var technologySurfaces = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var runState = Assert.Single(runCatalog.GetByToolId("retrying-analyst"));
        var agenticsSurface = Assert.Single(technologySurfaces.GetByTechnology("agentic-workloads"));
        var retryingEntry = Assert.Single(agenticsSurface.Entries, entry => entry.Id == "retrying-analyst");
        var retryReport = Assert.Single(auditProbe.Reports, report =>
            report.RunId == "agentics-retry-run-001" &&
            report.Outcome == AgentToolExecutionOutcomes.RetryScheduled);

        Assert.Equal(AgentToolExecutionOutcomes.Succeeded, result.Outcome);
        Assert.Equal("Retried agentics attempt 2.", result.OutputSummary);
        Assert.Equal(2, provider.GetRequiredService<RetryingAgentToolExecutor>().CallCount);
        Assert.Equal(AgentToolExecutionOutcomes.Succeeded, runState.LastOutcome);
        Assert.Equal(2, runState.StartedCount);
        Assert.Equal(1, runState.RetryScheduledCount);
        Assert.Equal(1, runState.SucceededCount);
        Assert.Equal(4, runState.TotalReports);
        Assert.Equal(2, runState.LastAttempt);
        Assert.False(runState.RetryPending);
        Assert.True(runState.IsTerminal);
        Assert.Equal("bounded-in-process", retryReport.Metadata["retryPolicy"]);
        Assert.Equal("3", retryReport.Metadata["retryMaxAttempts"]);
        Assert.Equal("none", retryReport.Metadata["retryDurability"]);
        Assert.Equal("process-local", retryReport.Metadata["retryScope"]);
        Assert.Equal("retry-scheduled", retryReport.Metadata["retryOutcome"]);
        Assert.Equal("2", retryReport.Metadata["nextAttempt"]);
        Assert.Equal("bounded-in-process", retryingEntry.Metadata["retryPolicy"]);
        Assert.Equal("3", retryingEntry.Metadata["retryMaxAttempts"]);
        Assert.Equal("1", retryingEntry.Metadata["retryScheduledCount"]);
        Assert.Equal("false", retryingEntry.Metadata["retryPending"]);
        Assert.Equal("true", retryingEntry.Metadata["isTerminal"]);
    }

    [Fact]
    public async Task AddTechnologyPacksSkipDuplicateCompletedAgentToolRunsWhenIdempotencyIsEnabled()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CountingAgentToolExecutor>();
        services.AddSingleton<IAgentToolExecutor>(static serviceProvider =>
            serviceProvider.GetRequiredService<CountingAgentToolExecutor>());
        services.AddSingleton<AgentToolExecutionAuditProbe>();
        services.AddSingleton<IAgentToolExecutionObserver>(static serviceProvider =>
            serviceProvider.GetRequiredService<AgentToolExecutionAuditProbe>());
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["AgenticWorkloads"]));
            engine.AddAgentics(options =>
            {
                options.EnableExecutionIdempotency = true;
                options.ExecutionIdempotencyRetentionMinutes = 30;
                options.Tools.Add(new AgentToolDescriptor(
                    id: "idempotent-analyst",
                    displayName: "Idempotent Analyst",
                    description: "Exercises process-local duplicate completed run suppression for the managed agentics lane."));
            });
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IAgentToolDispatcher>();
        var executor = provider.GetRequiredService<CountingAgentToolExecutor>();
        var auditProbe = provider.GetRequiredService<AgentToolExecutionAuditProbe>();

        var firstResult = await dispatcher.ExecuteAsync(new AgentToolExecutionRequest(
            toolId: "idempotent-analyst",
            runId: "agentics-idempotent-run-001",
            actorId: "operator",
            correlationId: "corr-agentics-idempotent-001",
            metadata: new Dictionary<string, string>
            {
                ["requestSource"] = "composition-test"
            }));
        var duplicateResult = await dispatcher.ExecuteAsync(new AgentToolExecutionRequest(
            toolId: "idempotent-analyst",
            runId: "agentics-idempotent-run-001",
            actorId: "operator",
            correlationId: "corr-agentics-idempotent-001",
            metadata: new Dictionary<string, string>
            {
                ["requestSource"] = "composition-test"
            }));

        var runCatalog = provider.GetRequiredService<IAgentToolRunCatalog>();
        var technologySurfaces = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var runState = Assert.Single(runCatalog.GetByToolId("idempotent-analyst"));
        var agenticsSurface = Assert.Single(technologySurfaces.GetByTechnology("agentic-workloads"));
        var idempotentEntry = Assert.Single(agenticsSurface.Entries, entry => entry.Id == "idempotent-analyst");
        var duplicateReport = Assert.Single(auditProbe.Reports, report =>
            report.RunId == "agentics-idempotent-run-001" &&
            report.Outcome == AgentToolExecutionOutcomes.Skipped);

        Assert.Equal(AgentToolExecutionOutcomes.Succeeded, firstResult.Outcome);
        Assert.Equal(AgentToolExecutionOutcomes.Skipped, duplicateResult.Outcome);
        Assert.Equal("Agent-tool run already completed in this process.", duplicateResult.OutputSummary);
        Assert.Equal(1, executor.CallCount);
        Assert.Equal(AgentToolExecutionOutcomes.Skipped, runState.LastOutcome);
        Assert.Equal(1, runState.StartedCount);
        Assert.Equal(1, runState.SucceededCount);
        Assert.Equal(1, runState.SkippedCount);
        Assert.Equal(3, runState.TotalReports);
        Assert.True(runState.DuplicateCompleted);
        Assert.True(runState.IsTerminal);
        Assert.False(runState.RetryPending);
        Assert.Equal("completed-run", duplicateResult.Metadata["idempotencyPolicy"]);
        Assert.Equal("tool-run", duplicateResult.Metadata["idempotencyKey"]);
        Assert.Equal("30", duplicateResult.Metadata["idempotencyRetentionMinutes"]);
        Assert.Equal("none", duplicateResult.Metadata["idempotencyDurability"]);
        Assert.Equal("process-local", duplicateResult.Metadata["idempotencyScope"]);
        Assert.Equal("duplicate-skipped", duplicateResult.Metadata["idempotencyOutcome"]);
        Assert.Equal("agentics-idempotent-run-001", duplicateResult.Metadata["completedRunId"]);
        Assert.Equal("succeeded", duplicateResult.Metadata["completedOutcome"]);
        Assert.Equal("composition-test", duplicateResult.Metadata["requestSource"]);
        Assert.Equal("duplicate-skipped", duplicateReport.Metadata["idempotencyOutcome"]);
        Assert.Equal("completed-run", idempotentEntry.Metadata["idempotencyPolicy"]);
        Assert.Equal("tool-run", idempotentEntry.Metadata["idempotencyKey"]);
        Assert.Equal("30", idempotentEntry.Metadata["idempotencyRetentionMinutes"]);
        Assert.Equal("none", idempotentEntry.Metadata["idempotencyDurability"]);
        Assert.Equal("process-local", idempotentEntry.Metadata["idempotencyScope"]);
        Assert.Equal("1", idempotentEntry.Metadata["succeededCount"]);
        Assert.Equal("1", idempotentEntry.Metadata["skippedCount"]);
        Assert.Equal("true", idempotentEntry.Metadata["duplicateCompleted"]);
        Assert.Equal("duplicate-skipped", idempotentEntry.Metadata["reported.idempotencyOutcome"]);
    }

    [Fact]
    public async Task AddTechnologyPacksReportApprovalRequiredAgentToolRunsWithoutCallingExecutor()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["AgenticWorkloads"]));
            engine.AddAgentics();
            engine.AddModule(new TechnologyPackContributionModule());
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IAgentToolDispatcher>();

        var result = await dispatcher.ExecuteAsync(new AgentToolExecutionRequest(
            toolId: "analyst",
            runId: "analysis-run-approval-001",
            metadata: new Dictionary<string, string>
            {
                ["approval"] = "required"
            }));

        var runCatalog = provider.GetRequiredService<IAgentToolRunCatalog>();
        var technologySurfaces = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var runState = Assert.Single(runCatalog.GetByToolId("analyst"));
        var agenticsSurface = Assert.Single(technologySurfaces.GetByTechnology("agentic-workloads"));
        var analystEntry = Assert.Single(agenticsSurface.Entries, entry => entry.Id == "analyst");

        Assert.Equal(AgentToolExecutionOutcomes.ApprovalRequired, result.Outcome);
        Assert.Equal("The analyst tool requires approval for this request.", result.OutputSummary);
        Assert.Equal(AgentToolExecutionOutcomes.ApprovalRequired, runState.LastOutcome);
        Assert.Equal(1, runState.StartedCount);
        Assert.Equal(1, runState.ApprovalRequiredCount);
        Assert.Equal(0, runState.SucceededCount);
        Assert.True(runState.RequiresApproval);
        Assert.False(runState.IsTerminal);
        Assert.False(runState.TerminalFailure);
        Assert.Equal("true", analystEntry.Metadata["requiresApproval"]);
        Assert.Equal("false", analystEntry.Metadata["terminalFailure"]);
        Assert.Equal("false", analystEntry.Metadata["isTerminal"]);
    }

    [Fact]
    public async Task AddTechnologyPacksProjectTerminalFailedAgentToolRuns()
    {
        var services = new ServiceCollection();
        services.AddSingleton<FailingAgentToolExecutor>();
        services.AddSingleton<IAgentToolExecutor>(static serviceProvider =>
            serviceProvider.GetRequiredService<FailingAgentToolExecutor>());
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["AgenticWorkloads"]));
            engine.AddAgentics(options =>
            {
                options.Tools.Add(new AgentToolDescriptor(
                    id: "failing-analyst",
                    displayName: "Failing Analyst",
                    description: "Exercises terminal failed run posture for the managed agentics lane."));
            });
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IAgentToolDispatcher>();

        var result = await dispatcher.ExecuteAsync(new AgentToolExecutionRequest(
            toolId: "failing-analyst",
            runId: "analysis-run-terminal-failure-001",
            actorId: "operator",
            correlationId: "corr-agentics-terminal-failure-001"));

        var runCatalog = provider.GetRequiredService<IAgentToolRunCatalog>();
        var technologySurfaces = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var runState = Assert.Single(runCatalog.GetByToolId("failing-analyst"));
        var agenticsSurface = Assert.Single(technologySurfaces.GetByTechnology("agentic-workloads"));
        var failedEntry = Assert.Single(agenticsSurface.Entries, entry => entry.Id == "failing-analyst");

        Assert.Equal(AgentToolExecutionOutcomes.Failed, result.Outcome);
        Assert.Equal("Failing analyst tool failed.", result.Error);
        Assert.Equal(AgentToolExecutionOutcomes.Failed, runState.LastOutcome);
        Assert.Equal(1, runState.StartedCount);
        Assert.Equal(1, runState.FailedCount);
        Assert.Equal(2, runState.TotalReports);
        Assert.True(runState.TerminalFailure);
        Assert.True(runState.IsTerminal);
        Assert.False(runState.RetryPending);
        Assert.False(runState.RequiresApproval);
        Assert.Equal("failing-analyst", runState.ToolId);
        Assert.Equal("analysis-run-terminal-failure-001", runState.RunId);
        Assert.Equal("Failing analyst tool failed.", runState.LastError);
        Assert.Equal("failed", failedEntry.Metadata["lastOutcome"]);
        Assert.Equal("1", failedEntry.Metadata["failedCount"]);
        Assert.Equal("true", failedEntry.Metadata["terminalFailure"]);
        Assert.Equal("true", failedEntry.Metadata["isTerminal"]);
        Assert.Equal(nameof(FailingAgentToolExecutor), failedEntry.Metadata["reported.executor"]);
    }

    [Fact]
    public void AddTechnologyPacksRejectAgentToolsThatReferenceUnknownRuntimeContracts()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["AgenticWorkloads"]));
            engine.AddAgentics(options =>
            {
                options.Tools.Add(new AgentToolDescriptor(
                    id: "broken-planner",
                    displayName: "Broken Planner",
                    description: "References runtime contracts that do not exist.",
                    capabilityKeys: ["workflow.approval.request"],
                    executionGraphId: "missing-flow"));
            });
            engine.AddModule(new PlatformTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IAgentToolCatalog>());

        Assert.Contains("unknown capability", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddTechnologyPacksStayDormantWhenSelectionsAreInactive()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"]));
            engine.AddAgentics(options =>
            {
                options.Tools.Add(new AgentToolDescriptor(
                    id: "planner",
                    displayName: "Planner",
                    description: "Creates agent plans."));
            });
            engine.AddRetrieval(options =>
            {
                options.Collections.Add(new KnowledgeCollectionDescriptor(
                    id: "docs",
                    displayName: "Docs",
                    description: "Knowledge base for retrieval."));
            });
            engine.AddModule(new TechnologyPackContributionModule());
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "orders",
                    displayName: "Orders",
                    description: "Integration events for the order domain."));
            });
            engine.AddEdge(options =>
            {
                options.Nodes.Add(new EdgeNodeDescriptor(
                    id: "storefront-edge",
                    displayName: "Storefront Edge",
                    description: "Regional node serving intermittently connected storefront experiences."));
            });
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();

        Assert.Null(provider.GetService<IAgentToolCatalog>());
        Assert.Null(provider.GetService<IAgentToolDispatcher>());
        Assert.Null(provider.GetService<IAgentToolRunCatalog>());
        Assert.Null(provider.GetService<IEventChannelCatalog>());
        Assert.Null(provider.GetService<IEventSubscriptionCatalog>());
        Assert.Null(provider.GetService<IEventDispatchRuntimeCatalog>());
        Assert.Null(provider.GetService<IEventDispatchRuntimeReporter>());
        Assert.Null(provider.GetService<IEventSubscriptionRuntimeCatalog>());
        Assert.Null(provider.GetService<IEventSubscriptionRuntimeReporter>());
        Assert.Null(provider.GetService<IEventSubscriptionExecutionReadinessCatalog>());
        Assert.Null(provider.GetService<IKnowledgeCatalog>());
        Assert.Null(provider.GetService<IKnowledgeIndexer>());
        Assert.Null(provider.GetService<IKnowledgeQueryEngine>());
        Assert.Null(provider.GetService<IKnowledgeIndexCatalog>());
        Assert.Null(provider.GetService<IEdgeNodeCatalog>());
        Assert.DoesNotContain(runtime.Manifest.Capabilities, capability => capability.Key.StartsWith("agentics.", StringComparison.Ordinal));
        Assert.DoesNotContain(runtime.Manifest.Capabilities, capability => capability.Key.StartsWith("eventing.", StringComparison.Ordinal));
        Assert.DoesNotContain(runtime.Manifest.Capabilities, capability => capability.Key.StartsWith("retrieval.", StringComparison.Ordinal));
        Assert.DoesNotContain(runtime.Manifest.Capabilities, capability => capability.Key.StartsWith("edge.", StringComparison.Ordinal));
    }

    [Fact]
    public void AddTechnologyPacksRejectSubscriptionsThatReferenceUnknownChannels()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["EventDrivenIntegration"]));
            engine.AddEventing(options =>
            {
                options.Subscriptions.Add(new EventSubscriptionDescriptor(
                    id: "broken-subscription",
                    displayName: "Broken Subscription",
                    description: "References a missing channel.",
                    channelId: "missing-channel",
                    handlerId: "missing-handler",
                    deliveryMode: "background-service"));
            });
        });

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IEventSubscriptionCatalog>());

        Assert.Contains("broken-subscription", exception.Message, StringComparison.Ordinal);
        Assert.Contains("missing-channel", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddTechnologyPacksCanReportSubscriptionRuntimeStateWithoutHostedExecutionLink()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["EventDrivenIntegration"]));
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "audit",
                    displayName: "Audit",
                    description: "Audit integration events."));
                options.Subscriptions.Add(new EventSubscriptionDescriptor(
                    id: "audit-projection",
                    displayName: "Audit Projection",
                    description: "Projects audit events into a read model.",
                    channelId: "audit",
                    handlerId: "audit-projection-handler",
                    deliveryMode: "application-service"));
            });
        });

        using var provider = services.BuildServiceProvider();
        var reporter = provider.GetRequiredService<IEventSubscriptionRuntimeReporter>();
        var readinessCatalog = provider.GetRequiredService<IEventSubscriptionExecutionReadinessCatalog>();
        var surfaces = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        await reporter.ReportAsync(
            new EventSubscriptionExecutionReport(
                subscriptionId: "audit-projection",
                outcome: EventSubscriptionExecutionOutcomes.Failed,
                observedAtUtc: new DateTimeOffset(2026, 04, 04, 10, 15, 0, TimeSpan.Zero),
                messageId: "audit-msg-404",
                attempt: 3,
                error: "Projection store unavailable",
                metadata: new Dictionary<string, string>
                {
                    ["retryPolicy"] = "linear",
                    ["retryWindow"] = "00:00:30"
                }));

        var eventingSubscriptionSurface = Assert.Single(
            surfaces.GetByTechnology("event-driven-integration"),
            surface => surface.SurfaceId == "event-subscriptions");
        var runtimeEntry = Assert.Single(eventingSubscriptionSurface.Entries, entry => entry.Id == "audit-projection");
        var readiness = Assert.Single(readinessCatalog.Readiness);

        Assert.Equal("application-managed", runtimeEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.DispatchRuntime]);
        Assert.Equal("application-managed-state", runtimeEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.SubscriptionRuntime]);
        Assert.Equal(EventSubscriptionExecutionReadinessStates.ApplicationManagedState, readiness.ReadinessState);
        Assert.True(readiness.HasExecutionPath);
        Assert.Equal("application-managed", readiness.ExecutionOwnership);
        Assert.Equal("runtime-reported", readiness.ExecutionMode);
        Assert.Contains("runtime-state-reported", readiness.Reasons);
        Assert.Equal(EventSubscriptionExecutionReadinessStates.ApplicationManagedState, runtimeEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionReadiness]);
        Assert.Equal("observed", runtimeEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionPath]);
        Assert.Equal("reported", runtimeEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.RuntimeState]);
        Assert.Equal("failed", runtimeEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.LastOutcome]);
        Assert.Equal("audit-msg-404", runtimeEntry.Metadata["lastMessageId"]);
        Assert.Equal("3", runtimeEntry.Metadata["lastAttempt"]);
        Assert.Equal("1", runtimeEntry.Metadata["failedCount"]);
        Assert.Equal("1", runtimeEntry.Metadata["totalReports"]);
        Assert.Equal("linear", runtimeEntry.Metadata[$"{EventSubscriptionRuntimeMetadataKeys.ReportedMetadataPrefix}retryPolicy"]);
        Assert.Equal("00:00:30", runtimeEntry.Metadata[$"{EventSubscriptionRuntimeMetadataKeys.ReportedMetadataPrefix}retryWindow"]);
        Assert.Equal("Projection store unavailable", runtimeEntry.Metadata["lastError"]);
    }

    [Fact]
    public void AddTechnologyPacksExposeDeclaredOnlySubscriptionReadiness()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["EventDrivenIntegration"]));
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "audit",
                    displayName: "Audit",
                    description: "Audit integration events."));
                options.Subscriptions.Add(new EventSubscriptionDescriptor(
                    id: "audit-projection",
                    displayName: "Audit Projection",
                    description: "Projects audit events into a read model.",
                    channelId: "audit",
                    handlerId: "audit-projection-handler",
                    deliveryMode: "application-service"));
            });
        });

        using var provider = services.BuildServiceProvider();
        var readinessCatalog = provider.GetRequiredService<IEventSubscriptionExecutionReadinessCatalog>();
        var surfaces = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        var readiness = Assert.Single(readinessCatalog.Readiness);
        var runtimeEntry = Assert.Single(
            surfaces.GetByTechnology("event-driven-integration")
                .Single(surface => surface.SurfaceId == "event-subscriptions")
                .Entries,
            entry => entry.Id == "audit-projection");

        Assert.Equal(EventSubscriptionExecutionReadinessStates.DeclaredOnly, readiness.ReadinessState);
        Assert.False(readiness.HasExecutionPath);
        Assert.Equal("not-configured", readiness.ExecutionOwnership);
        Assert.Equal("not-configured", readiness.ExecutionMode);
        Assert.Contains("no-execution-path-observed", readiness.Reasons);
        Assert.True(readinessCatalog.TryGet("audit-projection", out var resolvedReadiness));
        Assert.Equal(EventSubscriptionExecutionReadinessStates.DeclaredOnly, resolvedReadiness?.ReadinessState);
        Assert.False(readinessCatalog.TryGet("missing-subscription", out resolvedReadiness));
        Assert.Null(resolvedReadiness);
        Assert.Equal(EventSubscriptionExecutionReadinessStates.DeclaredOnly, runtimeEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionReadiness]);
        Assert.Equal("not-observed", runtimeEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionPath]);
        Assert.Equal("no-execution-path-observed", runtimeEntry.Metadata[EventSubscriptionRuntimeMetadataKeys.ExecutionReadinessReasons]);
    }

    [Fact]
    public async Task AddTechnologyPacksRejectSubscriptionRuntimeReportsForUnknownSubscriptions()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"],
                technologies: ["EventDrivenIntegration"]));
            engine.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "audit",
                    displayName: "Audit",
                    description: "Audit integration events."));
            });
        });

        using var provider = services.BuildServiceProvider();
        var reporter = provider.GetRequiredService<IEventSubscriptionRuntimeReporter>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await reporter.ReportAsync(
                new EventSubscriptionExecutionReport(
                    subscriptionId: "missing-subscription",
                    outcome: EventSubscriptionExecutionOutcomes.Started,
                    observedAtUtc: new DateTimeOffset(2026, 04, 04, 10, 30, 0, TimeSpan.Zero),
                    messageId: "audit-msg-500",
                    attempt: 1)));

        Assert.Contains("missing-subscription", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSkipsTechnologyAwareActivationWhenTechnologyIsNotSelected()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularVerticalSlice",
                transports: ["WebSocket"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new TechnologyCatalogTestModule());
            engine.AddModule(new TechnologyAwareModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var technologySelection = provider.GetRequiredService<TechnologySelection>();

        Assert.False(technologySelection.IsSelected("DigitalTwinOrchestration"));
        Assert.True(technologySelection.IsAvailable("DigitalTwinOrchestration"));
        Assert.Null(provider.GetService<TechnologyActivationMarker>());
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "technology-aware.base");
        Assert.DoesNotContain(runtime.Manifest.Capabilities, capability => capability.Key == "technology-aware.digital-twin");
    }

    [Fact]
    public void BuildBlocksUntrustedPackagesWhenTrustPolicyRequiresIt()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseTrustPolicy(new TrustPolicy(requireTrustedPackages: true));
        builder.AddPackageAssembly(GetReferenceModuleAssemblyPath(), id: "reference-operations");

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("reference-operations", exception.Message, StringComparison.Ordinal);
        Assert.Contains("not trusted", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildAllowsTrustedPackagesAndFiltersCapabilitiesByTrustPolicy()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularMonolith",
            transports: ["RestApi"],
            trustPolicy: new TrustPolicy(
                requireTrustedPackages: true,
                defaultCapabilityAccess: CapabilityAccess.TrustedOnly,
                trustedPackages: ["reference-operations"],
                capabilities: new Dictionary<string, CapabilityAccess>
                {
                    ["operations.localization"] = CapabilityAccess.Denied
                })));
        builder.AddPackageAssembly(GetReferenceModuleAssemblyPath(), id: "reference-operations");

        var runtime = builder.Build();
        var trust = builder.Services.BuildServiceProvider().GetRequiredService<CapabilityPolicyEvaluator>().Snapshot;
        var operationsModule = Assert.Single(runtime.Manifest.Modules, module => module.Id == "operations");
        var package = Assert.Single(runtime.Manifest.Packages);

        Assert.True(package.IsTrusted);
        Assert.True(operationsModule.IsTrusted);
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "operations.status");
        Assert.DoesNotContain(runtime.Manifest.Capabilities, capability => capability.Key == "operations.localization");
        Assert.Contains(trust.Capabilities, decision =>
            decision.CapabilityKey == "operations.status" &&
            decision.IsAllowed &&
            decision.Access == CapabilityAccess.TrustedOnly);
        Assert.Contains(trust.Capabilities, decision =>
            decision.CapabilityKey == "operations.localization" &&
            !decision.IsAllowed &&
            decision.Access == CapabilityAccess.Denied);
    }

    [Fact]
    public void BuildAllowsPackagesTrustedByChecksumAllowList()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseTrustPolicy(new TrustPolicy(
            requireTrustedPackages: true,
            allowedPackageChecksums: new Dictionary<string, IReadOnlyList<string>>
            {
                ["reference-operations"] = [ComputeSha256(GetReferenceModuleAssemblyPath())]
            }));
        builder.AddPackageAssembly(GetReferenceModuleAssemblyPath(), id: "reference-operations");

        var runtime = builder.Build();
        var package = Assert.Single(runtime.Manifest.Packages);

        Assert.True(package.IsTrusted);
        Assert.Equal("Package checksum is allow-listed by the current trust policy.", package.TrustReason);
    }

    [Fact]
    public void BuildAllowsPackagesTrustedByPublisher()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseTrustPolicy(new TrustPolicy(
            requireTrustedPackages: true,
            trustedPublishers: ["cephalon-labs"]));
        builder.AddPackageManifest(GetReferenceModuleManifestPath());

        var runtime = builder.Build();
        var package = Assert.Single(runtime.Manifest.Packages);

        Assert.True(package.IsTrusted);
        Assert.Equal("Package publisher is explicitly trusted by the current trust policy.", package.TrustReason);
    }

    [Fact]
    public void BuildAllowsPackagesTrustedBySignerFingerprint()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseTrustPolicy(new TrustPolicy(
            requireTrustedPackages: true,
            trustedSignerFingerprints: ["sha256:cephalon-labs-reference-operations"]));
        builder.AddPackageManifest(GetReferenceModuleManifestPath());

        var runtime = builder.Build();
        var package = Assert.Single(runtime.Manifest.Packages);

        Assert.True(package.IsTrusted);
        Assert.Equal("Package signer fingerprint is explicitly trusted by the current trust policy.", package.TrustReason);
    }

    [Fact]
    public void AddCephalonRegistersLocalizedTextCatalogAndSupportsOverrides()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Localization:DefaultCulture"] = "th",
                ["Engine:Localization:SupportedCultures:0"] = "en",
                ["Engine:Localization:SupportedCultures:1"] = "th",
                ["Engine:Localization:Resources:th:engine.docs.rest.title"] = "Cephalon เอกสารไทย"
            })
            .Build();

        services.AddCephalon(configuration, engine =>
        {
            engine.AddLanguageResources("ja", new Dictionary<string, string>
            {
                ["engine.docs.rest.title"] = "Cephalon REST API 日本語"
            });
        });

        using var provider = services.BuildServiceProvider();
        var localizedTextCatalog = provider.GetRequiredService<ILocalizedTextCatalog>();

        Assert.Equal("th", localizedTextCatalog.DefaultCulture);
        Assert.Equal("Cephalon เอกสารไทย", localizedTextCatalog.ResolveText("engine.docs.rest.title", "th"));
        Assert.Equal("Cephalon REST API 日本語", localizedTextCatalog.ResolveText("engine.docs.rest.title", "ja"));
        Assert.Contains("en", localizedTextCatalog.SupportedCultures);
        Assert.Contains("th", localizedTextCatalog.SupportedCultures);
        Assert.Contains("ja", localizedTextCatalog.SupportedCultures);
    }

    [Fact]
    public void AddCephalonAllowsProjectsToOverrideLocalizationSettings()
    {
        var services = new ServiceCollection();
        services.AddCephalon(static _ => { });
        services.AddSingleton(new LocalizationSettings(
            defaultCulture: "fr",
            supportedCultures: ["en", "fr"],
            resources: new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["fr"] = new Dictionary<string, string>
                {
                    ["engine.docs.rest.title"] = "API REST Cephalon"
                }
            }));

        using var provider = services.BuildServiceProvider();
        var localizedTextCatalog = provider.GetRequiredService<ILocalizedTextCatalog>();

        Assert.Equal("fr", localizedTextCatalog.DefaultCulture);
        Assert.Equal("API REST Cephalon", localizedTextCatalog.ResolveText("engine.docs.rest.title", "fr"));
        Assert.Contains("fr", localizedTextCatalog.SupportedCultures);
    }

    [Fact]
    public void AddCephalonMergesModuleLanguagePacksBeforeProjectOverrides()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.AddModule(new LocalizationPackTestModule());
            engine.AddLanguageResources("es", new Dictionary<string, string>
            {
                ["engine.docs.rest.title"] = "API REST del Proyecto"
            });
        });

        using var provider = services.BuildServiceProvider();
        var localizedTextCatalog = provider.GetRequiredService<ILocalizedTextCatalog>();
        var snapshot = localizedTextCatalog.CreateSnapshot("es");

        Assert.Equal("es", snapshot.ResolvedCulture);
        Assert.Equal("API REST del Proyecto", snapshot.Resources["engine.docs.rest.title"]);
        Assert.Equal(
            "Superficie REST expuesta por el host ASP.NET Core de Cephalon.",
            snapshot.Resources["engine.docs.rest.description"]);
        Assert.Contains("es", localizedTextCatalog.SupportedCultures);
    }

    [Fact]
    public void AddModulesFromAssemblyDiscoversModules()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.AddModulesFromAssemblyContaining<PlatformTestModule>(type =>
            type == typeof(PlatformTestModule) ||
            type == typeof(DiscoveryTestModule));

        var runtime = builder.Build();

        Assert.Collection(
            runtime.Manifest.Modules,
            module => Assert.Equal("platform", module.Id),
            module => Assert.Equal("discovery", module.Id));
    }

    [Fact]
    public void AddModulesFromAssemblySupportsOptInFilters()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.AddModulesFromAssemblyContaining<PlatformTestModule>(type => type == typeof(PlatformTestModule));

        var runtime = builder.Build();

        Assert.Single(runtime.Manifest.Modules);
        Assert.Equal("platform", runtime.Manifest.Modules[0].Id);
    }

    [Fact]
    public void BuildIncludesBlueprintScaffoldPlanAndTransportPackageHints()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(new EngineSettings(
            blueprint: "ModularVerticalSlice",
            transports: ["JsonRpc", "Grpc", "GraphQL"]));
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new DiscoveryTestModule());

        var runtime = builder.Build();
        var scaffold = Assert.IsType<ScaffoldPlan>(runtime.Manifest.AppProfile.Scaffold);

        Assert.Equal("modular-vertical-slice", scaffold.Id);
        Assert.Contains(scaffold.Folders, folder =>
            folder.ProjectId == "module" &&
            folder.PathTemplate == "Features/{FeatureName}/Commands");
        Assert.Contains(scaffold.Conventions, convention =>
            convention.Contains("AddJsonRpcTransport()", StringComparison.Ordinal));
        Assert.Contains(scaffold.Conventions, convention =>
            convention.Contains("AddGrpcTransport()", StringComparison.Ordinal));
        Assert.Contains(scaffold.Conventions, convention =>
            convention.Contains("AddGraphQLTransport()", StringComparison.Ordinal));
        Assert.Contains(scaffold.Projects, project =>
            project.Id == "host" &&
            project.Role == ProjectRoles.Host &&
            project.Packages.Contains("Cephalon.AspNetCore", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Observability", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.GraphQL", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.JsonRpc", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.Grpc", StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildAppliesModuleAndCapabilityOptionsFromConfiguration()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Blueprint"] = "ModularVerticalSlice",
                ["Engine:Transports:0"] = "RestApi",
                ["Engine:Options:Modules:discovery:Enabled"] = "false",
                ["Engine:Options:Capabilities:platform.clock"] = "false"
            })
            .Build();

        services.AddCephalon(configuration, cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new DiscoveryTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();
        var options = provider.GetRequiredService<EngineOptions>();

        Assert.Single(runtime.Manifest.Modules);
        Assert.Equal("platform", runtime.Manifest.Modules[0].Id);
        Assert.Empty(runtime.Manifest.Capabilities);
        Assert.False(options.IsModuleEnabled("discovery"));
        Assert.False(options.IsCapabilityEnabled("platform.clock"));
    }

    [Fact]
    public void BuildOrdersModulesAndCollectsCapabilities()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:Blueprint"] = "ModularVerticalSlice",
                ["Engine:Patterns:0"] = "StrategyPattern",
                ["Engine:Transports:0"] = "RestApi"
            })
            .Build();

        services.AddCephalon(configuration, cephalon =>
        {
            cephalon.AddModule(new DiscoveryTestModule());
            cephalon.AddModule(new PlatformTestModule());
        });

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IRuntime>();

        Assert.Collection(
            runtime.Manifest.Modules,
            module => Assert.Equal("platform", module.Id),
            module => Assert.Equal("discovery", module.Id));

        Assert.Equal("modular-vertical-slice", runtime.Manifest.AppProfile.BlueprintId);
        Assert.Contains(runtime.Manifest.AppProfile.Patterns, pattern => pattern.Id == "shared-foundation-pattern");
        Assert.Contains(runtime.Manifest.AppProfile.Patterns, pattern => pattern.Id == "strategy-pattern");
        Assert.Contains(runtime.Manifest.AppProfile.Transports, transport => transport.Id == "rest-api");
        Assert.Contains(runtime.Manifest.Capabilities, capability =>
            capability.Key == "platform.clock" &&
            capability.SourceModuleId == "platform" &&
            capability.Metadata["kind"] == "clock");
        Assert.Contains(runtime.Manifest.Capabilities, capability =>
            capability.Key == "discovery.greetings" &&
            capability.SourceModuleId == "discovery" &&
            capability.Metadata["dependsOn"] == "platform.clock");
    }

    [Fact]
    public void BuildThrowsWhenEnabledModuleDependsOnDisabledModule()
    {
        var settings = new EngineSettings(
            blueprint: "ModularVerticalSlice",
            transports: ["RestApi"],
            options: new EngineOptions(
                modules: new Dictionary<string, bool>
                {
                    ["platform"] = false
                }));

        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(settings);
        builder.AddModule(new PlatformTestModule());
        builder.AddModule(new DiscoveryTestModule());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("disabled by engine options", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildThrowsWhenDependencyIsMissing()
    {
        var builder = new EngineBuilder(new ServiceCollection());
        builder.AddModule(new DiscoveryTestModule());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("PlatformTestModule", exception.Message);
    }

    [Fact]
    public void BuildThrowsWhenSelectedPatternsConflict()
    {
        var settings = new EngineSettings(
            blueprint: "ModularVerticalSlice",
            patterns: ["MicroserviceTopology"]);

        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(settings);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("microservice-topology", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildThrowsWhenConfigurationReferencesUnknownPattern()
    {
        var settings = new EngineSettings(
            blueprint: "ModularVerticalSlice",
            patterns: ["UnknownPattern"]);

        var builder = new EngineBuilder(new ServiceCollection());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.UseSettings(settings));

        Assert.Contains("UnknownPattern", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildThrowsWhenConfigurationReferencesUnknownTransport()
    {
        var settings = new EngineSettings(
            blueprint: "ModularVerticalSlice",
            patterns: ["StrategyPattern"],
            transports: ["UnknownTransport"]);

        var builder = new EngineBuilder(new ServiceCollection());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.UseSettings(settings));

        Assert.Contains("UnknownTransport", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildThrowsWhenConfigurationReferencesUnknownTechnology()
    {
        var settings = new EngineSettings(
            blueprint: "ModularVerticalSlice",
            technologies: ["UnknownTechnology"]);

        var builder = new EngineBuilder(new ServiceCollection());
        builder.UseSettings(settings);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("UnknownTechnology", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildThrowsWhenDiscoveryAssemblyCannotBeLoaded()
    {
        var settings = new EngineSettings(
            blueprint: "ModularVerticalSlice",
            discovery: new ModuleDiscoverySettings(["Cephalon.Missing.Modules"]));

        var builder = new EngineBuilder(new ServiceCollection());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.UseSettings(settings));

        Assert.Contains("Cephalon.Missing.Modules", exception.Message, StringComparison.Ordinal);
    }

    private static string GetReferenceModuleAssemblyPath()
    {
        var path = typeof(OperationsModule).Assembly.Location;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Reference module assembly location was not available.");
        }

        return path;
    }

    private static SignedPackageFixture CreateSignedPackageFixture(
        bool tamperSignature = false,
        bool includeKeyId = true,
        bool includeSignatureValue = true,
        bool includeFingerprint = true)
    {
        var assemblyPath = GetReferenceModuleAssemblyPath();
        var keyId = "cephalon-labs-build";
        var directory = Path.Combine(Path.GetTempPath(), $"cephalon-signed-package-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        using var rsa = RSA.Create(2048);
        var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        var fingerprint = Convert.ToHexString(SHA256.HashData(publicKeyBytes)).ToLowerInvariant();
        var publicKeyPath = Path.Combine(directory, "trusted-signing-key.pem");
        File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());

        using var assemblyStream = File.OpenRead(assemblyPath);
        var assemblyHash = SHA256.HashData(assemblyStream);
        var signatureBytes = rsa.SignHash(
            assemblyHash,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        if (tamperSignature)
        {
            signatureBytes[0] ^= 0xFF;
        }

        var signatureProperties = new List<string>
        {
            "    \"type\": \"detached-signature\"",
            "    \"signer\": \"Cephalon Labs Build\"",
            "    \"algorithm\": \"RSA-SHA256\""
        };

        if (includeKeyId)
        {
            signatureProperties.Add($"    \"keyId\": \"{keyId}\"");
        }

        if (includeFingerprint)
        {
            signatureProperties.Add($"    \"fingerprint\": \"sha256:{fingerprint}\"");
        }

        if (includeSignatureValue)
        {
            signatureProperties.Add($"    \"value\": \"{Convert.ToBase64String(signatureBytes)}\"");
        }

        var manifestContents =
            "{\n" +
            "  \"id\": \"signed-operations\",\n" +
            "  \"version\": \"1.0.0\",\n" +
            $"  \"assembly\": \"{EscapeJson(assemblyPath)}\",\n" +
            "  \"publisher\": {\n" +
            "    \"id\": \"cephalon-labs\",\n" +
            "    \"displayName\": \"Cephalon Labs\"\n" +
            "  },\n" +
            "  \"signature\": {\n" +
            string.Join(",\n", signatureProperties) + "\n" +
            "  },\n" +
            "  \"compatibility\": {\n" +
            $"    \"minimumEngineVersion\": \"{GetCurrentEngineCompatibilityVersion()}\",\n" +
            "    \"supportedTargetFrameworks\": [ \"net10.0\" ]\n" +
            "  }\n" +
            "}";

        var manifestPath = Path.Combine(directory, ModulePackageDirectory.DefaultManifestFileName);
        File.WriteAllText(manifestPath, manifestContents);

        return new SignedPackageFixture(manifestPath, publicKeyPath, keyId, fingerprint);
    }

    private static MultiSignedPackageFixture CreateMultiSignedPackageFixture()
    {
        var assemblyPath = GetReferenceModuleAssemblyPath();
        var directory = Path.Combine(Path.GetTempPath(), $"cephalon-multi-signed-package-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        using var assemblyStream = File.OpenRead(assemblyPath);
        var assemblyHash = SHA256.HashData(assemblyStream);

        var signers = new List<SignedKeyFixture>();
        for (var index = 0; index < 2; index++)
        {
            using var rsa = RSA.Create(2048);
            var keyId = $"cephalon-labs-build-{index + 1}";
            var signer = $"Cephalon Labs Build {index + 1}";
            var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
            var fingerprint = Convert.ToHexString(SHA256.HashData(publicKeyBytes)).ToLowerInvariant();
            var publicKeyPath = Path.Combine(directory, $"trusted-signing-key-{index + 1}.pem");
            File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
            var signature = Convert.ToBase64String(rsa.SignHash(
                assemblyHash,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1));

            signers.Add(new SignedKeyFixture(keyId, signer, fingerprint, publicKeyPath, signature));
        }

        var signaturesJson = string.Join(
            ",\n",
            signers.Select(static signer =>
                "    {\n" +
                "      \"type\": \"detached-signature\",\n" +
                $"      \"signer\": \"{signer.Signer}\",\n" +
                $"      \"keyId\": \"{signer.KeyId}\",\n" +
                $"      \"fingerprint\": \"sha256:{signer.Fingerprint}\",\n" +
                "      \"algorithm\": \"RSA-SHA256\",\n" +
                $"      \"value\": \"{signer.SignatureValue}\"\n" +
                "    }"));

        var manifestContents =
            "{\n" +
            "  \"id\": \"multi-signed-operations\",\n" +
            "  \"version\": \"1.0.0\",\n" +
            $"  \"assembly\": \"{EscapeJson(assemblyPath)}\",\n" +
            "  \"publisher\": {\n" +
            "    \"id\": \"cephalon-labs\",\n" +
            "    \"displayName\": \"Cephalon Labs\"\n" +
            "  },\n" +
            "  \"signatures\": [\n" +
            signaturesJson + "\n" +
            "  ],\n" +
            "  \"compatibility\": {\n" +
            $"    \"minimumEngineVersion\": \"{GetCurrentEngineCompatibilityVersion()}\",\n" +
            "    \"supportedTargetFrameworks\": [ \"net10.0\" ]\n" +
            "  }\n" +
            "}";

        var manifestPath = Path.Combine(directory, ModulePackageDirectory.DefaultManifestFileName);
        File.WriteAllText(manifestPath, manifestContents);

        return new MultiSignedPackageFixture(manifestPath, signers);
    }

    private static CertificateSignedPackageFixture CreateCertificateSignedPackageFixture()
    {
        var assemblyPath = GetReferenceModuleAssemblyPath();
        var keyId = "cephalon-labs-signing-cert";
        var directory = Path.Combine(Path.GetTempPath(), $"cephalon-certificate-signed-package-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        using var rootKey = RSA.Create(2048);
        var rootRequest = new CertificateRequest(
            "CN=Cephalon Test Root",
            rootKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        rootRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(rootRequest.PublicKey, false));
        using var rootCertificate = rootRequest.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(10));

        using var signingKey = RSA.Create(2048);
        var signingRequest = new CertificateRequest(
            "CN=Cephalon Labs Signing",
            signingKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        signingRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        signingRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        signingRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(signingRequest.PublicKey, false));
        var serialNumber = RandomNumberGenerator.GetBytes(16);
        using var issuedCertificate = signingRequest.Create(
            rootCertificate,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(2),
            serialNumber);
        using var signingCertificateWithKey = issuedCertificate.CopyWithPrivateKey(signingKey);

        var publicKeyBytes = signingKey.ExportSubjectPublicKeyInfo();
        var fingerprint = Convert.ToHexString(SHA256.HashData(publicKeyBytes)).ToLowerInvariant();
        var certificateThumbprint = NormalizeCertificateThumbprint(signingCertificateWithKey.Thumbprint);

        var rootCertificatePath = Path.Combine(directory, "trusted-root-cert.pem");
        File.WriteAllText(rootCertificatePath, rootCertificate.ExportCertificatePem());

        var signingCertificatePath = Path.Combine(directory, "trusted-signing-cert.pem");
        File.WriteAllText(signingCertificatePath, signingCertificateWithKey.ExportCertificatePem());

        using var assemblyStream = File.OpenRead(assemblyPath);
        var assemblyHash = SHA256.HashData(assemblyStream);
        var signatureBytes = signingKey.SignHash(
            assemblyHash,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var manifestContents =
            "{\n" +
            "  \"id\": \"signed-operations\",\n" +
            "  \"version\": \"1.0.0\",\n" +
            $"  \"assembly\": \"{EscapeJson(assemblyPath)}\",\n" +
            "  \"publisher\": {\n" +
            "    \"id\": \"cephalon-labs\",\n" +
            "    \"displayName\": \"Cephalon Labs\"\n" +
            "  },\n" +
            "  \"signature\": {\n" +
            "    \"type\": \"detached-signature\",\n" +
            "    \"signer\": \"Cephalon Labs Build\",\n" +
            $"    \"keyId\": \"{keyId}\",\n" +
            $"    \"fingerprint\": \"sha256:{fingerprint}\",\n" +
            "    \"algorithm\": \"RSA-SHA256\",\n" +
            $"    \"value\": \"{Convert.ToBase64String(signatureBytes)}\"\n" +
            "  },\n" +
            "  \"compatibility\": {\n" +
            $"    \"minimumEngineVersion\": \"{GetCurrentEngineCompatibilityVersion()}\",\n" +
            "    \"supportedTargetFrameworks\": [ \"net10.0\" ]\n" +
            "  }\n" +
            "}";

        var manifestPath = Path.Combine(directory, ModulePackageDirectory.DefaultManifestFileName);
        File.WriteAllText(manifestPath, manifestContents);

        return new CertificateSignedPackageFixture(
            manifestPath,
            signingCertificatePath,
            rootCertificatePath,
            keyId,
            fingerprint,
            certificateThumbprint);
    }

    private static string GetReferenceModuleManifestPath()
    {
        return Path.Combine(GetReferenceModulePackageDirectory(), ModulePackageDirectory.DefaultManifestFileName);
    }

    private static string GetCurrentEngineCompatibilityVersion()
    {
        var version = typeof(EngineBuilder).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? typeof(EngineBuilder).Assembly.GetName().Version?.ToString()
            ?? "0.0.0";
        var buildMetadataSeparator = version.IndexOf('+', StringComparison.Ordinal);

        return buildMetadataSeparator >= 0
            ? version[..buildMetadataSeparator]
            : version;
    }

    private static string GetAdditionalPackageAssemblyPath()
    {
        var candidates = EnumerateAdditionalPackageAssemblyCandidates()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var candidate = candidates.FirstOrDefault(File.Exists);
        if (candidate is not null)
        {
            return candidate;
        }

        throw new FileNotFoundException(
            "Could not find the additional package sample assembly. Tried: " +
            string.Join("; ", candidates));
    }

    private static IEnumerable<string> EnumerateAdditionalPackageAssemblyCandidates()
    {
        var repositoryRoot = RepositoryPaths.GetRepositoryRoot();
        var currentConfiguration = GetCurrentTestConfiguration();

        if (!string.IsNullOrWhiteSpace(currentConfiguration))
        {
            yield return GetAdditionalPackageAssemblyPath(repositoryRoot, currentConfiguration);
        }

        yield return GetAdditionalPackageAssemblyPath(repositoryRoot, "Debug");
        yield return GetAdditionalPackageAssemblyPath(repositoryRoot, "Release");
    }

    private static string GetAdditionalPackageAssemblyPath(string repositoryRoot, string configuration)
    {
        return Path.Combine(
            repositoryRoot,
            "samples",
            "Cephalon.Sample.ModularMonolith",
            "bin",
            configuration,
            "net10.0",
            "Cephalon.Sample.ModularMonolith.dll");
    }

    private static string? GetCurrentTestConfiguration()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var parent = directory.Parent;
            if (parent?.Parent is not null &&
                string.Equals(parent.Parent.Name, "bin", StringComparison.OrdinalIgnoreCase))
            {
                return parent.Name;
            }

            directory = parent;
        }

        return null;
    }

    private static string GetReferenceModulePackageDirectory()
    {
        var directory = Path.GetDirectoryName(GetReferenceModuleAssemblyPath());
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Reference module assembly directory was not available.");
        }

        return directory;
    }

    private static string CreateTemporaryManifest(string manifestTemplate)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"cephalon-package-manifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        var manifestPath = Path.Combine(directory, ModulePackageDirectory.DefaultManifestFileName);
        var manifestContents = manifestTemplate.Replace(
            "__ASSEMBLY__",
            EscapeJson(GetReferenceModuleAssemblyPath()),
            StringComparison.Ordinal)
            .Replace(
                "__PACKAGE_ASSEMBLY__",
                EscapeJson(GetAdditionalPackageAssemblyPath()),
                StringComparison.Ordinal)
            .Replace(
                "\"minimumEngineVersion\": \"1.0.0\"",
                $"\"minimumEngineVersion\": \"{GetCurrentEngineCompatibilityVersion()}\"",
                StringComparison.Ordinal)
            .Replace(
                "\"minimumEngineVersion\": \"0.1.0-preview\"",
                $"\"minimumEngineVersion\": \"{GetCurrentEngineCompatibilityVersion()}\"",
                StringComparison.Ordinal);
        File.WriteAllText(manifestPath, manifestContents);

        return manifestPath;
    }

    private static void DeleteManifestDirectory(string manifestPath)
    {
        var directory = Path.GetDirectoryName(manifestPath);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellation.Token);
        }
    }

    private static string NormalizeCertificateThumbprint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }

    private sealed class SignedPackageFixture : IDisposable
    {
        public SignedPackageFixture(string manifestPath, string publicKeyPath, string keyId, string fingerprint)
        {
            ManifestPath = manifestPath;
            PublicKeyPath = publicKeyPath;
            KeyId = keyId;
            Fingerprint = fingerprint;
        }

        public string ManifestPath { get; }

        public string PublicKeyPath { get; }

        public string KeyId { get; }

        public string Fingerprint { get; }

        public void Dispose()
        {
            DeleteManifestDirectory(ManifestPath);
        }
    }

    private sealed class MultiSignedPackageFixture : IDisposable
    {
        public MultiSignedPackageFixture(string manifestPath, IReadOnlyList<SignedKeyFixture> signers)
        {
            ManifestPath = manifestPath;
            Signers = signers;
        }

        public string ManifestPath { get; }

        public IReadOnlyList<SignedKeyFixture> Signers { get; }

        public void Dispose()
        {
            DeleteManifestDirectory(ManifestPath);
        }
    }

    private sealed class CertificateSignedPackageFixture : IDisposable
    {
        public CertificateSignedPackageFixture(
            string manifestPath,
            string signingCertificatePath,
            string rootCertificatePath,
            string keyId,
            string fingerprint,
            string certificateThumbprint)
        {
            ManifestPath = manifestPath;
            SigningCertificatePath = signingCertificatePath;
            RootCertificatePath = rootCertificatePath;
            KeyId = keyId;
            Fingerprint = fingerprint;
            CertificateThumbprint = certificateThumbprint;
        }

        public string ManifestPath { get; }

        public string SigningCertificatePath { get; }

        public string RootCertificatePath { get; }

        public string KeyId { get; }

        public string Fingerprint { get; }

        public string CertificateThumbprint { get; }

        public void Dispose()
        {
            DeleteManifestDirectory(ManifestPath);
        }
    }

    private sealed record SignedKeyFixture(
        string KeyId,
        string Signer,
        string Fingerprint,
        string PublicKeyPath,
        string SignatureValue);

    private sealed class RetryingAgentToolExecutor : IAgentToolExecutor
    {
        public string ToolId => "retrying-analyst";

        public int CallCount { get; private set; }

        public ValueTask<AgentToolExecutionResult> ExecuteAsync(
            AgentToolExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            if (CallCount == 1)
            {
                throw new InvalidOperationException("Transient agentics executor failure.");
            }

            return ValueTask.FromResult(AgentToolExecutionResult.Succeeded(
                $"Retried agentics attempt {context.Attempt}.",
                new Dictionary<string, string>
                {
                    ["executor"] = nameof(RetryingAgentToolExecutor),
                    ["observedAttempt"] = context.Attempt.ToString(CultureInfo.InvariantCulture)
                }));
        }
    }

    private sealed class FailingAgentToolExecutor : IAgentToolExecutor
    {
        public string ToolId => "failing-analyst";

        public ValueTask<AgentToolExecutionResult> ExecuteAsync(
            AgentToolExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(AgentToolExecutionResult.Failed(
                "Failing analyst tool failed.",
                new Dictionary<string, string>
                {
                    ["executor"] = nameof(FailingAgentToolExecutor),
                    ["observedAttempt"] = context.Attempt.ToString(CultureInfo.InvariantCulture)
                }));
        }
    }

    private sealed class CountingAgentToolExecutor : IAgentToolExecutor
    {
        public string ToolId => "idempotent-analyst";

        public int CallCount { get; private set; }

        public ValueTask<AgentToolExecutionResult> ExecuteAsync(
            AgentToolExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;

            return ValueTask.FromResult(AgentToolExecutionResult.Succeeded(
                $"Idempotent agentics attempt {context.Attempt}.",
                new Dictionary<string, string>
                {
                    ["executor"] = nameof(CountingAgentToolExecutor),
                    ["observedAttempt"] = context.Attempt.ToString(CultureInfo.InvariantCulture)
                }));
        }
    }

    private static EventDispatchRuntimeState CreateDispatchRuntimeState(EventDispatchExecutionReport report)
    {
        return new EventDispatchRuntimeState(
            OutboxId: report.OutboxId,
            LastChannelId: report.ChannelId,
            LastOutcome: report.Outcome,
            LastObservedAtUtc: report.ObservedAtUtc,
            LastMessageId: report.MessageId,
            LastAttempt: report.Attempt,
            StartedCount: 0,
            SucceededCount: string.Equals(report.Outcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            FailedCount: string.Equals(report.Outcome, EventDispatchExecutionOutcomes.Failed, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            RetryScheduledCount: string.Equals(report.Outcome, EventDispatchExecutionOutcomes.RetryScheduled, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            SkippedCount: string.Equals(report.Outcome, EventDispatchExecutionOutcomes.Skipped, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            LastError: report.Error,
            Metadata: report.Metadata);
    }

    private sealed class MultiOutboxEventingTestModule : ModuleBase, IOutboxContributor
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "multi-outbox-eventing-tests",
            displayName: "Multi Outbox Eventing Tests",
            description: "Registers multiple outboxes for eventing runtime proof-selection tests.",
            tags: ["eventing", "outbox", "tests"],
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public void RegisterOutboxes(IOutboxRegistry outboxes)
        {
            outboxes.Add(new OutboxDescriptor(
                id: "alpha-outbox",
                displayName: "Alpha Outbox",
                description: "Older eventing proof outbox.",
                sourceModuleId: Descriptor.Id,
                provider: "test",
                mode: "in-memory",
                channelIds: ["contracts"],
                tags: ["test"]));
            outboxes.Add(new OutboxDescriptor(
                id: "beta-outbox",
                displayName: "Beta Outbox",
                description: "Newer eventing proof outbox.",
                sourceModuleId: Descriptor.Id,
                provider: "test",
                mode: "in-memory",
                channelIds: ["contracts"],
                tags: ["test"]));
        }
    }

    private sealed class ContractTestEventContributor : IEventContractContributor
    {
        public void RegisterEventContracts(IEventContractRegistry contracts)
        {
            contracts.Add(new EventContractDescriptor(
                id: "inventory.item.reserved.v1",
                eventType: "inventory.item.reserved",
                displayName: "Inventory Item Reserved",
                description: "Inventory event emitted when stock is reserved.",
                version: "1",
                contentType: "application/vnd.cephalon.inventory.item-reserved.v1+json",
                serializerId: "system-text-json-sourcegen",
                tags: ["inventory", "contract"]));
        }
    }

    private sealed class SerializerTestEventContributor : IEventSerializerContributor
    {
        public void RegisterEventSerializers(IEventSerializerRegistry serializers)
        {
            serializers.Add(new EventSerializerDescriptor(
                id: "cephalon-protobuf",
                displayName: "Cephalon Protobuf",
                description: "Provider-neutral protobuf serializer descriptor registered by a module.",
                contentType: "application/x-protobuf",
                format: "protobuf",
                runtimeKind: "custom",
                requiresSchemaRegistry: true,
                tags: ["binary", "contract"]));
        }
    }

    private sealed class SchemaRegistrySerializerTestEventContributor : IEventSerializerContributor
    {
        public void RegisterEventSerializers(IEventSerializerRegistry serializers)
        {
            serializers.Add(new EventSerializerDescriptor(
                id: "cephalon-avro",
                displayName: "Cephalon Avro",
                description: "Provider-neutral Avro serializer descriptor registered by a module.",
                contentType: "application/avro",
                format: "avro",
                runtimeKind: "custom",
                requiresSchemaRegistry: true,
                schemaRegistryId: "module-avro-schema-registry",
                tags: ["binary", "contract"]));
        }
    }

    private sealed class SchemaRegistryTestEventContributor : IEventSchemaRegistryContributor
    {
        public void RegisterEventSchemaRegistries(IEventSchemaRegistryRegistry registries)
        {
            registries.Add(new EventSchemaRegistryDescriptor(
                id: "module-avro-schema-registry",
                displayName: "Module Avro Schema Registry",
                description: "Provider-neutral Avro schema registry descriptor registered by a module.",
                provider: "module",
                endpointKind: "external",
                runtimeKind: "code-first",
                canReadSchemas: true,
                canWriteSchemas: false,
                validatesCompatibility: false,
                supportedFormats: ["avro"],
                tags: ["binary", "contract"]));
        }
    }

    private sealed class UpcasterTestEventContributor : IEventUpcasterContributor
    {
        public void RegisterEventUpcasters(IEventUpcasterRegistry upcasters)
        {
            upcasters.Add(new EventUpcasterDescriptor(
                id: "inventory-item-reserved-v1-to-v2",
                eventType: "inventory.item.reserved",
                displayName: "Inventory Item Reserved V1 To V2",
                description: "Provider-neutral inventory event version transition registered by a module.",
                fromVersion: "1",
                toVersion: "2",
                runtimeKind: "code-first",
                tags: ["inventory", "contract"]));
        }
    }

    private sealed class ContextPolicyTestEventContributor : IEventContextPolicyContributor
    {
        public void RegisterEventContextPolicies(IEventContextPolicyRegistry policies)
        {
            policies.Add(new EventContextPolicyDescriptor(
                id: "module-context",
                displayName: "Module Context",
                description: "Module-owned context policy descriptor registered by a module.",
                runtimeKind: "code-first",
                declaresTenantContext: true,
                declaresCorrelationId: true,
                declaresCausationId: false,
                declaresBaggage: false,
                validatesMessageHeaders: false,
                headerNames: ["cephalon-message-id"],
                tags: ["context", "module"]));
        }
    }

    private sealed class ContextPolicyExecutionProbe
    {
        public EventSubscriptionExecutionContext LastContext { get; private set; } = null!;

        public void Record(EventSubscriptionExecutionContext context)
        {
            LastContext = context;
        }
    }

    private sealed class ContextPolicyAuditExecutor(ContextPolicyExecutionProbe probe) : IEventSubscriptionExecutor
    {
        public string SubscriptionId => "context-audit";

        public ValueTask ExecuteAsync(
            EventSubscriptionExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            probe.Record(context);
            return ValueTask.CompletedTask;
        }
    }
}
