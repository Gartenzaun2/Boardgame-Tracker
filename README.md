# Boardgame Tracker

Boardgame Tracker ist eine .NET-MAUI-App für Android und iOS zum Verwalten,
Fortsetzen und Auswerten von Brettspielpartien. Spieler, Gruppen, Historie und
Navigation sind spielübergreifend; konkrete Spiele ergänzen Vorbereitung, Wertung
und weitere Fachfunktionen als eigene Module.

Aktuell vollständig unterstütztes Spiel: **Arche Nova**.

Das Projekt ist zugleich ein Lernprojekt. Architektur und Code sollen verständlich,
wartbar und nachvollziehbar bleiben; zusätzliche Abstraktionen oder Patterns sind
kein Selbstzweck.

## Aktueller Umfang

Boardgame Tracker unterstützt unter anderem:

- wiederverwendbare Spieler und feste Gruppen;
- spielbezogene Gruppendefaults und Partievorbereitung;
- laufende Partien, Wertung und abgeschlossene Ergebnisse;
- Historie mit Korrektur sowie lokalem Import/Export;
- App- und spielbezogene Einstellungen;
- mehrere Themes und eine lokale Open-Source-Lizenzanzeige.

Arche Nova ergänzt unter anderem Zoopläne, Spielerfarben, Attraktivität,
Artenschutz, optionalen Ruf, Spezialkarten, Gleichstandsauflösung und assistierte
Fotoerkennung.

Der vollständige aktuelle Produktstand steht in
[docs/PRODUCT_SPEC.md](docs/PRODUCT_SPEC.md) und
[docs/Games/ArkNova/PRODUCT_SPEC.md](docs/Games/ArkNova/PRODUCT_SPEC.md).

## Technologie

```text
.NET 10
.NET MAUI
EF Core 10
SQLite
Android 11+
iOS 15+
```

Die Anwendung ist als modularer Monolith mit pragmatischen Vertical Slices
organisiert. Allgemeiner Code bleibt spielunabhängig; spielbezogener Code liegt
unter `Games/<GameName>/`.

Details: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Schnellstart

Vorausgesetzt werden .NET 10, die .NET-MAUI-Workload sowie die gewünschte
Android- und/oder iOS-Toolchain. Für plattformspezifische App-Builds muss zusätzlich
die benötigte native Arche-Nova-Bibliothek gemäß [SETUP.md](SETUP.md) lokal
bereitstehen.

Vom Solution-Root:

```powershell
dotnet restore BoardgameTracker.sln
dotnet build BoardgameTracker.sln --no-restore
dotnet test --project tests/BoardgameTracker.Core.Tests --no-restore
dotnet test --project tests/BoardgameTracker.Application.Tests --no-restore
```

Plattformspezifische Voraussetzungen, Entwicklungsdatenbank, Versionierung und
Lieferregeln stehen in [SETUP.md](SETUP.md).

## Dokumentation

| Thema | Quelle |
|---|---|
| Dokumentationslandkarte | [docs/README.md](docs/README.md) |
| Produktverhalten | [docs/PRODUCT_SPEC.md](docs/PRODUCT_SPEC.md) |
| Architektur | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) |
| Domain- und Persistenzmodell | [docs/DATA_MODEL.md](docs/DATA_MODEL.md) |
| Codekonventionen | [docs/CODE_GUIDELINES.md](docs/CODE_GUIDELINES.md) |
| UI-Design und Interaktion | [docs/UI_DESIGN_GUIDE.md](docs/UI_DESIGN_GUIDE.md) |
| Screens und Navigation | [docs/UI_WIREFRAME.md](docs/UI_WIREFRAME.md) |
| Import / Export | [docs/IMPORT_EXPORT.md](docs/IMPORT_EXPORT.md) |
| Drittanbieter-Lizenzen | [docs/THIRD_PARTY_LICENSE_POLICY.md](docs/THIRD_PARTY_LICENSE_POLICY.md) |
| bekannte technische Schulden | [docs/TODO.md](docs/TODO.md) |
| Arche-Nova-Dokumente | [docs/Games/ArkNova/](docs/Games/ArkNova/) |
| Entwicklungs- und Verifikationsregeln | [AGENTS.md](AGENTS.md) |
| Versionshistorie | [CHANGELOG.md](CHANGELOG.md) |

Für Arche-Nova-Fotoerkennung sind Produktstatus und bekannte Grenzen in
[docs/Games/ArkNova/CV_STATUS.md](docs/Games/ArkNova/CV_STATUS.md) dokumentiert;
technische Reproduktion und native Builds liegen beim jeweiligen Tool.

## Quellen der Wahrheit

Die Spezifikationen unter `docs/` beschreiben den aktuellen gewünschten Zustand.
Bekannte Abweichungen stehen in `docs/TODO.md`; historische Änderungen in
`CHANGELOG.md`.

`AGENTS.md` enthält Arbeits- und Routingregeln für Coding Agents und verweist auf
die jeweils zuständige kanonische Quelle.
