using Cephalon.Engine.AppModel;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Patterns;
using Cephalon.Engine.Runtime;
using Cephalon.Engine.Technologies;
using Cephalon.Engine.Trust;
using Cephalon.Engine.Transports;
using Cephalon.Agentics.Registration;
using Cephalon.Agentics.Services;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.AppModel.Scaffolding;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Localization;
using Cephalon.Edge.Registration;
using Cephalon.Edge.Services;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.ReferenceModule.Operations.Registration;
using Cephalon.Retrieval.Registration;
using Cephalon.Retrieval.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Cephalon.Tests.Support;
using System.Reflection;
using System.Security.Cryptography;

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
                ["Engine:Options:Modules:dependency-health:Enabled"] = "false",
                ["Engine:Options:Modules:throwing-dependency-health:Enabled"] = "false",
                ["Engine:Options:Modules:restricted:Enabled"] = "false",
                ["Engine:Options:Modules:technology-catalog:Enabled"] = "false"
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
        Assert.Equal("1.0.0", package.MinimumEngineVersion);
        Assert.EndsWith("cephalon.package.json", package.SourcePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("Cephalon.ReferenceModule.Operations.dll", package.Path, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("net10.0", package.SupportedTargetFrameworks);
        Assert.Equal("cephalon-labs", package.PublisherId);
        Assert.Equal("Cephalon Labs", package.PublisherDisplayName);
        Assert.Equal("provenance-manifest", package.SignatureType);
        Assert.Equal("Cephalon Labs Build", package.SignatureSigner);
        Assert.Null(package.SignatureKeyId);
        Assert.Equal("cephalon-labs-reference-operations", package.SignatureFingerprint);
        Assert.Equal("SHA256", package.SignatureAlgorithm);
        Assert.False(package.IsSignatureVerified);
        Assert.Contains("no signature value", package.SignatureVerificationReason, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(package.ChecksumSha256));
        Assert.Contains("operations", package.Modules);
        Assert.Equal("reference-operations", operationsModule.PackageId);
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
        Assert.Null(package.SignatureKeyId);
        Assert.Equal("cephalon-labs-reference-operations", package.SignatureFingerprint);
        Assert.False(package.IsSignatureVerified);
        Assert.Contains("no signature value", package.SignatureVerificationReason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("operations", package.Modules);
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
                "minimumEngineVersion": "1.0.0",
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
        Assert.Equal("1.0.0", package.MinimumEngineVersion);
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
        Assert.Contains(scaffold.Projects, project =>
            project.Role == ProjectRoles.Host &&
            project.Packages.Contains("Cephalon.Agentics", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Eventing", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Retrieval", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Edge", StringComparer.OrdinalIgnoreCase));
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
    public void AddTechnologyPacksRegisterServicesAndCapabilitiesWhenSelectionsAreActive()
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
        var eventChannelCatalog = provider.GetRequiredService<IEventChannelCatalog>();
        var edgeNodeCatalog = provider.GetRequiredService<IEdgeNodeCatalog>();
        var technologySurfaces = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var snapshotProvider = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>();
        var snapshot = snapshotProvider.CreateSnapshot();

        Assert.Equal(2, toolCatalog.Tools.Count);
        Assert.Contains(toolCatalog.Tools, tool => tool.Id == "planner");
        Assert.Contains(toolCatalog.Tools, tool => tool.Id == "analyst");
        Assert.Equal(2, knowledgeCatalog.Collections.Count);
        Assert.Contains(knowledgeCatalog.Collections, collection => collection.Id == "docs");
        Assert.Contains(knowledgeCatalog.Collections, collection => collection.Id == "runbooks");
        Assert.Equal(2, eventChannelCatalog.Channels.Count);
        Assert.Contains(eventChannelCatalog.Channels, channel => channel.Id == "orders");
        Assert.Contains(eventChannelCatalog.Channels, channel => channel.Id == "audit");
        Assert.Equal(2, edgeNodeCatalog.Nodes.Count);
        Assert.Contains(edgeNodeCatalog.Nodes, node => node.Id == "storefront-edge");
        Assert.Contains(edgeNodeCatalog.Nodes, node => node.Id == "warehouse-edge");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "agentics.runtime");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "agentics.tools");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.publish");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "eventing.channels");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "retrieval.query");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "retrieval.collections");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "edge.offline");
        Assert.Contains(runtime.Manifest.Capabilities, capability => capability.Key == "edge.nodes");
        Assert.Equal(4, technologySurfaces.Surfaces.Count);
        Assert.Single(technologySurfaces.GetByTechnology("agentic-workloads"));
        Assert.Contains(
            technologySurfaces.GetByTechnology("event-driven-integration").Single().Entries,
            entry => entry.Id == "audit");
        Assert.Same(runtime.Manifest, snapshot.Manifest);
        Assert.Equal(RuntimeStatus.Created, snapshot.Status.Status);
        Assert.Equal(4, snapshot.TechnologySurfaces.Count);
        Assert.Contains(
            snapshot.TechnologySurfaces.Single(surface => surface.TechnologyId == "knowledge-retrieval").Entries,
            entry => entry.Id == "runbooks");
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
        Assert.Null(provider.GetService<IEventChannelCatalog>());
        Assert.Null(provider.GetService<IKnowledgeCatalog>());
        Assert.Null(provider.GetService<IEdgeNodeCatalog>());
        Assert.DoesNotContain(runtime.Manifest.Capabilities, capability => capability.Key.StartsWith("agentics.", StringComparison.Ordinal));
        Assert.DoesNotContain(runtime.Manifest.Capabilities, capability => capability.Key.StartsWith("eventing.", StringComparison.Ordinal));
        Assert.DoesNotContain(runtime.Manifest.Capabilities, capability => capability.Key.StartsWith("retrieval.", StringComparison.Ordinal));
        Assert.DoesNotContain(runtime.Manifest.Capabilities, capability => capability.Key.StartsWith("edge.", StringComparison.Ordinal));
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
            "    \"minimumEngineVersion\": \"1.0.0\",\n" +
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
            "    \"minimumEngineVersion\": \"1.0.0\",\n" +
            "    \"supportedTargetFrameworks\": [ \"net10.0\" ]\n" +
            "  }\n" +
            "}";

        var manifestPath = Path.Combine(directory, ModulePackageDirectory.DefaultManifestFileName);
        File.WriteAllText(manifestPath, manifestContents);

        return new MultiSignedPackageFixture(manifestPath, signers);
    }

    private static string GetReferenceModuleManifestPath()
    {
        return Path.Combine(GetReferenceModulePackageDirectory(), ModulePackageDirectory.DefaultManifestFileName);
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

    private sealed record SignedKeyFixture(
        string KeyId,
        string Signer,
        string Fingerprint,
        string PublicKeyPath,
        string SignatureValue);
}
