param(
    [string]$AppName = "Cephalon.MicroserviceTransportSmoke",
    [string]$HostUrl = "http://127.0.0.1:18086",
    [string]$GrpcHostUrl = "http://127.0.0.1:18087",
    [int]$TimeoutSeconds = 120,
    [string]$Configuration = "Release",
    [string]$ReportPath = "artifacts/adoption-smoke/microservice-multi-transport-adoption.json",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$validationStartedAtUtc = [DateTimeOffset]::UtcNow
$repoRoot = Split-Path -Parent $PSScriptRoot
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cmsta-" + ([Guid]::NewGuid().ToString("N")).Substring(0, 12))
$packageFeedPath = Join-Path $tempRoot "package-feed"
$toolPath = Join-Path $tempRoot ".tools\cephalon"
$nuGetPackagesPath = Join-Path $tempRoot ".nuget\packages"
$workspaceRoot = Join-Path $tempRoot "workspace"
$generatedRoot = Join-Path $workspaceRoot $AppName
$generatedPackageFeedPath = Join-Path $generatedRoot ".cephalon\packages"
$grpcProbeRoot = Join-Path $workspaceRoot "$AppName.GrpcProbe"
$grpcProbeProjectPath = Join-Path $grpcProbeRoot "$AppName.GrpcProbe.csproj"
$grpcProbeProgramPath = Join-Path $grpcProbeRoot "Program.cs"
$stdoutLogPath = Join-Path $tempRoot "microservice-multi-transport.stdout.log"
$stderrLogPath = Join-Path $tempRoot "microservice-multi-transport.stderr.log"
$grpcProbeStdoutLogPath = Join-Path $tempRoot "microservice-multi-transport-grpc-probe.stdout.log"
$grpcProbeStderrLogPath = Join-Path $tempRoot "microservice-multi-transport-grpc-probe.stderr.log"
$packageProjectPaths = @(
    "src/Cephalon.Abstractions/Cephalon.Abstractions.csproj",
    "src/Cephalon.Diagnostics/Cephalon.Diagnostics.csproj",
    "src/Cephalon.Engine/Cephalon.Engine.csproj",
    "src/Cephalon.Engine.SourceGen/Cephalon.Engine.SourceGen.csproj",
    "src/Cephalon.AspNetCore/Cephalon.AspNetCore.csproj",
    "src/Cephalon.AspNetCore.JsonRpc/Cephalon.AspNetCore.JsonRpc.csproj",
    "src/Cephalon.AspNetCore.Grpc/Cephalon.AspNetCore.Grpc.csproj",
    "src/Cephalon.Behaviors/Cephalon.Behaviors.csproj",
    "src/Cephalon.Behaviors.Http/Cephalon.Behaviors.Http.csproj",
    "src/Cephalon.Behaviors.SourceGen/Cephalon.Behaviors.SourceGen.csproj",
    "src/Cephalon.Cli/Cephalon.Cli.csproj",
    "src/Cephalon.Observability/Cephalon.Observability.csproj",
    "src/Cephalon.Observability.DependencyHealth.Core/Cephalon.Observability.DependencyHealth.Core.csproj",
    "src/Cephalon.Observability.HttpDependencies/Cephalon.Observability.HttpDependencies.csproj",
    "src/Cephalon.Observability.OpenTelemetry/Cephalon.Observability.OpenTelemetry.csproj",
    "src/Cephalon.Observability.Serilog/Cephalon.Observability.Serilog.csproj",
    "src/Cephalon.ReferenceDocs/Cephalon.ReferenceDocs.csproj",
    "src/Cephalon.Resilience/Cephalon.Resilience.csproj",
    "src/Cephalon.Scaffolding/Cephalon.Scaffolding.csproj"
)
$cephalonExecutableFileName = if ($IsWindows) { "cephalon.exe" } else { "cephalon" }
$cephalonExecutablePath = Join-Path $toolPath $cephalonExecutableFileName
$environmentSnapshot = @{}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet command failed: dotnet $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-Cephalon {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        & $cephalonExecutablePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Cephalon CLI command failed: $cephalonExecutablePath $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Set-ScopedEnvironmentVariable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [AllowNull()]
        [string]$Value
    )

    if (-not $script:environmentSnapshot.ContainsKey($Name)) {
        $current = Get-Item -LiteralPath "Env:\$Name" -ErrorAction SilentlyContinue
        $script:environmentSnapshot[$Name] = if ($null -eq $current) { $null } else { $current.Value }
    }

    if ($null -eq $Value) {
        Remove-Item -LiteralPath "Env:\$Name" -ErrorAction SilentlyContinue
        return
    }

    Set-Item -LiteralPath "Env:\$Name" -Value $Value
}

function Restore-ScopedEnvironmentVariables {
    foreach ($entry in $script:environmentSnapshot.GetEnumerator()) {
        if ($null -eq $entry.Value) {
            Remove-Item -LiteralPath "Env:\$($entry.Key)" -ErrorAction SilentlyContinue
        }
        else {
            Set-Item -LiteralPath "Env:\$($entry.Key)" -Value $entry.Value
        }
    }
}

function Resolve-GeneratedHostProjectPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GeneratedRoot,
        [Parameter(Mandatory = $true)]
        [string]$AppName
    )

    $sourceRoot = Join-Path $GeneratedRoot "src"
    $projects = @(Get-ChildItem -Path $sourceRoot -Recurse -Filter "*.csproj" -File | Sort-Object FullName)
    if ($projects.Count -eq 0) {
        throw "No generated host project was found under '$sourceRoot'."
    }

    $preferredProject = @(
        $projects | Where-Object { $_.BaseName -eq "$AppName.Host" } | Select-Object -First 1
        $projects | Where-Object { $_.BaseName -eq "$AppName.Service" } | Select-Object -First 1
        $projects | Where-Object { $_.Name.EndsWith(".Host.csproj", [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
        $projects | Where-Object { $_.Name.EndsWith(".Service.csproj", [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
    ) | Where-Object { $null -ne $_ } | Select-Object -First 1

    if ($null -ne $preferredProject) {
        return $preferredProject.FullName
    }

    if ($projects.Count -eq 1) {
        return $projects[0].FullName
    }

    throw "Could not determine the generated host project under '$sourceRoot'."
}

function Wait-ForHttpSuccess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds,
        [System.Diagnostics.Process]$Process = $null
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if ($null -ne $Process -and $Process.HasExited) {
            throw "Generated host exited before '$Uri' became ready. Exit code: $($Process.ExitCode)."
        }

        try {
            $response = Invoke-WebRequest -Uri $Uri -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                Write-Host "Validated $Uri" -ForegroundColor Green
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 2
    }
    while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for HTTP success from '$Uri'."
}

function Invoke-HttpJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [string]$Method = "GET",
        [object]$Body = $null
    )

    $arguments = @{
        Uri = $Uri
        Method = $Method
        TimeoutSec = 15
    }
    if ($null -ne $Body) {
        $arguments["ContentType"] = "application/json"
        $arguments["Body"] = ($Body | ConvertTo-Json -Depth 12)
    }

    $response = Invoke-WebRequest @arguments
    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw "Expected HTTP success from '$Uri' but received $($response.StatusCode)."
    }

    return $response.Content
}

function Wait-ForHttpContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [Parameter(Mandatory = $true)]
        [string[]]$Tokens,
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds,
        [System.Diagnostics.Process]$Process = $null
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if ($null -ne $Process -and $Process.HasExited) {
            throw "Generated host exited before '$Uri' contained expected content. Exit code: $($Process.ExitCode)."
        }

        try {
            $content = Invoke-HttpJson -Uri $Uri
            $missing = @($Tokens | Where-Object { -not $content.Contains($_, [System.StringComparison]::OrdinalIgnoreCase) })
            if ($missing.Count -eq 0) {
                Write-Host "Validated content from $Uri" -ForegroundColor Green
                return $content
            }
        }
        catch {
        }

        Start-Sleep -Seconds 2
    }
    while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for '$Uri' to contain: $($Tokens -join ', ')."
}

function Wait-ForDependencyHealthReport {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [Parameter(Mandatory = $true)]
        [string]$DependencyId,
        [Parameter(Mandatory = $true)]
        [string]$Source,
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds,
        [System.Diagnostics.Process]$Process = $null
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastContent = ""
    do {
        if ($null -ne $Process -and $Process.HasExited) {
            throw "Generated host exited before '$Uri' reported dependency '$DependencyId'. Exit code: $($Process.ExitCode)."
        }

        try {
            $lastContent = Invoke-HttpJson -Uri $Uri
            $reports = @($lastContent | ConvertFrom-Json -Depth 32)
            $report = $reports |
                Where-Object { $_.id -eq $DependencyId -or $_.Id -eq $DependencyId } |
                Select-Object -First 1

            if ($null -ne $report) {
                $state = if ($report.PSObject.Properties.Name -contains "state") { $report.state } else { $report.State }
                $reportSource = if ($report.PSObject.Properties.Name -contains "source") { [string]$report.source } else { [string]$report.Source }
                $isHealthy = ([string]$state -eq "0" -or [string]$state -eq "Healthy")
                $hasSource = $reportSource.Contains($Source, [System.StringComparison]::OrdinalIgnoreCase)

                if ($isHealthy -and $hasSource) {
                    Write-Host "Validated dependency '$DependencyId' from $Uri" -ForegroundColor Green
                    return $lastContent
                }
            }
        }
        catch {
        }

        Start-Sleep -Seconds 2
    }
    while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for '$Uri' to report healthy dependency '$DependencyId' from '$Source'. Last payload: $lastContent"
}

function Write-RecentLogs {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    Write-Host ""
    Write-Host "${Label}:" -ForegroundColor Yellow
    Get-Content -LiteralPath $Path -Tail 120
}

function Resolve-ReportPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Assert-GeneratedMicroserviceMultiTransportFoundation {
    param(
        [Parameter(Mandatory = $true)]
        [string]$HostProjectPath
    )

    $hostProjectDirectory = Split-Path -Parent $HostProjectPath
    $sourceRoot = Split-Path -Parent $hostProjectDirectory
    $appModelSettingsPath = Join-Path $hostProjectDirectory "Configurations\AddEngine.AppModel.json"
    $observabilitySettingsPath = Join-Path $hostProjectDirectory "Configurations\AddEngine.Observability.json"
    $hostProgramPath = Join-Path $hostProjectDirectory "Program.cs"

    foreach ($path in @($appModelSettingsPath, $observabilitySettingsPath, $hostProgramPath)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Expected generated microservice adoption file '$path'."
        }
    }

    $hostProject = Get-Content -LiteralPath $HostProjectPath -Raw
    foreach ($token in @(
        "Cephalon.AspNetCore",
        "Cephalon.AspNetCore.JsonRpc",
        "Cephalon.AspNetCore.Grpc",
        "Cephalon.Observability.HttpDependencies",
        "Cephalon.Engine.SourceGen")) {
        if (-not $hostProject.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated host project to contain package reference token '$token'."
        }
    }

    $projectText = (
        Get-ChildItem -Path $sourceRoot -Recurse -Filter "*.csproj" -File |
            ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }
    ) -join [Environment]::NewLine
    foreach ($token in @("Cephalon.Behaviors.Http", "Cephalon.Behaviors.SourceGen", "Cephalon.AspNetCore.JsonRpc", "Cephalon.AspNetCore.Grpc")) {
        if (-not $projectText.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated project graph to contain package reference token '$token'."
        }
    }

    $moduleText = (
        Get-ChildItem -Path $sourceRoot -Recurse -Filter "*Module.cs" -File |
            ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }
    ) -join [Environment]::NewLine
    foreach ($token in @(
        "IJsonRpcModule",
        "IGrpcModule",
        "MapJsonRpcEndpoints",
        "MapGrpcEndpoints",
        "DiscoveryService.DiscoveryServiceBase",
        "GeneratedTransportStatusComposer",
        '"platform.status.get"',
        '"microservice"',
        '"multi-transport"',
        '"observable"')) {
        if (-not $moduleText.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated module source to contain multi-transport token '$token'."
        }
    }

    $program = Get-Content -LiteralPath $hostProgramPath -Raw
    foreach ($token in @(
        "builder.AddJsonRpcTransport();",
        "builder.AddGrpcTransport();",
        "builder.Services.AddCephalonHttpDependencyHealth(builder.Configuration);",
        "engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>",
        "behaviors.AddHttpBehaviorBindings();",
        "app.MapCephalon();")) {
        if (-not $program.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated Program.cs to contain '$token'."
        }
    }

    $appModel = Get-Content -LiteralPath $appModelSettingsPath -Raw
    foreach ($token in @('"Blueprint": "microservice"', '"rest-api"', '"json-rpc"', '"grpc"')) {
        if (-not $appModel.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated app-model settings to contain '$token'."
        }
    }

    $observability = Get-Content -LiteralPath $observabilitySettingsPath -Raw
    foreach ($token in @('"DependencyHealth"', '"Http"', '"RefreshIntervalSeconds"', '"Dependencies"')) {
        if (-not $observability.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated observability settings to contain '$token'."
        }
    }
}

function Write-GrpcProbeProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageFeedPath
    )

    New-Item -ItemType Directory -Path $grpcProbeRoot -Force | Out-Null
    $nugetConfigPath = Join-Path $grpcProbeRoot "NuGet.config"

    @"
<configuration>
  <packageSources>
    <clear />
    <add key="cephalon" value="$PackageFeedPath" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="cephalon">
      <package pattern="Cephalon*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content -LiteralPath $nugetConfigPath -Encoding UTF8

    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Cephalon.AspNetCore.Grpc" Version="0.1.0-preview" />
    <PackageReference Include="Grpc.Net.Client" Version="2.76.0" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $grpcProbeProjectPath -Encoding UTF8

    @'
using System.Net;
using Cephalon.AspNetCore.Grpc.Contracts.Discovery;
using Grpc.Net.Client;

var grpcUrl = args.Length > 0
    ? args[0]
    : throw new InvalidOperationException("The gRPC host URL argument is required.");

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

using var httpHandler = new GrpcSubdirectoryHandler(new SocketsHttpHandler(), "/grpc");
using var httpClient = new HttpClient(httpHandler)
{
    DefaultRequestVersion = HttpVersion.Version20,
    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
};
using var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
{
    HttpClient = httpClient,
    DisposeHttpClient = false
});
var client = new DiscoveryService.DiscoveryServiceClient(channel);
var reply = await client.SayHelloAsync(new HelloRequest
{
    Name = "Codex"
});

Console.WriteLine($"grpc-message={reply.Message}");
Console.WriteLine($"grpc-traits={string.Join(",", reply.Traits)}");

if (!reply.Message.Contains("Hello, Codex", StringComparison.Ordinal))
{
    return 2;
}

if (!reply.Message.Contains("Platform", StringComparison.OrdinalIgnoreCase))
{
    return 3;
}

if (!reply.Traits.Contains("grpc"))
{
    return 4;
}

return 0;

internal sealed class GrpcSubdirectoryHandler : DelegatingHandler
{
    private readonly string subdirectory;

    public GrpcSubdirectoryHandler(HttpMessageHandler innerHandler, string subdirectory)
        : base(innerHandler)
    {
        ArgumentNullException.ThrowIfNull(innerHandler);
        ArgumentException.ThrowIfNullOrWhiteSpace(subdirectory);

        this.subdirectory = subdirectory.StartsWith('/')
            ? subdirectory.TrimEnd('/')
            : "/" + subdirectory.TrimEnd('/');
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.RequestUri);

        var requestUri = request.RequestUri;
        request.RequestUri = new UriBuilder(requestUri)
        {
            Path = subdirectory + requestUri.AbsolutePath
        }.Uri;

        return base.SendAsync(request, cancellationToken);
    }
}
'@ | Set-Content -LiteralPath $grpcProbeProgramPath -Encoding UTF8

    return [pscustomobject]([ordered]@{
        Root = $grpcProbeRoot
        Project = $grpcProbeProjectPath
        Program = $grpcProbeProgramPath
        NuGetConfig = $nugetConfigPath
    })
}

function New-MicroserviceMultiTransportAdoptionRuntimeProbeRows {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status
    )

    @(
        [pscustomobject]([ordered]@{ Kind = "health-ready"; Path = "/health/ready"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "health-live"; Path = "/health/live"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "engine-root"; Path = "/engine"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "snapshot"; Path = "/engine/snapshot"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "transports"; Path = "/engine/transports"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "dependencies"; Path = "/engine/dependencies"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "diagnostics"; Path = "/engine/diagnostics"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "runtime-story"; Path = "/engine/runtime-story"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "json-rpc-runtime"; Path = "/engine/technology-surfaces/json-rpc"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "grpc-runtime"; Path = "/engine/technology-surfaces/grpc"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "rest-business-endpoint"; Path = "/api/v1/platform/status"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "json-rpc-business-endpoint"; Path = "/json-rpc/platform"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "grpc-business-endpoint"; Path = "$GrpcHostUrl/grpc"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "rest-docs"; Path = "/scalar"; Status = $Status })
    )
}

function New-MicroserviceMultiTransportAdoptionAssertionRows {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Passed
    )

    @(
        [pscustomobject]([ordered]@{ Name = "runsOutsideRepository"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "publishesLocalPackages"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "publishesDependencyHealthCorePackage"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "installsCliFromTemporaryFeed"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "scaffoldsMicroserviceMultiTransportApp"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "seedsGeneratedLocalPackageFeed"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "replaysGeneratedAppDoctor"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "restoresGeneratedSolution"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "buildsGeneratedSolution"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "runsGeneratedHost"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesRestBusinessEndpoint"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesJsonRpcBusinessEndpoint"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesGrpcBusinessEndpoint"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesTransportRuntimeTruth"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesHttpDependencyProbe"; Passed = $Passed })
    )
}

function Get-NonSymbolPackageCount {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return 0
    }

    return @(
        Get-ChildItem -Path $Path -Filter "*.nupkg" -File |
            Where-Object { -not $_.Name.EndsWith(".symbols.nupkg", [System.StringComparison]::OrdinalIgnoreCase) }
    ).Count
}

function New-ProofSnippet {
    param(
        [AllowNull()]
        [string]$Value,
        [int]$MaximumLength = 1600
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    $normalized = $Value.Trim()
    if ($normalized.Length -le $MaximumLength) {
        return $normalized
    }

    return $normalized.Substring(0, $MaximumLength) + "...[truncated]"
}

function Write-MicroserviceMultiTransportAdoptionExecutionReport {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status,
        [string]$ErrorMessage = ""
    )

    $completedAtUtc = [DateTimeOffset]::UtcNow
    $passed = $Status.Equals("passed", [System.StringComparison]::OrdinalIgnoreCase)
    $runtimeProbeStatus = if ($passed) { "passed" } else { "not-run-or-failed" }
    $resolvedReportPath = Resolve-ReportPath -Path $ReportPath
    $reportDirectory = Split-Path -Parent $resolvedReportPath
    if (-not [string]::IsNullOrWhiteSpace($reportDirectory)) {
        New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
    }

    $report = [pscustomobject]([ordered]@{
        '$schemaVersion' = "1.0.0"
        ScenarioId = "microservice-multi-transport-operations"
        Status = $Status
        AppName = $AppName
        HostUrl = $HostUrl
        GrpcHostUrl = $GrpcHostUrl
        Configuration = $Configuration
        SkipPackageBuild = [bool]$SkipPackageBuild
        KeepOutput = [bool]$KeepOutput
        StartedAtUtc = $validationStartedAtUtc.ToString("O")
        CompletedAtUtc = $completedAtUtc.ToString("O")
        DurationMilliseconds = [math]::Round(($completedAtUtc - $validationStartedAtUtc).TotalMilliseconds, 2)
        Assertions = New-MicroserviceMultiTransportAdoptionAssertionRows -Passed:$passed
        RuntimeProbes = New-MicroserviceMultiTransportAdoptionRuntimeProbeRows -Status $runtimeProbeStatus
        Evidence = [pscustomobject]([ordered]@{
            LocalPackageCount = Get-NonSymbolPackageCount -Path $packageFeedPath
            GeneratedPackageCount = Get-NonSymbolPackageCount -Path $generatedPackageFeedPath
            RequiredTransports = @("rest-api", "json-rpc", "grpc")
            RequiredSnapshotTokens = @("microservice", "rest-api", "json-rpc", "grpc", "Platform")
            JsonRpcSurfaceCount = $jsonRpcSurfaceCount
            GrpcSurfaceCount = $grpcSurfaceCount
            RestStatusSnippet = New-ProofSnippet -Value $restStatus
            JsonRpcStatusSnippet = New-ProofSnippet -Value $rpcStatus
            GrpcProbeSnippet = New-ProofSnippet -Value $grpcProbeOutput
            TransportsSnippet = New-ProofSnippet -Value $transports
            JsonRpcSurfaceSnippet = New-ProofSnippet -Value $jsonRpcSurface
            GrpcSurfaceSnippet = New-ProofSnippet -Value $grpcSurface
            DependencyHealthSnippet = New-ProofSnippet -Value $dependencies
        })
        DependencyProbe = [pscustomobject]([ordered]@{
            Id = "self-health"
            Endpoint = "$HostUrl/engine"
            Provider = "Cephalon.Observability.HttpDependencies"
            Required = $false
        })
        Paths = [pscustomobject]([ordered]@{
            TemporaryRoot = $tempRoot
            PackageFeed = $packageFeedPath
            ToolPath = $toolPath
            NuGetPackages = $nuGetPackagesPath
            WorkspaceRoot = $workspaceRoot
            GeneratedAppRoot = $generatedRoot
            GeneratedPackageFeed = $generatedPackageFeedPath
            Solution = $solutionPath
            HostProject = $hostProjectPath
            HostAssembly = $hostAssemblyPath
            GrpcProbeRoot = $grpcProbeRoot
            GrpcProbeProject = $grpcProbeProjectPath
            StdoutLog = $stdoutLogPath
            StderrLog = $stderrLogPath
            GrpcProbeStdoutLog = $grpcProbeStdoutLogPath
            GrpcProbeStderrLog = $grpcProbeStderrLogPath
            TemporaryOutputRetained = [bool]$KeepOutput
        })
        Error = $ErrorMessage
    })

    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $resolvedReportPath -Encoding UTF8
    Write-Host "Microservice multi-transport adoption execution report: $resolvedReportPath" -ForegroundColor Cyan
}

$process = $null
$previousNuGetPackages = $null
$restoreRepoPackageAssets = $false
$hostProjectPath = ""
$solutionPath = ""
$hostAssemblyPath = ""
$transports = ""
$snapshot = ""
$restStatus = ""
$rpcStatus = ""
$grpcProbeOutput = ""
$dependencies = ""
$diagnostics = ""
$jsonRpcSurface = ""
$grpcSurface = ""
$jsonRpcSurfaceCount = 0
$grpcSurfaceCount = 0

try {
    New-Item -ItemType Directory -Path $packageFeedPath -Force | Out-Null
    New-Item -ItemType Directory -Path $toolPath -Force | Out-Null
    New-Item -ItemType Directory -Path $nuGetPackagesPath -Force | Out-Null
    New-Item -ItemType Directory -Path $workspaceRoot -Force | Out-Null

    $previousNuGetPackages = $env:NUGET_PACKAGES
    $env:NUGET_PACKAGES = $nuGetPackagesPath
    $restoreRepoPackageAssets = $true

    Write-Host ""
    Write-Host "Publishing repo-local Cephalon packages for microservice multi-transport adoption..." -ForegroundColor Cyan
    $publishPackageArguments = @{
        Configuration = $Configuration
        OutputPath = $packageFeedPath
        ProjectPaths = $packageProjectPaths
    }
    if ($SkipPackageBuild) {
        $publishPackageArguments["SkipBuild"] = $true
    }

    & $publishPackagesScriptPath @publishPackageArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Package publishing to the temporary feed failed."
    }

    Write-Host ""
    Write-Host "Installing Cephalon CLI from the temporary package feed..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $workspaceRoot -Arguments @(
        "tool",
        "install",
        "--tool-path", $toolPath,
        "Cephalon.Cli",
        "--add-source", $packageFeedPath,
        "--ignore-failed-sources",
        "--no-cache",
        "--prerelease")

    if (-not (Test-Path -LiteralPath $cephalonExecutablePath -PathType Leaf)) {
        throw "Expected installed Cephalon CLI at '$cephalonExecutablePath'."
    }

    Write-Host ""
    Write-Host "Scaffolding a microservice host with REST, JSON-RPC, and gRPC outside the repository..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @(
        "new",
        $AppName,
        "--output", $generatedRoot,
        "--blueprint", "Microservice",
        "--module", "Platform",
        "--feature", "Overview",
        "--transport", "RestApi",
        "--transport", "JsonRpc",
        "--transport", "Grpc")

    $hostProjectPath = Resolve-GeneratedHostProjectPath -GeneratedRoot $generatedRoot -AppName $AppName
    $solutionPath = Join-Path $generatedRoot "$AppName.slnx"
    if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
        throw "Expected generated solution at '$solutionPath'."
    }

    Assert-GeneratedMicroserviceMultiTransportFoundation -HostProjectPath $hostProjectPath

    Write-Host ""
    Write-Host "Seeding the generated local package feed..." -ForegroundColor Cyan
    & $publishPackagesScriptPath -Configuration $Configuration -OutputPath $generatedPackageFeedPath -SkipBuild -ProjectPaths $packageProjectPaths
    if ($LASTEXITCODE -ne 0) {
        throw "Package publishing to the generated local feed failed."
    }

    Write-Host ""
    Write-Host "Replaying generated-app doctor checks through the installed CLI..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @("doctor", "--app-root", $generatedRoot)

    Write-Host ""
    Write-Host "Restoring the generated solution..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $generatedRoot -Arguments @("restore", $solutionPath)

    Write-Host ""
    Write-Host "Building the generated solution..." -ForegroundColor Cyan
    Invoke-DotNet -WorkingDirectory $generatedRoot -Arguments @("build", $solutionPath, "-c", $Configuration, "--no-restore")

    Write-Host ""
    Write-Host "Running the generated multi-transport host..." -ForegroundColor Cyan
    $hostProjectDirectory = Split-Path -Parent $hostProjectPath
    $hostAssemblyPath = Join-Path $hostProjectDirectory "bin\$Configuration\net10.0\$([System.IO.Path]::GetFileNameWithoutExtension($hostProjectPath)).dll"
    if (-not (Test-Path -LiteralPath $hostAssemblyPath -PathType Leaf)) {
        throw "Expected built generated host assembly at '$hostAssemblyPath'."
    }

    Set-ScopedEnvironmentVariable -Name "ASPNETCORE_URLS" -Value $null
    Set-ScopedEnvironmentVariable -Name "DOTNET_ENVIRONMENT" -Value "Development"
    Set-ScopedEnvironmentVariable -Name "Kestrel__Endpoints__Http__Url" -Value $HostUrl
    Set-ScopedEnvironmentVariable -Name "Kestrel__Endpoints__Http__Protocols" -Value "Http1"
    Set-ScopedEnvironmentVariable -Name "Kestrel__Endpoints__Grpc__Url" -Value $GrpcHostUrl
    Set-ScopedEnvironmentVariable -Name "Kestrel__Endpoints__Grpc__Protocols" -Value "Http2"
    Set-ScopedEnvironmentVariable -Name "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2UNENCRYPTEDSUPPORT" -Value "1"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__RefreshIntervalSeconds" -Value "1"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__Id" -Value "self-health"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__DisplayName" -Value "Generated microservice engine self probe"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__Endpoint" -Value "$HostUrl/engine"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__Method" -Value "GET"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__Required" -Value "false"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__TimeoutSeconds" -Value "5"
    Set-ScopedEnvironmentVariable -Name "Engine__Observability__DependencyHealth__Http__Dependencies__0__ExpectedStatusCodes__0" -Value "200"

    $process = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @($hostAssemblyPath) `
        -WorkingDirectory $hostProjectDirectory `
        -RedirectStandardOutput $stdoutLogPath `
        -RedirectStandardError $stderrLogPath `
        -PassThru `
        -WindowStyle Hidden

    Wait-ForHttpSuccess -Uri "$HostUrl/health/ready" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/health/live" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/snapshot" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/transports" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/diagnostics" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/runtime-story" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/technology-surfaces/json-rpc" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/technology-surfaces/grpc" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/scalar" -TimeoutSeconds $TimeoutSeconds -Process $process

    $transports = Invoke-HttpJson -Uri "$HostUrl/engine/transports"
    foreach ($token in @("rest-api", "json-rpc", "grpc")) {
        if (-not $transports.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected /engine/transports to contain '$token'."
        }
    }

    $snapshot = Invoke-HttpJson -Uri "$HostUrl/engine/snapshot"
    foreach ($token in @("microservice", "rest-api", "json-rpc", "grpc", "Platform")) {
        if (-not $snapshot.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected /engine/snapshot to contain '$token'."
        }
    }

    $restStatus = Invoke-HttpJson -Uri "$HostUrl/api/v1/platform/status"
    foreach ($token in @("platform", "Microservice", "Generated behavior-backed REST module is running")) {
        if (-not $restStatus.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected generated REST status response to contain '$token'."
        }
    }

    $rpcBody = [pscustomobject]([ordered]@{
        jsonRpc = "2.0"
        method = "platform.status.get"
        params = [pscustomobject]([ordered]@{
            name = "Codex"
        })
        id = "microservice-jsonrpc"
    })
    $rpcStatus = Invoke-HttpJson -Uri "$HostUrl/json-rpc/platform" -Method "POST" -Body $rpcBody
    foreach ($token in @("microservice-jsonrpc", "Hello, Codex", "platform", "json-rpc", "grpc")) {
        if (-not $rpcStatus.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected generated JSON-RPC response to contain '$token'."
        }
    }

    Write-Host ""
    Write-Host "Creating and running the gRPC client probe..." -ForegroundColor Cyan
    $grpcProbe = Write-GrpcProbeProject -PackageFeedPath $generatedPackageFeedPath
    Invoke-DotNet -WorkingDirectory $grpcProbe.Root -Arguments @("restore", $grpcProbe.Project)
    Invoke-DotNet -WorkingDirectory $grpcProbe.Root -Arguments @("build", $grpcProbe.Project, "-c", $Configuration, "--no-restore")

    $grpcProcess = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--project", $grpcProbe.Project, "-c", $Configuration, "--no-build", "--", $GrpcHostUrl) `
        -WorkingDirectory $grpcProbe.Root `
        -RedirectStandardOutput $grpcProbeStdoutLogPath `
        -RedirectStandardError $grpcProbeStderrLogPath `
        -PassThru `
        -Wait `
        -WindowStyle Hidden

    if ($grpcProcess.ExitCode -ne 0) {
        throw "gRPC probe failed with exit code $($grpcProcess.ExitCode)."
    }

    $grpcProbeOutput = Get-Content -LiteralPath $grpcProbeStdoutLogPath -Raw
    foreach ($token in @("grpc-message=Hello, Codex", "Platform", "grpc-traits=", "grpc")) {
        if (-not $grpcProbeOutput.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected gRPC probe output to contain '$token'."
        }
    }

    $dependencies = Wait-ForDependencyHealthReport `
        -Uri "$HostUrl/engine/dependencies" `
        -DependencyId "self-health" `
        -Source "Cephalon.Observability.HttpDependencies" `
        -TimeoutSeconds $TimeoutSeconds `
        -Process $process

    $diagnostics = Invoke-HttpJson -Uri "$HostUrl/engine/diagnostics"
    foreach ($token in @("Cephalon.Engine", "Cephalon.AspNetCore", "Cephalon.Observability", "Cephalon.Observability.HttpDependencies")) {
        if (-not $diagnostics.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected diagnostics surface to contain '$token'."
        }
    }

    $jsonRpcSurface = Invoke-HttpJson -Uri "$HostUrl/engine/technology-surfaces/json-rpc"
    $jsonRpcSurfacePayload = @($jsonRpcSurface | ConvertFrom-Json -Depth 32)
    if ($null -eq $jsonRpcSurfacePayload) {
        throw "Expected JSON-RPC runtime surface endpoint to return a JSON array."
    }
    $jsonRpcSurfaceCount = $jsonRpcSurfacePayload.Count

    $grpcSurface = Invoke-HttpJson -Uri "$HostUrl/engine/technology-surfaces/grpc"
    $grpcSurfacePayload = @($grpcSurface | ConvertFrom-Json -Depth 32)
    if ($null -eq $grpcSurfacePayload) {
        throw "Expected gRPC runtime surface endpoint to return a JSON array."
    }
    $grpcSurfaceCount = $grpcSurfacePayload.Count

    $null = $dependencies | ConvertFrom-Json -Depth 12

    Write-MicroserviceMultiTransportAdoptionExecutionReport -Status "passed"
    Write-Host ""
    Write-Host "Microservice multi-transport adoption validation completed successfully." -ForegroundColor Green
    Write-Host "Generated app root: $generatedRoot" -ForegroundColor Cyan
    Write-Host "HTTP host URL: $HostUrl" -ForegroundColor Cyan
    Write-Host "gRPC host URL: $GrpcHostUrl" -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "Microservice multi-transport adoption validation failed." -ForegroundColor Yellow
    Write-RecentLogs -Path $stdoutLogPath -Label "Generated host stdout"
    Write-RecentLogs -Path $stderrLogPath -Label "Generated host stderr"
    Write-RecentLogs -Path $grpcProbeStdoutLogPath -Label "gRPC probe stdout"
    Write-RecentLogs -Path $grpcProbeStderrLogPath -Label "gRPC probe stderr"
    try {
        Write-MicroserviceMultiTransportAdoptionExecutionReport -Status "failed" -ErrorMessage $_.Exception.Message
    }
    catch {
        Write-Warning "Could not write microservice multi-transport adoption execution report: $($_.Exception.Message)"
    }
    throw
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit(5000) | Out-Null
    }

    Restore-ScopedEnvironmentVariables
    $env:NUGET_PACKAGES = $previousNuGetPackages

    if ($restoreRepoPackageAssets) {
        try {
            Write-Host ""
            Write-Host "Restoring repo package assets back to the default NuGet cache..." -ForegroundColor Cyan
            foreach ($packageProjectPath in $packageProjectPaths) {
                Invoke-DotNet -WorkingDirectory $repoRoot -Arguments @("restore", $packageProjectPath)
            }
        }
        catch {
            Write-Warning "Could not restore repo package assets back to the default NuGet cache: $($_.Exception.Message)"
        }
    }

    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
