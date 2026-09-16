using Cephalon.Tests.Support;

namespace Cephalon.Tests.Composition;

public sealed class MongoDbBootstrapTests
{
    [Fact]
    public async Task BootstrapDeadlinePreservesDiagnosticFailureAndCancellationCause()
    {
        OperationCanceledException? cause = null;
        var diagnostic = new InvalidOperationException("MongoDB process output: fixture-startup-evidence");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            MongoDbReplicaSetRunner.RunBootstrapPhaseAsync(
                token => Task.Delay(Timeout.InfiniteTimeSpan, token),
                TimeSpan.FromMilliseconds(1),
                cancellation => { cause = cancellation; return diagnostic; }, CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(10)));

        Assert.Same(diagnostic, exception);
        Assert.NotNull(cause);
    }

    [Fact]
    public async Task CallerCancellationIsNotReportedAsBootstrapTimeout()
    {
        using var caller = new CancellationTokenSource();
        var diagnosticsCreated = false;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            MongoDbReplicaSetRunner.RunBootstrapPhaseAsync(token =>
            {
                caller.Cancel();
                return Task.Delay(Timeout.InfiniteTimeSpan, token);
            }, TimeSpan.FromSeconds(20), cancellation =>
            {
                diagnosticsCreated = true;
                return new InvalidOperationException("unexpected", cancellation);
            }, caller.Token));

        Assert.False(diagnosticsCreated);
    }

    [Fact]
    public async Task PreCanceledBootstrapDoesNotStartItsOperation()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var started = false;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            MongoDbReplicaSetRunner.RunBootstrapPhaseAsync(_ =>
            {
                started = true;
                return Task.CompletedTask;
            }, TimeSpan.FromSeconds(20), cancellation =>
                new InvalidOperationException("unexpected", cancellation), caller.Token));
        Assert.False(started);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => MongoDbReplicaSetRunner.StartAsync(caller.Token));
    }

    [Fact]
    public async Task SuccessfulBootstrapDoesNotCreateTimeoutDiagnostics()
    {
        var started = false;
        await MongoDbReplicaSetRunner.RunBootstrapPhaseAsync(_ =>
        {
            started = true;
            return Task.CompletedTask;
        }, TimeSpan.FromSeconds(20),
            _ => throw new InvalidOperationException("Diagnostics must only be created on deadline expiry."), CancellationToken.None);
        Assert.True(started);
    }

    [Fact]
    public async Task BootstrapPreservesNonDeadlineFailure()
    {
        var failure = new InvalidOperationException("process exited");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            MongoDbReplicaSetRunner.RunBootstrapPhaseAsync(_ => Task.FromException(failure),
                TimeSpan.FromSeconds(20),
                _ => throw new InvalidOperationException("unexpected"), CancellationToken.None));
        Assert.Same(failure, exception);
    }
}
