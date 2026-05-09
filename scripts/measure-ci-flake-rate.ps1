param(
    [string]$Repository = "Cephalon-Labs/CephalonEngine",
    [int]$WindowDays = 7,
    [decimal]$TargetFlakeRatePercent = 0.5,
    [int]$MinimumCompletedRunCount = 5,
    [string[]]$WorkflowName = @("Release Validation", "Provider Live Testcontainers", "Publish Release"),
    [string]$WorkflowRunsJsonPath = "",
    [string]$AttemptJobsDirectory = "",
    [string]$OutputPath = "artifacts/sre-ci-flake-rate",
    [string]$TestFailurePattern = "(?i)(Pester|dotnet test|testcontainers|test results|Run tests|Run release validation)",
    [switch]$AllowUnavailable,
    [switch]$RequirePromotion
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    $candidate = Resolve-Path (Join-Path $PSScriptRoot "..")
    return $candidate.Path
}

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $Path))
}

function Resolve-CiFlakeRateOutputTarget {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $resolvedPath = Resolve-RepoPath -Path $Path -RepoRoot $RepoRoot
    if ((Test-Path -LiteralPath $resolvedPath -PathType Container) -or
        -not $resolvedPath.EndsWith(".json", [System.StringComparison]::OrdinalIgnoreCase)) {
        return [pscustomobject]@{
            Directory = $resolvedPath
            JsonPath  = Join-Path $resolvedPath "ci-flake-rate.json"
        }
    }

    $directory = Split-Path -Parent $resolvedPath
    if ([string]::IsNullOrWhiteSpace($directory)) {
        $directory = $RepoRoot
    }

    return [pscustomobject]@{
        Directory = $directory
        JsonPath  = $resolvedPath
    }
}

function Read-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 32
}

function Invoke-GitHubApiJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Endpoint
    )

    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if ($null -eq $gh) {
        return [pscustomobject]@{
            Status = "unavailable-gh-cli"
            Detail = "gh CLI was not found."
            Value  = $null
        }
    }

    $output = & gh api $Endpoint 2>&1
    if ($LASTEXITCODE -ne 0) {
        return [pscustomobject]@{
            Status = "unavailable-github-api"
            Detail = ($output | Out-String).Trim()
            Value  = $null
        }
    }

    return [pscustomobject]@{
        Status = "available"
        Detail = "GitHub Actions metadata was queried with gh."
        Value  = ($output | Out-String | ConvertFrom-Json -Depth 32)
    }
}

function Get-WorkflowRunsPayload {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repository,
        [Parameter(Mandatory = $true)]
        [datetime]$WindowStartUtc,
        [string]$WorkflowRunsJsonPath,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($WorkflowRunsJsonPath)) {
        $resolvedPath = Resolve-RepoPath -Path $WorkflowRunsJsonPath -RepoRoot $RepoRoot
        if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
            throw "Workflow runs fixture was not found at '$resolvedPath'."
        }

        return [pscustomobject]@{
            Status = "available"
            Detail = "Workflow runs were read from fixture '$WorkflowRunsJsonPath'."
            Value  = Read-JsonFile -Path $resolvedPath
        }
    }

    $createdFilter = [uri]::EscapeDataString(">=$($WindowStartUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"))")
    return Invoke-GitHubApiJson -Endpoint "repos/$Repository/actions/runs?per_page=100&created=$createdFilter"
}

function Get-WorkflowRunAttemptJobsPayload {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repository,
        [Parameter(Mandatory = $true)]
        [string]$RunId,
        [Parameter(Mandatory = $true)]
        [int]$AttemptNumber,
        [string]$AttemptJobsDirectory,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($AttemptJobsDirectory)) {
        $resolvedDirectory = Resolve-RepoPath -Path $AttemptJobsDirectory -RepoRoot $RepoRoot
        $fixturePath = Join-Path $resolvedDirectory "$RunId-attempt-$AttemptNumber.json"
        if (Test-Path -LiteralPath $fixturePath -PathType Leaf) {
            return [pscustomobject]@{
                Status = "available"
                Detail = "Attempt jobs were read from fixture '$fixturePath'."
                Value  = Read-JsonFile -Path $fixturePath
            }
        }

        return [pscustomobject]@{
            Status = "unavailable-attempt-fixture"
            Detail = "Attempt jobs fixture '$fixturePath' was not found."
            Value  = $null
        }
    }

    return Invoke-GitHubApiJson -Endpoint "repos/$Repository/actions/runs/$RunId/attempts/$AttemptNumber/jobs?per_page=100"
}

function Convert-ToNormalizedWorkflowRun {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Run
    )

    $createdAt = [datetime]::MinValue
    if (-not [string]::IsNullOrWhiteSpace([string]$Run.created_at)) {
        $createdAt = ([datetimeoffset]::Parse([string]$Run.created_at, [System.Globalization.CultureInfo]::InvariantCulture)).UtcDateTime
    }

    [pscustomobject]([ordered]@{
        Id         = [string]$Run.id
        Name       = [string]$Run.name
        Status     = [string]$Run.status
        Conclusion = [string]$Run.conclusion
        HeadSha    = [string]$Run.head_sha
        RunAttempt = if ($null -eq $Run.run_attempt) { 1 } else { [int]$Run.run_attempt }
        CreatedAtUtc = $createdAt.ToString("O")
        HtmlUrl    = [string]$Run.html_url
    })
}

function Test-WorkflowNameIncluded {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [string[]]$IncludedNames
    )

    if ($IncludedNames.Count -eq 0) {
        return $true
    }

    foreach ($includedName in $IncludedNames) {
        if ($Name.Equals($includedName, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Test-HasTestFailureSignal {
    param(
        [AllowNull()]
        [object]$JobsPayload,
        [Parameter(Mandatory = $true)]
        [string]$Pattern
    )

    if ($null -eq $JobsPayload) {
        return $false
    }

    $jobs = @($JobsPayload.jobs)
    foreach ($job in $jobs) {
        $jobName = [string]$job.name
        $jobConclusion = [string]$job.conclusion
        if ($jobConclusion -eq "failure" -and $jobName -match $Pattern) {
            return $true
        }

        foreach ($step in @($job.steps)) {
            $stepName = [string]$step.name
            $stepConclusion = [string]$step.conclusion
            if ($stepConclusion -eq "failure" -and $stepName -match $Pattern) {
                return $true
            }
        }
    }

    return $false
}

function Invoke-CiFlakeRateMeasurement {
    param(
        [string]$Repository = "Cephalon-Labs/CephalonEngine",
        [int]$WindowDays = 7,
        [decimal]$TargetFlakeRatePercent = 0.5,
        [int]$MinimumCompletedRunCount = 5,
        [string[]]$WorkflowName = @("Release Validation", "Provider Live Testcontainers", "Publish Release"),
        [string]$WorkflowRunsJsonPath = "",
        [string]$AttemptJobsDirectory = "",
        [string]$OutputPath = "artifacts/sre-ci-flake-rate",
        [string]$TestFailurePattern = "(?i)(Pester|dotnet test|testcontainers|test results|Run tests|Run release validation)",
        [switch]$AllowUnavailable,
        [switch]$RequirePromotion
    )

    if ($WindowDays -le 0) {
        throw "WindowDays must be greater than zero."
    }

    if ($MinimumCompletedRunCount -le 0) {
        throw "MinimumCompletedRunCount must be greater than zero."
    }

    $repoRoot = Resolve-RepoRoot
    $windowStartUtc = [DateTime]::UtcNow.AddDays(-$WindowDays)
    $workflowRunsPayload = Get-WorkflowRunsPayload `
        -Repository $Repository `
        -WindowStartUtc $windowStartUtc `
        -WorkflowRunsJsonPath $WorkflowRunsJsonPath `
        -RepoRoot $repoRoot

    $availabilityStatus = $workflowRunsPayload.Status
    $availabilityDetail = $workflowRunsPayload.Detail
    $rawRuns = @()
    if ($workflowRunsPayload.Status -eq "available") {
        $rawRuns = @($workflowRunsPayload.Value.workflow_runs)
    }
    elseif (-not $AllowUnavailable) {
        throw "CI flake-rate metadata is unavailable: $availabilityStatus. $availabilityDetail"
    }

    $allRuns = @(
        $rawRuns |
            ForEach-Object { Convert-ToNormalizedWorkflowRun -Run $_ } |
            Where-Object { Test-WorkflowNameIncluded -Name $_.Name -IncludedNames $WorkflowName } |
            Sort-Object CreatedAtUtc
    )
    $completedRuns = @($allRuns | Where-Object { $_.Status -eq "completed" -and -not [string]::IsNullOrWhiteSpace($_.Conclusion) })
    $successfulRuns = @($completedRuns | Where-Object { $_.Conclusion -eq "success" })
    $failedRuns = @($completedRuns | Where-Object { $_.Conclusion -eq "failure" })

    $testFailureSignals = @{}
    $inspectedAttemptCount = 0
    $attemptInspectionUnavailableCount = 0
    foreach ($run in @($completedRuns | Where-Object { $_.Conclusion -eq "failure" -or ($_.Conclusion -eq "success" -and $_.RunAttempt -gt 1) })) {
        $attemptNumbers = if ($run.Conclusion -eq "success" -and $run.RunAttempt -gt 1) {
            1..($run.RunAttempt - 1)
        }
        else {
            @($run.RunAttempt)
        }

        foreach ($attemptNumber in $attemptNumbers) {
            $attemptJobsPayload = Get-WorkflowRunAttemptJobsPayload `
                -Repository $Repository `
                -RunId $run.Id `
                -AttemptNumber $attemptNumber `
                -AttemptJobsDirectory $AttemptJobsDirectory `
                -RepoRoot $repoRoot

            if ($attemptJobsPayload.Status -eq "available") {
                $inspectedAttemptCount++
                if (Test-HasTestFailureSignal -JobsPayload $attemptJobsPayload.Value -Pattern $TestFailurePattern) {
                    $testFailureSignals["$($run.Id):$attemptNumber"] = $true
                }
            }
            else {
                $attemptInspectionUnavailableCount++
            }
        }
    }

    $flakeEvents = @()
    foreach ($run in $successfulRuns) {
        $priorFailedAttemptNumbers = @()
        if ($run.RunAttempt -gt 1) {
            $priorFailedAttemptNumbers = @(
                1..($run.RunAttempt - 1) |
                    Where-Object { $testFailureSignals.ContainsKey("$($run.Id):$_") }
            )
        }

        if ($priorFailedAttemptNumbers.Count -gt 0) {
            $flakeEvents += [pscustomobject]([ordered]@{
                WorkflowName = $run.Name
                HeadSha      = $run.HeadSha
                RunId        = $run.Id
                SuccessAttempt = $run.RunAttempt
                FailedAttempts = $priorFailedAttemptNumbers
                EvidenceKind = "successful-rerun-after-test-failure-attempt"
                Url          = $run.HtmlUrl
            })
            continue
        }

        $failedSameCommitRuns = @(
            $failedRuns |
                Where-Object {
                    $_.Name -eq $run.Name -and
                    $_.HeadSha -eq $run.HeadSha -and
                    $_.CreatedAtUtc -le $run.CreatedAtUtc -and
                    $testFailureSignals.ContainsKey("$($_.Id):$($_.RunAttempt)")
                }
        )
        if ($failedSameCommitRuns.Count -gt 0) {
            $flakeEvents += [pscustomobject]([ordered]@{
                WorkflowName = $run.Name
                HeadSha      = $run.HeadSha
                RunId        = $run.Id
                SuccessAttempt = $run.RunAttempt
                FailedRunIds = @($failedSameCommitRuns | ForEach-Object { $_.Id })
                EvidenceKind = "successful-run-after-test-failure-on-same-commit"
                Url          = $run.HtmlUrl
            })
        }
    }

    $flakeEventCount = @($flakeEvents).Count
    $flakeRatePercent = if ($completedRuns.Count -eq 0) {
        [decimal]0
    }
    else {
        [decimal]::Round((([decimal]$flakeEventCount / [decimal]$completedRuns.Count) * 100), 4)
    }

    if ($workflowRunsPayload.Status -ne "available") {
        $availabilityStatus = $workflowRunsPayload.Status
    }
    elseif ($completedRuns.Count -eq 0) {
        $availabilityStatus = "unavailable-no-actions-history"
        $availabilityDetail = "No completed matching GitHub Actions workflow runs were available in the requested window."
    }
    elseif ($completedRuns.Count -lt $MinimumCompletedRunCount) {
        $availabilityStatus = "insufficient-actions-history"
        $availabilityDetail = "Only $($completedRuns.Count) completed matching workflow run(s) were available; $MinimumCompletedRunCount are required for promotion."
    }
    else {
        $availabilityStatus = "measured"
        $availabilityDetail = "Completed matching GitHub Actions workflow runs were available for the requested window."
    }

    $promotionAllowed = $availabilityStatus -eq "measured" -and $flakeRatePercent -le $TargetFlakeRatePercent
    $status = if ($promotionAllowed) {
        "stable-baseline-candidate"
    }
    elseif ($availabilityStatus -eq "measured") {
        "measured-over-target"
    }
    elseif ($availabilityStatus -eq "insufficient-actions-history") {
        "pending-insufficient-history"
    }
    else {
        "pending-$availabilityStatus"
    }

    $report = [pscustomobject]([ordered]@{
        '$schemaVersion' = "1.0.0"
        Status = $status
        SliId = "engine.tests.flake-rate.7d"
        GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        Repository = $Repository
        WindowDays = $WindowDays
        WindowStartUtc = $windowStartUtc.ToString("O")
        WorkflowNames = $WorkflowName
        TargetFlakeRatePercent = $TargetFlakeRatePercent
        MinimumCompletedRunCount = $MinimumCompletedRunCount
        AvailabilityStatus = $availabilityStatus
        AvailabilityDetail = $availabilityDetail
        TotalRunCount = $allRuns.Count
        CompletedRunCount = $completedRuns.Count
        SuccessfulRunCount = $successfulRuns.Count
        FailedRunCount = $failedRuns.Count
        InspectedAttemptCount = $inspectedAttemptCount
        AttemptInspectionUnavailableCount = $attemptInspectionUnavailableCount
        FlakeEventCount = $flakeEventCount
        FlakeRatePercent = $flakeRatePercent
        PromotionAllowed = $promotionAllowed
        FlakeEvents = $flakeEvents
        ObservedRuns = $completedRuns
    })

    $outputTarget = Resolve-CiFlakeRateOutputTarget -Path $OutputPath -RepoRoot $repoRoot
    New-Item -ItemType Directory -Path $outputTarget.Directory -Force | Out-Null
    $jsonPath = $outputTarget.JsonPath
    $report | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

    Write-Host ("CI flake-rate evidence: {0}; completed {1}; flakes {2}; rate {3}%; promotionAllowed {4}; report {5}" -f `
            $status,
            $completedRuns.Count,
            $flakeEventCount,
            $flakeRatePercent,
            $promotionAllowed,
            $jsonPath)

    if ($RequirePromotion -and -not $promotionAllowed) {
        throw "CI flake-rate evidence is not promotable: status=$status, availability=$availabilityStatus, flakeRate=$flakeRatePercent%, target=$TargetFlakeRatePercent%."
    }

    return [pscustomobject]@{
        Report = $report
        JsonPath = $jsonPath
    }
}

if (-not $env:CEPHALON_CI_FLAKE_RATE_NO_RUN) {
    $null = Invoke-CiFlakeRateMeasurement `
        -Repository $Repository `
        -WindowDays $WindowDays `
        -TargetFlakeRatePercent $TargetFlakeRatePercent `
        -MinimumCompletedRunCount $MinimumCompletedRunCount `
        -WorkflowName $WorkflowName `
        -WorkflowRunsJsonPath $WorkflowRunsJsonPath `
        -AttemptJobsDirectory $AttemptJobsDirectory `
        -OutputPath $OutputPath `
        -TestFailurePattern $TestFailurePattern `
        -AllowUnavailable:$AllowUnavailable `
        -RequirePromotion:$RequirePromotion
}
