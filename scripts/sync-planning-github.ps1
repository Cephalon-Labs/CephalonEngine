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

    $arguments = @(
        "api",
        "-H", "Accept: application/vnd.github+json",
        "-H", "X-GitHub-Api-Version: 2026-03-10",
        $Route,
        "--method", $Method)
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

function Get-PlanningSyncKeyFromBody {
    param([AllowNull()][string]$Body)

    $normalizedBody = Normalize-Text -Value $Body
    $match = [regex]::Match($normalizedBody, "<!-- planning-sync:key=(?<key>[^>]+) -->")
    if ($match.Success) {
        return $match.Groups["key"].Value.Trim()
    }

    return $null
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
        "ENG-026" = 1
        "ENG-027" = 1
        "ENG-028" = 1
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
        "ENG-029" = 6
        "ENG-033" = 7
        "ENG-034" = 7
        "ENG-035" = 7
        "ENG-036" = 7
        "ENG-037" = 7
        "ENG-038" = 7
        "ENG-039" = 7
        "ENG-040" = 7
        "ENG-041" = 7
        "ENG-042" = 7
        "ENG-043" = 7
        "ENG-044" = 7
        "ENG-045" = 7
        "ENG-097" = 1
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
            $estimateMatch = [regex]::Match($body, "(?m)^Estimate:\s*(?<estimate>\d+(?:\.\d+)?)\s*$")
            $estimate = if ($estimateMatch.Success) { [decimal]::Parse($estimateMatch.Groups["estimate"].Value, [System.Globalization.CultureInfo]::InvariantCulture) } else { $null }
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
                PlanningStatus = $status
                SyncKey = $syncKey
                PhaseNumber = $phaseNumber
                EngCode = $engCode
                Estimate = $estimate
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

    $phase2State = if ((Normalize-Text -Value $phase2.Status).ToLowerInvariant().Contains("complete")) { "closed" } else { "open" }

    return @(
        [pscustomobject]@{
            Kind = "Roadmap"
            Title = "Phase 2 operational hardening follow-through"
            ContentBody = $phase2.Body
            State = $phase2State
            PlanningStatus = $phase2.Status
            SyncKey = "roadmap:phase-2"
            PhaseNumber = 2
            RoadmapPhaseTitle = $phase2.Title
            Estimate = $null
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

function Get-LabelState {
    param([Parameter(Mandatory = $true)]$RepositoryContext)

    $labels = @()
    $page = 1
    do {
        $pageLabels = @(Invoke-GhApiJson -Route "repos/$($RepositoryContext.FullName)/labels?per_page=100&page=$page")
        if ($pageLabels.Count -eq 0) {
            break
        }

        $labels += $pageLabels
        $page += 1
    }
    while ($pageLabels.Count -gt 0)

    $labelsByName = @{}
    foreach ($label in $labels) {
        $labelsByName[$label.name] = $label
    }

    return [pscustomobject]@{
        LabelsByName = $labelsByName
    }
}

function Ensure-ManagedLabel {
    param(
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [Parameter(Mandatory = $true)]$LabelState,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Color,
        [Parameter(Mandatory = $true)][string]$Description
    )

    if ($LabelState.LabelsByName.ContainsKey($Name)) {
        $existing = $LabelState.LabelsByName[$Name]
        if ($existing.color -ne $Color -or (Normalize-Text -Value $existing.description) -ne (Normalize-Text -Value $Description)) {
            Invoke-GhApiJson -Route ("repos/{0}/labels/{1}" -f $RepositoryContext.FullName, [System.Uri]::EscapeDataString($Name)) -Method "PATCH" -Body @{
                new_name = $Name
                color = $Color
                description = $Description
            } | Out-Null
        }
        return
    }

    $created = Invoke-GhApiJson -Route "repos/$($RepositoryContext.FullName)/labels" -Method "POST" -Body @{
        name = $Name
        color = $Color
        description = $Description
    }
    $LabelState.LabelsByName[$Name] = $created
}

function Ensure-ManagedPlanningLabels {
    param(
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [Parameter(Mandatory = $true)]$LabelState,
        [Parameter(Mandatory = $true)]$DesiredIssues,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$DesiredIterationTitles
    )

    $baseLabels = @(
        @{ Name = "planning"; Color = "0e8a16"; Description = "Managed planning issue synced from roadmap or backlog." },
        @{ Name = "planning:legacy"; Color = "cfd3d7"; Description = "Managed historical planning issue retained for traceability only." },
        @{ Name = "planning:superseded"; Color = "8a63d2"; Description = "Managed historical planning issue replaced by a canonical synced issue." },
        @{ Name = "kind:epic"; Color = "5319e7"; Description = "Managed planning epic / top-level work item." },
        @{ Name = "kind:task"; Color = "1d76db"; Description = "Managed planning child task." },
        @{ Name = "source:backlog"; Color = "bfdadc"; Description = "Managed planning item sourced from docs/engine-backlog.md." },
        @{ Name = "source:roadmap"; Color = "d4c5f9"; Description = "Managed planning item sourced from docs/engine-roadmap.md." }
    )

    foreach ($label in $baseLabels) {
        Ensure-ManagedLabel -RepositoryContext $RepositoryContext -LabelState $LabelState -Name $label.Name -Color $label.Color -Description $label.Description
    }

    $phaseNumbers = $DesiredIssues | Where-Object { $null -ne $_.PhaseNumber } | Select-Object -ExpandProperty PhaseNumber -Unique
    foreach ($phaseNumber in $phaseNumbers) {
        $phaseLabel = Get-PhaseLabelName -PhaseNumber $phaseNumber
        if (-not [string]::IsNullOrWhiteSpace($phaseLabel)) {
            Ensure-ManagedLabel -RepositoryContext $RepositoryContext -LabelState $LabelState -Name $phaseLabel -Color (Get-PhaseLabelColor -PhaseNumber $phaseNumber) -Description (Get-PhaseLabelDescription -PhaseNumber $phaseNumber)
        }
    }

    foreach ($spec in $DesiredIssues) {
        $trackLabel = Get-TrackLabelNameForSpec -Spec $spec
        if (-not [string]::IsNullOrWhiteSpace($trackLabel)) {
            Ensure-ManagedLabel -RepositoryContext $RepositoryContext -LabelState $LabelState -Name $trackLabel -Color (Get-TrackLabelColorForSpec -Spec $spec) -Description (Get-TrackLabelDescriptionForSpec -Spec $spec)
        }
    }

    foreach ($iterationTitle in ($DesiredIterationTitles | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)) {
        $iterationLabel = Get-IterationLabelName -IterationTitle $iterationTitle -PlanningStatus $null
        if (-not [string]::IsNullOrWhiteSpace($iterationLabel)) {
            Ensure-ManagedLabel -RepositoryContext $RepositoryContext -LabelState $LabelState -Name $iterationLabel -Color (Get-IterationLabelColor -IterationTitle $iterationTitle -PlanningStatus $null) -Description (Get-IterationLabelDescription -IterationTitle $iterationTitle -PlanningStatus $null)
        }
    }
}

function Set-IssueManagedLabels {
    param(
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [Parameter(Mandatory = $true)]$Issue,
        [Parameter(Mandatory = $true)][string[]]$ManagedLabels
    )

    $existingLabels = @()
    if ($Issue.PSObject.Properties["labels"] -and $null -ne $Issue.labels) {
        foreach ($label in $Issue.labels) {
            if ($label -is [string]) {
                $existingLabels += $label
            }
            elseif ($label.PSObject.Properties["name"]) {
                $existingLabels += [string]$label.name
            }
        }
    }

    $preservedLabels = @($existingLabels | Where-Object { -not (Test-IsManagedLabel -LabelName $_) })
    $finalLabels = @($preservedLabels + $ManagedLabels | Select-Object -Unique)
    $currentManaged = @($existingLabels | Where-Object { Test-IsManagedLabel -LabelName $_ } | Sort-Object)
    $desiredManaged = @($ManagedLabels | Sort-Object)

    if (([string]::Join("|", $currentManaged)) -eq ([string]::Join("|", $desiredManaged)) -and ([string]::Join("|", ($existingLabels | Sort-Object))) -eq ([string]::Join("|", ($finalLabels | Sort-Object)))) {
        return
    }

    Invoke-GhApiJson -Route "repos/$($RepositoryContext.FullName)/issues/$($Issue.number)" -Method "PATCH" -Body @{
        labels = $finalLabels
    } | Out-Null
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
        "(?ms)^##\s+(?:Sprint history and next 3 sprints|Sprint history and next 4 sprints|Recommended next 3 sprints)\r?\n(?<body>.*?)(?=^##\s+|\z)")

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

function Test-IsLaterPlanningStatus {
    param([AllowNull()][string]$PlanningStatus)

    if ([string]::IsNullOrWhiteSpace($PlanningStatus)) {
        return $false
    }

    $normalized = $PlanningStatus.ToLowerInvariant()
    return $normalized.Contains("later") -or $normalized.Contains("future") -or $normalized.Contains("deferred")
}

function Get-ResolvedIterationTitle {
    param(
        [AllowNull()][string]$IterationTitle,
        [AllowNull()][string]$PlanningStatus
    )

    if (-not [string]::IsNullOrWhiteSpace($IterationTitle)) {
        return $IterationTitle.Trim()
    }

    if (Test-IsLaterPlanningStatus -PlanningStatus $PlanningStatus) {
        return "Later / not scheduled yet"
    }

    return $null
}

function ConvertTo-IterationSlug {
    param([AllowNull()][string]$IterationTitle)

    if ([string]::IsNullOrWhiteSpace($IterationTitle)) {
        return $null
    }

    $slug = ($IterationTitle.ToLowerInvariant() -replace "[^a-z0-9]+", "-").Trim("-")
    if ([string]::IsNullOrWhiteSpace($slug)) {
        return $null
    }

    return $slug
}

function Get-PlanningIterationTitles {
    param(
        [Parameter(Mandatory = $true)]$DesiredIssues,
        [Parameter(Mandatory = $true)]$TopLevelIterationMap
    )

    $titles = [System.Collections.Generic.List[string]]::new()
    foreach ($value in $TopLevelIterationMap.Values) {
        if (-not [string]::IsNullOrWhiteSpace([string]$value) -and -not $titles.Contains([string]$value)) {
            $titles.Add([string]$value)
        }
    }

    foreach ($issue in $DesiredIssues) {
        $title = Get-ResolvedIterationTitle -IterationTitle $null -PlanningStatus $issue.PlanningStatus
        if (-not [string]::IsNullOrWhiteSpace($title) -and -not $titles.Contains($title)) {
            $titles.Add($title)
        }
    }

    return [string[]]$titles.ToArray()
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

function Get-ManagedLabelPrefixes {
    return @(
        "planning",
        "planning:",
        "kind:",
        "source:",
        "phase:",
        "iteration:",
        "track:"
    )
}

function Test-IsManagedLabel {
    param([AllowNull()][string]$LabelName)

    if ([string]::IsNullOrWhiteSpace($LabelName)) {
        return $false
    }

    foreach ($prefix in Get-ManagedLabelPrefixes) {
        if ($prefix -eq "planning" -and $LabelName -eq "planning") {
            return $true
        }

        if ($prefix -ne "planning" -and $LabelName.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Get-PhaseLabelName {
    param($PhaseNumber)

    switch ([int]$PhaseNumber) {
        0 { return "phase:0-foundation" }
        1 { return "phase:1-sdk-hardening" }
        2 { return "phase:2-operational" }
        3 { return "phase:3-extensibility" }
        4 { return "phase:4-orchestration" }
        5 { return "phase:5-solution-platform" }
        6 { return "phase:6-cloud-platform" }
        7 { return "phase:7-adoption-ops" }
        default { return $null }
    }
}

function Get-PhaseLabelDescription {
    param($PhaseNumber)

    switch ([int]$PhaseNumber) {
        0 { return "Planning work aligned to Phase 0 foundation hardening." }
        1 { return "Planning work aligned to Phase 1 SDK hardening and adoption." }
        2 { return "Planning work aligned to Phase 2 operational hardening." }
        3 { return "Planning work aligned to Phase 3 extensibility and package loading." }
        4 { return "Planning work aligned to Phase 4 execution and orchestration." }
        5 { return "Planning work aligned to Phase 5 solution-level platform work." }
        6 { return "Planning work aligned to Phase 6 cloud and platform integrations." }
        7 { return "Planning work aligned to Phase 7 external adoption and operator readiness." }
        default { return "Planning work aligned to a roadmap phase." }
    }
}

function Get-PhaseLabelColor {
    param($PhaseNumber)

    switch ([int]$PhaseNumber) {
        0 { return "5319e7" }
        1 { return "1d76db" }
        2 { return "fbca04" }
        3 { return "0e8a16" }
        4 { return "d93f0b" }
        5 { return "b60205" }
        6 { return "0052cc" }
        7 { return "5319e7" }
        default { return "cfd3d7" }
    }
}

function Get-IterationLabelName {
    param([AllowNull()][string]$IterationTitle, [AllowNull()][string]$PlanningStatus)

    $resolvedTitle = Get-ResolvedIterationTitle -IterationTitle $IterationTitle -PlanningStatus $PlanningStatus
    $slug = ConvertTo-IterationSlug -IterationTitle $resolvedTitle
    if (-not [string]::IsNullOrWhiteSpace($slug)) {
        return "iteration:$slug"
    }

    return $null
}

function Get-IterationLabelDescription {
    param(
        [AllowNull()][string]$IterationTitle,
        [AllowNull()][string]$PlanningStatus
    )

    $resolvedTitle = Get-ResolvedIterationTitle -IterationTitle $IterationTitle -PlanningStatus $PlanningStatus
    if ([string]::IsNullOrWhiteSpace($resolvedTitle)) {
        return "Tracked to a planning iteration."
    }

    if ($resolvedTitle -eq "Later / not scheduled yet") {
        return "Tracked as later / not scheduled yet."
    }

    return "Tracked to iteration '$resolvedTitle'."
}

function Get-IterationLabelColor {
    param(
        [AllowNull()][string]$IterationTitle,
        [AllowNull()][string]$PlanningStatus
    )

    $resolvedTitle = Get-ResolvedIterationTitle -IterationTitle $IterationTitle -PlanningStatus $PlanningStatus
    if ([string]::IsNullOrWhiteSpace($resolvedTitle)) {
        return "ededed"
    }

    switch ($resolvedTitle.ToLowerInvariant()) {
        "sprint 1" { return "c2e0c6" }
        "sprint 2" { return "c5def5" }
        "sprint 3" { return "fef2c0" }
        "sprint 4" { return "f9d0c4" }
        "sprint 5" { return "bfdadc" }
        "sprint 6" { return "d4c5f9" }
        "sprint 7" { return "ead1dc" }
        "sprint 8" { return "d0e0e3" }
        "sprint 9" { return "d9ead3" }
        "sprint 10" { return "fff2cc" }
        "sprint 11" { return "cfe2f3" }
        "sprint 12" { return "fce5cd" }
        "sprint 13" { return "b4a7d6" }
        "sprint 14" { return "c9daf8" }
        "sprint 15" { return "d9ead3" }
        "later / not scheduled yet" { return "f9d0c4" }
    }

    if ($resolvedTitle.ToLowerInvariant().Contains("foundation")) {
        return "d4c5f9"
    }

    if ($resolvedTitle.ToLowerInvariant().Contains("adoption")) {
        return "bfd4f2"
    }

    if ($resolvedTitle.ToLowerInvariant().Contains("operational")) {
        return "fbca04"
    }

    if ($resolvedTitle.ToLowerInvariant().Contains("platform")) {
        return "c2e0c6"
    }

    return "ededed"
}

function Get-TrackLabelNameForSpec {
    param($Spec)

    if ($null -eq $Spec) {
        return $null
    }

    if ($Spec.Kind -eq "Backlog" -and -not [string]::IsNullOrWhiteSpace($Spec.EngCode)) {
        return "track:$($Spec.EngCode.ToLowerInvariant())"
    }

    if ($Spec.Kind -eq "Roadmap" -and $null -ne $Spec.PhaseNumber) {
        return "track:phase-$($Spec.PhaseNumber)"
    }

    return $null
}

function Get-TrackLabelDescriptionForSpec {
    param($Spec)

    if ($null -eq $Spec) {
        return "Planning track label."
    }

    if ($Spec.Kind -eq "Backlog" -and -not [string]::IsNullOrWhiteSpace($Spec.EngCode)) {
        return "Planning track for $($Spec.EngCode)."
    }

    if ($Spec.Kind -eq "Roadmap" -and $null -ne $Spec.PhaseNumber) {
        return "Planning track for roadmap phase $($Spec.PhaseNumber)."
    }

    return "Planning track label."
}

function Get-TrackLabelColorForSpec {
    param($Spec)

    if ($null -ne $Spec -and $null -ne $Spec.PhaseNumber) {
        return Get-PhaseLabelColor -PhaseNumber $Spec.PhaseNumber
    }

    return "bfd4f2"
}

function Get-ManagedTopLevelLabels {
    param(
        [Parameter(Mandatory = $true)]$Spec,
        [AllowNull()][string]$IterationTitle
    )

    $labels = @("planning", "kind:epic")
    if ($Spec.Kind -eq "Backlog") {
        $labels += "source:backlog"
    }
    elseif ($Spec.Kind -eq "Roadmap") {
        $labels += "source:roadmap"
    }

    $phaseLabel = Get-PhaseLabelName -PhaseNumber $Spec.PhaseNumber
    if (-not [string]::IsNullOrWhiteSpace($phaseLabel)) {
        $labels += $phaseLabel
    }

    $trackLabel = Get-TrackLabelNameForSpec -Spec $Spec
    if (-not [string]::IsNullOrWhiteSpace($trackLabel)) {
        $labels += $trackLabel
    }

    $iterationLabel = Get-IterationLabelName -IterationTitle $IterationTitle -PlanningStatus $Spec.PlanningStatus
    if (-not [string]::IsNullOrWhiteSpace($iterationLabel)) {
        $labels += $iterationLabel
    }

    return $labels | Select-Object -Unique
}

function Get-ManagedChildLabels {
    param(
        [Parameter(Mandatory = $true)]$ParentSpec,
        [AllowNull()][string]$IterationTitle
    )

    $labels = @("planning", "kind:task")

    $phaseLabel = Get-PhaseLabelName -PhaseNumber $ParentSpec.PhaseNumber
    if (-not [string]::IsNullOrWhiteSpace($phaseLabel)) {
        $labels += $phaseLabel
    }

    $trackLabel = Get-TrackLabelNameForSpec -Spec $ParentSpec
    if (-not [string]::IsNullOrWhiteSpace($trackLabel)) {
        $labels += $trackLabel
    }

    $iterationLabel = Get-IterationLabelName -IterationTitle $IterationTitle -PlanningStatus $ParentSpec.PlanningStatus
    if (-not [string]::IsNullOrWhiteSpace($iterationLabel)) {
        $labels += $iterationLabel
    }

    return $labels | Select-Object -Unique
}

function Get-LegacyPlanningIterationTitle {
    param(
        [AllowNull()][string]$SyncKey,
        [AllowNull()][string]$IssueTitle
    )

    if (-not [string]::IsNullOrWhiteSpace($IssueTitle) -and $IssueTitle -match "^Sprint\s+\d+$") {
        return $IssueTitle.Trim()
    }

    if (-not [string]::IsNullOrWhiteSpace($SyncKey) -and $SyncKey -match "^backlog:sprint-(?<number>\d+)$") {
        return "Sprint $($Matches.number)"
    }

    return $null
}

function Get-ManagedLegacyPlanningLabels {
    param(
        [AllowNull()][string]$SyncKey,
        [AllowNull()]$CanonicalSpec,
        [AllowNull()][string]$IterationTitle,
        [switch]$IsSuperseded
    )

    $labels = @("planning", "planning:legacy")
    if ($IsSuperseded) {
        $labels += "planning:superseded"
    }

    if ($null -ne $CanonicalSpec) {
        $labels += Get-ManagedTopLevelLabels -Spec $CanonicalSpec -IterationTitle $IterationTitle
        return $labels | Select-Object -Unique
    }

    if (-not [string]::IsNullOrWhiteSpace($SyncKey)) {
        if ($SyncKey.StartsWith("backlog:", [System.StringComparison]::OrdinalIgnoreCase)) {
            $labels += "source:backlog"
        }
        elseif ($SyncKey.StartsWith("roadmap:", [System.StringComparison]::OrdinalIgnoreCase)) {
            $labels += "source:roadmap"
        }
    }

    $iterationLabel = Get-IterationLabelName -IterationTitle $IterationTitle -PlanningStatus $null
    if (-not [string]::IsNullOrWhiteSpace($iterationLabel)) {
        $labels += $iterationLabel
    }

    return $labels | Select-Object -Unique
}

function Render-LegacyPlanningStatusSection {
    param(
        [Parameter(Mandatory = $true)]$Issue,
        [AllowNull()][string]$SyncKey,
        [AllowNull()]$CanonicalIssue,
        [AllowNull()][string]$BoardUrl,
        [AllowNull()][string]$IterationTitle
    )

    $lines = @(
        "<!-- planning-sync:section=legacy-status:start -->"
        "## Legacy planning status"
        ""
    )

    if ($null -ne $CanonicalIssue) {
        $lines += "- Canonical issue: [#$($CanonicalIssue.number) $($CanonicalIssue.title)]($($CanonicalIssue.url))"
        $lines += "- Tracking note: this older planning issue is retained only for historical traceability and should not be used for progress or completion reporting."
    }
    elseif (-not [string]::IsNullOrWhiteSpace($IterationTitle)) {
        $lines += "- Replaced by project iteration tracking: **$IterationTitle**"
        $lines += "- Tracking note: this planning placeholder is retained only as historical context and is no longer part of the active issue workflow."
    }
    else {
        $lines += "- Tracking note: this planning issue is retained only as historical context and is no longer the canonical source of truth."
    }

    if (-not [string]::IsNullOrWhiteSpace($BoardUrl)) {
        $lines += "- Active board: [@Cephalon-Engine]($BoardUrl)"
    }

    if (-not [string]::IsNullOrWhiteSpace($SyncKey)) {
        $lines += "- Planning sync key: ``$SyncKey``"
    }

    $lines += "<!-- planning-sync:section=legacy-status:end -->"
    return ($lines -join "`n").Trim()
}

function Sync-LegacyPlanningIssues {
    param(
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [Parameter(Mandatory = $true)][string]$RepositoryFullName,
        [Parameter(Mandatory = $true)]$IssuesByNumber,
        [Parameter(Mandatory = $true)]$DesiredIssuesBySyncKey,
        [Parameter(Mandatory = $true)]$CanonicalIssueNumbersBySyncKey,
        [Parameter(Mandatory = $true)]$CanonicalIterationTitlesBySyncKey,
        [AllowNull()][string]$BoardUrl
    )

    foreach ($issue in ($IssuesByNumber.Values | Sort-Object number)) {
        $syncKey = Get-PlanningSyncKeyFromBody -Body $issue.body
        if ([string]::IsNullOrWhiteSpace($syncKey)) {
            continue
        }

        $canonicalIssue = $null
        $canonicalSpec = $null
        $isLegacy = $false

        if ($CanonicalIssueNumbersBySyncKey.ContainsKey($syncKey)) {
            $canonicalIssueNumber = [int]$CanonicalIssueNumbersBySyncKey[$syncKey]
            if ([int]$issue.number -ne $canonicalIssueNumber) {
                $isLegacy = $true
                if ($IssuesByNumber.ContainsKey($canonicalIssueNumber)) {
                    $canonicalIssue = $IssuesByNumber[$canonicalIssueNumber]
                }
                if ($DesiredIssuesBySyncKey.ContainsKey($syncKey)) {
                    $canonicalSpec = $DesiredIssuesBySyncKey[$syncKey]
                }
            }
        }
        elseif (-not $DesiredIssuesBySyncKey.ContainsKey($syncKey)) {
            $isLegacy = $true
        }

        if (-not $isLegacy) {
            continue
        }

        $iterationTitle = $null
        if ($CanonicalIterationTitlesBySyncKey.ContainsKey($syncKey)) {
            $iterationTitle = [string]$CanonicalIterationTitlesBySyncKey[$syncKey]
        }
        if ([string]::IsNullOrWhiteSpace($iterationTitle)) {
            $iterationTitle = Get-LegacyPlanningIterationTitle -SyncKey $syncKey -IssueTitle $issue.title
        }

        $desiredLabels = Get-ManagedLegacyPlanningLabels -SyncKey $syncKey -CanonicalSpec $canonicalSpec -IterationTitle $iterationTitle -IsSuperseded:($null -ne $canonicalIssue)
        Set-IssueManagedLabels -RepositoryContext $RepositoryContext -Issue $issue -ManagedLabels $desiredLabels

        $legacySection = Render-LegacyPlanningStatusSection -Issue $issue -SyncKey $syncKey -CanonicalIssue $canonicalIssue -BoardUrl $BoardUrl -IterationTitle $iterationTitle
        $renderedBody = Upsert-ManagedSection -Body $issue.body -Key "legacy-status" -RenderedSection $legacySection
        if ((Normalize-Text -Value $issue.body) -ne (Normalize-Text -Value $renderedBody)) {
            Write-Host "Marking legacy planning issue #$($issue.number) '$($issue.title)'..."
            Invoke-GhApiJson -Route "repos/$RepositoryFullName/issues/$($issue.number)" -Method "PATCH" -Body @{
                body = $renderedBody
            } | Out-Null
        }
    }
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
        [string]$TestStatus,
        [string]$BenchmarkStatus,
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

    if (-not [string]::IsNullOrWhiteSpace($TestStatus)) {
        $lines += "- Test gate: $TestStatus"
    }

    if (-not [string]::IsNullOrWhiteSpace($BenchmarkStatus)) {
        $lines += "- Benchmark gate: $BenchmarkStatus"
    }

    $lines += "<!-- planning-sync:section=planning-links:end -->"
    return ($lines -join "`n").Trim()
}

function Render-ChildTasksSection {
    param(
        [Parameter(Mandatory = $true)]$ChildItems
    )

    $lines = @(
        "<!-- planning-sync:section=child-tasks:start -->"
        "## Child tasks"
        ""
    )

    if ($ChildItems.Count -eq 0) {
        $lines += "- No linked child tasks yet."
    }
    else {
        foreach ($child in $ChildItems) {
            $details = @()
            if (-not [string]::IsNullOrWhiteSpace($child.Status)) {
                $details += "status: $($child.Status)"
            }
            if (-not [string]::IsNullOrWhiteSpace($child.IterationTitle)) {
                $details += "iteration: $($child.IterationTitle)"
            }
            if (-not [string]::IsNullOrWhiteSpace($child.EstimateText)) {
                $details += "estimate: $($child.EstimateText)"
            }

            $suffix = if ($details.Count -gt 0) { " - " + ($details -join "; ") } else { "" }
            $lines += "- [#$($child.Number) $($child.Title)]($($child.Url))$suffix"
        }
    }

    $lines += "<!-- planning-sync:section=child-tasks:end -->"
    return ($lines -join "`n").Trim()
}

function Upsert-ManagedSection {
    param(
        [AllowNull()][string]$Body,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$RenderedSection
    )

    $normalizedBody = Normalize-Text -Value $Body
    $startMarker = "<!-- planning-sync:section=${Key}:start -->"
    $endMarker = "<!-- planning-sync:section=${Key}:end -->"
    $escapedStartMarker = [regex]::Escape($startMarker)
    $escapedEndMarker = [regex]::Escape($endMarker)
    $pattern = "(?ms)$escapedStartMarker.*?$escapedEndMarker"

    $strippedBody = [regex]::Replace($normalizedBody, $pattern, "")
    $strippedBody = [regex]::Replace(
        $strippedBody,
        "(?m)^\s*(?:$escapedStartMarker|$escapedEndMarker)\s*$\r?\n?",
        "")
    $strippedBody = [regex]::Replace($strippedBody.Trim(), '(\r?\n){3,}', "`n`n")

    if ($normalizedBody -match $pattern -or $strippedBody -ne $normalizedBody.Trim()) {
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

    $normalizedBody = [regex]::Replace($normalizedBody, '(?m)^Item type:\s+\*\*Draft item\*\*$', 'Item type: **Issue**')
    $normalizedBody = [regex]::Replace(
        $normalizedBody,
        '(?ms)^## Planning links\s*\r?\n.*?(?=(?:<!-- planning-sync:section=planning-links:start -->|\z))',
        '')

    return $normalizedBody.Trim()
}

function Set-ChildPlanningMetadataBody {
    param(
        [AllowNull()][string]$Body,
        [AllowNull()][string]$IterationTitle,
        [AllowNull()]$Estimate
    )

    $renderedBody = Normalize-ChildIssueBody -Body $Body
    if ([string]::IsNullOrWhiteSpace($renderedBody)) {
        return $renderedBody
    }

    if (-not [string]::IsNullOrWhiteSpace($IterationTitle)) {
        $renderedBody = [regex]::Replace(
            $renderedBody,
            '(?m)^Planned iteration:\s+\*\*.+?\*\*\s*$',
            ('Planned iteration: **{0}**' -f $IterationTitle.Trim()))
    }

    if ($null -ne $Estimate) {
        $estimateText = [string]::Format([System.Globalization.CultureInfo]::InvariantCulture, "{0:0.##}", [decimal]$Estimate)
        $renderedBody = [regex]::Replace(
            $renderedBody,
            '(?m)^Estimate:\s+\*\*.+?\*\*\s*$',
            ('Estimate: **{0}**' -f $estimateText))
    }

    return $renderedBody
}

function Get-UnmanagedIssueBody {
    param([AllowNull()][string]$Body)

    $normalizedBody = Normalize-Text -Value $Body
    if ([string]::IsNullOrWhiteSpace($normalizedBody)) {
        return $normalizedBody
    }

    $strippedBody = $normalizedBody
    foreach ($key in @("planning-links", "child-tasks")) {
        $startMarker = "<!-- planning-sync:section=${Key}:start -->"
        $endMarker = "<!-- planning-sync:section=${Key}:end -->"
        $escapedStartMarker = [regex]::Escape($startMarker)
        $escapedEndMarker = [regex]::Escape($endMarker)
        $pattern = "(?ms)$escapedStartMarker.*?$escapedEndMarker"
        $strippedBody = [regex]::Replace($strippedBody, $pattern, "")
        $strippedBody = [regex]::Replace(
            $strippedBody,
            "(?m)^\s*(?:$escapedStartMarker|$escapedEndMarker)\s*$\r?\n?",
            "")
    }

    $strippedBody = [regex]::Replace($strippedBody, "<!-- planning-sync:key=[^>]+ -->", "")
    $strippedBody = [regex]::Replace(
        $strippedBody,
        '(?ms)^## Planning links\s*\r?\n.*?(?=(?:^##\s+|\z))',
        '')

    return $strippedBody.Trim()
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
        [string]$EstimateText,
        [string]$TestStatus,
        [string]$BenchmarkStatus
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
    $lines += (Render-PlanningLinksSection -BacklogUrl $BacklogUrl -RoadmapUrl $RoadmapUrl -BoardUrl $BoardUrl -MilestoneTitle $MilestoneTitle -MilestoneUrl $MilestoneUrl -IterationTitle $IterationTitle -Status $Status -IssueTypeName $IssueTypeName -Assignee $Assignee -EstimateText $EstimateText -TestStatus $TestStatus -BenchmarkStatus $BenchmarkStatus)

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

function Get-ManagedProjectSingleSelectFields {
    return @(
        [pscustomobject]@{
            Name = "Test"
            PropertyName = "test"
            Options = @(
                [pscustomobject]@{ Name = "N/A"; Description = "No explicit automated test gate is required for this planning item." }
                [pscustomobject]@{ Name = "Needed"; Description = "Relevant automated tests should be added or validated before completion." }
                [pscustomobject]@{ Name = "Running"; Description = "Automated test validation is actively in progress." }
                [pscustomobject]@{ Name = "Passed"; Description = "Relevant automated tests passed for this work item." }
                [pscustomobject]@{ Name = "Failed"; Description = "Relevant automated tests failed and need follow-up." }
            )
        }
        [pscustomobject]@{
            Name = "Benchmark"
            PropertyName = "benchmark"
            Options = @(
                [pscustomobject]@{ Name = "N/A"; Description = "No benchmark or guardrail validation is required for this planning item." }
                [pscustomobject]@{ Name = "Needed"; Description = "Benchmark or guardrail validation should happen before completion." }
                [pscustomobject]@{ Name = "Running"; Description = "Benchmark or performance validation is actively in progress." }
                [pscustomobject]@{ Name = "Passed"; Description = "Benchmark or guardrail validation passed for this work item." }
                [pscustomobject]@{ Name = "Regressed"; Description = "Benchmark validation found a regression that needs follow-up." }
            )
        }
    )
}

function Ensure-ManagedProjectSingleSelectFields {
    param(
        [Parameter(Mandatory = $true)]$ProjectView,
        [Parameter(Mandatory = $true)][string]$Owner,
        [Parameter(Mandatory = $true)][int]$ProjectNumber
    )

    $createdAny = $false
    foreach ($spec in Get-ManagedProjectSingleSelectFields) {
        $existingField = $ProjectView.fields.nodes | Where-Object { $_.name -eq $spec.Name } | Select-Object -First 1
        if ($null -eq $existingField) {
            Write-Host "Creating project field '$($spec.Name)'..."
            Invoke-GhNoJson -Arguments @(
                "project", "field-create", $ProjectNumber.ToString(),
                "--owner", $Owner,
                "--name", $spec.Name,
                "--data-type", "SINGLE_SELECT",
                "--single-select-options", (($spec.Options | ForEach-Object { $_.Name }) -join ",")
            )
            $createdAny = $true
            continue
        }

        if ($existingField.PSObject.Properties["options"] -and $null -ne $existingField.options) {
            $existingOptionNames = @($existingField.options | ForEach-Object { [string]$_.name })
            $desiredOptionNames = @($spec.Options | ForEach-Object { [string]$_.Name })
            if (([string]::Join("|", $existingOptionNames)) -ne ([string]::Join("|", $desiredOptionNames))) {
                Write-Warning "Project field '$($spec.Name)' already exists with a different option set. The sync script will preserve the current field but may not be able to apply every default value."
            }
        }
    }

    return $createdAny
}

function Get-ManagedProjectStatusOptions {
    return @(
        [pscustomobject]@{
            Name = "Todo"
            Color = "GREEN"
            Description = "This item hasn't been started"
        }
        [pscustomobject]@{
            Name = "In progress"
            Color = "YELLOW"
            Description = "This is actively being worked on"
        }
        [pscustomobject]@{
            Name = "Validation"
            Color = "BLUE"
            Description = "This is ready for validation before completion"
        }
        [pscustomobject]@{
            Name = "Done"
            Color = "PURPLE"
            Description = "This has been completed"
        }
    )
}

function Get-ProjectStatusSnapshot {
    param([Parameter(Mandatory = $true)][string]$Owner, [Parameter(Mandatory = $true)][int]$ProjectNumber)

    $items = Invoke-GhJson -Arguments @("project", "item-list", $ProjectNumber.ToString(), "--owner", $Owner, "-L", "1000", "--format", "json")
    $snapshots = [System.Collections.Generic.List[object]]::new()
    foreach ($item in $items.items) {
        $statusProperty = $item.PSObject.Properties["status"]
        $statusValue = if ($null -ne $statusProperty -and $null -ne $statusProperty.Value) { [string]$statusProperty.Value } else { "" }
        if ([string]::IsNullOrWhiteSpace($statusValue)) {
            continue
        }

        $snapshots.Add([pscustomobject]@{
            ItemId = [string]$item.id
            Status = $statusValue
        })
    }

    return @($snapshots)
}

function Restore-ProjectStatusSnapshot {
    param(
        [Parameter(Mandatory = $true)]$ProjectContext,
        [Parameter(Mandatory = $true)][object[]]$StatusSnapshot
    )

    foreach ($entry in $StatusSnapshot) {
        if ($null -eq $entry -or [string]::IsNullOrWhiteSpace([string]$entry.ItemId) -or [string]::IsNullOrWhiteSpace([string]$entry.Status)) {
            continue
        }

        if (-not $ProjectContext.StatusOptionIds.ContainsKey([string]$entry.Status)) {
            continue
        }

        Invoke-GhNoJson -Arguments @(
            "project", "item-edit",
            "--id", [string]$entry.ItemId,
            "--project-id", $ProjectContext.ProjectId,
            "--field-id", $ProjectContext.StatusFieldId,
            "--single-select-option-id", $ProjectContext.StatusOptionIds[[string]$entry.Status]
        )
    }
}

function Ensure-ManagedProjectStatusFieldOptions {
    param(
        [Parameter(Mandatory = $true)]$ProjectView,
        [Parameter(Mandatory = $true)][string]$Owner,
        [Parameter(Mandatory = $true)][int]$ProjectNumber
    )

    $statusField = $ProjectView.fields.nodes | Where-Object { $_.name -eq "Status" } | Select-Object -First 1
    if ($null -eq $statusField) {
        throw "Unable to find the Status field for project $Owner/$ProjectNumber."
    }

    $existingOptions = @($statusField.options | ForEach-Object { [string]$_.name })
    $desiredOptions = @((Get-ManagedProjectStatusOptions) | ForEach-Object { [string]$_.Name })
    if (([string]::Join("|", $existingOptions)) -eq ([string]::Join("|", $desiredOptions))) {
        return $null
    }

    Write-Host "Updating project field 'Status' to include managed validation flow..."
    $statusSnapshot = Get-ProjectStatusSnapshot -Owner $Owner -ProjectNumber $ProjectNumber

    $optionLiterals = @(
        (Get-ManagedProjectStatusOptions | ForEach-Object {
            ('{{name:{0}, color:{1}, description:{2}}}' -f
                (ConvertTo-GraphQlStringLiteral -Value $_.Name),
                $_.Color,
                (ConvertTo-GraphQlStringLiteral -Value $_.Description))
        })
    ) -join ", "

    $mutation = @"
mutation(`$fieldId: ID!) {
  updateProjectV2Field(
    input: {
      fieldId: `$fieldId
      singleSelectOptions: [$optionLiterals]
    }
  ) {
    projectV2Field {
      ... on ProjectV2SingleSelectField {
        id
        name
      }
    }
  }
}
"@

    Invoke-GhNoJson -Arguments @(
        "api", "graphql",
        "-f", "fieldId=$($statusField.id)",
        "-f", "query=$mutation"
    )

    return $statusSnapshot
}

function Get-ManagedProjectViews {
    return @(
        [pscustomobject]@{
            Name = "Validation"
            Layout = "table"
            Filter = "test:Needed,Running,Failed"
            VisibleFieldNames = @("Title", "Assignees", "Status", "Milestone", "Parent issue", "Estimate", "Iteration", "Test", "Benchmark")
        }
        [pscustomobject]@{
            Name = "Benchmarks"
            Layout = "table"
            Filter = "benchmark:Needed,Running,Regressed"
            VisibleFieldNames = @("Title", "Assignees", "Status", "Milestone", "Parent issue", "Estimate", "Iteration", "Test", "Benchmark")
        }
    )
}

function Get-ProjectViewLayoutName {
    param([Parameter(Mandatory = $true)][string]$Layout)

    switch ($Layout.ToLowerInvariant()) {
        "table" { return "TABLE_LAYOUT" }
        "board" { return "BOARD_LAYOUT" }
        "roadmap" { return "ROADMAP_LAYOUT" }
        default { return $Layout }
    }
}

function Get-ProjectRestRouteBase {
    param(
        [Parameter(Mandatory = $true)][string]$OwnerKind,
        [Parameter(Mandatory = $true)][string]$OwnerRestIdentifier,
        [Parameter(Mandatory = $true)][int]$ProjectNumber
    )

    switch ($OwnerKind) {
        "organization" { return "orgs/$OwnerRestIdentifier/projectsV2/$ProjectNumber" }
        "user" { return "users/$OwnerRestIdentifier/projectsV2/$ProjectNumber" }
        default { return $null }
    }
}

function Get-ManagedProjectViewFieldIds {
    param(
        [Parameter(Mandatory = $true)]$ProjectRestFields,
        [Parameter(Mandatory = $true)][string[]]$VisibleFieldNames,
        [Parameter(Mandatory = $true)][string]$ViewName
    )

    $fieldIds = [System.Collections.Generic.List[int]]::new()
    foreach ($fieldName in $VisibleFieldNames) {
        $field = $ProjectRestFields | Where-Object { [string]$_.name -eq $fieldName } | Select-Object -First 1
        if ($null -eq $field) {
            Write-Warning "Managed project view '$ViewName' could not find field '$fieldName'."
            continue
        }

        $fieldIds.Add([int]$field.id)
    }

    return ,([int[]]$fieldIds.ToArray())
}

function Ensure-ManagedProjectViews {
    param(
        [Parameter(Mandatory = $true)]$ProjectView,
        [Parameter(Mandatory = $true)][string]$RestRouteBase
    )

    if ([string]::IsNullOrWhiteSpace($RestRouteBase)) {
        return $false
    }

    $projectRestFields = @(Invoke-GhApiJson -Route "$RestRouteBase/fields")
    $createdAny = $false

    foreach ($spec in Get-ManagedProjectViews) {
        $existingView = $ProjectView.views.nodes | Where-Object { $_.name -eq $spec.Name } | Select-Object -First 1
        $visibleFieldIds = Get-ManagedProjectViewFieldIds -ProjectRestFields $projectRestFields -VisibleFieldNames $spec.VisibleFieldNames -ViewName $spec.Name
        if ($visibleFieldIds.Count -eq 0) {
            Write-Warning "Skipping managed project view '$($spec.Name)' because no visible fields could be resolved."
            continue
        }

        if ($null -eq $existingView) {
            Write-Host "Creating project view '$($spec.Name)'..."
            Invoke-GhApiJson -Route "$RestRouteBase/views" -Method "POST" -Body @{
                name = $spec.Name
                layout = $spec.Layout
                filter = $spec.Filter
                visible_fields = $visibleFieldIds
            } | Out-Null
            $createdAny = $true
            continue
        }

        $expectedLayout = Get-ProjectViewLayoutName -Layout $spec.Layout
        if ($existingView.layout -ne $expectedLayout -or
            (Normalize-Text -Value ([string]$existingView.filter)) -ne (Normalize-Text -Value ([string]$spec.Filter))) {
            Write-Warning "Managed project view '$($spec.Name)' already exists with a different layout or filter. The sync script will preserve the current view."
        }
    }

    return $createdAny
}

function Get-ProjectContext {
    param(
        [Parameter(Mandatory = $true)][string]$Owner,
        [Parameter(Mandatory = $true)][int]$ProjectNumber,
        [switch]$SkipManagedFieldInitialization
    )

    $orgProjectQuery = @'
query($owner: String!, $number: Int!) {
  organization(login: $owner) {
    projectV2(number: $number) {
      id
      url
      views(first: 50) {
        nodes {
          id
          name
          layout
          filter
          fields(first: 20) {
            nodes {
              ... on ProjectV2FieldCommon {
                id
                name
              }
              ... on ProjectV2IterationField {
                id
                name
              }
            }
          }
        }
      }
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
                startDate
                duration
              }
              completedIterations {
                id
                title
                startDate
                duration
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
    databaseId
    projectV2(number: $number) {
      id
      url
      views(first: 50) {
        nodes {
          id
          name
          layout
          filter
          fields(first: 20) {
            nodes {
              ... on ProjectV2FieldCommon {
                id
                name
              }
              ... on ProjectV2IterationField {
                id
                name
              }
            }
          }
        }
      }
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
                startDate
                duration
              }
              completedIterations {
                id
                title
                startDate
                duration
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
    $ownerKind = $null
    $ownerRestIdentifier = $null
    try {
        $projectResponse = Invoke-GhJson -Arguments @(
            "api", "graphql",
            "-f", "owner=$Owner",
            "-F", "number=$ProjectNumber",
            "-f", "query=$orgProjectQuery")
        $projectView = $projectResponse.data.organization.projectV2
        if ($null -ne $projectView) {
            $ownerKind = "organization"
            $ownerRestIdentifier = $Owner
        }
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
        if ($null -ne $projectView) {
            $ownerKind = "user"
            $ownerRestIdentifier = [string]$projectResponse.data.user.databaseId
        }
    }

    if ($null -eq $projectView) {
        throw "Unable to load project $Owner/$ProjectNumber."
    }

    if (-not $SkipManagedFieldInitialization) {
        $createdManagedFields = Ensure-ManagedProjectSingleSelectFields -ProjectView $projectView -Owner $Owner -ProjectNumber $ProjectNumber
        if ($createdManagedFields) {
            return Get-ProjectContext -Owner $Owner -ProjectNumber $ProjectNumber -SkipManagedFieldInitialization
        }
    }

    $statusSnapshot = Ensure-ManagedProjectStatusFieldOptions -ProjectView $projectView -Owner $Owner -ProjectNumber $ProjectNumber
    if ($null -ne $statusSnapshot) {
        $refreshedContext = Get-ProjectContext -Owner $Owner -ProjectNumber $ProjectNumber -SkipManagedFieldInitialization
        Restore-ProjectStatusSnapshot -ProjectContext $refreshedContext -StatusSnapshot $statusSnapshot
        return Get-ProjectContext -Owner $Owner -ProjectNumber $ProjectNumber -SkipManagedFieldInitialization
    }

    $restRouteBase = Get-ProjectRestRouteBase -OwnerKind $ownerKind -OwnerRestIdentifier $ownerRestIdentifier -ProjectNumber $ProjectNumber
    $createdManagedViews = Ensure-ManagedProjectViews -ProjectView $projectView -RestRouteBase $restRouteBase
    if ($createdManagedViews) {
        return Get-ProjectContext -Owner $Owner -ProjectNumber $ProjectNumber -SkipManagedFieldInitialization
    }

    $items = Invoke-GhJson -Arguments @("project", "item-list", $ProjectNumber.ToString(), "--owner", $Owner, "-L", "1000", "--format", "json")

    $statusField = $projectView.fields.nodes | Where-Object { $_.name -eq "Status" } | Select-Object -First 1
    if ($null -eq $statusField) {
        throw "Unable to find the Status field for project $Owner/$ProjectNumber."
    }

    $estimateField = $projectView.fields.nodes | Where-Object { $_.name -eq "Estimate" } | Select-Object -First 1
    $iterationField = $projectView.fields.nodes | Where-Object { $_.name -eq "Iteration" } | Select-Object -First 1
    $managedFieldContexts = @{}
    foreach ($managedField in Get-ManagedProjectSingleSelectFields) {
        $existingField = $projectView.fields.nodes | Where-Object { $_.name -eq $managedField.Name } | Select-Object -First 1
        if ($null -eq $existingField) {
            continue
        }

        $optionIds = @{}
        if ($existingField.PSObject.Properties["options"] -and $null -ne $existingField.options) {
            foreach ($option in $existingField.options) {
                $optionIds[[string]$option.name] = $option.id
            }
        }

        $managedFieldContexts[$managedField.Name] = [pscustomobject]@{
            Name = $managedField.Name
            PropertyName = $managedField.PropertyName
            FieldId = $existingField.id
            OptionIds = $optionIds
        }
    }

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
        IterationFieldName = if ($null -ne $iterationField) { $iterationField.name } else { $null }
        IterationIdsByTitle = $iterationIdsByTitle
        IterationConfigurations = if ($null -ne $iterationField -and $null -ne $iterationField.configuration) { @($iterationField.configuration.iterations) } else { @() }
        ManagedSingleSelectFields = $managedFieldContexts
        ProjectViews = if ($projectView.PSObject.Properties["views"] -and $null -ne $projectView.views) { @($projectView.views.nodes) } else { @() }
        ProjectRestRouteBase = $restRouteBase
    }
}

function ConvertTo-GraphQlStringLiteral {
    param([AllowNull()][string]$Value)

    return ($Value | ConvertTo-Json -Compress)
}

function ConvertTo-IsoDateLiteral {
    param([AllowNull()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    $match = [regex]::Match($Value, '^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})$')
    if ($match.Success) {
        $year = [int]$match.Groups["year"].Value
        if ($year -gt 2200) {
            $year -= 543
        }

        return "{0:D4}-{1}-{2}" -f $year, $match.Groups["month"].Value, $match.Groups["day"].Value
    }

    return ([datetime]::Parse($Value, [System.Globalization.CultureInfo]::InvariantCulture)).ToString("yyyy-MM-dd", [System.Globalization.CultureInfo]::InvariantCulture)
}

function Get-DesiredProjectIterationTitles {
    param(
        [Parameter(Mandatory = $true)]$DesiredIssues,
        [Parameter(Mandatory = $true)]$TopLevelIterationMap,
        [Parameter(Mandatory = $true)]$ProjectContext,
        [Parameter(Mandatory = $true)]$ExistingIssues
    )

    $titles = [System.Collections.Generic.List[string]]::new()

    foreach ($value in $TopLevelIterationMap.Values) {
        if (-not [string]::IsNullOrWhiteSpace([string]$value) -and -not $titles.Contains([string]$value)) {
            $titles.Add([string]$value)
        }
    }

    foreach ($issue in $DesiredIssues) {
        $resolvedTitle = Get-ResolvedIterationTitle -IterationTitle $null -PlanningStatus $issue.PlanningStatus
        if (-not [string]::IsNullOrWhiteSpace($resolvedTitle) -and -not $titles.Contains($resolvedTitle)) {
            $titles.Add($resolvedTitle)
        }
    }

    foreach ($draftItem in $ProjectContext.DraftItems) {
        if ($null -eq $draftItem.content -or -not $draftItem.content.PSObject.Properties["body"]) {
            continue
        }

        $metadata = Parse-PlanningMetadataFromBody -Body $draftItem.content.body
        if (-not [string]::IsNullOrWhiteSpace($metadata.IterationTitle) -and -not $titles.Contains($metadata.IterationTitle)) {
            $titles.Add($metadata.IterationTitle)
        }
    }

    foreach ($issue in $ExistingIssues) {
        $metadata = Parse-PlanningMetadataFromBody -Body $issue.body
        if (-not [string]::IsNullOrWhiteSpace($metadata.IterationTitle) -and -not $titles.Contains($metadata.IterationTitle)) {
            $titles.Add($metadata.IterationTitle)
        }
    }

    return @($titles)
}

function Ensure-ProjectIterations {
    param(
        [Parameter(Mandatory = $true)]$ProjectContext,
        [Parameter(Mandatory = $true)][string]$ProjectOwner,
        [Parameter(Mandatory = $true)][int]$ProjectNumber,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$DesiredIterationTitles
    )

    if ($null -eq $ProjectContext.IterationFieldId -or $null -eq $DesiredIterationTitles -or $DesiredIterationTitles.Count -eq 0) {
        return $ProjectContext
    }

    $missingTitles = @(
        $DesiredIterationTitles |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and -not $ProjectContext.IterationIdsByTitle.ContainsKey($_) } |
            Select-Object -Unique
    )

    if ($ProjectContext.IterationConfigurations.Count -eq 0) {
        Write-Warning "Iteration field '$($ProjectContext.IterationFieldName)' has no configured iterations. Skipping automatic iteration creation."
        return $ProjectContext
    }

    $configuredIterations = @()
    $needsNormalization = $false
    foreach ($iteration in $ProjectContext.IterationConfigurations | Sort-Object startDate) {
        $normalizedStartDate = ConvertTo-IsoDateLiteral -Value ([string]$iteration.startDate)
        if ($normalizedStartDate -ne [string]$iteration.startDate) {
            $needsNormalization = $true
        }

        $configuredIterations += [pscustomobject]@{
            Title = [string]$iteration.title
            StartDate = $normalizedStartDate
            Duration = [int]$iteration.duration
        }
    }

    if ($missingTitles.Count -eq 0 -and -not $needsNormalization) {
        return $ProjectContext
    }

    $defaultDuration = [int]$configuredIterations[0].Duration
    if ($defaultDuration -le 0) {
        $defaultDuration = 7
    }

    $lastIteration = $configuredIterations[-1]
    $nextStartDate = ([datetime]$lastIteration.StartDate).AddDays([int]$lastIteration.Duration)

    foreach ($title in $missingTitles) {
        Write-Host "Adding iteration '$title' to project $($ProjectContext.ProjectId)..."
        $configuredIterations += [pscustomobject]@{
            Title = $title
            StartDate = $nextStartDate.ToString("yyyy-MM-dd", [System.Globalization.CultureInfo]::InvariantCulture)
            Duration = $defaultDuration
        }
        $nextStartDate = $nextStartDate.AddDays($defaultDuration)
    }

    $iterationInputs = @(
        foreach ($iteration in $configuredIterations) {
            ("{{ title: {0}, startDate: {1}, duration: {2} }}" -f
                (ConvertTo-GraphQlStringLiteral -Value $iteration.Title),
                (ConvertTo-GraphQlStringLiteral -Value $iteration.StartDate),
                $iteration.Duration)
        }
    )

    $mutation = @"
mutation {
  updateProjectV2Field(input: {
    fieldId: $(ConvertTo-GraphQlStringLiteral -Value $ProjectContext.IterationFieldId),
    iterationConfiguration: {
      startDate: $(ConvertTo-GraphQlStringLiteral -Value $configuredIterations[0].StartDate),
      duration: $defaultDuration,
      iterations: [
        $($iterationInputs -join ",`n        ")
      ]
    }
  }) {
    projectV2Field {
      ... on ProjectV2IterationField {
        id
      }
    }
  }
}
"@

    Invoke-GhNoJson -Arguments @(
        "api", "graphql",
        "-f", "query=$mutation"
    )

    return (Get-ProjectContext -Owner $ProjectOwner -ProjectNumber $ProjectNumber)
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

    foreach ($attempt in 1..5) {
        $refreshed = Invoke-GhJson -Arguments @("project", "item-list", $ProjectNumber.ToString(), "--owner", $ProjectOwner, "-L", "1000", "--format", "json")
        foreach ($item in $refreshed.items) {
            if ($null -ne $item.content -and $item.content.type -eq "Issue" -and [int]$item.content.number -eq [int]$Issue.number) {
                $ProjectContext.ItemsByIssueNumber[[int]$Issue.number] = $item
                return $item
            }
        }

        if ($attempt -lt 5) {
            Start-Sleep -Seconds 2
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

    $currentStatus = Get-ProjectItemStatusValue -ProjectItem $ProjectItem

    $shouldUpdate = $false
    if ($DesiredStatus -eq "Done") {
        $shouldUpdate = $currentStatus -ne "Done"
    }
    elseif ($DesiredStatus -eq "Validation") {
        $shouldUpdate = $currentStatus -ne "Validation"
    }
    elseif ($DesiredStatus -eq "In progress") {
        $shouldUpdate = [string]::IsNullOrWhiteSpace($currentStatus) -or $currentStatus -eq "Todo" -or $currentStatus -eq "Done"
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

    $ProjectItem | Add-Member -NotePropertyName status -NotePropertyValue $DesiredStatus -Force
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

function Get-ProjectManagedSingleSelectValue {
    param(
        $ProjectItem,
        [AllowNull()][string]$PropertyName
    )

    if ($null -eq $ProjectItem -or [string]::IsNullOrWhiteSpace($PropertyName)) {
        return $null
    }

    $property = $ProjectItem.PSObject.Properties[$PropertyName]
    if ($null -eq $property -or $null -eq $property.Value) {
        return $null
    }

    if ($property.Value -is [string]) {
        return [string]$property.Value
    }

    if ($property.Value.PSObject.Properties["name"] -and $null -ne $property.Value.name) {
        return [string]$property.Value.name
    }

    return [string]$property.Value
}

function Set-ProjectManagedSingleSelectValue {
    param(
        [Parameter(Mandatory = $true)]$ProjectContext,
        [Parameter(Mandatory = $true)]$ProjectItem,
        [Parameter(Mandatory = $true)][string]$FieldName,
        [AllowNull()][string]$DesiredValue,
        [string[]]$MutableCurrentValues = @()
    )

    if ([string]::IsNullOrWhiteSpace($DesiredValue)) {
        return
    }

    if (-not $ProjectContext.ManagedSingleSelectFields.ContainsKey($FieldName)) {
        return
    }

    $fieldContext = $ProjectContext.ManagedSingleSelectFields[$FieldName]
    if ($null -eq $fieldContext -or [string]::IsNullOrWhiteSpace([string]$fieldContext.FieldId)) {
        return
    }

    if (-not $fieldContext.OptionIds.ContainsKey($DesiredValue)) {
        Write-Warning "Project field '$FieldName' does not contain option '$DesiredValue'."
        return
    }

    $currentValue = Get-ProjectManagedSingleSelectValue -ProjectItem $ProjectItem -PropertyName $fieldContext.PropertyName
    if ($currentValue -eq $DesiredValue) {
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($currentValue) -and $MutableCurrentValues.Count -gt 0 -and ($MutableCurrentValues -notcontains $currentValue)) {
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($currentValue) -and $MutableCurrentValues.Count -eq 0) {
        return
    }

    Invoke-GhNoJson -Arguments @(
        "project", "item-edit",
        "--id", $ProjectItem.id,
        "--project-id", $ProjectContext.ProjectId,
        "--field-id", $fieldContext.FieldId,
        "--single-select-option-id", $fieldContext.OptionIds[$DesiredValue]
    )

    $ProjectItem | Add-Member -NotePropertyName $fieldContext.PropertyName -NotePropertyValue $DesiredValue -Force
}

function Test-IsDocumentationOnlyWorkItem {
    param([AllowNull()][string]$Title)

    if ([string]::IsNullOrWhiteSpace($Title)) {
        return $false
    }

    $normalized = $Title.ToLowerInvariant()
    return $normalized.StartsWith("document ") -or
        $normalized.Contains("inventory") -or
        $normalized.Contains("planning") -or
        $normalized.Contains("documentation") -or
        $normalized.Contains(" guide") -or
        $normalized.Contains("guidance") -or
        $normalized.Contains("readme")
}

function Test-IsCurrentFocusWorkItem {
    param([AllowNull()][string]$Body)

    $unmanagedBody = Get-UnmanagedIssueBody -Body $Body
    if ([string]::IsNullOrWhiteSpace($unmanagedBody)) {
        return $false
    }

    return [regex]::IsMatch($unmanagedBody, "(?im)^Status:\s*current focus\s*$")
}

function Test-RequiresBenchmarkValidation {
    param(
        [AllowNull()][string]$Title,
        [AllowNull()][string]$Body
    )

    $normalized = ((Normalize-Text -Value $Title) + "`n" + (Get-UnmanagedIssueBody -Body $Body)).ToLowerInvariant()
    foreach ($keyword in @("benchmark", "guardrail", "performance", "hot path", "cephalon.benchmarks")) {
        if ($normalized.Contains($keyword)) {
            return $true
        }
    }

    return $false
}

function Get-DesiredTestFieldValue {
    param(
        [AllowNull()][string]$Title,
        [AllowNull()][string]$Body,
        [AllowNull()][string]$State
    )

    if (Test-IsDocumentationOnlyWorkItem -Title $Title) {
        return "N/A"
    }

    if (-not [string]::IsNullOrWhiteSpace($State) -and $State.ToLowerInvariant() -eq "closed") {
        return "Passed"
    }

    return "Needed"
}

function Get-DesiredBenchmarkFieldValue {
    param(
        [AllowNull()][string]$Title,
        [AllowNull()][string]$Body,
        [AllowNull()][string]$State
    )

    if (-not (Test-RequiresBenchmarkValidation -Title $Title -Body $Body)) {
        return "N/A"
    }

    if (-not [string]::IsNullOrWhiteSpace($State) -and $State.ToLowerInvariant() -eq "closed") {
        return "Passed"
    }

    return "Needed"
}

function Sync-ProjectValidationFields {
    param(
        [Parameter(Mandatory = $true)]$ProjectContext,
        [Parameter(Mandatory = $true)]$ProjectItem,
        [AllowNull()][string]$Title,
        [AllowNull()][string]$Body,
        [AllowNull()][string]$State
    )

    $desiredTest = Get-DesiredTestFieldValue -Title $Title -Body $Body -State $State
    switch ($desiredTest) {
        "Passed" {
            Set-ProjectManagedSingleSelectValue -ProjectContext $ProjectContext -ProjectItem $ProjectItem -FieldName "Test" -DesiredValue $desiredTest -MutableCurrentValues @("Needed", "Running", "Failed")
        }
        "Needed" {
            Set-ProjectManagedSingleSelectValue -ProjectContext $ProjectContext -ProjectItem $ProjectItem -FieldName "Test" -DesiredValue $desiredTest -MutableCurrentValues @("N/A", "Passed", "Failed")
        }
        "N/A" {
            Set-ProjectManagedSingleSelectValue -ProjectContext $ProjectContext -ProjectItem $ProjectItem -FieldName "Test" -DesiredValue $desiredTest -MutableCurrentValues @("Needed", "Running", "Passed", "Failed")
        }
    }

    $desiredBenchmark = Get-DesiredBenchmarkFieldValue -Title $Title -Body $Body -State $State
    switch ($desiredBenchmark) {
        "Passed" {
            Set-ProjectManagedSingleSelectValue -ProjectContext $ProjectContext -ProjectItem $ProjectItem -FieldName "Benchmark" -DesiredValue $desiredBenchmark -MutableCurrentValues @("Needed", "Running", "Regressed")
        }
        "Needed" {
            Set-ProjectManagedSingleSelectValue -ProjectContext $ProjectContext -ProjectItem $ProjectItem -FieldName "Benchmark" -DesiredValue $desiredBenchmark -MutableCurrentValues @("N/A", "Passed", "Regressed")
        }
        "N/A" {
            Set-ProjectManagedSingleSelectValue -ProjectContext $ProjectContext -ProjectItem $ProjectItem -FieldName "Benchmark" -DesiredValue $desiredBenchmark -MutableCurrentValues @("Needed", "Running", "Passed", "Regressed")
        }
    }
}

function Get-EffectiveValidationFieldValue {
    param(
        $ProjectContext,
        $ProjectItem,
        [Parameter(Mandatory = $true)][string]$FieldName,
        [AllowNull()][string]$Title,
        [AllowNull()][string]$Body,
        [AllowNull()][string]$State
    )

    if ($null -ne $ProjectContext -and $ProjectContext.ManagedSingleSelectFields.ContainsKey($FieldName)) {
        $propertyName = [string]$ProjectContext.ManagedSingleSelectFields[$FieldName].PropertyName
        $currentValue = Get-ProjectManagedSingleSelectValue -ProjectItem $ProjectItem -PropertyName $propertyName
        if (-not [string]::IsNullOrWhiteSpace($currentValue)) {
            return $currentValue
        }
    }

    switch ($FieldName) {
        "Test" { return Get-DesiredTestFieldValue -Title $Title -Body $Body -State $State }
        "Benchmark" { return Get-DesiredBenchmarkFieldValue -Title $Title -Body $Body -State $State }
        default { return $null }
    }
}

function Test-IsValidationGateSatisfied {
    param([AllowNull()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    return $Value -eq "Passed" -or $Value -eq "N/A"
}

function Get-DesiredProjectStatus {
    param(
        [Parameter(Mandatory = $true)]$ProjectContext,
        $ProjectItem,
        [AllowNull()][string]$Title,
        [AllowNull()][string]$Body,
        [AllowNull()][string]$State
    )

    $currentStatus = Get-ProjectItemStatusValue -ProjectItem $ProjectItem
    $testStatus = Get-EffectiveValidationFieldValue -ProjectContext $ProjectContext -ProjectItem $ProjectItem -FieldName "Test" -Title $Title -Body $Body -State $State
    $benchmarkStatus = Get-EffectiveValidationFieldValue -ProjectContext $ProjectContext -ProjectItem $ProjectItem -FieldName "Benchmark" -Title $Title -Body $Body -State $State
    $gatesSatisfied = (Test-IsValidationGateSatisfied -Value $testStatus) -and (Test-IsValidationGateSatisfied -Value $benchmarkStatus)

    if (-not [string]::IsNullOrWhiteSpace($State) -and $State.ToLowerInvariant() -eq "closed") {
        if ($gatesSatisfied) {
            return "Done"
        }

        return "Validation"
    }

    if (Test-IsCurrentFocusWorkItem -Body $Body) {
        if ($gatesSatisfied) {
            return "Validation"
        }

        return "In progress"
    }

    if ($currentStatus -eq "Validation") {
        if ($gatesSatisfied) {
            return "Validation"
        }

        return "In progress"
    }

    if ($currentStatus -eq "In progress") {
        if ($gatesSatisfied) {
            return "Validation"
        }

        return "In progress"
    }

    if ($currentStatus -eq "Done") {
        if ($gatesSatisfied) {
            return "Validation"
        }

        return "In progress"
    }

    return "Todo"
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

function Get-IssueParent {
    param(
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [Parameter(Mandatory = $true)][int]$IssueNumber
    )

    $query = @'
query($owner: String!, $name: String!, $number: Int!) {
  repository(owner: $owner, name: $name) {
    issue(number: $number) {
      parent {
        id
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

    return [pscustomobject]@{
        Id = $response.data.repository.issue.parent.id
        Number = [int]$response.data.repository.issue.parent.number
    }
}

function Ensure-SubIssueLink {
    param(
        [Parameter(Mandatory = $true)]$RepositoryContext,
        [Parameter(Mandatory = $true)]$ParentIssue,
        [Parameter(Mandatory = $true)]$ChildIssue
    )

    $currentParent = Get-IssueParent -RepositoryContext $RepositoryContext -IssueNumber ([int]$ChildIssue.number)
    if ($null -ne $currentParent -and $currentParent.Number -eq [int]$ParentIssue.number) {
        return
    }

    if ($null -ne $currentParent) {
        $removeMutation = @'
mutation($issueId: ID!, $subIssueId: ID!) {
  removeSubIssue(input: {
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
            "-f", "issueId=$($currentParent.Id)",
            "-f", "subIssueId=$($ChildIssue.id)",
            "-f", "query=$removeMutation"
        )
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
        "--json", "id,number,title,state,body,url,milestone,assignees,labels"
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
    else {
        $iterationTitle = Get-ResolvedIterationTitle -IterationTitle $null -PlanningStatus $Spec.PlanningStatus
    }

    $backlogUrl = $null
    if ($Spec.Kind -eq "Backlog") {
        $backlogUrl = Get-RepositoryDocumentUrl -RepositoryContext $RepositoryContext -Path $Spec.SourcePath -Anchor (Get-GitHubAnchorSlug -Value $Spec.Title)
    }
    elseif ($Spec.Kind -eq "Roadmap" -and $null -ne $Spec.PhaseNumber) {
        $backlogAnchorTitle = switch ([int]$Spec.PhaseNumber) {
            2 { "Current operational focus" }
            default { $iterationTitle }
        }

        if (-not [string]::IsNullOrWhiteSpace($backlogAnchorTitle)) {
            $backlogUrl = Get-RepositoryDocumentUrl -RepositoryContext $RepositoryContext -Path "docs/engine-backlog.md" -Anchor (Get-GitHubAnchorSlug -Value $backlogAnchorTitle)
        }
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
$desiredIterationTitles = @(Get-PlanningIterationTitles -DesiredIssues $desiredIssues -TopLevelIterationMap $topLevelIterationMap)
$labelState = Get-LabelState -RepositoryContext $repositoryContext
Ensure-ManagedPlanningLabels -RepositoryContext $repositoryContext -LabelState $labelState -DesiredIssues $desiredIssues -DesiredIterationTitles $desiredIterationTitles

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
if ($null -ne $projectContext) {
    $desiredIterationTitles = @(Get-DesiredProjectIterationTitles -DesiredIssues $desiredIssues -TopLevelIterationMap $topLevelIterationMap -ProjectContext $projectContext -ExistingIssues $existingIssues)
    $projectContext = Ensure-ProjectIterations -ProjectContext $projectContext -ProjectOwner $ProjectOwner -ProjectNumber $ProjectNumber -DesiredIterationTitles $desiredIterationTitles
}

$issueIndexByKey = @{}
$issueIndexByTitle = @{}
$desiredIssuesBySyncKey = @{}
foreach ($issue in $existingIssues) {
    $syncKey = Get-PlanningSyncKeyFromBody -Body $issue.body
    if (-not [string]::IsNullOrWhiteSpace($syncKey)) {
        if (-not $issueIndexByKey.ContainsKey($syncKey) -or [int]$issue.number -lt [int]$issueIndexByKey[$syncKey].number) {
            $issueIndexByKey[$syncKey] = $issue
        }
    }

    if (-not $issueIndexByTitle.ContainsKey($issue.title) -or [int]$issue.number -lt [int]$issueIndexByTitle[$issue.title].number) {
        $issueIndexByTitle[$issue.title] = $issue
    }
}

foreach ($desiredIssue in $desiredIssues) {
    $desiredIssuesBySyncKey[$desiredIssue.SyncKey] = $desiredIssue
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
$canonicalIssueNumbersBySyncKey = @{}
$canonicalIterationTitlesBySyncKey = @{}
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
    $desiredLabels = Get-ManagedTopLevelLabels -Spec $desiredIssue -IterationTitle $topLevelContext.IterationTitle
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
    elseif ($null -ne $desiredIssue.Estimate) {
        Format-EstimateText -Estimate $desiredIssue.Estimate
    }
    else {
        ""
    }

    $testStatus = Get-EffectiveValidationFieldValue -ProjectContext $projectContext -ProjectItem $currentProjectItem -FieldName "Test" -Title $desiredIssue.Title -Body $desiredIssue.ContentBody -State $desiredIssue.State
    $benchmarkStatus = Get-EffectiveValidationFieldValue -ProjectContext $projectContext -ProjectItem $currentProjectItem -FieldName "Benchmark" -Title $desiredIssue.Title -Body $desiredIssue.ContentBody -State $desiredIssue.State

    $renderedBody = Render-SyncedIssueBody -Spec $desiredIssue -RepositoryContext $repositoryContext -BacklogUrl $topLevelContext.BacklogUrl -RoadmapUrl $topLevelContext.RoadmapUrl -BoardUrl $topLevelContext.BoardUrl -MilestoneTitle $topLevelContext.MilestoneTitle -MilestoneUrl $topLevelContext.MilestoneUrl -IterationTitle $topLevelContext.IterationTitle -Status $statusText -IssueTypeName (Get-TopLevelIssueTypeName) -Assignee $DefaultAssignee -EstimateText $estimateText -TestStatus $testStatus -BenchmarkStatus $benchmarkStatus

    if ($null -eq $existingIssue) {
        Write-Host "Creating issue '$($desiredIssue.Title)'..."
        $body = @{
            title = $desiredIssue.Title
            body = $renderedBody
            assignees = @($DefaultAssignee)
            labels = $desiredLabels
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

    Set-IssueManagedLabels -RepositoryContext $repositoryContext -Issue $existingIssue -ManagedLabels $desiredLabels

    $issueIndexByKey[$desiredIssue.SyncKey] = $existingIssue
    $issueIndexByTitle[$existingIssue.title] = $existingIssue
    $canonicalIssueNumbersBySyncKey[$desiredIssue.SyncKey] = [int]$existingIssue.number
    $canonicalIterationTitlesBySyncKey[$desiredIssue.SyncKey] = $topLevelContext.IterationTitle
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

        if (-not [string]::IsNullOrWhiteSpace($syncEntry.Value.IterationTitle)) {
            Set-ProjectIteration -ProjectContext $projectContext -ProjectItem $projectItem -IterationTitle $syncEntry.Value.IterationTitle
        }

        $targetEstimate = $null
        if ($null -ne $desired.Estimate) {
            $targetEstimate = $desired.Estimate
        }
        elseif ($parentEstimateRollups.ContainsKey([int]$issue.number)) {
            $targetEstimate = $parentEstimateRollups[[int]$issue.number]
        }

        if ($null -ne $targetEstimate) {
            Set-ProjectEstimate -ProjectContext $projectContext -ProjectItem $projectItem -Estimate $targetEstimate
        }

        Sync-ProjectValidationFields -ProjectContext $projectContext -ProjectItem $projectItem -Title $desired.Title -Body $desired.ContentBody -State $desired.State
        $desiredStatus = Get-DesiredProjectStatus -ProjectContext $projectContext -ProjectItem $projectItem -Title $desired.Title -Body $desired.ContentBody -State $desired.State
        Set-ProjectStatus -ProjectContext $projectContext -ProjectItem $projectItem -DesiredStatus $desiredStatus
    }

    $projectContext = Get-ProjectContext -Owner $ProjectOwner -ProjectNumber $ProjectNumber
}

foreach ($issue in $issuesByNumber.Values) {
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
    if ($null -eq $parentDesired) {
        continue
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

    $projectItem = $null
    if ($null -ne $projectContext) {
        $projectItem = Ensure-ProjectItem -ProjectContext $projectContext -ProjectOwner $ProjectOwner -ProjectNumber $ProjectNumber -Issue $issue
        $stateText = if ($issue.state -eq "CLOSED") { "closed" } else { "open" }

        $projectEstimate = Get-ProjectItemEstimateValue -ProjectItem $projectItem
        if (($null -eq $projectEstimate) -and $null -ne $metadata.Estimate) {
            Set-ProjectEstimate -ProjectContext $projectContext -ProjectItem $projectItem -Estimate $metadata.Estimate
            $projectEstimate = $metadata.Estimate
        }

        $projectIterationTitle = Get-ProjectItemIterationTitle -ProjectItem $projectItem
        if ([string]::IsNullOrWhiteSpace($projectIterationTitle) -and -not [string]::IsNullOrWhiteSpace($metadata.IterationTitle)) {
            Set-ProjectIteration -ProjectContext $projectContext -ProjectItem $projectItem -IterationTitle $metadata.IterationTitle
            $projectIterationTitle = $metadata.IterationTitle
        }

        Sync-ProjectValidationFields -ProjectContext $projectContext -ProjectItem $projectItem -Title $issue.title -Body $issue.body -State $stateText
        $desiredStatus = Get-DesiredProjectStatus -ProjectContext $projectContext -ProjectItem $projectItem -Title $issue.title -Body $issue.body -State $stateText
        Set-ProjectStatus -ProjectContext $projectContext -ProjectItem $projectItem -DesiredStatus $desiredStatus
    }

    $effectiveIterationTitle = if ($null -ne $projectItem -and -not [string]::IsNullOrWhiteSpace((Get-ProjectItemIterationTitle -ProjectItem $projectItem))) {
        Get-ProjectItemIterationTitle -ProjectItem $projectItem
    }
    else {
        $metadata.IterationTitle
    }
    $effectiveEstimate = if ($null -ne $projectItem -and $null -ne (Get-ProjectItemEstimateValue -ProjectItem $projectItem)) {
        Get-ProjectItemEstimateValue -ProjectItem $projectItem
    }
    else {
        $metadata.Estimate
    }

    $issuePatchBody = Set-ChildPlanningMetadataBody -Body $issue.body -IterationTitle $effectiveIterationTitle -Estimate $effectiveEstimate
    $childDesiredLabels = Get-ManagedChildLabels -ParentSpec $parentDesired -IterationTitle $effectiveIterationTitle

    Set-IssueType -RepositoryContext $repositoryContext -Issue $issue -IssueTypeName (Get-ChildIssueTypeName)
    Ensure-SubIssueLink -RepositoryContext $repositoryContext -ParentIssue $parentIssue -ChildIssue $issue
    Set-IssueManagedLabels -RepositoryContext $repositoryContext -Issue $issue -ManagedLabels $childDesiredLabels

    if ($issuePatch.Count -gt 0 -or (Normalize-Text -Value $issue.body) -ne (Normalize-Text -Value $issuePatchBody)) {
        if ((Normalize-Text -Value $issue.body) -ne (Normalize-Text -Value $issuePatchBody)) {
            $issuePatch.body = $issuePatchBody
        }

        Write-Host "Aligning metadata for child issue #$($issue.number) '$($issue.title)'..."
        Invoke-GhApiJson -Route "repos/$repo/issues/$($issue.number)" -Method "PATCH" -Body $issuePatch | Out-Null
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
    $testStatus = Get-EffectiveValidationFieldValue -ProjectContext $projectContext -ProjectItem $projectItem -FieldName "Test" -Title $desired.Title -Body $desired.ContentBody -State $desired.State
    $benchmarkStatus = Get-EffectiveValidationFieldValue -ProjectContext $projectContext -ProjectItem $projectItem -FieldName "Benchmark" -Title $desired.Title -Body $desired.ContentBody -State $desired.State
    $renderedBody = Render-SyncedIssueBody -Spec $desired -RepositoryContext $repositoryContext -BacklogUrl $topLevelContext.BacklogUrl -RoadmapUrl $topLevelContext.RoadmapUrl -BoardUrl $topLevelContext.BoardUrl -MilestoneTitle $topLevelContext.MilestoneTitle -MilestoneUrl $topLevelContext.MilestoneUrl -IterationTitle $(if (-not [string]::IsNullOrWhiteSpace((Get-ProjectItemIterationTitle -ProjectItem $projectItem))) { Get-ProjectItemIterationTitle -ProjectItem $projectItem } else { $topLevelContext.IterationTitle }) -Status $statusText -IssueTypeName (Get-TopLevelIssueTypeName) -Assignee $DefaultAssignee -EstimateText $estimateText -TestStatus $testStatus -BenchmarkStatus $benchmarkStatus

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
    $effectiveIterationTitle = if (-not [string]::IsNullOrWhiteSpace((Get-ProjectItemIterationTitle -ProjectItem $projectItem))) { Get-ProjectItemIterationTitle -ProjectItem $projectItem } else { $metadata.IterationTitle }
    $effectiveEstimate = if ($null -ne (Get-ProjectItemEstimateValue -ProjectItem $projectItem)) { Get-ProjectItemEstimateValue -ProjectItem $projectItem } else { $metadata.Estimate }
    $planningSection = Render-PlanningLinksSection -BacklogUrl $childContext.BacklogUrl -RoadmapUrl $childContext.RoadmapUrl -BoardUrl $childContext.BoardUrl -MilestoneTitle $childContext.MilestoneTitle -MilestoneUrl $childContext.MilestoneUrl -IterationTitle $effectiveIterationTitle -Status $(if (-not [string]::IsNullOrWhiteSpace((Get-ProjectItemStatusValue -ProjectItem $projectItem))) { Get-ProjectItemStatusValue -ProjectItem $projectItem } elseif ($issue.state -eq "CLOSED") { "Done" } else { "Todo" }) -IssueTypeName (Get-ChildIssueTypeName) -Assignee $DefaultAssignee -EstimateText $(if ($null -ne $effectiveEstimate) { Format-EstimateText -Estimate $effectiveEstimate } else { "" }) -TestStatus (Get-EffectiveValidationFieldValue -ProjectContext $projectContext -ProjectItem $projectItem -FieldName "Test" -Title $issue.title -Body $issue.body -State $(if ($issue.state -eq "CLOSED") { "closed" } else { "open" })) -BenchmarkStatus (Get-EffectiveValidationFieldValue -ProjectContext $projectContext -ProjectItem $projectItem -FieldName "Benchmark" -Title $issue.title -Body $issue.body -State $(if ($issue.state -eq "CLOSED") { "closed" } else { "open" })) -ParentTitle $childContext.ParentTitle -ParentUrl $childContext.ParentUrl
    $baseBody = Set-ChildPlanningMetadataBody -Body $liveIssue.body -IterationTitle $effectiveIterationTitle -Estimate $effectiveEstimate
    $renderedBody = Upsert-ManagedSection -Body $baseBody -Key "planning-links" -RenderedSection $planningSection
    if ((Normalize-Text -Value $liveIssue.body) -ne (Normalize-Text -Value $renderedBody)) {
        Write-Host "Refreshing planning links for child issue #$($issue.number) '$($issue.title)'..."
        Invoke-GhApiJson -Route "repos/$repo/issues/$($issue.number)" -Method "PATCH" -Body @{
            body = $renderedBody
        } | Out-Null
    }
}

$childNumbersByParent = @{}
foreach ($issue in $issuesByNumber.Values) {
    $metadata = Parse-PlanningMetadataFromBody -Body $issue.body
    if ($null -eq $metadata.ParentIssueNumber) {
        continue
    }

    if (-not $childNumbersByParent.ContainsKey([int]$metadata.ParentIssueNumber)) {
        $childNumbersByParent[[int]$metadata.ParentIssueNumber] = @()
    }

    $childNumbersByParent[[int]$metadata.ParentIssueNumber] += [int]$issue.number
}

foreach ($syncEntry in $syncedIssues.GetEnumerator()) {
    $parentNumber = [int]$syncEntry.Key
    if (-not $issuesByNumber.ContainsKey($parentNumber)) {
        continue
    }

    $parentIssue = $issuesByNumber[$parentNumber]
    $childItems = @()
    if ($childNumbersByParent.ContainsKey($parentNumber)) {
        foreach ($childNumber in ($childNumbersByParent[$parentNumber] | Sort-Object)) {
            if (-not $issuesByNumber.ContainsKey([int]$childNumber)) {
                continue
            }

            $childIssue = $issuesByNumber[[int]$childNumber]
            $childMetadata = Parse-PlanningMetadataFromBody -Body $childIssue.body
            $childProjectItem = $null
            if ($null -ne $projectContext -and $projectContext.ItemsByIssueNumber.ContainsKey([int]$childIssue.number)) {
                $childProjectItem = $projectContext.ItemsByIssueNumber[[int]$childIssue.number]
            }

            $childItems += [pscustomobject]@{
                Number = $childIssue.number
                Title = $childIssue.title
                Url = Get-IssueWebUrl -Issue $childIssue
                Status = if (-not [string]::IsNullOrWhiteSpace((Get-ProjectItemStatusValue -ProjectItem $childProjectItem))) { Get-ProjectItemStatusValue -ProjectItem $childProjectItem } elseif ($childIssue.state -eq "CLOSED") { "Done" } else { "Todo" }
                IterationTitle = if (-not [string]::IsNullOrWhiteSpace((Get-ProjectItemIterationTitle -ProjectItem $childProjectItem))) { Get-ProjectItemIterationTitle -ProjectItem $childProjectItem } else { $childMetadata.IterationTitle }
                EstimateText = if ($null -ne (Get-ProjectItemEstimateValue -ProjectItem $childProjectItem)) { Format-EstimateText -Estimate (Get-ProjectItemEstimateValue -ProjectItem $childProjectItem) } elseif ($null -ne $childMetadata.Estimate) { Format-EstimateText -Estimate $childMetadata.Estimate } else { "" }
            }
        }
    }

    $childSection = Render-ChildTasksSection -ChildItems $childItems
    $liveParentIssue = Invoke-GhApiJson -Route "repos/$repo/issues/$($parentIssue.number)"
    $renderedParentBody = Upsert-ManagedSection -Body $liveParentIssue.body -Key "child-tasks" -RenderedSection $childSection
    if ((Normalize-Text -Value $liveParentIssue.body) -ne (Normalize-Text -Value $renderedParentBody)) {
        Write-Host "Refreshing child task links for issue #$($parentIssue.number) '$($parentIssue.title)'..."
        Invoke-GhApiJson -Route "repos/$repo/issues/$($parentIssue.number)" -Method "PATCH" -Body @{
            body = $renderedParentBody
        } | Out-Null
    }
}

$allIssues = Get-RepositoryIssues -RepositoryFullName $repo
$issuesByNumber = @{}
foreach ($issue in $allIssues) {
    $issuesByNumber[[int]$issue.number] = $issue
}

Sync-LegacyPlanningIssues -RepositoryContext $repositoryContext -RepositoryFullName $repo -IssuesByNumber $issuesByNumber -DesiredIssuesBySyncKey $desiredIssuesBySyncKey -CanonicalIssueNumbersBySyncKey $canonicalIssueNumbersBySyncKey -CanonicalIterationTitlesBySyncKey $canonicalIterationTitlesBySyncKey -BoardUrl $boardUrl

Write-Host "Planning sync completed for $repo."
