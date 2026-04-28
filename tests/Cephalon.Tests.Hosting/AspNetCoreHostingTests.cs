using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Net;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.AppModel.Scaffolding;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Health;
using Cephalon.Abstractions.Localization;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Tenancy;
using Cephalon.Abstractions.Transports;
using Cephalon.Agentics.Registration;
using Cephalon.Agentics.Services;
using Cephalon.Audit.Registration;
using Cephalon.AspNetCore.Diagnostics;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Documentation;
using Cephalon.AspNetCore.GraphQL.Hosting;
using Cephalon.AspNetCore.Grpc.Contracts.Discovery;
using Cephalon.AspNetCore.Grpc.Hosting;
using Cephalon.AspNetCore.JsonRpc.Hosting;
using Cephalon.Data.Configuration;
using Cephalon.Data.Registration;
using Cephalon.Data.Services;
using Cephalon.Edge.Registration;
using Cephalon.Edge.Services;
using Cephalon.Engine.AppModel;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Manifest;
using Cephalon.Engine.Runtime;
using Cephalon.Engine.Transports;
using Cephalon.Engine.Trust;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Cli;
using Cephalon.ReferenceModule.Operations.Registration;
using Cephalon.ReferenceDocs.Generation;
using Cephalon.ReferenceDocs.IO;
using Cephalon.MultiTenancy.Registration;
using Cephalon.Retrieval.Registration;
using Cephalon.Retrieval.Services;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cephalon.Tests.Support;

namespace Cephalon.Tests.Hosting;

public sealed class AspNetCoreHostingTests
{
    private static readonly string[] MultiDocumentNames = ["v1", "v2"];

    [Fact]
    public async Task MapCephalonServesHostedReferenceDocsWhenEnabled()
    {
        var outputPath = await CreateHostedReferenceDocsAsync("Cephalon.Engine", "Cephalon.Agentics");

        try
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseTestServer();
            builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
            builder.Configuration[$"{ReferenceDocsHostingOptions.SectionName}:Enabled"] = "true";
            builder.Configuration[$"{ReferenceDocsHostingOptions.SectionName}:DirectoryPath"] = outputPath;
            builder.AddCephalon(engine =>
            {
                engine.AddModule(new PlatformTestModule());
                engine.AddModule(new DiscoveryTestModule());
            });

            await using var app = builder.Build();
            app.MapCephalon();

            await app.StartAsync();
            var client = app.GetTestClient();

            var surface = await client.GetFromJsonAsync<ReferenceDocsSurface>("/engine/reference-docs");
            var browseResponse = await client.GetAsync("/reference/browse.html");
            var browsePayload = await browseResponse.Content.ReadAsStringAsync();
            var memberIndexResponse = await client.GetAsync("/reference/members.md");
            var memberIndexPayload = await memberIndexResponse.Content.ReadAsStringAsync();
            var manifestPayload = await client.GetStringAsync("/reference/reference-manifest.json");

            Assert.NotNull(surface);
            Assert.True(surface.Enabled);
            Assert.True(surface.Available);
            Assert.Equal("/reference", surface.RoutePrefix);
            Assert.Equal("browse.html", surface.DefaultDocument);
            Assert.Equal("/reference/browse.html", surface.BrowserPath);
            Assert.Equal("/reference/members.md", surface.MemberIndexPath);

            Assert.True(browseResponse.IsSuccessStatusCode);
            Assert.Equal("text/html", browseResponse.Content.Headers.ContentType?.MediaType);
            Assert.Contains("Reference Browser", browsePayload, StringComparison.Ordinal);
            Assert.Contains("scope-filter", browsePayload, StringComparison.Ordinal);

            Assert.True(memberIndexResponse.IsSuccessStatusCode);
            Assert.Equal("text/markdown", memberIndexResponse.Content.Headers.ContentType?.MediaType);
            Assert.Contains("# Member Index", memberIndexPayload, StringComparison.Ordinal);
            Assert.Contains("EngineBuilder", memberIndexPayload, StringComparison.Ordinal);

            using var manifestDocument = JsonDocument.Parse(manifestPayload);
            Assert.Equal(2, manifestDocument.RootElement.GetProperty("SchemaVersion").GetInt32());
            Assert.True(manifestDocument.RootElement.GetProperty("Members").GetArrayLength() > 0);
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AddReferenceDocsHostingAllowsCodeLevelOverride()
    {
        var outputPath = await CreateHostedReferenceDocsAsync("Cephalon.Engine");

        try
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseTestServer();
            builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
            builder.AddCephalon(engine =>
            {
                engine.AddModule(new PlatformTestModule());
                engine.AddModule(new DiscoveryTestModule());
            });
            builder.AddReferenceDocsHosting(options =>
            {
                options.Enabled = true;
                options.DirectoryPath = outputPath;
                options.RoutePrefix = "/docs";
                options.DefaultDocument = "README.md";
            });

            await using var app = builder.Build();
            app.MapCephalon();

            await app.StartAsync();
            var client = app.GetTestClient();

            var surface = await client.GetFromJsonAsync<ReferenceDocsSurface>("/engine/reference-docs");
            var readmeResponse = await client.GetAsync("/docs/README.md");
            var readmePayload = await readmeResponse.Content.ReadAsStringAsync();

            Assert.NotNull(surface);
            Assert.True(surface.Enabled);
            Assert.True(surface.Available);
            Assert.Equal("/docs", surface.RoutePrefix);
            Assert.Equal("README.md", surface.DefaultDocument);
            Assert.Equal("/docs/README.md", surface.DefaultDocumentPath);
            Assert.Equal("/docs/browse.html", surface.BrowserPath);

            Assert.True(readmeResponse.IsSuccessStatusCode);
            Assert.Equal("text/markdown", readmeResponse.Content.Headers.ContentType?.MediaType);
            Assert.Contains("# Cephalon Reference Docs", readmePayload, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task MapCephalonFailsFastWhenReferenceDocsDirectoryIsMissing()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{ReferenceDocsHostingOptions.SectionName}:Enabled"] = "true";
        builder.Configuration[$"{ReferenceDocsHostingOptions.SectionName}:DirectoryPath"] = Path.Combine(
            Path.GetTempPath(),
            $"cephalon-reference-docs-missing-{Guid.NewGuid():N}");
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("Reference docs directory", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonExposesPhase12PatternTaxonomyDescriptors()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks()
            .AddCheck("cephalon.liveness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live", "engine"])
            .AddCheck("cephalon.readiness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["ready", "engine"]);
        builder.Services.AddSingleton<IRateLimitingRuntimeCatalog>(EmptyRateLimitingRuntimeCatalog.Instance);
        builder.Services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                patterns: ["StranglerFig", "BFF"]));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var appModel = await client.GetFromJsonAsync<AppProfile>("/engine/app-model");
        var patterns = await client.GetFromJsonAsync<PatternDescriptor[]>("/engine/patterns");

        Assert.NotNull(appModel);
        Assert.Contains(appModel.Patterns, pattern => pattern.Id == "strangler-fig");
        Assert.Contains(appModel.Patterns, pattern => pattern.Id == "backend-for-frontend");
        Assert.NotNull(patterns);
        Assert.Contains(patterns, pattern => pattern.Id == "strangler-fig");
        Assert.Contains(patterns, pattern => pattern.Id == "backend-for-frontend");
    }

    [Fact]
    public async Task MapCephalonExposesBackendForFrontendBindingsAndSnapshot()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:BackendForFrontend:Bindings:0:Id"] = "mobile-rest";
        builder.Configuration[$"{EngineSettings.SectionName}:BackendForFrontend:Bindings:0:ClientId"] = "mobile";
        builder.Configuration[$"{EngineSettings.SectionName}:BackendForFrontend:Bindings:0:SourceModuleId"] = "platform";
        builder.Configuration[$"{EngineSettings.SectionName}:BackendForFrontend:Bindings:0:DisplayName"] = "Mobile REST";
        builder.Configuration[$"{EngineSettings.SectionName}:BackendForFrontend:Bindings:0:Description"] = "Projects the mobile REST experience through the platform module.";
        builder.Configuration[$"{EngineSettings.SectionName}:BackendForFrontend:Bindings:0:TransportId"] = "rest-api";
        builder.Configuration[$"{EngineSettings.SectionName}:BackendForFrontend:Bindings:0:EntryPoint"] = "/api/mobile";
        builder.Configuration[$"{EngineSettings.SectionName}:BackendForFrontend:Bindings:0:BehaviorFilter:IncludedBehaviorIds:0"] = "tests.mobile.lookup";
        builder.Configuration[$"{EngineSettings.SectionName}:BackendForFrontend:Bindings:0:BehaviorFilter:IncludedTags:0"] = "mobile";
        builder.Configuration[$"{EngineSettings.SectionName}:BackendForFrontend:Bindings:0:Metadata:audience"] = "mobile";
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddModule(new BackendForFrontendHostingTestModule());
            engine.AddBackendForFrontendClientBinding(new BackendForFrontendClientBindingDescriptor(
                id: "storefront-rest",
                clientId: "storefront",
                sourceModuleId: "platform",
                displayName: "Storefront REST",
                description: "Projects the storefront REST surface through the platform module.",
                transportId: "rest-api",
                entryPoint: "/api/storefront",
                behaviorFilter: new BackendForFrontendBehaviorFilterDescriptor(
                    includedBehaviorIds: ["tests.storefront.lookup"],
                    includedTags: ["storefront"]),
                metadata: new Dictionary<string, string>
                {
                    ["audience"] = "public"
                }));
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var appModel = await client.GetFromJsonAsync<AppProfile>("/engine/app-model");
        var bindings = await client.GetFromJsonAsync<BackendForFrontendClientBindingDescriptor[]>("/engine/backend-for-frontend");
        var binding = await client.GetFromJsonAsync<BackendForFrontendClientBindingDescriptor>("/engine/backend-for-frontend/storefront-rest");
        var mobileBindings = await client.GetFromJsonAsync<BackendForFrontendClientBindingDescriptor[]>("/engine/backend-for-frontend/clients/mobile");
        var moduleBindings = await client.GetFromJsonAsync<BackendForFrontendClientBindingDescriptor[]>("/engine/backend-for-frontend/modules/backend-for-frontend-hosting-tests");
        var restBindings = await client.GetFromJsonAsync<BackendForFrontendClientBindingDescriptor[]>("/engine/backend-for-frontend/transports/rest-api");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(appModel);
        Assert.Contains(appModel.Patterns, pattern => pattern.Id == "backend-for-frontend");

        Assert.NotNull(bindings);
        Assert.Equal(3, bindings.Length);
        Assert.Contains(bindings, candidate => candidate.Id == "mobile-rest");
        Assert.Contains(bindings, candidate => candidate.Id == "storefront-rest");
        Assert.Contains(bindings, candidate => candidate.Id == "storefront-graphql");

        Assert.NotNull(binding);
        Assert.Equal("storefront", binding.ClientId);
        Assert.Equal("rest-api", binding.TransportId);
        Assert.Equal("/api/storefront", binding.EntryPoint);
        Assert.Contains("tests.storefront.lookup", binding.BehaviorFilter.IncludedBehaviorIds);
        Assert.Contains("storefront", binding.BehaviorFilter.IncludedTags);
        Assert.Equal("public", binding.Metadata["audience"]);

        Assert.NotNull(mobileBindings);
        var mobileBinding = Assert.Single(mobileBindings);
        Assert.Equal("mobile-rest", mobileBinding.Id);
        Assert.Contains("tests.mobile.lookup", mobileBinding.BehaviorFilter.IncludedBehaviorIds);

        Assert.NotNull(moduleBindings);
        var moduleBinding = Assert.Single(moduleBindings);
        Assert.Equal("storefront-graphql", moduleBinding.Id);
        Assert.Contains("discovery.greetings", moduleBinding.BehaviorFilter.IncludedCapabilityKeys);
        Assert.Contains("admin", moduleBinding.BehaviorFilter.ExcludedTags);

        Assert.NotNull(restBindings);
        Assert.Equal(2, restBindings.Length);
        Assert.Contains(restBindings, candidate => candidate.Id == "mobile-rest");
        Assert.Contains(restBindings, candidate => candidate.Id == "storefront-rest");

        Assert.NotNull(snapshot);
        Assert.Equal(3, snapshot.BackendForFrontendBindings.Count);
        Assert.Contains(snapshot.BackendForFrontendBindings, candidate => candidate.Id == "storefront-graphql");
    }

    [Fact]
    public async Task MapCephalonExposesStranglerFigRoutesAndResolution()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks()
            .AddCheck("cephalon.liveness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live", "engine"])
            .AddCheck("cephalon.readiness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["ready", "engine"]);
        builder.Services.AddSingleton<IRateLimitingRuntimeCatalog>(EmptyRateLimitingRuntimeCatalog.Instance);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "StranglerFig";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:DefaultTarget"] = "legacy";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:DefaultProgressState"] = "assessing";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:DefaultProgressPercent"] = "25";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:Routes:0:RouteId"] = "orders-modern";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:Routes:0:Target"] = "modern";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:Routes:0:ProgressState"] = "cutover";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:Routes:0:ProgressPercent"] = "90";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:Routes:0:Notes"] = "Ready for the final traffic shift.";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:Routes:1:RouteId"] = "reports-fallback";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:Routes:1:ProgressState"] = "validating";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:Routes:1:ProgressPercent"] = "40";
        builder.Services.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddStranglerFigRoute(new StranglerFigRouteDescriptor(
                id: "orders-modern",
                sourceModuleId: "platform",
                displayName: "Orders modernization",
                description: "Routes order workflows to the modern Cephalon boundary.",
                pathPrefix: "/checkout/orders",
                preferredTarget: StranglerFigTarget.Modern,
                legacyEndpoint: "legacy://orders",
                modernEndpoint: "modern://orders"));
            engine.AddStranglerFigRoute(new StranglerFigRouteDescriptor(
                id: "reports-fallback",
                sourceModuleId: "platform",
                displayName: "Reports fallback",
                description: "Keeps reporting traffic on the legacy boundary until the modern endpoint exists.",
                pathPrefix: "/reports",
                preferredTarget: StranglerFigTarget.Modern,
                legacyEndpoint: "legacy://reports"));
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var routes = await client.GetFromJsonAsync<StranglerFigRouteDescriptor[]>("/engine/strangler-fig");
        var route = await client.GetFromJsonAsync<StranglerFigRouteDescriptor>("/engine/strangler-fig/orders-modern");
        var runtimeRoutes = await client.GetFromJsonAsync<StranglerFigMigrationRuntimeDescriptor[]>("/engine/strangler-fig/runtime");
        var runtimeRoute = await client.GetFromJsonAsync<StranglerFigMigrationRuntimeDescriptor>("/engine/strangler-fig/runtime/orders-modern");
        var resolution = await client.GetFromJsonAsync<StranglerFigRouteResolution>("/engine/strangler-fig/resolve?path=/checkout/orders/42&method=GET");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(routes);
        Assert.Equal(2, routes.Length);
        Assert.Contains(routes, candidate => candidate.Id == "orders-modern");
        Assert.Contains(routes, candidate => candidate.Id == "reports-fallback");

        Assert.NotNull(route);
        Assert.Equal("/checkout/orders", route.PathPrefix);
        Assert.Equal("modern://orders", route.ModernEndpoint);

        Assert.NotNull(runtimeRoutes);
        Assert.Equal(2, runtimeRoutes.Length);
        Assert.Contains(runtimeRoutes, candidate =>
            candidate.RouteId == "orders-modern" &&
            candidate.RequestedTargetSource == "migration-route" &&
            candidate.ProgressState == "cutover" &&
            candidate.ProgressPercent == 90);
        Assert.Contains(runtimeRoutes, candidate =>
            candidate.RouteId == "reports-fallback" &&
            candidate.RequestedTargetSource == "migration-default" &&
            candidate.EffectiveTarget == StranglerFigTarget.Legacy &&
            candidate.ProgressState == "validating" &&
            candidate.ProgressPercent == 40);

        Assert.NotNull(runtimeRoute);
        Assert.Equal("orders-modern", runtimeRoute.RouteId);
        Assert.Equal(StranglerFigTarget.Modern, runtimeRoute.RequestedTarget);
        Assert.Equal(StranglerFigTarget.Modern, runtimeRoute.EffectiveTarget);
        Assert.Equal("migration-route", runtimeRoute.RequestedTargetSource);
        Assert.Equal("cutover", runtimeRoute.ProgressState);
        Assert.Equal(90, runtimeRoute.ProgressPercent);
        Assert.Equal("Ready for the final traffic shift.", runtimeRoute.RuntimeMetadata["note"]);

        Assert.NotNull(resolution);
        Assert.Equal("orders-modern", resolution.RouteId);
        Assert.Equal("/checkout/orders/42", resolution.RequestedPath);
        Assert.Equal(StranglerFigTarget.Modern, resolution.SelectedTarget);
        Assert.Equal("modern://orders", resolution.SelectedEndpoint);
        Assert.Equal("configured-target", resolution.ResolutionMode);
        Assert.Equal("modern", resolution.Metadata["migrationRequestedTarget"]);
        Assert.Equal("migration-route", resolution.Metadata["migrationRequestedTargetSource"]);
        Assert.Equal("cutover", resolution.Metadata["migrationProgressState"]);
        Assert.Equal("90", resolution.Metadata["migrationProgressPercent"]);
        Assert.Equal("Ready for the final traffic shift.", resolution.Metadata["migrationNote"]);

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.StranglerFigRoutes, candidate => candidate.Id == "orders-modern");
        Assert.Contains(snapshot.StranglerFigRoutes, candidate => candidate.Id == "reports-fallback");
        Assert.Contains(snapshot.StranglerFigRoutePolicies, candidate =>
            candidate.RouteId == "orders-modern" &&
            candidate.RequestedTargetSource == "migration-route" &&
            candidate.ProgressState == "cutover" &&
            candidate.ProgressPercent == 90);
        Assert.Contains(snapshot.StranglerFigRoutePolicies, candidate =>
            candidate.RouteId == "reports-fallback" &&
            candidate.RequestedTargetSource == "migration-default" &&
            candidate.EffectiveTarget == StranglerFigTarget.Legacy &&
            candidate.ProgressState == "validating" &&
            candidate.ProgressPercent == 40);
    }

    [Fact]
    public async Task MapCephalonExposesStranglerFigIngressRoutesAndSnapshot()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks()
            .AddCheck("cephalon.liveness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live", "engine"])
            .AddCheck("cephalon.readiness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["ready", "engine"]);
        builder.Services.AddSingleton<IRateLimitingRuntimeCatalog>(EmptyRateLimitingRuntimeCatalog.Instance);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "StranglerFig";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:DefaultTarget"] = "legacy";
        builder.Services.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddStranglerFigRoute(new StranglerFigRouteDescriptor(
                id: "status-pass-through",
                sourceModuleId: "platform",
                displayName: "Status pass-through",
                description: "Keeps the selected route on the same rooted path.",
                pathPrefix: "/legacy/status",
                preferredTarget: StranglerFigTarget.Legacy,
                legacyEndpoint: "/legacy/status"));
            engine.AddStranglerFigRoute(new StranglerFigRouteDescriptor(
                id: "reports-rewrite",
                sourceModuleId: "platform",
                displayName: "Reports rewrite",
                description: "Rewrites report traffic to a different rooted local path.",
                pathPrefix: "/reports",
                preferredTarget: StranglerFigTarget.Legacy,
                legacyEndpoint: "/legacy/reports"));
            engine.AddStranglerFigRoute(new StranglerFigRouteDescriptor(
                id: "orders-proxy",
                sourceModuleId: "platform",
                displayName: "Orders proxy",
                description: "Proxies order traffic to an absolute legacy URI.",
                pathPrefix: "/checkout/orders",
                preferredTarget: StranglerFigTarget.Legacy,
                legacyEndpoint: "https://legacy.example.com/orders"));
            engine.AddStranglerFigRoute(new StranglerFigRouteDescriptor(
                id: "events-opaque",
                sourceModuleId: "platform",
                displayName: "Events opaque",
                description: "Keeps one opaque boundary visible for operators.",
                pathPrefix: "/events",
                preferredTarget: StranglerFigTarget.Legacy,
                legacyEndpoint: "legacy://events"));
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var routes = await client.GetFromJsonAsync<StranglerFigIngressRuntimeDescriptor[]>("/engine/strangler-fig/ingress");
        var route = await client.GetFromJsonAsync<StranglerFigIngressRuntimeDescriptor>("/engine/strangler-fig/ingress/reports-rewrite");
        var moduleRoutes = await client.GetFromJsonAsync<StranglerFigIngressRuntimeDescriptor[]>("/engine/strangler-fig/ingress/modules/platform");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(routes);
        Assert.Equal(4, routes.Length);
        Assert.Contains(routes, candidate =>
            candidate.RouteId == "status-pass-through" &&
            candidate.IngressMode == "pass-through");
        Assert.Contains(routes, candidate =>
            candidate.RouteId == "orders-proxy" &&
            candidate.IngressMode == "proxy-absolute-uri" &&
            candidate.TargetUri == "https://legacy.example.com/orders");
        Assert.Contains(routes, candidate =>
            candidate.RouteId == "events-opaque" &&
            candidate.IngressMode == "opaque-endpoint" &&
            candidate.CanMaterialize == false);

        Assert.NotNull(route);
        Assert.Equal("rewrite-local-path", route.IngressMode);
        Assert.True(route.CanMaterialize);
        Assert.Equal("/legacy/reports", route.TargetPathPrefix);
        Assert.Null(route.TargetUri);

        Assert.NotNull(moduleRoutes);
        Assert.Equal(4, moduleRoutes.Length);
        Assert.Contains(moduleRoutes, candidate => candidate.RouteId == "events-opaque");

        Assert.NotNull(snapshot);
        Assert.Equal(4, snapshot.StranglerFigIngressRoutes.Count);
        Assert.Contains(snapshot.StranglerFigIngressRoutes, candidate =>
            candidate.RouteId == "orders-proxy" &&
            candidate.SelectedEndpointKind == "absolute-uri");
        Assert.Contains(snapshot.StranglerFigIngressRoutes, candidate =>
            candidate.RouteId == "events-opaque" &&
            candidate.IngressMode == "opaque-endpoint");
    }

    [Fact]
    public async Task MapCephalonRewritesLocalStranglerFigCutoverTargets()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        ConfigureStranglerFigCutoverHost(
            builder,
            legacyEndpoint: "/legacy/orders",
            modernEndpoint: "/checkout/orders",
            absoluteEndpointMode: "Redirect");

        await using var app = builder.Build();
        app.MapCephalon();
        app.MapPost("/legacy/orders/{id}", (string id, HttpContext httpContext) => Results.Json(new
        {
            source = "legacy",
            id,
            path = httpContext.Request.Path.Value,
            query = httpContext.Request.QueryString.Value
        }));
        app.MapPost("/checkout/orders/{id}", () => Results.Json(new
        {
            source = "modern"
        }));

        await app.StartAsync();
        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/checkout/orders/42?expand=lines")
        {
            Content = JsonContent.Create(new
            {
                quantity = 2
            })
        };

        var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();
        using var payloadDocument = JsonDocument.Parse(payload);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("legacy", payloadDocument.RootElement.GetProperty("source").GetString());
        Assert.Equal("42", payloadDocument.RootElement.GetProperty("id").GetString());
        Assert.Equal("/legacy/orders/42", payloadDocument.RootElement.GetProperty("path").GetString());
        Assert.Equal("?expand=lines", payloadDocument.RootElement.GetProperty("query").GetString());
        Assert.Equal("orders-cutover", response.Headers.GetValues("X-Cephalon-StranglerFig-RouteId").Single());
        Assert.Equal("rewrite-local-path", response.Headers.GetValues("X-Cephalon-StranglerFig-Handling").Single());

        var cutoverPayload = await client.GetStringAsync("/engine/strangler-fig/cutover/orders-cutover");
        using var cutoverDocument = JsonDocument.Parse(cutoverPayload);
        Assert.Equal("rewrite-local-path", cutoverDocument.RootElement.GetProperty("handlingMode").GetString());
        Assert.Equal("local-path", cutoverDocument.RootElement.GetProperty("selectedEndpointKind").GetString());

        var decisionPayload = await client.GetStringAsync("/engine/strangler-fig/cutover/resolve?path=%2Fcheckout%2Forders%2F42&method=POST&query=expand%3Dlines");
        using var decisionDocument = JsonDocument.Parse(decisionPayload);
        Assert.Equal("rewrite-local-path", decisionDocument.RootElement.GetProperty("handlingMode").GetString());
        Assert.Equal("/legacy/orders/42", decisionDocument.RootElement.GetProperty("destinationPath").GetString());
        Assert.Equal("?expand=lines", decisionDocument.RootElement.GetProperty("destinationQuery").GetString());
    }

    [Fact]
    public async Task MapCephalonRedirectsAbsoluteStranglerFigCutoverTargets()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        ConfigureStranglerFigCutoverHost(
            builder,
            legacyEndpoint: "https://legacy.example.com/orders",
            modernEndpoint: "/checkout/orders",
            absoluteEndpointMode: "Redirect");

        await using var app = builder.Build();
        app.MapCephalon();
        app.MapGet("/checkout/orders/{id}", () => Results.Json(new
        {
            source = "modern"
        }));

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/checkout/orders/42?expand=lines");

        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.Equal(
            "https://legacy.example.com/orders/42?expand=lines",
            response.Headers.Location?.AbsoluteUri);
        Assert.Equal("redirect-absolute-uri", response.Headers.GetValues("X-Cephalon-StranglerFig-Handling").Single());

        var decisionPayload = await client.GetStringAsync("/engine/strangler-fig/cutover/resolve?path=%2Fcheckout%2Forders%2F42&method=GET&query=expand%3Dlines");
        using var decisionDocument = JsonDocument.Parse(decisionPayload);
        Assert.Equal("redirect-absolute-uri", decisionDocument.RootElement.GetProperty("handlingMode").GetString());
        Assert.Equal(
            "https://legacy.example.com/orders/42?expand=lines",
            decisionDocument.RootElement.GetProperty("destinationUri").GetString());
    }

    [Fact]
    public async Task MapCephalonProxiesAbsoluteStranglerFigCutoverTargets()
    {
        var proxyHandler = new CapturingProxyMessageHandler();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        ConfigureStranglerFigCutoverHost(
            builder,
            legacyEndpoint: "https://legacy.example.com/orders",
            modernEndpoint: "/checkout/orders",
            absoluteEndpointMode: "Proxy");
        builder.Services.AddHttpClient("cephalon-strangler-fig-proxy")
            .ConfigurePrimaryHttpMessageHandler(() => proxyHandler);

        await using var app = builder.Build();
        app.MapCephalon();
        app.MapPost("/checkout/orders/{id}", () => Results.Json(new
        {
            source = "modern"
        }));

        await app.StartAsync();
        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/checkout/orders/42?expand=lines")
        {
            Content = JsonContent.Create(new
            {
                quantity = 2
            })
        };

        var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();
        using var payloadDocument = JsonDocument.Parse(payload);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("proxy", payloadDocument.RootElement.GetProperty("source").GetString());
        Assert.Equal(
            "https://legacy.example.com/orders/42?expand=lines",
            payloadDocument.RootElement.GetProperty("destinationUri").GetString());
        Assert.Equal("proxy-absolute-uri", response.Headers.GetValues("X-Cephalon-StranglerFig-Handling").Single());
        Assert.NotNull(proxyHandler.LastRequestUri);
        Assert.Equal("https://legacy.example.com/orders/42?expand=lines", proxyHandler.LastRequestUri!.AbsoluteUri);
        Assert.Equal(HttpMethod.Post, proxyHandler.LastMethod);
        Assert.Equal("/checkout/orders/42", proxyHandler.LastForwardedPath);
        Assert.Equal("POST", proxyHandler.LastForwardedMethod);
        Assert.Equal("application/json; charset=utf-8", proxyHandler.LastContentType);
        Assert.Contains("\"quantity\":2", proxyHandler.LastBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonRejectsUnsupportedStranglerFigCutoverTargets()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        ConfigureStranglerFigCutoverHost(
            builder,
            legacyEndpoint: "legacy://orders",
            modernEndpoint: "/checkout/orders",
            absoluteEndpointMode: "Redirect");

        await using var app = builder.Build();
        app.MapCephalon();
        app.MapGet("/checkout/orders/{id}", () => Results.Json(new
        {
            source = "modern"
        }));

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/checkout/orders/42");
        var payload = await response.Content.ReadAsStringAsync();
        using var payloadDocument = JsonDocument.Parse(payload);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("orders-cutover", payloadDocument.RootElement.GetProperty("routeId").GetString());
        Assert.Equal("legacy://orders", payloadDocument.RootElement.GetProperty("selectedEndpoint").GetString());
        Assert.Equal("unsupported-endpoint", payloadDocument.RootElement.GetProperty("handlingMode").GetString());

        var cutoverPayload = await client.GetStringAsync("/engine/strangler-fig/cutover/orders-cutover");
        using var cutoverDocument = JsonDocument.Parse(cutoverPayload);
        Assert.Equal("unsupported-endpoint", cutoverDocument.RootElement.GetProperty("handlingMode").GetString());
        Assert.Equal("unsupported", cutoverDocument.RootElement.GetProperty("selectedEndpointKind").GetString());
    }

    private sealed class EmptyRateLimitingRuntimeCatalog : IRateLimitingRuntimeCatalog
    {
        public static EmptyRateLimitingRuntimeCatalog Instance { get; } = new();

        public IReadOnlyList<RateLimitingRuntimeDescriptor> Policies => [];

        public RateLimitingRuntimeDescriptor? GetById(string policyId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(policyId);
            return null;
        }

        public IReadOnlyList<RateLimitingRuntimeDescriptor> GetByTransportId(string transportId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(transportId);
            return [];
        }
    }

    private static void ConfigureStranglerFigCutoverHost(
        WebApplicationBuilder builder,
        string legacyEndpoint,
        string modernEndpoint,
        string absoluteEndpointMode)
    {
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "StranglerFig";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:DefaultTarget"] = "legacy";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:DefaultProgressState"] = "cutover";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:DefaultProgressPercent"] = "85";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:AspNetCore:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Migration:StranglerFig:AspNetCore:AbsoluteEndpointMode"] = absoluteEndpointMode;
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
            engine.AddStranglerFigRoute(new StranglerFigRouteDescriptor(
                id: "orders-cutover",
                sourceModuleId: "platform",
                displayName: "Orders cutover",
                description: "Routes order requests through the strangler-fig cutover surface.",
                pathPrefix: "/checkout/orders",
                preferredTarget: StranglerFigTarget.Modern,
                legacyEndpoint: legacyEndpoint,
                modernEndpoint: modernEndpoint));
        });
    }

    private sealed class CapturingProxyMessageHandler : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        public HttpMethod? LastMethod { get; private set; }

        public string? LastForwardedPath { get; private set; }

        public string? LastForwardedMethod { get; private set; }

        public string? LastContentType { get; private set; }

        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastMethod = request.Method;
            LastForwardedPath = request.Headers.TryGetValues("X-Forwarded-Path", out var forwardedPaths)
                ? forwardedPaths.SingleOrDefault()
                : null;
            LastForwardedMethod = request.Headers.TryGetValues("X-Forwarded-Method", out var forwardedMethods)
                ? forwardedMethods.SingleOrDefault()
                : null;
            LastContentType = request.Content?.Headers.ContentType?.ToString();
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = JsonContent.Create(new
                {
                    source = "proxy",
                    destinationUri = request.RequestUri?.AbsoluteUri
                })
            };
        }
    }

    [Fact]
    public async Task MapCephalonExposesRuntimeAndModuleRoutes()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "StrategyPattern";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:1"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:2"] = "Outbox";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:3"] = "OnionArchitecture";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:4"] = "AntiCorruptionLayer";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:1"] = "JsonRpc";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:2"] = "Grpc";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:3"] = "GraphQL";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:4"] = "ServerSentEvents";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:5"] = "WebSocket";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "AgenticWorkloads";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:1"] = "EventDrivenIntegration";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:2"] = "RealtimeExperience";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:3"] = "EdgeNativeDelivery";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Capabilities:platform.clock"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Data:Provider"] = "EntityFramework";
        builder.Configuration[$"{EngineSettings.SectionName}:Data:ReadWriteSplit"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Data:Outbox:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Retry:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Retry:MaxAttempts"] = "3";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Retry:BaseDelayMilliseconds"] = "200";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Retry:MaxDelayMilliseconds"] = "5000";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Retry:Backoff"] = "Exponential";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Retry:UseJitter"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:FailureRatio"] = "0.5";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:MinimumThroughput"] = "20";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:SamplingDurationSeconds"] = "30";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:BreakDurationSeconds"] = "15";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:TotalTimeoutSeconds"] = "10";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:AttemptTimeoutSeconds"] = "3";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:MaxConcurrentExecutions"] = "64";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:MaxQueuedActions"] = "32";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Algorithm"] = "SlidingWindow";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:PermitLimit"] = "100";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:QueueLimit"] = "10";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:WindowSeconds"] = "60";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:SegmentsPerWindow"] = "4";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:Runtime:EnableRetryOnFailure"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:Runtime:MaxRetryCount"] = "5";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:Write:Provider"] = "PostgreSql";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:Write:ConnectionStringName"] = "WriteDb";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:Read:Provider"] = "PostgreSql";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:Read:ConnectionStringName"] = "ReadDb";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:Outbox:Provider"] = "PostgreSql";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:Outbox:ConnectionStringName"] = "WriteDb";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:Outbox:Schema"] = "outbox01";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:History:Provider"] = "PostgreSql";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:History:ConnectionStringName"] = "HistoryDb";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:Migrations:ApplyOnStartup"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:Migrations:Targets:0"] = "write";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:Migrations:Targets:1"] = "outbox";
        builder.Configuration[$"{EngineSettings.SectionName}:Databases:Migrations:Targets:2"] = "history";
        builder.Configuration["OpenApi:Title"] = "Cephalon Test REST API";
        builder.Configuration["OpenApi:SecuritySchemes:0:Name"] = "Bearer";
        builder.Configuration["OpenApi:SecuritySchemes:0:Type"] = "Http";
        builder.Configuration["OpenApi:SecuritySchemes:0:Scheme"] = "bearer";
        builder.Configuration["OpenApi:SecuritySchemes:0:BearerFormat"] = "JWT";
        builder.Configuration["OpenApi:SecuritySchemes:0:In"] = "Header";
        builder.Configuration["OpenApi:SecuritySchemes:0:Description"] = "Bearer token authentication.";
        builder.AddGraphQLTransport();
        builder.AddGrpcTransport();
        builder.AddJsonRpcTransport();
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new DiscoveryTestModule());
            cephalon.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "orders",
                    displayName: "Orders",
                    description: "Integration events for the order domain."));
                options.Subscriptions.Add(new EventSubscriptionDescriptor(
                    id: "orders-projector",
                    displayName: "Orders Projector",
                    description: "Projects order integration events into the runtime test read model.",
                    channelId: "orders",
                    handlerId: "orders-projector",
                    deliveryMode: "background-service",
                    tags: ["orders", "projection"]));
            });
            cephalon.AddEdge(options =>
            {
                options.Nodes.Add(new EdgeNodeDescriptor(
                    id: "storefront-edge",
                    displayName: "Storefront Edge",
                    description: "Regional node serving intermittently connected storefront experiences."));
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var manifest = await client.GetFromJsonAsync<RuntimeManifest>("/engine");
        var appModel = await client.GetFromJsonAsync<AppProfile>("/engine/app-model");
        var resilience = await client.GetFromJsonAsync<ResilienceSelection>("/engine/resilience");
        var rateLimitingPolicies = await client.GetFromJsonAsync<RateLimitingRuntimeDescriptor[]>("/engine/rate-limiting");
        var databases = await client.GetFromJsonAsync<DatabaseTopologySelection>("/engine/databases");
        var databaseRoles = await client.GetFromJsonAsync<DatabaseRoleDescriptor[]>("/engine/database-roles");
        var databaseMigrations = await client.GetFromJsonAsync<DatabaseMigrationDescriptor[]>("/engine/database-migrations");
        var scaffold = await client.GetFromJsonAsync<ScaffoldPlan>("/engine/scaffold");
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var packages = await client.GetFromJsonAsync<PackageManifest[]>("/engine/packages");
        var patterns = await client.GetFromJsonAsync<PatternDescriptor[]>("/engine/patterns");
        var technologies = await client.GetFromJsonAsync<TechnologyDescriptor[]>("/engine/technologies");
        var technologyCatalog = await client.GetFromJsonAsync<TechnologyDescriptor[]>("/engine/technology-catalog");
        var transports = await client.GetFromJsonAsync<TransportDescriptor[]>("/engine/transports");
        var dependencies = await client.GetFromJsonAsync<DependencyHealthReport[]>("/engine/dependencies");
        var trustPolicy = await client.GetFromJsonAsync<TrustSnapshot>("/engine/trust-policy");
        var failurePolicy = await client.GetFromJsonAsync<FailurePolicy>("/engine/failure-policy");
        var status = await client.GetFromJsonAsync<RuntimeStatusSnapshot>("/engine/status");
        var diagnosticsResponse = await client.GetAsync("/engine/diagnostics");
        var diagnosticsPayload = await diagnosticsResponse.Content.ReadAsStringAsync();
        var healthResponse = await client.GetAsync("/health");
        var healthPayload = await healthResponse.Content.ReadAsStringAsync();
        var livenessResponse = await client.GetAsync("/health/live");
        var livenessPayload = await livenessResponse.Content.ReadAsStringAsync();
        var readinessResponse = await client.GetAsync("/health/ready");
        var readinessPayload = await readinessResponse.Content.ReadAsStringAsync();
        var optionsPayload = await client.GetStringAsync("/engine/options");
        var openApiResponse = await client.GetAsync("/openapi/v1.json");
        var openApiPayload = await openApiResponse.Content.ReadAsStringAsync();
        var scalarConfigResponse = await client.GetAsync("/scalar/openapi-toggle.js");
        var scalarConfigPayload = await scalarConfigResponse.Content.ReadAsStringAsync();
        var scalarFaviconResponse = await client.GetAsync("/scalar/assets/favicon.svg");
        var scalarFaviconPayload = await scalarFaviconResponse.Content.ReadAsStringAsync();
        var scalarResponse = await client.GetAsync("/scalar/v1");
        var scalarPayload = await scalarResponse.Content.ReadAsStringAsync();
        var greeting = await client.GetFromJsonAsync<GreetingEnvelope>("/api/discovery/hello/Codex");
        var time = await client.GetFromJsonAsync<PlatformTimeEnvelope>("/api/platform/time");
        var graphQlResponse = await client.PostAsJsonAsync("/graphql", new
        {
            query = "query ($name: String) { hello(name: $name) { message generatedAtUtc traits } }",
            variables = new
            {
                name = "Codex"
            }
        });
        var graphQlPayload = await graphQlResponse.Content.ReadAsStringAsync();
        var graphQlSdlResponse = await client.GetAsync("/graphql/schema");
        var graphQlSdlPayload = await graphQlSdlResponse.Content.ReadAsStringAsync();
        var rpcResponse = await client.PostAsJsonAsync("/json-rpc/discovery", new
        {
            jsonRpc = "2.0",
            method = "discovery.hello",
            @params = new Dictionary<string, string?> { ["name"] = "Codex" },
            id = "req-1"
        });
        var rpcPayload = await rpcResponse.Content.ReadAsStringAsync();
        var sseResponse = await client.GetAsync("/sse/discovery/principles");
        var ssePayload = await sseResponse.Content.ReadAsStringAsync();
        var grpcHandler = new GrpcSubdirectoryHandler(app.GetTestServer().CreateHandler(), "/grpc");
        using var grpcHttpClient = new HttpClient(grpcHandler)
        {
            BaseAddress = new Uri("http://localhost")
        };
        grpcHttpClient.DefaultRequestVersion = HttpVersion.Version20;
        grpcHttpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpClient = grpcHttpClient
        });
        var grpcClient = new DiscoveryService.DiscoveryServiceClient(channel);
        var grpcReply = await grpcClient.SayHelloAsync(new HelloRequest
        {
            Name = "Codex"
        });
        using var grpcPrinciplesCall = grpcClient.StreamPrinciples(new PrinciplesRequest());
        var grpcPrinciples = new List<string>();
        while (await grpcPrinciplesCall.ResponseStream.MoveNext(CancellationToken.None))
        {
            grpcPrinciples.Add(grpcPrinciplesCall.ResponseStream.Current.Principle);
        }

        using var grpcExchangeCall = grpcClient.ExchangeGreetings();
        await grpcExchangeCall.RequestStream.WriteAsync(new HelloRequest { Name = "Alpha" });
        await grpcExchangeCall.RequestStream.WriteAsync(new HelloRequest { Name = "Beta" });
        await grpcExchangeCall.RequestStream.CompleteAsync();
        var grpcMessages = new List<string>();
        while (await grpcExchangeCall.ResponseStream.MoveNext(CancellationToken.None))
        {
            grpcMessages.Add(grpcExchangeCall.ResponseStream.Current.Message);
        }

        var webSocketClient = app.GetTestServer().CreateWebSocketClient();
        using var socket = await webSocketClient.ConnectAsync(new Uri("ws://localhost/ws/discovery"), CancellationToken.None);
        var buffer = new byte[4096];
        var receive = await socket.ReceiveAsync(buffer, CancellationToken.None);
        var webSocketPayload = Encoding.UTF8.GetString(buffer, 0, receive.Count);

        Assert.NotNull(manifest);
        Assert.Equal("2.0", manifest.ManifestVersion);
        Assert.False(string.IsNullOrWhiteSpace(manifest.EngineVersion));
        Assert.Equal(4, manifest.Modules.Count);
        Assert.Equal("modular-vertical-slice", manifest.AppProfile.BlueprintId);

        Assert.NotNull(appModel);
        Assert.Equal("modular-vertical-slice", appModel.BlueprintId);
        Assert.NotNull(appModel.Scaffold);
        Assert.Equal("modular-vertical-slice", appModel.Scaffold.Id);
        Assert.True(appModel.Resilience.Retry.Enabled);
        Assert.Equal(3, appModel.Resilience.Retry.MaxAttempts);
        Assert.Equal("Exponential", appModel.Resilience.Retry.Backoff);
        Assert.Equal(10, appModel.Resilience.Timeout.TotalTimeoutSeconds);
        Assert.Equal(3, appModel.Resilience.Timeout.AttemptTimeoutSeconds);
        Assert.Equal(0.5m, appModel.Resilience.CircuitBreaker.FailureRatio);
        Assert.Equal(64, appModel.Resilience.Bulkhead.MaxConcurrentExecutions);
        Assert.Equal(100, appModel.Resilience.RateLimiting.PermitLimit);
        Assert.Equal("WriteDb", appModel.Databases.Write.ConnectionStringName);
        Assert.Equal("ReadDb", appModel.Databases.Read.ConnectionStringName);
        Assert.Equal("outbox01", appModel.Databases.Outbox.Schema);
        Assert.Equal(["history", "outbox", "write"], appModel.Databases.Migrations.Targets);

        Assert.NotNull(resilience);
        Assert.True(resilience.Retry.Enabled);
        Assert.Equal(3, resilience.Retry.MaxAttempts);
        Assert.Equal("Exponential", resilience.Retry.Backoff);
        Assert.Equal(200, resilience.Retry.BaseDelayMilliseconds);
        Assert.Equal(5000, resilience.Retry.MaxDelayMilliseconds);
        Assert.True(resilience.Retry.UseJitter);
        Assert.True(resilience.Timeout.Enabled);
        Assert.Equal(10, resilience.Timeout.TotalTimeoutSeconds);
        Assert.Equal(3, resilience.Timeout.AttemptTimeoutSeconds);
        Assert.True(resilience.CircuitBreaker.Enabled);
        Assert.Equal(0.5m, resilience.CircuitBreaker.FailureRatio);
        Assert.Equal(20, resilience.CircuitBreaker.MinimumThroughput);
        Assert.Equal(30, resilience.CircuitBreaker.SamplingDurationSeconds);
        Assert.Equal(15, resilience.CircuitBreaker.BreakDurationSeconds);
        Assert.True(resilience.Bulkhead.Enabled);
        Assert.Equal(64, resilience.Bulkhead.MaxConcurrentExecutions);
        Assert.Equal(32, resilience.Bulkhead.MaxQueuedActions);
        Assert.True(resilience.RateLimiting.Enabled);
        Assert.Equal("SlidingWindow", resilience.RateLimiting.Algorithm);
        Assert.Equal(100, resilience.RateLimiting.PermitLimit);
        Assert.Equal(10, resilience.RateLimiting.QueueLimit);
        Assert.Equal(60, resilience.RateLimiting.WindowSeconds);
        Assert.Equal(4, resilience.RateLimiting.SegmentsPerWindow);
        Assert.NotNull(rateLimitingPolicies);
        var rateLimitingPolicy = Assert.Single(rateLimitingPolicies);
        Assert.Equal("cephalon-public-http", rateLimitingPolicy.Id);
        Assert.Equal("aspnetcore-endpoint-policy", rateLimitingPolicy.ExecutionMode);
        Assert.Equal("public-http-endpoints", rateLimitingPolicy.Scope);
        Assert.Equal(429, rateLimitingPolicy.RejectionStatusCode);
        Assert.Contains("graphql", rateLimitingPolicy.TransportIds);
        Assert.Contains("grpc", rateLimitingPolicy.TransportIds);
        Assert.Contains("json-rpc", rateLimitingPolicy.TransportIds);
        Assert.Contains("rest-api", rateLimitingPolicy.TransportIds);
        Assert.Contains("/engine", rateLimitingPolicy.ExcludedPathPrefixes);
        Assert.Contains("/health", rateLimitingPolicy.ExcludedPathPrefixes);
        Assert.Contains("/openapi", rateLimitingPolicy.ExcludedPathPrefixes);
        Assert.Contains("/scalar", rateLimitingPolicy.ExcludedPathPrefixes);
        Assert.True(rateLimitingPolicy.Requested.Enabled);
        Assert.True(rateLimitingPolicy.Effective.Enabled);
        Assert.Equal("SlidingWindow", rateLimitingPolicy.Effective.Algorithm);
        Assert.Equal("subject-or-tenant-or-ip", rateLimitingPolicy.Metadata["partitionStrategy"]);

        Assert.NotNull(databases);
        Assert.Equal("PostgreSql", databases.Write.Provider);
        Assert.Equal("WriteDb", databases.Write.ConnectionStringName);
        Assert.True(databases.Runtime.EnableRetryOnFailure);
        Assert.Equal(5, databases.Runtime.MaxRetryCount);
        Assert.Equal("HistoryDb", databases.History.ConnectionStringName);
        Assert.NotNull(databaseRoles);
        Assert.Equal(4, databaseRoles.Length);
        Assert.Contains(databaseRoles, role => role.Id == "write" && role.ResolvedRoleId == "write");
        Assert.Contains(databaseRoles, role => role.Id == "read" && role.ResolvedRoleId == "read");
        Assert.Contains(databaseRoles, role => role.Id == "outbox" && role.ResolvedRoleId == "outbox");
        Assert.Contains(databaseRoles, role => role.Id == "history" && role.ResolvedRoleId == "history");
        Assert.NotNull(databaseMigrations);
        Assert.Empty(databaseMigrations);

        Assert.NotNull(scaffold);
        Assert.Contains(scaffold.Projects, project =>
            project.Id == "host" &&
            project.Packages.Contains("Cephalon.Agentics", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Eventing", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Edge", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.GraphQL", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.JsonRpc", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.Grpc", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(scaffold.Folders, folder =>
            folder.ProjectId == "module" &&
            folder.PathTemplate == "Features/{FeatureName}/Endpoints");

        Assert.NotNull(patterns);
        Assert.Contains(patterns, pattern => pattern.Id == "strategy-pattern");
        Assert.Contains(patterns, pattern => pattern.Id == "onion-architecture");
        Assert.Contains(patterns, pattern => pattern.Id == "anti-corruption-layer");
        Assert.DoesNotContain(patterns, pattern => pattern.Id == "strangler-fig");
        Assert.DoesNotContain(patterns, pattern => pattern.Id == "backend-for-frontend");
        Assert.NotNull(technologies);
        Assert.Contains(technologies, technology => technology.Id == "agentic-workloads");
        Assert.Contains(technologies, technology => technology.Id == "event-driven-integration");
        Assert.Contains(technologies, technology => technology.Id == "realtime-experience");
        Assert.Contains(technologies, technology => technology.Id == "edge-native-delivery");
        Assert.Contains(appModel.Technologies, technology => technology.Id == "agentic-workloads");
        Assert.NotNull(technologyCatalog);
        Assert.Contains(technologyCatalog, technology => technology.Id == "agentic-workloads");
        Assert.Contains(technologyCatalog, technology => technology.Id == "event-driven-integration");
        Assert.Contains(technologyCatalog, technology => technology.Id == "knowledge-retrieval");
        Assert.Contains(technologyCatalog, technology => technology.Id == "edge-native-delivery");

        Assert.NotNull(packages);
        Assert.Empty(packages);
        Assert.NotNull(trustPolicy);
        Assert.Empty(trustPolicy.Packages);

        Assert.NotNull(capabilities);
        Assert.DoesNotContain(capabilities, capability => capability.Key == "platform.clock");
        Assert.Contains(capabilities, capability =>
            capability.Key == "discovery.greetings" &&
            capability.SourceModuleId == "discovery");
        Assert.DoesNotContain(capabilities, capability => capability.Key == "eventing.publish");
        Assert.Contains(capabilities, capability => capability.Key == "eventing.channels");
        Assert.Contains(capabilities, capability => capability.Key == "eventing.subscriptions");
        Assert.Contains(capabilities, capability => capability.Key == "edge.offline");
        Assert.Contains(capabilities, capability => capability.Key == "edge.nodes");

        Assert.NotNull(transports);
        Assert.Contains(transports, transport => transport.Id == "rest-api");
        Assert.Contains(transports, transport => transport.Id == "json-rpc");
        Assert.Contains(transports, transport => transport.Id == "grpc");
        Assert.Contains(transports, transport => transport.Id == "graphql");
        Assert.Contains(transports, transport => transport.Id == "server-sent-events");
        Assert.Contains(transports, transport => transport.Id == "websocket");

        Assert.NotNull(dependencies);
        Assert.Empty(dependencies);

        Assert.True(openApiResponse.IsSuccessStatusCode);
        using var openApiDocument = JsonDocument.Parse(openApiPayload);
        Assert.Equal("Cephalon Test REST API", openApiDocument.RootElement.GetProperty("info").GetProperty("title").GetString());
        var paths = openApiDocument.RootElement.GetProperty("paths");
        var components = openApiDocument.RootElement.GetProperty("components");
        var securitySchemes = components.GetProperty("securitySchemes");
        var schemas = components.GetProperty("schemas");
        Assert.True(paths.TryGetProperty("/api/discovery/hello/{name}", out _));
        Assert.True(paths.TryGetProperty("/api/platform/time", out _));
        Assert.False(paths.TryGetProperty("/json-rpc/discovery", out _));
        Assert.False(paths.TryGetProperty("/sse/discovery/principles", out _));
        Assert.False(paths.TryGetProperty("/engine", out _));
        Assert.True(securitySchemes.TryGetProperty("Bearer", out var bearerScheme));
        Assert.Equal("http", bearerScheme.GetProperty("type").GetString());
        Assert.Equal("bearer", bearerScheme.GetProperty("scheme").GetString());

        var greetingSchemas = schemas.EnumerateObject()
            .Where(property => property.Name.Contains("GreetingEnvelope", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(greetingSchemas);

        var describedGreetingSchema = greetingSchemas
            .FirstOrDefault(property => property.Value.TryGetProperty("description", out _));
        if (greetingSchemas.Any(property => property.Value.TryGetProperty("description", out _)))
        {
            Assert.Contains(
                "Discovery greeting payload returned by the REST surface.",
                describedGreetingSchema.Value.GetProperty("description").GetString(),
                StringComparison.Ordinal);
        }

        Assert.True(scalarConfigResponse.IsSuccessStatusCode);
        Assert.Equal("application/javascript", scalarConfigResponse.Content.Headers.ContentType?.MediaType);
        Assert.Contains("export default", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("replaceState", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("configuredScalarRoutePrefix", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("configuredDocumentNames", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("configuredDefaultDocumentName", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("scalarRoutePrefix", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("encodeURIComponent(documentName)", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("hashchange", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("hashSectionRoots", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("cephalon-scalar-document-selector", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("api-reference-toolbar", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("findHeaderHost", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("isVersionDocumentName", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("hashCarriesKnownDocument", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("no-store", scalarConfigResponse.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);

        Assert.True(scalarFaviconResponse.IsSuccessStatusCode);
        Assert.Equal("image/svg+xml", scalarFaviconResponse.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<svg", scalarFaviconPayload, StringComparison.Ordinal);
        Assert.Contains("no-store", scalarFaviconResponse.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);

        Assert.True(scalarResponse.IsSuccessStatusCode, $"{(int)scalarResponse.StatusCode} {scalarResponse.StatusCode} {scalarPayload}");
        Assert.Equal("text/html", scalarResponse.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Scalar", scalarPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/scalar/openapi-toggle.js?v=", scalarPayload, StringComparison.Ordinal);
        Assert.Contains("/scalar/assets/favicon.svg?v=", scalarPayload, StringComparison.Ordinal);
        Assert.Contains("no-store", scalarResponse.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);

        using var optionsDocument = JsonDocument.Parse(optionsPayload);
        Assert.True(optionsDocument.RootElement.GetProperty("hasValues").GetBoolean());
        Assert.False(optionsDocument.RootElement.GetProperty("capabilities").GetProperty("platform.clock").GetBoolean());

        Assert.NotNull(status);
        Assert.Equal(RuntimeStatus.Started, status.Status);
        Assert.NotNull(status.InitializedAtUtc);
        Assert.NotNull(status.StartedAtUtc);
        Assert.Equal(0, status.RestartCount);
        Assert.Null(status.LastFailure);

        Assert.NotNull(failurePolicy);
        Assert.Equal(StartupFailureBehavior.FailFast, failurePolicy.StartupFailureBehavior);
        Assert.Equal(StopFailureBehavior.BestEffortContinue, failurePolicy.StopFailureBehavior);
        Assert.True(failurePolicy.AllowManualRestart);

        Assert.True(diagnosticsResponse.IsSuccessStatusCode);
        using var diagnosticsDocument = JsonDocument.Parse(diagnosticsPayload);
        Assert.Equal("Cephalon.Engine", diagnosticsDocument.RootElement.GetProperty("meterName").GetString());
        Assert.Equal("Cephalon.Engine", diagnosticsDocument.RootElement.GetProperty("activitySourceName").GetString());
        Assert.Contains(
            diagnosticsDocument.RootElement.GetProperty("counters").EnumerateArray().Select(item => item.GetString()),
            value => string.Equals(value, "cephalon.runtime.failures", StringComparison.Ordinal));
        Assert.Equal((int)RuntimeHealthState.Healthy, diagnosticsDocument.RootElement.GetProperty("liveness").GetProperty("state").GetInt32());
        Assert.Equal((int)RuntimeHealthState.Healthy, diagnosticsDocument.RootElement.GetProperty("readiness").GetProperty("state").GetInt32());
        Assert.Equal("/health/live", diagnosticsDocument.RootElement.GetProperty("livenessPath").GetString());
        Assert.Equal("/health/ready", diagnosticsDocument.RootElement.GetProperty("readinessPath").GetString());

        Assert.True(healthResponse.IsSuccessStatusCode, healthPayload);
        Assert.Equal("application/json", healthResponse.Content.Headers.ContentType?.MediaType);
        using var healthDocument = JsonDocument.Parse(healthPayload);
        Assert.Equal("Healthy", healthDocument.RootElement.GetProperty("status").GetString());

        Assert.True(livenessResponse.IsSuccessStatusCode, livenessPayload);
        using var livenessDocument = JsonDocument.Parse(livenessPayload);
        Assert.Equal("Healthy", livenessDocument.RootElement.GetProperty("status").GetString());
        Assert.True(livenessDocument.RootElement.GetProperty("entries").TryGetProperty("cephalon.liveness", out _));

        Assert.True(readinessResponse.IsSuccessStatusCode, readinessPayload);
        using var readinessDocument = JsonDocument.Parse(readinessPayload);
        Assert.Equal("Healthy", readinessDocument.RootElement.GetProperty("status").GetString());
        Assert.True(readinessDocument.RootElement.GetProperty("entries").TryGetProperty("cephalon.readiness", out _));

        Assert.NotNull(greeting);
        Assert.Equal("Hello, Codex from the Cephalon future stack.", greeting.Message);

        Assert.NotNull(time);
        Assert.Equal(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero), time.UtcNow);

        Assert.True(graphQlResponse.IsSuccessStatusCode, graphQlPayload);
        using var graphQlDocument = JsonDocument.Parse(graphQlPayload);
        Assert.Equal(
            "Hello, Codex from the Cephalon future stack.",
            graphQlDocument.RootElement.GetProperty("data").GetProperty("hello").GetProperty("message").GetString());
        Assert.Equal(
            3,
            graphQlDocument.RootElement.GetProperty("data").GetProperty("hello").GetProperty("traits").GetArrayLength());
        Assert.True(graphQlSdlResponse.IsSuccessStatusCode, graphQlSdlPayload);
        Assert.Contains("type Query", graphQlSdlPayload, StringComparison.Ordinal);
        Assert.Contains("hello(name: String)", graphQlSdlPayload, StringComparison.Ordinal);

        Assert.True(rpcResponse.IsSuccessStatusCode);
        var rpcDocument = JsonDocument.Parse(rpcPayload);
        Assert.Equal("2.0", rpcDocument.RootElement.GetProperty("jsonRpc").GetString());
        Assert.Equal("req-1", rpcDocument.RootElement.GetProperty("id").GetString());
        Assert.Equal(
            "Hello, Codex from the Cephalon future stack.",
            rpcDocument.RootElement.GetProperty("result").GetProperty("message").GetString());

        Assert.Equal("Hello, Codex from the Cephalon future stack.", grpcReply.Message);
        Assert.Equal("2030-01-01T00:00:00.0000000+00:00", grpcReply.GeneratedAtUtc);
        Assert.Contains("modular", grpcReply.Traits);
        Assert.Equal(["modular", "observable", "composable"], grpcPrinciples);
        Assert.Equal(
            [
                "Hello, Alpha from the Cephalon future stack.",
                "Hello, Beta from the Cephalon future stack."
            ],
            grpcMessages);

        Assert.Equal("text/event-stream", sseResponse.Content.Headers.ContentType?.MediaType);
        Assert.Contains("event: principle", ssePayload, StringComparison.Ordinal);
        Assert.Contains("\"Principle\":\"modular\"", ssePayload, StringComparison.Ordinal);

        var webSocketDocument = JsonDocument.Parse(webSocketPayload);
        Assert.Equal("Hello, socket from the Cephalon future stack.", webSocketDocument.RootElement.GetProperty("Message").GetString());

        var runtime = app.Services.GetRequiredService<IRuntime>();
        Assert.Equal(RuntimeStatus.Started, runtime.Status);

        await app.StopAsync();

        Assert.Equal(RuntimeStatus.Stopped, runtime.Status);
        Assert.NotNull(runtime.StatusSnapshot.StoppedAtUtc);
    }

    [Fact]
    public async Task MapCephalonAppliesConfiguredRateLimitingOnlyToPublicHttpEndpoints()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Algorithm"] = "FixedWindow";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:PermitLimit"] = "1";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:QueueLimit"] = "0";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:WindowSeconds"] = "60";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var firstPublicResponse = await client.GetAsync("/api/platform/time");
        var secondPublicResponse = await client.GetAsync("/api/platform/time");
        var firstManifestResponse = await client.GetAsync("/engine/manifest");
        var secondManifestResponse = await client.GetAsync("/engine/manifest");
        var firstOpenApiResponse = await client.GetAsync("/openapi/v1.json");
        var secondOpenApiResponse = await client.GetAsync("/openapi/v1.json");
        var rejectedPayload = await secondPublicResponse.Content.ReadAsStringAsync();
        var policies = await client.GetFromJsonAsync<RateLimitingRuntimeDescriptor[]>("/engine/rate-limiting");

        Assert.Equal(HttpStatusCode.OK, firstPublicResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondPublicResponse.StatusCode);
        Assert.Contains("Too Many Requests", rejectedPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, firstManifestResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondManifestResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, firstOpenApiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondOpenApiResponse.StatusCode);
        Assert.NotNull(policies);
        var policy = Assert.Single(policies);
        Assert.Equal("aspnetcore-endpoint-policy", policy.ExecutionMode);
        Assert.Equal("FixedWindow", policy.Effective.Algorithm);
        Assert.Equal(1, policy.Effective.PermitLimit);
        Assert.Equal(0, policy.Effective.QueueLimit);
        Assert.Equal(60, policy.Effective.WindowSeconds);
        Assert.Contains("/engine", policy.ExcludedPathPrefixes);
        Assert.Contains("/openapi", policy.ExcludedPathPrefixes);
        Assert.Contains("/scalar", policy.ExcludedPathPrefixes);
    }

    [Fact]
    public async Task MapCephalonAppliesRateLimitingWhenHttpTransportIsSelectedInCode()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:Algorithm"] = "fixedwindow";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:PermitLimit"] = "1";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:QueueLimit"] = "0";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:RateLimiting:WindowSeconds"] = "60";
        builder.AddCephalon(engine =>
        {
            engine.UseBlueprint(BuiltInBlueprints.ModularMonolith);
            engine.AddTransport(BuiltInTransports.RestApi);
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var firstPublicResponse = await client.GetAsync("/api/platform/time");
        var secondPublicResponse = await client.GetAsync("/api/platform/time");
        var policies = await client.GetFromJsonAsync<RateLimitingRuntimeDescriptor[]>("/engine/rate-limiting");

        Assert.Equal(HttpStatusCode.OK, firstPublicResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondPublicResponse.StatusCode);
        Assert.NotNull(policies);
        var policy = Assert.Single(policies);
        Assert.Equal("FixedWindow", policy.Effective.Algorithm);
        Assert.Contains("rest-api", policy.TransportIds);
    }

    [Fact]
    public async Task MapCephalonExposesNoActiveRateLimitingPolicyWhenLimiterIsNotEnabled()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var policies = await client.GetFromJsonAsync<RateLimitingRuntimeDescriptor[]>("/engine/rate-limiting");
        var policyResponse = await client.GetAsync("/engine/rate-limiting/cephalon-public-http");
        var firstPublicResponse = await client.GetAsync("/api/platform/time");
        var secondPublicResponse = await client.GetAsync("/api/platform/time");

        Assert.NotNull(policies);
        Assert.Empty(policies);
        Assert.Equal(HttpStatusCode.NotFound, policyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, firstPublicResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondPublicResponse.StatusCode);
    }

    [Fact]
    public async Task MapCephalonExposesModuleContributedTechnologyCatalog()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "WebSocket";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "DigitalTwinOrchestration";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new TechnologyCatalogTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var selectedTechnologies = await client.GetFromJsonAsync<TechnologyDescriptor[]>("/engine/technologies");
        var technologyCatalog = await client.GetFromJsonAsync<TechnologyDescriptor[]>("/engine/technology-catalog");

        Assert.NotNull(selectedTechnologies);
        Assert.Contains(selectedTechnologies, technology => technology.Id == "digital-twin-orchestration");

        Assert.NotNull(technologyCatalog);
        Assert.Contains(technologyCatalog, technology => technology.Id == "digital-twin-orchestration");
        Assert.Contains(technologyCatalog, technology => technology.Id == "agentic-workloads");
    }

    [Fact]
    public void RenderOpenApiToggleScriptInjectsScalarDocumentSelectorFromEnabledVersions()
    {
        var renderMethod = typeof(EngineWebApplicationExtensions).GetMethod(
            "RenderOpenApiToggleScript",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(renderMethod);

        var payload = Assert.IsType<string>(renderMethod!.Invoke(null, ["/scalar", MultiDocumentNames, "v2"]));

        Assert.Contains("configuredScalarRoutePrefix = \"/scalar\"", payload, StringComparison.Ordinal);
        Assert.Contains("configuredDocumentNames = [\"v1\",\"v2\"]", payload, StringComparison.Ordinal);
        Assert.Contains("configuredDefaultDocumentName = \"v2\"", payload, StringComparison.Ordinal);
        Assert.Contains("cephalon-scalar-document-selector", payload, StringComparison.Ordinal);
        Assert.Contains("navigateToSelectedDocument", payload, StringComparison.Ordinal);
        Assert.Contains("scheduleSelectorRefresh", payload, StringComparison.Ordinal);
        Assert.Contains("findHeaderHost", payload, StringComparison.Ordinal);
        Assert.Contains("applyHeaderShellStyles", payload, StringComparison.Ordinal);
        Assert.Contains("Version", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("__CEPHALON_SCALAR_DOCUMENT_NAMES__", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("__CEPHALON_SCALAR_DEFAULT_DOCUMENT_NAME__", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonSupportsNamedOpenApiDocumentsAndScalarPages()
    {
        var contentRootPath = Path.Combine(
            Path.GetTempPath(),
            $"cephalon-scalar-openapi-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(contentRootPath);

        try
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                ContentRootPath = contentRootPath,
                EnvironmentName = "Production"
            });
            builder.WebHost.UseTestServer();
            builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:1"] = string.Empty;
            builder.Configuration[$"{EngineSettings.SectionName}:Discovery:Assemblies:0"] = string.Empty;
            builder.Configuration["OpenApi:EnabledVersions:0"] = "1";
            builder.Configuration["OpenApi:EnabledVersions:1"] = "2";
            builder.Configuration["OpenApi:DefaultVersion"] = "2";
            builder.Configuration["OpenApi:Version"] = "2026.04";
            builder.AddCephalon(_ => { });

            await using var app = builder.Build();
            app.MapGet("/api/openapi-documents/orders/{orderId}", (string orderId) => TypedResults.Ok(new { orderId }))
                .WithName("GetOpenApiDocumentOrder")
                .WithGroupName("v2");
            app.MapCephalon();

            await app.StartAsync();
            var client = app.GetTestClient();

            var v1Response = await client.GetAsync("/openapi/v1.json");
            var v2Response = await client.GetAsync("/openapi/v2.json");
            var scalarConfigResponse = await client.GetAsync("/scalar/openapi-toggle.js");
            var scalarRootRedirectResponse = await client.GetAsync("/scalar?culture=en");
            var scalarRootResponse = await client.GetAsync("/scalar/?culture=en");
            var scalarV2Response = await client.GetAsync("/scalar/v2");

            Assert.True(v1Response.IsSuccessStatusCode);
            Assert.True(v2Response.IsSuccessStatusCode);
            Assert.True(scalarConfigResponse.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.Redirect, scalarRootRedirectResponse.StatusCode);
            Assert.NotNull(scalarRootRedirectResponse.Headers.Location);
            Assert.Equal("/scalar/v2?culture=en", scalarRootRedirectResponse.Headers.Location!.OriginalString);
            Assert.True(scalarRootResponse.IsSuccessStatusCode);
            Assert.True(scalarV2Response.IsSuccessStatusCode);

            using var v1Document = JsonDocument.Parse(await v1Response.Content.ReadAsStringAsync());
            using var v2Document = JsonDocument.Parse(await v2Response.Content.ReadAsStringAsync());
            var v1Paths = v1Document.RootElement.GetProperty("paths");
            var v2Paths = v2Document.RootElement.GetProperty("paths");

            Assert.Equal("v1", v1Document.RootElement.GetProperty("info").GetProperty("version").GetString());
            Assert.Equal("v2", v2Document.RootElement.GetProperty("info").GetProperty("version").GetString());
            Assert.False(v1Paths.TryGetProperty("/api/openapi-documents/orders/{orderId}", out _));
            Assert.True(v2Paths.TryGetProperty("/api/openapi-documents/orders/{orderId}", out var versionedPath));
            Assert.True(versionedPath.TryGetProperty("get", out _));

            var scalarConfigPayload = await scalarConfigResponse.Content.ReadAsStringAsync();
            var scalarRootPayload = await scalarRootResponse.Content.ReadAsStringAsync();
            var scalarV2Payload = await scalarV2Response.Content.ReadAsStringAsync();
            Assert.Contains("configuredDocumentNames = [\"v1\",\"v2\"]", scalarConfigPayload, StringComparison.Ordinal);
            Assert.Contains("configuredDefaultDocumentName = \"v2\"", scalarConfigPayload, StringComparison.Ordinal);
            Assert.Contains("cephalon-scalar-document-selector", scalarConfigPayload, StringComparison.Ordinal);
            Assert.Contains("navigateToSelectedDocument", scalarConfigPayload, StringComparison.Ordinal);
            Assert.Contains("Version", scalarConfigPayload, StringComparison.Ordinal);
            Assert.Contains("Scalar", scalarRootPayload, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/scalar/openapi-toggle.js?v=", scalarRootPayload, StringComparison.Ordinal);
            Assert.Contains("openapi/v2.json", scalarRootPayload, StringComparison.Ordinal);
            Assert.Contains("Scalar", scalarV2Payload, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/scalar/openapi-toggle.js?v=", scalarV2Payload, StringComparison.Ordinal);
            Assert.Contains("openapi/v2.json", scalarV2Payload, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(contentRootPath))
            {
                Directory.Delete(contentRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task MapCephalonPublishesOnlyEnabledOpenApiVersionsWhenOtherVersionedEndpointsExist()
    {
        var contentRootPath = Path.Combine(
            Path.GetTempPath(),
            $"cephalon-openapi-enabled-version-filter-{Guid.NewGuid():N}");
        Directory.CreateDirectory(contentRootPath);

        try
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                ContentRootPath = contentRootPath,
                EnvironmentName = "Production"
            });
            builder.WebHost.UseTestServer();
            builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:1"] = string.Empty;
            builder.Configuration[$"{EngineSettings.SectionName}:Discovery:Assemblies:0"] = string.Empty;
            builder.Configuration["OpenApi:EnabledVersions:0"] = "2";
            builder.Configuration["OpenApi:EnabledVersions:1"] = "3";
            builder.Configuration["OpenApi:DefaultVersion"] = "1";
            builder.AddCephalon(_ => { });

            await using var app = builder.Build();
            app.MapGet("/api/version-filter/v1/orders/{orderId}", (string orderId) => TypedResults.Ok(new { version = 1, orderId }))
                .WithName("GetVersionFilterV1Order")
                .WithGroupName("v1");
            app.MapGet("/api/version-filter/v2/orders/{orderId}", (string orderId) => TypedResults.Ok(new { version = 2, orderId }))
                .WithName("GetVersionFilterV2Order")
                .WithGroupName("v2");
            app.MapGet("/api/version-filter/v3/orders/{orderId}", (string orderId) => TypedResults.Ok(new { version = 3, orderId }))
                .WithName("GetVersionFilterV3Order")
                .WithGroupName("v3");
            app.MapCephalon();

            await app.StartAsync();
            var client = app.GetTestClient();

            var v1Response = await client.GetAsync("/openapi/v1.json");
            var v2Response = await client.GetAsync("/openapi/v2.json");
            var v3Response = await client.GetAsync("/openapi/v3.json");
            var scalarConfigResponse = await client.GetAsync("/scalar/openapi-toggle.js");
            var scalarRootRedirectResponse = await client.GetAsync("/scalar?culture=en");

            Assert.Equal(HttpStatusCode.NotFound, v1Response.StatusCode);
            Assert.True(v2Response.IsSuccessStatusCode);
            Assert.True(v3Response.IsSuccessStatusCode);
            Assert.True(scalarConfigResponse.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.Redirect, scalarRootRedirectResponse.StatusCode);
            Assert.NotNull(scalarRootRedirectResponse.Headers.Location);
            Assert.Equal("/scalar/v2?culture=en", scalarRootRedirectResponse.Headers.Location!.OriginalString);

            using var v2Document = JsonDocument.Parse(await v2Response.Content.ReadAsStringAsync());
            using var v3Document = JsonDocument.Parse(await v3Response.Content.ReadAsStringAsync());
            var v2Paths = v2Document.RootElement.GetProperty("paths");
            var v3Paths = v3Document.RootElement.GetProperty("paths");

            Assert.True(v2Paths.TryGetProperty("/api/version-filter/v2/orders/{orderId}", out _));
            Assert.False(v2Paths.TryGetProperty("/api/version-filter/v1/orders/{orderId}", out _));
            Assert.False(v2Paths.TryGetProperty("/api/version-filter/v3/orders/{orderId}", out _));
            Assert.True(v3Paths.TryGetProperty("/api/version-filter/v3/orders/{orderId}", out _));
            Assert.False(v3Paths.TryGetProperty("/api/version-filter/v1/orders/{orderId}", out _));
            Assert.False(v3Paths.TryGetProperty("/api/version-filter/v2/orders/{orderId}", out _));

            var scalarConfigPayload = await scalarConfigResponse.Content.ReadAsStringAsync();
            Assert.Contains("configuredDocumentNames = [\"v2\",\"v3\"]", scalarConfigPayload, StringComparison.Ordinal);
            Assert.Contains("configuredDefaultDocumentName = \"v2\"", scalarConfigPayload, StringComparison.Ordinal);
            Assert.DoesNotContain("configuredDocumentNames = [\"v1\",\"v2\",\"v3\"]", scalarConfigPayload, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(contentRootPath))
            {
                Directory.Delete(contentRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task MapCephalonSupportsConfigurableOpenApiScalarAndRestRoutePrefixes()
    {
        var contentRootPath = Path.Combine(
            Path.GetTempPath(),
            $"cephalon-configurable-route-prefixes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(contentRootPath);

        try
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                ContentRootPath = contentRootPath,
                EnvironmentName = "Production"
            });
            builder.WebHost.UseTestServer();
            builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:1"] = "JsonRpc";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:2"] = "Grpc";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:3"] = "GraphQL";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:4"] = "ServerSentEvents";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:5"] = "WebSocket";
            builder.Configuration["ApiRoutes:Prefixes:Rest"] = "/service-api";
            builder.Configuration["ApiRoutes:Prefixes:GraphQL"] = "/graph";
            builder.Configuration["ApiRoutes:Prefixes:JsonRpc"] = "/invoke";
            builder.Configuration["ApiRoutes:Prefixes:Grpc"] = "/rpc-bin";
            builder.Configuration["ApiRoutes:Prefixes:Sse"] = "/stream";
            builder.Configuration["ApiRoutes:Prefixes:Ws"] = "/socket";
            builder.Configuration["OpenApi:RoutePattern"] = "/specs/{documentName}.json";
            builder.Configuration["OpenApi:Scalar:RoutePrefix"] = "/docs/api-reference";
            builder.AddGraphQLTransport();
            builder.AddGrpcTransport();
            builder.AddJsonRpcTransport();
            builder.AddCephalon(cephalon =>
            {
                cephalon.AddModule(new PlatformTestModule());
                cephalon.AddModule(new DiscoveryTestModule());
            });

            await using var app = builder.Build();
            app.MapCephalon();

            await app.StartAsync();
            var client = app.GetTestClient();

            var restResponse = await client.GetAsync("/service-api/discovery/hello/Codex");
            var legacyRestResponse = await client.GetAsync("/api/discovery/hello/Codex");
            var openApiResponse = await client.GetAsync("/specs/v1.json");
            var legacyOpenApiResponse = await client.GetAsync("/openapi/v1.json");
            var scalarRootRedirectResponse = await client.GetAsync("/docs/api-reference?culture=en");
            var scalarRootResponse = await client.GetAsync("/docs/api-reference/?culture=en");
            var scalarV1Response = await client.GetAsync("/docs/api-reference/v1");
            var scalarConfigResponse = await client.GetAsync("/docs/api-reference/openapi-toggle.js");
            var scalarConfigPayload = await scalarConfigResponse.Content.ReadAsStringAsync();
            var scalarRootPayload = await scalarRootResponse.Content.ReadAsStringAsync();
            var graphQlResponse = await client.PostAsJsonAsync("/graph", new
            {
                query = "query ($name: String) { hello(name: $name) { message } }",
                variables = new { name = "Configurable" }
            });
            var rpcResponse = await client.PostAsJsonAsync("/invoke/discovery", new
            {
                jsonRpc = "2.0",
                method = "discovery.hello",
                @params = new Dictionary<string, string?> { ["name"] = "Configurable" },
                id = "req-3"
            });
            var sseResponse = await client.GetAsync("/stream/discovery/principles");
            var grpcHandler = new GrpcSubdirectoryHandler(app.GetTestServer().CreateHandler(), "/rpc-bin");
            using var grpcHttpClient = new HttpClient(grpcHandler)
            {
                BaseAddress = new Uri("http://localhost")
            };
            grpcHttpClient.DefaultRequestVersion = HttpVersion.Version20;
            grpcHttpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
            using var grpcChannel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
            {
                HttpClient = grpcHttpClient
            });
            var grpcClient = new DiscoveryService.DiscoveryServiceClient(grpcChannel);
            var grpcReply = await grpcClient.SayHelloAsync(new HelloRequest { Name = "Configurable" });
            var webSocketClient = app.GetTestServer().CreateWebSocketClient();
            using var webSocket = await webSocketClient.ConnectAsync(new Uri("ws://localhost/socket/discovery"), CancellationToken.None);
            var webSocketBuffer = new byte[4096];
            var webSocketReceive = await webSocket.ReceiveAsync(webSocketBuffer, CancellationToken.None);
            var webSocketPayload = Encoding.UTF8.GetString(webSocketBuffer, 0, webSocketReceive.Count);

            Assert.True(restResponse.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.NotFound, legacyRestResponse.StatusCode);
            Assert.True(openApiResponse.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.NotFound, legacyOpenApiResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, scalarRootRedirectResponse.StatusCode);
            Assert.NotNull(scalarRootRedirectResponse.Headers.Location);
            Assert.Equal("/docs/api-reference/v1?culture=en", scalarRootRedirectResponse.Headers.Location!.OriginalString);
            Assert.True(scalarRootResponse.IsSuccessStatusCode);
            Assert.True(scalarV1Response.IsSuccessStatusCode);
            Assert.True(scalarConfigResponse.IsSuccessStatusCode);
            Assert.True(graphQlResponse.IsSuccessStatusCode);
            Assert.True(rpcResponse.IsSuccessStatusCode);
            Assert.True(sseResponse.IsSuccessStatusCode);
            Assert.Contains("Configurable", grpcReply.Message, StringComparison.Ordinal);
            Assert.Contains("socket", webSocketPayload, StringComparison.Ordinal);
            Assert.Contains("configuredScalarRoutePrefix = \"/docs/api-reference\"", scalarConfigPayload, StringComparison.Ordinal);
            Assert.Contains("\"title\":\"v1\"", scalarRootPayload, StringComparison.Ordinal);
            Assert.Contains("specs/v1.json", scalarRootPayload, StringComparison.Ordinal);

            using var openApiDocument = JsonDocument.Parse(await openApiResponse.Content.ReadAsStringAsync());
            Assert.True(openApiDocument.RootElement.GetProperty("paths").TryGetProperty("/service-api/discovery/hello/{name}", out _));
            Assert.False(openApiDocument.RootElement.GetProperty("paths").TryGetProperty("/api/discovery/hello/{name}", out _));
        }
        finally
        {
            if (Directory.Exists(contentRootPath))
            {
                Directory.Delete(contentRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task MapCephalonSupportsEmptyRestPrefix()
    {
        var contentRootPath = Path.Combine(
            Path.GetTempPath(),
            $"cephalon-empty-rest-prefix-{Guid.NewGuid():N}");
        Directory.CreateDirectory(contentRootPath);

        try
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                ContentRootPath = contentRootPath,
                EnvironmentName = "Production"
            });
            builder.WebHost.UseTestServer();
            builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
            builder.Configuration["ApiRoutes:Prefixes:Rest"] = string.Empty;
            builder.AddCephalon(cephalon =>
            {
                cephalon.AddModule(new PlatformTestModule());
                cephalon.AddModule(new DiscoveryTestModule());
            });

            await using var app = builder.Build();
            app.MapCephalon();

            await app.StartAsync();
            var client = app.GetTestClient();

            var response = await client.GetAsync("/discovery/hello/Codex");
            var legacyResponse = await client.GetAsync("/api/discovery/hello/Codex");

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.NotFound, legacyResponse.StatusCode);
        }
        finally
        {
            if (Directory.Exists(contentRootPath))
            {
                Directory.Delete(contentRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task MapCephalonAppliesGlobalOpenApiInfoVersionOverrideToSingleDocumentHosts()
    {
        var contentRootPath = Path.Combine(
            Path.GetTempPath(),
            $"cephalon-single-document-openapi-version-{Guid.NewGuid():N}");
        Directory.CreateDirectory(contentRootPath);

        try
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                ContentRootPath = contentRootPath,
                EnvironmentName = "Production"
            });
            builder.WebHost.UseTestServer();
            builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
            builder.Configuration["OpenApi:Version"] = "2026.04";
            builder.AddCephalon(cephalon =>
            {
                cephalon.AddModule(new PlatformTestModule());
                cephalon.AddModule(new DiscoveryTestModule());
            });

            await using var app = builder.Build();
            app.MapCephalon();

            await app.StartAsync();
            var client = app.GetTestClient();

            var openApiResponse = await client.GetAsync("/openapi/v1.json");

            Assert.True(openApiResponse.IsSuccessStatusCode);

            using var document = JsonDocument.Parse(await openApiResponse.Content.ReadAsStringAsync());
            Assert.Equal("2026.04", document.RootElement.GetProperty("info").GetProperty("version").GetString());
        }
        finally
        {
            if (Directory.Exists(contentRootPath))
            {
                Directory.Delete(contentRootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task MapCephalonExposesTechnologyRuntimeSurfaces()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "AgenticWorkloads";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:1"] = "EventDrivenIntegration";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:2"] = "KnowledgeRetrieval";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:3"] = "EdgeNativeDelivery";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:4"] = "MultiTenancy";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new TechnologyPackContributionModule());
            cephalon.AddModule(new WorkflowCatalogTestModule("hosting-agentics"));
            cephalon.AddAgentics(options =>
            {
                options.Tools.Add(new AgentToolDescriptor(
                    id: "planner",
                    displayName: "Planner",
                    description: "Creates runtime plans."));
            });
            cephalon.AddEventing(options =>
            {
                options.Channels.Add(new EventChannelDescriptor(
                    id: "orders",
                    displayName: "Orders",
                    description: "Order integration channel."));
            });
            cephalon.AddRetrieval(options =>
            {
                options.Collections.Add(new KnowledgeCollectionDescriptor(
                    id: "docs",
                    displayName: "Docs",
                    description: "Retrieval collection for docs."));
            });
            cephalon.AddEdge(options =>
            {
                options.Nodes.Add(new EdgeNodeDescriptor(
                    id: "storefront-edge",
                    displayName: "Storefront Edge",
                    description: "Regional storefront edge node."));
            });
            cephalon.AddMultiTenancy(options =>
            {
                options.DefaultTenantId = "tenant-001";
                options.Tenants.Add(new TenantContext(
                    tenantId: "tenant-001",
                    tenantKey: "acme",
                    displayName: "Acme",
                    domains: ["acme.example.test"]));
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var agentToolDispatcher = app.Services.GetRequiredService<IAgentToolDispatcher>();
        await agentToolDispatcher.ExecuteAsync(new AgentToolExecutionRequest(
            toolId: "analyst",
            runId: "hosting-agent-run-001",
            arguments: new Dictionary<string, string>
            {
                ["subject"] = "hosting runtime"
            },
            actorId: "hosting-test",
            correlationId: "corr-hosting-agentics-001"));
        var reporter = app.Services.GetRequiredService<IEventSubscriptionRuntimeReporter>();
        await reporter.ReportAsync(
            new EventSubscriptionExecutionReport(
                subscriptionId: "audit-projector",
                outcome: EventSubscriptionExecutionOutcomes.RetryScheduled,
                observedAtUtc: new DateTimeOffset(2026, 04, 04, 11, 0, 0, TimeSpan.Zero),
                messageId: "audit-hosting-001",
                attempt: 4,
                error: "Hosting retry requested",
                metadata: new Dictionary<string, string>
                {
                    ["nextRetryAtUtc"] = "2026-04-04T11:05:00.0000000+00:00",
                    ["retryPolicy"] = "delayed"
                }));
        var retrievalIndexer = app.Services.GetRequiredService<IKnowledgeIndexer>();
        await retrievalIndexer.IndexAsync(new KnowledgeIndexingRequest(
            collectionId: "runbooks",
            runId: "hosting-retrieval-index-001",
            actorId: "hosting-test",
            correlationId: "corr-hosting-retrieval-001"));
        var retrievalQueryEngine = app.Services.GetRequiredService<IKnowledgeQueryEngine>();
        var retrievalQuery = await retrievalQueryEngine.QueryAsync(new KnowledgeQueryRequest(
            collectionId: "runbooks",
            queryText: "retrieval freshness",
            maxResults: 5,
            actorId: "hosting-test",
            correlationId: "corr-hosting-retrieval-query-001"));
        var client = app.GetTestClient();
        var manifest = await client.GetFromJsonAsync<RuntimeManifest>("/engine/manifest");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        var surfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces");
        var eventingSurfaces = await client.GetFromJsonAsync<TechnologyRuntimeSurface[]>("/engine/technology-surfaces/event-driven-integration");

        Assert.NotNull(manifest);
        Assert.Equal("modular-vertical-slice", manifest.AppProfile.BlueprintId);
        Assert.NotNull(snapshot);
        Assert.Equal(RuntimeStatus.Started, snapshot.Status.Status);
        Assert.Equal("modular-vertical-slice", snapshot.Manifest.AppProfile.BlueprintId);
        Assert.Equal(7, snapshot.TechnologySurfaces.Count);
        Assert.Contains(snapshot.DiagnosticsConventions, convention => convention.Source == "Cephalon.Eventing");
        Assert.NotNull(surfaces);
        Assert.Equal(7, surfaces.Length);
        Assert.NotNull(eventingSurfaces);
        Assert.Equal(2, eventingSurfaces.Length);

        var agentics = Assert.Single(surfaces, surface => surface.TechnologyId == "agentic-workloads");
        Assert.Contains(agentics.Entries, entry => entry.Id == "planner");
        var analyst = Assert.Single(agentics.Entries, entry => entry.Id == "analyst");
        Assert.Equal("cephalon-managed", analyst.Metadata["executionOwnership"]);
        Assert.Equal("reported", analyst.Metadata["runtimeState"]);
        Assert.Equal("hosting-agent-run-001", analyst.Metadata["lastRunId"]);
        Assert.Equal("succeeded", analyst.Metadata["lastOutcome"]);
        Assert.Equal("2", analyst.Metadata["totalReports"]);
        Assert.Equal("hosting-test", analyst.Metadata["lastActorId"]);
        Assert.Equal("corr-hosting-agentics-001", analyst.Metadata["lastCorrelationId"]);
        Assert.Equal("Analyzed hosting runtime.", analyst.Metadata["lastOutputSummary"]);
        var approvalOrchestrator = Assert.Single(agentics.Entries, entry => entry.Id == "approval-orchestrator");
        Assert.Equal("approval-flow", approvalOrchestrator.Metadata["executionGraphId"]);
        Assert.Equal("Approval Flow", approvalOrchestrator.Metadata["executionGraphDisplayName"]);
        Assert.Equal("activate", approvalOrchestrator.Metadata["executionGraphPhase"]);
        Assert.Equal("true", approvalOrchestrator.Metadata["executionGraphIsActive"]);
        Assert.Equal("approval-pump", approvalOrchestrator.Metadata["hostedExecutionId"]);
        Assert.Equal("Approval Pump", approvalOrchestrator.Metadata["hostedExecutionDisplayName"]);
        Assert.Equal("background-service", approvalOrchestrator.Metadata["hostedExecutionKind"]);
        Assert.Equal("activate", approvalOrchestrator.Metadata["hostedExecutionPhase"]);
        Assert.Equal("true", approvalOrchestrator.Metadata["hostedExecutionIsActive"]);
        Assert.Equal("workflow.approval.record,workflow.approval.request", approvalOrchestrator.Metadata["capabilityKeys"]);
        Assert.Equal("Approval decision,Approval request", approvalOrchestrator.Metadata["capabilityDisplayNames"]);
        Assert.Equal("true", approvalOrchestrator.Metadata["orchestrationLinked"]);

        var eventChannelSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-channels");
        Assert.Contains(eventChannelSurface.Entries, entry => entry.Id == "orders");
        Assert.Contains(eventChannelSurface.Entries, entry => entry.Id == "audit");
        var eventSubscriptionSurface = Assert.Single(eventingSurfaces, surface => surface.SurfaceId == "event-subscriptions");
        Assert.Contains(
            eventSubscriptionSurface.Entries,
                entry => entry.Id == "audit-projector" &&
                    entry.Metadata["channelId"] == "audit" &&
                    entry.Metadata["dispatchRuntime"] == "application-managed" &&
                    entry.Metadata["runtimeState"] == "reported" &&
                    entry.Metadata["subscriptionRuntime"] == "hosted-execution-linked" &&
                    entry.Metadata["hostedExecutionId"] == "audit-projector-pump" &&
                    entry.Metadata["executionGraphId"] == "audit-subscription-flow" &&
                    entry.Metadata["executionGraphDisplayName"] == "Audit Subscription Flow" &&
                    entry.Metadata["executionGraphPhase"] == "activate" &&
                    entry.Metadata["executionGraphIsActive"] == "true" &&
                    entry.Metadata["hostedExecutionPhase"] == "activate" &&
                    entry.Metadata["hostedExecutionIsActive"] == "true" &&
                    entry.Metadata["lastOutcome"] == "retry-scheduled" &&
                    entry.Metadata["lastMessageId"] == "audit-hosting-001" &&
                    entry.Metadata["lastAttempt"] == "4" &&
                    entry.Metadata["retryScheduledCount"] == "1" &&
                    entry.Metadata["retryPending"] == "true" &&
                    entry.Metadata["reported.nextRetryAtUtc"] == "2026-04-04T11:05:00.0000000+00:00" &&
                    entry.Metadata["reported.retryPolicy"] == "delayed" &&
                    entry.Metadata["lastError"] == "Hosting retry requested");
        Assert.Contains(
            snapshot.TechnologySurfaces.Single(surface => surface.TechnologyId == "agentic-workloads").Entries,
            entry => entry.Id == "approval-orchestrator" &&
                entry.Metadata["hostedExecutionId"] == "approval-pump");
        Assert.Contains(
            snapshot.TechnologySurfaces.Single(surface => surface.SurfaceId == "event-channels").Entries,
            entry => entry.Id == "audit");
        Assert.Contains(
            snapshot.TechnologySurfaces.Single(surface => surface.SurfaceId == "event-subscriptions").Entries,
            entry => entry.Id == "audit-projector" &&
                entry.Metadata["lastOutcome"] == "retry-scheduled" &&
                entry.Metadata["retryPending"] == "true");

        var retrieval = Assert.Single(surfaces, surface => surface.TechnologyId == "knowledge-retrieval");
        Assert.Contains(retrieval.Entries, entry => entry.Id == "docs");
        Assert.True(retrievalQuery.HasMatches);
        Assert.Contains(retrieval.Entries, entry => entry.Id == "runbooks" &&
            entry.Metadata["indexingOwnership"] == "cephalon-managed" &&
            entry.Metadata["queryOwnership"] == "cephalon-managed" &&
            entry.Metadata["runtimeState"] == "indexed" &&
            entry.Metadata["freshnessState"] == KnowledgeIndexFreshnessStates.Fresh &&
            entry.Metadata["documentCount"] == "2" &&
            entry.Metadata["queryCount"] == "1");

        var tenancySurfaces = surfaces.Where(surface => surface.TechnologyId == "multi-tenancy").ToArray();
        Assert.Equal(2, tenancySurfaces.Length);
        Assert.Contains(
            tenancySurfaces.Single(surface => surface.SurfaceId == "tenant-resolution").Entries,
            entry => entry.Id == "tenant-runtime" &&
                entry.Metadata["configuredTenantCount"] == "1" &&
                entry.Metadata["defaultTenantId"] == "tenant-001");
        Assert.Contains(
            tenancySurfaces.Single(surface => surface.SurfaceId == "tenant-governance-boundaries").Entries,
            entry => entry.Id == "tenant-membership" &&
                entry.Metadata["ownership"] == "taxonomy-only" &&
                entry.Metadata["plannedOwnership"] == "companion-planned" &&
                entry.Metadata["basePackageOwnership"] == "not-owned" &&
                entry.Metadata["suggestedPackage"] == "Cephalon.MultiTenancy.Governance");

        var edge = Assert.Single(surfaces, surface => surface.TechnologyId == "edge-native-delivery");
        Assert.Contains(edge.Entries, entry => entry.Id == "storefront-edge");
        Assert.Contains(edge.Entries, entry => entry.Id == "warehouse-edge");
    }

    [Fact]
    public async Task MapCephalonExposesPhase8DataProductCdcProjectionInboxOutboxAndAuthorizationCatalogs()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "IdentityAccess";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();
        var dataProducts = await client.GetFromJsonAsync<DataProductDescriptor[]>("/engine/data-products");
        var dataProduct = await client.GetFromJsonAsync<DataProductDescriptor>("/engine/data-products/tenant-profile");
        var cdcCaptures = await client.GetFromJsonAsync<CdcCaptureDescriptor[]>("/engine/cdc-captures");
        var cdcCapture = await client.GetFromJsonAsync<CdcCaptureDescriptor>("/engine/cdc-captures/tenant-profile-cdc");
        var cdcCapturesByOutbox = await client.GetFromJsonAsync<CdcCaptureDescriptor[]>("/engine/cdc-captures/outboxes/tenant-event-outbox");
        var projections = await client.GetFromJsonAsync<ProjectionDescriptor[]>("/engine/projections");
        var projection = await client.GetFromJsonAsync<ProjectionDescriptor>("/engine/projections/tenant-summary");
        var inboxes = await client.GetFromJsonAsync<InboxDescriptor[]>("/engine/inboxes");
        var inbox = await client.GetFromJsonAsync<InboxDescriptor>("/engine/inboxes/tenant-event-inbox");
        var outboxes = await client.GetFromJsonAsync<OutboxDescriptor[]>("/engine/outboxes");
        var outbox = await client.GetFromJsonAsync<OutboxDescriptor>("/engine/outboxes/tenant-event-outbox");
        var auditStores = await client.GetFromJsonAsync<AuditStoreDescriptor[]>("/engine/audit-stores");
        var auditStore = await client.GetFromJsonAsync<AuditStoreDescriptor>("/engine/audit-stores/tenant-audit-store");
        var policies = await client.GetFromJsonAsync<AuthorizationPolicyDescriptor[]>("/engine/authorization-policies");
        var policy = await client.GetFromJsonAsync<AuthorizationPolicyDescriptor>("/engine/authorization-policies/tenant-admin");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(dataProducts);
        Assert.Single(dataProducts);
        Assert.NotNull(dataProduct);
        Assert.Equal("phase8-runtime-catalogs", dataProduct.SourceModuleId);
        Assert.Equal("tenant-management", dataProduct.DomainId);
        Assert.Equal("tenant-profile-v1", dataProduct.ContractId);

        Assert.NotNull(cdcCaptures);
        Assert.Single(cdcCaptures);
        Assert.NotNull(cdcCapture);
        Assert.Equal("phase8-runtime-catalogs", cdcCapture.SourceModuleId);
        Assert.Equal("postgresql", cdcCapture.Provider);
        Assert.Equal("tenant-event-outbox", cdcCapture.OutboxId);
        Assert.Equal("debezium-envelope", cdcCapture.EventFormat);
        Assert.NotNull(cdcCapturesByOutbox);
        Assert.Single(cdcCapturesByOutbox);

        Assert.NotNull(projections);
        Assert.Single(projections);
        Assert.NotNull(projection);
        Assert.Equal("phase8-runtime-catalogs", projection.SourceModuleId);
        Assert.Equal("tenant-summary-read-model", projection.TargetStoreId);

        Assert.NotNull(outboxes);
        Assert.Single(outboxes);
        Assert.NotNull(outbox);
        Assert.Equal("phase8-runtime-catalogs", outbox.SourceModuleId);
        Assert.Equal("relational", outbox.Provider);
        Assert.Equal(["audit", "tenant-events"], outbox.ChannelIds);

        Assert.NotNull(inboxes);
        Assert.Single(inboxes);
        Assert.NotNull(inbox);
        Assert.Equal("phase8-runtime-catalogs", inbox.SourceModuleId);
        Assert.Equal("relational", inbox.Provider);
        Assert.Equal(["tenant-events"], inbox.ChannelIds);

        Assert.NotNull(auditStores);
        Assert.Single(auditStores);
        Assert.NotNull(auditStore);
        Assert.Equal("phase8-runtime-catalogs", auditStore.SourceModuleId);
        Assert.Equal("memory", auditStore.Provider);
        Assert.Equal("volatile-buffer", auditStore.Mode);

        Assert.NotNull(policies);
        Assert.Equal(2, policies.Length);
        Assert.NotNull(policy);
        Assert.Contains(AuthorizationMode.Rbac, policy.Modes);
        Assert.Equal("phase8-runtime-catalogs", policy.Metadata["sourceModuleId"]);

        Assert.NotNull(snapshot);
        Assert.Single(snapshot.DataProducts);
        Assert.Single(snapshot.CdcCaptures);
        Assert.Single(snapshot.Projections);
        Assert.Single(snapshot.Inboxes);
        Assert.Single(snapshot.Outboxes);
        Assert.Single(snapshot.AuditStores);
        Assert.Equal(2, snapshot.AuthorizationPolicies.Count);
        Assert.Contains(snapshot.DataProducts, item => item.Id == "tenant-profile");
        Assert.Contains(snapshot.CdcCaptures, item => item.Id == "tenant-profile-cdc");
        Assert.Contains(snapshot.Inboxes, item => item.Id == "tenant-event-inbox");
        Assert.Contains(snapshot.Outboxes, item => item.Id == "tenant-event-outbox");
        Assert.Contains(snapshot.AuditStores, item => item.Id == "tenant-audit-store");
        Assert.Contains(snapshot.AuthorizationPolicies, item => item.Id == "tenant-boundary");
    }

    [Fact]
    public async Task MapCephalonHonorsConfigurationDrivenAuditWriterDisablement()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Audit:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Audit:EnableInMemoryWriter"] = "false";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new AuditCaptureModule());
            cephalon.AddAudit();
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var auditStores = await client.GetFromJsonAsync<AuditStoreDescriptor[]>("/engine/audit-stores");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(auditStores);
        Assert.Empty(auditStores);
        Assert.NotNull(snapshot);
        Assert.Empty(snapshot.AuditStores);
    }

    [Fact]
    public async Task MapCephalonExposesExecutionGraphsAcrossEndpointAndSnapshot()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new WorkflowCatalogTestModule("hosting-test"));
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var hostedExecutions = await client.GetFromJsonAsync<HostedExecutionDescriptor[]>("/engine/hosted-executions");
        var hostedExecution = await client.GetFromJsonAsync<HostedExecutionDescriptor>("/engine/hosted-executions/approval-pump");
        var graphs = await client.GetFromJsonAsync<ExecutionGraphDescriptor[]>("/engine/execution-graphs");
        var graph = await client.GetFromJsonAsync<ExecutionGraphDescriptor>("/engine/execution-graphs/approval-flow");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(hostedExecutions);
        var approvalPump = Assert.Single(hostedExecutions);
        Assert.Equal("approval-pump", approvalPump.Id);
        Assert.Equal("workflow-catalog", approvalPump.SourceModuleId);
        Assert.Equal("background-service", approvalPump.Kind);
        Assert.Equal("approval-flow", approvalPump.ExecutionGraphId);
        Assert.True(approvalPump.StartsWithHost);
        Assert.NotNull(hostedExecution);
        Assert.Equal("Approval Pump", hostedExecution.DisplayName);

        Assert.NotNull(graphs);
        var approvalFlow = Assert.Single(graphs);
        Assert.Equal("approval-flow", approvalFlow.Id);
        Assert.Equal("workflow-catalog", approvalFlow.SourceModuleId);
        Assert.Equal("request-review", approvalFlow.EntryNodeId);
        Assert.Equal(3, approvalFlow.Nodes.Count);
        Assert.Equal(2, approvalFlow.Edges.Count);

        Assert.NotNull(graph);
        Assert.Equal("Approval Flow", graph.DisplayName);
        Assert.Contains(graph.Nodes, node => node.CapabilityKey == "workflow.approval.request");
        Assert.Contains(graph.Nodes, node => node.CapabilityKey == "workflow.approval.record");
        Assert.Contains(graph.Edges, edge => edge.Condition == "decision == approved");

        Assert.NotNull(snapshot);
        Assert.Single(snapshot.HostedExecutions);
        Assert.Equal("approval-pump", snapshot.HostedExecutions[0].Id);
        Assert.Single(snapshot.ExecutionGraphs);
        Assert.Equal("approval-flow", snapshot.ExecutionGraphs[0].Id);
        Assert.Contains(snapshot.ExecutionGraphs[0].Nodes, node => node.Id == "complete");
    }

    [Fact]
    public async Task MapCephalonExposesDiagnosticsCatalogAcrossDiagnosticsAndSnapshot()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var diagnostics = await client.GetFromJsonAsync<DiagnosticsSurface>("/engine/diagnostics");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(diagnostics);
        Assert.Contains(diagnostics.Counters, counter => counter == "cephalon.execution-graphs.transitions");
        Assert.Contains(diagnostics.Counters, counter => counter == "cephalon.hosted-executions.transitions");
        var engineConvention = Assert.Single(diagnostics.Conventions, convention => convention.Source == "Cephalon.Engine");
        Assert.Equal(2000, engineConvention.MinimumEventId);
        Assert.Equal(2005, engineConvention.MaximumEventId);
        Assert.Contains(engineConvention.Events, entry => entry.Id == 2002 && entry.Name == "LogRuntimeFailure");
        Assert.Contains(engineConvention.Events, entry => entry.Id == 2004 && entry.Name == "LogExecutionGraphTransition");
        Assert.Contains(engineConvention.Events, entry => entry.Id == 2005 && entry.Name == "LogHostedExecutionTransition");
        var aspNetCoreConvention = Assert.Single(diagnostics.Conventions, convention => convention.Source == "Cephalon.AspNetCore");
        Assert.Equal(3200, aspNetCoreConvention.MinimumEventId);
        Assert.Equal(3204, aspNetCoreConvention.MaximumEventId);
        Assert.Contains(aspNetCoreConvention.Events, entry => entry.Id == 3201 && entry.Name == "HttpRequestBodyCaptured");

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.DiagnosticsConventions, convention => convention.Source == "Cephalon.Engine");
        Assert.Contains(snapshot.DiagnosticsConventions, convention => convention.Source == "Cephalon.AspNetCore");
        Assert.Contains(
            snapshot.DiagnosticsConventions.Single(convention => convention.Source == "Cephalon.Engine").Events,
            entry => entry.Id == 2002);
        Assert.Contains(
            snapshot.DiagnosticsConventions.Single(convention => convention.Source == "Cephalon.Engine").Events,
            entry => entry.Id == 2004);
        Assert.Contains(
            snapshot.DiagnosticsConventions.Single(convention => convention.Source == "Cephalon.Engine").Events,
            entry => entry.Id == 2005);
        Assert.Contains(
            snapshot.DiagnosticsConventions.Single(convention => convention.Source == "Cephalon.AspNetCore").Events,
            entry => entry.Id == 3203);
    }

    [Fact]
    public async Task MapCephalonLogsHttpRequestsAndResponsesWithBodiesAndTraceCorrelation()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:LogRequestBody"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:LogResponseBody"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();
        app.MapPost("/echo", async context =>
        {
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var payload = await reader.ReadToEndAsync();
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync($"echo:{payload}");
        });

        await app.StartAsync();
        var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/echo?mode=inspect")
        {
            Content = new StringContent("""{"hello":"world"}""", Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("traceparent", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01");

        var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, payload);
        Assert.Equal("""echo:{"hello":"world"}""", payload);
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3200 &&
            entry.Message.Contains("POST", StringComparison.Ordinal) &&
            entry.Message.Contains("/echo", StringComparison.Ordinal) &&
            entry.Message.Contains("4bf92f3577b34da6a3ce929d0e0e4736", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3201 &&
            entry.Message.Contains("""{"hello":"world"}""", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3202 &&
            entry.Message.Contains("200", StringComparison.Ordinal) &&
            entry.Message.Contains("4bf92f3577b34da6a3ce929d0e0e4736", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3203 &&
            entry.Message.Contains("""echo:{"hello":"world"}""", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonTruncatesLoggedHttpBodiesToConfiguredLimits()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:LogRequestBody"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:LogResponseBody"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:RequestBodyLimit"] = "16";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:ResponseBodyLimit"] = "12";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();
        app.MapPost("/echo", async context =>
        {
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync($"echo:{requestBody}");
        });

        await app.StartAsync();
        var client = app.GetTestClient();
        var requestBody = "secret-token-abcdefghijklmnopqrstuvwxyz";
        var responseBody = $"echo:{requestBody}";
        var expectedRequestPrefix = requestBody[..16];
        var expectedResponsePrefix = responseBody[..12];

        using var request = new HttpRequestMessage(HttpMethod.Post, "/echo?mode=truncate")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "text/plain")
        };

        using var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, payload);
        Assert.Equal(responseBody, payload);
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3201 &&
            entry.Message.Contains("Truncated True", StringComparison.Ordinal) &&
            entry.Message.Contains(expectedRequestPrefix, StringComparison.Ordinal) &&
            !entry.Message.Contains(requestBody, StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3203 &&
            entry.Message.Contains("Truncated True", StringComparison.Ordinal) &&
            entry.Message.Contains(expectedResponsePrefix, StringComparison.Ordinal) &&
            !entry.Message.Contains(responseBody, StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonDoesNotLogBinaryRequestOrResponseBodies()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:LogRequestBody"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:LogResponseBody"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();
        app.MapPost("/binary", async context =>
        {
            context.Response.ContentType = "application/octet-stream";
            await context.Request.Body.CopyToAsync(context.Response.Body);
        });

        await app.StartAsync();
        var client = app.GetTestClient();
        var requestBody = "binary-secret-token";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/binary?mode=binary");
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(requestBody));
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

        using var response = await client.SendAsync(request);
        var payload = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());

        Assert.True(response.IsSuccessStatusCode, payload);
        Assert.Equal(requestBody, payload);
        Assert.DoesNotContain(loggerProvider.Entries, entry => entry.EventId.Id == 3201);
        Assert.DoesNotContain(loggerProvider.Entries, entry => entry.EventId.Id == 3203);
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3200 &&
            entry.Message.Contains("/binary", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3202 &&
            entry.Message.Contains("/binary", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonRedactsSensitiveQueryStringAndJsonBodiesByDefault()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:LogRequestBody"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:LogResponseBody"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();
        app.MapPost("/echo-json", async context =>
        {
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(requestBody);
        });

        await app.StartAsync();
        var client = app.GetTestClient();
        const string requestBody = """{"username":"codex","password":"s3cr3t","profile":{"apiKey":"abc123"},"nested":{"secret":"hidden"},"items":[{"token":"item-secret"}]}""";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/echo-json?token=query-secret&mode=inspect")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, payload);
        Assert.Equal(requestBody, payload);
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3200 &&
            entry.Message.Contains("token=[REDACTED]", StringComparison.Ordinal) &&
            !entry.Message.Contains("query-secret", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3201 &&
            entry.Message.Contains(@"""password"":""[REDACTED]""", StringComparison.Ordinal) &&
            entry.Message.Contains(@"""apiKey"":""[REDACTED]""", StringComparison.Ordinal) &&
            entry.Message.Contains(@"""secret"":""[REDACTED]""", StringComparison.Ordinal) &&
            entry.Message.Contains(@"""token"":""[REDACTED]""", StringComparison.Ordinal) &&
            !entry.Message.Contains("s3cr3t", StringComparison.Ordinal) &&
            !entry.Message.Contains("abc123", StringComparison.Ordinal) &&
            !entry.Message.Contains("hidden", StringComparison.Ordinal) &&
            !entry.Message.Contains("item-secret", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3203 &&
            entry.Message.Contains(@"""password"":""[REDACTED]""", StringComparison.Ordinal) &&
            !entry.Message.Contains("s3cr3t", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonSupportsCustomRedactionKeysAndPlaceholder()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:LogRequestBody"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:LogResponseBody"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:RedactedFieldNames:0"] = "tenantKey";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:RedactionValue"] = "***";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();
        app.MapPost("/echo-json", async context =>
        {
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(requestBody);
        });

        await app.StartAsync();
        var client = app.GetTestClient();
        const string requestBody = """{"tenantKey":"body-secret","password":"still-visible"}""";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/echo-json?tenantKey=query-secret&mode=inspect")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, payload);
        Assert.Equal(requestBody, payload);
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3200 &&
            entry.Message.Contains("tenantKey=***", StringComparison.Ordinal) &&
            !entry.Message.Contains("query-secret", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3201 &&
            entry.Message.Contains(@"""tenantKey"":""***""", StringComparison.Ordinal) &&
            entry.Message.Contains(@"""password"":""still-visible""", StringComparison.Ordinal) &&
            !entry.Message.Contains("body-secret", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonRedactsSensitiveTextBodiesWithHeaderStyleAndAssignmentPayloads()
    {
        var loggerProvider = new TestLoggerProvider();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:LogRequestBody"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Observability:HttpLogging:LogResponseBody"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();
        app.MapPost("/echo-text", async context =>
        {
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            await reader.ReadToEndAsync();

            const string responseBody = """
Authorization: Bearer response-secret
Cookie: session=response-cookie; Path=/
password=response-password
note: visible
""";
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(responseBody.ReplaceLineEndings("\r\n"));
        });

        await app.StartAsync();
        var client = app.GetTestClient();
        const string requestBody = """
Authorization: Bearer request-secret
password=request-password
note: visible
""";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/echo-text?authorization=query-secret&mode=inspect")
        {
            Content = new StringContent(requestBody.ReplaceLineEndings("\r\n"), Encoding.UTF8, "text/plain")
        };

        using var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, payload);
        Assert.Contains("response-secret", payload, StringComparison.Ordinal);
        Assert.Contains("response-password", payload, StringComparison.Ordinal);
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3200 &&
            entry.Message.Contains("authorization=[REDACTED]", StringComparison.Ordinal) &&
            !entry.Message.Contains("query-secret", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3201 &&
            entry.Message.Contains("Authorization: [REDACTED]", StringComparison.Ordinal) &&
            entry.Message.Contains("password=[REDACTED]", StringComparison.Ordinal) &&
            entry.Message.Contains("note: visible", StringComparison.Ordinal) &&
            !entry.Message.Contains("request-secret", StringComparison.Ordinal) &&
            !entry.Message.Contains("request-password", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, entry =>
            entry.EventId.Id == 3203 &&
            entry.Message.Contains("Authorization: [REDACTED]", StringComparison.Ordinal) &&
            entry.Message.Contains("Cookie: [REDACTED]", StringComparison.Ordinal) &&
            entry.Message.Contains("password=[REDACTED]", StringComparison.Ordinal) &&
            entry.Message.Contains("note: visible", StringComparison.Ordinal) &&
            !entry.Message.Contains("response-secret", StringComparison.Ordinal) &&
            !entry.Message.Contains("response-cookie", StringComparison.Ordinal) &&
            !entry.Message.Contains("response-password", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MapCephalonExposesRuntimeStoryAcrossDedicatedRouteAndSnapshot()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new WorkflowCatalogTestModule("hosting-runtime-story"));
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var story = await client.GetFromJsonAsync<RuntimeOperationalStory>("/engine/runtime-story");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(story);
        Assert.Equal(RuntimeStatus.Started, story.Status.Status);
        Assert.Equal(2, story.Modules.Count);
        var platform = Assert.Single(story.Modules, module => module.ModuleId == "platform");
        Assert.True(platform.IsLoaded);
        Assert.True(platform.IsInitialized);
        Assert.True(platform.IsStarted);
        Assert.False(platform.IsStopped);
        var approvalPump = Assert.Single(story.HostedExecutions, execution => execution.HostedExecutionId == "approval-pump");
        Assert.True(approvalPump.IsLoaded);
        Assert.True(approvalPump.IsActive);
        Assert.False(approvalPump.IsDeactivated);
        Assert.Equal("workflow-catalog", approvalPump.SourceModuleId);
        Assert.Equal("approval-flow", approvalPump.ExecutionGraphId);
        var approvalFlow = Assert.Single(story.ExecutionGraphs, graph => graph.GraphId == "approval-flow");
        Assert.True(approvalFlow.IsLoaded);
        Assert.True(approvalFlow.IsActive);
        Assert.False(approvalFlow.IsDeactivated);
        Assert.Equal("workflow-catalog", approvalFlow.SourceModuleId);
        Assert.Equal("1.0.0", approvalFlow.SourceModuleVersion);
        Assert.Contains(
            story.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.Runtime &&
                entry.Phase == "start" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);
        Assert.Contains(
            story.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.ExecutionGraph &&
                entry.SubjectId == "approval-flow" &&
                entry.Phase == "activate" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);
        Assert.Contains(
            story.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.HostedExecution &&
                entry.SubjectId == "approval-pump" &&
                entry.Phase == "activate" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Succeeded);

        Assert.NotNull(snapshot);
        Assert.Equal(RuntimeStatus.Started, snapshot.OperationalStory.Status.Status);
        Assert.Contains(snapshot.OperationalStory.HostedExecutions, execution => execution.HostedExecutionId == "approval-pump" && execution.IsActive);
        Assert.Contains(snapshot.OperationalStory.Modules, module => module.ModuleId == "platform" && module.IsStarted);
        Assert.Contains(snapshot.OperationalStory.ExecutionGraphs, graph => graph.GraphId == "approval-flow" && graph.IsActive);
        Assert.Contains(
            snapshot.OperationalStory.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.Module &&
                entry.SubjectId == "platform" &&
                entry.Phase == "load");
    }

    [Fact]
    public async Task MapCephalonExposesCapturedStartupFailuresWhenPolicyDoesNotFailFast()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:FailurePolicy:StartupFailureBehavior"] = "CaptureOnly";
        builder.Configuration[$"{EngineSettings.SectionName}:FailurePolicy:AllowManualRestart"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:FailurePolicy:ManualRestartBackoff"] = "00:00:02";
        builder.Services.AddSingleton<FailurePolicyRecorder>();
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new FailurePolicyPlatformModule());
            engine.AddModule(new FlakyStartModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var status = await client.GetFromJsonAsync<RuntimeStatusSnapshot>("/engine/status");
        var story = await client.GetFromJsonAsync<RuntimeOperationalStory>("/engine/runtime-story");
        var failurePolicy = await client.GetFromJsonAsync<FailurePolicy>("/engine/failure-policy");
        var diagnosticsResponse = await client.GetAsync("/engine/diagnostics");
        var diagnosticsPayload = await diagnosticsResponse.Content.ReadAsStringAsync();
        var livenessResponse = await client.GetAsync("/health/live");
        var livenessPayload = await livenessResponse.Content.ReadAsStringAsync();
        var readinessResponse = await client.GetAsync("/health/ready");
        var readinessPayload = await readinessResponse.Content.ReadAsStringAsync();

        Assert.NotNull(status);
        Assert.Equal(RuntimeStatus.Failed, status.Status);
        Assert.Equal("flaky-start", status.LastFailure?.ModuleId);
        Assert.Equal("start", status.LastFailure?.Phase);
        Assert.Equal("Simulated startup failure.", status.LastFailure?.Message);
        Assert.True(status.LastFailure?.CanRestart);
        Assert.NotNull(status.LastFailure?.RestartAvailableAtUtc);

        Assert.NotNull(story);
        Assert.Equal(RuntimeStatus.Failed, story.Status.Status);
        Assert.NotNull(story.Status.LastFailure?.RestartAvailableAtUtc);
        var failingModule = Assert.Single(story.Modules, module => module.ModuleId == "flaky-start");
        Assert.Equal("start", failingModule.LastObservedPhase);
        Assert.Equal("Simulated startup failure.", failingModule.LastFailure?.Message);
        Assert.NotNull(failingModule.LastFailure?.RestartAvailableAtUtc);
        Assert.Contains(
            story.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.Module &&
                entry.SubjectId == "flaky-start" &&
                entry.Phase == "start" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Failed);
        Assert.Contains(
            story.Timeline,
            entry => entry.Scope == RuntimeLifecycleEventScope.Runtime &&
                entry.Phase == "start" &&
                entry.Outcome == RuntimeLifecycleEventOutcome.Failed &&
                entry.Message.Contains("Simulated startup failure.", StringComparison.Ordinal));

        Assert.NotNull(failurePolicy);
        Assert.Equal(StartupFailureBehavior.CaptureOnly, failurePolicy.StartupFailureBehavior);
        Assert.Equal(TimeSpan.FromSeconds(2), failurePolicy.ManualRestartBackoff);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, livenessResponse.StatusCode);
        using var livenessDocument = JsonDocument.Parse(livenessPayload);
        Assert.Equal("Unhealthy", livenessDocument.RootElement.GetProperty("status").GetString());
        if (livenessDocument.RootElement.TryGetProperty("entries", out var livenessEntries) &&
            livenessEntries.TryGetProperty("cephalon.liveness", out var livenessEntry) &&
            livenessEntry.TryGetProperty("data", out var livenessData))
        {
            if (livenessData.TryGetProperty("activeWindow", out var livenessActiveWindow))
            {
                Assert.Equal("restart-backoff", livenessActiveWindow.GetString());
                Assert.True(livenessData.TryGetProperty("restartAvailableAtUtc", out _));
            }
        }

        Assert.Equal(HttpStatusCode.ServiceUnavailable, readinessResponse.StatusCode);
        using var readinessDocument = JsonDocument.Parse(readinessPayload);
        Assert.Equal("Unhealthy", readinessDocument.RootElement.GetProperty("status").GetString());

        Assert.True(diagnosticsResponse.IsSuccessStatusCode);
        using var diagnosticsDocument = JsonDocument.Parse(diagnosticsPayload);
        Assert.Equal((int)RuntimeHealthState.Unhealthy, diagnosticsDocument.RootElement.GetProperty("liveness").GetProperty("state").GetInt32());
        Assert.Equal((int)RuntimeHealthState.Unhealthy, diagnosticsDocument.RootElement.GetProperty("readiness").GetProperty("state").GetInt32());
        if (diagnosticsDocument.RootElement.GetProperty("liveness").TryGetProperty("activeWindow", out var activeWindow))
        {
            Assert.Equal("restart-backoff", activeWindow.GetString());
        }
    }

    [Fact]
    public async Task MapCephalonKeepsReadinessUnhealthyDuringConfiguredStartupWarmup()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:FailurePolicy:StartupReadinessDelay"] = "00:00:02";
        builder.Services.AddSingleton<FailurePolicyRecorder>();
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new FailurePolicyPlatformModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var failurePolicy = await client.GetFromJsonAsync<FailurePolicy>("/engine/failure-policy");
        var diagnosticsResponse = await client.GetAsync("/engine/diagnostics");
        var diagnosticsPayload = await diagnosticsResponse.Content.ReadAsStringAsync();
        var readinessResponse = await client.GetAsync("/health/ready");
        var readinessPayload = await readinessResponse.Content.ReadAsStringAsync();

        Assert.NotNull(failurePolicy);
        Assert.Equal(TimeSpan.FromSeconds(2), failurePolicy.StartupReadinessDelay);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, readinessResponse.StatusCode);
        using var readinessDocument = JsonDocument.Parse(readinessPayload);
        Assert.Equal("Unhealthy", readinessDocument.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "startup-warmup",
            readinessDocument.RootElement
                .GetProperty("entries")
                .GetProperty("cephalon.readiness")
                .GetProperty("data")
                .GetProperty("activeWindow")
                .GetString());

        Assert.True(diagnosticsResponse.IsSuccessStatusCode);
        using var diagnosticsDocument = JsonDocument.Parse(diagnosticsPayload);
        Assert.Equal("startup-warmup", diagnosticsDocument.RootElement.GetProperty("readiness").GetProperty("activeWindow").GetString());

        await Task.Delay(TimeSpan.FromMilliseconds(2250));

        var readyResponse = await client.GetAsync("/health/ready");
        var readyPayload = await readyResponse.Content.ReadAsStringAsync();

        Assert.True(readyResponse.IsSuccessStatusCode, readyPayload);
        using var readyDocument = JsonDocument.Parse(readyPayload);
        Assert.Equal("Healthy", readyDocument.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task MapCephalonMapsOnlyConfiguredTransportRoutes()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "JsonRpc";
        builder.AddJsonRpcTransport();
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var restResponse = await client.GetAsync("/api/discovery/hello/Codex");
        var openApiResponse = await client.GetAsync("/openapi/v1.json");
        var scalarConfigResponse = await client.GetAsync("/scalar/openapi-toggle.js");
        var scalarFaviconResponse = await client.GetAsync("/scalar/assets/favicon.svg");
        var scalarResponse = await client.GetAsync("/scalar/v1");
        var rpcResponse = await client.PostAsJsonAsync("/json-rpc/discovery", new
        {
            jsonRpc = "2.0",
            method = "discovery.hello",
            @params = new Dictionary<string, string?> { ["name"] = "ConfigOnly" },
            id = "req-2"
        });

        Assert.Equal(System.Net.HttpStatusCode.NotFound, restResponse.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, openApiResponse.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, scalarConfigResponse.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, scalarFaviconResponse.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, scalarResponse.StatusCode);
        Assert.True(rpcResponse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task MapCephalonSupportsLocalizedDocsAndLanguageOverrides()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Localization:DefaultCulture"] = "th";
        builder.Configuration[$"{EngineSettings.SectionName}:Localization:SupportedCultures:0"] = "en";
        builder.Configuration[$"{EngineSettings.SectionName}:Localization:SupportedCultures:1"] = "th";
        builder.Configuration[$"{EngineSettings.SectionName}:Localization:Resources:th:engine.docs.rest.title"] = "เอกสาร REST ของ Cephalon";
        builder.Configuration[$"{EngineSettings.SectionName}:Localization:Resources:th:engine.docs.rest.description"] = "พื้นผิว REST ภาษาไทย";
        builder.AddCephalon(engine =>
        {
            engine.AddLanguageResources("ja", new Dictionary<string, string>
            {
                ["engine.docs.rest.title"] = "Cephalon REST API 日本語"
            });
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var thaiSnapshot = await client.GetFromJsonAsync<LocalizedResourcesSnapshot>("/engine/localization?culture=th");
        var japaneseSnapshot = await client.GetFromJsonAsync<LocalizedResourcesSnapshot>("/engine/localization?culture=ja");
        var thaiOpenApiPayload = await client.GetStringAsync("/openapi/v1.json?culture=th&ui-culture=th");
        var japaneseOpenApiPayload = await client.GetStringAsync("/openapi/v1.json?culture=ja&ui-culture=ja");
        var thaiScalarResponse = await client.GetAsync("/scalar/v1?culture=th&ui-culture=th");

        Assert.NotNull(thaiSnapshot);
        Assert.Equal("th", thaiSnapshot.DefaultCulture);
        Assert.Equal("th", thaiSnapshot.ResolvedCulture);
        Assert.Equal("เอกสาร REST ของ Cephalon", thaiSnapshot.Resources["engine.docs.rest.title"]);
        Assert.Contains("ja", thaiSnapshot.SupportedCultures);

        Assert.NotNull(japaneseSnapshot);
        Assert.Equal("ja", japaneseSnapshot.ResolvedCulture);
        Assert.Equal("Cephalon REST API 日本語", japaneseSnapshot.Resources["engine.docs.rest.title"]);

        using var thaiOpenApiDocument = JsonDocument.Parse(thaiOpenApiPayload);
        Assert.Equal("เอกสาร REST ของ Cephalon", thaiOpenApiDocument.RootElement.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("พื้นผิว REST ภาษาไทย", thaiOpenApiDocument.RootElement.GetProperty("info").GetProperty("description").GetString());

        using var japaneseOpenApiDocument = JsonDocument.Parse(japaneseOpenApiPayload);
        Assert.Equal("Cephalon REST API 日本語", japaneseOpenApiDocument.RootElement.GetProperty("info").GetProperty("title").GetString());

        Assert.True(thaiScalarResponse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task MapCephalonExposesConfiguredPackagePolicy()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:AllowAssemblyPathPackages"] = "false";
        builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:RequireVersion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:RequireMinimumEngineVersion"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:RequireSupportedTargetFrameworks"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:RequirePublisherId"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:RequireSignatureFingerprint"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:RequireSignatureKeyId"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:RequireSignatureValue"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:RequireSignatureVerification"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:RequireIntegritySha256"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var packagePolicy = await client.GetFromJsonAsync<PackagePolicy>("/engine/package-policy");

        Assert.NotNull(packagePolicy);
        Assert.False(packagePolicy.AllowAssemblyPathPackages);
        Assert.True(packagePolicy.RequireVersion);
        Assert.True(packagePolicy.RequireMinimumEngineVersion);
        Assert.True(packagePolicy.RequireSupportedTargetFrameworks);
        Assert.True(packagePolicy.RequirePublisherId);
        Assert.True(packagePolicy.RequireSignatureFingerprint);
        Assert.True(packagePolicy.RequireSignatureKeyId);
        Assert.True(packagePolicy.RequireSignatureValue);
        Assert.True(packagePolicy.RequireSignatureVerification);
        Assert.True(packagePolicy.RequireIntegritySha256);
    }

    [Fact]
    public async Task MapCephalonUsesModuleLanguagePacksWhenProjectsDoNotOverride()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new LocalizationPackTestModule());
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var spanishSnapshot = await client.GetFromJsonAsync<LocalizedResourcesSnapshot>("/engine/localization?culture=es");
        var spanishOpenApiPayload = await client.GetStringAsync("/openapi/v1.json?culture=es&ui-culture=es");

        Assert.NotNull(spanishSnapshot);
        Assert.Equal("es", spanishSnapshot.ResolvedCulture);
        Assert.Equal("API REST de Cephalon", spanishSnapshot.Resources["engine.docs.rest.title"]);
        Assert.Equal(
            "Superficie REST expuesta por el host ASP.NET Core de Cephalon.",
            spanishSnapshot.Resources["engine.docs.rest.description"]);

        using var spanishOpenApiDocument = JsonDocument.Parse(spanishOpenApiPayload);
        Assert.Equal("API REST de Cephalon", spanishOpenApiDocument.RootElement.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal(
            "Superficie REST expuesta por el host ASP.NET Core de Cephalon.",
            spanishOpenApiDocument.RootElement.GetProperty("info").GetProperty("description").GetString());
    }

    [Fact]
    public async Task MapCephalonExposesDependencyHealthAcrossDiagnosticsAndReadiness()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new DependencyHealthModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var dependencies = await client.GetFromJsonAsync<DependencyHealthReport[]>("/engine/dependencies");
        var diagnosticsResponse = await client.GetAsync("/engine/diagnostics");
        var diagnosticsPayload = await diagnosticsResponse.Content.ReadAsStringAsync();
        var livenessResponse = await client.GetAsync("/health/live");
        var livenessPayload = await livenessResponse.Content.ReadAsStringAsync();
        var readinessResponse = await client.GetAsync("/health/ready");
        var readinessPayload = await readinessResponse.Content.ReadAsStringAsync();

        Assert.NotNull(dependencies);
        Assert.Equal(2, dependencies.Length);
        Assert.Contains(dependencies, dependency =>
            dependency.Id == "primary-sql" &&
            dependency.State == HealthState.Unhealthy &&
            dependency.Required);
        Assert.Contains(dependencies, dependency =>
            dependency.Id == "search-index" &&
            dependency.State == HealthState.Degraded &&
            !dependency.Required);

        Assert.True(diagnosticsResponse.IsSuccessStatusCode);
        using var diagnosticsDocument = JsonDocument.Parse(diagnosticsPayload);
        Assert.Equal((int)RuntimeHealthState.Degraded, diagnosticsDocument.RootElement.GetProperty("liveness").GetProperty("state").GetInt32());
        Assert.Equal((int)RuntimeHealthState.Unhealthy, diagnosticsDocument.RootElement.GetProperty("readiness").GetProperty("state").GetInt32());
        Assert.Equal(2, diagnosticsDocument.RootElement.GetProperty("readiness").GetProperty("dependencies").GetArrayLength());

        Assert.True(livenessResponse.IsSuccessStatusCode, livenessPayload);
        using var livenessDocument = JsonDocument.Parse(livenessPayload);
        Assert.Equal("Degraded", livenessDocument.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            2,
            livenessDocument.RootElement
                .GetProperty("entries")
                .GetProperty("cephalon.liveness")
                .GetProperty("data")
                .GetProperty("dependencyCount")
                .GetInt32());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, readinessResponse.StatusCode);
        using var readinessDocument = JsonDocument.Parse(readinessPayload);
        Assert.Equal("Unhealthy", readinessDocument.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "primary-sql",
            readinessDocument.RootElement
                .GetProperty("entries")
                .GetProperty("cephalon.readiness")
                .GetProperty("data")
                .GetProperty("dependencies")[0]
                .GetProperty("id")
                .GetString());
    }

    [Fact]
    public async Task MapCephalonFailsFastWhenTransportAdapterIsMissing()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "JsonRpc";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("json-rpc", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonFailsFastWhenGraphQLTransportAdapterIsMissing()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "GraphQL";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("graphql", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonLoadsRestModulePackagesFromConfiguredAssemblyPaths()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Discovery:Packages:0:Id"] = "reference-operations";
        builder.Configuration[$"{EngineSettings.SectionName}:Discovery:Packages:0:Path"] = GetReferenceModuleAssemblyPath();
        builder.AddCephalon();

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var packages = await client.GetFromJsonAsync<PackageManifest[]>("/engine/packages");
        var manifest = await client.GetFromJsonAsync<RuntimeManifest>("/engine");
        var operationsStatus = await client.GetStringAsync("/api/operations/status");

        Assert.NotNull(packages);
        var package = Assert.Single(packages);
        Assert.Equal("reference-operations", package.Id);
        Assert.Equal(ModulePackageReference.AssemblyPathKind, package.Kind);
        Assert.Equal(package.Path, package.SourcePath);
        Assert.False(string.IsNullOrWhiteSpace(package.ChecksumSha256));
        Assert.Null(package.PublisherId);
        Assert.Null(package.Distribution);
        Assert.Null(package.Provenance);
        Assert.Null(package.SignatureKeyId);
        Assert.Null(package.SignatureFingerprint);
        Assert.Empty(package.Signatures);
        Assert.Empty(package.Dependencies);
        Assert.False(package.IsSignatureVerified);
        Assert.Equal("Package did not declare a cryptographic signature.", package.SignatureVerificationReason);
        Assert.Contains("operations", package.Modules);

        Assert.NotNull(manifest);
        Assert.Contains(manifest.Modules, module =>
            module.Id == "operations" &&
            module.PackageId == "reference-operations");
        Assert.Contains("Operations module is running.", operationsStatus, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonExposesCdcCaptureRuntimeStateRoutesAndSnapshot()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Services.AddSingleton<IEventDispatchRuntimeCatalog>(new TestEventDispatchRuntimeCatalog(
            new EventDispatchRuntimeState(
                OutboxId: "tenant-event-outbox",
                LastChannelId: "tenant-events",
                LastOutcome: "succeeded",
                LastObservedAtUtc: DateTimeOffset.Parse("2026-04-20T09:45:00Z", CultureInfo.InvariantCulture),
                LastMessageId: "dispatch-002",
                LastAttempt: 1,
                StartedCount: 1,
                SucceededCount: 1,
                FailedCount: 0,
                RetryScheduledCount: 0,
                SkippedCount: 0,
                LastError: null,
                Metadata: new Dictionary<string, string>
                {
                    ["dispatchRuntime"] = "phase13"
                })));
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData();
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var reporter = app.Services.GetRequiredService<ICdcCaptureRuntimeReporter>();
        await reporter.ReportAsync(new CdcCaptureExecutionReport(
            cdcCaptureId: "tenant-profile-cdc",
            outcome: CdcCaptureRuntimeOutcomes.Captured,
            observedAtUtc: DateTimeOffset.Parse("2026-04-20T10:15:00Z", CultureInfo.InvariantCulture),
            capturedChangeCount: 4,
            producedMessageCount: 4,
            changeId: "lsn-0004",
            checkpoint: "0/16B6C90",
            freshness: new CdcCaptureFreshnessStatus(
                CdcCaptureFreshnessStates.Fresh,
                DateTimeOffset.Parse("2026-04-20T10:20:00Z", CultureInfo.InvariantCulture),
                "The capture is still within the expected freshness window."),
            lag: new CdcCaptureLagStatus(
                CdcCaptureLagStates.Current,
                pendingChangeCount: 0,
                description: "The capture is caught up with the source stream."),
            publication: new CdcCapturePublicationStatus(
                CdcCapturePublicationStates.Current,
                pendingPublicationCount: 0,
                description: "The capture does not report any pending publications."),
            metadata: new Dictionary<string, string>
            {
                ["captureRuntime"] = "phase13"
            }));

        var client = app.GetTestClient();
        var states = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>("/engine/cdc-captures/runtime");
        var state = await client.GetFromJsonAsync<CdcCaptureRuntimeState>("/engine/cdc-captures/runtime/tenant-profile-cdc");
        var statesByModule = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>("/engine/cdc-captures/runtime/modules/phase8-runtime-catalogs");
        var statesByProvider = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>("/engine/cdc-captures/runtime/providers/postgresql");
        var statesByOutbox = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>("/engine/cdc-captures/runtime/outboxes/tenant-event-outbox");
        var statesBySource = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>("/engine/cdc-captures/runtime/sources/tenant-db");
        var statesByResource = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>("/engine/cdc-captures/runtime/resources/public.tenants");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(states);
        var runtimeState = Assert.Single(states);
        Assert.Equal("tenant-profile-cdc", runtimeState.CdcCaptureId);
        Assert.NotNull(state);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
        Assert.Equal(4, state.LastCapturedChangeCount);
        Assert.Equal(4, state.TotalProducedMessageCount);
        Assert.Equal("lsn-0004", state.LastChangeId);
        Assert.Equal("0/16B6C90", state.LastCheckpoint);
        Assert.Equal("phase13", state.Metadata["captureRuntime"]);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, state.Freshness.State);
        Assert.Equal(DateTimeOffset.Parse("2026-04-20T10:20:00Z", CultureInfo.InvariantCulture), state.Freshness.FreshUntilUtc);
        Assert.Equal(CdcCaptureLagStates.Current, state.Lag.State);
        Assert.Equal(0, state.Lag.PendingChangeCount);
        Assert.Equal(CdcCapturePublicationStates.Current, state.Publication.State);
        Assert.Equal(0, state.Publication.PendingPublicationCount);
        Assert.True(state.HasFreshnessWindow);
        Assert.False(state.HasPendingChanges);
        Assert.False(state.HasPendingPublications);
        Assert.NotNull(state.OutboxDispatchState);
        Assert.Equal("succeeded", state.OutboxDispatchState!.LastOutcome);
        Assert.NotNull(statesByModule);
        Assert.Single(statesByModule);
        Assert.NotNull(statesByProvider);
        Assert.Single(statesByProvider);
        Assert.NotNull(statesByOutbox);
        Assert.Single(statesByOutbox);
        Assert.NotNull(statesBySource);
        Assert.Single(statesBySource);
        Assert.NotNull(statesByResource);
        Assert.Single(statesByResource);
        Assert.NotNull(snapshot);
        var snapshotState = Assert.Single(snapshot.CdcCaptureStates);
        Assert.Equal("tenant-profile-cdc", snapshotState.CdcCaptureId);
        Assert.Equal(CdcCapturePublicationStates.Current, snapshotState.Publication.State);
        Assert.NotNull(snapshotState.OutboxDispatchState);
    }

    [Fact]
    public async Task MapCephalonExposesSharedCdcExecutionSurfacesAndRuntimeStory()
    {
        var executionState = new TestCdcExecutionState();
        executionState.EnqueueResult(new CdcCaptureExecutionResult(
            messages:
            [
                new OutboxMessage(
                    id: "cdc-msg-002",
                    channelId: "tenant-events",
                    messageType: "tenant.profile.changed",
                    payload: """{"tenantId":"tenant-002"}""",
                    occurredAtUtc: DateTimeOffset.Parse("2026-04-20T10:45:00Z", CultureInfo.InvariantCulture))
            ],
            freshness: new CdcCaptureFreshnessStatus(
                CdcCaptureFreshnessStates.Fresh,
                DateTimeOffset.Parse("2026-04-20T10:50:00Z", CultureInfo.InvariantCulture),
                "The capture is still within the expected freshness window."),
            lag: new CdcCaptureLagStatus(
                CdcCaptureLagStates.Current,
                pendingChangeCount: 0,
                description: "The capture is caught up with the source stream."),
            metadata: new Dictionary<string, string>
            {
                ["captureRuntime"] = "phase13-shared-hosting"
            }));

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Services.AddSingleton(executionState);
        builder.Services.AddScoped<ICdcCapture, TestCdcCapture>();
        builder.Services.AddScoped<IOutbox, TestOutbox>();
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(options =>
            {
                options.EnableCdcExecution = true;
                options.CdcPollingIntervalSeconds = 600;
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await executionState.WaitForStagedMessageAsync(timeout.Token);

        var client = app.GetTestClient();
        var hostedExecutions = await client.GetFromJsonAsync<HostedExecutionDescriptor[]>("/engine/hosted-executions");
        var executionGraphs = await client.GetFromJsonAsync<ExecutionGraphDescriptor[]>("/engine/execution-graphs");
        var cdcCaptureRuntimes = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes");
        var cdcCaptureRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/data-cdc-capture-pump");
        var cdcCapturesByRuntime = await client.GetFromJsonAsync<CdcCaptureDescriptor[]>("/engine/cdc-captures/execution-runtimes/data-cdc-capture-pump");
        var cdcCaptureStatesByRuntime = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>("/engine/cdc-captures/runtime/execution-runtimes/data-cdc-capture-pump");
        var story = await client.GetFromJsonAsync<RuntimeOperationalStory>("/engine/runtime-story");
        var state = await client.GetFromJsonAsync<CdcCaptureRuntimeState>("/engine/cdc-captures/runtime/tenant-profile-cdc");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(hostedExecutions);
        var hostedExecution = Assert.Single(hostedExecutions, item => item.Id == "data-cdc-capture-pump");
        Assert.Equal("data-runtime", hostedExecution.SourceModuleId);
        Assert.Equal("data-cdc-capture-flow", hostedExecution.ExecutionGraphId);

        Assert.NotNull(executionGraphs);
        var executionGraph = Assert.Single(executionGraphs, item => item.Id == "data-cdc-capture-flow");
        Assert.Equal("data-runtime", executionGraph.SourceModuleId);
        Assert.Equal("resolve-cdc-captures", executionGraph.EntryNodeId);
        Assert.Equal(5, executionGraph.Nodes.Count);
        Assert.Contains(executionGraph.Nodes, item => item.Id == "acknowledge-cdc-progress");

        Assert.NotNull(cdcCaptureRuntimes);
        var runtimeDescriptor = Assert.Single(cdcCaptureRuntimes);
        Assert.Equal("data-cdc-capture-pump", runtimeDescriptor.Id);
        Assert.Contains("tenant-profile-cdc", runtimeDescriptor.CdcCaptureIds);
        Assert.Equal("host-managed", runtimeDescriptor.ExecutionOwnership);
        Assert.Equal("shared-in-process-polling", runtimeDescriptor.ExecutionTopology);
        Assert.Equal("post-stage-provider", runtimeDescriptor.AcknowledgementMode);
        Assert.Equal("data-cdc-capture-pump", runtimeDescriptor.HostedExecutionId);
        Assert.Equal("data-cdc-capture-flow", runtimeDescriptor.ExecutionGraphId);
        Assert.NotNull(cdcCaptureRuntime);
        Assert.Equal("data-cdc-capture-pump", cdcCaptureRuntime.Id);
        Assert.Equal("host-managed", cdcCaptureRuntime.ExecutionOwnership);
        Assert.Equal("shared-in-process-polling", cdcCaptureRuntime.ExecutionTopology);
        Assert.True(cdcCaptureRuntime.Summary.HasReports);
        Assert.Equal("tenant-profile-cdc", cdcCaptureRuntime.Summary.LastCdcCaptureId);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, cdcCaptureRuntime.Summary.LastOutcome);
        Assert.Equal(1, cdcCaptureRuntime.Summary.TotalProducedMessageCount);
        Assert.Equal("not-required", cdcCaptureRuntime.Summary.LastAcknowledgement);
        var cdcCapture = Assert.Single(cdcCapturesByRuntime!);
        Assert.Equal("tenant-profile-cdc", cdcCapture.Id);
        Assert.Equal("data-cdc-capture-pump", cdcCapture.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("shared-in-process-polling", cdcCapture.ExecutionBinding.ExecutionTopology);
        Assert.Equal("default-shared-runtime", cdcCapture.ExecutionBinding.ResolutionMode);
        var cdcCaptureStateByRuntime = Assert.Single(cdcCaptureStatesByRuntime!);
        Assert.Equal("tenant-profile-cdc", cdcCaptureStateByRuntime.CdcCaptureId);
        Assert.Equal("data-cdc-capture-pump", cdcCaptureStateByRuntime.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("shared-in-process-polling", cdcCaptureStateByRuntime.ExecutionBinding.ExecutionTopology);

        Assert.NotNull(story);
        var storyHostedExecution = Assert.Single(story.HostedExecutions, item => item.HostedExecutionId == "data-cdc-capture-pump");
        Assert.True(storyHostedExecution.IsActive);
        Assert.Equal("data-cdc-capture-flow", storyHostedExecution.ExecutionGraphId);
        var storyExecutionGraph = Assert.Single(story.ExecutionGraphs, item => item.GraphId == "data-cdc-capture-flow");
        Assert.True(storyExecutionGraph.IsActive);

        Assert.NotNull(state);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
        Assert.Equal(1, state.LastProducedMessageCount);
        Assert.Equal("shared-data-runtime", state.Metadata["captureExecution"]);
        Assert.Equal("phase13-shared-hosting", state.Metadata["captureRuntime"]);
        Assert.Equal("not-required", state.Metadata["acknowledgement"]);
        Assert.Equal("data-cdc-capture-pump", state.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("host-managed", state.ExecutionBinding.ExecutionOwnership);
        Assert.Equal(CdcCapturePublicationStates.PendingPublication, state.Publication.State);
        Assert.Equal(1, state.Publication.PendingPublicationCount);

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.HostedExecutions, item => item.Id == "data-cdc-capture-pump");
        Assert.Contains(snapshot.ExecutionGraphs, item => item.Id == "data-cdc-capture-flow");
        Assert.Contains(snapshot.OperationalStory.HostedExecutions, item => item.HostedExecutionId == "data-cdc-capture-pump" && item.IsActive);
        Assert.Contains(snapshot.CdcCaptureStates, item => item.CdcCaptureId == "tenant-profile-cdc" && item.LastProducedMessageCount == 1);
        Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == "data-cdc-capture-pump" && item.Summary.TotalProducedMessageCount == 1);
    }

    [Fact]
    public async Task MapCephalonExposesConfiguredProviderNativeCdcExecutionRuntimeSurfaces()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(options =>
            {
                var runtime = new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "Configured External CDC Runtime",
                    Description = "Represents a provider-native CDC runner declared by the host.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "provider-native",
                    AcknowledgementMode = "provider-native"
                };
                runtime.CdcCaptureIds.Add("tenant-profile-cdc");
                options.CdcExecutionRuntimes.Add(runtime);
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var cdcCaptureRuntimes = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes");
        var cdcCaptureRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");
        var cdcCapturesByRuntime = await client.GetFromJsonAsync<CdcCaptureDescriptor[]>("/engine/cdc-captures/execution-runtimes/external-cdc-runtime");
        var cdcCaptureStatesByRuntime = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>("/engine/cdc-captures/runtime/execution-runtimes/external-cdc-runtime");
        var cdcCapture = await client.GetFromJsonAsync<CdcCaptureDescriptor>("/engine/cdc-captures/tenant-profile-cdc");
        var cdcCaptureState = await client.GetFromJsonAsync<CdcCaptureRuntimeState>("/engine/cdc-captures/runtime/tenant-profile-cdc");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(cdcCaptureRuntimes);
        var runtimeDescriptor = Assert.Single(cdcCaptureRuntimes);
        Assert.Equal("external-cdc-runtime", runtimeDescriptor.Id);
        Assert.Equal("external-managed", runtimeDescriptor.ExecutionOwnership);
        Assert.Equal("provider-native", runtimeDescriptor.ExecutionTopology);
        Assert.Equal("provider-native", runtimeDescriptor.AcknowledgementMode);
        Assert.Equal(["tenant-profile-cdc"], runtimeDescriptor.CdcCaptureIds);
        Assert.False(runtimeDescriptor.Summary.HasReports);

        Assert.NotNull(cdcCaptureRuntime);
        Assert.Equal("external-cdc-runtime", cdcCaptureRuntime.Id);
        Assert.Equal("external-managed", cdcCaptureRuntime.ExecutionOwnership);
        Assert.Equal("provider-native", cdcCaptureRuntime.ExecutionTopology);
        Assert.Equal("provider-native", cdcCaptureRuntime.AcknowledgementMode);
        Assert.Equal(["tenant-profile-cdc"], cdcCaptureRuntime.CdcCaptureIds);
        Assert.False(cdcCaptureRuntime.Summary.HasReports);

        var captureByRuntime = Assert.Single(cdcCapturesByRuntime!);
        Assert.Equal("tenant-profile-cdc", captureByRuntime.Id);
        Assert.Equal("external-cdc-runtime", captureByRuntime.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("external-managed", captureByRuntime.ExecutionBinding.ExecutionOwnership);
        Assert.Equal("provider-native", captureByRuntime.ExecutionBinding.ExecutionTopology);
        Assert.Equal("runtime-claim", captureByRuntime.ExecutionBinding.ResolutionMode);

        var captureStateByRuntime = Assert.Single(cdcCaptureStatesByRuntime!);
        Assert.Equal("tenant-profile-cdc", captureStateByRuntime.CdcCaptureId);
        Assert.Equal("external-cdc-runtime", captureStateByRuntime.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("provider-native", captureStateByRuntime.ExecutionBinding.ExecutionTopology);
        Assert.False(captureStateByRuntime.HasReports);

        Assert.NotNull(cdcCapture);
        Assert.Equal("external-cdc-runtime", cdcCapture.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("provider-native", cdcCapture.ExecutionBinding.ExecutionTopology);

        Assert.NotNull(cdcCaptureState);
        Assert.Equal("external-cdc-runtime", cdcCaptureState.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("provider-native", cdcCaptureState.ExecutionBinding.ExecutionTopology);
        Assert.False(cdcCaptureState.HasReports);

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.CdcCaptures, item => item.Id == "tenant-profile-cdc" &&
            item.ExecutionBinding.EffectiveExecutionRuntimeId == "external-cdc-runtime" &&
            item.ExecutionBinding.ExecutionTopology == "provider-native");
        Assert.Contains(snapshot.CdcCaptureStates, item => item.CdcCaptureId == "tenant-profile-cdc" &&
            item.ExecutionBinding.EffectiveExecutionRuntimeId == "external-cdc-runtime" &&
            item.ExecutionBinding.ExecutionTopology == "provider-native");
        Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item => item.Id == "external-cdc-runtime" &&
            item.ExecutionOwnership == "external-managed" &&
            item.ExecutionTopology == "provider-native");
    }

    [Fact]
    public async Task MapCephalonDoesNotExposeExternalCdcRuntimeReportIngressWhenDisabled()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(options =>
            {
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    AcknowledgementMode = "runtime-managed",
                    ObservationStaleAfterSeconds = 60,
                    RejectOutOfOrderReports = true,
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
                options.CdcExecutionRuntimes[0].EdgeNodeIds.Add("edge-bkk-01");
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var initialRuntime = app.Services.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>().GetById("external-cdc-runtime");
        Assert.NotNull(initialRuntime);
        Assert.Equal(120, initialRuntime.ReporterLeaseSeconds);
        Assert.True(initialRuntime.RejectConflictingReporterIds);
        Assert.Equal(["edge-bkk-01"], initialRuntime.EdgeNodeIds);
        var response = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:00:00Z", CultureInfo.InvariantCulture))
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MapCephalonAcceptsExternalCdcRuntimeReportsAndRefreshesRuntimeSurfaces()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T03:10:30Z", CultureInfo.InvariantCulture));
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    AcknowledgementMode = "runtime-managed",
                    ObservationStaleAfterSeconds = 60,
                    RejectOutOfOrderReports = true,
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
                options.CdcExecutionRuntimes[0].EdgeNodeIds.Add("edge-bkk-01");
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:10:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-001",
                    capturedChangeCount: 5,
                    producedMessageCount: 5,
                    changeId: "lsn-external-0005",
                    checkpoint: "external-checkpoint-0005",
                    freshness: new CdcCaptureFreshnessStatus(
                        CdcCaptureFreshnessStates.Fresh,
                        DateTimeOffset.Parse("2026-04-21T03:15:00Z", CultureInfo.InvariantCulture),
                        "The external runtime is still within the expected freshness window."),
                    lag: new CdcCaptureLagStatus(
                        CdcCaptureLagStates.Lagging,
                        pendingChangeCount: 1,
                        description: "The external runtime is still catching up."),
                    publication: new CdcCapturePublicationStatus(
                        CdcCapturePublicationStates.PendingPublication,
                        pendingPublicationCount: 1,
                        description: "The external runtime still has a publication pending."),
                    metadata: new Dictionary<string, string>
                    {
                        ["captureExecution"] = "external-runtime-report"
                    },
                    reporterId: "edge-agent-a",
                    edgeNodeId: "edge-bkk-01")
            });

        response.EnsureSuccessStatusCode();
        var reportPayload = await response.Content.ReadAsStringAsync();
        using var reportDocument = JsonDocument.Parse(reportPayload);
        var reportedRuntime = await response.Content.ReadFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>();
        var runtime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");
        var state = await client.GetFromJsonAsync<CdcCaptureRuntimeState>("/engine/cdc-captures/runtime/tenant-profile-cdc");
        var statesByRuntime = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>("/engine/cdc-captures/runtime/execution-runtimes/external-cdc-runtime");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        var serviceRuntime = app.Services.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>().GetById("external-cdc-runtime");
        var serviceState = app.Services.GetRequiredService<ICdcCaptureRuntimeStateCatalog>().GetById("tenant-profile-cdc");

        Assert.NotNull(reportedRuntime);
        Assert.Equal("external-cdc-runtime", reportedRuntime.Id);
        Assert.True(reportedRuntime.Summary.HasReports);
        Assert.Equal("tenant-profile-cdc", reportedRuntime.Summary.LastCdcCaptureId);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, reportedRuntime.Summary.LastOutcome);
        Assert.Equal("external-report-001", reportedRuntime.Summary.LastReportId);
        Assert.Equal("edge-agent-a", reportedRuntime.Summary.LastReporterId);
        Assert.Equal("edge-agent-a", reportedRuntime.Summary.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:12:00Z", CultureInfo.InvariantCulture), reportedRuntime.Summary.ReporterLeaseExpiresAtUtc);
        Assert.Equal(["edge-bkk-01"], reportedRuntime.Summary.ObservedEdgeNodeIds);
        Assert.Equal("edge-bkk-01", reportedRuntime.Summary.LastEdgeNodeId);
        Assert.Equal(5, reportedRuntime.Summary.TotalCapturedChangeCount);
        Assert.Equal(5, reportedRuntime.Summary.TotalProducedMessageCount);
        Assert.NotNull(serviceState);
        Assert.Equal("60", serviceState.Metadata["observationStaleAfterSeconds"]);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, serviceState.Metadata["observationFreshnessState"]);
        Assert.Equal("edge-agent-a", serviceState.Metadata["cdcCaptureReporterId"]);
        Assert.Equal("2026-04-21T03:12:00.0000000+00:00", serviceState.Metadata["cdcCaptureReporterLeaseExpiresAtUtc"]);
        Assert.Equal("edge-bkk-01", serviceState.Metadata["cdcCaptureEdgeNodeId"]);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, serviceState.ObservationFreshness.State);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:11:00Z", CultureInfo.InvariantCulture), serviceState.ObservationFreshness.FreshUntilUtc);
        Assert.Equal("edge-agent-a", serviceState.LastReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:12:00Z", CultureInfo.InvariantCulture), serviceState.ReporterLeaseExpiresAtUtc);
        Assert.Equal("edge-bkk-01", serviceState.LastEdgeNodeId);
        Assert.NotNull(serviceRuntime);
        Assert.Equal(120, serviceRuntime.ReporterLeaseSeconds);
        Assert.True(serviceRuntime.RejectConflictingReporterIds);
        Assert.Equal(["edge-bkk-01"], serviceRuntime.EdgeNodeIds);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, serviceRuntime.Summary.ObservationFreshness.State);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:11:00Z", CultureInfo.InvariantCulture), serviceRuntime.Summary.ObservationFreshness.FreshUntilUtc);
        Assert.Equal("edge-agent-a", serviceRuntime.Summary.ActiveReporterId);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, reportDocument.RootElement.GetProperty("summary").GetProperty("observationFreshness").GetProperty("state").GetString());
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, reportedRuntime.Summary.ObservationFreshness.State);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:11:00Z", CultureInfo.InvariantCulture), reportedRuntime.Summary.ObservationFreshness.FreshUntilUtc);

        Assert.NotNull(runtime);
        Assert.True(runtime.Summary.HasReports);
        Assert.Equal("out-of-process-reporting", runtime.ExecutionTopology);
        Assert.Equal("external-report-001", runtime.Summary.LastReportId);
        Assert.Equal("edge-agent-a", runtime.Summary.LastReporterId);
        Assert.Equal("edge-agent-a", runtime.Summary.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:12:00Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterLeaseExpiresAtUtc);
        Assert.Equal(["edge-bkk-01"], runtime.Summary.ObservedEdgeNodeIds);
        Assert.Equal(["edge-bkk-01"], runtime.EdgeNodeIds);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, runtime.Summary.ObservationFreshness.State);

        Assert.NotNull(state);
        Assert.Equal("external-cdc-runtime", state.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("out-of-process-reporting", state.ExecutionBinding.ExecutionTopology);
        Assert.Equal(CdcCaptureRuntimeOutcomes.Captured, state.LastOutcome);
        Assert.Equal(5, state.TotalCapturedChangeCount);
        Assert.Equal(5, state.TotalProducedMessageCount);
        Assert.Equal("lsn-external-0005", state.LastChangeId);
        Assert.Equal("external-checkpoint-0005", state.LastCheckpoint);
        Assert.Equal("external-report-001", state.LastReportId);
        Assert.Equal("edge-agent-a", state.LastReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:12:00Z", CultureInfo.InvariantCulture), state.ReporterLeaseExpiresAtUtc);
        Assert.Equal("edge-bkk-01", state.LastEdgeNodeId);
        Assert.Equal("external-runtime-report", state.Metadata["captureExecution"]);
        Assert.Equal("external-cdc-runtime", state.Metadata["cdcCaptureExecutionRuntimeId"]);
        Assert.Equal("external-report-001", state.Metadata["cdcCaptureReportId"]);
        Assert.Equal("edge-agent-a", state.Metadata["cdcCaptureReporterId"]);
        Assert.Equal("2026-04-21T03:12:00.0000000+00:00", state.Metadata["cdcCaptureReporterLeaseExpiresAtUtc"]);
        Assert.Equal("edge-bkk-01", state.Metadata["cdcCaptureEdgeNodeId"]);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, state.ObservationFreshness.State);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:11:00Z", CultureInfo.InvariantCulture), state.ObservationFreshness.FreshUntilUtc);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, state.Metadata["observationFreshnessState"]);
        Assert.Equal("60", state.Metadata["observationStaleAfterSeconds"]);

        Assert.NotNull(statesByRuntime);
        var stateByRuntime = Assert.Single(statesByRuntime);
        Assert.Equal("tenant-profile-cdc", stateByRuntime.CdcCaptureId);
        Assert.Equal("external-cdc-runtime", stateByRuntime.ExecutionBinding.EffectiveExecutionRuntimeId);
        Assert.Equal("external-report-001", stateByRuntime.LastReportId);
        Assert.Equal("edge-agent-a", stateByRuntime.LastReporterId);
        Assert.Equal("edge-bkk-01", stateByRuntime.LastEdgeNodeId);

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.CdcCaptureStates, item =>
            item.CdcCaptureId == "tenant-profile-cdc" &&
            item.ExecutionBinding.EffectiveExecutionRuntimeId == "external-cdc-runtime" &&
            item.TotalProducedMessageCount == 5 &&
            item.LastReportId == "external-report-001" &&
            item.LastReporterId == "edge-agent-a" &&
            item.LastEdgeNodeId == "edge-bkk-01" &&
            item.ObservationFreshness.State == CdcCaptureFreshnessStates.Fresh);
        Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item =>
            item.Id == "external-cdc-runtime" &&
            item.Summary.LastOutcome == CdcCaptureRuntimeOutcomes.Captured &&
            item.Summary.TotalProducedMessageCount == 5 &&
            item.Summary.LastReportId == "external-report-001" &&
            item.Summary.ActiveReporterId == "edge-agent-a" &&
            item.Summary.LastEdgeNodeId == "edge-bkk-01" &&
            item.Summary.ObservationFreshness.State == CdcCaptureFreshnessStates.Fresh);
    }

    [Fact]
    public async Task MapCephalonRejectsOutOfOrderExternalCdcRuntimeReports()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    RejectOutOfOrderReports = true,
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var initialRuntime = app.Services.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>().GetById("external-cdc-runtime");
        Assert.NotNull(initialRuntime);
        Assert.Equal(120, initialRuntime.ReporterLeaseSeconds);
        Assert.True(initialRuntime.RejectConflictingReporterIds);
        var firstResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:20:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-late",
                    reporterId: "edge-agent-a")
            });
        firstResponse.EnsureSuccessStatusCode();

        var secondResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:15:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-early",
                    reporterId: "edge-agent-a")
            });
        var errorPayload = await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
        Assert.Contains("out-of-order", errorPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("external-report-early", errorPayload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonRejectsConflictingExternalCdcRuntimeReporterIdsWhileLeaseIsActive()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T03:40:45Z", CultureInfo.InvariantCulture));
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var firstResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:40:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a",
                    reporterId: "edge-agent-a")
            });
        firstResponse.EnsureSuccessStatusCode();

        var secondResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:40:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-b",
                    reporterId: "edge-agent-b")
            });
        var errorPayload = await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
        Assert.Contains("edge-agent-b", errorPayload, StringComparison.Ordinal);
        Assert.Contains("edge-agent-a", errorPayload, StringComparison.Ordinal);

        var state = await client.GetFromJsonAsync<CdcCaptureRuntimeState>("/engine/cdc-captures/runtime/tenant-profile-cdc");
        var runtime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");

        Assert.NotNull(state);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Conflicted, state.ReporterCoordination.State);
        Assert.Equal("edge-agent-a", state.ReporterCoordination.ActiveReporterId);
        Assert.Equal("edge-agent-b", state.ReporterCoordination.LastConflictingReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:40:30Z", CultureInfo.InvariantCulture), state.ReporterCoordination.LastConflictedAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.NotRequired, state.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.RejectedReporterConflict, state.ReporterCoordination.DegradedReason);
        Assert.True(state.ReporterCoordination.IsDegraded);
        Assert.True(state.HasReporterCoordinationIssue);
        Assert.False(state.ReporterCoordination.HasStandbyReporters);
        Assert.True(state.ReporterCoordination.HasRejectedReporters);
        Assert.Collection(
            state.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:40:00Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
                Assert.Equal("tenant-profile-cdc", participant.LastCdcCaptureId);
            },
            participant =>
            {
                Assert.Equal("edge-agent-b", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Rejected, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:40:30Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
                Assert.Equal("tenant-profile-cdc", participant.LastCdcCaptureId);
            });
        Assert.NotNull(runtime);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Conflicted, runtime.Summary.ReporterCoordination.State);
        Assert.Equal("edge-agent-a", runtime.Summary.ReporterCoordination.ActiveReporterId);
        Assert.Equal("edge-agent-b", runtime.Summary.ReporterCoordination.LastConflictingReporterId);
        Assert.Equal(CdcCaptureReporterTakeoverStates.NotRequired, runtime.Summary.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.RejectedReporterConflict, runtime.Summary.ReporterCoordination.DegradedReason);
        Assert.True(runtime.Summary.ReporterCoordination.IsDegraded);
        Assert.True(runtime.Summary.HasReporterCoordinationIssue);
        Assert.Equal(["edge-agent-a"], runtime.Summary.ReporterCoordinationRollup.ActiveReporterIds);
        Assert.Empty(runtime.Summary.ReporterCoordinationRollup.StandbyReporterIds);
        Assert.Equal(["edge-agent-b"], runtime.Summary.ReporterCoordinationRollup.RejectedReporterIds);
        Assert.Equal(["tenant-profile-cdc"], runtime.Summary.ReporterCoordinationRollup.DegradedCdcCaptureIds);
        Assert.True(runtime.Summary.ReporterCoordinationRollup.HasRejectedReporters);
        Assert.True(runtime.Summary.ReporterCoordinationRollup.HasDegradedCdcCaptures);
        Assert.Collection(
            runtime.Summary.ReporterCoordinationRollup.CoordinationStateBreakdown,
            breakdown =>
            {
                Assert.Equal(CdcCaptureReporterCoordinationStates.Conflicted, breakdown.Id);
                Assert.Equal(1, breakdown.Count);
            });
        Assert.Collection(
            runtime.Summary.ReporterCoordinationRollup.DegradedReasonBreakdown,
            breakdown =>
            {
                Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.RejectedReporterConflict, breakdown.Id);
                Assert.Equal(1, breakdown.Count);
            });
        Assert.Collection(
            runtime.Summary.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
            },
            participant =>
            {
                Assert.Equal("edge-agent-b", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Rejected, participant.Role);
            });
    }

    [Fact]
    public async Task MapCephalonExposesExternalCdcOperatorStoryDrillDownRoutes()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T03:40:45Z", CultureInfo.InvariantCulture));
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "edge-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
                options.CdcExecutionRuntimes[0].EdgeNodeIds.Add("edge-bkk-01");
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var firstResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:40:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a",
                    reporterId: "edge-agent-a",
                    edgeNodeId: "edge-bkk-01")
            });
        firstResponse.EnsureSuccessStatusCode();

        var secondResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:40:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-b",
                    reporterId: "edge-agent-b",
                    edgeNodeId: "edge-bkk-01")
            });
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var statesByReporterA = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>("/engine/cdc-captures/runtime/reporters/edge-agent-a");
        var statesByReporterB = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>("/engine/cdc-captures/runtime/reporters/edge-agent-b");
        var statesByEdgeNode = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>("/engine/cdc-captures/runtime/edge-nodes/edge-bkk-01");
        var statesByCoordination = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>($"/engine/cdc-captures/runtime/reporter-coordination/{CdcCaptureReporterCoordinationStates.Conflicted}");
        var statesByIssue = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>($"/engine/cdc-captures/runtime/reporter-coordination/issues/{CdcCaptureReporterCoordinationIssueReasons.RejectedReporterConflict}");

        var runtimesByReporterA = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/reporters/edge-agent-a");
        var runtimesByReporterB = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/reporters/edge-agent-b");
        var runtimesByEdgeNode = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>("/engine/cdc-capture-runtimes/edge-nodes/edge-bkk-01");
        var runtimesByCoordination = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>($"/engine/cdc-capture-runtimes/reporter-coordination/{CdcCaptureReporterCoordinationStates.Conflicted}");
        var runtimesByIssue = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>($"/engine/cdc-capture-runtimes/reporter-coordination/issues/{CdcCaptureReporterCoordinationIssueReasons.RejectedReporterConflict}");

        Assert.NotNull(statesByReporterA);
        var reporterAState = Assert.Single(statesByReporterA);
        Assert.Equal("tenant-profile-cdc", reporterAState.CdcCaptureId);
        Assert.NotNull(statesByReporterB);
        var reporterBState = Assert.Single(statesByReporterB);
        Assert.Equal("tenant-profile-cdc", reporterBState.CdcCaptureId);
        Assert.NotNull(statesByEdgeNode);
        var edgeNodeState = Assert.Single(statesByEdgeNode);
        Assert.Equal("tenant-profile-cdc", edgeNodeState.CdcCaptureId);
        Assert.NotNull(statesByCoordination);
        Assert.Single(statesByCoordination);
        Assert.NotNull(statesByIssue);
        Assert.Single(statesByIssue);

        Assert.NotNull(runtimesByReporterA);
        var reporterARuntime = Assert.Single(runtimesByReporterA);
        Assert.Equal("external-cdc-runtime", reporterARuntime.Id);
        Assert.NotNull(runtimesByReporterB);
        var reporterBRuntime = Assert.Single(runtimesByReporterB);
        Assert.Equal("external-cdc-runtime", reporterBRuntime.Id);
        Assert.NotNull(runtimesByEdgeNode);
        var edgeNodeRuntime = Assert.Single(runtimesByEdgeNode);
        Assert.Equal("external-cdc-runtime", edgeNodeRuntime.Id);
        Assert.NotNull(runtimesByCoordination);
        Assert.Single(runtimesByCoordination);
        Assert.NotNull(runtimesByIssue);
        Assert.Single(runtimesByIssue);
    }

    [Fact]
    public async Task MapCephalonMarksExternalCdcRuntimeReporterLeaseAsExpiredAfterLeaseWindow()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T03:40:45Z", CultureInfo.InvariantCulture));
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:40:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a",
                    reporterId: "edge-agent-a")
            });
        response.EnsureSuccessStatusCode();

        timeProvider.Advance(TimeSpan.FromSeconds(76));

        var state = await client.GetFromJsonAsync<CdcCaptureRuntimeState>("/engine/cdc-captures/runtime/tenant-profile-cdc");
        var runtime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(state);
        Assert.Equal(CdcCaptureReporterCoordinationStates.LeaseExpired, state.ReporterCoordination.State);
        Assert.Equal("edge-agent-a", state.ReporterCoordination.PreviousReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:42:00Z", CultureInfo.InvariantCulture), state.ReporterCoordination.LeaseExpiredAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.AwaitingTakeover, state.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.AwaitingTakeover, state.ReporterCoordination.DegradedReason);
        Assert.True(state.ReporterCoordination.IsDegraded);
        Assert.True(state.HasReporterCoordinationIssue);
        Assert.True(state.ReporterCoordination.HasStandbyReporters);
        Assert.False(state.ReporterCoordination.HasRejectedReporters);
        Assert.Collection(
            state.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Standby, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:40:00Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
            });
        Assert.NotNull(runtime);
        Assert.Null(runtime.Summary.ActiveReporterId);
        Assert.Null(runtime.Summary.ReporterLeaseExpiresAtUtc);
        Assert.Equal(CdcCaptureReporterCoordinationStates.LeaseExpired, runtime.Summary.ReporterCoordination.State);
        Assert.Equal("edge-agent-a", runtime.Summary.ReporterCoordination.PreviousReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:42:00Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterCoordination.LeaseExpiredAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.AwaitingTakeover, runtime.Summary.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.AwaitingTakeover, runtime.Summary.ReporterCoordination.DegradedReason);
        Assert.True(runtime.Summary.ReporterCoordination.IsDegraded);
        Assert.True(runtime.Summary.HasReporterCoordinationIssue);
        Assert.Collection(
            runtime.Summary.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Standby, participant.Role);
            });
        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.CdcCaptureStates, item =>
            item.CdcCaptureId == "tenant-profile-cdc" &&
            item.ReporterCoordination.State == CdcCaptureReporterCoordinationStates.LeaseExpired);
        Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item =>
            item.Id == "external-cdc-runtime" &&
            item.Summary.ReporterCoordination.State == CdcCaptureReporterCoordinationStates.LeaseExpired);
    }

    [Fact]
    public async Task MapCephalonAllowsExternalCdcRuntimeReporterTakeoverAfterLeaseExpires()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T03:40:45Z", CultureInfo.InvariantCulture));
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var firstResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:40:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a",
                    reporterId: "edge-agent-a")
            });
        firstResponse.EnsureSuccessStatusCode();

        timeProvider.Advance(TimeSpan.FromSeconds(76));

        var secondResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:42:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-b",
                    reporterId: "edge-agent-b")
            });
        secondResponse.EnsureSuccessStatusCode();

        var state = await client.GetFromJsonAsync<CdcCaptureRuntimeState>("/engine/cdc-captures/runtime/tenant-profile-cdc");
        var runtime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(state);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Active, state.ReporterCoordination.State);
        Assert.Equal("edge-agent-b", state.ReporterCoordination.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:44:30Z", CultureInfo.InvariantCulture), state.ReporterCoordination.ActiveReporterLeaseExpiresAtUtc);
        Assert.Equal("edge-agent-a", state.ReporterCoordination.PreviousReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:42:00Z", CultureInfo.InvariantCulture), state.ReporterCoordination.LeaseExpiredAtUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:42:30Z", CultureInfo.InvariantCulture), state.ReporterCoordination.LastTakeoverObservedAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.Completed, state.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.None, state.ReporterCoordination.DegradedReason);
        Assert.False(state.ReporterCoordination.IsDegraded);
        Assert.False(state.HasReporterCoordinationIssue);
        Assert.True(state.ReporterCoordination.HasCompletedTakeover);
        Assert.True(state.ReporterCoordination.HasStandbyReporters);
        Assert.False(state.ReporterCoordination.HasRejectedReporters);
        Assert.Collection(
            state.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-b", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:42:30Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
            },
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Standby, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:40:00Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
            });
        Assert.NotNull(runtime);
        Assert.Equal("edge-agent-b", runtime.Summary.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:44:30Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterLeaseExpiresAtUtc);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Active, runtime.Summary.ReporterCoordination.State);
        Assert.Equal("edge-agent-b", runtime.Summary.ReporterCoordination.ActiveReporterId);
        Assert.Equal("edge-agent-a", runtime.Summary.ReporterCoordination.PreviousReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:42:00Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterCoordination.LeaseExpiredAtUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:42:30Z", CultureInfo.InvariantCulture), runtime.Summary.ReporterCoordination.LastTakeoverObservedAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.Completed, runtime.Summary.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.None, runtime.Summary.ReporterCoordination.DegradedReason);
        Assert.False(runtime.Summary.ReporterCoordination.IsDegraded);
        Assert.False(runtime.Summary.HasReporterCoordinationIssue);
        Assert.True(runtime.Summary.ReporterCoordination.HasCompletedTakeover);
        Assert.Collection(
            runtime.Summary.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-b", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
            },
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Standby, participant.Role);
            });
        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.CdcCaptureStates, item =>
            item.CdcCaptureId == "tenant-profile-cdc" &&
            item.ReporterCoordination.ActiveReporterId == "edge-agent-b" &&
            item.ReporterCoordination.LastTakeoverObservedAtUtc == DateTimeOffset.Parse("2026-04-21T03:42:30Z", CultureInfo.InvariantCulture));
        Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item =>
            item.Id == "external-cdc-runtime" &&
            item.Summary.ReporterCoordination.ActiveReporterId == "edge-agent-b" &&
            item.Summary.ReporterCoordination.LastTakeoverObservedAtUtc == DateTimeOffset.Parse("2026-04-21T03:42:30Z", CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MapCephalonClearsRejectedReporterConflictAfterActiveReporterReaffirmsLease()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T03:40:45Z", CultureInfo.InvariantCulture));
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var firstResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:40:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a",
                    reporterId: "edge-agent-a")
            });
        firstResponse.EnsureSuccessStatusCode();

        var rejectedResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:40:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-b",
                    reporterId: "edge-agent-b")
            });
        Assert.Equal(HttpStatusCode.BadRequest, rejectedResponse.StatusCode);

        var recoveryResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:41:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a-2",
                    reporterId: "edge-agent-a")
            });
        recoveryResponse.EnsureSuccessStatusCode();

        var state = await client.GetFromJsonAsync<CdcCaptureRuntimeState>("/engine/cdc-captures/runtime/tenant-profile-cdc");
        var runtime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(state);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Active, state.ReporterCoordination.State);
        Assert.Equal("edge-agent-a", state.ReporterCoordination.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:43:00Z", CultureInfo.InvariantCulture), state.ReporterCoordination.ActiveReporterLeaseExpiresAtUtc);
        Assert.Null(state.ReporterCoordination.LastConflictingReporterId);
        Assert.Null(state.ReporterCoordination.LastConflictedAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.NotRequired, state.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.None, state.ReporterCoordination.DegradedReason);
        Assert.Equal(1, state.ReporterCoordination.ParticipantCount);
        Assert.Equal(1, state.ReporterCoordination.ActiveReporterCount);
        Assert.Equal(0, state.ReporterCoordination.StandbyReporterCount);
        Assert.Equal(0, state.ReporterCoordination.RejectedReporterCount);
        Assert.False(state.ReporterCoordination.HasRejectedReporters);
        Assert.False(state.ReporterCoordination.IsDegraded);
        Assert.False(state.HasReporterCoordinationIssue);
        Assert.Collection(
            state.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:41:00Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
            });

        Assert.NotNull(runtime);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Active, runtime.Summary.ReporterCoordination.State);
        Assert.Null(runtime.Summary.ReporterCoordination.LastConflictingReporterId);
        Assert.Null(runtime.Summary.ReporterCoordination.LastConflictedAtUtc);
        Assert.Equal(1, runtime.Summary.ReporterCoordination.ParticipantCount);
        Assert.Equal(1, runtime.Summary.ReporterCoordination.ActiveReporterCount);
        Assert.Equal(0, runtime.Summary.ReporterCoordination.RejectedReporterCount);
        Assert.False(runtime.Summary.ReporterCoordination.HasRejectedReporters);
        Assert.False(runtime.Summary.ReporterCoordination.IsDegraded);
        Assert.False(runtime.Summary.HasReporterCoordinationIssue);
        Assert.Collection(
            runtime.Summary.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-a", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
            });

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.CdcCaptureStates, item =>
            item.CdcCaptureId == "tenant-profile-cdc" &&
            item.ReporterCoordination.State == CdcCaptureReporterCoordinationStates.Active &&
            item.ReporterCoordination.RejectedReporterCount == 0);
        Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item =>
            item.Id == "external-cdc-runtime" &&
            item.Summary.ReporterCoordination.State == CdcCaptureReporterCoordinationStates.Active &&
            item.Summary.ReporterCoordination.RejectedReporterCount == 0);
    }

    [Fact]
    public async Task MapCephalonRemovesHistoricalStandbyReporterAfterReplacementReporterReaffirmsLease()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T03:40:45Z", CultureInfo.InvariantCulture));
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = true
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var firstResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:40:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-a",
                    reporterId: "edge-agent-a")
            });
        firstResponse.EnsureSuccessStatusCode();

        timeProvider.Advance(TimeSpan.FromSeconds(76));

        var secondResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:42:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-b",
                    reporterId: "edge-agent-b")
            });
        secondResponse.EnsureSuccessStatusCode();

        var thirdResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:43:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-reporter-b-2",
                    reporterId: "edge-agent-b")
            });
        thirdResponse.EnsureSuccessStatusCode();

        var state = await client.GetFromJsonAsync<CdcCaptureRuntimeState>("/engine/cdc-captures/runtime/tenant-profile-cdc");
        var runtime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(state);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Active, state.ReporterCoordination.State);
        Assert.Equal("edge-agent-b", state.ReporterCoordination.ActiveReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:45:00Z", CultureInfo.InvariantCulture), state.ReporterCoordination.ActiveReporterLeaseExpiresAtUtc);
        Assert.Equal("edge-agent-a", state.ReporterCoordination.PreviousReporterId);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:42:30Z", CultureInfo.InvariantCulture), state.ReporterCoordination.LastTakeoverObservedAtUtc);
        Assert.Equal(CdcCaptureReporterTakeoverStates.Completed, state.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.None, state.ReporterCoordination.DegradedReason);
        Assert.Equal(1, state.ReporterCoordination.ParticipantCount);
        Assert.Equal(1, state.ReporterCoordination.ActiveReporterCount);
        Assert.Equal(0, state.ReporterCoordination.StandbyReporterCount);
        Assert.False(state.ReporterCoordination.HasStandbyReporters);
        Assert.True(state.ReporterCoordination.HasCompletedTakeover);
        Assert.False(state.ReporterCoordination.IsDegraded);
        Assert.False(state.HasReporterCoordinationIssue);
        Assert.Collection(
            state.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-b", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
                Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:43:00Z", CultureInfo.InvariantCulture), participant.LastObservedAtUtc);
            });

        Assert.NotNull(runtime);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Active, runtime.Summary.ReporterCoordination.State);
        Assert.Equal("edge-agent-a", runtime.Summary.ReporterCoordination.PreviousReporterId);
        Assert.Equal(CdcCaptureReporterTakeoverStates.Completed, runtime.Summary.ReporterCoordination.TakeoverState);
        Assert.Equal(1, runtime.Summary.ReporterCoordination.ParticipantCount);
        Assert.Equal(1, runtime.Summary.ReporterCoordination.ActiveReporterCount);
        Assert.Equal(0, runtime.Summary.ReporterCoordination.StandbyReporterCount);
        Assert.False(runtime.Summary.ReporterCoordination.HasStandbyReporters);
        Assert.True(runtime.Summary.ReporterCoordination.HasCompletedTakeover);
        Assert.False(runtime.Summary.ReporterCoordination.IsDegraded);
        Assert.False(runtime.Summary.HasReporterCoordinationIssue);
        Assert.Equal(["edge-agent-b"], runtime.Summary.ReporterCoordinationRollup.ActiveReporterIds);
        Assert.Empty(runtime.Summary.ReporterCoordinationRollup.StandbyReporterIds);
        Assert.Empty(runtime.Summary.ReporterCoordinationRollup.RejectedReporterIds);
        Assert.Empty(runtime.Summary.ReporterCoordinationRollup.DegradedCdcCaptureIds);
        Assert.False(runtime.Summary.ReporterCoordinationRollup.HasStandbyReporters);
        Assert.False(runtime.Summary.ReporterCoordinationRollup.HasRejectedReporters);
        Assert.False(runtime.Summary.ReporterCoordinationRollup.HasDegradedCdcCaptures);
        Assert.Collection(
            runtime.Summary.ReporterCoordinationRollup.CoordinationStateBreakdown,
            breakdown =>
            {
                Assert.Equal(CdcCaptureReporterCoordinationStates.Active, breakdown.Id);
                Assert.Equal(1, breakdown.Count);
            });
        Assert.Collection(
            runtime.Summary.ReporterCoordinationRollup.DegradedReasonBreakdown,
            breakdown =>
            {
                Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.None, breakdown.Id);
                Assert.Equal(1, breakdown.Count);
            });
        Assert.Collection(
            runtime.Summary.ReporterCoordination.ReporterParticipants,
            participant =>
            {
                Assert.Equal("edge-agent-b", participant.ReporterId);
                Assert.Equal(CdcCaptureReporterParticipantRoles.Active, participant.Role);
            });

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.CdcCaptureStates, item =>
            item.CdcCaptureId == "tenant-profile-cdc" &&
            item.ReporterCoordination.ParticipantCount == 1 &&
            item.ReporterCoordination.HasCompletedTakeover);
        Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item =>
            item.Id == "external-cdc-runtime" &&
            item.Summary.ReporterCoordination.ParticipantCount == 1 &&
            item.Summary.ReporterCoordination.HasCompletedTakeover);
    }

    [Fact]
    public async Task MapCephalonTracksExternalCdcRuntimeReportingCoverageAcrossDeclaredCaptures()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new MultiCaptureExecutionRuntimeHostingTestModule());
            cephalon.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = false
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("multi-capture-cdc-a");
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("multi-capture-cdc-b");
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var initialRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");
        var initialSnapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(initialRuntime);
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.Unreported, initialRuntime.Summary.ReportingCoverage.State);
        Assert.Equal(2, initialRuntime.Summary.ReportingCoverage.DeclaredCaptureCount);
        Assert.Equal(0, initialRuntime.Summary.ReportingCoverage.ReportedCaptureCount);
        Assert.Equal(["multi-capture-cdc-a", "multi-capture-cdc-b"], initialRuntime.Summary.ReportingCoverage.UnreportedCdcCaptureIds);
        Assert.NotNull(initialSnapshot);
        Assert.Contains(
            initialSnapshot.CdcCaptureExecutionRuntimes,
            item => item.Id == "external-cdc-runtime" &&
                item.Summary.ReportingCoverage.State == CdcCaptureExecutionRuntimeReportingCoverageStates.Unreported &&
                item.Summary.ReportingCoverage.UnreportedCdcCaptureIds.Count == 2);

        var partialResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "multi-capture-cdc-a",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T08:10:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-coverage-a",
                    reporterId: "edge-agent-a")
            });
        partialResponse.EnsureSuccessStatusCode();

        var partiallyReportedRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");
        var partiallyReportedSnapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(partiallyReportedRuntime);
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.PartiallyReported, partiallyReportedRuntime.Summary.ReportingCoverage.State);
        Assert.Equal(2, partiallyReportedRuntime.Summary.ReportingCoverage.DeclaredCaptureCount);
        Assert.Equal(1, partiallyReportedRuntime.Summary.ReportingCoverage.ReportedCaptureCount);
        Assert.Equal(["multi-capture-cdc-b"], partiallyReportedRuntime.Summary.ReportingCoverage.UnreportedCdcCaptureIds);
        Assert.Equal(["multi-capture-cdc-a"], partiallyReportedRuntime.Summary.ReportedCdcCaptureIds);
        Assert.NotNull(partiallyReportedSnapshot);
        Assert.Contains(
            partiallyReportedSnapshot.CdcCaptureExecutionRuntimes,
            item => item.Id == "external-cdc-runtime" &&
                item.Summary.ReportingCoverage.State == CdcCaptureExecutionRuntimeReportingCoverageStates.PartiallyReported &&
                item.Summary.ReportingCoverage.UnreportedCdcCaptureIds.SequenceEqual(["multi-capture-cdc-b"]));

        var fullResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "multi-capture-cdc-b",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T08:10:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-coverage-b",
                    reporterId: "edge-agent-a")
            });
        fullResponse.EnsureSuccessStatusCode();

        var fullyReportedRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");
        var fullyReportedSnapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(fullyReportedRuntime);
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.FullyReported, fullyReportedRuntime.Summary.ReportingCoverage.State);
        Assert.Equal(2, fullyReportedRuntime.Summary.ReportingCoverage.DeclaredCaptureCount);
        Assert.Equal(2, fullyReportedRuntime.Summary.ReportingCoverage.ReportedCaptureCount);
        Assert.Empty(fullyReportedRuntime.Summary.ReportingCoverage.UnreportedCdcCaptureIds);
        Assert.True(fullyReportedRuntime.Summary.HasFullCaptureCoverage);
        Assert.NotNull(fullyReportedSnapshot);
        Assert.Contains(
            fullyReportedSnapshot.CdcCaptureExecutionRuntimes,
            item => item.Id == "external-cdc-runtime" &&
                item.Summary.ReportingCoverage.State == CdcCaptureExecutionRuntimeReportingCoverageStates.FullyReported &&
                item.Summary.HasFullCaptureCoverage);
    }

    [Fact]
    public async Task MapCephalonTracksExternalCdcRuntimeRemediationAcrossResolvedCaptureBindings()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new BoundMultiCaptureExecutionRuntimeHostingTestModule());
            cephalon.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = false
                });
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var initialRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");
        var initialAttention = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>($"/engine/cdc-capture-runtimes/remediation/{CdcCaptureExecutionRuntimeRemediationStates.Attention}");
        var initialUnreported = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>($"/engine/cdc-capture-runtimes/remediation/categories/{CdcCaptureExecutionRuntimeRemediationCategories.UnreportedCdcCaptures}");
        var initialSnapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(initialRuntime);
        Assert.Equal(["bound-multi-capture-cdc-a", "bound-multi-capture-cdc-b"], initialRuntime.CdcCaptureIds);
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.Unreported, initialRuntime.Summary.ReportingCoverage.State);
        Assert.Equal(CdcCaptureExecutionRuntimeRemediationStates.Attention, initialRuntime.Summary.Remediation.State);
        Assert.Equal([CdcCaptureExecutionRuntimeRemediationCategories.UnreportedCdcCaptures], initialRuntime.Summary.Remediation.CategoryIds);
        Assert.NotNull(initialAttention);
        Assert.Single(initialAttention);
        Assert.NotNull(initialUnreported);
        Assert.Single(initialUnreported);
        Assert.NotNull(initialSnapshot);
        Assert.Contains(
            initialSnapshot.CdcCaptureExecutionRuntimes,
            item => item.Id == "external-cdc-runtime" &&
                item.Summary.Remediation.State == CdcCaptureExecutionRuntimeRemediationStates.Attention &&
                item.Summary.Remediation.UnreportedCdcCaptureIds.Count == 2);

        var partialResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "bound-multi-capture-cdc-a",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T08:30:00Z", CultureInfo.InvariantCulture),
                    reportId: "bound-hosting-report-a",
                    reporterId: "edge-agent-a")
            });
        partialResponse.EnsureSuccessStatusCode();

        var partiallyReportedRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");

        Assert.NotNull(partiallyReportedRuntime);
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.PartiallyReported, partiallyReportedRuntime.Summary.ReportingCoverage.State);
        Assert.Equal(CdcCaptureExecutionRuntimeRemediationStates.Attention, partiallyReportedRuntime.Summary.Remediation.State);
        Assert.Equal(["bound-multi-capture-cdc-b"], partiallyReportedRuntime.Summary.Remediation.UnreportedCdcCaptureIds);
        Assert.Equal(["bound-multi-capture-cdc-b"], partiallyReportedRuntime.Summary.Remediation.AffectedCdcCaptureIds);

        var fullResponse = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "bound-multi-capture-cdc-b",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-23T08:30:30Z", CultureInfo.InvariantCulture),
                    reportId: "bound-hosting-report-b",
                    reporterId: "edge-agent-a")
            });
        fullResponse.EnsureSuccessStatusCode();

        var fullyReportedRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");
        var readyRuntimes = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>($"/engine/cdc-capture-runtimes/remediation/{CdcCaptureExecutionRuntimeRemediationStates.Ready}");
        var unresolvedUnreported = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor[]>($"/engine/cdc-capture-runtimes/remediation/categories/{CdcCaptureExecutionRuntimeRemediationCategories.UnreportedCdcCaptures}");
        var finalSnapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(fullyReportedRuntime);
        Assert.Equal(CdcCaptureExecutionRuntimeReportingCoverageStates.FullyReported, fullyReportedRuntime.Summary.ReportingCoverage.State);
        Assert.Equal(CdcCaptureExecutionRuntimeRemediationStates.Ready, fullyReportedRuntime.Summary.Remediation.State);
        Assert.Empty(fullyReportedRuntime.Summary.Remediation.CategoryIds);
        Assert.False(fullyReportedRuntime.Summary.RequiresRemediation);
        Assert.NotNull(readyRuntimes);
        Assert.Single(readyRuntimes);
        Assert.NotNull(unresolvedUnreported);
        Assert.Empty(unresolvedUnreported);
        Assert.NotNull(finalSnapshot);
        Assert.Contains(
            finalSnapshot.CdcCaptureExecutionRuntimes,
            item => item.Id == "external-cdc-runtime" &&
                item.Summary.Remediation.State == CdcCaptureExecutionRuntimeRemediationStates.Ready &&
                !item.Summary.RequiresRemediation);
    }

    [Fact]
    public async Task MapCephalonMarksExternalCdcRuntimeAsConflictedWhenMultipleReportersHoldActiveLeases()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T03:44:45Z", CultureInfo.InvariantCulture));
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new MultiCaptureExecutionRuntimeHostingTestModule());
            cephalon.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ReporterLeaseSeconds = 120,
                    RejectConflictingReporterIds = false
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("multi-capture-cdc-a");
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("multi-capture-cdc-b");
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "multi-capture-cdc-a",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:44:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-capture-a",
                    reporterId: "edge-agent-a"),
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "multi-capture-cdc-b",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:44:30Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-capture-b",
                    reporterId: "edge-agent-b")
            });
        response.EnsureSuccessStatusCode();

        var runtime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");
        var statesByRuntime = await client.GetFromJsonAsync<CdcCaptureRuntimeState[]>("/engine/cdc-captures/runtime/execution-runtimes/external-cdc-runtime");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(statesByRuntime);
        Assert.Equal(2, statesByRuntime.Length);
        Assert.All(statesByRuntime, state =>
        {
            Assert.Equal(CdcCaptureReporterCoordinationStates.Conflicted, state.ReporterCoordination.State);
            Assert.Null(state.ReporterCoordination.ActiveReporterId);
            Assert.Equal(CdcCaptureReporterTakeoverStates.NotApplicable, state.ReporterCoordination.TakeoverState);
            Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.MultipleActiveReporters, state.ReporterCoordination.DegradedReason);
            Assert.True(state.ReporterCoordination.IsDegraded);
            Assert.True(state.HasReporterCoordinationIssue);
            Assert.False(state.HasActiveReporterOwner);
            Assert.False(state.ReporterCoordination.HasStandbyReporters);
            Assert.False(state.ReporterCoordination.HasRejectedReporters);
            Assert.Equal(2, state.ReporterCoordination.ReporterParticipants.Count);
            Assert.Contains(
                state.ReporterCoordination.ReporterParticipants,
                participant => participant.ReporterId == "edge-agent-a" &&
                    participant.Role == CdcCaptureReporterParticipantRoles.Active);
            Assert.Contains(
                state.ReporterCoordination.ReporterParticipants,
                participant => participant.ReporterId == "edge-agent-b" &&
                    participant.Role == CdcCaptureReporterParticipantRoles.Active);
        });

        Assert.NotNull(runtime);
        Assert.Null(runtime.Summary.ActiveReporterId);
        Assert.Null(runtime.Summary.ReporterLeaseExpiresAtUtc);
        Assert.Equal(CdcCaptureReporterCoordinationStates.Conflicted, runtime.Summary.ReporterCoordination.State);
        Assert.Equal(CdcCaptureReporterTakeoverStates.NotApplicable, runtime.Summary.ReporterCoordination.TakeoverState);
        Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.MultipleActiveReporters, runtime.Summary.ReporterCoordination.DegradedReason);
        Assert.True(runtime.Summary.ReporterCoordination.IsDegraded);
        Assert.True(runtime.Summary.HasReporterCoordinationIssue);
        Assert.Equal(2, runtime.Summary.ReporterCoordination.ReporterParticipants.Count);
        Assert.Equal(["edge-agent-a", "edge-agent-b"], runtime.Summary.ReporterCoordinationRollup.ActiveReporterIds);
        Assert.Empty(runtime.Summary.ReporterCoordinationRollup.StandbyReporterIds);
        Assert.Empty(runtime.Summary.ReporterCoordinationRollup.RejectedReporterIds);
        Assert.Equal(["multi-capture-cdc-a", "multi-capture-cdc-b"], runtime.Summary.ReporterCoordinationRollup.DegradedCdcCaptureIds);
        Assert.True(runtime.Summary.ReporterCoordinationRollup.HasDegradedCdcCaptures);
        Assert.Collection(
            runtime.Summary.ReporterCoordinationRollup.CoordinationStateBreakdown,
            breakdown =>
            {
                Assert.Equal(CdcCaptureReporterCoordinationStates.Conflicted, breakdown.Id);
                Assert.Equal(2, breakdown.Count);
            });
        Assert.Collection(
            runtime.Summary.ReporterCoordinationRollup.DegradedReasonBreakdown,
            breakdown =>
            {
                Assert.Equal(CdcCaptureReporterCoordinationIssueReasons.MultipleActiveReporters, breakdown.Id);
                Assert.Equal(2, breakdown.Count);
            });
        Assert.Contains(
            runtime.Summary.ReporterCoordination.ReporterParticipants,
            participant => participant.ReporterId == "edge-agent-a" &&
                participant.Role == CdcCaptureReporterParticipantRoles.Active);
        Assert.Contains(
            runtime.Summary.ReporterCoordination.ReporterParticipants,
            participant => participant.ReporterId == "edge-agent-b" &&
                participant.Role == CdcCaptureReporterParticipantRoles.Active);

        Assert.NotNull(snapshot);
        Assert.Contains(
            snapshot.CdcCaptureStates,
            item => item.CdcCaptureId == "multi-capture-cdc-a" &&
                item.ReporterCoordination.DegradedReason == CdcCaptureReporterCoordinationIssueReasons.MultipleActiveReporters);
        Assert.Contains(
            snapshot.CdcCaptureStates,
            item => item.CdcCaptureId == "multi-capture-cdc-b" &&
                item.ReporterCoordination.DegradedReason == CdcCaptureReporterCoordinationIssueReasons.MultipleActiveReporters);
        Assert.Contains(
            snapshot.CdcCaptureExecutionRuntimes,
            item => item.Id == "external-cdc-runtime" &&
                item.Summary.ReporterCoordination.DegradedReason == CdcCaptureReporterCoordinationIssueReasons.MultipleActiveReporters);
    }

    [Fact]
    public async Task MapCephalonRejectsExternalCdcRuntimeReportsFromUndeclaredEdgeNodes()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an edge-aware out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "edge-reporting"
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
                options.CdcExecutionRuntimes[0].EdgeNodeIds.Add("edge-bkk-01");
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:45:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-edge-001",
                    edgeNodeId: "edge-bkk-02")
            });
        var errorPayload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("edge-bkk-02", errorPayload, StringComparison.Ordinal);
        Assert.Contains("edge-bkk-01", errorPayload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonExpiresExternalCdcRuntimeObservationFreshnessAfterConfiguredWindow()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-04-21T03:30:00Z", CultureInfo.InvariantCulture));
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new Phase8CatalogModule());
            cephalon.AddData(options =>
            {
                options.EnableExternalCdcRuntimeReporting = true;
                options.CdcExecutionRuntimes.Add(new CdcCaptureExecutionRuntimeOptions
                {
                    Id = "external-cdc-runtime",
                    DisplayName = "External CDC Runtime",
                    Description = "Represents an externally managed out-of-process CDC runner.",
                    ExecutionOwnership = "external-managed",
                    ExecutionTopology = "out-of-process-reporting",
                    ObservationStaleAfterSeconds = 60
                });
                options.CdcExecutionRuntimes[0].CdcCaptureIds.Add("tenant-profile-cdc");
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/engine/cdc-capture-runtimes/external-cdc-runtime/reports",
            new[]
            {
                new CdcCaptureRuntimeObservation(
                    cdcCaptureId: "tenant-profile-cdc",
                    outcome: CdcCaptureRuntimeOutcomes.Captured,
                    observedAtUtc: DateTimeOffset.Parse("2026-04-21T03:30:00Z", CultureInfo.InvariantCulture),
                    reportId: "external-report-freshness")
            });

        response.EnsureSuccessStatusCode();

        var freshState = await client.GetFromJsonAsync<CdcCaptureRuntimeState>("/engine/cdc-captures/runtime/tenant-profile-cdc");
        var freshRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");

        Assert.NotNull(freshState);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, freshState.ObservationFreshness.State);
        Assert.Equal(DateTimeOffset.Parse("2026-04-21T03:31:00Z", CultureInfo.InvariantCulture), freshState.ObservationFreshness.FreshUntilUtc);
        Assert.NotNull(freshRuntime);
        Assert.Equal(CdcCaptureFreshnessStates.Fresh, freshRuntime.Summary.ObservationFreshness.State);

        timeProvider.Advance(TimeSpan.FromSeconds(61));

        var staleState = await client.GetFromJsonAsync<CdcCaptureRuntimeState>("/engine/cdc-captures/runtime/tenant-profile-cdc");
        var staleRuntime = await client.GetFromJsonAsync<CdcCaptureExecutionRuntimeDescriptor>("/engine/cdc-capture-runtimes/external-cdc-runtime");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(staleState);
        Assert.Equal(CdcCaptureFreshnessStates.Stale, staleState.ObservationFreshness.State);
        Assert.True(staleState.IsObservationStale);
        Assert.NotNull(staleRuntime);
        Assert.Equal(CdcCaptureFreshnessStates.Stale, staleRuntime.Summary.ObservationFreshness.State);
        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.CdcCaptureStates, item =>
            item.CdcCaptureId == "tenant-profile-cdc" &&
            item.ObservationFreshness.State == CdcCaptureFreshnessStates.Stale);
        Assert.Contains(snapshot.CdcCaptureExecutionRuntimes, item =>
            item.Id == "external-cdc-runtime" &&
            item.Summary.ObservationFreshness.State == CdcCaptureFreshnessStates.Stale);
    }

    [Fact]
    public async Task MapCephalonLoadsRestModulePackagesFromConfiguredPackageDirectories()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Discovery:PackageDirectories:0:Path"] = GetReferenceModulePackageDirectory();
        builder.AddCephalon();

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var packages = await client.GetFromJsonAsync<PackageManifest[]>("/engine/packages");
        var operationsStatus = await client.GetStringAsync("/api/operations/status");

        Assert.NotNull(packages);
        var package = Assert.Single(packages);
        Assert.Equal("reference-operations", package.Id);
        Assert.Equal(ModulePackageReference.DirectoryManifestKind, package.Kind);
        Assert.Equal("1.0.0", package.Version);
        Assert.EndsWith("cephalon.package.json", package.SourcePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("Cephalon.ReferenceModule.Operations.dll", package.Path, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("net10.0", package.SupportedTargetFrameworks);
        Assert.Equal("cephalon-labs", package.PublisherId);
        Assert.Equal("Cephalon Labs", package.PublisherDisplayName);
        Assert.NotNull(package.Distribution);
        Assert.Equal("stable", package.Distribution.Channel);
        Assert.Equal("https://packages.example.invalid/cephalon/reference-operations/1.0.0/cephalon.package.json", package.Distribution.ManifestUri);
        Assert.NotNull(package.Provenance);
        Assert.Equal("https://github.com/Cephalon-Labs/CephalonEngine", package.Provenance.SourceRepository);
        Assert.Equal("https://packages.example.invalid/cephalon/reference-operations/1.0.0/provenance.json", package.Provenance.StatementUri);
        Assert.Null(package.SignatureKeyId);
        Assert.Equal("cephalon-labs-reference-operations", package.SignatureFingerprint);
        var signature = Assert.Single(package.Signatures);
        Assert.Equal("Cephalon Labs Build", signature.Signer);
        Assert.False(signature.IsVerified);
        Assert.False(package.IsSignatureVerified);
        Assert.Contains("no signature value", package.SignatureVerificationReason, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(package.Dependencies);
        Assert.Contains("operations", package.Modules);
        Assert.Contains("Operations module is running.", operationsStatus, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonLoadsStagedExternalPackageDirectoryAndExposesTrustAndPolicy()
    {
        var stagedPackage = CreateStagedReferenceModulePackage();

        try
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseTestServer();
            builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
            builder.Configuration[$"{EngineSettings.SectionName}:Discovery:PackageDirectories:0:Path"] = stagedPackage.PluginsRootPath;
            builder.Configuration[$"{EngineSettings.SectionName}:Discovery:PackageDirectories:0:IncludeSubdirectories"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:AllowAssemblyPathPackages"] = "false";
            builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:RequireVersion"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:RequireMinimumEngineVersion"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:RequireSupportedTargetFrameworks"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:PackagePolicy:RequirePublisherId"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:Trust:RequireTrustedPackages"] = "true";
            builder.Configuration[$"{EngineSettings.SectionName}:Trust:TrustedPublishers:0"] = "cephalon-labs";
            builder.AddCephalon();

            await using var app = builder.Build();
            app.MapCephalon();

            await app.StartAsync();
            var client = app.GetTestClient();

            var packages = await client.GetFromJsonAsync<PackageManifest[]>("/engine/packages");
            var packagePolicy = await client.GetFromJsonAsync<PackagePolicy>("/engine/package-policy");
            var trustSnapshot = await client.GetFromJsonAsync<TrustSnapshot>("/engine/trust-policy");
            var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
            var operationsStatus = await client.GetStringAsync("/api/operations/status");

            Assert.NotNull(packages);
            var package = Assert.Single(packages);
            Assert.Equal("reference-operations", package.Id);
            Assert.Equal(ModulePackageReference.DirectoryManifestKind, package.Kind);
            Assert.Equal("1.0.0", package.Version);
            Assert.Equal("cephalon-labs", package.PublisherId);
            Assert.True(package.IsTrusted);
            Assert.Equal("Package publisher is explicitly trusted by the current trust policy.", package.TrustReason);
            Assert.StartsWith(stagedPackage.PackageDirectoryPath, package.SourcePath, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("cephalon.package.json", package.SourcePath, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(stagedPackage.PackageDirectoryPath, package.Path, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("Cephalon.ReferenceModule.Operations.dll", package.Path, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("net10.0", package.SupportedTargetFrameworks);
            Assert.Contains("operations", package.Modules);

            Assert.NotNull(packagePolicy);
            Assert.False(packagePolicy.AllowAssemblyPathPackages);
            Assert.True(packagePolicy.RequireVersion);
            Assert.True(packagePolicy.RequireMinimumEngineVersion);
            Assert.True(packagePolicy.RequireSupportedTargetFrameworks);
            Assert.True(packagePolicy.RequirePublisherId);

            Assert.NotNull(trustSnapshot);
            Assert.True(trustSnapshot.Policy.RequireTrustedPackages);
            Assert.Contains(trustSnapshot.Policy.TrustedPublishers, publisher => string.Equals(publisher, "cephalon-labs", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(trustSnapshot.Packages, decision =>
                decision.PackageId == "reference-operations" &&
                decision.IsTrusted &&
                decision.PublisherId == "cephalon-labs" &&
                decision.Reason == "Package publisher is explicitly trusted by the current trust policy.");

            Assert.NotNull(snapshot);
            Assert.Contains(snapshot.Manifest.Packages, staged =>
                staged.Id == "reference-operations" &&
                staged.IsTrusted &&
                string.Equals(staged.TrustReason, "Package publisher is explicitly trusted by the current trust policy.", StringComparison.Ordinal));
            Assert.Contains(snapshot.Manifest.Modules, module =>
                module.Id == "operations" &&
                module.PackageId == "reference-operations" &&
                module.IsTrusted);

            Assert.Contains("Operations module is running.", operationsStatus, StringComparison.Ordinal);
        }
        finally
        {
            TryDeleteDirectory(stagedPackage.WorkspacePath);
        }
    }

    [Fact]
    public async Task MapCephalonEnforcesCapabilityTrustPolicyOnRestEndpoints()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Trust:Capabilities:restricted.secret"] = "Denied";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new RestrictedCapabilityModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var secretResponse = await client.GetAsync("/api/restricted/secret");
        var secretPayload = await secretResponse.Content.ReadAsStringAsync();
        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var trust = await client.GetFromJsonAsync<TrustSnapshot>("/engine/trust-policy");

        Assert.Equal(HttpStatusCode.Forbidden, secretResponse.StatusCode);
        Assert.Contains("Capability access denied", secretPayload, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(endpoints);
        Assert.Contains(endpoints, endpoint =>
            string.Equals(endpoint.RoutePattern, "/api/restricted/secret", StringComparison.Ordinal) &&
            string.Equals(endpoint.RequiredCapabilityKey, "restricted.secret", StringComparison.Ordinal));
        Assert.NotNull(capabilities);
        Assert.DoesNotContain(capabilities, capability => capability.Key == "restricted.secret");
        Assert.NotNull(trust);
        Assert.Contains(trust.Capabilities, decision =>
            decision.CapabilityKey == "restricted.secret" &&
            decision.Access == CapabilityAccess.Denied &&
            !decision.IsAllowed);
    }

    [Fact]
    public async Task MapCephalonBlocksReferenceDocsPathTraversal()
    {
        var outputPath = await CreateHostedReferenceDocsAsync("Cephalon.Engine");

        try
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseTestServer();
            builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
            builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
            builder.Configuration[$"{ReferenceDocsHostingOptions.SectionName}:Enabled"] = "true";
            builder.Configuration[$"{ReferenceDocsHostingOptions.SectionName}:DirectoryPath"] = outputPath;
            builder.AddCephalon(engine =>
            {
                engine.AddModule(new PlatformTestModule());
                engine.AddModule(new DiscoveryTestModule());
            });

            await using var app = builder.Build();
            app.MapCephalon();

            await app.StartAsync();
            var client = app.GetTestClient();

            var traversalResponse = await client.GetAsync("/reference/%2E%2E/%2E%2E/README.md");

            Assert.Equal(HttpStatusCode.NotFound, traversalResponse.StatusCode);
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task MapCephalonPreservesConsumerAuditStoreWhenBuiltInAuditWriterIsDisabled()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Audit:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Audit:EnableInMemoryWriter"] = "false";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new Phase8CatalogModule());
            engine.AddAudit();
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();

        var auditStoreCatalog = app.Services.GetRequiredService<IAuditStoreCatalog>();
        var snapshot = app.Services.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Single(auditStoreCatalog.AuditStores);
        Assert.Equal("tenant-audit-store", auditStoreCatalog.AuditStores[0].Id);
        Assert.Single(snapshot.AuditStores);
        Assert.Equal("tenant-audit-store", snapshot.AuditStores[0].Id);
        Assert.DoesNotContain(auditStoreCatalog.AuditStores, item => item.Id == "audit-default");
        Assert.DoesNotContain(snapshot.AuditStores, item => item.Id == "audit-default");

        await app.StopAsync();
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

    private static string GetReferenceModulePackageDirectory()
    {
        var directory = Path.GetDirectoryName(GetReferenceModuleAssemblyPath());
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Reference module assembly directory was not available.");
        }

        return directory;
    }

    private static async Task<string> CreateHostedReferenceDocsAsync(params string[] assemblies)
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-hosted-reference-docs-{Guid.NewGuid():N}");
        var request = new ReferenceDocsRequest(
            rootPath: GetRepositoryRoot(),
            outputPath: outputPath,
            configuration: GetCurrentBuildConfiguration(),
            assemblies: assemblies);
        var rendered = ReferenceDocsGenerator.Generate(request);
        await ReferenceDocsWriter.WriteAsync(rendered, overwrite: true);
        return outputPath;
    }

    private static StagedPackageResult CreateStagedReferenceModulePackage()
    {
        var workspacePath = Path.Combine(Path.GetTempPath(), $"cephalon-staged-package-{Guid.NewGuid():N}");
        var packageOutputPath = Path.Combine(workspacePath, "packages");
        var pluginsRootPath = Path.Combine(workspacePath, "plugins");
        var packageDirectoryPath = Path.Combine(pluginsRootPath, "reference-operations");

        Directory.CreateDirectory(packageOutputPath);
        Directory.CreateDirectory(pluginsRootPath);

        var projectPath = Path.Combine(
            GetRepositoryRoot(),
            "samples",
            "Cephalon.ReferenceModule.Operations",
            "Cephalon.ReferenceModule.Operations.csproj");
        var packResult = RunProcess(
            "dotnet",
            $"pack \"{projectPath}\" -c {GetCurrentBuildConfiguration()} -o \"{packageOutputPath}\" --no-build",
            GetRepositoryRoot());

        if (packResult.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet pack failed with exit code {packResult.ExitCode}.{Environment.NewLine}Output:{Environment.NewLine}{packResult.Output}{Environment.NewLine}Error:{Environment.NewLine}{packResult.Error}");
        }

        var packagePath = Directory.GetFiles(packageOutputPath, "Cephalon.ReferenceModule.Operations.*.nupkg", SearchOption.TopDirectoryOnly)
            .Single(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = CliApplication.RunAsync(
            [
                "package",
                "stage",
                "--package", packagePath,
                "--output", packageDirectoryPath
            ],
            stdout,
            stderr).GetAwaiter().GetResult();

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"cephalon package stage failed with exit code {exitCode}.{Environment.NewLine}Output:{Environment.NewLine}{stdout}{Environment.NewLine}Error:{Environment.NewLine}{stderr}");
        }

        return new StagedPackageResult(workspacePath, pluginsRootPath, packageDirectoryPath);
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
    }

    private static string GetCurrentBuildConfiguration()
    {
        return AppContext.BaseDirectory.Contains(
            $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";
    }

    private static ProcessResult RunProcess(string fileName, string arguments, string workingDirectory)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{fileName}'.");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, output, error);
    }

    private static void TryDeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset utcNow = now;

        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }

        public void Advance(TimeSpan duration)
        {
            utcNow = utcNow.Add(duration);
        }
    }

    private sealed class BackendForFrontendHostingTestModule : ModuleBase, IBackendForFrontendClientBindingContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "backend-for-frontend-hosting-tests",
            displayName: "Backend for Frontend Hosting Tests",
            description: "Provides backend-for-frontend bindings for hosting tests.",
            dependsOn: [typeof(PlatformTestModule), typeof(DiscoveryTestModule)]);

        public void RegisterClientBindings(IBackendForFrontendClientBindingRegistry bindings)
        {
            bindings.Add(new BackendForFrontendClientBindingDescriptor(
                id: "storefront-graphql",
                clientId: "storefront",
                sourceModuleId: "backend-for-frontend-hosting-tests",
                displayName: "Storefront GraphQL",
                description: "Projects the storefront GraphQL experience through the hosting test module.",
                transportId: "graphql",
                entryPoint: "/graphql/storefront",
                behaviorFilter: new BackendForFrontendBehaviorFilterDescriptor(
                    includedCapabilityKeys: ["discovery.greetings"],
                    excludedTags: ["admin"]),
                metadata: new Dictionary<string, string>
                {
                    ["document"] = "storefront"
                }));
        }
    }

    private sealed class MultiCaptureExecutionRuntimeHostingTestModule : ModuleBase, ICdcCaptureContributor, IOutboxContributor
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "multi-capture-execution-runtime-hosting-tests",
            displayName: "Multi-Capture Execution Runtime Hosting Tests",
            description: "Contributes two CDC captures so ASP.NET Core hosting can prove runtime-level reporter ambiguity.",
            version: "1.0.0",
            tags: ["cdc", "execution-runtime", "hosting-tests"]);

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void RegisterCapabilities(ICapabilityRegistry capabilities)
        {
        }

        public void RegisterCdcCaptures(ICdcCaptureRegistry cdcCaptures)
        {
            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: "multi-capture-cdc-a",
                displayName: "Multi Capture CDC A",
                description: "First CDC capture used to exercise runtime-level reporter ambiguity in hosting tests.",
                sourceModuleId: Descriptor.Id,
                provider: "postgresql",
                sourceId: "multi-capture-db-a",
                outboxId: "multi-capture-outbox-a",
                resourceIds: ["public.multi_capture_a"]));
            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: "multi-capture-cdc-b",
                displayName: "Multi Capture CDC B",
                description: "Second CDC capture used to exercise runtime-level reporter ambiguity in hosting tests.",
                sourceModuleId: Descriptor.Id,
                provider: "postgresql",
                sourceId: "multi-capture-db-b",
                outboxId: "multi-capture-outbox-b",
                resourceIds: ["public.multi_capture_b"]));
        }

        public void RegisterOutboxes(IOutboxRegistry outboxes)
        {
            outboxes.Add(new OutboxDescriptor(
                id: "multi-capture-outbox-a",
                displayName: "Multi Capture Outbox A",
                description: "Outbox for the first hosting-test CDC capture.",
                sourceModuleId: Descriptor.Id,
                provider: "relational"));
            outboxes.Add(new OutboxDescriptor(
                id: "multi-capture-outbox-b",
                displayName: "Multi Capture Outbox B",
                description: "Outbox for the second hosting-test CDC capture.",
                sourceModuleId: Descriptor.Id,
                provider: "relational"));
        }
    }

    private sealed class BoundMultiCaptureExecutionRuntimeHostingTestModule : ModuleBase, ICdcCaptureContributor, IOutboxContributor
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "bound-multi-capture-execution-runtime-hosting-tests",
            displayName: "Bound Multi-Capture Execution Runtime Hosting Tests",
            description: "Contributes two CDC captures that both bind to the same external execution runtime for hosting tests.",
            version: "1.0.0",
            tags: ["cdc", "execution-runtime", "hosting-tests", "bound"]);

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void RegisterCapabilities(ICapabilityRegistry capabilities)
        {
        }

        public void RegisterCdcCaptures(ICdcCaptureRegistry cdcCaptures)
        {
            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: "bound-multi-capture-cdc-a",
                displayName: "Bound Multi Capture CDC A",
                description: "First CDC capture used to exercise resolved execution-runtime ownership in hosting tests.",
                sourceModuleId: Descriptor.Id,
                provider: "postgresql",
                sourceId: "bound-multi-capture-db-a",
                outboxId: "bound-multi-capture-outbox-a",
                executionBinding: new CdcCaptureExecutionBindingDescriptor(
                    cdcCaptureId: "bound-multi-capture-cdc-a",
                    authoredExecutionRuntimeId: "external-cdc-runtime"),
                resourceIds: ["public.bound_multi_capture_a"]));
            cdcCaptures.Add(new CdcCaptureDescriptor(
                id: "bound-multi-capture-cdc-b",
                displayName: "Bound Multi Capture CDC B",
                description: "Second CDC capture used to exercise resolved execution-runtime ownership in hosting tests.",
                sourceModuleId: Descriptor.Id,
                provider: "postgresql",
                sourceId: "bound-multi-capture-db-b",
                outboxId: "bound-multi-capture-outbox-b",
                executionBinding: new CdcCaptureExecutionBindingDescriptor(
                    cdcCaptureId: "bound-multi-capture-cdc-b",
                    authoredExecutionRuntimeId: "external-cdc-runtime"),
                resourceIds: ["public.bound_multi_capture_b"]));
        }

        public void RegisterOutboxes(IOutboxRegistry outboxes)
        {
            outboxes.Add(new OutboxDescriptor(
                id: "bound-multi-capture-outbox-a",
                displayName: "Bound Multi Capture Outbox A",
                description: "Outbox for the first bound hosting-test CDC capture.",
                sourceModuleId: Descriptor.Id,
                provider: "relational"));
            outboxes.Add(new OutboxDescriptor(
                id: "bound-multi-capture-outbox-b",
                displayName: "Bound Multi Capture Outbox B",
                description: "Outbox for the second bound hosting-test CDC capture.",
                sourceModuleId: Descriptor.Id,
                provider: "relational"));
        }
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);

    private sealed record StagedPackageResult(
        string WorkspacePath,
        string PluginsRootPath,
        string PackageDirectoryPath);

    private sealed class GrpcSubdirectoryHandler : DelegatingHandler
    {
        private readonly string subdirectory;

        public GrpcSubdirectoryHandler(HttpMessageHandler innerHandler, string subdirectory)
            : base(innerHandler)
        {
            ArgumentNullException.ThrowIfNull(innerHandler);
            ArgumentException.ThrowIfNullOrWhiteSpace(subdirectory);

            this.subdirectory = subdirectory.StartsWith('/')
                ? subdirectory.TrimEnd('/')
                : $"/{subdirectory.TrimEnd('/')}";
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.RequestUri);

            var requestUri = request.RequestUri;
            var builder = new UriBuilder(requestUri)
            {
                Path = $"{subdirectory}{requestUri.AbsolutePath}"
            };
            request.RequestUri = builder.Uri;

            return base.SendAsync(request, cancellationToken);
        }
    }
}
