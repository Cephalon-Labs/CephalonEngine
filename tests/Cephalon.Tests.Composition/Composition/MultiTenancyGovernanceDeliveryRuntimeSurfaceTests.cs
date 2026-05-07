using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.HttpDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.MailgunDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Hosting;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.SendGridDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.SmtpDelivery.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class MultiTenancyGovernanceDeliveryRuntimeSurfaceTests
{
    [Fact]
    public void InvitationDeliveryProviderPacksProjectSanitizedRuntimeSurfaces()
    {
        var services = new ServiceCollection();
        services.AddCephalonHttpInvitationDelivery(options =>
        {
            options.Endpoint = "https://delivery.example.test/hooks/invitations?token=http-query-secret";
            options.SigningSecret = "http-signing-secret";
            options.SigningKeyId = "http-key-1";
            options.Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Delivery-Secret"] = "http-header-secret"
            };
            options.ExpectedStatusCodes = [202];
            options.SupportedChannels = ["email", "webhook"];
        });
        services.AddCephalonSmtpInvitationDelivery(options =>
        {
            options.Host = "smtp.example.test";
            options.FromAddress = "invites@example.test";
            options.UserName = "smtp-user";
            options.Password = "smtp-password-secret";
            options.Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Smtp-Secret"] = "smtp-header-secret"
            };
        });
        services.AddCephalonSendGridInvitationDelivery(options =>
        {
            options.ApiKey = "sendgrid-api-key-secret";
            options.FromEmail = "invites@example.test";
            options.CustomArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tenant-code"] = "sendgrid-custom-arg-secret"
            };
            options.Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-SendGrid-Secret"] = "sendgrid-header-secret"
            };
        });
        services.AddCephalonMailgunInvitationDelivery(options =>
        {
            options.DomainName = "mg.example.test";
            options.ApiKey = "mailgun-api-key-secret";
            options.FromEmail = "invites@example.test";
            options.Variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tenant-code"] = "mailgun-variable-secret"
            };
            options.Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Mailgun-Secret"] = "mailgun-header-secret"
            };
        });
        services.AddCephalonAmazonSesInvitationDelivery(options =>
        {
            options.RegionSystemName = "us-east-1";
            options.FromEmail = "invites@example.test";
            options.Tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tenant-code"] = "amazon-ses-tag-secret"
            };
        });
        services.AddCephalonMicrosoftGraphInvitationDelivery(options =>
        {
            options.SenderUserId = "invites@example.test";
            options.AccessToken = "microsoft-graph-token-secret";
            options.Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["x-graph-secret"] = "microsoft-graph-header-secret"
            };
        });
        services.AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(options =>
        {
            options.TenantId = "azure-tenant-secret";
            options.ManagedIdentityClientId = "azure-client-secret";
            options.AuthorityHost = "AzurePublicCloud";
            options.Scopes = ["https://graph.microsoft.com/.default"];
        });
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
        });

        using var provider = services.BuildServiceProvider();
        var runtimeCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();
        var surfaces = runtimeCatalog.GetByTechnology("multi-tenancy");

        var http = AssertSurface(surfaces, "tenant-invitation-delivery-http");
        Assert.Equal("provider-managed", http.Metadata["ownership"]);
        Assert.Equal("delivery.example.test", http.Metadata["endpointHost"]);
        Assert.Equal("true", http.Metadata["endpointQueryConfigured"]);
        Assert.Equal("true", http.Metadata["signedWebhookEnabled"]);
        Assert.Equal("202", http.Metadata["expectedStatusCodes"]);

        var smtp = AssertSurface(surfaces, "tenant-invitation-delivery-smtp");
        Assert.Equal("smtp.example.test", smtp.Metadata["host"]);
        Assert.Equal("true", smtp.Metadata["passwordConfigured"]);
        Assert.Equal("example.test", smtp.Metadata["fromAddressDomain"]);

        var sendGrid = AssertSurface(surfaces, "tenant-invitation-delivery-sendgrid");
        Assert.Equal("api.sendgrid.com", sendGrid.Metadata["baseUrlHost"]);
        Assert.Equal("true", sendGrid.Metadata["apiKeyConfigured"]);
        Assert.Equal("tenant-code", sendGrid.Metadata["customArgKeys"]);

        var mailgun = AssertSurface(surfaces, "tenant-invitation-delivery-mailgun");
        Assert.Equal("api.mailgun.net", mailgun.Metadata["baseUrlHost"]);
        Assert.Equal("mg.example.test", mailgun.Metadata["domainName"]);
        Assert.Equal("true", mailgun.Metadata["apiKeyConfigured"]);

        var amazonSes = AssertSurface(surfaces, "tenant-invitation-delivery-amazon-ses");
        Assert.Equal("us-east-1", amazonSes.Metadata["regionSystemName"]);
        Assert.Equal("tenant-code", amazonSes.Metadata["tagKeys"]);

        var graph = AssertSurface(surfaces, "tenant-invitation-delivery-microsoft-graph");
        Assert.Equal("graph.microsoft.com", graph.Metadata["baseUrlHost"]);
        Assert.Equal("users", graph.Metadata["sendMailPathKind"]);
        Assert.Equal("true", graph.Metadata["staticAccessTokenConfigured"]);

        var azureIdentity = AssertSurface(surfaces, "tenant-invitation-delivery-microsoft-graph-azure-identity");
        Assert.Equal("provider-managed", azureIdentity.Metadata["tokenProviderOwnership"]);
        Assert.Equal("true", azureIdentity.Metadata["tenantIdConfigured"]);
        Assert.Equal("true", azureIdentity.Metadata["managedIdentityClientIdConfigured"]);
        Assert.Equal("https://graph.microsoft.com/.default", azureIdentity.Metadata["scopes"]);

        var projectedValues = surfaces
            .Where(surface => surface.SurfaceId.StartsWith("tenant-invitation-delivery-", StringComparison.OrdinalIgnoreCase))
            .SelectMany(static surface => surface.Entries)
            .SelectMany(static entry => entry.Metadata.Values)
            .ToArray();

        foreach (var secret in new[]
        {
            "http-query-secret",
            "http-signing-secret",
            "http-header-secret",
            "smtp-password-secret",
            "smtp-header-secret",
            "sendgrid-api-key-secret",
            "sendgrid-custom-arg-secret",
            "sendgrid-header-secret",
            "mailgun-api-key-secret",
            "mailgun-variable-secret",
            "mailgun-header-secret",
            "amazon-ses-tag-secret",
            "microsoft-graph-token-secret",
            "microsoft-graph-header-secret",
            "azure-tenant-secret",
            "azure-client-secret"
        })
        {
            Assert.DoesNotContain(projectedValues, value => value.Contains(secret, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static TechnologyRuntimeEntry AssertSurface(
        IReadOnlyList<TechnologyRuntimeSurface> surfaces,
        string surfaceId)
    {
        var surface = Assert.Single(
            surfaces,
            candidate => string.Equals(candidate.SurfaceId, surfaceId, StringComparison.OrdinalIgnoreCase));

        return Assert.Single(surface.Entries);
    }
}
