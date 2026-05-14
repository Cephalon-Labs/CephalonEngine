param(
    [string]$AppName = "Cephalon.ModularMonolithSmoke",
    [string]$HostUrl = "http://127.0.0.1:18084",
    [int]$TimeoutSeconds = 120,
    [string]$Configuration = "Release",
    [string]$ReportPath = "artifacts/adoption-smoke/modular-monolith-adoption.json",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$validationStartedAtUtc = [DateTimeOffset]::UtcNow
$repoRoot = Split-Path -Parent $PSScriptRoot
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-modular-monolith-adoption-" + [Guid]::NewGuid().ToString("N"))
$packageFeedPath = Join-Path $tempRoot "package-feed"
$toolPath = Join-Path $tempRoot ".tools\cephalon"
$nuGetPackagesPath = Join-Path $tempRoot ".nuget\packages"
$workspaceRoot = Join-Path $tempRoot "workspace"
$generatedRoot = Join-Path $workspaceRoot $AppName
$generatedPackageFeedPath = Join-Path $generatedRoot ".cephalon\packages"
$workerProbeRoot = Join-Path $workspaceRoot "$AppName.WorkerProbe"
$workerProbeProjectPath = Join-Path $workerProbeRoot "$AppName.WorkerProbe.csproj"
$workerProbeProgramPath = Join-Path $workerProbeRoot "Program.cs"
$stdoutLogPath = Join-Path $tempRoot "modular-monolith.stdout.log"
$stderrLogPath = Join-Path $tempRoot "modular-monolith.stderr.log"
$workerStdoutLogPath = Join-Path $tempRoot "modular-monolith-worker.stdout.log"
$workerStderrLogPath = Join-Path $tempRoot "modular-monolith-worker.stderr.log"
$packageProjectPaths = @(
    "src/Cephalon.Abstractions/Cephalon.Abstractions.csproj",
    "src/Cephalon.Analyzers/Cephalon.Analyzers.csproj",
    "src/Cephalon.Diagnostics/Cephalon.Diagnostics.csproj",
    "src/Cephalon.Engine/Cephalon.Engine.csproj",
    "src/Cephalon.Engine.SourceGen/Cephalon.Engine.SourceGen.csproj",
    "src/Cephalon.AspNetCore/Cephalon.AspNetCore.csproj",
    "src/Cephalon.Data/Cephalon.Data.csproj",
    "src/Cephalon.Ids.Sfid/Cephalon.Ids.Sfid.csproj",
    "src/Cephalon.Behaviors/Cephalon.Behaviors.csproj",
    "src/Cephalon.Behaviors.Http/Cephalon.Behaviors.Http.csproj",
    "src/Cephalon.Behaviors.SourceGen/Cephalon.Behaviors.SourceGen.csproj",
    "src/Cephalon.Cli/Cephalon.Cli.csproj",
    "src/Cephalon.Worker/Cephalon.Worker.csproj",
    "src/Cephalon.Observability/Cephalon.Observability.csproj",
    "src/Cephalon.Observability.OpenTelemetry/Cephalon.Observability.OpenTelemetry.csproj",
    "src/Cephalon.Observability.Serilog/Cephalon.Observability.Serilog.csproj",
    "src/Cephalon.ReferenceDocs/Cephalon.ReferenceDocs.csproj",
    "src/Cephalon.Resilience/Cephalon.Resilience.csproj",
    "src/Cephalon.Scaffolding/Cephalon.Scaffolding.csproj"
)
$cephalonExecutableFileName = if ($IsWindows) { "cephalon.exe" } else { "cephalon" }
$cephalonExecutablePath = Join-Path $toolPath $cephalonExecutableFileName

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

function Resolve-GeneratedProjectPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GeneratedRoot,
        [Parameter(Mandatory = $true)]
        [string]$ProjectSuffix
    )

    $project = Get-ChildItem -Path (Join-Path $GeneratedRoot "src") -Recurse -Filter "*.csproj" -File |
        Where-Object { $_.BaseName.EndsWith($ProjectSuffix, [System.StringComparison]::OrdinalIgnoreCase) } |
        Sort-Object FullName |
        Select-Object -First 1

    if ($null -eq $project) {
        throw "Could not find generated project ending with '$ProjectSuffix' under '$GeneratedRoot'."
    }

    return $project.FullName
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
    Get-Content -LiteralPath $Path -Tail 80
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

function Assert-GeneratedModularMonolithDataFoundation {
    param(
        [Parameter(Mandatory = $true)]
        [string]$HostProjectPath
    )

    $hostProjectDirectory = Split-Path -Parent $HostProjectPath
    $appModelSettingsPath = Join-Path $hostProjectDirectory "Configurations\AddEngine.AppModel.json"
    $dataSettingsPath = Join-Path $hostProjectDirectory "Configurations\AddEngine.Data.json"
    $hostProgramPath = Join-Path $hostProjectDirectory "Program.cs"

    foreach ($path in @($appModelSettingsPath, $dataSettingsPath, $hostProgramPath)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Expected generated modular-monolith adoption file '$path'."
        }
    }

    $hostProject = Get-Content -LiteralPath $HostProjectPath -Raw
    foreach ($token in @(
        "Cephalon.AspNetCore",
        "Cephalon.Data",
        "Cephalon.Ids.Sfid",
        "Cephalon.Engine.SourceGen")) {
        if (-not $hostProject.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated host project to contain package reference token '$token'."
        }
    }

    $generatedSourceRoot = Split-Path -Parent $hostProjectDirectory
    $generatedProjectText = (
        Get-ChildItem -Path $generatedSourceRoot -Recurse -Filter "*.csproj" -File |
            ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }
    ) -join [Environment]::NewLine
    foreach ($token in @("Cephalon.Behaviors.Http", "Cephalon.Behaviors.SourceGen")) {
        if (-not $generatedProjectText.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated project graph to contain package reference token '$token'."
        }
    }

    $program = Get-Content -LiteralPath $hostProgramPath -Raw
    foreach ($token in @("builder.AddCephalonProjectConfigurations();", "engine.AddData();", "engine.AddSfidIds();", "app.MapCephalon();")) {
        if (-not $program.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated Program.cs to contain '$token'."
        }
    }

    $appModel = Get-Content -LiteralPath $appModelSettingsPath -Raw
    foreach ($token in @('"Blueprint": "modular-monolith"', '"cqrs"', '"outbox"', '"rest-api"')) {
        if (-not $appModel.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated app-model settings to contain '$token'."
        }
    }

    $dataSettings = Get-Content -LiteralPath $dataSettingsPath -Raw
    foreach ($token in @('"ReadWriteSplit": true', '"Enabled": true', '"Generator": "Sfid"')) {
        if (-not $dataSettings.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated data settings to contain '$token'."
        }
    }
}

function Write-WorkerProbeProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkerProbeRoot,
        [Parameter(Mandatory = $true)]
        [string]$WorkerProbeProjectPath,
        [Parameter(Mandatory = $true)]
        [string]$WorkerProbeProgramPath,
        [Parameter(Mandatory = $true)]
        [string]$GeneratedRoot,
        [Parameter(Mandatory = $true)]
        [string]$AppName,
        [Parameter(Mandatory = $true)]
        [string]$HostProjectPath,
        [Parameter(Mandatory = $true)]
        [string]$PackageFeedPath
    )

    New-Item -ItemType Directory -Path $WorkerProbeRoot -Force | Out-Null

    $foundationProjectPath = Resolve-GeneratedProjectPath -GeneratedRoot $GeneratedRoot -ProjectSuffix ".Foundation"
    $moduleProjectPath = Resolve-GeneratedProjectPath -GeneratedRoot $GeneratedRoot -ProjectSuffix ".Modules.Platform"
    $hostProjectDirectory = Split-Path -Parent $HostProjectPath
    $nugetConfigPath = Join-Path $WorkerProbeRoot "NuGet.config"

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
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Cephalon.Worker" Version="0.1.0-preview" />
    <PackageReference Include="Cephalon.Data" Version="0.1.0-preview" />
    <PackageReference Include="Cephalon.Ids.Sfid" Version="0.1.0-preview" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="$foundationProjectPath" />
    <ProjectReference Include="$moduleProjectPath" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $WorkerProbeProjectPath -Encoding UTF8

    @'
using Cephalon.Data.Registration;
using Cephalon.Engine.Runtime;
using Cephalon.Ids.Sfid.Registration;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var generatedHostRoot = args.Length > 0
    ? args[0]
    : throw new InvalidOperationException("The generated host root argument is required.");

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = generatedHostRoot,
    EnvironmentName = "Development"
});

builder.AddCephalon(engine =>
{
    engine.AddData();
    engine.AddSfidIds();
});

using var host = builder.Build();
await host.StartAsync();

var runtime = host.Services.GetRequiredService<IRuntime>();
var health = host.Services.GetRequiredService<RuntimeHealthEvaluator>();
var snapshotProvider = host.Services.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>();
var snapshot = snapshotProvider.CreateSnapshot();
var dependencies = health.EvaluateDependencies();

Console.WriteLine($"worker-status={runtime.Status}; blueprint={runtime.Manifest.AppProfile.BlueprintId}; dependencies={dependencies.Length}; snapshot-status={snapshot.Status.Status}");

if (runtime.Status != RuntimeStatus.Started)
{
    return 2;
}

if (!string.Equals(runtime.Manifest.AppProfile.BlueprintId, "modular-monolith", StringComparison.OrdinalIgnoreCase))
{
    return 3;
}

if (!runtime.Manifest.AppProfile.Patterns.Any(static pattern => string.Equals(pattern.Id, "cqrs", StringComparison.OrdinalIgnoreCase)))
{
    return 4;
}

if (!runtime.Manifest.AppProfile.Patterns.Any(static pattern => string.Equals(pattern.Id, "outbox", StringComparison.OrdinalIgnoreCase)))
{
    return 5;
}

await host.StopAsync();
return 0;
'@ | Set-Content -LiteralPath $WorkerProbeProgramPath -Encoding UTF8

    return [pscustomobject]([ordered]@{
        Root = $WorkerProbeRoot
        Project = $WorkerProbeProjectPath
        Program = $WorkerProbeProgramPath
        NuGetConfig = $nugetConfigPath
        HostConfigurationRoot = $hostProjectDirectory
    })
}

function New-ModularMonolithAdoptionRuntimeProbeRows {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status
    )

    @(
        [pscustomobject]([ordered]@{ Kind = "health"; Path = "/health/ready"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "engine-root"; Path = "/engine"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "snapshot"; Path = "/engine/snapshot"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "dependency-health"; Path = "/engine/dependencies"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "runtime-story"; Path = "/engine/runtime-story"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "rest-docs"; Path = "/scalar"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "worker-host"; Path = "generic-host-worker-probe"; Status = $Status })
    )
}

function New-ModularMonolithAdoptionAssertionRows {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Passed
    )

    @(
        [pscustomobject]([ordered]@{ Name = "runsOutsideRepository"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "publishesLocalPackages"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "installsCliFromTemporaryFeed"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "scaffoldsModularMonolithWithDataPatterns"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "seedsGeneratedLocalPackageFeed"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "replaysGeneratedAppDoctor"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "restoresGeneratedSolution"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "buildsGeneratedSolution"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "runsGeneratedHost"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesRestOperatorSurfaces"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesDependencyAndRuntimeStorySurfaces"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "buildsWorkerProbe"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "runsWorkerProbeFromGeneratedConfiguration"; Passed = $Passed })
    )
}

function Write-ModularMonolithAdoptionExecutionReport {
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
        ScenarioId = "modular-monolith-rest-worker-data"
        Status = $Status
        AppName = $AppName
        HostUrl = $HostUrl
        Configuration = $Configuration
        SkipPackageBuild = [bool]$SkipPackageBuild
        KeepOutput = [bool]$KeepOutput
        StartedAtUtc = $validationStartedAtUtc.ToString("O")
        CompletedAtUtc = $completedAtUtc.ToString("O")
        DurationMilliseconds = [math]::Round(($completedAtUtc - $validationStartedAtUtc).TotalMilliseconds, 2)
        Assertions = New-ModularMonolithAdoptionAssertionRows -Passed:$passed
        RuntimeProbes = New-ModularMonolithAdoptionRuntimeProbeRows -Status $runtimeProbeStatus
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
            WorkerProbeRoot = $workerProbeRoot
            WorkerProbeProject = $workerProbeProjectPath
            WorkerProbeProgram = $workerProbeProgramPath
            StdoutLog = $stdoutLogPath
            StderrLog = $stderrLogPath
            WorkerStdoutLog = $workerStdoutLogPath
            WorkerStderrLog = $workerStderrLogPath
            TemporaryOutputRetained = [bool]$KeepOutput
        })
        Error = $ErrorMessage
    })

    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $resolvedReportPath -Encoding UTF8
    Write-Host "Modular-monolith adoption execution report: $resolvedReportPath" -ForegroundColor Cyan
}

$process = $null
$previousNuGetPackages = $null
$restoreRepoPackageAssets = $false
$hostProjectPath = ""
$solutionPath = ""
$workerProbe = $null

try {
    New-Item -ItemType Directory -Path $packageFeedPath -Force | Out-Null
    New-Item -ItemType Directory -Path $toolPath -Force | Out-Null
    New-Item -ItemType Directory -Path $nuGetPackagesPath -Force | Out-Null
    New-Item -ItemType Directory -Path $workspaceRoot -Force | Out-Null

    $previousNuGetPackages = $env:NUGET_PACKAGES
    $env:NUGET_PACKAGES = $nuGetPackagesPath
    $restoreRepoPackageAssets = $true

    Write-Host ""
    Write-Host "Publishing repo-local Cephalon packages for modular-monolith adoption..." -ForegroundColor Cyan
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
    Write-Host "Replaying machine-level doctor checks through the installed CLI..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @("doctor")

    Write-Host ""
    Write-Host "Scaffolding a modular-monolith REST/data host outside the repository..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @(
        "new",
        $AppName,
        "--output", $generatedRoot,
        "--blueprint", "ModularMonolith",
        "--pattern", "CQRS",
        "--pattern", "Outbox",
        "--transport", "RestApi")

    $hostProjectPath = Resolve-GeneratedHostProjectPath -GeneratedRoot $generatedRoot -AppName $AppName
    $solutionPath = Join-Path $generatedRoot "$AppName.slnx"
    if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
        throw "Expected generated solution at '$solutionPath'."
    }

    Assert-GeneratedModularMonolithDataFoundation -HostProjectPath $hostProjectPath

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
    Write-Host "Running the generated REST host..." -ForegroundColor Cyan
    $previousAspNetCoreUrls = $env:ASPNETCORE_URLS
    $previousDotNetEnvironment = $env:DOTNET_ENVIRONMENT
    try {
        $env:ASPNETCORE_URLS = $HostUrl
        $env:DOTNET_ENVIRONMENT = "Development"

        $process = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList @("run", "--project", $hostProjectPath, "-c", $Configuration, "--no-build") `
            -WorkingDirectory $generatedRoot `
            -RedirectStandardOutput $stdoutLogPath `
            -RedirectStandardError $stderrLogPath `
            -PassThru `
            -NoNewWindow
    }
    finally {
        $env:ASPNETCORE_URLS = $previousAspNetCoreUrls
        $env:DOTNET_ENVIRONMENT = $previousDotNetEnvironment
    }

    Wait-ForHttpSuccess -Uri "$HostUrl/health/ready" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/snapshot" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/dependencies" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/runtime-story" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/scalar" -TimeoutSeconds $TimeoutSeconds -Process $process

    Write-Host ""
    Write-Host "Creating and running the generic-host Worker probe against the generated configuration..." -ForegroundColor Cyan
    $workerProbe = Write-WorkerProbeProject `
        -WorkerProbeRoot $workerProbeRoot `
        -WorkerProbeProjectPath $workerProbeProjectPath `
        -WorkerProbeProgramPath $workerProbeProgramPath `
        -GeneratedRoot $generatedRoot `
        -AppName $AppName `
        -HostProjectPath $hostProjectPath `
        -PackageFeedPath $generatedPackageFeedPath

    Invoke-DotNet -WorkingDirectory $workerProbeRoot -Arguments @("restore", $workerProbeProjectPath)
    Invoke-DotNet -WorkingDirectory $workerProbeRoot -Arguments @("build", $workerProbeProjectPath, "-c", $Configuration, "--no-restore")

    $workerProcess = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--project", $workerProbeProjectPath, "-c", $Configuration, "--no-build", "--", $workerProbe.HostConfigurationRoot) `
        -WorkingDirectory $workerProbeRoot `
        -RedirectStandardOutput $workerStdoutLogPath `
        -RedirectStandardError $workerStderrLogPath `
        -PassThru `
        -Wait `
        -NoNewWindow

    if ($workerProcess.ExitCode -ne 0) {
        throw "Worker probe failed with exit code $($workerProcess.ExitCode)."
    }

    Write-Host ""
    Write-Host "Modular-monolith REST/Worker/data adoption validation completed successfully." -ForegroundColor Green
    Write-ModularMonolithAdoptionExecutionReport -Status "passed"
    Write-Host "Temporary package feed: $packageFeedPath" -ForegroundColor Cyan
    Write-Host "Installed tool path: $toolPath" -ForegroundColor Cyan
    Write-Host "NuGet package cache: $nuGetPackagesPath" -ForegroundColor Cyan
    Write-Host "Generated app root: $generatedRoot" -ForegroundColor Cyan
    Write-Host "Worker probe root: $workerProbeRoot" -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "Modular-monolith adoption validation failed." -ForegroundColor Yellow
    Write-RecentLogs -Path $stdoutLogPath -Label "Generated host stdout"
    Write-RecentLogs -Path $stderrLogPath -Label "Generated host stderr"
    Write-RecentLogs -Path $workerStdoutLogPath -Label "Worker probe stdout"
    Write-RecentLogs -Path $workerStderrLogPath -Label "Worker probe stderr"
    try {
        Write-ModularMonolithAdoptionExecutionReport -Status "failed" -ErrorMessage $_.Exception.Message
    }
    catch {
        Write-Warning "Could not write modular-monolith adoption execution report: $($_.Exception.Message)"
    }
    throw
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit()
    }

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
