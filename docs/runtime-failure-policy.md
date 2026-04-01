# Cephalon Runtime Failure Policy

Cephalon now treats runtime failure behavior as a first-class engine contract.

## Configuration

```json
{
  "Engine": {
    "FailurePolicy": {
      "StartupFailureBehavior": "FailFast",
      "StopFailureBehavior": "BestEffortContinue",
      "AllowManualRestart": true,
      "MaxRestartAttempts": 3
    }
  }
}
```

## Startup behavior

- `FailFast`
  - default
  - startup exceptions are rethrown to the host
  - the runtime still records failure context before the exception leaves the lifecycle call
- `CaptureOnly`
  - startup exceptions are captured into runtime state
  - the runtime moves to `Failed`
  - hosts can stay alive and inspect `/engine/status`

## Stop behavior

- `FailFast`
  - first stop failure is rethrown
- `BestEffortContinue`
  - default
  - Cephalon keeps stopping the remaining started modules
  - the runtime still records failure context and ends in `Failed`

## Restart expectations

- manual restart is controlled by `AllowManualRestart`
- `MaxRestartAttempts` limits explicit `RestartAsync(...)` calls
- restart is intentionally conservative:
  - a failed `start` phase can be restarted
  - a failed `initialize` phase cannot be restarted safely and the runtime should be rebuilt
  - a failed `stop` phase must be resolved before retrying restart

## Runtime diagnostics

`/engine/status` now includes:

- current runtime status
- lifecycle timestamps
- restart count
- last failure context:
  - phase
  - module id/version when available
  - exception type and message
  - whether restart is currently allowed

`/engine/failure-policy` exposes the effective policy the runtime was built with.
