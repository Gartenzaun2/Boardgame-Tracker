# Boardgame Tracker – Setup und Entwicklungsregeln

## Voraussetzungen

- .NET 10
- .NET MAUI
- EF Core 10
- SQLite
- Visual Studio mit .NET-MAUI-Workload
- Android und/oder iOS Toolchain

## Repository-Struktur

```text
BoardgameTracker.sln

src/
  BoardgameTracker.App/
  BoardgameTracker.Application/
  BoardgameTracker.Core/
  BoardgameTracker.Infrastructure/

tests/
  BoardgameTracker.Core.Tests/
  BoardgameTracker.Application.Tests/

docs/
  ARCHITECTURE.md
  DATA_MODEL.md
  PRODUCT_SPEC.md
  UI_DESIGN_GUIDE.md
  UI_WIREFRAME.md

README.md
SETUP.md
CHANGELOG.md
```

`README.md`, `SETUP.md` und `CHANGELOG.md` liegen nur im Repository-Root. Die
übrigen kanonischen Dokumente liegen nur unter `docs/`.

## Verbindliche Architekturregeln

- Zentrale Domänennamen: `Game`, `GameSession`, `Group`, `Player`.
- Keine technischen Synonyme wie `Person`, `GroupPerson`, `SessionId` oder
  `ArkNovaSessionPlayer`.
- Spielspezifischer Code liegt in jedem Projekt unter `Games/<GameName>/`.
- Ein Spiel darf darunter allgemeine fachliche Bereiche spiegeln, wenn es dort
  tatsächlich eigene Erweiterungen besitzt.
- Application-/UI-Code wird nach Use Case/Feature organisiert, nicht primär nach
  `Commands`, `Services`, `Validators` usw.
- Spielspezifische persistierte Zusatzdaten ergänzen allgemeine Entities über
  Composition; EF-Entity-Vererbung wird nicht nur zur Property-Wiederverwendung
  eingesetzt.
- Keine generischen Repositories, kein MediatR nur als Dispatch-Schicht und keine
  `Common`-/`Utils`-Sammelordner ohne konkreten Bedarf.

Details stehen in `docs/ARCHITECTURE.md`.

## Standardablauf nach einem Update

1. Update-ZIP in den Repository-Root entpacken/übernehmen.
2. Wenn `UPDATE_FILES.txt` oder `DELETE_FILES.txt` enthalten ist,
   `APPLY_UPDATE.cmd` im Repository-Root starten.
3. `bin` und `obj` vollständig löschen.
4. NuGet Restore.
5. Rebuild Solution.
6. Tests ausführen.
7. Relevante Smoke Tests im Emulator/Gerät durchführen.

### Vollständiges Löschen von bin/obj unter Windows CMD

Direkt in einer geöffneten CMD:

```cmd
for /d /r . %d in (bin,obj) do @if exist "%d" rd /s /q "%d"
```

In einer `.cmd`-Datei muss `%d` als `%%d` geschrieben werden.

## Datenbankstrategie in der Pre-Alpha-Phase

Der Runtime-Start verwendet:

```text
Database.EnsureCreatedAsync()
```

Anschließend werden notwendige Referenzdaten idempotent angelegt.

`EnsureCreated` migriert ein älteres Schema nicht. Für die Umstellung der
Arche-Nova-Spezialkarten existiert ein gezielter automatischer Upgradepfad:
`DatabaseInitializer` prüft vor dem Seeding die alte Spalte `SpecialCard` und
überführt die Werte 1–5 in stabile Karten-GUIDs. Kartentabelle, Datenübernahme,
Assignment-Tabellentausch und FK-Prüfung erfolgen in einer SQLite-Transaktion.
Bei unbekannten Altdaten oder FK-Fehlern bricht der Start mit Fehler ab; die
Transaktion erhält den bisherigen Stand. Erneuter Start ist idempotent.

Für dieses Update **keine App-Daten löschen oder deinstallieren**: Bestehende
Partien und Kartenzuordnungen werden erhalten. Geräte-Smoke-Test: eine alte
Installation mit zugeordneten Karten aktualisieren, Historie öffnen und alle
Karten prüfen; danach Standardkarte bearbeiten/archivieren und erneut starten.

Andere, nicht ausdrücklich unterstützte Schemaänderungen erfordern in der
Pre-Alpha weiterhin Neuaufbau der Entwicklungsdatenbank. Ein allgemeiner
kanonischer EF-Migrationsverlauf mit `ModelSnapshot` wird dadurch nicht
vorgezogen; dieser Slice besitzt einen begrenzten, getesteten SQLite-Upgradepfad.

## Versionsregel

In `src/BoardgameTracker.App/BoardgameTracker.App.csproj`:

```xml
<ApplicationDisplayVersion>x.y.z</ApplicationDisplayVersion>
<ApplicationVersion>n</ApplicationVersion>
```

- `ApplicationDisplayVersion` entspricht exakt der ausgelieferten Version.
- `ApplicationVersion` steigt bei jedem installierbaren Entwicklungsupdate.
- Vor dem Verpacken werden beide Werte geprüft.
- Versionshistorie steht ausschließlich in `CHANGELOG.md`.

## Lieferregel für Entwicklungs-Updates

Standardmäßig wird ein **Delta-Paket** geliefert, kein vollständiges Repository.

Ein Delta enthält nur:

- geänderte Dateien,
- neue Dateien,
- `UPDATE_FILES.txt` bzw. kompatibel `DELETE_FILES.txt`, wenn Dateien gelöscht,
  verschoben oder umbenannt werden,
- bei jeder Operationsdatei immer `APPLY_UPDATE.cmd` und `APPLY_UPDATE.ps1`.

Die Skripte bleiben generisch. Update-spezifische Operationen stehen nur in der
TXT-Datei.

Unterstützte Operationen:

```text
DELETE|pfad/zur/datei
MOVE|alter/pfad|neuer/pfad
RENAME|alter/pfad|neuer/pfad
```

Einfache Pfadzeilen in `DELETE_FILES.txt` werden aus Kompatibilitätsgründen als
`DELETE` interpretiert.

Die Skripte müssen:

- alle Pfade relativ zum Repository-Root behandeln,
- Pfade außerhalb des Repositorys ablehnen,
- fehlende Dateien bei `DELETE` tolerieren,
- Zielordner für MOVE/RENAME anlegen,
- bei echten Fehlern mit Fehlercode abbrechen.

Nicht ins Update gehören `bin/`, `obj/` oder andere generierte Build-Artefakte.

## Dokumentationsregel

Die kanonischen Spezifikationen beschreiben immer den aktuellen Zustand.
Überholte Entscheidungen werden ersetzt statt als Versionsanhang stehen zu
bleiben. Versionshistorie gehört ausschließlich in `CHANGELOG.md`.

## Abschlussprüfung

In der Erstellungsumgebung steht nicht zwingend ein lokales .NET-/MAUI-SDK zur
Verfügung. Wenn dort kein Build möglich ist, ist vor Installation lokal zwingend:

```text
Restore -> Rebuild Solution -> Tests
```
