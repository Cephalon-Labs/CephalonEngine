#requires -Version 7.0

[CmdletBinding()]
param(
    [string]$RepoFullName,
    [string]$BacklogPath = "docs/engine-backlog.md",
    [string]$IssueListJsonPath,
    [int]$IssueLimit = 200
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepositoryFullName {
    param([string]$ExplicitRepo)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitRepo)) {
        return $ExplicitRepo
    }

    $originUrl = (& git remote get-url origin).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($originUrl)) {
        throw "Unable to determine repository name. Pass -RepoFullName explicitly."
    }

    if ($originUrl -match "github\.com[:/](?<owner>[^/]+)/(?<repo>[^/.]+)(?:\.git)?$") {
        return "$($Matches.owner)/$($Matches.repo)"
    }

    throw "Unable to parse GitHub repository from remote URL '$originUrl'."
}

function Import-IssueList {
    param(
        [string]$Repo,
        [string]$JsonPath,
        [int]$Limit
    )

    if (-not [string]::IsNullOrWhiteSpace($JsonPath)) {
        if (-not (Test-Path -LiteralPath $JsonPath)) {
            throw "Issue list JSON path '$JsonPath' does not exist."
        }

        $json = Get-Content -LiteralPath $JsonPath -Raw -Encoding utf8
        if ([string]::IsNullOrWhiteSpace($json)) {
            return @()
        }

        return @($json | ConvertFrom-Json)
    }

    $resolvedRepo = Get-RepositoryFullName -ExplicitRepo $Repo
    $output = & gh issue list `
        --repo $resolvedRepo `
        --state open `
        --limit $Limit `
        --json number,title,state,url,labels

    if ($LASTEXITCODE -ne 0) {
        throw "gh issue list failed for '$resolvedRepo'."
    }

    if ([string]::IsNullOrWhiteSpace($output)) {
        return @()
    }

    return @($output | ConvertFrom-Json)
}

function Get-IssueLabelNames {
    param($Issue)

    $names = @()
    foreach ($label in @($Issue.labels)) {
        if ($null -eq $label) {
            continue
        }

        if ($label -is [string]) {
            $names += $label
            continue
        }

        if ($label.PSObject.Properties.Name -contains "name") {
            $names += [string]$label.name
        }
    }

    return $names
}

function Get-IssueEngIds {
    param($Issue)

    $ids = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $title = if ($Issue.PSObject.Properties.Name -contains "title") { [string]$Issue.title } else { "" }
    foreach ($match in [regex]::Matches($title, "\bENG-\d+\b", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        [void]$ids.Add($match.Value.ToUpperInvariant())
    }

    foreach ($labelName in Get-IssueLabelNames -Issue $Issue) {
        if ($labelName -match "^track:(eng-\d+)$") {
            [void]$ids.Add($Matches[1].ToUpperInvariant())
        }
    }

    return @($ids)
}

function Get-BacklogIssueRows {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Backlog path '$Path' does not exist."
    }

    $lines = Get-Content -LiteralPath $Path -Encoding utf8
    $rows = @{}

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notmatch "^###\s+(?<id>ENG-\d+)\s+(?<title>.+)$") {
            continue
        }

        $id = $Matches.id.ToUpperInvariant()
        $title = $Matches.title.Trim()
        $status = $null
        $githubIssueNumber = $null

        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            if ($lines[$j] -match "^###\s+ENG-\d+\s+") {
                break
            }

            if ($lines[$j] -match "^Status:\s*(?<status>.+)$") {
                $status = $Matches.status.Trim()
                continue
            }

            if ($lines[$j] -match "^(GitHub issue|Issue):\s*#?(?<issue>\d+)\s*$") {
                $githubIssueNumber = [int]$Matches.issue
                continue
            }
        }

        $rows[$id] = [pscustomobject]@{
            Id = $id
            Title = $title
            Status = if ($null -ne $status) { $status } else { "" }
            GitHubIssueNumber = $githubIssueNumber
            Line = $i + 1
        }
    }

    return $rows
}

function Test-PlanningGitHubIssueState {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$BacklogRows,
        [Parameter(Mandatory = $true)]
        [object[]]$Issues
    )

    $issueRows = @()
    foreach ($issue in $Issues) {
        $issueNumber = [int]$issue.number
        $issueTitle = [string]$issue.title
        $issueUrl = if ($issue.PSObject.Properties.Name -contains "url") { [string]$issue.url } else { "" }
        foreach ($engId in Get-IssueEngIds -Issue $issue) {
            $backlogRow = if ($BacklogRows.ContainsKey($engId)) { $BacklogRows[$engId] } else { $null }
            $issueRows += [pscustomobject]@{
                EngId = $engId
                IssueNumber = $issueNumber
                Title = $issueTitle
                Url = $issueUrl
                BacklogRow = $backlogRow
            }
        }
    }

    $errors = @()
    $warnings = @()

    foreach ($group in $issueRows | Group-Object EngId) {
        $uniqueIssueNumbers = @($group.Group | Select-Object -ExpandProperty IssueNumber -Unique | Sort-Object)
        if ($uniqueIssueNumbers.Count -gt 1) {
            $errors += "Duplicate open GitHub issues track $($group.Name): #$($uniqueIssueNumbers -join ', #')."
        }
    }

    foreach ($row in $issueRows) {
        if ($null -eq $row.BacklogRow) {
            $warnings += "Open GitHub issue #$($row.IssueNumber) tracks $($row.EngId), but no matching backlog row was found."
            continue
        }

        $status = [string]$row.BacklogRow.Status
        $isClosedStatus = $status -match "^(done|shipped)\b"
        if (-not $isClosedStatus) {
            continue
        }

        $expectedIssue = $row.BacklogRow.GitHubIssueNumber
        if ($null -eq $expectedIssue) {
            $errors += "Backlog row $($row.EngId) is '$status' but open GitHub issue #$($row.IssueNumber) still tracks it and the row has no GitHub issue number."
            continue
        }

        if ($row.IssueNumber -ne $expectedIssue) {
            $errors += "Backlog row $($row.EngId) is '$status' at GitHub issue #$expectedIssue, but open GitHub issue #$($row.IssueNumber) also tracks it."
            continue
        }

        $warnings += "Backlog row $($row.EngId) is '$status' and its GitHub issue #$($row.IssueNumber) is still open; close it during the same closeout."
    }

    return [pscustomobject]@{
        OpenIssueCount = @($Issues).Count
        TrackedOpenIssueCount = @($issueRows).Count
        ErrorCount = $errors.Count
        WarningCount = $warnings.Count
        Errors = $errors
        Warnings = $warnings
    }
}

if (-not $env:CEPHALON_VALIDATE_PLANNING_GITHUB_ISSUES_NO_RUN) {
    $backlogRows = Get-BacklogIssueRows -Path $BacklogPath
    $issues = Import-IssueList -Repo $RepoFullName -JsonPath $IssueListJsonPath -Limit $IssueLimit
    $result = Test-PlanningGitHubIssueState -BacklogRows $backlogRows -Issues $issues

    foreach ($warning in $result.Warnings) {
        Write-Warning $warning
    }

    if ($result.ErrorCount -gt 0) {
        foreach ($errorText in $result.Errors) {
            Write-Error $errorText -ErrorAction Continue
        }

        throw "Planning GitHub issue validation failed with $($result.ErrorCount) error(s)."
    }

    Write-Host ("Planning GitHub issue validation passed: {0} open issues, {1} tracked ENG issue rows, {2} warning(s)." -f `
        $result.OpenIssueCount,
        $result.TrackedOpenIssueCount,
        $result.WarningCount)
}
