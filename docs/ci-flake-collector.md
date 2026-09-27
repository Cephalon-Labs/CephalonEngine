# CI flake evidence collector

`scripts/measure-ci-flake-rate.ps1` publishes descriptive GitHub Actions evidence for the pending `engine.tests.flake-rate.7d` SLI. Schema **2.0.0** prevents a low observed recovery rate from automatically authorizing a stable baseline. ENG-751 owns this collector correction; ENG-752 retains workload, recovery, telemetry and statistical SLO evidence.

## Collection and interpretation

- The default creation window is the seven days ending at invocation time. `-WindowEndUtc` pins that boundary for reproducible fixtures. Outcomes are read at collection time: this is not a reconstruction of historical run states. All branches and events for the configured workflow names are included.
- Run, workflow-definition and attempt-job lists are paginated before workflow filtering. Counts and unique IDs must agree. Missing pages, changed totals, duplicate/missing IDs, truncated fixtures, and missing/empty attempt jobs block complete assessment. Filtered run searches reaching GitHub's 1,000-result limit fail closed; definitions/jobs have a 10,000-item safety bound. Narrow the requested window or provide an independently collected complete fixture when a limit is reached.
- UTC query dates use the invariant Gregorian calendar, including on Thai-locale machines. Normalization preserves the instant when PowerShell materializes JSON dates as `DateTime`, and compares UTC instants without a local-time cast. Fixture runs outside the creation window are excluded and counted.
- `CompletedRunCount` remains the count of all completed outcomes. **`EligibleRunCount` is the denominator:** only `success` and `failure`. Cancelled, skipped, timed-out and other terminal outcomes are reported separately and block a clean assessment. Unfinished runs are visible and also block a clean assessment.
- `FlakeEventCount` counts at most one heuristic recovery per successful run: an earlier attempt of that same run contains a failed step matching `TestFailurePattern`. Multiple failed attempts do not create multiple events. Separate successful runs on the same SHA do not resolve another run's failure or establish an equivalent test execution.
- The default pattern matches explicit `Pester`, `dotnet test`, or `Run tests` step names. A failed job name, generic `Run release validation`, or provider/Testcontainers label does not identify a test failure. Nonmatching inspected attempts remain `unattributed-attempts`; custom patterns are recorded and remain heuristic evidence.
- `FlakeRatePercent = 100 * FlakeEventCount / EligibleRunCount`; it is **null** without eligible runs. An observed zero can coexist with unresolved failures, unknown attribution or incomplete collection. Read `ObservationComplete`, `AssessmentBlockers`, attempt receipts and per-workflow coverage alongside the rate.

GitHub documents the [filtered workflow-run search limit and pagination](https://docs.github.com/en/rest/actions/workflow-runs#list-workflow-runs-for-a-repository) and [attempt-specific job pagination](https://docs.github.com/en/rest/actions/workflow-jobs#list-jobs-for-a-workflow-run-attempt). The collector bounds API reads and retains partial evidence when subsequent pages become unavailable. It does not repair remote history or infer omitted jobs.

## Promotion boundary and schema migration

`PromotionAllowed` is always **false** in schema 2.0.0. `PromotionBlockers` always includes `statistical-and-test-level-review-required`. `-RequirePromotion` writes the report and then fails, including for a clean observed window. `-AllowUnavailable` permits an unavailable report; it never waives promotion requirements. The script does not edit `sre-stable-baselines.json`.

`ReadyForStatisticalReview=true` only means collection, attribution, Actions/workflow readiness and minimum eligible history are adequate for a separate review, with no unresolved failures or excluded/unfinished outcomes and an observed rate within target. The default five-run floor applies to each configured workflow as well as the total; it is a descriptive floor, **not** a confidence calculation. `workflow_dispatch` readiness remains diagnostic, since dispatch support is not necessary to measure workflow runs.

The intended test-suite SLI still needs equivalent test identity/outcomes, representative workload selection, repeatability, treatment of infrastructure failures and an explicit statistical method. ENG-752 owns that evidence. Neither five clean runs nor repeated runs from correlated environments demonstrate the 0.5% target.

Consumers of schema 1.1.0 must migrate:

| Field/behavior | Schema 2.0.0 |
| --- | --- |
| `stable-baseline-candidate` | Replaced by descriptive `measured-within-target`, `measured-with-blockers`, or `measured-over-target`; pending statuses remain for missing/incomplete history. |
| `FlakeRatePercent` | Uses eligible success/failure runs; nullable when denominator is zero. |
| `FlakeEvents` | Same-run attempt recovery only; no cross-run same-SHA inference. |
| `ObservedRuns` | Includes unfinished in-window runs; excluded terminal outcomes remain inspectable. |
| `PromotionAllowed` / `-RequirePromotion` | Automatic promotion disabled; independent test-level/statistical review required. |
| Coverage | `RunCollectionComplete`, `ObservationComplete`, `AttemptInspections`, `WorkflowCoverage` and explicit blocker lists. |

## Usage

```powershell
./scripts/measure-ci-flake-rate.ps1 -AllowUnavailable
./scripts/measure-ci-flake-rate.ps1 -WorkflowRunsJsonPath runs.json `
  -ActionsPermissionsJsonPath permissions.json -WorkflowsJsonPath workflows.json `
  -AttemptJobsDirectory attempts -WindowEndUtc '2026-09-27T12:00:00Z' `
  -OutputPath artifacts/sre-ci-flake-rate/replay.json
```

Fixtures use GitHub's `total_count` and collection shape with unique `id` values. Attempt files are named `<run-id>-attempt-<number>.json`; each job carries `steps` with names and conclusions. Fixture data must include the full collection it declares. Missing permissions/definition fixtures remain unverified, and attempt-job fixtures should be supplied to avoid live attempt queries.

## September 27 verification checkpoint

ENG-751 implementation is under validation. Local fixture tests cover pagination, partial/unavailable/duplicate data, search limits, cancellation denominators, unresolved failures, missing attribution, one-event-per-run behavior, creation-window boundaries, Thai locale dates, timezone preservation and the mandatory promotion rejection. Collector, planning, scorecard and release-script tests pass **79/79**; hand-authored/top-level Markdown link checks pass **2/2**. GitHub guards match **24** open issues/Project items and their five required fields. Committed-source CI acceptance is still pending at this checkpoint.

Live readback on **2026-09-27** found **8 completed runs: 3 success, 3 failure, 2 cancelled**. The eligible denominator is **6**; three failure attempts were retrieved but their broad failed steps could not establish test attribution. Workflow eligible counts were Release Validation **5**, Provider Live Testcontainers **1**, Publish Release **0**. The observed recovery rate was **0%**, with explicit blockers and `PromotionAllowed=false`. This is a collection snapshot, not an accepted SLO baseline. The local report, including its exact UTC collection/window timestamps, is retained under `artifacts/ci-flake-collector-2026-09-27/live/ci-flake-rate.json`.

Existing May baseline measurements and September source-specific release evidence retain their original meaning. No package maturity, shipping SDK/net10.0 baseline, runtime API or provider-support claim changes in this task.
