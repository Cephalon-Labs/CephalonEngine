param(
    [string]$ScorecardPath = "docs/engine-completion-scorecard.md",
    [string]$ConformanceMatrixPath = "docs/conformance-matrix.md",
    [string]$AdoptionSmokeManifestPath = "scripts/adoption-smoke-support.json",
    [string]$SrePostureManifestPath = "scripts/sre-posture-support.json",
    [string]$PublicApiDeltaScriptPath = "scripts/summarise-public-api-deltas.ps1",
    [string]$OutputPath = "artifacts/engine-completion-scorecard-release",
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Script:SchemaVersion = "1.4.0"
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

function Get-SourceReferenceKind {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Reference
    )

    $extension = [System.IO.Path]::GetExtension($Reference).ToLowerInvariant()
    switch ($extension) {
        ".md" { return "doc" }
        ".ps1" { return "script" }
        ".json" { return "data" }
        default { return "repo-file" }
    }
}

function Resolve-ScorecardSourceReference {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DeclaredPath,
        [Parameter(Mandatory = $true)]
        [string]$ScorecardDirectory,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$Context
    )

    if ($DeclaredPath -match '^[a-z]+://') {
        return $null
    }

    $pathWithoutAnchor = ($DeclaredPath -split '#')[0]
    if ([string]::IsNullOrWhiteSpace($pathWithoutAnchor)) {
        return $null
    }

    $basePath = $ScorecardDirectory
    if ($pathWithoutAnchor -match '^(docs|scripts)/') {
        $basePath = $ResolvedRepoRoot
    }

    $resolvedPath = Resolve-FullPath -Path $pathWithoutAnchor -BasePath $basePath
    if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
        throw "Scorecard evidence source reference '$DeclaredPath' in $Context was not found at '$resolvedPath'."
    }

    $relativePath = Get-RepoRelativePath -Path $resolvedPath -RepoRoot $ResolvedRepoRoot
    return [pscustomobject]([ordered]@{
        Reference  = $relativePath
        DeclaredAs = $DeclaredPath
        Kind       = Get-SourceReferenceKind -Reference $relativePath
    })
}

function Get-ScorecardSourceReferences {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Markdown,
        [Parameter(Mandatory = $true)]
        [string]$ScorecardDirectory,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$Context
    )

    $references = [System.Collections.Generic.List[object]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

    $declaredPaths = [System.Collections.Generic.List[string]]::new()
    foreach ($match in [regex]::Matches($Markdown, '\[[^\]]+\]\((?<Path>[^)]+)\)')) {
        $declaredPaths.Add($match.Groups['Path'].Value.Trim())
    }

    foreach ($match in [regex]::Matches($Markdown, '`(?<Path>(?:docs|scripts)/[^`]+)`')) {
        $declaredPaths.Add($match.Groups['Path'].Value.Trim())
    }

    foreach ($declaredPath in $declaredPaths) {
        $reference = Resolve-ScorecardSourceReference -DeclaredPath $declaredPath -ScorecardDirectory $ScorecardDirectory -ResolvedRepoRoot $ResolvedRepoRoot -Context $Context
        if ($null -ne $reference -and $seen.Add($reference.Reference)) {
            $references.Add($reference)
        }
    }

    return $references.ToArray()
}

function Convert-EvidenceSources {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Rows,
        [Parameter(Mandatory = $true)]
        [string]$ScorecardDirectory,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot
    )

    return @(
        foreach ($row in $Rows) {
            $evidenceSource = Remove-MarkdownInlineFormatting -Value $row.'Evidence source'
            $context = "evidence source '$evidenceSource'"
            [pscustomobject]@{
                EvidenceSource       = $evidenceSource
                SourceReferences     = @(Get-ScorecardSourceReferences -Markdown $row.'Evidence source' -ScorecardDirectory $ScorecardDirectory -ResolvedRepoRoot $ResolvedRepoRoot -Context $context)
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

function Get-PackageGAReadinessPosture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Maturity,
        [Parameter(Mandatory = $true)]
        [string]$Ownership
    )

    $normalizedMaturity = $Maturity.Trim()
    $normalizedOwnership = $Ownership.Trim()

    if ($normalizedOwnership -match 'taxonomy-only' -or $normalizedMaturity -match '^M0\b') {
        return [pscustomobject]@{
            Status             = "not-claimed"
            GABlockerClass     = "runtime-support-not-claimed"
            GABlockerSummary   = "Taxonomy-only surface; no runtime support claim until the maturity audit and matrix declare one."
            NextEvidenceNeeded = "Keep the public docs explicit that this package contributes vocabulary only, or land a real runtime proof before widening support claims."
        }
    }

    if ($normalizedMaturity -match '^M4\b') {
        return [pscustomobject]@{
            Status             = "partial"
            GABlockerClass     = "release-evidence"
            GABlockerSummary   = "Adoption proof exists, but GA still needs release, SRE, package-validation, supply-chain, and deployment-mode evidence."
            NextEvidenceNeeded = "Review package validation baselines, public API deltas, release artifacts, SRE posture, and deployment-mode support before GA."
        }
    }

    if ($normalizedMaturity -match '^M3\b') {
        return [pscustomobject]@{
            Status             = "partial"
            GABlockerClass     = "m4-adoption-and-release-evidence"
            GABlockerSummary   = "Broad managed/operator proof exists, but GA still needs M4 adoption alignment plus release evidence."
            NextEvidenceNeeded = "Close the package's M4 adoption proof and then run the same release evidence review required for M4 packages."
        }
    }

    if ($normalizedMaturity -match '^M2\b') {
        return [pscustomobject]@{
            Status             = "partial"
            GABlockerClass     = "operator-adoption-and-release-evidence"
            GABlockerSummary   = "Narrow managed proof exists, but GA needs broader operator/adoption proof plus release evidence."
            NextEvidenceNeeded = "Promote only after the package owns the next real operator/adoption proof and release evidence stays aligned."
        }
    }

    if ($normalizedMaturity -match '^M1\b') {
        return [pscustomobject]@{
            Status             = "partial"
            GABlockerClass     = "managed-proof-or-explicit-catalog-only-boundary"
            GABlockerSummary   = "Catalog-only proof exists; GA needs managed proof or a deliberate catalog-only support boundary plus release evidence."
            NextEvidenceNeeded = "Either land a managed proof path or document the package as an intentionally catalog-only GA surface before release."
        }
    }

    return [pscustomobject]@{
        Status             = "needs-refresh"
        GABlockerClass     = "maturity-needs-refresh"
        GABlockerSummary   = "Maturity could not be mapped to the scorecard readiness model."
        NextEvidenceNeeded = "Refresh conformance-matrix.md and engine-surface-maturity-audit.md before using this package row for release decisions."
    }
}

function Convert-ConformancePackageRows {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedConformanceMatrixPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot
    )

    if (-not (Test-Path -LiteralPath $ResolvedConformanceMatrixPath -PathType Leaf)) {
        throw "Conformance matrix '$ResolvedConformanceMatrixPath' was not found."
    }

    $lines = @(Get-Content -LiteralPath $ResolvedConformanceMatrixPath -Encoding UTF8)
    $rows = [System.Collections.Generic.List[object]]::new()
    $currentFamily = $null
    $index = 0

    while ($index -lt $lines.Count) {
        $line = $lines[$index]
        if ($line -match '^##\s+(?<Heading>.+)$') {
            $currentFamily = Remove-MarkdownInlineFormatting -Value $Matches.Heading
            $index++
            continue
        }

        if ($line.Trim() -eq '| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |') {
            if ([string]::IsNullOrWhiteSpace($currentFamily)) {
                throw "Conformance matrix package table was found before a family heading."
            }

            $tableLines = [System.Collections.Generic.List[string]]::new()
            while ($index -lt $lines.Count -and $lines[$index].Trim().StartsWith('|')) {
                $tableLines.Add($lines[$index])
                $index++
            }

            $packageRows = Convert-MarkdownTable -TableLines $tableLines.ToArray() -Heading "Conformance matrix $currentFamily"
            foreach ($row in $packageRows) {
                $package = Remove-MarkdownInlineFormatting -Value $row.Package
                $maturity = Remove-MarkdownInlineFormatting -Value $row.Maturity
                $ownership = Remove-MarkdownInlineFormatting -Value $row.Ownership
                $posture = Get-PackageGAReadinessPosture -Maturity $maturity -Ownership $ownership

                $rows.Add([pscustomobject]([ordered]@{
                    Package            = $package
                    Family             = $currentFamily
                    Maturity           = $maturity
                    Ownership          = $ownership
                    GAGateStatus       = $posture.Status
                    GABlockerClass     = $posture.GABlockerClass
                    GABlockerSummary   = $posture.GABlockerSummary
                    NextEvidenceNeeded = $posture.NextEvidenceNeeded
                    SourceDocuments    = @(
                        Get-RepoRelativePath -Path $ResolvedConformanceMatrixPath -RepoRoot $ResolvedRepoRoot
                        "docs/engine-surface-maturity-audit.md"
                    )
                }))
            }

            continue
        }

        $index++
    }

    if ($rows.Count -eq 0) {
        throw "No package rows were found in the conformance matrix."
    }

    return $rows.ToArray()
}

function Get-ManifestPropertyValue {
    param(
        $Object,
        [Parameter(Mandatory = $true)]
        [string]$PropertyName,
        $DefaultValue = $null
    )

    if ($null -ne $Object -and $Object.PSObject.Properties.Match($PropertyName).Count -gt 0) {
        return $Object.$PropertyName
    }

    return $DefaultValue
}

function ConvertTo-RequiredBoolean {
    param(
        $Value,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if ($Value -is [bool]) {
        return $Value
    }

    $parsed = $false
    if ([bool]::TryParse([string]$Value, [ref]$parsed)) {
        return $parsed
    }

    throw "Adoption smoke assertion '$Name' must be a boolean value."
}

function Resolve-AdoptionSmokeManifestPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DeclaredPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$Context,
        [string]$PathType = "Any"
    )

    if ([string]::IsNullOrWhiteSpace($DeclaredPath)) {
        throw "Adoption smoke manifest contains an empty path in $Context."
    }

    $resolvedPath = Resolve-FullPath -Path $DeclaredPath -BasePath $ResolvedRepoRoot
    $exists = if ($PathType -eq "File") {
        Test-Path -LiteralPath $resolvedPath -PathType Leaf
    }
    elseif ($PathType -eq "Directory") {
        Test-Path -LiteralPath $resolvedPath -PathType Container
    }
    else {
        Test-Path -LiteralPath $resolvedPath
    }

    if (-not $exists) {
        throw "Adoption smoke manifest path '$DeclaredPath' in $Context was not found at '$resolvedPath'."
    }

    return [pscustomobject]([ordered]@{
        Reference  = Get-RepoRelativePath -Path $resolvedPath -RepoRoot $ResolvedRepoRoot
        DeclaredAs = $DeclaredPath
        Kind       = Get-SourceReferenceKind -Reference $DeclaredPath
    })
}

function Resolve-SrePostureManifestPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DeclaredPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$Context,
        [string]$PathType = "Any"
    )

    if ([string]::IsNullOrWhiteSpace($DeclaredPath)) {
        throw "SRE posture support manifest contains an empty path in $Context."
    }

    $resolvedPath = Resolve-FullPath -Path $DeclaredPath -BasePath $ResolvedRepoRoot
    $exists = if ($PathType -eq "File") {
        Test-Path -LiteralPath $resolvedPath -PathType Leaf
    }
    elseif ($PathType -eq "Directory") {
        Test-Path -LiteralPath $resolvedPath -PathType Container
    }
    else {
        Test-Path -LiteralPath $resolvedPath
    }

    if (-not $exists) {
        throw "SRE posture support manifest path '$DeclaredPath' in $Context was not found at '$resolvedPath'."
    }

    return [pscustomobject]([ordered]@{
        Reference  = Get-RepoRelativePath -Path $resolvedPath -RepoRoot $ResolvedRepoRoot
        DeclaredAs = $DeclaredPath
        Kind       = Get-SourceReferenceKind -Reference $DeclaredPath
    })
}

function Convert-AdoptionSmokeEvidence {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot
    )

    if (-not (Test-Path -LiteralPath $ResolvedManifestPath -PathType Leaf)) {
        throw "Adoption smoke support manifest '$ResolvedManifestPath' was not found."
    }

    $manifest = Get-Content -LiteralPath $ResolvedManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
    $schemaVersion = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName '$schemaVersion' -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($schemaVersion)) {
        throw "Adoption smoke support manifest is missing '`$schemaVersion'."
    }

    $scenarioId = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "scenarioId" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($scenarioId)) {
        throw "Adoption smoke support manifest is missing scenarioId."
    }

    $validationScriptPath = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "validationScript" -DefaultValue "")
    $validationScriptReference = Resolve-AdoptionSmokeManifestPath -DeclaredPath $validationScriptPath -ResolvedRepoRoot $ResolvedRepoRoot -Context "validationScript" -PathType "File"
    $validationScriptFullPath = Resolve-FullPath -Path $validationScriptPath -BasePath $ResolvedRepoRoot
    $validationScript = Get-Content -LiteralPath $validationScriptFullPath -Raw -Encoding UTF8

    $referenceModuleProjectPath = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "referenceModuleProject" -DefaultValue "")
    $referenceModuleProjectReference = Resolve-AdoptionSmokeManifestPath -DeclaredPath $referenceModuleProjectPath -ResolvedRepoRoot $ResolvedRepoRoot -Context "referenceModuleProject" -PathType "File"

    $supportingScriptReferences = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "supportingScripts" -DefaultValue @() |
            ForEach-Object {
                Resolve-AdoptionSmokeManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "supportingScripts" -PathType "File"
            }
    )

    $sourceDocumentReferences = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "sourceDocs" -DefaultValue @() |
            ForEach-Object {
                Resolve-AdoptionSmokeManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "sourceDocs" -PathType "File"
            }
    )

    $assertions = Get-ManifestPropertyValue -Object $manifest -PropertyName "assertions"
    if ($null -eq $assertions) {
        throw "Adoption smoke support manifest is missing assertions."
    }

    $requiredAssertionNames = @(
        "runsOutsideRepository",
        "publishesLocalPackages",
        "installsCliFromTemporaryFeed",
        "scaffoldsGeneratedApp",
        "stagesReferenceModulePackage",
        "patchesPackagePolicyAndTrust",
        "runsGeneratedHost"
    )

    $assertionRows = @(
        foreach ($assertionName in $requiredAssertionNames) {
            $value = ConvertTo-RequiredBoolean -Value (Get-ManifestPropertyValue -Object $assertions -PropertyName $assertionName) -Name $assertionName
            if (-not $value) {
                throw "Adoption smoke assertion '$assertionName' must be true for the scorecard evidence read model."
            }

            [pscustomobject]([ordered]@{
                Name  = $assertionName
                Value = $value
            })
        }
    )

    $requiredScriptTokens = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "requiredScriptTokens" -DefaultValue @() |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )

    foreach ($token in $requiredScriptTokens) {
        if (-not $validationScript.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Adoption smoke validation script '$validationScriptPath' does not contain required token '$token'."
        }
    }

    $runtimeProbes = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "runtimeProbes" -DefaultValue @() |
            ForEach-Object {
                $kind = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "kind" -DefaultValue "")
                $path = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "path" -DefaultValue "")
                if ([string]::IsNullOrWhiteSpace($kind) -or [string]::IsNullOrWhiteSpace($path)) {
                    throw "Adoption smoke runtime probe entries must include kind and path."
                }

                if (-not $validationScript.Contains($path, [System.StringComparison]::Ordinal)) {
                    throw "Adoption smoke validation script '$validationScriptPath' does not contain runtime probe '$path'."
                }

                [pscustomobject]([ordered]@{
                    Kind = $kind
                    Path = $path
                })
            }
    )

    if ($runtimeProbes.Count -eq 0) {
        throw "Adoption smoke support manifest must declare at least one runtime probe."
    }

    return [pscustomobject]([ordered]@{
        Manifest                  = Get-RepoRelativePath -Path $ResolvedManifestPath -RepoRoot $ResolvedRepoRoot
        ManifestSchemaVersion     = $schemaVersion
        ScenarioId                = $scenarioId
        Status                    = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "status" -DefaultValue "unknown")
        Summary                   = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "summary" -DefaultValue "")
        ValidationScript          = $validationScriptReference.Reference
        SupportingScripts         = @($supportingScriptReferences | ForEach-Object { $_.Reference })
        SourceDocuments           = @($sourceDocumentReferences | ForEach-Object { $_.Reference })
        ReferenceModuleProject    = $referenceModuleProjectReference.Reference
        Assertions                = $assertionRows
        RequiredScriptTokens      = $requiredScriptTokens
        RuntimeProbes             = $runtimeProbes
        ValidatedReferences       = @(
            $validationScriptReference
            $referenceModuleProjectReference
            $supportingScriptReferences
            $sourceDocumentReferences
        ) | Sort-Object Reference -Unique
    })
}

function Convert-SrePostureEvidence {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot
    )

    if (-not (Test-Path -LiteralPath $ResolvedManifestPath -PathType Leaf)) {
        throw "SRE posture support manifest '$ResolvedManifestPath' was not found."
    }

    $manifest = Get-Content -LiteralPath $ResolvedManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
    $schemaVersion = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName '$schemaVersion' -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($schemaVersion)) {
        throw "SRE posture support manifest is missing '`$schemaVersion'."
    }

    $status = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "status" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($status)) {
        throw "SRE posture support manifest is missing status."
    }

    $releaseValidationSummaryMode = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "releaseValidationSummaryMode" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($releaseValidationSummaryMode)) {
        throw "SRE posture support manifest is missing releaseValidationSummaryMode."
    }

    $stableBaselinesPublishedValue = Get-ManifestPropertyValue -Object $manifest -PropertyName "stableBaselinesPublished" -DefaultValue $false
    if ($stableBaselinesPublishedValue -isnot [bool]) {
        throw "SRE posture support manifest stableBaselinesPublished must be a boolean value."
    }

    $sourceDocumentReferences = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "sourceDocs" -DefaultValue @() |
            ForEach-Object {
                Resolve-SrePostureManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "sourceDocs" -PathType "File"
            }
    )
    if ($sourceDocumentReferences.Count -eq 0) {
        throw "SRE posture support manifest must declare at least one sourceDocs entry."
    }

    $validationScriptReferences = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "validationScripts" -DefaultValue @() |
            ForEach-Object {
                Resolve-SrePostureManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "validationScripts" -PathType "File"
            }
    )
    if ($validationScriptReferences.Count -eq 0) {
        throw "SRE posture support manifest must declare at least one validationScripts entry."
    }

    $guardrailCatalogPath = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "guardrailCatalog" -DefaultValue "")
    $guardrailCatalogReference = Resolve-SrePostureManifestPath -DeclaredPath $guardrailCatalogPath -ResolvedRepoRoot $ResolvedRepoRoot -Context "guardrailCatalog" -PathType "File"
    $guardrailCatalog = Get-Content -LiteralPath (Resolve-FullPath -Path $guardrailCatalogPath -BasePath $ResolvedRepoRoot) -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
    $guardrailEntries = @(Get-ManifestPropertyValue -Object $guardrailCatalog -PropertyName "entries" -DefaultValue @())
    if ($guardrailEntries.Count -eq 0) {
        throw "SRE posture guardrail catalog '$($guardrailCatalogReference.Reference)' must contain at least one entry."
    }

    $sliRows = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "slis" -DefaultValue @() |
            ForEach-Object {
                $id = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "id" -DefaultValue "")
                $category = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "category" -DefaultValue "")
                $measurementSurface = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "measurementSurface" -DefaultValue "")
                $sourceDocumentPath = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "sourceDocument" -DefaultValue "")
                $sloTarget = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "sloTarget" -DefaultValue "")
                $window = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "window" -DefaultValue "")
                $targetStatus = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "targetStatus" -DefaultValue "")
                $baselineStatus = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "baselineStatus" -DefaultValue "")

                foreach ($field in @(
                    @{ Name = "id"; Value = $id },
                    @{ Name = "category"; Value = $category },
                    @{ Name = "measurementSurface"; Value = $measurementSurface },
                    @{ Name = "sourceDocument"; Value = $sourceDocumentPath },
                    @{ Name = "sloTarget"; Value = $sloTarget },
                    @{ Name = "window"; Value = $window },
                    @{ Name = "targetStatus"; Value = $targetStatus },
                    @{ Name = "baselineStatus"; Value = $baselineStatus }
                )) {
                    if ([string]::IsNullOrWhiteSpace([string]$field.Value)) {
                        throw "SRE posture SLI entry must include $($field.Name)."
                    }
                }

                $sourceDocumentReference = Resolve-SrePostureManifestPath -DeclaredPath $sourceDocumentPath -ResolvedRepoRoot $ResolvedRepoRoot -Context "SLI '$id' sourceDocument" -PathType "File"
                $sourceDocument = Get-Content -LiteralPath (Resolve-FullPath -Path $sourceDocumentPath -BasePath $ResolvedRepoRoot) -Raw -Encoding UTF8
                if (-not $sourceDocument.Contains($id, [System.StringComparison]::Ordinal)) {
                    throw "SRE posture source document '$($sourceDocumentReference.Reference)' does not contain SLI '$id'."
                }

                [pscustomobject]([ordered]@{
                    Id                 = $id
                    Category           = $category
                    MeasurementSurface = $measurementSurface
                    SourceDocument     = $sourceDocumentReference.Reference
                    SloTarget          = $sloTarget
                    Window             = $window
                    TargetStatus       = $targetStatus
                    BaselineStatus     = $baselineStatus
                })
            }
    )

    if ($sliRows.Count -eq 0) {
        throw "SRE posture support manifest must declare at least one SLI."
    }

    $targetDeclaredCount = @($sliRows | Where-Object { $_.TargetStatus -eq "target-declared" }).Count
    $pendingStableBaselineCount = @($sliRows | Where-Object { $_.BaselineStatus -eq "pending-stable-baseline" }).Count
    $stableBaselineCount = @($sliRows | Where-Object { $_.BaselineStatus -eq "stable-baseline" }).Count

    return [pscustomobject]([ordered]@{
        Manifest                   = Get-RepoRelativePath -Path $ResolvedManifestPath -RepoRoot $ResolvedRepoRoot
        ManifestSchemaVersion      = $schemaVersion
        Status                     = $status
        Summary                    = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "summary" -DefaultValue "")
        ReleaseValidationSummaryMode = $releaseValidationSummaryMode
        StableBaselinesPublished   = [bool]$stableBaselinesPublishedValue
        SourceDocuments            = @($sourceDocumentReferences | ForEach-Object { $_.Reference })
        ValidationScripts          = @($validationScriptReferences | ForEach-Object { $_.Reference })
        GuardrailCatalog           = $guardrailCatalogReference.Reference
        GuardrailCatalogEntryCount = $guardrailEntries.Count
        SliCount                   = $sliRows.Count
        TargetDeclaredCount        = $targetDeclaredCount
        PendingStableBaselineCount = $pendingStableBaselineCount
        StableBaselineCount        = $stableBaselineCount
        SliRows                    = $sliRows
        ValidatedReferences        = @(
            $sourceDocumentReferences
            $validationScriptReferences
            $guardrailCatalogReference
        ) | Sort-Object Reference -Unique
    })
}

function Convert-PublicApiCompatibilityEvidence {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedPublicApiDeltaScriptPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot
    )

    if (-not (Test-Path -LiteralPath $ResolvedPublicApiDeltaScriptPath -PathType Leaf)) {
        throw "Public API delta script '$ResolvedPublicApiDeltaScriptPath' was not found."
    }

    $srcRoot = Join-Path $ResolvedRepoRoot "src"
    if (-not (Test-Path -LiteralPath $srcRoot -PathType Container)) {
        throw "src folder '$srcRoot' was not found for public API compatibility evidence."
    }

    $unshippedFiles = @(Get-ChildItem -LiteralPath $srcRoot -Filter "PublicAPI.Unshipped.txt" -File -Recurse | Sort-Object FullName)
    if ($unshippedFiles.Count -eq 0) {
        throw "No PublicAPI.Unshipped.txt files were found under '$srcRoot'."
    }

    $packageDeltas = [System.Collections.Generic.List[object]]::new()
    $totalAdditions = 0
    $totalRemovals = 0
    $packagesWithPendingChanges = 0
    $headerOnlyPackages = 0

    foreach ($file in $unshippedFiles) {
        $packageDirectory = $file.Directory.FullName
        $packageId = $file.Directory.Name
        $shippedPath = Join-Path $packageDirectory "PublicAPI.Shipped.txt"
        if (-not (Test-Path -LiteralPath $shippedPath -PathType Leaf)) {
            throw "Public API package '$packageId' has PublicAPI.Unshipped.txt but is missing PublicAPI.Shipped.txt."
        }

        $projectPath = Join-Path $packageDirectory "$packageId.csproj"
        if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
            throw "Public API package '$packageId' is missing expected project file '$projectPath'."
        }

        $additions = [System.Collections.Generic.List[string]]::new()
        $removals = [System.Collections.Generic.List[string]]::new()
        foreach ($line in Get-Content -LiteralPath $file.FullName -Encoding UTF8) {
            $trimmed = $line.Trim()
            if ([string]::IsNullOrWhiteSpace($trimmed)) {
                continue
            }

            if ($trimmed.StartsWith("#", [System.StringComparison]::Ordinal)) {
                continue
            }

            if ($trimmed.StartsWith("*REMOVED*", [System.StringComparison]::Ordinal)) {
                $removals.Add($trimmed.Substring("*REMOVED*".Length).TrimStart())
                continue
            }

            $additions.Add($trimmed)
        }

        $pendingEntryCount = $additions.Count + $removals.Count
        if ($pendingEntryCount -eq 0) {
            $headerOnlyPackages++
        }
        else {
            $packagesWithPendingChanges++
        }

        $totalAdditions += $additions.Count
        $totalRemovals += $removals.Count

        $packageDeltas.Add([pscustomobject]([ordered]@{
            Package            = $packageId
            Project            = Get-RepoRelativePath -Path $projectPath -RepoRoot $ResolvedRepoRoot
            Unshipped          = Get-RepoRelativePath -Path $file.FullName -RepoRoot $ResolvedRepoRoot
            Shipped            = Get-RepoRelativePath -Path $shippedPath -RepoRoot $ResolvedRepoRoot
            AdditiveEntryCount = $additions.Count
            RemovalEntryCount  = $removals.Count
            PendingEntryCount  = $pendingEntryCount
            HasPendingChanges  = $pendingEntryCount -gt 0
            HasRemovalEntries  = $removals.Count -gt 0
        }))
    }

    return [pscustomobject]([ordered]@{
        DeltaScript            = Get-RepoRelativePath -Path $ResolvedPublicApiDeltaScriptPath -RepoRoot $ResolvedRepoRoot
        PackageCount           = $packageDeltas.Count
        PendingPackageCount    = $packagesWithPendingChanges
        HeaderOnlyPackageCount = $headerOnlyPackages
        AdditiveEntryCount     = $totalAdditions
        RemovalEntryCount      = $totalRemovals
        HasRemovalEntries      = $totalRemovals -gt 0
        PackageDeltas          = $packageDeltas.ToArray()
    })
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
        [string]$ResolvedConformanceMatrixPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedAdoptionSmokeManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedSrePostureManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedPublicApiDeltaScriptPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot
    )

    if (-not (Test-Path -LiteralPath $ResolvedScorecardPath -PathType Leaf)) {
        throw "Scorecard document '$ResolvedScorecardPath' was not found."
    }

    $lines = @(Get-Content -LiteralPath $ResolvedScorecardPath -Encoding UTF8)

    $scorecardDirectory = [System.IO.Path]::GetDirectoryName($ResolvedScorecardPath)
    $statusVocabulary = Convert-StatusVocabulary -Rows (Get-ScorecardTable -Lines $lines -Heading "Status vocabulary")
    $evidenceSources = Convert-EvidenceSources -Rows (Get-ScorecardTable -Lines $lines -Heading "Evidence sources") -ScorecardDirectory $scorecardDirectory -ResolvedRepoRoot $ResolvedRepoRoot
    $platformGates = Convert-PlatformGates -Rows (Get-ScorecardTable -Lines $lines -Heading "Platform-level gates")
    $qualityDimensions = Convert-QualityDimensions -Rows (Get-ScorecardTable -Lines $lines -Heading "Quality-dimension gates")
    $packageFamilies = Convert-PackageFamilies -Rows (Get-ScorecardTable -Lines $lines -Heading "Package-family readiness roll-up")
    $packageGAReadinessRows = Convert-ConformancePackageRows -ResolvedConformanceMatrixPath $ResolvedConformanceMatrixPath -ResolvedRepoRoot $ResolvedRepoRoot
    $adoptionSmokeEvidence = Convert-AdoptionSmokeEvidence -ResolvedManifestPath $ResolvedAdoptionSmokeManifestPath -ResolvedRepoRoot $ResolvedRepoRoot
    $srePostureEvidence = Convert-SrePostureEvidence -ResolvedManifestPath $ResolvedSrePostureManifestPath -ResolvedRepoRoot $ResolvedRepoRoot
    $publicApiCompatibilityEvidence = Convert-PublicApiCompatibilityEvidence -ResolvedPublicApiDeltaScriptPath $ResolvedPublicApiDeltaScriptPath -ResolvedRepoRoot $ResolvedRepoRoot
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
    $evidenceSourceReferences = @($evidenceSources | ForEach-Object { $_.SourceReferences } | Sort-Object Reference -Unique)
    $packageGAStatusCounts = Get-StatusCountObject -Entries @(
        $packageGAReadinessRows | ForEach-Object {
            [pscustomobject]@{ Statuses = @($_.GAGateStatus) }
        }
    )

    return [pscustomobject]([ordered]@{
        '$schemaVersion'   = $Script:SchemaVersion
        GeneratedAtUtc     = (Get-Date).ToUniversalTime().ToString("o")
        SourceDocument     = Get-RepoRelativePath -Path $ResolvedScorecardPath -RepoRoot $ResolvedRepoRoot
        ConformanceMatrix  = Get-RepoRelativePath -Path $ResolvedConformanceMatrixPath -RepoRoot $ResolvedRepoRoot
        AdoptionSmokeManifest = Get-RepoRelativePath -Path $ResolvedAdoptionSmokeManifestPath -RepoRoot $ResolvedRepoRoot
        SrePostureManifest = Get-RepoRelativePath -Path $ResolvedSrePostureManifestPath -RepoRoot $ResolvedRepoRoot
        PublicApiDeltaScript = Get-RepoRelativePath -Path $ResolvedPublicApiDeltaScriptPath -RepoRoot $ResolvedRepoRoot
        StatusVocabulary   = $statusVocabulary
        EvidenceSources    = $evidenceSources
        EvidenceSourceReferences = $evidenceSourceReferences
        PlatformGates      = $platformGates
        QualityDimensions  = $qualityDimensions
        PackageFamilies    = $packageFamilies
        PackageGAReadiness = $packageGAReadinessRows
        AdoptionSmokeEvidence = $adoptionSmokeEvidence
        SrePostureEvidence = $srePostureEvidence
        PublicApiCompatibilityEvidence = $publicApiCompatibilityEvidence
        PromotionRules     = $promotionRules
        RefreshCadence     = $refreshCadence
        Summary            = [pscustomobject]([ordered]@{
            PlatformGateCount      = $platformGates.Count
            QualityDimensionCount  = $qualityDimensions.Count
            PackageFamilyCount     = $packageFamilies.Count
            PackageGAReadinessCount = $packageGAReadinessRows.Count
            AdoptionSmokeScenarioCount = if ($null -ne $adoptionSmokeEvidence) { 1 } else { 0 }
            AdoptionSmokeRuntimeProbeCount = @($adoptionSmokeEvidence.RuntimeProbes).Count
            AdoptionSmokeAssertionCount = @($adoptionSmokeEvidence.Assertions).Count
            SreSliCount = $srePostureEvidence.SliCount
            SreTargetDeclaredCount = $srePostureEvidence.TargetDeclaredCount
            SrePendingStableBaselineCount = $srePostureEvidence.PendingStableBaselineCount
            SreStableBaselineCount = $srePostureEvidence.StableBaselineCount
            PublicApiPackageCount = $publicApiCompatibilityEvidence.PackageCount
            PublicApiPendingPackageCount = $publicApiCompatibilityEvidence.PendingPackageCount
            PublicApiAdditiveEntryCount = $publicApiCompatibilityEvidence.AdditiveEntryCount
            PublicApiRemovalEntryCount = $publicApiCompatibilityEvidence.RemovalEntryCount
            EvidenceSourceCount    = $evidenceSources.Count
            EvidenceSourceReferenceCount = $evidenceSourceReferences.Count
            PlatformStatusCounts   = $platformStatusCounts
            PackageStatusCounts    = $packageStatusCounts
            PackageGAStatusCounts  = $packageGAStatusCounts
            BlockedPlatformGates   = @($platformGates | Where-Object { $_.Statuses -contains "blocked" }).Count
            NeedsRefreshGates      = @($platformGates | Where-Object { $_.Statuses -contains "needs-refresh" }).Count
            PartialPlatformGates   = @($platformGates | Where-Object { $_.Statuses -contains "partial" }).Count
            NotClaimedPlatformGates = @($platformGates | Where-Object { $_.Statuses -contains "not-claimed" }).Count
            PartialPackageGAGates  = @($packageGAReadinessRows | Where-Object { $_.GAGateStatus -eq "partial" }).Count
            NotClaimedPackageGAGates = @($packageGAReadinessRows | Where-Object { $_.GAGateStatus -eq "not-claimed" }).Count
            NeedsRefreshPackageGAGates = @($packageGAReadinessRows | Where-Object { $_.GAGateStatus -eq "needs-refresh" }).Count
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
    $markdown.Add("Conformance matrix: ``$($Report.ConformanceMatrix)``")
    $markdown.Add("Adoption smoke manifest: ``$($Report.AdoptionSmokeManifest)``")
    $markdown.Add("SRE posture manifest: ``$($Report.SrePostureManifest)``")
    $markdown.Add("Public API delta script: ``$($Report.PublicApiDeltaScript)``")
    $markdown.Add("Generated at UTC: ``$($Report.GeneratedAtUtc)``")
    $markdown.Add("Schema version: ``$($Report.'$schemaVersion')``")
    $markdown.Add("")
    $markdown.Add("## Summary")
    $markdown.Add("")
    $markdown.Add("- Platform gates: $($Report.Summary.PlatformGateCount)")
    $markdown.Add("- Quality dimensions: $($Report.Summary.QualityDimensionCount)")
    $markdown.Add("- Package families: $($Report.Summary.PackageFamilyCount)")
    $markdown.Add("- Package GA readiness rows: $($Report.Summary.PackageGAReadinessCount)")
    $markdown.Add("- Adoption smoke scenarios: $($Report.Summary.AdoptionSmokeScenarioCount)")
    $markdown.Add("- Adoption smoke runtime probes: $($Report.Summary.AdoptionSmokeRuntimeProbeCount)")
    $markdown.Add("- SRE SLIs: $($Report.Summary.SreSliCount)")
    $markdown.Add("- SRE target-declared SLIs: $($Report.Summary.SreTargetDeclaredCount)")
    $markdown.Add("- SRE pending stable baselines: $($Report.Summary.SrePendingStableBaselineCount)")
    $markdown.Add("- SRE stable baselines: $($Report.Summary.SreStableBaselineCount)")
    $markdown.Add("- Public API packages: $($Report.Summary.PublicApiPackageCount)")
    $markdown.Add("- Public API packages with pending changes: $($Report.Summary.PublicApiPendingPackageCount)")
    $markdown.Add("- Public API additive entries: $($Report.Summary.PublicApiAdditiveEntryCount)")
    $markdown.Add("- Public API removal entries: $($Report.Summary.PublicApiRemovalEntryCount)")
    $markdown.Add("- Evidence sources: $($Report.Summary.EvidenceSourceCount)")
    $markdown.Add("- Evidence source references: $($Report.Summary.EvidenceSourceReferenceCount)")
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
    $markdown.Add("## Evidence Source References")
    $markdown.Add("")
    $markdown.Add("| Reference | Kind |")
    $markdown.Add("| --- | --- |")
    foreach ($reference in $Report.EvidenceSourceReferences) {
        $markdown.Add("| $($reference.Reference) | $($reference.Kind) |")
    }

    $markdown.Add("")
    $markdown.Add("## SRE Posture Evidence")
    $markdown.Add("")
    $markdown.Add("- Manifest: ``$($Report.SrePostureEvidence.Manifest)``")
    $markdown.Add("- Status: $($Report.SrePostureEvidence.Status)")
    $markdown.Add("- Release validation summary mode: $($Report.SrePostureEvidence.ReleaseValidationSummaryMode)")
    $markdown.Add("- Stable baselines published: $($Report.SrePostureEvidence.StableBaselinesPublished)")
    $markdown.Add("- Guardrail catalog: ``$($Report.SrePostureEvidence.GuardrailCatalog)``")
    $markdown.Add("- Guardrail catalog entries: $($Report.SrePostureEvidence.GuardrailCatalogEntryCount)")
    $markdown.Add("")
    $markdown.Add("| SLI | Category | Target status | Baseline status |")
    $markdown.Add("| --- | --- | --- | --- |")
    foreach ($sli in $Report.SrePostureEvidence.SliRows) {
        $markdown.Add("| ``$($sli.Id)`` | $($sli.Category) | $($sli.TargetStatus) | $($sli.BaselineStatus) |")
    }

    $markdown.Add("")
    $markdown.Add("## Public API Compatibility Evidence")
    $markdown.Add("")
    $markdown.Add("- Delta script: ``$($Report.PublicApiCompatibilityEvidence.DeltaScript)``")
    $markdown.Add("- Packages with PublicAPI.Unshipped.txt: $($Report.PublicApiCompatibilityEvidence.PackageCount)")
    $markdown.Add("- Packages with pending changes: $($Report.PublicApiCompatibilityEvidence.PendingPackageCount)")
    $markdown.Add("- Header-only packages: $($Report.PublicApiCompatibilityEvidence.HeaderOnlyPackageCount)")
    $markdown.Add("- Additive entries: $($Report.PublicApiCompatibilityEvidence.AdditiveEntryCount)")
    $markdown.Add("- Removal entries: $($Report.PublicApiCompatibilityEvidence.RemovalEntryCount)")
    $markdown.Add("")
    $markdown.Add("| Package | Additions | Removals | Unshipped |")
    $markdown.Add("| --- | --- | --- | --- |")
    foreach ($package in @($Report.PublicApiCompatibilityEvidence.PackageDeltas | Where-Object { $_.HasPendingChanges })) {
        $markdown.Add("| $($package.Package) | $($package.AdditiveEntryCount) | $($package.RemovalEntryCount) | ``$($package.Unshipped)`` |")
    }

    $markdown.Add("")
    $markdown.Add("## Adoption Smoke Evidence")
    $markdown.Add("")
    $markdown.Add("- Scenario: $($Report.AdoptionSmokeEvidence.ScenarioId)")
    $markdown.Add("- Status: $($Report.AdoptionSmokeEvidence.Status)")
    $markdown.Add("- Validation script: ``$($Report.AdoptionSmokeEvidence.ValidationScript)``")
    $markdown.Add("- Reference module project: ``$($Report.AdoptionSmokeEvidence.ReferenceModuleProject)``")
    $markdown.Add("- Assertions: $(@($Report.AdoptionSmokeEvidence.Assertions).Count)")
    $markdown.Add("- Runtime probes: $(@($Report.AdoptionSmokeEvidence.RuntimeProbes).Count)")
    $markdown.Add("")
    $markdown.Add("| Probe kind | Path |")
    $markdown.Add("| --- | --- |")
    foreach ($probe in $Report.AdoptionSmokeEvidence.RuntimeProbes) {
        $markdown.Add("| $($probe.Kind) | ``$($probe.Path)`` |")
    }

    $markdown.Add("")
    $markdown.Add("## Package Families")
    $markdown.Add("")
    $markdown.Add("| Family | Current posture | Statuses |")
    $markdown.Add("| --- | --- | --- |")
    foreach ($family in $Report.PackageFamilies) {
        $markdown.Add("| $($family.Family) | $($family.CurrentScorecardPosture) | $(@($family.Statuses) -join ', ') |")
    }

    $markdown.Add("")
    $markdown.Add("## Package GA Readiness")
    $markdown.Add("")
    $markdown.Add("| Package | Family | Maturity | GA gate status | Blocker class |")
    $markdown.Add("| --- | --- | --- | --- | --- |")
    foreach ($package in $Report.PackageGAReadiness) {
        $markdown.Add("| $($package.Package) | $($package.Family) | $($package.Maturity) | $($package.GAGateStatus) | $($package.GABlockerClass) |")
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
        [string]$ConformanceMatrixPath,
        [string]$AdoptionSmokeManifestPath = "scripts/adoption-smoke-support.json",
        [string]$SrePostureManifestPath = "scripts/sre-posture-support.json",
        [string]$PublicApiDeltaScriptPath = "scripts/summarise-public-api-deltas.ps1",
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $resolvedRepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
    $resolvedScorecardPath = Resolve-FullPath -Path $ScorecardPath -BasePath $resolvedRepoRoot
    $resolvedConformanceMatrixPath = Resolve-FullPath -Path $ConformanceMatrixPath -BasePath $resolvedRepoRoot
    $resolvedAdoptionSmokeManifestPath = Resolve-FullPath -Path $AdoptionSmokeManifestPath -BasePath $resolvedRepoRoot
    $resolvedSrePostureManifestPath = Resolve-FullPath -Path $SrePostureManifestPath -BasePath $resolvedRepoRoot
    $resolvedPublicApiDeltaScriptPath = Resolve-FullPath -Path $PublicApiDeltaScriptPath -BasePath $resolvedRepoRoot
    $resolvedOutputPath = Resolve-FullPath -Path $OutputPath -BasePath $resolvedRepoRoot

    $report = New-EngineCompletionScorecardReport -ResolvedScorecardPath $resolvedScorecardPath -ResolvedConformanceMatrixPath $resolvedConformanceMatrixPath -ResolvedAdoptionSmokeManifestPath $resolvedAdoptionSmokeManifestPath -ResolvedSrePostureManifestPath $resolvedSrePostureManifestPath -ResolvedPublicApiDeltaScriptPath $resolvedPublicApiDeltaScriptPath -ResolvedRepoRoot $resolvedRepoRoot
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

    $null = Invoke-EngineCompletionScorecardPublish -ScorecardPath $ScorecardPath -ConformanceMatrixPath $ConformanceMatrixPath -AdoptionSmokeManifestPath $AdoptionSmokeManifestPath -SrePostureManifestPath $SrePostureManifestPath -PublicApiDeltaScriptPath $PublicApiDeltaScriptPath -OutputPath $OutputPath -RepoRoot $resolvedRoot
}
