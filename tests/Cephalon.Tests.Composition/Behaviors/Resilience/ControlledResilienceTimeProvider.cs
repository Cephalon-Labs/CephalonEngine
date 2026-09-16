namespace Cephalon.Tests.Behaviors;

// Only one-shot timers are needed by the Polly timeout fixtures. Advancing time never
// waits for a thread-pool timer, so runner load cannot race a simulated successful effect.
internal sealed class ControlledResilienceTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private readonly List<ControlledTimer> _timers = [];
    private TimeSpan _elapsed;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp()
    {
        lock (_gate) { return _elapsed.Ticks; }
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate) { return DateTimeOffset.UnixEpoch + _elapsed; }
    }

    public int ActiveTimerCount
    {
        get { lock (_gate) { return _timers.Count(timer => timer.DueAt.HasValue); } }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ControlledTimer(this, callback, state);
        lock (_gate)
        {
            _timers.Add(timer);
            timer.Change(dueTime, period);
        }

        return timer;
    }

    public void Advance(TimeSpan elapsed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);
        ControlledTimer[] due;
        lock (_gate)
        {
            _elapsed += elapsed;
            due = _timers.Where(timer => timer.DueAt <= _elapsed).ToArray();
            foreach (var timer in due) { timer.DueAt = null; }
        }

        // Cancellation callbacks can dispose/change timers; invoke outside the clock lock.
        foreach (var timer in due) { timer.Callback(timer.State); }
    }

    private sealed class ControlledTimer(
        ControlledResilienceTimeProvider clock, TimerCallback callback, object? state) : ITimer
    {
        private bool _disposed;
        public TimerCallback Callback { get; } = callback;
        public object? State { get; } = state;
        public TimeSpan? DueAt { get; set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            if (period != Timeout.InfiniteTimeSpan)
            {
                throw new NotSupportedException("The resilience fixture supports one-shot timers only.");
            }

            lock (clock._gate)
            {
                if (_disposed) { return false; }
                DueAt = dueTime == Timeout.InfiniteTimeSpan ? null : clock._elapsed + dueTime;
                return true;
            }
        }

        public void Dispose()
        {
            lock (clock._gate)
            {
                _disposed = true;
                DueAt = null;
                clock._timers.Remove(this);
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
