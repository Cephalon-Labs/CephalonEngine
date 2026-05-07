using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;

internal sealed class MicrosoftGraphInvitationDeliveryRuntimeSurfaceContributor(MicrosoftGraphInvitationDeliveryOptions options) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var baseUrl = options.TryGetBaseUrl();
        var sendMailEndpoint = options.GetSendMailEndpoint();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "provider-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery",
            ["provider"] = "microsoft-graph",
            ["transport"] = "http-rest",
            ["runtimeState"] = baseUrl is null ? "invalid" : "configured",
            ["senderId"] = Normalize(options.SenderId, "microsoft-graph-email"),
            ["senderOwnership"] = "provider-managed",
            ["supportedChannels"] = Join(options.SupportedChannels, "all"),
            ["baseUrlHost"] = baseUrl?.Host ?? "invalid",
            ["apiVersion"] = options.GetApiVersion(),
            ["sendMailPathKind"] = string.IsNullOrWhiteSpace(options.SenderUserId) ? "me" : "users",
            ["sendMailPathConfigured"] = (!string.IsNullOrWhiteSpace(sendMailEndpoint.AbsolutePath)).ToString().ToLowerInvariant(),
            ["senderUserIdConfigured"] = (!string.IsNullOrWhiteSpace(options.SenderUserId)).ToString().ToLowerInvariant(),
            ["staticAccessTokenConfigured"] = (!string.IsNullOrWhiteSpace(options.AccessToken)).ToString().ToLowerInvariant(),
            ["recipientEmailMetadataKey"] = Normalize(options.RecipientEmailMetadataKey, "email"),
            ["subjectTemplateConfigured"] = (!string.IsNullOrWhiteSpace(options.SubjectTemplate)).ToString().ToLowerInvariant(),
            ["textBodyTemplateConfigured"] = (!string.IsNullOrWhiteSpace(options.TextBodyTemplate)).ToString().ToLowerInvariant(),
            ["htmlBodyTemplateConfigured"] = (!string.IsNullOrWhiteSpace(options.HtmlBodyTemplate)).ToString().ToLowerInvariant(),
            ["categories"] = Join(options.Categories, "none"),
            ["headerNames"] = JoinKeys(options.Headers, "none"),
            ["includeContextHeaders"] = options.IncludeContextHeaders.ToString().ToLowerInvariant(),
            ["saveToSentItems"] = options.SaveToSentItems.ToString().ToLowerInvariant(),
            ["acceptedStatusCodes"] = Join(options.GetAcceptedStatusCodes(), "none"),
            ["timeoutSeconds"] = ((int)options.GetTimeout().TotalSeconds).ToString(CultureInfo.InvariantCulture),
            ["secretProjection"] = "redacted"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-invitation-delivery-microsoft-graph",
            displayName: "Microsoft Graph Tenant Invitation Delivery",
            description: "Projects the configured Microsoft Graph sendMail sender for tenant invitation delivery without exposing bearer tokens or message content.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "microsoft-graph-email-sender",
                    displayName: "Microsoft Graph Email Sender",
                    description: "Summarizes the configured Graph sendMail endpoint, mailbox scope kind, accepted status contract, and message header policy.",
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
}
