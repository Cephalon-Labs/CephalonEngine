using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Services;
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
        var messagingPath = $"{sectionPath}:Messaging";
        var messagingSection = configuration
            .GetSection(sectionPath)
            .GetSection("Messaging");
        var section = messagingSection
            .GetSection("InProcessSubscriptions");
        var idempotencySection = section.GetSection("Idempotency");
        var publicationSchedulingSection = messagingSection
            .GetSection("Publications")
            .GetSection("Scheduling");
        var publicationRoutingSection = messagingSection
            .GetSection("Publications")
            .GetSection("Routing");
        var legacyPublicationSchedulingSection = messagingSection
            .GetSection("PublicationScheduling");

        ReadChannels(messagingSection.GetSection("Channels"), options, $"{messagingPath}:Channels");
        ReadChannels(messagingSection.GetSection("EventChannels"), options, $"{messagingPath}:EventChannels");
        RejectCodeOwnedSection(
            messagingSection.GetSection("Subscriptions"),
            $"{messagingPath}:Subscriptions",
            "event subscription descriptors");
        RejectCodeOwnedSection(
            messagingSection.GetSection("EventSubscriptions"),
            $"{messagingPath}:EventSubscriptions",
            "event subscription descriptors");
        RejectCodeOwnedSection(
            messagingSection.GetSection("SubscriptionHandlers"),
            $"{messagingPath}:SubscriptionHandlers",
            "event subscription handler bindings");
        RejectCodeOwnedSection(
            messagingSection.GetSection("EventSubscriptionHandlers"),
            $"{messagingPath}:EventSubscriptionHandlers",
            "event subscription handler bindings");

        ReadBoolean(section, options, static (target, value) => target.EnableInProcessSubscriptionExecution = value, "EnableExecution", "Enabled");
        ReadInteger(section, options, static (target, value) => target.InProcessSubscriptionMaxAttempts = value, "MaxAttempts");
        ReadInteger(section, options, static (target, value) => target.InProcessSubscriptionRetryDelayMilliseconds = value, "RetryDelayMilliseconds");
        ReadString(section, options, static (target, value) => target.InProcessSubscriptionRetryBackoff = value, "RetryBackoff", "Backoff");
        ReadInteger(section, options, static (target, value) => target.InProcessSubscriptionRetryBackoffMultiplier = value, "RetryBackoffMultiplier", "BackoffMultiplier");
        ReadInteger(section, options, static (target, value) => target.InProcessSubscriptionRetryMaxDelayMilliseconds = value, "RetryMaxDelayMilliseconds", "MaxRetryDelayMilliseconds");
        ReadInteger(section, options, static (target, value) => target.InProcessSubscriptionRetryJitterPercent = value, "RetryJitterPercent", "JitterPercent");
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

    private static void ReadChannels(
        IConfiguration configuration,
        EventingOptions options,
        string path)
    {
        foreach (var child in configuration.GetChildren())
        {
            var id = ReadDescriptorId(child, path, "channel");
            var displayName = ReadOptionalString(child, "DisplayName", "Name") ?? id;
            var description = ReadOptionalString(child, "Description") ?? $"{displayName} event channel.";

            options.Channels.Add(new EventChannelDescriptor(
                id,
                displayName,
                description,
                ReadStringList(child.GetSection("Tags"))));
        }
    }

    private static void RejectCodeOwnedSection(
        IConfigurationSection configuration,
        string path,
        string descriptorKind)
    {
        if (string.IsNullOrWhiteSpace(configuration.Value) &&
            !configuration.GetChildren().Any())
        {
            return;
        }

        throw new InvalidOperationException(
            $"Eventing {descriptorKind} are code-owned for performance and type safety. Do not configure '{path}'; register subscriptions and executors in code through AddEventing(...), IEventSubscriptionContributor, and IEventSubscriptionExecutor.");
    }

    private static string ReadDescriptorId(
        IConfigurationSection configuration,
        string path,
        string descriptorKind)
    {
        var id = ReadOptionalString(configuration, "Id");
        if (!string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        if (int.TryParse(configuration.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            throw new FormatException(
                $"Eventing {descriptorKind} configuration entry '{path}:{configuration.Key}' must provide an 'Id' value when entries are represented as an array.");
        }

        return configuration.Key;
    }

    private static string? ReadOptionalString(
        IConfiguration configuration,
        params string[] keys)
    {
        return TryGetValue(configuration, out var value, keys)
            ? value.Trim()
            : null;
    }

    private static List<string> ReadStringList(IConfigurationSection configuration)
    {
        var values = new List<string>();
        if (!string.IsNullOrWhiteSpace(configuration.Value))
        {
            values.AddRange(configuration.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        foreach (var child in configuration.GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(child.Value))
            {
                values.Add(child.Value.Trim());
            }
        }

        return values;
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
