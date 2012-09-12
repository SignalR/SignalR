$assemblyPath = Join-Path (Get-Location) "\SignalR\bin\Debug\SignalR.dll"
[Reflection.Assembly]::LoadFrom($assemblyPath) | Out-Null

Write-Host "Installing counters..."

$installer = New-Object SignalR.Infrastructure.PerformanceCounterInstaller
$installer.UninstallCounters()

Write-Host "Counters uninstalled!"