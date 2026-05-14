param(
    [string]$AppName = "Cephalon.VerticalSliceEventingSmoke",
    [string]$HostUrl = "http://127.0.0.1:18085",
    [int]$TimeoutSeconds = 120,
    [string]$Configuration = "Release",
    [string]$ReportPath = "artifacts/adoption-smoke/vertical-slice-eventing-adoption.json",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$validationStartedAtUtc = [DateTimeOffset]::UtcNow
$repoRoot = Split-Path -Parent $PSScriptRoot
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cvsea-" + ([Guid]::NewGuid().ToString("N")).Substring(0, 12))
$packageFeedPath = Join-Path $tempRoot "package-feed"
$toolPath = Join-Path $tempRoot ".tools\cephalon"
$nuGetPackagesPath = Join-Path $tempRoot ".nuget\packages"
$workspaceRoot = Join-Path $tempRoot "workspace"
$generatedRoot = Join-Path $workspaceRoot $AppName
$generatedPackageFeedPath = Join-Path $generatedRoot ".cephalon\packages"
$stdoutLogPath = Join-Path $tempRoot "vertical-slice-eventing.stdout.log"
$stderrLogPath = Join-Path $tempRoot "vertical-slice-eventing.stderr.log"
$publicationId = "vertical-slice-eventing-" + [Guid]::NewGuid().ToString("N")
$packageProjectPaths = @(
    "src/Cephalon.Abstractions/Cephalon.Abstractions.csproj",
    "src/Cephalon.Analyzers/Cephalon.Analyzers.csproj",
    "src/Cephalon.Diagnostics/Cephalon.Diagnostics.csproj",
    "src/Cephalon.Engine/Cephalon.Engine.csproj",
    "src/Cephalon.Engine.SourceGen/Cephalon.Engine.SourceGen.csproj",
    "src/Cephalon.AspNetCore/Cephalon.AspNetCore.csproj",
    "src/Cephalon.Audit/Cephalon.Audit.csproj",
    "src/Cephalon.Data/Cephalon.Data.csproj",
    "src/Cephalon.Ids.Sfid/Cephalon.Ids.Sfid.csproj",
    "src/Cephalon.Behaviors/Cephalon.Behaviors.csproj",
    "src/Cephalon.Behaviors.Http/Cephalon.Behaviors.Http.csproj",
    "src/Cephalon.Behaviors.SourceGen/Cephalon.Behaviors.SourceGen.csproj",
    "src/Cephalon.Eventing/Cephalon.Eventing.csproj",
    "src/Cephalon.Eventing.Behaviors/Cephalon.Eventing.Behaviors.csproj",
    "src/Cephalon.Cli/Cephalon.Cli.csproj",
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
        $arguments["Body"] = ($Body | ConvertTo-Json -Depth 8)
    }

    $response = Invoke-WebRequest @arguments
    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw "Expected HTTP success from '$Uri' but received $($response.StatusCode)."
    }

    return $response.Content
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

function Assert-GeneratedVerticalSliceEventingFoundation {
    param(
        [Parameter(Mandatory = $true)]
        [string]$HostProjectPath
    )

    $hostProjectDirectory = Split-Path -Parent $HostProjectPath
    $appModelSettingsPath = Join-Path $hostProjectDirectory "Configurations\AddEngine.AppModel.json"
    $dataSettingsPath = Join-Path $hostProjectDirectory "Configurations\AddEngine.Data.json"
    $messagingSettingsPath = Join-Path $hostProjectDirectory "Configurations\AddEngine.Messaging.json"
    $hostProgramPath = Join-Path $hostProjectDirectory "Program.cs"

    foreach ($path in @($appModelSettingsPath, $dataSettingsPath, $messagingSettingsPath, $hostProgramPath)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Expected generated vertical-slice adoption file '$path'."
        }
    }

    $hostProject = Get-Content -LiteralPath $HostProjectPath -Raw
    foreach ($token in @("Cephalon.AspNetCore", "Cephalon.Data", "Cephalon.Eventing", "Cephalon.Ids.Sfid", "Cephalon.Engine.SourceGen")) {
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

    $generatedModuleText = (
        Get-ChildItem -Path $generatedSourceRoot -Recurse -Filter "*Module.cs" -File |
            ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }
    ) -join [Environment]::NewLine
    foreach ($token in @("IOutboxContributor", "IOutbox", "OutboxDescriptor", '"application-events"', '"vertical-slice-eventing-outbox"', '"generated-process-local"')) {
        if (-not $generatedModuleText.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated module source to contain outbox token '$token'."
        }
    }

    $program = Get-Content -LiteralPath $hostProgramPath -Raw
    foreach ($token in @(
        "builder.AddCephalonProjectConfigurations();",
        "engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>",
        "behaviors.AddHttpBehaviorBindings();",
        "engine.AddData();",
        "engine.AddSfidIds();",
        "engine.AddEventingFromConfiguration(builder.Configuration);",
        "app.MapCephalon();")) {
        if (-not $program.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated Program.cs to contain '$token'."
        }
    }

    $appModel = Get-Content -LiteralPath $appModelSettingsPath -Raw
    foreach ($token in @('"Blueprint": "modular-vertical-slice"', '"cqrs"', '"outbox"', '"event-driven-integration"', '"rest-api"')) {
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

    $messagingSettings = Get-Content -LiteralPath $messagingSettingsPath -Raw
    foreach ($token in @('"Channels"', '"application-events"', '"InProcessSubscriptions"', '"EnableExecution": false', '"Publications"', '"Routing"', '"application.*"')) {
        if (-not $messagingSettings.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "Expected generated messaging settings to contain '$token'."
        }
    }
}

function New-VerticalSliceEventingAdoptionRuntimeProbeRows {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status
    )

    @(
        [pscustomobject]([ordered]@{ Kind = "health"; Path = "/health/ready"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "engine-root"; Path = "/engine"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "snapshot"; Path = "/engine/snapshot"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "event-publication-runtime"; Path = "/engine/event-publications/runtime"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "event-publication-command"; Path = "/engine/event-publications"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "event-publication-runtime-detail"; Path = "/engine/event-publications/runtime/{publicationId}"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "event-dispatch-terminal-failures"; Path = "/engine/event-dispatches/terminal-failures"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "event-dispatch-remediation-summary"; Path = "/engine/event-dispatch-remediation-commands/summary"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "diagnostics"; Path = "/engine/diagnostics"; Status = $Status })
        [pscustomobject]([ordered]@{ Kind = "rest-docs"; Path = "/scalar"; Status = $Status })
    )
}

function New-VerticalSliceEventingAdoptionAssertionRows {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Passed
    )

    @(
        [pscustomobject]([ordered]@{ Name = "runsOutsideRepository"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "publishesLocalPackages"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "installsCliFromTemporaryFeed"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "scaffoldsVerticalSliceEventingApp"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "seedsGeneratedLocalPackageFeed"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "replaysGeneratedAppDoctor"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "restoresGeneratedSolution"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "buildsGeneratedSolution"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "runsGeneratedHost"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "publishesEventThroughOperatorRoute"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesEventingRuntimeTruth"; Passed = $Passed })
        [pscustomobject]([ordered]@{ Name = "validatesDispatchRemediationAndDiagnosticsSurfaces"; Passed = $Passed })
    )
}

function Write-VerticalSliceEventingAdoptionExecutionReport {
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
        ScenarioId = "vertical-slice-eventing-outbox"
        Status = $Status
        AppName = $AppName
        HostUrl = $HostUrl
        Configuration = $Configuration
        SkipPackageBuild = [bool]$SkipPackageBuild
        KeepOutput = [bool]$KeepOutput
        StartedAtUtc = $validationStartedAtUtc.ToString("O")
        CompletedAtUtc = $completedAtUtc.ToString("O")
        DurationMilliseconds = [math]::Round(($completedAtUtc - $validationStartedAtUtc).TotalMilliseconds, 2)
        Assertions = New-VerticalSliceEventingAdoptionAssertionRows -Passed:$passed
        RuntimeProbes = New-VerticalSliceEventingAdoptionRuntimeProbeRows -Status $runtimeProbeStatus
        Publication = [pscustomobject]([ordered]@{
            Id = $publicationId
            EventType = "application.checkout.requested"
            RequestedChannelId = "auto"
            ExpectedChannelId = "application-events"
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
            StdoutLog = $stdoutLogPath
            StderrLog = $stderrLogPath
            TemporaryOutputRetained = [bool]$KeepOutput
        })
        Error = $ErrorMessage
    })

    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $resolvedReportPath -Encoding UTF8
    Write-Host "Vertical-slice Eventing adoption execution report: $resolvedReportPath" -ForegroundColor Cyan
}

$process = $null
$previousNuGetPackages = $null
$solutionPath = ""
$hostProjectPath = ""
$hostAssemblyPath = ""

try {
    New-Item -ItemType Directory -Path $packageFeedPath -Force | Out-Null
    New-Item -ItemType Directory -Path $toolPath -Force | Out-Null
    New-Item -ItemType Directory -Path $nuGetPackagesPath -Force | Out-Null
    New-Item -ItemType Directory -Path $workspaceRoot -Force | Out-Null

    $previousNuGetPackages = $env:NUGET_PACKAGES
    $env:NUGET_PACKAGES = $nuGetPackagesPath

    Write-Host ""
    Write-Host "Publishing repo-local Cephalon packages for vertical-slice Eventing adoption..." -ForegroundColor Cyan
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
    Write-Host "Scaffolding a modular vertical-slice Eventing/outbox host outside the repository..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @(
        "new",
        $AppName,
        "--output", $generatedRoot,
        "--blueprint", "ModularVerticalSlice",
        "--module", "Orders",
        "--feature", "Checkout",
        "--pattern", "CQRS",
        "--pattern", "Outbox",
        "--transport", "RestApi",
        "--technology", "EventDrivenIntegration")

    $hostProjectPath = Resolve-GeneratedHostProjectPath -GeneratedRoot $generatedRoot -AppName $AppName
    $solutionPath = Join-Path $generatedRoot "$AppName.slnx"
    if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
        throw "Expected generated solution at '$solutionPath'."
    }

    Assert-GeneratedVerticalSliceEventingFoundation -HostProjectPath $hostProjectPath

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
    $hostProjectDirectory = Split-Path -Parent $hostProjectPath
    $hostAssemblyPath = Join-Path $hostProjectDirectory "bin\$Configuration\net10.0\$([System.IO.Path]::GetFileNameWithoutExtension($hostProjectPath)).dll"
    if (-not (Test-Path -LiteralPath $hostAssemblyPath -PathType Leaf)) {
        throw "Expected built generated host assembly at '$hostAssemblyPath'."
    }

    $previousAspNetCoreUrls = $env:ASPNETCORE_URLS
    $previousDotNetEnvironment = $env:DOTNET_ENVIRONMENT
    try {
        $env:ASPNETCORE_URLS = $HostUrl
        $env:DOTNET_ENVIRONMENT = "Development"

        $process = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList @($hostAssemblyPath) `
            -WorkingDirectory $hostProjectDirectory `
            -RedirectStandardOutput $stdoutLogPath `
            -RedirectStandardError $stderrLogPath `
            -PassThru `
            -WindowStyle Hidden
    }
    finally {
        $env:ASPNETCORE_URLS = $previousAspNetCoreUrls
        $env:DOTNET_ENVIRONMENT = $previousDotNetEnvironment
    }

    Wait-ForHttpSuccess -Uri "$HostUrl/health/ready" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/snapshot" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/event-publications/runtime" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/event-dispatches/terminal-failures" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/event-dispatch-remediation-commands/summary" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/diagnostics" -TimeoutSeconds $TimeoutSeconds -Process $process
    Wait-ForHttpSuccess -Uri "$HostUrl/scalar" -TimeoutSeconds $TimeoutSeconds -Process $process

    Write-Host ""
    Write-Host "Publishing an application event through the generated operator route..." -ForegroundColor Cyan
    $publicationBody = [pscustomobject]([ordered]@{
        Id = $publicationId
        ChannelId = "auto"
        EventType = "application.checkout.requested"
        ContentType = "application/json"
        CorrelationId = "vertical-slice-eventing-adoption"
        TenantId = "demo-tenant"
        ActorId = "cephalon-adoption-smoke"
        Payload = [pscustomobject]([ordered]@{
            checkoutId = "checkout-" + [Guid]::NewGuid().ToString("N")
            total = 42.25
            currency = "USD"
        })
        Metadata = [pscustomobject]([ordered]@{
            scenario = "vertical-slice-eventing-outbox"
            blueprint = "modular-vertical-slice"
        })
    })
    $publicationResult = Invoke-HttpJson -Uri "$HostUrl/engine/event-publications" -Method "POST" -Body $publicationBody
    foreach ($token in @($publicationId, "application-events", "application.checkout.requested", "Accepted")) {
        if (-not $publicationResult.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected event publication result to contain '$token'."
        }
    }

    $publicationRuntime = Invoke-HttpJson -Uri "$HostUrl/engine/event-publications/runtime/$publicationId"
    foreach ($token in @($publicationId, "application-events", "application.checkout.requested", "Accepted", "outbox")) {
        if (-not $publicationRuntime.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected event publication runtime state to contain '$token'."
        }
    }

    $terminalFailures = Invoke-HttpJson -Uri "$HostUrl/engine/event-dispatches/terminal-failures"
    $null = $terminalFailures | ConvertFrom-Json -Depth 8

    $diagnostics = Invoke-HttpJson -Uri "$HostUrl/engine/diagnostics"
    foreach ($token in @("Cephalon.Engine", "Cephalon.Eventing")) {
        if (-not $diagnostics.Contains($token, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected diagnostics surface to contain '$token'."
        }
    }

    Write-VerticalSliceEventingAdoptionExecutionReport -Status "passed"
    Write-Host ""
    Write-Host "Vertical-slice Eventing adoption validation completed successfully." -ForegroundColor Green
}
catch {
    Write-VerticalSliceEventingAdoptionExecutionReport -Status "failed" -ErrorMessage $_.Exception.Message
    Write-RecentLogs -Path $stdoutLogPath -Label "Generated host stdout"
    Write-RecentLogs -Path $stderrLogPath -Label "Generated host stderr"
    throw
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit(5000) | Out-Null
    }

    if ($null -ne $previousNuGetPackages) {
        $env:NUGET_PACKAGES = $previousNuGetPackages
    }
    else {
        Remove-Item Env:\NUGET_PACKAGES -ErrorAction SilentlyContinue
    }

    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
