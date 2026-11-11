# Boardgame Tracker – Allgemeines Datenmodell

## 1. Geltungsbereich

Diese Datei beschreibt das spielübergreifende Domain- und Persistenzmodell.
Spielspezifische Ergänzungen stehen unter
`docs/Games/<GameName>/DATA_MODEL.md`.

Grundsätze:

- stabile Identitäten statt Anzeigenamen als Referenzen;
- Spielmodule ergänzen allgemeine Aggregate per Composition;
- zuverlässig ableitbare Werte werden nicht redundant gespeichert;
- Datenbank-Constraints ergänzen Domainregeln, ersetzen sie aber nicht.

## 2. Identitäten und Namen

Neue reguläre `Player`, `Group` und `GameSession` erhalten
`Guid.CreateVersion7()`. Import darf gültige nichtleere IDs übernehmen.
Deterministische Referenz-IDs eines Spielmoduls werden dort dokumentiert.

`Player.Name` und `Group.Name`:

```text
Trim()
1..100 Zeichen
OrdinalIgnoreCase
```

Keine Fuzzy-/Akzent-/Satzzeichen- oder innere-Leerzeichen-Normalisierung.

Player-Namen sind unter Playern, Group-Namen unter Groups jeweils einschließlich
archivierter Datensätze eindeutig. Beide Typen teilen keinen Namensraum.
SQLite schützt dies mit eindeutigen Indizes und `BGT_NAME`.

## 3. Player

```text
Player
- Id
- Name
- IsGuest
- IsArchived
- CreatedAt
```

`Player` ist Aggregate Root. Gaststatus und Archivierung ändern seine Identität
nicht. `CreatedAt` ist der unveränderliche UTC-Anlagezeitpunkt.

## 4. Group und Game-Zuordnung

```text
Group
- Id
- Name
- IsArchived
- Players

GroupPlayer
- GroupId
- PlayerId
- DefaultOrder

GroupGame
- GroupId
- GameId
```

Invarianten:

- mindestens ein Spieler;
- jeder Spieler höchstens einmal;
- `DefaultOrder` positiv, eindeutig und lückenlos `1..n`;
- Mitgliedermenge einer bestehenden Group bleibt fest;
- je Gruppe null bis beliebig viele explizite Spielzuordnungen;
- jede Gruppe-Spiel-Kombination höchstens einmal;
- neue Zuordnungen nur bei Mitgliederzahl innerhalb `Game.MinPlayers..Game.MaxPlayers`; Prüfung vor dem Speichern anhand der vollständigen Mitgliedschaft; ein historischer Import darf eine unzulässige aktuelle Zuordnung auslassen;
- Archivierung von `Group` nur, wenn in **keinem** Spiel eine `GameSession` dieser Gruppe `Running` oder `Scoring` ist;
- Entfernen von `GroupGame` nur, wenn für genau dieses Paar keine offene `GameSession` existiert.

`GroupPlayer` hat PK `(GroupId, PlayerId)`; `(GroupId, DefaultOrder)` ist eindeutig. `GroupGame` hat PK `(GroupId, GameId)`, verweist auf bestehende `Group` und `Game` und speichert ausschließlich die **aktuelle** Zuordnung zur Spielen-Auswahl. Eine historische `GameSession` erfordert **keine** gegenwärtige `GroupGame`-Zuordnung; Entfernen ändert ihre Referenzen nicht.

Bewusst doppelte Gruppen mit identischer Mitgliedermenge sind zulässig. Die Dublettenerkennung beim Anlegen ist daher kein Unique-Constraint für die Spielermenge.

Eine Archivierungs- oder Zuordnungsänderung wird beim Speichern gegen den aktuellen Sessionzustand geprüft. Historische `Completed`-Sessions sperren diese Aktionen nicht.

Löschen der Gruppe entfernt Mitgliedschaften und Zuordnungen, nicht die referenzierten Player oder Games. Historische Session-Restrict-Regeln bleiben vorrangig.

## 5. Game

```text
Game
- Id
- Key
- Name
- BadgeText
- MinPlayers
- MaxPlayers
```

`Key` ist sprachneutral, eindeutig, unveränderlich und maximal 100 Zeichen. Integrierte Spiele besitzen feste registrierte Keys; benutzerdefinierte Spiele einen lokal erzeugten Key im reservierten Format `manual:<GUID>`. Eine zusätzliche persistierte `ModuleKey`-Spalte existiert nicht. `Name` ist ein nichtleerer, getrimmter Anzeigename (`1..100` Zeichen), `BadgeText` eine separat festgelegte nichtleere kompakte Capsule-Bezeichnung. `1 <= MinPlayers <= MaxPlayers`; konkrete Grenzen und Änderbarkeit bestimmt das Spielmodul. Integrierte Spiele schreiben ihre registrierten festen Metadaten bei der Initialisierung in `Game`; manuelle Spiele die Nutzereingaben. Innerhalb manueller Spiele sind Namen (einschließlich verborgener) `OrdinalIgnoreCase` eindeutig; Namensgleichheit mit integrierten Spielen ist erlaubt. Sichtbarkeit bleibt eine Host-Preference und kein Game-Domainfeld.

## 6. GameSession

```text
GameSession
- Id
- GameId
- GroupId
- Status
- StartedAt
- EndedAt?
- ModifiedAt
- Players

GameSessionPlayer
- GameSessionId
- PlayerId
- Order?
- Placement?
```

Invarianten:

- mindestens ein Spieler, jeder höchstens einmal;
- `Order` ist für **alle** Partieteilnehmer `null` (nicht erfasst) oder vollständig positiv, eindeutig und lückenlos `1..n`; nur im zweiten Fall bezeichnet `Order = 1` den Startspieler;
- `Placement` ist nullable, gesetzt aber positiv;
- gleiche Placements sind erlaubt;
- `Placement = 1` bedeutet Gewinner.

Lebenszyklus:

```text
Running -> Scoring -> Completed
```

Andere Übergänge sind ungültig. `BeginScoring` setzt `EndedAt`; Zeitänderungen dürfen
nicht vor `StartedAt` bzw. `EndedAt` liegen.

Die Vorbereitung ist keine persistierte `GameSession`. Spielabhängige Ergebnisfelder
dürfen während `Scoring` nullable sein; das Spielmodul definiert Abschlussinvarianten.

Die gegebenenfalls erfasste Startreihenfolge bleibt über `GameSessionPlayer.Order?` historisch erhalten. Bei `Order = null` enthält die Session keine Startreihenfolge; die Anzeige kann alphabetisch erfolgen. Der globale `GroupPlayer.DefaultOrder` ändert sich nur bei tatsächlich erfasster Reihenfolge und verändert niemals ältere Sessions.

`GameSession` referenziert `Game` und `Group` mit Restrict. `GameSessionPlayer`
referenziert `Player` mit Restrict. Session-Kinder werden beim Löschen kaskadierend
entfernt, Stammdaten nicht.

## 7. Referenzen und Composition

Aggregate referenzieren andere Aggregate bevorzugt über IDs. Öffentliche Navigation
Properties existieren nur, wenn sie zum Aggregat gehören (`Group.Players`,
`GameSession.Players`).

Spielabhängige Ergänzungen verwenden denselben Schlüssel als PK/FK und duplizieren
keinen allgemeinen Fachzustand:

```text
Game               1 -> 0..1 <GameName>Game
GameSession        1 -> 0..1 <GameName>GameSession
GameSessionPlayer  1 -> 0..1 <GameName>GameSessionPlayer
```

Architekturdetails: [ARCHITECTURE.md](ARCHITECTURE.md).

## 8. PlayerColor

Gemeinsamer semantischer Enum:

```text
Blue=1 Yellow=2 Red=3 Black=4
Green=5 White=6 Orange=7 Brown=8
Purple=9 Pink=10 Turquoise=11 Gray=12
```

Die Integerwerte sind persistierte Identitäten und bleiben stabil.
`PlayerColor` enthält keine UI-/Kamerawerte und ist kein Feld auf `Player`.

Ein Spiel definiert nur seine erlaubte Teilmenge. Farben sind **optional** und werden nicht als `null`-Platzhalter erzwungen:

```text
GroupGamePlayerColor
- GroupId
- GameId
- PlayerId
- DefaultColor

GameSessionPlayerColor
- GameSessionId
- PlayerId
- Color
```

Beide Zuordnungen sind optionale Datensätze mit gültigem `PlayerColor`-Wert. Ihre Schlüssel sind `(GroupId, GameId, PlayerId)` bzw. `(GameSessionId, PlayerId)`; nur Gruppenmitglieder bzw. Sessionteilnehmer dürfen referenziert werden. Farben sind je Gruppe/Spiel und je Session eindeutig, sofern verwendet. Fehlt die Farbunterstützung, gibt es keine Einträge. Änderungen an Gruppendefaults verändern keine historischen Sessionfarben; Gruppen-/Spieldefaults werden erst beim erfolgreichen Partiestart aktualisiert.

## 9. Historische Identitäten und abgeleitete Werte

Historische Datensätze speichern stabile IDs statt lokalisierter Namenssnapshots.
Built-in-Content wird nicht nur für Anzeige/Lokalisierung in SQLite gespiegelt.

Nicht autoritativ persistieren, wenn zuverlässig ableitbar, z. B.:

- Partiedauer;
- Gewinner;
- Siege/Gewinnquote;
- durchschnittliche Platzierung;
- Startpositionseffekte und daraus abgeleitete Statistiken.

Spielspezifische abgeleitete Werte stehen im Spiel-Datenmodell.

## 10. Historische Korrekturen

Wenn ein Spiel historische Korrekturen unterstützt, bleibt die `GameSession`
durchgehend `Completed`; es gibt keine Rücktransition zu `Scoring`.

Allgemein stabil bleiben dabei:

- `GameSession.Id`;
- `GameId`;
- `GroupId`;
- Teilnehmermenge;
- `StartedAt`;
- `EndedAt`;
- `GameSessionPlayer.Order`.

Nur ausdrücklich korrekturfähige Ergebnisdaten werden atomar ersetzt. Welche das
sind und welche Validierung gilt, definiert das Spielmodul. Aktuelle Player-,
Group- und Contentnamen werden weiter über stabile Identitäten aufgelöst; die
Korrektur erzeugt keine Namenssnapshots.

## 11. Persistenzschutz

Allgemeine relationale Schutzregeln:

- eindeutige `Player.Name`, `Group.Name`, `Game.Key`; manuelle `Game.Name` sind innerhalb ihres Typs eindeutig;
- PK `(GroupId, PlayerId)` und Unique `(GroupId, DefaultOrder)`;
- PK `(GroupId, GameId)` für `GroupGame`;
- PK `(GameSessionId, PlayerId)` und Unique `(GameSessionId, Order)` für gesetzte `Order`-Werte; mehrere `null` sind erlaubt;
- positive Checks für `DefaultOrder`, gesetztes `Order` und gesetztes `Placement`;
- optionale Farb-Zuordnungs-PKs und Eindeutigkeit pro Gruppe/Spiel bzw. Session;
- Restrict auf langlebige Stammdaten;
- Cascade nur für echte Aggregate-/Composition-Kinder.

Provider-/DbContext-/Seedingarchitektur: [ARCHITECTURE.md](ARCHITECTURE.md).
Produktverhalten: [PRODUCT_SPEC.md](PRODUCT_SPEC.md).
Importreferenzen: [IMPORT_EXPORT.md](IMPORT_EXPORT.md).
