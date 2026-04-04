param(
    [switch]$UsePackages,
    [switch]$SkipBuild,
    [string]$HostUrl = "http://localhost:8080",
    [string]$CollectorHealthUrl = "http://localhost:13133/",
    [int]$TimeoutSeconds = 120
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$sampleRoot = Join-Path $repoRoot "samples\Cephalon.Sample.ModularMonolith"
$composeFiles = @(
    (Join-Path $sampleRoot "compose.yaml")
)

if ($UsePackages) {
    $composeFiles += Join-Path $sampleRoot "compose.packages.yaml"
}

function Invoke-DockerCompose {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$ComposeArguments,
        [switch]$IgnoreExitCode
    )

    & docker @ComposeArguments
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
        throw "docker command failed: docker $($ComposeArguments -join ' ')"
    }
}

function New-ComposeArguments {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $composeArguments = @("compose")
    foreach ($composeFile in $composeFiles) {
        $composeArguments += @("-f", $composeFile)
    }

    $composeArguments += $Arguments
    return $composeArguments
}

function Wait-ForHttpSuccess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
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

function Wait-ForHttpContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedContent,
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        try {
            $response = Invoke-WebRequest -Uri $Uri -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and
                $response.StatusCode -lt 300 -and
                $response.Content.Contains($ExpectedContent, [System.StringComparison]::Ordinal))
            {
                Write-Host "Validated content from $Uri" -ForegroundColor Green
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 2
    }
    while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for '$Uri' to contain '$ExpectedContent'."
}

$upArguments = @("up", "-d")
if (-not $SkipBuild) {
    $upArguments += "--build"
}

$composeUp = New-ComposeArguments -Arguments $upArguments
$composeLogs = New-ComposeArguments -Arguments @("logs", "--tail", "80")
$composeDown = New-ComposeArguments -Arguments @("down", "-v", "--remove-orphans")

try {
    Invoke-DockerCompose -ComposeArguments $composeUp

    Wait-ForHttpSuccess -Uri $CollectorHealthUrl -TimeoutSeconds $TimeoutSeconds
    Wait-ForHttpSuccess -Uri "$HostUrl/health/ready" -TimeoutSeconds $TimeoutSeconds
    Wait-ForHttpSuccess -Uri "$HostUrl/engine" -TimeoutSeconds $TimeoutSeconds
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/snapshot" -TimeoutSeconds $TimeoutSeconds
    Wait-ForHttpSuccess -Uri "$HostUrl/api/catalog/overview" -TimeoutSeconds $TimeoutSeconds

    if ($UsePackages) {
        Wait-ForHttpContent -Uri "$HostUrl/engine/packages" -ExpectedContent "reference-operations" -TimeoutSeconds $TimeoutSeconds
        Wait-ForHttpContent -Uri "$HostUrl/api/operations/status" -ExpectedContent "Operations module is running." -TimeoutSeconds $TimeoutSeconds
    }

    Write-Host ""
    Write-Host "Container runtime validation completed successfully." -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Container runtime validation failed. Recent compose logs:" -ForegroundColor Yellow
    Invoke-DockerCompose -ComposeArguments $composeLogs -IgnoreExitCode
    throw
}
finally {
    Invoke-DockerCompose -ComposeArguments $composeDown -IgnoreExitCode
}
