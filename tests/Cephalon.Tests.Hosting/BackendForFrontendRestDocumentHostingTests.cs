using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

public sealed class BackendForFrontendRestDocumentHostingTests
{
    [Fact]
    public async Task MapCephalonMaterializesClientAwareRestDocumentsAndScalarPagesFromSharedRuntimeTruth()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "1";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "2";
        builder.Configuration["OpenApi:DefaultVersion"] = "1";
        builder.Configuration["OpenApi:Title"] = "Cephalon Test REST API";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new BackendForFrontendRestDocumentModule());
            engine.AddBackendForFrontendClientBinding(new BackendForFrontendClientBindingDescriptor(
                id: "public-rest",
                clientId: "public",
                sourceModuleId: "tests.bff.documents.public",
                displayName: "Public REST",
                description: "Projects the public REST surface while excluding admin routes.",
                transportId: "rest-api",
                entryPoint: "/api/v1/tests/bff-documents/storefront/orders",
                behaviorFilter: new BackendForFrontendBehaviorFilterDescriptor(
                    excludedTags: ["admin"])));
            engine.AddBackendForFrontendClientBinding(new BackendForFrontendClientBindingDescriptor(
                id: "catalog-rest",
                clientId: "catalog",
                sourceModuleId: "tests.bff.documents.catalog",
                displayName: "Catalog REST",
                description: "Projects only the v2 catalog REST surface.",
                transportId: "rest-api",
                entryPoint: "/api/v2/tests/bff-documents/catalog/items",
                behaviorFilter: new BackendForFrontendBehaviorFilterDescriptor(
                    includedBehaviorIds: ["tests.bff.documents.catalog.lookup"])));
            engine.AddBackendForFrontendClientBinding(new BackendForFrontendClientBindingDescriptor(
                id: "full-rest",
                clientId: "full",
                sourceModuleId: "tests.bff.documents.full",
                displayName: "Full REST",
                description: "Projects every published REST endpoint.",
                transportId: "rest-api",
                entryPoint: "/api/v1/tests/bff-documents"));
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var documents = await client.GetFromJsonAsync<BackendForFrontendRestDocumentRuntimeDescriptor[]>("/engine/backend-for-frontend/rest-documents");
        var publicBindingDocuments = await client.GetFromJsonAsync<BackendForFrontendRestDocumentRuntimeDescriptor[]>("/engine/backend-for-frontend/rest-documents/bindings/public-rest");
        var fullClientDocuments = await client.GetFromJsonAsync<BackendForFrontendRestDocumentRuntimeDescriptor[]>("/engine/backend-for-frontend/rest-documents/clients/full");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");
        var publicV1Response = await client.GetAsync("/openapi/backend-for-frontend/bindings/public-rest/v1.json");
        var publicV2Response = await client.GetAsync("/openapi/backend-for-frontend/bindings/public-rest/v2.json");
        var missingCatalogV1Response = await client.GetAsync("/openapi/backend-for-frontend/bindings/catalog-rest/v1.json");
        var fullClientV1Response = await client.GetAsync("/openapi/backend-for-frontend/clients/full/v1.json");
        var publicScalarToggleResponse = await client.GetAsync("/scalar/backend-for-frontend/bindings/openapi-toggle.js?bindingId=public-rest");
        var catalogScalarToggleResponse = await client.GetAsync("/scalar/backend-for-frontend/bindings/openapi-toggle.js?bindingId=catalog-rest");
        var publicScalarResponse = await client.GetAsync("/scalar/backend-for-frontend/bindings/v1?bindingId=public-rest");
        var catalogScalarRootResponse = await client.GetAsync("/scalar/backend-for-frontend/bindings?bindingId=catalog-rest&culture=en");

        Assert.NotNull(documents);
        Assert.NotNull(publicBindingDocuments);
        Assert.NotNull(fullClientDocuments);
        Assert.NotNull(snapshot);
        Assert.Equal(10, documents.Length);
        Assert.Equal(documents.Length, snapshot.BackendForFrontendRestDocuments.Count);

        var publicV1Document = Assert.Single(publicBindingDocuments, static document =>
            string.Equals(document.DocumentName, "v1", StringComparison.Ordinal));
        Assert.Equal(BackendForFrontendRestDocumentRuntimeDescriptor.BindingKind, publicV1Document.Kind);
        Assert.Equal("public-rest", publicV1Document.ScopeId);
        Assert.Equal("public", publicV1Document.ClientId);
        Assert.Equal("/openapi/backend-for-frontend/bindings/public-rest/v1.json", publicV1Document.OpenApiPath);
        Assert.Equal("/scalar/backend-for-frontend/bindings/v1?bindingId=public-rest", publicV1Document.ScalarPath);
        Assert.Equal(["public-rest"], publicV1Document.BindingIds);
        Assert.Contains("tests.bff.documents", publicV1Document.SourceModuleIds);

        var publicV2Document = Assert.Single(publicBindingDocuments, static document =>
            string.Equals(document.DocumentName, "v2", StringComparison.Ordinal));
        Assert.Equal(["public-rest"], publicV2Document.BindingIds);

        Assert.Equal(2, fullClientDocuments.Length);
        Assert.All(fullClientDocuments, static document =>
            Assert.Equal(BackendForFrontendRestDocumentRuntimeDescriptor.ClientKind, document.Kind));
        Assert.Contains(snapshot.BackendForFrontendRestDocuments, static document =>
            string.Equals(document.Id, "binding::public-rest::v1", StringComparison.Ordinal));
        Assert.Contains(snapshot.BackendForFrontendRestDocuments, static document =>
            string.Equals(document.Id, "client::full::v2", StringComparison.Ordinal));

        Assert.True(publicV1Response.IsSuccessStatusCode);
        Assert.True(publicV2Response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingCatalogV1Response.StatusCode);
        Assert.True(fullClientV1Response.IsSuccessStatusCode);
        Assert.True(publicScalarToggleResponse.IsSuccessStatusCode);
        Assert.True(catalogScalarToggleResponse.IsSuccessStatusCode);
        Assert.True(publicScalarResponse.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Redirect, catalogScalarRootResponse.StatusCode);
        Assert.NotNull(catalogScalarRootResponse.Headers.Location);
        Assert.Equal(
            "/scalar/backend-for-frontend/bindings/v2?bindingId=catalog-rest&culture=en",
            catalogScalarRootResponse.Headers.Location!.OriginalString);

        using var publicV1DocumentJson = JsonDocument.Parse(await publicV1Response.Content.ReadAsStringAsync());
        using var publicV2DocumentJson = JsonDocument.Parse(await publicV2Response.Content.ReadAsStringAsync());
        using var fullClientV1DocumentJson = JsonDocument.Parse(await fullClientV1Response.Content.ReadAsStringAsync());

        var publicV1Paths = publicV1DocumentJson.RootElement.GetProperty("paths");
        Assert.True(publicV1Paths.TryGetProperty("/api/v1/tests/bff-documents/mobile/orders/{orderId}", out _));
        Assert.True(publicV1Paths.TryGetProperty("/api/v1/tests/bff-documents/storefront/orders/{orderId}", out _));
        Assert.False(publicV1Paths.TryGetProperty("/api/v1/tests/bff-documents/admin/orders/{orderId}", out _));
        Assert.False(publicV1Paths.TryGetProperty("/api/v2/tests/bff-documents/catalog/items/{itemId}", out _));
        Assert.Contains("Public REST", publicV1DocumentJson.RootElement.GetProperty("info").GetProperty("title").GetString(), StringComparison.Ordinal);
        Assert.Equal(
            BackendForFrontendRestDocumentRuntimeDescriptor.BindingKind,
            publicV1DocumentJson.RootElement
                .GetProperty("x-cephalon-backend-for-frontend")
                .GetProperty("kind")
                .GetString());
        Assert.DoesNotContain("BackendForFrontendRestDocumentAdminOutput", await publicV1Response.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var publicV1Tags = publicV1DocumentJson.RootElement.GetProperty("tags");
        Assert.DoesNotContain(
            publicV1Tags.EnumerateArray().Select(static tag => tag.GetProperty("name").GetString()),
            static tag => string.Equals(tag, "admin", StringComparison.OrdinalIgnoreCase));

        var publicV2Paths = publicV2DocumentJson.RootElement.GetProperty("paths");
        Assert.True(publicV2Paths.TryGetProperty("/api/v2/tests/bff-documents/catalog/items/{itemId}", out _));
        Assert.False(publicV2Paths.TryGetProperty("/api/v1/tests/bff-documents/storefront/orders/{orderId}", out _));

        var fullClientV1Paths = fullClientV1DocumentJson.RootElement.GetProperty("paths");
        Assert.True(fullClientV1Paths.TryGetProperty("/api/v1/tests/bff-documents/admin/orders/{orderId}", out _));
        Assert.Contains("BackendForFrontendRestDocumentAdminOutput", await fullClientV1Response.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var publicScalarTogglePayload = await publicScalarToggleResponse.Content.ReadAsStringAsync();
        var catalogScalarTogglePayload = await catalogScalarToggleResponse.Content.ReadAsStringAsync();
        var publicScalarPayload = await publicScalarResponse.Content.ReadAsStringAsync();
        Assert.Contains("configuredDocumentNames = [\"v1\",\"v2\"]", publicScalarTogglePayload, StringComparison.Ordinal);
        Assert.Contains("configuredDefaultDocumentName = \"v1\"", publicScalarTogglePayload, StringComparison.Ordinal);
        Assert.Contains("configuredDocumentNames = [\"v2\"]", catalogScalarTogglePayload, StringComparison.Ordinal);
        Assert.Contains("configuredDefaultDocumentName = \"v2\"", catalogScalarTogglePayload, StringComparison.Ordinal);
        Assert.Contains("/scalar/backend-for-frontend/bindings/openapi-toggle.js?v=", publicScalarPayload, StringComparison.Ordinal);
        Assert.Contains("bindingId=public-rest", publicScalarPayload, StringComparison.Ordinal);
        Assert.Contains("backend-for-frontend/bindings/public-rest/v1.json", publicScalarPayload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapCephalonKeepsBackendForFrontendRestDocumentsAlignedWithCustomOpenApiAndScalarPrefixes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "1";
        builder.Configuration["OpenApi:DefaultVersion"] = "1";
        builder.Configuration["OpenApi:RoutePattern"] = "/specs/{documentName}.json";
        builder.Configuration["OpenApi:Scalar:RoutePrefix"] = "/docs/api-reference";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new BackendForFrontendRestDocumentModule());
            engine.AddBackendForFrontendClientBinding(new BackendForFrontendClientBindingDescriptor(
                id: "public-rest",
                clientId: "public",
                sourceModuleId: "tests.bff.documents.public",
                displayName: "Public REST",
                description: "Projects the public REST surface while excluding admin routes.",
                transportId: "rest-api",
                entryPoint: "/service-api/v1/tests/bff-documents/storefront/orders",
                behaviorFilter: new BackendForFrontendBehaviorFilterDescriptor(
                    excludedTags: ["admin"])));
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var documentRouteResponse = await client.GetAsync("/specs/backend-for-frontend/bindings/public-rest/v1.json");
        var legacyDocumentRouteResponse = await client.GetAsync("/openapi/backend-for-frontend/bindings/public-rest/v1.json");
        var scalarResponse = await client.GetAsync("/docs/api-reference/backend-for-frontend/bindings/v1?bindingId=public-rest");
        var scalarToggleResponse = await client.GetAsync("/docs/api-reference/backend-for-frontend/bindings/openapi-toggle.js?bindingId=public-rest");
        var engineDocuments = await client.GetFromJsonAsync<BackendForFrontendRestDocumentRuntimeDescriptor[]>("/engine/backend-for-frontend/rest-documents/bindings/public-rest");

        Assert.True(documentRouteResponse.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.NotFound, legacyDocumentRouteResponse.StatusCode);
        Assert.True(scalarResponse.IsSuccessStatusCode);
        Assert.True(scalarToggleResponse.IsSuccessStatusCode);
        Assert.NotNull(engineDocuments);
        Assert.Single(engineDocuments);
        Assert.Equal("/specs/backend-for-frontend/bindings/public-rest/v1.json", engineDocuments[0].OpenApiPath);
        Assert.Equal("/docs/api-reference/backend-for-frontend/bindings/v1?bindingId=public-rest", engineDocuments[0].ScalarPath);

        var scalarPayload = await scalarResponse.Content.ReadAsStringAsync();
        var togglePayload = await scalarToggleResponse.Content.ReadAsStringAsync();
        Assert.Contains("/docs/api-reference/backend-for-frontend/bindings/openapi-toggle.js?v=", scalarPayload, StringComparison.Ordinal);
        Assert.Contains("specs/backend-for-frontend/bindings/public-rest/v1.json", scalarPayload, StringComparison.Ordinal);
        Assert.Contains("configuredScalarRoutePrefix = \"/docs/api-reference/backend-for-frontend/bindings\"", togglePayload, StringComparison.Ordinal);
    }

    private sealed class BackendForFrontendRestDocumentModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "tests.bff.documents",
            displayName: "Backend for Frontend REST Document Tests",
            description: "Publishes versioned REST endpoints for backend-for-frontend documentation coverage.");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/bff-documents/mobile/orders")
                .ApiVersion(1)
                .WithTagName("mobile")
                .MapProfile<GetBackendForFrontendRestDocumentMobileOrderBehavior>();

            behaviors.Group("/tests/bff-documents/storefront/orders")
                .ApiVersion(1)
                .WithTagName("storefront")
                .MapProfile<GetBackendForFrontendRestDocumentStorefrontOrderBehavior>(builder =>
                    builder.RequireCapability("orders.read"));

            behaviors.Group("/tests/bff-documents/admin/orders")
                .ApiVersion(1)
                .WithTagName("admin")
                .MapProfile<GetBackendForFrontendRestDocumentAdminOrderBehavior>(builder =>
                    builder.RequireCapability("admin.read"));

            behaviors.Group("/tests/bff-documents/catalog/items")
                .ApiVersion(2)
                .WithTagName("catalog")
                .MapProfile<GetBackendForFrontendRestDocumentCatalogItemBehavior>();
        }
    }

    [AppBehavior("tests.bff.documents.mobile.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 1)]
    private sealed class GetBackendForFrontendRestDocumentMobileOrderBehavior : IAppBehavior<BackendForFrontendRestDocumentOrderInput, BackendForFrontendRestDocumentMobileOutput>
    {
        public Task<BackendForFrontendRestDocumentMobileOutput> HandleAsync(
            BackendForFrontendRestDocumentOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new BackendForFrontendRestDocumentMobileOutput(input.OrderId, "mobile"));
        }
    }

    [AppBehavior("tests.bff.documents.storefront.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 1)]
    private sealed class GetBackendForFrontendRestDocumentStorefrontOrderBehavior : IAppBehavior<BackendForFrontendRestDocumentOrderInput, BackendForFrontendRestDocumentStorefrontOutput>
    {
        public Task<BackendForFrontendRestDocumentStorefrontOutput> HandleAsync(
            BackendForFrontendRestDocumentOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new BackendForFrontendRestDocumentStorefrontOutput(input.OrderId, "storefront"));
        }
    }

    [AppBehavior("tests.bff.documents.admin.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 1)]
    private sealed class GetBackendForFrontendRestDocumentAdminOrderBehavior : IAppBehavior<BackendForFrontendRestDocumentOrderInput, BackendForFrontendRestDocumentAdminOutput>
    {
        public Task<BackendForFrontendRestDocumentAdminOutput> HandleAsync(
            BackendForFrontendRestDocumentOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new BackendForFrontendRestDocumentAdminOutput(input.OrderId, "admin"));
        }
    }

    [AppBehavior("tests.bff.documents.catalog.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{itemId}", ApiVersionMajor = 2)]
    private sealed class GetBackendForFrontendRestDocumentCatalogItemBehavior : IAppBehavior<BackendForFrontendRestDocumentCatalogInput, BackendForFrontendRestDocumentCatalogOutput>
    {
        public Task<BackendForFrontendRestDocumentCatalogOutput> HandleAsync(
            BackendForFrontendRestDocumentCatalogInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new BackendForFrontendRestDocumentCatalogOutput(input.ItemId, "catalog"));
        }
    }

    private sealed record BackendForFrontendRestDocumentOrderInput(string OrderId);

    private sealed record BackendForFrontendRestDocumentCatalogInput(string ItemId);

    private sealed record BackendForFrontendRestDocumentMobileOutput(string OrderId, string Surface);

    private sealed record BackendForFrontendRestDocumentStorefrontOutput(string OrderId, string Surface);

    private sealed record BackendForFrontendRestDocumentAdminOutput(string OrderId, string Surface);

    private sealed record BackendForFrontendRestDocumentCatalogOutput(string ItemId, string Surface);
}
