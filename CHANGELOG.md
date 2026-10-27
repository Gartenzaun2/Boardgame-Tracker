# Changelog

## 0.8.4.0

- Completed-Partien mit Spiel-/Gruppenfiltern, aktuellen Namen und gespeicherten Platzierungen.
- Explizite Arche-Nova-Korrektur und bestätigtes Löschen abgeschlossener Partien; normale Scoring-Statusgrenze bleibt erhalten.
- Fachliche Festlegung zu Historie und späterer unabhängiger Statistik kanonisch dokumentiert.
- App-Version 0.8.4.0 / ApplicationVersion 9; keine Schemaänderung.


## 0.8.3.2

- Android Adaptive App Icon: Foreground füllt die Launcher-Maske stärker aus; der sichtbare Hintergrundring um das Logo wird deutlich reduziert.
- Arche-Nova-Ergebnis: Attraktivität, Artenschutz und besondere Karten verwenden auf spielerfarbig getönten Karten jetzt die Primärtextfarbe gemäß verbindlicher Kontrastregel.

Versionshistorie des Boardgame Trackers.

Die übrigen Projekt-MD-Dateien beschreiben ausschließlich den aktuellen
gültigen Stand.

## 0.8.3.1

- Buildfehler `CS1931` in `SqliteArkNovaRunningGameSessionQuery` behoben: lokale Ergebnisvariable und LINQ-Range-Variable verwenden wieder eindeutige Namen.
- Der dadurch verursachte Folgefehler `CS0006` für die fehlende `BoardgameTracker.Infrastructure.dll` entfällt nach erfolgreichem Infrastructure-Build.
- App-Version auf 0.8.3.1 / ApplicationVersion 7 erhöht.

## 0.8.3.0

- Domänenvokabular vollständig auf `Game`, `GameSession`, `Group` und `Player` vereinheitlicht.
- `Person` -> `Player`, `GroupPerson` -> `GroupPlayer`, `PersonId` -> `PlayerId`.
- `ArkNovaSessionPlayer` -> `ArkNovaGameSessionPlayer` und weitere verkürzte Session-Namen auf `GameSession` vereinheitlicht.
- Spielespezifischer Code in allen Projekten unter top-level `Games/<GameName>/` konsolidiert.
- Ark-Nova-Application- und UI-Code nach GameSession-Vertical-Slices strukturiert.
- Ark-Nova-EF-Konfigurationen, Seeder und SQLite-Implementierungen in das Ark-Nova-Infrastructure-Modul verschoben.
- Composition als verbindliches Modell für spielespezifische Entity-Erweiterungen dokumentiert; allgemeine Felder werden nicht dupliziert.
- Historische EF-Migrationen aus dem aktuellen Pre-Alpha-Quellmodell entfernt; Runtime bleibt bei `EnsureCreatedAsync()`. Nach diesem Schema-Refactoring ist ein lokaler DB-Reset erforderlich.
- Kanonische Dokumentstruktur bereinigt und Architektur-/Datenmodell-Dokumentation auf Current State konsolidiert.
- App-Version auf 0.8.3.0 / ApplicationVersion 6 erhöht.
- In der Erstellungsumgebung war kein `dotnet`-SDK verfügbar; Build/Test müssen lokal per Rebuild und Testlauf bestätigt werden.

## 0.8.2.5

- Android-Launcher-Icon endgültig auf den kanonischen Ressourcennamen `appicon` vereinheitlicht.
- Ursache des Roboter-Icons behoben: Der `MauiIcon`-Hintergrund hieß zuvor `appiconbg`; Androids Manifest muss aber exakt den aus dem `MauiIcon Include`-Dateinamen abgeleiteten Mipmap-Namen referenzieren.
- `MauiIcon Include` verwendet jetzt `Resources\AppIcon\appicon.png`, Foreground bleibt `appiconfg.png`; AndroidManifest referenziert passend `@mipmap/appicon` und `@mipmap/appicon_round`.
- `ForegroundScale="0.70"` gesetzt, damit das Logo innerhalb der Android-Adaptive-Icon-Safe-Zone bleibt.
- App-Version auf 0.8.2.5 / ApplicationVersion 5 erhöht.

## 0.8.2.4

- Android-Buildfehler APT2260/APT2067 behoben: explizite `@mipmap/appicon`- und `@mipmap/appicon_round`-Referenzen aus `AndroidManifest.xml` entfernt.
- Das Launcher-Icon wird wieder ausschließlich über `MauiIcon` in der Projektdatei generiert und vom MAUI-Build eingebunden.
- App-Version auf 0.8.2.4 / ApplicationVersion 4 erhöht.

## 0.8.2.3

- Android-Launcher-Icon auf ein explizites MAUI Adaptive Icon umgestellt.
- Farbige Logo-Grafik wird als Foreground mit deckendem App-Hintergrund erzeugt.
- AndroidManifest referenziert `@mipmap/appicon` und `@mipmap/appicon_round` explizit.
- App-Version auf 0.8.2.3 / ApplicationVersion 3 erhöht.

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
