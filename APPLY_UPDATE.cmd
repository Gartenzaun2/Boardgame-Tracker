@echo off
setlocal EnableExtensions

rem Boardgame Tracker - update runner
rem Run this file from the repository root after copying the update package into it.

set "SCRIPT=%~dp0APPLY_UPDATE.ps1"
if not exist "%SCRIPT%" (
  echo ERROR: APPLY_UPDATE.ps1 was not found next to APPLY_UPDATE.cmd.
  exit /b 1
)

powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%SCRIPT%" -Root "%CD%"
set "RC=%ERRORLEVEL%"
if not "%RC%"=="0" (
  echo Update file operations failed with exit code %RC%.
  exit /b %RC%
)

echo File operations completed successfully.
exit /b 0
