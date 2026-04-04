using Microsoft.Extensions.Logging;

namespace Cephalon.Eventing.Services;

internal static class EventingLoggerMessages
{
    private static readonly Action<ILogger, string, string, Exception?> PublicationStaged = LoggerMessage.Define<string, string>(
        logLevel: LogLevel.Information,
        eventId: new EventId(EventingDiagnosticsConventions.PublicationStaged.Id, EventingDiagnosticsConventions.PublicationStaged.Name),
        formatString: "Event publication '{PublicationId}' was staged on channel '{ChannelId}' through the active outbox path.");

    private static readonly Action<ILogger, string, string, int, Exception?> SubscriptionStarted = LoggerMessage.Define<string, string, int>(
        logLevel: LogLevel.Information,
        eventId: new EventId(EventingDiagnosticsConventions.SubscriptionStarted.Id, EventingDiagnosticsConventions.SubscriptionStarted.Name),
        formatString: "Declared subscription '{SubscriptionId}' started handling message '{MessageId}' at attempt {Attempt}.");

    private static readonly Action<ILogger, string, string, int, Exception?> SubscriptionSucceeded = LoggerMessage.Define<string, string, int>(
        logLevel: LogLevel.Information,
        eventId: new EventId(EventingDiagnosticsConventions.SubscriptionSucceeded.Id, EventingDiagnosticsConventions.SubscriptionSucceeded.Name),
        formatString: "Declared subscription '{SubscriptionId}' succeeded for message '{MessageId}' at attempt {Attempt}.");

    private static readonly Action<ILogger, string, string, int, string, Exception?> SubscriptionFailed = LoggerMessage.Define<string, string, int, string>(
        logLevel: LogLevel.Error,
        eventId: new EventId(EventingDiagnosticsConventions.SubscriptionFailed.Id, EventingDiagnosticsConventions.SubscriptionFailed.Name),
        formatString: "Declared subscription '{SubscriptionId}' failed for message '{MessageId}' at attempt {Attempt}. Error: {Error}.");

    private static readonly Action<ILogger, string, string, int, string, Exception?> SubscriptionRetryScheduled = LoggerMessage.Define<string, string, int, string>(
        logLevel: LogLevel.Warning,
        eventId: new EventId(EventingDiagnosticsConventions.SubscriptionRetryScheduled.Id, EventingDiagnosticsConventions.SubscriptionRetryScheduled.Name),
        formatString: "Declared subscription '{SubscriptionId}' scheduled another retry for message '{MessageId}' at attempt {Attempt}. Error: {Error}.");

    private static readonly Action<ILogger, string, string, int, Exception?> SubscriptionSkipped = LoggerMessage.Define<string, string, int>(
        logLevel: LogLevel.Information,
        eventId: new EventId(EventingDiagnosticsConventions.SubscriptionSkipped.Id, EventingDiagnosticsConventions.SubscriptionSkipped.Name),
        formatString: "Declared subscription '{SubscriptionId}' skipped message '{MessageId}' at attempt {Attempt}.");

    private static readonly Action<ILogger, string, string, int, Exception?> PublicationDispatchStarted = LoggerMessage.Define<string, string, int>(
        logLevel: LogLevel.Information,
        eventId: new EventId(EventingDiagnosticsConventions.PublicationDispatchStarted.Id, EventingDiagnosticsConventions.PublicationDispatchStarted.Name),
        formatString: "Event publisher '{PublisherId}' started dispatch for publication '{PublicationId}' at attempt {Attempt}.");

    private static readonly Action<ILogger, string, string, int, Exception?> PublicationDispatchSucceeded = LoggerMessage.Define<string, string, int>(
        logLevel: LogLevel.Information,
        eventId: new EventId(EventingDiagnosticsConventions.PublicationDispatchSucceeded.Id, EventingDiagnosticsConventions.PublicationDispatchSucceeded.Name),
        formatString: "Event publisher '{PublisherId}' succeeded for publication '{PublicationId}' at attempt {Attempt}.");

    private static readonly Action<ILogger, string, string, int, string, Exception?> PublicationDispatchFailed = LoggerMessage.Define<string, string, int, string>(
        logLevel: LogLevel.Error,
        eventId: new EventId(EventingDiagnosticsConventions.PublicationDispatchFailed.Id, EventingDiagnosticsConventions.PublicationDispatchFailed.Name),
        formatString: "Event publisher '{PublisherId}' failed for publication '{PublicationId}' at attempt {Attempt}. Error: {Error}.");

    private static readonly Action<ILogger, string, string, int, string, Exception?> PublicationDispatchRetryScheduled = LoggerMessage.Define<string, string, int, string>(
        logLevel: LogLevel.Warning,
        eventId: new EventId(EventingDiagnosticsConventions.PublicationDispatchRetryScheduled.Id, EventingDiagnosticsConventions.PublicationDispatchRetryScheduled.Name),
        formatString: "Event publisher '{PublisherId}' scheduled another retry for publication '{PublicationId}' at attempt {Attempt}. Error: {Error}.");

    private static readonly Action<ILogger, string, string, int, Exception?> PublicationDispatchSkipped = LoggerMessage.Define<string, string, int>(
        logLevel: LogLevel.Information,
        eventId: new EventId(EventingDiagnosticsConventions.PublicationDispatchSkipped.Id, EventingDiagnosticsConventions.PublicationDispatchSkipped.Name),
        formatString: "Event publisher '{PublisherId}' skipped publication '{PublicationId}' at attempt {Attempt}.");

    public static void LogPublicationStaged(
        ILogger logger,
        string publicationId,
        string channelId)
    {
        PublicationStaged(logger, publicationId, channelId, null);
    }

    public static void LogSubscriptionStarted(
        ILogger logger,
        string subscriptionId,
        string messageId,
        int attempt)
    {
        SubscriptionStarted(logger, subscriptionId, messageId, attempt, null);
    }

    public static void LogSubscriptionSucceeded(
        ILogger logger,
        string subscriptionId,
        string messageId,
        int attempt)
    {
        SubscriptionSucceeded(logger, subscriptionId, messageId, attempt, null);
    }

    public static void LogSubscriptionFailed(
        ILogger logger,
        string subscriptionId,
        string messageId,
        int attempt,
        string error)
    {
        SubscriptionFailed(logger, subscriptionId, messageId, attempt, error, null);
    }

    public static void LogSubscriptionRetryScheduled(
        ILogger logger,
        string subscriptionId,
        string messageId,
        int attempt,
        string error)
    {
        SubscriptionRetryScheduled(logger, subscriptionId, messageId, attempt, error, null);
    }

    public static void LogSubscriptionSkipped(
        ILogger logger,
        string subscriptionId,
        string messageId,
        int attempt)
    {
        SubscriptionSkipped(logger, subscriptionId, messageId, attempt, null);
    }

    public static void LogPublicationDispatchStarted(
        ILogger logger,
        string publisherId,
        string publicationId,
        int attempt)
    {
        PublicationDispatchStarted(logger, publisherId, publicationId, attempt, null);
    }

    public static void LogPublicationDispatchSucceeded(
        ILogger logger,
        string publisherId,
        string publicationId,
        int attempt)
    {
        PublicationDispatchSucceeded(logger, publisherId, publicationId, attempt, null);
    }

    public static void LogPublicationDispatchFailed(
        ILogger logger,
        string publisherId,
        string publicationId,
        int attempt,
        string error)
    {
        PublicationDispatchFailed(logger, publisherId, publicationId, attempt, error, null);
    }

    public static void LogPublicationDispatchRetryScheduled(
        ILogger logger,
        string publisherId,
        string publicationId,
        int attempt,
        string error)
    {
        PublicationDispatchRetryScheduled(logger, publisherId, publicationId, attempt, error, null);
    }

    public static void LogPublicationDispatchSkipped(
        ILogger logger,
        string publisherId,
        string publicationId,
        int attempt)
    {
        PublicationDispatchSkipped(logger, publisherId, publicationId, attempt, null);
    }

    public static void LogDispatchStarted(
        ILogger logger,
        string outboxId,
        string channelId,
        string messageId,
        int attempt)
    {
        LogPublicationDispatchStarted(logger, outboxId, messageId, attempt);
    }

    public static void LogDispatchSucceeded(
        ILogger logger,
        string outboxId,
        string channelId,
        string messageId,
        int attempt)
    {
        LogPublicationDispatchSucceeded(logger, outboxId, messageId, attempt);
    }

    public static void LogDispatchFailed(
        ILogger logger,
        string outboxId,
        string channelId,
        string messageId,
        int attempt,
        string error)
    {
        LogPublicationDispatchFailed(logger, outboxId, messageId, attempt, error);
    }

    public static void LogDispatchRetryScheduled(
        ILogger logger,
        string outboxId,
        string channelId,
        string messageId,
        int attempt,
        string error)
    {
        LogPublicationDispatchRetryScheduled(logger, outboxId, messageId, attempt, error);
    }

    public static void LogDispatchSkipped(
        ILogger logger,
        string outboxId,
        string channelId,
        string messageId,
        int attempt)
    {
        LogPublicationDispatchSkipped(logger, outboxId, messageId, attempt);
    }
}
