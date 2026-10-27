# Boardgame Tracker v0.8.1.7 – Setup / Upgrade

## Upgrade auf v0.8.1.7

v0.8.1.7 enthält ausschließlich UI-/Navigationsänderungen sowie zusätzliche
Read-Model-Daten für die Ergebnisdauer. Das Datenbankschema ändert sich gegenüber
v0.8.1.6 nicht.

Nach dem Übernehmen der geänderten/neuen Dateien:

1. NuGet Restore ausführen,
2. `Clean Solution`,
3. `Rebuild Solution` und insbesondere XAML-/Binding-Warnungen prüfen,
4. beide Testprojekte ausführen,
5. App starten und die unten genannten Smoke Tests durchführen.

Von v0.8.1.6 auf v0.8.1.7 ist **kein Datenbank-Reset** erforderlich.

Es werden für dieses Update keine bestehenden App-/Projektquelldateien gelöscht;
deshalb gibt es kein neues Upgrade-/Löschskript.

## Testprojekte

Die Tests verwenden ab v0.8.1 xUnit v3 mit Microsoft Testing Platform. Das
Root-`global.json` wählt für `.NET 10` den MTP-Test-Runner; die Testprojekte
sind ausführbare Testprojekte und referenzieren `xunit.v3.mtp-v2`.

In Visual Studio 2026 sollten die Tests nach einem Restore/Build im Test
Explorer erscheinen. Alternativ kann mit einem .NET-10-SDK aus dem
Repository-Root ausgeführt werden:

```text
dotnet test
```

## Empfohlene Smoke Tests

### Gruppen

- Gruppenliste laden und `Temporär`/`Archiviert` schnell umschalten.
- Gruppe anlegen und bearbeiten.
- Ark-Nova-Defaultfarben ändern.
- Prüfen, dass Fehlerbereiche ohne Fehlermeldung keinen Leerraum reservieren.

### Partievorbereitung

- Gruppe öffnen.
- Prüfen, dass nur die letzte **abgeschlossene** Arche-Nova-Partie angezeigt
  wird.
- Bei gemeinsamem letzten Platz prüfen, dass alphabetisch der erste Name auf
  Position 1 rotiert wird.
- Zoopläne auslosen und je Spieler einen Plan auswählen.
- Doppelte Farben zulassen, aber Start dadurch blockieren.
- Danach eindeutige Farben wählen und Partie starten.

### Laufende Partie

- Prüfen, dass Spieler, Reihenfolge, Farbe, Zooplan und Dauer geladen werden.
- App schließen/neu öffnen und Wiederaufnahme der offenen Partie prüfen.
- `Partie beenden` testen (`Running -> Scoring`).
- In einem separaten Lauf `Verwerfen` einschließlich Sicherheitsabfrage testen.

### Wertung

- Prüfen, dass alle Spielerkacheln selbst antippbar sind und den numerischen
  Eingabescreen öffnen.
- Prüfen, dass Farbe und lokalisierter Zooplanname gut lesbar angezeigt werden.
- `Verwerfen` in der Wertung einschließlich Sicherheitsabfrage testen.
- Untere Navigation `Spiele / Gruppen / Historie / Einstellungen` von mehreren
  Screens aus testen. `Gruppen` muss immer zur Gruppenübersicht führen und bleibt
  beim normalen App-Start der aktive Bereich.
- In der Gruppenübersicht bei einer offenen Session das Aktiv-Icon neben dem
  Gruppennamen prüfen.
- Nach `Partie beenden` prüfen, dass die Session in `Scoring` wieder aufgenommen
  werden kann und noch nicht als abgeschlossen gilt.
- Für einen Spieler Attraktivität/Artenschutz eingeben, kurz warten, zurückgehen
  und prüfen, dass der gültige Zwischenstand per Autosave erhalten bleibt.
- Wertebereich prüfen: Attraktivität 0–113, Artenschutz 0–41, Ruf leer oder 1–15.
- In den Arche-Nova-Einstellungen `Ruf erfassen` aus-/einschalten und prüfen,
  dass die Eingabe entsprechend ein-/ausgeblendet wird.
- Neue/alte Punkteanzeige umschalten; die angezeigten Werte müssen exakt 100
  Punkte auseinanderliegen, ohne die Rohwerte zu verändern.
- Das Spezialkarten-Icon rechts in jeder Spielerkachel antippen; freie Spezialkarten
  müssen hervorgehoben sein und sich per Auswahl zuordnen lassen. Das Flyout muss
  nach Auswahl direkt schließen. Unter Attraktivität/Artenschutz muss die kompakte
  Zusammenfassung der Spezialkartenanzahl und – bei aktivierter Erfassung – des
  Rufs erscheinen.
- Dieselbe Spezialkarte einem zweiten Spieler zuordnen und die explizite
  Mehrfachzuordnungs-Abfrage prüfen.
- `Wertung abschließen` darf erst aktiv sein, wenn bei allen Spielern
  Attraktivität und Artenschutz vorhanden sind.
- Bei Punktgleichstand prüfen, dass alle Platzrahmen sichtbar sind, aber nur die
  punktgleichen Spieler innerhalb ihres ursprünglichen Rangbereichs verschoben
  werden können; Gleichstand unverändert lassen und alternativ auflösen.
- Nach vollständiger Eingabe prüfen, dass die berechneten Punkte direkt hinter
  dem Spielernamen stehen.
- Nach Abschluss den Ergebnis-Screen prüfen; dort muss zusätzlich die Partiedauer
  stehen. Erst `Fertig` soll zur Gruppenübersicht zurückführen.
- Kamera-Button ist bereits am endgültigen Platz sichtbar, die eigentliche
  Fotoerkennung gehört noch nicht zu v0.8.1.5.

## Datenbankstrategie während der Programmierphase

Der Runtime-Code verwendet aktuell:

```text
Database.EnsureCreatedAsync()
```

Danach werden das Spiel `ark-nova` und alle 14 Zoopläne idempotent als
Referenzdaten sichergestellt. Dadurch ist keine Initialmigration erforderlich.

Daher gibt es in diesem Entwicklungsstand bewusst auch **kein**
`create-*-migration.cmd`: Es wäre mit `EnsureCreatedAsync()` widersprüchlich.

`EnsureCreated` aktualisiert ein bereits vorhandenes, älteres Schema nicht. Bei
einer Schemaänderung wird die lokale Entwicklungsdatenbank deshalb gelöscht und
neu erzeugt. Für Android ist das beispielsweise durch Deinstallieren der App
`de.boardgametracker.app` oder Löschen ihres App-Speichers möglich.

Sobald veröffentlichte Nutzerdaten über Schemaänderungen hinweg erhalten werden
müssen, wird diese Entwicklungsstrategie beendet und ein versionierter
EF-Core-Migrationsverlauf eingeführt.

## v0.8.1.3 – Entwicklungsdatenbank neu erstellen

v0.8.1.3 vereinfacht die Ark-Nova-Zooplanschlüssel: `Number` ist jetzt der Primärschlüssel und die Session-Fremdschlüssel heißen entsprechend `...Number`. Da die aktuelle Entwicklungsphase bewusst `EnsureCreatedAsync()` statt EF-Migrationen verwendet, kann ein vorhandenes altes Schema nicht automatisch angepasst werden.

Nach dem Update auf v0.8.1.3 daher einmal die App-Daten löschen oder die App deinstallieren und neu installieren. Anschließend wird die Datenbank aus dem aktuellen Modell neu erzeugt und die Ark-Nova-Referenzdaten werden automatisch angelegt.
## Lieferregel für Entwicklungs-Updates

Update-Pakete enthalten standardmäßig **nur geänderte oder neu hinzugekommene
Dateien**. Ein vollständiges Repository-ZIP wird nur auf ausdrücklichen Wunsch
erstellt.

- Unveränderte Projektdateien werden nicht erneut mitgeliefert.
- Generierte Ordner und Dateien wie `bin` und `obj` gehören nicht in das Paket.
- Müssen vorhandene Dateien entfernt werden, enthält das Paket eine
  `DELETE_FILES.txt` mit den relativ zum Repository-Root zu löschenden Pfaden;
  bei komplexeren Upgrade-Schritten kann stattdessen bzw. zusätzlich ein
  passendes Upgrade-Skript mitgeliefert werden.
- Neue und geänderte Dateien behalten im Paket ihre Pfade relativ zum
  Repository-Root, damit das Update direkt über den bestehenden Stand kopiert
  werden kann.

