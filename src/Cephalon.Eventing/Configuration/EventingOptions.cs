using Cephalon.Eventing.Services;

namespace Cephalon.Eventing.Configuration;

/// <summary>
/// Configures the built-in eventing runtime pack.
/// </summary>
/// <remarks>
/// These options seed the host-owned part of the eventing runtime. Installed modules can still
/// contribute additional channels through <see cref="Services.IEventChannelContributor" /> and
/// additional subscription descriptors through <see cref="Services.IEventSubscriptionContributor" />.
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
