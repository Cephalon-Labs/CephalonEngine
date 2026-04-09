using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Modules;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

public sealed class BehaviorRestOpenApiTests
{
    [Fact]
    public async Task ModuleOwnedRestEndpointsStayDocumentedWhileGenericBehaviorRoutesRemainNonRestOnly()
    {
        const string rpcRoute = "/json-rpc/v2/rest/helper/echo";
        const string helperRoute = "/api/v2/tests/cart/{cartId}/items";
        const string helperTagName = "Test Cart API";
        const string helperTagDescription = "Commands and queries exposed by the test cart REST surface.";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["Engine:Transports:1"] = "BehaviorHttp";
        builder.Configuration["OpenApi:EnabledVersions:0"] = "1";
        builder.Configuration["OpenApi:EnabledVersions:1"] = "2";
        builder.Configuration["OpenApi:DefaultVersion"] = "2";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new DocumentedRestHelperModule(helperTagName, helperTagDescription));
            engine.AddBehaviors(behaviors =>
            {
                behaviors.Register<RestHelperEchoBehavior>(topology => topology
                    .AsDirect()
                    .ViaHttpJsonRpc());
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var rpcResponse = await client.PostAsJsonAsync(
            rpcRoute,
            new { jsonrpc = "2.0", method = "handle", @params = new RestHelperEchoInput("cart-123", "Keyboard", 2), id = 7 });
        using var v2Document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v2.json"));

        Assert.True(rpcResponse.IsSuccessStatusCode);
        Assert.False(v2Document.RootElement.GetProperty("paths").TryGetProperty(rpcRoute, out _));
        Assert.True(v2Document.RootElement.GetProperty("paths").TryGetProperty(helperRoute, out _));

        var tag = v2Document.RootElement.GetProperty("tags")
            .EnumerateArray()
            .FirstOrDefault(candidate =>
                string.Equals(candidate.GetProperty("name").GetString(), helperTagName, StringComparison.Ordinal));

        Assert.Equal(helperTagName, tag.GetProperty("name").GetString());
        Assert.Equal(helperTagDescription, tag.GetProperty("description").GetString());
    }

    [Fact]
    public async Task GenericRestRoutesAreNotMappedAnymore()
    {
        const string genericRoute = "/api/v2/rest/helper/echo";
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["Engine:Transports:1"] = "BehaviorHttp";
        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(behaviors =>
            {
                behaviors.Register<RestHelperEchoBehavior>(topology => topology
                    .AsDirect()
                    .ViaHttpJsonRpc());
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(genericRoute, new { productName = "Keyboard" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [AppBehavior("rest.helper.echo")]
    private sealed class RestHelperEchoBehavior : IAppBehavior<RestHelperEchoInput, RestHelperEchoOutput>
    {
        public Task<RestHelperEchoOutput> HandleAsync(
            RestHelperEchoInput input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RestHelperEchoOutput(input.CartId, input.ProductName, input.Quantity));
        }
    }

    private sealed record RestHelperEchoInput(string CartId, string ProductName, int Quantity);

    private sealed record RestHelperEchoOutput(string CartId, string ProductName, int Quantity);

    private sealed class DocumentedRestHelperModule(string tagName, string tagDescription) : ModuleBase, IEndpointModule
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.cart",
            displayName: "Test Cart",
            description: "Test module for behavior-aware REST endpoint OpenAPI coverage.",
            version: "2.4.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapBehaviorRestGroup(this, "/tests/cart")
                .WithTagName(tagName)
                .WithTagDescription(tagDescription);
            group.MapBehaviorPost<RestHelperEchoBehavior>("/{cartId}/items");
        }
    }
}
