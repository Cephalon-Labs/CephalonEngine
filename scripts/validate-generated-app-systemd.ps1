param(
    [string]$AppName = "Cephalon.SystemdSmoke",
    [string]$Configuration = "Release",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$cliProjectPath = Join-Path $repoRoot "src\Cephalon.Cli\Cephalon.Cli.csproj"
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-generated-systemd-" + [Guid]::NewGuid().ToString("N"))
$generatedRoot = Join-Path $tempRoot "app"
$hostProjectName = "$AppName.Host"
$hostProjectPath = Join-Path $generatedRoot "src\$hostProjectName\$hostProjectName.csproj"
$publishOutputPath = Join-Path $generatedRoot "artifacts\publish\$hostProjectName"
$publishedDllPath = Join-Path $publishOutputPath "$hostProjectName.dll"
$systemdReadmePath = Join-Path $generatedRoot "deploy\linux\systemd\README.md"
$serviceTemplatePath = Join-Path $generatedRoot "deploy\linux\systemd\$AppName.service"
$envTemplatePath = Join-Path $generatedRoot "deploy\linux\systemd\$AppName.env"
$verificationRoot = Join-Path $tempRoot "systemd-verify"
$serviceVerifyPath = Join-Path $verificationRoot "$AppName.service"
$envVerifyPath = Join-Path $verificationRoot "$AppName.env"

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

function Invoke-Wsl {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command
    )

    $result = & wsl.exe bash -lc $Command
    if ($LASTEXITCODE -ne 0) {
        throw "WSL command failed: $Command"
    }

    return ($result | Out-String).Trim()
}

function Convert-ToWslPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WindowsPath
    )

    $normalizedWindowsPath = $WindowsPath -replace '\\', '/'
    $converted = & wsl.exe wslpath -a $normalizedWindowsPath
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to convert Windows path '$WindowsPath' to a WSL path."
    }

    return ($converted | Out-String).Trim()
}

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

    foreach ($path in @(
        $publishedDllPath,
        $systemdReadmePath,
        $serviceTemplatePath,
        $envTemplatePath))
    {
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Expected generated asset at '$path'."
        }
    }

    $null = Invoke-Wsl -Command "which systemd-analyze"

    New-Item -ItemType Directory -Path $verificationRoot -Force | Out-Null
    Copy-Item -LiteralPath $envTemplatePath -Destination $envVerifyPath -Force

    $publishOutputPathWsl = Convert-ToWslPath $publishOutputPath
    $publishedDllPathWsl = Convert-ToWslPath $publishedDllPath
    $envVerifyPathWsl = Convert-ToWslPath $envVerifyPath
    $serviceVerifyPathWsl = Convert-ToWslPath $serviceVerifyPath

    $serviceContents = Get-Content -LiteralPath $serviceTemplatePath -Raw
    $serviceContents = $serviceContents.Replace("/opt/$AppName/current/$hostProjectName.dll", $publishedDllPathWsl)
    $serviceContents = $serviceContents.Replace("/opt/$AppName/current", $publishOutputPathWsl)
    $serviceContents = $serviceContents.Replace("/etc/cephalon/$AppName.env", $envVerifyPathWsl)
    Set-Content -LiteralPath $serviceVerifyPath -Value $serviceContents -Encoding utf8

    Invoke-Wsl -Command "systemd-analyze verify '$serviceVerifyPathWsl'"

    Write-Host ""
    Write-Host "Generated app Linux systemd validation completed successfully." -ForegroundColor Green
    Write-Host "Published output: $publishOutputPath" -ForegroundColor Cyan
    Write-Host "Verified unit: $serviceVerifyPathWsl" -ForegroundColor Cyan
}
finally {
    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
