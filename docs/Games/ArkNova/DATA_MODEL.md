# Arche Nova – Datenmodell

## 1. Geltungsbereich

Diese Datei ergänzt das
[allgemeine Datenmodell](../../DATA_MODEL.md) ausschließlich um
Arche-Nova-spezifische Entities, Referenzdaten, Werte und Invarianten.

Allgemeine Regeln für `Player`, `Group`, `Game`, `GameSession`,
`GameSessionPlayer`, IDs, Namen und gemeinsame `PlayerColor`-Werte werden hier
nicht wiederholt.

Architektur des Moduls:
[ARCHITECTURE.md](ARCHITECTURE.md).

## 2. Composition auf allgemeinen Daten

Arche Nova ergänzt zwei allgemeine Beziehungen:

```text
GroupPlayer
    1
    |
    0..1
    |
ArkNovaGroupPlayer
```

```text
GameSessionPlayer
    1
    |
    0..1
    |
ArkNovaGameSessionPlayer
```

Die spielabhängigen Datensätze wiederholen nur den jeweiligen gemeinsamen
Schlüssel als PK/FK. Allgemeiner fachlicher Zustand wie Reihenfolge oder
Platzierung bleibt ausschließlich im allgemeinen Modell.

## 3. Gruppendaten

```text
ArkNovaGroupPlayer
- GroupId
- PlayerId
- DefaultColor
```

`(GroupId, PlayerId)` ist gleichzeitig PK und FK auf `GroupPlayer`.

`DefaultColor` ist nicht nullable. Ohne gespeicherten Default existiert für das
Mitglied kein `ArkNovaGroupPlayer`-Datensatz.

Arche Nova erlaubt aus dem gemeinsamen `PlayerColor`-Katalog:

```text
Blue   = 1
Yellow = 2
Red    = 3
Black  = 4
```

Innerhalb einer Group darf jede gesetzte Defaultfarbe nur einmal vorkommen.
Die Datenbank schützt dies über einen eindeutigen Index auf
`(GroupId, DefaultColor)`.

Es gibt keine globale Arche-Nova-Farbe auf `Player`.

## 4. Partiedaten

```text
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

`(GameSessionId, PlayerId)` ist gleichzeitig PK und FK auf
`GameSessionPlayer`.

Regeln:

- `Color` ist innerhalb der GameSession eindeutig;
- nur Blue, Yellow, Red oder Black sind gültig;
- die beiden angebotenen Zoopläne müssen verschieden sein;
- `SelectedZooPlanNumber` muss einer der beiden angebotenen Pläne sein;
- jeder persistierte Ark-Nova-Partieteilnehmer besitzt einen ausgewählten Plan;
- jeder Zooplan darf innerhalb der gesamten GameSession insgesamt nur einmal
  angeboten werden;
- die GameSession-weite Zooplanregel wird durch Start-/Applicationlogik geschützt,
  nicht durch einen einzelnen `ArkNovaGameSessionPlayer`.

Wertungsfelder bleiben nullable, solange die Wertung nicht vollständig ist.

## 5. Zoopläne

```text
ArkNovaZooPlan
- Number
```

`Number` ist die natürliche, stabile Identität und der Primärschlüssel.
Eine künstliche ZooPlan-GUID wird nicht eingeführt.

Der aktuelle feste Katalog umfasst die gedruckten Nummern `1..14`.
Der persistierte Referenzdatensatz enthält nur die Nummer.

Sprachabhängige Namen sind **kein Datenmodell** und werden nicht in SQLite
gespiegelt. Ihre kanonische Built-in-Quelle ist:

```text
Application/Games/ArkNova/Content/content.json
```

Die Namen werden über `Number` adressiert. Sprachneutrale Bilder/Skizzen bleiben
App-Ressourcen.

Jeder Partieteilnehmer erhält genau zwei verschiedene Pläne und wählt einen davon.

## 6. Spezialkarten

`ArkNovaSpecialCard` ist eine persistierte Entity mit stabiler GUID, kein Enum:

```text
ArkNovaSpecialCard
- Id
- Symbol1?
- Symbol2?
- Symbol3?
- IsArchived
```

Regeln:

- `Id` darf nicht leer sein;
- null bis drei Symbole sind erlaubt;
- Symbole sind geordnet;
- es gibt keine Lücken (`Symbol2` setzt `Symbol1` voraus,
  `Symbol3` setzt `Symbol2` voraus);
- Symbolwiederholungen sind erlaubt;
- `IsArchived` ist fachlicher Zustand;
- der sichtbare Kartenname ist Game Content und kein Kartenfeld.

`ArkNovaCardSymbol` besitzt stabile persistierte Integerwerte:

```text
1  Africa
2  America
3  Asia
4  Australia
5  Europe
6  Bear
7  Bird
8  Herbivore
9  PettingAnimal
10 Predator
11 Primate
12 Reptile
13 SeaAnimal
14 Science
```

Die Datenbank schützt gültige Symbolwerte und die lückenlose Symbolreihenfolge
zusätzlich per Check Constraint.

### Assignments

```text
ArkNovaGameSessionSpecialCardAssignment
- GameSessionId
- PlayerId
- SpecialCardId

PK: (GameSessionId, PlayerId, SpecialCardId)
```

Der Teilnehmer-FK verweist auf `GameSessionPlayer` und wird bei Löschung der
GameSession kaskadierend entfernt.

Der Karten-FK verwendet Restrict. Das Entfernen einer Partie löscht daher niemals
globale Spezialkarten.

Dieselbe Karte darf verschiedenen Spielern zugeordnet sein. Derselbe Spieler darf
dieselbe Karte innerhalb derselben GameSession nicht doppelt besitzen.

Aktive Karten dürfen neu zugeordnet werden. Archivierte Karten bleiben in alten
Partien referenzierbar und können dort entfernt, aber nicht neu hinzugefügt werden.

## 7. Mitgelieferte Spezialkarten

Die mitgelieferten Spezialkarten implementieren die allgemeine
Referenzdaten-Seeding-Regel: Fehlende Karten werden idempotent ergänzt; vorhandene
Karten werden nicht auf Defaultsymbole oder Archivstatus zurückgesetzt.

Die stabilen Referenz-IDs und Defaultsymbole sind:

| Seed | GUID | Defaultsymbole |
|---|---|---|
| AfricanElephant | `a11c0001-0000-4000-8000-000000000001` | Africa, Africa, Herbivore |
| AsianElephant | `a11c0001-0000-4000-8000-000000000002` | Asia, Asia, Herbivore |
| BaldEagle | `a11c0001-0000-4000-8000-000000000003` | America, Bird |
| GoldenEagle | `a11c0001-0000-4000-8000-000000000004` | Europe, Bird |
| SunBear | `a11c0001-0000-4000-8000-000000000005` | Asia, Bear, Predator |

Die mitgelieferten Anzeigenamen liegen ausschließlich in `content.json` und sind
keine Identität. Aktuell enthält der deutsche Built-in-Content dafür
Afrikanischer Elefant, Asiatischer Elefant, Weißkopfseeadler, Steinadler und
Malaienbär.

Es gibt kein persistiertes `Name`, `NameKey`, `CustomName` oder separates
Localization-Key-Feld auf `ArkNovaSpecialCard`.

## 8. Nutzerinhalt

Veränderliche Nutzernamen liegen in:

```text
ArkNovaUserContent
- ResourceType
- ObjectId
- Name

PK: (ResourceType, ObjectId)
```

Erlaubte Ressourcentypen:

```text
special-card
zoo-plan
```

Identitäten:

- `special-card`: kanonische nichtleere GUID im `D`-Format;
- `zoo-plan`: kanonische Nummer `1..14` als Dezimaltext.

`Name` wird getrimmt und besitzt 1..120 Zeichen.

„Nutzer“ ist eine logische Contentquelle ohne Sprachfeld. Der Ressourcentyp
erzeugt keine neue Domainidentität.

Neue Spezialkarte und zugehöriger Nutzername werden gemeinsam gespeichert.
Eine Namensänderung verändert ausschließlich `ArkNovaUserContent`.
Symboländerungen verändern ausschließlich `ArkNovaSpecialCard`.

Das Entfernen einer Nutzerressource würde nur auf die normale Content-Fallbackkette
zurückfallen und niemals Symbole oder Archivstatus ändern.

Built-in-Content wird nicht in SQLite gespiegelt.

## 9. Content-Identitäten

Arche Nova implementiert die allgemeinen Content-/Materialsprache-Regeln mit
diesen stabilen Identitäten:

- Spezialkarten: GUID,
- Zoopläne: Nummer.

Historische Datensätze speichern ausschließlich diese stabilen Identitäten.
Lokalisierte Anzeigenamen werden aus aktuellem Built-in- bzw. Nutzercontent
aufgelöst.

Die konkrete Built-in-Quelle ist
`Application/Games/ArkNova/Content/content.json`.
Veränderliche Nutzernamen liegen in `ArkNovaUserContent`.

Die Materialsprache folgt vollständig den allgemeinen Regeln aus
[`docs/DATA_MODEL.md`](../../DATA_MODEL.md) und ist kein Ark-Nova-Historienfeld.

## 10. Wertung

Arche Nova nutzt die allgemeine Möglichkeit, während `Scoring` unvollständige
Ergebnisdaten zu persistieren.

Persistierte Rohwerte:

```text
Attractiveness?   0..113
Conservation?     0..41
Reputation?       null oder 1..15
```

Zum Abschluss müssen Attraktivität und Artenschutz für jeden Spieler gesetzt sein.
Ruf und Spezialkarten bleiben optional.

Berechnete Punkte werden nicht persistiert:

```text
NewScore = Attractiveness + ConservationAddend[Conservation]
OldScore = NewScore - 100
```

`ConservationAddend` ist die feste fachliche Lookup-Tabelle aus
`ArkNovaScoringRules`.

## 11. Platzierung und Gleichstand

`Placement` bleibt Bestandteil des allgemeinen `GameSessionPlayer`, weil die
tatsächliche Platzierung nicht immer vollständig aus dem Score rekonstruierbar ist.

Arche Nova verwendet für automatische Ausgangsplatzierungen Competition Ranking.

Bei Punktgleichstand darf ein Spieler nur innerhalb des ursprünglichen
Gleichstandsbereichs verschoben werden. Geteilte Plätze sind erlaubt; auch
Zuordnungen wie `1,3,3,4` sind zulässig.

Gleichstandsblöcke werden zur Laufzeit aus Scores und Placements abgeleitet und
nicht separat persistiert.

Spieler mit unterschiedlichen Scores dürfen nicht gegeneinander verschoben werden.
Die allgemeine Bedeutung von `Placement = 1` als Gewinner gilt unverändert.

## 12. Historische Korrektur

Arche Nova folgt vollständig dem allgemeinen Completed-Korrekturprinzip aus
[`docs/DATA_MODEL.md`](../../DATA_MODEL.md).

Atomar korrekturfähig sind ausschließlich:

- Attraktivität,
- Artenschutz,
- Ruf,
- Spezialkarten-Zuordnungen,
- neu validierte Placements.

Arche-Nova-Setupdaten wie Farbe und Zooplan-Auswahl bleiben unverändert.

## 13. Nicht-historische Arche-Nova-Preferences

Arche Nova besitzt aktuell unter anderem:

- Ruf erfassen,
- alte/neue Punkteanzeige.

Diese Preferences folgen der allgemeinen Regel für nicht-historischen
spielabhängigen Hostzustand und liegen nicht in SQLite-Historientabellen.

## 14. Nicht redundant gespeicherte Arche-Nova-Daten

Nicht als autoritative Felder speichern:

- alte Punkte,
- neue Punkte,
- Gleichstandsblöcke,
- Gewinner,
- Häufigkeiten je Spezialkarte,
- lokalisierte ZooPlan-Namen,
- lokalisierte Spezialkarten-Namen,
- Materialsprache pro Partie.

Diese Werte werden aus Rohdaten, stabilen IDs und aktuellem Content abgeleitet.

## 15. Persistenzschutz

Zusätzlich zu Domain- und Use-Case-Regeln schützt das relationale Modell unter anderem:

- `ArkNovaGroupPlayer`: PK `(GroupId, PlayerId)`;
- eindeutige Defaultfarbe je `(GroupId, DefaultColor)`;
- `ArkNovaGameSessionPlayer`: PK `(GameSessionId, PlayerId)`;
- eindeutige Partiefarbe je `(GameSessionId, Color)`;
- verschiedene ZooPlanoptionen;
- Auswahl muss einer angebotenen Option entsprechen;
- Wertebereiche für Attraktivität, Artenschutz und Ruf;
- Spezialkarten-Assignment-PK
  `(GameSessionId, PlayerId, SpecialCardId)`;
- Restrict auf globale Spezialkarten;
- Cascade vom allgemeinen Partieteilnehmer auf spielabhängige Partiedaten.

Die GameSession-weite Regel „kein Zooplan wird doppelt angeboten“ wird nicht durch
einen einzelnen Tabellen-Constraint ausgedrückt; sie bleibt eine
Application-/Domainregel beim Start der Partie.

Import-/Exportmodell und Matching:
[../../IMPORT_EXPORT.md](../../IMPORT_EXPORT.md).
