using Cephalon.Eventing.Services;

namespace Cephalon.Eventing.Configuration;

/// <summary>
/// Configures the built-in eventing runtime pack.
/// </summary>
/// <remarks>
/// These options seed the host-owned part of the eventing runtime. Installed modules can still
/// contribute additional channels through <see cref="Services.IEventChannelContributor" /> and
/// additional subscription, contract, serializer, schema registry, upcaster, and context policy descriptors through
/// <see cref="Services.IEventSubscriptionContributor" />,
/// <see cref="Services.IEventContractContributor" />,
/// <see cref="Services.IEventSerializerContributor" />,
/// <see cref="Services.IEventSchemaRegistryContributor" />,
/// <see cref="Services.IEventUpcasterContributor" />, and
/// <see cref="Services.IEventContextPolicyContributor" />.
/// </remarks>
public sealed class EventingOptions
{
    /// <summary>
    /// Creates eventing options with the default host-owned features enabled.
    /// </summary>
    public EventingOptions()
    {
    }

    /// <summary>
    /// Gets the host-defined event channels that should be available to the eventing runtime.
    /// </summary>
    public IList<EventChannelDescriptor> Channels { get; } = [];

    /// <summary>
    /// Gets the host-defined event subscription descriptors that should be available to the eventing runtime.
    /// </summary>
    public IList<EventSubscriptionDescriptor> Subscriptions { get; } = [];

    /// <summary>
    /// Gets the host-defined event contract descriptors that should be available to the eventing runtime.
    /// </summary>
    /// <remarks>
    /// These descriptors are code-owned contract metadata. They do not make publish or subscription
    /// execution perform config lookups on the hot path.
    /// </remarks>
    public IList<EventContractDescriptor> Contracts { get; } = [];

    /// <summary>
    /// Gets the host-defined event serializer descriptors that should be available to the eventing runtime.
    /// </summary>
    /// <remarks>
    /// These descriptors are code-owned serializer availability metadata. They do not make publish or
    /// subscription execution perform config lookups on the hot path.
    /// </remarks>
    public IList<EventSerializerDescriptor> Serializers { get; } = [];

    /// <summary>
    /// Gets the host-defined event schema registry descriptors that should be available to the eventing runtime.
    /// </summary>
    /// <remarks>
    /// These descriptors are code-owned registry availability metadata. They do not make publish or
    /// subscription execution perform registry lookups on the hot path.
    /// </remarks>
    public IList<EventSchemaRegistryDescriptor> SchemaRegistries { get; } = [];

    /// <summary>
    /// Gets the host-defined event upcaster descriptors that should be available to the eventing runtime.
    /// </summary>
    /// <remarks>
    /// These descriptors are code-owned version-transition metadata. They do not make publish or
    /// subscription execution perform upcaster lookups on the hot path.
    /// </remarks>
    public IList<EventUpcasterDescriptor> Upcasters { get; } = [];

    /// <summary>
    /// Gets the host-defined event context policy descriptors that should be available to the eventing runtime.
    /// </summary>
    /// <remarks>
    /// These descriptors are code-owned tenant, correlation, causation, baggage, and message-header
    /// policy metadata. They do not make publish or subscription execution perform context propagation
    /// lookups on the hot path.
    /// </remarks>
    public IList<EventContextPolicyDescriptor> ContextPolicies { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether publishing features are enabled.
    /// </summary>
    public bool EnablePublishing { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether subscription features are enabled.
    /// </summary>
    public bool EnableSubscriptions { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the core eventing pack should execute matching
    /// subscription executors directly inside the current process when a publication is accepted.
    /// </summary>
    /// <remarks>
    /// This is an opt-in managed execution baseline for lightweight hosts and tests. It is not a
    /// durable broker, inbox, or retry runtime; companion packs should still own those richer
    /// delivery guarantees when they are selected.
    /// </remarks>
    public bool EnableInProcessSubscriptionExecution { get; set; }

    private int inProcessSubscriptionMaxAttempts = 1;
    private int inProcessSubscriptionRetryDelayMilliseconds;
    private int inProcessSubscriptionIdempotencyRetentionMinutes = 60;
    private string inProcessSubscriptionIdempotencyStore = InProcessEventingIdempotencyPolicy.ProcessLocalStore;
    private int publicationSchedulingMaxDelayMilliseconds = 86_400_000;
    private int publicationSchedulingMaxPendingCount = 256;
    private string publicationRoutingAutoChannelId = "auto";
    private int remediationCommandHistoryLimit = 256;

    /// <summary>
    /// Gets or sets the maximum number of direct in-process execution attempts per matching subscription.
    /// </summary>
    /// <remarks>
    /// The default value of <c>1</c> preserves the no-retry baseline. Values greater than <c>1</c>
    /// enable a bounded, process-local retry loop; this still does not provide durable broker,
    /// inbox, or distributed retry guarantees.
    /// </remarks>
    public int InProcessSubscriptionMaxAttempts
    {
        get => inProcessSubscriptionMaxAttempts;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "In-process subscription max attempts must be greater than or equal to 1.");
            }

            inProcessSubscriptionMaxAttempts = value;
        }
    }

    /// <summary>
    /// Gets or sets the delay in milliseconds before the direct in-process publisher retries a failed subscription attempt.
    /// </summary>
    /// <remarks>
    /// The delay is applied only when <see cref="InProcessSubscriptionMaxAttempts" /> is greater
    /// than <c>1</c>. The default value of <c>0</c> retries immediately and is useful for tests
    /// and lightweight process-local remediation paths.
    /// </remarks>
    public int InProcessSubscriptionRetryDelayMilliseconds
    {
        get => inProcessSubscriptionRetryDelayMilliseconds;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "In-process subscription retry delay must be greater than or equal to 0 milliseconds.");
            }

            inProcessSubscriptionRetryDelayMilliseconds = value;
        }
    }

    private string inProcessSubscriptionRetryBackoff = InProcessEventingRetryPolicy.FixedBackoff;
    private int inProcessSubscriptionRetryBackoffMultiplier = 2;
    private int inProcessSubscriptionRetryMaxDelayMilliseconds = 60_000;
    private int inProcessSubscriptionRetryJitterPercent;

    /// <summary>
    /// Gets or sets the process-local backoff strategy used between failed direct subscription attempts.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>fixed</c> and <c>exponential</c>. The default <c>fixed</c> value
    /// preserves the original bounded in-process retry behavior.
    /// </remarks>
    public string InProcessSubscriptionRetryBackoff
    {
        get => inProcessSubscriptionRetryBackoff;
        set => inProcessSubscriptionRetryBackoff = InProcessEventingRetryPolicy.NormalizeBackoff(value);
    }

    /// <summary>
    /// Gets or sets the exponential retry-delay multiplier used by the direct in-process publisher.
    /// </summary>
    /// <remarks>
    /// The value is used only when <see cref="InProcessSubscriptionRetryBackoff" /> is <c>exponential</c>.
    /// </remarks>
    public int InProcessSubscriptionRetryBackoffMultiplier
    {
        get => inProcessSubscriptionRetryBackoffMultiplier;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "In-process subscription retry backoff multiplier must be greater than or equal to 1.");
            }

            inProcessSubscriptionRetryBackoffMultiplier = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum retry delay, in milliseconds, accepted by the direct in-process publisher.
    /// </summary>
    public int InProcessSubscriptionRetryMaxDelayMilliseconds
    {
        get => inProcessSubscriptionRetryMaxDelayMilliseconds;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "In-process subscription retry max delay must be greater than or equal to 0 milliseconds.");
            }

            inProcessSubscriptionRetryMaxDelayMilliseconds = value;
        }
    }

    /// <summary>
    /// Gets or sets the deterministic retry jitter percentage applied by the direct in-process publisher.
    /// </summary>
    /// <remarks>
    /// Jitter is derived from publication id, subscription id, and attempt number so it stays
    /// deterministic for a message while still spreading retry timings across different messages.
    /// </remarks>
    public int InProcessSubscriptionRetryJitterPercent
    {
        get => inProcessSubscriptionRetryJitterPercent;
        set
        {
            if (value is < 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "In-process subscription retry jitter percent must be between 0 and 100.");
            }

            inProcessSubscriptionRetryJitterPercent = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the direct in-process publisher should suppress
    /// duplicate completed subscription executions for the same publication identifier.
    /// </summary>
    /// <remarks>
    /// This is a bounded process-local guard for lightweight hosts. It records only successful
    /// direct executions in memory and skips later duplicate <c>subscriptionId + publicationId</c>
    /// pairs while the entry remains in the retention window. It is not a durable inbox,
    /// cross-node idempotency store, or broker-owned exactly-once guarantee.
    /// </remarks>
    public bool EnableInProcessSubscriptionIdempotency { get; set; }

    /// <summary>
    /// Gets or sets the store used by the direct in-process publisher for completed subscription-execution idempotency.
    /// </summary>
    /// <remarks>
    /// The default value is <c>process-local</c>, which records completed executions in the current process only.
    /// Set the value to <c>inbox</c> to use exactly one registered <see cref="Cephalon.Abstractions.Data.IInbox" />
    /// as the duplicate-suppression store while keeping the same direct in-process execution path.
    /// </remarks>
    public string InProcessSubscriptionIdempotencyStore
    {
        get => inProcessSubscriptionIdempotencyStore;
        set => inProcessSubscriptionIdempotencyStore = InProcessEventingIdempotencyPolicy.NormalizeStore(value);
    }

    /// <summary>
    /// Gets or sets the number of minutes that successful direct in-process subscription
    /// executions remain eligible for duplicate suppression.
    /// </summary>
    /// <remarks>
    /// The default value is <c>60</c> minutes. The value is used only when
    /// <see cref="EnableInProcessSubscriptionIdempotency" /> is enabled.
    /// </remarks>
    public int InProcessSubscriptionIdempotencyRetentionMinutes
    {
        get => inProcessSubscriptionIdempotencyRetentionMinutes;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "In-process subscription idempotency retention must be greater than or equal to 1 minute.");
            }

            inProcessSubscriptionIdempotencyRetentionMinutes = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the in-process publisher should continue executing
    /// later subscriptions on the same channel after one subscription fails.
    /// </summary>
    /// <remarks>
    /// The publisher still reports failed subscriptions and throws after the publication attempt
    /// finishes. This setting only controls whether independent subscriptions on the same channel
    /// get a chance to run before the failure is returned to the caller.
    /// </remarks>
    public bool ContinueInProcessSubscriptionExecutionAfterFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether publication requests can be delayed by the native eventing pack.
    /// </summary>
    /// <remarks>
    /// Delayed publications are held in the current process until their due time and then handed to the active
    /// publisher. This is a lightweight Wolverine-free scheduling baseline, not a durable or distributed scheduler.
    /// </remarks>
    public bool EnablePublicationScheduling { get; set; }

    /// <summary>
    /// Gets or sets the maximum delay, in milliseconds, accepted by the process-local publication scheduler.
    /// </summary>
    /// <remarks>
    /// The default value is <c>86,400,000</c> milliseconds, or twenty-four hours.
    /// </remarks>
    public int PublicationSchedulingMaxDelayMilliseconds
    {
        get => publicationSchedulingMaxDelayMilliseconds;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Publication scheduling max delay must be greater than or equal to 1 millisecond.");
            }

            publicationSchedulingMaxDelayMilliseconds = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of delayed publications retained by the process-local scheduler.
    /// </summary>
    public int PublicationSchedulingMaxPendingCount
    {
        get => publicationSchedulingMaxPendingCount;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Publication scheduling max pending count must be greater than or equal to 1.");
            }

            publicationSchedulingMaxPendingCount = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether publication requests can resolve an effective
    /// channel from the configured event-type routing table before the active publisher runs.
    /// </summary>
    /// <remarks>
    /// The routing table is provider-neutral and runs inside the Cephalon eventing dispatcher.
    /// It can route requests that use <see cref="PublicationRoutingAutoChannelId" /> as the
    /// requested channel, and it can validate explicit channels against declared event-type
    /// ownership. Broker topology, queues, exchanges, topics, and subscriptions still belong to
    /// the selected transport or companion package.
    /// </remarks>
    public bool EnablePublicationRouting { get; set; }

    /// <summary>
    /// Gets or sets the requested channel identifier that tells the dispatcher to resolve the
    /// effective channel from <see cref="PublicationRoutes" />.
    /// </summary>
    /// <remarks>
    /// The default value is <c>auto</c>. Hosts can keep application code stable by allowing
    /// callers to publish with this logical channel while routing remains configuration-owned.
    /// </remarks>
    public string PublicationRoutingAutoChannelId
    {
        get => publicationRoutingAutoChannelId;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Publication routing auto channel id must not be blank.",
                    nameof(value));
            }

            publicationRoutingAutoChannelId = value.Trim();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether every routed publication must match a configured event-type route.
    /// </summary>
    /// <remarks>
    /// This guard is useful when a host wants central routing governance for all events. Requests
    /// that use the auto channel always require a match because the dispatcher has no effective
    /// channel without one.
    /// </remarks>
    public bool PublicationRoutingRequireMatchedRoute { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an explicitly requested channel that disagrees
    /// with a matched event-type route should be rejected before publishing.
    /// </summary>
    public bool PublicationRoutingRejectMismatchedExplicitChannel { get; set; }

    /// <summary>
    /// Gets the provider-neutral event-type route table used by the Cephalon dispatcher.
    /// </summary>
    /// <remarks>
    /// Keys are event-type patterns. Exact keys match first; keys ending with <c>*</c> match by
    /// case-insensitive prefix. Values are effective event channel identifiers.
    /// </remarks>
    public IDictionary<string, string> PublicationRoutes { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the maximum number of event-dispatch remediation command results retained in memory for operator reads.
    /// </summary>
    /// <remarks>
    /// The default value is <c>256</c>. Set the value to <c>0</c> to disable the process-local remediation command
    /// history while keeping the command dispatcher itself available. The catalog is an operator-audit read model,
    /// not a durable compliance store; hosts that need long-term retention should also persist command results.
    /// </remarks>
    public int RemediationCommandHistoryLimit
    {
        get => remediationCommandHistoryLimit;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Remediation command history limit must be greater than or equal to 0.");
            }

            remediationCommandHistoryLimit = value;
        }
    }
}
