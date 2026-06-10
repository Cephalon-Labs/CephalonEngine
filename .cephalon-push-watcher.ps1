# Cephalon git push watcher — runs in the background
# Watches D:\SaaS\CephalonEngine\.push-trigger and pushes when AI creates it.
# Start with: pwsh -WindowStyle Hidden -File D:\SaaS\CephalonEngine\.cephalon-push-watcher.ps1
# Or one-shot foreground for testing: pwsh D:\SaaS\CephalonEngine\.cephalon-push-watcher.ps1

$ErrorActionPreference = 'Continue'

$repoRoot = 'D:\SaaS\CephalonEngine'
$triggerPath = Join-Path $repoRoot '.push-trigger'
$logPath = Join-Path $repoRoot '.cephalon-push-watcher.log'

function Write-Log {
    param([string]$Message)
    $stamp = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
    "$stamp $Message" | Out-File -FilePath $logPath -Append -Encoding UTF8
}

Write-Log "watcher started; pid=$PID; repo=$repoRoot"

while ($true) {
    try {
        if (Test-Path -LiteralPath $triggerPath) {
            Write-Log "trigger detected — running git push"
            Set-Location $repoRoot

            $pushOutput = git push origin master 2>&1
            $exitCode = $LASTEXITCODE

            if ($exitCode -eq 0) {
                Write-Log "push success: $($pushOutput -join ' | ')"
                Remove-Item -LiteralPath $triggerPath -ErrorAction SilentlyContinue
            } else {
                Write-Log "push failed (exit $exitCode): $($pushOutput -join ' | ')"
                # leave trigger so next loop retries (e.g., transient network)
                Start-Sleep -Seconds 60
            }
        }
    } catch {
        Write-Log "loop error: $($_.Exception.Message)"
    }

    Start-Sleep -Seconds 15
}
