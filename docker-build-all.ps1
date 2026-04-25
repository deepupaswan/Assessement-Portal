#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build all Docker images for the microservices and frontend.

.DESCRIPTION
    This script builds Docker images for all backend services and the Angular frontend.
    It uses the docker-compose.yml file to build all services.

.EXAMPLE
    ./docker-build-all.ps1
    # Builds all images in parallel

.EXAMPLE
    ./docker-build-all.ps1 -Services answer-service,api-gateway
    # Builds only the specified services
#>

param(
    [string[]]$Services = @(),
    [switch]$NoBuildCache
)

$ErrorActionPreference = 'Stop'

Write-Host "🐳 Building Docker images..." -ForegroundColor Cyan

$composeArgs = @('compose', 'build')

if ($NoBuildCache) {
    $composeArgs += '--no-cache'
}

if ($Services.Count -gt 0) {
    $composeArgs += $Services
}

try {
    & docker @composeArgs
    Write-Host "✅ Docker images built successfully!" -ForegroundColor Green
} catch {
    Write-Host "❌ Build failed: $_" -ForegroundColor Red
    exit 1
}
