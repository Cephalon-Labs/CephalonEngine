using System.Net.Http.Json;
using Cephalon.Abstractions.AppModel;
using Cephalon.Sample.Microservice;
using Cephalon.Sample.MicroserviceSuite.CatalogService;
using Cephalon.Sample.MicroserviceSuite.OrdersService;
using Cephalon.Sample.MicroserviceSuite.Foundation.Contracts;
using Cephalon.Sample.MicroserviceSuite.Foundation.Conventions;
using Cephalon.Sample.ModularMonolith;
using Cephalon.Sample.ModularVerticalSlice;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

public sealed class SampleSuiteHostingTests
{
    [Fact]
    public async Task ModularMonolithSampleBootsAndExposesCatalogOverview()
    {
        await using var app = ModularMonolithSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var profile = await client.GetFromJsonAsync<AppProfile>("/engine/app-model");
        var overview = await client.GetStringAsync("/api/catalog/overview");

        Assert.NotNull(profile);
        Assert.Equal("modular-monolith", profile.BlueprintId);
        Assert.Contains("ModularMonolith", overview, StringComparison.Ordinal);
        Assert.Contains("starter-kit", overview, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ModularVerticalSliceSampleBootsAndExposesCheckoutPreview()
    {
        await using var app = ModularVerticalSliceSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var profile = await client.GetFromJsonAsync<AppProfile>("/engine/app-model");
        var preview = await client.GetStringAsync("/api/orders/checkout/preview/vip-42");

        Assert.NotNull(profile);
        Assert.Equal("modular-vertical-slice", profile.BlueprintId);
        Assert.Contains("ModularVerticalSlice", preview, StringComparison.Ordinal);
        Assert.Contains("vip-fast-lane", preview, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MicroserviceSampleBootsAndExposesWelcomeContract()
    {
        await using var app = MicroserviceSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await app.StartAsync();
        var client = app.GetTestClient();

        var profile = await client.GetFromJsonAsync<AppProfile>("/engine/app-model");
        var welcome = await client.GetStringAsync("/api/customers/welcome/Ada?tenant=enterprise");

        Assert.NotNull(profile);
        Assert.Equal("microservice", profile.BlueprintId);
        Assert.Contains("enterprise-boundary", welcome, StringComparison.Ordinal);
        Assert.Contains("Cephalon microservice sample", welcome, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MicroserviceSuiteSamplesBootAndExposeSharedFoundationConventions()
    {
        await using var catalogApp = CatalogServiceSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());
        await using var ordersApp = OrdersServiceSampleApp.Build(
            configureBuilder: builder => builder.WebHost.UseTestServer());

        await catalogApp.StartAsync();
        await ordersApp.StartAsync();

        var catalogClient = catalogApp.GetTestClient();
        var ordersClient = ordersApp.GetTestClient();

        var catalogProfile = await catalogClient.GetFromJsonAsync<AppProfile>("/engine/app-model");
        var ordersProfile = await ordersClient.GetFromJsonAsync<AppProfile>("/engine/app-model");
        var catalogSummary = await catalogClient.GetFromJsonAsync<SuiteServiceSummaryContract>("/api/catalog/overview");
        var ordersSummary = await ordersClient.GetFromJsonAsync<SuiteServiceSummaryContract>("/api/orders/coordination/PO-42?fulfillmentRegion=apac");

        Assert.NotNull(catalogProfile);
        Assert.NotNull(ordersProfile);
        Assert.NotNull(catalogSummary);
        Assert.NotNull(ordersSummary);
        Assert.Equal("microservice", catalogProfile.BlueprintId);
        Assert.Equal("microservice", ordersProfile.BlueprintId);
        Assert.Equal(CommerceSuiteConventions.SuiteName, catalogSummary.Suite);
        Assert.Equal(CommerceSuiteConventions.SuiteName, ordersSummary.Suite);
        Assert.Equal(CommerceSuiteConventions.ConventionProfile, catalogSummary.ConventionProfile);
        Assert.Equal(CommerceSuiteConventions.ConventionProfile, ordersSummary.ConventionProfile);
        Assert.Equal(CommerceSuiteConventions.SharedFoundationProject, catalogSummary.SharedFoundation);
        Assert.Equal(CommerceSuiteConventions.SharedFoundationProject, ordersSummary.SharedFoundation);
        Assert.Contains(CommerceSuiteConventions.OrdersService, catalogSummary.PartnerServices);
        Assert.Contains(CommerceSuiteConventions.CatalogService, ordersSummary.PartnerServices);
        Assert.Contains("shared", catalogSummary.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PO-42", ordersSummary.Message, StringComparison.Ordinal);
        Assert.Contains("apac", ordersSummary.Message, StringComparison.OrdinalIgnoreCase);
    }
}
