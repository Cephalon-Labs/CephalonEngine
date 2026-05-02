<#
.SYNOPSIS
    Injects Microsoft.CodeAnalysis.PublicApiAnalyzers + PublicAPI.Shipped/Unshipped placeholder
    files + RS0026 / RS0027 NoWarn into one or more Cephalon.* csproj files.

.DESCRIPTION
    The Cephalon engine tracks the public API surface of every shipped package as a reviewable
    artefact via Microsoft.CodeAnalysis.PublicApiAnalyzers and per-project PublicAPI.Shipped.txt
    plus PublicAPI.Unshipped.txt files. The contract lock-in arc that ran from ENG-322 through
    ENG-345 added these artefacts to all 101 shipped packages. This script automates the per-
    project edits so adding a new Cephalon.* package to the rollout (or onboarding a new
    contributor's first package) is a single command instead of hand-editing the csproj.

    For each project this script:
      1. Adds <PackageReference Include="Microsoft.CodeAnalysis.PublicApiAnalyzers" /> with
         PrivateAssets=all and IncludeAssets="runtime; build; native; contentfiles; analyzers;
         buildtransitive" so the analyzer flows into the build but does not transitively flow to
         consumers.
      2. Declares PublicAPI.Shipped.txt and PublicAPI.Unshipped.txt as <AdditionalFiles> so the
         analyzer reads them as the project's declared public-API contract.
      3. Adds RS0026 (multiple-overloads-with-optional-parameters) and RS0027 (param-count
         hierarchy) to the project's <NoWarn> list with an inline comment naming the ENG-* card
         that did the rollout, since both rules flag pre-existing optional-overload patterns
         the engine has not yet refactored.
      4. Creates `#nullable enable` placeholder PublicAPI.Shipped.txt and PublicAPI.Unshipped.txt
         files when they do not already exist.

    After running this script, the operator should run a clean `dotnet build` against the
    project and capture the reported `error RS0016` symbols into PublicAPI.Shipped.txt to seed
    the baseline. See `docs/engineering-standards.md` for the full pattern. Three known
    edge-case projects (Cephalon.Cli vs Cephalon.Scaffolding, the dependency-health probe packs
    vs Cephalon.Observability.DependencyHealth.Core) require resetting the baseline once before
    re-extracting, because transitively visible types from the referenced project show up in
    the initial RS0016 set; if the rebuild reports RS0017 ("Symbol is part of declared API but
    is either not public or could not be found"), reset the .Shipped.txt to header-only and
    re-extract.

.PARAMETER Projects
    One or more `Cephalon.*` package suffixes (e.g. "Data", "Behaviors.Patterns", "Observability.OpenTelemetry"),
    relative to `src/Cephalon.`. Pass multiple project suffixes as a PowerShell array; the
    script tolerates already-injected projects and prints a "skipped" message rather than
    failing.

.PARAMETER EngId
    The ENG-* identifier responsible for the rollout slice; appears inside the inline comment
    for the RS0026 / RS0027 suppression so future contributors can find the planning context.

.EXAMPLE
    pwsh ./scripts/inject-public-api-analyzers.ps1 -EngId 'ENG-XYZ' -Projects @('NewPackage')

    Adds the analyzer + placeholders to `src/Cephalon.NewPackage/Cephalon.NewPackage.csproj`.
#>
param(
    [Parameter(Mandatory)] [string[]]$Projects,
    [Parameter(Mandatory)] [string]$EngId
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

foreach ($p in $Projects) {
    $path = "src/Cephalon.$p/Cephalon.$p.csproj"
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Warning "$p - csproj not found at $path; skipping"
        continue
    }

    $content = Get-Content -LiteralPath $path -Raw

    if ($content -match 'Microsoft\.CodeAnalysis\.PublicApiAnalyzers') {
        Write-Host "$p - already references PublicApiAnalyzers directly; skipping"
        continue
    }

    if ($content -match 'Cephalon\.Analyzers\.csproj') {
        Write-Host "$p - already adopts the Cephalon.Analyzers meta-package (which transitively flows PublicApiAnalyzers); skipping"
        continue
    }

    if ($content -notmatch 'RS0026') {
        $rx = [regex]::new('\s*</PropertyGroup>')
        $line = "`r`n    <!-- RS0026/RS0027: optional-overload patterns; suppressed for $EngId. -->`r`n    <NoWarn>`$(NoWarn);RS0026;RS0027</NoWarn>`r`n  </PropertyGroup>"
        $content = $rx.Replace($content, $line, 1)
    }

    $injection = @"

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.PublicApiAnalyzers">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <AdditionalFiles Include="PublicAPI.Shipped.txt" />
    <AdditionalFiles Include="PublicAPI.Unshipped.txt" />
  </ItemGroup>
</Project>
"@
    $content = $content -replace '</Project>\s*$', $injection.TrimStart()
    Set-Content -LiteralPath $path -Value $content -NoNewline -Encoding UTF8

    $shipped = "src/Cephalon.$p/PublicAPI.Shipped.txt"
    $unshipped = "src/Cephalon.$p/PublicAPI.Unshipped.txt"
    if (-not (Test-Path -LiteralPath $shipped)) {
        Set-Content -LiteralPath $shipped -Value "#nullable enable`r`n" -NoNewline -Encoding UTF8
    }
    if (-not (Test-Path -LiteralPath $unshipped)) {
        Set-Content -LiteralPath $unshipped -Value "#nullable enable`r`n" -NoNewline -Encoding UTF8
    }

    Write-Host "$p - csproj edited; PublicAPI.Shipped.txt and PublicAPI.Unshipped.txt seeded"
}

Write-Host ''
Write-Host 'Next step: run `dotnet build src/Cephalon.<Project>/Cephalon.<Project>.csproj -c Release` and capture the'
Write-Host 'reported `error RS0016` symbols into PublicAPI.Shipped.txt. See docs/engineering-standards.md for'
Write-Host 'the extraction pattern and known transitive-deps edge cases.'
