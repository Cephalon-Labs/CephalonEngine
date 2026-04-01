[CmdletBinding()]
param(
    [string]$RepoFullName,
    [string]$RoadmapPath = "docs/engine-roadmap.md",
    [string]$BacklogPath = "docs/engine-backlog.md",
    [string]$ProjectOwner = "Cephalon-Labs",
    [int]$ProjectNumber = 2,
    [string]$DefaultAssignee = "Cephalon-Neza",
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

    return $output | ConvertFrom-Json
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
            $json = $Body | ConvertTo-Json -Depth 100
            [System.IO.File]::WriteAllText($tempFile, $json, (New-Object System.Text.UTF8Encoding($false)))
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

    $content = Get-Content -LiteralPath $Path -Raw -Encoding utf8
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

        $phases += [pscustomobject]@{
            Number = $phaseNumber
            Name = $phaseName
            Title = Get-PhaseTitle -PhaseNumber $phaseNumber -Name $phaseName
            Status = $status
            Body = $phaseBody
            SyncKey = $syncKey
            SourcePath = $Path
        }
    }

    return $phases
}

function Get-BacklogPhaseMap {
    return @{
        "ENG-000" = 0
        "ENG-001" = 0
        "ENG-002" = 0
        "ENG-003" = 0
        "ENG-004" = 0
        "ENG-006" = 0
        "ENG-007" = 0
        "ENG-008" = 0
        "ENG-009" = 0
        "ENG-014" = 0
        "ENG-015" = 0
        "ENG-025" = 0
        "ENG-005" = 1
        "ENG-016" = 1
        "ENG-017" = 1
        "ENG-018" = 1
        "ENG-019" = 2
        "ENG-020" = 2
        "ENG-021" = 2
        "ENG-023" = 2
        "ENG-011" = 3
        "ENG-012" = 3
        "ENG-024" = 3
        "ENG-013" = 4
        "ENG-022" = 5
    }
}

function Get-BacklogIssueSpecs {
    param([Parameter(Mandatory = $true)][string]$Path)

    $phaseMap = Get-BacklogPhaseMap

    $content = Get-Content -LiteralPath $Path -Raw -Encoding utf8
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
            $engCode = $Matches[1].ToUpperInvariant()
            $syncId = $engCode.ToLowerInvariant()
            $syncKey = "backlog:$syncId"
            $state = if ($status -eq "done") { "closed" } else { "open" }
            $phaseNumber = $null
            if ($phaseMap.ContainsKey($engCode)) {
                $phaseNumber = $phaseMap[$engCode]
            }

            $specs += [pscustomobject]@{
                Kind = "Backlog"
                Title = $title
                ContentBody = $body
                State = $state
                SyncKey = $syncKey
                PhaseNumber = $phaseNumber
                EngCode = $engCode
                SectionTitle = $sectionTitle
                SourcePath = $Path
            }
        }
    }

    return $specs
}

function Get-RoadmapIssueSpecs {
    param(
        [Parameter(Mandatory = $true)]$RoadmapPhases,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $phase2 = $RoadmapPhases | Where-Object { $_.Number -eq 2 } | Select-Object -First 1
    if ($null -eq $phase2) {
        throw "Unable to find roadmap phase 2 in docs/engine-roadmap.md."
    }

    return @(
        [pscustomobject]@{
            Kind = "Roadmap"
            Title = "Phase 2 operational hardening follow-through"
            ContentBody = $phase2.Body
            State = "open"
            SyncKey = "roadmap:phase-2"
            PhaseNumber = 2
            RoadmapPhaseTitle = $phase2.Title
            SourcePath = $Path
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

function Get-RepositoryContext {
    param([Parameter(Mandatory = $true)][string]$RepositoryFullName)

    $parts = $RepositoryFullName.Split("/", 2)
    if ($parts.Count -ne 2) {
        throw "Repository full name '$RepositoryFullName' must be in owner/repo form."
    }

    $owner = $parts[0]
    $name = $parts[1]
    $query = @'
query($owner: String!, $name: String!) {
  repository(owner: $owner, name: $name) {
    id
    url
    defaultBranchRef {
      name
    }
    issueTypes(first: 20) {
      nodes {
        id
        name
        isEnabled
      }
    }
  }
}
'@
    $repositoryResponse = Invoke-GhJson -Arguments @(
        "api", "graphql",
        "-f", "owner=$owner",
        "-f", "name=$name",
        "-f", "query=$query")
    $repository = $repositoryResponse.data.repository

    $issueTypeIdsByName = @{}
    foreach ($issueType in $repository.issueTypes.nodes) {
        if ($issueType.isEnabled) {
            $issueTypeIdsByName[$issueType.name] = $issueType.id
        }
    }

    return [pscustomobject]@{
        FullName = $RepositoryFullName
        Owner = $owner
        Name = $name
        Id = $repository.id
        Url = $repository.url
        DefaultBranch = if ($null -ne $repository.defaultBranchRef) { $repository.defaultBranchRef.name } else { "main" }
        IssueTypeIdsByName = $issueTypeIdsByName
    }
}

function Get-GitHubAnchorSlug {
    param([Parameter(Mandatory = $true)][string]$Value)

    $normalized = $Value.ToLowerInvariant()
    $normalized = [regex]::Replace($normalized, '[`"''.,:;!?/\\(){}\[\]&]+', "")
    $normalized = [regex]::Replace($normalized, "\s+", "-")
    $normalized = [regex]::Replace($normalized, "-{2,}", "-")
    return $normalized.Trim("-")
}

function Get-RepositoryDocumentUrl {
    param(
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$Anchor
    )

    $url = "$($RepositoryContext.Url)/blob/$($RepositoryContext.DefaultBranch)/$Path"
    if (-not [string]::IsNullOrWhiteSpace($Anchor)) {
        return "$url#$Anchor"
    }

    return $url
}

function Get-MilestoneWebUrl {
    param(
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [Parameter(Mandatory = $true)][int]$MilestoneNumber
    )

    return "$($RepositoryContext.Url)/milestone/$MilestoneNumber"
}

function Get-BacklogIterationMap {
    param([Parameter(Mandatory = $true)][string]$Path)

    $content = Get-Content -LiteralPath $Path -Raw -Encoding utf8
    $sectionMatch = [regex]::Match(
        $content,
        "(?ms)^##\s+Recommended next 3 sprints\r?\n(?<body>.*?)(?=^##\s+|\z)")

    $map = @{}
    if (-not $sectionMatch.Success) {
        return $map
    }

    $sprintMatches = [regex]::Matches(
        $sectionMatch.Groups["body"].Value,
        "(?ms)^###\s+(?<title>[^\r\n]+)\r?\n(?<body>.*?)(?=^###\s+|\z)")

    foreach ($sprintMatch in $sprintMatches) {
        $sprintTitle = $sprintMatch.Groups["title"].Value.Trim()
        $lines = $sprintMatch.Groups["body"].Value -split "\r?\n"
        foreach ($line in $lines) {
            $trimmed = $line.Trim()
            if ($trimmed -notmatch "^-") {
                continue
            }

            if ($trimmed -match "(ENG-\d+)") {
                $map[$Matches[1].ToUpperInvariant()] = $sprintTitle
                continue
            }

            if ($trimmed -match "operational hardening follow-through") {
                $map["roadmap:phase-2"] = $sprintTitle
            }
        }
    }

    return $map
}

function Parse-PlanningMetadataFromBody {
    param([AllowNull()][string]$Body)

    $normalizedBody = Normalize-Text -Value $Body
    $parentIssueNumber = $null
    $iterationTitle = $null
    $estimate = $null
    $sourcePaths = @()

    $parentMatch = [regex]::Match($normalizedBody, "(?m)^Parent epic:\s+#(?<number>\d+)")
    if ($parentMatch.Success) {
        $parentIssueNumber = [int]$parentMatch.Groups["number"].Value
    }

    $iterationMatch = [regex]::Match($normalizedBody, "(?m)^Planned iteration:\s+\*\*(?<title>.+?)\*\*")
    if ($iterationMatch.Success) {
        $iterationTitle = $iterationMatch.Groups["title"].Value.Trim()
    }

    $estimateMatch = [regex]::Match($normalizedBody, "(?m)^Estimate:\s+\*\*(?<value>\d+(?:\.\d+)?)\*\*")
    if ($estimateMatch.Success) {
        $estimate = [decimal]::Parse($estimateMatch.Groups["value"].Value, [System.Globalization.CultureInfo]::InvariantCulture)
    }

    $sourceMatch = [regex]::Match($normalizedBody, "(?ms)^Source:\s*(?<lines>(?:\r?\n-\s+[^\r\n]+)+)")
    if ($sourceMatch.Success) {
        foreach ($sourceLine in ($sourceMatch.Groups["lines"].Value -split "\r?\n")) {
            $trimmed = $sourceLine.Trim()
            if ($trimmed -match "^- (?<path>.+)$") {
                $sourcePaths += $Matches.path.Trim()
            }
        }
    }

    return [pscustomobject]@{
        ParentIssueNumber = $parentIssueNumber
        IterationTitle = $iterationTitle
        Estimate = $estimate
        SourcePaths = $sourcePaths
    }
}

function Format-EstimateText {
    param($Estimate)

    if ($null -eq $Estimate) {
        return ""
    }

    return ("{0:0.##}h" -f [decimal]$Estimate)
}

function Render-PlanningLinksSection {
    param(
        [string]$BacklogUrl,
        [string]$RoadmapUrl,
        [string]$BoardUrl,
        [string]$MilestoneTitle,
        [string]$MilestoneUrl,
        [string]$IterationTitle,
        [string]$Status,
        [string]$IssueTypeName,
        [string]$Assignee,
        [string]$EstimateText,
        [string]$ParentTitle,
        [string]$ParentUrl
    )

    $lines = @(
        "<!-- planning-sync:section=planning-links:start -->"
        "## Planning links"
        ""
    )

    if (-not [string]::IsNullOrWhiteSpace($ParentUrl)) {
        $lines += "- Parent epic: [$ParentTitle]($ParentUrl)"
    }

    if (-not [string]::IsNullOrWhiteSpace($BacklogUrl)) {
        $lines += "- Backlog: [docs/engine-backlog.md]($BacklogUrl)"
    }

    if (-not [string]::IsNullOrWhiteSpace($RoadmapUrl)) {
        $lines += "- Roadmap: [docs/engine-roadmap.md]($RoadmapUrl)"
    }

    if (-not [string]::IsNullOrWhiteSpace($MilestoneUrl)) {
        $lines += "- Milestone / phase: [$MilestoneTitle]($MilestoneUrl)"
    }

    if (-not [string]::IsNullOrWhiteSpace($BoardUrl)) {
        $lines += "- Board: [@Cephalon-Engine]($BoardUrl)"
    }

    if (-not [string]::IsNullOrWhiteSpace($IterationTitle)) {
        $lines += "- Iteration: $IterationTitle"
    }

    if (-not [string]::IsNullOrWhiteSpace($Status)) {
        $lines += "- Project status: $Status"
    }

    if (-not [string]::IsNullOrWhiteSpace($IssueTypeName)) {
        $lines += "- Issue type: $IssueTypeName"
    }

    if (-not [string]::IsNullOrWhiteSpace($Assignee)) {
        $lines += "- Assignee: $Assignee"
    }

    if (-not [string]::IsNullOrWhiteSpace($EstimateText)) {
        $lines += "- Time / estimate recorded on board: $EstimateText"
    }

    $lines += "<!-- planning-sync:section=planning-links:end -->"
    return ($lines -join "`n").Trim()
}

function Upsert-ManagedSection {
    param(
        [AllowNull()][string]$Body,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$RenderedSection
    )

    $normalizedBody = Normalize-Text -Value $Body
    $startMarker = "<!-- planning-sync:section=$Key:start -->"
    $endMarker = "<!-- planning-sync:section=$Key:end -->"
    $pattern = "(?ms)$([regex]::Escape($startMarker)).*?$([regex]::Escape($endMarker))"

    if ($normalizedBody -match $pattern) {
        $strippedBody = ([regex]::Replace($normalizedBody, $pattern, "")).Trim()
        if ([string]::IsNullOrWhiteSpace($strippedBody)) {
            return $RenderedSection.Trim()
        }

        return ($strippedBody + "`n`n" + $RenderedSection.Trim()).Trim()
    }

    if ([string]::IsNullOrWhiteSpace($normalizedBody)) {
        return $RenderedSection.Trim()
    }

    return ($normalizedBody.Trim() + "`n`n" + $RenderedSection.Trim()).Trim()
}

function Normalize-ChildIssueBody {
    param([AllowNull()][string]$Body)

    $normalizedBody = Normalize-Text -Value $Body
    if ([string]::IsNullOrWhiteSpace($normalizedBody)) {
        return $normalizedBody
    }

    return [regex]::Replace($normalizedBody, '(?m)^Item type:\s+\*\*Draft item\*\*$', 'Item type: **Issue**')
}

function Render-MilestoneDescription {
    param(
        [Parameter(Mandatory = $true)]$Phase,
        [Parameter(Mandatory = $true)]$RepositoryContext
    )

    $roadmapUrl = Get-RepositoryDocumentUrl -RepositoryContext $RepositoryContext -Path $Phase.SourcePath -Anchor (Get-GitHubAnchorSlug -Value ("Phase {0}: {1}" -f $Phase.Number, $Phase.Name))
    return @(
        (Get-SyncMarker -Key $Phase.SyncKey)
        "Synced from [docs/engine-roadmap.md]($roadmapUrl)."
        ""
        "Roadmap phase: **$($Phase.Title)**"
        ""
        $Phase.Body
    ) -join "`n"
}

function Render-SyncedIssueBody {
    param(
        [Parameter(Mandatory = $true)]$Spec,
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [string]$BacklogUrl,
        [string]$RoadmapUrl,
        [string]$BoardUrl,
        [string]$MilestoneTitle,
        [string]$MilestoneUrl,
        [string]$IterationTitle,
        [string]$Status,
        [string]$IssueTypeName,
        [string]$Assignee,
        [string]$EstimateText
    )

    $lines = @((Get-SyncMarker -Key $Spec.SyncKey))

    if ($Spec.Kind -eq "Backlog") {
        $lines += "Synced from [docs/engine-backlog.md]($BacklogUrl)."
        $lines += ""
        $lines += "Planning section: **$($Spec.SectionTitle)**"
    }
    else {
        $lines += "Synced from [docs/engine-roadmap.md]($RoadmapUrl)."
        $lines += ""
        $lines += "Roadmap phase: **$($Spec.RoadmapPhaseTitle)**"
    }

    $lines += ""
    $lines += $Spec.ContentBody
    $lines += ""
    $lines += (Render-PlanningLinksSection -BacklogUrl $BacklogUrl -RoadmapUrl $RoadmapUrl -BoardUrl $BoardUrl -MilestoneTitle $MilestoneTitle -MilestoneUrl $MilestoneUrl -IterationTitle $IterationTitle -Status $Status -IssueTypeName $IssueTypeName -Assignee $Assignee -EstimateText $EstimateText)

    return ($lines -join "`n").Trim()
}

function Get-TopLevelIssueTypeName {
    return "Feature"
}

function Get-ChildIssueTypeName {
    return "Task"
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

    $orgProjectQuery = @'
query($owner: String!, $number: Int!) {
  organization(login: $owner) {
    projectV2(number: $number) {
      id
      url
      fields(first: 50) {
        nodes {
          ... on ProjectV2FieldCommon {
            id
            name
          }
          ... on ProjectV2SingleSelectField {
            id
            name
            options {
              id
              name
            }
          }
          ... on ProjectV2IterationField {
            id
            name
            configuration {
              iterations {
                id
                title
              }
              completedIterations {
                id
                title
              }
            }
          }
        }
      }
    }
  }
}
'@
    $userProjectQuery = @'
query($owner: String!, $number: Int!) {
  user(login: $owner) {
    projectV2(number: $number) {
      id
      url
      fields(first: 50) {
        nodes {
          ... on ProjectV2FieldCommon {
            id
            name
          }
          ... on ProjectV2SingleSelectField {
            id
            name
            options {
              id
              name
            }
          }
          ... on ProjectV2IterationField {
            id
            name
            configuration {
              iterations {
                id
                title
              }
              completedIterations {
                id
                title
              }
            }
          }
        }
      }
    }
  }
}
'@

    $projectView = $null
    try {
        $projectResponse = Invoke-GhJson -Arguments @(
            "api", "graphql",
            "-f", "owner=$Owner",
            "-F", "number=$ProjectNumber",
            "-f", "query=$orgProjectQuery")
        $projectView = $projectResponse.data.organization.projectV2
    }
    catch {
        $projectView = $null
    }

    if ($null -eq $projectView) {
        $projectResponse = Invoke-GhJson -Arguments @(
            "api", "graphql",
            "-f", "owner=$Owner",
            "-F", "number=$ProjectNumber",
            "-f", "query=$userProjectQuery")
        $projectView = $projectResponse.data.user.projectV2
    }

    if ($null -eq $projectView) {
        throw "Unable to load project $Owner/$ProjectNumber."
    }

    $items = Invoke-GhJson -Arguments @("project", "item-list", $ProjectNumber.ToString(), "--owner", $Owner, "-L", "200", "--format", "json")

    $statusField = $projectView.fields.nodes | Where-Object { $_.name -eq "Status" } | Select-Object -First 1
    if ($null -eq $statusField) {
        throw "Unable to find the Status field for project $Owner/$ProjectNumber."
    }

    $estimateField = $projectView.fields.nodes | Where-Object { $_.name -eq "Estimate" } | Select-Object -First 1
    $iterationField = $projectView.fields.nodes | Where-Object { $_.name -eq "Iteration" } | Select-Object -First 1

    $statusOptions = @{}
    foreach ($option in $statusField.options) {
        $statusOptions[$option.name] = $option.id
    }

    $itemsByIssueNumber = @{}
    $draftItems = @()
    foreach ($item in $items.items) {
        if ($null -ne $item.content -and $item.content.type -eq "Issue" -and $null -ne $item.content.number) {
            $itemsByIssueNumber[[int]$item.content.number] = $item
        }
        elseif ($null -ne $item.content -and $item.content.type -eq "DraftIssue") {
            $draftItems += $item
        }
    }

    $iterationIdsByTitle = @{}
    if ($null -ne $iterationField -and $null -ne $iterationField.configuration) {
        foreach ($iteration in @($iterationField.configuration.iterations + $iterationField.configuration.completedIterations)) {
            if (-not [string]::IsNullOrWhiteSpace([string]$iteration.title) -and -not $iterationIdsByTitle.ContainsKey($iteration.title)) {
                $iterationIdsByTitle[$iteration.title] = $iteration.id
            }
        }
    }

    return [pscustomobject]@{
        ProjectId = $projectView.id
        ProjectUrl = $projectView.url
        ItemsByIssueNumber = $itemsByIssueNumber
        DraftItems = $draftItems
        StatusFieldId = $statusField.id
        StatusOptionIds = $statusOptions
        EstimateFieldId = if ($null -ne $estimateField) { $estimateField.id } else { $null }
        IterationFieldId = if ($null -ne $iterationField) { $iterationField.id } else { $null }
        IterationIdsByTitle = $iterationIdsByTitle
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

function Set-ProjectEstimate {
    param(
        [Parameter(Mandatory = $true)]$ProjectContext,
        [Parameter(Mandatory = $true)]$ProjectItem,
        $Estimate
    )

    if ($null -eq $ProjectContext.EstimateFieldId -or $null -eq $Estimate) {
        return
    }

    $currentEstimate = $null
    if ($ProjectItem.PSObject.Properties["estimate"]) {
        $currentEstimate = $ProjectItem.estimate
    }

    if ($null -ne $currentEstimate -and [decimal]$currentEstimate -eq [decimal]$Estimate) {
        return
    }

    Invoke-GhNoJson -Arguments @(
        "project", "item-edit",
        "--id", $ProjectItem.id,
        "--project-id", $ProjectContext.ProjectId,
        "--field-id", $ProjectContext.EstimateFieldId,
        "--number", ([string]([decimal]$Estimate))
    )

    $ProjectItem | Add-Member -NotePropertyName estimate -NotePropertyValue ([decimal]$Estimate) -Force
}

function Set-ProjectIteration {
    param(
        [Parameter(Mandatory = $true)]$ProjectContext,
        [Parameter(Mandatory = $true)]$ProjectItem,
        [string]$IterationTitle
    )

    if ($null -eq $ProjectContext.IterationFieldId -or [string]::IsNullOrWhiteSpace($IterationTitle)) {
        return
    }

    if (-not $ProjectContext.IterationIdsByTitle.ContainsKey($IterationTitle)) {
        Write-Warning "Iteration '$IterationTitle' does not exist in project $($ProjectContext.ProjectId)."
        return
    }

    $currentIterationTitle = $null
    if ($ProjectItem.PSObject.Properties["iteration"] -and $null -ne $ProjectItem.iteration) {
        $currentIterationTitle = [string]$ProjectItem.iteration.title
    }

    if ($currentIterationTitle -eq $IterationTitle) {
        return
    }

    Invoke-GhNoJson -Arguments @(
        "project", "item-edit",
        "--id", $ProjectItem.id,
        "--project-id", $ProjectContext.ProjectId,
        "--field-id", $ProjectContext.IterationFieldId,
        "--iteration-id", $ProjectContext.IterationIdsByTitle[$IterationTitle]
    )
}

function Get-ProjectItemEstimateValue {
    param($ProjectItem)

    if ($null -eq $ProjectItem) {
        return $null
    }

    if ($ProjectItem.PSObject.Properties["estimate"] -and $null -ne $ProjectItem.estimate) {
        return [decimal]$ProjectItem.estimate
    }

    return $null
}

function Get-ProjectItemIterationTitle {
    param($ProjectItem)

    if ($null -eq $ProjectItem) {
        return $null
    }

    if ($ProjectItem.PSObject.Properties["iteration"] -and $null -ne $ProjectItem.iteration) {
        return [string]$ProjectItem.iteration.title
    }

    return $null
}

function Get-ProjectItemStatusValue {
    param($ProjectItem)

    if ($null -eq $ProjectItem) {
        return $null
    }

    if ($ProjectItem.PSObject.Properties["status"] -and -not [string]::IsNullOrWhiteSpace([string]$ProjectItem.status)) {
        return [string]$ProjectItem.status
    }

    return $null
}

function Set-IssueType {
    param(
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [Parameter(Mandatory = $true)]$Issue,
        [Parameter(Mandatory = $true)][string]$IssueTypeName
    )

    if (-not $RepositoryContext.IssueTypeIdsByName.ContainsKey($IssueTypeName)) {
        return
    }

    $mutation = @'
mutation($issueId: ID!, $issueTypeId: ID!) {
  updateIssueIssueType(input: {
    issueId: $issueId,
    issueTypeId: $issueTypeId
  }) {
    issue {
      id
    }
  }
}
'@

    Invoke-GhNoJson -Arguments @(
        "api", "graphql",
        "-f", "issueId=$($Issue.id)",
        "-f", "issueTypeId=$($RepositoryContext.IssueTypeIdsByName[$IssueTypeName])",
        "-f", "query=$mutation"
    )
}

function Get-IssueParentNumber {
    param(
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [Parameter(Mandatory = $true)][int]$IssueNumber
    )

    $query = @'
query($owner: String!, $name: String!, $number: Int!) {
  repository(owner: $owner, name: $name) {
    issue(number: $number) {
      parent {
        number
      }
    }
  }
}
'@
    $response = Invoke-GhJson -Arguments @(
        "api", "graphql",
        "-f", "owner=$($RepositoryContext.Owner)",
        "-f", "name=$($RepositoryContext.Name)",
        "-F", "number=$IssueNumber",
        "-f", "query=$query")

    if ($null -eq $response.data.repository.issue.parent) {
        return $null
    }

    return [int]$response.data.repository.issue.parent.number
}

function Ensure-SubIssueLink {
    param(
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [Parameter(Mandatory = $true)]$ParentIssue,
        [Parameter(Mandatory = $true)]$ChildIssue
    )

    $currentParentNumber = Get-IssueParentNumber -RepositoryContext $RepositoryContext -IssueNumber ([int]$ChildIssue.number)
    if ($currentParentNumber -eq [int]$ParentIssue.number) {
        return
    }

    $mutation = @'
mutation($issueId: ID!, $subIssueId: ID!) {
  addSubIssue(input: {
    issueId: $issueId,
    subIssueId: $subIssueId
  }) {
    issue {
      id
    }
    subIssue {
      id
    }
  }
}
'@

    Invoke-GhNoJson -Arguments @(
        "api", "graphql",
        "-f", "issueId=$($ParentIssue.id)",
        "-f", "subIssueId=$($ChildIssue.id)",
        "-f", "query=$mutation"
    )
}

function Convert-DraftItemsToIssues {
    param(
        [Parameter(Mandatory = $true)]$ProjectContext,
        [Parameter(Mandatory = $true)]$RepositoryContext
    )

    foreach ($draftItem in $ProjectContext.DraftItems) {
        $metadata = Parse-PlanningMetadataFromBody -Body $draftItem.content.body
        if ($null -eq $metadata.ParentIssueNumber) {
            continue
        }

        Write-Host "Converting draft project item '$($draftItem.title)' to an issue..."
        $mutation = @'
mutation($itemId: ID!, $repositoryId: ID!) {
  convertProjectV2DraftIssueItemToIssue(input: {
    itemId: $itemId,
    repositoryId: $repositoryId
  }) {
    item {
      id
    }
  }
}
'@

        Invoke-GhNoJson -Arguments @(
            "api", "graphql",
            "-f", "itemId=$($draftItem.id)",
            "-f", "repositoryId=$($RepositoryContext.Id)",
            "-f", "query=$mutation"
        )
    }
}

function Get-ParentEstimateRollups {
    param([Parameter(Mandatory = $true)]$ProjectContext)

    $rollups = @{}
    foreach ($item in @($ProjectContext.ItemsByIssueNumber.Values + $ProjectContext.DraftItems)) {
        if ($null -eq $item.content -or -not $item.content.PSObject.Properties["body"]) {
            continue
        }

        $metadata = Parse-PlanningMetadataFromBody -Body $item.content.body
        if ($null -eq $metadata.ParentIssueNumber -or $null -eq $metadata.Estimate) {
            continue
        }

        if (-not $rollups.ContainsKey([int]$metadata.ParentIssueNumber)) {
            $rollups[[int]$metadata.ParentIssueNumber] = [decimal]0
        }

        $rollups[[int]$metadata.ParentIssueNumber] += [decimal]$metadata.Estimate
    }

    return $rollups
}

function Get-RepositoryIssues {
    param([Parameter(Mandatory = $true)][string]$RepositoryFullName)

    return Invoke-GhJson -Arguments @(
        "issue", "list",
        "--repo", $RepositoryFullName,
        "--state", "all",
        "--limit", "200",
        "--json", "id,number,title,state,body,url,milestone,assignees"
    )
}

function Get-TopLevelPlanningContext {
    param(
        [Parameter(Mandatory = $true)]$Spec,
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [Parameter(Mandatory = $true)]$PhaseMilestones,
        [Parameter(Mandatory = $true)]$IterationMap,
        [Parameter(Mandatory = $true)][string]$BoardUrl
    )

    $iterationTitle = $null
    if ($Spec.Kind -eq "Backlog" -and -not [string]::IsNullOrWhiteSpace($Spec.EngCode) -and $IterationMap.ContainsKey($Spec.EngCode)) {
        $iterationTitle = $IterationMap[$Spec.EngCode]
    }
    elseif ($Spec.Kind -eq "Roadmap" -and $IterationMap.ContainsKey($Spec.SyncKey)) {
        $iterationTitle = $IterationMap[$Spec.SyncKey]
    }

    $backlogUrl = $null
    if ($Spec.Kind -eq "Backlog") {
        $backlogUrl = Get-RepositoryDocumentUrl -RepositoryContext $RepositoryContext -Path $Spec.SourcePath -Anchor (Get-GitHubAnchorSlug -Value $Spec.Title)
    }
    elseif (-not [string]::IsNullOrWhiteSpace($iterationTitle)) {
        $backlogUrl = Get-RepositoryDocumentUrl -RepositoryContext $RepositoryContext -Path "docs/engine-backlog.md" -Anchor (Get-GitHubAnchorSlug -Value $iterationTitle)
    }

    $roadmapUrl = $null
    $milestoneTitle = $null
    $milestoneUrl = $null
    if ($null -ne $Spec.PhaseNumber -and $PhaseMilestones.ContainsKey([int]$Spec.PhaseNumber)) {
        $phase = $PhaseMilestones[[int]$Spec.PhaseNumber]
        $roadmapUrl = Get-RepositoryDocumentUrl -RepositoryContext $RepositoryContext -Path $phase.SourcePath -Anchor (Get-GitHubAnchorSlug -Value ("Phase {0}: {1}" -f $phase.Number, $phase.Name))
        $milestoneTitle = $phase.Title
        $milestoneUrl = Get-MilestoneWebUrl -RepositoryContext $RepositoryContext -MilestoneNumber $phase.MilestoneNumber
    }

    return [pscustomobject]@{
        IterationTitle = $iterationTitle
        BacklogUrl = $backlogUrl
        RoadmapUrl = $roadmapUrl
        MilestoneTitle = $milestoneTitle
        MilestoneUrl = $milestoneUrl
        BoardUrl = $BoardUrl
    }
}

function Get-ChildPlanningContext {
    param(
        [Parameter(Mandatory = $true)]$ParentIssue,
        $ParentDesired,
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [Parameter(Mandatory = $true)]$PhaseMilestones,
        [Parameter(Mandatory = $true)][string]$BoardUrl
    )

    $backlogUrl = $null
    $roadmapUrl = $null
    $milestoneTitle = $null
    $milestoneUrl = $null

    if ($null -ne $ParentDesired) {
        $topLevelContext = Get-TopLevelPlanningContext -Spec $ParentDesired -RepositoryContext $RepositoryContext -PhaseMilestones $PhaseMilestones -IterationMap @{} -BoardUrl $BoardUrl
        $backlogUrl = $topLevelContext.BacklogUrl
        $roadmapUrl = $topLevelContext.RoadmapUrl
        $milestoneTitle = $topLevelContext.MilestoneTitle
        $milestoneUrl = $topLevelContext.MilestoneUrl
    }
    elseif ($null -ne $ParentIssue.milestone) {
        $milestoneTitle = $ParentIssue.milestone.title
        if ($null -ne $ParentIssue.milestone.number) {
            $milestoneUrl = Get-MilestoneWebUrl -RepositoryContext $RepositoryContext -MilestoneNumber ([int]$ParentIssue.milestone.number)
        }
    }

    return [pscustomobject]@{
        BacklogUrl = $backlogUrl
        RoadmapUrl = $roadmapUrl
        MilestoneTitle = $milestoneTitle
        MilestoneUrl = $milestoneUrl
        BoardUrl = $BoardUrl
        ParentTitle = "#$($ParentIssue.number) $($ParentIssue.title)"
        ParentUrl = Get-IssueWebUrl -Issue $ParentIssue
    }
}

Set-GitHubTokenPreference
$repo = Get-RepositoryFullName -ExplicitRepo $RepoFullName
$repositoryContext = Get-RepositoryContext -RepositoryFullName $repo

$roadmapPhases = Get-RoadmapPhases -Path $RoadmapPath
$backlogIssues = Get-BacklogIssueSpecs -Path $BacklogPath
$roadmapIssues = Get-RoadmapIssueSpecs -RoadmapPhases $roadmapPhases -Path $RoadmapPath
$desiredIssues = @($backlogIssues + $roadmapIssues)
$topLevelIterationMap = Get-BacklogIterationMap -Path $BacklogPath

$projectContext = $null
if (-not $SkipProjectSync) {
    try {
        $projectContext = Get-ProjectContext -Owner $ProjectOwner -ProjectNumber $ProjectNumber
    }
    catch {
        Write-Warning "Project sync is unavailable with the current token or project settings. Repo issues and milestones were still synced. $($_.Exception.Message)"
    }
}

$boardUrl = if ($null -ne $projectContext -and -not [string]::IsNullOrWhiteSpace([string]$projectContext.ProjectUrl)) {
    [string]$projectContext.ProjectUrl
}
else {
    "https://github.com/orgs/$ProjectOwner/projects/$ProjectNumber"
}

$phaseMilestones = @{}
foreach ($phase in $roadmapPhases) {
    $phaseMilestones[$phase.Number] = $phase
}

$existingIssues = Get-RepositoryIssues -RepositoryFullName $repo
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
    $renderedDescription = Render-MilestoneDescription -Phase $phase -RepositoryContext $repositoryContext
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
            description = $renderedDescription
        }
    }
    else {
        $needsUpdate = (Normalize-Text -Value $milestone.title) -ne (Normalize-Text -Value $phase.Title) -or
            (Normalize-Text -Value $milestone.description) -ne (Normalize-Text -Value $renderedDescription)

        if ($needsUpdate) {
            Write-Host "Updating milestone '$($phase.Title)'..."
            $milestone = Invoke-GhApiJson -Route "repos/$repo/milestones/$($milestone.number)" -Method "PATCH" -Body @{
                title = $phase.Title
                description = $renderedDescription
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
        SyncKey = $phase.SyncKey
        SourcePath = $phase.SourcePath
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

    $topLevelContext = Get-TopLevelPlanningContext -Spec $desiredIssue -RepositoryContext $repositoryContext -PhaseMilestones $phaseMilestones -IterationMap $topLevelIterationMap -BoardUrl $boardUrl
    $milestoneNumber = $null
    if ($null -ne $desiredIssue.PhaseNumber -and $phaseMilestones.ContainsKey([int]$desiredIssue.PhaseNumber)) {
        $milestoneNumber = $phaseMilestones[[int]$desiredIssue.PhaseNumber].MilestoneNumber
    }

    $currentProjectItem = $null
    if ($null -ne $projectContext -and $null -ne $existingIssue -and $projectContext.ItemsByIssueNumber.ContainsKey([int]$existingIssue.number)) {
        $currentProjectItem = $projectContext.ItemsByIssueNumber[[int]$existingIssue.number]
    }

    $statusText = if (-not [string]::IsNullOrWhiteSpace((Get-ProjectItemStatusValue -ProjectItem $currentProjectItem))) {
        Get-ProjectItemStatusValue -ProjectItem $currentProjectItem
    }
    elseif ($desiredIssue.State -eq "closed") {
        "Done"
    }
    else {
        "Todo"
    }

    $estimateText = if ($null -ne $currentProjectItem -and $currentProjectItem.PSObject.Properties["estimate"] -and $null -ne $currentProjectItem.estimate) {
        Format-EstimateText -Estimate $currentProjectItem.estimate
    }
    else {
        ""
    }

    $renderedBody = Render-SyncedIssueBody -Spec $desiredIssue -RepositoryContext $repositoryContext -BacklogUrl $topLevelContext.BacklogUrl -RoadmapUrl $topLevelContext.RoadmapUrl -BoardUrl $topLevelContext.BoardUrl -MilestoneTitle $topLevelContext.MilestoneTitle -MilestoneUrl $topLevelContext.MilestoneUrl -IterationTitle $topLevelContext.IterationTitle -Status $statusText -IssueTypeName (Get-TopLevelIssueTypeName) -Assignee $DefaultAssignee -EstimateText $estimateText

    if ($null -eq $existingIssue) {
        Write-Host "Creating issue '$($desiredIssue.Title)'..."
        $body = @{
            title = $desiredIssue.Title
            body = $renderedBody
            assignees = @($DefaultAssignee)
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

        $currentAssignees = @($existingIssue.assignees | ForEach-Object { $_.login })
        $needsUpdate = (Normalize-Text -Value $existingIssue.title) -ne (Normalize-Text -Value $desiredIssue.Title) -or
            (Normalize-Text -Value $existingIssue.body) -ne (Normalize-Text -Value $renderedBody) -or
            ($existingIssue.state.ToLowerInvariant() -ne $desiredIssue.State.ToLowerInvariant()) -or
            ($currentAssignees -notcontains $DefaultAssignee) -or
            (($null -ne $milestoneNumber) -and ($currentMilestoneNumber -ne $milestoneNumber))

        if ($needsUpdate) {
            Write-Host "Updating issue #$($existingIssue.number) '$($desiredIssue.Title)'..."
            $body = @{
                title = $desiredIssue.Title
                body = $renderedBody
                state = $desiredIssue.State
                assignees = @($DefaultAssignee)
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
        Desired = $desiredIssue
        IterationTitle = $topLevelContext.IterationTitle
    }
}

$allIssues = Get-RepositoryIssues -RepositoryFullName $repo
$issuesByNumber = @{}
foreach ($issue in $allIssues) {
    $issuesByNumber[[int]$issue.number] = $issue
}

if ($null -ne $projectContext) {
    Convert-DraftItemsToIssues -ProjectContext $projectContext -RepositoryContext $repositoryContext
    $projectContext = Get-ProjectContext -Owner $ProjectOwner -ProjectNumber $ProjectNumber
    $allIssues = Get-RepositoryIssues -RepositoryFullName $repo
    $issuesByNumber = @{}
    foreach ($issue in $allIssues) {
        $issuesByNumber[[int]$issue.number] = $issue
    }
}

foreach ($syncNumber in @($syncedIssues.Keys)) {
    if ($issuesByNumber.ContainsKey([int]$syncNumber)) {
        $issue = $issuesByNumber[[int]$syncNumber]
        Set-IssueType -RepositoryContext $repositoryContext -Issue $issue -IssueTypeName (Get-TopLevelIssueTypeName)
    }
}

if ($null -ne $projectContext) {
    $parentEstimateRollups = Get-ParentEstimateRollups -ProjectContext $projectContext
    foreach ($syncEntry in $syncedIssues.GetEnumerator()) {
        $issue = $issuesByNumber[[int]$syncEntry.Key]
        $desired = $syncEntry.Value.Desired
        $projectItem = Ensure-ProjectItem -ProjectContext $projectContext -ProjectOwner $ProjectOwner -ProjectNumber $ProjectNumber -Issue $issue
        $desiredStatus = if ($desired.State -eq "closed") { "Done" } else { "Todo" }
        Set-ProjectStatus -ProjectContext $projectContext -ProjectItem $projectItem -DesiredStatus $desiredStatus

        if (-not [string]::IsNullOrWhiteSpace($syncEntry.Value.IterationTitle)) {
            Set-ProjectIteration -ProjectContext $projectContext -ProjectItem $projectItem -IterationTitle $syncEntry.Value.IterationTitle
        }

        if (($null -eq (Get-ProjectItemEstimateValue -ProjectItem $projectItem)) -and $parentEstimateRollups.ContainsKey([int]$issue.number)) {
            Set-ProjectEstimate -ProjectContext $projectContext -ProjectItem $projectItem -Estimate $parentEstimateRollups[[int]$issue.number]
        }
    }

    $projectContext = Get-ProjectContext -Owner $ProjectOwner -ProjectNumber $ProjectNumber
}

foreach ($issue in $issuesByNumber.Values) {
    $metadata = Parse-PlanningMetadataFromBody -Body $liveIssue.body
    if ($null -eq $metadata.ParentIssueNumber) {
        continue
    }

    if (-not $issuesByNumber.ContainsKey([int]$metadata.ParentIssueNumber)) {
        continue
    }

    $parentIssue = $issuesByNumber[[int]$metadata.ParentIssueNumber]
    $parentDesired = $null
    if ($syncedIssues.ContainsKey([int]$metadata.ParentIssueNumber)) {
        $parentDesired = $syncedIssues[[int]$metadata.ParentIssueNumber].Desired
    }

    $targetMilestoneNumber = $null
    if ($null -ne $parentIssue.milestone) {
        $targetMilestoneNumber = $parentIssue.milestone.number
    }

    $currentAssignees = @($issue.assignees | ForEach-Object { $_.login })
    $issuePatch = @{}
    if (($null -ne $targetMilestoneNumber) -and (($null -eq $issue.milestone) -or ([int]$issue.milestone.number -ne [int]$targetMilestoneNumber))) {
        $issuePatch.milestone = $targetMilestoneNumber
    }
    if ($currentAssignees -notcontains $DefaultAssignee) {
        $issuePatch.assignees = @($DefaultAssignee)
    }

    Set-IssueType -RepositoryContext $repositoryContext -Issue $issue -IssueTypeName (Get-ChildIssueTypeName)
    Ensure-SubIssueLink -RepositoryContext $repositoryContext -ParentIssue $parentIssue -ChildIssue $issue

    if ($issuePatch.Count -gt 0) {
        Write-Host "Aligning metadata for child issue #$($issue.number) '$($issue.title)'..."
        Invoke-GhApiJson -Route "repos/$repo/issues/$($issue.number)" -Method "PATCH" -Body $issuePatch | Out-Null
    }

    if ($null -ne $projectContext) {
        $projectItem = Ensure-ProjectItem -ProjectContext $projectContext -ProjectOwner $ProjectOwner -ProjectNumber $ProjectNumber -Issue $issue
        $desiredStatus = if ($issue.state -eq "CLOSED") { "Done" } else { "Todo" }
        Set-ProjectStatus -ProjectContext $projectContext -ProjectItem $projectItem -DesiredStatus $desiredStatus

        if (($null -eq (Get-ProjectItemEstimateValue -ProjectItem $projectItem)) -and $null -ne $metadata.Estimate) {
            Set-ProjectEstimate -ProjectContext $projectContext -ProjectItem $projectItem -Estimate $metadata.Estimate
        }

        if (-not [string]::IsNullOrWhiteSpace($metadata.IterationTitle)) {
            Set-ProjectIteration -ProjectContext $projectContext -ProjectItem $projectItem -IterationTitle $metadata.IterationTitle
        }
    }
}

if ($null -ne $projectContext) {
    $projectContext = Get-ProjectContext -Owner $ProjectOwner -ProjectNumber $ProjectNumber
}

$allIssues = Get-RepositoryIssues -RepositoryFullName $repo
$issuesByNumber = @{}
foreach ($issue in $allIssues) {
    $issuesByNumber[[int]$issue.number] = $issue
}

foreach ($syncEntry in $syncedIssues.GetEnumerator()) {
    $issue = $issuesByNumber[[int]$syncEntry.Key]
    $liveIssue = Invoke-GhApiJson -Route "repos/$repo/issues/$($issue.number)"
    $desired = $syncEntry.Value.Desired
    $topLevelContext = Get-TopLevelPlanningContext -Spec $desired -RepositoryContext $repositoryContext -PhaseMilestones $phaseMilestones -IterationMap $topLevelIterationMap -BoardUrl $boardUrl
    $projectItem = $null
    if ($null -ne $projectContext -and $projectContext.ItemsByIssueNumber.ContainsKey([int]$issue.number)) {
        $projectItem = $projectContext.ItemsByIssueNumber[[int]$issue.number]
    }

    $statusText = if (-not [string]::IsNullOrWhiteSpace((Get-ProjectItemStatusValue -ProjectItem $projectItem))) { Get-ProjectItemStatusValue -ProjectItem $projectItem } elseif ($issue.state -eq "CLOSED") { "Done" } else { "Todo" }
    $estimateText = if ($null -ne (Get-ProjectItemEstimateValue -ProjectItem $projectItem)) { Format-EstimateText -Estimate (Get-ProjectItemEstimateValue -ProjectItem $projectItem) } else { "" }
    $renderedBody = Render-SyncedIssueBody -Spec $desired -RepositoryContext $repositoryContext -BacklogUrl $topLevelContext.BacklogUrl -RoadmapUrl $topLevelContext.RoadmapUrl -BoardUrl $topLevelContext.BoardUrl -MilestoneTitle $topLevelContext.MilestoneTitle -MilestoneUrl $topLevelContext.MilestoneUrl -IterationTitle $(if (-not [string]::IsNullOrWhiteSpace((Get-ProjectItemIterationTitle -ProjectItem $projectItem))) { Get-ProjectItemIterationTitle -ProjectItem $projectItem } else { $topLevelContext.IterationTitle }) -Status $statusText -IssueTypeName (Get-TopLevelIssueTypeName) -Assignee $DefaultAssignee -EstimateText $estimateText

    if ((Normalize-Text -Value $liveIssue.body) -ne (Normalize-Text -Value $renderedBody)) {
        Write-Host "Refreshing planning links for issue #$($issue.number) '$($issue.title)'..."
        Invoke-GhApiJson -Route "repos/$repo/issues/$($issue.number)" -Method "PATCH" -Body @{
            body = $renderedBody
        } | Out-Null
    }
}

foreach ($issue in $issuesByNumber.Values) {
    $liveIssue = Invoke-GhApiJson -Route "repos/$repo/issues/$($issue.number)"
    $metadata = Parse-PlanningMetadataFromBody -Body $issue.body
    if ($null -eq $metadata.ParentIssueNumber) {
        continue
    }

    if (-not $issuesByNumber.ContainsKey([int]$metadata.ParentIssueNumber)) {
        continue
    }

    $parentIssue = $issuesByNumber[[int]$metadata.ParentIssueNumber]
    $parentDesired = $null
    if ($syncedIssues.ContainsKey([int]$metadata.ParentIssueNumber)) {
        $parentDesired = $syncedIssues[[int]$metadata.ParentIssueNumber].Desired
    }

    $projectItem = $null
    if ($null -ne $projectContext -and $projectContext.ItemsByIssueNumber.ContainsKey([int]$issue.number)) {
        $projectItem = $projectContext.ItemsByIssueNumber[[int]$issue.number]
    }

    $childContext = Get-ChildPlanningContext -ParentIssue $parentIssue -ParentDesired $parentDesired -RepositoryContext $repositoryContext -PhaseMilestones $phaseMilestones -BoardUrl $boardUrl
    $planningSection = Render-PlanningLinksSection -BacklogUrl $childContext.BacklogUrl -RoadmapUrl $childContext.RoadmapUrl -BoardUrl $childContext.BoardUrl -MilestoneTitle $childContext.MilestoneTitle -MilestoneUrl $childContext.MilestoneUrl -IterationTitle $(if (-not [string]::IsNullOrWhiteSpace((Get-ProjectItemIterationTitle -ProjectItem $projectItem))) { Get-ProjectItemIterationTitle -ProjectItem $projectItem } else { $metadata.IterationTitle }) -Status $(if (-not [string]::IsNullOrWhiteSpace((Get-ProjectItemStatusValue -ProjectItem $projectItem))) { Get-ProjectItemStatusValue -ProjectItem $projectItem } elseif ($issue.state -eq "CLOSED") { "Done" } else { "Todo" }) -IssueTypeName (Get-ChildIssueTypeName) -Assignee $DefaultAssignee -EstimateText $(if ($null -ne (Get-ProjectItemEstimateValue -ProjectItem $projectItem)) { Format-EstimateText -Estimate (Get-ProjectItemEstimateValue -ProjectItem $projectItem) } elseif ($null -ne $metadata.Estimate) { Format-EstimateText -Estimate $metadata.Estimate } else { "" }) -ParentTitle $childContext.ParentTitle -ParentUrl $childContext.ParentUrl
    $baseBody = Normalize-ChildIssueBody -Body $liveIssue.body
    $renderedBody = Upsert-ManagedSection -Body $baseBody -Key "planning-links" -RenderedSection $planningSection
    if ((Normalize-Text -Value $liveIssue.body) -ne (Normalize-Text -Value $renderedBody)) {
        Write-Host "Refreshing planning links for child issue #$($issue.number) '$($issue.title)'..."
        Invoke-GhApiJson -Route "repos/$repo/issues/$($issue.number)" -Method "PATCH" -Body @{
            body = $renderedBody
        } | Out-Null
    }
}

Write-Host "Planning sync completed for $repo."
