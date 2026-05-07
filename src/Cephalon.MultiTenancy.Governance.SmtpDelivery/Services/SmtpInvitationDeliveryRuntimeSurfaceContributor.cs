using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.SmtpDelivery.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.SmtpDelivery.Services;

internal sealed class SmtpInvitationDeliveryRuntimeSurfaceContributor(SmtpInvitationDeliveryOptions options) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "provider-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance.SmtpDelivery",
            ["provider"] = "smtp",
            ["transport"] = "smtp",
            ["runtimeState"] = "configured",
            ["senderId"] = Normalize(options.SenderId, "smtp-email"),
            ["senderOwnership"] = "provider-managed",
            ["supportedChannels"] = Join(options.SupportedChannels, "all"),
            ["host"] = Normalize(options.Host, "invalid"),
            ["port"] = options.GetPort().ToString(CultureInfo.InvariantCulture),
            ["useSsl"] = options.UseSsl.ToString().ToLowerInvariant(),
            ["usernameConfigured"] = (!string.IsNullOrWhiteSpace(options.UserName)).ToString().ToLowerInvariant(),
            ["passwordConfigured"] = (!string.IsNullOrWhiteSpace(options.Password)).ToString().ToLowerInvariant(),
            ["fromAddressDomain"] = GetEmailDomain(options.FromAddress),
            ["fromDisplayNameConfigured"] = (!string.IsNullOrWhiteSpace(options.FromDisplayName)).ToString().ToLowerInvariant(),
            ["recipientAddressMetadataKey"] = Normalize(options.RecipientAddressMetadataKey, "email"),
            ["subjectTemplateConfigured"] = (!string.IsNullOrWhiteSpace(options.SubjectTemplate)).ToString().ToLowerInvariant(),
            ["textBodyTemplateConfigured"] = (!string.IsNullOrWhiteSpace(options.TextBodyTemplate)).ToString().ToLowerInvariant(),
            ["htmlBodyTemplateConfigured"] = (!string.IsNullOrWhiteSpace(options.HtmlBodyTemplate)).ToString().ToLowerInvariant(),
            ["headerNames"] = JoinKeys(options.Headers, "none"),
            ["includeContextHeaders"] = options.IncludeContextHeaders.ToString().ToLowerInvariant(),
            ["messageIdDomain"] = options.GetMessageIdDomain(),
            ["timeoutSeconds"] = ((int)options.GetTimeout().TotalSeconds).ToString(CultureInfo.InvariantCulture),
            ["secretProjection"] = "redacted"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-invitation-delivery-smtp",
            displayName: "SMTP Tenant Invitation Delivery",
            description: "Projects the configured SMTP sender for tenant invitation delivery without exposing relay credentials or message body templates.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "smtp-email-sender",
                    displayName: "SMTP Email Sender",
                    description: "Summarizes the configured SMTP relay, accepted delivery channels, message-shaping posture, and credential configuration state.",
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
