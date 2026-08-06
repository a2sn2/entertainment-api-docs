@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\START-PUBLISHED-ATHAR.ps1"
if errorlevel 1 pause
