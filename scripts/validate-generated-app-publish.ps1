param(
    [string]$AppName = "Cephalon.PublishSmoke",
    [string]$HostUrl = "http://127.0.0.1:18080",
    [int]$TimeoutSeconds = 120,
    [string]$Configuration = "Release",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$cliProjectPath = Join-Path $repoRoot "src\Cephalon.Cli\Cephalon.Cli.csproj"
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-generated-publish-" + [Guid]::NewGuid().ToString("N"))
$generatedRoot = Join-Path $tempRoot "app"
$stdoutLogPath = Join-Path $tempRoot "published-app.stdout.log"
$stderrLogPath = Join-Path $tempRoot "published-app.stderr.log"
$hostProjectName = "$AppName.Host"
$hostProjectPath = Join-Path $generatedRoot "src\$hostProjectName\$hostProjectName.csproj"
$publishOutputPath = Join-Path $generatedRoot "artifacts\publish\$hostProjectName"
$publishedDllPath = Join-Path $publishOutputPath "$hostProjectName.dll"

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
            throw "Published host exited before '$Uri' became ready. Exit code: $($Process.ExitCode)."
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

$process = $null

try {
    New-Item -ItemType Directory -Path $generatedRoot -Force | Out-Null

    Invoke-DotNet -WorkingDirectory $repoRoot -Arguments @(
        "run",
        "--project", $cliProjectPath,
        "--",
        "new",
        $AppName,
        "--output", $generatedRoot)

    $publishPackageArguments = @{
        Configuration = $Configuration
        OutputPath = (Join-Path $generatedRoot ".cephalon\packages")
    }

    if ($SkipPackageBuild) {
        $publishPackageArguments["SkipBuild"] = $true
    }

    & $publishPackagesScriptPath @publishPackageArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Package publishing failed."
    }

    Invoke-DotNet -WorkingDirectory $generatedRoot -Arguments @(
        "publish",
        $hostProjectPath,
        "-c", $Configuration,
        "-p:PublishProfile=CephalonFolder")

    if (-not (Test-Path -LiteralPath $publishedDllPath)) {
        throw "Expected published host at '$publishedDllPath'."
    }

    $previousAspNetCoreUrls = $env:ASPNETCORE_URLS
    $previousDotNetEnvironment = $env:DOTNET_ENVIRONMENT
    try {
        $env:ASPNETCORE_URLS = $HostUrl
        $env:DOTNET_ENVIRONMENT = "PublishedSmoke"

        $process = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList @($publishedDllPath) `
            -WorkingDirectory $publishOutputPath `
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
    Wait-ForHttpSuccess -Uri "$HostUrl/scalar" -TimeoutSeconds $TimeoutSeconds -Process $process

    Write-Host ""
    Write-Host "Generated app publish validation completed successfully." -ForegroundColor Green
    Write-Host "Published output: $publishOutputPath" -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "Generated app publish validation failed." -ForegroundColor Yellow
    Write-RecentLogs -Path $stdoutLogPath -Label "Published app stdout"
    Write-RecentLogs -Path $stderrLogPath -Label "Published app stderr"
    throw
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit()
    }

    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
