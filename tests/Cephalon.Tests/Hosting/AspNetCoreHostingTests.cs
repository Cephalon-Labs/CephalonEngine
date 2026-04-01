using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.AppModel.Scaffolding;
using Cephalon.Abstractions.Health;
using Cephalon.Abstractions.Localization;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Transports;
using Cephalon.Agentics.Registration;
using Cephalon.Agentics.Services;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Documentation;
using Cephalon.AspNetCore.Grpc.Contracts.Discovery;
using Cephalon.AspNetCore.Grpc.Hosting;
using Cephalon.AspNetCore.JsonRpc.Hosting;
using Cephalon.Edge.Registration;
using Cephalon.Edge.Services;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Manifest;
using Cephalon.Engine.Runtime;
using Cephalon.Engine.Trust;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.Abstractions.Capabilities;
using Cephalon.ReferenceModule.Operations.Registration;
using Cephalon.ReferenceDocs.Generation;
using Cephalon.ReferenceDocs.IO;
using Cephalon.Retrieval.Registration;
using Cephalon.Retrieval.Services;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Cephalon.Tests.Support;

namespace Cephalon.Tests.Hosting;

public sealed class AspNetCoreHostingTests
{
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
    public async Task MapCephalonExposesRuntimeAndModuleRoutes()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularVerticalSlice";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:0"] = "StrategyPattern";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:1"] = "JsonRpc";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:2"] = "Grpc";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:3"] = "ServerSentEvents";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:4"] = "WebSocket";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "AgenticWorkloads";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:1"] = "EventDrivenIntegration";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:2"] = "RealtimeExperience";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:3"] = "EdgeNativeDelivery";
        builder.Configuration[$"{EngineSettings.SectionName}:Options:Capabilities:platform.clock"] = "false";
        builder.Configuration["OpenApi:Title"] = "Cephalon Test REST API";
        builder.Configuration["OpenApi:SecuritySchemes:0:Name"] = "Bearer";
        builder.Configuration["OpenApi:SecuritySchemes:0:Type"] = "Http";
        builder.Configuration["OpenApi:SecuritySchemes:0:Scheme"] = "bearer";
        builder.Configuration["OpenApi:SecuritySchemes:0:BearerFormat"] = "JWT";
        builder.Configuration["OpenApi:SecuritySchemes:0:In"] = "Header";
        builder.Configuration["OpenApi:SecuritySchemes:0:Description"] = "Bearer token authentication.";
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
        var rpcResponse = await client.PostAsJsonAsync("/rpc/discovery", new
        {
            jsonRpc = "2.0",
            method = "discovery.hello",
            @params = new Dictionary<string, string?> { ["name"] = "Codex" },
            id = "req-1"
        });
        var rpcPayload = await rpcResponse.Content.ReadAsStringAsync();
        var sseResponse = await client.GetAsync("/events/discovery/principles");
        var ssePayload = await sseResponse.Content.ReadAsStringAsync();
        var grpcHttpClient = app.GetTestClient();
        grpcHttpClient.DefaultRequestVersion = HttpVersion.Version20;
        grpcHttpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        using var channel = GrpcChannel.ForAddress(grpcHttpClient.BaseAddress!, new GrpcChannelOptions
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

        Assert.NotNull(scaffold);
        Assert.Contains(scaffold.Projects, project =>
            project.Id == "host" &&
            project.Packages.Contains("Cephalon.Agentics", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Eventing", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.Edge", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.JsonRpc", StringComparer.OrdinalIgnoreCase) &&
            project.Packages.Contains("Cephalon.AspNetCore.Grpc", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(scaffold.Folders, folder =>
            folder.ProjectId == "module" &&
            folder.PathTemplate == "Features/{FeatureName}/Endpoints");

        Assert.NotNull(patterns);
        Assert.Contains(patterns, pattern => pattern.Id == "strategy-pattern");
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
        Assert.Contains(capabilities, capability => capability.Key == "eventing.publish");
        Assert.Contains(capabilities, capability => capability.Key == "eventing.channels");
        Assert.Contains(capabilities, capability => capability.Key == "edge.offline");
        Assert.Contains(capabilities, capability => capability.Key == "edge.nodes");

        Assert.NotNull(transports);
        Assert.Contains(transports, transport => transport.Id == "rest-api");
        Assert.Contains(transports, transport => transport.Id == "json-rpc");
        Assert.Contains(transports, transport => transport.Id == "grpc");
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
        Assert.False(paths.TryGetProperty("/rpc/discovery", out _));
        Assert.False(paths.TryGetProperty("/events/discovery/principles", out _));
        Assert.False(paths.TryGetProperty("/engine", out _));
        Assert.True(securitySchemes.TryGetProperty("Bearer", out var bearerScheme));
        Assert.Equal("http", bearerScheme.GetProperty("type").GetString());
        Assert.Equal("bearer", bearerScheme.GetProperty("scheme").GetString());

        var greetingSchema = schemas.EnumerateObject()
            .FirstOrDefault(property => property.Name.Contains("GreetingEnvelope", StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(greetingSchema.Name));
        Assert.Contains(
            "Discovery greeting payload returned by the REST surface.",
            greetingSchema.Value.GetProperty("description").GetString(),
            StringComparison.Ordinal);

        Assert.True(scalarConfigResponse.IsSuccessStatusCode);
        Assert.Equal("application/javascript", scalarConfigResponse.Content.Headers.ContentType?.MediaType);
        Assert.Contains("export default", scalarConfigPayload, StringComparison.Ordinal);
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
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new TechnologyPackContributionModule());
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
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
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
        Assert.Equal(4, snapshot.TechnologySurfaces.Count);
        Assert.NotNull(surfaces);
        Assert.Equal(4, surfaces.Length);
        Assert.NotNull(eventingSurfaces);
        Assert.Single(eventingSurfaces);

        var agentics = Assert.Single(surfaces, surface => surface.TechnologyId == "agentic-workloads");
        Assert.Contains(agentics.Entries, entry => entry.Id == "planner");
        Assert.Contains(agentics.Entries, entry => entry.Id == "analyst");

        var eventing = Assert.Single(surfaces, surface => surface.TechnologyId == "event-driven-integration");
        Assert.Contains(eventing.Entries, entry => entry.Id == "orders");
        Assert.Contains(eventing.Entries, entry => entry.Id == "audit");
        Assert.Equal("event-channels", eventingSurfaces[0].SurfaceId);
        Assert.Contains(
            snapshot.TechnologySurfaces.Single(surface => surface.TechnologyId == "event-driven-integration").Entries,
            entry => entry.Id == "audit");

        var retrieval = Assert.Single(surfaces, surface => surface.TechnologyId == "knowledge-retrieval");
        Assert.Contains(retrieval.Entries, entry => entry.Id == "docs");
        Assert.Contains(retrieval.Entries, entry => entry.Id == "runbooks");

        var edge = Assert.Single(surfaces, surface => surface.TechnologyId == "edge-native-delivery");
        Assert.Contains(edge.Entries, entry => entry.Id == "storefront-edge");
        Assert.Contains(edge.Entries, entry => entry.Id == "warehouse-edge");
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

        Assert.NotNull(failurePolicy);
        Assert.Equal(StartupFailureBehavior.CaptureOnly, failurePolicy.StartupFailureBehavior);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, livenessResponse.StatusCode);
        using var livenessDocument = JsonDocument.Parse(livenessPayload);
        Assert.Equal("Unhealthy", livenessDocument.RootElement.GetProperty("status").GetString());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, readinessResponse.StatusCode);
        using var readinessDocument = JsonDocument.Parse(readinessPayload);
        Assert.Equal("Unhealthy", readinessDocument.RootElement.GetProperty("status").GetString());

        Assert.True(diagnosticsResponse.IsSuccessStatusCode);
        using var diagnosticsDocument = JsonDocument.Parse(diagnosticsPayload);
        Assert.Equal((int)RuntimeHealthState.Unhealthy, diagnosticsDocument.RootElement.GetProperty("liveness").GetProperty("state").GetInt32());
        Assert.Equal((int)RuntimeHealthState.Unhealthy, diagnosticsDocument.RootElement.GetProperty("readiness").GetProperty("state").GetInt32());
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
        var rpcResponse = await client.PostAsJsonAsync("/rpc/discovery", new
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
        Assert.Null(package.SignatureKeyId);
        Assert.Null(package.SignatureFingerprint);
        Assert.Empty(package.Signatures);
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
        Assert.Null(package.SignatureKeyId);
        Assert.Equal("cephalon-labs-reference-operations", package.SignatureFingerprint);
        var signature = Assert.Single(package.Signatures);
        Assert.Equal("Cephalon Labs Build", signature.Signer);
        Assert.False(signature.IsVerified);
        Assert.False(package.IsSignatureVerified);
        Assert.Contains("no signature value", package.SignatureVerificationReason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("operations", package.Modules);
        Assert.Contains("Operations module is running.", operationsStatus, StringComparison.Ordinal);
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
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var trust = await client.GetFromJsonAsync<TrustSnapshot>("/engine/trust-policy");

        Assert.Equal(HttpStatusCode.Forbidden, secretResponse.StatusCode);
        Assert.Contains("Capability access denied", secretPayload, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(capabilities);
        Assert.DoesNotContain(capabilities, capability => capability.Key == "restricted.secret");
        Assert.NotNull(trust);
        Assert.Contains(trust.Capabilities, decision =>
            decision.CapabilityKey == "restricted.secret" &&
            decision.Access == CapabilityAccess.Denied &&
            !decision.IsAllowed);
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
}
