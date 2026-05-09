using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace Cephalon.Eventing.Configuration;

internal static class EventingOptionsConfigurationReader
{
    public static EventingOptions Read(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new EventingOptions();
        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Messaging")
            .GetSection("InProcessSubscriptions");
        var idempotencySection = section.GetSection("Idempotency");
        var publicationSchedulingSection = configuration
            .GetSection(sectionPath)
            .GetSection("Messaging")
            .GetSection("Publications")
            .GetSection("Scheduling");
        var publicationRoutingSection = configuration
            .GetSection(sectionPath)
            .GetSection("Messaging")
            .GetSection("Publications")
            .GetSection("Routing");
        var legacyPublicationSchedulingSection = configuration
            .GetSection(sectionPath)
            .GetSection("Messaging")
            .GetSection("PublicationScheduling");

        ReadBoolean(section, options, static (target, value) => target.EnableInProcessSubscriptionExecution = value, "EnableExecution", "Enabled");
        ReadInteger(section, options, static (target, value) => target.InProcessSubscriptionMaxAttempts = value, "MaxAttempts");
        ReadInteger(section, options, static (target, value) => target.InProcessSubscriptionRetryDelayMilliseconds = value, "RetryDelayMilliseconds");
        ReadBoolean(section, options, static (target, value) => target.ContinueInProcessSubscriptionExecutionAfterFailure = value, "ContinueAfterFailure");

        ReadBoolean(idempotencySection, options, static (target, value) => target.EnableInProcessSubscriptionIdempotency = value, "Enabled");
        ReadString(idempotencySection, options, static (target, value) => target.InProcessSubscriptionIdempotencyStore = value, "Store");
        ReadInteger(idempotencySection, options, static (target, value) => target.InProcessSubscriptionIdempotencyRetentionMinutes = value, "RetentionMinutes");

        ReadBoolean(section, options, static (target, value) => target.EnableInProcessSubscriptionIdempotency = value, "EnableIdempotency");
        ReadString(section, options, static (target, value) => target.InProcessSubscriptionIdempotencyStore = value, "IdempotencyStore");
        ReadInteger(section, options, static (target, value) => target.InProcessSubscriptionIdempotencyRetentionMinutes = value, "IdempotencyRetentionMinutes");

        ReadBoolean(publicationSchedulingSection, options, static (target, value) => target.EnablePublicationScheduling = value, "Enabled");
        ReadInteger(publicationSchedulingSection, options, static (target, value) => target.PublicationSchedulingMaxDelayMilliseconds = value, "MaxDelayMilliseconds");
        ReadInteger(publicationSchedulingSection, options, static (target, value) => target.PublicationSchedulingMaxPendingCount = value, "MaxPendingCount");

        ReadBoolean(publicationRoutingSection, options, static (target, value) => target.EnablePublicationRouting = value, "Enabled");
        ReadString(publicationRoutingSection, options, static (target, value) => target.PublicationRoutingAutoChannelId = value, "AutoChannelId");
        ReadBoolean(publicationRoutingSection, options, static (target, value) => target.PublicationRoutingRequireMatchedRoute = value, "RequireMatchedRoute");
        ReadBoolean(publicationRoutingSection, options, static (target, value) => target.PublicationRoutingRejectMismatchedExplicitChannel = value, "RejectMismatchedExplicitChannel");
        ReadRoutes(publicationRoutingSection.GetSection("Routes"), options);

        ReadBoolean(legacyPublicationSchedulingSection, options, static (target, value) => target.EnablePublicationScheduling = value, "Enabled");
        ReadInteger(legacyPublicationSchedulingSection, options, static (target, value) => target.PublicationSchedulingMaxDelayMilliseconds = value, "MaxDelayMilliseconds");
        ReadInteger(legacyPublicationSchedulingSection, options, static (target, value) => target.PublicationSchedulingMaxPendingCount = value, "MaxPendingCount");

        return options;
    }

    private static void ReadBoolean(
        IConfiguration configuration,
        EventingOptions options,
        Action<EventingOptions, bool> assign,
        params string[] keys)
    {
        if (!TryGetValue(configuration, out var value, keys))
        {
            return;
        }

        if (!bool.TryParse(value, out var parsed))
        {
            throw new FormatException(
                $"Eventing configuration value '{value}' for '{string.Join("' or '", keys)}' must be 'true' or 'false'.");
        }

        assign(options, parsed);
    }

    private static void ReadInteger(
        IConfiguration configuration,
        EventingOptions options,
        Action<EventingOptions, int> assign,
        params string[] keys)
    {
        if (!TryGetValue(configuration, out var value, keys))
        {
            return;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new FormatException(
                $"Eventing configuration value '{value}' for '{string.Join("' or '", keys)}' must be an integer.");
        }

        assign(options, parsed);
    }

    private static void ReadString(
        IConfiguration configuration,
        EventingOptions options,
        Action<EventingOptions, string> assign,
        params string[] keys)
    {
        if (TryGetValue(configuration, out var value, keys))
        {
            assign(options, value);
        }
    }

    private static void ReadRoutes(
        IConfiguration configuration,
        EventingOptions options)
    {
        foreach (var child in configuration.GetChildren())
        {
            if (string.IsNullOrWhiteSpace(child.Key) || string.IsNullOrWhiteSpace(child.Value))
            {
                continue;
            }

            options.PublicationRoutes[child.Key.Trim()] = child.Value.Trim();
        }
    }

    private static bool TryGetValue(
        IConfiguration configuration,
        out string value,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            var configured = configuration[key];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                value = configured;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }
}
