#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stop and remove all Docker containers.

.DESCRIPTION
    This script stops and removes all running Docker containers created by docker-compose.
    Use the --volumes flag to also remove persistent data volumes.

.PARAMETER RemoveVolumes
    Remove volumes (databases) as well. Use this to reset all data.

.PARAMETER Force
    Don't prompt for confirmation before removing volumes.

.EXAMPLE
    ./docker-down.ps1
    # Stops all containers

.EXAMPLE
    ./docker-down.ps1 -RemoveVolumes
    # Stops containers and removes data volumes
#>

param(
    [switch]$RemoveVolumes,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

Write-Host "🛑 Stopping Docker containers..." -ForegroundColor Cyan

if ($RemoveVolumes -and -not $Force) {
    Write-Host "⚠️  WARNING: You are about to remove all database volumes and data!" -ForegroundColor Yellow
    $response = Read-Host "Are you sure you want to delete all persistent data? (yes/no)"
    
    if ($response -ne 'yes') {
        Write-Host "Aborted." -ForegroundColor Yellow
        exit 0
    }
}

$composeArgs = @('compose', 'down')

if ($RemoveVolumes) {
    $composeArgs += '-v'
}

try {
    & docker @composeArgs
    Write-Host "✅ All containers stopped and removed!" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to stop containers: $_" -ForegroundColor Red
    exit 1
}
