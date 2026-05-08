#requires -Version 7.0

param(
    [ValidateSet("All", "Cassandra", "ClickHouse", "Elasticsearch", "Nats", "Neo4j", "OpenSearch", "Qdrant")]
    [string[]]$Providers = @("All"),
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipRestore,
    [switch]$NoBuild,
    [switch]$PreflightOnly,
    [string[]]$Logger = @("console;verbosity=normal"),
    [string]$ResultsDirectory = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$providerIntegrationProjectPath = [System.IO.Path]::Combine(
    $repoRoot,
    "tests",
    "Cephalon.Tests.ProviderIntegration",
    "Cephalon.Tests.ProviderIntegration.csproj")

function Get-ProviderLiveTestMatrix {
    [CmdletBinding()]
    param()

    return [ordered]@{
        Cassandra = [pscustomobject]@{
            Provider = "Cassandra"
            FilterToken = "CassandraProvider_StagesOutboxInboxAndDispatchAgainstLiveService"
            Runtime = "cassandra:4.1"
        }
        ClickHouse = [pscustomobject]@{
            Provider = "ClickHouse"
            FilterToken = "ClickHouseProvider_StagesOutboxAndInboxAgainstLiveService"
            Runtime = "clickhouse/clickhouse-server:24.8-alpine"
        }
        Elasticsearch = [pscustomobject]@{
            Provider = "Elasticsearch"
            FilterToken = "ElasticsearchProvider_StagesOutboxInboxAndDispatchAgainstLiveService"
            Runtime = "docker.elastic.co/elasticsearch/elasticsearch:8.17.0"
        }
        Nats = [pscustomobject]@{
            Provider = "Nats"
            FilterToken = "NatsProvider_StagesOutboxInboxAndDispatchAgainstLiveJetStream"
            Runtime = "nats:2.10-alpine -js"
        }
        Neo4j = [pscustomobject]@{
            Provider = "Neo4j"
            FilterToken = "Neo4jProvider_StagesOutboxInboxAndDispatchAgainstLiveService"
            Runtime = "neo4j:5-community"
        }
        OpenSearch = [pscustomobject]@{
            Provider = "OpenSearch"
            FilterToken = "OpenSearchProvider_StagesOutboxInboxAndDispatchAgainstLiveService"
            Runtime = "opensearchproject/opensearch:2.18.0"
        }
        Qdrant = [pscustomobject]@{
            Provider = "Qdrant"
            FilterToken = "QdrantProvider_StagesOutboxInboxAndDispatchAgainstLiveService"
            Runtime = "qdrant/qdrant:v1.12.5"
        }
    }
}

function Resolve-ProviderLiveTestSelection {
    [CmdletBinding()]
    param(
        [string[]]$ProviderNames = @("All")
    )

    $matrix = Get-ProviderLiveTestMatrix
    if ($null -eq $ProviderNames -or $ProviderNames.Count -eq 0 -or $ProviderNames -contains "All") {
        return @($matrix.GetEnumerator() | ForEach-Object { $_.Value })
    }

    $selected = foreach ($providerName in $ProviderNames) {
        $matchingProviderName = @($matrix.Keys | Where-Object {
                [string]::Equals([string]$_, $providerName, [System.StringComparison]::OrdinalIgnoreCase)
            } | Select-Object -First 1)

        if ($matchingProviderName.Count -eq 0) {
            $knownProviders = @("All") + @($matrix.Keys)
            throw "Unknown provider '$providerName'. Expected one of: $($knownProviders -join ', ')."
        }

        $matrix[$matchingProviderName[0]]
    }

    return @($selected)
}

function Invoke-ExternalCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$FailureMessage
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FailureMessage Exit code: $LASTEXITCODE. Command: $FilePath $($Arguments -join ' ')"
    }
}

function Test-ProviderLiveDockerDaemon {
    [CmdletBinding()]
    param()

    $dockerCommand = Get-Command -Name "docker" -CommandType Application -ErrorAction SilentlyContinue
    if ($null -eq $dockerCommand) {
        throw "Docker CLI was not found. Install Docker on this runner before using the provider Testcontainers lane."
    }

    $dockerInfoOutput = & $dockerCommand.Source "info" "--format" "{{.ServerVersion}}" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker daemon is not reachable. Start Docker before using the provider Testcontainers lane. docker info output: $dockerInfoOutput"
    }

    return [string]$dockerInfoOutput
}

function Invoke-ProviderLiveTestcontainers {
    [CmdletBinding()]
    param(
        [string[]]$Providers = @("All"),
        [ValidateSet("Debug", "Release")]
        [string]$Configuration = "Release",
        [switch]$SkipRestore,
        [switch]$NoBuild,
        [switch]$PreflightOnly,
        [string[]]$Logger = @("console;verbosity=normal"),
        [string]$ResultsDirectory = "",
        [string]$ProjectPath = $script:providerIntegrationProjectPath
    )

    if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) {
        throw "Provider integration test project not found: $ProjectPath"
    }

    $selectedProviders = Resolve-ProviderLiveTestSelection -ProviderNames $Providers
    $dotnetVersionOutput = & dotnet "--version" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet CLI is not available. dotnet --version output: $dotnetVersionOutput"
    }

    $dockerVersion = Test-ProviderLiveDockerDaemon

    Write-Host "Provider live Testcontainers preflight passed." -ForegroundColor Green
    Write-Host "dotnet SDK: $dotnetVersionOutput"
    Write-Host "Docker server: $dockerVersion"
    Write-Host "Providers: $($selectedProviders.Provider -join ', ')"

    if ($PreflightOnly) {
        return [pscustomobject]@{
            ProjectPath = $ProjectPath
            Providers = @($selectedProviders.Provider)
            DotNetSdk = [string]$dotnetVersionOutput
            DockerServerVersion = [string]$dockerVersion
            ResultsDirectory = $ResultsDirectory
        }
    }

    if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) {
        $ResultsDirectory = [System.IO.Path]::Combine($script:repoRoot, "artifacts", "provider-live-testcontainers")
    }

    New-Item -Path $ResultsDirectory -ItemType Directory -Force | Out-Null

    if (-not $SkipRestore) {
        Invoke-ExternalCommand `
            -FilePath "dotnet" `
            -Arguments @("restore", $ProjectPath, "--locked-mode") `
            -FailureMessage "Provider integration project restore failed."
    }

    $previousExternalServices = $env:CEPHALON_PROVIDER_EXTERNAL_SERVICES
    $previousTestcontainers = $env:CEPHALON_PROVIDER_TESTCONTAINERS

    try {
        $env:CEPHALON_PROVIDER_EXTERNAL_SERVICES = "1"
        $env:CEPHALON_PROVIDER_TESTCONTAINERS = "1"

        foreach ($provider in $selectedProviders) {
            $providerResultsDirectory = Join-Path $ResultsDirectory $provider.Provider
            New-Item -Path $providerResultsDirectory -ItemType Directory -Force | Out-Null

            $arguments = @(
                "test",
                $ProjectPath,
                "--configuration",
                $Configuration,
                "--no-restore",
                "--filter",
                "FullyQualifiedName~$($provider.FilterToken)",
                "--results-directory",
                $providerResultsDirectory
            )

            if ($NoBuild) {
                $arguments += "--no-build"
            }

            foreach ($loggerValue in $Logger) {
                if (-not [string]::IsNullOrWhiteSpace($loggerValue)) {
                    $arguments += @("--logger", $loggerValue)
                }
            }

            Write-Host ""
            Write-Host "==> Running $($provider.Provider) live provider proof with $($provider.Runtime)" -ForegroundColor Cyan
            Invoke-ExternalCommand `
                -FilePath "dotnet" `
                -Arguments $arguments `
                -FailureMessage "$($provider.Provider) live provider Testcontainers proof failed."
        }
    }
    finally {
        if ($null -eq $previousExternalServices) {
            Remove-Item Env:\CEPHALON_PROVIDER_EXTERNAL_SERVICES -ErrorAction SilentlyContinue
        }
        else {
            $env:CEPHALON_PROVIDER_EXTERNAL_SERVICES = $previousExternalServices
        }

        if ($null -eq $previousTestcontainers) {
            Remove-Item Env:\CEPHALON_PROVIDER_TESTCONTAINERS -ErrorAction SilentlyContinue
        }
        else {
            $env:CEPHALON_PROVIDER_TESTCONTAINERS = $previousTestcontainers
        }
    }
}

if ($env:CEPHALON_PROVIDER_LIVE_TESTCONTAINERS_NO_RUN -eq "1") {
    return
}

Invoke-ProviderLiveTestcontainers `
    -Providers $Providers `
    -Configuration $Configuration `
    -SkipRestore:$SkipRestore `
    -NoBuild:$NoBuild `
    -PreflightOnly:$PreflightOnly `
    -Logger $Logger `
    -ResultsDirectory $ResultsDirectory
