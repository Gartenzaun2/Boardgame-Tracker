@echo off
setlocal

cd /d "%~dp0\.."

dotnet tool restore
if errorlevel 1 exit /b %errorlevel%

dotnet ef migrations add InitialCreate ^
  --project src\BoardgameTracker.Infrastructure\BoardgameTracker.Infrastructure.csproj ^
  --context BoardgameTrackerDbContext ^
  --output-dir Persistence\SQLite\Migrations

exit /b %errorlevel%
