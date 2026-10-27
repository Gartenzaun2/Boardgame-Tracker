# Boardgame Tracker – Fachliches Datenmodell v0.3

## 1. Grundprinzip

Allgemeine Daten bleiben spielunabhängig. Spielspezifische Ergebnisse,
Regeln und Einstellungen sind stark typisiert.

Der allgemeine Kern kennt Spieler, Gruppen, Spiele und Partien. Ein konkretes Spiel wie Arche Nova ergänzt ausschließlich
seine eigenen Daten.

Berechnete Werte werden nicht redundant gespeichert, wenn sie
zuverlässig aus Rohdaten abgeleitet werden können.

------------------------------------------------------------------------

## 2. Aggregate und Grenzen

### Player

`Player` ist ein eigenes Aggregate Root.

``` text
Player
- Id
- Name
- IsGuest
- IsActive
```

Regeln:

- Gast und reguläre Spieler verwenden denselben Datentyp.
- Ein Spieler kann später von Gast zu regulär wechseln, ohne seine ID
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
- IsArchived
- Players
```

`GroupPlayer` ist Bestandteil dieses Aggregats:

``` text
GroupPlayer
- GroupId
- PlayerId
- DefaultOrder
```

Invarianten:

- eine Gruppe besitzt mindestens einen Spieler,
- derselbe Spieler kommt höchstens einmal vor,
- `DefaultOrder` ist innerhalb einer Gruppe eindeutig,
- die Reihenfolge ist lückenlos `1..n`,
- eine andere Spielerzusammensetzung repräsentiert fachlich eine andere
  Gruppe.

Eine bestehende Gruppe kann daher nicht beliebig um Spieler erweitert
oder verkleinert werden.

Bearbeitbar sind:

- Name,
- Default-Reihenfolge der bestehenden Spieler,
- Archivstatus,
- bei einer temporären Gruppe die einmalige Transition zu permanent.

Eine permanente Gruppe kann nicht nachträglich temporär gemacht werden.

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

`GameSessionPlayer` gehört zum `GameSession`-Aggregat:

``` text
GameSessionPlayer
- GameSessionId
- PlayerId
- Order
- Placement?
```

Invarianten:

- eine gestartete Partie besitzt mindestens einen Spieler,
- jeder Spieler kommt höchstens einmal vor,
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
GroupPlayer -> PlayerId
GameSession -> GroupId
GameSession -> GameId
GameSessionPlayer -> PlayerId
```

Öffentliche Navigation Properties werden nicht nur für
EF-Core-Bequemlichkeit angelegt.

Navigationen bleiben dort erhalten, wo sie Teil des Aggregats sind:

``` text
Group -> Players
GameSession -> Players
```

EF Core konfiguriert die übrigen Beziehungen über Foreign Keys.

------------------------------------------------------------------------

## 4. Gruppen- und Spieleranlage

Der Use Case `CreateGroup` darf:

- vorhandene aktive Spieler auswählen,
- neue Spieler innerhalb des Workflows erfassen,
- einen neuen Spieler als Gast markieren,
- die Default-Reihenfolge festlegen.

Neue Spieler werden erst gemeinsam mit der Gruppe gespeichert.

Daraus folgt:

- Abbruch der Gruppenerstellung erzeugt keinen verwaisten Spieler,
- vorhandene Spieler können in mehreren Gruppen wiederverwendet werden,
- der Create-Group-Use-Case darf Spielerdaten verwenden, ohne selbst
  ein Players-Feature zu werden.

------------------------------------------------------------------------

## 5. Gruppenbearbeitung

`EditGroup` verändert ausschließlich den bestehenden
Group-Aggregatzustand:

``` text
Name
IsArchived
DefaultOrder
optional: IsTemporary true -> false
```

Die Spielermenge bleibt unverändert. `IsTemporary` ist kein frei
editierbarer Schalter: erlaubt ist nur `temporary -> permanent`.

Spielspezifische Gruppeneinstellungen werden separat modelliert. Die
Arche-Nova-Defaultfarben werden bereits über denselben UI-Bereich gepflegt.

------------------------------------------------------------------------

## 6. Arche-Nova-Gruppendaten

### ArkNovaGroupPlayer

``` text
ArkNovaGroupPlayer
- GroupId
- PlayerId
- DefaultColor
```

Die Standardfarbe ist abhängig von:

- Gruppe,
- Spiel,
- Spieler.

Die Beziehung `(GroupId, PlayerId)` verweist auf eine bestehende
`GroupPlayer`-Zuordnung.

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

### ArkNovaGameSessionPlayer

``` text
ArkNovaGameSessionPlayer
- GameSessionId
- PlayerId
- Color
- ZooPlanOption1Number
- ZooPlanOption2Number
- SelectedZooPlanNumber
- Attractiveness?
- Conservation?
- Reputation?
```

Die Beziehung `(GameSessionId, PlayerId)` verweist auf einen bestehenden
`GameSessionPlayer`.

Regeln:

- die beiden angebotenen Zoopläne müssen verschieden sein,
- `SelectedZooPlanNumber` muss einer der beiden angebotenen Pläne sein,
- `SelectedZooPlanNumber` ist für eine persistierte laufende Partie
  verpflichtend,
- die Farbe ist innerhalb einer Partie eindeutig.

`Attractiveness`, `Conservation` und `Reputation` bleiben nullable,
solange die Wertung noch nicht vollständig erfasst ist.

### GameSession-weite Zooplanregel

Jeder Zooplan darf innerhalb einer Partie insgesamt nur einmal angeboten
werden.

Diese Regel gilt für die gesamte GameSession und wird von der
Arche-Nova-Vorbereitungs-/Startlogik geschützt, nicht von einem
einzelnen `ArkNovaGameSessionPlayer`.

------------------------------------------------------------------------

## 8. Arche-Nova-Zoopläne

``` text
ArkNovaZooPlan
- Number
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

`Number` ist die natürliche, stabile Identität und der Primärschlüssel.
Eine künstliche ZooPlan-ID wird nicht verwendet.

Jeder Spieler erhält genau zwei verschiedene Pläne und wählt einen
davon.

------------------------------------------------------------------------

## 9. Arche-Nova-Spezialkarten

Die fünf konkreten, fachlich festen Karten werden als `ArkNovaSpecialCard`
identifiziert. Dafür ist keine zusätzliche Referenztabelle mit künstlicher ID
notwendig.

``` text
ArkNovaGameSessionSpecialCardAssignment
- GameSessionId
- PlayerId
- SpecialCard

PK: (GameSessionId, PlayerId, SpecialCard)
```

Karten:

- Afrikanischer Elefant
- Asiatischer Elefant
- Weißkopfseeadler
- Steinadler
- Malaienbär

Der Malaienbär zählt für die gewünschte Statistik zur Adler-Kategorie.

Dieselbe konkrete Karte darf in seltenen Fällen mehreren Spielern derselben
Partie zugeordnet sein. Der Composite Key verhindert lediglich eine doppelte
Zuordnung derselben Karte an denselben Spieler.

------------------------------------------------------------------------

## 10. Wertung und Platzierung

Während `GameSession.Status = Scoring` dürfen Wertungszwischenstände bereits
automatisch persistiert werden. Fachlich abgeschlossen ist die Partie erst mit
`GameSession.Status = Completed`; nur abgeschlossene Partien gehören in
Historie und Statistik. Ein zusätzlicher „Spieler fertig“-Status existiert
nicht.

Dauerhaft gespeichert werden die Rohwerte:

``` text
Attractiveness?   0..113
Conservation?     0..41
Reputation?       null oder 1..15
Placement?
```

Zum Abschluss müssen Attraktivität und Artenschutz für jeden Spieler gesetzt
sein. Ruf und Spezialkarten bleiben optional.

Die neue Punkteanzeige wird ausschließlich aus den Rohwerten berechnet:

``` text
NewScore = Attractiveness + ConservationAddend[Conservation]
OldScore = NewScore - 100
```

Berechnete Punkte werden nicht redundant gespeichert.

`Placement` wird nach der gegebenenfalls nötigen Gleichstandsauflösung
persistiert, weil die tatsächliche Platzierung bei Punktgleichheit nicht allein
aus den Punkten rekonstruierbar ist. Competition Ranking wird verwendet, z. B.:

``` text
1, 1, 3
1, 2, 2, 4
```

Nur Spieler desselben ursprünglichen Punktegleichstands dürfen innerhalb des
zugehörigen Rangbereichs verschoben werden. Jeder Spieler mit `Placement = 1`
gilt als Gewinner.

Die globalen Arche-Nova-Präferenzen `Ruf erfassen` und `alte/neue
Punkteanzeige` sind keine historischen Spieldaten und werden deshalb über den
nativen MAUI-Preferences-Speicher gespeichert, nicht in SQLite.

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

- Composite Key für `GroupPlayer(GroupId, PlayerId)`,
- Unique Index für `(GroupId, DefaultOrder)`,
- Composite Key für `GameSessionPlayer(GameSessionId, PlayerId)`,
- Unique Index für Arche-Nova-Farben je Gruppe bzw. Partie,
- Check Constraint für verschiedene Zooplanoptionen,
- Check Constraint, dass der gewählte Zooplan angeboten wurde,
- Check Constraints für Attraktivität, Artenschutz und Ruf,
- Composite Key für Spezialkarten `(GameSessionId, PlayerId, SpecialCard)`.

Die Datenbank ist eine zusätzliche Schutzlinie. Die fachlichen Regeln
bleiben im Core bzw. in GameSession-weiten Application-/Domainregeln.

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

## Spielidentität und Zoopläne

- `Game.Id` bleibt eine stabile `Guid`. Spiele sollen später auch vom Benutzer als manuelle Spiele angelegt werden können; Name und Konfiguration sind deshalb keine geeignete Identität.
- Ein eingebaut unterstütztes Spiel kann zusätzlich über einen stabilen Implementierungsschlüssel wie `ark-nova` erkannt werden. Dieser Schlüssel ersetzt die `Game.Id` nicht.
- Für manuelle Spiele ist perspektivisch konfigurierbar, ob nur die Tatsache einer Partie, Gewinner, Punkte und/oder Platzierungen erfasst werden. Das konkrete Konfigurationsmodell wird erst mit diesem Feature festgelegt.
- `ArkNovaZooPlan.Number` (1–14) ist dagegen die stabile fachliche Identität und der Primärschlüssel. Eine zusätzliche künstliche `Id` entfällt.
- Die `ArkNovaZooPlans`-Tabelle enthält nur die gültigen Nummern und dient der referenziellen Integrität.
- Lokalisierter Zooplanname und Sketch-Datei sind Presentation-Metadaten und werden nicht persistiert.

## Historische Korrekturen

Completed-Partien sind über einen expliziten Korrektur-Anwendungsfall bearbeitbar; keine Transition zurück zu Scoring. Rohwerte, besondere Karten und neu validierte Placements werden atomar gespeichert. Spiel-, Gruppen-, Teilnehmer- und Setupdaten sowie Start-/Endzeit bleiben unverändert. Namen werden über aktuelle Player-/Group-Referenzen aufgelöst. Löschen einer Completed-Partie entfernt alle abhängigen Partiedaten kaskadierend, niemals Spieler oder Gruppen. Kein neues Schema und keine Namens-Snapshots.
