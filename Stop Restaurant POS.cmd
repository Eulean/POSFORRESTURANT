@echo off
title Stop Restaurant POS
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Stop-LocalHost.ps1"
pause
