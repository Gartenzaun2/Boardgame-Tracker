@echo off
setlocal
cd /d "%~dp0\.."

echo Boardgame Tracker - Upgrade v0.6 auf v0.6.1
echo.

echo Entferne Dateien, die in v0.6.1 nicht mehr verwendet werden ...
if exist "SETUP.txt" (
    del /q "SETUP.txt"
    echo   geloescht: SETUP.txt
) else (
    echo   bereits entfernt: SETUP.txt
)

echo.
echo Upgrade-Bereinigung abgeschlossen.
echo SETUP.txt wurde durch SETUP.md ersetzt.
endlocal
