# Changelog

Versionshistorie des Boardgame Trackers.

Die übrigen Projekt-MD-Dateien beschreiben ausschließlich den aktuellen
gültigen Stand.

## Unreleased – Dokumentationskonsolidierung

- Update-Pakete mit Dateioperationen erhalten künftig einen generischen `APPLY_UPDATE.cmd`; Lösch-, Verschiebe- und Umbenennungsaktionen werden deklarativ aus der Operationsdatei gelesen.

- Kanonische Dokumentstruktur festgelegt.
- Historische Versionsanhänge aus Spezifikationen werden in dieses Changelog
  ausgelagert.
- Widersprüche zu Navigation, Zooplanschlüssel, besonderen Karten,
  Datenbankstrategie und Session-Parallelität aufgelöst.
- Fachliche Zielregel festgelegt: maximal eine offene Session je Spiel + Gruppe.
- Verbindliche Kontrastregel ergänzt: keine grauen fachlichen Inhalte auf
  spielerfarbigen Kacheln.
- Versions-/Lieferregel präzisiert.
- Bekannte Diskrepanz dokumentiert: `ApplicationDisplayVersion` im Code ist noch
  `0.8.1.6`, obwohl der funktionale Stand v0.8.2.1 ist.

## v0.8.2.1

- Spielkontext vor automatischer Gruppen-Navigation stabilisiert.
- Defaultspiel-Start, Spielauswahl und Session-Wiederaufnahme auf gemeinsamen
  Navigationspfad vereinheitlicht.
- Ergebnis-Spielerkacheln sollten wieder Spielerfarbtönung verwenden.
- Keine Datenbankschemaänderung.

Bekannter Nachtest: Ergebnisfarben greifen noch nicht in allen Fällen korrekt.

## v0.8.2.0

- Expliziter Spiel-/Navigationskontext eingeführt.
- Hauptnavigation: `Spiele · Gruppen · Historie · Einstellungen`.
- Gruppenübersicht immer im Kontext eines ausgewählten Spiels.
- Optionales Standardspiel.
- Letztes Spiel, letzter Hauptbereich und zuletzt betrachtete Session in MAUI
  Preferences.
- Wertungstexte/Orientierungsinformationen kontrastreicher vereinheitlicht.
- Deutsche UI-Bezeichnungen `Besondere Karten` und `Ruf`.
- Keine Datenbankschemaänderung.

## v0.8.1.9

- Gemeinsamer sichtbarer Disabled-Zustand für Primäraktionen.
- `Starten` bis zur gültigen Zooplanauswahl deaktiviert.
- `Wertung abschließen` bis zu vollständigen Pflichtwerten deaktiviert.
- Überlappende Zeilen in Wertungskacheln bereinigt.
- Fehlende Werte als `–`.
- Counter-Badge am besonderen-Karten-Icon entfernt.
- Partiedatum in Wertung ergänzt.
- Spiele-/Einstellungen-Erklärungen hinter Info-Icons verschoben.

## v0.8.1.8

- Neutrales besonderes-Karten-Icon statt fotografischem Kartenrücken.
- Zusätzliche Wertungszeile für Kartenanzahl und optional Ruf.
- Fehlende Rufdarstellung in der Übersicht ergänzt.
- Delta-Lieferregel in Setup dokumentiert.

## v0.8.1.7

- Bottom Tabs auf `Spiele · Gruppen · Historie · Einstellungen` erweitert.
- Gruppen-Tab als Root-Ziel.
- Busy-Schutz gegen Doppeltap auf Wertungsspieler.
- Besondere-Karten-Auswahl direkt nach Aktion schließen.
- Berechnete Punkte hinter Spielernamen.
- Partiedauer im Ergebnis.
- Historie-Tab als Platzhalter vorbereitet.

## v0.8.1.6

- Spezialkartentexte lokalisiert.
- Allgemeine Hauptnavigation eingeführt.
- Aktiv-Icon für Gruppen mit offener Session.
- Wertung verwerfen.
- Touch-/Command-Fehler bei Spielerwahl korrigiert.
- Zooplanname statt bloßer Nummer in Wertung.
- Besondere-Karten-Auswahl als größere Kacheln.
- Keine Datenbankschemaänderung.

## v0.8.1.5

- Vollständiger Arche-Nova-Wertungsslice.
- Offizielles 42-stufiges Artenschutz-Lookup.
- Neue/alte Punkteanzeige.
- Wertevalidierung für Attraktivität, Artenschutz, Ruf.
- Autosave während `Scoring`.
- `Completed` als Grenze für Historie/Statistik.
- Globale Arche-Nova-Preferences.
- Fünf konkrete besondere Karten mit Composite Key.
- Gleichstands-Screen mit Competition Ranking.
- Ergebnis bleibt bis `Fertig`.
- Kamera-Button vorbereitet.
- Tests für Wertungslookup/Punkte/Ranking.

## v0.8.1.3

- `ArkNovaZooPlan.Number` als natürlicher Primärschlüssel festgelegt.
- Künstliche Zooplan-ID entfernt.
- Spielidentität weiterhin `Game.Id` plus stabiler Key für speziell
  unterstützte Spiele.
- Entwicklungsdatenbank bei Schemawechsel neu erzeugen.

## v0.8.1.1

- Entwicklungsbetrieb auf `Database.EnsureCreatedAsync()` umgestellt.
- Idempotentes Seeding für Arche Nova und 14 Zoopläne.
- Shell-Navigationsparameter zentralisiert.

## v0.8.1

- Technischer Gesamt-Review.
- Architekturgrenzen bereinigt.
- `IQueryAttributable`/`ShellNavigationQueryParameters`.
- kompilierte XAML-Bindings mit `x:DataType`.
- zustandslose Persistenzports.
- xUnit v3 / Microsoft Testing Platform.
- Touchziele und Lokalisierung konsolidiert.

## v0.8.0

- Persistenter Arche-Nova-Start-Slice.
- `GameSession` + Spieler + Arche-Nova-Snapshots atomar gespeichert.
- Laufende Partie.
- Wiederaufnahme offener Partie.
- `Running -> Scoring`.
- autoritative Setup-Regeln aus UI in Core/Application verschoben.

## v0.7.x

- Arche-Nova-Partievorbereitung aufgebaut.
- Gruppen-Defaultfarben.
- Zooplan-Auslosung und Auswahl.
- Startspielerrotation aus letzter abgeschlossener Partie.
- kompakte Vorbereitungskacheln, Touch-/Busy-Regeln und Fehlerdarstellung
  iterativ bereinigt.

## v0.6.x

- Gruppen-Slice mit Vertical-Slice-Struktur.
- Gruppenerstellung, Bearbeitung, feste Mitgliedschaft und Reihenfolge.
- temporäre Gruppen.
- Archivierung/Reaktivierung.
- grundlegender UI-Design-Guide und Fehler-/Busy-Konzept.
