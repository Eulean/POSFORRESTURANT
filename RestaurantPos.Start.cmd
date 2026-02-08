@echo off
setlocal
set ROOT=%~dp0
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%RestaurantPos.Start.ps1" -Mode release
endlocal
