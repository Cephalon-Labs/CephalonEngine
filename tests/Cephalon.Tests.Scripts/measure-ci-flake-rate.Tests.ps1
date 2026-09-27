#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
.SYNOPSIS
    Pester tests for scripts/measure-ci-flake-rate.ps1.
.DESCRIPTION
    Verifies the SRE flake-rate evidence collector without requiring live
    GitHub Actions metadata.
#>

BeforeAll {
    $env:CEPHALON_CI_FLAKE_RATE_NO_RUN = "1"
    $script:repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $script:scriptPath = Join-Path $script:repoRoot "scripts\measure-ci-flake-rate.ps1"
    . $script:scriptPath
}

AfterAll {
    Remove-Item Env:\CEPHALON_CI_FLAKE_RATE_NO_RUN -ErrorAction SilentlyContinue
}

function script:New-WorkflowRunFixture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [array]$Runs
    )

    @{
        total_count = $Runs.Count
        workflow_runs = $Runs
    } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function script:New-ActionsPermissionsFixture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [bool]$Enabled = $true,
        [string]$AllowedActions = "all"
    )

    @{
        enabled = $Enabled
        allowed_actions = $AllowedActions
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function script:New-WorkflowDefinitionsFixture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [array]$Workflows
    )

    @{
        total_count = $Workflows.Count
        workflows = $Workflows
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function script:New-AttemptJobsFixture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Directory,
        [Parameter(Mandatory = $true)]
        [string]$RunId,
        [Parameter(Mandatory = $true)]
        [int]$Attempt,
        [Parameter(Mandatory = $true)]
        [string]$Conclusion,
        [Parameter(Mandatory = $true)]
        [string]$StepName
    )

    @{
        total_count = 1
        jobs = @(
            @{
                id = 1
                name = "Release Validation (windows-latest)"
                conclusion = $Conclusion
                steps = @(
                    @{
                        name = $StepName
                        conclusion = $Conclusion
                    }
                )
            }
        )
    } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $Directory "$RunId-attempt-$Attempt.json") -Encoding UTF8
}

function script:New-TestRun {
    param([int]$Id, [string]$Conclusion = 'success', [int]$Attempt = 1,
        [string]$CreatedAt = '2026-05-08T12:00:00Z')
    return @{ id = $Id; name = 'Release Validation'; status = 'completed'; conclusion = $Conclusion
        head_sha = 'same-sha'; run_attempt = $Attempt; created_at = $CreatedAt; html_url = "https://example.test/runs/$Id" }
}

function script:Invoke-FixtureMeasurement {
    Invoke-CiFlakeRateMeasurement -WorkflowRunsJsonPath $script:runsPath -AttemptJobsDirectory $script:attemptsPath `
        -WorkflowName 'Release Validation' -MinimumCompletedRunCount 1 -OutputPath $script:outputPath `
        -WindowEndUtc '2026-05-09T00:00:00Z'
}

Describe "measure-ci-flake-rate.ps1" {
    BeforeEach {
        $PSDefaultParameterValues['Invoke-CiFlakeRateMeasurement:WindowEndUtc'] = [datetimeoffset]'2026-05-09T00:00:00Z'
        $script:tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "cephalon-ci-flake-rate-$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $script:tempRoot -Force | Out-Null
        $script:runsPath = Join-Path $script:tempRoot "workflow-runs.json"
        $script:actionsPermissionsPath = Join-Path $script:tempRoot "actions-permissions.json"
        $script:workflowsPath = Join-Path $script:tempRoot "workflows.json"
        $script:attemptsPath = Join-Path $script:tempRoot "attempts"
        $script:outputPath = Join-Path $script:tempRoot "out"
        New-Item -ItemType Directory -Path $script:attemptsPath -Force | Out-Null
    }

    AfterEach {
        $PSDefaultParameterValues.Remove('Invoke-CiFlakeRateMeasurement:WindowEndUtc')
        if (Test-Path -LiteralPath $script:tempRoot) {
            Remove-Item -LiteralPath $script:tempRoot -Recurse -Force
        }
    }

    It "writes a pending report when no matching Actions history exists" {
        New-WorkflowRunFixture -Path $script:runsPath -Runs @()

        $result = Invoke-CiFlakeRateMeasurement `
            -WorkflowRunsJsonPath $script:runsPath `
            -AttemptJobsDirectory $script:attemptsPath `
            -OutputPath $script:outputPath `
            -AllowUnavailable

        Test-Path -LiteralPath $result.JsonPath -PathType Leaf | Should -BeTrue
        $result.Report.Status | Should -Be "pending-unavailable-no-actions-history"
        $result.Report.AvailabilityStatus | Should -Be "unavailable-no-actions-history"
        $result.Report.ActionsReadinessStatus | Should -Be "not-evaluated-fixture-mode"
        $result.Report.WorkflowReadinessStatus | Should -Be "not-evaluated-fixture-mode"
        $result.Report.CompletedRunCount | Should -Be 0
        $result.Report.PromotionAllowed | Should -BeFalse
    }

    It "records Actions and workflow readiness separately from missing run history" {
        New-WorkflowRunFixture -Path $script:runsPath -Runs @()
        New-ActionsPermissionsFixture -Path $script:actionsPermissionsPath
        New-WorkflowDefinitionsFixture -Path $script:workflowsPath -Workflows @(
            @{
                id = 1
                name = "Release Validation"
                path = ".github/workflows/release-validation.yml"
                state = "active"
                html_url = "https://example.test/release-validation"
                dispatch_configured = $true
            },
            @{
                id = 2
                name = "Provider Live Testcontainers"
                path = ".github/workflows/provider-live-testcontainers.yml"
                state = "active"
                html_url = "https://example.test/provider-live"
                dispatch_configured = $true
            },
            @{
                id = 3
                name = "Publish Release"
                path = ".github/workflows/publish-release.yml"
                state = "active"
                html_url = "https://example.test/publish-release"
                dispatch_configured = $true
            }
        )

        $result = Invoke-CiFlakeRateMeasurement `
            -WorkflowRunsJsonPath $script:runsPath `
            -ActionsPermissionsJsonPath $script:actionsPermissionsPath `
            -WorkflowsJsonPath $script:workflowsPath `
            -AttemptJobsDirectory $script:attemptsPath `
            -OutputPath $script:outputPath `
            -AllowUnavailable

        $result.Report.Status | Should -Be "pending-unavailable-no-actions-history"
        $result.Report.ActionsReadinessStatus | Should -Be "actions-enabled"
        $result.Report.ActionsEnabled | Should -BeTrue
        $result.Report.AllowedActions | Should -Be "all"
        $result.Report.WorkflowReadinessStatus | Should -Be "active-workflows"
        $result.Report.WorkflowDispatchReadinessStatus | Should -Be "workflow-dispatch-configured"
        $result.Report.MatchingWorkflowCount | Should -Be 3
        $result.Report.ActiveWorkflowCount | Should -Be 3
        $result.Report.DispatchConfiguredWorkflowCount | Should -Be 3
        @($result.Report.MissingWorkflowNames).Count | Should -Be 0
        @($result.Report.InactiveWorkflowNames).Count | Should -Be 0
        @($result.Report.MissingWorkflowDispatchNames).Count | Should -Be 0
        $result.Report.ReadinessBlockerClass | Should -Be "no-completed-actions-history"
        @($result.Report.WorkflowDefinitions).Count | Should -Be 3
        $result.Report.PromotionAllowed | Should -BeFalse
    }

    It "reports workflow dispatch as a readiness blocker when target workflows are not dispatchable" {
        New-WorkflowRunFixture -Path $script:runsPath -Runs @()
        New-ActionsPermissionsFixture -Path $script:actionsPermissionsPath
        New-WorkflowDefinitionsFixture -Path $script:workflowsPath -Workflows @(
            @{
                id = 1
                name = "Release Validation"
                path = ".github/workflows/release-validation.yml"
                state = "active"
                html_url = "https://example.test/release-validation"
                dispatch_configured = $false
            }
        )

        $result = Invoke-CiFlakeRateMeasurement `
            -WorkflowRunsJsonPath $script:runsPath `
            -ActionsPermissionsJsonPath $script:actionsPermissionsPath `
            -WorkflowsJsonPath $script:workflowsPath `
            -WorkflowName @("Release Validation") `
            -AttemptJobsDirectory $script:attemptsPath `
            -OutputPath $script:outputPath `
            -AllowUnavailable

        $result.Report.WorkflowReadinessStatus | Should -Be "active-workflows"
        $result.Report.WorkflowDispatchReadinessStatus | Should -Be "workflow-dispatch-missing"
        $result.Report.DispatchConfiguredWorkflowCount | Should -Be 0
        $result.Report.MissingWorkflowDispatchNames | Should -Contain "Release Validation"
        $result.Report.ReadinessBlockerClass | Should -Be "workflow-dispatch-not-ready"
        $result.Report.PromotionAllowed | Should -BeFalse
    }

    It "accepts a JSON output file path without nesting another report directory" {
        New-WorkflowRunFixture -Path $script:runsPath -Runs @()
        $jsonOutputPath = Join-Path $script:tempRoot "reports\custom-ci-flake-rate.json"

        $result = Invoke-CiFlakeRateMeasurement `
            -WorkflowRunsJsonPath $script:runsPath `
            -AttemptJobsDirectory $script:attemptsPath `
            -OutputPath $jsonOutputPath `
            -AllowUnavailable

        $result.JsonPath | Should -Be ([System.IO.Path]::GetFullPath($jsonOutputPath))
        Test-Path -LiteralPath $jsonOutputPath -PathType Leaf | Should -BeTrue
        Test-Path -LiteralPath (Join-Path $jsonOutputPath "ci-flake-rate.json") | Should -BeFalse
    }

    It "detects a successful rerun after a failed test attempt" {
        New-WorkflowRunFixture -Path $script:runsPath -Runs @(
            @{
                id = 101
                name = "Release Validation"
                status = "completed"
                conclusion = "success"
                head_sha = "abc123"
                run_attempt = 2
                created_at = "2026-05-08T10:00:00Z"
                html_url = "https://example.test/runs/101"
            },
            @{
                id = 102
                name = "Release Validation"
                status = "completed"
                conclusion = "success"
                head_sha = "def456"
                run_attempt = 1
                created_at = "2026-05-08T11:00:00Z"
                html_url = "https://example.test/runs/102"
            }
        )
        New-AttemptJobsFixture -Directory $script:attemptsPath -RunId "101" -Attempt 1 -Conclusion "failure" -StepName "Run PowerShell Pester suites"

        $result = Invoke-CiFlakeRateMeasurement `
            -WorkflowRunsJsonPath $script:runsPath `
            -AttemptJobsDirectory $script:attemptsPath `
            -OutputPath $script:outputPath `
            -MinimumCompletedRunCount 1

        $result.Report.Status | Should -Be "measured-over-target"
        $result.Report.FlakeEventCount | Should -Be 1
        $result.Report.FlakeRatePercent | Should -Be 50
        $result.Report.FlakeEvents.EvidenceKind | Should -Be "successful-rerun-after-test-failure-attempt"
        $result.Report.PromotionAllowed | Should -BeFalse
    }

    It "keeps a clean observed window pending statistical and test-level review" {
        New-WorkflowRunFixture -Path $script:runsPath -Runs @(
            1..5 | ForEach-Object {
                @{
                    id = $_
                    name = "Release Validation"
                    status = "completed"
                    conclusion = "success"
                    head_sha = "sha$_"
                    run_attempt = 1
                    created_at = "2026-05-08T1$($_):00:00Z"
                    html_url = "https://example.test/runs/$_"
                }
            }
        )

        $result = Invoke-CiFlakeRateMeasurement `
            -WorkflowRunsJsonPath $script:runsPath `
            -AttemptJobsDirectory $script:attemptsPath `
            -OutputPath $script:outputPath `
            -MinimumCompletedRunCount 5

        $result.Report.Status | Should -Be "measured-with-blockers"
        $result.Report.CompletedRunCount | Should -Be 5
        $result.Report.FlakeEventCount | Should -Be 0
        $result.Report.FlakeRatePercent | Should -Be 0
        $result.Report.PromotionAllowed | Should -BeFalse
        $result.Report.PromotionBlockers | Should -Contain 'statistical-and-test-level-review-required'
    }

    It "fails RequirePromotion when the measured window is not promotable" {
        New-WorkflowRunFixture -Path $script:runsPath -Runs @()

        {
            Invoke-CiFlakeRateMeasurement `
                -WorkflowRunsJsonPath $script:runsPath `
                -AttemptJobsDirectory $script:attemptsPath `
                -OutputPath $script:outputPath `
                -AllowUnavailable `
                -RequirePromotion
        } | Should -Throw "*CI flake-rate evidence is not promotable*"
    }

    It "keeps the release-validation workflow wired to the collector artifact" {
        $workflowPath = Join-Path $script:repoRoot ".github\workflows\release-validation.yml"
        $workflow = Get-Content -LiteralPath $workflowPath -Raw -Encoding UTF8

        $workflow | Should -Match "actions: read"
        $workflow | Should -Match "measure-ci-flake-rate.ps1"
        $workflow | Should -Match "GH_TOKEN"
        $workflow | Should -Match "artifacts/sre-ci-flake-rate"
        $workflow | Should -Match "sre-ci-flake-rate-"
    }

    It 'does not dilute failures or satisfy the sample minimum with cancellations' {
        New-WorkflowRunFixture $script:runsPath @(
            (New-TestRun 1), (New-TestRun 2 failure), (New-TestRun 3 failure), (New-TestRun 4 failure),
            (New-TestRun 5 cancelled), (New-TestRun 6 cancelled)
        )
        $report = (Invoke-FixtureMeasurement).Report
        $report.CompletedRunCount | Should -Be 6
        $report.EligibleRunCount | Should -Be 4
        $report.ExcludedCompletedRunCount | Should -Be 2
        $report.AttemptInspectionUnavailableCount | Should -Be 3
        $report.AssessmentBlockers | Should -Contain 'unresolved-failed-runs'
        $report.AssessmentBlockers | Should -Contain 'excluded-terminal-outcomes'
        $report.PromotionAllowed | Should -BeFalse
        $report.ReadyForStatisticalReview | Should -BeFalse
        $withMinimum = Invoke-CiFlakeRateMeasurement -WorkflowRunsJsonPath $script:runsPath `
            -AttemptJobsDirectory $script:attemptsPath -OutputPath $script:outputPath -MinimumCompletedRunCount 5
        $withMinimum.Report.AvailabilityStatus | Should -Be 'insufficient-actions-history'
    }

    It 'reports no eligible rate as null rather than a measured zero' {
        New-WorkflowRunFixture $script:runsPath @((New-TestRun 1 cancelled), (New-TestRun 2 skipped), (New-TestRun 3 timed_out))
        $report = (Invoke-FixtureMeasurement).Report
        $report.EligibleRunCount | Should -Be 0
        $report.FlakeRatePercent | Should -BeNullOrEmpty
        $report.ExcludedCompletedRunCount | Should -Be 3
        $report.PromotionAllowed | Should -BeFalse
    }

    It 'counts one recovery per run and excludes cancelled runs from its denominator' {
        New-WorkflowRunFixture $script:runsPath @((New-TestRun 1 success 3), (New-TestRun 2 cancelled))
        New-AttemptJobsFixture $script:attemptsPath 1 1 failure 'Run Pester tests'
        New-AttemptJobsFixture $script:attemptsPath 1 2 failure 'dotnet test'
        $report = (Invoke-FixtureMeasurement).Report
        $report.InspectedAttemptCount | Should -Be 2
        $report.FlakeEventCount | Should -Be 1
        $report.FlakeRatePercent | Should -Be 100
    }

    It 'does not infer recovery from later separate runs on the same commit' {
        New-WorkflowRunFixture $script:runsPath @((New-TestRun 1 failure), (New-TestRun 2), (New-TestRun 3))
        New-AttemptJobsFixture $script:attemptsPath 1 1 failure 'Run Pester tests'
        $report = (Invoke-FixtureMeasurement).Report
        $report.FlakeEventCount | Should -Be 0
        $report.FailedRunCount | Should -Be 1
        $report.AssessmentBlockers | Should -Contain 'unresolved-failed-runs'
    }

    It 'does not attribute broad validation failures to tests from job or step names' {
        New-WorkflowRunFixture $script:runsPath @((New-TestRun 1 success 2))
        New-AttemptJobsFixture $script:attemptsPath 1 1 failure 'Run release validation'
        $path = Join-Path $script:attemptsPath '1-attempt-1.json'
        $jobs = Get-Content $path -Raw | ConvertFrom-Json
        $jobs.jobs[0].name = 'Run Pester tests'
        $jobs | ConvertTo-Json -Depth 12 | Set-Content $path
        $report = (Invoke-FixtureMeasurement).Report
        $report.FlakeEventCount | Should -Be 0
        $report.AttemptInspectionUnattributedCount | Should -Be 1
        $report.AssessmentBlockers | Should -Contain 'unattributed-attempts'
    }

    It 'marks truncated and empty attempt-job fixtures incomplete' -ForEach @(
        @{ Total = 2; Empty = $false }, @{ Total = 0; Empty = $true }
    ) {
        New-WorkflowRunFixture $script:runsPath @((New-TestRun 1 success 2))
        New-AttemptJobsFixture $script:attemptsPath 1 1 failure 'Run Pester tests'
        $path = Join-Path $script:attemptsPath '1-attempt-1.json'
        $jobs = Get-Content $path -Raw | ConvertFrom-Json
        $jobs.total_count = $Total
        if ($Empty) { $jobs.jobs = @() }
        $jobs | ConvertTo-Json -Depth 12 | Set-Content $path
        $report = (Invoke-FixtureMeasurement).Report
        $report.AttemptInspectionUnavailableCount | Should -Be 1
        $report.FlakeEventCount | Should -Be 0
        $report.AssessmentBlockers | Should -Contain 'incomplete-attempt-inspection'
    }

    It 'marks incomplete or duplicate run fixtures incomplete' -ForEach @(
        @{ Duplicate = $false }, @{ Duplicate = $true }
    ) {
        $runs = @((New-TestRun 1))
        if ($Duplicate) { $runs += New-TestRun 1 }
        @{ total_count = 2; workflow_runs = $runs } | ConvertTo-Json -Depth 12 | Set-Content $script:runsPath
        $report = (Invoke-FixtureMeasurement).Report
        $report.RunCollectionComplete | Should -BeFalse
        $report.EligibleRunCount | Should -Be 1
        $report.AvailabilityStatus | Should -Be 'incomplete-actions-history'
    }

    It 'uses the declared creation window for fixtures and reports excluded and unfinished runs' {
        $pending = New-TestRun 4
        $pending.status = 'in_progress'; $pending.conclusion = $null
        New-WorkflowRunFixture $script:runsPath @(
            (New-TestRun 1 -CreatedAt '2026-05-01T23:59:59Z'),
            (New-TestRun 2 -CreatedAt '2026-05-02T00:00:00Z'),
            (New-TestRun 3 -CreatedAt '2026-05-09T00:00:01Z'), $pending)
        $report = (Invoke-FixtureMeasurement).Report
        $report.OutOfWindowRunCount | Should -Be 2
        $report.EligibleRunCount | Should -Be 1
        $report.IncompleteRunCount | Should -Be 1
        $report.ObservedRuns.Count | Should -Be 2
        ($report.ObservedRuns | Where-Object Id -eq '2').CreatedAtUtc | Should -Be '2026-05-02T00:00:00.0000000Z'
    }

    It 'preserves UTC instants for JSON strings DateTime and DateTimeOffset inputs' {
        foreach ($timestamp in @('2026-05-08T19:00:00+07:00', ([datetimeoffset]'2026-05-08T12:00:00Z').UtcDateTime,
            [datetimeoffset]'2026-05-08T19:00:00+07:00')) {
            $run = [pscustomobject](New-TestRun 1)
            $run.created_at = $timestamp
            (Convert-ToNormalizedWorkflowRun $run).CreatedAtUtc | Should -Be '2026-05-08T12:00:00.0000000Z'
        }
    }

    It 'retains a report for a fixture missing its run collection' {
        '{"total_count":1}' | Set-Content $script:runsPath
        $report = (Invoke-FixtureMeasurement).Report
        $report.AvailabilityStatus | Should -Be 'incomplete-actions-history'
        $report.RunCollectionComplete | Should -BeFalse
        $report.FlakeRatePercent | Should -BeNullOrEmpty
    }

    It 'treats absent step details as incomplete evidence' {
        New-WorkflowRunFixture $script:runsPath @((New-TestRun 1 success 2))
        '{"total_count":1,"jobs":[{"id":1}]}' | Set-Content (Join-Path $script:attemptsPath '1-attempt-1.json')
        $report = (Invoke-FixtureMeasurement).Report
        $report.AttemptInspectionUnavailableCount | Should -Be 1
        $report.ObservationComplete | Should -BeFalse
    }

    It 'requires separate statistical review even with complete clean metadata' {
        New-WorkflowRunFixture $script:runsPath @((New-TestRun 1))
        New-ActionsPermissionsFixture $script:actionsPermissionsPath
        New-WorkflowDefinitionsFixture $script:workflowsPath @(
            @{ id = 1; name = 'Release Validation'; path = '.github/workflows/release-validation.yml';
               state = 'active'; html_url = 'https://example.test/workflow'; dispatch_configured = $true })
        $params = @{ WorkflowRunsJsonPath = $script:runsPath; ActionsPermissionsJsonPath = $script:actionsPermissionsPath
            WorkflowsJsonPath = $script:workflowsPath; OutputPath = $script:outputPath
            WorkflowName = @('Release Validation'); MinimumCompletedRunCount = 1 }
        $report = (Invoke-CiFlakeRateMeasurement @params).Report
        $report.ReadyForStatisticalReview | Should -BeTrue
        $report.Status | Should -Be 'measured-within-target'
        $report.PromotionAllowed | Should -BeFalse
        { Invoke-CiFlakeRateMeasurement @params -RequirePromotion } | Should -Throw '*statistical-and-test-level-review-required*'
        $params.WorkflowName += 'Publish Release'
        $missing = (Invoke-CiFlakeRateMeasurement @params).Report
        $missing.ReadyForStatisticalReview | Should -BeFalse
        $missing.AssessmentBlockers | Should -Contain 'insufficient-workflow-history'
    }

    It 'collects multiple pages for runs jobs and workflow definitions' -ForEach @(
        @{ Collection = 'workflow_runs' }, @{ Collection = 'jobs' }, @{ Collection = 'workflows' }
    ) {
        $script:collectionUnderTest = $Collection
        Mock Invoke-GitHubApiJson {
            $ids = if ($Endpoint -match '&page=1$') { 1..100 } else { @(101) }
            [pscustomobject]@{ Status = 'available'; Detail = ''; Value = [pscustomobject]@{
                total_count = 101; $script:collectionUnderTest = @($ids | ForEach-Object { [pscustomobject]@{ id = $_ } }) } }
        }
        $result = Get-PagedGitHubCollection -Endpoint 'repos/test/repo/actions/runs?created=range' -CollectionName $Collection
        $result.Status | Should -Be 'available'
        $result.Value.$Collection.Count | Should -Be 101
        Should -Invoke Invoke-GitHubApiJson -Times 2 -Exactly
    }

    It 'retains partial evidence and fails coverage on unavailable changed or duplicate pages' -ForEach @(
        @{ Mode = 'unavailable' }, @{ Mode = 'changed-total' }, @{ Mode = 'duplicate' }, @{ Mode = 'short-page' }
    ) {
        $script:pageFailureMode = $Mode
        Mock Invoke-GitHubApiJson {
            if ($Endpoint -match 'page=1$') {
                return [pscustomobject]@{ Status = 'available'; Detail = ''; Value = [pscustomobject]@{
                    total_count = 101; jobs = @(1..100 | ForEach-Object { [pscustomobject]@{ id = $_ } }) } }
            }
            if ($script:pageFailureMode -eq 'unavailable') { return [pscustomobject]@{ Status = 'unavailable-github-api'; Detail = '403'; Value = $null } }
            $count = if ($script:pageFailureMode -eq 'changed-total') { 102 } else { 101 }
            $jobs = if ($script:pageFailureMode -eq 'short-page') { @() } elseif ($script:pageFailureMode -eq 'duplicate') { @(@{id=1}) } else { @(@{id=101}) }
            [pscustomobject]@{ Status = 'available'; Detail = ''; Value = [pscustomobject]@{ total_count = $count; jobs = @($jobs) } }
        }
        $result = Get-PagedGitHubCollection -Endpoint 'jobs' -CollectionName jobs
        $result.Status | Should -Be 'incomplete-github-pagination'
        $result.Value.jobs.Count | Should -BeGreaterOrEqual 100
    }

    It 'fails closed at the filtered workflow search cap' {
        Mock Invoke-GitHubApiJson {
            [pscustomobject]@{ Status = 'available'; Detail = ''; Value = [pscustomobject]@{
                total_count = 1000; workflow_runs = @(1..100 | ForEach-Object { [pscustomobject]@{ id = $_ } }) } }
        }
        $result = Get-PagedGitHubCollection -Endpoint 'runs' -CollectionName workflow_runs -MaximumItems 1000
        $result.Status | Should -Be 'incomplete-github-pagination'
        $result.Detail | Should -Match '1000'
        Should -Invoke Invoke-GitHubApiJson -Times 1 -Exactly
    }

    It 'queries Gregorian UTC dates under a Thai Buddhist calendar culture' {
        Mock Get-PagedGitHubCollection { [pscustomobject]@{ Endpoint = $Endpoint } }
        $culture = [System.Globalization.CultureInfo]::CurrentCulture
        try {
            [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('th-TH')
            $result = Get-WorkflowRunsPayload -Repository test/repo -RepoRoot $script:repoRoot `
                -WindowStartUtc ([datetimeoffset]'2026-05-02T00:00:00Z').UtcDateTime `
                -WindowEndUtc ([datetimeoffset]'2026-05-09T00:00:00Z').UtcDateTime
            [uri]::UnescapeDataString($result.Endpoint) | Should -Be 'repos/test/repo/actions/runs?created=2026-05-02T00:00:00Z..2026-05-09T00:00:00Z'
        }
        finally { [System.Globalization.CultureInfo]::CurrentCulture = $culture }
    }
}
