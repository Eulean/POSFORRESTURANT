@echo off
setlocal
set ROOT=%~dp0
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%RestaurantPos.HealthMonitor.ps1" %*
endlocal
