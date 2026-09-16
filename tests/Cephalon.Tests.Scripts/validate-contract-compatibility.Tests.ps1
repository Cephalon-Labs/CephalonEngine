BeforeAll {
    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $support = Get-Content (Join-Path $repoRoot 'scripts/contract-compatibility-support.json') -Raw | ConvertFrom-Json
    $tokens = $null; $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile(
        (Join-Path $repoRoot 'scripts/validate-contract-compatibility.ps1'), [ref]$tokens, [ref]$errors)
    if ($errors.Count -gt 0) { throw ($errors | Out-String) }
    foreach ($name in @('Read-CompatPackage', 'Invoke-CompatProcess')) {
        $definition = $ast.Find({ param($node)
            $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
        }, $true)
        . ([scriptblock]::Create($definition.Extent.Text))
    }
    $runRoot = $TestDrive
    $ProcessTimeoutSeconds = 30
    function New-TestPackage {
        param([string]$Name, [string]$Id = 'Cephalon.Abstractions', [string]$AssemblyEntry = 'lib/net10.0/Cephalon.Abstractions.dll', [switch]$DuplicateSpec, [switch]$Dtd)
        $path = Join-Path $TestDrive "$Name.nupkg"
        $archive = [IO.Compression.ZipFile]::Open($path, [IO.Compression.ZipArchiveMode]::Create)
        try {
            $prefix = if ($Dtd) { '<!DOCTYPE package [<!ENTITY id "Cephalon.Abstractions">]>' } else { '' }
            $xml = "$prefix<package><metadata><id>$Id</id><version>0.1.0-probe</version><repository commit=`"abc`"/></metadata></package>"
            foreach ($spec in @('package.nuspec') + $(if ($DuplicateSpec) { @('second.nuspec') } else { @() })) {
                $writer = [IO.StreamWriter]::new($archive.CreateEntry($spec).Open())
                try { $writer.Write($xml) } finally { $writer.Dispose() }
            }
            $writer = [IO.StreamWriter]::new($archive.CreateEntry($AssemblyEntry).Open())
            try { $writer.Write('test-assembly-bytes') } finally { $writer.Dispose() }
        }
        finally { $archive.Dispose() }
        return $path
    }
}

Describe 'Contract compatibility artifact validation' {
    It 'reads package identity and independently hashes package and assembly bytes' {
        $path = New-TestPackage -Name valid
        $package = Read-CompatPackage -Path $path -Role baseline
        $package.version | Should -Be '0.1.0-probe'
        $package.packageSha256 | Should -Be (Get-FileHash $path -Algorithm SHA256).Hash
        $package.assemblySha256 | Should -Be (Get-FileHash $package.assemblyPath -Algorithm SHA256).Hash
        $package.repositoryCommit | Should -Be 'abc'
    }
    It 'rejects another package before running its contents' {
        $path = New-TestPackage -Name wrong-id -Id Other.Package
        { Read-CompatPackage -Path $path -Role baseline } | Should -Throw '*package id*'
    }
    It 'requires the declared framework asset rather than accepting a different target' {
        $path = New-TestPackage -Name wrong-tfm -AssemblyEntry 'lib/net8.0/Cephalon.Abstractions.dll'
        { Read-CompatPackage -Path $path -Role baseline } | Should -Throw '*no contract assembly*'
    }
    It 'does not extract path traversal entries' {
        $path = New-TestPackage -Name traversal -AssemblyEntry '../escaped.dll'
        { Read-CompatPackage -Path $path -Role baseline } | Should -Throw '*no contract assembly*'
        Test-Path (Join-Path (Split-Path $TestDrive -Parent) 'escaped.dll') | Should -BeFalse
    }
    It 'rejects ambiguous package metadata' {
        $path = New-TestPackage -Name duplicate -DuplicateSpec
        { Read-CompatPackage -Path $path -Role baseline } | Should -Throw '*exactly one root nuspec*'
    }
    It 'rejects DTDs in package metadata' {
        $path = New-TestPackage -Name dtd -Dtd
        { Read-CompatPackage -Path $path -Role baseline } | Should -Throw '*DTD*'
    }
    It 'records and rejects unexpected subprocess exits' {
        { Invoke-CompatProcess -Name bad-exit -Command (Get-Process -Id $PID).Path -Arguments @('-NoProfile', '-Command', 'exit 7') } | Should -Throw '*exited 7, expected 0*'
        Test-Path (Join-Path $runRoot 'bad-exit.log') | Should -BeTrue
    }
    It 'requires the exact negative-control exit rather than any failure' {
        { Invoke-CompatProcess -Name wrong-boundary -Command (Get-Process -Id $PID).Path -Arguments @('-NoProfile', '-Command', 'exit 1') -ExpectedExitCode 42 } | Should -Throw '*exited 1, expected 42*'
    }
    It 'terminates a process that exceeds the configured time budget' {
        $previous = $ProcessTimeoutSeconds
        $ProcessTimeoutSeconds = 1
        try {
            { Invoke-CompatProcess -Name timeout -Command (Get-Process -Id $PID).Path -Arguments @('-NoProfile', '-Command', 'Start-Sleep -Seconds 20') } | Should -Throw '*exceeded*process limit*'
        }
        finally { $ProcessTimeoutSeconds = $previous }
    }
}
