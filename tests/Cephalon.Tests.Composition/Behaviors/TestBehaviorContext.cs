using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.Tests.Behaviors;

/// <summary>
/// A test double for <see cref="IBehaviorContext" /> that records reply messages
/// and throws <see cref="NotSupportedException" /> when the behavior uses the <c>direct</c> pattern.
/// </summary>
internal sealed class TestBehaviorContext : IBehaviorContext
{
    private readonly bool _isDirect;
    private readonly List<object> _replies = [];
    private readonly IEventStore? _eventStore;

    internal TestBehaviorContext(
        string behaviorId,
        bool isDirect = false,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? correlationId = null,
        IEventStore? eventStore = null)
    {
        BehaviorId = behaviorId;
        _isDirect = isDirect;
        Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        CorrelationId = correlationId;
        _eventStore = eventStore;
    }

    public string BehaviorId { get; }

    public string? CorrelationId { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    public IEventStore? EventStore => _eventStore;

    public IReadOnlyList<object> Replies => _replies.AsReadOnly();

    public Task ReplyAsync(object reply, CancellationToken cancellationToken = default)
    {
        if (_isDirect)
        {
            throw new NotSupportedException(
                $"ReplyAsync is not supported in the 'direct' interaction pattern. " +
                $"Use the behavior return value to communicate results.");
        }

        _replies.Add(reply);
        return Task.CompletedTask;
    }
}
