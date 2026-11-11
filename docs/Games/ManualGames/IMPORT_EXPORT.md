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
  placementKind: none | winners | placements
  players: [{ playerId, points, color }]
```

`placementKind` ist ein **erforderliches, nicht-nullbares** JSON-Stringfeld mit genau den drei Werten:

- `none`: Keine historischen Sieger oder Platzierungen; alle allgemeinen `placement`-Werte sind `null`.
- `winners`: Reine Siegerwertung; mindestens ein `placement = 1`, alle übrigen `placement = null` oder `1`.
- `placements`: Vollständige historische Platzierung; jeder Teilnehmer hat einen gültigen Platz, mindestens einer Platz 1; Gleichstände und Ranglücken sind zulässig.

Die Platzierungsart bezeichnet **nur** Sieger/Plätze, nicht das Vorhandensein von Punkten. Punkte können bei jedem der drei Werte vollständig erfasst oder vollständig nicht erfasst sein. Auch `placementKind = none` mit ausschließlich vollständigen Punkten oder ohne jegliche Ergebniswerte ist gültig.

Beim Export bestimmt die tatsächliche historische Ergebnisform `placementKind`: Wo die gespeicherten Plätze eindeutig sind, anhand der Platzwerte; bei ausschließlich Platz 1 für alle Teilnehmer anhand des ursprünglichen `ScoringRulesSnapshot.CapturePlacements` (`true` → `placements`, `false` → `winners`). Keine aktuelle `ManualGame`-Konfiguration für diese Ableitung verwenden.

`points` und `color` sind **erforderliche JSON-Felder mit nullable Werten**. Die Farben werden als numerische `PlayerColor`-Enumwerte übertragen, ohne gespeicherte Gruppen-Farbdefaults. Die gemeinsame `gameSessions[].players[].placement` und `order` transportieren die tatsächlichen Sieger-/Platzwerte und die optionale historische Reihenfolge; `order` ist `null` bei nicht erfasster Reihenfolge. `placementKind` dupliziert keine Platzwerte, sondern erhält deren andernfalls mehrdeutige **historische Art**.

Die aus den Punktdaten einst abgeleitete Platzierung ist bereits im allgemeinen `placement` gespeichert und wird beim Import **nicht neu berechnet**. Nicht exportiert werden aktuelle Sichtbarkeits-Preferences, `GroupGame`-Zuordnungen, Farbdefaults, laufende Sessions oder der vollständige historische Regel-Snapshot abgeschlossener Sessions.

## 3. Validierung

- `games[].data` ist vollständig, selbst wenn eine einzelne historische Partie andere Ergebnisarten enthält. Name, Tag, Spielergrenzen und abhängige Einstellungen müssen als **Quellkonfiguration** gültig sein.
- Die Teilnehmermenge in `gameSessions[].data.players` entspricht exakt der allgemeinen Sessionteilnehmermenge; IDs sind eindeutig.
- `points`: ganzzahlig und **für alle Spieler gesetzt oder für alle `null`**. Negative historische Punkte sind zulässig, auch wenn die aktuelle Quell-/Zielkonfiguration sie nicht vorsieht.
- `placementKind` muss vorhanden und einer der exakt vereinbarten Werte sein. Die allgemeinen `placement`-Werte müssen zur angegebenen Art passen: `none` → alle `null`; `winners` → ausschließlich `null/1` mit mindestens einem `1`; `placements` → jeder Platz gültig, mindestens ein Platz `1`, Gleichstände und Ranglücken zulässig. Insbesondere darf bei **allen Teilnehmern auf Platz 1** sowohl `winners` als auch `placements` übertragen werden; der Wert von `placementKind` entscheidet eindeutig. Ein einziger anderer Platz als `1` erfordert vollständige Plätze.
- `order`: für alle `null` oder lückenlos `1..n`. Farben: für alle `null` oder eindeutige gültige Werte für alle Teilnehmer. Diese historischen Formen sind **unabhängig von aktuellen Spieloptionen** gültig.
- Ein Verstoß lehnt wie im gemeinsamen Transfervertrag die gesamte Datei ab; es gibt keinen partiellen Import. Fehlendes oder unbekanntes `placementKind` ist kein Anlass zur stillen Ableitung aus aktuellen Regeln oder zum partiellen Import.

## 4. Historische Ergebnisart beim Import

Beim Import wird `placementKind` unverändert als fachliche historische Ergebnisart übernommen. Weder die aktuelle Quellkonfiguration noch die lokale Zielkonfiguration darf die Platzierungsart überschreiben oder vorhandene Platzierungen neu aus Punkten berechnen.

Da das lokale `ManualGameSession`-Modell einen `ScoringRulesSnapshot` besitzt, das Wireformat aber keinen vollständigen historischen Snapshot überträgt, wird für **importierte abgeschlossene Sessions** ein minimaler, in sich gültiger lokaler Snapshot erzeugt, der die übertragene historische Ergebnisform reproduziert:

- `CapturePoints` entsprechend vollständig vorhandenen Punkten; `AllowNegativePoints` bei vorhandenen negativen Punkten so setzen, dass diese gültig bleiben.
- `CaptureWinner` genau dann, wenn `placementKind` nicht `none` ist; `CapturePlacements` genau bei `placements`.
- `AllowTies` bei historisch vorhandenen Gleichständen passend setzen.
- `ResultFromPoints = false`, `PointsDirection = null`: Es wird keine ursprüngliche automatische Ableitungsart oder Punkterichtung behauptet oder rekonstruiert.
- Optionen für historische Startreihenfolge/Farben entsprechen den tatsächlich übertragenen optionalen Sessiondaten, soweit für einen konsistenten lokalen Konfigurationszustand benötigt.

Dieser Snapshot ist ausschließlich ein **lokaler Rekonstruktionswert für bereits abgeschlossene importierte Partien**, nicht der nachgewiesene ursprüngliche Regelsnapshot. Die tatsächlichen Plätze, Punkte, Farben, Reihenfolge und die ausdrücklich übertragene Platzierungsart bleiben maßgeblich. Er wird nicht verwendet, um Ergebnisse erneut zu berechnen oder gegen heutige Spieleinstellungen einzuschränken. Insbesondere bleibt im Fall „alle Platz 1“ durch `CapturePlacements` die übertragene Unterscheidung auch nach einem erneuten Export und in der historischen Ergebniskorrektur erhalten.

Eine zusätzliche persistente Ergebnisart-Spalte oder eine Änderung des allgemeinen `.bgt`-Envelopes ist nicht erforderlich.

## 5. Zuordnung und Konfiguration

Nur **manuelle** Spiele sind Namenskandidaten für manuelle Spiele. Matching erfolgt nach `Trim` + `OrdinalIgnoreCase`, **nicht** über `Game.Id`, `Game.Key` oder gespeicherte Fremd-IDs. Gleichnamige Kandidaten werden vorgeschlagen und vom Benutzer bestätigt; eine Zuordnung gilt nur im aktuellen Importvorgang. Es gibt keine Fuzzy-Namenssuche oder dauerhaft gespeicherte Aliaslisten.

- **Match bestätigt:** lokale `Game.Id`, `Game.Key`, Name, Tag, Min-/Max-Spieler und gesamte ManualGame-Konfiguration bleiben unverändert. Alle in sich gültigen historischen Daten der Quelle werden übernommen.
- **Kein Match / bewusst trennen:** neues manuelles Spiel mit eigener lokaler Identität und vollständiger importierter Konfiguration erzeugen. Bei Namenskollision deterministisch `Name-Importiert1`, `Name-Importiert2`, … verwenden; nötigenfalls Stamm zur zulässigen Länge kürzen.
- Integrationsspiel mit gleichem Namen: niemals ein Match-Kandidat für ManualGames.
- Für mehrere Quellspiele gelten eindeutige Zuordnungen im Importplan; lokale manuelle Spiele werden nicht implizit miteinander vereinigt.

Das normale Dependency-Pruning nach übersprungenen Sessions, Preview, atomare Commit-Transaktion und die vollständige Erhaltung lokaler Gruppen- und Spieleridentitäten gelten unverändert.

## 6. Spielergrenzen bei neuen Gruppenzuordnungen

Beim Import darf die historische Session mit sämtlichen Spielern erhalten bleiben, selbst wenn die lokale Spielkonfiguration heute weniger oder mehr Spieler verlangt. Bei **neuen** Gruppen wird `GroupGame` nur erzeugt, wenn deren Größe innerhalb des **lokalen** `[MinPlayers, MaxPlayers]` liegt; sonst wird die aktuelle Zuordnung ausgelassen, nicht die Session. Bestehende Gruppen-Zuordnungen werden nicht verändert. Historische Sessions benötigen keine aktuelle `GroupGame`-Zuordnung.
