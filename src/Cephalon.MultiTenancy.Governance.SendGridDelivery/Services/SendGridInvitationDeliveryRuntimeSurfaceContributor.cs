using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.SendGridDelivery.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.Services;

internal sealed class SendGridInvitationDeliveryRuntimeSurfaceContributor(SendGridInvitationDeliveryOptions options) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var baseUrl = options.TryGetBaseUrl();
        var mailSendEndpoint = options.GetMailSendEndpoint();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "provider-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance.SendGridDelivery",
            ["provider"] = "sendgrid",
            ["transport"] = "http-rest",
            ["runtimeState"] = baseUrl is null ? "invalid" : "configured",
            ["senderId"] = Normalize(options.SenderId, "sendgrid-email"),
            ["senderOwnership"] = "provider-managed",
            ["supportedChannels"] = Join(options.SupportedChannels, "all"),
            ["baseUrlHost"] = baseUrl?.Host ?? "invalid",
            ["mailSendPath"] = mailSendEndpoint.AbsolutePath,
            ["apiKeyConfigured"] = (!string.IsNullOrWhiteSpace(options.ApiKey)).ToString().ToLowerInvariant(),
            ["fromEmailDomain"] = GetEmailDomain(options.FromEmail),
            ["fromNameConfigured"] = (!string.IsNullOrWhiteSpace(options.FromName)).ToString().ToLowerInvariant(),
            ["recipientEmailMetadataKey"] = Normalize(options.RecipientEmailMetadataKey, "email"),
            ["subjectTemplateConfigured"] = (!string.IsNullOrWhiteSpace(options.SubjectTemplate)).ToString().ToLowerInvariant(),
            ["textBodyTemplateConfigured"] = (!string.IsNullOrWhiteSpace(options.TextBodyTemplate)).ToString().ToLowerInvariant(),
            ["htmlBodyTemplateConfigured"] = (!string.IsNullOrWhiteSpace(options.HtmlBodyTemplate)).ToString().ToLowerInvariant(),
            ["categories"] = Join(options.Categories, "none"),
            ["customArgKeys"] = JoinKeys(options.CustomArgs, "none"),
            ["headerNames"] = JoinKeys(options.Headers, "none"),
            ["includeContextCustomArgs"] = options.IncludeContextCustomArgs.ToString().ToLowerInvariant(),
            ["includeContextHeaders"] = options.IncludeContextHeaders.ToString().ToLowerInvariant(),
            ["sandboxModeEnabled"] = options.EnableSandboxMode.ToString().ToLowerInvariant(),
            ["acceptedStatusCodes"] = Join(options.GetAcceptedStatusCodes(), "none"),
            ["providerMessageIdHeaderName"] = options.GetProviderMessageIdHeaderName(),
            ["timeoutSeconds"] = ((int)options.GetTimeout().TotalSeconds).ToString(CultureInfo.InvariantCulture),
            ["secretProjection"] = "redacted"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-invitation-delivery-sendgrid",
            displayName: "SendGrid Tenant Invitation Delivery",
            description: "Projects the configured SendGrid Mail Send API sender for tenant invitation delivery without exposing API keys or message content.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "sendgrid-email-sender",
                    displayName: "SendGrid Email Sender",
                    description: "Summarizes the configured SendGrid Mail Send endpoint, sender posture, accepted status contract, and contextual metadata policy.",
                    metadata: metadata)
            ]);
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string Join(IEnumerable<string> values, string emptyValue)
    {
        var normalized = values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized.Length == 0 ? emptyValue : string.Join(",", normalized);
    }

    private static string Join(IEnumerable<int> values, string emptyValue)
    {
        var normalized = values
            .Distinct()
            .Order()
            .Select(static value => value.ToString(CultureInfo.InvariantCulture))
            .ToArray();

        return normalized.Length == 0 ? emptyValue : string.Join(",", normalized);
    }

    private static string JoinKeys(IReadOnlyDictionary<string, string> values, string emptyValue)
    {
        var keys = values.Keys
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Select(static key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return keys.Length == 0 ? emptyValue : string.Join(",", keys);
    }

    private static string GetEmailDomain(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return "invalid";
        }

        var atIndex = address.LastIndexOf('@');
        return atIndex >= 0 && atIndex < address.Length - 1
            ? address[(atIndex + 1)..].Trim().ToLowerInvariant()
            : "invalid";
    }
}
