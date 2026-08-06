@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\foundationkit.ps1" start -Target Athar -Mode Auto
if errorlevel 1 pause
