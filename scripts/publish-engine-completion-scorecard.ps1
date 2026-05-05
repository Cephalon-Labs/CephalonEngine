param(
    [string]$ScorecardPath = "docs/engine-completion-scorecard.md",
    [string]$OutputPath = "artifacts/engine-completion-scorecard-release",
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Script:SchemaVersion = "1.0.0"
$Script:AllowedStatuses = @(
    "ready-for-preview",
    "partial",
    "blocked",
    "not-claimed",
    "not-applicable",
    "needs-refresh"
)

function Get-DirectoryUri {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $trimChars = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) |
        Select-Object -Unique
    $normalizedPath = $fullPath.TrimEnd($trimChars) + [System.IO.Path]::DirectorySeparatorChar
    return [System.Uri]::new($normalizedPath)
}

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$BasePath
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $Path))
}

function Get-RepoRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $repoRootUri = Get-DirectoryUri -Path $RepoRoot
    $pathUri = [System.Uri]::new([System.IO.Path]::GetFullPath($Path))
    $relativePath = [System.Uri]::UnescapeDataString($repoRootUri.MakeRelativeUri($pathUri).ToString())
    return $relativePath.Replace([System.IO.Path]::DirectorySeparatorChar, '/').Replace([System.IO.Path]::AltDirectorySeparatorChar, '/')
}

function Remove-MarkdownInlineFormatting {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $text = $Value.Trim()
    $text = [regex]::Replace($text, '\[([^\]]+)\]\([^)]+\)', '$1')
    $text = $text.Replace('`', '')
    return $text.Trim()
}

function Get-MarkdownSectionLines {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string[]]$Lines,
        [Parameter(Mandatory = $true)]
        [string]$Heading
    )

    $headingMarker = "## $Heading"
    $startIndex = -1
    for ($index = 0; $index -lt $Lines.Count; $index++) {
        if ($Lines[$index].Trim() -eq $headingMarker) {
            $startIndex = $index + 1
            break
        }
    }

    if ($startIndex -lt 0) {
        throw "Scorecard section '$Heading' was not found."
    }

    $endIndex = $Lines.Count
    for ($index = $startIndex; $index -lt $Lines.Count; $index++) {
        if ($Lines[$index] -match '^##\s+') {
            $endIndex = $index
            break
        }
    }

    if ($endIndex -le $startIndex) {
        return @()
    }

    return @($Lines[$startIndex..($endIndex - 1)])
}

function Get-MarkdownTableLines {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string[]]$SectionLines,
        [Parameter(Mandatory = $true)]
        [string]$Heading
    )

    $tableLines = [System.Collections.Generic.List[string]]::new()
    $insideTable = $false

    foreach ($line in $SectionLines) {
        if ($line.Trim().StartsWith('|')) {
            $insideTable = $true
            $tableLines.Add($line)
            continue
        }

        if ($insideTable) {
            break
        }
    }

    if ($tableLines.Count -lt 3) {
        throw "Scorecard section '$Heading' does not contain a Markdown table."
    }

    return $tableLines.ToArray()
}

function Split-MarkdownTableRow {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Line
    )

    $trimmed = $Line.Trim()
    if ($trimmed.StartsWith('|')) {
        $trimmed = $trimmed.Substring(1)
    }

    if ($trimmed.EndsWith('|')) {
        $trimmed = $trimmed.Substring(0, $trimmed.Length - 1)
    }

    return @($trimmed.Split('|') | ForEach-Object { $_.Trim() })
}

function Convert-MarkdownTable {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$TableLines,
        [Parameter(Mandatory = $true)]
        [string]$Heading
    )

    $headers = Split-MarkdownTableRow -Line $TableLines[0]
    if ($headers.Count -eq 0) {
        throw "Scorecard section '$Heading' has no table headers."
    }

    $rows = [System.Collections.Generic.List[object]]::new()
    foreach ($line in $TableLines[2..($TableLines.Count - 1)]) {
        $cells = Split-MarkdownTableRow -Line $line
        if ($cells.Count -ne $headers.Count) {
            throw "Scorecard section '$Heading' has a malformed row: $line"
        }

        $row = [ordered]@{}
        for ($index = 0; $index -lt $headers.Count; $index++) {
            $row[$headers[$index]] = $cells[$index]
        }

        $rows.Add([pscustomobject]$row)
    }

    return $rows.ToArray()
}

function Get-ScorecardTable {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string[]]$Lines,
        [Parameter(Mandatory = $true)]
        [string]$Heading
    )

    $sectionLines = Get-MarkdownSectionLines -Lines $Lines -Heading $Heading
    $tableLines = Get-MarkdownTableLines -SectionLines $sectionLines -Heading $Heading
    return Convert-MarkdownTable -TableLines $tableLines -Heading $Heading
}

function Get-ScorecardListItems {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string[]]$Lines,
        [Parameter(Mandatory = $true)]
        [string]$Heading
    )

    $sectionLines = Get-MarkdownSectionLines -Lines $Lines -Heading $Heading
    $items = [System.Collections.Generic.List[string]]::new()

    foreach ($line in $sectionLines) {
        if ($line -match '^\s*(?:\d+\.|-)\s+(?<Text>.+)$') {
            $items.Add($Matches.Text.Trim())
        }
    }

    return $items.ToArray()
}

function Get-StatusTokens {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,
        [Parameter(Mandatory = $true)]
        [string]$Context
    )

    $backtickTokens = @(
        [regex]::Matches($Text, '`([^`]+)`') |
            ForEach-Object { $_.Groups[1].Value.Trim() }
    )
    $statusTokens = @($backtickTokens | Where-Object { $Script:AllowedStatuses -contains $_ })
    $unknownStatusTokens = @(
        $backtickTokens |
            Where-Object { $_ -match '^[a-z]+(?:-[a-z]+)*$' -and $Script:AllowedStatuses -notcontains $_ }
    )

    if ($unknownStatusTokens.Count -gt 0) {
        throw "Unsupported scorecard status '$($unknownStatusTokens -join ', ')' in $Context."
    }

    if ($statusTokens.Count -eq 0) {
        throw "No supported scorecard status was found in $Context."
    }

    return @($statusTokens | Select-Object -Unique)
}

function Convert-StatusVocabulary {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Rows
    )

    $entries = @(
        foreach ($row in $Rows) {
            $status = Remove-MarkdownInlineFormatting -Value $row.Status
            if ($Script:AllowedStatuses -notcontains $status) {
                throw "Unsupported scorecard status vocabulary entry '$status'."
            }

            [pscustomobject]@{
                Status  = $status
                Meaning = Remove-MarkdownInlineFormatting -Value $row.Meaning
            }
        }
    )

    $missing = @($Script:AllowedStatuses | Where-Object { $entries.Status -notcontains $_ })
    if ($missing.Count -gt 0) {
        throw "Scorecard status vocabulary is missing: $($missing -join ', ')."
    }

    return $entries
}

function Convert-EvidenceSources {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Rows
    )

    return @(
        foreach ($row in $Rows) {
            [pscustomobject]@{
                EvidenceSource       = Remove-MarkdownInlineFormatting -Value $row.'Evidence source'
                TruthOwnedThere      = Remove-MarkdownInlineFormatting -Value $row.'Truth owned there'
                ScorecardConsumption = Remove-MarkdownInlineFormatting -Value $row.'How the scorecard consumes it'
            }
        }
    )
}

function Convert-PlatformGates {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Rows
    )

    return @(
        foreach ($row in $Rows) {
            $context = "platform gate '$($row.Gate)'"
            [pscustomobject]@{
                Gate           = Remove-MarkdownInlineFormatting -Value $row.Gate
                CurrentPosture = Remove-MarkdownInlineFormatting -Value $row.'Current posture'
                Statuses       = @(Get-StatusTokens -Text $row.'Current posture' -Context $context)
                BlocksPreview  = Remove-MarkdownInlineFormatting -Value $row.'Blocks preview?'
                BlocksGA       = Remove-MarkdownInlineFormatting -Value $row.'Blocks GA?'
                NextProofNeeded = Remove-MarkdownInlineFormatting -Value $row.'Next proof needed'
            }
        }
    )
}

function Convert-QualityDimensions {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Rows
    )

    return @(
        foreach ($row in $Rows) {
            [pscustomobject]@{
                Quality              = Remove-MarkdownInlineFormatting -Value $row.Quality
                ReleaseReadyEvidence = Remove-MarkdownInlineFormatting -Value $row.'Release-ready evidence'
            }
        }
    )
}

function Convert-PackageFamilies {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Rows
    )

    return @(
        foreach ($row in $Rows) {
            $context = "package family '$($row.Family)'"
            [pscustomobject]@{
                Family                     = Remove-MarkdownInlineFormatting -Value $row.Family
                CurrentScorecardPosture    = Remove-MarkdownInlineFormatting -Value $row.'Current scorecard posture'
                Statuses                   = @(Get-StatusTokens -Text $row.'Current scorecard posture' -Context $context)
                GABlockersToKeepVisible    = Remove-MarkdownInlineFormatting -Value $row.'GA blockers to keep visible'
            }
        }
    )
}

function Get-StatusCountObject {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Entries
    )

    $counts = [ordered]@{}
    foreach ($status in $Script:AllowedStatuses) {
        $counts[$status] = 0
    }

    foreach ($entry in $Entries) {
        foreach ($status in @($entry.Statuses)) {
            $counts[$status] = [int]$counts[$status] + 1
        }
    }

    return [pscustomobject]$counts
}

function New-EngineCompletionScorecardReport {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedScorecardPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot
    )

    if (-not (Test-Path -LiteralPath $ResolvedScorecardPath -PathType Leaf)) {
        throw "Scorecard document '$ResolvedScorecardPath' was not found."
    }

    $lines = @(Get-Content -LiteralPath $ResolvedScorecardPath -Encoding UTF8)

    $statusVocabulary = Convert-StatusVocabulary -Rows (Get-ScorecardTable -Lines $lines -Heading "Status vocabulary")
    $evidenceSources = Convert-EvidenceSources -Rows (Get-ScorecardTable -Lines $lines -Heading "Evidence sources")
    $platformGates = Convert-PlatformGates -Rows (Get-ScorecardTable -Lines $lines -Heading "Platform-level gates")
    $qualityDimensions = Convert-QualityDimensions -Rows (Get-ScorecardTable -Lines $lines -Heading "Quality-dimension gates")
    $packageFamilies = Convert-PackageFamilies -Rows (Get-ScorecardTable -Lines $lines -Heading "Package-family readiness roll-up")
    $promotionRules = @(Get-ScorecardListItems -Lines $lines -Heading "Promotion rules" | ForEach-Object { Remove-MarkdownInlineFormatting -Value $_ })
    $refreshCadence = @(Get-ScorecardListItems -Lines $lines -Heading "Refresh cadence" | ForEach-Object { Remove-MarkdownInlineFormatting -Value $_ })

    if ($promotionRules.Count -eq 0) {
        throw "Scorecard promotion rules were not found."
    }

    if ($refreshCadence.Count -eq 0) {
        throw "Scorecard refresh cadence entries were not found."
    }

    $platformStatusCounts = Get-StatusCountObject -Entries $platformGates
    $packageStatusCounts = Get-StatusCountObject -Entries $packageFamilies

    return [pscustomobject]([ordered]@{
        '$schemaVersion'   = $Script:SchemaVersion
        GeneratedAtUtc     = (Get-Date).ToUniversalTime().ToString("o")
        SourceDocument     = Get-RepoRelativePath -Path $ResolvedScorecardPath -RepoRoot $ResolvedRepoRoot
        StatusVocabulary   = $statusVocabulary
        EvidenceSources    = $evidenceSources
        PlatformGates      = $platformGates
        QualityDimensions  = $qualityDimensions
        PackageFamilies    = $packageFamilies
        PromotionRules     = $promotionRules
        RefreshCadence     = $refreshCadence
        Summary            = [pscustomobject]([ordered]@{
            PlatformGateCount      = $platformGates.Count
            QualityDimensionCount  = $qualityDimensions.Count
            PackageFamilyCount     = $packageFamilies.Count
            EvidenceSourceCount    = $evidenceSources.Count
            PlatformStatusCounts   = $platformStatusCounts
            PackageStatusCounts    = $packageStatusCounts
            BlockedPlatformGates   = @($platformGates | Where-Object { $_.Statuses -contains "blocked" }).Count
            NeedsRefreshGates      = @($platformGates | Where-Object { $_.Statuses -contains "needs-refresh" }).Count
            PartialPlatformGates   = @($platformGates | Where-Object { $_.Statuses -contains "partial" }).Count
            NotClaimedPlatformGates = @($platformGates | Where-Object { $_.Statuses -contains "not-claimed" }).Count
        })
    })
}

function Write-EngineCompletionScorecardReport {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Report,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedOutputPath
    )

    New-Item -ItemType Directory -Path $ResolvedOutputPath -Force | Out-Null

    $jsonPath = Join-Path $ResolvedOutputPath "engine-completion-scorecard.json"
    $markdownPath = Join-Path $ResolvedOutputPath "README.md"

    $Report | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

    $markdown = [System.Collections.Generic.List[string]]::new()
    $markdown.Add("# Engine Completion Scorecard Report")
    $markdown.Add("")
    $markdown.Add("Source: ``$($Report.SourceDocument)``")
    $markdown.Add("Generated at UTC: ``$($Report.GeneratedAtUtc)``")
    $markdown.Add("Schema version: ``$($Report.'$schemaVersion')``")
    $markdown.Add("")
    $markdown.Add("## Summary")
    $markdown.Add("")
    $markdown.Add("- Platform gates: $($Report.Summary.PlatformGateCount)")
    $markdown.Add("- Quality dimensions: $($Report.Summary.QualityDimensionCount)")
    $markdown.Add("- Package families: $($Report.Summary.PackageFamilyCount)")
    $markdown.Add("- Evidence sources: $($Report.Summary.EvidenceSourceCount)")
    $markdown.Add("- Blocked platform gates: $($Report.Summary.BlockedPlatformGates)")
    $markdown.Add("- Needs-refresh gates: $($Report.Summary.NeedsRefreshGates)")
    $markdown.Add("- Partial platform gates: $($Report.Summary.PartialPlatformGates)")
    $markdown.Add("- Not-claimed platform gates: $($Report.Summary.NotClaimedPlatformGates)")
    $markdown.Add("")
    $markdown.Add("## Platform Gates")
    $markdown.Add("")
    $markdown.Add("| Gate | Current posture | Statuses | Blocks GA? |")
    $markdown.Add("| --- | --- | --- | --- |")
    foreach ($gate in $Report.PlatformGates) {
        $markdown.Add("| $($gate.Gate) | $($gate.CurrentPosture) | $(@($gate.Statuses) -join ', ') | $($gate.BlocksGA) |")
    }

    $markdown.Add("")
    $markdown.Add("## Package Families")
    $markdown.Add("")
    $markdown.Add("| Family | Current posture | Statuses |")
    $markdown.Add("| --- | --- | --- |")
    foreach ($family in $Report.PackageFamilies) {
        $markdown.Add("| $($family.Family) | $($family.CurrentScorecardPosture) | $(@($family.Statuses) -join ', ') |")
    }

    $markdown | Set-Content -LiteralPath $markdownPath -Encoding UTF8

    return [pscustomobject]@{
        JsonPath     = $jsonPath
        MarkdownPath = $markdownPath
    }
}

function Invoke-EngineCompletionScorecardPublish {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScorecardPath,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $resolvedRepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
    $resolvedScorecardPath = Resolve-FullPath -Path $ScorecardPath -BasePath $resolvedRepoRoot
    $resolvedOutputPath = Resolve-FullPath -Path $OutputPath -BasePath $resolvedRepoRoot

    $report = New-EngineCompletionScorecardReport -ResolvedScorecardPath $resolvedScorecardPath -ResolvedRepoRoot $resolvedRepoRoot
    $paths = Write-EngineCompletionScorecardReport -Report $report -ResolvedOutputPath $resolvedOutputPath

    Write-Host "Engine completion scorecard artifact written to $($paths.JsonPath)"

    return [pscustomobject]@{
        Report = $report
        Paths  = $paths
    }
}

# === Main entry guard ===
# Tests dot-source this script and skip the auto-run by setting CEPHALON_ENGINE_COMPLETION_SCORECARD_NO_RUN.
if (-not $env:CEPHALON_ENGINE_COMPLETION_SCORECARD_NO_RUN) {
    $resolvedRoot = $RepoRoot
    if (-not $resolvedRoot) {
        $resolvedRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    }

    $null = Invoke-EngineCompletionScorecardPublish -ScorecardPath $ScorecardPath -OutputPath $OutputPath -RepoRoot $resolvedRoot
}
