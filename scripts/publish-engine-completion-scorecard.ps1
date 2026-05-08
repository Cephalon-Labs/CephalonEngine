param(
    [string]$ScorecardPath = "docs/engine-completion-scorecard.md",
    [string]$ConformanceMatrixPath = "docs/conformance-matrix.md",
    [string]$DeploymentModeManifestPath = "scripts/deployment-mode-support.json",
    [string]$AdoptionSmokeManifestPath = "scripts/adoption-smoke-support.json",
    [string]$ProviderIntegrationManifestPath = "scripts/provider-integration-support.json",
    [string]$SrePostureManifestPath = "scripts/sre-posture-support.json",
    [string]$SupplyChainManifestPath = "scripts/supply-chain-release-support.json",
    [string]$PublicApiDeltaScriptPath = "scripts/summarise-public-api-deltas.ps1",
    [string]$OutputPath = "artifacts/engine-completion-scorecard-release",
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Script:SchemaVersion = "1.8.0"
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
            Id                 = "$providerKey-dependency-health-invariant"
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
            [pscustomobject]@{ Name = "status"; Actual = $actualRow.Status; Expected = "composition-only" },
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
            throw "Provider integration dependency-health row '$($expectedRow.Id)' must keep environment variables empty for deterministic no-external-service proof."
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

function Convert-DeploymentModeEvidence {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedManifestPath,
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
    $stableBaselineCount = @($sliRows | Where-Object { $_.BaselineStatus -eq "stable-baseline" }).Count
    $guardrailMappedSliCount = @($sliRows | Where-Object { $_.GuardrailCoverageStatus -eq "guardrail-catalog-mapped" }).Count
    $guardrailPendingSliCount = @($sliRows | Where-Object { $_.GuardrailCoverageStatus -eq "pending-stable-baseline" }).Count
    $guardrailNotApplicableSliCount = @($sliRows | Where-Object { $_.GuardrailCoverageStatus -eq "not-applicable" }).Count
    $guardrailReferenceCount = @($sliRows | ForEach-Object { $_.GuardrailReferences }).Count

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
        GuardrailMappedSliCount    = $guardrailMappedSliCount
        GuardrailPendingSliCount   = $guardrailPendingSliCount
        GuardrailNotApplicableSliCount = $guardrailNotApplicableSliCount
        GuardrailReferenceCount    = $guardrailReferenceCount
        SliRows                    = $sliRows
        ValidatedReferences        = @(
            $sourceDocumentReferences
            $validationScriptReferences
            $guardrailCatalogReference
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
        [Parameter(Mandatory = $true)]
        [string]$ResolvedAdoptionSmokeManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedProviderIntegrationManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedSrePostureManifestPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedSupplyChainManifestPath,
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
    $deploymentModeEvidence = Convert-DeploymentModeEvidence -ResolvedManifestPath $ResolvedDeploymentModeManifestPath -ResolvedRepoRoot $ResolvedRepoRoot
    $adoptionSmokeEvidence = Convert-AdoptionSmokeEvidence -ResolvedManifestPath $ResolvedAdoptionSmokeManifestPath -ResolvedRepoRoot $ResolvedRepoRoot
    $providerIntegrationEvidence = Convert-ProviderIntegrationEvidence -ResolvedManifestPath $ResolvedProviderIntegrationManifestPath -ResolvedRepoRoot $ResolvedRepoRoot
    $srePostureEvidence = Convert-SrePostureEvidence -ResolvedManifestPath $ResolvedSrePostureManifestPath -ResolvedRepoRoot $ResolvedRepoRoot
    $supplyChainEvidence = Convert-SupplyChainEvidence -ResolvedManifestPath $ResolvedSupplyChainManifestPath -ResolvedRepoRoot $ResolvedRepoRoot
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
        AdoptionSmokeManifest = Get-RepoRelativePath -Path $ResolvedAdoptionSmokeManifestPath -RepoRoot $ResolvedRepoRoot
        ProviderIntegrationManifest = Get-RepoRelativePath -Path $ResolvedProviderIntegrationManifestPath -RepoRoot $ResolvedRepoRoot
        SrePostureManifest = Get-RepoRelativePath -Path $ResolvedSrePostureManifestPath -RepoRoot $ResolvedRepoRoot
        SupplyChainManifest = Get-RepoRelativePath -Path $ResolvedSupplyChainManifestPath -RepoRoot $ResolvedRepoRoot
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
        SrePostureEvidence = $srePostureEvidence
        SupplyChainEvidence = $supplyChainEvidence
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
            AdoptionSmokeScenarioCount = if ($null -ne $adoptionSmokeEvidence) { 1 } else { 0 }
            AdoptionSmokeRuntimeProbeCount = @($adoptionSmokeEvidence.RuntimeProbes).Count
            AdoptionSmokeAssertionCount = @($adoptionSmokeEvidence.Assertions).Count
            ProviderIntegrationEvidenceRowCount = $providerIntegrationEvidence.EvidenceRowCount
            ProviderIntegrationLiveProofCount = $providerIntegrationEvidence.LiveProofCount
            ProviderIntegrationCompositionOnlyCount = $providerIntegrationEvidence.CompositionOnlyCount
            ProviderIntegrationExternalServiceGateCount = $providerIntegrationEvidence.ExternalServiceGateCount
            ProviderIntegrationDefaultSkippedCount = $providerIntegrationEvidence.DefaultSkippedCount
            ProviderIntegrationRuntimeContractCount = $providerIntegrationEvidence.RuntimeContractCount
            SreSliCount = $srePostureEvidence.SliCount
            SreTargetDeclaredCount = $srePostureEvidence.TargetDeclaredCount
            SrePendingStableBaselineCount = $srePostureEvidence.PendingStableBaselineCount
            SreStableBaselineCount = $srePostureEvidence.StableBaselineCount
            SreGuardrailMappedSliCount = $srePostureEvidence.GuardrailMappedSliCount
            SreGuardrailPendingSliCount = $srePostureEvidence.GuardrailPendingSliCount
            SreGuardrailNotApplicableSliCount = $srePostureEvidence.GuardrailNotApplicableSliCount
            SreGuardrailReferenceCount = $srePostureEvidence.GuardrailReferenceCount
            SupplyChainEvidenceItemCount = $supplyChainEvidence.EvidenceItemCount
            SupplyChainWorkflowReadyCount = $supplyChainEvidence.WorkflowReadyCount
            SupplyChainExternalPolicyPendingCount = $supplyChainEvidence.ExternalPolicyPendingCount
            SupplyChainBlockedCount = $supplyChainEvidence.BlockedCount
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
    $markdown.Add("SRE posture manifest: ``$($Report.SrePostureManifest)``")
    $markdown.Add("Supply-chain release manifest: ``$($Report.SupplyChainManifest)``")
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
    $markdown.Add("- Provider integration evidence rows: $($Report.Summary.ProviderIntegrationEvidenceRowCount)")
    $markdown.Add("- Provider integration live proofs: $($Report.Summary.ProviderIntegrationLiveProofCount)")
    $markdown.Add("- Provider integration composition-only rows: $($Report.Summary.ProviderIntegrationCompositionOnlyCount)")
    $markdown.Add("- Provider integration external-service gates: $($Report.Summary.ProviderIntegrationExternalServiceGateCount)")
    $markdown.Add("- Provider integration default-skipped rows: $($Report.Summary.ProviderIntegrationDefaultSkippedCount)")
    $markdown.Add("- Provider integration runtime contracts: $($Report.Summary.ProviderIntegrationRuntimeContractCount)")
    $markdown.Add("- SRE SLIs: $($Report.Summary.SreSliCount)")
    $markdown.Add("- SRE target-declared SLIs: $($Report.Summary.SreTargetDeclaredCount)")
    $markdown.Add("- SRE pending stable baselines: $($Report.Summary.SrePendingStableBaselineCount)")
    $markdown.Add("- SRE stable baselines: $($Report.Summary.SreStableBaselineCount)")
    $markdown.Add("- SRE guardrail-mapped SLIs: $($Report.Summary.SreGuardrailMappedSliCount)")
    $markdown.Add("- SRE pending guardrail coverage SLIs: $($Report.Summary.SreGuardrailPendingSliCount)")
    $markdown.Add("- Supply-chain evidence items: $($Report.Summary.SupplyChainEvidenceItemCount)")
    $markdown.Add("- Supply-chain workflow-ready items: $($Report.Summary.SupplyChainWorkflowReadyCount)")
    $markdown.Add("- Supply-chain external-policy-pending items: $($Report.Summary.SupplyChainExternalPolicyPendingCount)")
    $markdown.Add("- Supply-chain blocked items: $($Report.Summary.SupplyChainBlockedCount)")
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
    $markdown.Add("## SRE Posture Evidence")
    $markdown.Add("")
    $markdown.Add("- Manifest: ``$($Report.SrePostureEvidence.Manifest)``")
    $markdown.Add("- Status: $($Report.SrePostureEvidence.Status)")
    $markdown.Add("- Release validation summary mode: $($Report.SrePostureEvidence.ReleaseValidationSummaryMode)")
    $markdown.Add("- Stable baselines published: $($Report.SrePostureEvidence.StableBaselinesPublished)")
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
    $markdown.Add("## Supply-Chain Release Evidence")
    $markdown.Add("")
    $markdown.Add("- Manifest: ``$($Report.SupplyChainEvidence.Manifest)``")
    $markdown.Add("- Status: $($Report.SupplyChainEvidence.Status)")
    $markdown.Add("- Release workflow: ``$($Report.SupplyChainEvidence.ReleaseWorkflow)``")
    $markdown.Add("- Evidence items: $($Report.SupplyChainEvidence.EvidenceItemCount)")
    $markdown.Add("- Workflow-ready items: $($Report.SupplyChainEvidence.WorkflowReadyCount)")
    $markdown.Add("- External-policy-pending items: $($Report.SupplyChainEvidence.ExternalPolicyPendingCount)")
    $markdown.Add("- Blocked items: $($Report.SupplyChainEvidence.BlockedCount)")
    $markdown.Add("")
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
        [string]$AdoptionSmokeManifestPath = "scripts/adoption-smoke-support.json",
        [string]$ProviderIntegrationManifestPath = "scripts/provider-integration-support.json",
        [string]$SrePostureManifestPath = "scripts/sre-posture-support.json",
        [string]$SupplyChainManifestPath = "scripts/supply-chain-release-support.json",
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
    $resolvedAdoptionSmokeManifestPath = Resolve-FullPath -Path $AdoptionSmokeManifestPath -BasePath $resolvedRepoRoot
    $resolvedProviderIntegrationManifestPath = Resolve-FullPath -Path $ProviderIntegrationManifestPath -BasePath $resolvedRepoRoot
    $resolvedSrePostureManifestPath = Resolve-FullPath -Path $SrePostureManifestPath -BasePath $resolvedRepoRoot
    $resolvedSupplyChainManifestPath = Resolve-FullPath -Path $SupplyChainManifestPath -BasePath $resolvedRepoRoot
    $resolvedPublicApiDeltaScriptPath = Resolve-FullPath -Path $PublicApiDeltaScriptPath -BasePath $resolvedRepoRoot
    $resolvedOutputPath = Resolve-FullPath -Path $OutputPath -BasePath $resolvedRepoRoot

    $report = New-EngineCompletionScorecardReport -ResolvedScorecardPath $resolvedScorecardPath -ResolvedConformanceMatrixPath $resolvedConformanceMatrixPath -ResolvedDeploymentModeManifestPath $resolvedDeploymentModeManifestPath -ResolvedAdoptionSmokeManifestPath $resolvedAdoptionSmokeManifestPath -ResolvedProviderIntegrationManifestPath $resolvedProviderIntegrationManifestPath -ResolvedSrePostureManifestPath $resolvedSrePostureManifestPath -ResolvedSupplyChainManifestPath $resolvedSupplyChainManifestPath -ResolvedPublicApiDeltaScriptPath $resolvedPublicApiDeltaScriptPath -ResolvedRepoRoot $resolvedRepoRoot
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

    $null = Invoke-EngineCompletionScorecardPublish -ScorecardPath $ScorecardPath -ConformanceMatrixPath $ConformanceMatrixPath -DeploymentModeManifestPath $DeploymentModeManifestPath -AdoptionSmokeManifestPath $AdoptionSmokeManifestPath -ProviderIntegrationManifestPath $ProviderIntegrationManifestPath -SrePostureManifestPath $SrePostureManifestPath -SupplyChainManifestPath $SupplyChainManifestPath -PublicApiDeltaScriptPath $PublicApiDeltaScriptPath -OutputPath $OutputPath -RepoRoot $resolvedRoot
}
