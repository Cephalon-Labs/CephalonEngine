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
        ReadSubscriptions(messagingSection.GetSection("Subscriptions"), options, $"{messagingPath}:Subscriptions");
        ReadSubscriptions(messagingSection.GetSection("EventSubscriptions"), options, $"{messagingPath}:EventSubscriptions");

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

    private static void ReadSubscriptions(
        IConfiguration configuration,
        EventingOptions options,
        string path)
    {
        foreach (var child in configuration.GetChildren())
        {
            var id = ReadDescriptorId(child, path, "subscription");
            var displayName = ReadOptionalString(child, "DisplayName", "Name") ?? id;
            var description = ReadOptionalString(child, "Description") ?? $"{displayName} event subscription.";
            var channelId = ReadRequiredString(child, path, id, "subscription", "ChannelId", "Channel");
            var handlerId = ReadOptionalString(child, "HandlerId", "Handler", "ExecutorId", "ConsumerId") ?? id;
            var deliveryMode = ReadOptionalString(child, "DeliveryMode", "Mode") ?? "message-handler";
            var metadata = ReadMetadata(child.GetSection("Metadata"));
            metadata.TryAdd("descriptorSource", "configuration");
            metadata.TryAdd("configurationPath", $"{path}:{child.Key}");

            options.Subscriptions.Add(new EventSubscriptionDescriptor(
                id,
                displayName,
                description,
                channelId,
                handlerId,
                deliveryMode,
                ReadStringList(child.GetSection("Tags")),
                metadata));
        }
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

    private static string ReadRequiredString(
        IConfiguration configuration,
        string path,
        string id,
        string descriptorKind,
        params string[] keys)
    {
        var value = ReadOptionalString(configuration, keys);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new FormatException(
            $"Eventing {descriptorKind} configuration entry '{path}:{id}' must provide '{string.Join("' or '", keys)}'.");
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

    private static Dictionary<string, string> ReadMetadata(IConfiguration configuration)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in configuration.GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(child.Key) && !string.IsNullOrWhiteSpace(child.Value))
            {
                metadata[child.Key.Trim()] = child.Value.Trim();
            }
        }

        return metadata;
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
