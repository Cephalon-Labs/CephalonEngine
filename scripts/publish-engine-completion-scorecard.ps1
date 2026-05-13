param(
    [string]$ScorecardPath = "docs/engine-completion-scorecard.md",
    [string]$ConformanceMatrixPath = "docs/conformance-matrix.md",
    [string]$DeploymentModeManifestPath = "scripts/deployment-mode-support.json",
    [string]$DeploymentModeClaimsReportPath = "artifacts/deployment-mode-claims-release/claim-validation-report.json",
    [string]$AdoptionSmokeManifestPath = "scripts/adoption-smoke-support.json",
    [string]$ProviderIntegrationManifestPath = "scripts/provider-integration-support.json",
    [string]$EventingOperationalSuperiorityManifestPath = "scripts/eventing-operational-superiority-support.json",
    [string]$SrePostureManifestPath = "scripts/sre-posture-support.json",
    [string]$SupplyChainManifestPath = "scripts/supply-chain-release-support.json",
    [string]$TestCoverageRoadmapPath = "docs/test-coverage-roadmap.md",
    [string]$PublicApiDeltaScriptPath = "scripts/summarise-public-api-deltas.ps1",
    [string]$OutputPath = "artifacts/engine-completion-scorecard-release",
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Script:SchemaVersion = "1.23.0"
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
            Where-Object { $_ -cmatch '^[a-z]+(?:-[a-z]+)*$' -and $Script:AllowedStatuses -notcontains $_ }
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

function ConvertTo-RequiredManifestBoolean {
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

    throw "$Name must be a boolean value."
}

function Resolve-SupportManifestPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DeclaredPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$Context,
        [Parameter(Mandatory = $true)]
        [string]$OwnerName,
        [string]$PathType = "Any"
    )

    if ([string]::IsNullOrWhiteSpace($DeclaredPath)) {
        throw "$OwnerName contains an empty path in $Context."
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
        throw "$OwnerName path '$DeclaredPath' in $Context was not found at '$resolvedPath'."
    }

    return [pscustomobject]([ordered]@{
        Reference  = Get-RepoRelativePath -Path $resolvedPath -RepoRoot $ResolvedRepoRoot
        DeclaredAs = $DeclaredPath
        Kind       = Get-SourceReferenceKind -Reference $DeclaredPath
    })
}

function Resolve-ProviderIntegrationManifestPath {
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
        throw "Provider integration support manifest contains an empty path in $Context."
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
        throw "Provider integration support manifest path '$DeclaredPath' in $Context was not found at '$resolvedPath'."
    }

    return [pscustomobject]([ordered]@{
        Reference  = Get-RepoRelativePath -Path $resolvedPath -RepoRoot $ResolvedRepoRoot
        DeclaredAs = $DeclaredPath
        Kind       = Get-SourceReferenceKind -Reference $DeclaredPath
    })
}

function Get-ProviderIntegrationDependencyHealthProviderKey {
    param(
        [Parameter(Mandatory = $true)]
        $ProviderManifestRow
    )

    $source = [string](Get-ManifestPropertyValue -Object $ProviderManifestRow -PropertyName "source" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($source)) {
        throw "Dependency-health provider manifest row must declare source."
    }

    $match = [regex]::Match($source, '^Cephalon\.Observability\.(?<provider>[A-Za-z0-9]+)Dependencies$')
    if (-not $match.Success) {
        throw "Dependency-health provider manifest source '$source' must use the Cephalon.Observability.*Dependencies package convention."
    }

    return $match.Groups["provider"].Value.ToLowerInvariant()
}

function Convert-ProviderIntegrationDependencyHealthManifest {
    param(
        [Parameter(Mandatory = $true)]
        $ProviderIntegrationManifest,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot
    )

    $declaredPath = [string](Get-ManifestPropertyValue -Object $ProviderIntegrationManifest -PropertyName "dependencyHealthProviderManifest" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($declaredPath)) {
        return $null
    }

    $manifestReference = Resolve-ProviderIntegrationManifestPath -DeclaredPath $declaredPath -ResolvedRepoRoot $ResolvedRepoRoot -Context "dependencyHealthProviderManifest" -PathType "File"
    $resolvedManifestPath = Resolve-FullPath -Path $declaredPath -BasePath $ResolvedRepoRoot
    $manifest = Get-Content -LiteralPath $resolvedManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 32

    $schemaVersion = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "schemaVersion" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($schemaVersion)) {
        throw "Dependency-health provider manifest '$declaredPath' is missing schemaVersion."
    }

    $status = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "status" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($status)) {
        throw "Dependency-health provider manifest '$declaredPath' is missing status."
    }

    $providers = @(Get-ManifestPropertyValue -Object $manifest -PropertyName "providers" -DefaultValue @())
    if ($providers.Count -eq 0) {
        throw "Dependency-health provider manifest '$declaredPath' must declare at least one provider."
    }

    $declaredProviderCount = [int](Get-ManifestPropertyValue -Object $manifest -PropertyName "providerCount" -DefaultValue 0)
    if ($declaredProviderCount -ne $providers.Count) {
        throw "Dependency-health provider manifest '$declaredPath' declares providerCount '$declaredProviderCount' but contains '$($providers.Count)' providers."
    }

    $expectedRows = foreach ($providerRow in $providers) {
        $provider = [string](Get-ManifestPropertyValue -Object $providerRow -PropertyName "provider" -DefaultValue "")
        $componentDoc = [string](Get-ManifestPropertyValue -Object $providerRow -PropertyName "componentDoc" -DefaultValue "")
        foreach ($field in @(
            [pscustomobject]@{ Name = "provider"; Value = $provider },
            [pscustomobject]@{ Name = "componentDoc"; Value = $componentDoc }
        )) {
            if ([string]::IsNullOrWhiteSpace($field.Value)) {
                throw "Dependency-health provider manifest row must declare $($field.Name)."
            }
        }

        $providerKey = Get-ProviderIntegrationDependencyHealthProviderKey -ProviderManifestRow $providerRow
        [pscustomobject]([ordered]@{
            Id                 = "$providerKey-dependency-health-live"
            Provider           = $provider
            ComponentDoc       = $componentDoc
            RuntimeContract    = "dependency-health.$providerKey"
            TestProject        = "tests/Cephalon.Tests.Hosting/Cephalon.Tests.Hosting.csproj"
            TestFile           = "tests/Cephalon.Tests.Hosting/ObservabilityDependencyHealthProviderInvariantTests.cs"
            SharedComponentDoc = "docs/components/observability.md"
        })
    }

    return [pscustomobject]([ordered]@{
        Reference             = $manifestReference.Reference
        ManifestSchemaVersion = $schemaVersion
        Status                = $status
        ProviderCount         = $declaredProviderCount
        ExpectedRows          = @($expectedRows)
        ValidatedReferences   = @($manifestReference)
    })
}

function Assert-ProviderIntegrationDependencyHealthRows {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$ProviderRows,
        $DependencyHealthProviderManifest
    )

    $dependencyHealthRows = @($ProviderRows | Where-Object { $_.Family -eq "dependency-health" })
    if ($null -eq $DependencyHealthProviderManifest) {
        if ($dependencyHealthRows.Count -gt 0) {
            throw "Provider integration support manifest must declare dependencyHealthProviderManifest when dependency-health provider rows are present."
        }

        return
    }

    if ($dependencyHealthRows.Count -ne $DependencyHealthProviderManifest.ProviderCount) {
        throw "Provider integration dependency-health rows must match source-derived manifest count '$($DependencyHealthProviderManifest.ProviderCount)' but found '$($dependencyHealthRows.Count)'."
    }

    foreach ($expectedRow in @($DependencyHealthProviderManifest.ExpectedRows)) {
        $matches = @($dependencyHealthRows | Where-Object { $_.Id -eq $expectedRow.Id })
        if ($matches.Count -eq 0) {
            throw "Provider integration dependency-health row '$($expectedRow.Id)' is missing from source-derived manifest coverage."
        }

        if ($matches.Count -gt 1) {
            throw "Provider integration dependency-health row '$($expectedRow.Id)' is duplicated."
        }

        $actualRow = $matches[0]
        foreach ($field in @(
            [pscustomobject]@{ Name = "provider"; Actual = $actualRow.Provider; Expected = $expectedRow.Provider },
            [pscustomobject]@{ Name = "status"; Actual = $actualRow.Status; Expected = "live-proof-available" },
            [pscustomobject]@{ Name = "defaultRunBehavior"; Actual = $actualRow.DefaultRunBehavior; Expected = "runs-without-external-services" },
            [pscustomobject]@{ Name = "testProject"; Actual = $actualRow.TestProject; Expected = $expectedRow.TestProject }
        )) {
            if ($field.Actual -ne $field.Expected) {
                throw "Provider integration dependency-health row '$($expectedRow.Id)' $($field.Name) must match source-derived manifest value '$($field.Expected)' but found '$($field.Actual)'."
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($actualRow.ExternalServiceGate)) {
            throw "Provider integration dependency-health row '$($expectedRow.Id)' must not declare an external service gate."
        }

        foreach ($expectedTestFile in @($expectedRow.TestFile)) {
            if (@($actualRow.TestFiles) -notcontains $expectedTestFile) {
                throw "Provider integration dependency-health row '$($expectedRow.Id)' must include test file '$expectedTestFile'."
            }
        }

        foreach ($expectedSourceDocument in @($expectedRow.ComponentDoc, $expectedRow.SharedComponentDoc)) {
            if (@($actualRow.SourceDocuments) -notcontains $expectedSourceDocument) {
                throw "Provider integration dependency-health row '$($expectedRow.Id)' must include source document '$expectedSourceDocument'."
            }
        }

        foreach ($expectedRuntimeContract in @("dependency-health", $expectedRow.RuntimeContract, "RuntimeHealthEvaluator.EvaluateDependencies", "IRuntimeDiagnosticsCatalog", "IRuntimeIntrospectionSnapshot.DiagnosticsConventions")) {
            if (@($actualRow.RuntimeContracts) -notcontains $expectedRuntimeContract) {
                throw "Provider integration dependency-health row '$($expectedRow.Id)' must include runtime contract '$expectedRuntimeContract'."
            }
        }

        if (@($actualRow.EnvironmentVariables).Count -ne 0) {
            throw "Provider integration dependency-health row '$($expectedRow.Id)' must keep environment variables empty for deterministic managed-probe live proof."
        }
    }
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

function Assert-SreStringSetEquals {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [string[]]$Expected,
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [string[]]$Actual,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $expectedSorted = @($Expected | Sort-Object)
    $actualSorted = @($Actual | Sort-Object)
    $differences = @(Compare-Object -ReferenceObject $expectedSorted -DifferenceObject $actualSorted)
    if ($differences.Count -gt 0) {
        throw $Message
    }
}

function Resolve-SupplyChainManifestPath {
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
        throw "Supply-chain release support manifest contains an empty path in $Context."
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
        throw "Supply-chain release support manifest path '$DeclaredPath' in $Context was not found at '$resolvedPath'."
    }

    return [pscustomobject]([ordered]@{
        Reference  = Get-RepoRelativePath -Path $resolvedPath -RepoRoot $ResolvedRepoRoot
        DeclaredAs = $DeclaredPath
        Kind       = Get-SourceReferenceKind -Reference $DeclaredPath
    })
}

function Resolve-DeploymentModeManifestPath {
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
        throw "Deployment-mode support manifest contains an empty path in $Context."
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
        throw "Deployment-mode support manifest path '$DeclaredPath' in $Context was not found at '$resolvedPath'."
    }

    return [pscustomobject]([ordered]@{
        Reference  = Get-RepoRelativePath -Path $resolvedPath -RepoRoot $ResolvedRepoRoot
        DeclaredAs = $DeclaredPath
        Kind       = Get-SourceReferenceKind -Reference $DeclaredPath
    })
}

function ConvertTo-RequiredSupplyChainBoolean {
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

    throw "Supply-chain release evidence '$Name' must be a boolean value."
}

function ConvertTo-RequiredDeploymentModeBoolean {
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

    throw "Deployment-mode support '$Name' must be a boolean value."
}

function Convert-DeploymentModeClaimsReportEvidence {
    param(
        [AllowNull()]
        [string]$ResolvedClaimsReportPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedReleaseValidationMode,
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [string[]]$ExpectedReleaseValidationDeploymentModes,
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [string[]]$ExpectedGatedModes,
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [string[]]$ExpectedAuditOnlyModes,
        [Parameter(Mandatory = $true)]
        [bool]$ExpectedReleaseValidationSkipsPublish,
        [Parameter(Mandatory = $true)]
        [bool]$ExpectedNonOptOutGate,
        [Parameter(Mandatory = $true)]
        [bool]$ExpectedFailureBlocksRelease,
        [Parameter(Mandatory = $true)]
        [bool]$ExpectedFailOnWarnings,
        [Parameter(Mandatory = $true)]
        [int]$ExpectedPublishTargetCount,
        [Parameter(Mandatory = $true)]
        [int]$ExpectedPackageEntryCount,
        [Parameter(Mandatory = $true)]
        [int]$ExpectedPackageScopedClaimPackageCount,
        [Parameter(Mandatory = $true)]
        [int]$ExpectedKnownHazardEntryCount
    )

    $reportReference = if ([string]::IsNullOrWhiteSpace($ResolvedClaimsReportPath)) {
        ""
    }
    else {
        Get-RepoRelativePath -Path $ResolvedClaimsReportPath -RepoRoot $ResolvedRepoRoot
    }

    if ([string]::IsNullOrWhiteSpace($ResolvedClaimsReportPath) -or -not (Test-Path -LiteralPath $ResolvedClaimsReportPath -PathType Leaf)) {
        return [pscustomobject]([ordered]@{
            Report                              = $reportReference
            Present                             = $false
            AggregateVerdict                    = "not-found"
            DeploymentMode                      = ""
            PublishProbeGateStatus              = "not-found"
            PublishProbeGateEnabled             = $false
            PublishProbeGateFailureBlocksRelease = $false
            PublishProbeGateFailOnWarnings      = $false
            PublishProbeGateFailureCount        = 0
            PublishProbeTargetCount             = 0
            PublishProbeWarningCount            = 0
            PublishProbeErrorCount              = 0
            PackageClaimCount                   = 0
            PackageClaimTruthfulCount           = 0
            PackageClaimOverstatedCount         = 0
            HazardInventoryTotalPackages        = 0
            HazardInventoryKnownHazardEntries   = 0
            HazardInventoryScopedClaimPackages  = 0
            HazardInventoryTransitiveAuditStatus = ""
            HazardInventoryTransitiveAuditMissingEntries = 0
            HazardInventoryTransitiveAuditLockFileCount = 0
            HazardInventoryBoundaryAnnotationAuditStatus = ""
            HazardInventoryBoundaryAnnotationAuditFailureCount = 0
            HazardInventoryCoreRouteDelegateAuditStatus = ""
            HazardInventoryCoreRouteDelegateAuditFailureCount = 0
            HazardInventoryFullCommonRouteDelegateAuditStatus = ""
            HazardInventoryFullCommonRouteDelegateAuditFailureCount = 0
            HazardInventoryFullOperatorRouteDelegateAuditStatus = ""
            HazardInventoryFullOperatorRouteDelegateAuditFailureCount = 0
            HazardInventoryOperatorResponseJsonContractAuditStatus = ""
            HazardInventoryOperatorResponseJsonContractAuditFailureCount = 0
            HazardInventoryNonOperatorEndpointAuditStatus = ""
            HazardInventoryNonOperatorEndpointAuditFailureCount = 0
            HazardInventoryFrameworkEndpointBoundaryAuditStatus = ""
            HazardInventoryFrameworkEndpointBoundaryAuditFailureCount = 0
        })
    }

    $report = Get-Content -LiteralPath $ResolvedClaimsReportPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 32
    $manifestPath = [string](Get-ManifestPropertyValue -Object $report -PropertyName "ManifestPath" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($manifestPath)) {
        throw "Deployment-mode claims report '$reportReference' is missing ManifestPath."
    }

    if (-not [string]::Equals(
            [System.IO.Path]::GetFullPath($manifestPath),
            [System.IO.Path]::GetFullPath($ResolvedManifestPath),
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Deployment-mode claims report '$reportReference' ManifestPath does not match the deployment-mode support manifest."
    }

    $policy = Get-ManifestPropertyValue -Object $report -PropertyName "PublishProbePolicy"
    if ($null -eq $policy) {
        throw "Deployment-mode claims report '$reportReference' is missing PublishProbePolicy."
    }

    $reportReleaseValidationMode = [string](Get-ManifestPropertyValue -Object $policy -PropertyName "ReleaseValidationMode" -DefaultValue "")
    if ($reportReleaseValidationMode -ne $ExpectedReleaseValidationMode) {
        throw "Deployment-mode claims report '$reportReference' release-validation mode '$reportReleaseValidationMode' does not match manifest '$ExpectedReleaseValidationMode'."
    }

    $reportReleaseValidationDeploymentModes = @(
        Get-ManifestPropertyValue -Object $policy -PropertyName "ReleaseValidationDeploymentModes" -DefaultValue @() |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    Assert-SreStringSetEquals -Expected $ExpectedReleaseValidationDeploymentModes -Actual $reportReleaseValidationDeploymentModes -Message "Deployment-mode claims report release-validation deployment modes drift from manifest."

    $reportGatedModes = @(
        Get-ManifestPropertyValue -Object $policy -PropertyName "GatedModes" -DefaultValue @() |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    Assert-SreStringSetEquals -Expected $ExpectedGatedModes -Actual $reportGatedModes -Message "Deployment-mode claims report gated modes drift from manifest."

    $reportAuditOnlyModes = @(
        Get-ManifestPropertyValue -Object $policy -PropertyName "AuditOnlyModes" -DefaultValue @() |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    Assert-SreStringSetEquals -Expected $ExpectedAuditOnlyModes -Actual $reportAuditOnlyModes -Message "Deployment-mode claims report audit-only modes drift from manifest."

    $reportReleaseValidationSkipsPublish = ConvertTo-RequiredDeploymentModeBoolean -Value (Get-ManifestPropertyValue -Object $policy -PropertyName "ReleaseValidationSkipsPublish") -Name "claimsReport.PublishProbePolicy.ReleaseValidationSkipsPublish"
    $reportCurrentRunSkipsPublish = ConvertTo-RequiredDeploymentModeBoolean -Value (Get-ManifestPropertyValue -Object $policy -PropertyName "CurrentRunSkipsPublish") -Name "claimsReport.PublishProbePolicy.CurrentRunSkipsPublish"
    $reportNonOptOutGate = ConvertTo-RequiredDeploymentModeBoolean -Value (Get-ManifestPropertyValue -Object $policy -PropertyName "NonOptOutGate") -Name "claimsReport.PublishProbePolicy.NonOptOutGate"
    $reportFailureBlocksRelease = ConvertTo-RequiredDeploymentModeBoolean -Value (Get-ManifestPropertyValue -Object $policy -PropertyName "FailureBlocksRelease") -Name "claimsReport.PublishProbePolicy.FailureBlocksRelease"
    $reportFailOnWarnings = ConvertTo-RequiredDeploymentModeBoolean -Value (Get-ManifestPropertyValue -Object $policy -PropertyName "FailOnWarnings") -Name "claimsReport.PublishProbePolicy.FailOnWarnings"

    if ($reportReleaseValidationSkipsPublish -ne $ExpectedReleaseValidationSkipsPublish -or
        $reportCurrentRunSkipsPublish -ne $ExpectedReleaseValidationSkipsPublish -or
        $reportNonOptOutGate -ne $ExpectedNonOptOutGate -or
        $reportFailureBlocksRelease -ne $ExpectedFailureBlocksRelease -or
        $reportFailOnWarnings -ne $ExpectedFailOnWarnings) {
        throw "Deployment-mode claims report '$reportReference' publish-probe policy booleans drift from manifest."
    }

    $gate = Get-ManifestPropertyValue -Object $report -PropertyName "PublishProbeGate"
    if ($null -eq $gate) {
        throw "Deployment-mode claims report '$reportReference' is missing PublishProbeGate."
    }

    $gateStatus = [string](Get-ManifestPropertyValue -Object $gate -PropertyName "Status" -DefaultValue "")
    $gateEnabled = ConvertTo-RequiredDeploymentModeBoolean -Value (Get-ManifestPropertyValue -Object $gate -PropertyName "Enabled") -Name "claimsReport.PublishProbeGate.Enabled"
    $gateFailureBlocksRelease = ConvertTo-RequiredDeploymentModeBoolean -Value (Get-ManifestPropertyValue -Object $gate -PropertyName "FailureBlocksRelease") -Name "claimsReport.PublishProbeGate.FailureBlocksRelease"
    $gateFailOnWarnings = ConvertTo-RequiredDeploymentModeBoolean -Value (Get-ManifestPropertyValue -Object $gate -PropertyName "FailOnWarnings") -Name "claimsReport.PublishProbeGate.FailOnWarnings"
    $gateFailureCount = [int](Get-ManifestPropertyValue -Object $gate -PropertyName "FailureCount" -DefaultValue 0)

    if ($ExpectedNonOptOutGate -and ($gateStatus -ne "passed" -or -not $gateEnabled -or $gateFailureCount -ne 0)) {
        throw "Deployment-mode claims report '$reportReference' publish-probe gate must be passed for the non-opt-out release gate."
    }

    if ($gateFailureBlocksRelease -ne $ExpectedFailureBlocksRelease -or $gateFailOnWarnings -ne $ExpectedFailOnWarnings) {
        throw "Deployment-mode claims report '$reportReference' publish-probe gate booleans drift from manifest."
    }

    $modes = @(Get-ManifestPropertyValue -Object $report -PropertyName "Modes" -DefaultValue @())
    $publishProbeTargets = @(
        foreach ($modeReport in $modes) {
            $publishProbe = Get-ManifestPropertyValue -Object $modeReport -PropertyName "PublishProbe"
            if ($null -ne $publishProbe) {
                Get-ManifestPropertyValue -Object $publishProbe -PropertyName "Targets" -DefaultValue @()
            }
        }
    )

    $publishProbeWarningCount = 0
    $publishProbeErrorCount = 0
    $failedTargets = @()
    foreach ($target in $publishProbeTargets) {
        $publishProbeWarningCount += [int](Get-ManifestPropertyValue -Object $target -PropertyName "WarningCount" -DefaultValue 0)
        $publishProbeErrorCount += [int](Get-ManifestPropertyValue -Object $target -PropertyName "ErrorCount" -DefaultValue 0)
        $success = ConvertTo-RequiredDeploymentModeBoolean -Value (Get-ManifestPropertyValue -Object $target -PropertyName "Success" -DefaultValue $false) -Name "claimsReport.PublishProbe.Target.Success"
        if (-not $success) {
            $failedTargets += [string](Get-ManifestPropertyValue -Object $target -PropertyName "Target" -DefaultValue "unknown")
        }
    }

    if ($publishProbeTargets.Count -ne $ExpectedPublishTargetCount) {
        throw "Deployment-mode claims report '$reportReference' publish target count $($publishProbeTargets.Count) does not match manifest count $ExpectedPublishTargetCount."
    }

    if ($failedTargets.Count -gt 0 -or $publishProbeErrorCount -gt 0 -or ($ExpectedFailOnWarnings -and $publishProbeWarningCount -gt 0)) {
        throw "Deployment-mode claims report '$reportReference' contains failed publish targets, errors, or gated warnings."
    }

    $packageClaimAudits = @(
        foreach ($modeReport in $modes) {
            Get-ManifestPropertyValue -Object $modeReport -PropertyName "PackageClaimAudits" -DefaultValue @()
        }
    )
    $truthfulPackageClaims = @($packageClaimAudits | Where-Object { [string](Get-ManifestPropertyValue -Object $_ -PropertyName "Verdict" -DefaultValue "") -eq "claim-truthful" })
    $overstatedPackageClaims = @($packageClaimAudits | Where-Object { [string](Get-ManifestPropertyValue -Object $_ -PropertyName "Verdict" -DefaultValue "") -eq "claim-overstated" })

    if ($packageClaimAudits.Count -ne $ExpectedPackageScopedClaimPackageCount -or $overstatedPackageClaims.Count -gt 0) {
        throw "Deployment-mode claims report '$reportReference' package-claim verdicts drift from manifest truth."
    }

    $hazardInventory = Get-ManifestPropertyValue -Object $report -PropertyName "HazardInventory"
    if ($null -eq $hazardInventory) {
        throw "Deployment-mode claims report '$reportReference' is missing HazardInventory."
    }

    $hazardInventoryTotalPackages = [int](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "TotalPackages" -DefaultValue 0)
    $hazardInventoryKnownHazardEntries = [int](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "TotalKnownHazards" -DefaultValue 0)
    $hazardInventoryScopedClaimPackages = [int](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "PackagesWithScopedClaims" -DefaultValue 0)
    if ($hazardInventoryTotalPackages -ne $ExpectedPackageEntryCount -or
        $hazardInventoryKnownHazardEntries -ne $ExpectedKnownHazardEntryCount -or
        $hazardInventoryScopedClaimPackages -ne $ExpectedPackageScopedClaimPackageCount) {
        throw "Deployment-mode claims report '$reportReference' hazard inventory counts drift from manifest truth."
    }

    $knownTransitiveAudit = Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "KnownTransitiveHazardAudit"
    if ($null -eq $knownTransitiveAudit) {
        throw "Deployment-mode claims report '$reportReference' hazard inventory is missing KnownTransitiveHazardAudit."
    }

    $knownTransitiveAuditStatus = [string](Get-ManifestPropertyValue -Object $knownTransitiveAudit -PropertyName "Status" -DefaultValue "")
    $knownTransitiveAuditMissingEntries = [int](Get-ManifestPropertyValue -Object $knownTransitiveAudit -PropertyName "MissingEntries" -DefaultValue 0)
    $knownTransitiveAuditLockFileCount = [int](Get-ManifestPropertyValue -Object $knownTransitiveAudit -PropertyName "LockFileCount" -DefaultValue 0)
    if ($knownTransitiveAuditStatus -ne "matched" -or $knownTransitiveAuditMissingEntries -ne 0) {
        throw "Deployment-mode claims report '$reportReference' transitive hazard lock-file audit is not matched."
    }

    $boundaryAnnotationAuditStatus = [string](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "BoundaryAnnotationAuditStatus" -DefaultValue "")
    $boundaryAnnotationAuditFailureCount = [int](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "BoundaryAnnotationAuditFailureCount" -DefaultValue -1)
    if ($boundaryAnnotationAuditStatus -ne "matched" -or $boundaryAnnotationAuditFailureCount -ne 0) {
        throw "Deployment-mode claims report '$reportReference' boundary annotation audit is not matched."
    }

    $coreRouteDelegateAuditStatus = [string](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "CoreRouteDelegateAuditStatus" -DefaultValue "")
    $coreRouteDelegateAuditFailureCount = [int](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "CoreRouteDelegateAuditFailureCount" -DefaultValue -1)
    if ($coreRouteDelegateAuditStatus -ne "matched" -or $coreRouteDelegateAuditFailureCount -ne 0) {
        throw "Deployment-mode claims report '$reportReference' core route-delegate audit is not matched."
    }

    $fullCommonRouteDelegateAuditStatus = [string](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "FullCommonRouteDelegateAuditStatus" -DefaultValue "")
    $fullCommonRouteDelegateAuditFailureCount = [int](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "FullCommonRouteDelegateAuditFailureCount" -DefaultValue -1)
    if ($fullCommonRouteDelegateAuditStatus -ne "matched" -or $fullCommonRouteDelegateAuditFailureCount -ne 0) {
        throw "Deployment-mode claims report '$reportReference' full common route-delegate audit is not matched."
    }

    $fullOperatorRouteDelegateAuditStatus = [string](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "FullOperatorRouteDelegateAuditStatus" -DefaultValue "")
    $fullOperatorRouteDelegateAuditFailureCount = [int](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "FullOperatorRouteDelegateAuditFailureCount" -DefaultValue -1)
    if ($fullOperatorRouteDelegateAuditStatus -ne "matched" -or $fullOperatorRouteDelegateAuditFailureCount -ne 0) {
        throw "Deployment-mode claims report '$reportReference' full operator route-delegate audit is not matched."
    }

    $operatorResponseJsonContractAuditStatus = [string](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "OperatorResponseJsonContractAuditStatus" -DefaultValue "")
    $operatorResponseJsonContractAuditFailureCount = [int](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "OperatorResponseJsonContractAuditFailureCount" -DefaultValue -1)
    if ($operatorResponseJsonContractAuditStatus -ne "matched" -or $operatorResponseJsonContractAuditFailureCount -ne 0) {
        throw "Deployment-mode claims report '$reportReference' operator response JSON contract audit is not matched."
    }

    $nonOperatorEndpointAuditStatus = [string](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "NonOperatorEndpointAuditStatus" -DefaultValue "")
    $nonOperatorEndpointAuditFailureCount = [int](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "NonOperatorEndpointAuditFailureCount" -DefaultValue -1)
    if ($nonOperatorEndpointAuditStatus -ne "matched" -or $nonOperatorEndpointAuditFailureCount -ne 0) {
        throw "Deployment-mode claims report '$reportReference' non-operator endpoint audit is not matched."
    }

    $frameworkEndpointBoundaryAuditStatus = [string](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "FrameworkEndpointBoundaryAuditStatus" -DefaultValue "")
    $frameworkEndpointBoundaryAuditFailureCount = [int](Get-ManifestPropertyValue -Object $hazardInventory -PropertyName "FrameworkEndpointBoundaryAuditFailureCount" -DefaultValue -1)
    if ($frameworkEndpointBoundaryAuditStatus -ne "matched" -or $frameworkEndpointBoundaryAuditFailureCount -ne 0) {
        throw "Deployment-mode claims report '$reportReference' framework endpoint boundary audit is not matched."
    }

    return [pscustomobject]([ordered]@{
        Report                              = $reportReference
        Present                             = $true
        AggregateVerdict                    = [string](Get-ManifestPropertyValue -Object $report -PropertyName "AggregateVerdict" -DefaultValue "")
        DeploymentMode                      = [string](Get-ManifestPropertyValue -Object $report -PropertyName "DeploymentMode" -DefaultValue "")
        PublishProbeGateStatus              = $gateStatus
        PublishProbeGateEnabled             = $gateEnabled
        PublishProbeGateFailureBlocksRelease = $gateFailureBlocksRelease
        PublishProbeGateFailOnWarnings      = $gateFailOnWarnings
        PublishProbeGateFailureCount        = $gateFailureCount
        PublishProbeTargetCount             = $publishProbeTargets.Count
        PublishProbeWarningCount            = $publishProbeWarningCount
        PublishProbeErrorCount              = $publishProbeErrorCount
        PackageClaimCount                   = $packageClaimAudits.Count
        PackageClaimTruthfulCount           = $truthfulPackageClaims.Count
        PackageClaimOverstatedCount         = $overstatedPackageClaims.Count
        HazardInventoryTotalPackages        = $hazardInventoryTotalPackages
        HazardInventoryKnownHazardEntries   = $hazardInventoryKnownHazardEntries
        HazardInventoryScopedClaimPackages  = $hazardInventoryScopedClaimPackages
        HazardInventoryTransitiveAuditStatus = $knownTransitiveAuditStatus
        HazardInventoryTransitiveAuditMissingEntries = $knownTransitiveAuditMissingEntries
        HazardInventoryTransitiveAuditLockFileCount = $knownTransitiveAuditLockFileCount
        HazardInventoryBoundaryAnnotationAuditStatus = $boundaryAnnotationAuditStatus
        HazardInventoryBoundaryAnnotationAuditFailureCount = $boundaryAnnotationAuditFailureCount
        HazardInventoryCoreRouteDelegateAuditStatus = $coreRouteDelegateAuditStatus
        HazardInventoryCoreRouteDelegateAuditFailureCount = $coreRouteDelegateAuditFailureCount
        HazardInventoryFullCommonRouteDelegateAuditStatus = $fullCommonRouteDelegateAuditStatus
        HazardInventoryFullCommonRouteDelegateAuditFailureCount = $fullCommonRouteDelegateAuditFailureCount
        HazardInventoryFullOperatorRouteDelegateAuditStatus = $fullOperatorRouteDelegateAuditStatus
        HazardInventoryFullOperatorRouteDelegateAuditFailureCount = $fullOperatorRouteDelegateAuditFailureCount
        HazardInventoryOperatorResponseJsonContractAuditStatus = $operatorResponseJsonContractAuditStatus
        HazardInventoryOperatorResponseJsonContractAuditFailureCount = $operatorResponseJsonContractAuditFailureCount
        HazardInventoryNonOperatorEndpointAuditStatus = $nonOperatorEndpointAuditStatus
        HazardInventoryNonOperatorEndpointAuditFailureCount = $nonOperatorEndpointAuditFailureCount
        HazardInventoryFrameworkEndpointBoundaryAuditStatus = $frameworkEndpointBoundaryAuditStatus
        HazardInventoryFrameworkEndpointBoundaryAuditFailureCount = $frameworkEndpointBoundaryAuditFailureCount
    })
}

function Convert-DeploymentModeEvidence {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedManifestPath,
        [AllowNull()]
        [string]$ResolvedClaimsReportPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot
    )

    if (-not (Test-Path -LiteralPath $ResolvedManifestPath -PathType Leaf)) {
        throw "Deployment-mode support manifest '$ResolvedManifestPath' was not found."
    }

    $manifest = Get-Content -LiteralPath $ResolvedManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 32
    $schemaVersion = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName '$schemaVersion' -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($schemaVersion)) {
        throw "Deployment-mode support manifest is missing '`$schemaVersion'."
    }

    $shippingBaseline = Get-ManifestPropertyValue -Object $manifest -PropertyName "shippingBaseline"
    if ($null -eq $shippingBaseline) {
        throw "Deployment-mode support manifest is missing shippingBaseline."
    }

    $stableTargetFramework = [string](Get-ManifestPropertyValue -Object $shippingBaseline -PropertyName "stableTargetFramework" -DefaultValue "")
    $readinessLaneTargetFramework = [string](Get-ManifestPropertyValue -Object $shippingBaseline -PropertyName "readinessLaneTargetFramework" -DefaultValue "")
    $readinessLaneStatus = [string](Get-ManifestPropertyValue -Object $shippingBaseline -PropertyName "readinessLaneStatus" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($stableTargetFramework) -or
        [string]::IsNullOrWhiteSpace($readinessLaneTargetFramework) -or
        [string]::IsNullOrWhiteSpace($readinessLaneStatus)) {
        throw "Deployment-mode support manifest shippingBaseline must declare stableTargetFramework, readinessLaneTargetFramework, and readinessLaneStatus."
    }

    $documentation = Get-ManifestPropertyValue -Object $manifest -PropertyName "documentation"
    if ($null -eq $documentation) {
        throw "Deployment-mode support manifest is missing documentation."
    }

    $sourceDocumentFields = @("guidePath", "readinessGuidePath", "compatibilityGuidePath", "packagePublishingGuidePath")
    $sourceDocumentReferences = @(
        $sourceDocumentFields | ForEach-Object {
            $path = [string](Get-ManifestPropertyValue -Object $documentation -PropertyName $_ -DefaultValue "")
            Resolve-DeploymentModeManifestPath -DeclaredPath $path -ResolvedRepoRoot $ResolvedRepoRoot -Context "documentation.$_" -PathType "File"
        }
    )

    $validationScriptFields = @("validationHarnessPath", "validationHarnessTestsPath")
    $validationScriptReferences = @(
        $validationScriptFields | ForEach-Object {
            $path = [string](Get-ManifestPropertyValue -Object $documentation -PropertyName $_ -DefaultValue "")
            Resolve-DeploymentModeManifestPath -DeclaredPath $path -ResolvedRepoRoot $ResolvedRepoRoot -Context "documentation.$_" -PathType "File"
        }
    )

    $deploymentModes = Get-ManifestPropertyValue -Object $manifest -PropertyName "deploymentModes"
    if ($null -eq $deploymentModes) {
        throw "Deployment-mode support manifest is missing deploymentModes."
    }

    $modeRows = [System.Collections.Generic.List[object]]::new()
    foreach ($property in $deploymentModes.PSObject.Properties) {
        $mode = [string]$property.Name
        $modeDefinition = $property.Value
        $status = [string](Get-ManifestPropertyValue -Object $modeDefinition -PropertyName "status" -DefaultValue "")
        if ([string]::IsNullOrWhiteSpace($status)) {
            throw "Deployment-mode support manifest deploymentModes.$mode is missing status."
        }

        $modeRows.Add([pscustomobject]([ordered]@{
            Mode    = $mode
            Status  = $status
            Summary = [string](Get-ManifestPropertyValue -Object $modeDefinition -PropertyName "summary" -DefaultValue "")
        }))
    }

    if ($modeRows.Count -eq 0) {
        throw "Deployment-mode support manifest must declare at least one deploymentModes entry."
    }

    $publishProbePolicy = Get-ManifestPropertyValue -Object $manifest -PropertyName "publishProbePolicy"
    if ($null -eq $publishProbePolicy) {
        throw "Deployment-mode support manifest is missing publishProbePolicy."
    }

    $releaseValidationMode = [string](Get-ManifestPropertyValue -Object $publishProbePolicy -PropertyName "releaseValidationMode" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($releaseValidationMode)) {
        throw "Deployment-mode support manifest publishProbePolicy.releaseValidationMode is missing."
    }

    $releaseValidationSkipsPublish = ConvertTo-RequiredDeploymentModeBoolean `
        -Value (Get-ManifestPropertyValue -Object $publishProbePolicy -PropertyName "releaseValidationSkipsPublish") `
        -Name "publishProbePolicy.releaseValidationSkipsPublish"
    $nonOptOutGate = ConvertTo-RequiredDeploymentModeBoolean `
        -Value (Get-ManifestPropertyValue -Object $publishProbePolicy -PropertyName "nonOptOutGate") `
        -Name "publishProbePolicy.nonOptOutGate"
    $releaseValidationDeploymentModes = @(
        Get-ManifestPropertyValue -Object $publishProbePolicy -PropertyName "releaseValidationDeploymentModes" -DefaultValue @() |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    $gatedModes = @(
        Get-ManifestPropertyValue -Object $publishProbePolicy -PropertyName "gatedModes" -DefaultValue @() |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    $auditOnlyModes = @(
        Get-ManifestPropertyValue -Object $publishProbePolicy -PropertyName "auditOnlyModes" -DefaultValue @() |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    $failureBlocksRelease = ConvertTo-RequiredDeploymentModeBoolean `
        -Value (Get-ManifestPropertyValue -Object $publishProbePolicy -PropertyName "failureBlocksRelease" -DefaultValue $nonOptOutGate) `
        -Name "publishProbePolicy.failureBlocksRelease"
    $failOnWarnings = ConvertTo-RequiredDeploymentModeBoolean `
        -Value (Get-ManifestPropertyValue -Object $publishProbePolicy -PropertyName "failOnWarnings" -DefaultValue $true) `
        -Name "publishProbePolicy.failOnWarnings"

    $representativePublishTargets = Get-ManifestPropertyValue -Object $manifest -PropertyName "representativePublishTargets"
    if ($null -eq $representativePublishTargets) {
        throw "Deployment-mode support manifest is missing representativePublishTargets."
    }

    $publishTargetReferences = @(
        Get-ManifestPropertyValue -Object $representativePublishTargets -PropertyName "projects" -DefaultValue @() |
            ForEach-Object {
                Resolve-DeploymentModeManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "representativePublishTargets.projects" -PathType "File"
            }
    )
    if ($publishTargetReferences.Count -eq 0) {
        throw "Deployment-mode support manifest representativePublishTargets.projects must contain at least one project."
    }

    $eligibility = Get-ManifestPropertyValue -Object $manifest -PropertyName "deploymentModeEligibility"
    if ($null -eq $eligibility) {
        throw "Deployment-mode support manifest is missing deploymentModeEligibility."
    }

    $packages = @(Get-ManifestPropertyValue -Object $eligibility -PropertyName "packages" -DefaultValue @())
    if ($packages.Count -eq 0) {
        throw "Deployment-mode support manifest deploymentModeEligibility.packages must contain at least one package."
    }

    $packageRows = [System.Collections.Generic.List[object]]::new()
    $claimAuditTierCounts = [ordered]@{}
    $knownHazardEntryCount = 0
    $packageScopedClaimCount = 0
    foreach ($package in $packages) {
        $packageName = [string](Get-ManifestPropertyValue -Object $package -PropertyName "packageName" -DefaultValue "")
        if ([string]::IsNullOrWhiteSpace($packageName)) {
            throw "Deployment-mode support manifest contains a package entry without packageName."
        }

        $claimAuditTier = [string](Get-ManifestPropertyValue -Object $package -PropertyName "claimAuditTier" -DefaultValue "")
        if ([string]::IsNullOrWhiteSpace($claimAuditTier)) {
            throw "Deployment-mode support manifest package '$packageName' is missing claimAuditTier."
        }

        if (-not $claimAuditTierCounts.Contains($claimAuditTier)) {
            $claimAuditTierCounts[$claimAuditTier] = 0
        }

        $claimAuditTierCounts[$claimAuditTier] = [int]$claimAuditTierCounts[$claimAuditTier] + 1
        $supportedModes = @(
            Get-ManifestPropertyValue -Object $package -PropertyName "supportedModes" -DefaultValue @() |
                ForEach-Object { [string]$_ }
        )
        $knownHazards = @(Get-ManifestPropertyValue -Object $package -PropertyName "knownHazards" -DefaultValue @())
        $knownHazardEntryCount += $knownHazards.Count
        $packageScopedClaimCount += $supportedModes.Count

        $packageRows.Add([pscustomobject]([ordered]@{
            PackageName       = $packageName
            NuGetId           = [string](Get-ManifestPropertyValue -Object $package -PropertyName "nugetId" -DefaultValue "")
            ClaimAuditTier    = $claimAuditTier
            SupportedModes    = $supportedModes
            SupportedModeCount = $supportedModes.Count
            KnownHazardCount  = $knownHazards.Count
            Evidence          = [string](Get-ManifestPropertyValue -Object $package -PropertyName "evidence" -DefaultValue "")
        }))
    }

    $transitiveAudit = Get-ManifestPropertyValue -Object $manifest -PropertyName "knownTransitiveHazardAudit"
    if ($null -eq $transitiveAudit) {
        throw "Deployment-mode support manifest is missing knownTransitiveHazardAudit."
    }

    $transitiveAuditEntries = @(Get-ManifestPropertyValue -Object $transitiveAudit -PropertyName "entries" -DefaultValue @())
    if ($transitiveAuditEntries.Count -eq 0) {
        throw "Deployment-mode support manifest knownTransitiveHazardAudit.entries must contain at least one package pattern."
    }

    $transitiveAuditRows = @(
        $transitiveAuditEntries | ForEach-Object {
            [pscustomobject]([ordered]@{
                PackagePattern = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "packagePattern" -DefaultValue "")
                Modes          = @(Get-ManifestPropertyValue -Object $_ -PropertyName "modes" -DefaultValue @() | ForEach-Object { [string]$_ })
            })
        }
    )

    $knownHazardPackages = @($packageRows | Where-Object { $_.KnownHazardCount -gt 0 })
    $packageScopedClaimPackages = @($packageRows | Where-Object { $_.SupportedModeCount -gt 0 })
    $globalNotClaimedRows = @($modeRows | Where-Object { $_.Status -eq "not-claimed" })
    $claimsReportEvidence = Convert-DeploymentModeClaimsReportEvidence `
        -ResolvedClaimsReportPath $ResolvedClaimsReportPath `
        -ResolvedManifestPath $ResolvedManifestPath `
        -ResolvedRepoRoot $ResolvedRepoRoot `
        -ExpectedReleaseValidationMode $releaseValidationMode `
        -ExpectedReleaseValidationDeploymentModes $releaseValidationDeploymentModes `
        -ExpectedGatedModes $gatedModes `
        -ExpectedAuditOnlyModes $auditOnlyModes `
        -ExpectedReleaseValidationSkipsPublish $releaseValidationSkipsPublish `
        -ExpectedNonOptOutGate $nonOptOutGate `
        -ExpectedFailureBlocksRelease $failureBlocksRelease `
        -ExpectedFailOnWarnings $failOnWarnings `
        -ExpectedPublishTargetCount $publishTargetReferences.Count `
        -ExpectedPackageEntryCount $packageRows.Count `
        -ExpectedPackageScopedClaimPackageCount $packageScopedClaimPackages.Count `
        -ExpectedKnownHazardEntryCount $knownHazardEntryCount

    return [pscustomobject]([ordered]@{
        Manifest                             = Get-RepoRelativePath -Path $ResolvedManifestPath -RepoRoot $ResolvedRepoRoot
        ManifestSchemaVersion                = $schemaVersion
        ShippingStableTargetFramework        = $stableTargetFramework
        ReadinessLaneTargetFramework         = $readinessLaneTargetFramework
        ReadinessLaneStatus                  = $readinessLaneStatus
        SourceDocuments                      = @($sourceDocumentReferences | ForEach-Object { $_.Reference })
        ValidationScripts                    = @($validationScriptReferences | ForEach-Object { $_.Reference })
        GlobalClaimStatuses                  = $modeRows.ToArray()
        GlobalClaimCount                     = $modeRows.Count
        GlobalNotClaimedCount                = $globalNotClaimedRows.Count
        PackageEntryCount                    = $packageRows.Count
        PackageRows                          = $packageRows.ToArray()
        PackageScopedClaimPackageCount       = $packageScopedClaimPackages.Count
        PackageScopedClaimCount              = $packageScopedClaimCount
        KnownHazardPackageCount              = $knownHazardPackages.Count
        KnownHazardEntryCount                = $knownHazardEntryCount
        ClaimAuditTierCounts                 = [pscustomobject]$claimAuditTierCounts
        TransitiveAuditEntryCount            = $transitiveAuditRows.Count
        TransitiveAuditLockFileGlobCount     = @(Get-ManifestPropertyValue -Object $transitiveAudit -PropertyName "lockFileGlobs" -DefaultValue @()).Count
        TransitiveAuditRows                  = $transitiveAuditRows
        RepresentativePublishTargetCount     = $publishTargetReferences.Count
        RepresentativePublishTargets         = @($publishTargetReferences | ForEach-Object { $_.Reference })
        PublishProbeReleaseValidationMode    = $releaseValidationMode
        PublishProbeReleaseValidationDeploymentModes = $releaseValidationDeploymentModes
        PublishProbeReleaseValidationSkipsPublish = $releaseValidationSkipsPublish
        PublishProbeNonOptOutGate            = $nonOptOutGate
        PublishProbeGatedModes               = $gatedModes
        PublishProbeAuditOnlyModes           = $auditOnlyModes
        PublishProbeFailureBlocksRelease     = $failureBlocksRelease
        PublishProbeFailOnWarnings           = $failOnWarnings
        PublishProbeGatePromotion            = [string](Get-ManifestPropertyValue -Object $publishProbePolicy -PropertyName "gatePromotion" -DefaultValue "")
        PublishProbePromotionRequirements    = @(Get-ManifestPropertyValue -Object $publishProbePolicy -PropertyName "promotionRequirements" -DefaultValue @() | ForEach-Object { [string]$_ })
        ClaimsReport                         = $claimsReportEvidence.Report
        ClaimsReportPresent                  = $claimsReportEvidence.Present
        ClaimsReportAggregateVerdict         = $claimsReportEvidence.AggregateVerdict
        ClaimsReportDeploymentMode           = $claimsReportEvidence.DeploymentMode
        ClaimsReportPublishProbeGateStatus   = $claimsReportEvidence.PublishProbeGateStatus
        ClaimsReportPublishProbeGateEnabled  = $claimsReportEvidence.PublishProbeGateEnabled
        ClaimsReportPublishProbeGateFailureBlocksRelease = $claimsReportEvidence.PublishProbeGateFailureBlocksRelease
        ClaimsReportPublishProbeGateFailOnWarnings = $claimsReportEvidence.PublishProbeGateFailOnWarnings
        ClaimsReportPublishProbeGateFailureCount = $claimsReportEvidence.PublishProbeGateFailureCount
        ClaimsReportPublishProbeTargetCount  = $claimsReportEvidence.PublishProbeTargetCount
        ClaimsReportPublishProbeWarningCount = $claimsReportEvidence.PublishProbeWarningCount
        ClaimsReportPublishProbeErrorCount   = $claimsReportEvidence.PublishProbeErrorCount
        ClaimsReportPackageClaimCount        = $claimsReportEvidence.PackageClaimCount
        ClaimsReportPackageClaimTruthfulCount = $claimsReportEvidence.PackageClaimTruthfulCount
        ClaimsReportPackageClaimOverstatedCount = $claimsReportEvidence.PackageClaimOverstatedCount
        ClaimsReportHazardInventoryTotalPackages = $claimsReportEvidence.HazardInventoryTotalPackages
        ClaimsReportHazardInventoryKnownHazardEntries = $claimsReportEvidence.HazardInventoryKnownHazardEntries
        ClaimsReportHazardInventoryScopedClaimPackages = $claimsReportEvidence.HazardInventoryScopedClaimPackages
        ClaimsReportHazardInventoryTransitiveAuditStatus = $claimsReportEvidence.HazardInventoryTransitiveAuditStatus
        ClaimsReportHazardInventoryTransitiveAuditMissingEntries = $claimsReportEvidence.HazardInventoryTransitiveAuditMissingEntries
        ClaimsReportHazardInventoryTransitiveAuditLockFileCount = $claimsReportEvidence.HazardInventoryTransitiveAuditLockFileCount
        ClaimsReportHazardInventoryBoundaryAnnotationAuditStatus = $claimsReportEvidence.HazardInventoryBoundaryAnnotationAuditStatus
        ClaimsReportHazardInventoryBoundaryAnnotationAuditFailureCount = $claimsReportEvidence.HazardInventoryBoundaryAnnotationAuditFailureCount
        ClaimsReportHazardInventoryCoreRouteDelegateAuditStatus = $claimsReportEvidence.HazardInventoryCoreRouteDelegateAuditStatus
        ClaimsReportHazardInventoryCoreRouteDelegateAuditFailureCount = $claimsReportEvidence.HazardInventoryCoreRouteDelegateAuditFailureCount
        ClaimsReportHazardInventoryFullCommonRouteDelegateAuditStatus = $claimsReportEvidence.HazardInventoryFullCommonRouteDelegateAuditStatus
        ClaimsReportHazardInventoryFullCommonRouteDelegateAuditFailureCount = $claimsReportEvidence.HazardInventoryFullCommonRouteDelegateAuditFailureCount
        ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditStatus = $claimsReportEvidence.HazardInventoryFullOperatorRouteDelegateAuditStatus
        ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditFailureCount = $claimsReportEvidence.HazardInventoryFullOperatorRouteDelegateAuditFailureCount
        ClaimsReportHazardInventoryOperatorResponseJsonContractAuditStatus = $claimsReportEvidence.HazardInventoryOperatorResponseJsonContractAuditStatus
        ClaimsReportHazardInventoryOperatorResponseJsonContractAuditFailureCount = $claimsReportEvidence.HazardInventoryOperatorResponseJsonContractAuditFailureCount
        ClaimsReportHazardInventoryNonOperatorEndpointAuditStatus = $claimsReportEvidence.HazardInventoryNonOperatorEndpointAuditStatus
        ClaimsReportHazardInventoryNonOperatorEndpointAuditFailureCount = $claimsReportEvidence.HazardInventoryNonOperatorEndpointAuditFailureCount
        ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditStatus = $claimsReportEvidence.HazardInventoryFrameworkEndpointBoundaryAuditStatus
        ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditFailureCount = $claimsReportEvidence.HazardInventoryFrameworkEndpointBoundaryAuditFailureCount
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

    $executionReport = Get-ManifestPropertyValue -Object $manifest -PropertyName "executionReport"
    if ($null -eq $executionReport) {
        throw "Adoption smoke support manifest is missing executionReport."
    }

    $executionReportSchemaVersion = [string](Get-ManifestPropertyValue -Object $executionReport -PropertyName "schemaVersion" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($executionReportSchemaVersion)) {
        throw "Adoption smoke executionReport is missing schemaVersion."
    }

    $executionReportPath = [string](Get-ManifestPropertyValue -Object $executionReport -PropertyName "defaultPath" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($executionReportPath)) {
        throw "Adoption smoke executionReport is missing defaultPath."
    }

    if (-not $executionReportPath.EndsWith(".json", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Adoption smoke executionReport defaultPath '$executionReportPath' must point at a JSON report."
    }

    $executionReportStatusValues = @(
        Get-ManifestPropertyValue -Object $executionReport -PropertyName "statusValues" -DefaultValue @() |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    foreach ($requiredStatus in @("passed", "failed")) {
        if ($executionReportStatusValues -notcontains $requiredStatus) {
            throw "Adoption smoke executionReport statusValues must include '$requiredStatus'."
        }
    }

    $executionReportRequiredFields = @(
        Get-ManifestPropertyValue -Object $executionReport -PropertyName "requiredFields" -DefaultValue @() |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    foreach ($requiredField in @('$schemaVersion', "ScenarioId", "Status", "Assertions", "RuntimeProbes", "Paths")) {
        if ($executionReportRequiredFields -notcontains $requiredField) {
            throw "Adoption smoke executionReport requiredFields must include '$requiredField'."
        }
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
        ExecutionReport           = [pscustomobject]([ordered]@{
            SchemaVersion = $executionReportSchemaVersion
            DefaultPath = $executionReportPath
            StatusValues = $executionReportStatusValues
            RequiredFields = $executionReportRequiredFields
        })
        RuntimeProbes             = $runtimeProbes
        ValidatedReferences       = @(
            $validationScriptReference
            $referenceModuleProjectReference
            $supportingScriptReferences
            $sourceDocumentReferences
        ) | Sort-Object Reference -Unique
    })
}

function Convert-TestCoverageEvidence {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRoadmapPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot
    )

    if (-not (Test-Path -LiteralPath $ResolvedRoadmapPath -PathType Leaf)) {
        throw "Test coverage roadmap '$ResolvedRoadmapPath' was not found."
    }

    $lines = @(Get-Content -LiteralPath $ResolvedRoadmapPath -Encoding UTF8)
    $markdown = [string]::Join("`n", $lines)
    $roadmapDirectory = [System.IO.Path]::GetDirectoryName($ResolvedRoadmapPath)
    $roadmapReference = Get-RepoRelativePath -Path $ResolvedRoadmapPath -RepoRoot $ResolvedRepoRoot

    $validatedReferences = [System.Collections.Generic.List[object]]::new()
    $validatedReferences.Add([pscustomobject]([ordered]@{
        Reference  = $roadmapReference
        DeclaredAs = $roadmapReference
        Kind       = "doc"
    }))

    $layeredPostureRows = Convert-MarkdownTable `
        -TableLines (Get-MarkdownTableLines `
            -SectionLines (Get-MarkdownSectionLines -Lines $lines -Heading "Layered test posture") `
            -Heading "Layered test posture") `
        -Heading "Layered test posture"

    if (@($layeredPostureRows).Count -eq 0) {
        throw "Test coverage roadmap must declare at least one layered test posture row."
    }

    $layeredProjects = @(
        foreach ($row in $layeredPostureRows) {
            $projectCell = [string]$row.Project
            $projectPathMatch = [regex]::Match($projectCell, '\]\((?<Path>[^)]+)\)')
            if (-not $projectPathMatch.Success) {
                throw "Test coverage roadmap layered project row '$projectCell' must link to a repo path."
            }

            $declaredPath = $projectPathMatch.Groups["Path"].Value
            $declaredPathWithoutAnchor = ($declaredPath -split '#')[0]
            $resolvedProjectPath = Resolve-FullPath -Path $declaredPathWithoutAnchor -BasePath $roadmapDirectory
            if (-not (Test-Path -LiteralPath $resolvedProjectPath)) {
                throw "Test coverage roadmap layered project reference '$declaredPath' was not found at '$resolvedProjectPath'."
            }

            $projectReference = Get-RepoRelativePath -Path $resolvedProjectPath -RepoRoot $ResolvedRepoRoot
            $validatedReferences.Add([pscustomobject]([ordered]@{
                Reference  = $projectReference
                DeclaredAs = $declaredPath
                Kind       = if (Test-Path -LiteralPath $resolvedProjectPath -PathType Container) { "directory" } else { Get-SourceReferenceKind -Reference $projectReference }
            }))

            [pscustomobject]([ordered]@{
                Project      = Remove-MarkdownInlineFormatting -Value $projectCell
                Reference    = $projectReference
                Layer        = Remove-MarkdownInlineFormatting -Value ([string]$row.Layer)
                WhatItProves = Remove-MarkdownInlineFormatting -Value ([string]$row.'What it proves')
            })
        }
    )

    $gapCriterionLines = @(
        Get-MarkdownSectionLines -Lines $lines -Heading "Gap definition criteria" |
            Where-Object { $_ -match '^\s*-\s+\([a-z]\)\s+' }
    )
    if ($gapCriterionLines.Count -eq 0) {
        throw "Test coverage roadmap gap definition criteria were not found."
    }

    $gapCriteria = @(
        foreach ($line in $gapCriterionLines) {
            $match = [regex]::Match($line, '^\s*-\s+\((?<Id>[a-z])\)\s+(?<Text>.+)$')
            [pscustomobject]([ordered]@{
                Id   = $match.Groups["Id"].Value
                Text = Remove-MarkdownInlineFormatting -Value $match.Groups["Text"].Value
            })
        }
    )

    $recommendationSection = Get-MarkdownSectionLines -Lines $lines -Heading "Prioritized recommendations"
    $recommendations = @(
        foreach ($line in $recommendationSection) {
            $match = [regex]::Match($line, '^### #(?<Number>\d+)\s+\S+\s+(?<Text>.+)$')
            if (-not $match.Success) {
                continue
            }

            $number = [int]$match.Groups["Number"].Value
            $text = $match.Groups["Text"].Value.Trim()
            $priorityMatch = [regex]::Match($text, '(?<Priority>high|medium|low) priority')
            if (-not $priorityMatch.Success) {
                throw "Test coverage recommendation #$number must declare high, medium, or low priority."
            }

            $status = "active"
            if ($text -match '\*\*shipped through\b') {
                $status = "shipped"
            }
            elseif ($text -match '\*\*gated\*\*') {
                $status = "gated"
            }

            $title = [regex]::Replace($text, '\s+\((?:high|medium|low) priority,.*$', '')

            [pscustomobject]([ordered]@{
                Number   = $number
                Title    = Remove-MarkdownInlineFormatting -Value $title
                Priority = $priorityMatch.Groups["Priority"].Value
                Status   = $status
            })
        }
    )

    if ($recommendations.Count -eq 0) {
        throw "Test coverage roadmap prioritized recommendations were not found."
    }

    $quarantineRows = @(
        Convert-MarkdownTable `
            -TableLines (Get-MarkdownTableLines `
                -SectionLines (Get-MarkdownSectionLines -Lines $lines -Heading "Test-flake quarantine queue") `
                -Heading "Test-flake quarantine queue") `
            -Heading "Test-flake quarantine queue" |
            ForEach-Object {
                $deadline = Remove-MarkdownInlineFormatting -Value ([string]$_.Deadline)
                $action = Remove-MarkdownInlineFormatting -Value ([string]$_.'Quarantine action')
                $status = if ($deadline -match '^Closed\b' -or $action -match '\bResolved\b') { "closed" } else { "open" }

                [pscustomobject]([ordered]@{
                    Test             = Remove-MarkdownInlineFormatting -Value ([string]$_.Test)
                    Project          = Remove-MarkdownInlineFormatting -Value ([string]$_.Project)
                    FirstObserved    = Remove-MarkdownInlineFormatting -Value ([string]$_.'First observed')
                    QuarantineAction = $action
                    Deadline         = $deadline
                    Status           = $status
                })
            }
    )

    $queueEmptyDeclared = $markdown.Contains("The queue is empty.", [System.StringComparison]::Ordinal)
    $openQuarantineRows = @($quarantineRows | Where-Object { $_.Status -ne "closed" })
    if ($queueEmptyDeclared -and $openQuarantineRows.Count -gt 0) {
        throw "Test coverage roadmap declares an empty quarantine queue but has $($openQuarantineRows.Count) open row(s)."
    }

    $queueStatus = if ($queueEmptyDeclared -and $openQuarantineRows.Count -eq 0) {
        "empty"
    }
    elseif ($openQuarantineRows.Count -gt 0) {
        "open"
    }
    else {
        "not-declared"
    }

    return [pscustomobject]([ordered]@{
        Roadmap                        = $roadmapReference
        SourceDocument                 = $roadmapReference
        LayeredProjectCount            = $layeredProjects.Count
        GapDefinitionCriterionCount    = $gapCriteria.Count
        RecommendationCount            = $recommendations.Count
        ShippedRecommendationCount     = @($recommendations | Where-Object { $_.Status -eq "shipped" }).Count
        GatedRecommendationCount       = @($recommendations | Where-Object { $_.Status -eq "gated" }).Count
        ActiveGapRecommendationCount   = @($recommendations | Where-Object { $_.Status -eq "active" }).Count
        QuarantineEntryCount           = $quarantineRows.Count
        OpenQuarantineEntryCount       = $openQuarantineRows.Count
        QuarantineQueueStatus          = $queueStatus
        LayeredProjects                = $layeredProjects
        GapCriteria                    = $gapCriteria
        Recommendations                = $recommendations
        QuarantineRows                 = $quarantineRows
        ValidatedReferences            = @($validatedReferences | Sort-Object Reference -Unique)
    })
}

function Convert-ProviderIntegrationEvidence {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot
    )

    if (-not (Test-Path -LiteralPath $ResolvedManifestPath -PathType Leaf)) {
        throw "Provider integration support manifest '$ResolvedManifestPath' was not found."
    }

    $manifest = Get-Content -LiteralPath $ResolvedManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 32
    $schemaVersion = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName '$schemaVersion' -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($schemaVersion)) {
        throw "Provider integration support manifest is missing '`$schemaVersion'."
    }

    $status = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "status" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($status)) {
        throw "Provider integration support manifest is missing status."
    }

    $sourceDocumentReferences = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "sourceDocs" -DefaultValue @() |
            ForEach-Object {
                Resolve-ProviderIntegrationManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "sourceDocs" -PathType "File"
            }
    )
    if ($sourceDocumentReferences.Count -eq 0) {
        throw "Provider integration support manifest must declare at least one source document."
    }

    $validationProjectReferences = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "validationProjects" -DefaultValue @() |
            ForEach-Object {
                Resolve-ProviderIntegrationManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "validationProjects" -PathType "File"
            }
    )
    if ($validationProjectReferences.Count -eq 0) {
        throw "Provider integration support manifest must declare at least one validation project."
    }

    $dependencyHealthProviderManifest = Convert-ProviderIntegrationDependencyHealthManifest -ProviderIntegrationManifest $manifest -ResolvedRepoRoot $ResolvedRepoRoot

    $allowedStatuses = @(
        "live-proof-available",
        "composition-only",
        "planned",
        "not-claimed"
    )
    $providerRows = [System.Collections.Generic.List[object]]::new()

    foreach ($row in @(Get-ManifestPropertyValue -Object $manifest -PropertyName "providerRows" -DefaultValue @())) {
        $id = [string](Get-ManifestPropertyValue -Object $row -PropertyName "id" -DefaultValue "")
        $provider = [string](Get-ManifestPropertyValue -Object $row -PropertyName "provider" -DefaultValue "")
        $family = [string](Get-ManifestPropertyValue -Object $row -PropertyName "family" -DefaultValue "")
        $rowStatus = [string](Get-ManifestPropertyValue -Object $row -PropertyName "status" -DefaultValue "")
        $defaultRunBehavior = [string](Get-ManifestPropertyValue -Object $row -PropertyName "defaultRunBehavior" -DefaultValue "")
        $testProjectPath = [string](Get-ManifestPropertyValue -Object $row -PropertyName "testProject" -DefaultValue "")

        foreach ($field in @(
            [pscustomobject]@{ Name = "id"; Value = $id },
            [pscustomobject]@{ Name = "provider"; Value = $provider },
            [pscustomobject]@{ Name = "family"; Value = $family },
            [pscustomobject]@{ Name = "status"; Value = $rowStatus },
            [pscustomobject]@{ Name = "defaultRunBehavior"; Value = $defaultRunBehavior },
            [pscustomobject]@{ Name = "testProject"; Value = $testProjectPath }
        )) {
            if ([string]::IsNullOrWhiteSpace($field.Value)) {
                throw "Provider integration evidence row must include $($field.Name)."
            }
        }

        if ($allowedStatuses -notcontains $rowStatus) {
            throw "Unsupported provider integration evidence status '$rowStatus' for row '$id'."
        }

        $testProjectReference = Resolve-ProviderIntegrationManifestPath -DeclaredPath $testProjectPath -ResolvedRepoRoot $ResolvedRepoRoot -Context "provider row '$id' testProject" -PathType "File"
        $testFileReferences = @(
            Get-ManifestPropertyValue -Object $row -PropertyName "testFiles" -DefaultValue @() |
                ForEach-Object {
                    Resolve-ProviderIntegrationManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "provider row '$id' testFiles" -PathType "File"
                }
        )
        if ($testFileReferences.Count -eq 0) {
            throw "Provider integration evidence row '$id' must declare at least one test file."
        }

        $rowSourceDocumentReferences = @(
            Get-ManifestPropertyValue -Object $row -PropertyName "sourceDocuments" -DefaultValue @() |
                ForEach-Object {
                    Resolve-ProviderIntegrationManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "provider row '$id' sourceDocuments" -PathType "File"
                }
        )
        if ($rowSourceDocumentReferences.Count -eq 0) {
            throw "Provider integration evidence row '$id' must declare at least one source document."
        }

        $runtimeContracts = @(
            Get-ManifestPropertyValue -Object $row -PropertyName "runtimeContracts" -DefaultValue @() |
                ForEach-Object { [string]$_ } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )
        if ($runtimeContracts.Count -eq 0) {
            throw "Provider integration evidence row '$id' must declare at least one runtime contract."
        }

        $environmentVariables = @(
            Get-ManifestPropertyValue -Object $row -PropertyName "environmentVariables" -DefaultValue @() |
                ForEach-Object { [string]$_ } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )
        $externalServiceGate = [string](Get-ManifestPropertyValue -Object $row -PropertyName "externalServiceGate" -DefaultValue "")
        if (
            $rowStatus -eq "live-proof-available" -and
            [string]::IsNullOrWhiteSpace($externalServiceGate) -and
            $defaultRunBehavior -ne "runs-without-external-services"
        ) {
            throw "Provider integration live-proof row '$id' must declare externalServiceGate unless defaultRunBehavior is runs-without-external-services."
        }

        $providerRows.Add([pscustomobject]([ordered]@{
            Id                   = $id
            Provider             = $provider
            Family               = $family
            Status               = $rowStatus
            DefaultRunBehavior   = $defaultRunBehavior
            ExternalServiceGate  = $externalServiceGate
            TestProject          = $testProjectReference.Reference
            TestFiles            = @($testFileReferences | ForEach-Object { $_.Reference })
            SourceDocuments      = @($rowSourceDocumentReferences | ForEach-Object { $_.Reference })
            RuntimeContracts     = $runtimeContracts
            EnvironmentVariables = $environmentVariables
            ValidatedReferences  = @(
                $testProjectReference
                $testFileReferences
                $rowSourceDocumentReferences
            ) | Sort-Object Reference -Unique
        }))
    }

    if ($providerRows.Count -eq 0) {
        throw "Provider integration support manifest must declare at least one provider row."
    }

    Assert-ProviderIntegrationDependencyHealthRows -ProviderRows $providerRows.ToArray() -DependencyHealthProviderManifest $dependencyHealthProviderManifest

    $liveProofRows = @($providerRows | Where-Object { $_.Status -eq "live-proof-available" })
    $compositionOnlyRows = @($providerRows | Where-Object { $_.Status -eq "composition-only" })
    $plannedRows = @($providerRows | Where-Object { $_.Status -eq "planned" })
    $notClaimedRows = @($providerRows | Where-Object { $_.Status -eq "not-claimed" })
    $externalServiceGateRows = @($providerRows | Where-Object { -not [string]::IsNullOrWhiteSpace($_.ExternalServiceGate) })
    $defaultSkippedRows = @($providerRows | Where-Object { $_.DefaultRunBehavior -eq "skipped-unless-enabled" })
    $runtimeContracts = @($providerRows | ForEach-Object { $_.RuntimeContracts } | Sort-Object -Unique)
    $environmentVariables = @($providerRows | ForEach-Object { $_.EnvironmentVariables } | Sort-Object -Unique)

    return [pscustomobject]([ordered]@{
        Manifest                 = Get-RepoRelativePath -Path $ResolvedManifestPath -RepoRoot $ResolvedRepoRoot
        ManifestSchemaVersion    = $schemaVersion
        Status                   = $status
        Summary                  = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "summary" -DefaultValue "")
        SourceDocuments          = @($sourceDocumentReferences | ForEach-Object { $_.Reference })
        ValidationProjects       = @($validationProjectReferences | ForEach-Object { $_.Reference })
        DependencyHealthProviderManifest = if ($null -eq $dependencyHealthProviderManifest) {
            $null
        } else {
            [pscustomobject]([ordered]@{
                Reference             = $dependencyHealthProviderManifest.Reference
                ManifestSchemaVersion = $dependencyHealthProviderManifest.ManifestSchemaVersion
                Status                = $dependencyHealthProviderManifest.Status
                ProviderCount         = $dependencyHealthProviderManifest.ProviderCount
            })
        }
        EvidenceRowCount         = $providerRows.Count
        LiveProofCount           = $liveProofRows.Count
        CompositionOnlyCount     = $compositionOnlyRows.Count
        PlannedCount             = $plannedRows.Count
        NotClaimedCount          = $notClaimedRows.Count
        ExternalServiceGateCount = $externalServiceGateRows.Count
        DefaultSkippedCount      = $defaultSkippedRows.Count
        RuntimeContractCount     = $runtimeContracts.Count
        EnvironmentVariableCount = $environmentVariables.Count
        RuntimeContracts         = $runtimeContracts
        EnvironmentVariables     = $environmentVariables
        ProviderRows             = $providerRows.ToArray()
        ValidatedReferences      = @(
            $sourceDocumentReferences
            $validationProjectReferences
            if ($null -ne $dependencyHealthProviderManifest) {
                $dependencyHealthProviderManifest.ValidatedReferences
            }
            $providerRows | ForEach-Object { $_.ValidatedReferences }
        ) | Sort-Object Reference -Unique
    })
}

function Convert-SupportManifestSourceConcordance {
    param(
        $Concordance,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$OwnerName
    )

    if ($null -eq $Concordance) {
        throw "$OwnerName is missing runtimeConcordance."
    }

    $status = [string](Get-ManifestPropertyValue -Object $Concordance -PropertyName "status" -DefaultValue "")
    $source = [string](Get-ManifestPropertyValue -Object $Concordance -PropertyName "source" -DefaultValue "")
    $profileSurfaceToken = [string](Get-ManifestPropertyValue -Object $Concordance -PropertyName "profileSurfaceToken" -DefaultValue "")
    foreach ($field in @(
        [pscustomobject]@{ Name = "runtimeConcordance.status"; Value = $status },
        [pscustomobject]@{ Name = "runtimeConcordance.source"; Value = $source },
        [pscustomobject]@{ Name = "runtimeConcordance.profileSurfaceToken"; Value = $profileSurfaceToken }
    )) {
        if ([string]::IsNullOrWhiteSpace($field.Value)) {
            throw "$OwnerName is missing $($field.Name)."
        }
    }

    if ($status -ne "matched") {
        throw "$OwnerName runtimeConcordance.status must be 'matched'."
    }

    $sourceReference = Resolve-SupportManifestPath -DeclaredPath $source -ResolvedRepoRoot $ResolvedRepoRoot -Context "runtimeConcordance.source" -OwnerName $OwnerName -PathType "File"
    $resolvedSourcePath = Resolve-FullPath -Path $source -BasePath $ResolvedRepoRoot
    $sourceText = Get-Content -LiteralPath $resolvedSourcePath -Raw -Encoding UTF8
    $requiredTokens = @(
        $profileSurfaceToken
        Get-ManifestPropertyValue -Object $Concordance -PropertyName "requiredTokens" -DefaultValue @() |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )

    if ($requiredTokens.Count -le 1) {
        throw "$OwnerName runtimeConcordance.requiredTokens must declare at least one token beyond the profile surface token."
    }

    $missingTokens = @(
        $requiredTokens |
            Where-Object { -not $sourceText.Contains($_) }
    )

    if ($missingTokens.Count -gt 0) {
        throw "$OwnerName runtimeConcordance source '$($sourceReference.Reference)' is missing required token(s): $([string]::Join(', ', $missingTokens))."
    }

    return [pscustomobject]([ordered]@{
        Status              = $status
        Source              = $sourceReference.Reference
        ProfileSurfaceToken = $profileSurfaceToken
        RequiredTokenCount  = $requiredTokens.Count
        MatchedTokenCount   = $requiredTokens.Count - $missingTokens.Count
        MissingTokenCount   = $missingTokens.Count
        RequiredTokens      = $requiredTokens
        MissingTokens       = $missingTokens
        ValidatedReferences = @($sourceReference)
    })
}

function Convert-EventingOperationalSuperiorityEvidence {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot
    )

    $ownerName = "Eventing operational-superiority support manifest"
    if (-not (Test-Path -LiteralPath $ResolvedManifestPath -PathType Leaf)) {
        throw "$ownerName '$ResolvedManifestPath' was not found."
    }

    $manifestReference = [pscustomobject]([ordered]@{
        Reference  = Get-RepoRelativePath -Path $ResolvedManifestPath -RepoRoot $ResolvedRepoRoot
        DeclaredAs = Get-RepoRelativePath -Path $ResolvedManifestPath -RepoRoot $ResolvedRepoRoot
        Kind       = "script"
    })
    $manifest = Get-Content -LiteralPath $ResolvedManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 32
    $schemaVersion = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName '$schemaVersion' -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($schemaVersion)) {
        throw "$ownerName is missing '`$schemaVersion'."
    }

    $status = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "status" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($status)) {
        throw "$ownerName is missing status."
    }

    $allowedStatuses = @("claimed", "partial", "not-claimed")
    if ($allowedStatuses -notcontains $status) {
        throw "$ownerName has unsupported status '$status'."
    }

    $sourceDocumentReferences = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "sourceDocs" -DefaultValue @() |
            ForEach-Object {
                Resolve-SupportManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "sourceDocs" -OwnerName $ownerName -PathType "File"
            }
    )
    if ($sourceDocumentReferences.Count -eq 0) {
        throw "$ownerName must declare at least one source document."
    }

    $validationProjectReferences = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "validationProjects" -DefaultValue @() |
            ForEach-Object {
                Resolve-SupportManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "validationProjects" -OwnerName $ownerName -PathType "File"
            }
    )
    if ($validationProjectReferences.Count -eq 0) {
        throw "$ownerName must declare at least one validation project."
    }

    $validationFileReferences = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "validationFiles" -DefaultValue @() |
            ForEach-Object {
                Resolve-SupportManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "validationFiles" -OwnerName $ownerName -PathType "File"
            }
    )
    if ($validationFileReferences.Count -eq 0) {
        throw "$ownerName must declare at least one validation file."
    }

    $runtimeConcordance = Convert-SupportManifestSourceConcordance `
        -Concordance (Get-ManifestPropertyValue -Object $manifest -PropertyName "runtimeConcordance" -DefaultValue $null) `
        -ResolvedRepoRoot $ResolvedRepoRoot `
        -OwnerName $ownerName

    $technology = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "technology" -DefaultValue "")
    $surfaceId = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "surfaceId" -DefaultValue "")
    $profileEntryId = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "profileEntryId" -DefaultValue "")
    $hotPathBindingMode = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "hotPathBindingMode" -DefaultValue "")
    $configurationRole = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "configurationRole" -DefaultValue "")
    foreach ($field in @(
        [pscustomobject]@{ Name = "technology"; Value = $technology },
        [pscustomobject]@{ Name = "surfaceId"; Value = $surfaceId },
        [pscustomobject]@{ Name = "profileEntryId"; Value = $profileEntryId },
        [pscustomobject]@{ Name = "hotPathBindingMode"; Value = $hotPathBindingMode },
        [pscustomobject]@{ Name = "configurationRole"; Value = $configurationRole }
    )) {
        if ([string]::IsNullOrWhiteSpace($field.Value)) {
            throw "$ownerName is missing $($field.Name)."
        }
    }

    if ($technology -ne "event-driven-integration") {
        throw "$ownerName technology must be 'event-driven-integration'."
    }

    if ($surfaceId -ne "eventing-superiority-profile") {
        throw "$ownerName surfaceId must be 'eventing-superiority-profile'."
    }

    if ($profileEntryId -ne "operational-superiority-coverage") {
        throw "$ownerName profileEntryId must be 'operational-superiority-coverage'."
    }

    if ($hotPathBindingMode -ne "code-first-publish-subscribe") {
        throw "$ownerName hotPathBindingMode must be 'code-first-publish-subscribe'."
    }

    $providerNeutral = ConvertTo-RequiredManifestBoolean -Value (Get-ManifestPropertyValue -Object $manifest -PropertyName "providerNeutral" -DefaultValue $null) -Name "$ownerName providerNeutral"
    if (-not $providerNeutral) {
        throw "$ownerName must remain provider-neutral."
    }

    $wolverineRequired = ConvertTo-RequiredManifestBoolean -Value (Get-ManifestPropertyValue -Object $manifest -PropertyName "wolverineRequired" -DefaultValue $null) -Name "$ownerName wolverineRequired"
    if ($wolverineRequired) {
        throw "$ownerName must keep Wolverine optional by declaring wolverineRequired=false."
    }

    $comparisonBaseline = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "comparisonBaseline" -DefaultValue @() |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    foreach ($requiredBaseline in @("MassTransit", "NServiceBus", "Wolverine", "MediatR")) {
        if ($comparisonBaseline -notcontains $requiredBaseline) {
            throw "$ownerName comparisonBaseline must include '$requiredBaseline'."
        }
    }

    $promotionGate = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "promotionGate" -DefaultValue "")
    $promotionPolicy = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "promotionPolicy" -DefaultValue "")
    $promotionEvidenceContract = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "promotionEvidenceContract" -DefaultValue "")
    $promotionEvidenceContractVersion = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "promotionEvidenceContractVersion" -DefaultValue "")
    $promotionTarget = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "promotionTarget" -DefaultValue "")
    $promotionRequiredStatus = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "promotionRequiredStatus" -DefaultValue "")
    $promotionDecisionCode = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "promotionDecisionCode" -DefaultValue "")
    foreach ($field in @(
        [pscustomobject]@{ Name = "promotionGate"; Value = $promotionGate },
        [pscustomobject]@{ Name = "promotionPolicy"; Value = $promotionPolicy },
        [pscustomobject]@{ Name = "promotionEvidenceContract"; Value = $promotionEvidenceContract },
        [pscustomobject]@{ Name = "promotionEvidenceContractVersion"; Value = $promotionEvidenceContractVersion },
        [pscustomobject]@{ Name = "promotionTarget"; Value = $promotionTarget },
        [pscustomobject]@{ Name = "promotionRequiredStatus"; Value = $promotionRequiredStatus },
        [pscustomobject]@{ Name = "promotionDecisionCode"; Value = $promotionDecisionCode }
    )) {
        if ([string]::IsNullOrWhiteSpace($field.Value)) {
            throw "$ownerName is missing $($field.Name)."
        }
    }

    if ($promotionEvidenceContract -ne "cephalon-eventing-operational-superiority-promotion-v1") {
        throw "$ownerName promotionEvidenceContract must be 'cephalon-eventing-operational-superiority-promotion-v1'."
    }

    if ($promotionTarget -ne "eventing-operational-superiority") {
        throw "$ownerName promotionTarget must be 'eventing-operational-superiority'."
    }

    if ($promotionRequiredStatus -ne "claimed") {
        throw "$ownerName promotionRequiredStatus must be 'claimed'."
    }

    $dimensionRows = [System.Collections.Generic.List[object]]::new()
    foreach ($row in @(Get-ManifestPropertyValue -Object $manifest -PropertyName "requiredDimensions" -DefaultValue @())) {
        $id = [string](Get-ManifestPropertyValue -Object $row -PropertyName "id" -DefaultValue "")
        $dimensionStatus = [string](Get-ManifestPropertyValue -Object $row -PropertyName "status" -DefaultValue "")
        $requiredStatus = [string](Get-ManifestPropertyValue -Object $row -PropertyName "requiredStatus" -DefaultValue "")
        $nextRequirement = [string](Get-ManifestPropertyValue -Object $row -PropertyName "nextRequirement" -DefaultValue "")

        foreach ($field in @(
            [pscustomobject]@{ Name = "id"; Value = $id },
            [pscustomobject]@{ Name = "status"; Value = $dimensionStatus },
            [pscustomobject]@{ Name = "requiredStatus"; Value = $requiredStatus },
            [pscustomobject]@{ Name = "nextRequirement"; Value = $nextRequirement }
        )) {
            if ([string]::IsNullOrWhiteSpace($field.Value)) {
                throw "$ownerName requiredDimensions row must include $($field.Name)."
            }
        }

        if ($allowedStatuses -notcontains $dimensionStatus) {
            throw "$ownerName dimension '$id' has unsupported status '$dimensionStatus'."
        }

        if ($requiredStatus -ne "claimed") {
            throw "$ownerName dimension '$id' must require status 'claimed'."
        }

        $dimensionSourceReferences = @(
            Get-ManifestPropertyValue -Object $row -PropertyName "sourceDocs" -DefaultValue @() |
                ForEach-Object {
                    Resolve-SupportManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "requiredDimensions '$id' sourceDocs" -OwnerName $ownerName -PathType "File"
                }
        )
        if ($dimensionSourceReferences.Count -eq 0) {
            throw "$ownerName dimension '$id' must declare at least one source document."
        }

        $dimensionValidationFileReferences = @(
            Get-ManifestPropertyValue -Object $row -PropertyName "validationFiles" -DefaultValue @() |
                ForEach-Object {
                    Resolve-SupportManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "requiredDimensions '$id' validationFiles" -OwnerName $ownerName -PathType "File"
                }
        )
        if ($dimensionValidationFileReferences.Count -eq 0) {
            throw "$ownerName dimension '$id' must declare at least one validation file."
        }

        $runtimeEvidenceTokens = @(
            Get-ManifestPropertyValue -Object $row -PropertyName "runtimeEvidenceTokens" -DefaultValue @() |
                ForEach-Object { [string]$_ } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )
        if ($runtimeEvidenceTokens.Count -eq 0) {
            throw "$ownerName dimension '$id' must declare at least one runtimeEvidenceToken."
        }

        $dimensionRows.Add([pscustomobject]([ordered]@{
            Id                    = $id
            Status                = $dimensionStatus
            RequiredStatus        = $requiredStatus
            NextRequirement       = $nextRequirement
            SourceDocuments       = @($dimensionSourceReferences | ForEach-Object { $_.Reference })
            ValidationFiles       = @($dimensionValidationFileReferences | ForEach-Object { $_.Reference })
            RuntimeEvidenceTokens = $runtimeEvidenceTokens
            ValidatedReferences   = @(
                $dimensionSourceReferences
                $dimensionValidationFileReferences
            ) | Sort-Object Reference -Unique
        }))
    }

    if ($dimensionRows.Count -eq 0) {
        throw "$ownerName must declare at least one required dimension."
    }

    $coveredRows = @($dimensionRows | Where-Object { $_.Status -eq $_.RequiredStatus })
    $partialRows = @($dimensionRows | Where-Object { $_.Status -eq "partial" })
    $missingRows = @($dimensionRows | Where-Object { $_.Status -eq "not-claimed" })
    $blockingRows = @($dimensionRows | Where-Object { $_.Status -ne $_.RequiredStatus })
    $coveragePercent = [int][math]::Floor(($coveredRows.Count / [double]$dimensionRows.Count) * 100)
    $computedPromotionAllowed = (
        $status -eq "claimed" -and
        $promotionGate -eq "allowed" -and
        $promotionPolicy -eq "all-operational-superiority-dimensions-claimed" -and
        $promotionDecisionCode -eq "all-required-dimensions-claimed" -and
        $coveredRows.Count -eq $dimensionRows.Count -and
        -not $wolverineRequired -and
        $providerNeutral
    )

    $declaredPromotionAllowed = ConvertTo-RequiredManifestBoolean -Value (Get-ManifestPropertyValue -Object $manifest -PropertyName "promotionAllowed" -DefaultValue $null) -Name "$ownerName promotionAllowed"
    if ($declaredPromotionAllowed -ne $computedPromotionAllowed) {
        throw "$ownerName promotionAllowed '$declaredPromotionAllowed' does not match computed value '$computedPromotionAllowed'."
    }

    return [pscustomobject]([ordered]@{
        Manifest                         = Get-RepoRelativePath -Path $ResolvedManifestPath -RepoRoot $ResolvedRepoRoot
        ManifestSchemaVersion            = $schemaVersion
        Status                           = $status
        Summary                          = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "summary" -DefaultValue "")
        Technology                       = $technology
        SurfaceId                        = $surfaceId
        ProfileEntryId                   = $profileEntryId
        SourceDocuments                  = @($sourceDocumentReferences | ForEach-Object { $_.Reference })
        ValidationProjects               = @($validationProjectReferences | ForEach-Object { $_.Reference })
        ValidationFiles                  = @($validationFileReferences | ForEach-Object { $_.Reference })
        ComparisonBaseline               = $comparisonBaseline
        ComparisonBaselineCount          = $comparisonBaseline.Count
        ProviderNeutral                  = $providerNeutral
        WolverineRequired                = $wolverineRequired
        HotPathBindingMode               = $hotPathBindingMode
        ConfigurationRole                = $configurationRole
        RuntimeConcordanceStatus         = $runtimeConcordance.Status
        RuntimeConcordanceSource         = $runtimeConcordance.Source
        RuntimeConcordanceTokenCount     = $runtimeConcordance.RequiredTokenCount
        RuntimeConcordanceMatchedTokenCount = $runtimeConcordance.MatchedTokenCount
        RuntimeConcordanceMissingTokenCount = $runtimeConcordance.MissingTokenCount
        RuntimeConcordanceProfileSurfaceToken = $runtimeConcordance.ProfileSurfaceToken
        RuntimeConcordanceRequiredTokens = $runtimeConcordance.RequiredTokens
        PromotionGate                    = $promotionGate
        PromotionAllowed                 = $declaredPromotionAllowed
        PromotionPolicy                  = $promotionPolicy
        PromotionEvidenceContract        = $promotionEvidenceContract
        PromotionEvidenceContractVersion = $promotionEvidenceContractVersion
        PromotionTarget                  = $promotionTarget
        PromotionRequiredStatus          = $promotionRequiredStatus
        PromotionDecisionCode            = $promotionDecisionCode
        RequiredDimensionCount           = $dimensionRows.Count
        CoveredDimensionCount            = $coveredRows.Count
        PartialDimensionCount            = $partialRows.Count
        MissingDimensionCount            = $missingRows.Count
        BlockingDimensionCount           = $blockingRows.Count
        CoveragePercent                  = $coveragePercent
        OperationalSuperiorityComplete   = $blockingRows.Count -eq 0
        RequiredDimensions               = $dimensionRows.ToArray()
        BlockingDimensions               = @($blockingRows | ForEach-Object {
            [pscustomobject]([ordered]@{
                Id              = $_.Id
                Status          = $_.Status
                RequiredStatus  = $_.RequiredStatus
                NextRequirement = $_.NextRequirement
            })
        })
        NextRequirements                 = @($blockingRows | Where-Object { $_.NextRequirement -ne "none" } | ForEach-Object {
            [pscustomobject]([ordered]@{
                Id              = $_.Id
                NextRequirement = $_.NextRequirement
            })
        })
        ValidatedReferences              = @(
            $manifestReference
            $sourceDocumentReferences
            $validationProjectReferences
            $validationFileReferences
            $runtimeConcordance.ValidatedReferences
            $dimensionRows | ForEach-Object { $_.ValidatedReferences }
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

    $stableBaselineManifestPath = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "stableBaselineManifest" -DefaultValue "")
    if ($stableBaselinesPublishedValue -and [string]::IsNullOrWhiteSpace($stableBaselineManifestPath)) {
        throw "SRE posture support manifest stableBaselineManifest is required when stableBaselinesPublished is true."
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

    $guardrailEntryLookup = @{}
    foreach ($guardrailEntry in $guardrailEntries) {
        $reportFileName = [string](Get-ManifestPropertyValue -Object $guardrailEntry -PropertyName "reportFileName" -DefaultValue "")
        $benchmark = [string](Get-ManifestPropertyValue -Object $guardrailEntry -PropertyName "benchmark" -DefaultValue "")
        if ([string]::IsNullOrWhiteSpace($reportFileName) -or [string]::IsNullOrWhiteSpace($benchmark)) {
            throw "SRE posture guardrail catalog '$($guardrailCatalogReference.Reference)' contains an entry without reportFileName and benchmark."
        }

        $guardrailKey = "$reportFileName`n$benchmark"
        if ($guardrailEntryLookup.ContainsKey($guardrailKey)) {
            throw "SRE posture guardrail catalog '$($guardrailCatalogReference.Reference)' contains duplicate guardrail entry '$reportFileName' / '$benchmark'."
        }

        $guardrailEntryLookup[$guardrailKey] = $guardrailEntry
    }

    $allowedGuardrailCoverageStatuses = @(
        "guardrail-catalog-mapped",
        "pending-stable-baseline",
        "not-applicable"
    )

    $allowedBaselineStatuses = @(
        "pending-stable-baseline",
        "stable-baseline-published"
    )

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
                $guardrailCoverageStatus = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "guardrailCoverageStatus" -DefaultValue "")

                foreach ($field in @(
                    @{ Name = "id"; Value = $id },
                    @{ Name = "category"; Value = $category },
                    @{ Name = "measurementSurface"; Value = $measurementSurface },
                    @{ Name = "sourceDocument"; Value = $sourceDocumentPath },
                    @{ Name = "sloTarget"; Value = $sloTarget },
                    @{ Name = "window"; Value = $window },
                    @{ Name = "targetStatus"; Value = $targetStatus },
                    @{ Name = "baselineStatus"; Value = $baselineStatus },
                    @{ Name = "guardrailCoverageStatus"; Value = $guardrailCoverageStatus }
                )) {
                    if ([string]::IsNullOrWhiteSpace([string]$field.Value)) {
                        throw "SRE posture SLI entry must include $($field.Name)."
                    }
                }

                if ($allowedGuardrailCoverageStatuses -notcontains $guardrailCoverageStatus) {
                    throw "Unsupported SRE posture guardrail coverage status '$guardrailCoverageStatus' for SLI '$id'."
                }

                if ($allowedBaselineStatuses -notcontains $baselineStatus) {
                    throw "Unsupported SRE posture baseline status '$baselineStatus' for SLI '$id'."
                }

                $sourceDocumentReference = Resolve-SrePostureManifestPath -DeclaredPath $sourceDocumentPath -ResolvedRepoRoot $ResolvedRepoRoot -Context "SLI '$id' sourceDocument" -PathType "File"
                $sourceDocument = Get-Content -LiteralPath (Resolve-FullPath -Path $sourceDocumentPath -BasePath $ResolvedRepoRoot) -Raw -Encoding UTF8
                if (-not $sourceDocument.Contains($id, [System.StringComparison]::Ordinal)) {
                    throw "SRE posture source document '$($sourceDocumentReference.Reference)' does not contain SLI '$id'."
                }

                $guardrailReferences = @(
                    Get-ManifestPropertyValue -Object $_ -PropertyName "guardrailReferences" -DefaultValue @() |
                        ForEach-Object {
                            $referenceReportFileName = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "reportFileName" -DefaultValue "")
                            $referenceBenchmark = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "benchmark" -DefaultValue "")
                            if ([string]::IsNullOrWhiteSpace($referenceReportFileName) -or [string]::IsNullOrWhiteSpace($referenceBenchmark)) {
                                throw "SRE posture SLI '$id' guardrail reference must include reportFileName and benchmark."
                            }

                            $referenceKey = "$referenceReportFileName`n$referenceBenchmark"
                            if (-not $guardrailEntryLookup.ContainsKey($referenceKey)) {
                                throw "SRE posture SLI '$id' references guardrail '$referenceReportFileName' / '$referenceBenchmark', but that entry is missing from '$($guardrailCatalogReference.Reference)'."
                            }

                            [pscustomobject]([ordered]@{
                                ReportFileName = $referenceReportFileName
                                Benchmark      = $referenceBenchmark
                            })
                        }
                )

                if ($guardrailCoverageStatus -eq "guardrail-catalog-mapped" -and $guardrailReferences.Count -eq 0) {
                    throw "SRE posture SLI '$id' is guardrail-catalog-mapped but does not declare any guardrailReferences."
                }

                if ($guardrailCoverageStatus -ne "guardrail-catalog-mapped" -and $guardrailReferences.Count -gt 0) {
                    throw "SRE posture SLI '$id' declares guardrailReferences while guardrailCoverageStatus is '$guardrailCoverageStatus'."
                }

                [pscustomobject]([ordered]@{
                    Id                      = $id
                    Category                = $category
                    MeasurementSurface      = $measurementSurface
                    SourceDocument          = $sourceDocumentReference.Reference
                    SloTarget               = $sloTarget
                    Window                  = $window
                    TargetStatus            = $targetStatus
                    BaselineStatus          = $baselineStatus
                    GuardrailCoverageStatus = $guardrailCoverageStatus
                    GuardrailReferences     = $guardrailReferences
                })
            }
    )

    if ($sliRows.Count -eq 0) {
        throw "SRE posture support manifest must declare at least one SLI."
    }

    $targetDeclaredCount = @($sliRows | Where-Object { $_.TargetStatus -eq "target-declared" }).Count
    $pendingStableBaselineCount = @($sliRows | Where-Object { $_.BaselineStatus -eq "pending-stable-baseline" }).Count
    $stableBaselineCount = @($sliRows | Where-Object { $_.BaselineStatus -eq "stable-baseline-published" }).Count
    $guardrailMappedSliCount = @($sliRows | Where-Object { $_.GuardrailCoverageStatus -eq "guardrail-catalog-mapped" }).Count
    $guardrailPendingSliCount = @($sliRows | Where-Object { $_.GuardrailCoverageStatus -eq "pending-stable-baseline" }).Count
    $guardrailNotApplicableSliCount = @($sliRows | Where-Object { $_.GuardrailCoverageStatus -eq "not-applicable" }).Count
    $guardrailReferenceCount = @($sliRows | ForEach-Object { $_.GuardrailReferences }).Count
    $sliRowLookup = @{}
    foreach ($sliRow in $sliRows) {
        $sliRowLookup[$sliRow.Id] = $sliRow
    }

    if (-not $stableBaselinesPublishedValue -and $stableBaselineCount -gt 0) {
        throw "SRE posture support manifest has stable-baseline-published SLI rows while stableBaselinesPublished is false."
    }

    if ($stableBaselinesPublishedValue -and $stableBaselineCount -eq 0) {
        throw "SRE posture support manifest stableBaselinesPublished is true but no SLI has baselineStatus stable-baseline-published."
    }

    $stableBaselineManifestReference = $null
    $stableBaselineManifestSchemaVersion = $null
    $stableBaselineManifestStatus = $null
    $stableBaselineManifestCapturedAtUtc = $null
    $stableBaselineManifestCapturedFromCommit = $null
    $stableBaselineRows = @()
    $stableBaselineMeasurementCount = 0
    $stableBaselinePublishedSliIds = @()
    $pendingBaselineSliIds = @($sliRows | Where-Object { $_.BaselineStatus -eq "pending-stable-baseline" } | ForEach-Object { $_.Id })
    $pendingBaselineRows = @()
    $pendingBaselineBlockerCount = 0
    $pendingBaselineEvidenceCount = 0

    if (-not [string]::IsNullOrWhiteSpace($stableBaselineManifestPath)) {
        $stableBaselineManifestReference = Resolve-SrePostureManifestPath -DeclaredPath $stableBaselineManifestPath -ResolvedRepoRoot $ResolvedRepoRoot -Context "stableBaselineManifest" -PathType "File"
        $stableBaselineManifest = Get-Content -LiteralPath (Resolve-FullPath -Path $stableBaselineManifestPath -BasePath $ResolvedRepoRoot) -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
        $stableBaselineManifestSchemaVersion = [string](Get-ManifestPropertyValue -Object $stableBaselineManifest -PropertyName '$schemaVersion' -DefaultValue "")
        if ([string]::IsNullOrWhiteSpace($stableBaselineManifestSchemaVersion)) {
            throw "SRE stable baseline manifest is missing '`$schemaVersion'."
        }

        $stableBaselineManifestStatus = [string](Get-ManifestPropertyValue -Object $stableBaselineManifest -PropertyName "status" -DefaultValue "")
        if ([string]::IsNullOrWhiteSpace($stableBaselineManifestStatus)) {
            throw "SRE stable baseline manifest is missing status."
        }

        $stableBaselineManifestCapturedAtUtcValue = Get-ManifestPropertyValue -Object $stableBaselineManifest -PropertyName "capturedAtUtc" -DefaultValue ""
        $stableBaselineManifestCapturedAtUtc = if ($stableBaselineManifestCapturedAtUtcValue -is [datetime]) {
            $stableBaselineManifestCapturedAtUtcValue.ToUniversalTime().ToString("o")
        }
        else {
            [string]$stableBaselineManifestCapturedAtUtcValue
        }
        if ([string]::IsNullOrWhiteSpace($stableBaselineManifestCapturedAtUtc)) {
            throw "SRE stable baseline manifest is missing capturedAtUtc."
        }

        $stableBaselineManifestCapturedFromCommit = [string](Get-ManifestPropertyValue -Object $stableBaselineManifest -PropertyName "capturedFromCommit" -DefaultValue "")
        if ([string]::IsNullOrWhiteSpace($stableBaselineManifestCapturedFromCommit)) {
            throw "SRE stable baseline manifest is missing capturedFromCommit."
        }

        $stableBaselineManifestPublishedSliIds = @(
            Get-ManifestPropertyValue -Object $stableBaselineManifest -PropertyName "publishedBaselineSliIds" -DefaultValue @() |
                ForEach-Object { [string]$_ }
        )
        if ($stableBaselineManifestPublishedSliIds.Count -eq 0) {
            throw "SRE stable baseline manifest must declare at least one publishedBaselineSliIds entry."
        }

        $stableBaselineManifestPendingSliIds = @(
            Get-ManifestPropertyValue -Object $stableBaselineManifest -PropertyName "pendingBaselineSliIds" -DefaultValue @() |
                ForEach-Object { [string]$_ }
        )

        $stableBaselineRows = @(
            Get-ManifestPropertyValue -Object $stableBaselineManifest -PropertyName "baselineRows" -DefaultValue @() |
                ForEach-Object {
                    $sliId = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "sliId" -DefaultValue "")
                    $rowStatus = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "status" -DefaultValue "")
                    $measurementKind = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "measurementKind" -DefaultValue "")
                    $notes = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "notes" -DefaultValue "")

                    foreach ($field in @(
                        @{ Name = "sliId"; Value = $sliId },
                        @{ Name = "status"; Value = $rowStatus },
                        @{ Name = "measurementKind"; Value = $measurementKind }
                    )) {
                        if ([string]::IsNullOrWhiteSpace([string]$field.Value)) {
                            throw "SRE stable baseline row must include $($field.Name)."
                        }
                    }

                    if ($rowStatus -ne "stable-baseline-published") {
                        throw "SRE stable baseline row for SLI '$sliId' must use status stable-baseline-published."
                    }

                    if (-not $sliRowLookup.ContainsKey($sliId)) {
                        throw "SRE stable baseline row references SLI '$sliId', but that SLI is missing from the SRE posture support manifest."
                    }

                    if ($sliRowLookup[$sliId].BaselineStatus -ne "stable-baseline-published") {
                        throw "SRE stable baseline row references SLI '$sliId', but its SRE posture baselineStatus is '$($sliRowLookup[$sliId].BaselineStatus)'."
                    }

                    $measurements = @(
                        Get-ManifestPropertyValue -Object $_ -PropertyName "measurements" -DefaultValue @() |
                            ForEach-Object {
                                if ($measurementKind -eq "benchmark-mean-baseline-proxy" -or $measurementKind -eq "benchmark-allocation-baseline") {
                                    $reportFileName = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "reportFileName" -DefaultValue "")
                                    $benchmark = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "benchmark" -DefaultValue "")
                                    $meanNanosecondsValue = Get-ManifestPropertyValue -Object $_ -PropertyName "meanNanoseconds" -DefaultValue $null
                                    $errorNanosecondsValue = Get-ManifestPropertyValue -Object $_ -PropertyName "errorNanoseconds" -DefaultValue $null
                                    $stdDevNanosecondsValue = Get-ManifestPropertyValue -Object $_ -PropertyName "stdDevNanoseconds" -DefaultValue $null
                                    $allocatedBytesValue = Get-ManifestPropertyValue -Object $_ -PropertyName "allocatedBytes" -DefaultValue $null
                                    $guardrailMaxMeanNanosecondsValue = Get-ManifestPropertyValue -Object $_ -PropertyName "guardrailMaxMeanNanoseconds" -DefaultValue $null
                                    $guardrailMaxAllocatedBytesValue = Get-ManifestPropertyValue -Object $_ -PropertyName "guardrailMaxAllocatedBytes" -DefaultValue $null

                                    foreach ($field in @(
                                        @{ Name = "reportFileName"; Value = $reportFileName },
                                        @{ Name = "benchmark"; Value = $benchmark }
                                    )) {
                                        if ([string]::IsNullOrWhiteSpace([string]$field.Value)) {
                                            throw "SRE stable baseline measurement for SLI '$sliId' must include $($field.Name)."
                                        }
                                    }

                                    foreach ($field in @(
                                        @{ Name = "meanNanoseconds"; Value = $meanNanosecondsValue },
                                        @{ Name = "errorNanoseconds"; Value = $errorNanosecondsValue },
                                        @{ Name = "stdDevNanoseconds"; Value = $stdDevNanosecondsValue },
                                        @{ Name = "allocatedBytes"; Value = $allocatedBytesValue },
                                        @{ Name = "guardrailMaxMeanNanoseconds"; Value = $guardrailMaxMeanNanosecondsValue },
                                        @{ Name = "guardrailMaxAllocatedBytes"; Value = $guardrailMaxAllocatedBytesValue }
                                    )) {
                                        if ($null -eq $field.Value) {
                                            throw "SRE stable baseline measurement for SLI '$sliId' must include $($field.Name)."
                                        }
                                    }

                                    $guardrailKey = "$reportFileName`n$benchmark"
                                    if (-not $guardrailEntryLookup.ContainsKey($guardrailKey)) {
                                        throw "SRE stable baseline row for SLI '$sliId' references guardrail '$reportFileName' / '$benchmark', but that entry is missing from '$($guardrailCatalogReference.Reference)'."
                                    }

                                    $guardrailEntry = $guardrailEntryLookup[$guardrailKey]
                                    $catalogMaxMeanNanoseconds = [decimal](Get-ManifestPropertyValue -Object $guardrailEntry -PropertyName "maxMeanNanoseconds" -DefaultValue 0)
                                    $catalogMaxAllocatedBytes = [decimal](Get-ManifestPropertyValue -Object $guardrailEntry -PropertyName "maxAllocatedBytes" -DefaultValue 0)
                                    $declaredMaxMeanNanoseconds = [decimal]$guardrailMaxMeanNanosecondsValue
                                    $declaredMaxAllocatedBytes = [decimal]$guardrailMaxAllocatedBytesValue
                                    if ($declaredMaxMeanNanoseconds -ne $catalogMaxMeanNanoseconds -or $declaredMaxAllocatedBytes -ne $catalogMaxAllocatedBytes) {
                                        throw "SRE stable baseline row for SLI '$sliId' has guardrail limits that do not match '$($guardrailCatalogReference.Reference)' for '$reportFileName' / '$benchmark'."
                                    }

                                    [pscustomobject]([ordered]@{
                                        ReportFileName                 = $reportFileName
                                        Benchmark                      = $benchmark
                                        MeanNanoseconds                = [decimal]$meanNanosecondsValue
                                        ErrorNanoseconds               = [decimal]$errorNanosecondsValue
                                        StdDevNanoseconds              = [decimal]$stdDevNanosecondsValue
                                        AllocatedBytes                 = [decimal]$allocatedBytesValue
                                        GuardrailMaxMeanNanoseconds    = $declaredMaxMeanNanoseconds
                                        GuardrailMaxAllocatedBytes     = $declaredMaxAllocatedBytes
                                    })
                                }
                                elseif ($measurementKind -eq "deployment-mode-claims-report-baseline") {
                                    $claimsReportPath = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "claimsReportPath" -DefaultValue "")
                                    $deploymentMode = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "deploymentMode" -DefaultValue "")
                                    $aggregateVerdict = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "aggregateVerdict" -DefaultValue "")
                                    $publishProbeGateStatus = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "publishProbeGateStatus" -DefaultValue "")
                                    $publishProbeTargetCountValue = Get-ManifestPropertyValue -Object $_ -PropertyName "publishProbeTargetCount" -DefaultValue $null
                                    $publishProbeWarningCountValue = Get-ManifestPropertyValue -Object $_ -PropertyName "publishProbeWarningCount" -DefaultValue $null
                                    $publishProbeErrorCountValue = Get-ManifestPropertyValue -Object $_ -PropertyName "publishProbeErrorCount" -DefaultValue $null
                                    $packageClaimCountValue = Get-ManifestPropertyValue -Object $_ -PropertyName "packageClaimCount" -DefaultValue $null
                                    $packageClaimTruthfulCountValue = Get-ManifestPropertyValue -Object $_ -PropertyName "packageClaimTruthfulCount" -DefaultValue $null
                                    $packageClaimOverstatedCountValue = Get-ManifestPropertyValue -Object $_ -PropertyName "packageClaimOverstatedCount" -DefaultValue $null
                                    $truthfulFractionValue = Get-ManifestPropertyValue -Object $_ -PropertyName "truthfulFraction" -DefaultValue $null

                                    foreach ($field in @(
                                        @{ Name = "claimsReportPath"; Value = $claimsReportPath },
                                        @{ Name = "deploymentMode"; Value = $deploymentMode },
                                        @{ Name = "aggregateVerdict"; Value = $aggregateVerdict },
                                        @{ Name = "publishProbeGateStatus"; Value = $publishProbeGateStatus }
                                    )) {
                                        if ([string]::IsNullOrWhiteSpace([string]$field.Value)) {
                                            throw "SRE stable baseline deployment-mode claims measurement for SLI '$sliId' must include $($field.Name)."
                                        }
                                    }

                                    foreach ($field in @(
                                        @{ Name = "publishProbeTargetCount"; Value = $publishProbeTargetCountValue },
                                        @{ Name = "publishProbeWarningCount"; Value = $publishProbeWarningCountValue },
                                        @{ Name = "publishProbeErrorCount"; Value = $publishProbeErrorCountValue },
                                        @{ Name = "packageClaimCount"; Value = $packageClaimCountValue },
                                        @{ Name = "packageClaimTruthfulCount"; Value = $packageClaimTruthfulCountValue },
                                        @{ Name = "packageClaimOverstatedCount"; Value = $packageClaimOverstatedCountValue },
                                        @{ Name = "truthfulFraction"; Value = $truthfulFractionValue }
                                    )) {
                                        if ($null -eq $field.Value) {
                                            throw "SRE stable baseline deployment-mode claims measurement for SLI '$sliId' must include $($field.Name)."
                                        }
                                    }

                                    $packageClaimCount = [int]$packageClaimCountValue
                                    $packageClaimTruthfulCount = [int]$packageClaimTruthfulCountValue
                                    $packageClaimOverstatedCount = [int]$packageClaimOverstatedCountValue
                                    $truthfulFraction = [decimal]$truthfulFractionValue
                                    $expectedTruthfulFraction = if ($packageClaimCount -eq 0) { [decimal]1 } else { [decimal]$packageClaimTruthfulCount / [decimal]$packageClaimCount }
                                    if ([int]$publishProbeTargetCountValue -le 0 -or $publishProbeGateStatus -ne "passed" -or [int]$publishProbeWarningCountValue -ne 0 -or [int]$publishProbeErrorCountValue -ne 0 -or $packageClaimOverstatedCount -ne 0 -or $packageClaimTruthfulCount -ne $packageClaimCount -or $truthfulFraction -ne $expectedTruthfulFraction) {
                                        throw "SRE stable baseline deployment-mode claims measurement for SLI '$sliId' must represent a fully truthful passed claims-report baseline."
                                    }

                                    [pscustomobject]([ordered]@{
                                        ClaimsReportPath                 = $claimsReportPath
                                        DeploymentMode                   = $deploymentMode
                                        AggregateVerdict                 = $aggregateVerdict
                                        PublishProbeGateStatus           = $publishProbeGateStatus
                                        PublishProbeTargetCount          = [int]$publishProbeTargetCountValue
                                        PublishProbeWarningCount         = [int]$publishProbeWarningCountValue
                                        PublishProbeErrorCount           = [int]$publishProbeErrorCountValue
                                        PackageClaimCount                = $packageClaimCount
                                        PackageClaimTruthfulCount        = $packageClaimTruthfulCount
                                        PackageClaimOverstatedCount      = $packageClaimOverstatedCount
                                        TruthfulFraction                 = $truthfulFraction
                                    })
                                }
                                elseif ($measurementKind -eq "release-validation-step-wall-time-baseline") {
                                    $timingReportPath = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "timingReportPath" -DefaultValue "")
                                    $stepName = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "stepName" -DefaultValue "")
                                    $command = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "command" -DefaultValue "")
                                    $wallTimeStatus = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "status" -DefaultValue "")
                                    $elapsedMillisecondsValue = Get-ManifestPropertyValue -Object $_ -PropertyName "elapsedMilliseconds" -DefaultValue $null
                                    $targetMillisecondsValue = Get-ManifestPropertyValue -Object $_ -PropertyName "targetMilliseconds" -DefaultValue $null
                                    $capturedAtUtc = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "capturedAtUtc" -DefaultValue "")
                                    $capturedFromCommit = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "capturedFromCommit" -DefaultValue "")

                                    foreach ($field in @(
                                        @{ Name = "timingReportPath"; Value = $timingReportPath },
                                        @{ Name = "stepName"; Value = $stepName },
                                        @{ Name = "command"; Value = $command },
                                        @{ Name = "status"; Value = $wallTimeStatus },
                                        @{ Name = "capturedAtUtc"; Value = $capturedAtUtc },
                                        @{ Name = "capturedFromCommit"; Value = $capturedFromCommit }
                                    )) {
                                        if ([string]::IsNullOrWhiteSpace([string]$field.Value)) {
                                            throw "SRE stable baseline release-validation wall-time measurement for SLI '$sliId' must include $($field.Name)."
                                        }
                                    }

                                    foreach ($field in @(
                                        @{ Name = "elapsedMilliseconds"; Value = $elapsedMillisecondsValue },
                                        @{ Name = "targetMilliseconds"; Value = $targetMillisecondsValue }
                                    )) {
                                        if ($null -eq $field.Value) {
                                            throw "SRE stable baseline release-validation wall-time measurement for SLI '$sliId' must include $($field.Name)."
                                        }
                                    }

                                    $elapsedMilliseconds = [decimal]$elapsedMillisecondsValue
                                    $targetMilliseconds = [decimal]$targetMillisecondsValue
                                    if ($wallTimeStatus -ne "passed" -or $elapsedMilliseconds -le 0 -or $targetMilliseconds -le 0 -or $elapsedMilliseconds -gt $targetMilliseconds) {
                                        throw "SRE stable baseline release-validation wall-time measurement for SLI '$sliId' must be a passed timing below its target."
                                    }

                                    $resolvedTimingReportPath = Resolve-FullPath -Path $timingReportPath -BasePath $ResolvedRepoRoot
                                    if (Test-Path -LiteralPath $resolvedTimingReportPath -PathType Leaf) {
                                        $timingReport = Get-Content -LiteralPath $resolvedTimingReportPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 8
                                        $timingReportSliId = [string](Get-ManifestPropertyValue -Object $timingReport -PropertyName "sliId" -DefaultValue "")
                                        $timingReportStepName = [string](Get-ManifestPropertyValue -Object $timingReport -PropertyName "stepName" -DefaultValue "")
                                        $timingReportStatus = [string](Get-ManifestPropertyValue -Object $timingReport -PropertyName "status" -DefaultValue "")
                                        $timingReportElapsedMilliseconds = [decimal](Get-ManifestPropertyValue -Object $timingReport -PropertyName "elapsedMilliseconds" -DefaultValue 0)
                                        $timingReportTargetMilliseconds = [decimal](Get-ManifestPropertyValue -Object $timingReport -PropertyName "targetMilliseconds" -DefaultValue 0)
                                        if ($timingReportSliId -ne $sliId -or $timingReportStepName -ne $stepName -or $timingReportStatus -ne "passed" -or $timingReportElapsedMilliseconds -le 0 -or $timingReportTargetMilliseconds -ne $targetMilliseconds -or $timingReportElapsedMilliseconds -gt $targetMilliseconds) {
                                            throw "SRE stable baseline release-validation wall-time timing report '$timingReportPath' does not match a passed timing below target for SLI '$sliId'."
                                        }
                                    }

                                    [pscustomobject]([ordered]@{
                                        TimingReportPath    = $timingReportPath
                                        StepName            = $stepName
                                        Command             = $command
                                        Status              = $wallTimeStatus
                                        ElapsedMilliseconds = $elapsedMilliseconds
                                        TargetMilliseconds  = $targetMilliseconds
                                        CapturedAtUtc       = $capturedAtUtc
                                        CapturedFromCommit  = $capturedFromCommit
                                    })
                                }
                                else {
                                    throw "Unsupported SRE stable baseline measurement kind '$measurementKind' for SLI '$sliId'."
                                }
                            }
                    )

                    if ($measurements.Count -eq 0) {
                        throw "SRE stable baseline row for SLI '$sliId' must declare at least one measurement."
                    }

                    [pscustomobject]([ordered]@{
                        SliId           = $sliId
                        Status          = $rowStatus
                        MeasurementKind = $measurementKind
                        Notes           = $notes
                        Measurements    = $measurements
                    })
                }
        )

        $pendingBaselineRows = @(
            Get-ManifestPropertyValue -Object $stableBaselineManifest -PropertyName "pendingBaselineRows" -DefaultValue @() |
                ForEach-Object {
                    $sliId = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "sliId" -DefaultValue "")
                    $rowStatus = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "status" -DefaultValue "")
                    $blockerClass = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "blockerClass" -DefaultValue "")
                    $blockerSummary = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "blockerSummary" -DefaultValue "")
                    $nextEvidenceNeeded = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "nextEvidenceNeeded" -DefaultValue "")

                    foreach ($field in @(
                        @{ Name = "sliId"; Value = $sliId },
                        @{ Name = "status"; Value = $rowStatus },
                        @{ Name = "blockerClass"; Value = $blockerClass },
                        @{ Name = "blockerSummary"; Value = $blockerSummary },
                        @{ Name = "nextEvidenceNeeded"; Value = $nextEvidenceNeeded }
                    )) {
                        if ([string]::IsNullOrWhiteSpace([string]$field.Value)) {
                            throw "SRE pending baseline row must include $($field.Name)."
                        }
                    }

                    if ($rowStatus -ne "pending-stable-baseline") {
                        throw "SRE pending baseline row for SLI '$sliId' must use status pending-stable-baseline."
                    }

                    if (-not $sliRowLookup.ContainsKey($sliId)) {
                        throw "SRE pending baseline row references SLI '$sliId', but that SLI is missing from the SRE posture support manifest."
                    }

                    if ($sliRowLookup[$sliId].BaselineStatus -ne "pending-stable-baseline") {
                        throw "SRE pending baseline row references SLI '$sliId', but its SRE posture baselineStatus is '$($sliRowLookup[$sliId].BaselineStatus)'."
                    }

                    $evidenceRows = @(
                        Get-ManifestPropertyValue -Object $_ -PropertyName "evidence" -DefaultValue @() |
                            ForEach-Object {
                                $kind = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "kind" -DefaultValue "")
                                $promotionAllowedValue = Get-ManifestPropertyValue -Object $_ -PropertyName "promotionAllowed" -DefaultValue $null
                                if ([string]::IsNullOrWhiteSpace($kind)) {
                                    throw "SRE pending baseline evidence for SLI '$sliId' must include kind."
                                }

                                if ($promotionAllowedValue -isnot [bool]) {
                                    throw "SRE pending baseline evidence for SLI '$sliId' must include boolean promotionAllowed."
                                }

                                if ([bool]$promotionAllowedValue) {
                                    throw "SRE pending baseline evidence for SLI '$sliId' cannot set promotionAllowed to true."
                                }

                                $evidence = [ordered]@{
                                    Kind             = $kind
                                    PromotionAllowed = [bool]$promotionAllowedValue
                                }

                                foreach ($optionalField in @(
                                    @{ Source = "reportFileName"; Target = "ReportFileName" },
                                    @{ Source = "benchmark"; Target = "Benchmark" },
                                    @{ Source = "meanNanoseconds"; Target = "MeanNanoseconds" },
                                    @{ Source = "errorNanoseconds"; Target = "ErrorNanoseconds" },
                                    @{ Source = "stdDevNanoseconds"; Target = "StdDevNanoseconds" },
                                    @{ Source = "allocatedBytes"; Target = "AllocatedBytes" },
                                    @{ Source = "sloTargetNanoseconds"; Target = "SloTargetNanoseconds" },
                                    @{ Source = "targetMilliseconds"; Target = "TargetMilliseconds" },
                                    @{ Source = "guardrailMaxMeanNanoseconds"; Target = "GuardrailMaxMeanNanoseconds" },
                                    @{ Source = "guardrailMaxAllocatedBytes"; Target = "GuardrailMaxAllocatedBytes" },
                                    @{ Source = "provider"; Target = "Provider" },
                                    @{ Source = "repository"; Target = "Repository" },
                                    @{ Source = "query"; Target = "Query" },
                                    @{ Source = "evidenceScript"; Target = "EvidenceScript" },
                                    @{ Source = "reportPath"; Target = "ReportPath" },
                                    @{ Source = "requiredWindowDays"; Target = "RequiredWindowDays" },
                                    @{ Source = "minimumCompletedRunCount"; Target = "MinimumCompletedRunCount" },
                                    @{ Source = "availabilityStatus"; Target = "AvailabilityStatus" },
                                    @{ Source = "actionsReadinessStatus"; Target = "ActionsReadinessStatus" },
                                    @{ Source = "actionsEnabled"; Target = "ActionsEnabled" },
                                    @{ Source = "allowedActions"; Target = "AllowedActions" },
                                    @{ Source = "workflowReadinessStatus"; Target = "WorkflowReadinessStatus" },
                                    @{ Source = "matchingWorkflowCount"; Target = "MatchingWorkflowCount" },
                                    @{ Source = "activeWorkflowCount"; Target = "ActiveWorkflowCount" },
                                    @{ Source = "workflowDispatchReadinessStatus"; Target = "WorkflowDispatchReadinessStatus" },
                                    @{ Source = "dispatchConfiguredWorkflowCount"; Target = "DispatchConfiguredWorkflowCount" },
                                    @{ Source = "readinessBlockerClass"; Target = "ReadinessBlockerClass" },
                                    @{ Source = "totalRunCount"; Target = "TotalRunCount" },
                                    @{ Source = "targetFlakeRatePercent"; Target = "TargetFlakeRatePercent" }
                                )) {
                                    $optionalValue = Get-ManifestPropertyValue -Object $_ -PropertyName $optionalField.Source -DefaultValue $null
                                    if ($null -ne $optionalValue) {
                                        $evidence[$optionalField.Target] = $optionalValue
                                    }
                                }

                                [pscustomobject]$evidence
                            }
                    )

                    if ($evidenceRows.Count -eq 0) {
                        throw "SRE pending baseline row for SLI '$sliId' must declare at least one evidence item."
                    }

                    [pscustomobject]([ordered]@{
                        SliId              = $sliId
                        Status             = $rowStatus
                        BlockerClass       = $blockerClass
                        BlockerSummary     = $blockerSummary
                        NextEvidenceNeeded = $nextEvidenceNeeded
                        Evidence           = $evidenceRows
                    })
                }
        )

        if ($stableBaselineRows.Count -eq 0) {
            throw "SRE stable baseline manifest must declare at least one baselineRows entry."
        }

        $stableBaselinePublishedSliIds = @($stableBaselineRows | ForEach-Object { $_.SliId })
        $pendingBaselineRowSliIds = @($pendingBaselineRows | ForEach-Object { $_.SliId })
        $actualStableBaselineSliIds = @($sliRows | Where-Object { $_.BaselineStatus -eq "stable-baseline-published" } | ForEach-Object { $_.Id })
        Assert-SreStringSetEquals -Expected $actualStableBaselineSliIds -Actual $stableBaselinePublishedSliIds -Message "SRE stable baseline rows do not match SLI rows with baselineStatus stable-baseline-published."
        Assert-SreStringSetEquals -Expected $actualStableBaselineSliIds -Actual $stableBaselineManifestPublishedSliIds -Message "SRE stable baseline manifest publishedBaselineSliIds do not match SLI rows with baselineStatus stable-baseline-published."
        Assert-SreStringSetEquals -Expected $pendingBaselineSliIds -Actual $stableBaselineManifestPendingSliIds -Message "SRE stable baseline manifest pendingBaselineSliIds do not match SLI rows with baselineStatus pending-stable-baseline."
        Assert-SreStringSetEquals -Expected $pendingBaselineSliIds -Actual $pendingBaselineRowSliIds -Message "SRE stable baseline manifest pendingBaselineRows do not match SLI rows with baselineStatus pending-stable-baseline."
        $stableBaselineMeasurementCount = @($stableBaselineRows | ForEach-Object { $_.Measurements }).Count
        $pendingBaselineBlockerCount = @($pendingBaselineRows | ForEach-Object { $_.BlockerClass } | Sort-Object -Unique).Count
        $pendingBaselineEvidenceCount = @($pendingBaselineRows | ForEach-Object { $_.Evidence }).Count
    }
    elseif ($stableBaselinesPublishedValue -or $stableBaselineCount -gt 0) {
        throw "SRE posture support manifest stableBaselineManifest is required when stable baselines are published."
    }

    $stableBaselineManifestReferenceName = $null
    if ($null -ne $stableBaselineManifestReference) {
        $stableBaselineManifestReferenceName = $stableBaselineManifestReference.Reference
    }

    return [pscustomobject]([ordered]@{
        Manifest                   = Get-RepoRelativePath -Path $ResolvedManifestPath -RepoRoot $ResolvedRepoRoot
        ManifestSchemaVersion      = $schemaVersion
        Status                     = $status
        Summary                    = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "summary" -DefaultValue "")
        ReleaseValidationSummaryMode = $releaseValidationSummaryMode
        StableBaselinesPublished   = [bool]$stableBaselinesPublishedValue
        StableBaselineManifest     = $stableBaselineManifestReferenceName
        StableBaselineManifestSchemaVersion = $stableBaselineManifestSchemaVersion
        StableBaselineManifestStatus = $stableBaselineManifestStatus
        StableBaselineManifestCapturedAtUtc = $stableBaselineManifestCapturedAtUtc
        StableBaselineManifestCapturedFromCommit = $stableBaselineManifestCapturedFromCommit
        SourceDocuments            = @($sourceDocumentReferences | ForEach-Object { $_.Reference })
        ValidationScripts          = @($validationScriptReferences | ForEach-Object { $_.Reference })
        GuardrailCatalog           = $guardrailCatalogReference.Reference
        GuardrailCatalogEntryCount = $guardrailEntries.Count
        SliCount                   = $sliRows.Count
        TargetDeclaredCount        = $targetDeclaredCount
        PendingStableBaselineCount = $pendingStableBaselineCount
        StableBaselineCount        = $stableBaselineCount
        GuardrailMappedSliCount    = $guardrailMappedSliCount
        GuardrailPendingSliCount   = $guardrailPendingSliCount
        GuardrailNotApplicableSliCount = $guardrailNotApplicableSliCount
        GuardrailReferenceCount    = $guardrailReferenceCount
        StableBaselineRowCount     = $stableBaselineRows.Count
        StableBaselineMeasurementCount = $stableBaselineMeasurementCount
        PendingBaselineRowCount    = $pendingBaselineRows.Count
        PendingBaselineBlockerCount = $pendingBaselineBlockerCount
        PendingBaselineEvidenceCount = $pendingBaselineEvidenceCount
        StableBaselinePublishedSliIds = $stableBaselinePublishedSliIds
        PendingBaselineSliIds      = $pendingBaselineSliIds
        StableBaselineRows         = $stableBaselineRows
        PendingBaselineRows        = $pendingBaselineRows
        SliRows                    = $sliRows
        ValidatedReferences        = @(
            $sourceDocumentReferences
            $validationScriptReferences
            $guardrailCatalogReference
            if ($null -ne $stableBaselineManifestReference) {
                $stableBaselineManifestReference
            }
        ) | Sort-Object Reference -Unique
    })
}

function Convert-SupplyChainEvidence {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepoRoot
    )

    if (-not (Test-Path -LiteralPath $ResolvedManifestPath -PathType Leaf)) {
        throw "Supply-chain release support manifest '$ResolvedManifestPath' was not found."
    }

    $manifest = Get-Content -LiteralPath $ResolvedManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
    $schemaVersion = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName '$schemaVersion' -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($schemaVersion)) {
        throw "Supply-chain release support manifest is missing '`$schemaVersion'."
    }

    $status = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "status" -DefaultValue "")
    if ([string]::IsNullOrWhiteSpace($status)) {
        throw "Supply-chain release support manifest is missing status."
    }

    $releaseWorkflowPath = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "releaseWorkflow" -DefaultValue "")
    $releaseWorkflowReference = Resolve-SupplyChainManifestPath -DeclaredPath $releaseWorkflowPath -ResolvedRepoRoot $ResolvedRepoRoot -Context "releaseWorkflow" -PathType "File"
    $releaseWorkflow = Get-Content -LiteralPath (Resolve-FullPath -Path $releaseWorkflowPath -BasePath $ResolvedRepoRoot) -Raw -Encoding UTF8

    $sourceDocumentReferences = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "sourceDocs" -DefaultValue @() |
            ForEach-Object {
                Resolve-SupplyChainManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "sourceDocs" -PathType "File"
            }
    )
    if ($sourceDocumentReferences.Count -eq 0) {
        throw "Supply-chain release support manifest must declare at least one sourceDocs entry."
    }

    $validationScriptReferences = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "validationScripts" -DefaultValue @() |
            ForEach-Object {
                Resolve-SupplyChainManifestPath -DeclaredPath ([string]$_) -ResolvedRepoRoot $ResolvedRepoRoot -Context "validationScripts" -PathType "File"
            }
    )
    if ($validationScriptReferences.Count -eq 0) {
        throw "Supply-chain release support manifest must declare at least one validationScripts entry."
    }

    $requiredWorkflowTokens = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "requiredWorkflowTokens" -DefaultValue @() |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    foreach ($token in $requiredWorkflowTokens) {
        if (-not $releaseWorkflow.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Supply-chain release workflow '$releaseWorkflowPath' does not contain required token '$token'."
        }
    }

    $allowedEvidenceStatuses = @("workflow-ready", "external-policy-pending", "blocked")
    $evidenceItems = @(
        Get-ManifestPropertyValue -Object $manifest -PropertyName "evidenceItems" -DefaultValue @() |
            ForEach-Object {
                $id = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "id" -DefaultValue "")
                $category = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "category" -DefaultValue "")
                $itemStatus = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "status" -DefaultValue "")
                $summary = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "summary" -DefaultValue "")
                $sourceDocumentPath = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "sourceDocument" -DefaultValue "")
                $sourceToken = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "sourceToken" -DefaultValue "")

                foreach ($field in @(
                    @{ Name = "id"; Value = $id },
                    @{ Name = "category"; Value = $category },
                    @{ Name = "status"; Value = $itemStatus },
                    @{ Name = "summary"; Value = $summary },
                    @{ Name = "sourceDocument"; Value = $sourceDocumentPath },
                    @{ Name = "sourceToken"; Value = $sourceToken }
                )) {
                    if ([string]::IsNullOrWhiteSpace([string]$field.Value)) {
                        throw "Supply-chain release evidence item must include $($field.Name)."
                    }
                }

                if ($allowedEvidenceStatuses -notcontains $itemStatus) {
                    throw "Unsupported supply-chain release evidence status '$itemStatus' for item '$id'."
                }

                $sourceDocumentReference = Resolve-SupplyChainManifestPath -DeclaredPath $sourceDocumentPath -ResolvedRepoRoot $ResolvedRepoRoot -Context "evidence item '$id' sourceDocument" -PathType "File"
                $sourceDocument = Get-Content -LiteralPath (Resolve-FullPath -Path $sourceDocumentPath -BasePath $ResolvedRepoRoot) -Raw -Encoding UTF8
                if (-not $sourceDocument.Contains($sourceToken, [System.StringComparison]::OrdinalIgnoreCase)) {
                    throw "Supply-chain source document '$($sourceDocumentReference.Reference)' does not contain token '$sourceToken' for evidence item '$id'."
                }

                $workflowTokens = @(
                    Get-ManifestPropertyValue -Object $_ -PropertyName "workflowTokens" -DefaultValue @() |
                        ForEach-Object { [string]$_ } |
                        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
                )
                foreach ($workflowToken in $workflowTokens) {
                    if (-not $releaseWorkflow.Contains($workflowToken, [System.StringComparison]::OrdinalIgnoreCase)) {
                        throw "Supply-chain release workflow '$releaseWorkflowPath' does not contain workflow token '$workflowToken' for evidence item '$id'."
                    }
                }

                $externalPolicyRequired = ConvertTo-RequiredSupplyChainBoolean -Value (Get-ManifestPropertyValue -Object $_ -PropertyName "externalPolicyRequired" -DefaultValue $false) -Name "$id.externalPolicyRequired"

                [pscustomobject]([ordered]@{
                    Id                     = $id
                    Category               = $category
                    Status                 = $itemStatus
                    Summary                = $summary
                    SourceDocument         = $sourceDocumentReference.Reference
                    SourceToken            = $sourceToken
                    WorkflowTokens         = $workflowTokens
                    ExternalPolicyRequired = $externalPolicyRequired
                })
            }
    )

    if ($evidenceItems.Count -eq 0) {
        throw "Supply-chain release support manifest must declare at least one evidence item."
    }

    $workflowReadyCount = @($evidenceItems | Where-Object { $_.Status -eq "workflow-ready" }).Count
    $externalPolicyPendingCount = @($evidenceItems | Where-Object { $_.Status -eq "external-policy-pending" }).Count
    $blockedCount = @($evidenceItems | Where-Object { $_.Status -eq "blocked" }).Count
    $externalPolicyPreflight = $null

    if ($externalPolicyPendingCount -gt 0) {
        $preflightManifest = Get-ManifestPropertyValue -Object $manifest -PropertyName "externalPolicyPreflight" -DefaultValue $null
        if ($null -eq $preflightManifest) {
            throw "Supply-chain release support manifest must declare externalPolicyPreflight when external-policy-pending evidence items exist."
        }

        $preflightStatus = [string](Get-ManifestPropertyValue -Object $preflightManifest -PropertyName "status" -DefaultValue "")
        $preflightValidationScriptPath = [string](Get-ManifestPropertyValue -Object $preflightManifest -PropertyName "validationScript" -DefaultValue "")
        $preflightOutputPath = [string](Get-ManifestPropertyValue -Object $preflightManifest -PropertyName "outputPath" -DefaultValue "")
        $preflightSummary = [string](Get-ManifestPropertyValue -Object $preflightManifest -PropertyName "summary" -DefaultValue "")

        foreach ($field in @(
            @{ Name = "status"; Value = $preflightStatus },
            @{ Name = "validationScript"; Value = $preflightValidationScriptPath },
            @{ Name = "outputPath"; Value = $preflightOutputPath },
            @{ Name = "summary"; Value = $preflightSummary }
        )) {
            if ([string]::IsNullOrWhiteSpace([string]$field.Value)) {
                throw "Supply-chain externalPolicyPreflight must include $($field.Name)."
            }
        }

        $preflightValidationScriptReference = Resolve-SupplyChainManifestPath -DeclaredPath $preflightValidationScriptPath -ResolvedRepoRoot $ResolvedRepoRoot -Context "externalPolicyPreflight.validationScript" -PathType "File"
        if (@($validationScriptReferences | ForEach-Object { $_.Reference }) -notcontains $preflightValidationScriptReference.Reference) {
            throw "Supply-chain externalPolicyPreflight validationScript '$($preflightValidationScriptReference.Reference)' must also be listed in validationScripts."
        }

        $requiredChecks = @(
            Get-ManifestPropertyValue -Object $preflightManifest -PropertyName "requiredChecks" -DefaultValue @() |
                ForEach-Object {
                    $checkEvidenceItemId = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "evidenceItemId" -DefaultValue "")
                    $verificationMode = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "verificationMode" -DefaultValue "")
                    $requiredInput = [string](Get-ManifestPropertyValue -Object $_ -PropertyName "requiredInput" -DefaultValue "")
                    foreach ($field in @(
                        @{ Name = "evidenceItemId"; Value = $checkEvidenceItemId },
                        @{ Name = "verificationMode"; Value = $verificationMode },
                        @{ Name = "requiredInput"; Value = $requiredInput }
                    )) {
                        if ([string]::IsNullOrWhiteSpace([string]$field.Value)) {
                            throw "Supply-chain externalPolicyPreflight required check must include $($field.Name)."
                        }
                    }

                    [pscustomobject]([ordered]@{
                        EvidenceItemId   = $checkEvidenceItemId
                        VerificationMode = $verificationMode
                        RequiredInput    = $requiredInput
                    })
                }
        )
        if ($requiredChecks.Count -eq 0) {
            throw "Supply-chain externalPolicyPreflight must declare at least one requiredChecks entry."
        }

        $externalPolicyEvidenceIds = @($evidenceItems | Where-Object { $_.Status -eq "external-policy-pending" } | ForEach-Object { $_.Id } | Sort-Object -Unique)
        $requiredCheckEvidenceIds = @($requiredChecks | ForEach-Object { $_.EvidenceItemId } | Sort-Object -Unique)
        $missingPreflightChecks = @($externalPolicyEvidenceIds | Where-Object { $requiredCheckEvidenceIds -notcontains $_ })
        if ($missingPreflightChecks.Count -gt 0) {
            throw "Supply-chain externalPolicyPreflight is missing checks for evidence items: $($missingPreflightChecks -join ', ')."
        }

        $preflightWorkflowTokens = @(
            Get-ManifestPropertyValue -Object $preflightManifest -PropertyName "releaseWorkflowTokens" -DefaultValue @() |
                ForEach-Object { [string]$_ } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )
        if ($preflightWorkflowTokens.Count -eq 0) {
            throw "Supply-chain externalPolicyPreflight must declare releaseWorkflowTokens."
        }

        foreach ($preflightWorkflowToken in $preflightWorkflowTokens) {
            if (-not $releaseWorkflow.Contains($preflightWorkflowToken, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Supply-chain release workflow '$releaseWorkflowPath' does not contain external-policy preflight token '$preflightWorkflowToken'."
            }
        }

        $externalPolicyPreflight = [pscustomobject]([ordered]@{
            Status                = $preflightStatus
            ValidationScript      = $preflightValidationScriptReference.Reference
            OutputPath            = $preflightOutputPath
            Summary               = $preflightSummary
            RequiredCheckCount    = $requiredChecks.Count
            RequiredChecks        = $requiredChecks
            ReleaseWorkflowTokens = $preflightWorkflowTokens
        })
    }

    $signedReleaseDryRunManifest = Get-ManifestPropertyValue -Object $manifest -PropertyName "signedReleaseDryRun" -DefaultValue $null
    $signedReleaseDryRun = $null
    $validationScriptReferenceNames = @($validationScriptReferences | ForEach-Object { $_.Reference })
    if ($null -eq $signedReleaseDryRunManifest) {
        if ($validationScriptReferenceNames -contains "scripts/invoke-signed-release-dry-run.ps1") {
            throw "Supply-chain release support manifest must declare signedReleaseDryRun when scripts/invoke-signed-release-dry-run.ps1 is listed in validationScripts."
        }
    }
    else {
        $dryRunStatus = [string](Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "status" -DefaultValue "")
        $dryRunCurrentProofState = [string](Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "currentProofState" -DefaultValue "")
        $dryRunCurrentBlockerClass = [string](Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "currentBlockerClass" -DefaultValue "")
        $dryRunReadinessPolicy = [string](Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "readinessPolicy" -DefaultValue "")
        $dryRunValidationScriptPath = [string](Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "validationScript" -DefaultValue "")
        $dryRunOutputPath = [string](Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "outputPath" -DefaultValue "")
        $dryRunHandoffOutputPath = [string](Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "handoffOutputPath" -DefaultValue "")
        $dryRunRequiredCommand = [string](Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "requiredCommand" -DefaultValue "")
        $dryRunRequiredStatus = [string](Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "requiredStatus" -DefaultValue "")
        $dryRunSourceDocumentPath = [string](Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "sourceDocument" -DefaultValue "")
        $dryRunSourceToken = [string](Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "sourceToken" -DefaultValue "")
        $dryRunSummary = [string](Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "summary" -DefaultValue "")

        foreach ($field in @(
            @{ Name = "status"; Value = $dryRunStatus },
            @{ Name = "currentProofState"; Value = $dryRunCurrentProofState },
            @{ Name = "readinessPolicy"; Value = $dryRunReadinessPolicy },
            @{ Name = "validationScript"; Value = $dryRunValidationScriptPath },
            @{ Name = "outputPath"; Value = $dryRunOutputPath },
            @{ Name = "handoffOutputPath"; Value = $dryRunHandoffOutputPath },
            @{ Name = "requiredCommand"; Value = $dryRunRequiredCommand },
            @{ Name = "requiredStatus"; Value = $dryRunRequiredStatus },
            @{ Name = "sourceDocument"; Value = $dryRunSourceDocumentPath },
            @{ Name = "sourceToken"; Value = $dryRunSourceToken },
            @{ Name = "summary"; Value = $dryRunSummary }
        )) {
            if ([string]::IsNullOrWhiteSpace([string]$field.Value)) {
                throw "Supply-chain signedReleaseDryRun must include $($field.Name)."
            }
        }

        if (@("ready", "blocked", "submitted") -notcontains $dryRunStatus) {
            throw "Unsupported supply-chain signed-release dry-run status '$dryRunStatus'."
        }

        if ($dryRunStatus -eq "blocked" -and [string]::IsNullOrWhiteSpace($dryRunCurrentBlockerClass)) {
            throw "Supply-chain signedReleaseDryRun.currentBlockerClass is required when status is blocked."
        }

        $dryRunValidationScriptReference = Resolve-SupplyChainManifestPath -DeclaredPath $dryRunValidationScriptPath -ResolvedRepoRoot $ResolvedRepoRoot -Context "signedReleaseDryRun.validationScript" -PathType "File"
        if ($validationScriptReferenceNames -notcontains $dryRunValidationScriptReference.Reference) {
            throw "Supply-chain signedReleaseDryRun validationScript '$($dryRunValidationScriptReference.Reference)' must also be listed in validationScripts."
        }

        $dryRunSourceDocumentReference = Resolve-SupplyChainManifestPath -DeclaredPath $dryRunSourceDocumentPath -ResolvedRepoRoot $ResolvedRepoRoot -Context "signedReleaseDryRun.sourceDocument" -PathType "File"
        $dryRunSourceDocument = Get-Content -LiteralPath (Resolve-FullPath -Path $dryRunSourceDocumentPath -BasePath $ResolvedRepoRoot) -Raw -Encoding UTF8
        if (-not $dryRunSourceDocument.Contains($dryRunSourceToken, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Supply-chain signedReleaseDryRun source document '$($dryRunSourceDocumentReference.Reference)' does not contain token '$dryRunSourceToken'."
        }

        $dryRunRequiredReportFields = @(
            Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "requiredReportFields" -DefaultValue @() |
                ForEach-Object { [string]$_ } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )
        if ($dryRunRequiredReportFields.Count -eq 0) {
            throw "Supply-chain signedReleaseDryRun must declare requiredReportFields."
        }

        $signedReleaseDryRun = [pscustomobject]([ordered]@{
            Status                   = $dryRunStatus
            CurrentProofState        = $dryRunCurrentProofState
            CurrentBlockerClass      = $dryRunCurrentBlockerClass
            ReadinessPolicy          = $dryRunReadinessPolicy
            ValidationScript         = $dryRunValidationScriptReference.Reference
            OutputPath               = $dryRunOutputPath
            HandoffOutputPath        = $dryRunHandoffOutputPath
            RequiredCommand          = $dryRunRequiredCommand
            RequiredStatus           = $dryRunRequiredStatus
            RequiredRunCreated       = ConvertTo-RequiredSupplyChainBoolean -Value (Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "requiredRunCreated" -DefaultValue $true) -Name "signedReleaseDryRun.requiredRunCreated"
            RequiredRunUrl           = ConvertTo-RequiredSupplyChainBoolean -Value (Get-ManifestPropertyValue -Object $signedReleaseDryRunManifest -PropertyName "requiredRunUrl" -DefaultValue $true) -Name "signedReleaseDryRun.requiredRunUrl"
            SourceDocument           = $dryRunSourceDocumentReference.Reference
            SourceToken              = $dryRunSourceToken
            Summary                  = $dryRunSummary
            RequiredReportFields     = $dryRunRequiredReportFields
            RequiredReportFieldCount = $dryRunRequiredReportFields.Count
        })
    }

    return [pscustomobject]([ordered]@{
        Manifest                   = Get-RepoRelativePath -Path $ResolvedManifestPath -RepoRoot $ResolvedRepoRoot
        ManifestSchemaVersion      = $schemaVersion
        Status                     = $status
        Summary                    = [string](Get-ManifestPropertyValue -Object $manifest -PropertyName "summary" -DefaultValue "")
        ReleaseWorkflow            = $releaseWorkflowReference.Reference
        SourceDocuments            = @($sourceDocumentReferences | ForEach-Object { $_.Reference })
        ValidationScripts          = @($validationScriptReferences | ForEach-Object { $_.Reference })
        RequiredWorkflowTokens     = $requiredWorkflowTokens
        EvidenceItemCount          = $evidenceItems.Count
        WorkflowReadyCount         = $workflowReadyCount
        ExternalPolicyPendingCount = $externalPolicyPendingCount
        ExternalPolicyPreflight    = $externalPolicyPreflight
        ExternalPolicyPreflightCheckCount = if ($null -eq $externalPolicyPreflight) { 0 } else { $externalPolicyPreflight.RequiredCheckCount }
        SignedReleaseDryRun        = $signedReleaseDryRun
        SignedReleaseDryRunStatus  = if ($null -eq $signedReleaseDryRun) { "not-declared" } else { $signedReleaseDryRun.Status }
        SignedReleaseDryRunBlockerClass = if ($null -eq $signedReleaseDryRun) { "" } else { $signedReleaseDryRun.CurrentBlockerClass }
        BlockedCount               = $blockedCount
        EvidenceItems              = $evidenceItems
        ValidatedReferences        = @(
            $releaseWorkflowReference
            $sourceDocumentReferences
            $validationScriptReferences
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
        [string]$ResolvedDeploymentModeManifestPath,
        [AllowNull()]
        [string]$ResolvedDeploymentModeClaimsReportPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedAdoptionSmokeManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedProviderIntegrationManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedEventingOperationalSuperiorityManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedSrePostureManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedSupplyChainManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedTestCoverageRoadmapPath,
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
    $deploymentModeEvidence = Convert-DeploymentModeEvidence -ResolvedManifestPath $ResolvedDeploymentModeManifestPath -ResolvedClaimsReportPath $ResolvedDeploymentModeClaimsReportPath -ResolvedRepoRoot $ResolvedRepoRoot
    $adoptionSmokeEvidence = Convert-AdoptionSmokeEvidence -ResolvedManifestPath $ResolvedAdoptionSmokeManifestPath -ResolvedRepoRoot $ResolvedRepoRoot
    $providerIntegrationEvidence = Convert-ProviderIntegrationEvidence -ResolvedManifestPath $ResolvedProviderIntegrationManifestPath -ResolvedRepoRoot $ResolvedRepoRoot
    $eventingOperationalSuperiorityEvidence = Convert-EventingOperationalSuperiorityEvidence -ResolvedManifestPath $ResolvedEventingOperationalSuperiorityManifestPath -ResolvedRepoRoot $ResolvedRepoRoot
    $srePostureEvidence = Convert-SrePostureEvidence -ResolvedManifestPath $ResolvedSrePostureManifestPath -ResolvedRepoRoot $ResolvedRepoRoot
    $supplyChainEvidence = Convert-SupplyChainEvidence -ResolvedManifestPath $ResolvedSupplyChainManifestPath -ResolvedRepoRoot $ResolvedRepoRoot
    $testCoverageEvidence = Convert-TestCoverageEvidence -ResolvedRoadmapPath $ResolvedTestCoverageRoadmapPath -ResolvedRepoRoot $ResolvedRepoRoot
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
        DeploymentModeManifest = Get-RepoRelativePath -Path $ResolvedDeploymentModeManifestPath -RepoRoot $ResolvedRepoRoot
        DeploymentModeClaimsReport = if ([string]::IsNullOrWhiteSpace($ResolvedDeploymentModeClaimsReportPath)) { "" } else { Get-RepoRelativePath -Path $ResolvedDeploymentModeClaimsReportPath -RepoRoot $ResolvedRepoRoot }
        AdoptionSmokeManifest = Get-RepoRelativePath -Path $ResolvedAdoptionSmokeManifestPath -RepoRoot $ResolvedRepoRoot
        ProviderIntegrationManifest = Get-RepoRelativePath -Path $ResolvedProviderIntegrationManifestPath -RepoRoot $ResolvedRepoRoot
        EventingOperationalSuperiorityManifest = Get-RepoRelativePath -Path $ResolvedEventingOperationalSuperiorityManifestPath -RepoRoot $ResolvedRepoRoot
        SrePostureManifest = Get-RepoRelativePath -Path $ResolvedSrePostureManifestPath -RepoRoot $ResolvedRepoRoot
        SupplyChainManifest = Get-RepoRelativePath -Path $ResolvedSupplyChainManifestPath -RepoRoot $ResolvedRepoRoot
        TestCoverageRoadmap = Get-RepoRelativePath -Path $ResolvedTestCoverageRoadmapPath -RepoRoot $ResolvedRepoRoot
        PublicApiDeltaScript = Get-RepoRelativePath -Path $ResolvedPublicApiDeltaScriptPath -RepoRoot $ResolvedRepoRoot
        StatusVocabulary   = $statusVocabulary
        EvidenceSources    = $evidenceSources
        EvidenceSourceReferences = $evidenceSourceReferences
        PlatformGates      = $platformGates
        QualityDimensions  = $qualityDimensions
        PackageFamilies    = $packageFamilies
        PackageGAReadiness = $packageGAReadinessRows
        DeploymentModeEvidence = $deploymentModeEvidence
        AdoptionSmokeEvidence = $adoptionSmokeEvidence
        ProviderIntegrationEvidence = $providerIntegrationEvidence
        EventingOperationalSuperiorityEvidence = $eventingOperationalSuperiorityEvidence
        SrePostureEvidence = $srePostureEvidence
        SupplyChainEvidence = $supplyChainEvidence
        TestCoverageEvidence = $testCoverageEvidence
        PublicApiCompatibilityEvidence = $publicApiCompatibilityEvidence
        PromotionRules     = $promotionRules
        RefreshCadence     = $refreshCadence
        Summary            = [pscustomobject]([ordered]@{
            PlatformGateCount      = $platformGates.Count
            QualityDimensionCount  = $qualityDimensions.Count
            PackageFamilyCount     = $packageFamilies.Count
            PackageGAReadinessCount = $packageGAReadinessRows.Count
            DeploymentModeGlobalClaimCount = $deploymentModeEvidence.GlobalClaimCount
            DeploymentModeGlobalNotClaimedCount = $deploymentModeEvidence.GlobalNotClaimedCount
            DeploymentModePackageEntryCount = $deploymentModeEvidence.PackageEntryCount
            DeploymentModePackageScopedClaimPackageCount = $deploymentModeEvidence.PackageScopedClaimPackageCount
            DeploymentModePackageScopedClaimCount = $deploymentModeEvidence.PackageScopedClaimCount
            DeploymentModeKnownHazardPackageCount = $deploymentModeEvidence.KnownHazardPackageCount
            DeploymentModeKnownHazardEntryCount = $deploymentModeEvidence.KnownHazardEntryCount
            DeploymentModeTransitiveAuditEntryCount = $deploymentModeEvidence.TransitiveAuditEntryCount
            DeploymentModeRepresentativePublishTargetCount = $deploymentModeEvidence.RepresentativePublishTargetCount
            DeploymentModeClaimsReportPresent = $deploymentModeEvidence.ClaimsReportPresent
            DeploymentModeClaimsReportPublishProbeTargetCount = $deploymentModeEvidence.ClaimsReportPublishProbeTargetCount
            DeploymentModeClaimsReportPublishProbeWarningCount = $deploymentModeEvidence.ClaimsReportPublishProbeWarningCount
            DeploymentModeClaimsReportPublishProbeErrorCount = $deploymentModeEvidence.ClaimsReportPublishProbeErrorCount
            DeploymentModeClaimsReportPackageClaimTruthfulCount = $deploymentModeEvidence.ClaimsReportPackageClaimTruthfulCount
            DeploymentModeClaimsReportPackageClaimOverstatedCount = $deploymentModeEvidence.ClaimsReportPackageClaimOverstatedCount
            DeploymentModeClaimsReportBoundaryAnnotationAuditFailures = $deploymentModeEvidence.ClaimsReportHazardInventoryBoundaryAnnotationAuditFailureCount
            DeploymentModeClaimsReportCoreRouteDelegateAuditFailures = $deploymentModeEvidence.ClaimsReportHazardInventoryCoreRouteDelegateAuditFailureCount
            DeploymentModeClaimsReportFullCommonRouteDelegateAuditFailures = $deploymentModeEvidence.ClaimsReportHazardInventoryFullCommonRouteDelegateAuditFailureCount
            DeploymentModeClaimsReportFullOperatorRouteDelegateAuditFailures = $deploymentModeEvidence.ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditFailureCount
            DeploymentModeClaimsReportOperatorResponseJsonContractAuditFailures = $deploymentModeEvidence.ClaimsReportHazardInventoryOperatorResponseJsonContractAuditFailureCount
            DeploymentModeClaimsReportNonOperatorEndpointAuditFailures = $deploymentModeEvidence.ClaimsReportHazardInventoryNonOperatorEndpointAuditFailureCount
            DeploymentModeClaimsReportFrameworkEndpointBoundaryAuditFailures = $deploymentModeEvidence.ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditFailureCount
            AdoptionSmokeScenarioCount = if ($null -ne $adoptionSmokeEvidence) { 1 } else { 0 }
            AdoptionSmokeRuntimeProbeCount = @($adoptionSmokeEvidence.RuntimeProbes).Count
            AdoptionSmokeAssertionCount = @($adoptionSmokeEvidence.Assertions).Count
            AdoptionSmokeExecutionReportRequiredFieldCount = @($adoptionSmokeEvidence.ExecutionReport.RequiredFields).Count
            ProviderIntegrationEvidenceRowCount = $providerIntegrationEvidence.EvidenceRowCount
            ProviderIntegrationLiveProofCount = $providerIntegrationEvidence.LiveProofCount
            ProviderIntegrationCompositionOnlyCount = $providerIntegrationEvidence.CompositionOnlyCount
            ProviderIntegrationExternalServiceGateCount = $providerIntegrationEvidence.ExternalServiceGateCount
            ProviderIntegrationDefaultSkippedCount = $providerIntegrationEvidence.DefaultSkippedCount
            ProviderIntegrationRuntimeContractCount = $providerIntegrationEvidence.RuntimeContractCount
            EventingOperationalSuperiorityRequiredDimensionCount = $eventingOperationalSuperiorityEvidence.RequiredDimensionCount
            EventingOperationalSuperiorityCoveredDimensionCount = $eventingOperationalSuperiorityEvidence.CoveredDimensionCount
            EventingOperationalSuperiorityPartialDimensionCount = $eventingOperationalSuperiorityEvidence.PartialDimensionCount
            EventingOperationalSuperiorityMissingDimensionCount = $eventingOperationalSuperiorityEvidence.MissingDimensionCount
            EventingOperationalSuperiorityCoveragePercent = $eventingOperationalSuperiorityEvidence.CoveragePercent
            EventingOperationalSuperiorityPromotionAllowed = $eventingOperationalSuperiorityEvidence.PromotionAllowed
            EventingOperationalSuperiorityWolverineRequired = $eventingOperationalSuperiorityEvidence.WolverineRequired
            EventingOperationalSuperiorityRuntimeConcordanceMatched = $eventingOperationalSuperiorityEvidence.RuntimeConcordanceStatus -eq "matched" -and $eventingOperationalSuperiorityEvidence.RuntimeConcordanceMissingTokenCount -eq 0
            EventingOperationalSuperiorityRuntimeConcordanceTokenCount = $eventingOperationalSuperiorityEvidence.RuntimeConcordanceTokenCount
            EventingOperationalSuperiorityRuntimeConcordanceMissingTokenCount = $eventingOperationalSuperiorityEvidence.RuntimeConcordanceMissingTokenCount
            SreSliCount = $srePostureEvidence.SliCount
            SreTargetDeclaredCount = $srePostureEvidence.TargetDeclaredCount
            SrePendingStableBaselineCount = $srePostureEvidence.PendingStableBaselineCount
            SreStableBaselineCount = $srePostureEvidence.StableBaselineCount
            SreGuardrailMappedSliCount = $srePostureEvidence.GuardrailMappedSliCount
            SreGuardrailPendingSliCount = $srePostureEvidence.GuardrailPendingSliCount
            SreGuardrailNotApplicableSliCount = $srePostureEvidence.GuardrailNotApplicableSliCount
            SreGuardrailReferenceCount = $srePostureEvidence.GuardrailReferenceCount
            SrePendingBaselineRowCount = $srePostureEvidence.PendingBaselineRowCount
            SrePendingBaselineBlockerCount = $srePostureEvidence.PendingBaselineBlockerCount
            SrePendingBaselineEvidenceCount = $srePostureEvidence.PendingBaselineEvidenceCount
            SupplyChainEvidenceItemCount = $supplyChainEvidence.EvidenceItemCount
            SupplyChainWorkflowReadyCount = $supplyChainEvidence.WorkflowReadyCount
            SupplyChainExternalPolicyPendingCount = $supplyChainEvidence.ExternalPolicyPendingCount
            SupplyChainExternalPolicyPreflightCheckCount = $supplyChainEvidence.ExternalPolicyPreflightCheckCount
            SupplyChainSignedReleaseDryRunStatus = $supplyChainEvidence.SignedReleaseDryRunStatus
            SupplyChainSignedReleaseDryRunBlockerClass = $supplyChainEvidence.SignedReleaseDryRunBlockerClass
            SupplyChainBlockedCount = $supplyChainEvidence.BlockedCount
            TestCoverageLayeredProjectCount = $testCoverageEvidence.LayeredProjectCount
            TestCoverageGapCriterionCount = $testCoverageEvidence.GapDefinitionCriterionCount
            TestCoverageRecommendationCount = $testCoverageEvidence.RecommendationCount
            TestCoverageShippedRecommendationCount = $testCoverageEvidence.ShippedRecommendationCount
            TestCoverageGatedRecommendationCount = $testCoverageEvidence.GatedRecommendationCount
            TestCoverageActiveGapRecommendationCount = $testCoverageEvidence.ActiveGapRecommendationCount
            TestCoverageQuarantineEntryCount = $testCoverageEvidence.QuarantineEntryCount
            TestCoverageOpenQuarantineEntryCount = $testCoverageEvidence.OpenQuarantineEntryCount
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
    $markdown.Add("Deployment-mode manifest: ``$($Report.DeploymentModeManifest)``")
    $markdown.Add("Adoption smoke manifest: ``$($Report.AdoptionSmokeManifest)``")
    $markdown.Add("Provider integration manifest: ``$($Report.ProviderIntegrationManifest)``")
    $markdown.Add("Eventing operational-superiority manifest: ``$($Report.EventingOperationalSuperiorityManifest)``")
    $markdown.Add("SRE posture manifest: ``$($Report.SrePostureManifest)``")
    $markdown.Add("Supply-chain release manifest: ``$($Report.SupplyChainManifest)``")
    $markdown.Add("Test coverage roadmap: ``$($Report.TestCoverageRoadmap)``")
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
    $markdown.Add("- Deployment-mode global claims: $($Report.Summary.DeploymentModeGlobalClaimCount)")
    $markdown.Add("- Deployment-mode global not-claimed rows: $($Report.Summary.DeploymentModeGlobalNotClaimedCount)")
    $markdown.Add("- Deployment-mode package-scoped claim packages: $($Report.Summary.DeploymentModePackageScopedClaimPackageCount)")
    $markdown.Add("- Deployment-mode known hazards: $($Report.Summary.DeploymentModeKnownHazardEntryCount)")
    $markdown.Add("- Deployment-mode transitive audit entries: $($Report.Summary.DeploymentModeTransitiveAuditEntryCount)")
    $markdown.Add("- Deployment-mode representative publish targets: $($Report.Summary.DeploymentModeRepresentativePublishTargetCount)")
    $markdown.Add("- Deployment-mode publish probes: $($Report.DeploymentModeEvidence.PublishProbeReleaseValidationMode)")
    $markdown.Add("- Adoption smoke scenarios: $($Report.Summary.AdoptionSmokeScenarioCount)")
    $markdown.Add("- Adoption smoke runtime probes: $($Report.Summary.AdoptionSmokeRuntimeProbeCount)")
    $markdown.Add("- Adoption smoke execution-report fields: $($Report.Summary.AdoptionSmokeExecutionReportRequiredFieldCount)")
    $markdown.Add("- Provider integration evidence rows: $($Report.Summary.ProviderIntegrationEvidenceRowCount)")
    $markdown.Add("- Provider integration live proofs: $($Report.Summary.ProviderIntegrationLiveProofCount)")
    $markdown.Add("- Provider integration composition-only rows: $($Report.Summary.ProviderIntegrationCompositionOnlyCount)")
    $markdown.Add("- Provider integration external-service gates: $($Report.Summary.ProviderIntegrationExternalServiceGateCount)")
    $markdown.Add("- Provider integration default-skipped rows: $($Report.Summary.ProviderIntegrationDefaultSkippedCount)")
    $markdown.Add("- Provider integration runtime contracts: $($Report.Summary.ProviderIntegrationRuntimeContractCount)")
    $markdown.Add("- Eventing operational-superiority dimensions: $($Report.Summary.EventingOperationalSuperiorityCoveredDimensionCount)/$($Report.Summary.EventingOperationalSuperiorityRequiredDimensionCount) covered, partial $($Report.Summary.EventingOperationalSuperiorityPartialDimensionCount), missing $($Report.Summary.EventingOperationalSuperiorityMissingDimensionCount), coverage $($Report.Summary.EventingOperationalSuperiorityCoveragePercent)%, promotion allowed $($Report.Summary.EventingOperationalSuperiorityPromotionAllowed), Wolverine required $($Report.Summary.EventingOperationalSuperiorityWolverineRequired), runtime concordance matched $($Report.Summary.EventingOperationalSuperiorityRuntimeConcordanceMatched)")
    $markdown.Add("- SRE SLIs: $($Report.Summary.SreSliCount)")
    $markdown.Add("- SRE target-declared SLIs: $($Report.Summary.SreTargetDeclaredCount)")
    $markdown.Add("- SRE pending stable baselines: $($Report.Summary.SrePendingStableBaselineCount)")
    $markdown.Add("- SRE stable baselines: $($Report.Summary.SreStableBaselineCount)")
    $markdown.Add("- SRE stable baseline rows: $($Report.SrePostureEvidence.StableBaselineRowCount)")
    $markdown.Add("- SRE stable baseline measurements: $($Report.SrePostureEvidence.StableBaselineMeasurementCount)")
    $markdown.Add("- SRE guardrail-mapped SLIs: $($Report.Summary.SreGuardrailMappedSliCount)")
    $markdown.Add("- SRE pending guardrail coverage SLIs: $($Report.Summary.SreGuardrailPendingSliCount)")
    $markdown.Add("- Supply-chain evidence items: $($Report.Summary.SupplyChainEvidenceItemCount)")
    $markdown.Add("- Supply-chain workflow-ready items: $($Report.Summary.SupplyChainWorkflowReadyCount)")
    $markdown.Add("- Supply-chain external-policy-pending items: $($Report.Summary.SupplyChainExternalPolicyPendingCount)")
    $markdown.Add("- Supply-chain external-policy preflight checks: $($Report.Summary.SupplyChainExternalPolicyPreflightCheckCount)")
    $markdown.Add("- Supply-chain blocked items: $($Report.Summary.SupplyChainBlockedCount)")
    $markdown.Add("- Test coverage layered projects: $($Report.Summary.TestCoverageLayeredProjectCount)")
    $markdown.Add("- Test coverage gap criteria: $($Report.Summary.TestCoverageGapCriterionCount)")
    $markdown.Add("- Test coverage recommendations: $($Report.Summary.TestCoverageRecommendationCount)")
    $markdown.Add("- Test coverage shipped recommendations: $($Report.Summary.TestCoverageShippedRecommendationCount)")
    $markdown.Add("- Test coverage gated recommendations: $($Report.Summary.TestCoverageGatedRecommendationCount)")
    $markdown.Add("- Test coverage active gap recommendations: $($Report.Summary.TestCoverageActiveGapRecommendationCount)")
    $markdown.Add("- Test coverage quarantine entries: $($Report.Summary.TestCoverageQuarantineEntryCount)")
    $markdown.Add("- Test coverage open quarantine entries: $($Report.Summary.TestCoverageOpenQuarantineEntryCount)")
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
    $markdown.Add("## Deployment-Mode Evidence")
    $markdown.Add("")
    $markdown.Add("- Manifest: ``$($Report.DeploymentModeEvidence.Manifest)``")
    $markdown.Add("- Stable target framework: $($Report.DeploymentModeEvidence.ShippingStableTargetFramework)")
    $markdown.Add("- Readiness lane target framework: $($Report.DeploymentModeEvidence.ReadinessLaneTargetFramework) ($($Report.DeploymentModeEvidence.ReadinessLaneStatus))")
    $markdown.Add("- Package entries: $($Report.DeploymentModeEvidence.PackageEntryCount)")
    $markdown.Add("- Package-scoped claim packages: $($Report.DeploymentModeEvidence.PackageScopedClaimPackageCount)")
    $markdown.Add("- Known hazard packages: $($Report.DeploymentModeEvidence.KnownHazardPackageCount)")
    $markdown.Add("- Known hazard entries: $($Report.DeploymentModeEvidence.KnownHazardEntryCount)")
    $markdown.Add("- Transitive audit entries: $($Report.DeploymentModeEvidence.TransitiveAuditEntryCount)")
    $markdown.Add("- Publish-probe release validation mode: $($Report.DeploymentModeEvidence.PublishProbeReleaseValidationMode)")
    $markdown.Add("- Publish-probe release validation deployment modes: $([string]::Join(', ', @($Report.DeploymentModeEvidence.PublishProbeReleaseValidationDeploymentModes)))")
    $markdown.Add("- Publish-probe skips publish in release validation: $($Report.DeploymentModeEvidence.PublishProbeReleaseValidationSkipsPublish)")
    $markdown.Add("- Publish-probe non-opt-out gate: $($Report.DeploymentModeEvidence.PublishProbeNonOptOutGate)")
    $markdown.Add("- Publish-probe gated modes: $([string]::Join(', ', @($Report.DeploymentModeEvidence.PublishProbeGatedModes)))")
    $markdown.Add("- Publish-probe audit-only modes: $([string]::Join(', ', @($Report.DeploymentModeEvidence.PublishProbeAuditOnlyModes)))")
    $markdown.Add("- Publish-probe failure blocks release: $($Report.DeploymentModeEvidence.PublishProbeFailureBlocksRelease)")
    $markdown.Add("- Publish-probe fails on warnings: $($Report.DeploymentModeEvidence.PublishProbeFailOnWarnings)")
    $markdown.Add("- Claims report: ``$($Report.DeploymentModeEvidence.ClaimsReport)`` (present: $($Report.DeploymentModeEvidence.ClaimsReportPresent))")
    $markdown.Add("- Claims-report gate: $($Report.DeploymentModeEvidence.ClaimsReportPublishProbeGateStatus); targets $($Report.DeploymentModeEvidence.ClaimsReportPublishProbeTargetCount); warnings $($Report.DeploymentModeEvidence.ClaimsReportPublishProbeWarningCount); errors $($Report.DeploymentModeEvidence.ClaimsReportPublishProbeErrorCount)")
    $markdown.Add("- Claims-report package claims: truthful $($Report.DeploymentModeEvidence.ClaimsReportPackageClaimTruthfulCount); overstated $($Report.DeploymentModeEvidence.ClaimsReportPackageClaimOverstatedCount)")
    $markdown.Add("- Claims-report boundary annotation audit: $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryBoundaryAnnotationAuditStatus); failures $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryBoundaryAnnotationAuditFailureCount)")
    $markdown.Add("- Claims-report core route-delegate audit: $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryCoreRouteDelegateAuditStatus); failures $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryCoreRouteDelegateAuditFailureCount)")
    $markdown.Add("- Claims-report full common route-delegate audit: $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryFullCommonRouteDelegateAuditStatus); failures $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryFullCommonRouteDelegateAuditFailureCount)")
    $markdown.Add("- Claims-report full operator route-delegate audit: $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditStatus); failures $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryFullOperatorRouteDelegateAuditFailureCount)")
    $markdown.Add("- Claims-report operator response JSON contract audit: $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryOperatorResponseJsonContractAuditStatus); failures $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryOperatorResponseJsonContractAuditFailureCount)")
    $markdown.Add("- Claims-report non-operator host/documentation endpoint audit: $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryNonOperatorEndpointAuditStatus); failures $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryNonOperatorEndpointAuditFailureCount)")
    $markdown.Add("- Claims-report framework endpoint boundary audit: $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditStatus); failures $($Report.DeploymentModeEvidence.ClaimsReportHazardInventoryFrameworkEndpointBoundaryAuditFailureCount)")
    $markdown.Add("")
    $markdown.Add("| Mode | Status | Summary |")
    $markdown.Add("| --- | --- | --- |")
    foreach ($mode in $Report.DeploymentModeEvidence.GlobalClaimStatuses) {
        $markdown.Add("| $($mode.Mode) | $($mode.Status) | $($mode.Summary) |")
    }

    $markdown.Add("")
    $markdown.Add("| Package | Tier | Supported modes | Known hazards |")
    $markdown.Add("| --- | --- | --- | --- |")
    foreach ($package in $Report.DeploymentModeEvidence.PackageRows) {
        $supportedModes = if (@($package.SupportedModes).Count -eq 0) { "" } else { [string]::Join(", ", @($package.SupportedModes)) }
        $markdown.Add("| $($package.PackageName) | $($package.ClaimAuditTier) | $supportedModes | $($package.KnownHazardCount) |")
    }

    $markdown.Add("")
    $markdown.Add("## Eventing Operational-Superiority Evidence")
    $markdown.Add("")
    $markdown.Add("- Manifest: ``$($Report.EventingOperationalSuperiorityEvidence.Manifest)``")
    $markdown.Add("- Status: $($Report.EventingOperationalSuperiorityEvidence.Status)")
    $markdown.Add("- Technology surface: $($Report.EventingOperationalSuperiorityEvidence.Technology) / $($Report.EventingOperationalSuperiorityEvidence.SurfaceId) / $($Report.EventingOperationalSuperiorityEvidence.ProfileEntryId)")
    $markdown.Add("- Dimensions: $($Report.EventingOperationalSuperiorityEvidence.CoveredDimensionCount)/$($Report.EventingOperationalSuperiorityEvidence.RequiredDimensionCount) covered; partial $($Report.EventingOperationalSuperiorityEvidence.PartialDimensionCount); missing $($Report.EventingOperationalSuperiorityEvidence.MissingDimensionCount); coverage $($Report.EventingOperationalSuperiorityEvidence.CoveragePercent)%")
    $markdown.Add("- Promotion: $($Report.EventingOperationalSuperiorityEvidence.PromotionGate); allowed $($Report.EventingOperationalSuperiorityEvidence.PromotionAllowed); decision $($Report.EventingOperationalSuperiorityEvidence.PromotionDecisionCode); contract $($Report.EventingOperationalSuperiorityEvidence.PromotionEvidenceContract) $($Report.EventingOperationalSuperiorityEvidence.PromotionEvidenceContractVersion)")
    $markdown.Add("- Hot-path binding mode: $($Report.EventingOperationalSuperiorityEvidence.HotPathBindingMode)")
    $markdown.Add("- Configuration role: $($Report.EventingOperationalSuperiorityEvidence.ConfigurationRole)")
    $markdown.Add("- Wolverine required: $($Report.EventingOperationalSuperiorityEvidence.WolverineRequired)")
    $markdown.Add("- Runtime concordance: $($Report.EventingOperationalSuperiorityEvidence.RuntimeConcordanceStatus); source ``$($Report.EventingOperationalSuperiorityEvidence.RuntimeConcordanceSource)``; tokens $($Report.EventingOperationalSuperiorityEvidence.RuntimeConcordanceMatchedTokenCount)/$($Report.EventingOperationalSuperiorityEvidence.RuntimeConcordanceTokenCount) matched; missing $($Report.EventingOperationalSuperiorityEvidence.RuntimeConcordanceMissingTokenCount)")
    $markdown.Add("- Comparison baseline: $([string]::Join(', ', @($Report.EventingOperationalSuperiorityEvidence.ComparisonBaseline)))")
    $markdown.Add("")
    $markdown.Add("| Dimension | Status | Required status | Next requirement |")
    $markdown.Add("| --- | --- | --- | --- |")
    foreach ($dimension in $Report.EventingOperationalSuperiorityEvidence.RequiredDimensions) {
        $markdown.Add("| $($dimension.Id) | $($dimension.Status) | $($dimension.RequiredStatus) | $($dimension.NextRequirement) |")
    }

    $markdown.Add("")
    $markdown.Add("## SRE Posture Evidence")
    $markdown.Add("")
    $markdown.Add("- Manifest: ``$($Report.SrePostureEvidence.Manifest)``")
    $markdown.Add("- Status: $($Report.SrePostureEvidence.Status)")
    $markdown.Add("- Release validation summary mode: $($Report.SrePostureEvidence.ReleaseValidationSummaryMode)")
    $markdown.Add("- Stable baselines published: $($Report.SrePostureEvidence.StableBaselinesPublished)")
    $markdown.Add("- Stable baseline manifest: ``$($Report.SrePostureEvidence.StableBaselineManifest)``")
    $markdown.Add("- Stable baseline rows: $($Report.SrePostureEvidence.StableBaselineRowCount)")
    $markdown.Add("- Stable baseline measurements: $($Report.SrePostureEvidence.StableBaselineMeasurementCount)")
    $markdown.Add("- Guardrail catalog: ``$($Report.SrePostureEvidence.GuardrailCatalog)``")
    $markdown.Add("- Guardrail catalog entries: $($Report.SrePostureEvidence.GuardrailCatalogEntryCount)")
    $markdown.Add("- Guardrail-mapped SLIs: $($Report.SrePostureEvidence.GuardrailMappedSliCount)")
    $markdown.Add("- Pending guardrail coverage SLIs: $($Report.SrePostureEvidence.GuardrailPendingSliCount)")
    $markdown.Add("- Not-applicable guardrail coverage SLIs: $($Report.SrePostureEvidence.GuardrailNotApplicableSliCount)")
    $markdown.Add("- Guardrail references: $($Report.SrePostureEvidence.GuardrailReferenceCount)")
    $markdown.Add("")
    $markdown.Add("| SLI | Category | Target status | Baseline status | Guardrail coverage | Guardrail references |")
    $markdown.Add("| --- | --- | --- | --- | --- | --- |")
    foreach ($sli in $Report.SrePostureEvidence.SliRows) {
        $guardrailReferences = if (@($sli.GuardrailReferences).Count -eq 0) {
            ""
        }
        else {
            (@($sli.GuardrailReferences) | ForEach-Object { "``$($_.ReportFileName)`` / ``$($_.Benchmark)``" }) -join "<br>"
        }

        $markdown.Add("| ``$($sli.Id)`` | $($sli.Category) | $($sli.TargetStatus) | $($sli.BaselineStatus) | $($sli.GuardrailCoverageStatus) | $guardrailReferences |")
    }

    $markdown.Add("")
    $markdown.Add("| Pending baseline SLI | Blocker class | Evidence | Next evidence needed |")
    $markdown.Add("| --- | --- | --- | --- |")
    foreach ($row in $Report.SrePostureEvidence.PendingBaselineRows) {
        $evidence = (@($row.Evidence) | ForEach-Object {
            if ($_.PSObject.Properties.Name -contains "ReportFileName") {
                "``$($_.Kind)`` from ``$($_.ReportFileName)`` / ``$($_.Benchmark)``"
            }
            elseif ($_.PSObject.Properties.Name -contains "AvailabilityStatus") {
                $readiness = @($_.AvailabilityStatus)
                if ($_.PSObject.Properties.Name -contains "ActionsReadinessStatus") {
                    $readiness += $_.ActionsReadinessStatus
                }
                if ($_.PSObject.Properties.Name -contains "WorkflowReadinessStatus") {
                    $readiness += $_.WorkflowReadinessStatus
                }
                if ($_.PSObject.Properties.Name -contains "WorkflowDispatchReadinessStatus") {
                    $readiness += $_.WorkflowDispatchReadinessStatus
                }

                "``$($_.Kind)`` $([string]::Join('; ', $readiness))"
            }
            else {
                "``$($_.Kind)``"
            }
        }) -join "<br>"

        $markdown.Add("| ``$($row.SliId)`` | $($row.BlockerClass) | $evidence | $($row.NextEvidenceNeeded) |")
    }

    $markdown.Add("")
    $markdown.Add("| Stable baseline SLI | Measurement kind | Measurements |")
    $markdown.Add("| --- | --- | --- |")
    foreach ($row in $Report.SrePostureEvidence.StableBaselineRows) {
        $measurements = (@($row.Measurements) | ForEach-Object {
            if ($_.PSObject.Properties.Name -contains "ReportFileName") {
                "``$($_.ReportFileName)`` / ``$($_.Benchmark)`` mean $($_.MeanNanoseconds) ns, allocated $($_.AllocatedBytes) B"
            }
            elseif ($_.PSObject.Properties.Name -contains "ClaimsReportPath") {
                "``$($_.ClaimsReportPath)`` $($_.DeploymentMode), gate $($_.PublishProbeGateStatus), package claims $($_.PackageClaimTruthfulCount)/$($_.PackageClaimCount) truthful, fraction $($_.TruthfulFraction)"
            }
            elseif ($_.PSObject.Properties.Name -contains "TimingReportPath") {
                "``$($_.TimingReportPath)`` $($_.StepName), elapsed $($_.ElapsedMilliseconds) ms / target $($_.TargetMilliseconds) ms, status $($_.Status)"
            }
            else {
                "unsupported measurement shape"
            }
        }) -join "<br>"
        $markdown.Add("| ``$($row.SliId)`` | $($row.MeasurementKind) | $measurements |")
    }

    $markdown.Add("")
    $markdown.Add("## Supply-Chain Release Evidence")
    $markdown.Add("")
    $markdown.Add("- Manifest: ``$($Report.SupplyChainEvidence.Manifest)``")
    $markdown.Add("- Status: $($Report.SupplyChainEvidence.Status)")
    $markdown.Add("- Release workflow: ``$($Report.SupplyChainEvidence.ReleaseWorkflow)``")
    $markdown.Add("- Evidence items: $($Report.SupplyChainEvidence.EvidenceItemCount)")
    $markdown.Add("- Workflow-ready items: $($Report.SupplyChainEvidence.WorkflowReadyCount)")
    $markdown.Add("- External-policy-pending items: $($Report.SupplyChainEvidence.ExternalPolicyPendingCount)")
    $markdown.Add("- External-policy preflight checks: $($Report.SupplyChainEvidence.ExternalPolicyPreflightCheckCount)")
    $markdown.Add("- Signed-release dry-run status: $($Report.SupplyChainEvidence.SignedReleaseDryRunStatus)")
    if (-not [string]::IsNullOrWhiteSpace($Report.SupplyChainEvidence.SignedReleaseDryRunBlockerClass)) {
        $markdown.Add("- Signed-release dry-run blocker: ``$($Report.SupplyChainEvidence.SignedReleaseDryRunBlockerClass)``")
    }
    $markdown.Add("- Blocked items: $($Report.SupplyChainEvidence.BlockedCount)")
    $markdown.Add("")

    if ($null -ne $Report.SupplyChainEvidence.SignedReleaseDryRun) {
        $markdown.Add("### Signed-Release Dry Run")
        $markdown.Add("")
        $markdown.Add("- Status: $($Report.SupplyChainEvidence.SignedReleaseDryRun.Status)")
        $markdown.Add("- Current proof state: $($Report.SupplyChainEvidence.SignedReleaseDryRun.CurrentProofState)")
        $markdown.Add("- Current blocker class: ``$($Report.SupplyChainEvidence.SignedReleaseDryRun.CurrentBlockerClass)``")
        $markdown.Add("- Readiness policy: ``$($Report.SupplyChainEvidence.SignedReleaseDryRun.ReadinessPolicy)``")
        $markdown.Add("- Required command: ``$($Report.SupplyChainEvidence.SignedReleaseDryRun.RequiredCommand)``")
        $markdown.Add("- Output path: ``$($Report.SupplyChainEvidence.SignedReleaseDryRun.OutputPath)``")
        $markdown.Add("- Handoff output path: ``$($Report.SupplyChainEvidence.SignedReleaseDryRun.HandoffOutputPath)``")
        $markdown.Add("- Required report fields: $($Report.SupplyChainEvidence.SignedReleaseDryRun.RequiredReportFieldCount)")
        $markdown.Add("")
    }

    if ($null -ne $Report.SupplyChainEvidence.ExternalPolicyPreflight) {
        $markdown.Add("### External-Policy Preflight")
        $markdown.Add("")
        $markdown.Add("- Status: $($Report.SupplyChainEvidence.ExternalPolicyPreflight.Status)")
        $markdown.Add("- Validation script: ``$($Report.SupplyChainEvidence.ExternalPolicyPreflight.ValidationScript)``")
        $markdown.Add("- Output path: ``$($Report.SupplyChainEvidence.ExternalPolicyPreflight.OutputPath)``")
        $markdown.Add("- Required checks: $($Report.SupplyChainEvidence.ExternalPolicyPreflight.RequiredCheckCount)")
        $markdown.Add("")
        $markdown.Add("| Evidence item | Verification mode | Required input |")
        $markdown.Add("| --- | --- | --- |")
        foreach ($check in $Report.SupplyChainEvidence.ExternalPolicyPreflight.RequiredChecks) {
            $markdown.Add("| ``$($check.EvidenceItemId)`` | $($check.VerificationMode) | ``$($check.RequiredInput)`` |")
        }

        $markdown.Add("")
    }

    $markdown.Add("| Evidence item | Category | Status | Source document |")
    $markdown.Add("| --- | --- | --- | --- |")
    foreach ($item in $Report.SupplyChainEvidence.EvidenceItems) {
        $markdown.Add("| ``$($item.Id)`` | $($item.Category) | $($item.Status) | ``$($item.SourceDocument)`` |")
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
    $markdown.Add("- Execution report: ``$($Report.AdoptionSmokeEvidence.ExecutionReport.DefaultPath)`` (schema ``$($Report.AdoptionSmokeEvidence.ExecutionReport.SchemaVersion)``)")
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
    $markdown.Add("## Test Coverage Evidence")
    $markdown.Add("")
    $markdown.Add("- Roadmap: ``$($Report.TestCoverageEvidence.Roadmap)``")
    $markdown.Add("- Layered projects: $($Report.TestCoverageEvidence.LayeredProjectCount)")
    $markdown.Add("- Gap criteria: $($Report.TestCoverageEvidence.GapDefinitionCriterionCount)")
    $markdown.Add("- Recommendations: $($Report.TestCoverageEvidence.RecommendationCount)")
    $markdown.Add("- Shipped recommendations: $($Report.TestCoverageEvidence.ShippedRecommendationCount)")
    $markdown.Add("- Gated recommendations: $($Report.TestCoverageEvidence.GatedRecommendationCount)")
    $markdown.Add("- Active gap recommendations: $($Report.TestCoverageEvidence.ActiveGapRecommendationCount)")
    $markdown.Add("- Quarantine entries: $($Report.TestCoverageEvidence.QuarantineEntryCount)")
    $markdown.Add("- Open quarantine entries: $($Report.TestCoverageEvidence.OpenQuarantineEntryCount)")
    $markdown.Add("- Quarantine queue status: $($Report.TestCoverageEvidence.QuarantineQueueStatus)")
    $markdown.Add("")
    $markdown.Add("| Recommendation | Priority | Status | Title |")
    $markdown.Add("| --- | --- | --- | --- |")
    foreach ($recommendation in $Report.TestCoverageEvidence.Recommendations) {
        $markdown.Add("| #$($recommendation.Number) | $($recommendation.Priority) | $($recommendation.Status) | $($recommendation.Title) |")
    }

    $markdown.Add("")
    $markdown.Add("## Provider Integration Evidence")
    $markdown.Add("")
    $markdown.Add("- Manifest: ``$($Report.ProviderIntegrationEvidence.Manifest)``")
    $markdown.Add("- Status: $($Report.ProviderIntegrationEvidence.Status)")
    $markdown.Add("- Evidence rows: $($Report.ProviderIntegrationEvidence.EvidenceRowCount)")
    $markdown.Add("- Live proofs: $($Report.ProviderIntegrationEvidence.LiveProofCount)")
    $markdown.Add("- Composition-only rows: $($Report.ProviderIntegrationEvidence.CompositionOnlyCount)")
    $markdown.Add("- External-service gates: $($Report.ProviderIntegrationEvidence.ExternalServiceGateCount)")
    $markdown.Add("- Default-skipped rows: $($Report.ProviderIntegrationEvidence.DefaultSkippedCount)")
    $markdown.Add("- Runtime contracts: $($Report.ProviderIntegrationEvidence.RuntimeContractCount)")
    $markdown.Add("- Environment variables: $($Report.ProviderIntegrationEvidence.EnvironmentVariableCount)")
    $markdown.Add("")
    $markdown.Add("| Provider | Family | Status | Default run behavior | Runtime contracts |")
    $markdown.Add("| --- | --- | --- | --- | --- |")
    foreach ($provider in $Report.ProviderIntegrationEvidence.ProviderRows) {
        $runtimeContracts = if (@($provider.RuntimeContracts).Count -eq 0) { "" } else { [string]::Join(", ", @($provider.RuntimeContracts)) }
        $markdown.Add("| $($provider.Provider) | $($provider.Family) | $($provider.Status) | $($provider.DefaultRunBehavior) | $runtimeContracts |")
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
        [string]$DeploymentModeManifestPath = "scripts/deployment-mode-support.json",
        [string]$DeploymentModeClaimsReportPath = "artifacts/deployment-mode-claims-release/claim-validation-report.json",
        [string]$AdoptionSmokeManifestPath = "scripts/adoption-smoke-support.json",
        [string]$ProviderIntegrationManifestPath = "scripts/provider-integration-support.json",
        [string]$EventingOperationalSuperiorityManifestPath = "scripts/eventing-operational-superiority-support.json",
        [string]$SrePostureManifestPath = "scripts/sre-posture-support.json",
        [string]$SupplyChainManifestPath = "scripts/supply-chain-release-support.json",
        [string]$TestCoverageRoadmapPath = "docs/test-coverage-roadmap.md",
        [string]$PublicApiDeltaScriptPath = "scripts/summarise-public-api-deltas.ps1",
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $resolvedRepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
    $resolvedScorecardPath = Resolve-FullPath -Path $ScorecardPath -BasePath $resolvedRepoRoot
    $resolvedConformanceMatrixPath = Resolve-FullPath -Path $ConformanceMatrixPath -BasePath $resolvedRepoRoot
    $resolvedDeploymentModeManifestPath = Resolve-FullPath -Path $DeploymentModeManifestPath -BasePath $resolvedRepoRoot
    $resolvedDeploymentModeClaimsReportPath = Resolve-FullPath -Path $DeploymentModeClaimsReportPath -BasePath $resolvedRepoRoot
    $resolvedAdoptionSmokeManifestPath = Resolve-FullPath -Path $AdoptionSmokeManifestPath -BasePath $resolvedRepoRoot
    $resolvedProviderIntegrationManifestPath = Resolve-FullPath -Path $ProviderIntegrationManifestPath -BasePath $resolvedRepoRoot
    $resolvedEventingOperationalSuperiorityManifestPath = Resolve-FullPath -Path $EventingOperationalSuperiorityManifestPath -BasePath $resolvedRepoRoot
    $resolvedSrePostureManifestPath = Resolve-FullPath -Path $SrePostureManifestPath -BasePath $resolvedRepoRoot
    $resolvedSupplyChainManifestPath = Resolve-FullPath -Path $SupplyChainManifestPath -BasePath $resolvedRepoRoot
    $resolvedTestCoverageRoadmapPath = Resolve-FullPath -Path $TestCoverageRoadmapPath -BasePath $resolvedRepoRoot
    $resolvedPublicApiDeltaScriptPath = Resolve-FullPath -Path $PublicApiDeltaScriptPath -BasePath $resolvedRepoRoot
    $resolvedOutputPath = Resolve-FullPath -Path $OutputPath -BasePath $resolvedRepoRoot

    $report = New-EngineCompletionScorecardReport -ResolvedScorecardPath $resolvedScorecardPath -ResolvedConformanceMatrixPath $resolvedConformanceMatrixPath -ResolvedDeploymentModeManifestPath $resolvedDeploymentModeManifestPath -ResolvedDeploymentModeClaimsReportPath $resolvedDeploymentModeClaimsReportPath -ResolvedAdoptionSmokeManifestPath $resolvedAdoptionSmokeManifestPath -ResolvedProviderIntegrationManifestPath $resolvedProviderIntegrationManifestPath -ResolvedEventingOperationalSuperiorityManifestPath $resolvedEventingOperationalSuperiorityManifestPath -ResolvedSrePostureManifestPath $resolvedSrePostureManifestPath -ResolvedSupplyChainManifestPath $resolvedSupplyChainManifestPath -ResolvedTestCoverageRoadmapPath $resolvedTestCoverageRoadmapPath -ResolvedPublicApiDeltaScriptPath $resolvedPublicApiDeltaScriptPath -ResolvedRepoRoot $resolvedRepoRoot
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

    $null = Invoke-EngineCompletionScorecardPublish -ScorecardPath $ScorecardPath -ConformanceMatrixPath $ConformanceMatrixPath -DeploymentModeManifestPath $DeploymentModeManifestPath -DeploymentModeClaimsReportPath $DeploymentModeClaimsReportPath -AdoptionSmokeManifestPath $AdoptionSmokeManifestPath -ProviderIntegrationManifestPath $ProviderIntegrationManifestPath -EventingOperationalSuperiorityManifestPath $EventingOperationalSuperiorityManifestPath -SrePostureManifestPath $SrePostureManifestPath -SupplyChainManifestPath $SupplyChainManifestPath -TestCoverageRoadmapPath $TestCoverageRoadmapPath -PublicApiDeltaScriptPath $PublicApiDeltaScriptPath -OutputPath $OutputPath -RepoRoot $resolvedRoot
}
