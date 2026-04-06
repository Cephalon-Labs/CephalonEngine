<#
.SYNOPSIS
    Scaffolds all artifacts needed when adding a new Cephalon.Observability.*Dependencies project.

.DESCRIPTION
    Creates the csproj file, a stub doc page, a catalog entry in docs/components/README.md,
    and a ProjectReference entry in the test project. Prints a manual-steps checklist at the end.

.PARAMETER ProviderName
    PascalCase provider name used in the project suffix (e.g. "ScyllaDB", "VictoriaMetrics").
    The project will be named Cephalon.Observability.<ProviderName>Dependencies.

.PARAMETER NuGetPackage
    The primary NuGet client package to reference for health/tracing (e.g. "SomeClient.Driver").

.PARAMETER Sprint
    Sprint number for the backlog entry reminder.

.EXAMPLE
    .\New-ObservabilityPack.ps1 -ProviderName ScyllaDB -NuGetPackage ScyllaDB.Driver -Sprint 31
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $ProviderName,

    [Parameter(Mandatory)]
    [string] $NuGetPackage,

    [Parameter(Mandatory)]
    [int] $Sprint
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot    = Split-Path -Parent $PSScriptRoot
$projectName = "Cephalon.Observability.${ProviderName}Dependencies"
$slug        = $ProviderName.ToLowerInvariant()
$docSlug     = "observability-$slug-dependencies"

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
$projectDir  = Join-Path $repoRoot "src" $projectName
$projectFile = Join-Path $projectDir "$projectName.csproj"
$docsDir     = Join-Path $repoRoot "docs" "components"
$docFile     = Join-Path $docsDir "$docSlug.md"
$catalogFile = Join-Path $docsDir "README.md"
$testsCsproj = Join-Path $repoRoot "tests" "Cephalon.Tests" "Cephalon.Tests.csproj"

# ---------------------------------------------------------------------------
# Guard
# ---------------------------------------------------------------------------
if (Test-Path $projectFile) {
    Write-Error "File already exists: $projectFile — aborting to avoid overwriting existing work."
}

# ---------------------------------------------------------------------------
# 1. <ProjectName>.csproj
# ---------------------------------------------------------------------------
New-Item -ItemType Directory -Path $projectDir -Force | Out-Null

$csproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Description>$ProviderName dependency health integration for Cephalon hosts.</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" />
    <PackageReference Include="$NuGetPackage" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Cephalon.Abstractions\Cephalon.Abstractions.csproj" />
    <ProjectReference Include="..\Cephalon.Engine\Cephalon.Engine.csproj" />
  </ItemGroup>
</Project>
"@

Set-Content -Path $projectFile -Value $csproj -Encoding utf8NoBOM
Write-Host "Created: $projectFile"

# ---------------------------------------------------------------------------
# 2. Stub doc page
# ---------------------------------------------------------------------------
$doc = @"
# Cephalon.Observability.${ProviderName}Dependencies

`Cephalon.Observability.${ProviderName}Dependencies` integrates $ProviderName health and dependency telemetry into Cephalon hosts.

## What it owns

<!-- TODO: describe health checks, OpenTelemetry instrumentation, and any tracing enrichment registered -->

## Main surfaces

<!-- TODO: list key files once implemented -->

## How it fits

This pack wires $ProviderName health and instrumentation into the Cephalon observability surface via ``Cephalon.Observability`` and ``Cephalon.Engine``.

## Configuration

<!-- TODO: document any options or extension methods -->
"@

Set-Content -Path $docFile -Value $doc -Encoding utf8NoBOM
Write-Host "Created: $docFile"

# ---------------------------------------------------------------------------
# 3. Catalog entry in docs/components/README.md
# ---------------------------------------------------------------------------
$catalogContent = Get-Content -Path $catalogFile -Raw

$entry = "- [Cephalon.Observability.${ProviderName}Dependencies]($docSlug.md)"

if ($catalogContent -notlike "*${ProviderName}Dependencies*") {
    # Insert just before the ## Additional repo surfaces section
    $anchor = "`n## Additional repo surfaces"
    $catalogContent = $catalogContent.Replace($anchor, "`n$entry$anchor")
    Set-Content -Path $catalogFile -Value $catalogContent -Encoding utf8NoBOM -NoNewline
    Write-Host "Updated catalog: $catalogFile"
} else {
    Write-Warning "Catalog entry for $projectName already present — skipping."
}

# ---------------------------------------------------------------------------
# 4. ProjectReference in Cephalon.Tests.csproj
# ---------------------------------------------------------------------------
$testsContent = Get-Content -Path $testsCsproj -Raw

$ref = "    <ProjectReference Include=""..\..\src\$projectName\$projectName.csproj"" />"

if ($testsContent -notlike "*$projectName*") {
    $anchor = "    <ProjectReference Include=""..\..\src\Cephalon.Observability.Serilog\Cephalon.Observability.Serilog.csproj"" />"
    if ($testsContent.Contains($anchor)) {
        $testsContent = $testsContent.Replace($anchor, "$anchor`n$ref")
        Set-Content -Path $testsCsproj -Value $testsContent -Encoding utf8NoBOM -NoNewline
        Write-Host "Updated test project: $testsCsproj"
    } else {
        Write-Warning "Anchor not found in test csproj — please add the ProjectReference manually."
    }
} else {
    Write-Warning "ProjectReference for $projectName already present — skipping."
}

# ---------------------------------------------------------------------------
# 5. Manual steps checklist
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "============================================================"
Write-Host " MANUAL STEPS CHECKLIST"
Write-Host "============================================================"
Write-Host ""
Write-Host " [ ] Add to CephalonEngine.slnx (inside <Folder Name=""/src/"">):"
Write-Host "       <Project Path=""src/$projectName/$projectName.csproj"" />"
Write-Host ""
Write-Host " [ ] Add the $NuGetPackage version to Directory.Packages.props if not already present."
Write-Host ""
Write-Host " [ ] Implement $projectName:"
Write-Host "       - Registration/${ProviderName}DependenciesEngineBuilderExtensions.cs"
Write-Host "       - Health/${ProviderName}HealthCheck.cs (if applicable)"
Write-Host "       - Telemetry/${ProviderName}InstrumentationContributor.cs (if applicable)"
Write-Host ""
Write-Host " [ ] Add composition/integration tests in Cephalon.Tests."
Write-Host ""
Write-Host " [ ] Fill in the stub doc file: docs/components/$docSlug.md"
Write-Host ""
Write-Host " [ ] Update observability.slnf to include the new project."
Write-Host ""
Write-Host " [ ] If DocumentationCoverageTests has NOT yet been refactored to be"
Write-Host "     convention-based, add an entry for $docSlug.md to the"
Write-Host "     hardcoded dictionary manually."
Write-Host ""
Write-Host " [ ] Add a Sprint $Sprint entry to docs/engine-backlog.md."
Write-Host ""
Write-Host "============================================================"
