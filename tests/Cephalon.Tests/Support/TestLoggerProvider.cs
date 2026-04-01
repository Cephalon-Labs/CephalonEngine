using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cephalon.Tests.Support;

internal sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> entries = new();

    public IReadOnlyList<LogEntry> Entries => entries.ToArray();

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(categoryName, entries);
    }

    public void Dispose()
    {
    }

    private sealed class TestLogger : ILogger
    {
        private readonly string categoryName;
        private readonly ConcurrentQueue<LogEntry> entries;

        public TestLogger(string categoryName, ConcurrentQueue<LogEntry> entries)
        {
            this.categoryName = categoryName;
            this.entries = entries;
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NoopScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(new LogEntry(
                categoryName,
                logLevel,
                eventId,
                formatter(state, exception)));
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static NoopScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}

internal sealed record LogEntry(
    string Category,
    LogLevel Level,
    EventId EventId,
    string Message);
