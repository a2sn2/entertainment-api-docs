@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\foundationkit.ps1" expose -Target Athar
if errorlevel 1 pause
