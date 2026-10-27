# Boardgame Tracker – Scaffold v0.8.0

Mit v0.8.0 wird aus der bisher nur im UI gehaltenen Arche-Nova-Vorbereitung
eine echte laufende Partie.

## Umgesetzter Stand

- Die Partievorbereitung bleibt bis `Starten` nicht persistent.
- Beim Start werden `GameSession`, `GameSessionPlayer` und
  `ArkNovaSessionPlayer` gemeinsam gespeichert.
- Gespeichert werden Gruppe, Startzeit, Teilnehmer, Reihenfolge,
  tatsächliche Spielerfarben, beide angebotenen Zoopläne und der gewählte
  Zooplan.
- Core validiert beim Start nochmals Spielerzahl, eindeutige Farben,
  vollständige Zooplanauswahl und die sessionweite Eindeutigkeit der
  angebotenen Zoopläne.
- Application prüft zusätzlich, dass die gestarteten Personen exakt der
  festen Gruppenbesetzung entsprechen.
- Die letzte Partie der Vorbereitung ist ausdrücklich die letzte
  **abgeschlossene Arche-Nova-Partie**.
- Die Platzierungen der letzten Partie werden oberhalb der Vorbereitung
  angezeigt.
- Der Spieler auf dem letzten Platz wird durch zyklische Rotation auf
  Position 1 gesetzt. Teilen sich mehrere Personen den letzten Platz, wird
  alphabetisch nach Name entschieden.
- Das Zooplan-Auslosen liegt nicht mehr als Shuffle-Implementierung im
  MAUI-ViewModel, sondern in Application.
- Nach erfolgreichem Start öffnet sich eine minimale Ansicht der laufenden
  Partie mit Spielern, Reihenfolge, Farben, gewählten Zooplänen und Dauer.
- `Partie beenden` wechselt den Status von `Running` nach `Scoring` und
  öffnet den vorbereiteten Wertungs-Einstieg.
- `Verwerfen` löscht eine laufende Partie erst nach Bestätigung.

## Architektur

Die bestehende Projektgrenze bleibt unverändert:

- Core: fachliche Invarianten und reine Arche-Nova-Regeln,
- Application: Start-/Running-Use-Cases und Ports,
- Infrastructure: EF-Core-/SQLite-Implementierungen,
- App: Screen-State, MAUI-Darstellung und Shell-Navigation.

Es wurde kein generisches Repository, kein MediatR und kein zusätzlicher
Navigation-Service eingeführt.

## Datenbank

v0.8.0 benötigt **keine Schemaänderung** gegenüber dem vorhandenen
Session-/Arche-Nova-Modell und daher keine neue EF-Core-Migration.
Die bereits vorhandenen Referenzdaten für `ark-nova` und die 14 Zoopläne
werden vorausgesetzt.

## Noch offen

- Die eigentliche Wertungserfassung ist noch nicht implementiert; nach
  `Partie beenden` existiert zunächst nur der Einstieg in den Scoring-Slice.
- Beim App-Start wird eine vorhandene laufende bzw. in Wertung befindliche
  Arche-Nova-Partie automatisch wieder geöffnet. Solange eine offene Partie
  existiert, kann keine zweite Partie gestartet werden.
- Die bereits dokumentierten kleineren UI-Punkte (Touch-Ziele der
  Farbauswahl und Error-Label-Sichtbarkeit auf weiteren Gruppenseiten)
  bleiben unverändert offen.

Ein vollständiger `dotnet build` / `dotnet test` kann in der
Erstellungsumgebung nicht ausgeführt werden, weil dort kein .NET SDK
installiert ist. XAML/XML und die geänderten Abhängigkeitsgrenzen werden
statisch geprüft.
