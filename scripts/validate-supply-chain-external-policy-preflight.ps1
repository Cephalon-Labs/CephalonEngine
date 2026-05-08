param(
    [string]$ManifestPath = "scripts/supply-chain-release-support.json",
    [string]$OutputPath = "artifacts/supply-chain-external-policy",
    [string]$Repository = "Cephalon-Labs/CephalonEngine",
    [switch]$RequireAll,
    [switch]$UseGitHubCliSecretMetadata,
    [string]$NuGetUser = $env:NUGET_USER,
    [string]$TrustedPublishingPolicyConfirmed = $env:CEPHALON_NUGET_TRUSTED_PUBLISHING_POLICY_CONFIRMED,
    [string]$PrefixReservationConfirmed = $env:CEPHALON_NUGET_PREFIX_RESERVATION_CONFIRMED
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    $candidate = Resolve-Path (Join-Path $PSScriptRoot "..")
    return $candidate.Path
}

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $Path))
}

function Convert-ToBooleanConfirmation {
    param(
        [AllowNull()]
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    return @("1", "true", "yes", "y", "confirmed") -contains $Value.Trim().ToLowerInvariant()
}

function Get-GitHubSecretMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repository,
        [Parameter(Mandatory = $true)]
        [string]$SecretName
    )

    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if ($null -eq $gh) {
        return [pscustomobject]@{
            Status  = "unavailable"
            Detail  = "gh CLI was not found."
            Present = $false
        }
    }

    try {
        $output = & gh secret list --repo $Repository 2>&1
        if ($LASTEXITCODE -ne 0) {
            return [pscustomobject]@{
                Status  = "unavailable"
                Detail  = ($output | Out-String).Trim()
                Present = $false
            }
        }

        $secretNames = @(
            $output |
                Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
                ForEach-Object {
                    $line = [string]$_
                    ($line -split "\s+")[0]
                }
        )

        return [pscustomobject]@{
            Status  = "checked"
            Detail  = "Repository secret metadata was queried with gh."
            Present = $secretNames -contains $SecretName
        }
    }
    catch {
        return [pscustomobject]@{
            Status  = "unavailable"
            Detail  = $_.Exception.Message
            Present = $false
        }
    }
}

function New-PolicyCheck {
    param(
        [Parameter(Mandatory = $true)]
        [string]$EvidenceItemId,
        [Parameter(Mandatory = $true)]
        [string]$Category,
        [Parameter(Mandatory = $true)]
        [string]$VerificationMode,
        [Parameter(Mandatory = $true)]
        [string]$RequiredInput,
        [Parameter(Mandatory = $true)]
        [bool]$Verified,
        [Parameter(Mandatory = $true)]
        [string]$Summary
    )

    $status = if ($Verified) { "verified" } else { "pending" }
    return [pscustomobject]([ordered]@{
        EvidenceItemId  = $EvidenceItemId
        Category        = $Category
        Status          = $status
        VerificationMode = $VerificationMode
        RequiredInput   = $RequiredInput
        Summary         = $Summary
    })
}

function Invoke-SupplyChainExternalPolicyPreflight {
    param(
        [string]$ManifestPath = "scripts/supply-chain-release-support.json",
        [string]$OutputPath = "artifacts/supply-chain-external-policy",
        [string]$Repository = "Cephalon-Labs/CephalonEngine",
        [switch]$RequireAll,
        [switch]$UseGitHubCliSecretMetadata,
        [string]$NuGetUser = $env:NUGET_USER,
        [string]$TrustedPublishingPolicyConfirmed = $env:CEPHALON_NUGET_TRUSTED_PUBLISHING_POLICY_CONFIRMED,
        [string]$PrefixReservationConfirmed = $env:CEPHALON_NUGET_PREFIX_RESERVATION_CONFIRMED
    )

    $repoRoot = Resolve-RepoRoot
    $resolvedManifestPath = Resolve-RepoPath -Path $ManifestPath -RepoRoot $repoRoot
    if (-not (Test-Path -LiteralPath $resolvedManifestPath -PathType Leaf)) {
        throw "Supply-chain release support manifest was not found at '$resolvedManifestPath'."
    }

    $manifest = Get-Content -LiteralPath $resolvedManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 16
    $externalPolicyItems = @($manifest.evidenceItems | Where-Object { $_.status -eq "external-policy-pending" })
    if ($externalPolicyItems.Count -eq 0) {
        throw "Supply-chain release support manifest does not declare external-policy-pending evidence items."
    }

    $nugetUserPresent = -not [string]::IsNullOrWhiteSpace($NuGetUser)
    $secretMetadata = $null
    if (-not $nugetUserPresent -and $UseGitHubCliSecretMetadata) {
        $secretMetadata = Get-GitHubSecretMetadata -Repository $Repository -SecretName "NUGET_USER"
        $nugetUserPresent = $secretMetadata.Present
    }

    $checks = @(
        New-PolicyCheck `
            -EvidenceItemId "nuget-trusted-publishing-policy" `
            -Category "publisher-identity" `
            -VerificationMode "release-manager-confirmation" `
            -RequiredInput "CEPHALON_NUGET_TRUSTED_PUBLISHING_POLICY_CONFIRMED=true" `
            -Verified (Convert-ToBooleanConfirmation -Value $TrustedPublishingPolicyConfirmed) `
            -Summary "nuget.org trusted-publishing policy must point at Cephalon-Labs/CephalonEngine, .github/workflows/publish-release.yml, and the v*.*.* tag pattern."
        New-PolicyCheck `
            -EvidenceItemId "nuget-prefix-reservation" `
            -Category "package-discoverability" `
            -VerificationMode "release-manager-confirmation" `
            -RequiredInput "CEPHALON_NUGET_PREFIX_RESERVATION_CONFIRMED=true" `
            -Verified (Convert-ToBooleanConfirmation -Value $PrefixReservationConfirmed) `
            -Summary "The Cephalon.* package prefix must be reserved/protected on nuget.org before stable GA release."
        New-PolicyCheck `
            -EvidenceItemId "nuget-user-secret" `
            -Category "publisher-identity" `
            -VerificationMode "secret-present" `
            -RequiredInput "NUGET_USER secret or NUGET_USER environment value" `
            -Verified $nugetUserPresent `
            -Summary "The release workflow needs a non-empty NUGET_USER secret for NuGet trusted publishing login."
    )

    $declaredExternalIds = @($externalPolicyItems | ForEach-Object { [string]$_.id } | Sort-Object -Unique)
    $checkIds = @($checks | ForEach-Object { $_.EvidenceItemId } | Sort-Object -Unique)
    $missingChecks = @($declaredExternalIds | Where-Object { $checkIds -notcontains $_ })
    if ($missingChecks.Count -gt 0) {
        throw "Supply-chain external policy preflight is missing checks for evidence items: $($missingChecks -join ', ')."
    }

    $verifiedCount = @($checks | Where-Object { $_.Status -eq "verified" }).Count
    $pendingCount = @($checks | Where-Object { $_.Status -ne "verified" }).Count
    $status = if ($pendingCount -eq 0) { "passed" } elseif ($RequireAll) { "failed" } else { "pending" }

    $report = [pscustomobject]([ordered]@{
        '$schemaVersion' = "1.0.0"
        Status = $status
        RequireAll = [bool]$RequireAll
        Repository = $Repository
        ManifestPath = $ManifestPath
        GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        ExternalPolicyItemCount = $externalPolicyItems.Count
        CheckCount = $checks.Count
        VerifiedCheckCount = $verifiedCount
        PendingCheckCount = $pendingCount
        Checks = $checks
        GitHubSecretMetadata = $secretMetadata
    })

    $resolvedOutputPath = Resolve-RepoPath -Path $OutputPath -RepoRoot $repoRoot
    New-Item -ItemType Directory -Path $resolvedOutputPath -Force | Out-Null
    $jsonPath = Join-Path $resolvedOutputPath "external-policy-preflight.json"
    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

    Write-Host ("Supply-chain external policy preflight: {0}; verified {1}; pending {2}; report {3}" -f `
            $status,
            $verifiedCount,
            $pendingCount,
            $jsonPath)

    if ($RequireAll -and $pendingCount -gt 0) {
        $pendingIds = @($checks | Where-Object { $_.Status -ne "verified" } | ForEach-Object { $_.EvidenceItemId })
        throw "Supply-chain external policy preflight failed; pending checks: $($pendingIds -join ', ')."
    }

    return [pscustomobject]@{
        Report = $report
        JsonPath = $jsonPath
    }
}

if (-not $env:CEPHALON_SUPPLY_CHAIN_EXTERNAL_POLICY_PREFLIGHT_NO_RUN) {
    $null = Invoke-SupplyChainExternalPolicyPreflight `
        -ManifestPath $ManifestPath `
        -OutputPath $OutputPath `
        -Repository $Repository `
        -RequireAll:$RequireAll `
        -UseGitHubCliSecretMetadata:$UseGitHubCliSecretMetadata `
        -NuGetUser $NuGetUser `
        -TrustedPublishingPolicyConfirmed $TrustedPublishingPolicyConfirmed `
        -PrefixReservationConfirmed $PrefixReservationConfirmed
}
