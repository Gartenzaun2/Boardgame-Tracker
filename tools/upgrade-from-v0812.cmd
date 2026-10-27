@echo off
setlocal
cd /d "%~dp0.."

echo Removing obsolete upgrade helpers...
if exist "tools\upgrade-from-v0811.cmd" del /q "tools\upgrade-from-v0811.cmd"

echo Upgrade cleanup from v0.8.1.2 completed.
echo.
echo IMPORTANT:
echo v0.8.1.3 changes the development database schema.
echo Because this development version uses EnsureCreated instead of migrations,
echo delete the installed app data or reinstall the app before starting it.
endlocal
