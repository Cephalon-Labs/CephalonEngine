using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Cephalon.Tests.Hosting;

public sealed class MultiTenancyGovernanceAspNetCoreHostingTests
{
    [Fact]
    public async Task MapCephalonTenantDomainOwnershipHttpProofsServesPublishedProofsByHostAndPath()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance();
        });
        builder.AddCephalonMultiTenancyGovernanceAspNetCore();

        await using var app = builder.Build();
        app.MapCephalonTenantDomainOwnershipHttpProofs();

        var issuer = app.Services.GetRequiredService<ITenantDomainOwnershipProofChallengeIssuer>();
        var publisher = app.Services.GetRequiredService<ITenantDomainOwnershipHttpProofPublisher>();
        var challenge = await issuer.IssueAsync(new TenantDomainOwnershipProofChallengeRequest(
            tenantId: "tenant-001",
            domainName: "Proof.Example.",
            verificationMethod: TenantDomainVerificationMethods.HttpFile,
            challengeValue: "published-http-proof",
            atUtc: new DateTimeOffset(2026, 04, 29, 9, 0, 0, TimeSpan.Zero)));
        var publication = await publisher.PublishAsync(new TenantDomainOwnershipHttpProofPublicationRequest(
            tenantId: "tenant-001",
            domainName: "proof.example",
            atUtc: new DateTimeOffset(2026, 04, 29, 9, 5, 0, TimeSpan.Zero)));

        await app.StartAsync();
        var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, publication.HttpFilePath!);
        request.Headers.Host = "proof.example";
        using var missingHostRequest = new HttpRequestMessage(HttpMethod.Get, publication.HttpFilePath!);
        missingHostRequest.Headers.Host = "other.example";

        var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();
        var missingHostResponse = await client.SendAsync(missingHostRequest);

        Assert.True(challenge.Issued);
        Assert.True(publication.Published);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("published-http-proof", payload);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(HttpStatusCode.NotFound, missingHostResponse.StatusCode);
    }
}
