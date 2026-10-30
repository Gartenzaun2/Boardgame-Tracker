# Boardgame Tracker

Boardgame Tracker ist eine .NET-MAUI-App für Android und iOS zum Verwalten von
Brettspielgruppen und Partien. Erstes vollständig unterstütztes Spiel ist
**Arche Nova**.

Boardgame Tracker ist zugleich ein Lernprojekt. Architektur und Code sollen korrekt,
wartbar und nachvollziehbar sein. Nicht offensichtliche fachliche und technische
Entscheidungen werden bewusst dokumentiert; Kommentare erklären bevorzugt das Warum
statt den unmittelbar sichtbaren Code. Zusätzliche Patterns sind kein Selbstzweck.

## Aktueller Funktionsstand

- Hauptnavigation: `Spiele · Gruppen · Historie · Einstellungen`.
- Gruppen werden im Kontext eines ausgewählten Spiels angezeigt.
- Gruppen können angelegt, bearbeitet, permanent gemacht und archiviert werden.
- Spieler können gruppenübergreifend wiederverwendet oder beim Erstellen einer
  Gruppe neu angelegt werden.
- Spielabhängige Farbdefaults können bereits bei Gruppenanlage und -bearbeitung
  festgelegt werden; Arche Nova erlaubt Blau, Gelb, Rot und Schwarz.
- Partievorbereitung bleibt bis `Starten` nicht persistent.
- Startspieler und Zoopläne können ausgelost werden; jeder Spieler wählt einen
  der zwei angebotenen Zoopläne.
- Beim Start werden `GameSession`, `GameSessionPlayer` und
  `ArkNovaGameSessionPlayer` atomar persistiert.
- Laufende Partien können fortgesetzt, verworfen oder in die Wertung überführt
  werden.
- Arche-Nova-Wertung unterstützt Attraktivität, Artenschutz, optional Ruf,
  besondere Karten, Gleichstände und Ergebnisanzeige.
- Zahlenräder für Attraktivität/Artenschutz, optionaler horizontaler Ruf-Regler
  und direkte numerische Eingabe.
- Benutzerverwaltbare Arche-Nova-Spezialkarten mit GUID, Namen, 0–3 geordneten
  Symbolen und Archivstatus; 14 feste Symboltypen einschließlich Forschung.
- Historie abgeschlossener Partien mit Spiel-/Gruppenfiltern, Wertungskorrektur und bestätigtem Löschen. Statistik folgt separat.
- Drei unmittelbar umschaltbare App-Themes: Deep Forest (Standard), Königsblau und Dunkel; Historie mit kompakten Filtern und Monatsgruppen.
- Lokale Navigations- und Arche-Nova-Einstellungen werden über MAUI Preferences
  gespeichert.
- Fotoerkennung und Statistik sind geplante spätere Slices.

## Architektur

Modularer Monolith mit pragmatischen Vertical Slices und Composition:
`App → Application → Core`. Core enthält die technologieunabhängige Domain;
Application enthält Vertical Slices mit direktem EF-Core-Zugriff. App enthält
MAUI, den Composition Root und die SQLite-Providerkonfiguration.
Allgemeine Domänenbegriffe bleiben `Game`, `GameSession`, `Group` und `Player`;
Spielcode liegt unter `Games/<GameName>/`. Details stehen in
[Architektur](docs/ARCHITECTURE.md) und [Datenmodell](docs/DATA_MODEL.md).

## Datenbank

Neue Entwicklungsdatenbanken entstehen mit `EnsureCreatedAsync()`. Bis zum ersten
Deployment sind keine Migrationsstrategien oder Kompatibilitätspfade für ältere
Entwicklungsstände vorgesehen. Erfordert eine inkompatible Änderung eine
Neuinstallation mit Verlust lokaler Entwicklungsdaten, wird ausdrücklich darauf
hingewiesen. Details stehen in [SETUP.md](SETUP.md).

## Dokumentation und Einstieg in einen neuen Chat

[AGENTS.md](AGENTS.md) ist der Einstieg für Mitwirkende und Coding-Agenten und
ordnet jeder Frage das kanonische Dokument zu. Befehle laufen im Verzeichnis
mit `BoardgameTracker.sln`; in diesem Workspace ist das der Unterordner
`BoardgameTracker/`, nicht dessen übergeordneter Ordner mit den Quellgrafiken.
Für einen frischen Chat:

1. `git status` prüfen und vorhandene Änderungen bewahren.
2. AGENTS und die für den Auftrag relevanten Spezifikationen lesen.
3. UI-Stand und bekannte Codeabweichungen im [Wireframe](docs/UI_WIREFRAME.md)
   berücksichtigen; dokumentierte Abweichungen sind nicht automatisch zur Korrektur freigegeben.
4. Build und automatisierte Tests gemäß [Setup](SETUP.md) ausführen.
   Geräte-/UI-Smoke-Tests nur auf ausdrücklichen Auftrag oder im Rahmen einer
   ausdrücklich geplanten Installations-/Releaseprüfung. Maßgeblich für die
   Arbeits- und Verifikationspolicy bleibt [AGENTS.md](AGENTS.md).

Die Spezifikationen unter `docs/` beschreiben den gültigen Stand einschließlich
klar gekennzeichneter späterer Slices. Die [Versionshistorie](CHANGELOG.md)
ist historische Information, keine konkurrierende aktuelle Spezifikation.
Keine zusätzlichen Übergabe-/Kontextkopien anlegen.
