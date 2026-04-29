namespace Cephalon.Eventing.Services;

/// <summary>
/// Defines stable metadata keys used by the event-subscriptions runtime surface.
/// </summary>
/// <remarks>
/// These keys appear in the <c>event-subscriptions</c> technology runtime surface so operators
/// and companion packs can distinguish descriptor-only, application-managed, hosted-execution-linked,
/// and runtime-bound subscription paths without parsing provider-specific metadata.
/// </remarks>
public static class EventSubscriptionRuntimeMetadataKeys
{
    /// <summary>
    /// Identifies the logical event channel consumed by the declared subscription.
    /// </summary>
    public const string ChannelId = "channelId";

    /// <summary>
    /// Identifies the logical handler or consumer declared for the subscription.
    /// </summary>
    public const string HandlerId = "handlerId";

    /// <summary>
    /// Identifies the declared delivery mode for the subscription.
    /// </summary>
    public const string DeliveryMode = "deliveryMode";

    /// <summary>
    /// Identifies who owns the dispatch path feeding subscription execution.
    /// </summary>
    public const string DispatchRuntime = "dispatchRuntime";

    /// <summary>
    /// Identifies whether an inbox is available for the subscription's channel.
    /// </summary>
    public const string Inbox = "inbox";

    /// <summary>
    /// Identifies who owns the inbox linkage for the subscription's channel.
    /// </summary>
    public const string InboxLink = "inboxLink";

    /// <summary>
    /// Identifies whether runtime observations have been reported for the subscription.
    /// </summary>
    public const string RuntimeState = "runtimeState";

    /// <summary>
    /// Identifies the subscription execution posture, such as application-managed or runtime-bound.
    /// </summary>
    public const string SubscriptionRuntime = "subscriptionRuntime";

    /// <summary>
    /// Identifies the managed execution-runtime identifier bound to the subscription.
    /// </summary>
    public const string ExecutionRuntimeId = "executionRuntimeId";

    /// <summary>
    /// Identifies who owns the real subscription execution path.
    /// </summary>
    public const string ExecutionOwnership = "executionOwnership";

    /// <summary>
    /// Identifies the execution mode used by the managed subscription binding.
    /// </summary>
    public const string ExecutionMode = "executionMode";

    /// <summary>
    /// Identifies the comma-separated metadata keys contributed by the managed execution binding.
    /// </summary>
    public const string BindingMetadataKeys = "bindingMetadataKeys";

    /// <summary>
    /// Prefix for individual managed execution-binding metadata entries.
    /// </summary>
    public const string BindingMetadataPrefix = "binding.";

    /// <summary>
    /// Identifies the linked inbox identifiers that can observe the subscription channel.
    /// </summary>
    public const string InboxIds = "inboxIds";

    /// <summary>
    /// Identifies all hosted executions linked to the declared subscription.
    /// </summary>
    public const string HostedExecutionIds = "hostedExecutionIds";

    /// <summary>
    /// Identifies the single hosted execution linked to the declared subscription when exactly one exists.
    /// </summary>
    public const string HostedExecutionId = "hostedExecutionId";

    /// <summary>
    /// Identifies the execution graph linked to the subscription's hosted execution.
    /// </summary>
    public const string ExecutionGraphId = "executionGraphId";

    /// <summary>
    /// Identifies the latest reported subscription execution outcome.
    /// </summary>
    public const string LastOutcome = "lastOutcome";

    /// <summary>
    /// Identifies whether the latest runtime observation says a retry is pending.
    /// </summary>
    public const string RetryPending = "retryPending";

    /// <summary>
    /// Prefix for individual runtime-observation metadata entries.
    /// </summary>
    public const string ReportedMetadataPrefix = "reported.";
}
