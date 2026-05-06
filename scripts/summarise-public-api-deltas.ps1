<#
.SYNOPSIS
    Produces a release-notes-friendly markdown summary of every Cephalon.* package's pending
    PublicAPI.Unshipped.txt entries, grouped by package and additive-vs-removal kind, with
    an optional release gate for removal entries.

.DESCRIPTION
    Cephalon's contract lock-in arc (ENG-322 through ENG-345) added Microsoft.CodeAnalysis
    .PublicApiAnalyzers + per-project PublicAPI.Shipped.txt + PublicAPI.Unshipped.txt artefacts
    to shipped packages. Public-API additions and removals land in PublicAPI.Unshipped
    .txt first; on a release they graduate to PublicAPI.Shipped.txt. This script walks every
    Cephalon.*/PublicAPI.Unshipped.txt file under the repo, parses the entries, and emits a
    markdown report so PR reviewers and release managers can read the API delta in human form
    instead of squinting at 101 Unshipped.txt files.

    Each entry in Unshipped.txt is one of:
      - `#nullable enable` header (skipped by the report)
      - an additive line: a fully-qualified symbol signature (added in this release)
      - a removal line: starts with `*REMOVED*` then the previously-shipped signature (removed
        in this release)
      - a blank line (skipped)

    The report groups entries by package, then splits additions vs removals, and emits a count
    per package plus a total at the top so a release manager can see the contract delta size at
    a glance. The output is written to stdout by default; use -OutputPath to write to a file.

.PARAMETER RepoRoot
    Repository root. Defaults to the parent of the script directory so the script can be
    invoked from anywhere.

.PARAMETER OutputPath
    Optional file path to write the markdown report. When omitted the report is written to
    stdout.

.PARAMETER IncludeHeaderless
    Include packages whose Unshipped.txt is header-only (no real entries) in the output as
    "no pending API changes" rows. Default: false (header-only files are skipped to keep the
    report focused on actual deltas).

.PARAMETER FailOnRemovals
    Fail with a non-zero exit after writing the report when any `*REMOVED*` entry is found.
    Release validation uses this switch so binary-breaking public API removals cannot pass as
    report-only evidence.

.EXAMPLE
    pwsh ./scripts/summarise-public-api-deltas.ps1

    Print the markdown report to stdout for the current repo state.

.EXAMPLE
    pwsh ./scripts/summarise-public-api-deltas.ps1 -OutputPath artifacts/public-api-delta.md

    Write the report to `artifacts/public-api-delta.md` for use as a release-notes input.

.EXAMPLE
    pwsh ./scripts/summarise-public-api-deltas.ps1 -IncludeHeaderless

    Include rows for packages with no pending changes too, for a complete inventory.

.EXAMPLE
    pwsh ./scripts/summarise-public-api-deltas.ps1 -OutputPath artifacts/public-api-delta.md -FailOnRemovals

    Write the report and fail the command if the pending public API delta contains removals.
#>
param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = '',
    [switch]$IncludeHeaderless,
    [switch]$FailOnRemovals
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $RepoRoot)) {
    throw "Repo root not found: $RepoRoot"
}

$srcRoot = Join-Path $RepoRoot 'src'
if (-not (Test-Path -LiteralPath $srcRoot)) {
    throw "src/ folder not found under repo root: $srcRoot"
}

$entries = @()
$packagesWithEntries = 0
$packagesHeaderOnly = 0
$totalAdditions = 0
$totalRemovals = 0

$unshippedFiles = Get-ChildItem -Path $srcRoot -Filter 'PublicAPI.Unshipped.txt' -File -Recurse |
    Sort-Object FullName

foreach ($file in $unshippedFiles) {
    $packageDir = $file.Directory
    $packageId = $packageDir.Name
    $relative = [System.IO.Path]::GetRelativePath($RepoRoot, $file.FullName)

    $lines = Get-Content -LiteralPath $file.FullName
    $additions = @()
    $removals = @()

    foreach ($line in $lines) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed)) { continue }
        if ($trimmed.StartsWith('#')) { continue }

        if ($trimmed.StartsWith('*REMOVED*')) {
            $removals += $trimmed.Substring('*REMOVED*'.Length).TrimStart()
        }
        else {
            $additions += $trimmed
        }
    }

    if ($additions.Count -eq 0 -and $removals.Count -eq 0) {
        $packagesHeaderOnly++
        if ($IncludeHeaderless) {
            $entries += [pscustomobject]@{
                PackageId = $packageId
                Path      = $relative
                Additions = @()
                Removals  = @()
            }
        }
        continue
    }

    $packagesWithEntries++
    $totalAdditions += $additions.Count
    $totalRemovals += $removals.Count
    $entries += [pscustomobject]@{
        PackageId = $packageId
        Path      = $relative
        Additions = $additions
        Removals  = $removals
    }
}

$lines = @()
$lines += '# Cephalon public-API delta summary'
$lines += ''
$lines += ('Generated: {0}' -f ([System.DateTimeOffset]::UtcNow.ToString('o')))
$lines += ('Repo root: `{0}`' -f $RepoRoot)
$lines += ''
$lines += '## Aggregate'
$lines += ''
$lines += ('- Packages with pending API changes: **{0}**' -f $packagesWithEntries)
$lines += ('- Packages with header-only Unshipped.txt (no pending changes): {0}' -f $packagesHeaderOnly)
$lines += ('- Total additive entries: **{0}**' -f $totalAdditions)
$lines += ('- Total removal entries: **{0}**' -f $totalRemovals)
$lines += ''

if ($entries.Count -eq 0) {
    $lines += 'No pending API changes across the engine. Run with `-IncludeHeaderless` to list packages anyway.'
}
else {
    $lines += '## Per-package detail'
    $lines += ''

    foreach ($entry in $entries) {
        $lines += ('### `{0}`' -f $entry.PackageId)
        $lines += ''
        $lines += ('Source: `{0}`' -f $entry.Path)
        $lines += ''

        if ($entry.Additions.Count -gt 0) {
            $lines += ('**Additions ({0}):**' -f $entry.Additions.Count)
            $lines += ''
            $lines += '```'
            foreach ($a in $entry.Additions) { $lines += $a }
            $lines += '```'
            $lines += ''
        }

        if ($entry.Removals.Count -gt 0) {
            $lines += ('**Removals ({0}):**' -f $entry.Removals.Count)
            $lines += ''
            $lines += '```'
            foreach ($r in $entry.Removals) { $lines += '*REMOVED* ' + $r }
            $lines += '```'
            $lines += ''
        }

        if ($entry.Additions.Count -eq 0 -and $entry.Removals.Count -eq 0) {
            $lines += '_No pending API changes._'
            $lines += ''
        }
    }
}

$report = $lines -join [System.Environment]::NewLine

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    Write-Output $report
}
else {
    if (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
        $OutputPath = Join-Path $RepoRoot $OutputPath
    }
    $outDir = Split-Path -Parent $OutputPath
    if (-not [string]::IsNullOrWhiteSpace($outDir) -and -not (Test-Path -LiteralPath $outDir)) {
        New-Item -Path $outDir -ItemType Directory -Force | Out-Null
    }
    Set-Content -LiteralPath $OutputPath -Value $report -Encoding UTF8
    Write-Host ('Wrote public-API delta summary to {0}' -f $OutputPath)
    Write-Host ('  Packages with pending API changes: {0}' -f $packagesWithEntries)
    Write-Host ('  Total additive entries: {0}' -f $totalAdditions)
    Write-Host ('  Total removal entries: {0}' -f $totalRemovals)
}

if ($FailOnRemovals -and $totalRemovals -gt 0) {
    $packagesWithRemovals = @(
        $entries |
            Where-Object { $_.Removals.Count -gt 0 } |
            Select-Object -ExpandProperty PackageId
    )
    $packageList = $packagesWithRemovals -join ', '
    throw ('Public API removal entries detected: {0} removal(s) across {1} package(s): {2}. Release validation fails on removals; keep deprecation, obsoletion, and compatibility approval visible before shipping.' -f `
        $totalRemovals,
        $packagesWithRemovals.Count,
        $packageList)
}
