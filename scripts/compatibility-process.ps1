# Shared bounded subprocess runner. Callers supply repoRoot, runRoot and ProcessTimeoutSeconds.
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
    $start.Environment['MSBUILDDISABLENODEREUSE'] = '1'
    $start.Environment['DOTNET_CLI_USE_MSBUILD_SERVER'] = 'false'
    $start.Environment['UseSharedCompilation'] = 'false'
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    $elapsed = [Diagnostics.Stopwatch]::StartNew()
    try {
        if (-not $process.Start()) { throw "Could not start $Name." }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($ProcessTimeoutSeconds * 1000)) {
            $process.Kill($true)
            [void]$process.WaitForExit(2000)
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

