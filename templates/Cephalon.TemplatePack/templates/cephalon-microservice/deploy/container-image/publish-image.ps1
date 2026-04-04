param(
    [string]$Image = "replace-with-registry/cephalon-template-app:latest",
    [string[]]$AdditionalTags = @(),
    [string]$SourceRoot = (Join-Path (Join-Path $PSScriptRoot "..\..") "."),
    [string]$DockerfilePath = (Join-Path (Join-Path $PSScriptRoot "..\..") "Dockerfile"),
    [string]$Platform = "",
    [switch]$Pull,
    [switch]$Push,
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-PlaceholderValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value.StartsWith("replace-with-", [System.StringComparison]::Ordinal)
}

function Format-Command {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $parts = @($Command) + $Arguments

    return ($parts | ForEach-Object {
        if ($_ -match '\s') {
            '"' + $_ + '"'
        }
        else {
            $_
        }
    }) -join ' '
}

function Invoke-Docker {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        & docker @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "docker command failed: docker $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Get-ImageTags {
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $tags = [System.Collections.Generic.List[string]]::new()

    foreach ($tag in @($Image) + $AdditionalTags) {
        if ([string]::IsNullOrWhiteSpace($tag)) {
            continue
        }

        if ($seen.Add($tag)) {
            $tags.Add($tag)
        }
    }

    return $tags.ToArray()
}

function Get-DockerBuildArguments {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedSourceRoot,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedDockerfilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$ImageTags
    )

    $arguments = @("build", "-f", $ResolvedDockerfilePath)

    foreach ($tag in $ImageTags) {
        $arguments += @("-t", $tag)
    }

    if (-not [string]::IsNullOrWhiteSpace($Platform)) {
        $arguments += @("--platform", $Platform)
    }

    if ($Pull) {
        $arguments += "--pull"
    }

    $arguments += $ResolvedSourceRoot

    return $arguments
}

if (-not (Test-Path -LiteralPath $SourceRoot)) {
    throw "Expected generated app root at '$SourceRoot'."
}

if (-not (Test-Path -LiteralPath $DockerfilePath)) {
    throw "Expected generated Dockerfile at '$DockerfilePath'."
}

$resolvedSourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$resolvedDockerfilePath = (Resolve-Path -LiteralPath $DockerfilePath).Path
$nuGetConfigPath = Join-Path $resolvedSourceRoot "NuGet.config"

foreach ($path in @($resolvedDockerfilePath, $nuGetConfigPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Expected generated asset at '$path'."
    }
}

$imageTags = Get-ImageTags
if ($imageTags.Count -eq 0) {
    throw "Provide at least one -Image or -AdditionalTags value."
}

$buildArguments = Get-DockerBuildArguments -ResolvedSourceRoot $resolvedSourceRoot -ResolvedDockerfilePath $resolvedDockerfilePath -ImageTags $imageTags

if ($Preview) {
    Write-Host "Validated generated app root: $resolvedSourceRoot" -ForegroundColor Green
    Write-Host "Validated Dockerfile: $resolvedDockerfilePath" -ForegroundColor Green
    Write-Host "Preview only. The following commands would run:" -ForegroundColor Yellow
    Write-Host (Format-Command -Command "docker" -Arguments $buildArguments) -ForegroundColor Cyan

    if ($Push) {
        foreach ($tag in $imageTags) {
            Write-Host (Format-Command -Command "docker" -Arguments @("push", $tag)) -ForegroundColor Cyan
        }
    }
    else {
        Write-Host "Push skipped. Re-run with -Push when the target registry is ready." -ForegroundColor Yellow
    }

    return
}

foreach ($tag in $imageTags) {
    if (Test-PlaceholderValue -Value $tag) {
        throw "Set -Image and -AdditionalTags to real container-image references before running a live publish."
    }
}

$null = Get-Command docker -ErrorAction Stop

Invoke-Docker -WorkingDirectory $resolvedSourceRoot -Arguments $buildArguments

if ($Push) {
    foreach ($tag in $imageTags) {
        Invoke-Docker -WorkingDirectory $resolvedSourceRoot -Arguments @("push", $tag)
    }
}

Write-Host ""
Write-Host "Container image publishing completed successfully." -ForegroundColor Green
Write-Host "Source root: $resolvedSourceRoot" -ForegroundColor Cyan
Write-Host "Image tags: $($imageTags -join ', ')" -ForegroundColor Cyan

if ($Push) {
    Write-Host "The published image tags are ready for Kubernetes or other hosted container deployment surfaces." -ForegroundColor Cyan
}
else {
    Write-Host "Push skipped. Re-run with -Push when you want to publish the image tags to a registry." -ForegroundColor Yellow
}
