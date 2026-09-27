param(
    [string]$Repository = "Cephalon-Labs/CephalonEngine",
    [int]$WindowDays = 7,
    [decimal]$TargetFlakeRatePercent = 0.5,
    [int]$MinimumCompletedRunCount = 5,
    [datetimeoffset]$WindowEndUtc = [datetimeoffset]::UtcNow,
    [string[]]$WorkflowName = @("Release Validation", "Provider Live Testcontainers", "Publish Release"),
    [string]$WorkflowRunsJsonPath = "",
    [string]$ActionsPermissionsJsonPath = "",
    [string]$WorkflowsJsonPath = "",
    [string]$AttemptJobsDirectory = "",
    [string]$OutputPath = "artifacts/sre-ci-flake-rate",
    [string]$TestFailurePattern = "(?i)(Pester|dotnet test|Run tests)",
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

function Test-CompleteCollection {
    param([AllowNull()][object]$Value, [string]$CollectionName)

    if ($null -eq $Value -or $Value.PSObject.Properties.Name -notcontains 'total_count' -or
        $Value.PSObject.Properties.Name -notcontains $CollectionName) { return $false }
    $items = @($Value.$CollectionName)
    if ($null -eq $Value.total_count -or [int]$Value.total_count -ne $items.Count) { return $false }
    $ids = @($items | ForEach-Object {
        if ($null -ne $_ -and $_.PSObject.Properties.Name -contains 'id') { [string]$_.id }
    })
    return $ids.Count -eq $items.Count -and @($ids | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -eq 0 -and
        @($ids | Sort-Object -Unique).Count -eq $ids.Count
}

function Get-PagedGitHubCollection {
    param([string]$Endpoint, [string]$CollectionName, [int]$MaximumItems = 10000)

    $items = [System.Collections.Generic.List[object]]::new()
    $expectedCount = $null
    $status = 'incomplete-github-pagination'
    $detail = 'Collection coverage could not be established.'
    for ($page = 1; $page -le [math]::Ceiling($MaximumItems / 100); $page++) {
        $separator = if ($Endpoint.Contains('?')) { '&' } else { '?' }
        $payload = Invoke-GitHubApiJson -Endpoint "${Endpoint}${separator}per_page=100&page=$page"
        if ($payload.Status -ne 'available') {
            $detail = "Page ${page}: $($payload.Detail)"
            if ($page -eq 1) { return $payload }
            break
        }
        $value = $payload.Value
        if ($null -eq $value -or $value.PSObject.Properties.Name -notcontains 'total_count' -or
            $value.PSObject.Properties.Name -notcontains $CollectionName -or $null -eq $value.total_count) {
            $detail = "Page $page did not contain a collection and total_count."
            break
        }
        $count = [int]$value.total_count
        if ($null -eq $expectedCount) { $expectedCount = $count }
        if ($count -ne $expectedCount -or $count -lt 0) {
            $detail = 'Collection total_count changed during pagination.'
            break
        }
        $pageItems = @($value.$CollectionName)
        foreach ($item in $pageItems) { $items.Add($item) }
        # Filtered workflow searches are capped by GitHub at 1,000 results.
        # Even an exact cap cannot establish absence of omitted matches.
        if ($expectedCount -ge $MaximumItems) {
            $detail = "Collection reached the $MaximumItems item safety/search limit. Narrow the collection window."
            break
        }
        if ($items.Count -ge $expectedCount) {
            $candidate = [pscustomobject]@{ total_count = $expectedCount; $CollectionName = $items.ToArray() }
            if (Test-CompleteCollection -Value $candidate -CollectionName $CollectionName) {
                $status = 'available'
                $detail = "Collected $($items.Count) unique items across $page page(s)."
            }
            else { $detail = 'Collection contains duplicate or missing item IDs, or an inconsistent count.' }
            break
        }
        if ($pageItems.Count -lt 100) {
            $detail = 'A short page ended before total_count was reached.'
            break
        }
    }
    return [pscustomobject]@{
        Status = $status; Detail = $detail
        Value = [pscustomobject]@{ total_count = $expectedCount; $CollectionName = $items.ToArray() }
    }
}

function Get-WorkflowRunsPayload {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repository,
        [Parameter(Mandatory = $true)]
        [datetime]$WindowStartUtc,
        [datetime]$WindowEndUtc,
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

    $invariant = [System.Globalization.CultureInfo]::InvariantCulture
    $start = $WindowStartUtc.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ', $invariant)
    $end = $WindowEndUtc.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ', $invariant)
    $createdFilter = [uri]::EscapeDataString("$start..$end")
    return Get-PagedGitHubCollection -Endpoint "repos/$Repository/actions/runs?created=$createdFilter" -CollectionName workflow_runs -MaximumItems 1000
}

function Get-ActionsPermissionsPayload {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repository,
        [string]$ActionsPermissionsJsonPath,
        [string]$WorkflowRunsJsonPath,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($ActionsPermissionsJsonPath)) {
        $resolvedPath = Resolve-RepoPath -Path $ActionsPermissionsJsonPath -RepoRoot $RepoRoot
        if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
            throw "Actions permissions fixture was not found at '$resolvedPath'."
        }

        return [pscustomobject]@{
            Status = "available"
            Detail = "Actions permissions were read from fixture '$ActionsPermissionsJsonPath'."
            Value  = Read-JsonFile -Path $resolvedPath
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($WorkflowRunsJsonPath)) {
        return [pscustomobject]@{
            Status = "not-evaluated-fixture-mode"
            Detail = "Actions permissions were not queried because workflow runs were read from a fixture."
            Value  = $null
        }
    }

    return Invoke-GitHubApiJson -Endpoint "repos/$Repository/actions/permissions"
}

function Get-WorkflowDefinitionsPayload {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repository,
        [string]$WorkflowsJsonPath,
        [string]$WorkflowRunsJsonPath,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($WorkflowsJsonPath)) {
        $resolvedPath = Resolve-RepoPath -Path $WorkflowsJsonPath -RepoRoot $RepoRoot
        if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
            throw "Workflow definitions fixture was not found at '$resolvedPath'."
        }

        return [pscustomobject]@{
            Status = "available"
            Detail = "Workflow definitions were read from fixture '$WorkflowsJsonPath'."
            Value  = Read-JsonFile -Path $resolvedPath
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($WorkflowRunsJsonPath)) {
        return [pscustomobject]@{
            Status = "not-evaluated-fixture-mode"
            Detail = "Workflow definitions were not queried because workflow runs were read from a fixture."
            Value  = $null
        }
    }

    return Get-PagedGitHubCollection -Endpoint "repos/$Repository/actions/workflows" -CollectionName workflows
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

    return Get-PagedGitHubCollection -Endpoint "repos/$Repository/actions/runs/$RunId/attempts/$AttemptNumber/jobs" -CollectionName jobs
}

function Convert-ToNormalizedWorkflowDefinition {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Workflow
    )

    $dispatchConfigured = $null
    if ($Workflow.PSObject.Properties.Name -contains "dispatch_configured") {
        $dispatchConfigured = [bool]$Workflow.dispatch_configured
    }
    elseif ($Workflow.PSObject.Properties.Name -contains "DispatchConfigured") {
        $dispatchConfigured = [bool]$Workflow.DispatchConfigured
    }

    [pscustomobject]([ordered]@{
        Id      = [string]$Workflow.id
        Name    = [string]$Workflow.name
        Path    = [string]$Workflow.path
        State   = [string]$Workflow.state
        HtmlUrl = [string]$Workflow.html_url
        DispatchConfigured = $dispatchConfigured
    })
}

function Test-WorkflowDispatchConfigured {
    param(
        [AllowNull()]
        [object]$Workflow,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    if ($null -eq $Workflow) {
        return $false
    }

    $workflowPath = [string]$Workflow.Path
    if ($Workflow.PSObject.Properties.Name -contains "DispatchConfigured" -and $null -ne $Workflow.DispatchConfigured) {
        return [bool]$Workflow.DispatchConfigured
    }

    if ([string]::IsNullOrWhiteSpace($workflowPath)) {
        return $false
    }

    $resolvedPath = Resolve-RepoPath -Path $workflowPath -RepoRoot $RepoRoot
    if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
        return $false
    }

    $workflowContent = Get-Content -LiteralPath $resolvedPath -Raw -Encoding UTF8
    return $workflowContent -match "(?m)^\s*workflow_dispatch\s*:"
}

function Convert-ToNormalizedWorkflowRun {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Run
    )

    $createdAt = [datetime]::MinValue
    if ($Run.created_at -is [datetime]) {
        # ConvertFrom-Json can materialize ISO timestamps as DateTime. Casting
        # that value to string discards its timezone and shifts it on reparse.
        $createdAt = $Run.created_at.ToUniversalTime()
    }
    elseif ($Run.created_at -is [datetimeoffset]) {
        $createdAt = $Run.created_at.UtcDateTime
    }
    elseif (-not [string]::IsNullOrWhiteSpace([string]$Run.created_at)) {
        $createdAt = ([datetimeoffset]::Parse([string]$Run.created_at, [System.Globalization.CultureInfo]::InvariantCulture,
            [System.Globalization.DateTimeStyles]::AssumeUniversal)).UtcDateTime
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
        # A job name (or a whole release-validation step) cannot identify which
        # test failed. Only explicit failed test-step names are heuristic signals.
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

function Test-CompleteAttemptJobs {
    param([AllowNull()][object]$Value)
    if (-not (Test-CompleteCollection -Value $Value -CollectionName jobs) -or @($Value.jobs).Count -eq 0) { return $false }
    foreach ($job in $Value.jobs) {
        if ($job.PSObject.Properties.Name -notcontains 'steps' -or $null -eq $job.steps) { return $false }
        foreach ($step in $job.steps) {
            if ($null -eq $step -or $step.PSObject.Properties.Name -notcontains 'name' -or
                $step.PSObject.Properties.Name -notcontains 'conclusion') { return $false }
        }
    }
    return $true
}

function Invoke-CiFlakeRateMeasurement {
    [CmdletBinding()]
    param(
        [string]$Repository = "Cephalon-Labs/CephalonEngine",
        [int]$WindowDays = 7,
        [decimal]$TargetFlakeRatePercent = 0.5,
        [int]$MinimumCompletedRunCount = 5,
        [datetimeoffset]$WindowEndUtc = [datetimeoffset]::UtcNow,
        [string[]]$WorkflowName = @("Release Validation", "Provider Live Testcontainers", "Publish Release"),
        [string]$WorkflowRunsJsonPath = "",
        [string]$ActionsPermissionsJsonPath = "",
        [string]$WorkflowsJsonPath = "",
        [string]$AttemptJobsDirectory = "",
        [string]$OutputPath = "artifacts/sre-ci-flake-rate",
        [string]$TestFailurePattern = "(?i)(Pester|dotnet test|Run tests)",
        [switch]$AllowUnavailable,
        [switch]$RequirePromotion
    )

    if ($WindowDays -le 0) {
        throw "WindowDays must be greater than zero."
    }

    if ($MinimumCompletedRunCount -le 0) {
        throw "MinimumCompletedRunCount must be greater than zero."
    }
    if ($TargetFlakeRatePercent -lt 0 -or $TargetFlakeRatePercent -gt 100) {
        throw 'TargetFlakeRatePercent must be between zero and 100.'
    }

    $repoRoot = Resolve-RepoRoot
    $windowStartUtc = $WindowEndUtc.UtcDateTime.AddDays(-$WindowDays)
    $workflowRunsPayload = Get-WorkflowRunsPayload `
        -Repository $Repository `
        -WindowStartUtc $windowStartUtc `
        -WindowEndUtc $WindowEndUtc.UtcDateTime `
        -WorkflowRunsJsonPath $WorkflowRunsJsonPath `
        -RepoRoot $repoRoot

    $actionsPermissionsPayload = Get-ActionsPermissionsPayload `
        -Repository $Repository `
        -ActionsPermissionsJsonPath $ActionsPermissionsJsonPath `
        -WorkflowRunsJsonPath $WorkflowRunsJsonPath `
        -RepoRoot $repoRoot

    $workflowDefinitionsPayload = Get-WorkflowDefinitionsPayload `
        -Repository $Repository `
        -WorkflowsJsonPath $WorkflowsJsonPath `
        -WorkflowRunsJsonPath $WorkflowRunsJsonPath `
        -RepoRoot $repoRoot

    $availabilityStatus = $workflowRunsPayload.Status
    $availabilityDetail = $workflowRunsPayload.Detail
    $rawRuns = @()
    $runCollectionComplete = $workflowRunsPayload.Status -eq 'available' -and
        (Test-CompleteCollection -Value $workflowRunsPayload.Value -CollectionName workflow_runs)
    if ($null -ne $workflowRunsPayload.Value -and $workflowRunsPayload.Value.PSObject.Properties.Name -contains 'workflow_runs') {
        $rawRuns = @($workflowRunsPayload.Value.workflow_runs)
    }
    elseif ($workflowRunsPayload.Status -ne 'available' -and -not $AllowUnavailable) {
        throw "CI flake-rate metadata is unavailable: $availabilityStatus. $availabilityDetail"
    }

    $actionsEnabled = $null
    $allowedActions = ""
    $actionsReadinessStatus = $actionsPermissionsPayload.Status
    $actionsReadinessDetail = $actionsPermissionsPayload.Detail
    if ($actionsPermissionsPayload.Status -eq "available") {
        $actionsEnabled = [bool]$actionsPermissionsPayload.Value.enabled
        $allowedActions = [string]$actionsPermissionsPayload.Value.allowed_actions
        if ($actionsEnabled) {
            $actionsReadinessStatus = "actions-enabled"
            $actionsReadinessDetail = "GitHub Actions is enabled for the repository."
        }
        else {
            $actionsReadinessStatus = "actions-disabled"
            $actionsReadinessDetail = "GitHub Actions is disabled for the repository."
        }
    }

    $workflowDefinitions = @()
    $matchingWorkflowDefinitions = @()
    $missingWorkflowNames = @()
    $inactiveWorkflowNames = @()
    $missingWorkflowDispatchNames = @()
    $workflowReadinessStatus = $workflowDefinitionsPayload.Status
    $workflowReadinessDetail = $workflowDefinitionsPayload.Detail
    $workflowDispatchReadinessStatus = $workflowDefinitionsPayload.Status
    $workflowDispatchReadinessDetail = $workflowDefinitionsPayload.Detail
    $workflowCollectionComplete = $workflowDefinitionsPayload.Status -eq 'available' -and
        (Test-CompleteCollection -Value $workflowDefinitionsPayload.Value -CollectionName workflows)
    if ($workflowCollectionComplete) {
        $workflowDefinitions = @(
            @($workflowDefinitionsPayload.Value.workflows) |
                ForEach-Object {
                    $definition = Convert-ToNormalizedWorkflowDefinition -Workflow $_
                    if ($null -eq $definition.DispatchConfigured) {
                        $definition.DispatchConfigured = Test-WorkflowDispatchConfigured -Workflow $definition -RepoRoot $repoRoot
                    }
                    $definition
                } |
                Sort-Object Name
        )
        $matchingWorkflowDefinitions = @(
            $workflowDefinitions |
                Where-Object { Test-WorkflowNameIncluded -Name $_.Name -IncludedNames $WorkflowName }
        )
        $workflowDefinitionNames = @($workflowDefinitions | ForEach-Object { $_.Name })
        $missingWorkflowNames = @(
            $WorkflowName |
                Where-Object {
                    $workflowNameToFind = $_
                    -not @($workflowDefinitionNames | Where-Object { $_.Equals($workflowNameToFind, [System.StringComparison]::OrdinalIgnoreCase) }).Count
                }
        )
        $inactiveWorkflowNames = @(
            $matchingWorkflowDefinitions |
                Where-Object { -not [string]::Equals($_.State, "active", [System.StringComparison]::OrdinalIgnoreCase) } |
                ForEach-Object { $_.Name }
        )
        $missingWorkflowDispatchNames = @(
            $matchingWorkflowDefinitions |
                Where-Object { -not [bool]$_.DispatchConfigured } |
                ForEach-Object { $_.Name }
        )

        if ($missingWorkflowNames.Count -gt 0) {
            $workflowReadinessStatus = "missing-workflows"
            $workflowReadinessDetail = "One or more configured workflow names were not found in repository workflow metadata."
        }
        elseif ($inactiveWorkflowNames.Count -gt 0) {
            $workflowReadinessStatus = "inactive-workflows"
            $workflowReadinessDetail = "One or more configured workflow names are present but not active."
        }
        else {
            $workflowReadinessStatus = "active-workflows"
            $workflowReadinessDetail = "All configured workflow names are present and active."
        }

        if ($missingWorkflowDispatchNames.Count -gt 0) {
            $workflowDispatchReadinessStatus = "workflow-dispatch-missing"
            $workflowDispatchReadinessDetail = "One or more configured workflow names are missing workflow_dispatch."
        }
        else {
            $workflowDispatchReadinessStatus = "workflow-dispatch-configured"
            $workflowDispatchReadinessDetail = "All configured workflow names include workflow_dispatch."
        }
    }

    elseif ($workflowDefinitionsPayload.Status -eq 'available') {
        $workflowReadinessStatus = 'incomplete-workflow-metadata'
        $workflowReadinessDetail = 'Workflow definitions have missing/duplicate IDs or do not match total_count.'
    }

    $normalizedRuns = @(
        $rawRuns |
            ForEach-Object { Convert-ToNormalizedWorkflowRun -Run $_ } |
            Where-Object { Test-WorkflowNameIncluded -Name $_.Name -IncludedNames $WorkflowName } |
            Sort-Object CreatedAtUtc
    )
    $allRuns = @($normalizedRuns | Where-Object {
        $createdAt = ([datetimeoffset]::Parse($_.CreatedAtUtc, [System.Globalization.CultureInfo]::InvariantCulture)).UtcDateTime
        $createdAt -ge $windowStartUtc -and $createdAt -le $WindowEndUtc.UtcDateTime
    } | Sort-Object Id -Unique | Sort-Object CreatedAtUtc)
    $completedRuns = @($allRuns | Where-Object { $_.Status -eq "completed" -and -not [string]::IsNullOrWhiteSpace($_.Conclusion) })
    $successfulRuns = @($completedRuns | Where-Object { $_.Conclusion -eq "success" })
    $failedRuns = @($completedRuns | Where-Object { $_.Conclusion -eq "failure" })
    $eligibleRuns = @($completedRuns | Where-Object { $_.Conclusion -in @('success', 'failure') })
    $excludedCompletedRuns = @($completedRuns | Where-Object { $_.Conclusion -notin @('success', 'failure') })

    $testFailureSignals = @{}
    $inspectedAttemptCount = 0
    $attemptInspectionUnavailableCount = 0
    $attemptInspectionUnattributedCount = 0
    $attemptInspections = @()
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

            $inspectionStatus = $attemptJobsPayload.Status
            if ($inspectionStatus -eq 'available' -and
                (Test-CompleteAttemptJobs -Value $attemptJobsPayload.Value)) {
                $inspectedAttemptCount++
                if (Test-HasTestFailureSignal -JobsPayload $attemptJobsPayload.Value -Pattern $TestFailurePattern) {
                    $testFailureSignals["$($run.Id):$attemptNumber"] = $true
                    $inspectionStatus = 'test-step-failure-signal'
                }
                else {
                    $attemptInspectionUnattributedCount++
                    $inspectionStatus = 'unattributed-attempt'
                }
            }
            else {
                $attemptInspectionUnavailableCount++
                if ($inspectionStatus -eq 'available') { $inspectionStatus = 'incomplete-attempt-jobs' }
            }
            $attemptInspections += [pscustomobject]@{
                RunId = $run.Id; Attempt = $attemptNumber; Status = $inspectionStatus; Detail = $attemptJobsPayload.Detail
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

        # Separate runs can use different inputs, events and environments even
        # at the same SHA. They neither resolve a failure nor prove a test flake.
    }

    $flakeEventCount = @($flakeEvents).Count
    $flakeRatePercent = if ($eligibleRuns.Count -eq 0) {
        $null
    }
    else {
        [decimal]::Round((([decimal]$flakeEventCount / [decimal]$eligibleRuns.Count) * 100), 4)
    }

    if ($workflowRunsPayload.Status -ne "available") {
        $availabilityStatus = $workflowRunsPayload.Status
    }
    elseif (-not $runCollectionComplete) {
        $availabilityStatus = 'incomplete-actions-history'
        $availabilityDetail = 'Run IDs/counts do not establish complete collection coverage.'
    }
    elseif ($eligibleRuns.Count -eq 0) {
        $availabilityStatus = "unavailable-no-actions-history"
        $availabilityDetail = "No matching success/failure workflow runs were available in the requested creation window."
    }
    elseif ($eligibleRuns.Count -lt $MinimumCompletedRunCount) {
        $availabilityStatus = "insufficient-actions-history"
        $availabilityDetail = "Only $($eligibleRuns.Count) eligible workflow run(s) were available; $MinimumCompletedRunCount are required for descriptive assessment."
    }
    else {
        $availabilityStatus = "measured"
        $availabilityDetail = "Completed matching GitHub Actions workflow runs were available for the requested window."
    }

    $workflowCoverage = @($WorkflowName | ForEach-Object {
        $name = $_
        [pscustomobject]@{ WorkflowName = $name; EligibleRunCount = @($eligibleRuns | Where-Object Name -eq $name).Count }
    })
    $assessmentBlockers = @(
        if (-not $runCollectionComplete) { 'incomplete-run-collection' }
        if ($attemptInspectionUnavailableCount -gt 0) { 'incomplete-attempt-inspection' }
        if ($attemptInspectionUnattributedCount -gt 0) { 'unattributed-attempts' }
        if ($failedRuns.Count -gt 0) { 'unresolved-failed-runs' }
        if ($excludedCompletedRuns.Count -gt 0) { 'excluded-terminal-outcomes' }
        if ($allRuns.Count -gt $completedRuns.Count) { 'unfinished-runs' }
        if ($actionsReadinessStatus -ne 'actions-enabled') { 'actions-readiness-unverified' }
        if ($workflowReadinessStatus -ne 'active-workflows') { 'workflow-readiness-unverified' }
        if ($eligibleRuns.Count -lt $MinimumCompletedRunCount -or
            @($workflowCoverage | Where-Object EligibleRunCount -lt $MinimumCompletedRunCount).Count -gt 0) { 'insufficient-workflow-history' }
        if ($null -ne $flakeRatePercent -and $flakeRatePercent -gt $TargetFlakeRatePercent) { 'observed-rate-over-target' }
    )
    # Run/step metadata cannot establish per-test identity, statistical confidence,
    # or independent representative samples. A clean observed rate is not an SLO.
    $promotionAllowed = $false
    $readinessBlockerClass = if ($actionsReadinessStatus -eq "actions-disabled") {
        "actions-disabled"
    }
    elseif ($workflowReadinessStatus -eq "missing-workflows" -or $workflowReadinessStatus -eq "inactive-workflows") {
        "workflow-metadata-not-ready"
    }
    elseif ($workflowDispatchReadinessStatus -eq "workflow-dispatch-missing") {
        "workflow-dispatch-not-ready"
    }
    elseif ($availabilityStatus -eq "unavailable-no-actions-history") {
        "no-completed-actions-history"
    }
    elseif ($availabilityStatus -eq "insufficient-actions-history") {
        "insufficient-actions-history"
    }
    elseif ($availabilityStatus -eq "measured") {
        "none"
    }
    else {
        $availabilityStatus
    }

    $status = if ($availabilityStatus -eq 'measured' -and $flakeRatePercent -gt $TargetFlakeRatePercent) {
        "measured-over-target"
    }
    elseif ($availabilityStatus -eq 'measured' -and $assessmentBlockers.Count -gt 0) {
        'measured-with-blockers'
    }
    elseif ($availabilityStatus -eq 'measured') { 'measured-within-target' }
    elseif ($availabilityStatus -eq "insufficient-actions-history") {
        "pending-insufficient-history"
    }
    else {
        "pending-$availabilityStatus"
    }

    $report = [pscustomobject]([ordered]@{
        '$schemaVersion' = "2.0.0"
        Status = $status
        SliId = "engine.tests.flake-rate.7d"
        GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        Repository = $Repository
        WindowDays = $WindowDays
        WindowStartUtc = $windowStartUtc.ToString("O")
        WindowEndUtc = $WindowEndUtc.ToUniversalTime().ToString('O')
        WindowBasis = 'run-created-at; outcomes observed at collection time'
        WorkflowNames = $WorkflowName
        TargetFlakeRatePercent = $TargetFlakeRatePercent
        MinimumCompletedRunCount = $MinimumCompletedRunCount
        AvailabilityStatus = $availabilityStatus
        AvailabilityDetail = $availabilityDetail
        ActionsReadinessStatus = $actionsReadinessStatus
        ActionsReadinessDetail = $actionsReadinessDetail
        ActionsEnabled = $actionsEnabled
        AllowedActions = $allowedActions
        WorkflowReadinessStatus = $workflowReadinessStatus
        WorkflowReadinessDetail = $workflowReadinessDetail
        MatchingWorkflowCount = $matchingWorkflowDefinitions.Count
        ActiveWorkflowCount = @($matchingWorkflowDefinitions | Where-Object { [string]::Equals($_.State, "active", [System.StringComparison]::OrdinalIgnoreCase) }).Count
        WorkflowDispatchReadinessStatus = $workflowDispatchReadinessStatus
        WorkflowDispatchReadinessDetail = $workflowDispatchReadinessDetail
        DispatchConfiguredWorkflowCount = @($matchingWorkflowDefinitions | Where-Object { [bool]$_.DispatchConfigured }).Count
        MissingWorkflowNames = $missingWorkflowNames
        InactiveWorkflowNames = $inactiveWorkflowNames
        MissingWorkflowDispatchNames = $missingWorkflowDispatchNames
        ReadinessBlockerClass = $readinessBlockerClass
        TotalRunCount = $allRuns.Count
        CompletedRunCount = $completedRuns.Count
        EligibleRunCount = $eligibleRuns.Count
        ExcludedCompletedRunCount = $excludedCompletedRuns.Count
        IncompleteRunCount = $allRuns.Count - $completedRuns.Count
        OutOfWindowRunCount = $normalizedRuns.Count - @($normalizedRuns | Where-Object {
            $createdAt = ([datetimeoffset]::Parse($_.CreatedAtUtc, [System.Globalization.CultureInfo]::InvariantCulture)).UtcDateTime
            $createdAt -ge $windowStartUtc -and $createdAt -le $WindowEndUtc.UtcDateTime
        }).Count
        RunCollectionComplete = $runCollectionComplete
        RunCollectionDetail = $workflowRunsPayload.Detail
        CollectedRunCount = $rawRuns.Count
        ObservationComplete = $runCollectionComplete -and $attemptInspectionUnavailableCount -eq 0 -and $attemptInspectionUnattributedCount -eq 0
        SuccessfulRunCount = $successfulRuns.Count
        FailedRunCount = $failedRuns.Count
        InspectedAttemptCount = $inspectedAttemptCount
        AttemptInspectionUnavailableCount = $attemptInspectionUnavailableCount
        AttemptInspectionUnattributedCount = $attemptInspectionUnattributedCount
        AttemptInspections = $attemptInspections
        FlakeEventCount = $flakeEventCount
        FlakeRatePercent = $flakeRatePercent
        PromotionAllowed = $promotionAllowed
        PromotionBlockers = @($assessmentBlockers) + @('statistical-and-test-level-review-required')
        AssessmentBlockers = $assessmentBlockers
        ReadyForStatisticalReview = $assessmentBlockers.Count -eq 0
        MetricKind = 'observed-run-level-test-step-recovery-rate'
        TestFailurePattern = $TestFailurePattern
        WorkflowCoverage = $workflowCoverage
        FlakeEvents = $flakeEvents
        WorkflowDefinitions = $matchingWorkflowDefinitions
        ObservedRuns = $allRuns
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
        throw "CI flake-rate evidence is not promotable: $($report.PromotionBlockers -join ', '). Run-level metadata requires independent statistical and test-level review."
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
        -WindowEndUtc $WindowEndUtc `
        -WorkflowName $WorkflowName `
        -WorkflowRunsJsonPath $WorkflowRunsJsonPath `
        -ActionsPermissionsJsonPath $ActionsPermissionsJsonPath `
        -WorkflowsJsonPath $WorkflowsJsonPath `
        -AttemptJobsDirectory $AttemptJobsDirectory `
        -OutputPath $OutputPath `
        -TestFailurePattern $TestFailurePattern `
        -AllowUnavailable:$AllowUnavailable `
        -RequirePromotion:$RequirePromotion
}
