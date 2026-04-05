param(
    [string]$AppName = "Cephalon.ContainerImageSmoke",
    [string]$Configuration = "Release",
    [switch]$SkipPackageBuild,
    [switch]$SkipRegistryPush,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$cliProjectPath = Join-Path $repoRoot "src\Cephalon.Cli\Cephalon.Cli.csproj"
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-generated-container-image-" + [Guid]::NewGuid().ToString("N"))
$generatedRoot = Join-Path $tempRoot "app"
$guidePath = Join-Path $generatedRoot "deploy\container-image\README.md"
$publishScriptPath = Join-Path $generatedRoot "deploy\container-image\publish-image.ps1"
$dockerfilePath = Join-Path $generatedRoot "Dockerfile"
$nuGetConfigPath = Join-Path $generatedRoot "NuGet.config"
$builtImageTags = [System.Collections.Generic.List[string]]::new()
$registryContainerName = $null
$registryPort = $null

function Invoke-Process {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FileName,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        & $FileName @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "$FileName command failed: $FileName $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-PwshScript {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptPath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & pwsh -NoLogo -NoProfile -File $ScriptPath @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "PowerShell script failed: $ScriptPath $($Arguments -join ' ')"
    }

    return ($output | Out-String)
}

function Get-FreeTcpPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()

    try {
        return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

function Wait-ForHttpEndpoint {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [int]$TimeoutSeconds = 30
    )

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        try {
            Invoke-RestMethod -Uri $Uri -Method Get -TimeoutSec 5 | Out-Null
            return
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }
    while ([DateTimeOffset]::UtcNow -lt $deadline)

    throw "Timed out waiting for HTTP endpoint '$Uri'."
}

try {
    & docker version 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker is required for the generated container-image validation baseline."
    }

    New-Item -ItemType Directory -Path $generatedRoot -Force | Out-Null

    Invoke-Process -FileName "dotnet" -WorkingDirectory $repoRoot -Arguments @(
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

    foreach ($path in @(
        $guidePath,
        $publishScriptPath,
        $dockerfilePath,
        $nuGetConfigPath))
    {
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Expected generated asset at '$path'."
        }
    }

    $imageRepository = "cephalon-container-image-smoke"
    $tagSuffix = ([Guid]::NewGuid().ToString("N")).ToLowerInvariant()
    $primaryImage = $null
    $additionalTag = $null

    if ($SkipRegistryPush) {
        $primaryImage = ("cephalon-container-image-smoke:" + $tagSuffix).ToLowerInvariant()
        $additionalTag = ("cephalon-container-image-smoke:latest-" + $tagSuffix).ToLowerInvariant()
    }
    else {
        $registryPort = Get-FreeTcpPort
        $registryContainerName = ("cephalon-registry-" + [Guid]::NewGuid().ToString("N")).ToLowerInvariant()

        Invoke-Process -FileName "docker" -WorkingDirectory $repoRoot -Arguments @(
            "run",
            "-d",
            "-p", ("{0}:5000" -f $registryPort),
            "--name", $registryContainerName,
            "registry:2")

        Wait-ForHttpEndpoint -Uri ("http://127.0.0.1:{0}/v2/" -f $registryPort)

        $primaryImage = ("localhost:{0}/{1}:{2}" -f $registryPort, $imageRepository, $tagSuffix).ToLowerInvariant()
        $additionalTag = ("localhost:{0}/{1}:latest" -f $registryPort, $imageRepository).ToLowerInvariant()
    }

    $preview = Invoke-PwshScript -ScriptPath $publishScriptPath -Arguments @(
        "-Image", $primaryImage,
        "-AdditionalTags", $additionalTag,
        "-Push",
        "-Preview")
    $previewPlain = [regex]::Replace($preview, '\x1B\[[0-9;]*m', '')

    foreach ($expectedValue in @(
        "docker build",
        "-t $primaryImage",
        "-t $additionalTag",
        "docker push $primaryImage",
        "docker push $additionalTag"))
    {
        if (-not $previewPlain.Contains($expectedValue, [System.StringComparison]::Ordinal)) {
            throw "Preview output did not render the expected container-image content '$expectedValue'."
        }
    }

    $publishArguments = @(
        "-Image", $primaryImage,
        "-AdditionalTags", $additionalTag)

    if (-not $SkipRegistryPush) {
        $publishArguments += "-Push"
    }

    $publishOutput = Invoke-PwshScript -ScriptPath $publishScriptPath -Arguments $publishArguments
    $publishOutputPlain = [regex]::Replace($publishOutput, '\x1B\[[0-9;]*m', '')

    if (-not $publishOutputPlain.Contains("Container image publishing completed successfully.", [System.StringComparison]::Ordinal)) {
        throw "Live publish output did not report success."
    }

    foreach ($tag in @($primaryImage, $additionalTag)) {
        if (-not [string]::IsNullOrWhiteSpace($tag)) {
            $builtImageTags.Add($tag)
        }
    }

    if ($SkipRegistryPush) {
        if (-not $publishOutputPlain.Contains("Push skipped.", [System.StringComparison]::Ordinal)) {
            throw "Live publish output did not report that push was skipped."
        }
    }
    else {
        $catalog = Invoke-RestMethod -Uri ("http://127.0.0.1:{0}/v2/_catalog" -f $registryPort) -TimeoutSec 10
        if ($catalog.repositories -notcontains $imageRepository) {
            throw "Local registry catalog did not contain '$imageRepository'."
        }

        $tagList = Invoke-RestMethod -Uri ("http://127.0.0.1:{0}/v2/{1}/tags/list" -f $registryPort, $imageRepository) -TimeoutSec 10
        if ($tagList.tags -notcontains $tagSuffix) {
            throw "Local registry did not contain the expected published tag '$tagSuffix'."
        }

        if ($tagList.tags -notcontains "latest") {
            throw "Local registry did not contain the expected published tag 'latest'."
        }
    }

    Write-Host ""
    Write-Host "Generated app container-image validation completed successfully." -ForegroundColor Green
    Write-Host "Generated app root: $generatedRoot" -ForegroundColor Cyan
    Write-Host "Validated image tags: $($builtImageTags -join ', ')" -ForegroundColor Cyan

    if (-not $SkipRegistryPush) {
        Write-Host ("Validated local registry endpoint: http://127.0.0.1:{0}/v2/" -f $registryPort) -ForegroundColor Cyan
    }
}
finally {
    foreach ($tag in $builtImageTags) {
        & docker image rm $tag 1>$null 2>$null
        $null = $LASTEXITCODE
    }

    if (-not [string]::IsNullOrWhiteSpace($registryContainerName)) {
        & docker rm -f $registryContainerName 1>$null 2>$null
        $null = $LASTEXITCODE
    }

    if (-not $KeepOutput -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
