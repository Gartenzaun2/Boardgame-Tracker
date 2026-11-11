# Manuelle Spiele – Import / Export

## 1. Vertrag

Dieser Vertrag ergänzt ausschließlich [../../IMPORT_EXPORT.md](../../IMPORT_EXPORT.md). Der gemeinsame `.bgt`-Envelope bleibt `formatVersion = 1`; sämtliche manuellen Spiele verwenden **ein** ManualGames-Transferschema, zunächst `schemaVersion = 1`. Jedes `games[]`-Element eines manuellen Spiels trägt seine **vollständige aktuelle Konfiguration**, damit es bei Bedarf neu angelegt werden kann. Die aktuellen lokalen Einstellungen werden beim Matching nicht ersetzt.

Das manuelle `games[].key` identifiziert den Eintrag **nur innerhalb der Importdatei** (Referenzen der Sessions); es wird **nie** zur geräteübergreifenden Gleichsetzung verwendet. Integrierte Keys und manuelle `manual:<GUID>`-Keys sind getrennte Namensräume; ein manuelles „Arche Nova“ ist nie Kandidat für das integrierte Arche Nova.

## 2. Payloads

```text
games[].data (ManualGames v1):
  name, badgeText, minPlayers, maxPlayers,
  capturePoints, allowNegativePoints,
  captureWinner, capturePlacements, allowTies,
  resultFromPoints, pointsDirection,
  capturePlayerOrder, capturePlayerColors

gameSessions[].data (ManualGames v1):
  players: [{ playerId, points, color }]
```

`points` und `color` sind **erforderliche JSON-Felder mit nullable Werten**. Die Farben werden als numerische `PlayerColor`-Enumwerte übertragen, ohne gespeicherte Gruppen-Farbdefaults. Die gemeinsame `gameSessions[].players[].placement` und `order` transportieren Sieg/Platz und optionale historische Reihenfolge; `order` ist `null` bei nicht erfasster Reihenfolge.

Die aus den Punktdaten einst abgeleitete Platzierung ist bereits im allgemeinen `placement` gespeichert und wird beim Import **nicht neu berechnet**. Nicht exportiert werden aktuelle Sichtbarkeits-Preferences, `GroupGame`-Zuordnungen, Farbdefaults, laufende Sessions oder ein historischer Regel-Snapshot abgeschlossener Sessions.

## 3. Validierung

- `games[].data` ist vollständig, selbst wenn eine einzelne historische Partie andere Ergebnisarten enthält. Name, Tag, Spielergrenzen und abhängige Einstellungen müssen als **Quellkonfiguration** gültig sein.
- Die Teilnehmermenge in `gameSessions[].data.players` entspricht exakt der allgemeinen Sessionteilnehmermenge; IDs sind eindeutig.
- `points`: ganzzahlig und **für alle Spieler gesetzt oder für alle `null`**. Negative historische Punkte sind zulässig, auch wenn die aktuelle Quell-/Zielkonfiguration sie nicht vorsieht.
- `placement`: entweder vollständig `null`, ausschließlich `null/1` (Sieger-only) oder vollständig gültige Plätze mit mindestens einem Platz 1. Mehrere Gewinner, Ranggleichheiten und Ranglücken können historisch vorkommen. Ein einziger anderer Platz als 1 erfordert die Platzierung aller Spieler.
- `order`: für alle `null` oder lückenlos `1..n`. Farben: für alle `null` oder eindeutige gültige Werte für alle Teilnehmer. Diese historischen Formen sind **unabhängig von aktuellen Spieloptionen** gültig.
- Ein Verstoß lehnt wie im gemeinsamen Transfervertrag die gesamte Datei ab; es gibt keinen partiellen Import.

## 4. Zuordnung und Konfiguration

Nur **manuelle** Spiele sind Namenskandidaten für manuelle Spiele. Matching erfolgt nach `Trim` + `OrdinalIgnoreCase`, **nicht** über `Game.Id`, `Game.Key` oder gespeicherte Fremd-IDs. Gleichnamige Kandidaten werden vorgeschlagen und vom Benutzer bestätigt; eine Zuordnung gilt nur im aktuellen Importvorgang. Es gibt keine Fuzzy-Namenssuche oder dauerhaft gespeicherte Aliaslisten.

- **Match bestätigt:** lokale `Game.Id`, `Game.Key`, Name, Tag, Min-/Max-Spieler und gesamte ManualGame-Konfiguration bleiben unverändert. Alle in sich gültigen historischen Daten der Quelle werden übernommen.
- **Kein Match / bewusst trennen:** neues manuelles Spiel mit eigener lokaler Identität und vollständiger importierter Konfiguration erzeugen. Bei Namenskollision deterministisch `Name-Importiert1`, `Name-Importiert2`, … verwenden; nötigenfalls Stamm zur zulässigen Länge kürzen.
- Integrationsspiel mit gleichem Namen: niemals ein Match-Kandidat für ManualGames.
- Für mehrere Quellspiele gelten eindeutige Zuordnungen im Importplan; lokale manuelle Spiele werden nicht implizit miteinander vereinigt.

Das normale Dependency-Pruning nach übersprungenen Sessions, Preview, atomare Commit-Transaktion und die vollständige Erhaltung lokaler Gruppen- und Spieleridentitäten gelten unverändert.

## 5. Spielergrenzen bei neuen Gruppenzuordnungen

Beim Import darf die historische Session mit sämtlichen Spielern erhalten bleiben, selbst wenn die lokale Spielkonfiguration heute weniger oder mehr Spieler verlangt. Bei **neuen** Gruppen wird `GroupGame` nur erzeugt, wenn deren Größe innerhalb des **lokalen** `[MinPlayers, MaxPlayers]` liegt; sonst wird die aktuelle Zuordnung ausgelassen, nicht die Session. Bestehende Gruppen-Zuordnungen werden nicht verändert. Historische Sessions benötigen keine aktuelle `GroupGame`-Zuordnung.
