#requires -Version 7.4
<#
.SYNOPSIS
Compiles a typed reader against the immutable source baseline and reads generated-host receipts.
.DESCRIPTION
This proves selected runtime snapshot/manifest v2 fields using historical Engine DTOs and an
unchanged consumer binary. It is source-checkpoint evidence, not a published-release guarantee.
The input must be a passed generated-app receipt from this candidate revision with matching hashes.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$BaselineSourceRoot,
    [Parameter(Mandatory)][string]$GeneratedAppReportPath,
    [string]$OutputPath = 'artifacts/host-compatibility/snapshot-reader',
    [string]$DotnetCommand = 'dotnet',
    [ValidateRange(1, 1800)][int]$ProcessTimeoutSeconds = 300
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$support = Get-Content (Join-Path $PSScriptRoot 'contract-compatibility-support.json') -Raw | ConvertFrom-Json
$runRoot = Join-Path ([IO.Path]::GetFullPath($OutputPath, $repoRoot)) ('run-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
. (Join-Path $PSScriptRoot 'compatibility-process.ps1')
$cases = [Collections.Generic.List[object]]::new()
$report = [ordered]@{
    schemaVersion = '1.0.0'; status = 'running'; startedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    baselineKind = $support.baselineKind; baselineSourceRevision = $support.baselineRevision
    targetFramework = 'net10.0'; runtimeIdentifier = [Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
    cases = $cases
}
try {
    $baselineRoot = (Resolve-Path -LiteralPath $BaselineSourceRoot).Path
    $baselineRevision = Invoke-CompatProcess -Name baseline-revision -Command git -Arguments @('-C', $baselineRoot, 'rev-parse', 'HEAD')
    $baselineDirty = Invoke-CompatProcess -Name baseline-status -Command git -Arguments @('-C', $baselineRoot, 'status', '--porcelain')
    if ($baselineRevision -cne $support.baselineRevision -or -not [string]::IsNullOrWhiteSpace($baselineDirty)) {
        throw 'The historical reader requires a clean checkout of the declared immutable baseline.'
    }
    $report.candidateSourceRevision = Invoke-CompatProcess -Name candidate-revision -Command git -Arguments @('rev-parse', 'HEAD')
    $report.candidateWorkingTreeDirty = -not [string]::IsNullOrWhiteSpace((Invoke-CompatProcess -Name candidate-status -Command git -Arguments @('status', '--porcelain')))
    $report.sdk = Invoke-CompatProcess -Name sdk -Command $DotnetCommand -Arguments @('--version')
    $generatedPath = (Resolve-Path -LiteralPath $GeneratedAppReportPath).Path
    $generated = Get-Content -LiteralPath $generatedPath -Raw | ConvertFrom-Json -Depth 100
    if ($generated.Status -cne 'passed' -or $generated.Toolchain.SourceRevision -cne $report.candidateSourceRevision -or
        $generated.Toolchain.GeneratedHostSdk -cne $report.sdk) { throw 'Generated host receipt must pass on this candidate revision and SDK.' }
    $report.generatedHostReceiptSha256 = (Get-FileHash -LiteralPath $generatedPath -Algorithm SHA256).Hash
    $report.generatedHostWorkingTreeDirty = $generated.Toolchain.WorkingTreeDirty
    $evidenceRoot = Join-Path (Split-Path -Parent $generatedPath) 'generated-app-runtime-contract'
    foreach ($name in @('manifest.json', 'snapshot.json', 'app-model.json')) {
        $expected = @($generated.RuntimeContract.Artifacts | Where-Object Name -CEQ $name)
        $path = Join-Path $evidenceRoot $name
        if ($expected.Count -ne 1 -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne $expected[0].Sha256) {
            throw "Missing or modified generated evidence: $name."
        }
        Copy-Item -LiteralPath $path -Destination (Join-Path $runRoot $name)
    }
    $engineProject = Join-Path $baselineRoot 'src/Cephalon.Engine/Cephalon.Engine.csproj'
    $report.baselineBuildSdk = Invoke-CompatProcess -Name baseline-sdk -Command $DotnetCommand -Arguments @('msbuild', $engineProject, '-getProperty:NETCoreSdkVersion', '-nologo')
    if ($report.baselineBuildSdk -cne $report.sdk) { throw 'Historical Engine build selected a different SDK.' }
    $consumer = Join-Path $runRoot 'consumer'
    New-Item -ItemType Directory -Path $consumer | Out-Null
    foreach ($file in @('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props')) {
        '<Project />' | Set-Content -LiteralPath (Join-Path $consumer $file) -Encoding utf8
    }
    Copy-Item -LiteralPath (Join-Path $repoRoot 'global.json') -Destination (Join-Path $consumer 'global.json')
    $fixture = Join-Path $repoRoot 'tests/compatibility/SnapshotReader'
    $projectText = (Get-Content -LiteralPath (Join-Path $fixture 'Reader.csproj.template') -Raw).Replace(
        '__BASELINE_ENGINE_PROJECT__', [Security.SecurityElement]::Escape($engineProject))
    $projectText | Set-Content -LiteralPath (Join-Path $consumer 'Reader.csproj') -Encoding utf8
    Copy-Item -LiteralPath (Join-Path $fixture 'Program.cs.template') -Destination (Join-Path $consumer 'Program.cs')
    $null = Invoke-CompatProcess -Name build-reader -Command $DotnetCommand -Arguments @('build', (Join-Path $consumer 'Reader.csproj'), '-c', 'Release', '--disable-build-servers')
    $output = Join-Path $consumer 'bin/Release/net10.0'
    $readerAssembly = Join-Path $output 'Reader.dll'
    $report.consumerSha256 = (Get-FileHash -LiteralPath $readerAssembly -Algorithm SHA256).Hash
    $report.baselineEngineSha256 = (Get-FileHash -LiteralPath (Join-Path $baselineRoot 'src/Cephalon.Engine/bin/Release/net10.0/Cephalon.Engine.dll') -Algorithm SHA256).Hash
    $manifestPath = Join-Path $runRoot 'manifest.json'
    $snapshotPath = Join-Path $runRoot 'snapshot.json'
    $additive = Get-Content -LiteralPath $snapshotPath -Raw | ConvertFrom-Json -Depth 100
    $additive | Add-Member -NotePropertyName futureSnapshotSurface -NotePropertyValue @{ version = 1 }
    $additive.manifest | Add-Member -NotePropertyName futureManifestField -NotePropertyValue 'additive'
    $additivePath = Join-Path $runRoot 'snapshot-additive.json'
    $additive | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $additivePath -Encoding utf8
    foreach ($scenario in @(@{ Name = 'historical-reader-current-snapshot'; Snapshot = $snapshotPath },
        @{ Name = 'historical-reader-additive-fields'; Snapshot = $additivePath })) {
        $receipt = Join-Path $runRoot ($scenario.Name + '.json')
        $null = Invoke-CompatProcess -Name $scenario.Name -Command $DotnetCommand -Arguments @($readerAssembly, $manifestPath, $scenario.Snapshot, $receipt)
        $result = Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
        if ($result.status -cne 'passed' -or $result.consumerSha256 -cne $report.consumerSha256 -or
            $result.engineAssemblySha256 -cne $report.baselineEngineSha256 -or
            $result.moduleCount -ne $generated.RuntimeContract.ModuleCount -or
            $result.capabilityCount -ne $generated.RuntimeContract.CapabilityCount) { throw 'Historical reader did not preserve the expected binary, assembly or snapshot contents.' }
        $cases.Add([ordered]@{ name = $scenario.Name; status = 'passed'; evidence = $result })
    }
    $invalid = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -Depth 100
    $invalid.manifestVersion = 'invalid-version'
    $invalidPath = Join-Path $runRoot 'manifest-invalid.json'
    $invalid | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $invalidPath -Encoding utf8
    $boundary = Invoke-CompatProcess -Name reject-invalid-version -Command $DotnetCommand -ExpectedExitCode 42 -Arguments @($readerAssembly, $invalidPath, $snapshotPath, (Join-Path $runRoot 'unexpected-receipt.json'))
    if ($boundary.Trim() -cne 'SNAPSHOT_BOUNDARY:invalid-manifest-version') { throw 'An unrelated failure cannot pass the snapshot boundary control.' }
    if ((Get-FileHash -LiteralPath $readerAssembly -Algorithm SHA256).Hash -cne $report.consumerSha256) { throw 'Reader binary changed between scenarios.' }
    $cases.Add([ordered]@{ name = 'historical-reader-rejects-invalid-version'; status = 'expected-incompatibility' })
    $report.status = 'passed'
}
catch {
    $report.status = 'failed'; $report.error = $_.Exception.Message
    throw
}
finally {
    $report.completedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    $report | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $runRoot 'snapshot-reader-compatibility.json') -Encoding utf8
    Write-Host "Historical snapshot reader evidence: $runRoot"
}
