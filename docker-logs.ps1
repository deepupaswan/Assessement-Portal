#!/usr/bin/env pwsh
<#
.SYNOPSIS
    View logs from Docker containers.

.DESCRIPTION
    This script displays logs from Docker containers. By default, shows logs from all services.

.PARAMETER Service
    Show logs from a specific service (e.g., api-gateway, identity-service, etc.)

.PARAMETER Follow
    Follow log output (like tail -f)

.PARAMETER Tail
    Number of lines to show from the end (default: 100)

.EXAMPLE
    ./docker-logs.ps1
    # Show recent logs from all services

.EXAMPLE
    ./docker-logs.ps1 -Service api-gateway -Follow
    # Follow logs from api-gateway in real-time

.EXAMPLE
    ./docker-logs.ps1 -Service identity-service -Tail 50
    # Show last 50 lines from identity-service
#>

param(
    [string]$Service = '',
    [switch]$Follow,
    [int]$Tail = 100
)

$ErrorActionPreference = 'Stop'

$composeArgs = @('compose', 'logs')

if ($Follow) {
    $composeArgs += '-f'
}

$composeArgs += @('--tail', $Tail.ToString())

if (-not [string]::IsNullOrWhiteSpace($Service)) {
    $composeArgs += $Service
}

try {
    & docker @composeArgs
} catch {
    Write-Host "❌ Failed to get logs: $_" -ForegroundColor Red
    exit 1
}
