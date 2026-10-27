# Boardgame Tracker

Boardgame Tracker ist eine .NET-MAUI-App für Android und iOS zum Verwalten von
Brettspielgruppen und Partien. Erstes vollständig unterstütztes Spiel ist
**Arche Nova**.

## Aktueller Funktionsstand

- Hauptnavigation: `Spiele · Gruppen · Historie · Einstellungen`.
- Gruppen werden im Kontext eines ausgewählten Spiels angezeigt.
- Gruppen können angelegt, bearbeitet, permanent gemacht und archiviert werden.
- Spieler können gruppenübergreifend wiederverwendet oder beim Erstellen einer
  Gruppe neu angelegt werden.
- Arche-Nova-Defaultfarben werden pro `GroupPlayer` ergänzt.
- Partievorbereitung bleibt bis `Starten` nicht persistent.
- Startspieler und Zoopläne können ausgelost werden; jeder Spieler wählt einen
  der zwei angebotenen Zoopläne.
- Beim Start werden `GameSession`, `GameSessionPlayer` und
  `ArkNovaGameSessionPlayer` atomar persistiert.
- Laufende Partien können fortgesetzt, verworfen oder in die Wertung überführt
  werden.
- Arche-Nova-Wertung unterstützt Attraktivität, Artenschutz, optional Ruf,
  besondere Karten, Gleichstände und Ergebnisanzeige.
- Historie abgeschlossener Partien mit Spiel-/Gruppenfiltern, Wertungskorrektur und bestätigtem Löschen. Statistik folgt separat.
- Lokale Navigations- und Arche-Nova-Einstellungen werden über MAUI Preferences
  gespeichert.

## Architektur

Boardgame Tracker ist ein modularer Monolith mit pragmatischen Vertical Slices:

```text
App -> Application -> Core
          ^
          |
Infrastructure -> Application + Core
```

Verbindliches Domänenvokabular:

```text
Game
GameSession
Group
Player
```

Spielspezifischer Code liegt in jedem Projekt unter:

```text
Games/<GameName>/
```

Arche-Nova-Erweiterungen allgemeiner Entities werden über Composition modelliert,
z. B. `GameSessionPlayer` + `ArkNovaGameSessionPlayer` und `GroupPlayer` +
`ArkNovaGroupPlayer`.

Details: `docs/ARCHITECTURE.md` und `docs/DATA_MODEL.md`.

## Datenbank

Aktuell wird in der Pre-Alpha-Phase `Database.EnsureCreatedAsync()` verwendet.
Nach Schemaänderungen wird die lokale Datenbank gelöscht und aus dem aktuellen
EF-Core-10-Modell neu erstellt. Ein kanonischer Migrationsverlauf wird erst
wieder eingeführt, wenn veröffentlichte Nutzerdaten migrationsfähig erhalten
werden müssen.

## Dokumentation

```text
README.md
SETUP.md
CHANGELOG.md

docs/
  ARCHITECTURE.md
  DATA_MODEL.md
  PRODUCT_SPEC.md
  UI_DESIGN_GUIDE.md
  UI_WIREFRAME.md
```

Die Spezifikationen beschreiben ausschließlich den aktuellen Zustand.
Versionshistorie steht nur in `CHANGELOG.md`.

## Build-Hinweis

Nach strukturellen Updates oder Änderungen an MAUI-Ressourcen empfiehlt sich ein
vollständiges Löschen von `bin` und `obj`, anschließend Restore/Rebuild und eine
Neuinstallation der App im Emulator/Gerät.
