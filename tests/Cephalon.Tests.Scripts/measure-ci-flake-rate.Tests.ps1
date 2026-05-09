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

Describe "measure-ci-flake-rate.ps1" {
    BeforeEach {
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

    It "promotes a clean measured window as a stable-baseline candidate" {
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

        $result.Report.Status | Should -Be "stable-baseline-candidate"
        $result.Report.CompletedRunCount | Should -Be 5
        $result.Report.FlakeEventCount | Should -Be 0
        $result.Report.FlakeRatePercent | Should -Be 0
        $result.Report.PromotionAllowed | Should -BeTrue
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
}
