param(
    [string]$Configuration = "Release",
    [switch]$NoBuild,
    [string[]]$TestFilters = @(
        "FullyQualifiedName~Cephalon.Tests.Hosting.AspNetCoreHostingTests.MapCephalonExposesDependencyHealthAcrossDiagnosticsAndReadiness",
        "FullyQualifiedName~Cephalon.Tests.Hosting.AspNetCoreHostingTests.MapCephalonExposesCapturedStartupFailuresWhenPolicyDoesNotFailFast",
        "FullyQualifiedName~Cephalon.Tests.Hosting.AspNetCoreHostingTests.MapCephalonKeepsReadinessUnhealthyDuringConfiguredStartupWarmup",
        "FullyQualifiedName~Cephalon.Tests.Hosting.WorkerHostingTests.AddCephalonSurfacesDependencyHealthWithinGenericHost",
        "FullyQualifiedName~Cephalon.Tests.Hosting.ObservabilityHostingTests.AddCephalonObservabilityLogsManifestSummaryAndRuntimeSurface",
        "FullyQualifiedName~Cephalon.Tests.Hosting.OpenTelemetryHostingTests.AddCephalonOpenTelemetryExportsConfiguredSignalsOverHttpProtobuf"
    )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testsProjectPath = Join-Path $repoRoot "tests\Cephalon.Tests\Cephalon.Tests.csproj"

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

Write-Host "Validating Cephalon operational health and telemetry-export conventions..." -ForegroundColor Cyan
Write-Host "Focused suite covers ASP.NET Core health routes, worker-host health parity, observability startup guidance, and OTLP exporter wiring." -ForegroundColor DarkCyan

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
    Write-Host "Operational convention validation completed successfully." -ForegroundColor Green
}
finally {
    Pop-Location
}
