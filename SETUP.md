# Boardgame Tracker – Setup und Entwicklungsregeln

## Voraussetzungen

- .NET 10
- .NET MAUI
- EF Core 10
- SQLite
- Visual Studio mit .NET-MAUI-Workload
- Android und/oder iOS Toolchain

macOS/iOS-spezifische Toolchain-Konfiguration kann lokal unter `.local/` liegen.
Dieser Entwicklerbereich wird nicht versioniert und nicht von Codex verwaltet.
Persönliche Signing- und Device-Daten gehören niemals ins Repository.

## Quellen und Architektur

Projektüberblick und Einstieg: `README.md`; Zuständigkeiten und Arbeitsregeln:
`AGENTS.md`; Projektgrenzen und Slice-Organisation: `docs/ARCHITECTURE.md`.
Hier stehen ausschließlich Umgebungs-, Build-, Datenbank- und Lieferregeln.

Die drei Produktionsprojekte sind Core, Application und App. Application referenziert
EF Core und EF Core Relational; App und Application-Tests referenzieren zusätzlich
den SQLite-Provider. Die Versionen bleiben zentral in Directory.Packages.props.
DatabaseInitializer und spielabhängiges Seeding liegen in Application; der App-Host
registriert DbContextFactory, SQLite und BGT_NAME in App/Persistence/SQLite.
Integrationstests verwenden denselben Provider-/Collation-Code mit isolierten Datenbanken.

## Lokaler Entwicklungsablauf

Die mobile Ark-Nova-Fotoerkennung benötigt vor dem App-Build den gemeinsamen
nativen OpenCV-Build. Gepinnte Abhängigkeiten, Android-ARM64/x64- und iOS-
Device-/Simulator-Befehle stehen im
[Native-README](tools/Games/ArkNova/NativeVisionSpike/README.md).
Die App verwendet die daraus erzeugten lokalen Libraries; diese werden nicht
versioniert. Auf Windows kompiliert das iOS-C#-Target ohne Apple-Linknachweis.
Fachliche Freigabe für assistierte Wertung, dokumentierte GT-Ergebnisse und
offene manuelle Runtime-Nachweise: [CV-Status](docs/ARK_NOVA_CV_STATUS.md).

Standard gemäß `AGENTS.md`: Code ändern → Build → automatisierte Tests → fertig.
Restore bei Bedarf; automatisierte Tests dürfen sinnvoll ergänzt oder erweitert
werden. Eine manuelle Geräte-/UI-Prüfung gehört nicht zum Standardabschluss.

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
7. Nur bei ausdrücklich beauftragter Geräte-/Installations-/Releaseprüfung:
   zusätzlich relevante Smoke-Tests im Emulator/Gerät durchführen.

## Datenbankstrategie in der Pre-Alpha-Phase

Der Runtime-Start verwendet:

```text
Database.EnsureCreatedAsync()
```

Anschließend werden notwendige Referenzdaten idempotent angelegt.

Bis zum ersten Deployment werden keine Migrationsstrategien geplant und keine
Kompatibilitätspfade für ältere Entwicklungsstände eingeführt oder beibehalten.
Das gilt auch für einmalige Schema-Upgrades und Compatibility-Aliase.

`EnsureCreated` migriert ein älteres Schema nicht. Bei inkompatiblen Änderungen
ist eine Neuinstallation bzw. ein Neuaufbau der Entwicklungsdatenbank zulässig.
Den Benutzer ausdrücklich informieren, wenn dies erforderlich ist und dabei
lokale Entwicklungsdaten verloren gehen. App-Daten nicht ungefragt löschen.

Installationen mit dem alten Integer-Spezialkartenschema (vor Version 0.9)
benötigen eine Neuinstallation; ihre lokalen Daten werden nicht übernommen.
Ab Version 0.9.0.10 benötigt `Player.CreatedAt` eine neue Entwicklungsdatenbank
bzw. Neuinstallation. Ältere Datenbanken besitzen diese erforderliche Spalte nicht;
lokale Entwicklungsdaten gehen beim Neuaufbau verloren. Es gibt keine Übernahme
oder automatische Löschung bestehender Daten durch die App.
Der aktuelle Content-Slice entfernt ArkNovaSpecialCard.Name und ergänzt
ArkNovaUserContent. Auch dafür ist eine frische Entwicklungsdatenbank bzw.
Neuinstallation erforderlich: EnsureCreated aktualisiert bestehende Tabellen nicht.
Beim bewussten Neuaufbau gehen lokale Entwicklungsdaten verloren. Bestehende Daten
werden weder automatisch gelöscht noch migriert. Alte .bgt-Dateien mit Ark-Nova-
Schema 1 werden nicht übernommen; das aktuelle Spielschema ist 2.
Es gibt keine EF-Migrationswerkzeuge oder versionsgebundenen Upgrade-Skripte.
Vor dem ersten Deployment wird die Datenbankstrategie neu festgelegt.

## Versionsregel

In `src/BoardgameTracker.App/BoardgameTracker.App.csproj`:

```xml
<ApplicationDisplayVersion>x.y.z.r</ApplicationDisplayVersion>
<ApplicationVersion>n</ApplicationVersion>
```

- Jeder abgeschlossene Änderungsauftrag erhöht `ApplicationDisplayVersion` und `ApplicationVersion`, auch bei reinen Dokumentations-, Test- oder Konfigurationsänderungen.
- Die Erhöhung erfolgt vor Abschluss der Arbeit, unabhängig von Commit, Push, Verpackung oder Installation. Mehrere Dateien und Korrekturschritte innerhalb desselben Auftrags benötigen keine getrennten Erhöhungen.
- Ohne ausdrücklich festgelegten größeren Versionssprung steigt die letzte Komponente von `ApplicationDisplayVersion` um eins; `ApplicationVersion` steigt ebenfalls um eins. Beide Werte müssen gegenüber dem Auftragsbeginn erhöht sein.
- `ApplicationDisplayVersion` entspricht dem aktuellen Entwicklungsstand und bei Auslieferung exakt der ausgelieferten Version.
- Vor jedem Abschluss werden beide Werte und der zugehörige Changelog-Eintrag geprüft; vor dem Verpacken werden sie erneut geprüft.
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

## Codebasierte Abschlussprüfung

Für normale Implementierungsaufgaben gelten Build und relevante automatisierte
Tests als Standardverifikation gemäß `AGENTS.md`. Nur tatsächlich ausgeführte
Prüfungen als erfolgreich melden; nicht ausführbare Builds/Tests ausdrücklich
benennen. Nach erfolgreicher codebasierter Prüfung keine zusätzliche manuelle
Prüfphase automatisch starten.

## Explizit beauftragte Geräte-/UI-Smoke-Tests

Ein Build ersetzt technisch keinen MAUI-XAML-Laufzeit- oder Gerätetest. Daraus
folgt keine automatische Geräteprüfung für normale Implementierungsaufgaben.
Manuelle und visuelle Prüfungen sind Opt-in: nur auf ausdrücklichen Auftrag,
bei konkret verlangtem Geräte-/UI-Smoke-Test oder im Rahmen einer ausdrücklich
angeforderten Installations-/Releaseprüfung durchführen.

Ohne solchen Auftrag weder die App zur Sichtprüfung starten noch Oberflächen
manuell durchklicken, Screenshots erstellen/analysieren, Bildanalyse durchführen
oder alle Themes manuell prüfen. Eine nicht ausgeführte visuelle Prüfung ist
kein Fehler; sie kann bei Bedarf als nicht ausgeführte optionale Prüfung benannt
werden. Maßgeblich für die Verifikationspolicy bleibt `AGENTS.md`.

Bei ausdrücklich beauftragter Geräteprüfung zunächst relevante Builds und beide
Testsuiten ausführen. Anschließend passend zum Prüfauftrag auf dem Zielgerät prüfen:

- Navigation, neue Screens und Dialoge einschließlich Android-Zurück,
- alle drei Themes, große Schrift, Tastatur und Zahlenrad-/Slider-Bedienung,
- aktive Kartenwahl, Archivieren/Entarchivieren und historische Korrekturen,
- bei inkompatiblen Änderungen den Start nach Neuinstallation mit frischer Datenbank.

Für iOS-Gerätetests ist die passende Apple-Toolchain erforderlich. Nur tatsächlich
ausgeführte Geräteprüfungen als erfolgreich melden.

## Entwicklungsdatenbank nach Import-/Export-Schemaänderung

Die Umstellung auf Player.IsArchived und eindeutige Player-/Group-Namensindizes
ist mit bisherigen Entwicklungsdatenbanken inkompatibel. Vor Installation dieses
Stands ist eine Neuinstallation bzw. ein bewusster Neuaufbau der Entwicklungsdatenbank
notwendig. Dabei gehen lokale Entwicklungsdaten verloren. Die App löscht oder migriert
bestehende Daten nicht automatisch. Keine persistenten Import-Aliase und keine DB-Migrationen.
