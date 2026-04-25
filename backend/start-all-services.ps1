param(
    [switch]$NoBuild,
    [string]$LaunchProfile = 'https'
)

$ErrorActionPreference = 'Stop'

$services = @(
    @{ Name = 'answer-service'; Project = 'answer-service/Api/AnswerService.Api.csproj' },
    @{ Name = 'api-gateway'; Project = 'api-gateway/Api/ApiGateway.csproj' },
    @{ Name = 'assessment-service'; Project = 'assessment-service/Api/AssessmentService.Api.csproj' },
    @{ Name = 'candidate-service'; Project = 'candidate-service/Api/CandidateService.Api.csproj' },
    @{ Name = 'identity-service'; Project = 'identity-service/Api/IdentityService.Api.csproj' },
    @{ Name = 'result-service'; Project = 'result-service/Api/ResultService.Api.csproj' }
)

$logDir = Join-Path $PSScriptRoot '.run-logs'
$pidFile = Join-Path $PSScriptRoot '.service-pids.json'

if (-not (Test-Path $logDir)) {
    New-Item -Path $logDir -ItemType Directory | Out-Null
}

$started = @()

foreach ($service in $services) {
    $projectPath = Join-Path $PSScriptRoot $service.Project

    if (-not (Test-Path $projectPath)) {
        Write-Warning "Skipping $($service.Name): project not found at $projectPath"
        continue
    }

    $stdoutLog = Join-Path $logDir "$($service.Name).out.log"
    $stderrLog = Join-Path $logDir "$($service.Name).err.log"

    if ($NoBuild) {
        $args = @('run', '--project', $projectPath, '--no-build')
    }
    else {
        $args = @('run', '--project', $projectPath)
    }

    if (-not [string]::IsNullOrWhiteSpace($LaunchProfile)) {
        $args += @('--launch-profile', $LaunchProfile)
    }

    $process = Start-Process -FilePath 'dotnet' `
        -ArgumentList $args `
        -RedirectStandardOutput $stdoutLog `
        -RedirectStandardError $stderrLog `
        -PassThru

    $started += [PSCustomObject]@{
        Name    = $service.Name
        Pid     = $process.Id
        Project = $projectPath
        StdOut  = $stdoutLog
        StdErr  = $stderrLog
    }

    Write-Host "Started $($service.Name) (PID: $($process.Id), Profile: $LaunchProfile)"
}

$started | ConvertTo-Json | Set-Content -Path $pidFile

Write-Host ""
Write-Host "All requested services have been launched."
Write-Host "PID file: $pidFile"
Write-Host "Logs: $logDir"
Write-Host "Use ./stop-all-services.ps1 to stop everything."