param(
    [string]$AppName = "Cephalon.SignedPackageSmoke",
    [string]$HostUrl = "http://127.0.0.1:18084",
    [int]$TimeoutSeconds = 120,
    [string]$Configuration = "Release",
    [switch]$SkipPackageBuild,
    [switch]$KeepOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishPackagesScriptPath = Join-Path $repoRoot "scripts\publish-package-artifacts.ps1"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-signed-package-governance-" + [Guid]::NewGuid().ToString("N"))
$packageFeedPath = Join-Path $tempRoot "package-feed"
$referencePackageArtifactsPath = Join-Path $tempRoot "reference-packages"
$signedPackageArtifactsPath = Join-Path $tempRoot "signed-packages"
$signingMaterialsPath = Join-Path $tempRoot "signing-materials"
$toolPath = Join-Path $tempRoot ".tools\cephalon"
$nuGetPackagesPath = Join-Path $tempRoot ".nuget\packages"
$workspaceRoot = Join-Path $tempRoot "workspace"
$generatedRoot = Join-Path $workspaceRoot $AppName
$generatedPackageFeedPath = Join-Path $generatedRoot ".cephalon\packages"
$pluginsRootPath = Join-Path $generatedRoot "plugins"
$stagedPackagePath = Join-Path $pluginsRootPath "reference-operations"
$trustedStdoutLogPath = Join-Path $tempRoot "trusted.stdout.log"
$trustedStderrLogPath = Join-Path $tempRoot "trusted.stderr.log"
$tamperedStdoutLogPath = Join-Path $tempRoot "tampered.stdout.log"
$tamperedStderrLogPath = Join-Path $tempRoot "tampered.stderr.log"
$packageProjectPaths = @(
    "src/Cephalon.Abstractions/Cephalon.Abstractions.csproj",
    "src/Cephalon.Engine/Cephalon.Engine.csproj",
    "src/Cephalon.AspNetCore/Cephalon.AspNetCore.csproj",
    "src/Cephalon.Behaviors/Cephalon.Behaviors.csproj",
    "src/Cephalon.Behaviors.Http/Cephalon.Behaviors.Http.csproj",
    "src/Cephalon.Behaviors.SourceGen/Cephalon.Behaviors.SourceGen.csproj",
    "src/Cephalon.Cli/Cephalon.Cli.csproj",
    "src/Cephalon.Observability/Cephalon.Observability.csproj",
    "src/Cephalon.Observability.OpenTelemetry/Cephalon.Observability.OpenTelemetry.csproj",
    "src/Cephalon.Observability.Serilog/Cephalon.Observability.Serilog.csproj",
    "src/Cephalon.ReferenceDocs/Cephalon.ReferenceDocs.csproj",
    "src/Cephalon.Scaffolding/Cephalon.Scaffolding.csproj"
)
$referenceModuleProjectPath = "samples/Cephalon.ReferenceModule.Operations/Cephalon.ReferenceModule.Operations.csproj"
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

function Resolve-GeneratedAppModelConfigurationPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$HostProjectPath,
        [Parameter(Mandatory = $true)]
        [string]$GeneratedRoot
    )

    $hostProjectDirectory = Split-Path -Parent $HostProjectPath
    $preferredPath = Join-Path $hostProjectDirectory "Configurations\AddEngine.AppModel.json"
    if (Test-Path -LiteralPath $preferredPath -PathType Leaf) {
        return $preferredPath
    }

    $candidates = @(Get-ChildItem -Path $GeneratedRoot -Recurse -Filter "AddEngine.AppModel.json" -File | Sort-Object FullName)
    if ($candidates.Count -eq 1) {
        return $candidates[0].FullName
    }

    throw "Could not determine the generated AddEngine.AppModel.json path under '$GeneratedRoot'."
}

function Resolve-PackageManifestPathInExtraction {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExtractionPath
    )

    foreach ($candidatePath in @(
            (Join-Path $ExtractionPath "content\cephalon.package.json"),
            (Join-Path $ExtractionPath "contentFiles\any\net10.0\cephalon.package.json")))
    {
        if (Test-Path -LiteralPath $candidatePath -PathType Leaf) {
            return $candidatePath
        }
    }

    $discoveredPaths = @(Get-ChildItem -Path $ExtractionPath -Recurse -Filter "cephalon.package.json" -File | Sort-Object FullName)
    if ($discoveredPaths.Count -eq 1) {
        return $discoveredPaths[0].FullName
    }

    throw "Could not determine the package manifest path inside '$ExtractionPath'."
}

function Resolve-PackageAssemblyPathInExtraction {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExtractionPath,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Manifest
    )

    $declaredAssemblyPath = [string]$Manifest.assembly
    if ([string]::IsNullOrWhiteSpace($declaredAssemblyPath)) {
        throw "Expected the package manifest to declare an assembly path."
    }

    $normalizedAssemblyPath = $declaredAssemblyPath.Replace('/', [System.IO.Path]::DirectorySeparatorChar).Replace('\', [System.IO.Path]::DirectorySeparatorChar)
    $directCandidate = Join-Path $ExtractionPath $normalizedAssemblyPath
    if (Test-Path -LiteralPath $directCandidate -PathType Leaf) {
        return $directCandidate
    }

    $assemblyFileName = [System.IO.Path]::GetFileName($declaredAssemblyPath)
    $discoveredPaths = @(Get-ChildItem -Path $ExtractionPath -Recurse -Filter $assemblyFileName -File | Sort-Object FullName)
    if ($discoveredPaths.Count -eq 1) {
        return $discoveredPaths[0].FullName
    }

    throw "Could not determine the package assembly path for '$declaredAssemblyPath' inside '$ExtractionPath'."
}

function New-SigningMaterial {
    param(
        [Parameter(Mandatory = $true)]
        [string]$OutputDirectory
    )

    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

    $keyId = "cephalon-labs-build"
    $signer = "Cephalon Labs Build"
    $privateKeyPath = Join-Path $OutputDirectory "trusted-signing-key.private.pem"
    $publicKeyPath = Join-Path $OutputDirectory "trusted-signing-key.public.pem"

    $rsa = [System.Security.Cryptography.RSA]::Create(2048)
    try {
        $publicKeyBytes = $rsa.ExportSubjectPublicKeyInfo()
        $fingerprint = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($publicKeyBytes)).ToLowerInvariant()
        [System.IO.File]::WriteAllText($privateKeyPath, $rsa.ExportPkcs8PrivateKeyPem())
        [System.IO.File]::WriteAllText($publicKeyPath, $rsa.ExportSubjectPublicKeyInfoPem())
    }
    finally {
        $rsa.Dispose()
    }

    return [pscustomobject]@{
        KeyId = $keyId
        Signer = $signer
        Fingerprint = $fingerprint
        PrivateKeyPath = $privateKeyPath
        PublicKeyPath = $publicKeyPath
    }
}

function Compress-DirectoryAsZip {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceDirectory,
        [Parameter(Mandatory = $true)]
        [string]$DestinationPath
    )

    if (Test-Path -LiteralPath $DestinationPath) {
        Remove-Item -LiteralPath $DestinationPath -Force
    }

    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $SourceDirectory,
        $DestinationPath,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $false)
}

function New-SignedPackageArtifact {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePackagePath,
        [Parameter(Mandatory = $true)]
        [string]$OutputPackagePath,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$SigningMaterial,
        [switch]$TamperSignature
    )

    $extractionPath = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-signed-package-artifact-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $extractionPath -Force | Out-Null

    try {
        [System.IO.Compression.ZipFile]::ExtractToDirectory($BasePackagePath, $extractionPath)

        $manifestPath = Resolve-PackageManifestPathInExtraction -ExtractionPath $extractionPath
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -Depth 20
        $assemblyPath = Resolve-PackageAssemblyPathInExtraction -ExtractionPath $extractionPath -Manifest $manifest

        $privateKeyPem = Get-Content -LiteralPath $SigningMaterial.PrivateKeyPath -Raw
        $rsa = [System.Security.Cryptography.RSA]::Create()
        try {
            $rsa.ImportFromPem($privateKeyPem)

            $assemblyStream = [System.IO.File]::OpenRead($assemblyPath)
            try {
                $assemblyHash = [System.Security.Cryptography.SHA256]::HashData($assemblyStream)
            }
            finally {
                $assemblyStream.Dispose()
            }

            $signatureBytes = $rsa.SignHash(
                $assemblyHash,
                [System.Security.Cryptography.HashAlgorithmName]::SHA256,
                [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
            if ($TamperSignature) {
                $signatureBytes[0] = $signatureBytes[0] -bxor 0xFF
            }

            $signatureValue = [Convert]::ToBase64String($signatureBytes)
        }
        finally {
            $rsa.Dispose()
        }

        if ($manifest.PSObject.Properties.Match("signatures").Count -gt 0) {
            $manifest.PSObject.Properties.Remove("signatures")
        }

        $signatureObject = [pscustomobject]@{
            type = "detached-signature"
            signer = $SigningMaterial.Signer
            keyId = $SigningMaterial.KeyId
            fingerprint = "sha256:$($SigningMaterial.Fingerprint)"
            algorithm = "RSA-SHA256"
            value = $signatureValue
        }

        if ($manifest.PSObject.Properties.Match("signature").Count -gt 0) {
            $manifest.signature = $signatureObject
        }
        else {
            $manifest | Add-Member -NotePropertyName signature -NotePropertyValue $signatureObject -Force
        }

        $manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding utf8
        Compress-DirectoryAsZip -SourceDirectory $extractionPath -DestinationPath $OutputPackagePath
        return $OutputPackagePath
    }
    finally {
        if (Test-Path -LiteralPath $extractionPath) {
            Remove-Item -LiteralPath $extractionPath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function Set-SignedPackagePolicyConfiguration {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigurationPath,
        [Parameter(Mandatory = $true)]
        [string]$PluginsRootPath,
        [Parameter(Mandatory = $true)]
        [string]$TrustedPublicKeyPath,
        [Parameter(Mandatory = $true)]
        [string]$KeyId
    )

    $configuration = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json -Depth 20
    if ($null -eq $configuration.Engine) {
        throw "Expected '$ConfigurationPath' to contain an Engine section."
    }

    if ($null -eq $configuration.Engine.Discovery) {
        $configuration.Engine | Add-Member -NotePropertyName Discovery -NotePropertyValue ([pscustomobject]@{}) -Force
    }

    $configuration.Engine.Discovery | Add-Member -NotePropertyName PackageDirectories -NotePropertyValue @(
        [pscustomobject]@{
            Path = $PluginsRootPath
            IncludeSubdirectories = $true
        }
    ) -Force

    $configuration.Engine | Add-Member -NotePropertyName PackagePolicy -NotePropertyValue ([pscustomobject]@{
            AllowAssemblyPathPackages = $false
            RequireVersion = $true
            RequireMinimumEngineVersion = $true
            RequireSupportedTargetFrameworks = $true
            RequirePublisherId = $true
            RequireSignatureFingerprint = $true
            RequireSignatureKeyId = $true
            RequireSignatureValue = $true
            RequireSignatureVerification = $true
        }) -Force

    $configuration.Engine | Add-Member -NotePropertyName Trust -NotePropertyValue ([pscustomobject]@{
            RequireTrustedPackages = $true
            TrustedSignaturePublicKeys = [pscustomobject]@{}
        }) -Force
    $configuration.Engine.Trust.TrustedSignaturePublicKeys | Add-Member -NotePropertyName $KeyId -NotePropertyValue $TrustedPublicKeyPath -Force

    $configuration | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $ConfigurationPath -Encoding utf8
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

function Wait-ForProcessExit {
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds
    )

    if (-not $Process.WaitForExit($TimeoutSeconds * 1000)) {
        throw "Timed out waiting for the generated host process to exit."
    }
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

function Resolve-ReferencePackagePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ArtifactsRootPath
    )

    $packages = @(Get-ChildItem -Path $ArtifactsRootPath -Filter "*.nupkg" -File |
            Where-Object { -not $_.Name.EndsWith(".snupkg", [System.StringComparison]::OrdinalIgnoreCase) } |
            Sort-Object Name)
    if ($packages.Count -ne 1) {
        throw "Expected exactly one reference module package in '$ArtifactsRootPath'."
    }

    return $packages[0].FullName
}

function Assert-SignedPackageRuntimeTruth {
    param(
        [Parameter(Mandatory = $true)]
        [string]$HostUrl,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$SigningMaterial
    )

    $packages = @(Invoke-RestMethod -Uri "$HostUrl/engine/packages" -TimeoutSec 15)
    $package = $packages | Where-Object { $_.id -eq "reference-operations" } | Select-Object -First 1
    if ($null -eq $package) {
        throw "Expected /engine/packages to contain the signed 'reference-operations' package."
    }

    if ($package.kind -ne "directory-manifest") {
        throw "Expected the staged package kind to be 'directory-manifest', but found '$($package.kind)'."
    }

    if ($package.publisherId -ne "cephalon-labs") {
        throw "Expected the signed package publisher to be 'cephalon-labs', but found '$($package.publisherId)'."
    }

    if (-not $package.isTrusted) {
        throw "Expected the signed package to be trusted."
    }

    if (-not $package.isSignatureVerified) {
        throw "Expected /engine/packages to report the signed package as cryptographically verified."
    }

    if ($package.signatureKeyId -ne $SigningMaterial.KeyId) {
        throw "Expected the package signature key id to be '$($SigningMaterial.KeyId)', but found '$($package.signatureKeyId)'."
    }

    if ($package.signatureFingerprint -ne $SigningMaterial.Fingerprint) {
        throw "Expected the package signature fingerprint to be '$($SigningMaterial.Fingerprint)', but found '$($package.signatureFingerprint)'."
    }

    if (-not $package.signatureVerificationReason.Contains("verified", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Expected the package signature verification reason to confirm verification, but found '$($package.signatureVerificationReason)'."
    }

    $packageSignature = @($package.signatures) | Where-Object { $_.keyId -eq $SigningMaterial.KeyId } | Select-Object -First 1
    if ($null -eq $packageSignature) {
        throw "Expected /engine/packages to expose the signed package signature entry."
    }

    if (-not $packageSignature.isVerified) {
        throw "Expected the staged package signature entry to be marked verified."
    }

    if ($packageSignature.verificationSource -ne "trusted-public-key") {
        throw "Expected the staged package signature to verify through 'trusted-public-key', but found '$($packageSignature.verificationSource)'."
    }

    $packagePolicy = Invoke-RestMethod -Uri "$HostUrl/engine/package-policy" -TimeoutSec 15
    if ($packagePolicy.allowAssemblyPathPackages) {
        throw "Expected /engine/package-policy to disable assembly-path package loading."
    }

    if (-not $packagePolicy.requireVersion -or
        -not $packagePolicy.requireMinimumEngineVersion -or
        -not $packagePolicy.requireSupportedTargetFrameworks -or
        -not $packagePolicy.requirePublisherId -or
        -not $packagePolicy.requireSignatureFingerprint -or
        -not $packagePolicy.requireSignatureKeyId -or
        -not $packagePolicy.requireSignatureValue -or
        -not $packagePolicy.requireSignatureVerification) {
        throw "Expected /engine/package-policy to require the signed-package metadata and verification contract."
    }

    $trustPolicy = Invoke-RestMethod -Uri "$HostUrl/engine/trust-policy" -TimeoutSec 15
    if (-not $trustPolicy.policy.requireTrustedPackages) {
        throw "Expected /engine/trust-policy to require trusted packages."
    }

    if (-not ($trustPolicy.policy.trustedSignaturePublicKeys.PSObject.Properties.Name -contains $SigningMaterial.KeyId)) {
        throw "Expected /engine/trust-policy to include the trusted signing key '$($SigningMaterial.KeyId)'."
    }

    $trustDecision = @($trustPolicy.packages) | Where-Object {
        $_.packageId -eq "reference-operations"
    } | Select-Object -First 1
    if ($null -eq $trustDecision -or -not $trustDecision.isTrusted) {
        throw "Expected /engine/trust-policy to expose a trusted decision for 'reference-operations'."
    }

    if (-not $trustDecision.isSignatureVerified) {
        throw "Expected /engine/trust-policy to expose the verified signature decision for 'reference-operations'."
    }

    if (-not $trustDecision.signatureVerificationReason.Contains("verified", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Expected /engine/trust-policy to expose a signature verification reason confirming verification."
    }

    $trustSignature = @($trustDecision.signatures) | Where-Object { $_.keyId -eq $SigningMaterial.KeyId } | Select-Object -First 1
    if ($null -eq $trustSignature -or -not $trustSignature.isVerified) {
        throw "Expected /engine/trust-policy to expose a verified signature entry for the trusted signing key."
    }

    if ($trustSignature.verificationSource -ne "trusted-public-key") {
        throw "Expected /engine/trust-policy to expose 'trusted-public-key' as the verification source."
    }

    $snapshot = Invoke-RestMethod -Uri "$HostUrl/engine/snapshot" -TimeoutSec 15
    $snapshotPackage = @($snapshot.manifest.packages) | Where-Object { $_.id -eq "reference-operations" } | Select-Object -First 1
    if ($null -eq $snapshotPackage -or -not $snapshotPackage.isTrusted -or -not $snapshotPackage.isSignatureVerified) {
        throw "Expected /engine/snapshot to expose the trusted, verified staged package."
    }

    $snapshotModule = @($snapshot.manifest.modules) | Where-Object {
        $_.id -eq "operations" -and $_.packageId -eq "reference-operations"
    } | Select-Object -First 1
    if ($null -eq $snapshotModule -or -not $snapshotModule.isTrusted) {
        throw "Expected /engine/snapshot to expose the trusted 'operations' module from the signed package."
    }

    $operationsStatus = Invoke-WebRequest -Uri "$HostUrl/api/operations/status" -TimeoutSec 15
    if (-not $operationsStatus.Content.Contains("Operations module is running.", [System.StringComparison]::Ordinal)) {
        throw "Expected /api/operations/status to confirm the signed operations module."
    }
}

function Assert-SignatureVerificationFailure {
    param(
        [Parameter(Mandatory = $true)]
        [string]$StdoutLogPath,
        [Parameter(Mandatory = $true)]
        [string]$StderrLogPath
    )

    $combined = ""
    if (Test-Path -LiteralPath $StdoutLogPath -PathType Leaf) {
        $combined += Get-Content -LiteralPath $StdoutLogPath -Raw
    }

    if (Test-Path -LiteralPath $StderrLogPath -PathType Leaf) {
        $combined += Get-Content -LiteralPath $StderrLogPath -Raw
    }

    if ([string]::IsNullOrWhiteSpace($combined)) {
        throw "Expected startup logs when the tampered package fails signature verification."
    }

    $mentionsSignatureVerification =
        $combined.Contains("must pass cryptographic signature verification", [System.StringComparison]::OrdinalIgnoreCase) -or
        $combined.Contains("failed cryptographic signature verification", [System.StringComparison]::OrdinalIgnoreCase)

    if (-not $mentionsSignatureVerification) {
        throw "Expected the tampered package startup logs to explain the signature verification failure."
    }

    if (-not $combined.Contains("reference-operations", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Expected the tampered package startup logs to reference 'reference-operations'."
    }
}

$trustedProcess = $null
$tamperedProcess = $null
$previousNuGetPackages = $null
$restoreRepoPackageAssets = $false

try {
    New-Item -ItemType Directory -Path $packageFeedPath -Force | Out-Null
    New-Item -ItemType Directory -Path $referencePackageArtifactsPath -Force | Out-Null
    New-Item -ItemType Directory -Path $signedPackageArtifactsPath -Force | Out-Null
    New-Item -ItemType Directory -Path $signingMaterialsPath -Force | Out-Null
    New-Item -ItemType Directory -Path $toolPath -Force | Out-Null
    New-Item -ItemType Directory -Path $nuGetPackagesPath -Force | Out-Null
    New-Item -ItemType Directory -Path $workspaceRoot -Force | Out-Null

    $previousNuGetPackages = $env:NUGET_PACKAGES
    $env:NUGET_PACKAGES = $nuGetPackagesPath
    $restoreRepoPackageAssets = $true

    Write-Host ""
    Write-Host "Publishing repo-local Cephalon packages..." -ForegroundColor Cyan
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
    Write-Host "Packing the reference module for staged signature governance..." -ForegroundColor Cyan
    & $publishPackagesScriptPath -Configuration $Configuration -OutputPath $referencePackageArtifactsPath -ProjectPaths $referenceModuleProjectPath
    if ($LASTEXITCODE -ne 0) {
        throw "Reference module package publishing failed."
    }

    $referencePackagePath = Resolve-ReferencePackagePath -ArtifactsRootPath $referencePackageArtifactsPath

    Write-Host ""
    Write-Host "Generating trusted signing material..." -ForegroundColor Cyan
    $signingMaterial = New-SigningMaterial -OutputDirectory $signingMaterialsPath

    Write-Host ""
    Write-Host "Creating trusted and tampered detached-signature package variants..." -ForegroundColor Cyan
    $trustedSignedPackagePath = Join-Path $signedPackageArtifactsPath "Cephalon.ReferenceModule.Operations.signed.nupkg"
    $tamperedSignedPackagePath = Join-Path $signedPackageArtifactsPath "Cephalon.ReferenceModule.Operations.tampered.nupkg"
    New-SignedPackageArtifact -BasePackagePath $referencePackagePath -OutputPackagePath $trustedSignedPackagePath -SigningMaterial $signingMaterial | Out-Null
    New-SignedPackageArtifact -BasePackagePath $referencePackagePath -OutputPackagePath $tamperedSignedPackagePath -SigningMaterial $signingMaterial -TamperSignature | Out-Null

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
    Write-Host "Scaffolding a fresh app outside the repository..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @("new", $AppName, "--output", $generatedRoot)

    $hostProjectPath = Resolve-GeneratedHostProjectPath -GeneratedRoot $generatedRoot -AppName $AppName
    $solutionPath = Join-Path $generatedRoot "$AppName.slnx"
    if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
        throw "Expected generated solution at '$solutionPath'."
    }

    Write-Host ""
    Write-Host "Seeding the generated local package feed..." -ForegroundColor Cyan
    & $publishPackagesScriptPath -Configuration $Configuration -OutputPath $generatedPackageFeedPath -SkipBuild -ProjectPaths $packageProjectPaths
    if ($LASTEXITCODE -ne 0) {
        throw "Package publishing to the generated local feed failed."
    }

    $generatedPackageReadmePath = Join-Path $generatedPackageFeedPath "README.md"
    if (-not (Test-Path -LiteralPath $generatedPackageReadmePath -PathType Leaf)) {
        throw "Expected generated local package-feed README at '$generatedPackageReadmePath' after seeding packages."
    }

    $generatedPackageReadme = Get-Content -LiteralPath $generatedPackageReadmePath -Raw
    if (-not $generatedPackageReadme.Contains("publish-package-artifacts.ps1", [System.StringComparison]::Ordinal)) {
        throw "Expected the generated local package-feed README to remain intact after seeding packages."
    }

    Write-Host ""
    Write-Host "Staging the trusted signed package into the generated app..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $pluginsRootPath -Force | Out-Null
    # Replay the shipped `cephalon package stage` flow so the generated host loads a directory-manifest package.
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @(
        "package",
        "stage",
        "--package", $trustedSignedPackagePath,
        "--output", $stagedPackagePath)

    foreach ($expectedStagedFile in @(
            "cephalon.package.json",
            "Cephalon.ReferenceModule.Operations.dll",
            "PACKAGE.md"))
    {
        $expectedStagedPath = Join-Path $stagedPackagePath $expectedStagedFile
        if (-not (Test-Path -LiteralPath $expectedStagedPath -PathType Leaf)) {
            throw "Expected staged package asset '$expectedStagedPath'."
        }
    }

    Write-Host ""
    Write-Host "Configuring package discovery, signature policy, and trust..." -ForegroundColor Cyan
    $appModelConfigurationPath = Resolve-GeneratedAppModelConfigurationPath -HostProjectPath $hostProjectPath -GeneratedRoot $generatedRoot
    Set-SignedPackagePolicyConfiguration `
        -ConfigurationPath $appModelConfigurationPath `
        -PluginsRootPath $pluginsRootPath `
        -TrustedPublicKeyPath $signingMaterial.PublicKeyPath `
        -KeyId $signingMaterial.KeyId

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
    Write-Host "Running the generated host with the trusted signed package..." -ForegroundColor Cyan
    $previousAspNetCoreUrls = $env:ASPNETCORE_URLS
    $previousDotNetEnvironment = $env:DOTNET_ENVIRONMENT
    try {
        $env:ASPNETCORE_URLS = $HostUrl
        $env:DOTNET_ENVIRONMENT = "Development"

        $trustedProcess = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList @("run", "--project", $hostProjectPath, "-c", $Configuration, "--no-build") `
            -WorkingDirectory $generatedRoot `
            -RedirectStandardOutput $trustedStdoutLogPath `
            -RedirectStandardError $trustedStderrLogPath `
            -PassThru `
            -NoNewWindow
    }
    finally {
        $env:ASPNETCORE_URLS = $previousAspNetCoreUrls
        $env:DOTNET_ENVIRONMENT = $previousDotNetEnvironment
    }

    Wait-ForHttpSuccess -Uri "$HostUrl/health/ready" -TimeoutSeconds $TimeoutSeconds -Process $trustedProcess
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/packages" -TimeoutSeconds $TimeoutSeconds -Process $trustedProcess
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/package-policy" -TimeoutSeconds $TimeoutSeconds -Process $trustedProcess
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/trust-policy" -TimeoutSeconds $TimeoutSeconds -Process $trustedProcess
    Wait-ForHttpSuccess -Uri "$HostUrl/engine/snapshot" -TimeoutSeconds $TimeoutSeconds -Process $trustedProcess
    Wait-ForHttpSuccess -Uri "$HostUrl/api/operations/status" -TimeoutSeconds $TimeoutSeconds -Process $trustedProcess
    Assert-SignedPackageRuntimeTruth -HostUrl $HostUrl -SigningMaterial $signingMaterial

    if ($null -ne $trustedProcess -and -not $trustedProcess.HasExited) {
        Stop-Process -Id $trustedProcess.Id -Force -ErrorAction SilentlyContinue
        $trustedProcess.WaitForExit()
    }

    Write-Host ""
    Write-Host "Restaging the tampered signed package and expecting startup failure..." -ForegroundColor Cyan
    Invoke-Cephalon -WorkingDirectory $workspaceRoot -Arguments @(
        "package",
        "stage",
        "--package", $tamperedSignedPackagePath,
        "--output", $stagedPackagePath,
        "--force")

    try {
        $env:ASPNETCORE_URLS = $HostUrl
        $env:DOTNET_ENVIRONMENT = "Development"

        $tamperedProcess = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList @("run", "--project", $hostProjectPath, "-c", $Configuration, "--no-build") `
            -WorkingDirectory $generatedRoot `
            -RedirectStandardOutput $tamperedStdoutLogPath `
            -RedirectStandardError $tamperedStderrLogPath `
            -PassThru `
            -NoNewWindow
    }
    finally {
        $env:ASPNETCORE_URLS = $previousAspNetCoreUrls
        $env:DOTNET_ENVIRONMENT = $previousDotNetEnvironment
    }

    Wait-ForProcessExit -Process $tamperedProcess -TimeoutSeconds $TimeoutSeconds
    if ($tamperedProcess.ExitCode -eq 0) {
        throw "Expected the generated host to fail when the staged package signature is tampered."
    }

    Assert-SignatureVerificationFailure -StdoutLogPath $tamperedStdoutLogPath -StderrLogPath $tamperedStderrLogPath

    Write-Host ""
    Write-Host "Signed package governance validation completed successfully." -ForegroundColor Green
    Write-Host "Temporary package feed: $packageFeedPath" -ForegroundColor Cyan
    Write-Host "Reference module package artifact: $referencePackagePath" -ForegroundColor Cyan
    Write-Host "Trusted signed package artifact: $trustedSignedPackagePath" -ForegroundColor Cyan
    Write-Host "Tampered signed package artifact: $tamperedSignedPackagePath" -ForegroundColor Cyan
    Write-Host "Trusted signing key: $($signingMaterial.PublicKeyPath)" -ForegroundColor Cyan
    Write-Host "Installed tool path: $toolPath" -ForegroundColor Cyan
    Write-Host "NuGet package cache: $nuGetPackagesPath" -ForegroundColor Cyan
    Write-Host "Generated app root: $generatedRoot" -ForegroundColor Cyan
    Write-Host "Staged package root: $stagedPackagePath" -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "Signed package governance validation failed." -ForegroundColor Yellow
    Write-RecentLogs -Path $trustedStdoutLogPath -Label "Trusted host stdout"
    Write-RecentLogs -Path $trustedStderrLogPath -Label "Trusted host stderr"
    Write-RecentLogs -Path $tamperedStdoutLogPath -Label "Tampered host stdout"
    Write-RecentLogs -Path $tamperedStderrLogPath -Label "Tampered host stderr"
    throw
}
finally {
    if ($null -ne $trustedProcess -and -not $trustedProcess.HasExited) {
        Stop-Process -Id $trustedProcess.Id -Force -ErrorAction SilentlyContinue
        $trustedProcess.WaitForExit()
    }

    if ($null -ne $tamperedProcess -and -not $tamperedProcess.HasExited) {
        Stop-Process -Id $tamperedProcess.Id -Force -ErrorAction SilentlyContinue
        $tamperedProcess.WaitForExit()
    }

    $env:NUGET_PACKAGES = $previousNuGetPackages

    if ($restoreRepoPackageAssets) {
        try {
            Write-Host ""
            Write-Host "Restoring repo package assets back to the default NuGet cache..." -ForegroundColor Cyan
            foreach ($projectPath in @($packageProjectPaths + $referenceModuleProjectPath) | Sort-Object -Unique) {
                Invoke-DotNet -WorkingDirectory $repoRoot -Arguments @("restore", $projectPath)
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
