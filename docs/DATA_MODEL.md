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

## 4. Group

```text
Group
- Id
- Name
- IsTemporary
- IsArchived
- Players

GroupPlayer
- GroupId
- PlayerId
- DefaultOrder
```

Invarianten:

- mindestens ein Spieler;
- jeder Spieler höchstens einmal;
- `DefaultOrder` positiv, eindeutig und lückenlos `1..n`;
- Mitgliedermenge einer bestehenden Group bleibt fest;
- nur `IsTemporary: true -> false`.

`GroupPlayer` hat PK `(GroupId, PlayerId)`. `(GroupId, DefaultOrder)` ist eindeutig.
Löschen der Group entfernt Memberships kaskadierend, nicht die referenzierten Player.

## 5. Game

```text
Game
- Id
- Key
```

`Key` ist sprachneutral, eindeutig und maximal 100 Zeichen. Anzeigenamen sind keine
Identität und werden nicht in `Game` gespeichert.

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
- Order
- Placement?
```

Invarianten:

- mindestens ein Spieler, jeder höchstens einmal;
- `Order` positiv, eindeutig und lückenlos; `Order = 1` ist Startspieler;
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
GroupPlayer       1 -> 0..1 <GameName>GroupPlayer
GameSessionPlayer 1 -> 0..1 <GameName>GameSessionPlayer
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

Ein Spiel definiert nur seine erlaubte Teilmenge. Spielabhängige Gruppendefaults
werden im jeweiligen Spielmodell gespeichert.

## 9. Historische Identitäten und abgeleitete Werte

Historische Datensätze speichern stabile IDs statt lokalisierter Namenssnapshots.
Built-in-Content wird nicht nur für Anzeige/Lokalisierung in SQLite gespiegelt.

Nicht autoritativ persistieren, wenn zuverlässig ableitbar, z. B.:

- Partiedauer;
- Gewinner;
- Siege/Gewinnquote;
- durchschnittliche Platzierung.

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
- `EndedAt`.

Nur ausdrücklich korrekturfähige Ergebnisdaten werden atomar ersetzt. Welche das
sind und welche Validierung gilt, definiert das Spielmodul. Aktuelle Player-,
Group- und Contentnamen werden weiter über stabile Identitäten aufgelöst; die
Korrektur erzeugt keine Namenssnapshots.

## 11. Persistenzschutz

Allgemeine relationale Schutzregeln:

- eindeutige `Player.Name`, `Group.Name`, `Game.Key`;
- PK `(GroupId, PlayerId)` und Unique `(GroupId, DefaultOrder)`;
- PK `(GameSessionId, PlayerId)` und Unique `(GameSessionId, Order)`;
- positive Checks für `DefaultOrder`, `Order` und gesetztes `Placement`;
- Restrict auf langlebige Stammdaten;
- Cascade nur für echte Aggregate-/Composition-Kinder.

Provider-/DbContext-/Seedingarchitektur: [ARCHITECTURE.md](ARCHITECTURE.md).
Produktverhalten: [PRODUCT_SPEC.md](PRODUCT_SPEC.md).
Importreferenzen: [IMPORT_EXPORT.md](IMPORT_EXPORT.md).
