# Boardgame Tracker – Setup und Entwicklung

## 1. Zweck

Diese Datei ist die kanonische Quelle für **Entwicklungsumgebung, Build/Test,
Entwicklungsdatenbank, Versionierung und Lieferung**.

Nicht hierher gehören Produktstatus, Softwarearchitektur, UI-Regeln, Forschungsstände
oder historische Schemaänderungen. Dafür gelten die jeweils zuständigen Dokumente
unter `docs/`.

Arbeits- und Verifikationspolicy: [`AGENTS.md`](AGENTS.md).

## 2. Voraussetzungen

- .NET 10
- .NET MAUI
- EF Core 10
- SQLite
- Visual Studio bzw. passende .NET-MAUI-Toolchain
- Android- und/oder iOS-Toolchain für die jeweilige Zielplattform

Unterstützte Mindestplattformen gemäß App-Projekt:

```text
Android 11 / API 30
iOS 15
```

Lokale macOS-/iOS-Konfiguration darf unter `.local/` liegen. Dieser Bereich wird
nicht versioniert. Persönliche Signing-, Zertifikats- und Gerätedaten gehören
niemals ins Repository.

Paketversionen werden zentral über `Directory.Packages.props` verwaltet.

## 3. Native Arche-Nova-Abhängigkeit

Die produktive Arche-Nova-Fotoerkennung benötigt vor einem entsprechenden
Android-/iOS-App-Build die lokal gebaute native Bibliothek.

Buildpfade, gepinnte Abhängigkeiten und Plattformbefehle stehen ausschließlich in:

`tools/Games/ArkNova/NativeVisionSpike/README.md`

Die erzeugten nativen Libraries werden nicht versioniert.

Produktstatus der Fotoerkennung:
`docs/Games/ArkNova/CV_STATUS.md`.

## 4. Build und automatisierte Tests

Vom Verzeichnis mit `BoardgameTracker.sln`:

```powershell
dotnet restore BoardgameTracker.sln
dotnet build BoardgameTracker.sln --no-restore
dotnet test --project tests/BoardgameTracker.Core.Tests --no-restore
dotnet test --project tests/BoardgameTracker.Application.Tests --no-restore
```

Die Tests verwenden xUnit v3 mit Microsoft Testing Platform; deshalb wird
`dotnet test --project` verwendet.

Welcher Teil davon für einen konkreten Auftrag tatsächlich ausgeführt werden soll,
richtet sich nach `AGENTS.md`. Geräte-/Emulator-/UI-Prüfungen sind dort ausdrücklich
geregelt und werden nicht durch diese Datei erweitert.

### Gesperrte App-Assembly

Ist `BoardgameTracker.App.dll` gesperrt:

1. laufende App bzw. Debug-Sitzung beenden und Build wiederholen;
2. bei fortbestehender Sperre die Solution schließen;
3. keinen Datenbankreset als Buildfix verwenden.

## 5. Entwicklungsdatenbank

Der Runtime-Start verwendet:

```text
Database.EnsureCreatedAsync()
```

Danach werden notwendige Referenzdaten idempotent ergänzt.

Für die aktuelle Pre-Alpha-Phase gilt:

- keine EF-Migrationsstrategie;
- keine Schema-Upgrades für ältere Entwicklungsdatenbanken;
- keine Compatibility-Aliase nur zum Erhalt alter Entwicklungsschemata;
- `EnsureCreated` migriert vorhandene Schemas nicht;
- bei inkompatiblen Schemaänderungen ist ein bewusster Neuaufbau bzw. eine
  Neuinstallation der Entwicklungsdatenbank zulässig;
- vor einem notwendigen Neuaufbau ausdrücklich auf möglichen lokalen Datenverlust
  hinweisen;
- vorhandene App-Daten niemals ungefragt löschen;
- vor dem ersten Deployment wird die Datenbankstrategie neu festgelegt.

Historische einzelne Schemaumbrüche gehören in `CHANGELOG.md`, nicht dauerhaft in
diese Setup-Anleitung.

Kompatibilität von `.bgt`-Dateien und Spielschemas steht in
`docs/IMPORT_EXPORT.md`.

## 6. Versionsregel

In `src/BoardgameTracker.App/BoardgameTracker.App.csproj`:

```xml
<ApplicationDisplayVersion>x.y.z.r</ApplicationDisplayVersion>
<ApplicationVersion>n</ApplicationVersion>
```

Für jeden abgeschlossenen Änderungsauftrag gilt:

- `ApplicationDisplayVersion` und `ApplicationVersion` werden beide erhöht;
- dies gilt auch für reine Dokumentations-, Test- oder Konfigurationsänderungen;
- mehrere Dateien und Korrekturschritte desselben Auftrags erzeugen keine
  zusätzlichen Erhöhungen;
- ohne ausdrücklich festgelegten größeren Versionssprung steigt die letzte
  Komponente von `ApplicationDisplayVersion` um eins;
- `ApplicationVersion` steigt ebenfalls um eins;
- beide Werte müssen gegenüber dem Auftragsbeginn erhöht sein;
- bei Auslieferung entspricht `ApplicationDisplayVersion` exakt der ausgelieferten
  Version;
- vor Abschluss werden beide Werte und der zugehörige `CHANGELOG.md`-Eintrag geprüft;
- vor einer Verpackung werden sie erneut geprüft.

Versionshistorie steht ausschließlich in `CHANGELOG.md`.

## 7. Entwicklungs-Updatepakete

Standardlieferung ist ein **Delta-Paket**, kein vollständiges Repository.

Ein Delta enthält nur:

- geänderte und neue Dateien;
- `UPDATE_FILES.txt` bzw. kompatibel `DELETE_FILES.txt`, wenn Dateien gelöscht,
  verschoben oder umbenannt werden;
- bei jeder Operationsdatei `APPLY_UPDATE.cmd` und `APPLY_UPDATE.ps1`.

Nicht enthalten werden `bin/`, `obj/` oder andere generierte Build-Artefakte.

Unterstützte Operationen:

```text
DELETE|pfad/zur/datei
MOVE|alter/pfad|neuer/pfad
RENAME|alter/pfad|neuer/pfad
```

Einfache Pfadzeilen in `DELETE_FILES.txt` gelten aus Kompatibilitätsgründen als
`DELETE`.

Die Apply-Skripte bleiben generisch. Sie müssen:

- alle Pfade relativ zum Repository-Root behandeln;
- Pfade außerhalb des Repositorys ablehnen;
- fehlende Dateien bei `DELETE` tolerieren;
- Zielordner für `MOVE`/`RENAME` anlegen;
- bei echten Fehlern mit Fehlercode abbrechen.

### Updatepaket übernehmen

1. Update-ZIP in den Repository-Root entpacken bzw. übernehmen.
2. Falls `UPDATE_FILES.txt` oder `DELETE_FILES.txt` enthalten ist,
   `APPLY_UPDATE.cmd` im Repository-Root ausführen.
3. `bin` und `obj` ausschließlich unter dieser Solution bereinigen.
4. NuGet Restore.
5. Solution neu bauen.
6. erforderliche automatisierte Tests ausführen.
7. Geräte-/Installations-/Releaseprüfung nur gemäß `AGENTS.md`.

## 8. Abhängigkeiten und Lizenzen

Bei Änderungen an Drittanbieterabhängigkeiten gelten die Regeln aus
[`docs/THIRD_PARTY_LICENSE_POLICY.md`](docs/THIRD_PARTY_LICENSE_POLICY.md).

Lizenzanzeige, Produktverhalten und konkrete Assetstruktur werden hier nicht
dupliziert.

## 9. Weiterführende Quellen

- Arbeitsweise und Verifikation: `AGENTS.md`
- Dokumentationszuständigkeiten: `docs/README.md`
- Softwarearchitektur: `docs/ARCHITECTURE.md`
- Import/Export: `docs/IMPORT_EXPORT.md`
- Drittanbieter-Lizenzen: `docs/THIRD_PARTY_LICENSE_POLICY.md`
- historische Änderungen: `CHANGELOG.md`
