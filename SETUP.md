# Boardgame Tracker – Setup und Entwicklungsregeln

## Voraussetzungen

- .NET 10
- .NET MAUI
- EF Core 10
- SQLite
- Visual Studio mit .NET-MAUI-Workload
- Android und/oder iOS Toolchain

## Quellen und Architektur

Projektüberblick und Einstieg: `README.md`; Zuständigkeiten und Arbeitsregeln:
`AGENTS.md`; Projektgrenzen und Slice-Organisation: `docs/ARCHITECTURE.md`.
Hier stehen ausschließlich Umgebungs-, Build-, Datenbank- und Lieferregeln.

## Lokaler Entwicklungsablauf

Ab dem Solution-Root:

```powershell
dotnet restore BoardgameTracker.sln
dotnet build BoardgameTracker.sln --no-restore
dotnet test --project tests/BoardgameTracker.Core.Tests --no-restore
dotnet test --project tests/BoardgameTracker.Application.Tests --no-restore
```

Normale Codearbeit kann inkrementell gebaut werden. Für gelieferte Updatearchive
gilt der vollständige Übernahmeablauf im folgenden Abschnitt.

Die Tests verwenden xUnit v3/Microsoft Testing Platform; deshalb `--project`.
Der unterstützte Mindeststand ist Android 11 (API 30) bzw. iOS 15 gemäß App-Projekt.
Für iOS-Gerätetests ist die passende Apple-Toolchain erforderlich.

Bei gesperrter `BoardgameTracker.App.dll` zuerst die laufende App/Debug-Sitzung
beenden und den Build wiederholen. Visual Studio kann zunächst geöffnet bleiben;
bei fortbestehender Sperre die Solution schließen. Keinen DB-Reset als Buildfix
verwenden.

## Übernahme eines Entwicklungs-Updatepakets

1. Update-ZIP in den Repository-Root entpacken/übernehmen.
2. Wenn `UPDATE_FILES.txt` oder `DELETE_FILES.txt` enthalten ist,
   `APPLY_UPDATE.cmd` im Repository-Root starten.
3. `bin` und `obj` vollständig bereinigen, ausschließlich unter dieser Solution.
4. NuGet Restore.
5. Rebuild Solution.
6. Tests ausführen.
7. Relevante Smoke Tests im Emulator/Gerät durchführen.

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

Beim Spezialkarten-Upgrade **keine App-Daten löschen oder deinstallieren**: Bestehende
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

## Abschlussprüfung und Geräte-Smoke-Test

Nur tatsächlich ausgeführte Prüfungen als erfolgreich melden. Ist ein Build
lokal nicht möglich, bleibt er als offene Prüfung benannt. Ein Build ersetzt
keinen MAUI-XAML-Laufzeit- oder Gerätetest.

Vor einer Installation bzw. Lieferung relevante Builds und beide Testsuiten
ausführen. Auf dem Zielgerät zusätzlich prüfen:

- Navigation, neue Screens und Dialoge einschließlich Android-Zurück,
- alle drei Themes, große Schrift, Tastatur und Zahlenrad-/Slider-Bedienung,
- aktive Kartenwahl, Archivieren/Entarchivieren und historische Korrekturen,
- den oben beschriebenen Upgrade-Smoke-Test mit erhaltener Bestandsdatenbank.

Der erfolgreiche Abschluss dieser Geräteprüfungen ist noch nicht bestätigt.
Offene Codeabweichungen werden im `docs/UI_WIREFRAME.md` geführt.
