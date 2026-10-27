# Boardgame Tracker – Fachliches Datenmodell

## 1. Grundprinzip

Allgemeine Daten bleiben spielunabhängig. Spielspezifische Ergebnisse, Regeln
und Einstellungen sind stark typisiert.

Der allgemeine Kern kennt:

- Personen
- Gruppen
- Spiele
- Partien
- Teilnehmer

Ein Spielmodul wie Arche Nova ergänzt nur seine eigenen Daten.

Berechnete Werte werden nicht redundant gespeichert, wenn sie zuverlässig aus
Rohdaten abgeleitet werden können.

## 2. Person

```text
Person
- Id : Guid
- Name : string
- IsGuest : bool
- IsActive : bool
```

Regeln:

- Name ist nicht leer.
- Namen müssen nicht global eindeutig sein.
- Gast und reguläre Person verwenden denselben Typ.
- Gaststatus kann später geändert werden, ohne Historie zu verlieren.
- `IsActive` ist ein echter boolescher Zustand.

## 3. Group

```text
Group
- Id : Guid
- Name : string
- IsTemporary : bool
- IsArchived : bool
- Members : GroupPerson[]
```

```text
GroupPerson
- GroupId : Guid
- PersonId : Guid
- DefaultOrder : int
```

Primärschlüssel:

```text
(GroupId, PersonId)
```

Invarianten:

- mindestens ein Mitglied,
- Person höchstens einmal,
- `DefaultOrder` eindeutig,
- Reihenfolge lückenlos `1..n`,
- feste Personenzusammensetzung.

Lifecycle:

- `temporary -> permanent` erlaubt,
- `permanent -> temporary` nicht erlaubt,
- Archivierung ist reversibel.

Eine andere Personenzusammensetzung repräsentiert eine andere Gruppe.

## 4. Game

```text
Game
- Id : Guid
- Key : string
```

`Key` ist stabil und sprachneutral.

Beispiel:

```text
ark-nova
```

`Game.Id` bleibt eine `Guid`, weil Spiele später auch benutzerdefiniert angelegt
und umbenannt werden können.

## 5. GameSession

```text
GameSession
- Id : Guid
- GameId : Guid
- GroupId : Guid
- Status : GameSessionStatus
- StartedAt : DateTimeOffset
- EndedAt : DateTimeOffset?
- ModifiedAt : DateTimeOffset
- Players : GameSessionPlayer[]
```

```text
GameSessionPlayer
- GameSessionId : Guid
- PersonId : Guid
- Order : int
- Placement : int?
```

Sessionstatus:

```text
Running -> Scoring -> Completed
```

Invarianten:

- mindestens ein Teilnehmer,
- Teilnehmer höchstens einmal,
- `Order` eindeutig und lückenlos,
- `Order = 1` ist Startspieler,
- `Placement` während laufender Partie nullable,
- mehrere Spieler dürfen bei Gleichstand dieselbe Platzierung haben,
- `EndedAt` darf nicht vor `StartedAt` liegen.

Die Partievorbereitung wird nicht persistiert. Eine Session entsteht erst beim
Start.

### Offene-Session-Regel

Fachliche Zielregel:

```text
maximal eine offene Session je (GameId, GroupId)
```

Offen bedeutet `Running` oder `Scoring`.

Mehrere offene Sessions sind erlaubt, wenn Spiel und/oder Gruppe
unterschiedlich sind.

Hinweis: Der aktuelle Arche-Nova-Startpfad prüft im Code noch global auf
irgendeine offene Session. Das ist ein bekannter Implementierungsunterschied.

## 6. Arche Nova – Gruppendaten

```text
ArkNovaGroupPlayer
- GroupId : Guid
- PersonId : Guid
- DefaultColor : ArkNovaPlayerColor
```

Schlüssel:

```text
(GroupId, PersonId)
```

Die Beziehung verweist auf eine bestehende `GroupPerson`-Mitgliedschaft.

Farben:

```text
Blue
Yellow
Red
Black
```

Defaultfarben sind innerhalb einer Gruppe eindeutig.

## 7. Arche Nova – Session-Snapshot

```text
ArkNovaSessionPlayer
- GameSessionId : Guid
- PersonId : Guid
- Color : ArkNovaPlayerColor
- ZooPlanOption1Number : int
- ZooPlanOption2Number : int
- SelectedZooPlanNumber : int
- Attractiveness : int?
- Conservation : int?
- Reputation : int?
```

Schlüssel:

```text
(GameSessionId, PersonId)
```

Regeln:

- beide angebotenen Zoopläne verschieden,
- gewählter Zooplan ist einer der beiden angebotenen,
- gewählter Zooplan ist in einer persistierten Session verpflichtend,
- Spielerfarbe innerhalb einer Partie eindeutig,
- Attraktivität `null | 0..113`,
- Artenschutz `null | 0..41`,
- Ruf `null | 1..15`.

Sessionweite Regel:

> Jeder Zooplan darf innerhalb derselben Partie insgesamt nur einmal angeboten
> werden.

## 8. Arche-Nova-Zoopläne

```text
ArkNovaZooPlan
- Number : int
```

`Number` ist der natürliche Primärschlüssel.

Es existiert **keine zusätzliche künstliche `Id`**.

Gültige Werte:

| Nummer | Name |
|---:|---|
| 1 | Aussichtsplattform |
| 2 | Freilaufflächen |
| 3 | Silbersee |
| 4 | Handelshafen |
| 5 | Parkrestaurant |
| 6 | Forschungsinstitut |
| 7 | Eisdielen |
| 8 | Hollywood Hills |
| 9 | Geozoo |
| 10 | Auffangstation |
| 11 | Höhlen |
| 12 | Künstliche Intelligenz |
| 13 | Reißbrett |
| 14 | Lagune |

Lokalisierter Name und Sketch-Ressource sind Presentation-Metadaten und werden
nicht als autoritative Daten in Core/SQLite gespeichert.

## 9. Besondere Karten

Domain-Enum:

```text
ArkNovaSpecialCard
- AfricanElephant
- AsianElephant
- BaldEagle
- GoldenEagle
- SunBear
```

Persistenz:

```text
ArkNovaSessionSpecialCardAssignment
- GameSessionId : Guid
- PersonId : Guid
- SpecialCard : ArkNovaSpecialCard

PK: (GameSessionId, PersonId, SpecialCard)
```

Dadurch:

- dieselbe Karte kann demselben Spieler nicht doppelt zugeordnet werden,
- dieselbe Karte darf mehreren Spielern derselben Session zugeordnet werden.

Sichtbare deutsche Namen:

- Afrikanischer Elefant
- Asiatischer Elefant
- Weißkopfseeadler
- Steinadler
- Malaienbär

Für spätere Statistik zählt der Malaienbär zur Adler-Kategorie.

## 10. Arche-Nova-Wertung

Persistierte Rohwerte:

```text
Attractiveness : null | 0..113
Conservation   : null | 0..41
Reputation     : null | 1..15
Placement      : null | positive int
```

Attraktivität und Artenschutz müssen beim Abschluss für jeden Spieler vorhanden
sein.

Ruf und besondere Karten sind optional.

### Artenschutz-Lookup

| Artenschutz | Addend |
|---:|---:|
| 0 | -14 |
| 1 | -12 |
| 2 | -10 |
| 3 | -8 |
| 4 | -6 |
| 5 | -4 |
| 6 | -2 |
| 7 | 0 |
| 8 | 2 |
| 9 | 4 |
| 10 | 6 |
| 11 | 9 |
| 12 | 12 |
| 13 | 15 |
| 14 | 18 |
| 15 | 21 |
| 16 | 24 |
| 17 | 27 |
| 18 | 30 |
| 19 | 33 |
| 20 | 36 |
| 21 | 39 |
| 22 | 42 |
| 23 | 45 |
| 24 | 48 |
| 25 | 51 |
| 26 | 54 |
| 27 | 57 |
| 28 | 60 |
| 29 | 63 |
| 30 | 66 |
| 31 | 69 |
| 32 | 72 |
| 33 | 75 |
| 34 | 78 |
| 35 | 81 |
| 36 | 84 |
| 37 | 87 |
| 38 | 90 |
| 39 | 93 |
| 40 | 96 |
| 41 | 99 |

Berechnung:

```text
NewScore = Attractiveness + ConservationAddend[Conservation]
OldScore = NewScore - 100
```

Berechnete Punkte werden nicht gespeichert.

Kontrollwerte:

```text
0  -> -14
16 -> 24
18 -> 30
24 -> 48
41 -> 99
```

## 11. Platzierung

Default-Platzierung wird aus den berechneten Punkten mit Competition Ranking
ermittelt.

Beispiele:

```text
1,1,3
1,2,2,4
```

Bei manueller Gleichstandsauflösung:

- nur Spieler desselben ursprünglichen Punktegleichstands sind beweglich,
- nur innerhalb des ursprünglichen Rangbereichs,
- Nicht-Gleichstände bleiben fest,
- echter Gleichstand darf bestehen bleiben.

`Placement` wird gespeichert, weil eine manuell aufgelöste Platzierung nicht aus
den Punkten allein rekonstruierbar ist.

## 12. Nicht redundant gespeicherte Werte

Nicht als autoritative Felder speichern:

- Partiedauer
- Gewinner
- Anzahl Siege
- Gewinnquote
- durchschnittliche Platzierung
- neue Punkte
- alte Punkte
- Anzahl Elefanten
- Anzahl Adler

Diese Werte werden aus Rohdaten abgeleitet.

## 13. Lokale Preferences statt Spieldaten

Nicht in SQLite:

### Arche Nova

```text
ScoringDisplayMode
CaptureReputation
```

### Navigation

```text
DefaultGameKey
SelectedGameKey
LastMainSection
LastSessionId
```

Diese Werte sind lokaler UI-/Präsentationskontext, keine historische
Spieldaten.

## 14. Persistenzschutz

Datenbank-Constraints ergänzen Domain-/Application-Regeln.

Wichtige Schutzlinien:

- Composite Key `GroupPerson(GroupId, PersonId)`
- eindeutige Gruppenreihenfolge
- Composite Key `GameSessionPlayer(GameSessionId, PersonId)`
- eindeutige Arche-Nova-Farben je Gruppe bzw. Session
- verschiedene Zooplanoptionen
- SelectedZooPlan muss angeboten sein
- Wertebereiche für Attraktivität, Artenschutz, Ruf
- Composite Key für besondere Karten

Die Datenbank ist zusätzliche Schutzlinie; fachliche Wahrheit bleibt in
Core/Application.

## 15. Erweiterbarkeit

Weitere Spiele erhalten eigene Module:

```text
Games/
  ArkNova/
  TerraformingMars/
  ...
```

Der allgemeine Kern bleibt frei von Arche-Nova-Sonderfällen.
