@echo off
setlocal
cd /d "%~dp0"
set /p ATHAR_ADMIN_PASSWORD=Enter a temporary admin password: 
powershell -NoProfile -ExecutionPolicy Bypass -File ".\START-PUBLISHED-ATHAR.ps1" -AdminPassword "%ATHAR_ADMIN_PASSWORD%"
if errorlevel 1 pause
