using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cephalon.Abstractions.AppModel;
using Cephalon.Sample.Showcase;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

/// <summary>
/// Integration tests verifying that the showcase sample boots correctly and exposes
/// all five behavior patterns, domain modules, and cross-cutting capabilities.
/// </summary>
public sealed class ShowcaseSampleHostingTests
{
    [Fact]
    public async Task ShowcaseSampleBootsAndExposesRootSummary()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Showcase", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Modular", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShowcaseSampleExposesAppProfileWithAllCapabilities()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var profile = await client.GetFromJsonAsync<AppProfile>("/engine/app-model");

        Assert.NotNull(profile);
        Assert.Equal("modular-monolith", profile.BlueprintId);
        Assert.Equal("Sfid", profile.Data.IdGenerator);
        Assert.True(profile.Audit.Enabled);
        Assert.True(profile.Identity.Enabled);
        Assert.True(profile.Tenancy.Enabled);
    }

    [Fact]
    public async Task ShowcaseSampleExposesEngineManifest()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/engine/manifest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("showcase.catalog", body, StringComparison.Ordinal);
        Assert.Contains("showcase.cart", body, StringComparison.Ordinal);
        Assert.Contains("showcase.orders", body, StringComparison.Ordinal);
        Assert.Contains("showcase.inventory", body, StringComparison.Ordinal);
        Assert.Contains("showcase.shipping", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleExposesCapabilities()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/engine/capabilities");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        // Catalog capabilities
        Assert.Contains("showcase.catalog.read", body, StringComparison.Ordinal);
        Assert.Contains("showcase.catalog.write", body, StringComparison.Ordinal);

        // Cart capabilities
        Assert.Contains("showcase.cart.cqrs", body, StringComparison.Ordinal);
        Assert.Contains("showcase.cart.event-sourcing", body, StringComparison.Ordinal);

        // Orders capabilities
        Assert.Contains("showcase.orders.event-driven", body, StringComparison.Ordinal);

        // Inventory capabilities
        Assert.Contains("showcase.inventory.saga", body, StringComparison.Ordinal);

        // Shipping capabilities
        Assert.Contains("showcase.shipping.process-manager", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleExposesCatalogProductsViaRestEndpoint()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/showcase/catalog/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        // Verify seeded products are present
        Assert.Contains("ProBook Laptop", body, StringComparison.Ordinal);
        Assert.Contains("ErgoGrip Wireless Mouse", body, StringComparison.Ordinal);
        Assert.Contains("AdjustaPro Standing Desk", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleExposesCatalogProductByIdViaRestEndpoint()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/showcase/catalog/products/prod-001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("prod-001", body, StringComparison.Ordinal);
        Assert.Contains("LAPTOP-PRO-15", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleReturnsNotFoundForMissingProduct()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/showcase/catalog/products/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShowcaseSampleExposesHealthEndpoints()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var healthResponse = await client.GetAsync("/health");
        var liveResponse = await client.GetAsync("/health/live");
        var readyResponse = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
    }

    [Fact]
    public async Task ShowcaseSampleExposesModulesEndpoint()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/engine/modules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        // All five domain modules should be registered
        Assert.Contains("showcase.catalog", body, StringComparison.Ordinal);
        Assert.Contains("showcase.cart", body, StringComparison.Ordinal);
        Assert.Contains("showcase.orders", body, StringComparison.Ordinal);
        Assert.Contains("showcase.inventory", body, StringComparison.Ordinal);
        Assert.Contains("showcase.shipping", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleExposesMultipleTenants()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var profile = await client.GetFromJsonAsync<AppProfile>("/engine/app-model");

        Assert.NotNull(profile);
        Assert.True(profile.Tenancy.Enabled);
    }

    [Fact]
    public async Task ShowcaseSampleExposesAuthorizationPolicies()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/engine/authorization-policies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("showcase.viewer", body, StringComparison.Ordinal);
        Assert.Contains("showcase.customer", body, StringComparison.Ordinal);
        Assert.Contains("showcase.admin", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleSupportsCreateProductViaPost()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var payload = new
        {
            sku = "TEST-CREATE-001",
            name = "Test Product",
            description = "Created via integration test",
            category = "Testing",
            priceInCents = 1999,
            currency = "USD",
            tags = new[] { "test" }
        };
        var content = JsonContent.Create(payload);
        var response = await client.PostAsync("/api/showcase/catalog/products", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("TEST-CREATE-001", body, StringComparison.Ordinal);
        Assert.Contains("Test Product", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleSupportsUpdateProductViaPut()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var updatePayload = new
        {
            productId = "prod-001",
            name = "ProBook Laptop 15\" Updated",
            priceInCents = 139999
        };
        var content = JsonContent.Create(updatePayload);
        var response = await client.PutAsync("/api/showcase/catalog/products/prod-001", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Updated", body, StringComparison.Ordinal);
        Assert.Contains("139999", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleHandlesConcurrentReadsWithoutErrors()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        // Fire 20 concurrent read requests
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => client.GetAsync("/api/showcase/catalog/products"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }

    [Fact]
    public async Task ShowcaseSampleHandlesConcurrentWritesWithoutDataLoss()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        // Count initial products
        var initialResponse = await client.GetAsync("/api/showcase/catalog/products");
        var initialBody = await initialResponse.Content.ReadAsStringAsync();
        var initialCount = JsonSerializer.Deserialize<JsonElement>(initialBody).GetArrayLength();

        // Fire 10 concurrent writes
        var writeTasks = Enumerable.Range(0, 10)
            .Select(i =>
            {
                var payload = new
                {
                    sku = $"CONC-{i:D3}",
                    name = $"Concurrent Product {i}",
                    category = "ConcurrencyTest",
                    priceInCents = (i + 1) * 100,
                    currency = "USD"
                };
                return client.PostAsync("/api/showcase/catalog/products", JsonContent.Create(payload));
            })
            .ToArray();

        var writeResponses = await Task.WhenAll(writeTasks);
        Assert.All(writeResponses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));

        // Verify all 10 were persisted
        var finalResponse = await client.GetAsync("/api/showcase/catalog/products");
        var finalBody = await finalResponse.Content.ReadAsStringAsync();
        var finalCount = JsonSerializer.Deserialize<JsonElement>(finalBody).GetArrayLength();

        Assert.Equal(initialCount + 10, finalCount);
    }

    [Fact]
    public async Task ShowcaseSampleReturnsNotFoundForUpdateOfMissingProduct()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var payload = new { productId = "nonexistent", name = "Ghost" };
        var response = await client.PutAsync(
            "/api/showcase/catalog/products/nonexistent",
            JsonContent.Create(payload));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    //  Orders domain tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ShowcaseSampleExposesOrdersListEndpoint()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/showcase/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var orders = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(JsonValueKind.Array, orders.ValueKind);
    }

    [Fact]
    public async Task ShowcaseSamplePlacesAndRetrievesOrder()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        // Place an order
        var placePayload = new
        {
            customerId = "cust-test-001",
            shippingAddress = "123 Test Street",
            items = new[]
            {
                new { productId = "prod-001", productName = "ProBook Laptop 15\"", quantity = 1, unitPriceInCents = 149999L }
            }
        };
        var placeResponse = await client.PostAsync("/api/showcase/orders", JsonContent.Create(placePayload));

        Assert.Equal(HttpStatusCode.Created, placeResponse.StatusCode);
        var placeBody = await placeResponse.Content.ReadAsStringAsync();
        var placed = JsonSerializer.Deserialize<JsonElement>(placeBody);
        var orderId = placed.GetProperty("orderId").GetString();
        Assert.NotNull(orderId);
        Assert.Equal("Pending", placed.GetProperty("status").GetString());

        // Retrieve the order
        var getResponse = await client.GetAsync($"/api/showcase/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getBody = await getResponse.Content.ReadAsStringAsync();
        Assert.Contains(orderId, getBody, StringComparison.Ordinal);
        Assert.Contains("cust-test-001", getBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleCancelsOrder()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        // Place an order first
        var placePayload = new
        {
            customerId = "cust-cancel-001",
            shippingAddress = "456 Cancel Lane",
            items = new[]
            {
                new { productId = "prod-002", productName = "ErgoGrip Wireless Mouse", quantity = 2, unitPriceInCents = 4999L }
            }
        };
        var placeResponse = await client.PostAsync("/api/showcase/orders", JsonContent.Create(placePayload));
        var placed = JsonSerializer.Deserialize<JsonElement>(await placeResponse.Content.ReadAsStringAsync());
        var orderId = placed.GetProperty("orderId").GetString()!;

        // Cancel the order
        var cancelPayload = new { orderId, reason = "Changed my mind" };
        var cancelResponse = await client.PutAsync(
            $"/api/showcase/orders/{orderId}/cancel",
            JsonContent.Create(cancelPayload));

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelBody = await cancelResponse.Content.ReadAsStringAsync();
        Assert.Contains("Cancelled", cancelBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleReturnsNotFoundForMissingOrder()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/showcase/orders/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    //  Inventory domain tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ShowcaseSampleExposesInventoryListEndpoint()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/showcase/inventory");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("prod-001", body, StringComparison.Ordinal);
        Assert.Contains("WH-01", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleExposesInventoryByProductId()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/showcase/inventory/prod-005");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("prod-005", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleReservesStock()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var payload = new
        {
            orderId = "ord-reserve-001",
            items = new[]
            {
                new { productId = "prod-001", quantity = 2 },
                new { productId = "prod-002", quantity = 5 }
            }
        };
        var response = await client.PostAsync("/api/showcase/inventory/reserve", JsonContent.Create(payload));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.True(result.GetProperty("allReserved").GetBoolean());
    }

    [Fact]
    public async Task ShowcaseSampleReturnsNotFoundForMissingInventoryItem()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/showcase/inventory/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    //  Shipping domain tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ShowcaseSampleExposesShipmentsListEndpoint()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/showcase/shipping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var shipments = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(JsonValueKind.Array, shipments.ValueKind);
    }

    [Fact]
    public async Task ShowcaseSampleInitiatesAndTracksShipment()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        // Initiate a shipment
        var initiatePayload = new
        {
            orderId = "ord-ship-001",
            destinationAddress = "789 Delivery Ave",
            items = new[]
            {
                new { productId = "prod-001", productName = "ProBook Laptop 15\"", quantity = 1 }
            }
        };
        var initiateResponse = await client.PostAsync("/api/showcase/shipping", JsonContent.Create(initiatePayload));

        Assert.Equal(HttpStatusCode.Created, initiateResponse.StatusCode);
        var initiateBody = await initiateResponse.Content.ReadAsStringAsync();
        var initiated = JsonSerializer.Deserialize<JsonElement>(initiateBody);
        var shipmentId = initiated.GetProperty("shipmentId").GetString();
        Assert.NotNull(shipmentId);
        Assert.Equal("LabelCreated", initiated.GetProperty("status").GetString());

        // Track the shipment
        var trackResponse = await client.GetAsync($"/api/showcase/shipping/{shipmentId}");
        Assert.Equal(HttpStatusCode.OK, trackResponse.StatusCode);
        var trackBody = await trackResponse.Content.ReadAsStringAsync();
        Assert.Contains(shipmentId, trackBody, StringComparison.Ordinal);
        Assert.Contains("Showcase Express", trackBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleConfirmsDelivery()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        // Initiate
        var initiatePayload = new
        {
            orderId = "ord-deliver-001",
            destinationAddress = "321 Complete St",
            items = new[] { new { productId = "prod-003", productName = "AdjustaPro Standing Desk", quantity = 1 } }
        };
        var initiateResponse = await client.PostAsync("/api/showcase/shipping", JsonContent.Create(initiatePayload));
        var initiated = JsonSerializer.Deserialize<JsonElement>(await initiateResponse.Content.ReadAsStringAsync());
        var shipmentId = initiated.GetProperty("shipmentId").GetString()!;

        // Confirm delivery
        var deliverPayload = new { shipmentId, recipientName = "Test User" };
        var deliverResponse = await client.PutAsync(
            $"/api/showcase/shipping/{shipmentId}/deliver",
            JsonContent.Create(deliverPayload));

        Assert.Equal(HttpStatusCode.OK, deliverResponse.StatusCode);
        var deliverBody = await deliverResponse.Content.ReadAsStringAsync();
        Assert.Contains("Delivered", deliverBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleReturnsNotFoundForMissingShipment()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/showcase/shipping/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    //  Cart domain tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ShowcaseSampleAddsItemToCartAndRetrievesCart()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        // Add item to cart
        var addPayload = new
        {
            cartId = "cart-test-001",
            customerId = "cust-cart-001",
            productId = "prod-001",
            productName = "ProBook Laptop 15\"",
            quantity = 1,
            priceInCents = 149999L
        };
        var addResponse = await client.PostAsync("/api/showcase/cart/cart-test-001/items", JsonContent.Create(addPayload));

        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var addBody = await addResponse.Content.ReadAsStringAsync();
        var added = JsonSerializer.Deserialize<JsonElement>(addBody);
        Assert.Equal(1, added.GetProperty("itemCount").GetInt32());
        Assert.Equal(149999, added.GetProperty("totalInCents").GetInt64());

        // Retrieve cart
        var getResponse = await client.GetAsync("/api/showcase/cart/cart-test-001");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getBody = await getResponse.Content.ReadAsStringAsync();
        Assert.Contains("cart-test-001", getBody, StringComparison.Ordinal);
        Assert.Contains("ProBook Laptop", getBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowcaseSampleRemovesItemFromCart()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        // Add two items
        var item1 = new { cartId = "cart-remove-001", customerId = "cust-rm", productId = "prod-001",
            productName = "Laptop", quantity = 1, priceInCents = 149999L };
        await client.PostAsync("/api/showcase/cart/cart-remove-001/items", JsonContent.Create(item1));

        var item2 = new { cartId = "cart-remove-001", customerId = "cust-rm", productId = "prod-002",
            productName = "Mouse", quantity = 1, priceInCents = 4999L };
        await client.PostAsync("/api/showcase/cart/cart-remove-001/items", JsonContent.Create(item2));

        // Remove one
        var removeResponse = await client.DeleteAsync("/api/showcase/cart/cart-remove-001/items/prod-001");

        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        var body = await removeResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(1, result.GetProperty("itemCount").GetInt32());
    }

    [Fact]
    public async Task ShowcaseSampleChecksOutCart()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        // Add item
        var addPayload = new { cartId = "cart-checkout-001", customerId = "cust-co",
            productId = "prod-006", productName = "TypeMaster Mechanical Keyboard",
            quantity = 1, priceInCents = 12999L };
        await client.PostAsync("/api/showcase/cart/cart-checkout-001/items", JsonContent.Create(addPayload));

        // Checkout
        var checkoutPayload = new { cartId = "cart-checkout-001", shippingAddress = "123 Checkout Blvd" };
        var checkoutResponse = await client.PostAsync(
            "/api/showcase/cart/cart-checkout-001/checkout",
            JsonContent.Create(checkoutPayload));

        Assert.Equal(HttpStatusCode.OK, checkoutResponse.StatusCode);
        var body = await checkoutResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.StartsWith("ord-", result.GetProperty("orderId").GetString()!);
        Assert.Equal(12999, result.GetProperty("totalInCents").GetInt64());
    }

    [Fact]
    public async Task ShowcaseSampleReturnsNotFoundForMissingCart()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/showcase/cart/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    //  End-to-end flow test
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ShowcaseSampleSupportsEndToEndOrderFlow()
    {
        await using var app = ShowcaseSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        // 1. Add items to cart
        var addItem1 = new { cartId = "cart-e2e-001", customerId = "cust-e2e",
            productId = "prod-001", productName = "ProBook Laptop 15\"",
            quantity = 1, priceInCents = 149999L };
        await client.PostAsync("/api/showcase/cart/cart-e2e-001/items", JsonContent.Create(addItem1));

        var addItem2 = new { cartId = "cart-e2e-001", customerId = "cust-e2e",
            productId = "prod-006", productName = "TypeMaster Mechanical Keyboard",
            quantity = 2, priceInCents = 12999L };
        await client.PostAsync("/api/showcase/cart/cart-e2e-001/items", JsonContent.Create(addItem2));

        // 2. Checkout cart
        var checkoutPayload = new { cartId = "cart-e2e-001", shippingAddress = "1 E2E Lane" };
        var checkoutResponse = await client.PostAsync(
            "/api/showcase/cart/cart-e2e-001/checkout",
            JsonContent.Create(checkoutPayload));
        Assert.Equal(HttpStatusCode.OK, checkoutResponse.StatusCode);

        // 3. Place order
        var orderPayload = new
        {
            customerId = "cust-e2e",
            shippingAddress = "1 E2E Lane",
            items = new[]
            {
                new { productId = "prod-001", productName = "ProBook Laptop 15\"", quantity = 1, unitPriceInCents = 149999L },
                new { productId = "prod-006", productName = "TypeMaster Mechanical Keyboard", quantity = 2, unitPriceInCents = 12999L }
            }
        };
        var orderResponse = await client.PostAsync("/api/showcase/orders", JsonContent.Create(orderPayload));
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var orderResult = JsonSerializer.Deserialize<JsonElement>(await orderResponse.Content.ReadAsStringAsync());
        var orderId = orderResult.GetProperty("orderId").GetString()!;

        // 4. Reserve inventory
        var reservePayload = new
        {
            orderId,
            items = new[]
            {
                new { productId = "prod-001", quantity = 1 },
                new { productId = "prod-006", quantity = 2 }
            }
        };
        var reserveResponse = await client.PostAsync("/api/showcase/inventory/reserve", JsonContent.Create(reservePayload));
        Assert.Equal(HttpStatusCode.OK, reserveResponse.StatusCode);
        var reserved = JsonSerializer.Deserialize<JsonElement>(await reserveResponse.Content.ReadAsStringAsync());
        Assert.True(reserved.GetProperty("allReserved").GetBoolean());

        // 5. Initiate shipping
        var shipPayload = new
        {
            orderId,
            destinationAddress = "1 E2E Lane",
            items = new[]
            {
                new { productId = "prod-001", productName = "ProBook Laptop 15\"", quantity = 1 },
                new { productId = "prod-006", productName = "TypeMaster Mechanical Keyboard", quantity = 2 }
            }
        };
        var shipResponse = await client.PostAsync("/api/showcase/shipping", JsonContent.Create(shipPayload));
        Assert.Equal(HttpStatusCode.Created, shipResponse.StatusCode);
        var shipped = JsonSerializer.Deserialize<JsonElement>(await shipResponse.Content.ReadAsStringAsync());
        var shipmentId = shipped.GetProperty("shipmentId").GetString()!;

        // 6. Confirm delivery
        var deliverPayload = new { shipmentId, recipientName = "E2E Customer" };
        var deliverResponse = await client.PutAsync(
            $"/api/showcase/shipping/{shipmentId}/deliver",
            JsonContent.Create(deliverPayload));
        Assert.Equal(HttpStatusCode.OK, deliverResponse.StatusCode);
        var delivered = JsonSerializer.Deserialize<JsonElement>(await deliverResponse.Content.ReadAsStringAsync());
        Assert.Equal("Delivered", delivered.GetProperty("status").GetString());

        // 7. Verify order status reflects delivery
        var orderStatus = await client.GetAsync($"/api/showcase/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, orderStatus.StatusCode);
        var orderBody = await orderStatus.Content.ReadAsStringAsync();
        Assert.Contains("Delivered", orderBody, StringComparison.Ordinal);
    }
}
