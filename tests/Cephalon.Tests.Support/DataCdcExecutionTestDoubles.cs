using Cephalon.Abstractions.Data;

namespace Cephalon.Tests.Support;

internal sealed class TestCdcExecutionState
{
    private readonly Lock gate = new();
    private readonly Queue<CdcCaptureExecutionResult> results = new();
    private readonly List<OutboxMessage> stagedMessages = [];
    private readonly List<CdcCaptureExecutionAcknowledgement> acknowledgements = [];
    private readonly TaskCompletionSource<bool> captureInvocationTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> enqueueAttemptTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> acknowledgementAttemptTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> stagedMessageTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> acknowledgementTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int captureInvocationCount;
    private int enqueueAttemptCount;
    private int acknowledgementAttemptCount;

    public bool ThrowOnEnqueue { get; set; }

    public string EnqueueFailureMessage { get; set; } = "Simulated outbox staging failure.";

    public bool ThrowOnAcknowledge { get; set; }

    public string AcknowledgementFailureMessage { get; set; } = "Simulated CDC acknowledgement failure.";

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

    public IReadOnlyList<CdcCaptureExecutionAcknowledgement> Acknowledgements
    {
        get
        {
            lock (gate)
            {
                return acknowledgements.ToArray();
            }
        }
    }

    public int AcknowledgementAttemptCount
    {
        get
        {
            lock (gate)
            {
                return acknowledgementAttemptCount;
            }
        }
    }

    public Task WaitForCaptureInvocationAsync(CancellationToken cancellationToken = default)
    {
        return captureInvocationTcs.Task.WaitAsync(cancellationToken);
    }

    public Task WaitForEnqueueAttemptAsync(CancellationToken cancellationToken = default)
    {
        return enqueueAttemptTcs.Task.WaitAsync(cancellationToken);
    }

    public Task WaitForStagedMessageAsync(CancellationToken cancellationToken = default)
    {
        return stagedMessageTcs.Task.WaitAsync(cancellationToken);
    }

    public Task WaitForAcknowledgementAttemptAsync(CancellationToken cancellationToken = default)
    {
        return acknowledgementAttemptTcs.Task.WaitAsync(cancellationToken);
    }

    public Task WaitForAcknowledgementAsync(CancellationToken cancellationToken = default)
    {
        return acknowledgementTcs.Task.WaitAsync(cancellationToken);
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

    public void RecordCaptureInvocation()
    {
        lock (gate)
        {
            captureInvocationCount++;
        }

        captureInvocationTcs.TrySetResult(true);
    }

    public void RecordEnqueueAttempt()
    {
        lock (gate)
        {
            enqueueAttemptCount++;
        }

        enqueueAttemptTcs.TrySetResult(true);
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

    public void RecordAcknowledgementAttempt()
    {
        lock (gate)
        {
            acknowledgementAttemptCount++;
        }

        acknowledgementAttemptTcs.TrySetResult(true);
    }

    public void RecordAcknowledgement(CdcCaptureExecutionAcknowledgement acknowledgement)
    {
        ArgumentNullException.ThrowIfNull(acknowledgement);

        lock (gate)
        {
            acknowledgements.Add(acknowledgement);
        }

        acknowledgementTcs.TrySetResult(true);
    }
}

internal sealed class TestCdcCapture(TestCdcExecutionState state) : ICdcCapture
{
    public string CdcCaptureId => "tenant-profile-cdc";

    public ValueTask<CdcCaptureExecutionResult> CaptureAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        state.RecordCaptureInvocation();
        return ValueTask.FromResult(state.DequeueResult());
    }
}

internal sealed class TestAcknowledgingCdcCapture(TestCdcExecutionState state) : ICdcCapture, ICdcCaptureAcknowledger
{
    public string CdcCaptureId => "tenant-profile-cdc";

    public ValueTask<CdcCaptureExecutionResult> CaptureAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        state.RecordCaptureInvocation();
        return ValueTask.FromResult(state.DequeueResult());
    }

    public ValueTask AcknowledgeAsync(
        CdcCaptureExecutionAcknowledgement acknowledgement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(acknowledgement);
        cancellationToken.ThrowIfCancellationRequested();
        state.RecordAcknowledgementAttempt();

        if (state.ThrowOnAcknowledge)
        {
            throw new InvalidOperationException(state.AcknowledgementFailureMessage);
        }

        state.RecordAcknowledgement(acknowledgement);
        return ValueTask.CompletedTask;
    }
}

internal sealed class TestOutbox(TestCdcExecutionState state) : IOutbox
{
    public string OutboxId => "tenant-event-outbox";

    public ValueTask EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        state.RecordEnqueueAttempt();

        if (state.ThrowOnEnqueue)
        {
            throw new InvalidOperationException(state.EnqueueFailureMessage);
        }

        state.RecordStagedMessage(message);
        return ValueTask.CompletedTask;
    }
}
