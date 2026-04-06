using System.Net.Http.Json;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Manifest;
using Cephalon.ReferenceModule.Operations.Application;
using Cephalon.ReferenceModule.Operations.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class ReferenceModuleHostingTests
{
    [Fact]
    public async Task ReferenceModulePackageCanBeDiscoveredAndExposeLocalizedRestStatus()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Discovery:Assemblies:0"] = "Cephalon.ReferenceModule.Operations";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon();

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var manifest = await client.GetFromJsonAsync<RuntimeManifest>("/engine");
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var status = await client.GetFromJsonAsync<OperationsStatusEnvelope>("/api/operations/status?culture=th");

        Assert.NotNull(manifest);
        Assert.Contains(manifest.Modules, module => module.Id == "operations");

        Assert.NotNull(capabilities);
        Assert.Contains(capabilities, capability =>
            capability.Key == "operations.status" &&
            capability.SourceModuleId == "operations");
        Assert.Contains(capabilities, capability =>
            capability.Key == "operations.localization" &&
            capability.SourceModuleId == "operations");

        Assert.NotNull(status);
        Assert.Equal(1, status.InitializeCount);
        Assert.Equal(1, status.StartCount);
        Assert.Equal(0, status.StopCount);
        Assert.Equal("Started", status.CurrentPhase);
        Assert.Equal("th", status.Culture);
        Assert.Equal("โมดูล Operations พร้อมทำงาน", status.Message);

        await app.StopAsync();

        var service = app.Services.GetRequiredService<OperationsStatusService>();
        var stopped = service.CreateEnvelope("en", "ignored");

        Assert.Equal(1, stopped.StopCount);
        Assert.Equal("Stopped", stopped.CurrentPhase);
    }
}
