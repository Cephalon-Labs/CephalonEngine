[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$SourceRoot = "src",
    [string]$ComponentDocsRoot = "docs/components",
    [string]$ComponentCatalogPath = "docs/components/README.md",
    [string]$MaturityAuditPath = "docs/engine-surface-maturity-audit.md",
    [string]$ConformanceMatrixPath = "docs/conformance-matrix.md",
    [string]$BacklogPath = "docs/engine-backlog.md",
    [string]$DependencyHealthProviderManifestPath = "scripts/observability-dependency-health-providers.json",
    [string]$OutputPath = "artifacts/surface-maturity-report/surface-maturity-report.json",
    [switch]$FailOnDrift
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepositoryPath {
    param([Parameter(Mandatory = $true)] [string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $RepositoryRoot $Path))
}

function Get-RelativeRepositoryPath {
    param([Parameter(Mandatory = $true)] [string]$Path)

    return [System.IO.Path]::GetRelativePath(
        [System.IO.Path]::GetFullPath($RepositoryRoot),
        [System.IO.Path]::GetFullPath($Path)).Replace('\', '/')
}

function Remove-MarkdownFormatting {
    param([AllowNull()] [string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return ($Value -replace '`', '').Trim()
}

function Get-OwnershipModes {
    param([AllowNull()] [string]$Value)

    $knownModes = @("taxonomy-only", "application-managed", "cephalon-managed", "provider-managed")
    return @($knownModes | Where-Object { $Value -match [regex]::Escape($_) })
}

function Get-BaselineDate {
    param(
        [Parameter(Mandatory = $true)] [string]$Contents,
        [Parameter(Mandatory = $true)] [string]$Label
    )

    $match = [regex]::Match($Contents, [regex]::Escape($Label) + '.*?`(?<date>[A-Za-z]+ \d{1,2}, \d{4})`')
    if (-not $match.Success) {
        return $null
    }

    return $match.Groups["date"].Value
}

function Add-Drift {
    param(
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [System.Collections.Generic.List[object]]$Drift,
        [Parameter(Mandatory = $true)] [string]$Code,
        [AllowNull()] [string]$Package,
        [Parameter(Mandatory = $true)] [string]$Message
    )

    $Drift.Add([ordered]@{
        code = $Code
        package = $Package
        message = $Message
    })
}

$resolvedSourceRoot = Resolve-RepositoryPath $SourceRoot
$resolvedComponentDocsRoot = Resolve-RepositoryPath $ComponentDocsRoot
$resolvedComponentCatalogPath = Resolve-RepositoryPath $ComponentCatalogPath
$resolvedMaturityAuditPath = Resolve-RepositoryPath $MaturityAuditPath
$resolvedConformanceMatrixPath = Resolve-RepositoryPath $ConformanceMatrixPath
$resolvedBacklogPath = Resolve-RepositoryPath $BacklogPath
$resolvedDependencyHealthProviderManifestPath = Resolve-RepositoryPath $DependencyHealthProviderManifestPath
$resolvedOutputPath = Resolve-RepositoryPath $OutputPath

foreach ($requiredPath in @(
    $resolvedSourceRoot,
    $resolvedComponentDocsRoot,
    $resolvedComponentCatalogPath,
    $resolvedMaturityAuditPath,
    $resolvedConformanceMatrixPath,
    $resolvedBacklogPath,
    $resolvedDependencyHealthProviderManifestPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Surface maturity evidence path not found: $requiredPath"
    }
}

$componentCatalog = Get-Content -LiteralPath $resolvedComponentCatalogPath -Raw -Encoding UTF8
$maturityAudit = Get-Content -LiteralPath $resolvedMaturityAuditPath -Raw -Encoding UTF8
$conformanceMatrix = Get-Content -LiteralPath $resolvedConformanceMatrixPath -Raw -Encoding UTF8
$backlog = Get-Content -LiteralPath $resolvedBacklogPath -Raw -Encoding UTF8
$dependencyHealthManifest = Get-Content -LiteralPath $resolvedDependencyHealthProviderManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 32
$drift = [System.Collections.Generic.List[object]]::new()

$matrixRows = @{}
foreach ($line in ($conformanceMatrix -split "`r?`n")) {
    if ($line -notmatch '^\| `(?<package>Cephalon\.[^`]+)` \| (?<maturity>M[0-4]) \| (?<ownership>[^|]+) \|') {
        continue
    }

    $packageName = $Matches["package"]
    if ($matrixRows.ContainsKey($packageName)) {
        Add-Drift $drift "duplicate-conformance-row" $packageName "The conformance matrix declares the package more than once."
        continue
    }

    $matrixRows[$packageName] = [ordered]@{
        maturity = $Matches["maturity"]
        ownership = Remove-MarkdownFormatting $Matches["ownership"]
        source = "conformance-matrix"
    }
}

$providerRows = @{}
foreach ($provider in @($dependencyHealthManifest.providers)) {
    $packageName = [string]$provider.source
    if ($providerRows.ContainsKey($packageName)) {
        Add-Drift $drift "duplicate-provider-row" $packageName "The dependency-health provider manifest declares the package more than once."
        continue
    }

    $providerRows[$packageName] = [ordered]@{
        maturity = [string]$provider.maturity
        ownership = [string]$provider.ownership
        source = "dependency-health-provider-manifest"
    }
}

$sourceProjects = @(
    Get-ChildItem -LiteralPath $resolvedSourceRoot -Directory -Filter "Cephalon.*" |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "$($_.Name).csproj") } |
        Sort-Object Name
)

$componentDocs = @(
    Get-ChildItem -LiteralPath $resolvedComponentDocsRoot -File -Filter "*.md" |
        Where-Object { $_.Name -ne "README.md" } |
        Sort-Object Name
)

$sourceProjectNames = @{}
foreach ($project in $sourceProjects) {
    $sourceProjectNames[$project.Name] = $project.FullName
}

$documentedPackages = @{}
$packageEvidence = [System.Collections.Generic.List[object]]::new()

foreach ($componentDoc in $componentDocs) {
    $contents = Get-Content -LiteralPath $componentDoc.FullName -Raw -Encoding UTF8
    $headingMatch = [regex]::Match($contents, '(?m)^# (?<package>Cephalon\.[^\r\n]+)\r?$')
    if (-not $headingMatch.Success) {
        Add-Drift $drift "missing-package-heading" $null "Component document '$($componentDoc.Name)' does not start with a Cephalon package heading."
        continue
    }

    $packageName = $headingMatch.Groups["package"].Value.Trim()
    if ($documentedPackages.ContainsKey($packageName)) {
        Add-Drift $drift "duplicate-component-document" $packageName "Multiple component documents declare the same package."
        continue
    }

    $documentedPackages[$packageName] = $componentDoc.FullName

    $badgeMatch = [regex]::Match(
        $contents,
        '(?m)^> \*\*Maturity:\*\* `(?<maturity>M[0-4])` · \*\*Ownership:\*\* (?<ownership>.+?) — authoritative truth in \[`engine-surface-maturity-audit\.md`\]\(\.\./engine-surface-maturity-audit\.md\)\r?$')
    if (-not $badgeMatch.Success) {
        Add-Drift $drift "invalid-maturity-badge" $packageName "Component document '$($componentDoc.Name)' does not use the canonical maturity badge."
        continue
    }

    $maturity = $badgeMatch.Groups["maturity"].Value
    $ownership = Remove-MarkdownFormatting $badgeMatch.Groups["ownership"].Value
    $ownershipModes = @(Get-OwnershipModes $ownership)
    if ($ownershipModes.Count -eq 0) {
        Add-Drift $drift "invalid-ownership" $packageName "The maturity badge does not declare a supported ownership mode."
    }

    if (-not $sourceProjectNames.ContainsKey($packageName)) {
        Add-Drift $drift "component-without-source" $packageName "The component document has no matching source project."
    }

    $catalogTarget = "]($($componentDoc.Name))"
    $catalogEntryCount = ([regex]::Matches($componentCatalog, [regex]::Escape($catalogTarget))).Count
    if ($catalogEntryCount -ne 1) {
        Add-Drift $drift "component-catalog-count" $packageName "Expected one component catalog link to '$($componentDoc.Name)' but found $catalogEntryCount."
    }

    $declaredEvidence = $null
    if ($matrixRows.ContainsKey($packageName)) {
        $declaredEvidence = $matrixRows[$packageName]
    }
    elseif ($providerRows.ContainsKey($packageName)) {
        $declaredEvidence = $providerRows[$packageName]
    }
    else {
        Add-Drift $drift "missing-maturity-evidence" $packageName "No exact conformance row or dependency-health provider manifest row declares this package."
    }

    if ($null -ne $declaredEvidence) {
        if ($maturity -ne $declaredEvidence.maturity) {
            Add-Drift $drift "maturity-mismatch" $packageName "Component badge '$maturity' disagrees with '$($declaredEvidence.maturity)' from $($declaredEvidence.source)."
        }

        $declaredOwnershipModes = @(Get-OwnershipModes $declaredEvidence.ownership)
        foreach ($mode in $declaredOwnershipModes) {
            if ($ownershipModes -notcontains $mode) {
                Add-Drift $drift "ownership-mismatch" $packageName "Component ownership '$ownership' omits '$mode' declared by $($declaredEvidence.source)."
            }
        }
    }

    $packageEvidence.Add([ordered]@{
        package = $packageName
        componentDocument = Get-RelativeRepositoryPath $componentDoc.FullName
        maturity = $maturity
        ownership = $ownership
        ownershipModes = $ownershipModes
        declarationSource = if ($null -eq $declaredEvidence) { $null } else { $declaredEvidence.source }
    })
}

foreach ($sourceProject in $sourceProjects) {
    if (-not $documentedPackages.ContainsKey($sourceProject.Name)) {
        Add-Drift $drift "source-without-component" $sourceProject.Name "The source project has no matching component document."
    }
}

$auditBaselineDate = Get-BaselineDate $maturityAudit "Surface maturity in this document reflects the repository state as of"
$backlogBaselineDate = Get-BaselineDate $backlog "Backlog status in this document reflects the repository state as of"
if ([string]::IsNullOrWhiteSpace($auditBaselineDate)) {
    Add-Drift $drift "missing-audit-baseline" $null "The maturity audit baseline date could not be parsed."
}
if ([string]::IsNullOrWhiteSpace($backlogBaselineDate)) {
    Add-Drift $drift "missing-backlog-baseline" $null "The backlog baseline date could not be parsed."
}
if (-not [string]::IsNullOrWhiteSpace($auditBaselineDate) -and
    -not [string]::IsNullOrWhiteSpace($backlogBaselineDate) -and
    $auditBaselineDate -ne $backlogBaselineDate) {
    Add-Drift $drift "baseline-date-mismatch" $null "Maturity audit baseline '$auditBaselineDate' disagrees with backlog baseline '$backlogBaselineDate'."
}

$maturityCounts = [ordered]@{}
foreach ($level in @("M0", "M1", "M2", "M3", "M4")) {
    $maturityCounts[$level] = @($packageEvidence | Where-Object { $_.maturity -eq $level }).Count
}

$ownershipCounts = [ordered]@{}
foreach ($mode in @("taxonomy-only", "application-managed", "cephalon-managed", "provider-managed")) {
    $ownershipCounts[$mode] = @($packageEvidence | Where-Object { $_.ownershipModes -contains $mode }).Count
}

$capturedFromCommit = $null
try {
    $capturedFromCommit = (& git -C $RepositoryRoot rev-parse HEAD 2>$null).Trim()
}
catch {
}

$report = [ordered]@{
    schemaVersion = "1.0.0"
    status = if ($drift.Count -eq 0) { "passed" } else { "drift-detected" }
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    capturedFromCommit = $capturedFromCommit
    auditBaselineDate = $auditBaselineDate
    backlogBaselineDate = $backlogBaselineDate
    summary = [ordered]@{
        sourceProjectCount = $sourceProjects.Count
        componentDocumentCount = $componentDocs.Count
        packageEvidenceCount = $packageEvidence.Count
        exactConformanceRowCount = $matrixRows.Count
        providerManifestRowCount = $providerRows.Count
        driftCount = $drift.Count
        maturityCounts = $maturityCounts
        ownershipCounts = $ownershipCounts
    }
    evidenceSources = [ordered]@{
        componentCatalog = Get-RelativeRepositoryPath $resolvedComponentCatalogPath
        maturityAudit = Get-RelativeRepositoryPath $resolvedMaturityAuditPath
        conformanceMatrix = Get-RelativeRepositoryPath $resolvedConformanceMatrixPath
        backlog = Get-RelativeRepositoryPath $resolvedBacklogPath
        dependencyHealthProviderManifest = Get-RelativeRepositoryPath $resolvedDependencyHealthProviderManifestPath
    }
    packages = @($packageEvidence | Sort-Object package)
    drift = @($drift)
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$report | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $resolvedOutputPath -Encoding UTF8

Write-Host "Surface maturity report: $($report.status)"
Write-Host "Packages: $($packageEvidence.Count); M0=$($maturityCounts.M0), M1=$($maturityCounts.M1), M2=$($maturityCounts.M2), M3=$($maturityCounts.M3), M4=$($maturityCounts.M4); drift=$($drift.Count)."
Write-Host "Report: $resolvedOutputPath"

if ($FailOnDrift -and $drift.Count -gt 0) {
    $summary = ($drift | ForEach-Object { "[$($_.code)] $($_.package): $($_.message)" }) -join [Environment]::NewLine
    throw "Surface maturity drift detected:$([Environment]::NewLine)$summary"
}
