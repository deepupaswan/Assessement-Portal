#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Get status of all Docker containers.

.DESCRIPTION
    This script shows the current status of all running containers, their health status, and port mappings.

.EXAMPLE
    ./docker-status.ps1
    # Show status of all containers
#>

$ErrorActionPreference = 'Stop'

Write-Host "📊 Docker Container Status" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

try {
    & docker compose ps
    
    Write-Host ""
    Write-Host "🏥 Health Check Summary:" -ForegroundColor Cyan
    
    $services = @(
        'api-gateway',
        'identity-service',
        'answer-service',
        'assessment-service',
        'candidate-service',
        'result-service',
        'frontend',
        'rabbitmq',
        'sqlserver'
    )
    
    foreach ($service in $services) {
        $status = & docker compose ps $service --format 'table {{.State}}' 2>$null | Select-Object -Last 1
        $health = & docker compose ps $service --format 'table {{.Health}}' 2>$null | Select-Object -Last 1
        
        if ($status) {
            $icon = "✅"
            if ($status -like "*exited*") { $icon = "❌" }
            elseif ($status -like "*restarting*") { $icon = "🔄" }
            
            Write-Host "  $icon $($service.PadRight(25)) $status" -ForegroundColor White
        }
    }
    
} catch {
    Write-Host "❌ Failed to get status: $_" -ForegroundColor Red
    exit 1
}
