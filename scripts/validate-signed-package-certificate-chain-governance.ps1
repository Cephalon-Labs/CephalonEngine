param(
    [string]$AppName = "Cephalon.CertChainSmoke",
    [string]$HostUrl = "http://127.0.0.1:18085",
    [int]$TimeoutSeconds = 120,
    [string]$Configuration = "Release",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$validationScriptPath = Join-Path $PSScriptRoot "validate-signed-package-governance.ps1"

$invokeArguments = @{
    AppName = $AppName
    HostUrl = $HostUrl
    TimeoutSeconds = $TimeoutSeconds
    Configuration = $Configuration
    SignatureTrustMode = "CertificateChain"
}

if ($SkipPackageBuild) {
    $invokeArguments["SkipPackageBuild"] = $true
}

if ($KeepOutput) {
    $invokeArguments["KeepOutput"] = $true
}

& $validationScriptPath @invokeArguments
