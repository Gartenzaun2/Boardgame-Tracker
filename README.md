# Boardgame Tracker – Scaffold v0.6

v0.6 schließt den ersten vollständigen Gruppen-Slice auf Basis der
pragmatischen Vertical-Slice-Architektur ab.

## Projekte

- `BoardgameTracker.App`: .NET MAUI, XAML, ViewModels und Navigation
- `BoardgameTracker.Application`: konkrete Use Cases, Read Models und
  notwendige Ports
- `BoardgameTracker.Core`: Domainmodell und reine Fachregeln
- `BoardgameTracker.Infrastructure`: EF Core, SQLite und technische
  Implementierungen

## In v0.6 umgesetzt

### Gruppenübersicht

- reguläre Gruppen laden
- Sortierung nach letzter Nutzung
- Mitglieder und letztes Spieldatum anzeigen
- neue Gruppe anlegen
- Bearbeiten-Icon pro Gruppe

### Gruppe anlegen

- eigene feste Kopfzeile mit Zurück, Information und grünem Speichern
- Gruppenname kompakt in einer Zeile
- temporäre Gruppe
- aktuelle Mitglieder zuerst
- vorhandene aktive Person per Picker hinzufügen
- neue Person sekundär anlegen
- Gaststatus
- Default-Reihenfolge per Hoch/Runter
- neue Personen werden erst mit der Gruppe persistiert

### Gruppe bearbeiten

Eigener Vertical Slice:

- Namen ändern
- temporär/regulär ändern
- Default-Reihenfolge ändern
- Mitgliedermenge bleibt unverändert

### Architektur

- `Application`-Schicht bleibt schlank und use-case-orientiert
- keine generischen Repositories
- kein `ICreateGroup`
- gezielte Ports nur an echten technischen Grenzen
- `Group` schützt seine fachlichen Invarianten
- `EditGroup` besitzt einen eigenen Application-/Infrastructure-Slice

### Tests

- Core-Tests für Group-Invarianten
- Application-Tests für `CreateGroupUseCase`
- Application-Tests für `EditGroupUseCase`

## Dokumentation

- `docs/ARCHITECTURE.md`
- `docs/DATA_MODEL.md` – jetzt konsolidiert als v0.3
- `docs/PRODUCT_SPEC.md`
- `docs/UI_WIREFRAME.md`

## Nächster großer Slice

Nach erfolgreichem lokalen Build und Test beginnt die
Arche-Nova-Partievorbereitung.
