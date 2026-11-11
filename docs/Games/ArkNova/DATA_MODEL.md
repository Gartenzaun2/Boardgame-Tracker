# Arche Nova – Datenmodell

## 1. Geltungsbereich

Diese Datei ergänzt [docs/DATA_MODEL.md](../../DATA_MODEL.md) nur um
Arche-Nova-spezifische Entities, Werte und Invarianten.

## 2. Composition

```text
ArkNovaGameSessionPlayer
- GameSessionId
- PlayerId
- SelectedZooPlanNumber
- Attractiveness?
- Conservation?
- Reputation?
```

`ArkNovaGameSessionPlayer` verwendet `(GameSessionId, PlayerId)` als PK/FK auf
den allgemeinen Sessionteilnehmer. Gruppendefaultfarben und historische Farben
liegen in den allgemeinen Farbzuordnungen. Arche Nova erlaubt dort genau
`Blue=1`, `Yellow=2`, `Red=3`, `Black=4`; vor dem Start benötigt jeder
Teilnehmer eine eindeutige Farbe.

### Partieteilnehmer

Jeder persistierte Teilnehmer besitzt genau
`SelectedZooPlanNumber` mit einer gültigen Katalogidentität. Dieselbe Zooplanidentität
darf innerhalb einer GameSession mehreren Teilnehmern zugeordnet sein.

Einzelzuweisungen und die beiden Optionen je Spieler existieren nur im
unpersistierten Vorbereitungsentwurf. Nur für die Auslosung gelten die zwei
verschiedenen Optionen pro Spieler und global eindeutige angebotene Pläne aus
`1..14`; diese Optionen sind keine Session- oder Historienfelder.

## 3. Zoopläne

```text
ArkNovaZooPlan
- Number
```

`Number` ist Primärschlüssel und stabile numerische Identität; es gibt keine
künstliche ZooPlan-GUID. Der feste Katalog verwendet:

| Persistierte `Number` | Sichtbare Kennzeichnung | Nutzung |
|---|---|---|
| `1..14` | `1..14` | Auslosung und manuelle Auswahl |
| `15` | `Turnier` | nur manuelle Auswahl |
| `16` | `0` | nur manuelle Auswahl |
| `17` | `A` | nur manuelle Auswahl |

Die numerische interne Identität wird nicht aus der sichtbaren Kennzeichnung
abgeleitet. Die manuelle Anzeige-Reihenfolge ist `1..14`, `Turnier`, `0`, `A`.
Lokalisierte Plannamen und Kennzeichnungen sind Content, keine Datenbankfelder,
und werden über die stabile Identität adressiert. Die drei Zusatzpläne erhalten
fest zugeordnete, unveränderliche Zooplangrafiken.

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
- Zooplan: kanonische Katalogidentität `1..17` als Zahl.
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

Farbe und tatsächlich gespielter Zooplan bleiben unverändert. Die GameSession bleibt `Completed`.

## 10. Persistenzschutz

Zusätzlich gelten:

- PK/FK `(GameSessionId, PlayerId)` für `ArkNovaGameSessionPlayer`;
- eindeutige verpflichtende Sessionfarben aus `Blue/Yellow/Red/Black` gemäß
  allgemeiner Farb-Persistenz;
- Gültigkeitsprüfung für die gewählte Zooplanidentität `1..17`, ohne Session-weite Eindeutigkeitsbedingung;
- Wertebereichschecks für Wertungsfelder;
- Assignment-PK `(GameSessionId, PlayerId, SpecialCardId)`;
- Restrict auf globale Spezialkarten.

Produktverhalten: [PRODUCT_SPEC.md](PRODUCT_SPEC.md).
UI: [UI_DESIGN_GUIDE.md](UI_DESIGN_GUIDE.md).
Allgemeiner Import/Export: [../../IMPORT_EXPORT.md](../../IMPORT_EXPORT.md).
Arche-Nova-Transferschema: [IMPORT_EXPORT.md](IMPORT_EXPORT.md).

## 11. Lokaler Bilderkennungsdatensatz

Der Datensatz liegt unabhängig von SQLite und historischen Partiedaten in einem
app-internen, vom Systembackup ausgeschlossenen Verzeichnis. Pro Sample existieren
JPEG und JSON-Metadaten; eine separate Preference hält Sammlung an/aus. Es gibt
keine SQLite-Bild-BLOBs und keine automatische Retention.

Ein lokales Sample enthält Dataset-ID, GameSession-Zuordnung, Content-Hash des
normalisierten Bildinhalts, Quelle/App-/Pipeline-/Baseline-Kennung, unveränderliche
Prediction, Reviewstatus und optionale Ground Truth. Dedupliziert wird innerhalb
derselben Wertung über GameSession-ID plus Content-Hash. Die Dataset-Bildkopie ist
orientierungsbereinigt, metadatenfrei und auf etwa 1600 px lange Kante begrenzt.

Ground Truth enthält die finalen Rohwerte je Spielerfarbe und wird höchstens einmal
nach regulärem Partieabschluss zugeordnet. Prediction und vorhandene Ground Truth
werden nicht überschrieben; Samples ohne Ground Truth bleiben erhalten. Verwerfen
oder Löschen der Partie entfernt Dataset-Samples nicht.
