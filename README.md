# Boardgame Tracker – Scaffold v0.5

v0.5 setzt die validierte Vertical-Slice-Architektur erstmals im Code
um.

## Projekte

- `BoardgameTracker.App`: .NET MAUI, XAML, ViewModels und Navigation
- `BoardgameTracker.Application`: konkrete Use Cases, Read Models und
  notwendige Ports
- `BoardgameTracker.Core`: Domainmodell und reine Fachregeln
- `BoardgameTracker.Infrastructure`: EF Core, SQLite und technische
  Implementierungen

## Enthaltener End-to-End-Slice

Der Gruppen-Slice unterstützt:

- reguläre oder temporäre Gruppe anlegen,
- vorhandene aktive Personen auswählen,
- neue Personen direkt beim Anlegen einer Gruppe erzeugen,
- neue Person als Gast markieren,
- Standard-Reihenfolge festlegen,
- Gruppe und neue Personen atomar in SQLite speichern,
- reguläre Gruppen nach Neustart wieder laden,
- Mitglieder in Standard-Reihenfolge anzeigen.

## Architekturänderungen gegenüber v0.4

- neues Projekt `BoardgameTracker.Application`
- `GroupStore` entfernt
- `GroupListItem` und Create-Group-Modelle liegen in Application
- `IListGroupsQuery` und `ICreateGroupDataAccess` sind gezielte Ports
  statt generischer Repositories
- `Game` liegt unter `Core/Games`
- Cross-Aggregate-Navigationen wurden entfernt, wenn die Domain sie
  nicht benötigt
- `Group` schützt Mitglieder- und Reihenfolge-Invarianten
- `GameSession` schützt Teilnehmer und Statusübergänge
- `ArkNovaSessionPlayer.SelectedZooPlanId` ist für persistierte Sessions
  verpflichtend

Siehe `docs/ARCHITECTURE.md` für den verbindlichen Architekturstand.
