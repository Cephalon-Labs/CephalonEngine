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
      "MaxRestartAttempts": 3,
      "StartupReadinessDelay": "00:00:10",
      "ShutdownLivenessGracePeriod": "00:00:20",
      "ManualRestartBackoff": "00:00:30"
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

## Startup readiness warmup

- `StartupReadinessDelay`
  - optional
  - keeps `/health/ready` and the readiness report in `/engine/diagnostics` `Unhealthy` for a bounded warmup window after startup succeeds
  - does not change liveness; the process is still considered live while warmup completes
  - surfaces as `activeWindow = "startup-warmup"` with an end timestamp in health payloads

## Stop behavior

- `FailFast`
  - first stop failure is rethrown
- `BestEffortContinue`
  - default
  - Cephalon keeps stopping the remaining started modules
  - the runtime still records failure context and ends in `Failed`

## Shutdown liveness drain

- `ShutdownLivenessGracePeriod`
  - optional
  - keeps `/health/live` `Healthy` while shutdown is in progress and the configured drain window has not expired
  - once the drain window expires, liveness becomes `Unhealthy` until shutdown completes so hosts and orchestrators can see a stuck drain
  - surfaces as `activeWindow = "shutdown-drain"` with the drain deadline in health payloads

## Restart expectations

- manual restart is controlled by `AllowManualRestart`
- `MaxRestartAttempts` limits explicit `RestartAsync(...)` calls
- `ManualRestartBackoff` delays explicit `RestartAsync(...)` calls after restartable `start` failures
- restart is intentionally conservative:
  - a failed `start` phase can be restarted
  - a failed `initialize` phase cannot be restarted safely and the runtime should be rebuilt
  - a failed `stop` phase must be resolved before retrying restart
- when backoff is configured, `RestartAsync(...)` throws until the backoff window expires
- restart backoff surfaces through `/engine/status`, `/engine/runtime-story`, `/health/live`, `/health/ready`, and `/engine/diagnostics`

## Runtime diagnostics

`/engine/status` now includes:

- current runtime status
- lifecycle timestamps
- shutdown drain timing when a stop is in progress
- restart count
- last failure context:
  - phase
  - module id/version when available
  - exception type and message
  - whether restart is currently allowed
  - when restart exits backoff, if a manual restart cooldown is active

`/engine/failure-policy` exposes the effective policy the runtime was built with.
