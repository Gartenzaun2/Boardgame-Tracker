$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

dotnet tool restore

dotnet ef migrations add InitialCreate `
  --project src/BoardgameTracker.Infrastructure/BoardgameTracker.Infrastructure.csproj `
  --context BoardgameTrackerDbContext `
  --output-dir Persistence/SQLite/Migrations
