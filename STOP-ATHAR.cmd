@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\athar-product.ps1" -Action Stop
if errorlevel 1 pause
