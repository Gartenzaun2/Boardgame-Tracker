@echo off
setlocal
cd /d "%~dp0\.."

echo Boardgame Tracker - Upgrade v0.6.4 auf v0.7.0
echo.
echo Keine Projektdateien muessen geloescht oder verschoben werden.
echo Keine neue Datenbankschema-Aenderung in v0.7.0.
echo.
echo Hinweis: Falls die v0.6.4-Migration AddGroupArchiving lokal noch nicht erzeugt wurde,
echo muss sie weiterhin einmal mit tools\create-v064-migration.cmd erzeugt werden.
echo.
echo Upgrade-Bereinigung abgeschlossen.
endlocal
