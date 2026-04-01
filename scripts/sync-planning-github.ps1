[CmdletBinding()]
param(
    [string]$RepoFullName,
    [string]$RoadmapPath = "docs/engine-roadmap.md",
    [string]$BacklogPath = "docs/engine-backlog.md",
    [string]$ProjectOwner = "Cephalon-Labs",
    [int]$ProjectNumber = 2,
    [switch]$SkipProjectSync
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Set-GitHubTokenPreference {
    if (-not [string]::IsNullOrWhiteSpace($env:GH_TOKEN)) {
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($env:CEPHALON_PROJECT_TOKEN)) {
        $env:GH_TOKEN = $env:CEPHALON_PROJECT_TOKEN
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
        $env:GH_TOKEN = $env:GITHUB_TOKEN
    }
}

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

function New-TempFile {
    return [System.IO.Path]::GetTempFileName()
}

function Invoke-GhJson {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & gh @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "gh $($Arguments -join ' ') failed."
    }

    if ([string]::IsNullOrWhiteSpace($output)) {
        return $null
    }

    return $output | ConvertFrom-Json -Depth 100
}

function Invoke-GhApiJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Route,
        [string]$Method = "GET",
        $Body = $null
    )

    $arguments = @("api", $Route, "--method", $Method)
    $tempFile = $null

    try {
        if ($null -ne $Body) {
            $tempFile = New-TempFile
            $Body | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $tempFile -Encoding utf8
            $arguments += @("--input", $tempFile)
        }

        return Invoke-GhJson -Arguments $arguments
    }
    finally {
        if ($null -ne $tempFile -and (Test-Path -LiteralPath $tempFile)) {
            Remove-Item -LiteralPath $tempFile -Force -ErrorAction SilentlyContinue
        }
    }
}

function Invoke-GhNoJson {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & gh @Arguments | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "gh $($Arguments -join ' ') failed."
    }
}

function Get-SyncMarker {
    param([Parameter(Mandatory = $true)][string]$Key)

    return "<!-- planning-sync:key=$Key -->"
}

function Get-PhaseTitle {
    param([Parameter(Mandatory = $true)][int]$PhaseNumber, [Parameter(Mandatory = $true)][string]$Name)

    return "Phase $PhaseNumber - $Name"
}

function Get-RoadmapPhases {
    param([Parameter(Mandatory = $true)][string]$Path)

    $content = Get-Content -LiteralPath $Path -Raw
    $matches = [regex]::Matches(
        $content,
        "(?ms)^##\s+Phase\s+(?<number>\d+):\s+(?<name>[^\r\n]+)\r?\n(?<body>.*?)(?=^##\s+|\z)")

    $phases = @()
    foreach ($match in $matches) {
        $phaseNumber = [int]$match.Groups["number"].Value
        $phaseName = $match.Groups["name"].Value.Trim()
        $phaseBody = $match.Groups["body"].Value.Trim()
        $statusMatch = [regex]::Match($phaseBody, "(?m)^Status:\s*(?<status>.+?)\s*$")
        $status = if ($statusMatch.Success) { $statusMatch.Groups["status"].Value.Trim() } else { "" }
        $syncKey = "milestone:phase-$phaseNumber"
        $description = @(
            (Get-SyncMarker -Key $syncKey)
            "Synced from `docs/engine-roadmap.md`."
            ""
            "Roadmap phase: **$(Get-PhaseTitle -PhaseNumber $phaseNumber -Name $phaseName)**"
            ""
            $phaseBody
        ) -join "`n"

        $phases += [pscustomobject]@{
            Number = $phaseNumber
            Name = $phaseName
            Title = Get-PhaseTitle -PhaseNumber $phaseNumber -Name $phaseName
            Status = $status
            Body = $phaseBody
            Description = $description.Trim()
            SyncKey = $syncKey
        }
    }

    return $phases
}

function Get-BacklogIssueSpecs {
    param([Parameter(Mandatory = $true)][string]$Path)

    $phaseMap = @{
        "ENG-005" = 1
        "ENG-011" = 3
        "ENG-013" = 4
        "ENG-022" = 5
    }

    $content = Get-Content -LiteralPath $Path -Raw
    $sectionMatches = [regex]::Matches(
        $content,
        "(?ms)^##\s+(?<section>[^\r\n]+)\r?\n(?<body>.*?)(?=^##\s+|\z)")

    $specs = @()
    foreach ($sectionMatch in $sectionMatches) {
        $sectionTitle = $sectionMatch.Groups["section"].Value.Trim()
        $sectionBody = $sectionMatch.Groups["body"].Value
        $issueMatches = [regex]::Matches(
            $sectionBody,
            "(?ms)^###\s+(?<title>[^\r\n]+)\r?\n(?<body>.*?)(?=^###\s+|^##\s+|\z)")

        foreach ($issueMatch in $issueMatches) {
            $title = $issueMatch.Groups["title"].Value.Trim()
            if ($title -notmatch "^(ENG-\d+)") {
                continue
            }

            $body = $issueMatch.Groups["body"].Value.Trim()
            $statusMatch = [regex]::Match($body, "(?m)^Status:\s*(?<status>.+?)\s*$")
            $status = if ($statusMatch.Success) { $statusMatch.Groups["status"].Value.Trim() } else { "" }
            $syncId = $Matches[1].ToLowerInvariant()
            $syncKey = "backlog:$syncId"
            $state = if ($status -eq "done") { "closed" } else { "open" }
            $phaseNumber = $null
            $engCode = $Matches[1]
            if ($phaseMap.ContainsKey($engCode)) {
                $phaseNumber = $phaseMap[$engCode]
            }

            $issueBody = @(
                (Get-SyncMarker -Key $syncKey)
                "Synced from `docs/engine-backlog.md`."
                ""
                "Planning section: **$sectionTitle**"
                ""
                $body
            ) -join "`n"

            $specs += [pscustomobject]@{
                Title = $title
                Body = $issueBody.Trim()
                State = $state
                SyncKey = $syncKey
                PhaseNumber = $phaseNumber
            }
        }
    }

    return $specs
}

function Get-RoadmapIssueSpecs {
    param($RoadmapPhases)

    $phase2 = $RoadmapPhases | Where-Object { $_.Number -eq 2 } | Select-Object -First 1
    if ($null -eq $phase2) {
        throw "Unable to find roadmap phase 2 in docs/engine-roadmap.md."
    }

    $body = @(
        (Get-SyncMarker -Key "roadmap:phase-2")
        "Synced from `docs/engine-roadmap.md`."
        ""
        "Roadmap phase: **$($phase2.Title)**"
        ""
        $phase2.Body
    ) -join "`n"

    return @(
        [pscustomobject]@{
            Title = "Phase 2 operational hardening follow-through"
            Body = $body.Trim()
            State = "open"
            SyncKey = "roadmap:phase-2"
            PhaseNumber = 2
        }
    )
}

function Normalize-Text {
    param([AllowNull()][string]$Value)

    if ($null -eq $Value) {
        return ""
    }

    return ($Value -replace "`r`n", "`n").Trim()
}

function Get-IssueWebUrl {
    param([Parameter(Mandatory = $true)]$Issue)

    $htmlUrlProperty = $Issue.PSObject.Properties["html_url"]
    if ($null -ne $htmlUrlProperty -and -not [string]::IsNullOrWhiteSpace([string]$htmlUrlProperty.Value)) {
        return [string]$htmlUrlProperty.Value
    }

    return [string]$Issue.url
}

function Get-ProjectContext {
    param(
        [Parameter(Mandatory = $true)][string]$Owner,
        [Parameter(Mandatory = $true)][int]$ProjectNumber
    )

    $projectView = Invoke-GhJson -Arguments @("project", "view", $ProjectNumber.ToString(), "--owner", $Owner, "--format", "json")
    $fields = Invoke-GhJson -Arguments @("project", "field-list", $ProjectNumber.ToString(), "--owner", $Owner, "--format", "json")
    $items = Invoke-GhJson -Arguments @("project", "item-list", $ProjectNumber.ToString(), "--owner", $Owner, "-L", "200", "--format", "json")

    $statusField = $fields.fields | Where-Object { $_.name -eq "Status" } | Select-Object -First 1
    if ($null -eq $statusField) {
        throw "Unable to find the Status field for project $Owner/$ProjectNumber."
    }

    $statusOptions = @{}
    foreach ($option in $statusField.options) {
        $statusOptions[$option.name] = $option.id
    }

    $itemsByIssueNumber = @{}
    foreach ($item in $items.items) {
        if ($null -ne $item.content -and $item.content.type -eq "Issue" -and $null -ne $item.content.number) {
            $itemsByIssueNumber[[int]$item.content.number] = $item
        }
    }

    return [pscustomobject]@{
        ProjectId = $projectView.id
        ItemsByIssueNumber = $itemsByIssueNumber
        StatusFieldId = $statusField.id
        StatusOptionIds = $statusOptions
    }
}

function Ensure-ProjectItem {
    param(
        [Parameter(Mandatory = $true)]$ProjectContext,
        [Parameter(Mandatory = $true)][string]$ProjectOwner,
        [Parameter(Mandatory = $true)][int]$ProjectNumber,
        [Parameter(Mandatory = $true)]$Issue
    )

    if ($ProjectContext.ItemsByIssueNumber.ContainsKey([int]$Issue.number)) {
        return $ProjectContext.ItemsByIssueNumber[[int]$Issue.number]
    }

    Invoke-GhNoJson -Arguments @(
        "project", "item-add", $ProjectNumber.ToString(),
        "--owner", $ProjectOwner,
        "--url", (Get-IssueWebUrl -Issue $Issue)
    )

    $refreshed = Invoke-GhJson -Arguments @("project", "item-list", $ProjectNumber.ToString(), "--owner", $ProjectOwner, "-L", "200", "--format", "json")
    foreach ($item in $refreshed.items) {
        if ($null -ne $item.content -and $item.content.type -eq "Issue" -and [int]$item.content.number -eq [int]$Issue.number) {
            $ProjectContext.ItemsByIssueNumber[[int]$Issue.number] = $item
            return $item
        }
    }

    throw "Issue #$($Issue.number) was added to the project, but the item could not be reloaded."
}

function Set-ProjectStatus {
    param(
        [Parameter(Mandatory = $true)]$ProjectContext,
        [Parameter(Mandatory = $true)]$ProjectItem,
        [Parameter(Mandatory = $true)][string]$DesiredStatus
    )

    $currentStatus = ""
    if ($null -ne $ProjectItem.status) {
        $currentStatus = [string]$ProjectItem.status
    }

    $shouldUpdate = $false
    if ($DesiredStatus -eq "Done") {
        $shouldUpdate = $currentStatus -ne "Done"
    }
    elseif ($DesiredStatus -eq "Todo") {
        $shouldUpdate = [string]::IsNullOrWhiteSpace($currentStatus) -or $currentStatus -eq "Done"
    }

    if (-not $shouldUpdate) {
        return
    }

    $optionId = $ProjectContext.StatusOptionIds[$DesiredStatus]
    Invoke-GhNoJson -Arguments @(
        "project", "item-edit",
        "--id", $ProjectItem.id,
        "--project-id", $ProjectContext.ProjectId,
        "--field-id", $ProjectContext.StatusFieldId,
        "--single-select-option-id", $optionId
    )
}

Set-GitHubTokenPreference
$repo = Get-RepositoryFullName -ExplicitRepo $RepoFullName

$roadmapPhases = Get-RoadmapPhases -Path $RoadmapPath
$backlogIssues = Get-BacklogIssueSpecs -Path $BacklogPath
$roadmapIssues = Get-RoadmapIssueSpecs -RoadmapPhases $roadmapPhases
$desiredIssues = @($backlogIssues + $roadmapIssues)

$phaseMilestones = @{}
foreach ($phase in $roadmapPhases | Where-Object { $_.Number -ge 1 }) {
    $phaseMilestones[$phase.Number] = $phase
}

$existingIssues = Invoke-GhJson -Arguments @(
    "issue", "list",
    "--repo", $repo,
    "--state", "all",
    "--limit", "200",
    "--json", "number,title,state,body,url,milestone"
)

$issueIndexByKey = @{}
$issueIndexByTitle = @{}
foreach ($issue in $existingIssues) {
    $bodyText = Normalize-Text -Value $issue.body
    if ($bodyText -match "<!-- planning-sync:key=(?<key>[^>]+) -->") {
        if (-not $issueIndexByKey.ContainsKey($Matches.key) -or [int]$issue.number -lt [int]$issueIndexByKey[$Matches.key].number) {
            $issueIndexByKey[$Matches.key] = $issue
        }
    }

    if (-not $issueIndexByTitle.ContainsKey($issue.title) -or [int]$issue.number -lt [int]$issueIndexByTitle[$issue.title].number) {
        $issueIndexByTitle[$issue.title] = $issue
    }
}

$existingMilestones = Invoke-GhApiJson -Route "repos/$repo/milestones?state=all&per_page=100"
$milestonesByKey = @{}
$milestonesByTitle = @{}
foreach ($milestone in $existingMilestones) {
    if ($null -ne $milestone.description -and $milestone.description -match "<!-- planning-sync:key=(?<key>[^>]+) -->") {
        $milestonesByKey[$Matches.key] = $milestone
    }

    $milestonesByTitle[$milestone.title] = $milestone
}

foreach ($phaseNumber in ($phaseMilestones.Keys | Sort-Object)) {
    $phase = $phaseMilestones[$phaseNumber]
    $milestone = $null
    if ($milestonesByKey.ContainsKey($phase.SyncKey)) {
        $milestone = $milestonesByKey[$phase.SyncKey]
    }
    elseif ($milestonesByTitle.ContainsKey($phase.Title)) {
        $milestone = $milestonesByTitle[$phase.Title]
    }

    if ($null -eq $milestone) {
        Write-Host "Creating milestone '$($phase.Title)'..."
        $milestone = Invoke-GhApiJson -Route "repos/$repo/milestones" -Method "POST" -Body @{
            title = $phase.Title
            description = $phase.Description
            state = "open"
        }
    }
    else {
        $needsUpdate = (Normalize-Text -Value $milestone.title) -ne (Normalize-Text -Value $phase.Title) -or
            (Normalize-Text -Value $milestone.description) -ne (Normalize-Text -Value $phase.Description)

        if ($needsUpdate) {
            Write-Host "Updating milestone '$($phase.Title)'..."
            $milestone = Invoke-GhApiJson -Route "repos/$repo/milestones/$($milestone.number)" -Method "PATCH" -Body @{
                title = $phase.Title
                description = $phase.Description
                state = "open"
            }
        }
    }

    $milestonesByKey[$phase.SyncKey] = $milestone
    $milestonesByTitle[$milestone.title] = $milestone
    $phaseMilestones[$phaseNumber] = [pscustomobject]@{
        Number = $phase.Number
        Name = $phase.Name
        Title = $milestone.title
        Status = $phase.Status
        Body = $phase.Body
        Description = $phase.Description
        SyncKey = $phase.SyncKey
        MilestoneNumber = $milestone.number
    }
}

$syncedIssues = @{}
foreach ($desiredIssue in $desiredIssues) {
    $existingIssue = $null
    $keyIssue = $null
    $titleIssue = $null
    if ($issueIndexByKey.ContainsKey($desiredIssue.SyncKey)) {
        $keyIssue = $issueIndexByKey[$desiredIssue.SyncKey]
    }
    if ($issueIndexByTitle.ContainsKey($desiredIssue.Title)) {
        $titleIssue = $issueIndexByTitle[$desiredIssue.Title]
    }

    if ($null -ne $keyIssue -and $null -ne $titleIssue) {
        if ([int]$titleIssue.number -le [int]$keyIssue.number) {
            $existingIssue = $titleIssue
        }
        else {
            $existingIssue = $keyIssue
        }
    }
    elseif ($null -ne $keyIssue) {
        $existingIssue = $keyIssue
    }
    elseif ($null -ne $titleIssue) {
        $existingIssue = $titleIssue
    }

    $milestoneNumber = $null
    if ($null -ne $desiredIssue.PhaseNumber -and $phaseMilestones.ContainsKey([int]$desiredIssue.PhaseNumber)) {
        $milestoneNumber = $phaseMilestones[[int]$desiredIssue.PhaseNumber].MilestoneNumber
    }

    if ($null -eq $existingIssue) {
        Write-Host "Creating issue '$($desiredIssue.Title)'..."
        $body = @{
            title = $desiredIssue.Title
            body = $desiredIssue.Body
        }

        if ($null -ne $milestoneNumber) {
            $body.milestone = $milestoneNumber
        }

        $existingIssue = Invoke-GhApiJson -Route "repos/$repo/issues" -Method "POST" -Body $body
    }
    else {
        $currentMilestoneNumber = $null
        if ($null -ne $existingIssue.milestone) {
            $currentMilestoneNumber = $existingIssue.milestone.number
        }

        $needsUpdate = (Normalize-Text -Value $existingIssue.title) -ne (Normalize-Text -Value $desiredIssue.Title) -or
            (Normalize-Text -Value $existingIssue.body) -ne (Normalize-Text -Value $desiredIssue.Body) -or
            ($existingIssue.state.ToLowerInvariant() -ne $desiredIssue.State.ToLowerInvariant()) -or
            (($null -ne $milestoneNumber) -and ($currentMilestoneNumber -ne $milestoneNumber))

        if ($needsUpdate) {
            Write-Host "Updating issue #$($existingIssue.number) '$($desiredIssue.Title)'..."
            $body = @{
                title = $desiredIssue.Title
                body = $desiredIssue.Body
                state = $desiredIssue.State
            }

            if ($null -ne $milestoneNumber) {
                $body.milestone = $milestoneNumber
            }

            $existingIssue = Invoke-GhApiJson -Route "repos/$repo/issues/$($existingIssue.number)" -Method "PATCH" -Body $body
        }
    }

    $issueIndexByKey[$desiredIssue.SyncKey] = $existingIssue
    $issueIndexByTitle[$existingIssue.title] = $existingIssue
    $syncedIssues[[int]$existingIssue.number] = [pscustomobject]@{
        Issue = $existingIssue
        Desired = $desiredIssue
    }
}

$allIssues = Invoke-GhJson -Arguments @(
    "issue", "list",
    "--repo", $repo,
    "--state", "all",
    "--limit", "200",
    "--json", "number,title,state,body,url,milestone"
)

foreach ($issue in $allIssues) {
    $bodyText = Normalize-Text -Value $issue.body
    $parentMatch = [regex]::Match($bodyText, "Parent epic:\s+#(?<number>\d+)")
    if (-not $parentMatch.Success) {
        continue
    }

    $parentNumber = [int]$parentMatch.Groups["number"].Value
    if (-not $syncedIssues.ContainsKey($parentNumber)) {
        continue
    }

    $parentDesired = $syncedIssues[$parentNumber].Desired
    if ($null -eq $parentDesired.PhaseNumber -or -not $phaseMilestones.ContainsKey([int]$parentDesired.PhaseNumber)) {
        continue
    }

    $targetMilestoneNumber = $phaseMilestones[[int]$parentDesired.PhaseNumber].MilestoneNumber
    $currentMilestoneNumber = $null
    if ($null -ne $issue.milestone) {
        $currentMilestoneNumber = $issue.milestone.number
    }

    if ($currentMilestoneNumber -eq $targetMilestoneNumber) {
        continue
    }

    Write-Host "Aligning milestone for child issue #$($issue.number) '$($issue.title)'..."
    Invoke-GhApiJson -Route "repos/$repo/issues/$($issue.number)" -Method "PATCH" -Body @{
        milestone = $targetMilestoneNumber
    } | Out-Null
}

if (-not $SkipProjectSync) {
    $projectContext = $null
    try {
        $projectContext = Get-ProjectContext -Owner $ProjectOwner -ProjectNumber $ProjectNumber
    }
    catch {
        Write-Warning "Project sync is unavailable with the current token or project settings. Repo issues and milestones were still synced. $($_.Exception.Message)"
    }

    if ($null -ne $projectContext) {
        foreach ($syncEntry in $syncedIssues.GetEnumerator()) {
            $issue = $syncEntry.Value.Issue
            $desired = $syncEntry.Value.Desired
            $projectItem = Ensure-ProjectItem -ProjectContext $projectContext -ProjectOwner $ProjectOwner -ProjectNumber $ProjectNumber -Issue $issue
            $desiredStatus = if ($desired.State -eq "closed") { "Done" } else { "Todo" }
            Set-ProjectStatus -ProjectContext $projectContext -ProjectItem $projectItem -DesiredStatus $desiredStatus
        }
    }
}

Write-Host "Planning sync completed for $repo."
