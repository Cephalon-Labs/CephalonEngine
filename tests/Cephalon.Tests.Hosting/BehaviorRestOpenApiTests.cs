using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Resilience;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

public sealed class BehaviorRestOpenApiTests
{
    private static readonly JsonSerializerOptions WebJsonSerializerOptions = new(JsonSerializerDefaults.Web);

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

    [Fact]
    public async Task ResultEnvelopeWrapsExceptionHandlerProblemDetailsForRestRequestsWhenEnabled()
    {
        const string route = "/api/v1/tests/problem";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["ApiRoutes:ResultEnvelope:Enabled"] = "true";
        builder.AddCephalon();

        await using var app = builder.Build();
        app.UseExceptionHandler();
        app.MapGet(route, ThrowUnhandledRestProblem);
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync(route);
        var payloadText = await response.Content.ReadAsStringAsync();
        using var payload = JsonDocument.Parse(payloadText);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(
            "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            payload.RootElement.GetProperty("type").GetString());
        Assert.Equal(500, payload.RootElement.GetProperty("status").GetInt32());
        Assert.False(payload.RootElement.GetProperty("success").GetBoolean());
        Assert.True(payload.RootElement.TryGetProperty("errors", out var errors), payloadText);
        Assert.Single(errors.EnumerateArray());
        Assert.False(payload.RootElement.TryGetProperty("status_code", out _), payloadText);
    }

    [Fact]
    public async Task ResultEnvelopeDisabledKeepsExceptionHandlerProblemDetailsForRestRequests()
    {
        const string route = "/api/v1/tests/problem";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["ApiRoutes:ResultEnvelope:Enabled"] = "false";
        builder.AddCephalon();

        await using var app = builder.Build();
        app.UseExceptionHandler();
        app.MapGet(route, ThrowUnhandledRestProblem);
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync(route);
        var payloadText = await response.Content.ReadAsStringAsync();
        using var payload = JsonDocument.Parse(payloadText);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(500, payload.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            payload.RootElement.GetProperty("type").GetString());
        Assert.False(payload.RootElement.TryGetProperty("success", out _), payloadText);
        Assert.False(payload.RootElement.TryGetProperty("errors", out _), payloadText);
        Assert.False(payload.RootElement.TryGetProperty("status_code", out _), payloadText);
    }

    [Fact]
    public async Task BehaviorResultResponsesCanBeWrappedInResultModelEnvelopes()
    {
        const string route = "/api/v1/tests/results/widgets/widget-1";
        const string missingRoute = "/api/v1/tests/results/widgets/missing";
        const string invalidRoute = "/api/v1/tests/results/widgets/invalid";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["ApiRoutes:ResultEnvelope:Enabled"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new EnvelopeResultModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var successHttpResponse = await client.GetAsync(route);
        var successPayload = await successHttpResponse.Content.ReadAsStringAsync();
        var successResponse = successHttpResponse.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<ResultModel<EnvelopeLookupOutput>>(successPayload, WebJsonSerializerOptions)
            : null;
        var missingHttpResponse = await client.GetAsync(missingRoute);
        var missingPayload = await missingHttpResponse.Content.ReadAsStringAsync();
        var missingResponse = await missingHttpResponse.Content.ReadFromJsonAsync<ResultModelError>();
        var invalidHttpResponse = await client.GetAsync(invalidRoute);
        var invalidPayload = await invalidHttpResponse.Content.ReadAsStringAsync();
        var invalidResponse = await invalidHttpResponse.Content.ReadFromJsonAsync<ResultModelError>();

        Assert.True(successHttpResponse.IsSuccessStatusCode, successPayload);
        Assert.NotNull(successResponse);
        Assert.True(successResponse!.Success);
        Assert.Equal(200, successResponse.StatusCode);
        Assert.Equal("Widget resolved.", successResponse.Message);
        Assert.NotNull(successResponse.Data);
        Assert.Equal("widget-1", successResponse.Data!.WidgetId);
        Assert.Contains("\"status\":200", successPayload, StringComparison.Ordinal);
        Assert.DoesNotContain("\"status_code\":", successPayload, StringComparison.Ordinal);

        Assert.Equal(HttpStatusCode.NotFound, missingHttpResponse.StatusCode);
        Assert.NotNull(missingResponse);
        Assert.False(missingResponse!.Success);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.5", missingResponse.Type);
        Assert.Equal(404, missingResponse.StatusCode);
        Assert.NotNull(missingResponse.Errors);
        Assert.Single(missingResponse.Errors!);
        Assert.Equal("tests.widgets.not_found", missingResponse.Errors[0].Key);
        Assert.Contains("\"type\":\"https://tools.ietf.org/html/rfc9110#section-15.5.5\"", missingPayload, StringComparison.Ordinal);
        Assert.Contains("\"status\":404", missingPayload, StringComparison.Ordinal);
        Assert.DoesNotContain("\"status_code\":", missingPayload, StringComparison.Ordinal);
        Assert.Contains("\"severity\":", missingPayload, StringComparison.Ordinal);
        Assert.DoesNotContain("\"error\":", missingPayload, StringComparison.Ordinal);
        Assert.Contains("\"errors\":[", missingPayload, StringComparison.Ordinal);

        Assert.Equal(HttpStatusCode.BadRequest, invalidHttpResponse.StatusCode);
        Assert.NotNull(invalidResponse);
        Assert.False(invalidResponse!.Success);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", invalidResponse.Type);
        Assert.Equal(400, invalidResponse.StatusCode);
        Assert.NotNull(invalidResponse.Errors);
        Assert.Equal(2, invalidResponse.Errors!.Count);
        Assert.Contains(invalidResponse.Errors, error => error.Key == "tests.widgets.widget_id.required");
        Assert.Contains(invalidResponse.Errors, error => error.Key == "tests.widgets.widget_id.length");
        Assert.Contains("\"type\":\"https://tools.ietf.org/html/rfc9110#section-15.5.1\"", invalidPayload, StringComparison.Ordinal);
        Assert.Contains("\"status\":400", invalidPayload, StringComparison.Ordinal);
        Assert.DoesNotContain("\"status_code\":", invalidPayload, StringComparison.Ordinal);
        Assert.DoesNotContain("\"error\":", invalidPayload, StringComparison.Ordinal);
        Assert.Contains("\"errors\":[", invalidPayload, StringComparison.Ordinal);

        var successSchemaReference = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/tests/results/widgets/{widgetId}")
            .GetProperty("get")
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();

        Assert.NotNull(successSchemaReference);
        var componentName = successSchemaReference!.Split('/').Last();
        var successSchema = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty(componentName);

        Assert.True(successSchema.GetProperty("properties").TryGetProperty("data", out var dataSchema));
        Assert.True(dataSchema.TryGetProperty("$ref", out var dataSchemaReference), dataSchema.ToString());
        Assert.Contains("EnvelopeLookupOutput", dataSchemaReference.GetString(), StringComparison.Ordinal);
        Assert.True(successSchema.GetProperty("properties").TryGetProperty("status", out _));
        Assert.False(successSchema.GetProperty("properties").TryGetProperty("status_code", out _));
        Assert.False(successSchema.GetProperty("properties").TryGetProperty("error", out _));
        Assert.False(successSchema.GetProperty("properties").TryGetProperty("errors", out _));

        var documentedResponses = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/tests/results/widgets/{widgetId}")
            .GetProperty("get")
            .GetProperty("responses");

        Assert.True(documentedResponses.TryGetProperty("500", out _));
    }

    [Fact]
    public async Task BehaviorRestOpenApiDocumentedStatusesCanBeConfigured()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["ApiRoutes:ResultEnvelope:Enabled"] = "true";
        builder.Configuration["OpenApi:BehaviorRest:DocumentedStatusCodes:0"] = "200";
        builder.Configuration["OpenApi:BehaviorRest:DocumentedStatusCodes:1"] = "400";
        builder.Configuration["OpenApi:BehaviorRest:DocumentedStatusCodes:2"] = "500";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new EnvelopeResultModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var responses = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/tests/results/widgets/{widgetId}")
            .GetProperty("get")
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("500", out _));
        Assert.False(responses.TryGetProperty("201", out _));
        Assert.False(responses.TryGetProperty("202", out _));
        Assert.False(responses.TryGetProperty("204", out _));
        Assert.False(responses.TryGetProperty("401", out _));
        Assert.False(responses.TryGetProperty("403", out _));
        Assert.False(responses.TryGetProperty("404", out _));
        Assert.False(responses.TryGetProperty("409", out _));
        Assert.False(responses.TryGetProperty("429", out _));
    }

    [Fact]
    public async Task BehaviorRestOpenApiAdds429WhenAspNetCoreRateLimitingIsEnabled()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["Engine:Resilience:RateLimiting:Enabled"] = "true";
        builder.Configuration["Engine:Resilience:RateLimiting:Algorithm"] = "FixedWindow";
        builder.Configuration["Engine:Resilience:RateLimiting:PermitLimit"] = "10";
        builder.Configuration["Engine:Resilience:RateLimiting:QueueLimit"] = "0";
        builder.Configuration["Engine:Resilience:RateLimiting:WindowSeconds"] = "60";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new EnvelopeResultModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var responses = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/tests/results/widgets/{widgetId}")
            .GetProperty("get")
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty("429", out _));
    }

    [Fact]
    public async Task BehaviorRestOpenApiOmits429WhenBehaviorOverrideDisablesRateLimiting()
    {
        const string route = "/api/v1/tests/results/widgets/widget-1";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["Engine:Resilience:RateLimiting:Enabled"] = "true";
        builder.Configuration["Engine:Resilience:RateLimiting:Algorithm"] = "FixedWindow";
        builder.Configuration["Engine:Resilience:RateLimiting:PermitLimit"] = "1";
        builder.Configuration["Engine:Resilience:RateLimiting:QueueLimit"] = "0";
        builder.Configuration["Engine:Resilience:RateLimiting:WindowSeconds"] = "60";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:widget-free-pass:Behaviors:0"] = "tests.results.lookup";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:widget-free-pass:Transports:0"] = "rest-api";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:widget-free-pass:Enabled"] = "false";
        builder.Configuration["Engine:Resilience:BehaviorExecution:Overrides:widget-free-pass:Behaviors:0"] = "tests.results.lookup";
        builder.Configuration["Engine:Resilience:BehaviorExecution:Overrides:widget-free-pass:Transports:0"] = "rest-api";
        builder.Configuration["Engine:Resilience:BehaviorExecution:Overrides:widget-free-pass:RateLimiting:Enabled"] = "false";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new EnvelopeResultModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var firstResponse = await client.GetAsync(route);
        var secondResponse = await client.GetAsync(route);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var responses = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/tests/results/widgets/{widgetId}")
            .GetProperty("get")
            .GetProperty("responses");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.False(responses.TryGetProperty("429", out _));
    }

    [Fact]
    public async Task BehaviorRestTransportOverrideCanEnableLimiterWithoutDefaultPolicy()
    {
        const string route = "/api/v1/tests/results/widgets/widget-1";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:rest-only:Transports:0"] = "rest-api";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:rest-only:Algorithm"] = "FixedWindow";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:rest-only:PermitLimit"] = "1";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:rest-only:QueueLimit"] = "0";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:rest-only:WindowSeconds"] = "60";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new EnvelopeResultModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var firstResponse = await client.GetAsync(route);
        var secondResponse = await client.GetAsync(route);
        var policies = await client.GetFromJsonAsync<RateLimitingRuntimeDescriptor[]>("/engine/rate-limiting");
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var responses = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/tests/results/widgets/{widgetId}")
            .GetProperty("get")
            .GetProperty("responses");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
        Assert.True(responses.TryGetProperty("429", out _));
        Assert.NotNull(policies);
        var policy = Assert.Single(policies);
        Assert.Equal("cephalon-rate-limit-rest-only", policy.Id);
        Assert.Equal("rest-api", policy.Metadata["transportIds"]);
        Assert.Equal(1, policy.Effective.PermitLimit);
    }

    [Fact]
    public async Task BehaviorRestBehaviorTransportOverrideBeatsTransportOverride()
    {
        const string lookupRoute = "/api/v1/tests/results/widgets/widget-1";
        const string echoRoute = "/api/v1/tests/envelope/cart/cart-123/items";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:rest-relaxed:Transports:0"] = "rest-api";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:rest-relaxed:Algorithm"] = "FixedWindow";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:rest-relaxed:PermitLimit"] = "10";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:rest-relaxed:QueueLimit"] = "0";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:rest-relaxed:WindowSeconds"] = "60";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:widget-tight:Behaviors:0"] = "tests.results.lookup";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:widget-tight:Transports:0"] = "rest-api";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:widget-tight:Algorithm"] = "FixedWindow";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:widget-tight:PermitLimit"] = "1";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:widget-tight:QueueLimit"] = "0";
        builder.Configuration["Engine:Resilience:RateLimiting:Overrides:widget-tight:WindowSeconds"] = "60";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new EnvelopeResultModule());
            engine.AddModule(new EnvelopeEchoModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var firstLookupResponse = await client.GetAsync(lookupRoute);
        var secondLookupResponse = await client.GetAsync(lookupRoute);
        var firstEchoResponse = await client.PostAsJsonAsync(echoRoute, new { productName = "Keyboard", quantity = 1 });
        var secondEchoResponse = await client.PostAsJsonAsync(echoRoute, new { productName = "Mouse", quantity = 1 });
        var policies = await client.GetFromJsonAsync<RateLimitingRuntimeDescriptor[]>("/engine/rate-limiting");

        Assert.Equal(HttpStatusCode.OK, firstLookupResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondLookupResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, firstEchoResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondEchoResponse.StatusCode);
        Assert.NotNull(policies);
        Assert.Equal(2, policies.Length);
        Assert.Contains(policies, policy => policy.Id == "cephalon-rate-limit-rest-relaxed");
        Assert.Contains(policies, policy => policy.Id == "cephalon-rate-limit-widget-tight");
    }

    [Fact]
    public async Task PlainBehaviorResponsesCanBeWrappedInResultModelEnvelopes()
    {
        const string route = "/api/v1/tests/envelope/cart/cart-123/items";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["ApiRoutes:ResultEnvelope:Enabled"] = "true";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new EnvelopeEchoModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(route, new
        {
            productName = "Keyboard",
            quantity = 2
        });
        var payload = await response.Content.ReadFromJsonAsync<ResultModel<RestHelperEchoOutput>>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.Equal(200, payload.StatusCode);
        Assert.Equal("Ok", payload.Title);
        Assert.Equal("Successful", payload.Message);
        Assert.NotNull(payload.Data);
        Assert.Equal("cart-123", payload.Data!.CartId);
        Assert.Equal("Keyboard", payload.Data.ProductName);
        Assert.Equal(2, payload.Data.Quantity);
    }

    [Fact]
    public void RestBehaviorModuleBaseRejectsMappingBehaviorOwnedByAnotherModule()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["Engine:Transports:1"] = "BehaviorHttp";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new DocumentedRestHelperModule("Test Cart API", "Commands and queries exposed by the test cart REST surface."));
            engine.AddModule(new ConflictingRestHelperModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        var app = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => app.MapCephalon());

        Assert.Contains("owned by module", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tests.cart", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tests.conflict", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IResult ThrowUnhandledRestProblem()
    {
        throw new InvalidOperationException("The test REST endpoint failed.");
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

    private sealed record EnvelopeLookupInput(string WidgetId);

    private sealed record EnvelopeLookupOutput(string WidgetId, string Label);

    [AppBehavior("tests.results.lookup")]
    private sealed class EnvelopeLookupBehavior : IAppBehavior<EnvelopeLookupInput, Result<EnvelopeLookupOutput>>
    {
        public Task<Result<EnvelopeLookupOutput>> HandleAsync(
            EnvelopeLookupInput input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            if (string.Equals(input.WidgetId, "missing", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<Result<EnvelopeLookupOutput>>(Result.NotFound(
                    "tests.widgets.not_found",
                    $"Widget '{input.WidgetId}' was not found.",
                    new BehaviorFault
                    {
                        Code = "tests.widgets.not_found",
                        Message = $"Widget '{input.WidgetId}' was not found."
                    }));
            }

            if (string.Equals(input.WidgetId, "invalid", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<Result<EnvelopeLookupOutput>>(Result.Invalid(
                    "tests.widgets.invalid",
                    "Widget validation failed.",
                    new BehaviorFault
                    {
                        Code = "tests.widgets.invalid",
                        Message = "Widget validation failed.",
                        Severity = BehaviorFaultSeverity.Error,
                        InnerFaults =
                        [
                            new BehaviorFault
                            {
                                Code = "tests.widgets.widget_id.required",
                                Message = "Widget id is required.",
                                Severity = BehaviorFaultSeverity.Error
                            },
                            new BehaviorFault
                            {
                                Code = "tests.widgets.widget_id.length",
                                Message = "Widget id must be at least 3 characters long.",
                                Severity = BehaviorFaultSeverity.Error
                            }
                        ]
                    }));
            }

            return Task.FromResult(Result.Ok(
                new EnvelopeLookupOutput(input.WidgetId, "Cephalon Widget"),
                message: "Widget resolved."));
        }
    }

    private sealed class DocumentedRestHelperModule(string tagName, string tagDescription) : RestBehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.cart",
            displayName: "Test Cart",
            description: "Test module for behavior-aware REST endpoint OpenAPI coverage.",
            version: "2.4.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            var group = behaviors.Group("/tests/cart")
                .WithTagName(tagName)
                .WithTagDescription(tagDescription);
            group.MapPost<RestHelperEchoBehavior>(
                "/{cartId}/items",
                topology => topology
                    .AsDirect()
                    .ViaHttpJsonRpc());
        }
    }

    private sealed class EnvelopeResultModule : RestBehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.results",
            displayName: "Envelope Results",
            description: "Test module for behavior result envelopes.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            var group = behaviors.Group("/tests/results/widgets");
            group.MapGet<EnvelopeLookupBehavior>("/{widgetId}");
        }
    }

    private sealed class EnvelopeEchoModule : RestBehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.envelope",
            displayName: "Envelope Echo",
            description: "Test module for wrapping plain REST behavior outputs.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            var group = behaviors.Group("/tests/envelope/cart");
            group.MapPost<RestHelperEchoBehavior>("/{cartId}/items");
        }
    }

    private sealed class ConflictingRestHelperModule : RestBehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.conflict",
            displayName: "Conflict",
            description: "Conflicting test module.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
        }

        protected override void MapAdditionalEndpoints(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapBehaviorRestGroup(this, "/tests/conflict");
            group.MapBehaviorPost<RestHelperEchoBehavior>("/echo");
        }
    }
}
