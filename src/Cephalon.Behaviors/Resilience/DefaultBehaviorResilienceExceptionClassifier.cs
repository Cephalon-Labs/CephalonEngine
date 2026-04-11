using System.Data.Common;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Resilience;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;

namespace Cephalon.Behaviors.Resilience;

internal sealed class DefaultBehaviorResilienceExceptionClassifier : IBehaviorResilienceExceptionClassifier
{
    public BehaviorResilienceExceptionHandling Classify(BehaviorResilienceExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Exception switch
        {
            BrokenCircuitException => BehaviorResilienceExceptionHandling.Ignore,
            OperationCanceledException => BehaviorResilienceExceptionHandling.Ignore,
            RateLimiterRejectedException => BehaviorResilienceExceptionHandling.Ignore,
            KeyNotFoundException => BehaviorResilienceExceptionHandling.Ignore,
            ArgumentException => BehaviorResilienceExceptionHandling.Ignore,
            BehaviorSecurityException => BehaviorResilienceExceptionHandling.Ignore,
            NotSupportedException => BehaviorResilienceExceptionHandling.Ignore,
            NotImplementedException => BehaviorResilienceExceptionHandling.Ignore,
            TimeoutRejectedException => ResolveTransientHandling(context),
            TimeoutException => ResolveTransientHandling(context),
            HttpRequestException => ResolveTransientHandling(context),
            IOException => ResolveTransientHandling(context),
            SocketException => ResolveTransientHandling(context),
            DbException => ResolveTransientHandling(context),
            _ => BehaviorResilienceExceptionHandling.Ignore
        };
    }

    private static BehaviorResilienceExceptionHandling ResolveTransientHandling(
        BehaviorResilienceExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.BehaviorIdempotency == BehaviorIdempotencyMode.Idempotent
            ? BehaviorResilienceExceptionHandling.RetryAndTrip
            : BehaviorResilienceExceptionHandling.TripOnly;
    }
}
