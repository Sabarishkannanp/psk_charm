@echo off
title Launching PSK Charms...
cd /d "%~dp0"

:: Automatically strip Windows Download Protection / SmartScreen Mark-of-the-Web
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '%~dp0' -Recurse -File | Unblock-File -ErrorAction SilentlyContinue" >nul 2>&1

if exist "PSK.exe" (
    start "" "PSK.exe"
) else if exist "..\PSK.exe" (
    start "" "..\PSK.exe"
)
