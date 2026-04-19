using Cephalon.Abstractions.Data;

namespace Cephalon.Tests.Support;

internal sealed class TestCdcExecutionState
{
    private readonly Lock gate = new();
    private readonly Queue<CdcCaptureExecutionResult> results = new();
    private readonly List<OutboxMessage> stagedMessages = [];
    private readonly TaskCompletionSource<bool> stagedMessageTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IReadOnlyList<OutboxMessage> StagedMessages
    {
        get
        {
            lock (gate)
            {
                return stagedMessages.ToArray();
            }
        }
    }

    public Task WaitForStagedMessageAsync(CancellationToken cancellationToken = default)
    {
        return stagedMessageTcs.Task.WaitAsync(cancellationToken);
    }

    public void EnqueueResult(CdcCaptureExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        lock (gate)
        {
            results.Enqueue(result);
        }
    }

    public CdcCaptureExecutionResult DequeueResult()
    {
        lock (gate)
        {
            return results.Count > 0
                ? results.Dequeue()
                : new CdcCaptureExecutionResult();
        }
    }

    public void RecordStagedMessage(OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        lock (gate)
        {
            stagedMessages.Add(message);
        }

        stagedMessageTcs.TrySetResult(true);
    }
}

internal sealed class TestCdcCapture(TestCdcExecutionState state) : ICdcCapture
{
    public string CdcCaptureId => "tenant-profile-cdc";

    public ValueTask<CdcCaptureExecutionResult> CaptureAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(state.DequeueResult());
    }
}

internal sealed class TestOutbox(TestCdcExecutionState state) : IOutbox
{
    public string OutboxId => "tenant-event-outbox";

    public ValueTask EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        state.RecordStagedMessage(message);
        return ValueTask.CompletedTask;
    }
}
