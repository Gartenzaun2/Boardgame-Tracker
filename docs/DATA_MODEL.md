# Boardgame Tracker – Fachliches Datenmodell v0.3

## 1. Grundprinzip

Allgemeine Daten bleiben spielunabhängig. Spielspezifische Ergebnisse,
Regeln und Einstellungen sind stark typisiert.

Der allgemeine Kern kennt Personen, Gruppen, Spiele, Partien und
Teilnehmer. Ein konkretes Spiel wie Arche Nova ergänzt ausschließlich
seine eigenen Daten.

Berechnete Werte werden nicht redundant gespeichert, wenn sie
zuverlässig aus Rohdaten abgeleitet werden können.

------------------------------------------------------------------------

## 2. Aggregate und Grenzen

### Person

`Person` ist ein eigenes Aggregate Root.

``` text
Person
- Id
- Name
- IsGuest
- IsActive
```

Regeln:

- Gast und reguläre Person verwenden denselben Datentyp.
- Eine Person kann später von Gast zu regulär wechseln, ohne ihre ID
  oder Historie zu verlieren.
- `IsActive` ist ein boolescher Zustand.
- Namen müssen nicht global eindeutig sein.

### Group

`Group` ist Aggregate Root für eine feste Gruppenzusammensetzung.

``` text
Group
- Id
- Name
- IsTemporary
- Members
```

`GroupPerson` ist Bestandteil dieses Aggregats:

``` text
GroupPerson
- GroupId
- PersonId
- DefaultOrder
```

Invarianten:

- eine Gruppe besitzt mindestens eine Person,
- dieselbe Person kommt höchstens einmal vor,
- `DefaultOrder` ist innerhalb einer Gruppe eindeutig,
- die Reihenfolge ist lückenlos `1..n`,
- eine andere Personenzusammensetzung repräsentiert fachlich eine andere
  Gruppe.

Eine bestehende Gruppe kann daher nicht beliebig um Mitglieder erweitert
oder verkleinert werden.

Bearbeitbar sind:

- Name,
- temporär/regulär,
- Default-Reihenfolge der bestehenden Mitglieder.

### Game

``` text
Game
- Id
- Key
```

`Key` ist stabil und sprachneutral, zum Beispiel:

``` text
ark-nova
```

Sichtbare Namen werden lokalisiert.

### GameSession

`GameSession` ist Aggregate Root für eine gestartete Partie.

``` text
GameSession
- Id
- GameId
- GroupId
- Status
- StartedAt
- EndedAt?
- ModifiedAt
- Players
```

`GameSessionPlayer` gehört zum Session-Aggregat:

``` text
GameSessionPlayer
- GameSessionId
- PersonId
- Order
- Placement?
```

Invarianten:

- eine gestartete Partie besitzt mindestens einen Teilnehmer,
- jeder Teilnehmer kommt höchstens einmal vor,
- die Reihenfolge ist eindeutig und lückenlos,
- `Order = 1` ist der Startspieler,
- `Placement` ist während der laufenden Partie nullable,
- mehrere Spieler dürfen bei echtem Gleichstand dieselbe Platzierung
  haben.

Statusübergänge:

``` text
Running -> Scoring -> Completed
```

Andere Übergänge sind nicht erlaubt.

`EndedAt` darf nicht vor `StartedAt` liegen.

Die Partievorbereitung wird im MVP nicht persistiert. Eine `GameSession`
entsteht erst beim Start der Partie.

------------------------------------------------------------------------

## 3. Cross-Aggregate-Referenzen

Aggregate referenzieren andere Aggregate bevorzugt über IDs.

Beispiele:

``` text
GroupPerson -> PersonId
GameSession -> GroupId
GameSession -> GameId
GameSessionPlayer -> PersonId
```

Öffentliche Navigation Properties werden nicht nur für
EF-Core-Bequemlichkeit angelegt.

Navigationen bleiben dort erhalten, wo sie Teil des Aggregats sind:

``` text
Group -> Members
GameSession -> Players
```

EF Core konfiguriert die übrigen Beziehungen über Foreign Keys.

------------------------------------------------------------------------

## 4. Gruppen- und Personenanlage

Der Use Case `CreateGroup` darf:

- vorhandene aktive Personen auswählen,
- neue Personen innerhalb des Workflows erfassen,
- eine neue Person als Gast markieren,
- die Default-Reihenfolge festlegen.

Neue Personen werden erst gemeinsam mit der Gruppe gespeichert.

Daraus folgt:

- Abbruch der Gruppenerstellung erzeugt keine verwaiste Person,
- vorhandene Personen können in mehreren Gruppen wiederverwendet werden,
- der Create-Group-Use-Case darf Personendaten verwenden, ohne selbst
  ein People-Feature zu werden.

------------------------------------------------------------------------

## 5. Gruppenbearbeitung

`EditGroup` verändert ausschließlich den bestehenden
Group-Aggregatzustand:

``` text
Name
IsTemporary
DefaultOrder
```

Die Mitgliedermenge bleibt unverändert.

Spielspezifische Gruppeneinstellungen werden separat modelliert und
können später über denselben UI-Bereich bearbeitet werden.

------------------------------------------------------------------------

## 6. Arche-Nova-Gruppendaten

### ArkNovaGroupPlayer

``` text
ArkNovaGroupPlayer
- GroupId
- PersonId
- DefaultColor
```

Die Standardfarbe ist abhängig von:

- Gruppe,
- Spiel,
- Person.

Die Beziehung `(GroupId, PersonId)` verweist auf eine bestehende
`GroupPerson`-Zuordnung.

Innerhalb einer Arche-Nova-Gruppe dürfen Defaultfarben nicht doppelt
vergeben werden.

Arche Nova verwendet im MVP:

``` text
Blue
Yellow
Red
Black
```

------------------------------------------------------------------------

## 7. Arche-Nova-Partiedaten

### ArkNovaSessionPlayer

``` text
ArkNovaSessionPlayer
- GameSessionId
- PersonId
- Color
- ZooPlanOption1Id
- ZooPlanOption2Id
- SelectedZooPlanId
- Attractiveness?
- Conservation?
- Reputation?
```

Die Beziehung `(GameSessionId, PersonId)` verweist auf einen bestehenden
`GameSessionPlayer`.

Regeln:

- die beiden angebotenen Zoopläne müssen verschieden sein,
- `SelectedZooPlanId` muss einer der beiden angebotenen Pläne sein,
- `SelectedZooPlanId` ist für eine persistierte laufende Partie
  verpflichtend,
- die Farbe ist innerhalb einer Partie eindeutig.

`Attractiveness`, `Conservation` und `Reputation` bleiben nullable,
solange die Wertung noch nicht vollständig erfasst ist.

### Session-weite Zooplanregel

Jeder Zooplan darf innerhalb einer Partie insgesamt nur einmal angeboten
werden.

Diese Regel ist sessionweit und wird von der
Arche-Nova-Vorbereitungs-/Startlogik geschützt, nicht von einem
einzelnen `ArkNovaSessionPlayer`.

------------------------------------------------------------------------

## 8. Arche-Nova-Zoopläne

``` text
ArkNovaZooPlan
- Id
- Number
- NameResourceKey
- IconKey
```

MVP-Pläne:

1.  Aussichtsplattform
2.  Freilaufflächen
3.  Silbersee
4.  Handelshafen
5.  Parkrestaurant
6.  Forschungsinstitut
7.  Eisdielen
8.  Hollywood Hills
9.  Geozoo
10. Auffangstation
11. Höhlen
12. Künstliche Intelligenz
13. Reißbrett
14. Lagune

Jeder Spieler erhält genau zwei verschiedene Pläne und wählt einen
davon.

------------------------------------------------------------------------

## 9. Arche-Nova-Spezialkarten

Geplant:

``` text
ArkNovaSpecialCard
- Id
- NameResourceKey
- Category
- IconKey

ArkNovaSessionSpecialCard
- GameSessionId
- PersonId
- SpecialCardId
```

Karten:

- Afrikanischer Elefant
- Asiatischer Elefant
- Weißkopfseeadler
- Steinadler
- Malaienbär

Statistikkategorien:

``` text
Elephant
Eagle
```

Der Malaienbär zählt für die gewünschte Statistik zur Kategorie `Eagle`.

Eine konkrete Spezialkarte kann pro Partie höchstens einem Spieler
zugeordnet werden.

Diese Tabellen werden erst eingeführt, wenn der Scoring-Slice
implementiert wird.

------------------------------------------------------------------------

## 10. Wertung und Platzierung

Dauerhaft gespeichert werden die Rohwerte:

``` text
Attractiveness
Conservation
Reputation
Placement
```

Punkte werden daraus abgeleitet.

Die Punkteanzeige kann später zwischen alter und neuer
Arche-Nova-Wertung umgeschaltet werden, ohne historische Rohdaten zu
verändern.

`Placement` wird unabhängig von den angezeigten Punkten gespeichert, da
Gleichstände manuell bestätigt oder aufgelöst werden können.

Echter Gleichstand verwendet Competition Ranking:

``` text
1, 1, 3
1, 2, 2, 4
```

Jeder Spieler mit `Placement = 1` gilt als Gewinner.

------------------------------------------------------------------------

## 11. Nicht redundant gespeicherte Daten

Nicht als autoritative Felder speichern:

- Partiedauer,
- Gewinner,
- Anzahl Siege,
- Gewinnquote,
- durchschnittliche Platzierung,
- alte Punkte,
- neue Punkte,
- Elefantenanzahl,
- Adleranzahl.

Diese Werte werden aus den gespeicherten Rohdaten berechnet.

------------------------------------------------------------------------

## 12. Persistenzregeln

Datenbank-Constraints ergänzen die Domain-Invarianten.

Beispiele:

- Composite Key für `GroupPerson(GroupId, PersonId)`,
- Unique Index für `(GroupId, DefaultOrder)`,
- Composite Key für `GameSessionPlayer(GameSessionId, PersonId)`,
- Unique Index für Arche-Nova-Farben je Gruppe bzw. Partie,
- Check Constraint für verschiedene Zooplanoptionen,
- Check Constraint, dass der gewählte Zooplan angeboten wurde.

Die Datenbank ist eine zusätzliche Schutzlinie. Die fachlichen Regeln
bleiben im Core bzw. in sessionweiten Application-/Domainregeln.

------------------------------------------------------------------------

## 13. Erweiterbarkeit

Weitere Spiele erhalten eigene stark typisierte Modelle:

``` text
Games/
  ArkNova/
  TerraformingMars/
  ...
```

Der allgemeine Kern bleibt frei von Arche-Nova-Sonderfällen.
