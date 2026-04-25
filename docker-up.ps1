#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Start all Docker containers for the microservices platform.

.DESCRIPTION
    This script starts all Docker containers using docker-compose, including:
    - 6 backend microservices (answer, api-gateway, assessment, candidate, identity, result)
    - 1 SQL Server database shared by all services
    - RabbitMQ message broker
    - Angular frontend with Nginx

.PARAMETER Detach
    Run containers in the background (default: true)

.PARAMETER NoWait
    Don't wait for services to be healthy before returning

.EXAMPLE
    ./docker-up.ps1
    # Starts all containers in the background

.EXAMPLE
    ./docker-up.ps1 -NoWait
    # Starts containers without waiting for health checks
#>

param(
    [switch]$Detach = $true,
    [switch]$NoWait
)

$ErrorActionPreference = 'Stop'

Write-Host "🚀 Starting Docker containers..." -ForegroundColor Cyan

$composeArgs = @('compose', 'up')

if ($Detach) {
    $composeArgs += '-d'
}

try {
    & docker @composeArgs
    
    if (-not $NoWait) {
        Write-Host "⏳ Waiting for services to be healthy..." -ForegroundColor Yellow
        Start-Sleep -Seconds 5
        
        $healthServices = @(
            'api-gateway',
            'identity-service',
            'answer-service',
            'assessment-service',
            'candidate-service',
            'result-service',
            'frontend'
        )
        
        foreach ($service in $healthServices) {
            Write-Host "Checking $service..." -ForegroundColor Gray
            $retries = 0
            $maxRetries = 30
            
            while ($retries -lt $maxRetries) {
                $health = & docker compose ps $service --format 'table {{.Status}}' | Select-Object -Last 1
                
                if ($health -match '(healthy|Up)') {
                    Write-Host "  ✅ $service is healthy" -ForegroundColor Green
                    break
                }
                
                $retries++
                if ($retries -eq $maxRetries) {
                    Write-Host "  ⚠️  $service took longer to become healthy" -ForegroundColor Yellow
                } else {
                    Start-Sleep -Seconds 2
                }
            }
        }
    }
    
    Write-Host "" -ForegroundColor White
    Write-Host "✅ All containers started successfully!" -ForegroundColor Green
    Write-Host "" -ForegroundColor White
    Write-Host "📋 Services available at:" -ForegroundColor Cyan
    Write-Host "   • Frontend: http://localhost:4200" -ForegroundColor White
    Write-Host "   • API Gateway: http://localhost:7080" -ForegroundColor White
    Write-Host "   • API Gateway Health: http://localhost:7080/health" -ForegroundColor White
    Write-Host "   • RabbitMQ Management: http://localhost:15672 (guest/guest)" -ForegroundColor White
    Write-Host "   • Identity Service (Health): http://localhost:7140/health" -ForegroundColor White
    Write-Host "   • Answer Service (Health): http://localhost:7141/health" -ForegroundColor White
    Write-Host "   • Assessment Service (Health): http://localhost:7142/health" -ForegroundColor White
    Write-Host "   • Candidate Service (Health): http://localhost:7143/health" -ForegroundColor White
    Write-Host "   • Result Service (Health): http://localhost:7144/health" -ForegroundColor White
    Write-Host "" -ForegroundColor White
    Write-Host "Use './docker-logs.ps1' to view logs" -ForegroundColor Cyan
    Write-Host "Use './docker-down.ps1' to stop all containers" -ForegroundColor Cyan
    
} catch {
    Write-Host "❌ Failed to start containers: $_" -ForegroundColor Red
    exit 1
}
