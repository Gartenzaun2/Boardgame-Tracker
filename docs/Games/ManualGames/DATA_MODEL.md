# Manuelle Spiele – Datenmodell

## 1. Ownership

Jedes manuell angelegte Spiel ist ein reguläres `Game`; Name, Capsule-Tag und Spielergrenzen liegen im **allgemeinen** Game-Modell. `ManualGames` ergänzt nur Konfiguration und Wertung. Gemeinsame `GameSession`, `GameSessionPlayer.Order?`, `Placement?` und Farbzuordnungen stehen in [../../DATA_MODEL.md](../../DATA_MODEL.md).

## 2. Konfiguration

```text
ManualGame
- GameId                    PK/FK -> Game
- CapturePoints             bool
- AllowNegativePoints       bool
- CaptureWinner             bool
- CapturePlacements         bool
- AllowTies                 bool
- ResultFromPoints          bool
- PointsDirection?          Highest | Lowest; null bei manueller Wertung
- CapturePlayerOrder        bool
- CapturePlayerColors       bool
```

Invarianten: `CapturePlacements => CaptureWinner`; `AllowTies` nur mit `CaptureWinner`; `AllowNegativePoints` nur mit `CapturePoints`; `ResultFromPoints => CapturePoints && CaptureWinner`; `PointsDirection` ist genau dann gesetzt, wenn `ResultFromPoints`. Deaktivierte abhängige Optionen sind fachlich unwirksam und werden beim Speichern normalisiert.

`Game.Key` hat bei manuellen Spielen den reservierten lokalen Schlüsselraum `manual:<GUID>`; der Schlüssel ist unveränderlich und **kein** geräteübergreifendes Matchingmerkmal. Integrierte Spielschlüssel liegen außerhalb dieses Namensraums. Manuelle Spielnamen sind innerhalb dieses Typs (auch für ausgeblendete Spiele) nach `Trim` und `OrdinalIgnoreCase` eindeutig; gleiche Namen integrierter Spiele sind zulässig.

## 3. Session und Wertung

```text
ManualGameSession
- GameSessionId             PK/FK -> GameSession
- ScoringRulesSnapshot     zum Partiestart gültige Wertungseinstellungen

ManualGameSessionPlayer
- GameSessionId
- PlayerId                  PK/FK -> GameSessionPlayer
- Points?                  int
```

`ScoringRulesSnapshot` hält den für diese laufende Partie relevanten unveränderlichen Erfassungsvertrag (Punktepflicht, negative Punkte, Sieger/Plätze, Gleichstand, Ableitungsart/-richtung). Er verhindert, dass spätere Änderungen von `ManualGame` einen bereits begonnenen `Running`-/`Scoring`-Ablauf umstellen. Für abgeschlossene historische Korrekturen entscheidet die tatsächlich gespeicherte Ergebnisform, nicht die aktuelle `ManualGame`-Konfiguration.

`Placement?` bleibt ausschließlich auf dem allgemeinen `GameSessionPlayer`; es gibt weder ein doppeltes Siegerfeld noch eine zweite Platzierung. `Placement = 1` bedeutet Sieg. Punkte sind **gespeicherte** ganze Zahlen, nicht aus einer spielspezifischen Formel abgeleitet. Keine frei konfigurierbare Punkteformel.

### Gültige abgeschlossene historische Ergebnisformen

- **Punkte:** entweder alle Sessionteilnehmer haben `Points` oder alle `null`; auch `0` ist ein erfasster Punktwert. Negative ganze Zahlen sind historisch zulässig.
- **Keine Platzierung:** alle `Placement = null`.
- **Nur Sieger:** alle `Placement` sind `null` oder `1`; mindestens ein `1` bei einer tatsächlich erfassten Siegerwertung. Mehrere `1` sind zulässig.
- **Vollständige Platzierung:** alle `Placement` in `1..n` und mindestens ein `1`; geteilte Plätze und Lücken sind zulässig.
- Die drei Ergebnisformen dürfen jeweils mit **vollständigen Punkten oder ohne Punkte** vorkommen. Eine Partie ohne Ergebniswerte ist ebenfalls gültig.
- Eine erfasste Startreihenfolge ist vollständig `1..n`; eine nicht erfasste besteht ausschließlich aus `Order = null`. Es gibt keine Mischformen.
- Farben sind optionale allgemeine Session-Daten; bei einer farblich erfassten manuellen Partie besitzt jeder Spieler eine eindeutige Farbe. Ohne Farberfassung existieren keine Farbzuordnungen.

Für einen **neuen Abschluss** gelten zusätzlich die `ScoringRulesSnapshot`-Pflichtfelder und die damalige Gleichstands-/Negativpunkteinstellung. Während `Scoring` sind partielle Zwischenstände erlaubt. Importierte `Completed`-Partien werden anhand ihrer **eigenen vollständigen Ergebnisform** und nicht gegen die heutige lokale Konfiguration validiert.

## 4. Persistenz und Historie

`ManualGame` referenziert `Game` eins-zu-eins. `ManualGameSession` referenziert `GameSession` eins-zu-eins. `ManualGameSessionPlayer` referenziert `GameSessionPlayer` über `(GameSessionId, PlayerId)`; kein duplizierter allgemeiner Fachzustand. Das Löschen eines manuellen Spiels ist nur ohne irgendeine Session zulässig und entfernt seine Konfiguration sowie aktuelle Gruppenzuordnungen/Farbdefaults. Spiel- und Spieler-Referenzen historischer Sessions bleiben durch die allgemeinen Restrict-Regeln geschützt.
