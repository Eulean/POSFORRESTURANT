@echo off
title Restore Restaurant POS
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0Restore-LocalHost.ps1"
pause
