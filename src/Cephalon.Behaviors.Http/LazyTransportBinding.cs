using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;

namespace Cephalon.Behaviors.Http;

/// <summary>
/// Deferred-initialization wrapper around an <see cref="IHttpBehaviorBinding" />.
/// Ensures that <see cref="IHttpBehaviorBinding.MapAsync" /> is called exactly once,
/// on first request, using a <see cref="SemaphoreSlim" /> to guard concurrent callers.
/// This keeps pod startup under 100 ms regardless of transport count.
/// </summary>
public sealed class LazyTransportBinding : IDisposable
{
    private volatile bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Ensures the binding has been mapped. If not yet mapped, calls
    /// <see cref="IHttpBehaviorBinding.MapAsync" /> under a semaphore, then marks the
    /// instance as initialized so subsequent calls return immediately.
    /// On initialization failure the flag is NOT set, allowing a retry on the next call.
    /// </summary>
    /// <param name="app">The web application to register routes on.</param>
    /// <param name="descriptor">The behavior topology descriptor.</param>
    /// <param name="dispatcher">The behavior dispatcher.</param>
    /// <param name="binding">The concrete transport binding to initialize.</param>
    /// <param name="ct">Cancellation token threaded from the caller.</param>
    /// <returns>A task that completes when the binding is ready.</returns>
    public async Task EnsureMappedAsync(
        WebApplication app,
        BehaviorTopologyDescriptor descriptor,
        BehaviorDispatcher dispatcher,
        IHttpBehaviorBinding binding,
        CancellationToken ct = default)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_initialized) return;
            await binding.MapAsync(app, descriptor, dispatcher).ConfigureAwait(false);
            _initialized = true; // set AFTER success only — failure leaves flag false for retry
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _initLock.Dispose();
}
