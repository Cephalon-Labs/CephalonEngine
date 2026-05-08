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
        $result.Report.CompletedRunCount | Should -Be 0
        $result.Report.PromotionAllowed | Should -BeFalse
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
