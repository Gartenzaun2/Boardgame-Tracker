# Boardgame Tracker v0.8.1.1 – Setup / Upgrade

## Upgrade von v0.8.1

v0.8.1.1 verwendet während der aktuellen Entwicklungsphase keine EF-Core-
Migrationen. Nach dem Übernehmen der Dateien:

1. `tools\upgrade-from-v081.cmd` einmal ausführen, damit die veralteten
   Migrations-Hilfsskripte entfernt werden,
2. NuGet Restore ausführen,
3. `Clean Solution`,
4. `Rebuild Solution` und insbesondere XAML-/Binding-Warnungen prüfen,
5. beide Testprojekte ausführen,
6. App starten und die unten genannten Smoke Tests durchführen.

Wenn deine lokale App-Datenbank aus einem älteren Schema stammt, lösche für
diesen Entwicklungsstand einmal die App-Daten bzw. deinstalliere die App. Beim
nächsten Start wird die Datenbank aus dem aktuellen Modell neu erzeugt und mit
den verpflichtenden Ark-Nova-Referenzdaten befüllt.

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
