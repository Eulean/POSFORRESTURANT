@echo off
setlocal
set ROOT=%~dp0
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%RestaurantPos.Restore.ps1" %*
endlocal
