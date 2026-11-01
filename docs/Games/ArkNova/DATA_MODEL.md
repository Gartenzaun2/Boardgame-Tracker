# Arche Nova – Datenmodell

## 1. Geltungsbereich

Diese Datei ergänzt [docs/DATA_MODEL.md](../../DATA_MODEL.md) nur um
Arche-Nova-spezifische Entities, Werte und Invarianten.

## 2. Composition

```text
ArkNovaGroupPlayer
- GroupId
- PlayerId
- DefaultColor

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

Beide verwenden `(ParentId, PlayerId)` als PK/FK auf den allgemeinen Datensatz und
duplizieren keinen allgemeinen Zustand.

### Gruppendefaults

`DefaultColor` ist nicht nullable; ohne Default existiert kein
`ArkNovaGroupPlayer`-Datensatz. Erlaubt sind:

```text
Blue=1 Yellow=2 Red=3 Black=4
```

`(GroupId, DefaultColor)` ist eindeutig.

### Partieteilnehmer

`Color` ist pro GameSession eindeutig. Zooplanoptionen müssen verschieden sein;
`SelectedZooPlanNumber` muss eine angebotene Option sein.

Jeder persistierte Teilnehmer besitzt einen ausgewählten Plan. Über die gesamte
GameSession darf ein Zooplan nur einmal angeboten werden; diese übergreifende Regel
wird in der Startlogik geschützt.

## 3. Zoopläne

```text
ArkNovaZooPlan
- Number
```

`Number` ist Primärschlüssel und stabile natürliche Identität. Der feste Katalog
umfasst `1..14`; es gibt keine künstliche ZooPlan-GUID.

Lokalisierte Namen sind Content, keine Datenbankfelder, und werden über die Nummer
adressiert.

## 4. Spezialkarten

```text
ArkNovaSpecialCard
- Id
- Symbol1?
- Symbol2?
- Symbol3?
- IsArchived
```

`Id` ist eine stabile nichtleere GUID. Null bis drei geordnete Symbole sind erlaubt;
Lücken sind verboten, Wiederholungen erlaubt.

Persistierte Symbolwerte:

```text
1 Africa       2 America      3 Asia
4 Australia    5 Europe       6 Bear
7 Bird         8 Herbivore    9 PettingAnimal
10 Predator    11 Primate     12 Reptile
13 SeaAnimal   14 Science
```

Der Kartenname ist Content, kein Feld der Entity.

### Assignments

```text
ArkNovaGameSessionSpecialCardAssignment
- GameSessionId
- PlayerId
- SpecialCardId

PK: (GameSessionId, PlayerId, SpecialCardId)
```

Der Teilnehmer-FK kaskadiert mit der GameSession; der Karten-FK verwendet Restrict.
Eine Karte darf mehreren Spielern gehören, aber nicht doppelt demselben Spieler.

Neue Assignments benötigen eine aktive Karte. Historisch zugeordnete archivierte
Karten bleiben referenzierbar.

## 5. Feste Spezialkarten

Stabile Seed-IDs und Defaultsymbole:

| Seed | GUID | Defaultsymbole |
|---|---|---|
| AfricanElephant | `a11c0001-0000-4000-8000-000000000001` | Africa, Africa, Herbivore |
| AsianElephant | `a11c0001-0000-4000-8000-000000000002` | Asia, Asia, Herbivore |
| BaldEagle | `a11c0001-0000-4000-8000-000000000003` | America, Bird |
| GoldenEagle | `a11c0001-0000-4000-8000-000000000004` | Europe, Bird |
| SunBear | `a11c0001-0000-4000-8000-000000000005` | Asia, Bear, Predator |

Namen liegen in `Application/Games/ArkNova/Content/content.json`; vorhandener
Kartenzustand wird durch Initialisierung nicht auf Defaults zurückgesetzt.

## 6. Nutzercontent

```text
ArkNovaUserContent
- ResourceType
- ObjectId
- Name

PK: (ResourceType, ObjectId)
```

`ResourceType` ist `special-card` oder `zoo-plan`.

- Spezialkarte: kanonische nichtleere GUID im `D`-Format.
- Zooplan: kanonische Nummer `1..14`.
- `Name`: Trim, `1..120` Zeichen.

Nutzercontent verändert nur den Namen. Symbole/Archivstatus bleiben im Domainobjekt.
Eine neue nutzerdefinierte Spezialkarte und ihr Nutzername werden gemeinsam
gespeichert. Namensänderungen ändern ausschließlich `ArkNovaUserContent`,
Symboländerungen ausschließlich `ArkNovaSpecialCard`.

Das Entfernen einer Nutzerressource fällt nur auf die normale Contentauflösung
zurück und verändert weder Symbole noch Archivstatus. Built-in-Content wird nicht
in SQLite gespiegelt.

## 7. Wertung

Persistierte Rohwerte:

```text
Attractiveness? 0..113
Conservation?   0..41
Reputation?     null oder 1..15
```

Für `Completed` müssen Attraktivität und Artenschutz gesetzt sein; Ruf und
Spezialkarten bleiben optional.

Berechnete Punkte werden nicht persistiert:

```text
NewScore = Attractiveness + ConservationAddend[Conservation]
OldScore = NewScore - 100
```

`ConservationAddend` ist die feste Lookup-Tabelle aus `ArkNovaScoringRules`.

## 8. Placement

Automatische Ausgangsplatzierung verwendet Competition Ranking.

Bei Punktgleichstand darf ein Spieler nur innerhalb des ursprünglichen
Gleichstandsbereichs verschoben werden. Geteilte Plätze und Ranglücken wie
`1,3,3,4` sind erlaubt. Gleichstandsblöcke werden abgeleitet und nicht persistiert;
Spieler verschiedener Scores dürfen nicht gegeneinander verschoben werden.

Die Anzahl unterstützter Artenschutzprojekte wird nicht persistiert.

## 9. Historische Korrektur

Korrekturfähige persistierte Daten:

- Attraktivität;
- Artenschutz;
- Ruf;
- Spezialkarten-Assignments;
- neu validierte Placements.

Farbe und Zooplan-Setup bleiben unverändert. Die GameSession bleibt `Completed`.

## 10. Persistenzschutz

Zusätzlich gelten:

- PK/FK `(GroupId, PlayerId)` für `ArkNovaGroupPlayer`;
- Unique `(GroupId, DefaultColor)`;
- PK/FK `(GameSessionId, PlayerId)` für `ArkNovaGameSessionPlayer`;
- Unique `(GameSessionId, Color)`;
- Checks für verschiedene/ausgewählte Zoopläne;
- Wertebereichschecks für Wertungsfelder;
- Assignment-PK `(GameSessionId, PlayerId, SpecialCardId)`;
- Restrict auf globale Spezialkarten.

Produktverhalten: [PRODUCT_SPEC.md](PRODUCT_SPEC.md).
UI: [UI_DESIGN_GUIDE.md](UI_DESIGN_GUIDE.md).
Allgemeiner Import/Export: [../../IMPORT_EXPORT.md](../../IMPORT_EXPORT.md).
Arche-Nova-Transferschema: [IMPORT_EXPORT.md](IMPORT_EXPORT.md).
