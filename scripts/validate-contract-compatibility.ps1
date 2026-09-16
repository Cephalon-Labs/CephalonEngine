#requires -Version 7.0
<#
.SYNOPSIS
Runs isolated source, binary and bounded rollback probes for the declared contract scope.
.DESCRIPTION
Accepts two explicit NuGet artifacts, or packs the pinned baseline checkout and this checkout
with the current shipping SDK. Source-checkpoint packages are not published-release baselines.
Each run has isolated NuGet storage and preserves package, assembly and consumer SHA-256 hashes.
See docs/contract-compatibility.md for the covered surfaces and remaining matrix.
#>
[CmdletBinding(DefaultParameterSetName = 'Source')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Source')][string]$BaselineSourceRoot,
    [Parameter(Mandatory, ParameterSetName = 'Packages')][string]$BaselinePackagePath,
    [Parameter(Mandatory, ParameterSetName = 'Packages')][string]$CandidatePackagePath,
    [string]$OutputPath = 'artifacts/contract-compatibility',
    [string]$DotnetCommand = 'dotnet',
    [ValidateRange(1, 1800)][int]$ProcessTimeoutSeconds = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$support = Get-Content (Join-Path $PSScriptRoot 'contract-compatibility-support.json') -Raw | ConvertFrom-Json
$outputRoot = [IO.Path]::GetFullPath($OutputPath, $repoRoot)
$runRoot = Join-Path $outputRoot ('run-' + [guid]::NewGuid().ToString('N'))
$feed = Join-Path $runRoot 'feed'
New-Item -ItemType Directory -Path $feed -Force | Out-Null
$cases = [Collections.Generic.List[object]]::new()
$report = [ordered]@{
    schemaVersion = '1.0.0'; status = 'running'; generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    baselineKind = $support.baselineKind; targetFramework = $support.targetFramework
    os = [Runtime.InteropServices.RuntimeInformation]::OSDescription
    architecture = [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    scope = $support.scope; exclusions = $support.exclusions; cases = $cases
    runDirectory = $runRoot
}

function Invoke-CompatProcess {
    param([string]$Name, [string]$Command, [string[]]$Arguments, [int]$ExpectedExitCode = 0)
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $Command
    $start.WorkingDirectory = $repoRoot
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    $elapsed = [Diagnostics.Stopwatch]::StartNew()
    try {
        if (-not $process.Start()) { throw "Could not start $Name." }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($ProcessTimeoutSeconds * 1000)) {
            $process.Kill($true)
            $process.WaitForExit()
            throw "$Name exceeded the $ProcessTimeoutSeconds second process limit."
        }
        $remaining = [Math]::Max(1, $ProcessTimeoutSeconds * 1000 - [int]$elapsed.ElapsedMilliseconds)
        if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), $remaining)) {
            throw "$Name exceeded the process limit while draining output."
        }
        $text = $stdout.GetAwaiter().GetResult() + [Environment]::NewLine + $stderr.GetAwaiter().GetResult()
        [IO.File]::WriteAllText((Join-Path $runRoot "$Name.log"), $text)
        if ($process.ExitCode -ne $ExpectedExitCode) {
            throw "$Name exited $($process.ExitCode), expected $ExpectedExitCode. See $runRoot/$Name.log."
        }
        return $text.Trim()
    }
    finally { $elapsed.Stop(); $process.Dispose() }
}

function Read-CompatPackage {
    param([string]$Path, [string]$Role)
    $fullPath = (Resolve-Path -LiteralPath $Path).Path
    $archive = [IO.Compression.ZipFile]::OpenRead($fullPath)
    try {
        $specs = @($archive.Entries | Where-Object { $_.FullName -notmatch '/' -and $_.Name.EndsWith('.nuspec') })
        if ($specs.Count -ne 1) { throw "$Role must contain exactly one root nuspec." }
        $stream = $specs[0].Open()
        $settings = [Xml.XmlReaderSettings]::new()
        $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
        $reader = [Xml.XmlReader]::Create($stream, $settings)
        try {
            $xml = [Xml.XmlDocument]::new()
            $xml.XmlResolver = $null
            $xml.Load($reader)
        }
        finally { $reader.Dispose(); $stream.Dispose() }
        $id = $xml.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]/*[local-name()="id"]').InnerText
        $version = $xml.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]/*[local-name()="version"]').InnerText
        if ($id -cne $support.packageId) { throw "$Role package id must be $($support.packageId)." }
        if ($version -notmatch '^[0-9A-Za-z.+-]+$') { throw "$Role has an invalid package version." }
        $entry = $archive.GetEntry("lib/$($support.targetFramework)/$id.dll")
        if ($null -eq $entry) { throw "$Role has no contract assembly for $($support.targetFramework)." }
        $assemblyPath = Join-Path $runRoot "$Role.dll"
        $source = $entry.Open()
        $destination = [IO.File]::Create($assemblyPath)
        try { $source.CopyTo($destination) }
        finally { $destination.Dispose(); $source.Dispose() }
        $repository = $xml.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]/*[local-name()="repository"]')
        return [ordered]@{
            id = $id; version = $version; packagePath = $fullPath
            packageSha256 = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash
            assemblyPath = $assemblyPath; assemblySha256 = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash
            repositoryCommit = if ($null -ne $repository) { $repository.GetAttribute('commit') } else { '' }
        }
    }
    finally { $archive.Dispose() }
}

function Build-CompatConsumer {
    param([string]$Name, [string]$Version, [switch]$NewContract)
    $directory = Join-Path $runRoot $Name
    New-Item -ItemType Directory -Path $directory | Out-Null
    # Stop ambient repository/MSBuild policy from turning this into an in-tree consumer.
    foreach ($file in @('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props')) {
        [IO.File]::WriteAllText((Join-Path $directory $file), '<Project />')
    }
    Copy-Item -LiteralPath (Join-Path $repoRoot 'global.json') -Destination (Join-Path $directory 'global.json')
    $fixture = Join-Path $repoRoot $support.fixtureDirectory
    Copy-Item -LiteralPath (Join-Path $fixture 'Consumer.csproj.template') -Destination (Join-Path $directory 'Consumer.csproj')
    Copy-Item -LiteralPath (Join-Path $fixture 'Program.cs.template') -Destination (Join-Path $directory 'Program.cs')
    $escapedFeed = [Security.SecurityElement]::Escape($feed)
    [IO.File]::WriteAllText((Join-Path $directory 'NuGet.Config'), "<configuration><packageSources><clear/><add key=`"compatibility`" value=`"$escapedFeed`"/><add key=`"nuget.org`" value=`"https://api.nuget.org/v3/index.json`"/></packageSources><packageSourceMapping><clear/><packageSource key=`"compatibility`"><package pattern=`"Cephalon.Abstractions`"/></packageSource><packageSource key=`"nuget.org`"><package pattern=`"*`"/></packageSource></packageSourceMapping></configuration>")
    $buildArguments = @('build', (Join-Path $directory 'Consumer.csproj'), '-c', 'Release',
        "-p:ContractPackageVersion=$Version", "-p:RestorePackagesPath=$(Join-Path $runRoot 'packages')",
        "-p:RestoreConfigFile=$(Join-Path $directory 'NuGet.Config')", '--nologo')
    if ($NewContract) { $buildArguments += '-p:ProbeNewContract=true' }
    $null = Invoke-CompatProcess -Name "build-$Name" -Command $DotnetCommand -Arguments $buildArguments
    return Join-Path $directory "bin/Release/$($support.targetFramework)"
}

function Invoke-CompatConsumer {
    param([string]$Name, [string]$Directory, $Package, [string]$InputWire = '', [switch]$ExpectedBoundary)
    $consumer = Join-Path $Directory 'ContractConsumer.dll'
    $before = (Get-FileHash -LiteralPath $consumer -Algorithm SHA256).Hash
    Copy-Item -LiteralPath $Package.assemblyPath -Destination (Join-Path $Directory 'Cephalon.Abstractions.dll') -Force
    $receipt = Join-Path $runRoot "$Name.json"
    $wire = Join-Path $runRoot "$Name-wire.json"
    $expectedExit = if ($ExpectedBoundary) { 42 } else { 0 }
    $output = Invoke-CompatProcess -Name $Name -Command $DotnetCommand -ExpectedExitCode $expectedExit `
        -Arguments @($consumer, $receipt, $Package.assemblySha256, $InputWire, $wire)
    $after = (Get-FileHash -LiteralPath $consumer -Algorithm SHA256).Hash
    if ($before -cne $after) { throw "$Name modified the compiled consumer." }
    if ($ExpectedBoundary) {
        $boundaryPattern = '(?m)^COMPATIBILITY_BOUNDARY:TypeLoadException:' + [regex]::Escape($support.newApiBoundary) + '\s*$'
        if ($output -notmatch $boundaryPattern) { throw "$Name failed for an unexpected reason." }
        $evidence = @{ status = 'expected-incompatibility'; consumerSha256 = $before }
    }
    else {
        $evidence = Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
        if ($evidence.status -ne 'passed' -or $evidence.loadedAssemblySha256 -cne $Package.assemblySha256 -or $evidence.consumerSha256 -cne $before) {
            throw "$Name did not prove the expected loaded assembly and unmodified consumer."
        }
    }
    $cases.Add([ordered]@{ name = $Name; status = $evidence.status; contractSha256 = $Package.assemblySha256; consumerSha256 = $before; evidence = $evidence })
    return $wire
}

try {
    $report.sdk = Invoke-CompatProcess -Name 'sdk' -Command $DotnetCommand -Arguments @('--version')
    $report.candidateSourceRevision = Invoke-CompatProcess -Name 'candidate-revision' -Command 'git' -Arguments @('rev-parse', 'HEAD')
    $report.candidateWorkingTreeDirty = -not [string]::IsNullOrWhiteSpace((Invoke-CompatProcess -Name 'candidate-status' -Command 'git' -Arguments @('status', '--porcelain')))
    if ($PSCmdlet.ParameterSetName -eq 'Source') {
        $baselineRoot = (Resolve-Path -LiteralPath $BaselineSourceRoot).Path
        $revision = Invoke-CompatProcess -Name 'baseline-revision' -Command 'git' -Arguments @('-C', $baselineRoot, 'rev-parse', 'HEAD')
        if ($revision -cne $support.baselineRevision) { throw 'Baseline checkout is not the declared immutable source checkpoint.' }
        $dirty = Invoke-CompatProcess -Name 'baseline-status' -Command 'git' -Arguments @('-C', $baselineRoot, 'status', '--porcelain')
        if (-not [string]::IsNullOrWhiteSpace($dirty)) { throw 'Baseline checkout must be clean before packing.' }
        $report.baselineSourceRevision = $revision
        $report.baselineBuildSdk = Invoke-CompatProcess -Name 'baseline-build-sdk' -Command $DotnetCommand -Arguments @(
            'msbuild', (Join-Path $baselineRoot $support.project), '-getProperty:NETCoreSdkVersion', '-nologo')
        $report.candidateBuildSdk = Invoke-CompatProcess -Name 'candidate-build-sdk' -Command $DotnetCommand -Arguments @(
            'msbuild', (Join-Path $repoRoot $support.project), '-getProperty:NETCoreSdkVersion', '-nologo')
        if ($report.baselineBuildSdk -ne $report.sdk -or $report.candidateBuildSdk -ne $report.sdk) {
            throw 'The project SDK resolver selected a different toolchain from the candidate CLI SDK.'
        }
        # Both source checkpoints are built by the selected candidate SDK. SDK-dependent
        # lock regeneration in the disposable baseline is not historical release evidence.
        $null = Invoke-CompatProcess -Name 'pack-baseline' -Command $DotnetCommand -Arguments @('pack', (Join-Path $baselineRoot $support.project), '-c', 'Release', '--output', $feed, "-p:PackageVersion=$($support.baselinePackageVersion)")
        $null = Invoke-CompatProcess -Name 'pack-candidate' -Command $DotnetCommand -Arguments @('pack', (Join-Path $repoRoot $support.project), '-c', 'Release', '--output', $feed, "-p:PackageVersion=$($support.candidatePackageVersion)")
        $BaselinePackagePath = Join-Path $feed "$($support.packageId).$($support.baselinePackageVersion).nupkg"
        $CandidatePackagePath = Join-Path $feed "$($support.packageId).$($support.candidatePackageVersion).nupkg"
    }
    else { $report.baselineKind = 'caller-supplied-artifacts-not-release-certification' }

    $baseline = Read-CompatPackage -Path $BaselinePackagePath -Role 'baseline'
    $candidate = Read-CompatPackage -Path $CandidatePackagePath -Role 'candidate'
    $report.baseline = $baseline; $report.candidate = $candidate
    if ($baseline.version -eq $candidate.version) { throw 'Use distinct package versions to prevent a same-version NuGet cache comparison.' }
    if ($baseline.assemblySha256 -eq $candidate.assemblySha256) { throw 'Baseline and candidate assemblies are identical; no cross-version boundary is being exercised.' }
    foreach ($package in @($baseline, $candidate)) {
        $target = Join-Path $feed "$($package.id).$($package.version).nupkg"
        if ($package.packagePath -ne $target) { Copy-Item -LiteralPath $package.packagePath -Destination $target }
    }
    $oldClient = Build-CompatConsumer -Name 'baseline-client' -Version $baseline.version
    $oldWire = Invoke-CompatConsumer -Name 'baseline-control' -Directory $oldClient -Package $baseline
    $newWire = Invoke-CompatConsumer -Name 'old-binary-new-contract' -Directory $oldClient -Package $candidate -InputWire $oldWire
    $null = Invoke-CompatConsumer -Name 'old-reader-new-blueprint-json' -Directory $oldClient -Package $baseline -InputWire $newWire
    $newClient = Build-CompatConsumer -Name 'candidate-common-client' -Version $candidate.version
    $null = Invoke-CompatConsumer -Name 'recompiled-common-source' -Directory $newClient -Package $candidate -InputWire $oldWire
    $null = Invoke-CompatConsumer -Name 'common-surface-rollback' -Directory $newClient -Package $baseline -InputWire $newWire
    $newApiClient = Build-CompatConsumer -Name 'candidate-new-api-client' -Version $candidate.version -NewContract
    $null = Invoke-CompatConsumer -Name 'new-api-control' -Directory $newApiClient -Package $candidate
    $null = Invoke-CompatConsumer -Name 'new-api-rollback-rejected' -Directory $newApiClient -Package $baseline -ExpectedBoundary
    $report.status = 'passed'
}
catch {
    $report.status = 'failed'
    $report.error = $_.Exception.Message
    throw
}
finally {
    $report.completedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $runRoot 'contract-compatibility.json') -Encoding utf8
    $lines = @('# Contract compatibility receipt', '', "Status: $($report.status)", '', "Scope: $($support.scope -join ', ')", '', '| Scenario | Result |', '| --- | --- |')
    foreach ($case in $cases) { $lines += "| $($case.name) | $($case.status) |" }
    $lines += @('', "Excluded: $($support.exclusions -join ', ').", '', 'Package, loaded-assembly and unchanged-consumer hashes are recorded in the adjacent JSON receipt.')
    $lines | Set-Content -LiteralPath (Join-Path $runRoot 'README.md') -Encoding utf8
    Write-Host "Contract compatibility: $($report.status); receipt: $runRoot/contract-compatibility.json"
}
