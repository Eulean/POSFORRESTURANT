@echo off
title Restaurant POS Local Host
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0LocalHostLauncher.ps1"
if errorlevel 1 pause
