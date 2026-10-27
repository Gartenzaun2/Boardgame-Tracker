# Boardgame Tracker – Setup und Entwicklungsregeln

## Voraussetzungen

Zielplattformen:

- Android
- iOS

Technologie:

- .NET 10
- .NET MAUI
- EF Core 10
- SQLite
- xUnit v3 mit Microsoft Testing Platform

Empfohlene lokale Entwicklungsumgebung ist Visual Studio mit installierter
.NET-MAUI-Workload.

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
  PRODUCT_SPEC.md
  ARCHITECTURE.md
  DATA_MODEL.md
  UI_DESIGN_GUIDE.md
  UI_WIREFRAME.md

README.md
SETUP.md
CHANGELOG.md
```

`README.md`, `SETUP.md` und `CHANGELOG.md` liegen nur im Repository-Root.
Die übrigen kanonischen Dokumente liegen nur unter `docs/`. Doppelte Kopien
derselben Dokumente an mehreren Stellen sind nicht vorgesehen.

## Standardablauf nach einem Update

1. Geänderte/neue Dateien relativ zum Repository-Root übernehmen.
2. Falls `UPDATE_FILES.txt`/`DELETE_FILES.txt` vorhanden ist, müssen auch
   `APPLY_UPDATE.cmd` und `APPLY_UPDATE.ps1` enthalten sein.
   `APPLY_UPDATE.cmd` aus dem Repository-Root ausführen. Der CMD-Wrapper startet
   die robuste PowerShell-Implementierung, die Lösch-, Verschiebe- und
   Umbenennungsanweisungen automatisch verarbeitet.
3. NuGet Restore ausführen.
4. `Clean Solution`.
5. `Rebuild Solution`.
6. XAML-/Binding-Warnungen prüfen.
7. beide Testprojekte ausführen.
8. App starten und relevante Smoke Tests durchführen.

CLI, sofern ein passendes .NET-10-SDK und die nötigen Workloads installiert
sind:

```text
dotnet test
```

## Datenbankstrategie in der Pre-Alpha-Phase

Der aktuelle Runtime-Pfad verwendet:

```text
Database.EnsureCreatedAsync()
```

Danach stellt ein idempotentes Seeding sicher:

- Spiel `ark-nova`
- Zoopläne 1–14

Für den aktuellen Entwicklungsbetrieb ist deshalb keine neue Migration für
jeden Schemawechsel vorgesehen.

### Bei Schemaänderungen

`EnsureCreated` aktualisiert ein vorhandenes älteres Schema nicht.

Solange noch keine veröffentlichten Nutzerdaten erhalten werden müssen:

1. App-Daten löschen oder App deinstallieren,
2. App neu installieren/starten,
3. Datenbank aus dem aktuellen EF-Core-Modell neu erzeugen lassen.

Sobald reale Nutzerdaten versionsübergreifend erhalten werden müssen, endet
diese Strategie. Dann wird ein kanonischer EF-Core-Migrationsverlauf samt
`ModelSnapshot` verbindlich.

Im Repository vorhandene historische Migrationsdateien sind während des
`EnsureCreatedAsync()`-Betriebs nicht die autoritative Upgrade-Strategie.

## Lokale Preferences

Nicht-historische Einstellungen werden nicht in SQLite gespeichert.

### Arche Nova

- Punkteanzeige: neu/alt
- Ruf erfassen

### Navigation

- Standardspiel
- ausgewähltes Spiel
- letzter Hauptbereich
- zuletzt betrachtete offene Session

Die MAUI-Implementierungen verwenden native `Preferences`.

## Versionsregel

Auch in der Pre-Alpha-Phase wird jeder **ausgelieferte installierbare
Entwicklungsstand** eindeutig versioniert.

In `src/BoardgameTracker.App/BoardgameTracker.App.csproj`:

```xml
<ApplicationDisplayVersion>x.y.z</ApplicationDisplayVersion>
<ApplicationVersion>n</ApplicationVersion>
```

Regeln:

- `ApplicationDisplayVersion` entspricht exakt der ausgelieferten semantischen
  Entwicklungsversion, z. B. `0.8.2.2`.
- `ApplicationVersion` ist eine monoton steigende technische Buildnummer.
- Die Buildnummer muss nicht für jeden lokalen Zwischencommit steigen, aber für
  jedes ausgelieferte installierbare Update.
- Vor Erstellen eines Update-Pakets werden beide Werte geprüft.
- `README.md` beschreibt den funktionalen Stand; Versionshistorie steht in
  `CHANGELOG.md`.

Aktueller bekannter technischer Rückstand: Im Code-Stand v0.8.2.1 steht
`ApplicationDisplayVersion` noch auf `0.8.1.6` und `ApplicationVersion` auf `1`.
Das wird mit dem nächsten Code-Update korrigiert.

## Lieferregel für Entwicklungs-Updates

Standardmäßig wird **kein vollständiges Repository-ZIP** geliefert.

Ein Update-Paket enthält nur:

- geänderte Dateien,
- neu hinzugekommene Dateien,
- bei Dateioperationen `UPDATE_FILES.txt` (bzw. aus Kompatibilitätsgründen auch
  `DELETE_FILES.txt`),
- **immer wenn eine solche Operationsdatei enthalten ist:** `APPLY_UPDATE.cmd`
  **und** `APPLY_UPDATE.ps1`.

`APPLY_UPDATE.cmd` bleibt der vom Benutzer gestartete generische Runner. Er ruft
die danebenliegende `APPLY_UPDATE.ps1` auf; die PowerShell-Datei enthält die
eigentliche, über Updates wiederverwendbare Dateioperationslogik. Es werden keine
update-spezifischen Dateinamen in den Skripten fest codiert. Die Operationen werden
relativ zum Repository-Root aus der TXT-Datei gelesen. Unterstützte Befehle:

```text
DELETE|pfad/zur/datei
MOVE|alter/pfad|neuer/pfad
RENAME|alter/pfad|neuer/pfad
```

Für Abwärtskompatibilität darf `DELETE_FILES.txt` weiterhin reine Pfadzeilen
enthalten; diese werden als `DELETE` interpretiert. Leerzeilen und Zeilen mit `#`
am Anfang werden ignoriert. Das Skript muss vor jeder Operation Pfade auf das
Repository begrenzen, fehlende Quelldateien beim Löschen tolerieren und bei
Fehlern mit einem Fehlercode abbrechen. Dadurch bleibt das Skript über Updates
hinweg gleich; normalerweise ändert sich nur die Operationsdatei.

Nicht enthalten:

- unveränderte Dateien,
- `bin/`,
- `obj/`,
- sonstige generierte Build-Artefakte.

Alle Pfade im Update-Paket sind relativ zum Repository-Root.

Ein vollständiges Repository-Archiv wird nur auf ausdrücklichen Wunsch erstellt.

## Dokumentationsregel

Die kanonischen Spezifikationen beschreiben immer den aktuellen Stand.
Überholte Versionsabschnitte werden integriert oder entfernt.

Versionshistorie gehört nur in:

```text
CHANGELOG.md
```

Bei Änderungen werden betroffene Dokumente inhaltlich konsolidiert; neue
Versionsnotizen werden nicht einfach unten an Spezifikationsdateien angehängt.

## Smoke Tests

### Start und Navigation

- Frischer Start ohne Standardspiel: `Spiele`.
- Frischer Start mit Standardspiel: `Gruppen` für dieses Spiel.
- Unter `Spiele` Arche Nova wählen: Spielkontext wird vor dem Öffnen von
  `Gruppen` gespeichert.
- `Gruppen` darf im normalen Bedienfluss nie ohne gültigen Spielkontext
  erscheinen.
- Zwischen `Spiele`, `Gruppen`, `Historie`, `Einstellungen` wechseln, App
  schließen und letzten Hauptbereich prüfen.
- Eine offene Session öffnen, App schließen und prüfen, dass genau diese Session
  wiederhergestellt wird.
- Bei ungültigem/fehlendem Gruppen-Spielkontext auf `Spiele` zurückfallen bzw.
  das Standardspiel verwenden.

### Gruppen

- reguläre Gruppen laden,
- Filter `Temporär` und `Archiviert`,
- Gruppe anlegen,
- Gruppe bearbeiten,
- temporär -> permanent,
- archivieren/reaktivieren,
- Default-Reihenfolge ändern,
- Arche-Nova-Defaultfarben ändern,
- Aktiv-Icon nur für eine offene Session des aktuell ausgewählten Spiels,
- letzte Partie nur für das aktuell ausgewählte Spiel.

### Partievorbereitung

- letzte **abgeschlossene** Arche-Nova-Partie laden,
- vorherigen Verlierer auf Position 1 rotieren,
- bei geteiltem letzten Platz alphabetische Auflösung,
- zwei eindeutige Zoopläne pro Spieler auslosen,
- sessionweit keinen Zooplan doppelt anbieten,
- pro Spieler genau einen Zooplan auswählen,
- doppelte Spielerfarben vorübergehend zulassen,
- `Starten` bei unvollständigen Plänen oder doppelten Farben sichtbar
  deaktivieren,
- gültige Partie starten.

### Laufende Partie

- Reihenfolge, Spielerfarbe, Zooplan und Dauer,
- Wiederaufnahme nach App-Neustart,
- `Running -> Scoring`,
- `Verwerfen` mit Sicherheitsabfrage.

### Wertung

- Spielerkachel nur einmal öffnen, auch bei schnellem Doppeltap,
- Autosave für gültige Werte,
- Attraktivität `0..113`,
- Artenschutz `0..41`,
- Ruf leer oder `1..15`,
- Ruf-Feld gemäß Einstellung ein-/ausblenden,
- neue/alte Punkte unterscheiden sich exakt um 100,
- berechnete Punkte nach vollständigen Pflichtwerten hinter dem Namen,
- `Besondere Karten` als lokalisierte UI-Bezeichnung,
- Anzahl besonderer Karten in der Infozeile, kein Counter-Badge am Icon,
- Auswahl schließt nach Zuordnung/Entfernen,
- Mehrfachzuordnung derselben Karte an einen zweiten Spieler fragt explizit,
- `Wertung abschließen` bis zur Vollständigkeit sichtbar deaktiviert,
- Competition Ranking und Gleichstandsauflösung,
- Ergebnis bleibt bis `Fertig` sichtbar,
- Partiedauer wird angezeigt.

### Kontrast und Ergebnis

Auf **jeder spielerfarbig getönten Kachel** prüfen:

- Name/Punkte gut lesbar,
- Farbe/Zooplan gut lesbar,
- Attraktivität/Artenschutz gut lesbar,
- Ruf und besondere Karten gut lesbar,
- keine fachlichen Inhalte in grauer Sekundärtextfarbe.

Insbesondere den Ergebnis-Screen auf Blau, Gelb, Rot und Schwarz testen.

### Noch ausstehende fachliche Session-Regel

Zielregel:

> Pro Kombination aus Spiel + Gruppe darf höchstens eine Session in
> `Running` oder `Scoring` existieren.

Mehrere offene Sessions mit unterschiedlichen Spielen und/oder Gruppen sind
zulässig.

Der aktuelle Arche-Nova-Startpfad blockiert noch global bei irgendeiner offenen
Session. Bis zur Umsetzung dieser Regel muss dieser Punkt als bekannter
Implementierungsunterschied behandelt werden.


## Android-AppIcon

Der Dateiname im `MauiIcon Include` ist gleichzeitig der Android-Ressourcenname.
Bei `Include="Resources\AppIcon\appicon.png"` muss `Platforms/Android/AndroidManifest.xml` daher
`android:icon="@mipmap/appicon"` und `android:roundIcon="@mipmap/appicon_round"` verwenden.
Beim Umbenennen der AppIcon-Hintergrunddatei müssen diese Manifest-Verweise immer synchron angepasst werden.

