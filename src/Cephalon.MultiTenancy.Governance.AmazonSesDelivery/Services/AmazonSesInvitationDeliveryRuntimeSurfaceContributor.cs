using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Services;

internal sealed class AmazonSesInvitationDeliveryRuntimeSurfaceContributor(AmazonSesInvitationDeliveryOptions options) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "provider-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance.AmazonSesDelivery",
            ["provider"] = "amazon-ses",
            ["transport"] = "aws-sdk-sesv2",
            ["runtimeState"] = "configured",
            ["senderId"] = Normalize(options.SenderId, "amazon-ses-email"),
            ["senderOwnership"] = "provider-managed",
            ["supportedChannels"] = Join(options.SupportedChannels, "all"),
            ["regionSystemName"] = options.GetRegionSystemName() ?? "aws-sdk-default-chain",
            ["configurationSetNameConfigured"] = (options.GetConfigurationSetName() is not null).ToString().ToLowerInvariant(),
            ["fromEmailDomain"] = GetEmailDomain(options.FromEmail),
            ["fromNameConfigured"] = (!string.IsNullOrWhiteSpace(options.FromName)).ToString().ToLowerInvariant(),
            ["replyToAddressCount"] = options.ReplyToAddresses.Count.ToString(CultureInfo.InvariantCulture),
            ["recipientEmailMetadataKey"] = Normalize(options.RecipientEmailMetadataKey, "email"),
            ["subjectTemplateConfigured"] = (!string.IsNullOrWhiteSpace(options.SubjectTemplate)).ToString().ToLowerInvariant(),
            ["textBodyTemplateConfigured"] = (!string.IsNullOrWhiteSpace(options.TextBodyTemplate)).ToString().ToLowerInvariant(),
            ["htmlBodyTemplateConfigured"] = (!string.IsNullOrWhiteSpace(options.HtmlBodyTemplate)).ToString().ToLowerInvariant(),
            ["tagKeys"] = JoinKeys(options.Tags, "none"),
            ["includeContextTags"] = options.IncludeContextTags.ToString().ToLowerInvariant(),
            ["acceptedStatusCodes"] = Join(options.GetAcceptedStatusCodes(), "none"),
            ["timeoutSeconds"] = ((int)options.GetTimeout().TotalSeconds).ToString(CultureInfo.InvariantCulture),
            ["credentialSource"] = "aws-sdk-default-chain-or-host-registered-client",
            ["secretProjection"] = "redacted"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-invitation-delivery-amazon-ses",
            displayName: "Amazon SES Tenant Invitation Delivery",
            description: "Projects the configured Amazon SES v2 sender for tenant invitation delivery without exposing AWS credentials or message content.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "amazon-ses-email-sender",
                    displayName: "Amazon SES Email Sender",
                    description: "Summarizes the configured SES v2 sender, region resolution posture, accepted status contract, and message tag policy.",
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
