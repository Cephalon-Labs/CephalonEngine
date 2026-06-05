<#
.SYNOPSIS
    Scaffolds all artifacts needed when adding a new non-relational data provider companion pack.

.DESCRIPTION
    Creates the two csproj files, stub doc pages, catalog entries in docs/components/README.md,
    and ProjectReference entries in the test project. Prints a manual-steps checklist at the end.

.PARAMETER ProviderName
    PascalCase provider name (e.g. "Elasticsearch", "DynamoDB", "Couchbase").

.PARAMETER Family
    Short family label used in descriptions (e.g. "search", "document", "wide-column").

.PARAMETER NuGetPackage
    The primary NuGet client package to reference (e.g. "Elastic.Clients.Elasticsearch").

.PARAMETER Sprint
    Sprint number for the backlog entry reminder.

.EXAMPLE
    .\New-ProviderPack.ps1 -ProviderName Elasticsearch -Family search -NuGetPackage Elastic.Clients.Elasticsearch -Sprint 30
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $ProviderName,

    [Parameter(Mandatory)]
    [string] $Family,

    [Parameter(Mandatory)]
    [string] $NuGetPackage,

    [Parameter(Mandatory)]
    [int] $Sprint
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$slug = $ProviderName.ToLowerInvariant()

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
$dataProjectDir  = Join-Path $repoRoot "src" "Cephalon.Data.$ProviderName"
$dataProjectFile = Join-Path $dataProjectDir "Cephalon.Data.$ProviderName.csproj"
$esProjectDir    = Join-Path $repoRoot "src" "Cephalon.EventSourcing.$ProviderName"
$esProjectFile   = Join-Path $esProjectDir "Cephalon.EventSourcing.$ProviderName.csproj"
$docsDir         = Join-Path $repoRoot "docs" "components"
$dataDocFile     = Join-Path $docsDir "data-$slug.md"
$esDocFile       = Join-Path $docsDir "event-sourcing-$slug.md"
$catalogFile     = Join-Path $docsDir "README.md"
$testsCsproj     = Join-Path $repoRoot "tests" "Cephalon.Tests.Support" "Cephalon.Tests.Support.csproj"

# ---------------------------------------------------------------------------
# Guard: abort if projects already exist
# ---------------------------------------------------------------------------
foreach ($path in @($dataProjectFile, $esProjectFile)) {
    if (Test-Path $path) {
        Write-Error "File already exists: $path — aborting to avoid overwriting existing work."
    }
}

# ---------------------------------------------------------------------------
# 1. Cephalon.Data.<ProviderName>.csproj
# ---------------------------------------------------------------------------
New-Item -ItemType Directory -Path $dataProjectDir -Force | Out-Null

$dataCsproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <LangVersion>latest</LangVersion>
    <Description>$ProviderName $Family data companion package for the Cephalon engine.</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="$NuGetPackage" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Cephalon.Data\Cephalon.Data.csproj" />
    <ProjectReference Include="..\Cephalon.Engine\Cephalon.Engine.csproj" />
  </ItemGroup>
</Project>
"@

Set-Content -Path $dataProjectFile -Value $dataCsproj -Encoding utf8NoBOM
Write-Host "Created: $dataProjectFile"

# ---------------------------------------------------------------------------
# 2. Cephalon.EventSourcing.<ProviderName>.csproj
# ---------------------------------------------------------------------------
New-Item -ItemType Directory -Path $esProjectDir -Force | Out-Null

$esCsproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <LangVersion>latest</LangVersion>
    <Description>$ProviderName event-store provider for the Cephalon event-sourcing companion pack.</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="$NuGetPackage" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Cephalon.EventSourcing\Cephalon.EventSourcing.csproj" />
    <ProjectReference Include="..\Cephalon.Engine\Cephalon.Engine.csproj" />
  </ItemGroup>
</Project>
"@

Set-Content -Path $esProjectFile -Value $esCsproj -Encoding utf8NoBOM
Write-Host "Created: $esProjectFile"

# ---------------------------------------------------------------------------
# 3. Stub doc: docs/components/data-<slug>.md
# ---------------------------------------------------------------------------
$dataDoc = @"
# Cephalon.Data.$ProviderName

`Cephalon.Data.$ProviderName` is the $ProviderName $family data companion pack for Cephalon.

## What it owns

<!-- TODO: describe IOutbox and IInbox implementations, idempotency strategy, capabilities published -->

## Main surfaces

<!-- TODO: list key files once implemented -->

## How it fits

This pack sits on top of ``Cephalon.Data``, not in place of it. ``Cephalon.Data`` still owns the runtime-neutral ``IReadStore`` / ``IWriteStore`` dispatching surface. ``Cephalon.Data.$ProviderName`` adds the $ProviderName-backed outbox and inbox persistence paths.

## Configuration

<!-- TODO: document options class and connection string setup -->

## Tests

<!-- TODO: document how integration tests are run -->
"@

Set-Content -Path $dataDocFile -Value $dataDoc -Encoding utf8NoBOM
Write-Host "Created: $dataDocFile"

# ---------------------------------------------------------------------------
# 4. Stub doc: docs/components/event-sourcing-<slug>.md
# ---------------------------------------------------------------------------
$esDoc = @"
# Cephalon.EventSourcing.$ProviderName

`Cephalon.EventSourcing.$ProviderName` is the $ProviderName event-store provider for the Cephalon event-sourcing companion pack.

## What it owns

<!-- TODO: describe IEventStore implementation, concurrency strategy, serialization approach -->

## Main surfaces

<!-- TODO: list key files once implemented -->

## How it fits

This pack sits on top of ``Cephalon.EventSourcing`` and wires a $ProviderName-backed ``IEventStore`` into the Cephalon runtime.

## Configuration

<!-- TODO: document options class and connection string setup -->

## Tests

<!-- TODO: document how integration tests are run -->
"@

Set-Content -Path $esDocFile -Value $esDoc -Encoding utf8NoBOM
Write-Host "Created: $esDocFile"

# ---------------------------------------------------------------------------
# 5. Catalog entries in docs/components/README.md
# ---------------------------------------------------------------------------
$catalogContent = Get-Content -Path $catalogFile -Raw

$dataEntry = "- [Cephalon.Data.$ProviderName](data-$slug.md)"
$esEntry   = "- [Cephalon.EventSourcing.$ProviderName](event-sourcing-$slug.md)"

# Insert after the "## Phase 10 companion packs" block header (before the next ##)
# Strategy: append to the Phase 10 block
$phase10Header = '## Phase 10 companion packs'
if ($catalogContent -notcontains $dataEntry -and $catalogContent.Contains($phase10Header)) {
    # Find insertion point: just before the next ## section after Phase 10
    $insertBefore = "`n## Phase 8 companion packs"
    $newEntries   = "`n$dataEntry`n$esEntry"
    $catalogContent = $catalogContent.Replace($insertBefore, "$newEntries$insertBefore")
    Set-Content -Path $catalogFile -Value $catalogContent -Encoding utf8NoBOM -NoNewline
    Write-Host "Updated catalog: $catalogFile"
} else {
    Write-Warning "Could not auto-insert catalog entries — please add them manually to $catalogFile"
}

# ---------------------------------------------------------------------------
# 6. ProjectReference entries in Cephalon.Tests.Support.csproj
# ---------------------------------------------------------------------------
$testsContent = Get-Content -Path $testsCsproj -Raw

$dataRef = "    <ProjectReference Include=""..\..\..\src\Cephalon.Data.$ProviderName\Cephalon.Data.$ProviderName.csproj"" />"
$esRef   = "    <ProjectReference Include=""..\..\..\src\Cephalon.EventSourcing.$ProviderName\Cephalon.EventSourcing.$ProviderName.csproj"" />"

# The support test csproj uses relative paths from tests/Cephalon.Tests.Support/ so prefix is ..\..\src
$dataRef = "    <ProjectReference Include=""..\..\src\Cephalon.Data.$ProviderName\Cephalon.Data.$ProviderName.csproj"" />"
$esRef   = "    <ProjectReference Include=""..\..\src\Cephalon.EventSourcing.$ProviderName\Cephalon.EventSourcing.$ProviderName.csproj"" />"

if ($testsContent -notlike "*Cephalon.Data.$ProviderName*") {
    # Insert before the closing </ItemGroup> that contains the last ProjectReference block
    $anchor = "    <ProjectReference Include=""..\..\src\Cephalon.Retrieval\Cephalon.Retrieval.csproj"" />"
    if ($testsContent.Contains($anchor)) {
        $testsContent = $testsContent.Replace($anchor, "$dataRef`n$esRef`n$anchor")
        Set-Content -Path $testsCsproj -Value $testsContent -Encoding utf8NoBOM -NoNewline
        Write-Host "Updated test project: $testsCsproj"
    } else {
        Write-Warning "Anchor not found in test csproj — please add ProjectReferences manually."
    }
} else {
    Write-Warning "ProjectReference for Cephalon.Data.$ProviderName already present in test project — skipping."
}

# ---------------------------------------------------------------------------
# 7. Remind: add to CephalonEngine.slnx
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "============================================================"
Write-Host " MANUAL STEPS CHECKLIST"
Write-Host "============================================================"
Write-Host ""
Write-Host " [ ] Add to CephalonEngine.slnx (inside <Folder Name=""/src/"">):"
Write-Host "       <Project Path=""src/Cephalon.Data.$ProviderName/Cephalon.Data.$ProviderName.csproj"" />"
Write-Host "       <Project Path=""src/Cephalon.EventSourcing.$ProviderName/Cephalon.EventSourcing.$ProviderName.csproj"" />"
Write-Host ""
Write-Host " [ ] Add the $NuGetPackage version to Directory.Packages.props if not already present."
Write-Host ""
Write-Host " [ ] Implement Cephalon.Data.$ProviderName:"
Write-Host "       - Configuration/${ProviderName}DataOptions.cs"
Write-Host "       - Modules/${ProviderName}DataModule.cs"
Write-Host "       - Registration/${ProviderName}DataEngineBuilderExtensions.cs"
Write-Host "       - Services/${ProviderName}Outbox.cs (IOutbox)"
Write-Host "       - Services/${ProviderName}Inbox.cs (IInbox)"
Write-Host "       - Services/${ProviderName}OutboxRuntimeSurfaceContributor.cs"
Write-Host "       - Services/${ProviderName}InboxRuntimeSurfaceContributor.cs"
Write-Host ""
Write-Host " [ ] Implement Cephalon.EventSourcing.$ProviderName:"
Write-Host "       - Configuration/${ProviderName}EventStoreOptions.cs"
Write-Host "       - Modules/${ProviderName}EventSourcingModule.cs"
Write-Host "       - Registration/${ProviderName}EventSourcingEngineBuilderExtensions.cs"
Write-Host "       - Services/${ProviderName}EventStore.cs (IEventStore)"
Write-Host ""
Write-Host " [ ] Write integration tests in Cephalon.Tests.Composition and/or Cephalon.Tests.Hosting for both packs."
Write-Host ""
Write-Host " [ ] Fill in the stub doc files:"
Write-Host "       - docs/components/data-$slug.md"
Write-Host "       - docs/components/event-sourcing-$slug.md"
Write-Host ""
Write-Host " [ ] Update data.slnf to include the two new projects."
Write-Host ""
Write-Host " [ ] If DocumentationCoverageTests has NOT yet been refactored to be"
Write-Host "     convention-based, add entries for data-$slug.md and"
Write-Host "     event-sourcing-$slug.md to the hardcoded dictionary manually."
Write-Host ""
Write-Host " [ ] Add a Sprint $Sprint entry to docs/engine-backlog.md."
Write-Host ""
Write-Host "============================================================"
