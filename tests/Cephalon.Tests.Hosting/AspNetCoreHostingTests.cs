using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Net;
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
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Technologies;
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
using Cephalon.Edge.Registration;
using Cephalon.Edge.Services;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Manifest;
using Cephalon.Engine.Runtime;
using Cephalon.Engine.Trust;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Cli;
using Cephalon.ReferenceModule.Operations.Registration;
using Cephalon.ReferenceDocs.Generation;
using Cephalon.ReferenceDocs.IO;
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
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:1"] = "CQRS";
        builder.Configuration[$"{EngineSettings.SectionName}:Patterns:2"] = "Outbox";
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
        var databases = await client.GetFromJsonAsync<DatabaseTopologySelection>("/engine/databases");
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
        var graphQlSdlResponse = await client.GetAsync("/graphql?sdl");
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
        Assert.Equal("WriteDb", appModel.Databases.Write.ConnectionStringName);
        Assert.Equal("ReadDb", appModel.Databases.Read.ConnectionStringName);
        Assert.Equal("outbox01", appModel.Databases.Outbox.Schema);
        Assert.Equal(["history", "outbox", "write"], appModel.Databases.Migrations.Targets);

        Assert.NotNull(databases);
        Assert.Equal("PostgreSql", databases.Write.Provider);
        Assert.Equal("WriteDb", databases.Write.ConnectionStringName);
        Assert.True(databases.Runtime.EnableRetryOnFailure);
        Assert.Equal(5, databases.Runtime.MaxRetryCount);
        Assert.Equal("HistoryDb", databases.History.ConnectionStringName);

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
        Assert.Contains("scalarRoutePrefix", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("encodeURIComponent(documentName)", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("hashchange", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("hashSectionRoots", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("isVersionDocumentName", scalarConfigPayload, StringComparison.Ordinal);
        Assert.Contains("hashCarriesVersionDocument", scalarConfigPayload, StringComparison.Ordinal);
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
    public async Task MapCephalonSupportsNamedOpenApiDocumentsAndScalarPages()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "1";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "2";
        builder.Configuration["OpenApi:DefaultVersion"] = "2";
        builder.Configuration["OpenApi:Version"] = "2026.04";
        builder.AddCephalon(cephalon =>
        {
            cephalon.AddModule(new PlatformTestModule());
            cephalon.AddModule(new DiscoveryTestModule());
        });

        await using var app = builder.Build();
        app.MapGet("/api/openapi-documents/orders/{orderId}", (string orderId) => TypedResults.Ok(new { orderId }))
            .WithName("GetOpenApiDocumentOrder")
            .WithGroupName("v2");
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var v1Response = await client.GetAsync("/openapi/v1.json");
        var v2Response = await client.GetAsync("/openapi/v2.json");
        var scalarRootRedirectResponse = await client.GetAsync("/scalar?culture=en");
        var scalarRootResponse = await client.GetAsync("/scalar/?culture=en");
        var scalarV2Response = await client.GetAsync("/scalar/v2");

        Assert.True(v1Response.IsSuccessStatusCode);
        Assert.True(v2Response.IsSuccessStatusCode);
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

        var scalarRootPayload = await scalarRootResponse.Content.ReadAsStringAsync();
        var scalarV2Payload = await scalarV2Response.Content.ReadAsStringAsync();
        Assert.Contains("Scalar", scalarRootPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"title\":\"v1\"", scalarRootPayload, StringComparison.Ordinal);
        Assert.Contains("openapi/v1.json", scalarRootPayload, StringComparison.Ordinal);
        Assert.Contains("\"title\":\"v2\"", scalarRootPayload, StringComparison.Ordinal);
        Assert.Contains("openapi/v2.json", scalarRootPayload, StringComparison.Ordinal);
        Assert.Contains("Scalar", scalarV2Payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MapCephalonSupportsConfigurableOpenApiScalarAndRestRoutePrefixes()
    {
        var builder = WebApplication.CreateSlimBuilder();
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

    [Fact]
    public async Task MapCephalonSupportsEmptyRestPrefix()
    {
        var builder = WebApplication.CreateSlimBuilder();
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

    [Fact]
    public async Task MapCephalonAppliesGlobalOpenApiInfoVersionOverrideToSingleDocumentHosts()
    {
        var builder = WebApplication.CreateSlimBuilder();
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
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
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
        Assert.Equal(5, snapshot.TechnologySurfaces.Count);
        Assert.Contains(snapshot.DiagnosticsConventions, convention => convention.Source == "Cephalon.Eventing");
        Assert.NotNull(surfaces);
        Assert.Equal(5, surfaces.Length);
        Assert.NotNull(eventingSurfaces);
        Assert.Equal(2, eventingSurfaces.Length);

        var agentics = Assert.Single(surfaces, surface => surface.TechnologyId == "agentic-workloads");
        Assert.Contains(agentics.Entries, entry => entry.Id == "planner");
        Assert.Contains(agentics.Entries, entry => entry.Id == "analyst");
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
        Assert.Contains(retrieval.Entries, entry => entry.Id == "runbooks");

        var edge = Assert.Single(surfaces, surface => surface.TechnologyId == "edge-native-delivery");
        Assert.Contains(edge.Entries, entry => entry.Id == "storefront-edge");
        Assert.Contains(edge.Entries, entry => entry.Id == "warehouse-edge");
    }

    [Fact]
    public async Task MapCephalonExposesPhase8ProjectionInboxOutboxAndAuthorizationCatalogs()
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
        Assert.Single(snapshot.Projections);
        Assert.Single(snapshot.Inboxes);
        Assert.Single(snapshot.Outboxes);
        Assert.Single(snapshot.AuditStores);
        Assert.Equal(2, snapshot.AuthorizationPolicies.Count);
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
        builder.Configuration[$"{EngineSettings.SectionName}:FailurePolicy:ManualRestartBackoff"] = "00:00:00.200";
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
        Assert.Equal(TimeSpan.FromMilliseconds(200), failurePolicy.ManualRestartBackoff);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, livenessResponse.StatusCode);
        using var livenessDocument = JsonDocument.Parse(livenessPayload);
        Assert.Equal("Unhealthy", livenessDocument.RootElement.GetProperty("status").GetString());
        if (livenessDocument.RootElement.TryGetProperty("entries", out var livenessEntries) &&
            livenessEntries.TryGetProperty("cephalon.liveness", out var livenessEntry) &&
            livenessEntry.TryGetProperty("data", out var livenessData))
        {
            Assert.Equal("restart-backoff", livenessData.GetProperty("activeWindow").GetString());
            Assert.True(livenessData.TryGetProperty("restartAvailableAtUtc", out _));
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
        builder.Configuration[$"{EngineSettings.SectionName}:FailurePolicy:StartupReadinessDelay"] = "00:00:00.200";
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
        Assert.Equal(TimeSpan.FromMilliseconds(200), failurePolicy.StartupReadinessDelay);

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

        await Task.Delay(TimeSpan.FromMilliseconds(250));

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
