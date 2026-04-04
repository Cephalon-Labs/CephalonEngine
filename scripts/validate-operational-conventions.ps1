param(
    [string]$Configuration = "Release",
    [switch]$NoBuild,
    [string[]]$TestFilters = @(
        "FullyQualifiedName~Cephalon.Tests.Hosting.AspNetCoreHostingTests.MapCephalonExposesDependencyHealthAcrossDiagnosticsAndReadiness",
        "FullyQualifiedName~Cephalon.Tests.Hosting.AspNetCoreHostingTests.MapCephalonExposesCapturedStartupFailuresWhenPolicyDoesNotFailFast",
        "FullyQualifiedName~Cephalon.Tests.Hosting.AspNetCoreHostingTests.MapCephalonKeepsReadinessUnhealthyDuringConfiguredStartupWarmup",
        "FullyQualifiedName~Cephalon.Tests.Hosting.WorkerHostingTests.AddCephalonSurfacesDependencyHealthWithinGenericHost",
        "FullyQualifiedName~Cephalon.Tests.Hosting.ObservabilityHostingTests.AddCephalonObservabilityLogsManifestSummaryAndRuntimeSurface",
        "FullyQualifiedName~Cephalon.Tests.Hosting.ObservabilityHostingTests.AddCephalonObservabilityLogsSelfHostedTelemetryDefaultEndpointWhenEnabled",
        "FullyQualifiedName~Cephalon.Tests.Hosting.SerilogHostingTests.AddCephalonSerilogReadsTheStandardSerilogConfigurationSection",
        "FullyQualifiedName~Cephalon.Tests.Hosting.AlibabaCloudHostingTests.AddCephalonAlibabaCloudRegistersWhenManagedGrpcIngestionIsEnabled",
        "FullyQualifiedName~Cephalon.Tests.Hosting.AlibabaCloudHostingTests.AddCephalonAlibabaCloudRegistersWhenManagedHttpIngestionIsEnabled",
        "FullyQualifiedName~Cephalon.Tests.Hosting.AlibabaCloudHostingTests.AddCephalonAlibabaCloudLogsHostedPlatformSummaryWhenConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.AwsHostingTests.AddCephalonAwsRegistersWhenEndpointIsConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.AwsHostingTests.AddCephalonAwsLogsHostedPlatformSummaryWhenConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.AzureMonitorHostingTests.AddCephalonAzureMonitorRegistersWhenConnectionStringIsConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.AzureMonitorHostingTests.AddCephalonAzureMonitorLogsHostedPlatformSummaryWhenConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.DigitalOceanHostingTests.AddCephalonDigitalOceanRegistersWhenInClusterCollectorServiceIsConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.DigitalOceanHostingTests.AddCephalonDigitalOceanLogsHostedPlatformSummaryWhenConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.GrafanaCloudHostingTests.AddCephalonGrafanaCloudRegistersWhenDirectEndpointAndHeadersAreConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.GrafanaCloudHostingTests.AddCephalonGrafanaCloudLogsSummaryWhenAccessPolicyAuthenticationIsConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.NewRelicHostingTests.AddCephalonNewRelicRegistersWhenDirectEndpointAndLicenseKeyAreConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.NewRelicHostingTests.AddCephalonNewRelicLogsSummaryWhenLicenseKeyAuthenticationIsConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.GcpHostingTests.AddCephalonGcpRegistersWhenGoogleManagedIngestionIsEnabled",
        "FullyQualifiedName~Cephalon.Tests.Hosting.GcpHostingTests.AddCephalonGcpLogsHostedPlatformSummaryWhenConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.HuaweiCloudHostingTests.AddCephalonHuaweiCloudRegistersWhenManagedTraceIngestionIsEnabled",
        "FullyQualifiedName~Cephalon.Tests.Hosting.HuaweiCloudHostingTests.AddCephalonHuaweiCloudLogsHostedPlatformSummaryWhenConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.OracleCloudHostingTests.AddCephalonOracleCloudRegistersWhenManagedHttpIngestionIsEnabled",
        "FullyQualifiedName~Cephalon.Tests.Hosting.OracleCloudHostingTests.AddCephalonOracleCloudLogsHostedPlatformSummaryWhenConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.KubernetesHostingTests.AddCephalonKubernetesRegistersWhenInClusterCollectorServiceIsConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.KubernetesHostingTests.AddCephalonKubernetesLogsSummaryWhenConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.OpenShiftHostingTests.AddCephalonOpenShiftRegistersWhenInClusterCollectorServiceIsConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.OpenShiftHostingTests.AddCephalonOpenShiftLogsHostedPlatformSummaryWhenConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.TanzuHostingTests.AddCephalonTanzuRegistersWhenInClusterProxyServiceIsConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.TanzuHostingTests.AddCephalonTanzuLogsHostedPlatformSummaryWhenConfigured",
        "FullyQualifiedName~Cephalon.Tests.Hosting.OpenTelemetryHostingTests.AddCephalonOpenTelemetryExportsConfiguredSignalsOverHttpProtobuf",
        "FullyQualifiedName~Cephalon.Tests.Hosting.OpenTelemetryHostingTests.AddCephalonOpenTelemetryRegistersSelfHostedDefaultsWhenEndpointIsMissing"
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

Write-Host "Validating Cephalon operational health and telemetry-export conventions..." -ForegroundColor Cyan
Write-Host "Focused suite covers ASP.NET Core health routes, worker-host health parity, observability startup guidance, Serilog provider wiring, Alibaba Cloud managed ingestion, AWS hosted defaults, Azure Monitor exporter wiring, DigitalOcean collector defaults, Grafana Cloud direct endpoint wiring, New Relic native OTLP defaults, GCP managed-ingestion defaults, Huawei Cloud managed traces, Oracle Cloud managed traces and metrics, Kubernetes in-cluster collector defaults, OpenShift in-cluster collector defaults, Tanzu proxy trace handoff defaults, and OTLP exporter wiring." -ForegroundColor DarkCyan

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
