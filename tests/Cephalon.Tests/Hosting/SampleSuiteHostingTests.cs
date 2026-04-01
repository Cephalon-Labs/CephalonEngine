using System.Net.Http.Json;
using Cephalon.Abstractions.AppModel;
using Cephalon.Sample.Microservice;
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
}
