# Boardgame Tracker – Fachliches Datenmodell

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
- IsArchived
- CreatedAt (DateTimeOffset, UTC-Anlagezeitpunkt)
```

Regeln:

- Gast und reguläre Spieler verwenden denselben Datentyp.
- Ein Spieler kann später von Gast zu regulär wechseln, ohne seine ID
  oder Historie zu verlieren.
- `IsArchived` ist ein boolescher Zustand.
- `CreatedAt` wird beim Erzeugen eines Spielers gesetzt und durch Bearbeiten,
  Archivieren oder Reaktivieren nicht verändert. Kein abgeleitetes Datum.
- Player- und Group-Namen sind global eindeutig nach `Trim()` und `StringComparer.OrdinalIgnoreCase`.

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

`Id` ist die stabile Identität eines Spiels; `Key` erkennt ein eingebautes
Spielmodul. Der Anzeigename ist nicht die Identität. Namen eingebauter Spiele
werden lokalisiert. Benutzerdefinierte Spiele und ihre spätere Ergebniskonfiguration
sind Zielbild gemäß `PRODUCT_SPEC.md`, noch kein implementierter Slice.

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
- die Default-Reihenfolge festlegen,
- optionale Farbdefaults für das aktuelle Spiel nach dessen erlaubter Teilmenge wählen.

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
Arche-Nova-Defaultfarben werden beim Anlegen und Bearbeiten über denselben allgemeinen UI-Bereich gepflegt.

------------------------------------------------------------------------

## 6. Arche-Nova-Gruppendaten

### Gemeinsame Farben und spielabhängige Defaults

`PlayerColor` gehört zum gemeinsamen Kern und definiert zwölf fachliche Farbkategorien ohne UI-Hexwerte. Spiele wählen eine erlaubte Teilmenge; für Arche Nova gilt die unten aufgeführte Auswahl. Spielobjekte validieren die Teilmenge auch dann, wenn ein Wert im globalen Enum definiert ist.

Farbdefaults bleiben pro Spiel, Gruppe und Mitglied gespeichert. Die `ArkNovaGroupPlayer.DefaultColor`-Spalte und die Farbspalte der Partieteilnehmer verwenden diesen gemeinsamen Typ. Die Integerwerte bleiben unverändert: Blau=1, Gelb=2, Rot=3, Schwarz=4. Diese numerischen Werte sind persistierte Identitäten und bleiben stabil. Es wird keine globale Spielerfarbe auf `Player` eingeführt. Zusätzliche Spielmodule besitzen ihre eigenen fachlichen Defaults und Persistenzabbildungen.

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

Innerhalb einer Arche-Nova-Gruppe dürfen gesetzte Defaultfarben nicht doppelt
vergeben werden. Ohne gespeicherten Default existiert für dieses Mitglied kein
`ArkNovaGroupPlayer`-Eintrag; `DefaultColor` selbst ist nicht nullable.

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
Eine künstliche ZooPlan-ID wird nicht verwendet. Der persistierte Katalog
enthält nur Nummern; sprachabhängige Namen sind Content-Metadaten aus
Games/ArkNova/Content/content.json, adressiert über Number. Bestehende
sprachneutrale Skizzen bleiben App-Ressourcen. Keine zusätzlichen Content-Keys.

Jeder Spieler erhält genau zwei verschiedene Pläne und wählt einen
davon.

------------------------------------------------------------------------

## 9. Arche-Nova-Spezialkarten

`ArkNovaSpecialCard` ist eine persistierte Entity mit stabiler GUID, kein Enum:

```text
ArkNovaSpecialCard
- Id (Guid, nicht leer)
- Symbol1? / Symbol2? / Symbol3? (geordnet, ohne Lücken)
- IsArchived

ArkNovaGameSessionSpecialCardAssignment
- GameSessionId
- PlayerId
- SpecialCardId
PK: (GameSessionId, PlayerId, SpecialCardId)
```

`ArkNovaCardSymbol` enthält die festen Referenzwerte Africa, America, Asia,
Australia, Europe, Bear, Bird, Herbivore, PettingAnimal, Predator, Primate,
Reptile, SeaAnimal und Science. Null bis drei Symbole sind erlaubt; Wiederholungen sind
zulässig. Symbole und Archivstatus bleiben global editierbarer Fachzustand der
Karte. Der sichtbare Name ist Content-Metadatum, kein persistiertes Kartenfeld.
Die UI zeigt Symbol1 → Symbol2 → Symbol3 → aktuellen Namen; keine historischen
Name-/Symbol-Snapshots und keine Hexwerte/Bilddateien im Domainmodell.

Der Karten-FK verwendet Restrict; Karten werden in der normalen UI ausschließlich
archiviert/entarchiviert. Session-Löschung entfernt Assignments über den
bestehenden GameSessionPlayer-FK, niemals globale Karten. Dieselbe Karte darf
mehreren Spielern zugeordnet werden; nur derselbe Spieler darf sie nicht doppelt haben.

Aktive Karten dürfen neu zugeordnet werden. Archivierte Karten bleiben in alten
Partien referenzierbar und können dort entfernt, aber nicht neu hinzugefügt werden.
Der Korrekturentwurf und der atomare Speicherpfad prüfen dies; beim Speichern
wird der aktuelle Archivzustand erneut berücksichtigt.

Die Initialisierung legt fehlende Standardkarten mit stabilen IDs und Defaultsymbolen
idempotent an. Ihre Namen liegen ausschließlich im versionierten Content:

| Nr. | GUID | Mitgelieferter Content-Name / geordnete Defaultsymbole |
|---|---|---|
| 1 | `a11c0001-0000-4000-8000-000000000001` | Afrikanischer Elefant / Africa, Africa, Herbivore |
| 2 | `a11c0001-0000-4000-8000-000000000002` | Asiatischer Elefant / Asia, Asia, Herbivore |
| 3 | `a11c0001-0000-4000-8000-000000000003` | Weißkopfseeadler / America, Bird |
| 4 | `a11c0001-0000-4000-8000-000000000004` | Steinadler / Europe, Bird |
| 5 | `a11c0001-0000-4000-8000-000000000005` | Malaienbär / Asia, Bear, Predator |

Bearbeitete oder archivierte Standardkarten werden beim Seeding nicht zurückgesetzt.
Die fachlichen Kartenfelder sind SQLite-Referenzdaten, keine Preferences.
Mitgelieferte Namen werden nicht geseedet oder in Translation-Tabellen gespiegelt.
Die bestehende Karten-GUID adressiert den Content; kein NameKey, CustomName oder
zusätzlicher Localization-Key am Domainobjekt.

### ArkNovaUserContent

Eine kleine gemeinsame Abbildung innerhalb des Ark-Nova-Moduls speichert ausschließlich
veränderliche Nutzernamen:

~~~text
ArkNovaUserContent
- ResourceType (special-card oder zoo-plan)
- ObjectId (kanonische Karten-GUID bzw. Zooplannummer als Text)
- Name (Trim, 1–120 Zeichen)
PK: (ResourceType, ObjectId)
~~~

Der Ressourcentyp unterscheidet nur vorhandene Objektarten; er erzeugt keine neue
Domainidentität. „Nutzer“ ist eine logische Quelle ohne Sprachfeld. Neue Karten
und ihr Nutzername werden gemeinsam gespeichert. Eine Namensänderung aktualisiert
diese Ressource; Symboländerungen ausschließlich ArkNovaSpecialCard. Entfernen einer
Nutzerressource würde nur den Namen auf die Fallbackkette zurückführen, niemals
Symbole oder Archivstatus. Ein Reset-UI ist nicht Bestandteil des aktuellen Slice.

Die Spielmaterial-Sprache ist eine allgemeine lokale Preference pro Game.Key,
kein Feld von ArkNovaSettings, GameSession oder Ergebnissen. Es existiert keine
Content-Sprach-Entity und keine historischen Namenssnapshots.

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
aus den Punkten rekonstruierbar ist. Geteilte Plätze sind erlaubt. Automatische
Ausgangsplatzierungen verwenden Competition Ranking; die manuelle Arche-Nova-
Platzwahl darf jeden Platz des eigenen Punktegleichstandsblocks verwenden,
auch gemeinsame höhere Plätze wie `1,3,3,4`, ohne Ranglücken zu schließen.

Gleichstandsblöcke werden zur Laufzeit aus Scores und aktueller Platzierung
abgeleitet, nicht separat persistiert. Jeder Block besitzt seine Teilnehmer,
einen lückenlosen erlaubten Platzbereich entsprechend seiner Größe und eine
Zuordnung, die geteilte Plätze erlaubt. Die Auswahl ändert nur den Rang des
gewählten Spielers. Platzgruppen sind eine UI-Projektion der aktuellen Zuordnung,
keine zusätzlichen Entities. Blöcke überschneiden sich nicht; Spieler mit
unterschiedlichen Punkten können nicht gegeneinander verschoben werden.
Jeder Spieler mit `Placement = 1` gilt als Gewinner.

Die globalen Arche-Nova-Präferenzen `Ruf erfassen` und `alte/neue
Punkteanzeige` sind keine historischen Spieldaten und werden deshalb über den
nativen MAUI-Preferences-Speicher gespeichert, nicht in SQLite.

------------------------------------------------------------------------

### Historische Korrekturen

Completed-Partien sind über einen expliziten Korrektur-Anwendungsfall bearbeitbar; keine Transition zurück zu Scoring. Rohwerte, besondere Karten und neu validierte Placements werden atomar gespeichert. Spiel-, Gruppen-, Teilnehmer- und Setupdaten sowie Start-/Endzeit bleiben unverändert. Spieler- und Gruppennamen werden über aktuelle Player-/Group-Referenzen aufgelöst. Kartennamen und Symbole werden über die aktuelle Spezialkarten-ID aufgelöst. Löschen einer Completed-Partie entfernt alle abhängigen Partiedaten kaskadierend, niemals Spieler oder Gruppen. Keine Namens-Snapshots; Spezialkarten referenzieren die in Abschnitt 9 beschriebenen aktuellen Karten.

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
- Häufigkeiten je Spezialkarte.

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
- Composite Key für Spezialkarten `(GameSessionId, PlayerId, SpecialCardId)`.

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

## Identität und Namenseindeutigkeit

Neue reguläre Entities erhalten ihre ID in C# über `Guid.CreateVersion7()`.
Bestehende IDs bleiben gültig; Import übernimmt jede gültige, nichtleere GUID
unverändert. Deterministische IDs fester Referenzdaten bleiben unverändert.
Player.IsArchived ist false für aktive und true für archivierte Spieler; es gibt
keine inverse Aktivitätsproperty. Die feste Group-Zusammensetzung bleibt erhalten.

Player- und Group-Namen sind einschließlich archivierter Datensätze global eindeutig.
Create, Rename und Import verwenden Trim + OrdinalIgnoreCase. SQLite erzwingt dies
auch über eindeutige Namensindizes mit derselben Collation. Keine Fuzzy-, Akzent-,
Satzzeichen- oder innere-Leerzeichen-Normalisierung.

Bekannte Spiele behalten Game.Key als sprachneutrale technische Identität und
lokalisierte Anzeigenamen. Aktuell gibt es nur Arche Nova. Custom Games und damit
benutzerdefinierte Game-Namen/-Ergebnisfelder sind noch nicht implementiert und
werden vom Import abgelehnt. Ihre spätere Namensprüfung muss auch die lokalisierten
Namen bekannter Spiele reservieren; Anzeigenamen werden dadurch keine Identität.

Austauschmodell und Referenzintegrität stehen in IMPORT_EXPORT.md.
