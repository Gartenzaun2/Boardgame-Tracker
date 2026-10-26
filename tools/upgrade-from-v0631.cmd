@echo off
setlocal
cd /d "%~dp0\.."

echo Boardgame Tracker - Upgrade v0.6.3.1 auf v0.6.4
echo.
echo Keine Projektdateien muessen geloescht oder verschoben werden.
echo.
echo WICHTIG: Das Datenbankschema wurde um Groups.IsArchived erweitert.
echo Eine EF-Core-Migration ist erforderlich.
echo Fuehre nach dem Kopieren das im SETUP dokumentierte dotnet-ef Kommando aus.
echo.
endlocal
