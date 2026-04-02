using Microsoft.Extensions.Logging;

namespace Cephalon.Benchmarks.Support;

internal sealed class BenchmarkLoggerProvider(LogLevel minimumLevel) : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider scopeProvider = new LoggerExternalScopeProvider();

    public ILogger CreateLogger(string categoryName)
    {
        return new BenchmarkLogger(minimumLevel, () => scopeProvider);
    }

    public void Dispose()
    {
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        this.scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
    }

    private sealed class BenchmarkLogger(
        LogLevel minimumLevel,
        Func<IExternalScopeProvider> scopeProviderAccessor) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return scopeProviderAccessor().Push(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= minimumLevel;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            _ = formatter(state, exception);
            scopeProviderAccessor().ForEachScope(static (scope, stateObject) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> properties)
                {
                    foreach (var property in properties)
                    {
                        GC.KeepAlive(property);
                    }
                }

                GC.KeepAlive(stateObject);
            }, state);
        }
    }
}
