[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$NoBuild,
    [string[]]$TestFilters = @(
        "FullyQualifiedName~Cephalon.Tests.Composition.EngineSettingsTests",
        "FullyQualifiedName~BuildIncludesPhase8SelectionsInTheResolvedAppProfile",
        "FullyQualifiedName~BuildThrowsWhenReadWriteSplitIsConfiguredWithoutCqrsPattern",
        "FullyQualifiedName~BuildThrowsWhenMessagingProviderIsConfiguredWithoutEventDrivenTechnology",
        "FullyQualifiedName~BuildThrowsWhenIdentityUsesUnsupportedAuthorizationMode",
        "FullyQualifiedName~BuildExposesPhase8ProjectionInboxOutboxAndAuthorizationCatalogsThroughRuntimeSnapshot",
        "FullyQualifiedName~AddTechnologyPacksRegisterServicesAndCapabilitiesWhenSelectionsAreActive",
        "FullyQualifiedName~AddTechnologyPacksRejectHostedExecutionsThatReferenceUnknownEventSubscriptions",
        "FullyQualifiedName~AddTechnologyPacksStayDormantWhenSelectionsAreInactive",
        "FullyQualifiedName~AddTechnologyPacksRejectSubscriptionsThatReferenceUnknownChannels",
        "FullyQualifiedName~AddTechnologyPacksCanReportSubscriptionRuntimeStateWithoutHostedExecutionLink",
        "FullyQualifiedName~AddTechnologyPacksRejectSubscriptionRuntimeReportsForUnknownSubscriptions",
        "FullyQualifiedName~Cephalon.Tests.Composition.Phase8ContractTests",
        "FullyQualifiedName~Cephalon.Tests.Composition.DataRuntimePackTests",
        "FullyQualifiedName~Cephalon.Tests.Composition.EntityFrameworkDataPackTests",
        "FullyQualifiedName~Cephalon.Tests.Composition.SfidIdPackTests",
        "FullyQualifiedName~Cephalon.Tests.Composition.WolverineEventingPackTests",
        "FullyQualifiedName~Cephalon.Tests.Composition.IdentityPackTests",
        "FullyQualifiedName~Cephalon.Tests.Composition.MultiTenancyPackTests",
        "FullyQualifiedName~Cephalon.Tests.Composition.AuditPackTests",
        "FullyQualifiedName~Cephalon.Tests.Hosting.IdentityAspNetCoreHostingTests",
        "FullyQualifiedName~MapCephalonExposesTechnologyRuntimeSurfaces",
        "FullyQualifiedName~MapCephalonExposesPhase8ProjectionInboxOutboxAndAuthorizationCatalogs",
        "FullyQualifiedName~Cephalon.Tests.Hosting.SampleSuiteHostingTests",
        "FullyQualifiedName~Cephalon.Tests.Scaffolding.ScaffoldGeneratorTests",
        "FullyQualifiedName~Cephalon.Tests.Tooling.CliApplicationTests",
        "FullyQualifiedName~Cephalon.Tests.Tooling.TemplatePackTests",
        "FullyQualifiedName~Cephalon.Tests.Tooling.DocumentationCoverageTests",
        "FullyQualifiedName~AbstractionsAssemblyExposesOnlyTheDocumentedContractSurface",
        "FullyQualifiedName~EventingAssemblyExposesOnlyTheDocumentedPackContracts",
        "FullyQualifiedName~WolverineEventingAssemblyExposesOnlyTheDocumentedPackContracts",
        "FullyQualifiedName~DataAssemblyExposesOnlyTheDocumentedPackContracts",
        "FullyQualifiedName~SfidIdsAssemblyExposesOnlyTheDocumentedPackContracts",
        "FullyQualifiedName~IdentityAssemblyExposesOnlyTheDocumentedPackContracts",
        "FullyQualifiedName~IdentityAspNetCoreAssemblyExposesOnlyTheDocumentedHostContracts",
        "FullyQualifiedName~MultiTenancyAssemblyExposesOnlyTheDocumentedPackContracts",
        "FullyQualifiedName~AuditAssemblyExposesOnlyTheDocumentedPackContracts",
        "FullyQualifiedName~GenerateBuildsPageForAbstractionsAssemblyWithPhase8Contracts",
        "FullyQualifiedName~GenerateBuildsPageForEngineAssemblyWithPhase8RuntimeSnapshotMembers",
        "FullyQualifiedName~GenerateBuildsPageForSfidIdsAssembly",
        "FullyQualifiedName~GenerateBuildsPageForDataAssembly",
        "FullyQualifiedName~GenerateBuildsPageForDataEntityFrameworkAssembly",
        "FullyQualifiedName~GenerateBuildsPageForEventingAssembly",
        "FullyQualifiedName~GenerateBuildsPageForWolverineEventingAssembly",
        "FullyQualifiedName~GenerateBuildsPageForIdentityAssembly",
        "FullyQualifiedName~GenerateBuildsPageForIdentityAspNetCoreAssembly",
        "FullyQualifiedName~GenerateBuildsPageForMultiTenancyAssembly",
        "FullyQualifiedName~GenerateBuildsPageForAuditAssembly"
    )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testsProjectPath = [System.IO.Path]::Combine($repoRoot, "tests", "Cephalon.Tests", "Cephalon.Tests.csproj")

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed: dotnet $($Arguments -join ' ')"
    }
}

$filterExpression = $TestFilters -join "|"

Write-Host "Validating Cephalon phase-8 architecture, runtime, and starter conventions..." -ForegroundColor Cyan
Write-Host "Focused suite covers phase-8 settings and app-profile truth, host-agnostic contracts, runtime catalogs and surfaces, relational data and Sfid integration, eventing and Wolverine follow-through, identity and multi-tenancy baselines, audit surfaces, ASP.NET Core adapter behavior, low-ceremony starter output, and package/reference-doc/documentation truth." -ForegroundColor DarkCyan

Push-Location $repoRoot
try {
    $arguments = @(
        "test",
        $testsProjectPath,
        "-c",
        $Configuration,
        "--filter",
        $filterExpression)

    if ($NoBuild) {
        $arguments += "--no-build"
    }

    Invoke-DotNet $arguments

    Write-Host ""
    Write-Host "Phase-8 convention validation completed successfully." -ForegroundColor Green
}
finally {
    Pop-Location
}
