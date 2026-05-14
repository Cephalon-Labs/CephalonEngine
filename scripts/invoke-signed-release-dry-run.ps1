param(
    [string]$Repository = "Cephalon-Labs/CephalonEngine",
    [string]$WorkflowName = "Publish Release",
    [string]$Ref = "master",
    [string]$OutputPath = "artifacts/signed-release-dry-run",
    [switch]$SkipDispatch,
    [switch]$RequireRunCreated
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

function New-GitHubCliResult {
    param(
        [Parameter(Mandatory = $true)]
        [int]$ExitCode,
        [AllowNull()]
        [string]$Output
    )

    return [pscustomobject]@{
        ExitCode = $ExitCode
        Output   = if ($null -eq $Output) { "" } else { $Output.Trim() }
    }
}

function Invoke-GitHubCli {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [AllowNull()]
        [scriptblock]$GitHubCliInvoker
    )

    if ($null -ne $GitHubCliInvoker) {
        $result = & $GitHubCliInvoker $Arguments
        if ($null -eq $result) {
            return New-GitHubCliResult -ExitCode 1 -Output "Custom gh invoker returned no result."
        }

        return New-GitHubCliResult -ExitCode ([int]$result.ExitCode) -Output ([string]$result.Output)
    }

    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if ($null -eq $gh) {
        return New-GitHubCliResult -ExitCode 127 -Output "gh CLI was not found."
    }

    $output = & gh @Arguments 2>&1
    return New-GitHubCliResult -ExitCode $LASTEXITCODE -Output (($output | Out-String).Trim())
}

function ConvertFrom-JsonOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Output,
        [Parameter(Mandatory = $true)]
        [string]$Context
    )

    try {
        return $Output | ConvertFrom-Json -Depth 32
    }
    catch {
        throw "Unable to parse $Context JSON from gh output: $($_.Exception.Message)"
    }
}

function Resolve-SignedReleaseDryRunBlockerClass {
    param(
        [AllowNull()]
        [string]$Output
    )

    if ([string]::IsNullOrWhiteSpace($Output)) {
        return "dispatch-failed"
    }

    if ($Output.Contains("Actions has been disabled for this user", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "dispatch-identity-actions-disabled"
    }

    if ($Output.Contains("workflow_dispatch", [System.StringComparison]::OrdinalIgnoreCase) -and
        $Output.Contains("does not have", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "workflow-dispatch-not-enabled"
    }

    if ($Output.Contains("Resource not accessible by integration", [System.StringComparison]::OrdinalIgnoreCase) -or
        $Output.Contains("requires workflow scope", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "dispatch-token-permission-denied"
    }

    if ($Output.Contains("Not Found", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "workflow-not-found-or-inaccessible"
    }

    return "dispatch-failed"
}

function Test-SignedReleaseDryRunWorkflowDispatchDeclaration {
    param(
        [AllowNull()]
        [string]$WorkflowPath,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    if ([string]::IsNullOrWhiteSpace($WorkflowPath)) {
        return $null
    }

    $resolvedWorkflowPath = Resolve-RepoPath -Path $WorkflowPath -RepoRoot $RepoRoot
    if (-not (Test-Path -LiteralPath $resolvedWorkflowPath -PathType Leaf)) {
        return $null
    }

    $workflowContent = Get-Content -LiteralPath $resolvedWorkflowPath -Raw -Encoding UTF8
    return $workflowContent.Contains("workflow_dispatch", [System.StringComparison]::OrdinalIgnoreCase)
}

function Resolve-SignedReleaseDryRunBlockerScope {
    param(
        [AllowNull()]
        [string]$BlockerClass
    )

    if ([string]::IsNullOrWhiteSpace($BlockerClass)) {
        return "none"
    }

    switch ($BlockerClass) {
        "dispatch-identity-actions-disabled" { return "identity" }
        "dispatch-token-permission-denied" { return "token" }
        "repository-actions-disabled" { return "repository" }
        "repository-actions-permission-read-failed" { return "repository" }
        "workflow-dispatch-not-enabled" { return "workflow" }
        "workflow-inactive" { return "workflow" }
        "workflow-list-failed" { return "workflow" }
        "workflow-not-found" { return "workflow" }
        "workflow-not-found-or-inaccessible" { return "workflow" }
        default { return "unknown" }
    }
}

function Resolve-SignedReleaseDryRunPrerequisitesStatus {
    param(
        [AllowNull()]
        [string]$Status,
        [AllowNull()]
        [string]$BlockerClass,
        [AllowNull()]
        [object]$RepositoryWorkflowDispatchReady,
        [bool]$SkipDispatch,
        [bool]$RunCreated,
        [AllowNull()]
        [string]$RunLookupStatus
    )

    if ($Status -eq "submitted" -and $RunCreated) {
        return "workflow-dispatch-submitted-run-found"
    }

    if ($Status -eq "submitted") {
        return "workflow-dispatch-submitted-$RunLookupStatus"
    }

    if ($Status -eq "ready" -and $SkipDispatch) {
        return "repository-and-workflow-ready-dispatch-skipped"
    }

    if ($RepositoryWorkflowDispatchReady -eq $true) {
        switch ($BlockerClass) {
            "dispatch-identity-actions-disabled" { return "repository-and-workflow-ready-identity-blocked" }
            "dispatch-token-permission-denied" { return "repository-and-workflow-ready-token-blocked" }
            "workflow-dispatch-not-enabled" { return "workflow-dispatch-not-enabled" }
            "workflow-not-found-or-inaccessible" { return "repository-and-workflow-ready-workflow-inaccessible" }
            "dispatch-failed" { return "repository-and-workflow-ready-dispatch-failed" }
            { -not [string]::IsNullOrWhiteSpace($_) } { return "repository-and-workflow-ready-blocked" }
            default { return "repository-and-workflow-ready" }
        }
    }

    switch ($BlockerClass) {
        "repository-actions-disabled" { return "repository-actions-disabled" }
        "repository-actions-permission-read-failed" { return "repository-actions-permission-read-failed" }
        "workflow-dispatch-not-enabled" { return "workflow-dispatch-not-enabled" }
        "workflow-inactive" { return "workflow-inactive" }
        "workflow-list-failed" { return "workflow-list-failed" }
        "workflow-not-found" { return "workflow-not-found" }
        default {
            if ($Status -eq "blocked") {
                return "dispatch-prerequisites-blocked"
            }

            return "dispatch-prerequisites-unknown"
        }
    }
}

function Resolve-SignedReleaseDryRunDiagnostic {
    param(
        [AllowNull()]
        [string]$Status,
        [AllowNull()]
        [string]$BlockerClass,
        [AllowNull()]
        [string]$DispatchActor,
        [AllowNull()]
        [object]$RepositoryWorkflowDispatchReady,
        [AllowNull()]
        [object]$WorkflowDispatchDeclared,
        [bool]$SkipDispatch,
        [bool]$RunCreated,
        [AllowNull()]
        [string]$RunLookupStatus
    )

    $actor = if ([string]::IsNullOrWhiteSpace($DispatchActor)) { "the dispatching GitHub identity" } else { "dispatch identity '$DispatchActor'" }

    if ($Status -eq "submitted" -and $RunCreated) {
        return "The Publish Release workflow dry-run dispatch was submitted and a workflow run URL was found."
    }

    if ($Status -eq "submitted") {
        return "The Publish Release workflow dry-run dispatch was submitted, but run lookup finished with '$RunLookupStatus'."
    }

    if ($Status -eq "ready" -and $SkipDispatch) {
        return "Repository Actions are enabled, the Publish Release workflow is active, workflow_dispatch is declared, and dispatch was skipped by request."
    }

    if ($RepositoryWorkflowDispatchReady -eq $true -and $BlockerClass -eq "dispatch-identity-actions-disabled") {
        return "Repository Actions are enabled and the Publish Release workflow is active with workflow_dispatch declared; dry-run proof is blocked by $actor."
    }

    if ($RepositoryWorkflowDispatchReady -eq $true -and $BlockerClass -eq "dispatch-token-permission-denied") {
        return "Repository Actions and workflow dispatch prerequisites are ready, but the dispatch token cannot create the workflow_dispatch run."
    }

    switch ($BlockerClass) {
        "repository-actions-disabled" {
            return "Repository Actions permissions are disabled, so workflow dispatch cannot be attempted."
        }
        "repository-actions-permission-read-failed" {
            return "Repository Actions permission readback failed, so dispatch prerequisites cannot be trusted."
        }
        "workflow-inactive" {
            return "The Publish Release workflow exists but is not active."
        }
        "workflow-not-found" {
            return "The Publish Release workflow was not found in repository workflow metadata."
        }
        "workflow-dispatch-not-enabled" {
            return "The Publish Release workflow does not expose workflow_dispatch for dry-run dispatch."
        }
        "workflow-not-found-or-inaccessible" {
            return "The Publish Release workflow was not found or is inaccessible to the dispatch identity."
        }
        "workflow-list-failed" {
            return "Workflow metadata readback failed, so dispatch prerequisites cannot be trusted."
        }
        { -not [string]::IsNullOrWhiteSpace($_) } {
            return "The Publish Release workflow dry-run dispatch is blocked by '$BlockerClass'."
        }
        default {
            if ($WorkflowDispatchDeclared -eq $false) {
                return "The Publish Release workflow file does not declare workflow_dispatch."
            }

            return "Signed-release dry-run readiness could not produce a more specific diagnostic."
        }
    }
}

function Resolve-SignedReleaseDryRunRequiredAction {
    param(
        [AllowNull()]
        [string]$Status,
        [AllowNull()]
        [string]$BlockerClass,
        [AllowNull()]
        [string]$DispatchActor
    )

    $actor = if ([string]::IsNullOrWhiteSpace($DispatchActor)) { "the dispatching GitHub identity" } else { "dispatch identity '$DispatchActor'" }

    switch ($BlockerClass) {
        "dispatch-identity-actions-disabled" {
            return "Enable GitHub Actions for $actor or rerun the probe with an Actions-enabled release-manager identity."
        }
        "dispatch-token-permission-denied" {
            return "Rerun the probe with a release-manager token that can dispatch workflows for the repository."
        }
        "repository-actions-disabled" {
            return "Enable repository Actions permissions for the repository before rerunning the signed-release dry-run probe."
        }
        "workflow-inactive" {
            return "Activate the Publish Release workflow before rerunning the signed-release dry-run probe."
        }
        "workflow-not-found" {
            return "Restore the Publish Release workflow at .github/workflows/publish-release.yml before rerunning the signed-release dry-run probe."
        }
        "workflow-dispatch-not-enabled" {
            return "Restore workflow_dispatch support on the Publish Release workflow before rerunning the signed-release dry-run probe."
        }
        "workflow-not-found-or-inaccessible" {
            return "Verify the workflow name, repository, and release-manager workflow permissions before rerunning the signed-release dry-run probe."
        }
        { -not [string]::IsNullOrWhiteSpace($_) } {
            return "Resolve blocker '$BlockerClass' before rerunning the signed-release dry-run probe with -RequireRunCreated."
        }
        default {
            if ($Status -eq "submitted") {
                return "Attach the generated report, workflow RunUrl, and artifact summary to release-readiness evidence."
            }

            return "Run pwsh ./scripts/invoke-signed-release-dry-run.ps1 -RequireRunCreated with an Actions-enabled release-manager identity before claiming signed-release proof."
        }
    }
}

function Format-SignedReleaseDryRunHandoffValue {
    param(
        [AllowNull()]
        [object]$Value
    )

    if ($null -eq $Value) {
        return "_not recorded_"
    }

    $text = [string]$Value
    if ([string]::IsNullOrWhiteSpace($text)) {
        return "_not recorded_"
    }

    return $text.Trim()
}

function New-SignedReleaseDryRunHandoffMarkdown {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Report,
        [Parameter(Mandatory = $true)]
        [string]$JsonPath
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Signed-release dry-run handoff")
    $lines.Add("")
    $lines.Add("Generated at UTC: $(Format-SignedReleaseDryRunHandoffValue -Value $Report.GeneratedAtUtc)")
    $lines.Add("")
    $lines.Add("## Status")
    $lines.Add("")
    $lines.Add("- Status: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.Status)``")
    $lines.Add("- BlockerClass: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.BlockerClass)``")
    $lines.Add("- DispatchBlockerScope: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.DispatchBlockerScope)``")
    $lines.Add("- RunCreated: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.RunCreated)``")
    $lines.Add("- RunUrl: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.RunUrl)``")
    $lines.Add("- RunLookupStatus: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.RunLookupStatus)``")
    $lines.Add("")
    $lines.Add("## Diagnostics")
    $lines.Add("")
    $lines.Add("- RepositoryWorkflowDispatchReady: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.RepositoryWorkflowDispatchReady)``")
    $lines.Add("- WorkflowDispatchDeclared: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.WorkflowDispatchDeclared)``")
    $lines.Add("- WorkflowDispatchPrerequisitesStatus: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.WorkflowDispatchPrerequisitesStatus)``")
    $lines.Add("- ReadinessDiagnostic: $(Format-SignedReleaseDryRunHandoffValue -Value $Report.ReadinessDiagnostic)")
    $lines.Add("")
    $lines.Add("## Dispatch")
    $lines.Add("")
    $lines.Add("- DispatchActor: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.DispatchActor)``")
    $lines.Add("- DispatchIdentityStatus: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.DispatchIdentityStatus)``")
    $lines.Add("- DispatchAttempted: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.DispatchAttempted)``")
    $lines.Add("- DispatchCommand: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.DispatchCommand)``")
    $lines.Add("- RequiredReleaseManagerAction: $(Format-SignedReleaseDryRunHandoffValue -Value $Report.RequiredReleaseManagerAction)")
    $lines.Add("")
    $lines.Add("## Evidence")
    $lines.Add("")
    $lines.Add("- Repository: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.Repository)``")
    $lines.Add("- WorkflowName: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.WorkflowName)``")
    $lines.Add("- WorkflowId: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.WorkflowId)``")
    $lines.Add("- WorkflowPath: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.WorkflowPath)``")
    $lines.Add("- WorkflowState: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.WorkflowState)``")
    $lines.Add("- RepositoryActionsEnabled: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.RepositoryActionsEnabled)``")
    $lines.Add("- AllowedActions: ``$(Format-SignedReleaseDryRunHandoffValue -Value $Report.AllowedActions)``")
    $lines.Add("- JSON report: ``$JsonPath``")

    if (-not [string]::IsNullOrWhiteSpace([string]$Report.DispatchOutput)) {
        $lines.Add("")
        $lines.Add("## GitHub dispatch output")
        $lines.Add("")
        $lines.Add('```text')
        $lines.Add([string]$Report.DispatchOutput)
        $lines.Add('```')
    }

    return $lines -join [Environment]::NewLine
}

function Get-SignedReleaseWorkflow {
    param(
        [Parameter(Mandatory = $true)]
        [object]$WorkflowList,
        [Parameter(Mandatory = $true)]
        [string]$WorkflowName
    )

    $workflows = @($WorkflowList.workflows)
    $matches = @(
        $workflows |
            Where-Object {
                [string]$_.name -eq $WorkflowName -or
                [System.IO.Path]::GetFileName([string]$_.path) -eq $WorkflowName
            }
    )

    return $matches | Select-Object -First 1
}

function Get-LatestSignedReleaseDryRun {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Runs,
        [Parameter(Mandatory = $true)]
        [string]$Ref
    )

    $matches = @(
        $Runs |
            Where-Object {
                [string]$_.event -eq "workflow_dispatch" -and
                ([string]$_.headBranch -eq $Ref -or [string]$_.headSha -eq $Ref -or [string]$_.headBranch -eq "")
            } |
            Sort-Object -Property createdAt -Descending
    )

    return $matches | Select-Object -First 1
}

function Invoke-SignedReleaseDryRunReadiness {
    param(
        [string]$Repository = "Cephalon-Labs/CephalonEngine",
        [string]$WorkflowName = "Publish Release",
        [string]$Ref = "master",
        [string]$OutputPath = "artifacts/signed-release-dry-run",
        [switch]$SkipDispatch,
        [switch]$RequireRunCreated,
        [AllowNull()]
        [scriptblock]$GitHubCliInvoker
    )

    $repoRoot = Resolve-RepoRoot
    $resolvedOutputPath = Resolve-RepoPath -Path $OutputPath -RepoRoot $repoRoot
    New-Item -ItemType Directory -Path $resolvedOutputPath -Force | Out-Null
    $jsonPath = Join-Path $resolvedOutputPath "signed-release-dry-run-readiness.json"
    $handoffPath = Join-Path $resolvedOutputPath "signed-release-dry-run-handoff.md"

    $workflowListResult = Invoke-GitHubCli -Arguments @("api", "repos/$Repository/actions/workflows") -GitHubCliInvoker $GitHubCliInvoker
    $permissionsResult = Invoke-GitHubCli -Arguments @("api", "repos/$Repository/actions/permissions") -GitHubCliInvoker $GitHubCliInvoker
    $identityResult = Invoke-GitHubCli -Arguments @("api", "user", "--jq", ".login") -GitHubCliInvoker $GitHubCliInvoker

    $workflow = $null
    $workflowId = $null
    $workflowPath = $null
    $workflowState = "unknown"
    $workflowActive = $false
    $workflowDispatchDeclared = $null
    $repositoryActionsEnabled = $null
    $allowedActions = "unknown"
    $status = "ready"
    $blockerClass = $null
    $summary = "Publish Release dry-run dispatch prerequisites are ready."
    $dispatchActor = $null
    $dispatchIdentityStatus = "unresolved"

    if ($identityResult.ExitCode -eq 0 -and -not [string]::IsNullOrWhiteSpace($identityResult.Output)) {
        $dispatchActor = $identityResult.Output
        $dispatchIdentityStatus = "resolved"
    }

    if ($workflowListResult.ExitCode -ne 0) {
        $status = "blocked"
        $blockerClass = "workflow-list-failed"
        $summary = "Unable to list repository workflows with gh."
    }
    else {
        $workflowList = ConvertFrom-JsonOutput -Output $workflowListResult.Output -Context "workflow list"
        $workflow = Get-SignedReleaseWorkflow -WorkflowList $workflowList -WorkflowName $WorkflowName
        if ($null -eq $workflow) {
            $status = "blocked"
            $blockerClass = "workflow-not-found"
            $summary = "The Publish Release workflow was not found in repository workflow metadata."
        }
        else {
            $workflowId = [string]$workflow.id
            $workflowPath = [string]$workflow.path
            $workflowState = [string]$workflow.state
            $workflowActive = $workflowState -eq "active"
            $workflowDispatchDeclared = Test-SignedReleaseDryRunWorkflowDispatchDeclaration -WorkflowPath $workflowPath -RepoRoot $repoRoot
            if (-not $workflowActive) {
                $status = "blocked"
                $blockerClass = "workflow-inactive"
                $summary = "The Publish Release workflow is present but is not active."
            }
            elseif ($workflowDispatchDeclared -eq $false) {
                $status = "blocked"
                $blockerClass = "workflow-dispatch-not-enabled"
                $summary = "The Publish Release workflow does not declare workflow_dispatch."
            }
        }
    }

    if ($permissionsResult.ExitCode -eq 0) {
        $permissions = ConvertFrom-JsonOutput -Output $permissionsResult.Output -Context "repository actions permissions"
        $repositoryActionsEnabled = [bool]$permissions.enabled
        $allowedActions = [string]$permissions.allowed_actions
        if (-not $repositoryActionsEnabled -and $status -eq "ready") {
            $status = "blocked"
            $blockerClass = "repository-actions-disabled"
            $summary = "Repository Actions permissions are disabled."
        }
    }
    elseif ($status -eq "ready") {
        $status = "blocked"
        $blockerClass = "repository-actions-permission-read-failed"
        $summary = "Unable to read repository Actions permissions with gh."
    }

    $dispatchAttempted = $false
    $dispatchExitCode = $null
    $dispatchOutput = ""
    $runCreated = $false
    $runId = $null
    $runUrl = $null
    $runStatus = $null
    $runConclusion = $null
    $runHeadSha = $null
    $runCreatedAtUtc = $null
    $runLookupStatus = "not-run"
    $dispatchCommand = "gh workflow run `"$WorkflowName`" --repo $Repository --ref $Ref -f dry_run=true"
    $repositoryWorkflowDispatchReady = $repositoryActionsEnabled -eq $true -and $workflowActive -and $workflowDispatchDeclared -eq $true

    if ($status -eq "ready" -and -not $SkipDispatch) {
        $dispatchAttempted = $true
        $dispatchResult = Invoke-GitHubCli -Arguments @(
            "workflow",
            "run",
            $WorkflowName,
            "--repo",
            $Repository,
            "--ref",
            $Ref,
            "-f",
            "dry_run=true"
        ) -GitHubCliInvoker $GitHubCliInvoker
        $dispatchExitCode = $dispatchResult.ExitCode
        $dispatchOutput = $dispatchResult.Output

        if ($dispatchResult.ExitCode -ne 0) {
            $status = "blocked"
            $blockerClass = Resolve-SignedReleaseDryRunBlockerClass -Output $dispatchResult.Output
            $summary = "The Publish Release workflow could not be dispatched for a dry run."
        }
        else {
            $status = "submitted"
            $summary = "The Publish Release workflow dry-run dispatch was submitted."
            $runListResult = Invoke-GitHubCli -Arguments @(
                "run",
                "list",
                "--repo",
                $Repository,
                "--workflow",
                $WorkflowName,
                "--limit",
                "10",
                "--json",
                "databaseId,displayTitle,event,status,conclusion,createdAt,url,headBranch,headSha"
            ) -GitHubCliInvoker $GitHubCliInvoker

            if ($runListResult.ExitCode -eq 0) {
                $runs = @(ConvertFrom-JsonOutput -Output $runListResult.Output -Context "workflow run list")
                $run = Get-LatestSignedReleaseDryRun -Runs $runs -Ref $Ref
                if ($null -eq $run) {
                    $runLookupStatus = "submitted-run-not-yet-visible"
                }
                else {
                    $runCreated = $true
                    $runLookupStatus = "found"
                    $runId = [string]$run.databaseId
                    $runUrl = [string]$run.url
                    $runStatus = [string]$run.status
                    $runConclusion = [string]$run.conclusion
                    $runHeadSha = [string]$run.headSha
                    $runCreatedAtUtc = [string]$run.createdAt
                }
            }
            else {
                $runLookupStatus = "run-list-failed"
            }
        }
    }
    elseif ($status -eq "ready" -and $SkipDispatch) {
        $summary = "Publish Release dry-run dispatch prerequisites are ready; dispatch was skipped by request."
    }

    $requiredReleaseManagerAction = Resolve-SignedReleaseDryRunRequiredAction `
        -Status $status `
        -BlockerClass $blockerClass `
        -DispatchActor $dispatchActor

    $dispatchBlockerScope = Resolve-SignedReleaseDryRunBlockerScope -BlockerClass $blockerClass
    $workflowDispatchPrerequisitesStatus = Resolve-SignedReleaseDryRunPrerequisitesStatus `
        -Status $status `
        -BlockerClass $blockerClass `
        -RepositoryWorkflowDispatchReady $repositoryWorkflowDispatchReady `
        -SkipDispatch:$SkipDispatch `
        -RunCreated:$runCreated `
        -RunLookupStatus $runLookupStatus
    $readinessDiagnostic = Resolve-SignedReleaseDryRunDiagnostic `
        -Status $status `
        -BlockerClass $blockerClass `
        -DispatchActor $dispatchActor `
        -RepositoryWorkflowDispatchReady $repositoryWorkflowDispatchReady `
        -WorkflowDispatchDeclared $workflowDispatchDeclared `
        -SkipDispatch:$SkipDispatch `
        -RunCreated:$runCreated `
        -RunLookupStatus $runLookupStatus

    $report = [pscustomobject]([ordered]@{
        '$schemaVersion' = "1.2.0"
        Status = $status
        BlockerClass = $blockerClass
        DispatchBlockerScope = $dispatchBlockerScope
        Summary = $summary
        ReadinessDiagnostic = $readinessDiagnostic
        WorkflowDispatchPrerequisitesStatus = $workflowDispatchPrerequisitesStatus
        Repository = $Repository
        DispatchActor = $dispatchActor
        DispatchIdentityStatus = $dispatchIdentityStatus
        DispatchCommand = $dispatchCommand
        RequiredReleaseManagerAction = $requiredReleaseManagerAction
        WorkflowName = $WorkflowName
        WorkflowId = $workflowId
        WorkflowPath = $workflowPath
        WorkflowState = $workflowState
        WorkflowActive = $workflowActive
        WorkflowDispatchDeclared = $workflowDispatchDeclared
        Ref = $Ref
        DryRun = $true
        RepositoryActionsEnabled = $repositoryActionsEnabled
        RepositoryWorkflowDispatchReady = $repositoryWorkflowDispatchReady
        AllowedActions = $allowedActions
        DispatchAttempted = $dispatchAttempted
        DispatchExitCode = $dispatchExitCode
        DispatchOutput = $dispatchOutput
        RunCreated = $runCreated
        RunLookupStatus = $runLookupStatus
        RunId = $runId
        RunUrl = $runUrl
        RunStatus = $runStatus
        RunConclusion = $runConclusion
        RunHeadSha = $runHeadSha
        RunCreatedAtUtc = $runCreatedAtUtc
        GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    })

    $report | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $jsonPath -Encoding UTF8
    $handoffMarkdown = New-SignedReleaseDryRunHandoffMarkdown -Report $report -JsonPath $jsonPath
    $handoffMarkdown | Set-Content -LiteralPath $handoffPath -Encoding UTF8

    Write-Host ("Signed-release dry-run readiness: {0}; blocker {1}; run created {2}; report {3}; handoff {4}" -f `
            $status,
            ($(if ([string]::IsNullOrWhiteSpace($blockerClass)) { "none" } else { $blockerClass })),
            $runCreated,
            $jsonPath,
            $handoffPath)

    if ($RequireRunCreated -and -not $runCreated) {
        $detail = if ([string]::IsNullOrWhiteSpace($blockerClass)) { $status } else { "$status/$blockerClass" }
        throw "Signed-release dry-run workflow run was not created; readiness status is $detail."
    }

    return [pscustomobject]@{
        Report = $report
        JsonPath = $jsonPath
        HandoffPath = $handoffPath
    }
}

if (-not $env:CEPHALON_SIGNED_RELEASE_DRY_RUN_NO_RUN) {
    $null = Invoke-SignedReleaseDryRunReadiness `
        -Repository $Repository `
        -WorkflowName $WorkflowName `
        -Ref $Ref `
        -OutputPath $OutputPath `
        -SkipDispatch:$SkipDispatch `
        -RequireRunCreated:$RequireRunCreated
}
