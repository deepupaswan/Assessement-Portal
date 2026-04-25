$ErrorActionPreference = 'Stop'

$pidFile = Join-Path $PSScriptRoot '.service-pids.json'

if (-not (Test-Path $pidFile)) {
    Write-Host 'No PID file found. Nothing to stop.'
    exit 0
}

$entries = Get-Content $pidFile -Raw | ConvertFrom-Json

foreach ($entry in $entries) {
    $pid = [int]$entry.Pid
    try {
        $process = Get-Process -Id $pid -ErrorAction Stop
        Stop-Process -Id $pid -Force
        Write-Host "Stopped $($entry.Name) (PID: $pid)"
    }
    catch {
        Write-Warning "Process for $($entry.Name) not running (PID: $pid)"
    }
}

Remove-Item $pidFile -Force
Write-Host 'Done.'
