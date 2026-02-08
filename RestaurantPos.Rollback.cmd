@echo off
setlocal
set ROOT=%~dp0
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%RestaurantPos.Rollback.ps1" %*
endlocal
